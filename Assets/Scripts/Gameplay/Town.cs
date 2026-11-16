using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Trạng thái cốt truyện / nhiệm vụ phụ ở Phố Neon (lưu trong file lưu game cùng cờ nhiệm vụ):
    ///  • Shengu (tu viện ZenMonastery): lõi giả → nhiệm vụ đưa hộp đen tới DesTown4 → lõi thật;
    ///  • ArenaCyber: đã thắng 3 trận lấy thông tin chưa (có thẻ đấu sĩ), số trận đã thắng ở chế độ đấu sĩ.
    /// </summary>
    public static class Town
    {
        // Shengu: 0 chưa gặp · 1 đã nhận lõi (giả), từ chối việc nhờ · 2 đã nhận hộp đen, đang đi giao · 3 đã giao hộp, quay lại gặp Shengu · 4 xong (đã nhận lõi thật)
        public const int ShenguNone = 0, ShenguDeclined = 1, ShenguQuest = 2, ShenguDelivered = 3, ShenguDone = 4;
        public static int Shengu;
        public static bool ArenaInfoDone;       // thắng liền 3 trận → đã nhận thẻ đấu sĩ
        public static int FighterWins;          // số trận đã thắng ở chế độ "đấu sĩ"

        // ---- Rìa Phố Neon
        public static bool MerMet;              // đã nghe lễ tân MerShop chào lần đầu
        public static int MerPaid;              // số lần đã đưa tiền cho lễ tân (3 = mở phòng Furra)
        public static int MercOwned;            // bit i = đã thuê lính đánh thuê thứ i (xem MercDefs)
        public static int MercActive = -1;      // lính đang đi cùng (-1 = không ai)
        public static bool RyberOpen;           // cổng trại Ryber đã mở (sau sự kiện cậu bé)
        public static int Rossie;               // 0 chưa gặp · 1 đã nói chuyện, từ chối · 2 đồng minh
        public static bool RossieGift;          // đã nhận 100 xu khi nói chuyện lần đầu với Rossie
        public static int Beggar;               // 0 chưa gặp · 1 đã đưa 30 xu · 2 không đưa
        public static bool RobbersDone;         // đã hạ hết đám cướp ở bãi rác
        public static bool KnifeTaken;          // đã nhặt dao đỏ (rơi ra sau khi hạ hết đám cướp)
        // Albert: 0 chưa mở · 1 mở khoá (đã nhận Core-T06 thật, chờ về căn cứ nói chuyện) · 2 đã nói chuyện (Core-T06 do Albert cất giữ)
        public const int AlbertLocked = 0, AlbertPending = 1, AlbertDone = 2;
        public static int AlbertStory;

        // ---- Thám hiểm: bit i = đã tới / đã vào khu vực ExploreIds[i]. Thứ tự này được LƯU trong file lưu game — chỉ thêm vào cuối, đừng đổi chỗ.
        //  • Toà vào được (CenterShop, RoboStore, ZenMonastery, ArenaCyber, MerShop, Lều trại) tính khi bước vào lần đầu;
        //  • công trình không vào được (DesTown4, Phòng thí nghiệm, Bãi rác, Khu mỏ, ReWolrld) tính khi người chơi đi tới trước mặt (xem SiteExplorer).
        public static readonly string[] ExploreIds =
        {
            CenterShopBuilder.Id, RoboStoreBuilder.Id, ZenBuilder.Id, DesTownBuilder.Id, ArenaBuilder.Id,
            MerShopBuilder.Id, StreetBuildings.LabId, RyberBuilder.Id, StreetBuildings.JunkId, StreetBuildings.MineId, StreetBuildings.SlaveId,
        };
        public static int Explored;
        // Albert kể về các phe: 0 chưa mở · 1 mở khoá (đã thám hiểm hết, chờ về căn cứ) · 2 đã kể
        public const int FactionLocked = 0, FactionPending = 1, FactionDone = 2;
        public static int FactionStory;

        public static int ExploreIndex(string id) { return System.Array.IndexOf(ExploreIds, id); }
        public static bool IsExplored(string id) { int i = ExploreIndex(id); return i >= 0 && (Explored & (1 << i)) != 0; }
        public static bool AllExplored { get { int all = (1 << ExploreIds.Length) - 1; return (Explored & all) == all; } }

        /// <summary>Đánh dấu đã thám hiểm một khu vực. Khi đủ tất cả: mở khoá phần Albert kể về các phe (thông báo hiện sau khi hiệu ứng vào nhà kết thúc).</summary>
        public static void Explore(string id)
        {
            int i = ExploreIndex(id);
            if (i < 0 || (Explored & (1 << i)) != 0) return;
            Explored |= 1 << i;
            if (FactionStory == FactionLocked && AllExplored)
            {
                FactionStory = FactionPending;
                if (GameManager.Inst != null) GameManager.Inst.StartCoroutine(UnlockBanner());
            }
        }

        static System.Collections.IEnumerator UnlockBanner()
        {
            yield return new WaitForSeconds(2.7f);              // đợi tên toà nhà (Interior) hiện xong
            while (Interior.Busy) yield return null;
            GameManager.Banner(Cfg.L("MỞ KHÓA CÂU CHUYỆN MỚI", "NEW STORY UNLOCKED"), Cfg.Yellow, 4f);
            Sfx.Play("victory", 0.6f, 1.1f);
        }

        public static void ResetAll()
        {
            Shengu = ShenguNone;
            ArenaInfoDone = false;
            FighterWins = 0;
            MerMet = false; MerPaid = 0; MercOwned = 0; MercActive = -1;
            RyberOpen = false; Rossie = 0; RossieGift = false; Beggar = 0; RobbersDone = false; KnifeTaken = false;
            AlbertStory = AlbertLocked;
            Explored = 0; FactionStory = FactionLocked;
            QuestItems.Reset();
            Interior.ResetStatics();
        }

        public static void Export(SaveData d)
        {
            SaveManager.SetFlag(d.flags, "t.shengu", Shengu);
            SaveManager.SetFlag(d.flags, "t.arenaInfo", ArenaInfoDone ? 1 : 0);
            SaveManager.SetFlag(d.flags, "t.fWins", FighterWins);
            SaveManager.SetFlag(d.flags, "t.merMet", MerMet ? 1 : 0);
            SaveManager.SetFlag(d.flags, "t.merPaid", MerPaid);
            SaveManager.SetFlag(d.flags, "t.mercOwned", MercOwned);
            SaveManager.SetFlag(d.flags, "t.mercActive", MercActive + 1);
            SaveManager.SetFlag(d.flags, "t.ryberOpen", RyberOpen ? 1 : 0);
            SaveManager.SetFlag(d.flags, "t.rossie", Rossie);
            SaveManager.SetFlag(d.flags, "t.rossieGift", RossieGift ? 1 : 0);
            SaveManager.SetFlag(d.flags, "t.beggar", Beggar);
            SaveManager.SetFlag(d.flags, "t.robbers", RobbersDone ? 1 : 0);
            SaveManager.SetFlag(d.flags, "t.knife", KnifeTaken ? 1 : 0);
            SaveManager.SetFlag(d.flags, "t.albert", AlbertStory);
            SaveManager.SetFlag(d.flags, "t.explored", Explored);
            SaveManager.SetFlag(d.flags, "t.faction", FactionStory);
        }

        public static void Import(SaveData d)
        {
            Shengu = SaveManager.GetFlag(d.flags, "t.shengu");
            ArenaInfoDone = SaveManager.GetFlag(d.flags, "t.arenaInfo") != 0;
            FighterWins = SaveManager.GetFlag(d.flags, "t.fWins");
            MerMet = SaveManager.GetFlag(d.flags, "t.merMet") != 0;
            MerPaid = SaveManager.GetFlag(d.flags, "t.merPaid");
            MercOwned = SaveManager.GetFlag(d.flags, "t.mercOwned");
            MercActive = SaveManager.GetFlag(d.flags, "t.mercActive") - 1;
            RyberOpen = SaveManager.GetFlag(d.flags, "t.ryberOpen") != 0;
            Rossie = SaveManager.GetFlag(d.flags, "t.rossie");
            RossieGift = SaveManager.GetFlag(d.flags, "t.rossieGift") != 0;
            Beggar = SaveManager.GetFlag(d.flags, "t.beggar");
            RobbersDone = SaveManager.GetFlag(d.flags, "t.robbers") != 0;
            KnifeTaken = SaveManager.GetFlag(d.flags, "t.knife") != 0;
            AlbertStory = SaveManager.GetFlag(d.flags, "t.albert");
            Explored = SaveManager.GetFlag(d.flags, "t.explored");
            FactionStory = SaveManager.GetFlag(d.flags, "t.faction");
        }

        /// <summary>Dòng nhiệm vụ phụ đang làm (null = không có).</summary>
        public static string SideQuestText
        {
            get
            {
                if (Shengu == ShenguQuest) return Cfg.L("Đưa hộp đen đến DesTown4", "Deliver the black box to DesTown4");
                if (Shengu == ShenguDelivered) return Cfg.L("Quay lại gặp Shengu ở ZenMonastery", "Return to Shengu at ZenMonastery");
                return null;
            }
        }

        public static void DrawSideQuest(Hud h)
        {
            string t = SideQuestText;
            if (t == null) return;
            float y = GameFlow.InBase ? 12f : 82f;
            var m = BaseMissions.Current;
            if (m != null && BaseMissions.Accepted)
            {
                int n = m.reqs != null && m.reqs.Count > 0 ? m.reqs.Count : 1;
                y += 30f + n * 22f + 8f;
            }
            Rect r = h.R(1000, y, 266, 52);
            h.Fill(r, new Color(0.01f, 0.02f, 0.06f, 0.6f));
            h.Outline(r, Cfg.A(Cfg.Magenta, 0.6f), 1.5f);
            h.Txt(h.R(1010, y + 3, 246, 20), Cfg.L("NHIỆM VỤ PHỤ", "SIDE QUEST"), 12, Cfg.Magenta, TextAnchor.MiddleLeft);
            h.TxtWrap(h.R(1010, y + 22, 248, 30), t, 13, Color.white);
        }
    }

    /// <summary>
    /// Vật phẩm nhiệm vụ do code tạo (không có file asset): hai lõi Core-T06 (giả / thật), hộp đen, thẻ đấu sĩ.
    /// Lưu game tra lại theo tên ("Quest_...") qua ItemDatabase.Find. Không bán, không vứt, không mất khi chết (ItemData.questItem).
    /// </summary>
    public static class QuestItems
    {
        public const string Core6Fake = "Quest_core_t06_fake", Core6 = "Quest_core_t06", BlackBox = "Quest_black_box", FighterCard = "Quest_fighter_card", RedKnife = "Quest_red_knife";
        static readonly Dictionary<string, ItemData> map = new Dictionary<string, ItemData>();

        public static void Reset()
        {
            foreach (var kv in map) if (kv.Value != null) Object.Destroy(kv.Value);
            map.Clear();
        }

        public static ItemData Find(string id)
        {
            if (string.IsNullOrEmpty(id) || !id.StartsWith("Quest_")) return null;
            ItemData it;
            if (map.TryGetValue(id, out it) && it != null) return it;
            it = Make(id);
            if (it != null) map[id] = it;
            return it;
        }

        static ItemData Make(string id)
        {
            var it = ScriptableObject.CreateInstance<ItemData>();
            it.name = id;
            it.hideFlags = HideFlags.HideAndDontSave;
            it.kind = ItemKind.Valuable;
            it.questItem = true;
            it.maxStack = 1;
            it.sellValue = 0;
            switch (id)
            {
                case Core6Fake:
                case Core6:
                    it.nameVi = "Core-T06"; it.nameEn = "Core-T06";
                    it.descVi = "Lõi bậc SS, ký hiệu Core-T06. Loại lõi đủ sức lưu trữ ký ức vĩnh viễn.";
                    it.descEn = "An SS-rank core, designation Core-T06. The kind of core that can hold memories permanently.";
                    it.color = PowerRules.RankColor(Rank.SS);
                    it.icon = IconCore();
                    break;
                case BlackBox:
                    it.nameVi = "Hộp đen bí ẩn"; it.nameEn = "Mysterious black box";
                    it.descVi = "Chiếc hộp đen Shengu nhờ đưa tới DesTown4, kèm lời nhắn: Shengu có một yêu cầu.";
                    it.descEn = "The black box Shengu asked you to carry to DesTown4, with a message: Shengu has a request.";
                    it.color = new Color(0.55f, 0.55f, 0.65f, 1f);
                    it.icon = IconBox();
                    break;
                case FighterCard:
                    it.nameVi = "Thẻ đấu sĩ"; it.nameEn = "Fighter's card";
                    it.descVi = "Thẻ thành viên dành cho đấu sĩ của ArenaCyber. Một tấm thẻ đen.";
                    it.descEn = "A fighter's membership card from ArenaCyber. A plain black card.";
                    it.color = new Color(1f, 0.25f, 0.35f, 1f);
                    it.icon = IconCard();
                    break;
                case RedKnife:
                    it.nameVi = "Dao đỏ"; it.nameEn = "Red knife";
                    it.descVi = "Con dao đỏ nhặt được từ đám cướp ở Bãi rác công nghệ. Không phải trang bị.";
                    it.descEn = "A red knife taken from the robbers at the tech junkyard. Not equipment.";
                    it.color = new Color(1f, 0.2f, 0.2f, 1f);
                    it.icon = IconKnife();
                    break;
                default:
                    Object.Destroy(it);
                    return null;
            }
            return it;
        }

        /// <summary>Thêm vật phẩm nhiệm vụ vào kho (luôn thêm được, kể cả khi kho đầy).</summary>
        public static void Give(string id)
        {
            var it = Find(id);
            if (it == null) return;
            Inventory.AddTo(Inventory.Slots, it, 1, int.MaxValue);
            Inventory.RefreshStats();
            Sfx.Play("victory", 0.55f, 1.3f);
            Fx.Text(Player.Instance != null ? Player.Instance.Position + new Vector2(0f, 2.6f) : Vector2.zero, "+ " + it.DisplayName, it.color, 1.1f);
        }

        /// <summary>Lấy 1 vật phẩm nhiệm vụ khỏi kho. false nếu không có.</summary>
        public static bool Take(string id)
        {
            var it = Find(id);
            if (it == null) return false;
            bool ok = Inventory.RemoveItem(it, 1);
            Inventory.RefreshStats();
            return ok;
        }

        public static bool Has(string id)
        {
            var it = Find(id);
            return it != null && Inventory.Count(it) > 0;
        }

        // ------------------------------------------------------------ biểu tượng
        static Sprite IconCore()
        {
            var c = new PxCanvas(16, 16);
            var gold = PxCanvas.C(0xFF9A1A);
            c.Rect(5, 1, 6, 14, PxCanvas.Shade(gold, 0.55f)); c.Rect(1, 5, 14, 6, PxCanvas.Shade(gold, 0.55f));
            c.Rect(3, 3, 10, 10, PxCanvas.Shade(gold, 0.8f));
            c.Rect(5, 5, 6, 6, PxCanvas.C(0xFFE08A));
            c.Rect(7, 7, 2, 2, PxCanvas.C(0xFFFFFF));
            c.Outline(PxCanvas.C(0x0A0A16));
            return c.Bake(0.5f, 0.5f);
        }

        static Sprite IconBox()
        {
            var c = new PxCanvas(16, 16);
            c.Rect(2, 4, 12, 10, PxCanvas.C(0x16161F)); c.Rect(2, 4, 12, 2, PxCanvas.C(0x2C2C3C));
            c.Rect(2, 12, 12, 2, PxCanvas.C(0x0A0A12));
            c.Text(6, 7, "?", PxCanvas.C(0xFF2A4A));
            c.Frame(2, 4, 12, 10, PxCanvas.C(0x565C70));
            c.Outline(PxCanvas.C(0x0A0A16));
            return c.Bake(0.5f, 0.5f);
        }

        static Sprite IconKnife()
        {
            var c = new PxCanvas(16, 16);
            c.Line(3, 12, 11, 4, PxCanvas.C(0xE02828)); c.Line(4, 12, 12, 4, PxCanvas.C(0xFF5A5A)); c.Line(3, 11, 10, 4, PxCanvas.C(0xA01414));
            c.Rect(2, 12, 3, 3, PxCanvas.C(0x3A2A1E)); c.Rect(4, 10, 2, 2, PxCanvas.C(0x9A9AA8));
            c.Outline(PxCanvas.C(0x0A0A16));
            return c.Bake(0.5f, 0.5f);
        }

        static Sprite IconCard()
        {
            var c = new PxCanvas(16, 16);
            c.Rect(1, 3, 14, 10, PxCanvas.C(0x101018));
            c.Rect(1, 5, 14, 2, PxCanvas.C(0xFF2A4A));
            c.Rect(3, 9, 5, 1, PxCanvas.C(0x565C70)); c.Rect(3, 11, 8, 1, PxCanvas.C(0x565C70));
            c.Frame(1, 3, 14, 10, PxCanvas.C(0x565C70));
            c.Outline(PxCanvas.C(0x0A0A16));
            return c.Bake(0.5f, 0.5f);
        }
    }
}
