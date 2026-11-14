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

        public static void ResetAll()
        {
            Shengu = ShenguNone;
            ArenaInfoDone = false;
            FighterWins = 0;
            QuestItems.Reset();
            Interior.ResetStatics();
        }

        public static void Export(SaveData d)
        {
            SaveManager.SetFlag(d.flags, "t.shengu", Shengu);
            SaveManager.SetFlag(d.flags, "t.arenaInfo", ArenaInfoDone ? 1 : 0);
            SaveManager.SetFlag(d.flags, "t.fWins", FighterWins);
        }

        public static void Import(SaveData d)
        {
            Shengu = SaveManager.GetFlag(d.flags, "t.shengu");
            ArenaInfoDone = SaveManager.GetFlag(d.flags, "t.arenaInfo") != 0;
            FighterWins = SaveManager.GetFlag(d.flags, "t.fWins");
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
        public const string Core6Fake = "Quest_core_t06_fake", Core6 = "Quest_core_t06", BlackBox = "Quest_black_box", FighterCard = "Quest_fighter_card";
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
