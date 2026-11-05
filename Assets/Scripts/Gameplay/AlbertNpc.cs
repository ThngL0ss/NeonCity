using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Albert: ông già khoa học áo choàng trắng, NPC của căn cứ. Đi lại quanh căn cứ, thỉnh thoảng đứng lại ngắm nghía một công trình
    /// (Interest Points), lẩm bẩm vài câu. Bấm F để nói chuyện: kể cốt truyện, giao + nhận thưởng nhiệm vụ.
    /// Object con "Visual" có SpriteAnimator với clip: idle, walk, admire.
    /// </summary>
    public class AlbertNpc : MonoBehaviour, IInteractable
    {
        [Header("Di chuyển")]
        public float walkSpeed = 1.5f;
        [Tooltip("Giới hạn đi lại (toạ độ X)")] public float minX = 4f;
        public float maxX = 100f;
        [Tooltip("Thời gian đứng nghỉ giữa các lần đi (ngắn nhất, dài nhất)")] public Vector2 idleTime = new Vector2(2.5f, 6f);
        [Tooltip("Thời gian đứng ngắm công trình (ngắn nhất, dài nhất)")] public Vector2 admireTime = new Vector2(3.5f, 6.5f);
        [Tooltip("Xác suất đi ngắm một công trình thay vì đi lang thang")] [Range(0f, 1f)] public float admireChance = 0.55f;
        [Tooltip("Các công trình Albert hay đứng ngắm (kéo object trong Scene vào)")] public Transform[] interestPoints;

        [Header("Tương tác")]
        public float interactRange = 2.6f;

        public SpriteAnimator anim;
        public Transform vis;

        enum S { Idle, Walk, Admire, Talk, Scripted }
        S state = S.Idle;
        float timer;
        float targetX;
        int facing = -1;
        Transform poi;
        bool moving;
        float headBob;

        static readonly string[] AdmireVi =
        {
            "Hừm... hoàn hảo đến từng con ốc.", "Ta đúng là thiên tài.", "Chỗ này cần thêm một cuộn dây đồng...", "Một kiệt tác. Lại một kiệt tác.",
            "Nếu chỉnh góc này 2 độ nữa thì...", "Đẹp. Rất đẹp.",
        };
        static readonly string[] AdmireEn =
        {
            "Hmm... perfect down to the last bolt.", "I truly am a genius.", "This needs a bit more copper wire...", "A masterpiece. Another masterpiece.",
            "If I tilt this by two more degrees...", "Beautiful. Simply beautiful.",
        };

        public bool Scripted { get { return state == S.Scripted; } }
        public float X { get { return transform.position.x; } }
        public Vector2 Head { get { return (Vector2)transform.position + new Vector2(0f, 2.3f); } }

        // ---- IInteractable
        public bool CanInteract { get { return state != S.Scripted && !BaseUi.IsOpen && GameManager.State == GameState.Playing && !GameManager.PlayerLocked; } }
        public Vector2 PromptPos { get { return Head + new Vector2(0f, 0.5f); } }
        public string PromptText { get { return Cfg.L("[F] NÓI CHUYỆN VỚI ALBERT", "[F] TALK TO ALBERT"); } }
        public bool InRange(Player p) { return Interactor.Near(p, transform.position, interactRange, 3f); }

        void OnEnable() { Interactor.Register(this); }
        void OnDisable() { Interactor.Unregister(this); }

        void Awake()
        {
            if (vis == null) vis = transform.Find("Visual");
            if (anim == null && vis != null) anim = vis.GetComponent<SpriteAnimator>();
            timer = Random.Range(idleTime.x, idleTime.y);
        }

        public void Interact()
        {
            if (BaseManager.Inst != null) BaseManager.Inst.TalkToAlbert();
        }

        // ------------------------------------------------------------ điều khiển từ kịch bản
        public void SetScripted(bool on)
        {
            state = on ? S.Scripted : S.Idle;
            timer = Random.Range(idleTime.x, idleTime.y);
            moving = false;
        }

        public void Face(float worldX) { facing = worldX >= transform.position.x ? 1 : -1; }

        public void StartTalk(float playerX)
        {
            state = S.Talk;
            moving = false;
            Face(playerX);
        }

        public void EndTalk()
        {
            if (state == S.Talk) { state = S.Idle; timer = Random.Range(1.5f, 3f); }
        }

        /// <summary>Nói một câu thoáng qua trên đầu (chữ nổi).</summary>
        public void Mutter(string vi, string en)
        {
            Fx.Text(Head, Cfg.L(vi, en), Cfg.A(Color.white, 0.95f), 0.9f);
        }

        // ------------------------------------------------------------ AI đi lại
        void Update()
        {
            if (!GameManager.Active && GameManager.State != GameState.Playing) { Animate(); return; }
            float dt = Time.deltaTime;
            moving = false;

            switch (state)
            {
                case S.Idle:
                    timer -= dt;
                    if (timer <= 0f) PickNext();
                    break;
                case S.Walk:
                    MoveToward(targetX, dt);
                    if (Mathf.Abs(transform.position.x - targetX) < 0.1f)
                    {
                        if (poi != null)
                        {
                            state = S.Admire;
                            timer = Random.Range(admireTime.x, admireTime.y);
                            Face(poi.position.x);
                            int i = Random.Range(0, AdmireVi.Length);
                            Mutter(AdmireVi[i], AdmireEn[i]);
                        }
                        else { state = S.Idle; timer = Random.Range(idleTime.x, idleTime.y); }
                    }
                    break;
                case S.Admire:
                    timer -= dt;
                    if (timer <= 0f) { poi = null; state = S.Idle; timer = Random.Range(idleTime.x, idleTime.y); }
                    break;
                case S.Talk:
                    if (!BaseUi.IsOpen) EndTalk();
                    break;
            }
            Animate();
        }

        /// <summary>Đi tới x (dùng trong kịch bản, gọi mỗi khung hình). true khi đã tới.</summary>
        public bool WalkStep(float x, float dt)
        {
            moving = false;
            MoveToward(x, dt);
            return Mathf.Abs(transform.position.x - x) < 0.1f;
        }

        void MoveToward(float x, float dt)
        {
            float dx = x - transform.position.x;
            if (Mathf.Abs(dx) < 0.05f) return;
            facing = dx > 0f ? 1 : -1;
            float nx = Mathf.MoveTowards(transform.position.x, x, walkSpeed * dt);
            transform.position = new Vector3(Mathf.Clamp(nx, minX, maxX), transform.position.y, 0f);
            moving = true;
        }

        void PickNext()
        {
            poi = null;
            if (interestPoints != null && interestPoints.Length > 0 && Random.value < admireChance)
            {
                var p = interestPoints[Random.Range(0, interestPoints.Length)];
                if (p != null)
                {
                    poi = p;
                    // đứng cách công trình một khoảng, phía gần mình
                    float side = transform.position.x < p.position.x ? -1f : 1f;
                    targetX = Mathf.Clamp(p.position.x + side * Random.Range(2.2f, 3.2f), minX, maxX);
                    state = S.Walk;
                    return;
                }
            }
            targetX = Mathf.Clamp(transform.position.x + Random.Range(-14f, 14f), minX, maxX);
            state = S.Walk;
        }

        void Animate()
        {
            if (vis != null)
            {
                headBob += Time.deltaTime * (moving ? 9f : 2f);
                float sy = 1f + (moving ? 0.015f * Mathf.Sin(headBob) : 0.01f * Mathf.Sin(headBob));
                vis.localScale = new Vector3(facing, sy, 1f);
            }
            if (anim != null)
            {
                string clip = moving ? "walk" : (state == S.Admire && anim.Has("admire") ? "admire" : "idle");
                anim.Play(clip);
            }
        }
    }
}
