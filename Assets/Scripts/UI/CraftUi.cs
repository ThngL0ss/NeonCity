using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Bàn chế tác ở căn cứ: chọn công thức (W / S hoặc chuột), ENTER = chế tác. Công thức: BaseConfig ▸ Recipes.
    /// Nguyên liệu nhặt trên đường (bấm F) hoặc trong rương; nhiên liệu là dầu.
    /// </summary>
    public static class CraftUi
    {
        static int sel;
        static string msg = "";
        static bool msgOk;
        static float msgT;

        static List<CraftRecipe> Recipes()
        {
            var l = new List<CraftRecipe>();
            var all = BaseConfig.Cur.recipes;
            if (all != null) for (int i = 0; i < all.Count; i++) if (all[i] != null && all[i].output != null) l.Add(all[i]);
            return l;
        }

        public static void OnOpen() { sel = 0; msg = ""; msgT = 0f; }

        public static void UpdateInput()
        {
            var rs = Recipes();
            if (msgT > 0f) msgT -= Time.unscaledDeltaTime;
            if (GameInput.PauseDown || GameInput.InteractDown) { BaseUi.Close(); return; }
            if (rs.Count == 0) return;
            int s = sel;
            if (GameInput.NavUpDown || GameInput.NavLeftDown) s--;
            if (GameInput.NavDownDown || GameInput.NavRightDown) s++;
            s = Mathf.Clamp(s, 0, rs.Count - 1);
            if (s != sel) Sfx.Play("click", 0.4f, 1.3f);
            sel = s;
            if (GameInput.ConfirmDown) Do(rs[sel]);
        }

        static void Do(CraftRecipe r)
        {
            bool ok;
            msg = BaseCrafting.Craft(r, out ok);
            msgOk = ok;
            msgT = 3f;
            if (ok)
            {
                Sfx.Play("pickup", 0.9f, 1.1f);
                Sfx.Play("charge", 0.5f, 1.8f);
                var p = Player.Instance;
                if (p != null) Fx.Burst(p.Center + new Vector2(0f, 0.5f), Cfg.Cyan, 14, 6f, 0.5f, 0.3f);
            }
            else Sfx.Play("click", 0.7f, 0.6f);
        }

        public static void Draw(Hud h)
        {
            float s = h.s;
            h.Dim(0.74f);
            Rect panel = h.R(120, 50, 1040, 620);
            h.Fill(panel, new Color(0.01f, 0.02f, 0.06f, 0.95f));
            h.Outline(panel, Cfg.A(Cfg.Cyan, 0.8f), 2f);
            h.Txt(h.R(146, 58, 500, 44), Cfg.L("BÀN CHẾ TÁC", "CRAFTING BENCH"), 34, Cfg.Cyan);
            h.Txt(h.R(148, 104, 600, 20), Cfg.L("Nguyên liệu + dầu + xu = vật phẩm. Nguyên liệu nhặt trên đường (F) hoặc trong rương.", "Materials + oil + coins = items. Materials lie on the road (F) or in chests."),
                  13, Cfg.A(Color.white, 0.65f), TextAnchor.UpperLeft, false);
            DrawWallet(h, 800, 62);

            var rs = Recipes();
            Rect list = h.R(146, 134, 380, 456);
            h.Fill(list, new Color(0.02f, 0.03f, 0.09f, 0.7f));
            h.Outline(list, Cfg.A(Cfg.Cyan, 0.35f), 1.5f);
            if (rs.Count == 0)
            {
                h.Txt(list, Cfg.L("Chưa có công thức nào.\nChạy menu Tools ▸ Neon Breaker ▸ Cập nhật cấp bậc + dầu + căn cứ.", "No recipes yet.\nRun Tools ▸ Neon Breaker ▸ Cập nhật cấp bậc + dầu + căn cứ."),
                      15, Cfg.A(Color.white, 0.6f), TextAnchor.MiddleCenter, false);
                h.Txt(h.R(146, 640, 980, 20), Cfg.L("[ESC / F] đóng", "[ESC / F] close"), 13, Cfg.A(Color.white, 0.6f), TextAnchor.UpperLeft, false);
                return;
            }
            int shown = Mathf.Min(rs.Count, 7);
            int first = Mathf.Clamp(sel - shown + 1, 0, Mathf.Max(0, rs.Count - shown));
            for (int k = 0; k < shown; k++)
            {
                int i = first + k;
                var r = rs[i];
                Rect row = h.R(154, 142 + k * 62, 364, 56);
                bool can = BaseCrafting.CanCraft(r);
                h.Fill(row, new Color(0.01f, 0.02f, 0.06f, 0.85f));
                if (i == sel) h.Fill(row, Cfg.A(Cfg.Cyan, 0.16f));
                h.Outline(row, i == sel ? Cfg.Cyan : Cfg.A(r.output.color, 0.4f), i == sel ? 2.5f : 1.2f);
                Rect ic = h.R(162, 148 + k * 62, 44, 44);
                if (r.output.icon != null) h.DrawSprite(r.output.icon, ic, Color.white);
                h.Txt(h.R(214, 146 + k * 62, 250, 28), r.output.DisplayName, 18, r.output.color);
                h.Txt(h.R(214, 172 + k * 62, 250, 22), can ? Cfg.L("Đủ nguyên liệu", "Ready to craft") : Cfg.L("Thiếu nguyên liệu", "Missing materials"), 13, can ? Cfg.Lime : Cfg.A(Cfg.Red, 0.9f), TextAnchor.UpperLeft, false);
                if (r.outputCount > 1) h.Txt(h.R(400, 146 + k * 62, 110, 28), "x" + r.outputCount, 18, Cfg.A(Color.white, 0.85f), TextAnchor.UpperRight);
                if (BaseUi.Clicked(row)) { sel = i; Sfx.Play("click", 0.4f, 1.3f); }
            }

            // ---- chi tiết
            var cur = rs[Mathf.Clamp(sel, 0, rs.Count - 1)];
            var it = cur.output;
            Rect dp = h.R(548, 134, 590, 456);
            h.Fill(dp, new Color(0.02f, 0.03f, 0.09f, 0.7f));
            h.Outline(dp, Cfg.A(Cfg.Cyan, 0.4f), 1.5f);
            Rect big = h.R(566, 150, 72, 72);
            h.Fill(big, new Color(0.01f, 0.02f, 0.06f, 0.9f));
            h.Outline(big, Cfg.A(it.color, 0.85f), 2f);
            if (it.icon != null) h.DrawSprite(it.icon, new Rect(big.x + 5f * s, big.y + 5f * s, big.width - 10f * s, big.height - 10f * s), Color.white);
            h.Txt(h.R(654, 148, 470, 36), it.DisplayName + (cur.outputCount > 1 ? "  x" + cur.outputCount : ""), 26, it.color);
            h.TxtWrap(h.R(654, 186, 470, 40), it.KindLabel, 13, Cfg.A(Color.white, 0.7f));
            h.TxtWrap(h.R(566, 232, 556, 70), it.Description, 15, Cfg.A(Color.white, 0.92f));
            string eff = it.EffectLines();
            if (!string.IsNullOrEmpty(eff)) h.TxtWrap(h.R(566, 296, 556, 60), eff, 15, Cfg.Lime);

            h.Txt(h.R(566, 360, 300, 22), Cfg.L("CẦN CÓ", "REQUIRES"), 14, Cfg.A(Cfg.Yellow, 0.9f));
            float y = 384f;
            for (int i = 0; i < cur.ingredients.Count; i++)
            {
                var ing = cur.ingredients[i];
                if (ing == null || ing.item == null) continue;
                Row(h, y, ing.item.icon, ing.item.DisplayName, Inventory.Count(ing.item), ing.count);
                y += 30f;
            }
            if (cur.oil > 0) { Row(h, y, Art.Bank != null ? Art.Bank.oilIcon : null, Cfg.L("Dầu", "Oil"), Inventory.Oil, cur.oil); y += 30f; }
            if (cur.coins > 0) { Row(h, y, Art.Bank != null ? Art.Bank.coinIcon : null, Cfg.L("Xu", "Coins"), Inventory.Coins, cur.coins); }

            bool ok = BaseCrafting.CanCraft(cur);
            if (h.Button(h.R(900, 520, 220, 52), Cfg.L("CHẾ TÁC  [ENTER]", "CRAFT  [ENTER]"), ok ? Cfg.Lime : Cfg.A(Cfg.Red, 0.8f), 20)) Do(cur);
            if (msgT > 0f && !string.IsNullOrEmpty(msg)) h.TxtWrap(h.R(566, 526, 320, 50), msg, 15, msgOk ? Cfg.Lime : Cfg.Yellow);

            h.Txt(h.R(146, 640, 980, 20), Cfg.L("[W S / mũi tên] chọn   ·   [ENTER] chế tác   ·   [ESC / F] đóng   ·   chuột: bấm chọn", "[W S / arrows] select   ·   [ENTER] craft   ·   [ESC / F] close   ·   mouse: click to select"),
                  13, Cfg.A(Color.white, 0.6f), TextAnchor.UpperLeft, false);
        }

        static void Row(Hud h, float y, Sprite icon, string name, int have, int need)
        {
            bool enough = have >= need;
            Rect ic = h.R(566, y, 26, 26);
            if (icon != null) h.DrawSprite(icon, ic, Color.white);
            h.Txt(h.R(600, y, 360, 26), name, 16, Color.white, TextAnchor.MiddleLeft, false);
            h.Txt(h.R(760, y, 110, 26), have + " / " + need, 16, enough ? Cfg.Lime : Cfg.Red, TextAnchor.MiddleRight);
        }

        /// <summary>Ô dầu + xu ở góc trên các bảng căn cứ.</summary>
        public static void DrawWallet(Hud h, float x, float y)
        {
            Rect box = h.R(x, y, 316, 40);
            h.Fill(box, new Color(0.02f, 0.03f, 0.09f, 0.9f));
            h.Outline(box, Cfg.A(Cfg.Yellow, 0.6f), 1.5f);
            var bank = Art.Bank;
            if (bank != null && bank.oilIcon != null) h.DrawSprite(bank.oilIcon, h.R(x + 8, y + 6, 28, 28), Color.white);
            h.Txt(h.R(x + 40, y, 110, 40), Cfg.L("DẦU ", "OIL ") + Inventory.Oil, 17, new Color(1f, 0.75f, 0.3f, 1f), TextAnchor.MiddleLeft);
            if (bank != null && bank.coinIcon != null) h.DrawSprite(bank.coinIcon, h.R(x + 160, y + 6, 28, 28), Color.white);
            h.Txt(h.R(x + 192, y, 120, 40), Cfg.L("XU ", "COINS ") + Inventory.Coins, 17, Cfg.Yellow, TextAnchor.MiddleLeft);
        }
    }
}
