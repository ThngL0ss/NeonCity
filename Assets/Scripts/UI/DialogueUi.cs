using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>Một câu thoại.</summary>
    public class DlgLine
    {
        public string speaker, text;
        public Color color = Cfg.Cyan;
        /// <summary>Người nói (để camera zoom về phía đó). Để trống: tự đoán - "ALBERT" là Albert, còn lại là người chơi.</summary>
        public Transform who;
        public DlgLine(string speaker, string text, Color color, Transform who = null) { this.speaker = speaker; this.text = text; this.color = color; this.who = who; }
    }

    /// <summary>
    /// Hộp thoại nhỏ ở đáy màn hình (không che nhân vật): gõ chữ, ENTER / F / bấm chuột để sang câu kế (hoặc hiện hết câu), ESC bỏ qua cả đoạn.
    /// Trong lúc thoại camera phóng to về phía người đang nói (độ phóng: BaseConfig.dialogueZoom), kết thúc thì trả về bình thường.
    /// </summary>
    public static class DialogueUi
    {
        static List<DlgLine> lines;
        static int idx;
        static float chars;
        static System.Action done;

        public static void Reset() { lines = null; done = null; idx = 0; chars = 0f; Cine.ReleaseFocus(); Cine.SetZoom(1f); }

        // ------------------------------------------------------------ camera
        static Transform Speaker(DlgLine l)
        {
            if (l.who != null) return l.who;
            if (l.speaker != null && l.speaker.ToUpperInvariant() == "ALBERT")
                return BaseManager.Inst != null && BaseManager.Inst.albert != null ? BaseManager.Inst.albert.transform : null;
            return Player.Instance != null ? Player.Instance.transform : null;
        }

        /// <summary>Camera zoom về người đang nói, hơi nghiêng về phía người đối thoại để vẫn thấy cả hai khi họ đứng gần nhau.</summary>
        static void FollowSpeaker()
        {
            var l = lines[idx];
            var who = Speaker(l);
            if (who == null) { Cine.ReleaseFocus(); Cine.SetZoom(1f); return; }
            Transform other = null;
            for (int i = 0; i < lines.Count && other == null; i++)
            {
                var t = Speaker(lines[i]);
                if (t != null && t != who) other = t;
            }
            if (other == null)
            {
                var pl = Player.Instance;
                if (pl != null && pl.transform != who) other = pl.transform;
                else if (BaseManager.Inst != null && BaseManager.Inst.albert != null && BaseManager.Inst.albert.transform != who) other = BaseManager.Inst.albert.transform;
            }
            Vector2 f = (Vector2)who.position + new Vector2(0f, 1.1f);
            if (other != null) f.x += Mathf.Clamp((other.position.x - who.position.x) * 0.3f, -3.2f, 3.2f);
            Cine.Focus(f, 0.7f);
            Cine.SetZoom(BaseConfig.Cur.dialogueZoom);
        }

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
            FollowSpeaker();
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
            Reset();      // cũng trả camera về bình thường
            BaseUi.Close();
            if (d != null) d();
        }

        public static void Draw(Hud h)
        {
            if (lines == null || idx >= lines.Count) return;
            var l = lines[idx];
            // hộp thoại nhỏ ở sát đáy: nằm dưới chân nhân vật nên không che họ
            Rect box = h.R(210, 604, 860, 100);
            h.Fill(box, new Color(0.01f, 0.02f, 0.07f, 0.9f));
            h.Outline(box, Cfg.A(l.color, 0.85f), 2f);
            h.Fill(new Rect(box.x, box.y, 5f * h.s, box.height), Cfg.A(l.color, 0.9f));

            Rect tag = h.R(228, 586, 210, 28);
            h.Fill(tag, new Color(0.01f, 0.02f, 0.07f, 1f));
            h.Outline(tag, l.color, 2f);
            h.Txt(tag, l.speaker, 17, l.color, TextAnchor.MiddleCenter);

            int n = Mathf.Clamp((int)chars, 0, l.text.Length);
            h.TxtWrap(h.R(236, 622, 820, 78), l.text.Substring(0, n), 20, Color.white);

            if (chars >= l.text.Length)
            {
                float a = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
                h.Txt(h.R(780, 586, 290, 20), Cfg.L("[ENTER] tiếp   [ESC] bỏ qua", "[ENTER] next   [ESC] skip"), 13, Cfg.A(Color.white, 0.4f + 0.4f * a), TextAnchor.MiddleRight, false);
            }
        }
    }
}
