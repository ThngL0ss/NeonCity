using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Lều trại của Phe trộm cướp (Rìa Phố Neon). Bên ngoài: hàng rào dây thép gai rỉ quanh những chiếc lều vá víu, đống lửa lớn ở giữa,
    /// cổng sắt rỉ sơn đầu lâu. Lúc đầu KHÔNG vào được: phải qua sự kiện cậu bé bị lính áo giáp trắng đuổi (xem RyberKid) để mở cổng (Town.RyberOpen).
    /// Bên trong: những chiếc lều, người rách rưới có vũ khí, đống lửa; ở cuối trại là lều lớn có 2 người bịt mặt canh. Trong lều lớn là Rossie
    /// (nữ, Đội trưởng đội 4, áo ba lỗ, tóc ngắn). Nói chuyện với Rossie: lần đầu nhận 100 xu (Town.RossieGift); chọn đồng ý = đồng minh (Town.Rossie = 2),
    /// từ chối = Rossie hẹn dịp khác và đưa người chơi ra khỏi trại (có thể quay lại chọn đồng ý sau).
    /// </summary>
    public static class RyberBuilder
    {
        public const string Id = "RyberCamp";
        static readonly Color RossieCol = new Color(1f, 0.62f, 0.38f);

        static TownNpc rossie;
        public static void ResetStatics() { rossie = null; }

        public static Room Camp() { return Interior.GetRoom("ryber.camp", BuildCamp); }
        public static Room Tent() { return Interior.GetRoom("ryber.tent", BuildTent); }

        static string RossieName { get { return "ROSSIE"; } }

        // ================================================================ ngoài phố
        /// <summary>Gọi từ StreetBuildings.Place: ánh lửa chập chờn + máy trạng thái sự kiện cậu bé (x = tâm mặt tiền).</summary>
        public static void Place(Transform building, float x)
        {
            var glow = Art.Spr(building, "FireGlow", Art.Glow, new Vector2(0f, 2.4f), new Vector2(7f, 6f), Cfg.A(new Color(1f, 0.5f, 0.15f), 0.35f), -54, true);
            Flicker(glow, 0.5f);
            var go = new GameObject("RyberEvent");
            go.transform.SetParent(building, false);
            var k = go.AddComponent<RyberKid>();
            k.Init(building, x, x + RimArt.DoorDx(RimArt.FacadeCamp));
        }

        /// <summary>Gắn LampFlicker (tắt object trước để Awake đọc đúng mảng targets).</summary>
        static void Flicker(SpriteRenderer sr, float dip)
        {
            if (sr == null) return;
            var go = sr.gameObject;
            go.SetActive(false);
            var fl = go.AddComponent<LampFlicker>();
            fl.targets = new[] { sr };
            fl.gap = new Vector2(0.4f, 1.6f);
            fl.dip = dip;
            fl.shimmer = 0.25f;
            go.SetActive(true);
        }

        // ================================================================ trong trại
        static Room BuildCamp()
        {
            var r = new Room("ryber.camp", "LỀU TRẠI", "CAMP", Interior.Origin(5, 0), 72f, RoomStyle.Camp());
            r.entryX = 4f;
            RoomKit.ExitDoor(r, 2.4f, "RA NGOÀI", "LEAVE", Interior.Exit, 0x3A2A1A, 0xFF8A3A);

            // dây thép gai + lều vá
            r.Put("Wire", RimArt.Wire(240), 14f, 0f, -29);
            r.Put("Wire", RimArt.Wire(240), 49f, 0f, -29);
            r.Put("Tent", RimArt.Tent(0, 96, 70, 3), 11f, -0.12f, -31);
            r.Put("Tent", RimArt.Tent(2, 80, 58, 5), 22.5f, -0.12f, -31);
            r.Put("Tent", RimArt.Tent(3, 88, 62, 8), 44f, -0.12f, -31);
            r.Put("Tent", RimArt.Tent(4, 72, 52, 13), 53.5f, -0.12f, -31);

            // đống lửa lớn ở giữa
            r.Put("Bonfire", RimArt.Bonfire(), 33.5f, -0.25f, -22);
            var g1 = r.Glow(33.5f, 1.8f, 9f, 7f, Cfg.A(new Color(1f, 0.5f, 0.15f), 0.4f), -23);
            Flicker(g1, 0.55f);
            r.Glow(33.5f, 0.2f, 14f, 3f, Cfg.A(new Color(1f, 0.45f, 0.1f), 0.18f), -24);

            // đồ đạc
            r.Put("Barrel", RimArt.Barrel(0x6A3A22), 7.4f, 0f, -24);
            r.Put("Barrel", RimArt.Barrel(0x4A5A4A), 40.6f, 0f, -24);
            r.Put("Cot", RimArt.Cot(), 27.6f, 0f, -25);
            r.Put("Crate", TownArt.Crate(0x5A4A2A), 18.4f, 0f, -24);
            r.Put("Sandbags", RimArt.Sandbags(80), 68f, 0f, -23);

            // người rách rưới có vũ khí
            r.Npc(RimLooks.Crew(0), 30.2f, TownNpc.Mode.Sit);
            r.Npc(RimLooks.Crew(1), 36.8f, TownNpc.Mode.Sit);
            r.Npc(RimLooks.Crew(2), 16f, TownNpc.Mode.Wander, null, null, 12f, 21f);
            r.Npc(RimLooks.Crew(3), 41f, TownNpc.Mode.Wander, null, null, 38f, 43f, true);
            r.Npc(RimLooks.Crew(4), 49f, TownNpc.Mode.Wander, null, null, 47f, 52f);

            // lều lớn ở cuối trại + 2 người bịt mặt canh
            r.Put("BigTent", RimArt.Tent(1, 150, 108, 31), 63f, -0.12f, -30);
            r.Glow(63f, 2.5f, 9f, 5f, Cfg.A(new Color(1f, 0.6f, 0.2f), 0.12f), -29);
            r.Npc(RimLooks.MaskedGuard, 59.9f, TownNpc.Mode.Stand, null, null, -1f, -1f, false);
            r.Npc(RimLooks.MaskedGuard, 66.1f, TownNpc.Mode.Stand, null, null, -1f, -1f, true);
            r.Spot(63f, 0f, "VÀO LỀU LỚN", "ENTER THE BIG TENT", () => Interior.Go(Tent, 3.5f), 2.0f);
            return r;
        }

        // ================================================================ lều lớn của Rossie
        static Room BuildTent()
        {
            var r = new Room("ryber.tent", "LỀU LỚN", "BIG TENT", Interior.Origin(5, 1), 26f, RoomStyle.Tent());
            r.entryX = 3.5f;
            RoomKit.ExitDoor(r, 2.4f, "RA NGOÀI", "LEAVE", () => Interior.Go(Camp, 63f), 0x4A3A22, 0xFF8A3A);

            r.Put("Table", RimArt.ShabbyCounter(112), 12f, 0f, -26);
            r.Put("Cot", RimArt.Cot(), 23f, 0f, -26);
            r.Put("Crate", TownArt.Crate(0x5A4A2A), 7.4f, 0f, -24);
            r.Put("Barrel", RimArt.Barrel(0x4A5A4A), 24.9f, 0f, -24);
            r.Put("Lantern", TownArt.Lantern(0xE8923A), 8.5f, 7.2f, -26);
            r.Put("Lantern", TownArt.Lantern(0xE8923A), 17.5f, 7.2f, -26);
            r.Glow(8.5f, 7.0f, 6f, 5f, Cfg.A(new Color(1f, 0.6f, 0.2f), 0.22f), -25);
            r.Glow(17.5f, 7.0f, 6f, 5f, Cfg.A(new Color(1f, 0.6f, 0.2f), 0.22f), -25);

            rossie = r.Npc(RimLooks.Rossie, 17.6f, TownNpc.Mode.Stand, RossieName, TalkRossie, -1f, -1f, true);
            rossie.range = 3.2f;
            return r;
        }

        // ================================================================ Rossie
        static DlgLine R(Transform who, string vi, string en) { return new DlgLine(RossieName, Cfg.L(vi, en), RossieCol, who); }

        static void TalkRossie()
        {
            Transform who = rossie != null ? rossie.transform : null;
            var l = new List<DlgLine>();
            string last = "…Bọn ta không thể tiếp tục sống như này được nữa!";
            string lastEn = "…We can't go on living like this any longer!";
            if (Town.Rossie == 2)
            {
                l.Add(R(who, "…..", "….."));
                DialogueUi.Begin(l, null);
                return;
            }
            if (Town.Rossie == 0)
            {
                l.Add(R(who, "Cậu đã cứu em trai ta, đúng chứ?", "You saved my little brother, didn't you?"));
                l.Add(R(who, "Ta biết cậu.", "I know who you are."));
                l.Add(R(who, "Người của Pyber.", "One of Pyber's people."));
                l.Add(R(who, "Và ta cũng biết Pyber không muốn chiến tranh, chỉ là…", "And I also know Pyber doesn't want war, it's just that…"));
                l.Add(R(who, "Bọn ở trên chèn ép bọn ta quá! Ta cần sự giúp sức của Pyber… Ít nhất là cậu.", "Those above squeeze us too hard! I need Pyber's help… at least yours."));
                l.Add(R(who, "Nếu không…", "If not…"));
            }
            l.Add(Choice(who, last, lastEn));
            DialogueUi.Begin(l, null);
        }

        static DlgLine Choice(Transform who, string vi, string en)
        {
            string[] opts = { Cfg.L("Được, ta đồng ý !", "All right, I agree!"), Cfg.L("Không, ta không rảnh!", "No, I'm not free!") };
            return DlgLine.Ask(RossieName, Cfg.L(vi, en), RossieCol, who, opts, i =>
            {
                bool gift = !Town.RossieGift;
                if (gift)
                {
                    Town.RossieGift = true;
                    Inventory.AddCoins(100, false);
                    Sfx.Play("pickup", 0.9f, 1f);
                    var pl = Player.Instance;
                    if (pl != null) Fx.Text(pl.Center + new Vector2(0f, 1.6f), "+100", Cfg.Yellow, 1.2f);
                }
                if (i == 0)
                {
                    Town.Rossie = 2;
                    DialogueUi.Close(() => GameManager.Banner(Cfg.L("ROSSIE ĐÃ TRỞ THÀNH ĐỒNG MINH", "ROSSIE IS NOW YOUR ALLY") + (gift ? "  +100" : ""), Cfg.Orange, 3f));
                }
                else
                {
                    Town.Rossie = 1;
                    DialogueUi.Continue(new List<DlgLine> { R(who, "…Ta có thể gặp nhau tại dịp khác", "…We can meet another time") }, () => Interior.Exit());
                }
            });
        }
    }
}
