using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Menu chính (Scene MainMenu): tiêu đề giữa phía trên, TRÒ CHƠI MỚI · TIẾP TỤC · CÀI ĐẶT · THOÁT.
    /// • Trò chơi mới: lấy ô lưu trống đầu tiên (tối đa 10 ô) rồi vào màn hướng dẫn. Đầy 10 ô thì báo, phải xoá bớt một ô.
    /// • Tiếp tục: danh sách 10 ô lưu (cấp bậc, cấp độ, nơi lưu, thời gian chơi, lần lưu cuối). Bấm vào ô để nạp, nút XÓA (hoặc phím X) để xoá ô.
    /// • Cài đặt: ngôn ngữ, âm lượng nhạc, âm lượng hiệu ứng, toàn màn hình (nhớ cho lần sau).
    /// Phím: W S chọn · ENTER xác nhận · ESC quay lại · A D chỉnh giá trị (Cài đặt) · X xoá ô (Tiếp tục). Chuột dùng được mọi chỗ.
    /// Hud vẽ khi GameManager ở trạng thái Menu và MainMenuUi.Active = true (MainMenuScreen bật).
    /// </summary>
    public static class MainMenuUi
    {
        enum Page { Main, Load, Settings }

        public static bool Active;

        static Page page;
        static int sel;
        static int delSlot = -1;
        static string notice = "";
        static float noticeT;
        static SaveData[] slots = new SaveData[SaveManager.MaxSlots];
        static bool anySave;
        static Vector2 lastMouse;
        static bool mouseMoved;
        static float t;

        public static void ResetStatics()
        {
            Active = false;
            page = Page.Main;
            sel = 0;
            delSlot = -1;
            notice = "";
            noticeT = 0f;
        }

        public static void Open()
        {
            Active = true;
            page = Page.Main;
            delSlot = -1;
            noticeT = 0f;
            Refresh();
            sel = anySave ? 1 : 0;
        }

        public static void Close() { Active = false; }

        static void Refresh()
        {
            slots = SaveManager.ReadAll();
            anySave = false;
            for (int i = 0; i < slots.Length; i++) if (slots[i] != null) anySave = true;
        }

        static int LatestSlot()
        {
            int best = 0;
            string bt = "";
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;
                if (string.CompareOrdinal(slots[i].savedAt ?? "", bt) >= 0) { bt = slots[i].savedAt ?? ""; best = i; }
            }
            return best;
        }

        static void Notify(string vi, string en)
        {
            notice = Cfg.L(vi, en);
            noticeT = 4f;
            Sfx.Play("click", 0.7f, 0.6f);
        }

        // ------------------------------------------------------------ hành động
        static void DoNewGame()
        {
            if (SaveManager.FirstFree() < 0)
            {
                Notify("Cả 10 ô lưu đã đầy. Vào TIẾP TỤC và xóa bớt một ô.", "All 10 save slots are used. Open CONTINUE and delete one.");
                return;
            }
            Sfx.Play("charge", 0.8f, 1.2f);
            if (!GameFlow.StartNewGame()) Notify("Không tạo được ván mới.", "Could not start a new game.");
        }

        static void DoLoad(int slot)
        {
            if (slots[slot] == null) { Notify("Ô này đang trống.", "This slot is empty."); return; }
            Sfx.Play("charge", 0.8f, 1.2f);
            if (!GameFlow.ContinueGame(slot)) { Notify("Không đọc được ô lưu này (file hỏng).", "Could not read this save (corrupt file)."); Refresh(); }
        }

        static void DoQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        static void ActivateMain(int i)
        {
            Sfx.Play("click", 0.8f, 1.2f);
            switch (i)
            {
                case 0: DoNewGame(); break;
                case 1:
                    if (!anySave) { Notify("Chưa có ván nào được lưu.", "No saved game yet."); break; }
                    page = Page.Load; sel = LatestSlot(); break;
                case 2: page = Page.Settings; sel = 0; break;
                default: DoQuit(); break;
            }
        }

        static void Adjust(int row, int dir)
        {
            switch (row)
            {
                case 0: Cfg.Vietnamese = !Cfg.Vietnamese; break;
                case 1: Sfx.MusicVolume = Step(Sfx.MusicVolume, dir); break;
                case 2: Sfx.SfxVolume = Step(Sfx.SfxVolume, dir); break;
                case 3: GameSettings.SetFullscreen(!Screen.fullScreen); break;
                default: return;
            }
            GameSettings.Save();
            Sfx.Play("click", 0.6f, 1.3f);
        }

        static float Step(float v, int dir) { return Mathf.Clamp01(Mathf.Round((v + dir * 0.1f) * 10f) / 10f); }

        // ------------------------------------------------------------ phím (GameManager gọi mỗi khung hình ở trạng thái Menu)
        public static void UpdateInput()
        {
            if (noticeT > 0f) noticeT -= Time.unscaledDeltaTime;

            if (delSlot >= 0)
            {
                if (GameInput.ConfirmDown)
                {
                    SaveManager.Delete(delSlot);
                    Sfx.Play("click", 0.8f, 0.6f);
                    delSlot = -1;
                    Refresh();
                    if (!anySave) { page = Page.Main; sel = 0; }
                }
                else if (GameInput.PauseDown) delSlot = -1;
                return;
            }

            switch (page)
            {
                case Page.Main:
                    if (GameInput.NavUpDown) { sel = (sel + 3) % 4; Sfx.Play("click", 0.3f, 1.4f); }
                    if (GameInput.NavDownDown) { sel = (sel + 1) % 4; Sfx.Play("click", 0.3f, 1.4f); }
                    if (GameInput.ConfirmDown) ActivateMain(sel);
                    break;
                case Page.Load:
                    if (GameInput.NavUpDown) { sel = (sel + SaveManager.MaxSlots - 1) % SaveManager.MaxSlots; Sfx.Play("click", 0.3f, 1.4f); }
                    if (GameInput.NavDownDown) { sel = (sel + 1) % SaveManager.MaxSlots; Sfx.Play("click", 0.3f, 1.4f); }
                    if (GameInput.ConfirmDown) DoLoad(sel);
                    else if (GameInput.DiscardDown && slots[sel] != null) { delSlot = sel; Sfx.Play("click", 0.6f, 0.8f); }
                    else if (GameInput.PauseDown) { page = Page.Main; sel = 1; Sfx.Play("click", 0.5f, 0.9f); }
                    break;
                case Page.Settings:
                    if (GameInput.NavUpDown) { sel = (sel + 4) % 5; Sfx.Play("click", 0.3f, 1.4f); }
                    if (GameInput.NavDownDown) { sel = (sel + 1) % 5; Sfx.Play("click", 0.3f, 1.4f); }
                    if (GameInput.NavLeftDown) Adjust(sel, -1);
                    if (GameInput.NavRightDown) Adjust(sel, +1);
                    if (GameInput.ConfirmDown)
                    {
                        if (sel == 4) { page = Page.Main; sel = 2; Sfx.Play("click", 0.5f, 0.9f); }
                        else if (sel == 0 || sel == 3) Adjust(sel, +1);
                    }
                    else if (GameInput.PauseDown) { page = Page.Main; sel = 2; Sfx.Play("click", 0.5f, 0.9f); }
                    break;
            }
        }

        // ------------------------------------------------------------ vẽ
        public static void Draw(Hud h)
        {
            t = Time.unscaledTime;
            var ev = Event.current;
            mouseMoved = false;
            if (ev.type == EventType.Repaint)
            {
                mouseMoved = (ev.mousePosition - lastMouse).sqrMagnitude > 4f;
                lastMouse = ev.mousePosition;
            }

            Background(h);
            switch (page)
            {
                case Page.Main: DrawMain(h); break;
                case Page.Load: DrawLoad(h); break;
                default: DrawSettings(h); break;
            }

            if (noticeT > 0f && !string.IsNullOrEmpty(notice))
            {
                Rect nb = h.R(260, 640, 760, 40);
                h.Fill(nb, new Color(0.02f, 0.01f, 0.03f, 0.85f));
                h.Outline(nb, Cfg.A(Cfg.Yellow, 0.8f), 1.5f);
                h.Txt(nb, notice, 17, Cfg.Yellow, TextAnchor.MiddleCenter);
            }
            if (delSlot >= 0) DrawConfirmDelete(h);
        }

        static void DrawMain(Hud h)
        {
            Title(h, 70f, 100f);

            string[] labels =
            {
                Cfg.L("TRÒ CHƠI MỚI", "NEW GAME"),
                Cfg.L("TIẾP TỤC", "CONTINUE"),
                Cfg.L("CÀI ĐẶT", "SETTINGS"),
                Cfg.L("THOÁT", "QUIT"),
            };
            Color[] cols = { Cfg.Cyan, Cfg.Lime, Cfg.Magenta, Cfg.Orange };
            for (int i = 0; i < 4; i++)
            {
                bool enabled = i != 1 || anySave;
                Rect r = h.R(470, 290 + i * 76, 340, 60);
                if (MenuButton(h, r, labels[i], cols[i], enabled, sel == i, i)) ActivateMain(i);
            }

            h.Txt(h.R(0, 688, 1280, 24), Cfg.L("[W S] chọn   ·   [ENTER] xác nhận   ·   [V] đổi ngôn ngữ", "[W S] select   ·   [ENTER] confirm   ·   [V] switch language"),
                  14, Cfg.A(Color.white, 0.55f), TextAnchor.MiddleCenter, false);

#if UNITY_EDITOR
            // chỉ có trong Unity Editor: vào thẳng khu Dyber để thử nhanh (không dùng ô lưu)
            if (h.Button(h.R(1020, 676, 240, 32), "[DEV] Vào thẳng khu Dyber", Cfg.A(Cfg.Yellow, 0.7f), 13))
            {
                SaveManager.Detach();
                GameFlow.GoToPlace(Places.Dyber);
            }
#endif
        }

        static void DrawLoad(Hud h)
        {
            Title(h, 14f, 54f);
            Rect panel = h.R(190, 96, 900, 530);
            h.Fill(panel, new Color(0.01f, 0.02f, 0.06f, 0.88f));
            h.Outline(panel, Cfg.A(Cfg.Lime, 0.7f), 2f);
            h.Txt(h.R(214, 102, 700, 32), Cfg.L("TIẾP TỤC — CHỌN MỘT Ô LƯU", "CONTINUE — PICK A SAVE SLOT"), 22, Cfg.Lime);
            h.Txt(h.R(214, 130, 852, 20), Cfg.L("Tự động lưu mỗi khi bạn chết, khi về căn cứ và khi rời căn cứ.", "Autosaves when you die, when you reach the base and when you leave it."),
                  13, Cfg.A(Color.white, 0.6f), TextAnchor.UpperLeft, false);

            for (int i = 0; i < SaveManager.MaxSlots; i++)
            {
                float y = 156f + i * 46f;
                Rect row = h.R(210, y, 860, 42);
                var d = slots[i];
                bool on = sel == i;
                if (mouseMoved && row.Contains(Event.current.mousePosition)) sel = i;
                h.Fill(row, new Color(0.02f, 0.03f, 0.09f, 0.9f));
                if (on) h.Fill(row, Cfg.A(Cfg.Lime, 0.16f));
                h.Outline(row, Cfg.A(Cfg.Lime, on ? 0.9f : 0.3f), on ? 2.5f : 1.2f);
                h.Txt(h.R(222, y, 70, 42), Cfg.L("Ô ", "Slot ") + (i + 1).ToString("00"), 17, d != null ? Cfg.Lime : Cfg.A(Color.white, 0.4f), TextAnchor.MiddleLeft);

                if (d == null)
                {
                    h.Txt(h.R(300, y, 400, 42), Cfg.L("— trống —", "— empty —"), 16, Cfg.A(Color.white, 0.35f), TextAnchor.MiddleLeft, false);
                }
                else
                {
                    Rank rk = (Rank)Mathf.Clamp(d.rank, 0, 7);
                    h.Txt(h.R(300, y, 150, 42), rk + "  Lv." + d.level, 18, PowerRules.RankColor(rk), TextAnchor.MiddleLeft);
                    string place = d.tutorialDone ? Places.NameOf(d.place) : Cfg.L("Huấn luyện", "Training");
                    h.Txt(h.R(456, y, 230, 42), place, 16, Color.white, TextAnchor.MiddleLeft, false);
                    h.Txt(h.R(690, y, 90, 42), SaveManager.FormatPlayTime(d.playSeconds), 15, Cfg.A(Color.white, 0.8f), TextAnchor.MiddleLeft, false);
                    h.Txt(h.R(780, y, 110, 42), SaveManager.FormatSavedAt(d.savedAt), 14, Cfg.A(Color.white, 0.6f), TextAnchor.MiddleLeft, false);
                    if (h.Button(h.R(966, y + 5, 94, 32), Cfg.L("XÓA", "DELETE"), Cfg.Red, 15)) { delSlot = i; Sfx.Play("click", 0.6f, 0.8f); }
                }

                // bấm vào dòng (trừ nút XÓA) = nạp ô
                var ev = Event.current;
                if (delSlot < 0 && ev.type == EventType.MouseDown && ev.button == 0 && row.Contains(ev.mousePosition))
                {
                    ev.Use();
                    sel = i;
                    DoLoad(i);
                }
            }

            if (MenuButton(h, h.R(490, 632, 300, 44), Cfg.L("QUAY LẠI  [ESC]", "BACK  [ESC]"), Cfg.Cyan, true, false, -1))
            {
                page = Page.Main; sel = 1;
            }
            h.Txt(h.R(0, 684, 1280, 22), Cfg.L("[W S] chọn   ·   [ENTER] nạp   ·   [X] xóa ô   ·   [ESC] quay lại", "[W S] select   ·   [ENTER] load   ·   [X] delete   ·   [ESC] back"),
                  13, Cfg.A(Color.white, 0.55f), TextAnchor.MiddleCenter, false);
        }

        static void DrawSettings(Hud h)
        {
            Title(h, 30f, 64f);
            Rect panel = h.R(300, 150, 680, 440);
            h.Fill(panel, new Color(0.01f, 0.02f, 0.06f, 0.88f));
            h.Outline(panel, Cfg.A(Cfg.Magenta, 0.7f), 2f);
            h.Txt(h.R(326, 158, 500, 34), Cfg.L("CÀI ĐẶT", "SETTINGS"), 24, Cfg.Magenta);

            string[] labels = { Cfg.L("Ngôn ngữ", "Language"), Cfg.L("Âm lượng nhạc", "Music volume"), Cfg.L("Âm lượng hiệu ứng", "Effects volume"), Cfg.L("Toàn màn hình", "Fullscreen") };
            string[] values =
            {
                Cfg.Vietnamese ? "Tiếng Việt" : "English",
                Mathf.RoundToInt(Sfx.MusicVolume * 100f) + "%",
                Mathf.RoundToInt(Sfx.SfxVolume * 100f) + "%",
                Screen.fullScreen ? Cfg.L("BẬT", "ON") : Cfg.L("TẮT", "OFF"),
            };
            for (int i = 0; i < 4; i++)
            {
                float y = 210f + i * 76f;
                Rect row = h.R(326, y, 628, 60);
                bool on = sel == i;
                if (mouseMoved && row.Contains(Event.current.mousePosition)) sel = i;
                h.Fill(row, new Color(0.02f, 0.03f, 0.09f, 0.9f));
                if (on) h.Fill(row, Cfg.A(Cfg.Magenta, 0.14f));
                h.Outline(row, Cfg.A(Cfg.Magenta, on ? 0.9f : 0.3f), on ? 2.5f : 1.2f);
                h.Txt(h.R(342, y, 300, 60), labels[i], 20, Color.white, TextAnchor.MiddleLeft);
                if (h.Button(h.R(660, y + 10, 40, 40), "<", Cfg.Cyan, 22)) { sel = i; Adjust(i, -1); }
                h.Txt(h.R(704, y, 190, 60), values[i], 20, Cfg.Cyan, TextAnchor.MiddleCenter);
                if (h.Button(h.R(900, y + 10, 40, 40), ">", Cfg.Cyan, 22)) { sel = i; Adjust(i, +1); }
                if (i == 1 || i == 2)
                {
                    float v = i == 1 ? Sfx.MusicVolume : Sfx.SfxVolume;
                    h.NeonBar(h.R(704, y + 46, 190, 6), v, Cfg.Cyan, new Color(0f, 0f, 0f, 0.5f));
                }
            }
            if (MenuButton(h, h.R(490, 520, 300, 54), Cfg.L("QUAY LẠI  [ESC]", "BACK  [ESC]"), Cfg.Cyan, true, sel == 4, 4))
            {
                page = Page.Main; sel = 2;
            }
            h.Txt(h.R(0, 684, 1280, 22), Cfg.L("[W S] chọn   ·   [A D] chỉnh   ·   [ESC] quay lại", "[W S] select   ·   [A D] adjust   ·   [ESC] back"),
                  13, Cfg.A(Color.white, 0.55f), TextAnchor.MiddleCenter, false);
        }

        static void DrawConfirmDelete(Hud h)
        {
            h.Dim(0.7f);
            Rect box = h.R(340, 250, 600, 200);
            h.Fill(box, new Color(0.03f, 0.01f, 0.03f, 0.97f));
            h.Outline(box, Cfg.A(Cfg.Red, 0.9f), 2.5f);
            h.Txt(h.R(340, 266, 600, 36), Cfg.L("XÓA Ô LƯU " + (delSlot + 1) + "?", "DELETE SAVE SLOT " + (delSlot + 1) + "?"), 26, Cfg.Red, TextAnchor.MiddleCenter);
            h.Txt(h.R(360, 310, 560, 50), Cfg.L("Dữ liệu của ô này sẽ mất vĩnh viễn, không khôi phục được.", "This slot's data will be lost forever."), 17, Color.white, TextAnchor.MiddleCenter, false);
            if (h.Button(h.R(372, 384, 240, 48), Cfg.L("XÓA  [ENTER]", "DELETE  [ENTER]"), Cfg.Red, 20))
            {
                SaveManager.Delete(delSlot);
                Sfx.Play("click", 0.8f, 0.6f);
                delSlot = -1;
                Refresh();
                if (!anySave) { page = Page.Main; sel = 0; }
            }
            else if (delSlot >= 0 && h.Button(h.R(668, 384, 240, 48), Cfg.L("HỦY  [ESC]", "CANCEL  [ESC]"), Cfg.Cyan, 20)) delSlot = -1;
        }

        /// <summary>Nút menu: viền sáng khi được chọn (phím hoặc chuột), mờ khi bị khoá. Trả về true khi bấm chuột.</summary>
        static bool MenuButton(Hud h, Rect r, string label, Color c, bool enabled, bool selected, int index)
        {
            var ev = Event.current;
            bool hover = r.Contains(ev.mousePosition);
            if (index >= 0 && hover && mouseMoved) sel = index;
            bool hot = selected || hover;
            Color cc = enabled ? c : Cfg.A(c, 0.3f);
            h.Fill(r, new Color(0.02f, 0.03f, 0.08f, 0.88f));
            h.Fill(r, Cfg.A(cc, hot ? 0.28f : 0.10f));
            h.Outline(r, cc, hot ? 3.5f : 2f);
            h.Txt(r, label, 26, hot && enabled ? Color.white : cc, TextAnchor.MiddleCenter);
            if (ev.type == EventType.MouseDown && ev.button == 0 && hover && delSlot < 0)
            {
                ev.Use();
                return true;
            }
            return false;
        }

        // ------------------------------------------------------------ hình nền: thành phố neon trôi chậm
        static float Hash(int a, int b)
        {
            unchecked
            {
                int x = a * 374761393 + b * 668265263;
                x = (x ^ (x >> 13)) * 1274126177;
                x ^= x >> 16;
                return (x & 0xFFFF) / 65535f;
            }
        }

        static void Title(Hud h, float y, float size)
        {
            float pulse = 0.85f + 0.15f * Mathf.Sin(t * 3f);
            float flick = Hash((int)(t * 12f), 5) > 0.985f ? 0.45f : 1f;
            Rect shadow = h.R(5, y + 5, 1280, size * 1.15f);
            h.Txt(shadow, "NEON BREAKER", size, Cfg.A(Cfg.Magenta, 0.9f), TextAnchor.MiddleCenter);
            h.Txt(h.R(0, y, 1280, size * 1.15f), "NEON BREAKER", size, Cfg.A(Cfg.Cyan, pulse * flick), TextAnchor.MiddleCenter);
            float w = size * 6.2f;
            h.Fill(h.R(640f - w * 0.5f, y + size * 1.15f + 2f, w, 3f), Cfg.A(Cfg.Cyan, 0.55f * pulse));
            h.Fill(h.R(640f - w * 0.35f, y + size * 1.15f + 8f, w * 0.7f, 2f), Cfg.A(Cfg.Magenta, 0.5f * pulse));
        }

        static void Background(Hud h)
        {
            const int bands = 20;
            for (int i = 0; i < bands; i++)
            {
                float f = i / (float)(bands - 1);
                Color c = Color.Lerp(new Color(0.01f, 0.01f, 0.05f, 1f), new Color(0.08f, 0.02f, 0.11f, 1f), f * f);
                h.Fill(h.R(0, i * 720f / bands, 1280, 720f / bands + 1f), c);
            }
            h.Fill(h.R(0, 440, 1280, 160), Cfg.A(Cfg.Magenta, 0.05f));

            Layer(h, 0, 5f, 740f, 40f, 90f, 160f, 330f, new Color(0.035f, 0.02f, 0.09f, 1f), 0f, 0f, Cfg.Purple);
            Layer(h, 1, 12f, 750f, 60f, 110f, 200f, 400f, new Color(0.022f, 0.016f, 0.07f, 1f), 0.45f, 16f, Cfg.Cyan);
            Layer(h, 2, 26f, 760f, 90f, 150f, 260f, 520f, new Color(0.012f, 0.01f, 0.045f, 1f), 0.55f, 20f, Cfg.Magenta);

            // cột tối ở giữa để chữ dễ đọc + tối dần hai mép
            h.Fill(h.R(360, 0, 560, 720), new Color(0f, 0f, 0.02f, 0.32f));
            h.Fill(h.R(0, 0, 1280, 720), new Color(0f, 0f, 0.02f, 0.18f));
        }

        static void Layer(Hud h, int L, float speed, float baseY, float minW, float maxW, float minH, float maxH, Color body, float winA, float cell, Color neon)
        {
            const float period = 1700f;
            int n = Mathf.Max(4, Mathf.RoundToInt(period / ((minW + maxW) * 0.55f)));
            float slot = period / n;
            float scroll = (t * speed) % period;
            for (int i = 0; i < n; i++)
            {
                float w = Mathf.Lerp(minW, maxW, Hash(i, L * 11 + 1));
                float bh = Mathf.Lerp(minH, maxH, Hash(i, L * 11 + 2));
                float x = i * slot - scroll;
                while (x < -maxW) x += period;
                if (x > 1280f) continue;
                float top = baseY - bh;
                h.Fill(h.R(x, top, w, bh + 12f), body);
                h.Fill(h.R(x, top, w, 2f), Cfg.A(neon, 0.45f));
                if (Hash(i, L * 11 + 3) > 0.6f) h.Fill(h.R(x + w * 0.5f, top - 18f, 2f, 18f), Cfg.A(neon, 0.5f));
                if (winA <= 0f) continue;
                int cols = Mathf.Max(1, (int)(w / cell));
                int rows = (int)(bh / cell);
                float cw = cell * 0.42f;
                for (int r = 1; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        float hv = Hash(i * 131 + c, r + L * 977);
                        if (hv < 0.74f) continue;
                        float fl = 0.65f + 0.35f * Mathf.Sin(t * (0.6f + hv * 2f) + hv * 40f);
                        Color wc = hv > 0.93f ? Cfg.Magenta : (hv > 0.84f ? Cfg.Yellow : Cfg.Cyan);
                        h.Fill(h.R(x + (c + 0.3f) * cell, top + r * cell, cw, cw * 1.2f), Cfg.A(wc, winA * fl));
                    }
                }
            }
        }
    }
}
