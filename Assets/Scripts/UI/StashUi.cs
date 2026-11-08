using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Giao diện kho căn cứ (giá kệ ở căn cứ, bấm F). Hai cột: TRÊN NGƯỜI (kho đồ + linh kiện đang mang) và KHO CĂN CỨ.
    /// Cất / lấy đồ giữa hai cột. Đồ cất ở đây KHÔNG mất khi chết. Đồ đang gắn ở ô trang bị phải tháo ra (B ▸ Player) rồi mới cất được.
    /// Phím: A D đổi cột · W S chọn dòng · ENTER (hoặc bấm chuột vào dòng) = chuyển 1 · E = chuyển cả dòng · ESC / F đóng.
    /// </summary>
    public static class StashUi
    {
        struct Row
        {
            public ItemData item;
            public int index;      // vị trí trong danh sách (Inventory.Slots hoặc Stash.Items); -1 với linh kiện
            public int partRank;   // >= 0: dòng linh kiện của bậc này
            public int count;
        }

        static int col;                       // 0 = trên người, 1 = kho căn cứ
        static readonly int[] sel = new int[2];
        static readonly int[] scroll = new int[2];
        const int Visible = 12;
        const float RowH = 34f;

        public static void OnOpen()
        {
            col = 0;
            sel[0] = sel[1] = 0;
            scroll[0] = scroll[1] = 0;
            Inventory.Msg = "";
            Inventory.MsgT = 0f;
        }

        public static void OnClose() { SaveManager.AutoSave(); }

        static List<Row> Rows(int c)
        {
            var l = new List<Row>();
            if (c == 0)
            {
                for (int i = 0; i < Inventory.Slots.Count; i++)
                    l.Add(new Row { item = Inventory.Slots[i].item, index = i, partRank = -1, count = Inventory.Slots[i].count });
                for (int r = 0; r < PowerRules.PartRankCount; r++)
                    if (SkillSystem.Parts((Rank)r) > 0) l.Add(new Row { index = -1, partRank = r, count = SkillSystem.Parts((Rank)r) });
            }
            else
            {
                for (int i = 0; i < Stash.Items.Count; i++)
                    l.Add(new Row { item = Stash.Items[i].item, index = i, partRank = -1, count = Stash.Items[i].count });
                for (int r = 0; r < PowerRules.PartRankCount; r++)
                    if (Stash.Parts((Rank)r) > 0) l.Add(new Row { index = -1, partRank = r, count = Stash.Parts((Rank)r) });
            }
            return l;
        }

        static void Clamp(int c, int n)
        {
            sel[c] = Mathf.Clamp(sel[c], 0, Mathf.Max(0, n - 1));
            if (sel[c] < scroll[c]) scroll[c] = sel[c];
            if (sel[c] >= scroll[c] + Visible) scroll[c] = sel[c] - Visible + 1;
            scroll[c] = Mathf.Clamp(scroll[c], 0, Mathf.Max(0, n - Visible));
        }

        public static void UpdateInput()
        {
            if (GameInput.PauseDown || GameInput.InteractDown) { BaseUi.Close(); return; }
            if (GameInput.NavLeftDown && col != 0) { col = 0; Sfx.Play("click", 0.4f, 1.3f); }
            if (GameInput.NavRightDown && col != 1) { col = 1; Sfx.Play("click", 0.4f, 1.3f); }
            int n = Rows(col).Count;
            if (GameInput.NavUpDown && sel[col] > 0) { sel[col]--; Sfx.Play("click", 0.3f, 1.4f); }
            if (GameInput.NavDownDown && sel[col] < n - 1) { sel[col]++; Sfx.Play("click", 0.3f, 1.4f); }
            Clamp(col, n);
            if (GameInput.ConfirmDown) Move(col, sel[col], false);
            else if (GameInput.TabNextDown) Move(col, sel[col], true);
        }

        static void Move(int c, int row, bool all)
        {
            var rows = Rows(c);
            if (row < 0 || row >= rows.Count) return;
            var r = rows[row];
            int n = all ? r.count : 1;
            if (c == 0)
            {
                if (r.partRank >= 0) Stash.DepositParts((Rank)r.partRank, n);
                else Stash.Deposit(r.index, n);
            }
            else
            {
                if (r.partRank >= 0) Stash.WithdrawParts((Rank)r.partRank, n);
                else Stash.Withdraw(r.index, n);
            }
            Clamp(c, Rows(c).Count);
        }

        public static void Draw(Hud h)
        {
            h.Dim(0.76f);
            Rect panel = h.R(150, 40, 980, 640);
            h.Fill(panel, new Color(0.01f, 0.02f, 0.06f, 0.96f));
            h.Outline(panel, Cfg.A(Cfg.Cyan, 0.8f), 2f);
            h.Txt(h.R(176, 48, 600, 44), Cfg.L("KHO CĂN CỨ", "BASE STASH"), 34, Cfg.Cyan);
            h.Txt(h.R(178, 94, 800, 20), Cfg.L("Đồ cất ở đây không mất khi chết. Đồ mang trên người thì mất hết.", "Items stored here survive death. Items you carry are lost."),
                  13, Cfg.A(Color.white, 0.65f), TextAnchor.UpperLeft, false);
            CraftUi.DrawWallet(h, 800, 54);

            DrawColumn(h, 0, 176, Cfg.L("TRÊN NGƯỜI", "CARRIED"), Inventory.Slots.Count + " / " + Inventory.Capacity, Cfg.Lime);
            DrawColumn(h, 1, 644, Cfg.L("KHO CĂN CỨ", "BASE STASH"), Stash.Items.Count + " / " + Stash.Capacity, Cfg.Cyan);

            // mũi tên chuyển ở giữa
            h.Txt(h.R(600, 330, 40, 60), col == 0 ? ">" : "<", 40, Cfg.A(Cfg.Yellow, 0.9f), TextAnchor.MiddleCenter);

            if (Inventory.MsgT > 0f && !string.IsNullOrEmpty(Inventory.Msg))
                h.Txt(h.R(176, 590, 928, 22), Inventory.Msg, 15, Cfg.Yellow, TextAnchor.UpperLeft, false);

            if (h.Button(h.R(176, 616, 230, 44), Cfg.L("CHUYỂN 1  [ENTER]", "MOVE 1  [ENTER]"), Cfg.Cyan, 18)) Move(col, sel[col], false);
            if (h.Button(h.R(420, 616, 230, 44), Cfg.L("CHUYỂN HẾT  [E]", "MOVE ALL  [E]"), Cfg.Magenta, 18)) Move(col, sel[col], true);
            h.Txt(h.R(672, 622, 440, 36), Cfg.L("[A D] đổi cột   ·   [W S] chọn   ·   [ESC / F] đóng", "[A D] column   ·   [W S] select   ·   [ESC / F] close"),
                  13, Cfg.A(Color.white, 0.6f), TextAnchor.MiddleLeft, false);
        }

        static void DrawColumn(Hud h, int c, float x, string title, string cap, Color accent)
        {
            bool active = col == c;
            Rect box = h.R(x, 124, 460, 456);
            h.Fill(box, new Color(0.02f, 0.03f, 0.09f, 0.9f));
            h.Outline(box, Cfg.A(accent, active ? 0.9f : 0.35f), active ? 2.5f : 1.5f);
            h.Txt(h.R(x + 12, 128, 300, 28), title, 20, accent);
            h.Txt(h.R(x + 250, 128, 198, 28), cap, 16, Cfg.A(Color.white, 0.75f), TextAnchor.MiddleRight);

            var rows = Rows(c);
            Clamp(c, rows.Count);
            if (rows.Count == 0)
            {
                h.Txt(h.R(x, 300, 460, 40), Cfg.L("(trống)", "(empty)"), 18, Cfg.A(Color.white, 0.4f), TextAnchor.MiddleCenter, false);
                return;
            }

            Sprite partIcon = Art.Bank != null ? Art.Bank.partIcon : null;
            for (int k = 0; k < Visible; k++)
            {
                int i = scroll[c] + k;
                if (i >= rows.Count) break;
                var r = rows[i];
                float y = 160f + k * RowH;
                Rect rr = h.R(x + 8, y, 444, RowH - 2f);
                bool isSel = active && sel[c] == i;
                h.Fill(rr, Cfg.A(accent, isSel ? 0.24f : 0.06f));
                if (isSel) h.Outline(rr, accent, 2f);

                Rect ic = h.R(x + 14, y + 2, RowH - 6, RowH - 6);
                string name;
                Color tc;
                if (r.partRank >= 0)
                {
                    Color rc = PowerRules.RankColor((Rank)r.partRank);
                    if (partIcon != null) h.DrawSprite(partIcon, ic, rc); else h.Fill(ic, Cfg.A(rc, 0.8f));
                    name = Cfg.L("Linh kiện bậc ", "Rank ") + (Rank)r.partRank + Cfg.L("", " part");
                    tc = rc;
                }
                else
                {
                    if (r.item != null && r.item.icon != null) h.DrawSprite(r.item.icon, ic, Color.white);
                    else h.Fill(ic, Cfg.A(r.item != null ? r.item.color : Color.gray, 0.6f));
                    name = r.item != null ? r.item.DisplayName : "?";
                    tc = r.item != null ? r.item.color : Color.white;
                }
                h.Txt(h.R(x + 14 + RowH, y, 330, RowH - 2), name, 16, tc, TextAnchor.MiddleLeft);
                h.Txt(h.R(x + 350, y, 96, RowH - 2), "x" + r.count, 18, Color.white, TextAnchor.MiddleRight);

                if (BaseUi.Clicked(rr))
                {
                    col = c;
                    sel[c] = i;
                    Move(c, i, false);
                }
            }

            // thanh cuộn đơn giản
            if (rows.Count > Visible)
            {
                float frac = (float)scroll[c] / Mathf.Max(1, rows.Count - Visible);
                Rect track = h.R(x + 452, 160, 4, Visible * RowH);
                h.Fill(track, Cfg.A(accent, 0.15f));
                h.Fill(h.R(x + 452, 160 + frac * (Visible * RowH - 40), 4, 40), Cfg.A(accent, 0.8f));
            }
        }
    }
}
