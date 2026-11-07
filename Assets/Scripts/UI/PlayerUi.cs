using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Mục PLAYER của menu B (phím 3).
    ///  • Bên trái: hình nhân vật, bên dưới là 8 ô trang bị (chỉ nhận vật phẩm loại TRANG BỊ — không có đồ tiêu hao).
    ///  • Bên phải: thông tin — máu, cấp bậc, cấp độ, sát thương và các chỉ số khác (đã tính cả trang bị).
    /// Phím: A / D chọn ô trang bị · ENTER hoặc X gỡ trang bị đang chọn · chuột: bấm chọn, bấm đúp để gỡ.
    /// Muốn gắn trang bị: mở mục KHO ĐỒ, chọn món TRANG BỊ rồi bấm TRANG BỊ (hoặc ENTER).
    /// </summary>
    public static class PlayerUi
    {
        /// <summary>Ô trang bị đang chọn (0..7).</summary>
        public static int EqSel;

        const float LeftX = 176f, LeftW = 444f;
        const float RightX = 650f, RightW = 454f;

        // ============================================================ phím
        public static void UpdateInput()
        {
            int sel = EqSel;
            if (GameInput.NavLeftDown) sel--;
            if (GameInput.NavRightDown) sel++;
            sel = Mathf.Clamp(sel, 0, Inventory.EquipSlots - 1);
            if (sel != EqSel) Sfx.Play("click", 0.4f, 1.3f);
            EqSel = sel;

            if (GameInput.ConfirmDown || GameInput.DiscardDown) Inventory.Unequip(EqSel);
        }

        // ============================================================ vẽ
        public static void DrawContent(Hud h)
        {
            var pl = Player.Instance;
            DrawLeft(h, pl);
            DrawRight(h, pl);

            if (Inventory.MsgT > 0f && !string.IsNullOrEmpty(Inventory.Msg))
                h.Txt(h.R(176, 622, 928, 22), Inventory.Msg, 15, Cfg.Yellow, TextAnchor.UpperLeft, false);
            h.Txt(h.R(176, 644, 940, 20),
                  Cfg.L(GameMenu.CommonHintVi + "   ·   [A D] chọn ô trang bị   ·   [ENTER / X] gỡ   ·   chuột: bấm đúp ô để gỡ",
                        GameMenu.CommonHintEn + "   ·   [A D] select slot   ·   [ENTER / X] remove   ·   mouse: double-click a slot to remove"),
                  13, Cfg.A(Color.white, 0.6f), TextAnchor.UpperLeft, false);
        }

        // ------------------------------------------------------------ hình nhân vật hiện tại (khung đầu của clip "idle")
        static Sprite PlayerSprite(Player pl)
        {
            if (pl == null) return null;
            var an = pl.anim;
            if (an != null && an.clips != null)
            {
                for (int i = 0; i < an.clips.Length; i++)
                {
                    var c = an.clips[i];
                    if (c == null || c.name != "idle" || c.frames == null || c.frames.Length == 0) continue;
                    int f = (int)(Time.unscaledTime * Mathf.Max(1f, c.fps)) % c.frames.Length;
                    return c.frames[f] != null ? c.frames[f] : c.frames[0];
                }
            }
            var sr = an != null ? an.Renderer : pl.GetComponentInChildren<SpriteRenderer>();
            return sr != null ? sr.sprite : null;
        }

        static void DrawLeft(Hud h, Player pl)
        {
            // ---- khung hình nhân vật
            Rect box = h.R(LeftX, 108, LeftW, 330);
            h.Fill(box, new Color(0.02f, 0.03f, 0.09f, 0.85f));
            h.Outline(box, Cfg.A(Cfg.Cyan, 0.45f), 1.5f);

            // vầng sáng phía sau + vạch sàn neon
            var glow = Art.Bank != null ? Art.Bank.glow : null;
            if (glow != null) h.DrawSprite(glow, h.R(LeftX + 42, 130, LeftW - 84, 290), Cfg.A(Cfg.Cyan, 0.30f));
            h.Fill(h.R(LeftX + 50, 404, LeftW - 100, 2), Cfg.A(Cfg.Cyan, 0.65f));
            h.Fill(h.R(LeftX + 90, 410, LeftW - 180, 1), Cfg.A(Cfg.Cyan, 0.30f));

            Sprite sp = PlayerSprite(pl);
            if (sp != null)
            {
                float sw = sp.rect.width, sh = sp.rect.height;
                float sc = Mathf.Min((LeftW - 90f) / sw, 270f / sh);
                if (sc >= 1f) sc = Mathf.Floor(sc);                      // giữ pixel vuông, nét
                float w = sw * sc, hgt = sh * sc;
                h.DrawSprite(sp, h.R(LeftX + (LeftW - w) * 0.5f, 404f - hgt, w, hgt), Color.white);
            }
            else h.Txt(box, Cfg.L("(không có hình)", "(no image)"), 16, Cfg.A(Color.white, 0.4f), TextAnchor.MiddleCenter, false);

            // cấp bậc (trái) + cấp độ (phải) ngay trên khung hình
            Rank rk = pl != null ? pl.power.rank : Rank.F;
            int lv = pl != null ? PowerRules.ClampLevel(pl.power.level) : 1;
            Color rc = PowerRules.RankColor(rk);
            h.Txt(h.R(LeftX + 14, 114, 180, 40), Cfg.L("BẬC ", "RANK ") + rk, 28, rc, TextAnchor.UpperLeft);
            h.Txt(h.R(LeftX + LeftW - 194, 114, 180, 40), "Lv." + lv, 28, Color.white, TextAnchor.UpperRight);

            // ---- 8 ô trang bị
            h.Txt(h.R(LeftX + 2, 446, 300, 20), Cfg.L("TRANG BỊ", "EQUIPMENT") + "  " + Inventory.EquippedCount + "/" + Inventory.EquipSlots,
                  14, Cfg.A(Cfg.Cyan, 0.9f), TextAnchor.UpperLeft);

            const float size = 50f, pitch = 56f;
            Event ev = Event.current;
            for (int i = 0; i < Inventory.EquipSlots; i++)
            {
                Rect r = h.R(LeftX + i * pitch, 468, size, size);
                ItemData it = Inventory.Equipped[i];
                bool has = it != null;
                bool sel = i == EqSel;
                bool hover = r.Contains(ev.mousePosition);
                Color border = has ? it.color : Cfg.Cyan;

                h.Fill(r, new Color(0.02f, 0.03f, 0.09f, 0.95f));
                if (sel) h.Fill(r, Cfg.A(border, 0.22f));
                else if (hover) h.Fill(r, Cfg.A(border, 0.10f));
                if (has)
                {
                    float pad = 5f * h.s;
                    Rect icon = new Rect(r.x + pad, r.y + pad, r.width - pad * 2f, r.height - pad * 2f);
                    if (it.icon != null) h.DrawSprite(it.icon, icon, Color.white);
                    else h.Txt(icon, (it.DisplayName + "?").Substring(0, 1), 22, border, TextAnchor.MiddleCenter);
                }
                else h.Txt(r, (i + 1).ToString(), 16, Cfg.A(Color.white, 0.18f), TextAnchor.MiddleCenter, false);
                h.Outline(r, sel ? Color.white : Cfg.A(border, has ? 0.8f : 0.3f), sel ? 3f : 1.5f);

                if (hover && ev.type == EventType.MouseDown && ev.button == 0)
                {
                    EqSel = i;
                    if (ev.clickCount >= 2) Inventory.Unequip(i);
                    else Sfx.Play("click", 0.4f, 1.3f);
                    ev.Use();
                }
            }

            // ---- chi tiết ô đang chọn
            Rect dp = h.R(LeftX, 526, LeftW, 92);
            h.Fill(dp, new Color(0.02f, 0.03f, 0.09f, 0.7f));
            h.Outline(dp, Cfg.A(Cfg.Cyan, 0.35f), 1.5f);
            ItemData cur = Inventory.Equipped[Mathf.Clamp(EqSel, 0, Inventory.EquipSlots - 1)];
            if (cur == null)
            {
                h.TxtWrap(h.R(LeftX + 12, 532, LeftW - 24, 80),
                          Cfg.L("Ô trống. Mở mục KHO ĐỒ [1], chọn một món TRANG BỊ rồi bấm TRANG BỊ để gắn vào đây.",
                                "Empty slot. Open the INVENTORY tab [1], pick a GEAR item and press EQUIP to place it here."),
                          14, Cfg.A(Color.white, 0.55f));
            }
            else
            {
                h.Txt(h.R(LeftX + 12, 530, LeftW - 150, 26), cur.DisplayName, 18, cur.color, TextAnchor.UpperLeft);
                h.TxtWrap(h.R(LeftX + 12, 558, LeftW - 150, 58), cur.EffectLines(), 13, Cfg.Lime);
                if (h.Button(h.R(LeftX + LeftW - 128, 540, 116, 40), Cfg.L("GỠ  [ENTER]", "REMOVE [ENTER]"), Cfg.Red, 14)) Inventory.Unequip(EqSel);
            }
        }

        // ------------------------------------------------------------ thông tin bên phải
        static void Row(Hud h, ref float y, string label, string value, Color valueColor, string bonus = null, bool bonusGood = true)
        {
            h.Txt(h.R(RightX + 16, y, 230, 30), label, 15, Cfg.A(Color.white, 0.75f), TextAnchor.MiddleLeft, false);
            float vx = RightX + 240f;
            h.Txt(h.R(vx, y, RightW - 256f, 30), value, 20, valueColor, TextAnchor.MiddleRight);
            if (!string.IsNullOrEmpty(bonus))
                h.Txt(h.R(RightX + 150, y + 24, RightW - 166f, 14), bonus, 12, bonusGood ? Cfg.Lime : Cfg.Orange, TextAnchor.MiddleRight, false);
            h.Fill(h.R(RightX + 12, y + 38, RightW - 24, 1), Cfg.A(Cfg.Cyan, 0.12f));
            y += 40f;
        }

        static string Pct(float v) { return (v >= 0f ? "+" : "") + v.ToString("0.#") + "%"; }

        static void DrawRight(Hud h, Player pl)
        {
            Rect box = h.R(RightX, 108, RightW, 512);
            h.Fill(box, new Color(0.02f, 0.03f, 0.09f, 0.7f));
            h.Outline(box, Cfg.A(Cfg.Cyan, 0.4f), 1.5f);
            h.Txt(h.R(RightX + 16, 114, RightW - 32, 30), Cfg.L("THÔNG TIN NHÂN VẬT", "CHARACTER INFO"), 20, Cfg.Cyan);
            h.Fill(h.R(RightX + 12, 146, RightW - 24, 2), Cfg.A(Cfg.Cyan, 0.5f));

            if (pl == null)
            {
                h.Txt(box, Cfg.L("Chưa có nhân vật trong Scene", "No player in the Scene"), 16, Cfg.A(Color.white, 0.5f), TextAnchor.MiddleCenter, false);
                return;
            }

            float y = 154f;

            // ---- máu: số + thanh
            h.Txt(h.R(RightX + 16, y, 120, 26), Cfg.L("MÁU", "HEALTH"), 15, Cfg.A(Color.white, 0.75f), TextAnchor.MiddleLeft, false);
            h.Txt(h.R(RightX + 140, y, RightW - 156f, 26), Mathf.CeilToInt(pl.hp) + " / " + Mathf.RoundToInt(pl.maxHp), 20, Cfg.Lime, TextAnchor.MiddleRight);
            h.NeonBar(h.R(RightX + 16, y + 28, RightW - 32f, 10), pl.maxHp > 0f ? pl.hp / pl.maxHp : 0f, Cfg.Lime, new Color(0.04f, 0.06f, 0.12f, 1f));
            float hpBonus = Inventory.Stat(ItemStat.MaxHealth);
            if (Mathf.Abs(hpBonus) > 0.01f)
                h.Txt(h.R(RightX + 150, y + 38, RightW - 166f, 14), Cfg.L("từ trang bị: ", "from gear: ") + (hpBonus >= 0f ? "+" : "") + hpBonus.ToString("0.#"), 12, Cfg.Lime, TextAnchor.MiddleRight, false);
            y += 56f;

            // ---- cấp bậc / cấp độ
            Rank rk = pl.power.rank;
            Row(h, ref y, Cfg.L("CẤP BẬC", "RANK"), rk.ToString(), PowerRules.RankColor(rk));
            Row(h, ref y, Cfg.L("CẤP ĐỘ", "LEVEL"), PowerRules.ClampLevel(pl.power.level) + " / " + PowerRules.MaxLevel, Color.white);

            // ---- sát thương & chỉ số khác (đã gồm trang bị)
            float melee = pl.meleeDamage * GameManager.DamageMul * Inventory.Mul(ItemStat.MeleeDamagePct);
            float shur = pl.shurikenDamage * GameManager.DamageMul * Inventory.Mul(ItemStat.ShurikenDamagePct);
            float mel = Inventory.Stat(ItemStat.MeleeDamagePct), shu = Inventory.Stat(ItemStat.ShurikenDamagePct);
            Row(h, ref y, Cfg.L("SÁT THƯƠNG CHÉM  [J]", "MELEE DAMAGE  [J]"), Mathf.RoundToInt(melee).ToString(), Cfg.Orange,
                Mathf.Abs(mel) > 0.01f ? Pct(mel) : null, mel > 0f);
            Row(h, ref y, Cfg.L("SÁT THƯƠNG PHI TIÊU  [K]", "SHURIKEN DAMAGE  [K]"), Mathf.RoundToInt(shur).ToString(), Cfg.Orange,
                Mathf.Abs(shu) > 0.01f ? Pct(shu) : null, shu > 0f);

            float spd = Inventory.Stat(ItemStat.MoveSpeedPct);
            Row(h, ref y, Cfg.L("TỐC ĐỘ CHẠY", "MOVE SPEED"), (pl.moveSpeed * Inventory.Mul(ItemStat.MoveSpeedPct)).ToString("0.0"), Cfg.Cyan,
                Mathf.Abs(spd) > 0.01f ? Pct(spd) : null, spd > 0f);

            float dsh = Inventory.Stat(ItemStat.DashCooldownPct);
            Row(h, ref y, Cfg.L("HỒI CHIÊU LƯỚT  [I]", "DASH COOLDOWN  [I]"), pl.DashCooldownNow.ToString("0.00") + "s", Cfg.Cyan,
                Mathf.Abs(dsh) > 0.01f ? Pct(dsh) : null, dsh < 0f);

            float heal = Inventory.Stat(ItemStat.HealPowerPct);
            Row(h, ref y, Cfg.L("HỒI MÁU MỖI LẦN  [L]", "HEAL PER USE  [L]"),
                Mathf.RoundToInt(pl.healAmount * Inventory.Mul(ItemStat.HealPowerPct)).ToString(), Cfg.Lime,
                Mathf.Abs(heal) > 0.01f ? Pct(heal) : null, heal > 0f);
            Row(h, ref y, Cfg.L("LƯỢT HỒI MÁU", "HEAL CHARGES"), pl.healCharges + " / " + pl.healMaxCharges, Cfg.Lime);

            float tak = Inventory.Stat(ItemStat.DamageTakenPct);
            Row(h, ref y, Cfg.L("SÁT THƯƠNG NHẬN VÀO", "DAMAGE TAKEN"), Mathf.RoundToInt(Inventory.Mul(ItemStat.DamageTakenPct) * 100f) + "%", Cfg.Red,
                Mathf.Abs(tak) > 0.01f ? Pct(tak) : null, tak < 0f);
        }
    }
}
