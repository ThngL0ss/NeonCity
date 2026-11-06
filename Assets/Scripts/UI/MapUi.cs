using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Bản đồ (phím M, hoặc cửa ra của căn cứ): hiện tại chỉ là màn đen. Nếu BaseConfig ▸ Map Test Destination bật thì có 1 điểm
    /// thử nghiệm để quay lại màn chơi (chỉ chọn được khi đang ở căn cứ). ENTER / bấm vào điểm = đi; ESC / M đóng.
    /// </summary>
    public static class MapUi
    {
        static bool leaving;

        public static void OnOpen() { leaving = false; }

        static bool NodeEnabled { get { return GameFlow.InBase && BaseConfig.Cur.mapTestDestination; } }

        public static void UpdateInput()
        {
            if (leaving) return;
            if (GameInput.PauseDown || GameInput.MapDown) { BaseUi.Close(); return; }
            if (NodeEnabled && (GameInput.ConfirmDown || GameInput.InteractDown)) Go();
        }

        static void Go()
        {
            if (leaving) return;
            leaving = true;
            Sfx.Play("charge", 0.8f, 1.3f);
            GameManager.Inst.StartCoroutine(GoRoutine());
        }

        static System.Collections.IEnumerator GoRoutine()
        {
            Cine.SetFade(1f, 0.7f);
            float t = 0f;
            while (t < 0.8f) { t += Time.unscaledDeltaTime; yield return null; }
            GameFlow.GoToLevel();
        }

        public static void Draw(Hud h)
        {
            float s = h.s;
            h.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.985f));
            Rect frame = h.R(60, 40, 1160, 640);
            h.Outline(frame, Cfg.A(Cfg.Cyan, 0.55f), 2f);
            // lưới mờ
            for (int i = 1; i < 14; i++) h.Fill(new Rect(frame.x + i * 80f * s - 40f * s, frame.y, 1f, frame.height), new Color(0.1f, 0.9f, 1f, 0.05f));
            for (int j = 1; j < 8; j++) h.Fill(new Rect(frame.x, frame.y + j * 80f * s, frame.width, 1f), new Color(0.1f, 0.9f, 1f, 0.05f));

            h.Txt(h.R(90, 50, 600, 44), Cfg.L("BẢN ĐỒ", "MAP"), 34, Cfg.Cyan);
            h.Txt(h.R(92, 96, 800, 22), Cfg.L("Chưa có khu vực nào được khám phá.", "No region explored yet."), 15, Cfg.A(Color.white, 0.55f), TextAnchor.UpperLeft, false);

            if (BaseConfig.Cur.mapTestDestination)
            {
                Vector2 c = new Vector2(640f * s, 360f * s);
                bool en = NodeEnabled && !leaving;
                float pulse = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 3.5f);
                Color nc = en ? Cfg.Cyan : Cfg.A(Cfg.Cyan, 0.3f);
                float rr = (26f + 4f * pulse) * s;
                Rect node = new Rect(c.x - rr, c.y - rr, rr * 2f, rr * 2f);
                h.Fill(node, Cfg.A(nc, 0.12f + 0.12f * pulse));
                h.Outline(node, nc, 2.5f);
                h.Fill(new Rect(c.x - 6f * s, c.y - 6f * s, 12f * s, 12f * s), nc);
                h.Txt(h.R(440, 410, 400, 28), Cfg.L("KHU THỬ NGHIỆM — Thành phố Neon", "TEST ZONE — Neon City"), 20, nc, TextAnchor.MiddleCenter);
                h.Txt(h.R(440, 438, 400, 22), en ? Cfg.L("[ENTER] hoặc bấm vào để đi", "[ENTER] or click to go") : Cfg.L("Chỉ chọn được điểm đến khi đang ở căn cứ", "Destinations can only be picked at the base"),
                      14, Cfg.A(Color.white, 0.6f), TextAnchor.MiddleCenter, false);
                if (en && BaseUi.Clicked(node)) Go();
            }
            h.Txt(h.R(90, 646, 800, 22), Cfg.L("[M / ESC] đóng", "[M / ESC] close"), 14, Cfg.A(Color.white, 0.5f), TextAnchor.UpperLeft, false);
        }
    }
}
