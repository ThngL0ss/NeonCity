using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Phòng thí nghiệm của Dyber (Rìa Phố Neon): bức tường dài + cổng neon lớn, KHÔNG vào được. Hai robot canh cổng ngoài.
    ///  • Người chơi lại gần cửa → cả hai hiện "!" → chờ 1,5 s → mắt chuyển đỏ + nói "K….ẻ" "Kẻ Thù!!!" → tấn công.
    ///  • Giao tranh càng lâu, càng có thêm robot / quái (mọi loại quái màn đầu, không có boss) từ cổng tràn ra để giết người chơi.
    ///  • Nếu người chơi chỉ chạy và KHÔNG đánh trả trong 10 s: robot trở lại bình thường và về chỗ cũ, quái thêm biến mất.
    /// Mọi thứ là con của toà nhà (ẩn theo toà nhà khi người chơi vào nhà khác).
    /// </summary>
    public class LabSite : MonoBehaviour
    {
        const float TriggerDist = 7.5f, AlertTime = 1.5f, QuietLimit = 10f;
        const int GateLevel = 8, MaxExtras = 16;

        enum S { Calm, Alert, Speak, Hostile }
        S st = S.Calm;
        float gateX, alertT, fightT, spawnT, quietT, lastSum;
        bool far = true;
        Transform root;
        readonly RimFoe[] robots = new RimFoe[2];
        readonly List<GameObject> bangs = new List<GameObject>();
        readonly List<Hittable> extras = new List<Hittable>();
        static Sprite bangSprite;

        /// <summary>Gọi từ StreetBuildings.Place (x = tâm toà nhà = tâm cổng).</summary>
        public static void Place(Transform building, float x)
        {
            var go = new GameObject("LabSite");
            go.transform.SetParent(building, false);
            var s = go.AddComponent<LabSite>();
            s.root = building;
            s.gateX = x;
            s.EnsureRobots();
        }

        void EnsureRobots()
        {
            for (int i = 0; i < robots.Length; i++)
            {
                if (robots[i] != null && !robots[i].dead) continue;
                float rx = gateX + (i == 0 ? -7f : 7f);
                var f = RimFoe.Spawn(root, RimFoe.Kind.LabRobot, null, rx, GateLevel, Rank.C, i == 1);
                f.minX = gateX - 46f; f.maxX = gateX + 46f;
                f.FaceDir(i == 0 ? 1 : -1);
                robots[i] = f;
            }
        }

        bool AnyRobotAggro()
        {
            for (int i = 0; i < robots.Length; i++) if (robots[i] != null && !robots[i].dead && robots[i].aggro) return true;
            return false;
        }

        // ------------------------------------------------------------ vòng lặp
        void Update()
        {
            if (BaseUi.IsOpen || !GameManager.Active || Interior.Busy) return;
            var pl = Player.Instance;
            if (pl == null) return;
            float dt = Time.deltaTime;
            float dist = Mathf.Abs(pl.Position.x - gateX);

            switch (st)
            {
                case S.Calm:
                    if (dist > 16f) { far = true; EnsureRobots(); }
                    if (pl.Dead) return;
                    if (AnyRobotAggro()) { BeginFight(); return; }
                    if (far && dist < TriggerDist) { far = false; BeginAlert(pl); }
                    break;

                case S.Alert:
                    alertT -= dt;
                    for (int i = 0; i < robots.Length; i++)
                        if (robots[i] != null && !robots[i].dead) robots[i].FaceDir(pl.Position.x >= robots[i].transform.position.x ? 1 : -1);
                    if (AnyRobotAggro()) { ClearBangs(); BeginFight(); return; }
                    if (alertT <= 0f) { ClearBangs(); BeginSpeak(); }
                    break;

                case S.Hostile:
                    fightT += dt; quietT += dt;
                    extras.RemoveAll(h => h == null || h.dead);
                    float sum = CurrentSum();
                    if (sum < lastSum - 0.01f) quietT = 0f;            // có kẻ địch mất máu / bị hạ = người chơi (hoặc lính) đang đánh trả
                    lastSum = sum;
                    spawnT -= dt;
                    if (spawnT <= 0f)
                    {
                        SpawnExtra();
                        spawnT = Mathf.Lerp(6f, 2.2f, Mathf.Clamp01(fightT / 60f));
                        lastSum = CurrentSum();
                    }
                    if (quietT >= QuietLimit || pl.Dead) EndFight();
                    break;
            }
        }

        float CurrentSum()
        {
            float s = 0f;
            for (int i = 0; i < robots.Length; i++) if (robots[i] != null && !robots[i].dead) s += robots[i].hp;
            for (int i = 0; i < extras.Count; i++) if (extras[i] != null && !extras[i].dead) s += extras[i].hp;
            return s;
        }

        // ------------------------------------------------------------ các giai đoạn
        void BeginAlert(Player pl)
        {
            st = S.Alert;
            alertT = AlertTime;
            for (int i = 0; i < robots.Length; i++)
            {
                if (robots[i] == null || robots[i].dead) continue;
                robots[i].FaceDir(pl.Position.x >= robots[i].transform.position.x ? 1 : -1);
                bangs.Add(Bang(robots[i].transform));
            }
            Sfx.PlayAt("warn", new Vector3(gateX, 1f, 0f), 0.8f, 1.1f);
        }

        void BeginSpeak()
        {
            st = S.Speak;
            var live = new List<RimFoe>();
            for (int i = 0; i < robots.Length; i++)
                if (robots[i] != null && !robots[i].dead) { robots[i].SetEye(true); live.Add(robots[i]); }
            if (live.Count == 0) { st = S.Calm; return; }
            string who = Cfg.L("ROBOT CANH CỔNG", "GATE ROBOT");
            var l = new List<DlgLine>();
            l.Add(new DlgLine(who, Cfg.L("K….ẻ", "En….emy"), Cfg.Red, live[0].transform));
            l.Add(new DlgLine(who, Cfg.L("Kẻ Thù!!!", "ENEMY!!!"), Cfg.Red, live[live.Count - 1].transform));
            DialogueUi.Begin(l, BeginFight);
        }

        void BeginFight()
        {
            st = S.Hostile;
            fightT = 0f; quietT = 0f; spawnT = 3.5f;
            for (int i = 0; i < robots.Length; i++)
                if (robots[i] != null && !robots[i].dead) { robots[i].SetEye(true); robots[i].Wake(); }
            lastSum = CurrentSum();
        }

        void EndFight()
        {
            st = S.Calm;
            far = false;                                   // chỉ cảnh giác lại khi người chơi đã đi xa rồi quay lại
            for (int i = 0; i < robots.Length; i++)
                if (robots[i] != null && !robots[i].dead) robots[i].Retreat();
            for (int i = 0; i < extras.Count; i++)
            {
                var h = extras[i];
                if (h == null || h.dead) continue;
                var f = h as RimFoe;
                if (f != null) f.Vanish();
                else { var e = h as Enemy; if (e != null) e.Banish(); }
            }
            extras.Clear();
        }

        // ------------------------------------------------------------ quân tiếp viện
        static List<GameObject> Pool()
        {
            var l = new List<GameObject>();
            if (SkillSystem.HasConfig)
            {
                var list = SkillSystem.Config.craftables;
                for (int i = 0; i < list.Count; i++)
                {
                    var e = list[i];
                    if (e == null || e.enemyPrefab == null || e.enemyPrefab.GetComponent<Enemy>() == null) continue;
                    if (!l.Contains(e.enemyPrefab)) l.Add(e.enemyPrefab);
                }
            }
            if (l.Count == 0 && Art.Bank != null)
            {
                if (Art.Bank.walkerPrefab != null) l.Add(Art.Bank.walkerPrefab);
                if (Art.Bank.flyerPrefab != null) l.Add(Art.Bank.flyerPrefab);
            }
            return l;
        }

        void SpawnExtra()
        {
            if (extras.Count >= Mathf.Min(MaxExtras, 3 + (int)(fightT / 7f))) return;
            float ex = gateX + Random.Range(-2.5f, 2.5f);
            int lv = Mathf.Min(40, GateLevel + 1 + (int)(fightT / 12f));
            var pool = Pool();
            int roll = Random.Range(0, 3);                 // 0 robot cận chiến · 1 robot bắn xa · 2 quái màn đầu
            Hittable h = null;
            if (roll < 2 || pool.Count == 0)
            {
                var f = RimFoe.Spawn(root, RimFoe.Kind.LabRobot, null, ex, lv, Rank.C, roll == 1);
                f.minX = gateX - 46f; f.maxX = gateX + 46f;
                f.SetEye(true);
                f.Wake();
                h = f;
            }
            else
            {
                var prefab = pool[Random.Range(0, pool.Count)];
                bool fly = prefab.GetComponent<Flyer>() != null;
                var e = Enemy.Spawn(prefab, new Vector2(ex, fly ? 3.5f : 0.3f), null, lv);
                if (e != null) { e.transform.SetParent(root, true); h = e; }
            }
            if (h == null) return;
            extras.Add(h);
            Fx.Ring(h.Center, Cfg.A(Cfg.Orange, 0.9f), 0.3f, 2.4f, 0.3f);
            Fx.Burst(h.Center, Cfg.A(Cfg.Orange, 0.9f), 8, 6f, 0.5f, 0.3f);
            Sfx.PlayAt("spawn", new Vector3(ex, 1f, 0f), 0.7f, 1f);
        }

        // ------------------------------------------------------------ dấu "!" trên đầu
        static GameObject Bang(Transform parent)
        {
            if (bangSprite == null)
            {
                var c = new PxCanvas(16, 16);
                c.Disc(8, 8, 7, PxCanvas.C(0xFF2A2A));
                c.TextC(8, 3, "!", PxCanvas.C(0xFFFFFF), 2);
                c.Outline(PxCanvas.C(0x0A0A16));
                bangSprite = c.Bake(0.5f, 0.5f);
            }
            var sr = Art.Spr(parent, "Bang", bangSprite, new Vector2(0f, 2.5f), new Vector2(1.2f, 1.2f), Color.white, 40);
            return sr.gameObject;
        }

        void ClearBangs()
        {
            for (int i = 0; i < bangs.Count; i++) if (bangs[i] != null) Destroy(bangs[i]);
            bangs.Clear();
        }
    }
}
