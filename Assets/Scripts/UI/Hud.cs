using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Toàn bộ giao diện (HUD, menu, tạm dừng, màn thắng / thua) vẽ bằng IMGUI
    /// => không cần Canvas, EventSystem hay asset nào. Thiết kế quy chiếu 1280x720.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        public float s = 1f;
        GUIStyle st;
        float hpShown = 1f, bossShown = 1f;

        void OnGUI()
        {
            if (Art.Pixel == null) return;
            s = Mathf.Max(0.5f, Screen.height / 720f);
            if (st == null)
            {
                st = new GUIStyle(GUI.skin.label);
                st.wordWrap = false;
                st.clipping = TextClipping.Overflow;
                st.richText = false;
            }

            var pl = Player.Instance;

            bool cinema = GameManager.State == GameState.Cinematic;
            DrawFloatTexts();
            if (!cinema)
            {
                DrawWorldIndicators();
                DrawInteractPrompt();
            }

            if (pl != null && GameManager.State != GameState.Menu && !cinema) { DrawPlayerHud(pl); DrawCoinHud(); }
            if (GameManager.State == GameState.Playing || GameManager.State == GameState.Won || GameManager.State == GameState.Lost)
            {
                DrawTopCenter();
                DrawTopRight();
                DrawBossBar();
            }
            if (GameManager.State == GameState.Playing)
            {
                SkillUi.DrawPanel(this);      // bảng chọn minibot (H): vẽ trước để nhận chuột trước nhãn trong màn chơi
                SkillUi.DrawWorld(this);      // nhãn + bấm vào minibot để thu hồi
            }
            DrawBanner();
            if (!cinema) DrawCheatLabel();
            DrawScreenFx(pl);

            switch (GameManager.State)
            {
                case GameState.Menu: DrawMenu(); break;
                case GameState.Won: DrawResult(true); break;
                case GameState.Lost: DrawResult(false); break;
                case GameState.Playing:
                    if (SkillSystem.MenuOpen) SkillUi.DrawMenu(this);
                    else if (Inventory.IsOpen) InventoryUi.Draw(this);
                    else if (GameManager.Paused) DrawPause();
                    break;
            }
            DrawCine();
        }

        /// <summary>Cinematic: thanh đen trên / dưới, thoại gõ chữ, gợi ý phím, màn đen mờ dần. Vẽ sau cùng, đè lên mọi thứ.</summary>
        void DrawCine()
        {
            const float barH = 94f;                       // 13% chiều cao màn hình chuẩn 720
            if (Cine.Bars > 0.001f)
            {
                float bh = barH * Cine.Bars * s;
                Fill(new Rect(0f, 0f, Screen.width, bh), Color.black);
                Fill(new Rect(0f, Screen.height - bh, Screen.width, bh), Color.black);
            }
            if (Cine.Active && Cine.Bars > 0.6f)
            {
                if (!string.IsNullOrEmpty(Cine.Text))
                {
                    float y = 720f - barH + 6f;
                    if (!string.IsNullOrEmpty(Cine.Speaker)) Txt(R(140, y, 1000, 22), Cine.Speaker, 17, Cfg.Cyan, TextAnchor.UpperLeft);
                    TxtWrap(R(140, y + 24f, 1000, 64), Cine.TypedText, 22, Color.white, TextAnchor.UpperLeft);
                }
                if (Cine.CanSkip)
                    Txt(R(700, 8, 560, 22), Cfg.L("[ENTER] TIẾP     [ESC] BỎ QUA", "[ENTER] NEXT     [ESC] SKIP"), 14, Cfg.A(Color.white, 0.55f), TextAnchor.UpperRight, false);
            }
            if (Cine.Fade > 0.001f) Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, Cine.Fade));
        }

        // ------------------------------------------------------------ primitives
        public Rect R(float x, float y, float w, float h) { return new Rect(x * s, y * s, w * s, h * s); }

        public void Fill(Rect r, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(r, Art.Pixel);
            GUI.color = Color.white;
        }

        public void Outline(Rect r, Color c, float t = 2f)
        {
            float k = t * s;
            Fill(new Rect(r.x, r.y, r.width, k), c);
            Fill(new Rect(r.x, r.yMax - k, r.width, k), c);
            Fill(new Rect(r.x, r.y, k, r.height), c);
            Fill(new Rect(r.xMax - k, r.y, k, r.height), c);
        }

        public void Txt(Rect r, string t, float size, Color c, TextAnchor a = TextAnchor.UpperLeft, bool bold = true, bool shadow = true)
        {
            st.fontSize = Mathf.Max(8, Mathf.RoundToInt(size * s));
            st.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            st.alignment = a;
            if (shadow)
            {
                st.normal.textColor = new Color(0f, 0f, 0f, c.a * 0.85f);
                float o = Mathf.Max(1f, size * 0.07f * s);
                GUI.Label(new Rect(r.x + o, r.y + o, r.width, r.height), t, st);
            }
            st.normal.textColor = c;
            GUI.Label(r, t, st);
        }

        public void NeonBar(Rect r, float frac, Color c, Color back)
        {
            Fill(r, back);
            frac = Mathf.Clamp01(frac);
            Rect f = new Rect(r.x, r.y, r.width * frac, r.height);
            Fill(f, Cfg.A(c, 0.9f));
            Fill(new Rect(f.x, f.y, f.width, Mathf.Max(1f, r.height * 0.28f)), new Color(1f, 1f, 1f, 0.35f));
            Outline(r, Cfg.A(c, 0.95f), 1.5f);
        }

        public bool Button(Rect r, string label, Color c, float size = 26f)
        {
            bool hover = r.Contains(Event.current.mousePosition);
            Fill(r, new Color(0.02f, 0.03f, 0.08f, 0.85f));
            Fill(r, Cfg.A(c, hover ? 0.28f : 0.12f));
            Outline(r, c, hover ? 3f : 2f);
            Txt(r, label, size, hover ? Color.white : c, TextAnchor.MiddleCenter);
            if (Event.current.type == EventType.MouseDown && hover)
            {
                Event.current.Use();
                return true;
            }
            return false;
        }

        // ------------------------------------------------------------ in-game HUD
        void DrawPlayerHud(Player pl)
        {
            hpShown = Mathf.Lerp(hpShown, pl.hp / pl.maxHp, 0.2f);
            float frac = pl.hp / pl.maxHp;
            Color hc = frac > 0.5f ? Cfg.Cyan : (frac > 0.25f ? Cfg.Yellow : Cfg.Red);

            Fill(R(14, 12, 352, 108), new Color(0.01f, 0.02f, 0.06f, 0.55f));
            Outline(R(14, 12, 352, 108), Cfg.A(Cfg.Cyan, 0.35f), 1.5f);

            Txt(R(26, 16, 200, 20), "HP", 15, Cfg.A(hc, 0.95f));
            Txt(R(26, 16, 328, 20), Mathf.CeilToInt(pl.hp) + " / " + Mathf.RoundToInt(pl.maxHp), 15, Color.white, TextAnchor.UpperRight);
            Rect bar = R(26, 38, 328, 20);
            Fill(bar, new Color(0f, 0f, 0f, 0.7f));
            Fill(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(hpShown), bar.height), Cfg.A(Cfg.Red, 0.55f));   // vệt mất máu
            NeonBar(bar, frac, hc, new Color(0f, 0f, 0f, 0f));

            string[] keys = { "J", "I", "K", "L" };
            string[] names = { Cfg.L("CHÉM", "SLASH"), Cfg.L("LƯỚT", "DASH"), Cfg.L("PHI TIÊU", "SHURIKEN"), Cfg.L("HỒI MÁU", "HEAL") };
            Color[] cols = { Cfg.Cyan, Cfg.Magenta, Cfg.Yellow, Cfg.Lime };
            float[] cds = { pl.AtkCdFrac, pl.DashCdFrac, pl.ShurCdFrac, pl.HealCdFrac };
            for (int i = 0; i < 4; i++)
            {
                float x = 26f + i * 82f;
                Rect box = R(x, 66, 70, 46);
                Fill(box, new Color(0.02f, 0.03f, 0.08f, 0.9f));
                bool ready = cds[i] <= 0.001f && !(i == 3 && pl.healCharges <= 0);
                Color c = cols[i];
                Fill(new Rect(box.x, box.y, box.width, box.height * cds[i]), new Color(0f, 0f, 0f, 0.6f));
                Outline(box, Cfg.A(c, ready ? 0.95f : 0.35f), 2f);
                Txt(new Rect(box.x + 6 * s, box.y + 1 * s, 30 * s, 30 * s), keys[i], 24, Cfg.A(c, ready ? 1f : 0.5f));
                Txt(new Rect(box.x, box.y + 30 * s, box.width, 14 * s), names[i], 10, Cfg.A(Color.white, ready ? 0.9f : 0.45f), TextAnchor.UpperCenter);
                if (i == 3)
                {
                    Txt(new Rect(box.x, box.y + 4 * s, box.width - 6 * s, 28 * s), "x" + pl.healCharges, 22,
                        pl.healCharges > 0 ? Cfg.Lime : Cfg.Red, TextAnchor.UpperRight);
                }
                if (i == 1 && pl.IsDashing) Outline(box, Color.white, 3f);
            }
        }

        void DrawTopCenter()
        {
            string msg;
            Color c = Cfg.Cyan;
            int left = Tower.Remaining;
            int total = GameManager.TowerTotal;
            if (!GameManager.BossSpawned)
            {
                if (left > 0)
                {
                    msg = Cfg.L("TRỤ TRIỆU HỒI CÒN LẠI: ", "SUMMON TOWERS LEFT: ") + left + " / " + total;
                    c = Cfg.Magenta;
                }
                else
                {
                    msg = Cfg.L("TIẾN VỀ KHU BOSS  >>>", "ADVANCE TO THE BOSS  >>>");
                    c = Cfg.Cyan;
                }
            }
            else
            {
                msg = Boss.Instance != null && Boss.Instance.phase >= 2 ? Cfg.L("TIÊU DIỆT BOSS — PHASE ", "DESTROY THE BOSS — PHASE ") + Boss.Instance.phase : Cfg.L("TIÊU DIỆT BOSS", "DESTROY THE BOSS");
                c = Cfg.Red;
            }
            Rect r = R(390, 14, 500, 30);
            Fill(r, new Color(0.01f, 0.02f, 0.06f, 0.55f));
            Outline(r, Cfg.A(c, 0.6f), 1.5f);
            Txt(r, msg, 17, c, TextAnchor.MiddleCenter);

            // mũi tên chỉ tới trụ gần nhất ngoài màn hình
            if (!GameManager.BossSpawned && Player.Instance != null && GameManager.State == GameState.Playing)
            {
                float px = Player.Instance.Position.x;
                Vector2 target = Vector2.zero;
                bool has = false;
                float best = 1e9f;
                for (int i = 0; i < Tower.Towers.Count; i++)
                {
                    var t = Tower.Towers[i];
                    if (t == null || t.dead) continue;
                    float d = Mathf.Abs(t.transform.position.x - px);
                    if (d < best) { best = d; target = t.transform.position; has = true; }
                }
                if (!has && left == 0) { target = new Vector2(Cfg.BossTriggerX, 0f); has = true; best = Mathf.Abs(Cfg.BossTriggerX - px); }
                if (has && best > 12f)
                {
                    bool right = target.x > px;
                    string a = right ? Cfg.L("MỤC TIÊU  ", "TARGET  ") + Mathf.RoundToInt(best) + "m  >>" : "<<  " + Mathf.RoundToInt(best) + "m  " + Cfg.L("MỤC TIÊU", "TARGET");
                    float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 6f);
                    Txt(R(390, 50, 500, 24), a, 15, Cfg.A(Cfg.Yellow, pulse), TextAnchor.MiddleCenter);
                }
            }
        }

        void DrawTopRight()
        {
            int sec = Mathf.FloorToInt(GameManager.PlayTime);
            string t = (sec / 60).ToString("00") + ":" + (sec % 60).ToString("00");
            Rect r = R(1100, 12, 166, 62);
            Fill(r, new Color(0.01f, 0.02f, 0.06f, 0.55f));
            Outline(r, Cfg.A(Cfg.Cyan, 0.35f), 1.5f);
            Txt(R(1112, 16, 142, 24), Cfg.L("THỜI GIAN", "TIME") + "  " + t, 15, Color.white);
            Txt(R(1112, 42, 142, 24), Cfg.L("HẠ GỤC", "KILLS") + "  " + GameManager.Kills, 15, Cfg.Yellow);
        }

        void DrawBossBar()
        {
            var b = Boss.Instance;
            if (b == null || b.Intro) return;
            if (GameManager.State == GameState.Lost && b.dead) return;
            bossShown = Mathf.Lerp(bossShown, b.TotalFrac, 0.15f);
            bool p2 = b.phase >= 2;
            Color c = b.phase == 3 ? Cfg.Orange : (b.phase == 2 ? Cfg.Red : Cfg.Magenta);

            float w = 760f;
            float x = (1280f - w) * 0.5f;
            Rect bar = R(x, 676, w, 20);
            Txt(R(x, 646, w, 26), "OVERLORD — PHASE " + b.phase, 18, c, TextAnchor.MiddleLeft);
            Fill(bar, new Color(0f, 0f, 0f, 0.75f));
            Fill(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(bossShown), bar.height), Cfg.A(Color.white, 0.5f));
            NeonBar(bar, b.TotalFrac, c, new Color(0f, 0f, 0f, 0f));
            float mx = bar.x + bar.width * b.PhaseMarker;
            Fill(new Rect(mx - 1.5f * s, bar.y - 5f * s, 3f * s, bar.height + 10f * s), Cfg.A(Color.white, 0.9f));
            if (b.PhaseMarker2 > 0f)
            {
                float mx2 = bar.x + bar.width * b.PhaseMarker2;
                Fill(new Rect(mx2 - 1.5f * s, bar.y - 5f * s, 3f * s, bar.height + 10f * s), Cfg.A(Color.white, 0.9f));
            }
            if (p2) Txt(R(x, 646, w, 26), Cfg.L("HÚT MÁU", "LIFESTEAL"), 14, Cfg.Lime, TextAnchor.MiddleRight);
        }

        float coinShown;

        /// <summary>Vẽ một Sprite (ô vuông tỉ lệ bất kỳ) vào Rect, giữ nguyên pixel (Point filter của ảnh).</summary>
        public void DrawSprite(Sprite sp, Rect r, Color tint)
        {
            if (sp == null || sp.texture == null) return;
            Rect tr = sp.textureRect;
            Texture2D tex = sp.texture;
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(r, tex, new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height));
            GUI.color = Color.white;
        }

        /// <summary>Như Txt nhưng tự xuống dòng trong Rect.</summary>
        public void TxtWrap(Rect r, string t, float size, Color c, TextAnchor a = TextAnchor.UpperLeft)
        {
            st.wordWrap = true;
            Txt(r, t, size, c, a, false, true);
            st.wordWrap = false;
        }

        /// <summary>Ô tiền tệ ngay dưới thanh máu, cùng kiểu với bảng HP.</summary>
        void DrawCoinHud()
        {
            float target = Inventory.Coins;
            coinShown = Mathf.MoveTowards(coinShown, target, Mathf.Max(1f, Mathf.Abs(target - coinShown) * 0.15f));
            Rect box = R(14, 124, 352, 34);
            Fill(box, new Color(0.01f, 0.02f, 0.06f, 0.55f));
            Outline(box, Cfg.A(Cfg.Yellow, 0.4f), 1.5f);
            Rect ic = R(24, 129, 24, 24);
            var spr = Art.Bank != null ? Art.Bank.coinIcon : null;
            if (spr != null) DrawSprite(spr, ic, Color.white);
            else { Fill(ic, Cfg.A(Cfg.Yellow, 0.9f)); Fill(new Rect(ic.x + 6f * s, ic.y + 6f * s, 12f * s, 12f * s), new Color(0.5f, 0.35f, 0f, 1f)); }
            Txt(R(56, 127, 70, 28), Cfg.L("XU", "COINS"), 15, Cfg.A(Cfg.Yellow, 0.95f), TextAnchor.MiddleLeft);
            Txt(R(110, 127, 120, 28), Mathf.RoundToInt(coinShown).ToString(), 22, Color.white, TextAnchor.MiddleRight);
            Txt(R(236, 127, 122, 28), Cfg.L("DẦU ", "OIL ") + Inventory.Oil, 16, new Color(0.95f, 0.75f, 0.25f, 1f), TextAnchor.MiddleRight);

            // dòng phím tắt Skill + minibot + linh kiện
            Rect sk = R(14, 160, 352, 24);
            Fill(sk, new Color(0.01f, 0.02f, 0.06f, 0.55f));
            Outline(sk, Cfg.A(Cfg.Cyan, 0.3f), 1.5f);
            Txt(R(22, 160, 200, 24), "[N] SKILL   [H] MINIBOT " + SkillSystem.SummonedCount + "/" + SkillSystem.MaxSummoned, 12, Cfg.A(Cfg.Cyan, 0.9f), TextAnchor.MiddleLeft, true);
            Txt(R(222, 160, 138, 24), Cfg.L("Linh kiện ", "Parts ") + SkillSystem.PartsTotal, 12, Cfg.A(Color.white, 0.8f), TextAnchor.MiddleRight, false);

            // buff tạm đang chạy
            for (int i = 0; i < Inventory.TimedCount; i++)
            {
                var it = Inventory.TimedItem(i);
                if (it == null) continue;
                Txt(R(16, 190 + i * 18, 340, 18), it.DisplayName + "  " + Mathf.CeilToInt(Inventory.TimedLeft(i)) + "s", 13, Cfg.A(it.color, 0.95f), TextAnchor.UpperLeft, true);
            }
        }

        /// <summary>Nhãn gợi ý "[F] ..." bay trên vật tương tác gần nhất (rương, cửa, NPC, máy...).</summary>
        void DrawInteractPrompt()
        {
            if (GameManager.State != GameState.Playing || GameManager.Paused || GameManager.PlayerLocked || Cine.Active || SkillSystem.PanelOpen) return;
            var it = Interactor.Nearest();
            if (it == null) return;
            Vector2 sp = PixelScreen.WorldToGui(it.PromptPos);
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f);
            string t = it.PromptText;
            float w = Mathf.Max(190f, 24f + t.Length * 10.5f);
            Rect r = new Rect(sp.x - w * 0.5f * s, sp.y - 14f * s, w * s, 28f * s);
            Fill(r, new Color(0.01f, 0.02f, 0.06f, 0.78f));
            Outline(r, Cfg.A(Cfg.Yellow, pulse), 1.5f);
            Txt(r, t, 16, Cfg.A(Cfg.Yellow, pulse), TextAnchor.MiddleCenter);
        }

        /// <summary>Nhãn nhỏ góc trái dưới HUD khi đang bật chế độ thử nghiệm (phím P).</summary>
        void DrawCheatLabel()
        {
            if (!GameManager.Cheat) return;
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 5f);
            Txt(R(560, 80, 706, 22), Cfg.L("[P] CHẾ ĐỘ THỬ NGHIỆM: BẤT TỬ · SÁT THƯƠNG x" + GameManager.DamageMul + " · KHÔNG HỒI CHIÊU",
                                           "[P] TEST MODE: INVINCIBLE · x" + GameManager.DamageMul + " DAMAGE · NO COOLDOWNS"), 14, Cfg.A(Cfg.Yellow, pulse), TextAnchor.UpperRight);
        }

        void DrawBanner()
        {
            if (GameManager.BannerT <= 0f) return;
            float t = GameManager.BannerT;
            float m = GameManager.BannerMax;
            float a = Mathf.Clamp01(Mathf.Min(t / 0.5f, (m - t) / 0.18f));
            float pop = 1f + 0.25f * Mathf.Clamp01(1f - (m - t) / 0.25f);
            Color c = GameManager.BannerColor;
            Rect strip = R(0, 150, 1280, 70);
            Fill(strip, new Color(0f, 0f, 0f, 0.45f * a));
            Fill(new Rect(strip.x, strip.y, strip.width, 2f * s), Cfg.A(c, 0.85f * a));
            Fill(new Rect(strip.x, strip.yMax - 2f * s, strip.width, 2f * s), Cfg.A(c, 0.85f * a));
            Txt(R(0, 150, 1280, 70), GameManager.BannerText, 34 * pop, Cfg.A(c, a), TextAnchor.MiddleCenter);
        }

        void DrawFloatTexts()
        {
            var cam = Camera.main;
            if (cam == null) return;
            for (int i = 0; i < Fx.Texts.Count; i++)
            {
                var ft = Fx.Texts[i];
                Vector2 sp = PixelScreen.WorldToGui(new Vector3(ft.pos.x, ft.pos.y, 0f));
                float k = Mathf.Clamp01(ft.life / ft.maxLife);
                float a = Mathf.Clamp01(k * 2.2f);
                float scale = ft.size * (1f + 0.5f * Mathf.Clamp01((k - 0.8f) / 0.2f));
                Rect r = new Rect(sp.x - 100f * s, sp.y - 16f * s, 200f * s, 32f * s);
                Txt(r, ft.text, 22f * scale, Cfg.A(ft.color, a), TextAnchor.MiddleCenter);
            }
        }

        /// <summary>Thanh máu nhỏ của người chơi dưới chân / cảnh báo đòn đánh được xử lý trong thế giới; ở đây chỉ vẽ nhãn lượt hồi khi đang hồi.</summary>
        void DrawWorldIndicators()
        {
            var pl = Player.Instance;
            var cam = Camera.main;
            if (pl == null || cam == null || pl.Dead || GameManager.State != GameState.Playing) return;
            if (!pl.IsHealing) return;
            Vector2 sp = PixelScreen.WorldToGui(pl.Center + new Vector2(0f, 1.6f));
            Rect r = new Rect(sp.x - 50f * s, sp.y - 6f * s, 100f * s, 8f * s);
            Fill(r, new Color(0f, 0f, 0f, 0.7f));
            Fill(new Rect(r.x, r.y, r.width * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 12f)), r.height), Cfg.A(Cfg.Lime, 0.9f));
            Outline(r, Cfg.Lime, 1f);
        }

        void DrawScreenFx(Player pl)
        {
            if (Fx.FlashAlpha > 0f)
                Fill(new Rect(0, 0, Screen.width, Screen.height), Cfg.A(Fx.FlashColor, Fx.FlashAlpha * 0.5f));

            if (pl != null && !pl.Dead && GameManager.State == GameState.Playing)
            {
                float f = pl.hp / pl.maxHp;
                if (f < 0.3f)
                {
                    float a = (0.3f - f) / 0.3f * (0.25f + 0.15f * Mathf.Sin(Time.unscaledTime * 6f));
                    float t = 70f * s;
                    Color c = Cfg.A(Cfg.Red, a);
                    Fill(new Rect(0, 0, Screen.width, t), c);
                    Fill(new Rect(0, Screen.height - t, Screen.width, t), c);
                    Fill(new Rect(0, 0, t, Screen.height), c);
                    Fill(new Rect(Screen.width - t, 0, t, Screen.height), c);
                }
            }
        }

        // ------------------------------------------------------------ screens
        public void Dim(float a) { Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0.01f, 0.01f, 0.05f, a)); }

        void DrawMenu()
        {
            Dim(0.6f);
            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 3f);
            Txt(R(3, 88, 1280, 110), "NEON BREAKER", 96, Cfg.A(Cfg.Magenta, 0.9f), TextAnchor.MiddleCenter);
            Txt(R(0, 84, 1280, 110), "NEON BREAKER", 96, Cfg.A(Cfg.Cyan, pulse), TextAnchor.MiddleCenter);
            Txt(R(0, 190, 1280, 30), Cfg.L("PHÁ TRỤ  •  HIT & RUN  •  HẠ BOSS", "BREAK TOWERS  •  HIT & RUN  •  SLAY THE BOSS"), 20, Cfg.A(Color.white, 0.85f), TextAnchor.MiddleCenter);

            Rect panel = R(380, 222, 520, 388);
            Fill(panel, new Color(0.01f, 0.02f, 0.06f, 0.7f));
            Outline(panel, Cfg.A(Cfg.Cyan, 0.6f), 2f);
            string[] k = { "A / D", "SPACE", "J", "K", "I", "L", "F", "B", "N", "M", "H" };
            string[] d =
            {
                Cfg.L("Di chuyển", "Move"),
                Cfg.L("Nhảy (giữ = cao hơn, S + Space = xuyên bục)", "Jump (hold = higher, S + Space = drop)"),
                Cfg.L("Đánh gần", "Melee slash"),
                Cfg.L("Đánh xa (ném phi tiêu)", "Ranged (throw shuriken)"),
                Cfg.L("Lướt — bất tử trong lúc lướt", "Dash — invulnerable while dashing"),
                Cfg.L("Hồi máu (giới hạn lượt, nhặt thêm từ quái)", "Heal (limited, pick up more)"),
                Cfg.L("Tương tác: mở rương, cửa, nói chuyện, dùng máy", "Interact: chests, doors, talk, use machines"),
                Cfg.L("Mở / đóng kho đồ", "Open / close inventory"),
                Cfg.L("Mục Skill: cây skill, chế tạo, lưu trữ minibot", "Skill menu: skill tree, crafting, minibot storage"),
                Cfg.L("Bản đồ", "Map"),
                Cfg.L("Bảng chọn minibot (A D chọn, S triệu hồi / thu hồi)", "Minibot panel (A D select, S summon / recall)")
            };
            Color[] cs = { Cfg.White, Cfg.White, Cfg.Cyan, Cfg.Yellow, Cfg.Magenta, Cfg.Lime, Cfg.Yellow, Cfg.Yellow, Cfg.Magenta, Cfg.Cyan, Cfg.Cyan };
            for (int i = 0; i < k.Length; i++)
            {
                float y = 230f + i * 32f;
                Txt(R(400, y, 110, 30), k[i], 20, cs[i], TextAnchor.MiddleLeft);
                Txt(R(520, y, 370, 30), d[i], 15, Cfg.A(Color.white, 0.9f), TextAnchor.MiddleLeft, false);
            }

            if (Button(R(490, 624, 300, 52), Cfg.L("BẮT ĐẦU  [ENTER]", "START  [ENTER]"), Cfg.Cyan, 26)) GameManager.Inst.StartGame();
            Txt(R(0, 686, 1280, 24), Cfg.L("ESC = tạm dừng     P = chế độ thử nghiệm     V = đổi ngôn ngữ (VI/EN)", "ESC = pause     P = test mode     V = switch language (VI/EN)"), 14, Cfg.A(Color.white, 0.6f), TextAnchor.MiddleCenter, false);
        }

        void DrawPause()
        {
            Dim(0.55f);
            Txt(R(0, 230, 1280, 90), Cfg.L("TẠM DỪNG", "PAUSED"), 64, Cfg.Cyan, TextAnchor.MiddleCenter);
            if (Button(R(490, 350, 300, 54), Cfg.L("TIẾP TỤC  [ESC]", "RESUME  [ESC]"), Cfg.Cyan)) GameManager.Inst.TogglePause();
            Txt(R(0, 430, 1280, 24), Cfg.L("V = đổi ngôn ngữ", "V = switch language"), 14, Cfg.A(Color.white, 0.6f), TextAnchor.MiddleCenter, false);
        }

        void DrawResult(bool win)
        {
            Dim(0.7f);
            Color c = win ? Cfg.Cyan : Cfg.Red;
            string title = win ? Cfg.L("CHIẾN THẮNG", "VICTORY") : Cfg.L("THẤT BẠI", "DEFEAT");
            string sub = win ? Cfg.L("Bạn đã hạ gục Overlord và phá hủy hệ thống triệu hồi!", "You destroyed the Overlord and shut down the summoning grid!")
                             : Cfg.L("Bạn đã gục ngã... hãy thử lại và né đòn tốt hơn.", "You have fallen... try again and dodge better.");
            Txt(R(4, 94, 1280, 120), title, 100, Cfg.A(win ? Cfg.Magenta : Cfg.Orange, 0.9f), TextAnchor.MiddleCenter);
            Txt(R(0, 90, 1280, 120), title, 100, c, TextAnchor.MiddleCenter);
            Txt(R(0, 216, 1280, 30), sub, 19, Cfg.A(Color.white, 0.9f), TextAnchor.MiddleCenter, false);

            int sec = Mathf.FloorToInt(GameManager.PlayTime);
            Rect panel = R(430, 268, 420, 236);
            Fill(panel, new Color(0.01f, 0.02f, 0.06f, 0.75f));
            Outline(panel, Cfg.A(c, 0.7f), 2f);
            string[] lab =
            {
                Cfg.L("Thời gian", "Time"),
                Cfg.L("Quái hạ gục", "Enemies slain"),
                Cfg.L("Trụ phá hủy", "Towers destroyed"),
                Cfg.L("Sát thương nhận", "Damage taken"),
                Cfg.L("Lượt hồi đã dùng", "Heals used"),
                Cfg.L("Xu hiện có", "Coins")
            };
            string[] val =
            {
                (sec / 60).ToString("00") + ":" + (sec % 60).ToString("00"),
                GameManager.Kills.ToString(),
                GameManager.TowersDestroyed + " / " + GameManager.TowerTotal,
                Mathf.RoundToInt(GameManager.DamageTaken).ToString(),
                GameManager.HealsUsed.ToString(),
                Inventory.Coins.ToString()
            };
            for (int i = 0; i < lab.Length; i++)
            {
                float y = 282f + i * 36f;
                Txt(R(452, y, 220, 30), lab[i], 18, Cfg.A(Color.white, 0.85f), TextAnchor.MiddleLeft, false);
                Txt(R(640, y, 190, 30), val[i], 20, c, TextAnchor.MiddleRight);
            }

            if (Button(R(490, 524, 300, 58), Cfg.L("CHƠI LẠI  [R / ENTER]", "PLAY AGAIN  [R / ENTER]"), c, 24)) GameManager.Inst.Restart();
        }
    }
}
