using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Sự kiện mở cổng Lều trại ở Rìa Phố Neon (chỉ khi Town.RyberOpen = false). Lúc đầu người chơi chưa vào được trại.
    /// Khi đi qua trại một đoạn: một cậu bé ôm túi tiền chạy tới, sau lưng là 2 lính áo giáp + mũ trắng đuổi theo.
    /// Lính: "Tránh ra, thằng nhóc đó dám trộm tiền của “VIP”!!!" → chọn "Được thôi" (tấn công bất ngờ: lính choáng + mất máu trước)
    /// hoặc "Đừng hòng, để cậu bé yên" (đánh bình thường). Hạ cả hai: cậu bé cảm ơn, dẫn người chơi tới cổng trại rồi mở cổng (Town.RyberOpen).
    /// Là máy trạng thái chạy trong Update (tự dừng khi toà nhà bị ẩn lúc người chơi vào nhà khác).
    /// </summary>
    public class RyberKid : MonoBehaviour
    {
        enum S { Wait, Run, Talk, Fight, Thanks, Lead, Done }
        S st = S.Wait;

        Transform root;
        float campX, gateX, runT, speed = 7.4f;
        int dir;                                                // hướng chạy của cả nhóm (về phía người chơi)

        Transform kid, kidVis;
        SpriteRenderer kidSr;
        Sprite k0, k1, k2;
        float kidAnim;
        int kidFace = 1;
        readonly RimFoe[] soldiers = new RimFoe[2];

        public void Init(Transform building, float campCenterX, float gateWorldX)
        {
            root = building; campX = campCenterX; gateX = gateWorldX;
        }

        static string KidName { get { return Cfg.L("CẬU BÉ", "BOY"); } }
        static string SoldierName { get { return Cfg.L("LÍNH GIÁP TRẮNG", "WHITE-ARMOR SOLDIER"); } }

        void Update()
        {
            if (Town.RyberOpen && st == S.Wait) { enabled = false; return; }
            if (BaseUi.IsOpen || !GameManager.Active || Interior.Busy) return;
            var pl = Player.Instance;
            if (pl == null || pl.Dead) return;
            float dt = Time.deltaTime;

            switch (st)
            {
                case S.Wait:
                    if (pl.Position.x > campX + 16f) StartRun(pl);
                    break;

                case S.Run:
                    RunStep(pl, dt);
                    break;

                case S.Fight:
                    MoveKid(0f, false);
                    if ((soldiers[0] == null || soldiers[0].dead) && (soldiers[1] == null || soldiers[1].dead)) BeginThanks();
                    break;

                case S.Lead:
                    LeadStep(pl, dt);
                    break;
            }
        }

        // ------------------------------------------------------------ cậu bé + lính chạy tới
        void StartRun(Player pl)
        {
            st = S.Run;
            runT = 0f;
            speed = Mathf.Max(7.4f, pl.moveSpeed * 1.08f);
            int side = pl.Position.x + 20f <= Cfg.LevelMaxX - 3f ? 1 : -1;
            dir = -side;
            float kx = pl.Position.x + side * 20f;
            SpawnKid(kx);
            for (int i = 0; i < soldiers.Length; i++)
            {
                var f = RimFoe.Spawn(root, RimFoe.Kind.Guard, RimLooks.WhiteGuard, kx + side * (4.5f + i * 1.5f), 12, Rank.B, false);
                f.scripted = true;
                f.FaceDir(dir);
                soldiers[i] = f;
            }
            Sfx.PlayAt("warn", new Vector3(kx, 1f, 0f), 0.8f, 1.2f);
        }

        void SpawnKid(float x)
        {
            var go = new GameObject("Kid");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(x, 0f, 0f);
            kid = go.transform;
            var v = new GameObject("Visual");
            v.transform.SetParent(go.transform, false);
            kidVis = v.transform;
            kidSr = v.AddComponent<SpriteRenderer>();
            kidSr.sharedMaterial = Art.DefMat;
            kidSr.sortingOrder = 21;
            k0 = TownArt.Human(RimLooks.Kid, 0); k1 = TownArt.Human(RimLooks.Kid, 1); k2 = TownArt.Human(RimLooks.Kid, 2);
            kidSr.sprite = k0;
            var bag = Art.Spr(v.transform, "Bag", RimArt.CoinBag(), new Vector2(0.45f, 0.5f), Vector2.one, Color.white, 22);
            bag.sharedMaterial = Art.DefMat;
            kidFace = -dir;
            ApplyKidFace();
        }

        void ApplyKidFace()
        {
            if (kidVis != null) kidVis.localScale = new Vector3(kidFace * RimLooks.Kid.scale, RimLooks.Kid.scale, 1f);
        }

        void MoveKid(float dx, bool running)
        {
            if (kid == null) return;
            if (Mathf.Abs(dx) > 0.0001f) { kidFace = dx > 0f ? 1 : -1; ApplyKidFace(); }
            var p = kid.position;
            p.x += dx;
            kid.position = p;
            if (running) { kidAnim += Time.deltaTime * 10f; kidSr.sprite = ((int)kidAnim % 2 == 0) ? k1 : k2; }
            else kidSr.sprite = k0;
        }

        void RunStep(Player pl, float dt)
        {
            runT += dt;
            float px = pl.Position.x;
            // người chơi ra tay trước khi kịp nói chuyện: coi như đánh bình thường
            for (int i = 0; i < soldiers.Length; i++)
                if (soldiers[i] != null && !soldiers[i].dead && soldiers[i].aggro) { st = S.Talk; BeginFight(false); return; }
            // cậu bé chạy qua người chơi rồi nấp phía sau
            float passed = (kid.position.x - px) * dir;
            if (passed < 1.4f) MoveKid(dir * speed * dt, true);
            else MoveKid(0f, false);
            // lính đuổi theo, giữ cách cậu bé một đoạn
            for (int i = 0; i < soldiers.Length; i++)
            {
                var f = soldiers[i];
                if (f == null || f.dead) continue;
                float want = kid.position.x - dir * (4.5f + i * 1.5f);
                float step = Mathf.Clamp(want - f.transform.position.x, -speed * 1.15f * dt, speed * 1.15f * dt);
                if (Mathf.Abs(step) > 0.002f) f.Run(step); else f.Stand();
            }
            bool arrived = passed >= 1.4f && soldiers[0] != null && Mathf.Abs(soldiers[0].transform.position.x - px) < 9.5f;
            if (arrived) BeginTalk();
            else if (runT > 14f)
            {
                // người chơi chạy mất: kéo cả nhóm về sát người chơi cho gọn
                var p = kid.position; p.x = px + dir * 1.6f; kid.position = p;
                for (int i = 0; i < soldiers.Length; i++)
                    if (soldiers[i] != null) { var q = soldiers[i].transform.position; q.x = px - dir * (3f + i * 1.4f); soldiers[i].transform.position = q; }
                BeginTalk();
            }
        }

        // ------------------------------------------------------------ hội thoại + chiến đấu
        void BeginTalk()
        {
            st = S.Talk;
            MoveKid(0f, false);
            kidFace = -dir; ApplyKidFace();
            for (int i = 0; i < soldiers.Length; i++) if (soldiers[i] != null) { soldiers[i].FaceDir(dir); soldiers[i].Stand(); }
            string[] opts = { Cfg.L("Được thôi", "Sure"), Cfg.L("Đừng hòng, để cậu bé yên", "No way, leave the boy alone") };
            Transform who = soldiers[0] != null ? soldiers[0].transform : null;
            var ask = DlgLine.Ask(SoldierName,
                Cfg.L("Tránh ra, thằng nhóc đó dám trộm tiền của “VIP”!!!", "Out of the way, that brat dared to steal from a “VIP”!!!"),
                new Color(0.88f, 0.90f, 1f), who, opts, i => DialogueUi.Close(() => BeginFight(i == 0)));
            // bấm ESC bỏ qua = coi như từ chối (đánh bình thường)
            DialogueUi.Begin(new List<DlgLine> { ask }, () => { if (st == S.Talk) BeginFight(false); });
        }

        void BeginFight(bool surprise)
        {
            if (st != S.Talk) return;
            st = S.Fight;
            for (int i = 0; i < soldiers.Length; i++)
            {
                var f = soldiers[i];
                if (f == null || f.dead) continue;
                f.Wake();
                if (surprise)
                {
                    f.hp = Mathf.Max(1f, f.hp * 0.65f);
                    f.Stun(2.2f);
                    Fx.Text(f.Center + new Vector2(0f, 1.6f), Cfg.L("BẤT NGỜ!", "AMBUSH!"), Cfg.Orange, 1.1f);
                }
            }
        }

        void BeginThanks()
        {
            st = S.Thanks;
            MoveKid(0f, false);
            var pl = Player.Instance;
            if (pl != null) { kidFace = pl.Position.x >= kid.position.x ? 1 : -1; ApplyKidFace(); }
            Color c = new Color(1f, 0.82f, 0.5f);
            var l = new List<DlgLine>();
            l.Add(new DlgLine(KidName, Cfg.L("Cảm ơn anh", "Thank you"), c, kid));
            l.Add(new DlgLine(KidName, Cfg.L("Em không có gì để trả ơn anh…Nhưng chắc chị em sẽ có.", "I have nothing to repay you with… but my sister probably will."), c, kid));
            l.Add(new DlgLine(KidName, Cfg.L("Đi theo em", "Follow me"), c, kid));
            DialogueUi.Begin(l, () =>
            {
                st = S.Lead;
                GameManager.Banner(Cfg.L("ĐI THEO CẬU BÉ", "FOLLOW THE BOY"), Cfg.Orange, 2.5f);
            });
        }

        // ------------------------------------------------------------ dẫn tới cổng trại
        void LeadStep(Player pl, float dt)
        {
            float dx = gateX - kid.position.x;
            if (Mathf.Abs(dx) < 0.5f)
            {
                st = S.Done;
                MoveKid(0f, false);
                kidFace = 1; ApplyKidFace();
                Town.RyberOpen = true;
                Sfx.Play("slam", 0.8f, 0.9f);
                Fx.Ring(new Vector2(gateX, 1.5f), Cfg.A(Cfg.Orange, 0.9f), 0.5f, 4f, 0.5f);
                GameManager.Banner(Cfg.L("CỔNG TRẠI ĐÃ MỞ", "THE CAMP GATE IS OPEN"), Cfg.Orange, 3f);
                if (kid != null) Destroy(kid.gameObject, 1.2f);
                return;
            }
            // đợi người chơi nếu bị bỏ xa
            if (Mathf.Abs(pl.Position.x - kid.position.x) > 9f) { MoveKid(0f, false); return; }
            MoveKid(Mathf.Sign(dx) * Mathf.Min(6.8f, Mathf.Abs(dx) / Mathf.Max(0.001f, dt)) * dt, true);
        }
    }
}
