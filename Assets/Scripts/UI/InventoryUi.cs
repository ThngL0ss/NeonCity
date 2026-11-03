using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Giao diện kho đồ (phím B). Vẽ bằng IMGUI qua các hàm của Hud (không cần Canvas).
    /// Phím: A/D/W/S hoặc mũi tên = chọn ô · Enter = dùng · X = vứt 1 · B hoặc Esc = đóng. Chuột: bấm chọn, bấm đúp để dùng.
    /// Khi mở kho game tạm dừng.
    /// </summary>
    public static class InventoryUi
    {
        public const int Cols = 6;

        /// <summary>Đọc phím khi kho đang mở (GameManager gọi mỗi frame).</summary>
        public static void UpdateInput()
        {
            int cap = Inventory.Capacity;
            int sel = Inventory.Sel;
            if (GameInput.NavLeftDown) sel--;
            if (GameInput.NavRightDown) sel++;
            if (GameInput.NavUpDown) sel -= Cols;
            if (GameInput.NavDownDown) sel += Cols;
            sel = Mathf.Clamp(sel, 0, cap - 1);
            if (sel != Inventory.Sel) Sfx.Play("click", 0.4f, 1.3f);
            Inventory.Sel = sel;

            if (GameInput.ConfirmDown) Inventory.Use(sel);
            if (GameInput.DiscardDown) Discard(sel);
        }

        static void Discard(int index)
        {
            if (index < 0 || index >= Inventory.Slots.Count) return;
            var it = Inventory.Slots[index].item;
            Inventory.Remove(index, 1);
            Inventory.Say(Cfg.L("Đã vứt 1: ", "Dropped 1: ") + (it != null ? it.DisplayName : ""));
            Sfx.Play("click", 0.6f, 0.7f);
        }

        public static void Draw(Hud h)
        {
            float s = h.s;
            h.Dim(0.74f);
            Rect panel = h.R(150, 50, 980, 620);
            h.Fill(panel, new Color(0.01f, 0.02f, 0.06f, 0.94f));
            h.Outline(panel, Cfg.A(Cfg.Cyan, 0.75f), 2f);

            int cap = Inventory.Capacity;
            h.Txt(h.R(176, 58, 380, 44), Cfg.L("KHO ĐỒ", "INVENTORY"), 34, Cfg.Cyan);
            h.Txt(h.R(178, 104, 380, 22), Inventory.Slots.Count + " / " + cap + Cfg.L(" ô", " slots"), 14, Cfg.A(Color.white, 0.7f), TextAnchor.UpperLeft, false);

            // ---- ví tiền
            Rect coinBox = h.R(850, 62, 258, 40);
            h.Fill(coinBox, new Color(0.02f, 0.03f, 0.09f, 0.9f));
            h.Outline(coinBox, Cfg.A(Cfg.Yellow, 0.6f), 1.5f);
            Rect ic = h.R(858, 68, 28, 28);
            var coinSpr = Art.Bank != null ? Art.Bank.coinIcon : null;
            if (coinSpr != null) h.DrawSprite(coinSpr, ic, Color.white);
            else h.Fill(ic, Cfg.A(Cfg.Yellow, 0.9f));
            h.Txt(h.R(892, 62, 70, 40), Cfg.L("XU", "COINS"), 15, Cfg.A(Cfg.Yellow, 0.95f), TextAnchor.MiddleLeft);
            h.Txt(h.R(940, 62, 160, 40), Inventory.Coins.ToString(), 24, Color.white, TextAnchor.MiddleRight);

            // ---- lưới ô
            int rows = Mathf.CeilToInt(cap / (float)Cols);
            float size = Mathf.Clamp(300f / rows - 8f, 40f, 66f);
            float pitch = size + 8f;
            float gx = 176f, gy = 136f;
            Event ev = Event.current;
            for (int i = 0; i < cap; i++)
            {
                int cx = i % Cols, cy = i / Cols;
                Rect r = h.R(gx + cx * pitch, gy + cy * pitch, size, size);
                ItemData it = i < Inventory.Slots.Count ? Inventory.Slots[i].item : null;
                bool has = it != null;
                bool sel = i == Inventory.Sel;
                bool hover = r.Contains(ev.mousePosition);
                Color border = has ? it.color : Cfg.Cyan;

                h.Fill(r, new Color(0.02f, 0.03f, 0.09f, 0.95f));
                if (sel) h.Fill(r, Cfg.A(border, 0.22f));
                else if (hover) h.Fill(r, Cfg.A(border, 0.10f));
                if (has)
                {
                    float pad = 5f * s;
                    Rect icon = new Rect(r.x + pad, r.y + pad, r.width - pad * 2f, r.height - pad * 2f);
                    if (it.icon != null) h.DrawSprite(it.icon, icon, Color.white);
                    else h.Txt(icon, (it.DisplayName + "?").Substring(0, 1), 26, border, TextAnchor.MiddleCenter);
                    int n = Inventory.Slots[i].count;
                    if (n > 1) h.Txt(new Rect(r.x, r.yMax - 20f * s, r.width - 4f * s, 18f * s), "x" + n, 13, Color.white, TextAnchor.LowerRight);
                }
                h.Outline(r, sel ? Color.white : Cfg.A(border, has ? 0.75f : 0.3f), sel ? 3f : 1.5f);

                if (hover && ev.type == EventType.MouseDown)
                {
                    Inventory.Sel = i;
                    if (ev.clickCount >= 2) Inventory.Use(i);
                    Sfx.Play("click", 0.4f, 1.3f);
                    ev.Use();
                }
            }

            // ---- chi tiết ô đang chọn
            DrawDetails(h);

            // ---- tổng chỉ số từ vật phẩm
            float sy = gy + rows * pitch + 10f;
            h.Txt(h.R(176, sy, 440, 22), Cfg.L("CHỈ SỐ TỪ VẬT PHẨM", "BONUSES FROM ITEMS"), 14, Cfg.A(Cfg.Cyan, 0.9f));
            int lines = 0;
            var stats = (ItemStat[])System.Enum.GetValues(typeof(ItemStat));
            for (int i = 0; i < stats.Length; i++)
            {
                float v = Inventory.Stat(stats[i]);
                if (Mathf.Abs(v) < 0.001f) continue;
                bool good = stats[i] == ItemStat.DamageTakenPct || stats[i] == ItemStat.DashCooldownPct ? v < 0f : v > 0f;
                h.Txt(h.R(180, sy + 22f + lines * 17f, 440, 18), ItemData.StatLabel(stats[i], v), 14, good ? Cfg.Lime : Cfg.Orange, TextAnchor.UpperLeft, false);
                lines++;
            }
            if (lines == 0)
                h.Txt(h.R(180, sy + 22f, 440, 18), Cfg.L("(chưa có hiệu ứng nào)", "(no bonuses yet)"), 14, Cfg.A(Color.white, 0.5f), TextAnchor.UpperLeft, false);

            // ---- thông báo + gợi ý phím
            if (Inventory.MsgT > 0f && !string.IsNullOrEmpty(Inventory.Msg))
                h.TxtWrap(h.R(660, 552, 446, 44), Inventory.Msg, 15, Cfg.Yellow);
            h.Txt(h.R(660, 602, 446, 20), Cfg.L("Tổng giá trị đồ trong kho: ", "Total value in bag: ") + Inventory.TotalSellValue() + Cfg.L(" xu", " coins"),
                  13, Cfg.A(Cfg.Yellow, 0.75f), TextAnchor.UpperLeft, false);
            h.Txt(h.R(176, 644, 940, 20),
                  Cfg.L("[B / ESC] đóng   ·   [A D W S / mũi tên] chọn   ·   [ENTER] dùng   ·   [X] vứt 1   ·   chuột: bấm chọn, bấm đúp = dùng",
                        "[B / ESC] close   ·   [A D W S / arrows] select   ·   [ENTER] use   ·   [X] drop 1   ·   mouse: click select, double-click = use"),
                  13, Cfg.A(Color.white, 0.6f), TextAnchor.UpperLeft, false);
        }

        static void DrawDetails(Hud h)
        {
            Rect dp = h.R(660, 136, 446, 404);
            h.Fill(dp, new Color(0.02f, 0.03f, 0.09f, 0.7f));
            h.Outline(dp, Cfg.A(Cfg.Cyan, 0.4f), 1.5f);

            int sel = Inventory.Sel;
            if (sel < 0 || sel >= Inventory.Slots.Count || Inventory.Slots[sel].item == null)
            {
                h.Txt(dp, Cfg.L("Chọn một vật phẩm để xem chi tiết", "Select an item to see details"), 16, Cfg.A(Color.white, 0.5f), TextAnchor.MiddleCenter, false);
                return;
            }

            var slot = Inventory.Slots[sel];
            var it = slot.item;
            Rect big = h.R(676, 150, 64, 64);
            h.Fill(big, new Color(0.01f, 0.02f, 0.06f, 0.9f));
            h.Outline(big, Cfg.A(it.color, 0.8f), 2f);
            if (it.icon != null) h.DrawSprite(it.icon, new Rect(big.x + 4f * h.s, big.y + 4f * h.s, big.width - 8f * h.s, big.height - 8f * h.s), Color.white);

            h.Txt(h.R(754, 148, 340, 34), it.DisplayName, 24, it.color);
            h.Txt(h.R(754, 150, 340, 34), "x" + slot.count, 18, Cfg.A(Color.white, 0.8f), TextAnchor.UpperRight);
            h.TxtWrap(h.R(754, 184, 340, 36), it.KindLabel, 13, Cfg.A(Color.white, 0.7f));

            h.TxtWrap(h.R(676, 230, 414, 90), it.Description, 15, Cfg.A(Color.white, 0.92f));
            string eff = it.EffectLines();
            if (!string.IsNullOrEmpty(eff)) h.TxtWrap(h.R(676, 324, 414, 120), eff, 15, Cfg.Lime);

            h.Txt(h.R(676, 450, 414, 24), Cfg.L("Giá bán: ", "Sell value: ") + it.sellValue + Cfg.L(" xu / cái", " coins each")
                  + (slot.count > 1 ? "   (" + it.sellValue * slot.count + Cfg.L(" xu tất cả)", " total)") : ""), 14, Cfg.Yellow, TextAnchor.UpperLeft, false);

            if (it.kind == ItemKind.Consumable)
            {
                if (h.Button(h.R(676, 484, 200, 42), Cfg.L("DÙNG  [ENTER]", "USE  [ENTER]"), Cfg.Lime, 20)) Inventory.Use(sel);
            }
            if (h.Button(h.R(890, 484, 200, 42), Cfg.L("VỨT 1  [X]", "DROP 1  [X]"), Cfg.Red, 20)) Discard(sel);
        }
    }
}
