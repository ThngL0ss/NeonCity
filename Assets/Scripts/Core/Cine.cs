using System;
using System.Collections;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Hệ cinematic (đoạn phim trong game): thanh đen trên / dưới, thoại gõ chữ, camera bám điểm chỉ định, màn đen mờ dần.
    /// Cách dùng: Cine.Play(kịch_bản) với kịch_bản là một coroutine (IEnumerator), trong đó dùng các lệnh chờ:
    ///   yield return Cine.Wait(1.5f);                       // chờ
    ///   yield return Cine.Say("TÊN", "Câu thoại...", 1.2f); // gõ chữ rồi giữ 1.2 giây
    ///   Cine.SetBars(1f, 0.5f); Cine.SetFade(1f, 1f);       // thanh đen / màn đen (độ đậm, thời gian)
    ///   Cine.Focus(vị_trí, độ_chậm);                        // camera bay tới vị trí (độ chậm lớn = bay chậm hơn)
    /// Người chơi bấm ENTER / SPACE để sang câu thoại kế, ESC để bỏ qua cả đoạn phim (mọi lệnh chờ kết thúc ngay).
    /// Trong lúc chạy, GameManager.State = Cinematic: quái, trụ, bẫy đứng yên; HUD ẩn.
    /// Kịch bản có sẵn (đầu game, lúc chết) nằm ở CineScripts.
    /// </summary>
    public static class Cine
    {
        public static bool Active;
        /// <summary>Độ cao thanh đen 0..1 (1 = đầy đủ 13% màn hình mỗi bên).</summary>
        public static float Bars;
        /// <summary>Độ đậm màn đen 0..1.</summary>
        public static float Fade;
        public static string Speaker = "", Text = "";
        public static float TypedChars;
        public static bool HasFocus;
        public static Vector2 FocusPos;
        public static float CamSmooth = 1f;
        public static bool CanSkip = true;
        public static bool Skipping { get; private set; }
        public static bool Advance { get; private set; }

        static float barsTarget, barsSpeed, fadeTarget, fadeSpeed;
        static float startedAt;
        static Coroutine running;

        public static void ResetStatics()
        {
            Active = false;
            Bars = Fade = barsTarget = fadeTarget = 0f;
            barsSpeed = fadeSpeed = 0f;
            Speaker = Text = "";
            TypedChars = 0f;
            HasFocus = false;
            CamSmooth = 1f;
            CanSkip = true;
            Skipping = Advance = false;
            running = null;
        }

        /// <summary>Gọi khi một Scene mới nạp xong. keepFade = true: giữ màn đen (để mờ dần vào Scene mới khi chuyển cảnh).</summary>
        public static void NewScene(bool keepFade)
        {
            float f = keepFade ? Fade : 0f;
            ResetStatics();
            Fade = f;
            fadeTarget = 0f;
            fadeSpeed = 0.9f;
        }

        // ------------------------------------------------------------ chạy kịch bản
        public static void Play(IEnumerator script, bool skippable = true)
        {
            if (GameManager.Inst == null || script == null) return;
            if (running != null) GameManager.Inst.StopCoroutine(running);
            running = GameManager.Inst.StartCoroutine(Run(script, skippable));
        }

        static IEnumerator Run(IEnumerator script, bool skippable)
        {
            Active = true;
            CanSkip = skippable;
            Skipping = Advance = false;
            startedAt = Time.unscaledTime;
            Speaker = Text = "";
            TypedChars = 0f;
            yield return script;
            End();
        }

        /// <summary>Kết thúc đoạn phim: thả camera, hạ thanh đen.</summary>
        public static void End()
        {
            Active = false;
            HasFocus = false;
            CamSmooth = 1f;
            Skipping = Advance = false;
            Speaker = Text = "";
            TypedChars = 0f;
            barsTarget = 0f; barsSpeed = 2f;
            running = null;
            if (CameraRig.Inst != null) CameraRig.Inst.Snap();
        }

        /// <summary>GameManager gọi mỗi khung hình (thời gian thật, không bị dừng bởi hit-stop).</summary>
        public static void Tick()
        {
            float dt = Time.unscaledDeltaTime;
            Advance = false;
            if (Active)
            {
                if (Time.unscaledTime - startedAt > 0.5f)
                {
                    if (CanSkip && GameInput.PauseDown) Skipping = true;
                    if (GameInput.ConfirmDown || GameInput.ClickDown) Advance = true;
                }
            }
            else if (fadeSpeed <= 0f && Fade > 0f) { fadeTarget = 0f; fadeSpeed = 0.9f; }

            if (Skipping) { Bars = barsTarget; Fade = fadeTarget; }
            else
            {
                if (barsSpeed > 0f) Bars = Mathf.MoveTowards(Bars, barsTarget, barsSpeed * dt);
                if (fadeSpeed > 0f) Fade = Mathf.MoveTowards(Fade, fadeTarget, fadeSpeed * dt);
            }
        }

        public static void SetBars(float target, float dur)
        {
            barsTarget = Mathf.Clamp01(target);
            barsSpeed = dur <= 0.001f ? 999f : Mathf.Abs(barsTarget - Bars) / dur;
        }

        public static void SetFade(float target, float dur)
        {
            fadeTarget = Mathf.Clamp01(target);
            fadeSpeed = dur <= 0.001f ? 999f : Mathf.Abs(fadeTarget - Fade) / dur;
        }

        public static void Focus(Vector2 pos, float slowness = 1f)
        {
            HasFocus = true;
            FocusPos = pos;
            CamSmooth = Mathf.Max(0.2f, slowness);
        }

        public static void ReleaseFocus() { HasFocus = false; }

        // ------------------------------------------------------------ lệnh chờ
        /// <summary>Chờ (giây thời gian thật). Bỏ qua / ESC thì chờ xong ngay.</summary>
        public static IEnumerator Wait(float sec)
        {
            float t = 0f;
            while (t < sec && !Skipping)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        /// <summary>Gõ chữ từng ký tự rồi giữ hold giây. ENTER / SPACE: hiện hết câu, bấm lần nữa: sang câu kế.</summary>
        public static IEnumerator Say(string speaker, string text, float hold = 1.2f)
        {
            Speaker = speaker;
            Text = text;
            TypedChars = 0f;
            const float cps = 34f;
            int lastTick = 0;
            while (TypedChars < text.Length && !Skipping)
            {
                TypedChars += cps * Time.unscaledDeltaTime;
                if (Advance) TypedChars = text.Length;
                int n = (int)TypedChars;
                if (n >= lastTick + 3 && n < text.Length) { lastTick = n; Sfx.Play("click", 0.12f, 1.9f); }
                yield return null;
            }
            TypedChars = text.Length;
            float t = 0f;
            yield return null;                      // bỏ qua lần bấm vừa hiện hết câu
            while (t < hold && !Skipping && !Advance)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        public static void ClearText() { Speaker = ""; Text = ""; TypedChars = 0f; }

        /// <summary>Thoại đang hiển thị (đã cắt theo số ký tự đã gõ).</summary>
        public static string TypedText
        {
            get
            {
                if (string.IsNullOrEmpty(Text)) return "";
                int n = Mathf.Clamp((int)TypedChars, 0, Text.Length);
                return Text.Substring(0, n);
            }
        }
    }
}
