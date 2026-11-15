using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// MerShop (Rìa Phố Neon, phe Đánh thuê): cửa hàng 1 tầng tồi tàn. Trong nhà: nửa trái là vùng tối (không có gì), nửa phải là lễ tân
    /// (áo nâu, khăn xám che nửa mặt dưới) sau quầy gỗ cũ. Lễ tân hỏi: Mua / Bán (bảng ShopUi.Kind.Mer) / Đưa tiền / Không có gì.
    /// "Đưa tiền": lần 1 = 50 xu, lần 2 = 100, lần 3 = 150 (Town.MerPaid); mỗi lần lễ tân chỉ đáp "….."; lần thứ 3 nói "Xin mời ngài đi theo tôi"
    /// rồi dẫn người chơi vào vùng tối → phòng kín của Furra. Từ đó vùng tối có lối vào luôn.
    /// Furra (da đen, áo khoác đen dài, bịt mắt): "Thông tin" (chưa có gì) hoặc "Sức mạnh" = thuê lính đánh thuê (MercUi, xem MercDefs).
    /// </summary>
    public static class MerShopBuilder
    {
        public const string Id = "MerShop";
        const float W = 44f;
        static readonly Color Orange = new Color(0.95f, 0.62f, 0.25f);
        static readonly Color FurraCol = new Color(0.80f, 0.28f, 0.38f);

        static TownNpc clerk, furra;
        static float clerkHomeX;
        static SpriteRenderer darkDoor;
        static bool leading;

        public static void ResetStatics() { clerk = null; furra = null; darkDoor = null; leading = false; }

        public static Room Shop() { return Interior.GetRoom("mershop", BuildShop); }
        public static Room Back() { return Interior.GetRoom("mershop.back", BuildBack); }

        static string ClerkName { get { return Cfg.L("LỄ TÂN", "RECEPTIONIST"); } }
        static string FurraName { get { return "FURRA"; } }

        // ================================================================ phòng ngoài
        static Room BuildShop()
        {
            var r = new Room("mershop", "MERSHOP", "MERSHOP", Interior.Origin(4, 0), W, RoomStyle.Shabby());
            r.entryX = 22f;
            RoomKit.ExitDoor(r, 22f, "RA NGOÀI", "LEAVE", Interior.Exit, 0x3A2E22, 0xE8B060);

            // vùng tối bên trái: phủ đen, mờ dần ra mép
            r.Quad("Dark", -2f, -5f, 17f, 22f, new Color(0f, 0f, 0.015f, 0.97f), -20);
            float[] fade = { 0.75f, 0.5f, 0.28f, 0.1f };
            for (int i = 0; i < fade.Length; i++) r.Quad("DarkEdge", 15f + i, -5f, 1f, 22f, new Color(0f, 0f, 0.015f, fade[i]), -20);
            darkDoor = r.Put("DarkDoor", TownArt.DoorFrame(34, 54, 0x1A1410, 0xB02030), 6f, 0f, -19);
            darkDoor.enabled = Town.MerPaid >= 3;
            var h = r.Spot(6f, 0f, "VÀO PHÒNG TỐI", "ENTER THE DARK ROOM", GoBack, 1.8f);
            h.canUse = () => Town.MerPaid >= 3;

            // bên phải: quầy + lễ tân + đồ bày bừa
            r.Put("Junk", RimArt.JunkPile(88, 34, 4), 40f, 0f, -34);
            RoomKit.Shelf(r, 27.5f, 96, 80, 5, 0x4A3A2C);
            r.Put("Crate", TownArt.Crate(0x6A5030), 25.4f, 0f, -24);
            r.Put("Barrel", RimArt.Barrel(0x7A4A2A), 41.6f, 0f, -24);
            r.Put("Counter", RimArt.ShabbyCounter(88), 31f, 0f, -23);
            RoomKit.Plate(r, 33f, 8.0f, "MERSHOP", 2, 0x1A100A, 0xE8923A, 0x8A5A2A);
            r.Glow(33f, 8.3f, 9f, 4f, Cfg.A(new Color(1f, 0.6f, 0.2f), 0.16f), -44);

            clerkHomeX = r.X(34.6f);
            clerk = r.Npc(RimLooks.MerClerk, 34.6f, TownNpc.Mode.Stand, ClerkName, Talk, -1f, -1f, true);
            clerk.range = 3.2f;
            return r;
        }

        // ================================================================ phòng kín của Furra
        static Room BuildBack()
        {
            var r = new Room("mershop.back", "PHÒNG KÍN", "BACK ROOM", Interior.Origin(4, 1), 30f, RoomStyle.Vault());
            r.entryX = 4f;
            RoomKit.ExitDoor(r, 2.6f, "RA NGOÀI", "LEAVE", LeaveBack, 0x1A1410, 0xB02030);
            // bàn + nến đỏ + giá vũ khí
            r.Put("Table", RimArt.ShabbyCounter(80), 18f, 0f, -26);
            r.Put("Lantern", TownArt.Lantern(0x9A1A1A), 16.4f, 1.6f, -25);
            r.Put("Lantern", TownArt.Lantern(0x9A1A1A), 19.6f, 1.6f, -25);
            r.Glow(18f, 2.4f, 7f, 4f, new Color(0.9f, 0.15f, 0.2f, 0.22f), -24);
            RoomKit.Shelf(r, 8f, 64, 72, 9, 0x2A2A34);
            RoomKit.Shelf(r, 26f, 64, 72, 13, 0x2A2A34);
            r.Put("Barrel", RimArt.Barrel(0x3A3A44), 11.4f, 0f, -24);

            furra = r.Npc(RimLooks.Furra, 21.6f, TownNpc.Mode.Stand, FurraName, TalkFurra, -1f, -1f, true);
            furra.range = 3.4f;
            return r;
        }

        static void GoBack() { Interior.Go(Back, 4f); }

        static void LeaveBack()
        {
            Interior.Go(Shop, 8f);
            ResetClerk();
        }

        static void ResetClerk()
        {
            if (clerk != null) clerk.transform.position = new Vector3(clerkHomeX, clerk.transform.position.y, clerk.transform.position.z);
        }

        // ================================================================ hội thoại
        static DlgLine C(Transform who, string vi, string en) { return new DlgLine(ClerkName, Cfg.L(vi, en), Orange, who); }
        static DlgLine P(string vi, string en)
        {
            var pl = Player.Instance != null ? Player.Instance.transform : null;
            return new DlgLine("XbAst-013", Cfg.L(vi, en), Cfg.Cyan, pl);
        }

        static void Talk()
        {
            Transform who = clerk != null ? clerk.transform : null;
            var l = new List<DlgLine>();
            if (!Town.MerMet)
            {
                var hi = C(who, "Chào kẻ bên trên", "Hello, one from above");
                hi.onShow = () => Town.MerMet = true;
                l.Add(hi);
                l.Add(P("…", "…"));
                l.Add(C(who, "Haha! Cậu khó chịu hả? Xin lỗi, tôi đùa thôi", "Haha! Did that annoy you? Sorry, I was only joking"));
            }
            l.Add(Menu(who));
            DialogueUi.Begin(l, null);
        }

        static DlgLine Menu(Transform who)
        {
            string pay = Town.MerPaid >= 3
                ? Cfg.L("Đưa tiền", "Give money")
                : Cfg.L("Đưa tiền (" + 50 * (Town.MerPaid + 1) + " xu)", "Give money (" + 50 * (Town.MerPaid + 1) + " coins)");
            string[] opts = { Cfg.L("Mua", "Buy"), Cfg.L("Bán", "Sell"), pay, Cfg.L("Không có gì", "Nothing") };
            return DlgLine.Ask(ClerkName, Cfg.L("Cậu muốn gì nào?", "What do you want?"), Orange, who, opts, i =>
            {
                switch (i)
                {
                    case 0: DialogueUi.Close(() => ShopUi.Open(ShopUi.Kind.Mer, 0)); break;
                    case 1: DialogueUi.Close(() => ShopUi.Open(ShopUi.Kind.Mer, 1)); break;
                    case 2: Pay(who); break;
                    default: DialogueUi.Close(); break;
                }
            });
        }

        static void Pay(Transform who)
        {
            if (Town.MerPaid >= 3) { Lead(who, true); return; }
            int amt = 50 * (Town.MerPaid + 1);
            if (!Inventory.Spend(amt))
            {
                DialogueUi.Close(() => GameManager.Banner(Cfg.L("KHÔNG ĐỦ XU", "NOT ENOUGH COINS"), Cfg.Red, 2f));
                return;
            }
            Sfx.Play("pickup", 0.9f, 0.9f);
            Town.MerPaid++;
            bool open = Town.MerPaid >= 3;
            var l = new List<DlgLine>();
            l.Add(C(who, "…..", "….."));
            if (open) l.Add(C(who, "Xin mời ngài đi theo tôi", "Please, follow me, sir"));
            DialogueUi.Continue(l, () => { if (open) Lead(who, false); });
        }

        /// <summary>Lễ tân đi vào vùng tối, rồi chuyển người chơi sang phòng kín của Furra.</summary>
        static void Lead(Transform who, bool speak)
        {
            if (leading || GameManager.Inst == null) return;
            if (speak)
            {
                var l = new List<DlgLine> { C(who, "Xin mời ngài đi theo tôi", "Please, follow me, sir") };
                DialogueUi.Continue(l, () => StartLead(who));
            }
            else StartLead(who);
        }

        static void StartLead(Transform who)
        {
            if (leading || GameManager.Inst == null) return;
            leading = true;
            GameManager.Inst.StartCoroutine(LeadCo(who));
        }

        static IEnumerator LeadCo(Transform who)
        {
            yield return null;
            GameManager.PlayerLocked = true;
            if (darkDoor != null) darkDoor.enabled = true;
            if (who != null)
            {
                var room = Shop();
                Vector3 a = who.position;
                Vector3 b = new Vector3(room.X(9f), a.y, a.z);
                float t = 0f;
                while (t < 1.6f)
                {
                    t += Time.deltaTime;
                    who.position = Vector3.Lerp(a, b, Mathf.Clamp01(t / 1.6f));
                    yield return null;
                }
            }
            leading = false;
            Interior.Go(Back, 4f);
            yield return null;
            while (Interior.Busy) yield return null;
            ResetClerk();
        }

        static void TalkFurra()
        {
            Transform who = furra != null ? furra.transform : null;
            string[] opts = { Cfg.L("Thông tin", "Information"), Cfg.L("Sức mạnh", "Power") };
            var ask = DlgLine.Ask(FurraName, Cfg.L("Cậu muốn gì?", "What do you want?"), FurraCol, who, opts, i =>
            {
                if (i == 0)
                    DialogueUi.Continue(new List<DlgLine> { new DlgLine(FurraName, Cfg.L("Hiện tại… chưa có gì.", "For now… there is nothing."), FurraCol, who) }, null);
                else
                    DialogueUi.Close(() => BaseUi.Open(BaseMode.Merc));
            });
            DialogueUi.Begin(new List<DlgLine> { ask }, null);
        }
    }
}
