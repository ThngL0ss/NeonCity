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
        /// <summary>Có danh sách lựa chọn: sau khi gõ xong câu này người chơi chọn 1 mục (W/S hoặc chuột, ENTER / F để chọn) rồi gọi onChoice(chỉ số).
        /// Trong onChoice gọi DialogueUi.Continue(...) để nói tiếp, hoặc DialogueUi.Close(...) để kết thúc; không gọi gì thì hội thoại kết thúc.</summary>
        public string[] choices;
        public System.Action<int> onChoice;
        /// <summary>Có ô nhập chữ: người chơi gõ rồi ENTER, gọi onInput(chuỗi đã nhập). Quy tắc tiếp theo giống onChoice.</summary>
        public bool input;
        public System.Action<string> onInput;
        /// <summary>Chạy một lần khi câu này bắt đầu hiện (ví dụ trao vật phẩm đúng lúc NPC nói "Ta sẽ cho cậu thứ này").</summary>
        public System.Action onShow;
        public DlgLine(string speaker, string text, Color color, Transform who = null) { this.speaker = speaker; this.text = text; this.color = color; this.who = who; }

        public static DlgLine Ask(string speaker, string text, Color color, Transform who, string[] options, System.Action<int> onChoice)
        {
            return new DlgLine(speaker, text, color, who) { choices = options, onChoice = onChoice };
        }

        public static DlgLine Prompt(string speaker, string text, Color color, Transform who, System.Action<string> onInput)
        {
            return new DlgLine(speaker, text, color, who) { input = true, onInput = onInput };
        }
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
        static int sel;                 // mục đang chọn (câu hỏi có lựa chọn)
        static string typed = "";       // chữ đang nhập (câu hỏi có ô nhập)
        static float fullT;             // thời gian kể từ khi câu hiện đủ chữ (chống bấm nhầm)
        static bool replaced;           // callback đã gọi Continue / Close
        static Vector2 lastMouse;
        static List<DlgLine> shownList;     // danh sách + chỉ số của câu đã chạy onShow
        static int shownIdx = -1;

        public static void Reset() { lines = null; done = null; idx = 0; chars = 0f; sel = 0; typed = ""; fullT = 0f; replaced = false; shownList = null; shownIdx = -1; Cine.ReleaseFocus(); Cine.SetZoom(1f); }

        /// <summary>Đang có hội thoại (đang mở hoặc đang chạy callback).</summary>
        public static bool Running { get { return lines != null; } }

        /// <summary>Dùng trong onChoice / onInput: thay phần hội thoại còn lại bằng danh sách mới (không đóng hộp thoại).</summary>
        public static void Continue(List<DlgLine> l, System.Action onDone = null)
        {
            replaced = true;
            if (l == null || l.Count == 0) { Close(onDone); return; }
            lines = l;
            idx = 0; chars = 0f; sel = 0; typed = ""; fullT = 0f;
            shownList = null; shownIdx = -1;
            done = onDone;
        }

        /// <summary>Dùng trong onChoice / onInput: đóng hộp thoại ngay, rồi chạy after (ví dụ mở cửa hàng, đổi tầng).</summary>
        public static void Close(System.Action after = null)
        {
            done = after;
            Finish();
            replaced = true;            // sau Finish (Finish gọi Reset) để Pick / Submit không đóng thêm lần nữa
        }

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
            sel = 0; typed = ""; fullT = 0f; replaced = false;
            shownList = null; shownIdx = -1;
            done = onDone;
            BaseUi.Open(BaseMode.Dialogue);
            if (BaseUi.Mode != BaseMode.Dialogue) { Reset(); if (onDone != null) onDone(); }
        }

        public static void UpdateInput()
        {
            if (lines == null) { BaseUi.Close(); return; }
            FollowSpeaker();
            var cur = lines[idx];
            if (shownList != lines || shownIdx != idx)
            {
                shownList = lines; shownIdx = idx;
                if (cur.onShow != null) cur.onShow();
                if (lines == null || idx >= lines.Count) return;       // onShow có thể đã đóng / thay hội thoại
                cur = lines[idx];
            }
            int before = (int)chars;
            chars += 38f * Time.unscaledDeltaTime;
            if ((int)chars / 3 != before / 3 && (int)chars < cur.text.Length) Sfx.Play("click", 0.1f, 1.7f);

            bool full = chars >= cur.text.Length;
            if (full) fullT += Time.unscaledDeltaTime; else fullT = 0f;

            if (GameInput.PauseDown) { Finish(); return; }

            if (full && cur.choices != null && cur.choices.Length > 0)
            {
                int n = cur.choices.Length;
                if (GameInput.NavUpDown) { sel = (sel + n - 1) % n; Sfx.Play("click", 0.35f, 1.6f); }
                if (GameInput.NavDownDown) { sel = (sel + 1) % n; Sfx.Play("click", 0.35f, 1.6f); }
                if (fullT > 0.15f && (GameInput.ConfirmDown || GameInput.InteractDown)) Pick(sel);
                return;
            }
            if (full && cur.input) return;          // nhập chữ: xử lý ở Draw (sự kiện bàn phím IMGUI); chỉ ESC thoát

            if (GameInput.ConfirmDown || GameInput.InteractDown || GameInput.ClickDown)
            {
                if (chars < cur.text.Length) chars = cur.text.Length;
                else
                {
                    idx++;
                    chars = 0f;
                    fullT = 0f;
                    if (idx >= lines.Count) Finish();
                }
            }
        }

        static void Pick(int i)
        {
            if (lines == null || idx >= lines.Count) return;
            var cur = lines[idx];
            if (cur.choices == null || i < 0 || i >= cur.choices.Length) return;
            var cb = cur.onChoice;
            Sfx.Play("click", 0.7f, 1.3f);
            replaced = false;
            if (cb != null) cb(i);
            if (!replaced) Finish();
        }

        static void Submit()
        {
            if (lines == null || idx >= lines.Count) return;
            var cur = lines[idx];
            var cb = cur.onInput;
            string t = typed;
            typed = "";
            Sfx.Play("click", 0.7f, 1.3f);
            replaced = false;
            if (cb != null) cb(t);
            if (!replaced) Finish();
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

            bool fullNow = chars >= l.text.Length;
            if (fullNow && l.choices != null && l.choices.Length > 0) { DrawChoices(h, l); return; }
            if (fullNow && l.input) { DrawInput(h, l); return; }

            if (fullNow)
            {
                float a = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
                h.Txt(h.R(780, 586, 290, 20), Cfg.L("[ENTER] tiếp   [ESC] bỏ qua", "[ENTER] next   [ESC] skip"), 13, Cfg.A(Color.white, 0.4f + 0.4f * a), TextAnchor.MiddleRight, false);
            }
        }

        // ------------------------------------------------------------ lựa chọn + nhập chữ
        static void DrawChoices(Hud h, DlgLine l)
        {
            int n = l.choices.Length;
            float rowH = 42f, gap = 6f, w = 470f;
            float x = 600f, y = 586f - n * (rowH + gap) - 4f;
            bool moved = (Event.current.mousePosition - lastMouse).sqrMagnitude > 1f;
            for (int i = 0; i < n; i++)
            {
                Rect r = h.R(x, y + i * (rowH + gap), w, rowH);
                bool hover = r.Contains(Event.current.mousePosition);
                if (hover && moved && Event.current.type == EventType.Repaint) sel = i;
                bool on = sel == i;
                Color c = on ? l.color : Cfg.A(l.color, 0.55f);
                h.Fill(r, new Color(0.01f, 0.02f, 0.07f, 0.93f));
                if (on) h.Fill(r, Cfg.A(l.color, 0.16f));
                h.Outline(r, c, on ? 3f : 2f);
                h.Txt(new Rect(r.x + 16f * h.s, r.y, r.width - 24f * h.s, r.height), (on ? "▶ " : "   ") + l.choices[i], 19, on ? Color.white : Cfg.A(Color.white, 0.8f), TextAnchor.MiddleLeft);
                if (fullT > 0.15f && BaseUi.Clicked(r)) { sel = i; Pick(i); return; }
            }
            if (Event.current.type == EventType.Repaint) lastMouse = Event.current.mousePosition;
            h.Txt(h.R(600, 586, 470, 20), Cfg.L("[W/S] chọn   [ENTER] đồng ý   [ESC] thoát", "[W/S] choose   [ENTER] confirm   [ESC] leave"), 13, Cfg.A(Color.white, 0.55f), TextAnchor.MiddleRight, false);
        }

        static void DrawInput(Hud h, DlgLine l)
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown && fullT > 0.12f)
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    if (typed.Length > 0) { e.Use(); Submit(); return; }
                }
                else if (e.keyCode == KeyCode.Backspace)
                {
                    if (typed.Length > 0) typed = typed.Substring(0, typed.Length - 1);
                    e.Use();
                }
                else if (e.character >= ' ' && e.character != (char)127 && typed.Length < 24)
                {
                    typed += e.character;
                    Sfx.Play("click", 0.12f, 1.9f);
                    e.Use();
                }
            }
            Rect box = h.R(340, 520, 600, 52);
            h.Fill(box, new Color(0.01f, 0.02f, 0.07f, 0.95f));
            h.Outline(box, l.color, 3f);
            bool caret = ((int)(Time.unscaledTime * 2f)) % 2 == 0;
            h.Txt(new Rect(box.x + 16f * h.s, box.y, box.width - 24f * h.s, box.height), typed + (caret ? "_" : " "), 26, Color.white, TextAnchor.MiddleLeft);
            h.Txt(h.R(340, 574, 600, 20), Cfg.L("Gõ câu trả lời rồi bấm [ENTER]   [ESC] thoát", "Type your answer then press [ENTER]   [ESC] leave"), 13, Cfg.A(Color.white, 0.55f), TextAnchor.MiddleCenter, false);
        }
    }
}
