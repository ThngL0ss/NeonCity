using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// RoboStore: mặt tiền bán robot (thực chất là bắt cóc / buôn người — chỉ lộ ra dần). Cao 3 tầng.
    ///  Tầng 1: bên trái hành lang dài tối dần, cuối hành lang có cửa khoá; bên phải quầy lễ tân. Lễ tân hỏi loại robot (4 lựa chọn, lựa chọn "……" mở ô nhập mật khẩu).
    ///  Tầng 2: robot bình dân (chiến đấu / gia dụng / giáo dục, hạng C trở xuống). Tầng 3: robot thượng lưu (gia dụng hạng B, giáo dục hạng A).
    /// Giá ghi dưới bục trưng bày; bấm F vào bục để xem thông tin (chưa có chức năng mua robot).
    /// </summary>
    public static class RoboStoreBuilder
    {
        public const string Id = "RoboStore";
        static readonly Color Ink = new Color(1f, 0.35f, 0.85f);

        public static Room Floor1() { return Interior.GetRoom("rs1", BuildFloor1); }
        public static Room Floor2() { return Interior.GetRoom("rs2", BuildFloor2); }
        public static Room Floor3() { return Interior.GetRoom("rs3", BuildFloor3); }

        static string Rec { get { return Cfg.L("LỄ TÂN", "RECEPTIONIST"); } }
        static string Me { get { return "XbAst-013"; } }

        // ------------------------------------------------------------ tầng 1
        static Room BuildFloor1()
        {
            var r = new Room("rs1", "ROBOSTORE — TẦNG 1", "ROBOSTORE — FLOOR 1", Interior.Origin(1, 0), 84f, RoomStyle.Robo());
            r.entryX = 40.5f;
            RoomKit.ExitDoor(r, 38f, "RA PHỐ", "LEAVE", Interior.Exit, 0x1A1D30, 0xFF2FB5);

            // hành lang dài bên trái: tối dần về phía cuối, cửa khoá ở cuối
            for (int k = 0; k < 17; k++)
            {
                float a = 0.9f * (1f - k / 16f);
                r.Quad("Dark", k * 2f, -5f, 2.05f, 22f, new Color(0f, 0f, 0.02f, a), 25);
            }
            r.Put("LockedDoor", TownArt.DoorFrame(40, 56, 0x2A1018, 0xFF2A4A), 3f, 0f, -30);
            r.Spot(3f, 0f, "CỬA KHOÁ", "LOCKED DOOR", () => Fx.Text(Player.Instance.Position + new Vector2(0f, 2.4f), Cfg.L("Cửa khoá chặt.", "The door is locked."), Cfg.Red, 1.1f), 1.8f);
            RoomKit.Plate(r, 20f, 7.4f, "AUTHORIZED ONLY", 1, 0x1A0A12, 0xFF2A4A, 0xFF2A4A);

            // quầy lễ tân bên phải
            RoomKit.Plate(r, 54f, 7.6f, "RECEPTION", 2, 0x0A0C1C, 0xFF2FB5, 0xFF2FB5);
            RoomKit.Counter(r, 54f, 128, 0x8C93A8, 0x2B2F45);
            var rec = r.Npc(TownLooks.Receptionist, 54f, TownNpc.Mode.Stand, Rec, null);
            rec.SetTalk(() => DialogueUi.Begin(new List<DlgLine> { Menu(rec, false) }, null));
            Podium(r, 45f, 0, 0xFF2FB5, 0);
            Podium(r, 63f, 2, 0x35E8FF, 0);

            // cầu thang tượng trưng + biển số "1" ở cuối bên phải
            r.Put("StairsUp", TownArt.Stairs(true, 0x3D4363, 0xFF2FB5), 80f, 0f, -30);
            RoomKit.Plate(r, 80f, 6.9f, "1", 3, 0x0A0C1C, 0xFF2FB5, 0xFF2FB5);
            r.Spot(80f, 0f, "CẦU THANG", "STAIRS", () => Fx.Text(Player.Instance.Position + new Vector2(0f, 2.4f), Cfg.L("Hãy nói chuyện với lễ tân để lên tầng.", "Talk to the receptionist to go up."), Cfg.Yellow, 1.0f), 2.2f);
            RoomKit.Plant(r, 41.5f, 0xD83AA0);

            // 3 bảo vệ đi lại, 4 khách
            r.Npc(TownLooks.RoboGuard, 20f, TownNpc.Mode.Wander, null, null, 6f, 34f);
            r.Npc(TownLooks.RoboGuard, 46f, TownNpc.Mode.Wander, null, null, 40f, 60f);
            r.Npc(TownLooks.RoboGuard, 68f, TownNpc.Mode.Wander, null, null, 58f, 78f);
            r.Npc(TownLooks.Customer(2), 50.2f, TownNpc.Mode.Stand);
            r.Npc(TownLooks.Customer(4), 66f, TownNpc.Mode.Wander, null, null, 60f, 72f);
            r.Npc(TownLooks.Customer(5), 74f, TownNpc.Mode.Wander, null, null, 68f, 78f);
            r.Npc(TownLooks.Customer(1), 71.5f, TownNpc.Mode.Stand, null, null, -1f, -1f, true);
            return r;
        }

        // ------------------------------------------------------------ tầng 2
        static Room BuildFloor2()
        {
            var r = new Room("rs2", "ROBOSTORE — TẦNG 2", "ROBOSTORE — FLOOR 2", Interior.Origin(1, 1), 80f, RoomStyle.Robo());
            r.entryX = 11f;
            RoomKit.Plate(r, 6f, 7.6f, "RECEPTION", 2, 0x0A0C1C, 0xFF2FB5, 0xFF2FB5);
            RoomKit.Counter(r, 6f, 112, 0x8C93A8, 0x2B2F45);
            var rec = r.Npc(TownLooks.Receptionist, 6f, TownNpc.Mode.Stand, Rec, null);
            rec.SetTalk(() => DialogueUi.Begin(new List<DlgLine> { Back(rec, 1) }, null));

            Stall(r, 24f, 0, Rank.C, 3200, "ROBOT CHIẾN ĐẤU", "COMBAT ROBOT");
            Stall(r, 42f, 1, Rank.D, 1500, "ROBOT GIA DỤNG", "HOUSEHOLD ROBOT");
            Stall(r, 60f, 2, Rank.F, 600, "ROBOT GIÁO DỤC", "EDUCATION ROBOT");

            // cầu thang tượng trưng lên / xuống ở cuối bên phải
            r.Put("StairsDown", TownArt.Stairs(false, 0x3D4363, 0xFF2FB5), 70f, 0f, -30);
            r.Put("StairsUp", TownArt.Stairs(true, 0x3D4363, 0xFF2FB5), 77f, 0f, -30);
            RoomKit.Plate(r, 73.5f, 6.9f, "2", 3, 0x0A0C1C, 0xFF2FB5, 0xFF2FB5);

            r.Npc(TownLooks.RoboGuard, 18f, TownNpc.Mode.Wander, null, null, 12f, 40f);
            r.Npc(TownLooks.RoboGuard, 52f, TownNpc.Mode.Wander, null, null, 40f, 68f);
            r.Npc(TownLooks.Customer(0), 20f, TownNpc.Mode.Stand);
            r.Npc(TownLooks.Customer(3), 46f, TownNpc.Mode.Wander, null, null, 30f, 56f);
            r.Npc(TownLooks.Customer(6), 64f, TownNpc.Mode.Stand, null, null, -1f, -1f, true);
            RoomKit.Plant(r, 33f, 0xD83AA0);
            return r;
        }

        // ------------------------------------------------------------ tầng 3
        static Room BuildFloor3()
        {
            var r = new Room("rs3", "ROBOSTORE — TẦNG 3", "ROBOSTORE — FLOOR 3", Interior.Origin(1, 2), 72f, RoomStyle.Robo());
            r.entryX = 11f;
            RoomKit.Plate(r, 6f, 7.6f, "RECEPTION", 2, 0x0A0C1C, 0xFF2FB5, 0xFF2FB5);
            RoomKit.Counter(r, 6f, 112, 0x8C93A8, 0x2B2F45);
            var rec = r.Npc(TownLooks.Receptionist, 6f, TownNpc.Mode.Stand, Rec, null);
            rec.SetTalk(() => DialogueUi.Begin(new List<DlgLine> { Back(rec, 2) }, null));

            Stall(r, 30f, 1, Rank.B, 12000, "ROBOT GIA DỤNG", "HOUSEHOLD ROBOT");
            Stall(r, 50f, 2, Rank.A, 30000, "ROBOT GIÁO DỤC", "EDUCATION ROBOT");

            r.Put("StairsDown", TownArt.Stairs(false, 0x3D4363, 0xFF2FB5), 67f, 0f, -30);
            RoomKit.Plate(r, 67f, 6.9f, "3", 3, 0x0A0C1C, 0xFF2FB5, 0xFF2FB5);

            r.Npc(TownLooks.RoboGuard, 16f, TownNpc.Mode.Wander, null, null, 12f, 26f);
            r.Npc(TownLooks.RoboGuard, 38f, TownNpc.Mode.Wander, null, null, 26f, 44f);
            r.Npc(TownLooks.RoboGuard, 58f, TownNpc.Mode.Wander, null, null, 44f, 62f);
            r.Npc(TownLooks.RoboGuard, 41f, TownNpc.Mode.Stand, null, null, -1f, -1f, true);
            r.Npc(TownLooks.Customer(4), 26.4f, TownNpc.Mode.Stand);
            r.Npc(TownLooks.Customer(1), 46f, TownNpc.Mode.Stand, null, null, -1f, -1f, true);
            r.Npc(TownLooks.Customer(5), 56f, TownNpc.Mode.Wander, null, null, 52f, 64f);
            r.Npc(TownLooks.Customer(2), 36f, TownNpc.Mode.Wander, null, null, 32f, 42f);
            RoomKit.Plant(r, 20f, 0xD83AA0); RoomKit.Plant(r, 62f, 0xD83AA0);
            return r;
        }

        // ------------------------------------------------------------ đồ trưng bày
        /// <summary>Bục trưng bày + robot (chỉ để trang trí).</summary>
        static void Podium(Room r, float lx, int kind, int eye, int unused)
        {
            var ped = r.Put("Podium", TownArt.Pedestal(32, 0xFF2FB5), lx, 0f, -20);
            ped.transform.localScale = new Vector3(1f, 2f, 1f);
            var rb = r.Put("Robot", TownArt.Robot(kind, eye), lx, 1.25f, -19);
            rb.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
        }

        /// <summary>Gian hàng robot: bục + robot + giá ghi dưới bục + điểm bấm F xem thông tin.</summary>
        static void Stall(Room r, float lx, int kind, Rank grade, int price, string viName, string enName)
        {
            Color gc = PowerRules.RankColor(grade);
            int eye = kind == 0 ? 0xFF4D4D : kind == 1 ? 0x35E8FF : 0xFFD23F;
            Podium(r, lx, kind, eye, 0);
            r.Put("Price", TownArt.PriceTag(price, 0xFF2FB5), lx, 0.2f, -18);
            r.Quad("GradeBar", lx - 1f, 4.2f, 2f, 0.14f, gc, -27);
            RoomKit.Plate(r, lx, 5.0f, grade + "", 2, 0x0A0C1C, ToInt(gc), ToInt(gc));
            r.Spot(lx, 0f, "XEM " + viName, "VIEW " + enName, () =>
            {
                var l = new List<DlgLine>
                {
                    new DlgLine(Cfg.L("BẢNG GIÁ", "PRICE TAG"), Cfg.L(viName, enName) + Cfg.L(" — hạng ", " — rank ") + grade + Cfg.L(" — giá ", " — price ") + price + Cfg.L(" xu.", " coins."), gc, null)
                };
                DialogueUi.Begin(l, null);
            }, 1.8f);
        }

        static int ToInt(Color c) { return (Mathf.RoundToInt(c.r * 255f) << 16) | (Mathf.RoundToInt(c.g * 255f) << 8) | Mathf.RoundToInt(c.b * 255f); }

        // ------------------------------------------------------------ lời thoại lễ tân
        /// <summary>Câu hỏi 4 lựa chọn ở tầng 1. again = lần hỏi lại sau khi nhập sai mật khẩu.</summary>
        static DlgLine Menu(TownNpc rec, bool again)
        {
            string text = again ? Cfg.L("Quý khách đang nói đến loại robot nào ạ?", "Which kind of robot are you referring to, sir?")
                                : Cfg.L("Chào mừng quý khách đến với RoboStore! Quý khách muốn xem loại robot nào ạ?", "Welcome to RoboStore! Which kind of robot would you like to see?");
            string[] opts =
            {
                Cfg.L("Robot bình dân", "Standard robots"),
                Cfg.L("Robot thượng lưu", "Premium robots"),
                Cfg.L("Hiện tại chưa muốn mua", "Not buying right now"),
                "……"
            };
            return DlgLine.Ask(Rec, text, Ink, rec.transform, opts, i =>
            {
                switch (i)
                {
                    case 0:
                        DialogueUi.Continue(new List<DlgLine> { RoomKit.Say(Rec, rec.transform, Ink, "Mời quý khách lên tầng 2.", "Please head to the 2nd floor.") }, () => Interior.Go(Floor2, 11f));
                        break;
                    case 1:
                        DialogueUi.Continue(new List<DlgLine> { RoomKit.Say(Rec, rec.transform, Ink, "Mời quý khách lên tầng 3.", "Please head to the 3rd floor.") }, () => Interior.Go(Floor3, 11f));
                        break;
                    case 2:
                        DialogueUi.Close();
                        break;
                    default:
                        Password(rec);
                        break;
                }
            });
        }

        static void Password(TownNpc rec)
        {
            var pl = Player.Instance != null ? Player.Instance.transform : null;
            var ask = DlgLine.Prompt(Me, "……", Cfg.Cyan, pl, text =>
            {
                if (text.Trim() == "!9 /3")
                    DialogueUi.Continue(new List<DlgLine> { RoomKit.Say(Rec, rec.transform, Ink, "….. Mời ngài đi theo tôi.", "….. Please follow me, sir.") });
                else
                    DialogueUi.Continue(new List<DlgLine> { Menu(rec, true) });
            });
            DialogueUi.Continue(new List<DlgLine> { ask });
        }

        /// <summary>Lễ tân tầng 2 / 3: chọn "Tôi muốn đi về" để xuống tầng dưới.</summary>
        static DlgLine Back(TownNpc rec, int floorIndex)
        {
            string[] opts = { Cfg.L("Tôi muốn đi về", "I'd like to leave"), Cfg.L("Không có gì", "Nothing") };
            return DlgLine.Ask(Rec, Cfg.L("Quý khách cần gì ạ?", "How may I help you?"), Ink, rec.transform, opts, i =>
            {
                if (i != 0) { DialogueUi.Close(); return; }
                string vi = floorIndex == 1 ? "Mời quý khách xuống tầng 1." : "Mời quý khách xuống tầng 2.";
                string en = floorIndex == 1 ? "Please head down to the 1st floor." : "Please head down to the 2nd floor.";
                DialogueUi.Continue(new List<DlgLine> { RoomKit.Say(Rec, rec.transform, Ink, vi, en) },
                    () => { if (floorIndex == 1) Interior.Go(Floor1, 40.5f); else Interior.Go(Floor2, 11f); });
            });
        }
    }
}
