using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Bảng cửa hàng ở CenterShop. Hai quầy:
    ///  • Gear (quầy trái): mua / bán vật phẩm nhặt được (nguyên liệu, vật có giá trị) và trang bị;
    ///  • Supply (quầy phải): mua / bán vật phẩm tiêu hao và linh kiện (chỉ bậc B trở xuống).
    /// Cột trái = MUA (hàng bán sẵn, lấy từ ItemDatabase), cột phải = BÁN (đồ trong kho đồ). A / D đổi cột, W / S chọn, ENTER / F mua - bán 1 cái, ESC thoát.
    /// Giá mua = 3 lần giá bán của vật phẩm (tối thiểu 10). Linh kiện: giá theo bậc, bán lại được một nửa.
    /// </summary>
    public static class ShopUi
    {
        public enum Kind { Gear, Supply }

        class Row
        {
            public ItemData item;
            public bool isPart;
            public Rank rank;
            public int price, have, slot;
            public string name, sub;
            public Color color;
            public Sprite icon;
        }

        static Kind kind;
        static int col, selBuy, selSell;
        static readonly List<Row> buy = new List<Row>(), sell = new List<Row>();
        static string msg = "";
        static float msgT;
        static Vector2 lastMouse;

        const int Visible = 9;

        public static void Open(Kind k)
        {
            kind = k;
            BaseUi.Open(BaseMode.Shop);
        }

        public static int PartPrice(Rank r)
        {
            switch (r)
            {
                case Rank.F: return 12;
                case Rank.D: return 36;
                case Rank.C: return 100;
                default: return 300;           // B
            }
        }

        public static void ResetStatics() { buy.Clear(); sell.Clear(); msg = ""; msgT = 0f; col = 0; selBuy = selSell = 0; }

        public static void OnOpen() { col = 0; selBuy = selSell = 0; msg = ""; msgT = 0f; Rebuild(); }

        static bool InKind(ItemData it)
        {
            if (it == null || it.questItem) return false;
            if (kind == Kind.Gear) return it.kind == ItemKind.Passive || it.kind == ItemKind.Material || it.kind == ItemKind.Valuable;
            return it.kind == ItemKind.Consumable;
        }

        static void Rebuild()
        {
            buy.Clear(); sell.Clear();
            var all = ItemDatabase.All();
            for (int i = 0; i < all.Count; i++)
            {
                var it = all[i];
                if (!InKind(it) || it.kind == ItemKind.Valuable) continue;      // vật có giá trị chỉ bán lại, không bán sẵn
                buy.Add(new Row { item = it, price = Mathf.Max(10, it.sellValue * 3), name = it.DisplayName, color = it.color, icon = it.icon, have = Inventory.Count(it) });
            }
            buy.Sort((a, b) => a.item.kind != b.item.kind ? a.item.kind.CompareTo(b.item.kind) : a.price.CompareTo(b.price));
            if (kind == Kind.Supply)
                for (Rank r = Rank.F; r <= Rank.B; r++)
                    buy.Add(new Row { isPart = true, rank = r, price = PartPrice(r), name = Cfg.L("Linh kiện bậc ", "Part rank ") + r, color = PowerRules.RankColor(r), have = SkillSystem.Parts(r) });

            for (int i = 0; i < Inventory.Slots.Count; i++)
            {
                var s = Inventory.Slots[i];
                if (!InKind(s.item) || s.count <= 0) continue;
                sell.Add(new Row { item = s.item, slot = i, price = s.item.sellValue, name = s.item.DisplayName, color = s.item.color, icon = s.item.icon, have = s.count });
            }
            if (kind == Kind.Supply)
                for (Rank r = Rank.F; r <= Rank.B; r++)
                {
                    int n = SkillSystem.Parts(r);
                    if (n > 0) sell.Add(new Row { isPart = true, rank = r, price = PartPrice(r) / 2, name = Cfg.L("Linh kiện bậc ", "Part rank ") + r, color = PowerRules.RankColor(r), have = n });
                }
            selBuy = Mathf.Clamp(selBuy, 0, Mathf.Max(0, buy.Count - 1));
            selSell = Mathf.Clamp(selSell, 0, Mathf.Max(0, sell.Count - 1));
        }

        static void Say(string m) { msg = m; msgT = 2.4f; }

        static void DoBuy()
        {
            if (buy.Count == 0) return;
            var r = buy[Mathf.Clamp(selBuy, 0, buy.Count - 1)];
            if (Inventory.Coins < r.price) { Say(Cfg.L("Không đủ xu.", "Not enough coins.")); Sfx.Play("click", 0.6f, 0.5f); return; }
            if (r.isPart) SkillSystem.AddParts(r.rank, 1, false, false);
            else if (!Inventory.Add(r.item, 1)) { Say(Cfg.L("Kho đồ đã đầy.", "Your bag is full.")); Sfx.Play("click", 0.6f, 0.5f); return; }
            Inventory.Coins -= r.price;
            Say(Cfg.L("Đã mua: ", "Bought: ") + r.name);
            Sfx.Play("pickup", 0.7f, 1.1f);
            Rebuild();
        }

        static void DoSell()
        {
            if (sell.Count == 0) return;
            var r = sell[Mathf.Clamp(selSell, 0, sell.Count - 1)];
            if (r.isPart)
            {
                if (!SkillSystem.SpendParts(r.rank, 1)) return;
                Inventory.Coins += r.price;
            }
            else
            {
                if (Inventory.Sell(r.slot, 1) <= 0) return;
            }
            Say(Cfg.L("Đã bán: ", "Sold: ") + r.name + "  +" + r.price);
            Sfx.Play("pickup", 0.7f, 1.5f);
            Rebuild();
        }

        public static void UpdateInput()
        {
            if (GameInput.PauseDown) { BaseUi.Close(); return; }
            if (msgT > 0f) msgT -= Time.unscaledDeltaTime;
            if (GameInput.NavLeftDown) { col = 0; Sfx.Play("click", 0.3f, 1.5f); }
            if (GameInput.NavRightDown) { col = 1; Sfx.Play("click", 0.3f, 1.5f); }
            int d = (GameInput.NavDownDown ? 1 : 0) - (GameInput.NavUpDown ? 1 : 0);
            if (d != 0)
            {
                if (col == 0 && buy.Count > 0) selBuy = (selBuy + d + buy.Count) % buy.Count;
                if (col == 1 && sell.Count > 0) selSell = (selSell + d + sell.Count) % sell.Count;
                Sfx.Play("click", 0.3f, 1.7f);
            }
            if (GameInput.ConfirmDown || GameInput.InteractDown)
            {
                if (col == 0) DoBuy(); else DoSell();
            }
        }

        public static void Draw(Hud h)
        {
            h.Dim(0.55f);
            Rect panel = h.R(110, 56, 1060, 608);
            h.Fill(panel, new Color(0.01f, 0.02f, 0.07f, 0.96f));
            Color tc = kind == Kind.Gear ? Cfg.Cyan : Cfg.Lime;
            h.Outline(panel, tc, 3f);
            h.Txt(h.R(136, 66, 700, 40), kind == Kind.Gear ? Cfg.L("QUẦY VẬT PHẨM & TRANG BỊ", "ITEMS & GEAR COUNTER") : Cfg.L("QUẦY TIÊU HAO & LINH KIỆN", "CONSUMABLES & PARTS COUNTER"), 28, tc, TextAnchor.MiddleLeft);
            h.Txt(h.R(850, 66, 290, 40), Cfg.L("Xu: ", "Coins: ") + Inventory.Coins, 26, Cfg.Yellow, TextAnchor.MiddleRight);
            if (kind == Kind.Supply) h.Txt(h.R(136, 104, 800, 22), Cfg.L("Linh kiện: chỉ nhận / bán bậc B trở xuống", "Parts: rank B and below only"), 14, Cfg.A(Color.white, 0.6f), TextAnchor.MiddleLeft, false);

            bool moved = (Event.current.mousePosition - lastMouse).sqrMagnitude > 1f;
            DrawColumn(h, 0, 136, Cfg.L("MUA", "BUY"), buy, ref selBuy, moved);
            DrawColumn(h, 1, 616, Cfg.L("BÁN", "SELL"), sell, ref selSell, moved);
            if (Event.current.type == EventType.Repaint) lastMouse = Event.current.mousePosition;

            // mô tả mục đang chọn
            var cur = col == 0 ? (buy.Count > 0 ? buy[Mathf.Clamp(selBuy, 0, buy.Count - 1)] : null) : (sell.Count > 0 ? sell[Mathf.Clamp(selSell, 0, sell.Count - 1)] : null);
            Rect desc = h.R(136, 548, 760, 76);
            h.Fill(desc, new Color(0.03f, 0.05f, 0.12f, 0.9f));
            h.Outline(desc, Cfg.A(Color.white, 0.25f), 1.5f);
            if (cur != null)
            {
                string d = cur.isPart ? Cfg.L("Linh kiện dùng để chế tạo minibot hoặc ghép lên bậc cao hơn.", "Parts used to craft minibots or fuse into a higher rank.")
                                      : (cur.item.Description + (cur.item.EffectLines().Length > 0 ? "\n" + cur.item.EffectLines() : ""));
                h.TxtWrap(new Rect(desc.x + 10f * h.s, desc.y + 4f * h.s, desc.width - 20f * h.s, desc.height - 6f * h.s), d, 14, Color.white);
            }
            Rect act = h.R(916, 548, 224, 76);
            string lab = col == 0 ? Cfg.L("MUA 1  [ENTER]", "BUY 1  [ENTER]") : Cfg.L("BÁN 1  [ENTER]", "SELL 1  [ENTER]");
            if (h.Button(act, lab, col == 0 ? Cfg.Lime : Cfg.Orange, 22)) { if (col == 0) DoBuy(); else DoSell(); }
            if (msgT > 0f) h.Txt(h.R(136, 626, 760, 30), msg, 18, Cfg.Yellow, TextAnchor.MiddleLeft);
            h.Txt(h.R(136, 632, 1004, 24), Cfg.L("[A/D] đổi cột   [W/S] chọn   [ENTER] / [F] mua - bán   [ESC] thoát", "[A/D] switch column   [W/S] select   [ENTER] / [F] buy - sell   [ESC] leave"), 14, Cfg.A(Color.white, 0.55f), TextAnchor.MiddleRight, false);
        }

        static void DrawColumn(Hud h, int c, float x, string title, List<Row> rows, ref int sel, bool moved)
        {
            bool active = col == c;
            Color hc = active ? Cfg.Yellow : Cfg.A(Color.white, 0.6f);
            h.Txt(h.R(x, 128, 200, 28), title, 22, hc, TextAnchor.MiddleLeft);
            Rect box = h.R(x, 158, 504, 380);
            h.Outline(box, Cfg.A(active ? Cfg.Yellow : Color.white, active ? 0.8f : 0.25f), 2f);
            if (rows.Count == 0)
            {
                h.Txt(box, c == 0 ? Cfg.L("Hết hàng", "Out of stock") : Cfg.L("Không có gì để bán", "Nothing to sell"), 18, Cfg.A(Color.white, 0.5f), TextAnchor.MiddleCenter, false);
                return;
            }
            int top = Mathf.Clamp(sel - Visible / 2, 0, Mathf.Max(0, rows.Count - Visible));
            for (int i = 0; i < Visible && top + i < rows.Count; i++)
            {
                int idx = top + i;
                var r = rows[idx];
                Rect rr = h.R(x + 4, 162 + i * 41.5f, 496, 39);
                bool hover = rr.Contains(Event.current.mousePosition);
                if (hover && moved && Event.current.type == EventType.Repaint) { sel = idx; col = c; }
                bool on = sel == idx && active;
                h.Fill(rr, on ? Cfg.A(Cfg.Yellow, 0.18f) : new Color(0.03f, 0.05f, 0.12f, 0.8f));
                if (on) h.Outline(rr, Cfg.Yellow, 2f);
                Rect ic = new Rect(rr.x + 4f * h.s, rr.y + 3f * h.s, 33f * h.s, 33f * h.s);
                h.Fill(ic, Cfg.A(r.color, 0.18f));
                h.Outline(ic, Cfg.A(r.color, 0.8f), 1.5f);
                if (r.icon != null) h.DrawSprite(r.icon, ic, Color.white);
                h.Txt(new Rect(rr.x + 44f * h.s, rr.y, 290f * h.s, rr.height), r.name + (c == 1 || r.isPart ? "  ×" + r.have : (r.have > 0 ? "  (" + r.have + ")" : "")), 16, r.color, TextAnchor.MiddleLeft);
                h.Txt(new Rect(rr.x + 330f * h.s, rr.y, 160f * h.s, rr.height), r.price + Cfg.L(" xu", " c"), 17, Cfg.Yellow, TextAnchor.MiddleRight);
                if (BaseUi.Clicked(rr)) { sel = idx; col = c; }
            }
            if (rows.Count > Visible)
                h.Txt(h.R(x, 538, 504, 18), (sel + 1) + " / " + rows.Count, 12, Cfg.A(Color.white, 0.45f), TextAnchor.MiddleRight, false);
        }
    }
}
