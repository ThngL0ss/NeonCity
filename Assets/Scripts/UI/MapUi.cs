using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Bản đồ — mở bằng cửa ra của căn cứ HOẶC cửa ra / cửa vào ở hai đầu mọi khu vực (không còn phím M). Mỗi địa điểm trong Places.All là một biểu tượng
    /// riêng (MapIcons): Căn cứ = mặt Albert (phía Bắc Phố Neon), Phố Neon = các toà nhà, Rìa Phố Neon = mặt kẻ đeo mặt nạ. Tên ở TRÊN, hàng 5 đầu lâu ở DƯỚI
    /// (số đầu lâu = độ nguy hiểm). MŨI TÊN XANH chỉ xuống nằm trên biểu tượng đang chọn — lúc mở bản đồ nó đứng ở nơi bạn đang ở, bấm chọn sang nơi khác thì mũi tên chạy theo;
    /// khung thoi xanh quanh biểu tượng = nơi bạn đang đứng.
    /// Điều khiển: A D W S (hoặc mũi tên) chọn địa điểm gần nhất theo hướng · ENTER / F hoặc nút ĐI = đến đó · bấm chuột vào biểu tượng để chọn · ESC đóng.
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

        static string CurId { get { return GameFlow.InBase ? Places.Base : GameFlow.CurrentPlaceId; } }

        public static void OnOpen()
        {
            leaving = false;
            sel = 0;
            string cur = CurId;
            for (int i = 0; i < Places.All.Length; i++) if (Places.All[i].id == cur) { sel = i; break; }
        }

        static bool IsHere(PlaceDef p) { return p != null && p.id == CurId; }

        static bool CanGo(PlaceDef p)
        {
            return p != null && !IsHere(p) && Places.Unlocked(p) && GameFlow.SceneAvailable(p.id);
        }

        /// <summary>Địa điểm gần nhất theo hướng dir (toạ độ bản đồ, y hướng xuống). Không có thì giữ nguyên.</summary>
        static int Step(int from, Vector2 dir)
        {
            int n = Places.All.Length;
            int best = from;
            float bestScore = float.MaxValue;
            Vector2 o = Places.All[from].map;
            for (int i = 0; i < n; i++)
            {
                if (i == from) continue;
                Vector2 d = Places.All[i].map - o;
                float along = Vector2.Dot(d, dir);
                if (along < 0.03f) continue;
                float side = Mathf.Abs(d.x * dir.y - d.y * dir.x);
                float score = along + side * 1.5f;           // ưu tiên nơi nằm thẳng theo hướng bấm
                if (score < bestScore) { bestScore = score; best = i; }
            }
            return best;
        }

        public static void UpdateInput()
        {
            if (leaving) return;
            if (GameInput.PauseDown) { BaseUi.Close(); return; }
            int n = Places.All.Length;
            if (n == 0) return;
            int s2 = sel;
            if (GameInput.NavLeftDown) s2 = Step(sel, new Vector2(-1f, 0f));
            else if (GameInput.NavRightDown) s2 = Step(sel, new Vector2(1f, 0f));
            else if (GameInput.NavUpDown) s2 = Step(sel, new Vector2(0f, -1f));
            else if (GameInput.NavDownDown) s2 = Step(sel, new Vector2(0f, 1f));
            if (s2 != sel) { sel = s2; Sfx.Play("click", 0.4f, 1.3f); }
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
            string dest = goId;               // chép ra biến cục bộ: BaseUi.ResetStatics() bên dưới xoá goId
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
            bool ok;
            if (dest == Places.Base) { GameFlow.GoToBase(false, true); ok = true; }       // về căn cứ "bình thường": ra ở cửa ra, không hồi sinh
            else ok = GameFlow.GoToPlace(dest);
            if (!ok)
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

        static readonly Color Green = new Color(0.30f, 0.92f, 0.30f, 1f);

        /// <summary>Mũi tên xanh chỉ xuống, mũi tại (cx, tipY) (toạ độ tham chiếu).</summary>
        static void Arrow(Hud h, float cx, float tipY)
        {
            const int rows = 12;
            for (int r = 0; r < rows; r++)
            {
                float w = (rows - r) * 3f;                 // rộng dần lên trên, nhọn ở dưới
                float y = tipY - (rows - r) * 2.4f;
                h.Fill(h.R(cx - w * 0.5f, y, w, 2.6f), r < 2 ? new Color(0.55f, 1f, 0.55f, 1f) : Green);
            }
            h.Fill(h.R(cx - 14f, tipY - rows * 2.4f - 1f, 28f, 2f), new Color(0.65f, 1f, 0.65f, 1f));
        }

        /// <summary>Khung thoi xanh (hình vuông xoay 45°) bao quanh biểu tượng: nơi người chơi đang đứng.</summary>
        static void Diamond(Hud h, float cx, float cy, float r)
        {
            float s = h.s;
            Vector2 c = new Vector2(cx * s, cy * s);
            Vector2 t = c + new Vector2(0f, -r * s), rt = c + new Vector2(r * s, 0f), b = c + new Vector2(0f, r * s), l = c + new Vector2(-r * s, 0f);
            BaseUi.Line(h, t, rt, Green, 5f * s); BaseUi.Line(h, rt, b, Green, 5f * s);
            BaseUi.Line(h, b, l, Green, 5f * s); BaseUi.Line(h, l, t, Green, 5f * s);
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
            h.Txt(h.R(92, 96, 900, 22), Cfg.L("Chọn một địa điểm rồi bấm ĐI. Đầu lâu càng nhiều càng nguy hiểm.", "Pick a place and press GO. More skulls means more danger."),
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
            float bob = Mathf.Sin(Time.unscaledTime * 5f) * 4f;
            const float cell = 4f, half = 8f * cell;           // biểu tượng 16 x 16 ô, mỗi ô 4 đơn vị tham chiếu = 64 x 64
            for (int i = 0; i < Places.All.Length; i++)
            {
                var p = Places.All[i];
                bool on = Places.Unlocked(p);
                bool isSel = i == sel;
                float cx = FX + p.map.x * FW, cy = FY + p.map.y * FH;
                Color nc = on ? p.color : new Color(0.5f, 0.52f, 0.6f, 1f);
                h.Fill(h.R(cx - half - 6f, cy - half - 6f, half * 2f + 12f, half * 2f + 12f), Cfg.A(nc, isSel ? 0.20f : 0.08f));      // nền mờ sau biểu tượng
                MapIcons.Draw(h, p.id, cx, cy, cell, on ? 1f : 0.7f, !on);
                if (IsHere(p)) Diamond(h, cx, cy, half + 22f);
                h.Txt(h.R(cx - 150, cy - half - 36f, 300, 28), on ? p.Name : "???", 20, isSel ? Color.white : nc, TextAnchor.MiddleCenter);
                if (p.skulls > 0 || !on) Skulls(h, cx, cy + half + 10f, on ? p.skulls : 0, Cfg.Red);
                else h.Txt(h.R(cx - 80, cy + half + 8f, 160, 18), Cfg.L("AN TOÀN", "SAFE"), 13, Cfg.A(Cfg.Lime, 0.9f), TextAnchor.MiddleCenter);
                if (BaseUi.Clicked(h.R(cx - 80, cy - half - 40f, 160, half * 2f + 90f))) { sel = i; Sfx.Play("click", 0.4f, 1.3f); }
            }

            // mũi tên xanh chỉ xuống trên biểu tượng đang chọn (lúc mới mở = nơi đang đứng), nhấp nhô nhẹ
            if (Places.All.Length > 0)
            {
                var ps = Places.All[Mathf.Clamp(sel, 0, Places.All.Length - 1)];
                float ax = FX + ps.map.x * FW, ay = FY + ps.map.y * FH - half - 40f + bob;
                Arrow(h, ax, ay);
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
                string why = IsHere(p) ? Cfg.L("Bạn đang ở đây", "You are here")
                           : !on ? Cfg.L("Chưa mở", "Locked")
                           : Cfg.L("Chưa có Scene", "Scene missing");
                h.TxtWrap(h.R(1066, 596, 128, 64), why, 13, Cfg.A(IsHere(p) ? Green : Cfg.Orange, 0.9f), TextAnchor.MiddleRight);
            }
        }
    }
}
