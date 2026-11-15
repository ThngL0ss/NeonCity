using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Bãi rác công nghệ (Rìa Phố Neon, KHÔNG vào được): những đống rác chất cao, người nhặt rác rách rưới.
    ///  • Người ăn xin: "Lạy ông đi qua, lạy bà đi lại" / "Đã 2 ngày tôi chưa được ăn… giúp tôi với" → {Tiền của ông đây = đưa 30 xu} / {Ta không có tiền}.
    ///  • Đi tiếp một đoạn thì gặp 3 tên cướp. Nếu đã đưa tiền: "Tên này có tiền, CƯỚP CỦA NÓ!!!"; nếu không: "Thằng chó bên trên này kẹt sỉn thật! CƯỚP CỦA NÓ!!!".
    ///    Chúng lao vào, rồi 8 tên nữa (hạng C và B) lần lượt xuất hiện. Hạ hết → rơi con dao đỏ (vật phẩm nhiệm vụ, không phải trang bị), nhặt bằng phím tương tác.
    /// Máy trạng thái chạy trong Update (tự dừng khi toà nhà bị ẩn lúc người chơi vào nhà khác).
    /// </summary>
    public class JunkSite : MonoBehaviour
    {
        const int FirstWave = 3, Total = 11, MaxAlive = 7;
        const float TriggerDx = 14f;

        enum S { Idle, Talk, Fight, Done }
        S st = S.Idle;

        Transform root;
        float beggarX, waveT;
        int side = 1, spawned;
        Vector3 lastDeath;
        readonly List<RimFoe> foes = new List<RimFoe>();
        static TownNpc beggar;

        static string RobberName { get { return Cfg.L("TÊN CƯỚP", "ROBBER"); } }

        /// <summary>Gọi từ StreetBuildings.Place (x = tâm mặt tiền).</summary>
        public static void Place(Transform building, float x)
        {
            // vài gò rác đứng riêng phía trước cho có chiều sâu
            float[] px = { -19f, -9f, 9f, 19f };
            for (int i = 0; i < px.Length; i++)
                Art.Spr(building, "Pile", RimArt.JunkPile(112 + 16 * (i % 2), 40 + 6 * (i % 3), i + 1), new Vector2(px[i], 0f), Vector2.one, Color.white, -53);

            // người nhặt rác (không nói chuyện)
            float[] sx = { -14f, -4f, 5f, 12f, 20f };
            for (int i = 0; i < sx.Length; i++)
                TownNpc.Spawn(building, RimLooks.Scav(i), x + sx[i], TownNpc.Mode.Wander, null, null, x + sx[i] - 2.5f, x + sx[i] + 2.5f, i % 2 == 0);

            // người ăn xin ngồi ở đầu bãi
            float bx = x - 15f;
            beggar = TownNpc.Spawn(building, RimLooks.Beggar, bx, TownNpc.Mode.Sit, Cfg.L("NGƯỜI ĂN XIN", "BEGGAR"), TalkBeggar, -1e9f, -1e9f, false);
            beggar.range = 2.6f;

            var go = new GameObject("JunkEvent");
            go.transform.SetParent(building, false);
            var site = go.AddComponent<JunkSite>();
            site.root = building;
            site.beggarX = bx;
            // đã hạ hết cướp nhưng chưa nhặt dao (thoát trước khi nhặt): đặt lại dao ở bãi
            if (Town.RobbersDone && !Town.KnifeTaken) site.DropKnife(new Vector3(x + 2f, 0f, 0f));
            if (Town.RobbersDone) site.st = S.Done;
        }

        // ================================================================ người ăn xin
        static void TalkBeggar()
        {
            Transform who = beggar != null ? beggar.transform : null;
            string nm = Cfg.L("NGƯỜI ĂN XIN", "BEGGAR");
            Color col = new Color(0.85f, 0.75f, 0.6f);
            var l = new List<DlgLine>();
            if (Town.Beggar != 0)
            {
                l.Add(new DlgLine(nm, "…..", col, who));
                DialogueUi.Begin(l, null);
                return;
            }
            l.Add(new DlgLine(nm, Cfg.L("Lạy ông đi qua, lạy bà đi lại", "Bless you, sir who passes by, madam who comes and goes"), col, who));
            string[] opts = { Cfg.L("Tiền của ông đây", "Here's your money"), Cfg.L("Ta không có tiền", "I have no money") };
            l.Add(DlgLine.Ask(nm, Cfg.L("Đã 2 ngày tôi chưa được ăn… giúp tôi với", "I haven't eaten in 2 days… please help me"), col, who, opts, i =>
            {
                if (i == 0)
                {
                    if (Inventory.Spend(30))
                    {
                        Town.Beggar = 1;
                        Sfx.Play("pickup", 0.9f, 1f);
                        DialogueUi.Close();
                    }
                    else DialogueUi.Close(() => GameManager.Banner(Cfg.L("KHÔNG ĐỦ XU", "NOT ENOUGH COINS"), Cfg.Red, 2f));
                }
                else
                {
                    Town.Beggar = 2;
                    DialogueUi.Close();
                }
            }));
            DialogueUi.Begin(l, null);
        }

        // ================================================================ đám cướp
        void Update()
        {
            if (st == S.Done) { enabled = false; return; }
            if (BaseUi.IsOpen || !GameManager.Active || Interior.Busy) return;
            var pl = Player.Instance;
            if (pl == null || pl.Dead) return;

            switch (st)
            {
                case S.Idle:
                    if (pl.Position.x > beggarX + TriggerDx) BeginAmbush(pl);
                    break;

                case S.Fight:
                    foes.RemoveAll(f => f == null || f.dead);
                    waveT -= Time.deltaTime;
                    if (spawned < Total && waveT <= 0f && foes.Count < MaxAlive)
                    {
                        SpawnRobber(pl.Position.x, spawned);
                        waveT = 2.8f;
                    }
                    if (spawned >= Total && foes.Count == 0) Finish();
                    break;
            }
        }

        void BeginAmbush(Player pl)
        {
            st = S.Talk;
            side = pl.Position.x + 16f <= Cfg.LevelMaxX - 3f ? 1 : -1;
            foes.Clear();
            spawned = 0;
            for (int i = 0; i < FirstWave; i++) SpawnRobber(pl.Position.x, i, 11f + i * 1.8f);
            lastDeath = new Vector3(pl.Position.x, 0f, 0f);
            string line = Town.Beggar == 1
                ? Cfg.L("Tên này có tiền, CƯỚP CỦA NÓ!!!", "This one has money, ROB HIM!!!")
                : Cfg.L("Thằng chó bên trên này kẹt sỉn thật! CƯỚP CỦA NÓ!!!", "This cheap bastard from above! ROB HIM!!!");
            Transform who = foes.Count > 0 ? foes[0].transform : null;
            var l = new List<DlgLine> { new DlgLine(RobberName, line, Cfg.Red, who) };
            DialogueUi.Begin(l, BeginFight);
        }

        void BeginFight()
        {
            if (st != S.Talk) return;
            st = S.Fight;
            waveT = 3.5f;
            for (int i = 0; i < foes.Count; i++) if (foes[i] != null) foes[i].Wake();
        }

        /// <summary>Sinh một tên cướp ở rìa phía trước người chơi (index càng lớn càng mạnh: 3 tên đầu + 5 tên hạng C, 3 tên hạng B cuối).</summary>
        void SpawnRobber(float playerX, int index, float dist = 17f)
        {
            float x = Mathf.Clamp(playerX + side * dist, Cfg.LevelMinX + 1f, Cfg.LevelMaxX - 1f);
            Rank rank = index >= Total - 3 ? Rank.B : Rank.C;
            int level = 6 + index / 2 + (rank == Rank.B ? 3 : 0);
            bool ranged = index % 4 == 3;
            var f = RimFoe.Spawn(root, RimFoe.Kind.Robber, RimLooks.Robber(index), x, level, rank, ranged);
            f.minX = Cfg.LevelMinX; f.maxX = Cfg.LevelMaxX;
            f.FaceDir(-side);
            f.onDied = d => lastDeath = d.transform.position;
            if (st == S.Fight) f.Wake();
            foes.Add(f);
            spawned++;
            if (index >= FirstWave)
            {
                Fx.Burst(f.Center, Cfg.A(Cfg.Red, 0.8f), 6, 5f, 0.4f, 0.25f);
                Sfx.PlayAt("spawn", new Vector3(x, 1f, 0f), 0.6f, 1.1f);
            }
        }

        void Finish()
        {
            st = S.Done;
            Town.RobbersDone = true;
            DropKnife(new Vector3(Mathf.Clamp(lastDeath.x, Cfg.LevelMinX + 2f, Cfg.LevelMaxX - 2f), 0f, 0f));
        }

        // ================================================================ dao đỏ
        void DropKnife(Vector3 pos)
        {
            var go = new GameObject("RedKnife");
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            Art.Spr(go.transform, "Knife", RimArt.Knife(), new Vector2(0f, 0.1f), Vector2.one, Color.white, 12);
            Art.Spr(go.transform, "Glow", Art.Glow, new Vector2(0f, 0.4f), new Vector2(2.2f, 1.2f), Cfg.A(new Color(1f, 0.15f, 0.15f), 0.35f), 11, true);
            Hotspot.Make(go.transform, pos, "NHẶT DAO ĐỎ", "PICK UP THE RED KNIFE", () =>
            {
                if (Town.KnifeTaken) return;
                Town.KnifeTaken = true;
                QuestItems.Give(QuestItems.RedKnife);
                Destroy(go);
            }, 1.8f);
        }
    }
}
