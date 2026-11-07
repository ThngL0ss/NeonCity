using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Menu nhân vật (phím B): một cửa sổ chung có 3 mục — KHO ĐỒ · SKILL · PLAYER.
    /// Đổi mục: bấm chuột vào thẻ phía trên, phím 1 / 2 / 3 hoặc Tab. B hoặc Esc để đóng. Game tạm dừng khi menu mở.
    /// Nội dung từng mục do InventoryUi, SkillUi và PlayerUi vẽ; lớp này vẽ khung, thẻ mục và ví tiền.
    /// </summary>
    public static class GameMenu
    {
        public const int TabInventory = 0, TabSkill = 1, TabPlayer = 2, TabCount = 3;

        public static bool IsOpen;
        public static int Tab;

        public static void ResetAll()
        {
            IsOpen = false;
            Tab = TabInventory;
            PlayerUi.EqSel = 0;
        }

        static string TabName(int i)
        {
            switch (i)
            {
                case TabInventory: return Cfg.L("KHO ĐỒ", "INVENTORY");
                case TabSkill: return "SKILL";
            }
            return "PLAYER";
        }

        public static void SetTab(int t)
        {
            t = Mathf.Clamp(t, 0, TabCount - 1);
            if (t == Tab) return;
            Tab = t;
            Sfx.Play("click", 0.5f, 1.3f);
        }

        /// <summary>Đọc phím khi menu đang mở (GameManager gọi mỗi frame).</summary>
        public static void UpdateInput()
        {
            if (GameInput.MenuTabPick >= 0) SetTab(GameInput.MenuTabPick);
            else if (GameInput.MenuCycleDown) SetTab((Tab + 1) % TabCount);

            switch (Tab)
            {
                case TabInventory: InventoryUi.UpdateInput(); break;
                case TabSkill: SkillUi.UpdateInput(); break;
                default: PlayerUi.UpdateInput(); break;
            }
        }

        public static void Draw(Hud h)
        {
            h.Dim(0.74f);
            Rect panel = h.R(150, 50, 980, 620);
            h.Fill(panel, new Color(0.01f, 0.02f, 0.06f, 0.94f));
            h.Outline(panel, Cfg.A(Cfg.Cyan, 0.75f), 2f);

            DrawTabBar(h);
            DrawWallet(h);

            switch (Tab)
            {
                case TabInventory: InventoryUi.DrawContent(h); break;
                case TabSkill: SkillUi.DrawContent(h); break;
                default: PlayerUi.DrawContent(h); break;
            }
        }

        static void DrawTabBar(Hud h)
        {
            Event ev = Event.current;
            for (int i = 0; i < TabCount; i++)
            {
                Rect r = h.R(176 + i * 138, 58, 130, 38);
                bool sel = Tab == i;
                bool hover = r.Contains(ev.mousePosition);
                h.Fill(r, new Color(0.02f, 0.03f, 0.09f, 0.95f));
                if (sel) h.Fill(r, Cfg.A(Cfg.Cyan, 0.24f));
                else if (hover) h.Fill(r, Cfg.A(Cfg.Cyan, 0.10f));
                h.Outline(r, sel ? Cfg.Cyan : Cfg.A(Cfg.Cyan, 0.4f), sel ? 2.5f : 1.5f);
                h.Txt(r, "[" + (i + 1) + "] " + TabName(i), 17, sel ? Color.white : Cfg.A(Cfg.Cyan, 0.9f), TextAnchor.MiddleCenter);
                if (hover && ev.type == EventType.MouseDown && ev.button == 0)
                {
                    SetTab(i);
                    ev.Use();
                }
            }
        }

        static void DrawWallet(Hud h)
        {
            Rect box = h.R(850, 62, 258, 40);
            h.Fill(box, new Color(0.02f, 0.03f, 0.09f, 0.9f));
            h.Outline(box, Cfg.A(Cfg.Yellow, 0.6f), 1.5f);
            Rect ic = h.R(858, 68, 28, 28);
            var coinSpr = Art.Bank != null ? Art.Bank.coinIcon : null;
            if (coinSpr != null) h.DrawSprite(coinSpr, ic, Color.white);
            else h.Fill(ic, Cfg.A(Cfg.Yellow, 0.9f));
            h.Txt(h.R(892, 62, 70, 40), Cfg.L("XU", "COINS"), 15, Cfg.A(Cfg.Yellow, 0.95f), TextAnchor.MiddleLeft);
            h.Txt(h.R(940, 62, 160, 40), Inventory.Coins.ToString(), 24, Color.white, TextAnchor.MiddleRight);

            // mục Skill: thêm tổng linh kiện ngay cạnh ví
            if (Tab == TabSkill)
                h.Txt(h.R(604, 62, 236, 40), Cfg.L("Linh kiện: ", "Parts: ") + SkillSystem.PartsSummary(), 14, Cfg.A(Cfg.Cyan, 0.9f), TextAnchor.MiddleRight, false);
        }

        /// <summary>Dòng gợi ý phím chung cho cả ba mục.</summary>
        public const string CommonHintVi = "[B / ESC] đóng   ·   [1 2 3 / TAB] đổi mục";
        public const string CommonHintEn = "[B / ESC] close   ·   [1 2 3 / TAB] switch tab";
    }
}
