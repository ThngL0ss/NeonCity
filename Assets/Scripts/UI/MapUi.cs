using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Bản đồ (chỉ mở bằng cửa ra của căn cứ — không còn phím M). Mỗi địa điểm trong Places.All là một biểu tượng: tên ở TRÊN, hàng 5 đầu lâu ở DƯỚI
    /// (luôn vẽ đủ 5; số đầu lâu = độ nguy hiểm, phần còn lại mờ đi). Phố Neon nằm giữa bản đồ; Rìa Phố Neon là vùng hình chữ L ôm Phố Neon từ Tây xuống Nam.
    /// Điều khiển: A D / W S (hoặc mũi tên) chuyển giữa các địa điểm · ENTER / F hoặc nút ĐI = đến đó (chỉ khi đang ở căn cứ) · bấm chuột vào biểu tượng để chọn · ESC đóng.
    /// </summary>
    public static class MapUi
    {
        static bool leaving;
        static int sel;
        static string goId;

        // khung bản đồ (toạ độ tham chiếu 1280 x 720)
        const float FX = 60f, FY = 40f, FW = 1160f, FH = 640f;

        // đầu lâu 7 x 7 ô
        static readonly string[] Skull =
        {
            ".#####.",
            "#######",
            "##.#.##",
            "#######",
            ".#####.",
            "..#.#..",
            "..#.#..",
        };

        /// <summary>Dọn trạng thái static (gọi mỗi lần nạp Scene): không còn "đang đi" dở.</summary>
        public static void ResetStatics()
        {
            leaving = false;
            goId = null;
        }

        public static void OnOpen()
        {
            leaving = false;
            sel = 0;
            for (int i = 0; i < Places.All.Length; i++) if (Places.All[i].id == GameFlow.CurrentPlaceId) { sel = i; break; }
        }

        static bool CanGo(PlaceDef p)
        {
            return GameFlow.InBase && p != null && Places.Unlocked(p) && GameFlow.SceneAvailable(p.id);
        }

        static int Step(int from, int dir)
        {
            // đi theo thứ tự từ Tây sang Đông (x nhỏ → lớn)
            int n = Places.All.Length;
            int best = from;
            float bx = Places.All[from].map.x;
            float bestDx = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                if (i == from) continue;
                float dx = (Places.All[i].map.x - bx) * dir;
                if (dx > 0.0001f && dx < bestDx) { bestDx = dx; best = i; }
            }
            return best;
        }

        public static void UpdateInput()
        {
            if (leaving) return;
            if (GameInput.PauseDown) { BaseUi.Close(); return; }
            int n = Places.All.Length;
            if (n == 0) return;
            if (GameInput.NavLeftDown || GameInput.NavUpDown) { int s2 = Step(sel, -1); if (s2 != sel) { sel = s2; Sfx.Play("click", 0.4f, 1.3f); } }
            if (GameInput.NavRightDown || GameInput.NavDownDown) { int s2 = Step(sel, +1); if (s2 != sel) { sel = s2; Sfx.Play("click", 0.4f, 1.3f); } }
            if ((GameInput.ConfirmDown || GameInput.InteractDown) && CanGo(Places.All[sel])) Go(Places.All[sel].id);
        }

        static void Go(string id)
        {
            if (leaving) return;
            leaving = true;
            goId = id;
            Sfx.Play("charge", 0.8f, 1.3f);
            GameManager.Inst.StartCoroutine(GoRoutine());
        }

        static System.Collections.IEnumerator GoRoutine()
        {
            string dest = goId;               // chép ra biến cục bộ: BaseUi.ResetStatics() bên dưới xoá goId (bản sửa trước làm màn đen vì gọi GoToPlace(null))
            Cine.SetFade(1f, 0.7f);
            float t = 0f;
            while (t < 0.8f) { t += Time.unscaledDeltaTime; yield return null; }
            if (string.IsNullOrEmpty(dest) || !GameFlow.SceneAvailable(dest))
            {
                // không nạp được Scene đích: trả bản đồ về trạng thái chọn lại được (không để kẹt)
                Cine.SetFade(0f, 0.5f);
                leaving = false;
                GameManager.Banner(Cfg.L("KHÔNG MỞ ĐƯỢC ĐỊA ĐIỂM NÀY (Scene chưa có trong Build Settings)", "CANNOT OPEN THIS PLACE (scene is not in Build Settings)"), Cfg.Yellow, 3f);
                yield break;
            }
            BaseUi.ResetStatics();            // đóng bản đồ trước khi nạp Scene (không để Mode = Map sót lại)
            if (!GameFlow.GoToPlace(dest))
            {
                // phòng hờ: nạp thất bại thì hạ màn đen, không để người chơi kẹt
                Cine.SetFade(0f, 0.5f);
                GameManager.Banner(Cfg.L("KHÔNG MỞ ĐƯỢC ĐỊA ĐIỂM NÀY", "CANNOT OPEN THIS PLACE"), Cfg.Yellow, 3f);
            }
        }

        static Vector2 P(Hud h, float nx, float ny) { return new Vector2((FX + nx * FW) * h.s, (FY + ny * FH) * h.s); }

        /// <summary>Vẽ hàng 5 đầu lâu căn giữa tại cx: lit đầu lâu sáng (đỏ), còn lại mờ.</summary>
        static void Skulls(Hud h, float cx, float y, int lit, Color c)
        {
            const float cell = 2f, w = 7 * cell, gap = 6f;
            float total = 5 * w + 4 * gap;
            float x0 = cx - total * 0.5f;
            for (int k = 0; k < 5; k++)
            {
                Color col = k < lit ? c : new Color(c.r * 0.35f, c.g * 0.35f, c.b * 0.35f, 0.28f);
                float sx = x0 + k * (w + gap);
                for (int r = 0; r < Skull.Length; r++)
                    for (int q = 0; q < Skull[r].Length; q++)
                        if (Skull[r][q] == '#') h.Fill(h.R(sx + q * cell, y + r * cell, cell, cell), col);
            }
        }

        public static void Draw(Hud h)
        {
            float s = h.s;
            h.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.985f));
            Rect frame = h.R(FX, FY, FW, FH);
            h.Outline(frame, Cfg.A(Cfg.Cyan, 0.55f), 2f);
            // lưới mờ
            for (int i = 1; i < 14; i++) h.Fill(new Rect(frame.x + i * 80f * s - 40f * s, frame.y, 1f, frame.height), new Color(0.1f, 0.9f, 1f, 0.05f));
            for (int j = 1; j < 8; j++) h.Fill(new Rect(frame.x, frame.y + j * 80f * s, frame.width, 1f), new Color(0.1f, 0.9f, 1f, 0.05f));

            h.Txt(h.R(90, 50, 600, 44), Cfg.L("BẢN ĐỒ", "MAP"), 34, Cfg.Cyan);
            h.Txt(h.R(92, 96, 900, 22), GameFlow.InBase ? Cfg.L("Chọn một địa điểm rồi bấm ĐI. Đầu lâu càng nhiều càng nguy hiểm.", "Pick a place and press GO. More skulls means more danger.")
                                                         : Cfg.L("Chỉ chọn được điểm đến khi đang ở căn cứ.", "Destinations can only be picked at the base."),
                  15, Cfg.A(Color.white, 0.55f), TextAnchor.UpperLeft, false);

            // vùng tô (vẽ trước để biểu tượng nằm đè lên)
            for (int i = 0; i < Places.All.Length; i++)
            {
                var p = Places.All[i];
                if (p.zones == null) continue;
                bool on = Places.Unlocked(p);
                float a = (i == sel ? 0.2f : 0.1f) * (on ? 1f : 0.4f);
                for (int z = 0; z < p.zones.Length; z++)
                {
                    Rect zr = p.zones[z];
                    Vector2 a0 = P(h, zr.xMin, zr.yMin), a1 = P(h, zr.xMax, zr.yMax);
                    h.Fill(new Rect(a0.x, a0.y, a1.x - a0.x, a1.y - a0.y), Cfg.A(p.color, a));
                }
                if (p.outline != null && p.outline.Length > 1)
                    for (int k = 0; k < p.outline.Length; k++)
                        BaseUi.Line(h, P(h, p.outline[k].x, p.outline[k].y), P(h, p.outline[(k + 1) % p.outline.Length].x, p.outline[(k + 1) % p.outline.Length].y),
                                    Cfg.A(p.color, i == sel ? 0.9f : 0.5f), (i == sel ? 3f : 2f) * s);
            }

            // biểu tượng địa điểm: tên ở trên, đầu lâu ở dưới
            float pulse = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 3.5f);
            for (int i = 0; i < Places.All.Length; i++)
            {
                var p = Places.All[i];
                bool on = Places.Unlocked(p);
                bool isSel = i == sel;
                float cx = FX + p.map.x * FW, cy = FY + p.map.y * FH;
                Color nc = on ? p.color : new Color(0.5f, 0.52f, 0.6f, 1f);
                float rr = (22f + (isSel ? 4f * pulse : 0f));
                Rect node = h.R(cx - rr, cy - rr, rr * 2f, rr * 2f);
                h.Fill(node, Cfg.A(nc, 0.12f + (isSel ? 0.14f * pulse : 0.04f)));
                h.Outline(node, Cfg.A(nc, isSel ? 1f : 0.7f), isSel ? 3f : 2f);
                h.Fill(h.R(cx - 6, cy - 6, 12, 12), nc);
                if (!GameFlow.InBase && p.id == GameFlow.CurrentPlaceId)
                    h.Txt(h.R(cx - 80, cy + 26, 160, 18), Cfg.L("BẠN Ở ĐÂY", "YOU ARE HERE"), 12, Cfg.Yellow, TextAnchor.MiddleCenter);
                h.Txt(h.R(cx - 150, cy - rr - 34, 300, 28), on ? p.Name : "???", 20, nc, TextAnchor.MiddleCenter);
                Skulls(h, cx, cy + rr + 8f, on ? p.skulls : 0, Cfg.Red);
                if (BaseUi.Clicked(h.R(cx - 70, cy - rr - 36, 140, rr * 2f + 76f))) { sel = i; Sfx.Play("click", 0.4f, 1.3f); }
            }

            DrawInfo(h, Places.All.Length > 0 ? Places.All[Mathf.Clamp(sel, 0, Places.All.Length - 1)] : null);
            h.Txt(h.R(90, 650, 640, 22), Cfg.L("[A D] chọn   ·   [ENTER / F] đi   ·   [ESC] đóng", "[A D] pick   ·   [ENTER / F] go   ·   [ESC] close"),
                  14, Cfg.A(Color.white, 0.5f), TextAnchor.UpperLeft, false);
        }

        static void DrawInfo(Hud h, PlaceDef p)
        {
            if (p == null) return;
            bool on = Places.Unlocked(p);
            Rect box = h.R(760, 520, 440, 150);
            h.Fill(box, new Color(0.01f, 0.02f, 0.06f, 0.92f));
            h.Outline(box, Cfg.A(on ? p.color : Color.gray, 0.7f), 2f);
            h.Txt(h.R(776, 526, 280, 28), on ? p.Name : "???", 22, on ? p.color : Color.gray);
            Skulls(h, 1128f, 534f, on ? p.skulls : 0, Cfg.Red);
            h.TxtWrap(h.R(776, 558, 280, 108), on ? p.Desc : Cfg.L("Chưa khám phá.", "Not explored yet."), 13, Cfg.A(Color.white, 0.8f));

            bool can = CanGo(p);
            Rect btn = h.R(1066, 616, 124, 44);
            if (leaving) { h.Txt(btn, "...", 22, Cfg.Cyan, TextAnchor.MiddleCenter); return; }
            if (can)
            {
                if (h.Button(btn, Cfg.L("ĐI [ENTER]", "GO [ENTER]"), Cfg.Cyan, 16)) Go(p.id);
            }
            else
            {
                string why = !GameFlow.InBase ? Cfg.L("Chỉ đi được từ căn cứ", "Only from the base")
                           : !on ? Cfg.L("Chưa mở", "Locked")
                           : Cfg.L("Chưa có Scene", "Scene missing");
                h.TxtWrap(h.R(1066, 596, 128, 64), why, 13, Cfg.A(Cfg.Orange, 0.9f), TextAnchor.MiddleRight);
            }
        }
    }
}
