using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Hướng dẫn trong căn cứ ở lần hồi sinh đầu tiên: sau đoạn thoại đầu, người chơi phải (1) nói chuyện với Albert (F), rồi
    /// (2) ra hộp thư nhiệm vụ nhận nhiệm vụ đầu (F). Trong lúc đó vẽ dòng mục tiêu ở trên màn hình + mũi tên nhấp nhô chỉ tới mục tiêu
    /// (ngoài màn hình thì mũi tên nằm ở mép màn hình, chỉ về phía đó). Cửa ra của căn cứ bị khoá tới khi nhận nhiệm vụ (xem BaseStation).
    /// </summary>
    public static class BaseGuide
    {
        // mũi tên cắm xuống, 9 ô rộng (mỗi dòng là số ô, căn giữa)
        static readonly int[] Arrow = { 5, 5, 9, 7, 5, 3, 1 };

        /// <summary>Có đang cần hướng dẫn không (ở căn cứ, chưa nhận nhiệm vụ đầu tiên).</summary>
        public static bool Active
        {
            get
            {
                return GameFlow.InBase && !BaseMissions.FirstMissionReceived && BaseManager.Inst != null
                       && GameManager.State == GameState.Playing && !GameManager.Paused && !BaseUi.IsOpen && !GameMenu.IsOpen && !Cine.Active;
            }
        }

        /// <summary>Mục tiêu hiện tại: vị trí thế giới (trên đầu mục tiêu) + dòng chữ. false nếu không có.</summary>
        public static bool Target(out Vector2 pos, out string text)
        {
            pos = Vector2.zero; text = "";
            var bm = BaseManager.Inst;
            if (bm == null) return false;
            if (BaseMissions.StoryTold && MissionBoard.Exists && BaseMissions.BoardUnlocked)
            {
                pos = MissionBoard.Inst.PromptPos + new Vector2(0f, 1.2f);
                text = Cfg.L("MỤC TIÊU: đến HỘP THƯ NHIỆM VỤ, bấm [F] để nhận nhiệm vụ", "OBJECTIVE: go to the MISSION MAILBOX, press [F] to take the mission");
                return true;
            }
            if (bm.albert == null) return false;
            pos = bm.albert.Head + new Vector2(0f, 1.9f);
            text = Cfg.L("MỤC TIÊU: đến gặp ALBERT, bấm [F] để nói chuyện và nhận nhiệm vụ", "OBJECTIVE: go see ALBERT, press [F] to talk and get your mission");
            return true;
        }

        public static void Draw(Hud h)
        {
            if (!Active) return;
            Vector2 wp; string text;
            if (!Target(out wp, out text)) return;
            float s = h.s;
            float pulse = 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 6f);
            Color c = Cfg.Yellow;

            // dòng mục tiêu (dưới khung tên khu vực)
            Rect r = h.R(260, 52, 760, 30);
            h.Fill(r, new Color(0.01f, 0.02f, 0.06f, 0.7f));
            h.Outline(r, Cfg.A(c, 0.5f + 0.4f * pulse), 1.5f);
            h.Txt(r, text, 15, Cfg.A(c, 0.85f + 0.15f * pulse), TextAnchor.MiddleCenter);

            // mũi tên chỉ tới mục tiêu
            var pl = Player.Instance;
            Vector2 sp = PixelScreen.WorldToGui(wp);
            float bob = Mathf.Sin(Time.unscaledTime * 5f) * 6f * s;
            const float cell = 3f;
            bool onScreen = sp.x > 40f * s && sp.x < Screen.width - 40f * s;
            if (onScreen)
            {
                if (pl != null && Mathf.Abs(pl.Position.x - wp.x) < 3.2f) return;      // đã tới gần: nhãn [F] đã hiện, khỏi vẽ mũi tên
                DrawDown(h, sp.x, sp.y + bob - 40f * s, cell * s, Cfg.A(c, 0.9f * pulse + 0.1f));
            }
            else
            {
                bool right = sp.x >= Screen.width * 0.5f;
                float y = Screen.height * 0.45f;
                float x = right ? Screen.width - 34f * s : 34f * s;
                if (pl != null) h.Txt(new Rect(x - (right ? 150f * s : -6f * s), y + 22f * s, 150f * s, 20f * s),
                                     Mathf.RoundToInt(Mathf.Abs(pl.Position.x - wp.x)) + "m", 14, Cfg.A(c, pulse), right ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft);
                DrawSide(h, x + (right ? -bob : bob) * 0.5f, y, cell * s, right, Cfg.A(c, 0.9f * pulse + 0.1f));
            }
        }

        // mũi tên cắm xuống: đỉnh dưới cùng tại (cx, tipY + chiều cao)
        static void DrawDown(Hud h, float cx, float topY, float cell, Color c)
        {
            for (int r = 0; r < Arrow.Length; r++)
            {
                float w = Arrow[r] * cell;
                h.Fill(new Rect(cx - w * 0.5f, topY + r * cell, w, cell), c);
            }
        }

        // mũi tên ngang chỉ sang phải / trái: xoay mũi tên cắm xuống 90 độ
        static void DrawSide(Hud h, float cx, float cy, float cell, bool right, Color c)
        {
            for (int r = 0; r < Arrow.Length; r++)
            {
                float w = Arrow[r] * cell;
                // dòng r của mũi tên cắm xuống → cột (r) của mũi tên ngang
                float col = right ? cx - Arrow.Length * cell * 0.5f + r * cell : cx + Arrow.Length * cell * 0.5f - (r + 1) * cell;
                h.Fill(new Rect(col, cy - w * 0.5f, cell, w), c);
            }
        }
    }
}
