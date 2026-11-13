using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Cảnh cốt truyện mở đầu (Scene Story, sau khi chọn "Tất nhiên là xem rồi" ở menu Trò chơi mới): 10 cảnh, mỗi cảnh một hình + một đoạn chữ.
    /// Cuối cùng gọi GameFlow.FinishStory() để sang màn hướng dẫn. ESC = bỏ qua cả cảnh · ENTER / bấm chuột = qua đoạn chữ kế.
    ///
    /// Cách hoạt động: Scene do menu Editor dựng sẵn, mỗi cảnh là một object con của "Story" (S1_Bunker, S2_Nancelas, S3_Core, S4_Alert, S5_HallA, S5_HallB,
    /// S5_Machine, S7a_Ruins, S7b_Crowd, S8_War, S9_Evo). Script chỉ bật / tắt các object này, chạy hoạt ảnh (camera trượt, rung, chớp, hạt) và vẽ khung chữ.
    /// Khung chữ nằm ở 22% dưới màn hình, hình vẽ chỉ dùng 78% trên nên chữ không đè lên hình.
    /// Toạ độ trong code: Fp(px, py) = pixel trong khung 480 x 210 (gốc trên trái) -> toạ độ thế giới; FpW cho cảnh rộng hơn 480 px.
    /// Chữ ở StoryTexts.cs.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class StoryIntro : MonoBehaviour
    {
        [Header("Sprite hiệu ứng (menu Editor gán sẵn)")]
        public Sprite dot;               // chấm trắng 4x4: giấy pháo hoa, tia lửa, hạt (tô màu bằng code)
        public Sprite[] bolts;           // 3 tia sét nhỏ (gốc ở đỉnh)
        public Sprite boltBig;           // tia sét khổng lồ phủ gần hết khung
        public Sprite[] rifts;           // (không còn dùng: khe nứt cảnh 5c giờ vẽ bằng code — xem RiftLayout / MakeRiftSprite; giữ lại để Scene cũ không mất tham chiếu)
        public Sprite rain;              // hạt mưa

        const float CamY = -1.875f;      // camera hạ thấp để khung hình 480x210 nằm sát mép trên, 60 px dưới là khung chữ
        const float HallPx = 960f;
        const float WarPx = 1280f;
        const float TypeSpeed = 38f;     // ký tự / giây

        // ----- trạng thái giao diện
        Hud hud;
        string error;
        bool errorLogged;
        GUIStyle measure;
        int wrapH = -1;
        bool needWrap;
        string full = "", wrapped = "";
        int shown;
        bool panelOn, centerOn, ready;
        float panelA;
        Color ovCol = Color.black;
        float ovA = 1f;
        float flashA;
        Color flashCol = Color.white;
        int busy;
        bool finishing;

        // ----- camera
        Transform camT;
        float camX;
        float shakeAmp, shakeDecay = 30f;

        // ----- cảnh
        readonly List<Transform> sceneRoots = new List<Transform>();
        Transform fxRoot;

        // ----- hạt
        class Part
        {
            public Transform t;
            public SpriteRenderer sr;
            public Vector2 v;
            public float age, life, grav, spin;
            public Color c;
            public bool fade;
        }
        readonly List<Part> parts = new List<Part>();

        // ----- cờ điều khiển các vòng lặp nền
        bool embers, hallFx, rainOn, carsOn, sparking, dashing;

        // ============================================================== khởi tạo
        void Awake()
        {
            Art.Init();
            GameManager.State = GameState.Cinematic;
            GameManager.Paused = false;
            Time.timeScale = 1f;
            Cine.SetFade(0f, 0f);

            var cam = Camera.main;
            if (cam == null) { var ps = FindObjectOfType<PixelScreen>(); if (ps != null) cam = ps.GetComponent<Camera>(); }
            camT = cam != null ? cam.transform : null;

            hud = gameObject.AddComponent<Hud>();            // chỉ để dùng hàm vẽ (Fill, Txt); tắt OnGUI của nó
            hud.enabled = false;

            foreach (Transform c in transform) sceneRoots.Add(c);
            var fxGo = new GameObject("_FX");
            fxGo.transform.SetParent(transform, false);
            fxRoot = fxGo.transform;

            Show();
        }

        void Start()
        {
            StartCoroutine(Embers());
            StartCoroutine(Run());
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            TickParts(dt);
            panelA = Mathf.MoveTowards(panelA, panelOn ? 1f : 0f, dt * 4f);
            flashA = Mathf.MoveTowards(flashA, 0f, dt * 2.6f);
            if (!finishing && GameInput.PauseDown)
            {
                StopAllCoroutines();
                StartCoroutine(Finish(0.45f));
            }
        }

        void LateUpdate()
        {
            shakeAmp = Mathf.MoveTowards(shakeAmp, 0f, shakeDecay * Time.unscaledDeltaTime);
            Vector2 o = shakeAmp > 0.01f ? new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * (shakeAmp / 16f) : Vector2.zero;
            if (camT != null) camT.position = new Vector3(Snap(camX + o.x), Snap(CamY + o.y), -10f);
        }

        static float Snap(float v) { return Mathf.Round(v * 16f) / 16f; }

        // ============================================================== toạ độ
        /// <summary>Pixel trong khung 480 x 210 (gốc trên trái) -> toạ độ thế giới (khung nằm giữa gốc toạ độ).</summary>
        public static Vector2 Fp(float px, float py) { return new Vector2((px - 240f) / 16f, (105f - py) / 16f); }

        /// <summary>Giống Fp nhưng cho ảnh rộng w px (hầm 960, chiến tranh 1280).</summary>
        public static Vector2 FpW(float px, float py, float w) { return new Vector2((px - w * 0.5f) / 16f, (105f - py) / 16f); }

        // ============================================================== tiện ích cảnh
        Transform T(string path)
        {
            var t = transform.Find(path);
            if (t == null) Debug.LogWarning("Neon Breaker (Story): thiếu object '" + path + "' — chạy lại menu Editor hoặc xoá Story.unity rồi dựng lại.");
            return t;
        }

        SpriteRenderer SR(string path)
        {
            var t = T(path);
            return t != null ? t.GetComponent<SpriteRenderer>() : null;
        }

        SpriteAnimator Anim(string path)
        {
            var t = T(path);
            return t != null ? t.GetComponent<SpriteAnimator>() : null;
        }

        void Show(params string[] names)
        {
            ClearFx();
            embers = false;
            for (int i = 0; i < sceneRoots.Count; i++)
            {
                var r = sceneRoots[i];
                if (r != null) r.gameObject.SetActive(System.Array.IndexOf(names, r.name) >= 0);
            }
        }

        void SetCam(float x) { camX = x; }

        // ============================================================== hạt + hiệu ứng thoáng qua
        Part Spawn(Sprite s, Vector2 pos, Vector2 vel, float life, Color c, float grav, int order, bool additive, float scale, float rotZ, float spin, bool fade)
        {
            if (s == null) return null;
            var sr = Art.Spr(fxRoot, "p", s, pos, new Vector2(scale, scale), c, order, additive, rotZ);
            var p = new Part { t = sr.transform, sr = sr, v = vel, life = life, grav = grav, c = c, spin = spin, fade = fade };
            parts.Add(p);
            return p;
        }

        void TickParts(float dt)
        {
            for (int i = parts.Count - 1; i >= 0; i--)
            {
                var p = parts[i];
                if (p.t == null) { parts.RemoveAt(i); continue; }
                p.age += dt;
                if (p.age >= p.life) { Destroy(p.t.gameObject); parts.RemoveAt(i); continue; }
                p.v.y -= p.grav * dt;
                p.t.position += new Vector3(p.v.x * dt, p.v.y * dt, 0f);
                if (p.spin != 0f) p.t.Rotate(0f, 0f, p.spin * dt);
                if (p.fade) { var c = p.c; c.a *= 1f - p.age / p.life; p.sr.color = c; }
            }
        }

        void ClearFx()
        {
            parts.Clear();
            if (fxRoot == null) return;
            for (int i = fxRoot.childCount - 1; i >= 0; i--) Destroy(fxRoot.GetChild(i).gameObject);
        }

        void Flash(float a, Color c)
        {
            flashCol = c;
            flashA = Mathf.Max(flashA, a);
        }

        // ============================================================== đồng hồ, mờ / sáng, chớp
        static float Dt { get { return Time.unscaledDeltaTime; } }

        IEnumerator Wait(float s)
        {
            float t = 0f;
            while (t < s) { t += Dt; yield return null; }
        }

        IEnumerator FadeTo(float a, float dur)
        {
            float a0 = ovA, t = 0f;
            if (dur <= 0f) { ovA = a; yield break; }
            while (t < dur) { t += Dt; ovA = Mathf.Lerp(a0, a, Mathf.Clamp01(t / dur)); yield return null; }
            ovA = a;
        }

        IEnumerator FadeOut(float dur)
        {
            panelOn = false;
            ovCol = Color.black;
            yield return FadeTo(1f, dur);
        }

        /// <summary>Chớp đen: tối rất nhanh, đổi cảnh (mid) lúc tối hẳn, giữ đen một chút rồi sáng lại.</summary>
        IEnumerator BlackBlink(float hold, System.Action mid)
        {
            panelOn = false;
            ovCol = Color.black;
            yield return FadeTo(1f, 0.07f);
            if (mid != null) mid();
            yield return Wait(hold);
            yield return FadeTo(0f, 0.15f);
        }

        /// <summary>Màn hình chớp nháy nhanh (đen / sáng xen kẽ), đổi cảnh (mid) khi đang tối.</summary>
        IEnumerator Flicker(float dur, System.Action mid)
        {
            panelOn = false;
            float t = 0f, next = 0f;
            bool on = false, done = false;
            while (t < dur)
            {
                t += Dt;
                if (t >= next)
                {
                    on = !on;
                    next = t + 0.055f;
                    ovCol = on ? Color.black : new Color(0.8f, 0.7f, 1f);
                    ovA = on ? 1f : 0.35f;
                }
                if (!done && on && t > dur * 0.5f) { done = true; if (mid != null) mid(); }
                yield return null;
            }
            if (!done && mid != null) mid();
            ovA = 0f;
        }

        // ============================================================== chữ
        static bool Advance() { return GameInput.ConfirmDown || GameInput.ClickDown; }

        void SetText(string s)
        {
            full = s;
            wrapped = "";
            shown = 0;
            needWrap = true;
        }

        void Run(IEnumerator inner) { StartCoroutine(Par(inner)); }

        IEnumerator Par(IEnumerator inner)
        {
            busy++;
            yield return StartCoroutine(inner);
            busy--;
        }

        /// <summary>Gõ chữ từng ký tự rồi chờ người chơi bấm ENTER / chuột. center = chữ giữa màn hình đen, không khung (cảnh 10).</summary>
        IEnumerator Say(string[] pair, bool center = false, float autoAfter = -1f)
        {
            yield return null;                          // chờ 1 khung để phím vừa bấm ở đoạn trước không bị tính lại
            SetText(Cfg.L(pair[0], pair[1]));
            centerOn = center;
            panelOn = !center;
            while (needWrap) yield return null;         // OnGUI ngắt dòng xong mới gõ
            float acc = 0f;
            while (shown < wrapped.Length)
            {
                if (Advance()) { shown = wrapped.Length; yield return null; break; }
                acc += Dt * TypeSpeed;
                int n = (int)acc;
                if (n > 0)
                {
                    acc -= n;
                    int old = shown;
                    shown = Mathf.Min(wrapped.Length, shown + n);
                    if (shown / 3 != old / 3) Sfx.Play("click", 0.12f, 1.9f);
                }
                yield return null;
            }
            while (busy > 0) yield return null;         // hoạt ảnh song song của cảnh phải xong thì mới cho qua
            ready = true;
            float w = 0f;
            while (true)
            {
                w += Dt;
                if (Advance()) break;
                if (autoAfter > 0f && w >= autoAfter) break;
                yield return null;
            }
            ready = false;
        }

        IEnumerator Finish(float fade)
        {
            finishing = true;
            panelOn = false;
            centerOn = false;
            ready = false;
            ovCol = Color.black;
            yield return FadeTo(1f, fade);
            GameFlow.FinishStory();
        }

        // ============================================================== KỊCH BẢN
        IEnumerator Run()
        {
            yield return null;
            yield return Scene1();
            yield return Scene2();
            yield return Scene3();
            yield return Scene4();
            yield return Scene5a();
            yield return Scene5b();
            yield return Scene5c();
            yield return Scene6();
            yield return Scene7();
            yield return Scene8();
            yield return Scene9();
            yield return Scene10();
            yield return Finish(0.6f);
        }

        // ------------------------------------------------------------ cảnh 1: hầm công nghệ, cỗ máy xong
        IEnumerator Scene1()
        {
            Show("S1_Bunker");
            SetCam(0f);
            ovCol = Color.black;
            ovA = 1f;
            yield return Wait(0.8f);
            yield return FadeTo(0f, 1.4f);
            yield return Wait(0.4f);
            yield return Say(StoryTexts.S1);
            panelOn = false;
            yield return Wait(0.5f);
            yield return Celebrate();
            yield return FadeOut(1.2f);
        }

        /// <summary>Hết chữ: cỗ máy phát ánh sáng tím cực mạnh, các nhà khoa học vỗ tay, nhảy múa, giấy pháo hoa tung bay.</summary>
        IEnumerator Celebrate()
        {
            var glow = SR("S1_Bunker/glow");
            Sfx.Play("charge", 0.9f, 0.7f);
            float t = 0f;
            while (t < 0.9f)
            {
                t += Dt;
                float k = Mathf.Clamp01(t / 0.9f);
                if (glow != null) { glow.color = new Color(1f, 1f, 1f, k); glow.transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.35f, k); }
                Flash(Mathf.Sin(k * Mathf.PI) * 0.5f, new Color(0.72f, 0.45f, 1f));
                shakeAmp = 2f * k;
                yield return null;
            }
            Sfx.Play("victory", 0.6f, 1.2f);
            var scis = T("S1_Bunker/scis");
            if (scis != null)
                foreach (var a in scis.GetComponentsInChildren<SpriteAnimator>())
                    StartCoroutine(PlayLater(a, "cheer", Random.value * 0.35f));
            t = 0f;
            float acc = 0f;
            while (t < 3.2f)
            {
                t += Dt;
                if (glow != null) glow.color = new Color(1f, 1f, 1f, 0.78f + 0.22f * Mathf.Sin(t * 9f));
                acc += Dt * 55f;
                while (acc >= 1f) { acc -= 1f; Confetti(); }
                yield return null;
            }
        }

        IEnumerator PlayLater(SpriteAnimator a, string clip, float delay)
        {
            yield return Wait(delay);
            if (a != null) a.Play(clip, true);
        }

        void Confetti()
        {
            Color[] cols = { Cfg.Cyan, Cfg.Magenta, Cfg.Yellow, Cfg.Lime, Cfg.Orange, Color.white };
            Vector2 p = Fp(Random.Range(0f, 480f), Random.Range(-16f, 4f));
            Spawn(dot, p, new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(-4.5f, -1.5f)), Random.Range(3.5f, 5f),
                  cols[Random.Range(0, cols.Length)], 0f, 60, false, Random.Range(0.7f, 1.3f), Random.Range(0f, 90f), Random.Range(-240f, 240f), false);
        }

        // ------------------------------------------------------------ cảnh 2: Nancelas một mình sửa máy
        IEnumerator Scene2()
        {
            Show("S2_Nancelas");
            SetCam(0f);
            var nan = Anim("S2_Nancelas/nan");
            if (nan != null) nan.Play("work", true);
            ovCol = Color.black;
            yield return Wait(0.5f);
            yield return FadeTo(0f, 1.0f);
            sparking = true;
            StartCoroutine(SparkLoop());
            yield return Say(StoryTexts.S2);
            sparking = false;
            yield return Flicker(0.9f, () => Show("S3_Core"));       // chớp nháy nhanh -> cảnh 3
        }

        IEnumerator SparkLoop()
        {
            var nan = T("S2_Nancelas/nan");
            while (sparking)
            {
                if (nan != null)
                {
                    Vector2 tip = (Vector2)nan.position + new Vector2(14f / 16f, 28f / 16f);
                    for (int i = 0; i < 4; i++)
                        Spawn(dot, tip, new Vector2(Random.Range(-2.5f, 2.5f), Random.Range(0.5f, 3.5f)), Random.Range(0.3f, 0.6f),
                              Random.value < 0.5f ? Cfg.Yellow : Color.white, 9f, 60, true, Random.Range(0.4f, 0.8f), 0f, 0f, true);
                }
                yield return Wait(Random.Range(0.1f, 0.35f));
            }
        }

        // ------------------------------------------------------------ cảnh 3: thay lõi vàng bằng lõi đen
        IEnumerator Scene3()
        {
            Run(S3Anim());
            yield return Say(StoryTexts.S3);
            yield return BlackBlink(0.25f, () => Show("S4_Alert"));     // chớp đen -> cảnh 4
        }

        void S3Panel(string state)
        {
            string[] all = { "closed", "yellow", "empty", "black" };
            for (int i = 0; i < all.Length; i++)
            {
                var t = T("S3_Core/panel_" + all[i]);
                if (t != null) t.gameObject.SetActive(all[i] == state);
            }
        }

        void S3Nan(bool hold)
        {
            var a = T("S3_Core/nan_reach");
            var b = T("S3_Core/nan_hold");
            if (a != null) a.gameObject.SetActive(!hold);
            if (b != null) b.gameObject.SetActive(hold);
        }

        IEnumerator S3Anim()
        {
            Vector2 socket = Fp(300f, 105f), handReach = Fp(240f, 169f), handHold = Fp(237f, 175f);
            var cy = T("S3_Core/core_y");
            var cb = T("S3_Core/core_b");
            if (cy != null) cy.gameObject.SetActive(false);
            if (cb != null) cb.gameObject.SetActive(false);
            S3Panel("closed");
            S3Nan(false);
            yield return Wait(1.0f);
            Sfx.Play("slam", 0.4f, 1.6f);
            S3Panel("yellow");                           // nắp khoang mở ra, lõi vàng lộ ra
            shakeAmp = 1.5f;
            yield return Wait(1.3f);
            Sfx.Play("pickup", 0.7f, 0.8f);              // lấy lõi vàng ra
            S3Panel("empty");
            S3Nan(true);
            if (cy != null) { cy.gameObject.SetActive(true); yield return MoveObj(cy, socket, handHold, 0.8f, 0.8f); }
            yield return Wait(0.9f);
            if (cy != null) StartCoroutine(FallFade(cy));          // lõi vàng bị bỏ rơi
            if (cb != null) { cb.gameObject.SetActive(true); cb.position = handHold; }
            Sfx.Play("phase", 0.6f, 0.6f);
            yield return Wait(1.0f);
            S3Nan(false);                                // đặt lõi đen tuyền vào
            if (cb != null) yield return MoveObj(cb, handHold, socket, 0.8f, 0.6f);
            Sfx.Play("slam", 0.6f, 0.6f);
            if (cb != null) cb.gameObject.SetActive(false);
            S3Panel("black");
            shakeAmp = 4f;
            Flash(0.5f, new Color(0.4f, 0.1f, 0.7f));
            yield return Wait(0.3f);
        }

        IEnumerator MoveObj(Transform t, Vector2 from, Vector2 to, float dur, float arc)
        {
            float e = 0f;
            while (e < dur)
            {
                e += Dt;
                float k = Mathf.Clamp01(e / dur);
                Vector2 p = Vector2.Lerp(from, to, k * k * (3f - 2f * k)) + Vector2.up * Mathf.Sin(k * Mathf.PI) * arc;
                if (t != null) t.position = new Vector3(p.x, p.y, 0f);
                yield return null;
            }
            if (t != null) t.position = new Vector3(to.x, to.y, 0f);
        }

        IEnumerator FallFade(Transform t)
        {
            if (t == null) yield break;
            var sr = t.GetComponent<SpriteRenderer>();
            float e = 0f;
            Vector3 p0 = t.position;
            while (e < 0.7f)
            {
                e += Dt;
                float k = e / 0.7f;
                t.position = p0 + Vector3.down * (k * k * 2.4f);
                if (sr != null) sr.color = new Color(1f, 1f, 1f, 1f - k);
                yield return null;
            }
            t.gameObject.SetActive(false);
            if (sr != null) sr.color = Color.white;
        }

        // ------------------------------------------------------------ cảnh 4: các nhà khoa học phát hiện bất thường ("!")
        IEnumerator Scene4()
        {
            SetCam(0f);
            var scis = T("S4_Alert/scis");
            var ex = T("S4_Alert/excls");
            if (scis != null && ex != null) Run(S4Anim(scis, ex));
            yield return Say(StoryTexts.S4);
            yield return FadeOut(1.1f);
            yield return Wait(3f);                       // đen và cứ đen trong 3 giây
        }

        IEnumerator S4Anim(Transform scis, Transform ex)
        {
            int n = scis.childCount;
            var start = new Vector3[n];
            var goal = new float[n];
            for (int i = 0; i < n; i++)
            {
                start[i] = scis.GetChild(i).position;
                goal[i] = start[i].x < 0f ? Fp(150f - (i % 3) * 20f, 0f).x : Fp(330f + (i % 3) * 20f, 0f).x;
            }
            for (int i = 0; i < ex.childCount; i++) ex.GetChild(i).gameObject.SetActive(false);
            float t = 0f;
            bool alert = false;
            while (t < 4.4f)
            {
                t += Dt;
                float k = Mathf.Clamp01(t / 3.4f);
                float e = k * (2f - k);
                for (int i = 0; i < n; i++)
                {
                    var tr = scis.GetChild(i);
                    var p = tr.position;
                    p.x = Mathf.Lerp(start[i].x, goal[i], e);
                    p.y = start[i].y + (k < 1f ? Mathf.Abs(Mathf.Sin(t * 11f + i)) * 0.12f : 0f);
                    tr.position = p;
                    if (alert && i < ex.childCount) ex.GetChild(i).position = tr.position + new Vector3(0.5f, 1.9f, 0f);
                }
                if (!alert && t > 2.2f)
                {
                    alert = true;
                    Sfx.Play("warn", 0.5f, 1.3f);
                    for (int i = 0; i < ex.childCount; i++) ex.GetChild(i).gameObject.SetActive(true);
                }
                yield return null;
            }
        }

        // ------------------------------------------------------------ cảnh 5a: hầm đổ nát, các robot khổng lồ
        IEnumerator Scene5a()
        {
            Show("S5_HallA", "S5_Machine");
            SetCam(HallRight);
            embers = true;
            ovCol = Color.black;
            yield return FadeTo(0f, 1.0f);
            yield return Say(StoryTexts.S5a);
            yield return FadeOut(1.0f);
        }

        // phía phải của hầm 960 px (px 480..960) lúc đầu, phía trái (px 0..480, có cỗ máy ở px 240) lúc cuối
        const float HallRight = 15f;
        const float HallLeft = -15f;

        // ------------------------------------------------------------ cảnh 5b: hắn gục ngã; camera lao sang trái tới cỗ máy
        IEnumerator Scene5b()
        {
            Show("S5_HallB", "S5_Machine");
            SetCam(HallRight);
            embers = true;
            ovCol = Color.black;
            yield return FadeTo(0f, 1.0f);
            yield return Say(StoryTexts.S5b);
            panelOn = false;
            Sfx.Play("dash", 0.5f, 0.5f);
            yield return PanAccel(HallRight, HallLeft, 3.2f);
            shakeAmp = 4f;
            yield return Wait(0.6f);
        }

        IEnumerator PanAccel(float from, float to, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Dt;
                float k = Mathf.Clamp01(t / dur);
                camX = Mathf.Lerp(from, to, k * k * k);          // nhanh dần
                shakeAmp = Mathf.Max(shakeAmp, 3f * k);
                yield return null;
            }
            camX = to;
        }

        IEnumerator PanSmooth(float from, float to, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Dt;
                float k = Mathf.Clamp01(t / dur);
                camX = Mathf.Lerp(from, to, k * k * (3f - 2f * k));
                yield return null;
            }
            camX = to;
        }

        // ------------------------------------------------------------ cảnh 5c: cỗ máy rung, tia vật chất đen, sét, khe nứt không gian
        IEnumerator Scene5c()
        {
            hallFx = true;
            StartCoroutine(HallFx());
            yield return Say(StoryTexts.S5c);
            panelOn = false;
            yield return BigStrike();
        }

        IEnumerator HallFx()
        {
            var m = T("S5_Machine/machine");
            var glow = SR("S5_Machine/glow");
            var rays = T("S5_Machine/rays");
            Vector3 mBase = m != null ? m.localPosition : Vector3.zero;
            Vector2 ring = FpW(240f, 86f, HallPx);
            float t = 0f, nextBolt = 1.0f;
            BuildRiftSprites();
            Run(RiftSequence(ring));                 // mở lần lượt 8 khe nứt theo bố cục cố định; Say chờ xong rồi mới cho bấm qua đoạn kế
            while (hallFx)
            {
                t += Dt;
                float k = Mathf.Clamp01(t / 7f);
                if (m != null) m.localPosition = mBase + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * (0.02f + 0.1f * k);
                if (glow != null) glow.color = new Color(0.7f, 0.4f, 1f, (0.18f + 0.5f * k) * (0.7f + 0.3f * Random.value));
                if (rays != null)
                {
                    for (int i = 0; i < rays.childCount; i++)
                    {
                        var r = rays.GetChild(i);
                        r.Rotate(0f, 0f, (i % 2 == 0 ? 14f : -10f) * Dt);
                        var sr = r.GetComponent<SpriteRenderer>();
                        if (sr != null) sr.color = new Color(1f, 1f, 1f, Random.value < 0.25f + 0.55f * k ? 0.95f : 0.12f);
                    }
                }
                if (t >= nextBolt)
                {
                    nextBolt = t + Mathf.Lerp(1.3f, 0.35f, k);
                    Strike(ring);
                }
                yield return null;
            }
            if (m != null) m.localPosition = mBase;
        }

        // ---- Sét "nền": giáng theo các "ô" ngang cách nhau 3 đơn vị (một tia rộng ~2,5) trải khắp khung hình. Mỗi lần chọn ô khác với vài lần vừa rồi
        // và KHÔNG gần (dưới 3,1 đơn vị) tia nào vừa giáng trong 0,5 giây qua (kể cả tia mở khe nứt), nên hai tia không bao giờ đè lên nhau.
        static readonly float[] StrikeSlots = { -13.5f, -10.5f, -7.5f, -4.5f, -1.5f, 1.5f, 4.5f, 7.5f, 10.5f, 13.5f };
        readonly List<int> recentSlots = new List<int>();
        readonly List<Vector2> boltLog = new List<Vector2>();           // (x, thời điểm) các tia sét gần đây

        void LogBolt(float x)
        {
            boltLog.Add(new Vector2(x, Time.unscaledTime));
            if (boltLog.Count > 12) boltLog.RemoveAt(0);
        }

        bool BoltClear(float x)
        {
            float now = Time.unscaledTime;
            for (int i = 0; i < boltLog.Count; i++)
                if (now - boltLog[i].y < 0.5f && Mathf.Abs(boltLog[i].x - x) < 3.1f) return false;
            return true;
        }

        int PickSlot(float ringX)
        {
            var ok = new List<int>(StrikeSlots.Length);
            for (int i = 0; i < StrikeSlots.Length; i++)
            {
                if (recentSlots.Contains(i)) continue;
                if (dashing && Mathf.Abs(StrikeSlots[i]) < 7f) continue;      // robot đang lao vào cổng: sét chỉ giáng ở xa, không che nó
                if (!BoltClear(ringX + StrikeSlots[i])) continue;
                ok.Add(i);
            }
            if (ok.Count == 0) return -1;
            int s = ok[Random.Range(0, ok.Count)];
            recentSlots.Add(s);
            if (recentSlots.Count > 4) recentSlots.RemoveAt(0);
            return s;
        }

        /// <summary>Một tia sét giáng xuống điểm p (đầu tia chạm p) + tiếng + chớp + rung (không chớp / rung khi robot đang lao).</summary>
        void BoltAt(Vector2 p)
        {
            if (bolts != null && bolts.Length > 0)
                Spawn(bolts[Random.Range(0, bolts.Length)], p + new Vector2(0f, 6.9f), Vector2.zero, 0.18f, Color.white, 0f, 70, true, 1f, 0f, 0f, false);
            Sfx.Play("bolt", dashing ? 0.3f : 0.45f, Random.Range(0.6f, 1.1f));
            if (!dashing)
            {
                Flash(0.3f, new Color(0.85f, 0.75f, 1f));
                shakeAmp = Mathf.Max(shakeAmp, 3f);
            }
        }

        void Strike(Vector2 ring)
        {
            int slot = PickSlot(ring.x);
            if (slot < 0) return;
            float x = ring.x + StrikeSlots[slot] + Random.Range(-0.2f, 0.2f);
            LogBolt(x);
            BoltAt(new Vector2(x, ring.y + Random.Range(-3.2f, 2.0f)));
        }

        // ============================================================== khe nứt không gian (cảnh 5c)
        // Bố cục CỐ ĐỊNH, tính sẵn để: có khe dọc, chéo (hai chiều) và ngang; không khe nào chạm / đè khe khác (cách nhau ≥ 0,7 đơn vị);
        // nằm trên đầu các robot, hai bên cỗ máy (không che cỗ máy). Toạ độ (x, y) tính từ tâm cỗ máy, y hướng LÊN, đơn vị thế giới (16 px).
        // Khe được VẼ BẰNG CODE (Texture2D) theo đúng hướng nên không cần sprite / không cần dựng lại Scene, và chắc chắn hiện đủ 8 khe.
        struct RiftSpec
        {
            public float x, y, ang, len, wid;
            public int seed;
            public RiftSpec(float x, float y, float ang, float len, float wid, int seed) { this.x = x; this.y = y; this.ang = ang; this.len = len; this.wid = wid; this.seed = seed; }
        }

        static readonly RiftSpec[] RiftLayout =
        {
            new RiftSpec(-13.0f, 0.9f,  93f, 3.9f, 1.5f, 11),     // 0 dọc, mép trái
            new RiftSpec( -8.1f, 3.3f,  36f, 4.4f, 1.4f, 12),     // 1 chéo ↗
            new RiftSpec( -9.0f, 0.1f,   3f, 5.4f, 1.3f, 13),     // 2 ngang
            new RiftSpec(-11.8f, 3.6f, 128f, 3.4f, 1.2f, 14),     // 3 chéo ↖
            new RiftSpec(  9.9f, 4.0f,  -5f, 6.0f, 1.3f, 15),     // 4 ngang, trên cao
            new RiftSpec( 13.2f, 0.6f,  88f, 3.8f, 1.5f, 16),     // 5 dọc, mép phải
            new RiftSpec( 10.4f, 0.9f, 128f, 4.0f, 1.3f, 17),     // 6 chéo ↖
            new RiftSpec(  7.5f, 0.6f,  60f, 3.4f, 1.1f, 18),     // 7 chéo ↗
        };
        // thứ tự mở: xen kẽ trái / phải, dọc / chéo / ngang cho khỏi thành hàng
        static readonly int[] RiftOrder = { 4, 0, 6, 1, 5, 2, 7, 3 };

        readonly List<Sprite> riftSprites = new List<Sprite>();
        readonly List<Texture2D> riftTex = new List<Texture2D>();

        void OnDestroy()
        {
            for (int i = 0; i < riftSprites.Count; i++) if (riftSprites[i] != null) Destroy(riftSprites[i]);
            for (int i = 0; i < riftTex.Count; i++) if (riftTex[i] != null) Destroy(riftTex[i]);
        }

        void BuildRiftSprites()
        {
            if (riftSprites.Count > 0) return;
            for (int i = 0; i < RiftLayout.Length; i++)
            {
                Sprite sp = null;
                try { sp = MakeRiftSprite(RiftLayout[i]); }
                catch (System.Exception e) { Debug.LogWarning("Neon Breaker (Story): không vẽ được khe nứt " + i + " — " + e.Message); }
                riftSprites.Add(sp);
            }
        }

        /// <summary>Mở lần lượt các khe: mỗi khe có một tia sét đánh xuống, chớp sáng, rồi khe nứt bung ra. Chạy song song (busy) để đoạn chữ chờ mở đủ khe.</summary>
        IEnumerator RiftSequence(Vector2 ring)
        {
            yield return Wait(0.5f);
            for (int q = 0; q < RiftOrder.Length && hallFx; q++)
            {
                int idx = RiftOrder[q];
                var sp = RiftLayout[idx];
                Sprite spr = idx < riftSprites.Count ? riftSprites[idx] : null;
                Vector2 pos = new Vector2(Snap(ring.x + sp.x), Snap(ring.y + sp.y));
                LogBolt(pos.x);
                BoltAt(pos);
                if (spr != null) StartCoroutine(OpenRift(spr, pos));
                yield return Wait(Random.Range(0.32f, 0.42f));
            }
        }

        IEnumerator OpenRift(Sprite spr, Vector2 pos)
        {
            var sr = Art.Spr(fxRoot, "rift", spr, pos, new Vector2(0.15f, 0.15f), Color.white, 40, false, 0f);
            for (int i = 0; i < 10; i++)
                Spawn(dot, pos + Random.insideUnitCircle * 0.4f, Random.insideUnitCircle.normalized * Random.Range(2f, 6f), Random.Range(0.25f, 0.5f),
                      Random.value < 0.5f ? new Color(0.8f, 0.6f, 1f) : Color.white, 0f, 45, true, Random.Range(0.5f, 1.0f), 0f, 0f, true);
            const float dur = 0.3f;
            float t = 0f;
            while (t < dur && sr != null)
            {
                t += Dt;
                float k = Mathf.Clamp01(t / dur) - 1f;
                float e = 1f + 2.70158f * k * k * k + 1.70158f * k * k;      // easeOutBack: bung ra rồi nảy nhẹ
                sr.transform.localScale = new Vector3(e, e, 1f);
                yield return null;
            }
            if (sr != null) sr.transform.localScale = Vector3.one;
        }

        sealed class CrackRng
        {
            uint s;
            public CrackRng(int seed) { s = unchecked((uint)seed * 2654435761u + 1013904223u); if (s == 0) s = 1; }
            public float U() { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return (s & 0xFFFFFF) / 16777216f; }
            public float R(float a, float b) { return a + (b - a) * U(); }
            public int I(int a, int b) { return b >= a ? a + (int)(U() * (b - a + 1)) : a; }
        }

        static bool InPoly(Vector2[] poly, float x, float y)
        {
            bool c = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if ((poly[i].y > y) != (poly[j].y > y) && x < (poly[j].x - poly[i].x) * (y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x) c = !c;
            return c;
        }

        /// <summary>
        /// Vẽ một khe nứt pixel-art theo hướng ang độ: hai mép lởm chởm (bề rộng thon hai đầu), bên trong là khoảng không đầy sao của một thế giới khác,
        /// viền tím sáng, vài nhánh nứt nhỏ. 1 pixel = 1/16 đơn vị. Điểm giữa trục nằm đúng tại pivot của sprite.
        /// </summary>
        Sprite MakeRiftSprite(RiftSpec sp)
        {
            var rng = new CrackRng(sp.seed);
            float L = sp.len * 16f, W = sp.wid * 16f;
            float a = sp.ang * Mathf.Deg2Rad;
            var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            var nv = new Vector2(-d.y, d.x);
            int N = Mathf.Max(6, (int)(L / 5f));
            var left = new Vector2[N + 1];
            var right = new Vector2[N + 1];
            float wob = 0f;
            for (int k = 0; k <= N; k++)
            {
                float s = (float)k / N;
                wob = wob * 0.55f + rng.R(-1f, 1f) * W * 0.10f;
                Vector2 c = d * ((s - 0.5f) * L) + nv * wob;
                float prof = (k > 0 && k < N) ? Mathf.Pow(Mathf.Sin(Mathf.PI * s), 0.7f) : 0f;
                float hl = W * 0.5f * prof * rng.R(0.55f, 1f);
                float hr = W * 0.5f * prof * rng.R(0.55f, 1f);
                left[k] = c + nv * hl;
                right[k] = c - nv * hr;
            }
            var poly = new Vector2[2 * (N + 1)];
            for (int k = 0; k <= N; k++) { poly[k] = left[k]; poly[poly.Length - 1 - k] = right[k]; }

            // hai nhánh nứt nhỏ từ mép
            var brA = new Vector2[2];
            var brB = new Vector2[2];
            for (int b = 0; b < 2; b++)
            {
                int k = rng.I(2, N - 2);
                int side = rng.U() < 0.5f ? 1 : -1;
                Vector2 bp = side > 0 ? left[k] : right[k];
                float ln = rng.R(5f, 9f);
                float da = rng.R(25f, 55f) * Mathf.Deg2Rad * side;
                brA[b] = bp;
                brB[b] = bp + new Vector2(Mathf.Cos(a + da), Mathf.Sin(a + da)) * ln;
            }

            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < poly.Length; i++) { minX = Mathf.Min(minX, poly[i].x); maxX = Mathf.Max(maxX, poly[i].x); minY = Mathf.Min(minY, poly[i].y); maxY = Mathf.Max(maxY, poly[i].y); }
            for (int b = 0; b < 2; b++) { minX = Mathf.Min(minX, brB[b].x); maxX = Mathf.Max(maxX, brB[b].x); minY = Mathf.Min(minY, brB[b].y); maxY = Mathf.Max(maxY, brB[b].y); }
            const int margin = 3;
            int x0 = Mathf.FloorToInt(minX) - margin, y0 = Mathf.FloorToInt(minY) - margin;
            int w = Mathf.CeilToInt(maxX) + margin - x0, h = Mathf.CeilToInt(maxY) + margin - y0;

            var mask = new bool[w * h];
            int area = 0;
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                    if (InPoly(poly, x0 + i + 0.5f, y0 + j + 0.5f)) { mask[j * w + i] = true; area++; }

            var px = new Color32[w * h];                       // hàng 0 = đáy ảnh (toạ độ Unity), cùng chiều y LÊN với polygon
            var fill = new Color32(6, 2, 16, 255);
            var rim = new Color32(210, 170, 255, 255);
            var glow = new Color32(130, 70, 220, 110);
            System.Func<int, int, bool> M = (i, j) => i >= 0 && i < w && j >= 0 && j < h && mask[j * w + i];
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                    if (mask[j * w + i]) px[j * w + i] = (M(i - 1, j) && M(i + 1, j) && M(i, j - 1) && M(i, j + 1)) ? fill : rim;
            for (int j = 0; j < h; j++)                        // ánh sáng mờ quanh viền
                for (int i = 0; i < w; i++)
                    if (!mask[j * w + i] && (M(i - 1, j) || M(i + 1, j) || M(i, j - 1) || M(i, j + 1))) px[j * w + i] = glow;
            for (int b = 0; b < 2; b++)                        // nhánh nứt nhỏ
            {
                int steps = (int)Mathf.Max(Mathf.Abs(brB[b].x - brA[b].x), Mathf.Abs(brB[b].y - brA[b].y)) + 1;
                for (int t = 0; t <= steps; t++)
                {
                    float f = (float)t / steps;
                    int i = Mathf.FloorToInt(Mathf.Lerp(brA[b].x, brB[b].x, f)) - x0;
                    int j = Mathf.FloorToInt(Mathf.Lerp(brA[b].y, brB[b].y, f)) - y0;
                    if (i < 0 || i >= w || j < 0 || j >= h || mask[j * w + i]) continue;
                    px[j * w + i] = f < 0.7f ? new Color32(190, 140, 250, 255) : new Color32(150, 100, 230, 200);
                }
            }
            var stars = new[] { new Color32(255, 255, 255, 255), new Color32(40, 240, 255, 255), new Color32(255, 40, 200, 255), new Color32(170, 130, 255, 255) };
            for (int q = Mathf.Max(3, area / 38); q > 0; q--)  // sao của thế giới bên kia
            {
                int i = rng.I(0, w - 1), j = rng.I(0, h - 1);
                if (mask[j * w + i] && px[j * w + i].Equals(fill)) px[j * w + i] = stars[rng.I(0, 3)];
            }
            for (int q = Mathf.Max(1, area / 300); q > 0; q--) // vài đốm tím sẫm
            {
                int i = rng.I(0, w - 1), j = rng.I(0, h - 1);
                for (int dj = -1; dj <= 1; dj++)
                    for (int di = -1; di <= 1; di++)
                    {
                        int ii = i + di, jj = j + dj;
                        if (M(ii, jj) && px[jj * w + ii].Equals(fill)) px[jj * w + ii] = new Color32(70, 20, 120, 255);
                    }
            }

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(px);
            tex.Apply(false);
            riftTex.Add(tex);
            var spr = Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2((float)(-x0) / w, (float)(-y0) / h), 16f);
            return spr;
        }

        /// <summary>Sét giật một phát cực mạnh: rung chuyển màn hình, tia sét che gần hết khung hình.</summary>
        IEnumerator BigStrike()
        {
            Sfx.Play("slam", 1f, 0.5f);
            Sfx.Play("bolt", 1f, 0.5f);
            SpriteRenderer big = null;
            if (boltBig != null) big = Art.Spr(fxRoot, "bigbolt", boltBig, new Vector2(camX, 0f), Vector2.one * 1.35f, Color.white, 90, true, 0f);
            Flash(0.95f, Color.white);
            float t = 0f;
            while (t < 0.95f)
            {
                t += Dt;
                shakeAmp = Mathf.Lerp(16f, 0f, t / 0.95f);
                if (big != null) big.color = new Color(1f, 1f, 1f, t < 0.7f ? 1f : Mathf.Clamp01((0.95f - t) / 0.25f));
                yield return null;
            }
            if (big != null) Destroy(big.gameObject);
        }

        // ------------------------------------------------------------ cảnh 6: một robot lao vào điểm kỳ dị; ánh sáng trắng 3 giây
        IEnumerator Scene6()
        {
            Run(Dash());
            yield return Say(StoryTexts.S6);
            hallFx = false;
            panelOn = false;
            ovCol = Color.white;
            Sfx.Play("charge", 1f, 0.5f);
            yield return FadeTo(1f, 0.15f);
            yield return Wait(3f);                       // ánh sáng trắng che phủ hết màn hình trong 3 giây
        }

        IEnumerator Dash()
        {
            var d = T("S5_Machine/dasher");
            if (d == null) yield break;
            var dsr = d.GetComponent<SpriteRenderer>();
            Vector2 a = FpW(520f, 176f, HallPx), b = FpW(330f, 176f, HallPx), c = FpW(240f, 120f, HallPx);
            Vector2 center = FpW(240f, 86f, HallPx);
            yield return Wait(0.8f);
            dashing = true;                              // sét chỉ giáng ở xa + không chớp sáng: không che robot
            d.gameObject.SetActive(true);
            d.localScale = Vector3.one;
            if (dsr != null) { dsr.flipX = true; dsr.color = Color.white; dsr.sortingOrder = 100; }      // vẽ trên cùng: tia / vầng sáng không đè lên robot
            d.position = a;
            float t = 0f;
            const float runT = 0.95f, leapT = 0.8f, meltT = 1.7f;      // chậm hơn trước (0,55 / 0,5 / 1,0 giây)
            while (t < runT)                             // chạy dọc sàn
            {
                t += Dt;
                float k = t / runT;
                Vector2 p = Vector2.Lerp(a, b, k) + Vector2.up * Mathf.Abs(Mathf.Sin(t * 22f)) * 0.12f;
                d.position = new Vector3(p.x, p.y, 0f);
                shakeAmp = Mathf.Max(shakeAmp, 1.5f);
                yield return null;
            }
            Sfx.Play("dash", 0.9f, 0.7f);
            t = 0f;
            while (t < leapT)                            // bật lên lao vào tâm cỗ máy
            {
                t += Dt;
                float k = Mathf.Clamp01(t / leapT);
                Vector2 p = Vector2.Lerp(b, c, k) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 2.4f;
                d.position = new Vector3(p.x, p.y, 0f);
                yield return null;
            }
            Sfx.Play("teleport", 0.9f, 0.5f);
            t = 0f;
            while (t < meltT)                            // thân hình tan dần, bị kéo vào điểm kỳ dị
            {
                t += Dt;
                float k = Mathf.Clamp01(t / meltT);
                d.localScale = Vector3.one * Mathf.Lerp(1f, 0.15f, k * k);
                d.position = Vector3.Lerp(d.position, new Vector3(center.x, center.y - 1.2f, 0f), 0.08f);
                if (dsr != null) dsr.color = new Color(1f, 1f, 1f, 1f - k);
                Vector2 pp = (Vector2)d.position + Random.insideUnitCircle * 1.3f;
                Vector2 dir = center - pp;
                Spawn(dot, pp, dir * 2.5f, 0.5f, Random.value < 0.5f ? new Color(0.75f, 0.5f, 1f) : Color.white, 0f, 105, true, Random.Range(0.6f, 1.1f), 0f, 0f, true);
                shakeAmp = Mathf.Max(shakeAmp, 1.5f);
                yield return null;
            }
            d.gameObject.SetActive(false);
            dashing = false;
        }

        // ------------------------------------------------------------ cảnh 7: phế tích La Mã lẫn nhà chọc trời, rồi đám đông
        IEnumerator Scene7()
        {
            Show("S7a_Ruins");
            SetCam(0f);
            ovCol = Color.white;
            ovA = 1f;
            yield return Wait(0.3f);
            carsOn = true;
            StartCoroutine(Cars());
            yield return FadeTo(0f, 1.4f);
            Run(S7Switch());
            yield return Say(StoryTexts.S7);
            carsOn = false;
            yield return BlackBlink(0.25f, () => { Show("S8_War"); SetCam(WarStart); });     // chớp đen -> cảnh 8
        }

        IEnumerator S7Switch()
        {
            yield return Wait(3.4f);
            yield return BlackBlink(0.2f, () => { Show("S7b_Crowd"); carsOn = false; });      // camera nháy đen, sang ảnh đám đông
        }

        IEnumerator Cars()
        {
            var root = T("S7a_Ruins");
            if (root == null) yield break;
            while (carsOn)
            {
                for (int i = 0; i < root.childCount; i++)
                {
                    var c = root.GetChild(i);
                    if (!c.name.StartsWith("car_")) continue;
                    var p = c.position;
                    p.x += (i % 2 == 0 ? 1.6f : -1.1f) * (1f + 0.4f * i) * Dt;
                    if (p.x > 17f) p.x = -17f;
                    if (p.x < -17f) p.x = 17f;
                    c.position = p;
                    var sr = c.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.flipX = (i % 2 != 0);
                }
                yield return null;
            }
        }

        // ------------------------------------------------------------ cảnh 8: chiến tranh, camera chạy chậm từ trái sang phải
        const float WarStart = -25f;     // ảnh chiến tranh 1280 px: tâm khung từ px 240 -> px 1040
        const float WarEnd = 25f;

        IEnumerator Scene8()
        {
            SetCam(WarStart);
            rainOn = true;
            StartCoroutine(Rain());
            StartCoroutine(StormFlash());
            Run(PanSmooth(WarStart, WarEnd, 13f));
            yield return Say(StoryTexts.S8);
            rainOn = false;
            yield return FadeOut(1.3f);
        }

        IEnumerator Rain()
        {
            while (rainOn)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2 p = new Vector2(camX + Random.Range(-16f, 16f), Random.Range(5f, 8f));
                    Spawn(rain, p, new Vector2(-3f, -20f), 0.65f, new Color(0.65f, 0.72f, 0.95f, 0.7f), 0f, 80, false, 1f, -8.5f, 0f, false);
                }
                yield return null;
            }
        }

        IEnumerator StormFlash()
        {
            while (rainOn)
            {
                yield return Wait(Random.Range(2.2f, 4.5f));
                if (!rainOn) break;
                Flash(0.45f, new Color(0.85f, 0.88f, 1f));
                Sfx.Play("bolt", 0.3f, 0.5f);
                if (bolts != null && bolts.Length > 0)
                    Spawn(bolts[Random.Range(0, bolts.Length)], new Vector2(camX + Random.Range(-11f, 11f), 6.5f), Vector2.zero, 0.15f, Color.white, 0f, 70, true, 1.4f, 0f, 0f, false);
            }
        }

        // ------------------------------------------------------------ cảnh 9: tiến hóa của loài người (một khung hình, hiện dần)
        IEnumerator Scene9()
        {
            Show("S9_Evo");
            SetCam(0f);
            var root = T("S9_Evo");
            if (root != null)
                for (int i = 0; i < root.childCount; i++)
                {
                    root.GetChild(i).gameObject.SetActive(false);
                    var sr = root.GetChild(i).GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = new Color(1f, 1f, 1f, 0f);
                }
            ovCol = Color.black;
            yield return Wait(0.4f);
            yield return FadeTo(0f, 1.0f);

            Run(Seq(new[] { "fig_0", "", "line_0", "fig_1", "", "line_1", "fig_2" }));       // 9a: vượn 4 chân -> khom -> 2 chân
            yield return Say(StoryTexts.S9a);
            Run(Seq(new[] { "line_2", "fig_3", "", "line_3", "fig_4" }));                    // 9b: vũ khí lạnh -> vũ khí nóng
            yield return Say(StoryTexts.S9b);
            Run(Seq(new[] { "line_4", "fig_5" }));                                           // 9c: trang phục Neon, tương lai
            yield return Say(StoryTexts.S9c);
            yield return Wait(0.8f);
            yield return FadeOut(1.5f);
        }

        /// <summary>Hiện lần lượt các object con của S9_Evo (mờ dần vào); "" = nghỉ một nhịp.</summary>
        IEnumerator Seq(string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] == "") { yield return Wait(0.45f); continue; }
                var t = T("S9_Evo/" + names[i]);
                if (t == null) continue;
                t.gameObject.SetActive(true);
                var sr = t.GetComponent<SpriteRenderer>();
                bool line = names[i].StartsWith("line");
                float dur = line ? 0.7f : 1.0f;
                float e = 0f;
                while (e < dur)
                {
                    e += Dt;
                    if (sr != null) sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(e / dur));
                    yield return null;
                }
                if (sr != null) sr.color = Color.white;
            }
        }

        // ------------------------------------------------------------ cảnh 10: chỉ có chữ giữa màn hình đen
        IEnumerator Scene10()
        {
            Show();
            ovCol = Color.black;
            ovA = 0f;                                    // màn hình vốn đã đen (không còn hình nào), tắt lớp phủ để chữ không bị che
            yield return Wait(0.8f);
            yield return Say(StoryTexts.S10, true, 3.5f);
            centerOn = false;
            yield return Wait(0.4f);
        }

        // ------------------------------------------------------------ hạt tàn lửa bay lên ở cảnh hầm / chiến tranh
        IEnumerator Embers()
        {
            while (true)
            {
                if (embers)
                {
                    Vector2 p = new Vector2(camX + Random.Range(-15f, 15f), Random.Range(-6f, 4.5f));
                    Spawn(dot, p, new Vector2(Random.Range(-0.4f, 0.6f), Random.Range(0.8f, 2.4f)), Random.Range(2f, 3.5f),
                          Random.value < 0.5f ? new Color(1f, 0.6f, 0.15f) : new Color(1f, 0.3f, 0.12f), 0f, 55, true, Random.Range(0.4f, 0.7f), 0f, 0f, true);
                }
                yield return Wait(0.09f);
            }
        }

        // ============================================================== VẼ GIAO DIỆN (OnGUI riêng, nằm trên cùng)
        void OnGUI()
        {
            GUI.depth = -1000;
            if (hud == null) return;
            if (!hud.Prepare())
            {
                if (error == null) error = "Art.Pixel = null (Art.Init không tạo được texture).";
                DrawError();
                return;
            }
            try { DrawUi(); }
            catch (System.Exception e)
            {
                error = e.ToString();
                if (!errorLogged) { errorLogged = true; Debug.LogException(e); }
            }
            GUI.color = Color.white;
            if (error != null) DrawError();
        }

        void EnsureWrap()
        {
            if (!needWrap && wrapH == Screen.height) return;
            if (measure == null) measure = new GUIStyle(GUI.skin.label);
            float s = hud.s;
            measure.fontSize = Mathf.Max(8, Mathf.RoundToInt((centerOn ? 34f : 24f) * s));
            measure.fontStyle = centerOn ? FontStyle.Bold : FontStyle.Normal;
            measure.wordWrap = false;
            wrapped = Wrap(full, (centerOn ? 1000f : 1130f) * s, measure);
            needWrap = false;
            wrapH = Screen.height;
        }

        static string Wrap(string text, float maxW, GUIStyle st)
        {
            var sb = new StringBuilder();
            string line = "";
            string[] words = text.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                string test = line.Length == 0 ? words[i] : line + " " + words[i];
                if (line.Length > 0 && st.CalcSize(new GUIContent(test)).x > maxW)
                {
                    sb.Append(line).Append('\n');
                    line = words[i];
                }
                else line = test;
            }
            sb.Append(line);
            return sb.ToString();
        }

        void DrawUi()
        {
            EnsureWrap();
            string typed = wrapped.Substring(0, Mathf.Clamp(shown, 0, wrapped.Length));

            // khung chữ ở 22% dưới màn hình (hình vẽ chỉ dùng phần trên, không bị chữ đè)
            if (panelA > 0.01f)
            {
                hud.Fill(hud.R(0, 560, 1280, 160), new Color(0.01f, 0.01f, 0.05f, 0.96f * panelA));
                hud.Fill(hud.R(0, 560, 1280, 2), Cfg.A(Cfg.Cyan, 0.7f * panelA));
                hud.Txt(hud.R(70, 580, 1140, 110), typed, 24, Cfg.A(Color.white, panelA), TextAnchor.UpperLeft, false);
                if (ready)
                {
                    float b = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 6f);
                    hud.Txt(hud.R(1010, 686, 200, 24), Cfg.L("ENTER  >>", "ENTER  >>"), 16, Cfg.A(Cfg.Cyan, b * panelA), TextAnchor.MiddleRight);
                }
            }
            else if (centerOn)
            {
                hud.Txt(hud.R(140, 230, 1000, 260), typed, 34, Color.white, TextAnchor.MiddleCenter, true);
            }
            if (!finishing)
                hud.Txt(hud.R(700, 8, 560, 22), Cfg.L("[ENTER] TIẾP     [ESC] BỎ QUA CỐT TRUYỆN", "[ENTER] NEXT     [ESC] SKIP STORY"), 14,
                        Cfg.A(Color.white, 0.4f), TextAnchor.UpperRight, false);

            if (flashA > 0.002f) hud.Fill(new Rect(0, 0, Screen.width, Screen.height), Cfg.A(flashCol, flashA));
            if (ovA > 0.002f) hud.Fill(new Rect(0, 0, Screen.width, Screen.height), Cfg.A(ovCol, ovA));
        }

        void DrawError()
        {
            GUI.color = new Color(0.06f, 0f, 0.02f, 0.97f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var st = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = Mathf.Max(12, Screen.height / 45), alignment = TextAnchor.UpperLeft };
            st.normal.textColor = new Color(1f, 0.45f, 0.45f, 1f);
            GUI.Label(new Rect(24, 20, Screen.width - 48, Screen.height - 40),
                      "NEON BREAKER - CANH COT TRUYEN BI LOI (nhan ESC de bo qua)\n\n" + error, st);
        }
    }
}
