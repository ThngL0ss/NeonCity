using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// CenterShop: cửa hàng được phe Tài phiệt bảo kê (mặt tiền 2 tầng). Tầng 1: bên trái quầy vật phẩm + trang bị, bên phải quầy tiêu hao + linh kiện,
    /// cuối phải có cầu thang lên. Tầng 2: quầy mật khẩu (nhân viên đưa chuỗi mã, người chơi gõ câu đáp).
    /// NPC: 3 nhân viên bán hàng (đứng yên ở quầy), 5 bảo vệ đi tuần tầng 1 – 2 + 1 bảo vệ đứng gác cửa, 7 khách (đứng / đi lại cả hai tầng).
    /// </summary>
    public static class CenterShopBuilder
    {
        public const string Id = "CenterShop";
        const float W = 64f;
        static readonly Color Ink = Cfg.Cyan;

        public static Room Floor1() { return Interior.GetRoom("cs1", BuildFloor1); }
        public static Room Floor2() { return Interior.GetRoom("cs2", BuildFloor2); }

        static string Staff { get { return Cfg.L("NHÂN VIÊN", "STAFF"); } }

        static Room BuildFloor1()
        {
            var r = new Room("cs1", "CENTERSHOP — TẦNG 1", "CENTERSHOP — FLOOR 1", Interior.Origin(0, 0), W, RoomStyle.Corporate());
            r.entryX = 4.8f;
            RoomKit.ExitDoor(r, 2.2f, "RA PHỐ", "LEAVE", Interior.Exit);

            // quầy trái: vật phẩm + trang bị
            RoomKit.Shelf(r, 9f, 96, 96, 1);
            RoomKit.Shelf(r, 21f, 96, 96, 2);
            RoomKit.Plate(r, 15f, 7.6f, "ITEMS+GEAR", 2, 0x0B1630, 0x35E8FF, 0x35E8FF);
            RoomKit.Counter(r, 15f, 112);
            r.Npc(TownLooks.Seller, 15f, TownNpc.Mode.Stand, Staff, null);
            // quầy phải: tiêu hao + linh kiện
            RoomKit.Shelf(r, 36f, 96, 96, 3);
            RoomKit.Shelf(r, 48f, 96, 96, 4);
            RoomKit.Plate(r, 42f, 7.6f, "SUPPLY+PARTS", 2, 0x0B1630, 0x7BFF6A, 0x7BFF6A);
            RoomKit.Counter(r, 42f, 112);
            r.Npc(TownLooks.Seller, 42f, TownNpc.Mode.Stand, Staff, null);
            // gán lời thoại sau khi có NPC (cần transform)
            r.npcs[0].SetTalk(() => TalkSeller(r.npcs[0], ShopUi.Kind.Gear));
            r.npcs[1].SetTalk(() => TalkSeller(r.npcs[1], ShopUi.Kind.Supply));

            // cầu thang lên (cuối bên phải)
            RoomKit.Plate(r, 60f, 6.4f, "FLOOR 2", 2, 0x0B1630, 0xFFD23F, 0xFFD23F);
            RoomKit.StairsSpot(r, 60f, true, "LÊN TẦNG 2", "GO UP TO FLOOR 2", () => Interior.Go(Floor2, 56.5f));

            RoomKit.Plant(r, 28.5f); RoomKit.Plant(r, 55f); RoomKit.Plant(r, 7.5f, 0x35B0C8);
            RoomKit.Bench(r, 31.5f);

            // bảo vệ: 1 đứng gác cửa + 2 đi tuần
            r.Npc(TownLooks.Guard, 6.8f, TownNpc.Mode.Stand, null, null, -1f, -1f, true);
            r.Npc(TownLooks.Guard, 24f, TownNpc.Mode.Wander, null, null, 20f, 35f);
            r.Npc(TownLooks.Guard, 50f, TownNpc.Mode.Wander, null, null, 46f, 56f);
            // khách: 2 đứng trước quầy, 2 đi lại
            r.Npc(TownLooks.Customer(0), 10.6f, TownNpc.Mode.Stand);
            r.Npc(TownLooks.Customer(1), 37.6f, TownNpc.Mode.Stand);
            r.Npc(TownLooks.Customer(2), 23f, TownNpc.Mode.Wander, null, null, 20f, 28.5f);
            r.Npc(TownLooks.Customer(3), 51f, TownNpc.Mode.Wander, null, null, 47f, 56f);
            return r;
        }

        static Room BuildFloor2()
        {
            var r = new Room("cs2", "CENTERSHOP — TẦNG 2", "CENTERSHOP — FLOOR 2", Interior.Origin(0, 1), W, RoomStyle.Corporate());
            r.entryX = 56.5f;
            RoomKit.WindowBand(r, 4f, 58f, 4.2f, 5f, 4.2f, new Color(0.15f, 0.25f, 0.45f));
            RoomKit.StairsSpot(r, 60f, false, "XUỐNG TẦNG 1", "GO DOWN TO FLOOR 1", () => Interior.Go(Floor1, 56.5f));
            RoomKit.Plate(r, 60f, 6.4f, "FLOOR 1", 2, 0x0B1630, 0xFFD23F, 0xFFD23F);

            RoomKit.Shelf(r, 20f, 96, 80, 5);
            RoomKit.Plate(r, 30f, 7.6f, "COUNTER", 2, 0x0B1630, 0xFF9A1A, 0xFF9A1A);
            RoomKit.Counter(r, 30f, 112, 0xD8C8A8, 0x4A3A5A);
            var seller = r.Npc(TownLooks.Seller, 30f, TownNpc.Mode.Stand, Staff, null);
            seller.SetTalk(() => TalkCipher(seller));
            RoomKit.Shelf(r, 40f, 96, 80, 6);

            RoomKit.Plant(r, 8f); RoomKit.Plant(r, 52f, 0x35B0C8);
            RoomKit.Bench(r, 14f, 64);
            RoomKit.Bench(r, 46f, 48);

            r.Npc(TownLooks.Guard, 12f, TownNpc.Mode.Wander, null, null, 6f, 22f);
            r.Npc(TownLooks.Guard, 34f, TownNpc.Mode.Wander, null, null, 24f, 44f);
            r.Npc(TownLooks.Guard, 50f, TownNpc.Mode.Wander, null, null, 44f, 56f);
            r.Npc(TownLooks.Customer(4), 26.2f, TownNpc.Mode.Stand);
            r.Npc(TownLooks.Customer(5), 16f, TownNpc.Mode.Wander, null, null, 8f, 24f);
            r.Npc(TownLooks.Customer(6), 40f, TownNpc.Mode.Wander, null, null, 36f, 52f);
            return r;
        }

        // ------------------------------------------------------------ lời thoại
        static void TalkSeller(TownNpc npc, ShopUi.Kind kind)
        {
            var l = new List<DlgLine>
            {
                RoomKit.Say(Staff, npc.transform, Ink, "Chào mừng quý khách đến với CenterShop! Quý khách cần gì ạ?", "Welcome to CenterShop! What can I get for you?")
            };
            DialogueUi.Begin(l, () => ShopUi.Open(kind));
        }

        /// <summary>Quầy tầng 2: nhân viên đưa chuỗi mã, người chơi phải gõ đúng câu đáp.</summary>
        static void TalkCipher(TownNpc npc)
        {
            var ask = DlgLine.Prompt(Staff, "?@5 )+7 /@ -8 m", Ink, npc.transform, text =>
            {
                bool ok = text.Trim() == "5+9!- 58!";
                string vi = ok ? "Hiện tại chưa có tin gì" : "Quý khách vui lòng xuống tầng 1 để giao dịch!";
                string en = ok ? "There's no news at the moment" : "Dear customer, please go down to the 1st floor to make your purchase!";
                DialogueUi.Continue(new List<DlgLine> { RoomKit.Say(Staff, npc.transform, Ink, vi, en) });
            });
            DialogueUi.Begin(new List<DlgLine> { ask }, null);
        }
    }
}
