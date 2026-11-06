using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>Một câu thoại.</summary>
    public class DlgLine
    {
        public string speaker, text;
        public Color color = Cfg.Cyan;
        public DlgLine(string speaker, string text, Color color) { this.speaker = speaker; this.text = text; this.color = color; }
    }

    /// <summary>Hộp thoại ở đáy màn hình: gõ chữ, ENTER / F / bấm chuột để sang câu kế (hoặc hiện hết câu), ESC bỏ qua cả đoạn.</summary>
    public static class DialogueUi
    {
        static List<DlgLine> lines;
        static int idx;
        static float chars;
        static System.Action done;

        public static void Reset() { lines = null; done = null; idx = 0; chars = 0f; }

        public static void Begin(List<DlgLine> l, System.Action onDone)
        {
            if (l == null || l.Count == 0) { if (onDone != null) onDone(); return; }
            lines = l;
            idx = 0;
            chars = 0f;
            done = onDone;
            BaseUi.Open(BaseMode.Dialogue);
            if (BaseUi.Mode != BaseMode.Dialogue) { Reset(); if (onDone != null) onDone(); }
        }

        public static void UpdateInput()
        {
            if (lines == null) { BaseUi.Close(); return; }
            var cur = lines[idx];
            int before = (int)chars;
            chars += 38f * Time.unscaledDeltaTime;
            if ((int)chars / 3 != before / 3 && (int)chars < cur.text.Length) Sfx.Play("click", 0.1f, 1.7f);

            if (GameInput.PauseDown) { Finish(); return; }
            if (GameInput.ConfirmDown || GameInput.InteractDown || GameInput.ClickDown)
            {
                if (chars < cur.text.Length) chars = cur.text.Length;
                else
                {
                    idx++;
                    chars = 0f;
                    if (idx >= lines.Count) Finish();
                }
            }
        }

        static void Finish()
        {
            var d = done;
            Reset();
            BaseUi.Close();
            if (d != null) d();
        }

        public static void Draw(Hud h)
        {
            if (lines == null || idx >= lines.Count) return;
            var l = lines[idx];
            Rect box = h.R(110, 500, 1060, 190);
            h.Fill(box, new Color(0.01f, 0.02f, 0.07f, 0.92f));
            h.Outline(box, Cfg.A(l.color, 0.85f), 2f);
            h.Fill(new Rect(box.x, box.y, 6f * h.s, box.height), Cfg.A(l.color, 0.9f));

            Rect tag = h.R(134, 486, 260, 34);
            h.Fill(tag, new Color(0.01f, 0.02f, 0.07f, 1f));
            h.Outline(tag, l.color, 2f);
            h.Txt(tag, l.speaker, 20, l.color, TextAnchor.MiddleCenter);

            int n = Mathf.Clamp((int)chars, 0, l.text.Length);
            h.TxtWrap(h.R(140, 534, 1000, 130), l.text.Substring(0, n), 25, Color.white);

            if (chars >= l.text.Length)
            {
                float a = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
                h.Txt(h.R(900, 662, 250, 22), Cfg.L("[ENTER] tiếp   [ESC] bỏ qua", "[ENTER] next   [ESC] skip"), 14, Cfg.A(Color.white, 0.4f + 0.4f * a), TextAnchor.MiddleRight, false);
            }
        }
    }
}
