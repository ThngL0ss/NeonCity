using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Bảng nhiệm vụ của hộp thư (BaseMode.Missions). Bên trái: danh sách nhiệm vụ (đã xong / đang làm / chưa mở).
    /// Bên phải: thẻ nhiệm vụ hiện tại — mô tả, yêu cầu + tiến độ, phần thưởng, nút NHẬN (ENTER).
    /// Nhận xong, kiếm đủ nguyên liệu rồi mang tới giao cho Albert (nói chuyện bấm F).
    /// </summary>
    public static class MissionUi
    {
        public static void UpdateInput()
        {
            if (GameInput.PauseDown || GameInput.InteractDown) { BaseUi.Close(); return; }
            if (GameInput.ConfirmDown) TryAccept();
        }

        static void TryAccept()
        {
            BaseMissions.Accept();
        }

        public static void Draw(Hud h)
        {
            h.Dim(0.76f);
            Rect panel = h.R(150, 40, 980, 640);
            h.Fill(panel, new Color(0.01f, 0.02f, 0.06f, 0.96f));
            h.Outline(panel, Cfg.A(Cfg.Cyan, 0.8f), 2f);
            h.Txt(h.R(176, 48, 700, 44), Cfg.L("HỘP THƯ NHIỆM VỤ", "MISSION MAILBOX"), 34, Cfg.Cyan);

            // ---- danh sách bên trái
            Rect list = h.R(170, 108, 290, 500);
            h.Fill(list, new Color(0.02f, 0.03f, 0.09f, 0.9f));
            h.Outline(list, Cfg.A(Cfg.Cyan, 0.35f), 1.5f);
            var all = BaseMissions.All;
            int shown = 0;
            for (int i = 0; i < all.Count && shown < 11; i++)
            {
                // chỉ hiện nhiệm vụ đã xong + nhiệm vụ hiện tại; các nhiệm vụ sau là "???"
                bool done = i < BaseMissions.Index;
                bool cur = i == BaseMissions.Index;
                float y = 118f + shown * 44f;
                Rect row = h.R(180, y, 270, 40);
                h.Fill(row, Cfg.A(cur ? Cfg.Cyan : Color.white, cur ? 0.18f : 0.04f));
                if (cur) h.Outline(row, Cfg.Cyan, 2f);
                string mark = done ? "OK" : (cur ? ">" : "?");
                Color mc = done ? Cfg.Lime : (cur ? Cfg.Cyan : Cfg.A(Color.white, 0.35f));
                h.Txt(h.R(186, y, 30, 40), mark, done ? 14 : 20, mc, TextAnchor.MiddleCenter);
                string t = (done || cur) ? all[i].Title : "???";
                h.Txt(h.R(218, y, 228, 40), t, 15, done ? Cfg.A(Color.white, 0.6f) : (cur ? Color.white : Cfg.A(Color.white, 0.35f)), TextAnchor.MiddleLeft, cur);
                shown++;
                if (i > BaseMissions.Index + 2) break;
            }

            // ---- thẻ nhiệm vụ hiện tại
            Rect card = h.R(476, 108, 634, 500);
            h.Fill(card, new Color(0.02f, 0.03f, 0.09f, 0.9f));
            h.Outline(card, Cfg.A(Cfg.Cyan, 0.5f), 1.5f);
            var m = BaseMissions.Current;
            if (m == null)
            {
                h.Txt(card, Cfg.L("Hiện không còn nhiệm vụ nào.\nAlbert sẽ giao việc mới khi có.", "No missions left for now.\nAlbert will assign more later."), 22,
                      Cfg.A(Color.white, 0.7f), TextAnchor.MiddleCenter, false);
            }
            else
            {
                h.Txt(h.R(496, 118, 600, 40), m.Title, 28, Cfg.Yellow);
                h.TxtWrap(h.R(496, 164, 594, 130), m.Desc, 16, Cfg.A(Color.white, 0.92f));

                float y = 304f;
                h.Txt(h.R(496, y, 400, 26), Cfg.L("YÊU CẦU", "REQUIREMENTS"), 15, Cfg.A(Cfg.Cyan, 0.85f));
                y += 28f;
                if (m.reqs != null && m.reqs.Count > 0)
                {
                    for (int i = 0; i < m.reqs.Count; i++)
                    {
                        var r = m.reqs[i];
                        bool ok = r.Done;
                        h.Txt(h.R(496, y, 230, 30), r.Name, 18, ok ? Cfg.Lime : Color.white, TextAnchor.MiddleLeft);
                        h.NeonBar(h.R(730, y + 8, 250, 14), r.need > 0 ? (float)r.Have / r.need : 1f, ok ? Cfg.Lime : Cfg.Cyan, new Color(0f, 0f, 0f, 0.6f));
                        h.Txt(h.R(990, y, 100, 30), r.Have + " / " + r.need, 18, ok ? Cfg.Lime : Color.white, TextAnchor.MiddleRight);
                        y += 36f;
                    }
                }
                else
                {
                    h.NeonBar(h.R(496, y + 8, 480, 14), m.target > 0 ? (float)m.Progress / m.target : 1f, m.Done ? Cfg.Lime : Cfg.Cyan, new Color(0f, 0f, 0f, 0.6f));
                    h.Txt(h.R(990, y, 100, 30), m.Progress + " / " + m.target, 18, m.Done ? Cfg.Lime : Color.white, TextAnchor.MiddleRight);
                    y += 36f;
                }

                string reward = RewardText(m);
                if (reward.Length > 0)
                    h.Txt(h.R(496, y + 6, 594, 26), Cfg.L("Thưởng: ", "Reward: ") + reward, 16, Cfg.Yellow, TextAnchor.MiddleLeft, false);

                // ---- trạng thái + nút
                Rect st = h.R(496, 540, 594, 56);
                if (!BaseMissions.Accepted)
                {
                    if (h.Button(st, Cfg.L("NHẬN NHIỆM VỤ  [ENTER]", "ACCEPT MISSION  [ENTER]"), Cfg.Cyan, 24)) TryAccept();
                }
                else if (m.Done)
                {
                    h.Fill(st, Cfg.A(Cfg.Lime, 0.15f));
                    h.Outline(st, Cfg.Lime, 2f);
                    h.Txt(st, Cfg.L("ĐÃ ĐỦ — MANG TỚI GIAO CHO ALBERT", "READY — DELIVER IT TO ALBERT"), 20, Cfg.Lime, TextAnchor.MiddleCenter);
                }
                else
                {
                    h.Fill(st, Cfg.A(Cfg.Yellow, 0.10f));
                    h.Outline(st, Cfg.A(Cfg.Yellow, 0.7f), 2f);
                    h.Txt(st, Cfg.L("ĐANG THỰC HIỆN", "IN PROGRESS"), 20, Cfg.Yellow, TextAnchor.MiddleCenter);
                }
            }

            h.Txt(h.R(176, 640, 928, 28), Cfg.L("Nộp nguyên liệu và nhận thưởng: nói chuyện với Albert.   ·   [ESC / F] đóng", "Deliver materials and collect rewards: talk to Albert.   ·   [ESC / F] close"),
                  14, Cfg.A(Color.white, 0.6f), TextAnchor.MiddleLeft, false);
        }

        public static string RewardText(MissionDef m)
        {
            string s = "";
            if (m.rewardCoins > 0) s += "+" + m.rewardCoins + Cfg.L(" xu", " coins");
            if (m.rewardOil > 0) s += (s.Length > 0 ? "   " : "") + "+" + m.rewardOil + Cfg.L(" dầu", " oil");
            return s;
        }
    }
}
