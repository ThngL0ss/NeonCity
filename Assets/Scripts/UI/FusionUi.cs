using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Máy ghép linh kiện ở căn cứ: 3 ô hình tam giác chứa 3 linh kiện cùng bậc, 1 ô ở giữa chứa thành phẩm (bậc kế tiếp).
    /// A / D chọn bậc, ENTER = ghép. Cần dầu + xu theo BaseConfig ▸ Fusion. Có xác suất may mắn nhận thêm linh kiện thành phẩm.
    /// Chỉ ghép được F->D, D->C, C->B (không ghép lên A và S).
    /// </summary>
    public static class FusionUi
    {
        static int selIdx;
        static string msg = "";
        static bool msgOk;
        static float msgT;
        // hoạt ảnh sau khi ghép
        static float animT = -1f;
        static Rank animFrom, animTo;
        static int animSlots;
        static bool animLucky;
        static int animMade;
        const float AnimLen = 1.7f;

        static List<Rank> Ranks()
        {
            var l = new List<Rank>();
            var cfg = BaseConfig.Cur;
            for (int i = 0; i < cfg.fusion.Count; i++)
                if (cfg.fusion[i] != null && PartFusion.CanFuseRank(cfg.fusion[i].from) && !l.Contains(cfg.fusion[i].from)) l.Add(cfg.fusion[i].from);
            return l;
        }

        public static void OnOpen()
        {
            animT = -1f;
            msg = ""; msgT = 0f;
            var rs = Ranks();
            selIdx = 0;
            for (int i = 0; i < rs.Count; i++)
            {
                var rule = PartFusion.Rule(rs[i]);
                if (rule != null && SkillSystem.Parts(rs[i]) >= rule.partsNeeded) { selIdx = i; break; }
            }
        }

        public static void UpdateInput()
        {
            float dt = Time.unscaledDeltaTime;
            if (msgT > 0f) msgT -= dt;
            if (animT >= 0f)
            {
                animT += dt;
                if (animT > AnimLen) animT = -1f;
                if (GameInput.PauseDown) { animT = -1f; BaseUi.Close(); }
                return;
            }
            if (GameInput.PauseDown || GameInput.InteractDown) { BaseUi.Close(); return; }
            var rs = Ranks();
            if (rs.Count == 0) return;
            int s = selIdx;
            if (GameInput.NavLeftDown) s--;
            if (GameInput.NavRightDown) s++;
            s = Mathf.Clamp(s, 0, rs.Count - 1);
            if (s != selIdx) Sfx.Play("click", 0.4f, 1.3f);
            selIdx = s;
            if (GameInput.ConfirmDown) Do(rs[selIdx]);
        }

        static void Do(Rank r)
        {
            var rule = PartFusion.Rule(r);
            int slots = rule != null ? rule.partsNeeded : 3;
            var res = PartFusion.Fuse(r);
            msg = res.message;
            msgOk = res.ok;
            msgT = 4f;
            if (!res.ok) { Sfx.Play("click", 0.7f, 0.6f); return; }
            animT = 0f;
            animFrom = r;
            animTo = res.to;
            animSlots = slots;
            animLucky = res.lucky;
            animMade = res.made;
            Sfx.Play("charge", 0.8f, 0.9f);
        }

        static Vector2 P(Hud h, float x, float y) { return new Vector2(x * h.s, y * h.s); }

        static void Slot(Hud h, Vector2 c, float size, Color border, Sprite icon, Color tint, bool filled, string label)
        {
            Rect r = new Rect(c.x - size * h.s * 0.5f, c.y - size * h.s * 0.5f, size * h.s, size * h.s);
            h.Fill(r, new Color(0.01f, 0.02f, 0.06f, 0.95f));
            h.Fill(r, Cfg.A(border, filled ? 0.18f : 0.06f));
            h.Outline(r, Cfg.A(border, filled ? 0.95f : 0.45f), filled ? 3f : 2f);
            if (icon != null && filled)
                h.DrawSprite(icon, new Rect(r.x + 10f * h.s, r.y + 10f * h.s, r.width - 20f * h.s, r.height - 20f * h.s), tint);
            if (!string.IsNullOrEmpty(label)) h.Txt(new Rect(r.x, r.yMax - 22f * h.s, r.width, 22f * h.s), label, 14, Cfg.A(border, 0.9f), TextAnchor.MiddleCenter);
        }

        public static void Draw(Hud h)
        {
            float s = h.s;
            h.Dim(0.76f);
            Rect panel = h.R(170, 40, 940, 640);
            h.Fill(panel, new Color(0.01f, 0.02f, 0.06f, 0.96f));
            h.Outline(panel, Cfg.A(Cfg.Magenta, 0.8f), 2f);
            h.Txt(h.R(196, 48, 560, 44), Cfg.L("MÁY GHÉP LINH KIỆN", "PART FUSION MACHINE"), 34, Cfg.Magenta);
            h.Txt(h.R(198, 94, 600, 20), Cfg.L("Ghép 3 linh kiện cùng bậc thành 1 linh kiện bậc cao hơn (không ghép lên bậc A và S).", "Fuse 3 parts of one rank into 1 part of the next rank (A and S cannot be fused up)."),
                  13, Cfg.A(Color.white, 0.65f), TextAnchor.UpperLeft, false);
            CraftUi.DrawWallet(h, 770, 54);

            var rs = Ranks();
            if (rs.Count == 0)
            {
                h.Txt(h.R(170, 300, 940, 60), Cfg.L("Chưa có luật ghép (BaseConfig ▸ Fusion).", "No fusion rules yet (BaseConfig ▸ Fusion)."), 18, Cfg.A(Color.white, 0.6f), TextAnchor.MiddleCenter, false);
                return;
            }
            selIdx = Mathf.Clamp(selIdx, 0, rs.Count - 1);
            Rank from = animT >= 0f ? animFrom : rs[selIdx];
            Rank to;
            PowerRules.FusionTarget(from, out to);
            var rule = PartFusion.Rule(from);
            int need = rule != null ? rule.partsNeeded : 3;
            bool busy = animT >= 0f;

            // ---- chọn bậc
            for (int i = 0; i < rs.Count; i++)
            {
                Rank r = rs[i];
                Rank rt;
                PowerRules.FusionTarget(r, out rt);
                Rect tab = h.R(256 + i * 192, 126, 180, 56);
                Color rc = PowerRules.RankColor(r);
                bool on = i == selIdx;
                h.Fill(tab, new Color(0.01f, 0.02f, 0.06f, 0.9f));
                if (on) h.Fill(tab, Cfg.A(rc, 0.2f));
                h.Outline(tab, on ? rc : Cfg.A(rc, 0.4f), on ? 3f : 1.5f);
                h.Txt(h.R(256 + i * 192, 128, 180, 30), r + "  >  " + rt, 20, rc, TextAnchor.MiddleCenter);
                h.Txt(h.R(256 + i * 192, 156, 180, 24), Cfg.L("đang có: ", "owned: ") + SkillSystem.Parts(r), 14, Cfg.A(Color.white, 0.85f), TextAnchor.MiddleCenter, false);
                if (!busy && BaseUi.Clicked(tab)) { selIdx = i; Sfx.Play("click", 0.4f, 1.3f); }
            }

            // ---- tam giác 3 ô + ô thành phẩm ở giữa
            Vector2 cTop = P(h, 640, 250), cL = P(h, 490, 440), cR = P(h, 790, 440), cMid = P(h, 640, 372);
            Color fc = PowerRules.RankColor(from);
            Color tc = PowerRules.RankColor(to);
            Sprite pi = Art.Bank != null ? Art.Bank.partIcon : null;
            int have = SkillSystem.Parts(from);
            float t = busy ? animT : -1f;
            float fly = busy ? Mathf.Clamp01(t / 0.8f) : 0f;       // 0..1 linh kiện bay vào giữa
            float e = fly * fly * (3f - 2f * fly);

            Vector2[] slotPos = { cTop, cL, cR };
            for (int i = 0; i < 3; i++)
                BaseUi.Line(h, slotPos[i], cMid, Cfg.A(Color.Lerp(fc, tc, 0.5f), busy ? 0.9f : 0.35f), 3f * s);
            BaseUi.Line(h, cTop, cL, Cfg.A(fc, 0.25f), 2f * s);
            BaseUi.Line(h, cL, cR, Cfg.A(fc, 0.25f), 2f * s);
            BaseUi.Line(h, cR, cTop, Cfg.A(fc, 0.25f), 2f * s);

            for (int i = 0; i < 3; i++)
            {
                bool filled = busy ? i < animSlots : (i < need && i < have);
                Vector2 p = slotPos[i];
                if (busy) Slot(h, p, 96f, fc, pi, fc, false, null);       // ô trống trong lúc bay
                else Slot(h, p, 96f, fc, pi, fc, filled, from.ToString());
                if (busy && filled && fly < 1f)
                {
                    Vector2 q = Vector2.Lerp(p, cMid, e);
                    float sz = Mathf.Lerp(96f, 40f, e) * s;
                    if (pi != null) h.DrawSprite(pi, new Rect(q.x - sz * 0.5f, q.y - sz * 0.5f, sz, sz), fc);
                }
            }

            // ô thành phẩm
            if (busy)
            {
                float pop = t > 0.8f ? Mathf.Clamp01((t - 0.8f) / 0.25f) : 0f;
                float sc = 1f + 0.35f * Mathf.Sin(pop * Mathf.PI);
                Slot(h, cMid, 124f * sc, tc, pi, tc, t > 0.8f, t > 0.8f ? to.ToString() : null);
                if (t > 0.75f && t < 1.05f) h.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(1f, 1f, 1f, 0.55f * (1f - Mathf.Abs(t - 0.9f) / 0.15f)));
                if (t > 0.9f && animLucky)
                {
                    float u = Mathf.Clamp01((t - 0.9f) / 0.6f);
                    for (int k = 0; k < Mathf.Max(0, animMade - 1); k++)
                    {
                        Vector2 q = cMid + new Vector2((k == 0 ? -1f : 1f) * 150f * u * s, -60f * Mathf.Sin(u * Mathf.PI) * s);
                        float sz = 52f * s;
                        if (pi != null) h.DrawSprite(pi, new Rect(q.x - sz * 0.5f, q.y - sz * 0.5f, sz, sz), tc);
                    }
                    h.Txt(h.R(340, 296, 600, 40), Cfg.L("MAY MẮN!  +" + (animMade - 1) + " linh kiện", "LUCKY!  +" + (animMade - 1) + " parts"), 28, Cfg.Yellow, TextAnchor.MiddleCenter);
                }
            }
            else
            {
                Slot(h, cMid, 124f, tc, pi, Cfg.A(tc, 0.35f), true, to.ToString());
                h.Txt(new Rect(cMid.x - 60f * s, cMid.y - 12f * s, 120f * s, 24f * s), "?", 26, Cfg.A(tc, 0.7f), TextAnchor.MiddleCenter);
            }

            // ---- chi phí + nút ghép
            int pa = SkillSystem.Parts(from);
            bool enoughParts = rule != null && pa >= need;
            bool enoughOil = rule != null && Inventory.Oil >= rule.oil;
            bool enoughCoins = rule != null && Inventory.Coins >= rule.coins;
            if (rule != null)
            {
                float y = 556f;
                CostChip(h, 232, y, 230, Cfg.L("LINH KIỆN " + from, "RANK " + from + " PARTS"), pa, need, fc, enoughParts);
                CostChip(h, 490, y, 230, Cfg.L("DẦU", "OIL"), Inventory.Oil, rule.oil, new Color(1f, 0.75f, 0.3f, 1f), enoughOil);
                CostChip(h, 748, y, 230, Cfg.L("XU", "COINS"), Inventory.Coins, rule.coins, Cfg.Yellow, enoughCoins);
                float luck = Mathf.RoundToInt(BaseConfig.Cur.luckChance * 100f);
                h.Txt(h.R(232, 520, 760, 22), Cfg.L("Cơ hội may mắn " + luck + "%: nhận thêm " + BaseConfig.Cur.luckBonusParts + " linh kiện thành phẩm", "Lucky chance " + luck + "%: get " + BaseConfig.Cur.luckBonusParts + " extra parts"),
                      14, Cfg.A(Cfg.Yellow, 0.85f), TextAnchor.UpperCenter, false);
            }
            bool can = rule != null && PartFusion.CanFuse(from) && !busy;
            if (h.Button(h.R(520, 614, 240, 50), Cfg.L("GHÉP  [ENTER]", "FUSE  [ENTER]"), can ? Cfg.Magenta : Cfg.A(Cfg.Red, 0.7f), 22) && !busy) Do(from);
            if (msgT > 0f && !string.IsNullOrEmpty(msg) && !(busy && t < 1.0f))
                h.Txt(h.R(200, 584, 880, 26), msg, 16, msgOk ? Cfg.Lime : Cfg.Yellow, TextAnchor.MiddleCenter);
            h.Txt(h.R(196, 654, 880, 20), Cfg.L("[A D / chuột] chọn bậc   ·   [ENTER] ghép   ·   [ESC / F] đóng", "[A D / mouse] choose rank   ·   [ENTER] fuse   ·   [ESC / F] close"),
                  13, Cfg.A(Color.white, 0.6f), TextAnchor.UpperLeft, false);
        }

        static void CostChip(Hud h, float x, float y, float w, string label, int have, int need, Color c, bool ok)
        {
            Rect r = h.R(x, y, w, 56);
            h.Fill(r, new Color(0.02f, 0.03f, 0.09f, 0.85f));
            h.Outline(r, Cfg.A(ok ? c : Cfg.Red, 0.7f), 1.5f);
            h.Txt(h.R(x, y + 2, w, 22), label, 13, Cfg.A(c, 0.95f), TextAnchor.MiddleCenter);
            h.Txt(h.R(x, y + 24, w, 30), have + " / " + need, 22, ok ? Cfg.Lime : Cfg.Red, TextAnchor.MiddleCenter);
        }
    }
}
