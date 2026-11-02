using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Điều khiển màn chơi. Bố cục (nền, mặt đất, bục, trụ, cổng, đài boss) là các object thật trong Scene - bạn kéo thả để sửa.
    /// Component này chỉ:
    ///  * đọc vị trí các "mốc" bạn đặt (Limit Left/Right, Gate, Boss Spawn) rồi báo cho game biết;
    ///  * đóng / mở cổng khu boss;
    ///  * rắc bụi năng lượng quanh camera.
    /// Chọn object Level trong Scene để thấy các đường mốc vẽ bằng Gizmos.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class Level : MonoBehaviour
    {
        public static Level Instance;
        public static Transform Root;                       // nhóm "Runtime" chứa object sinh ra khi chơi (do GameManager tạo)

        [Header("Các mốc trong Scene (kéo object vào đây)")]
        public Transform limitLeft;
        public Transform limitRight;
        public Transform gate;
        public Transform bossSpawn;
        [Tooltip("Khoảng cách từ cổng tới điểm mà người chơi đi qua sẽ kích hoạt boss")]
        public float bossTriggerOffset = 7f;

        [Header("Quái & vật phẩm (toàn màn)")]
        [Tooltip("Trần số quái nhỏ sống cùng lúc trên toàn màn (cộng tất cả các trụ)")]
        public int maxEnemiesAlive = 8;
        [Tooltip("Tỉ lệ quái nhỏ rơi vật phẩm hồi máu (0.2 = 20%). Từng loại quái còn có hệ số riêng Drop Chance Mul")]
        [Range(0f, 1f)] public float dropChance = 0.20f;

        [Header("Cổng khu boss")]
        public BoxCollider2D gateCollider;
        public SpriteRenderer gateBar;
        public SpriteRenderer gateGlow;
        public SpriteRenderer[] gatePosts;
        public Color postClosedColor = Color.white;
        public Color postOpenColor = new Color(0.45f, 0.9f, 1f, 1f);

        static readonly Dictionary<string, Transform> subs = new Dictionary<string, Transform>();

        bool gateClosed = true;
        float gateAlpha = 1f;
        float moteT;
        Camera cam;

        public bool GateClosed { get { return gateClosed; } }

        /// <summary>Nhóm trống trong Hierarchy để gom các đối tượng sinh ra khi chơi (Enemies, Projectiles, Pickups, Telegraphs).</summary>
        public static Transform Sub(string name)
        {
            if (Root == null) return null;
            Transform t;
            if (!subs.TryGetValue(name, out t) || t == null)
            {
                var g = new GameObject(name);
                g.transform.SetParent(Root, false);
                t = g.transform;
                subs[name] = t;
            }
            return t;
        }

        void Awake()
        {
            Instance = this;
            Cfg.MaxEnemiesAlive = maxEnemiesAlive;
            Cfg.DropChance = dropChance;
            if (limitLeft != null) Cfg.LevelMinX = limitLeft.position.x;
            if (limitRight != null) Cfg.LevelMaxX = limitRight.position.x;
            if (gate != null) Cfg.GateX = gate.position.x;
            Cfg.BossTriggerX = Cfg.GateX + bossTriggerOffset;
            if (bossSpawn != null) Cfg.AltarX = bossSpawn.position.x;
            if (gateCollider == null && gate != null) gateCollider = gate.GetComponent<BoxCollider2D>();
            gateClosed = true;
            if (gateCollider != null) gateCollider.enabled = true;
            cam = Camera.main;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        // ------------------------------------------------------------ gate control
        public void OpenGate()
        {
            gateClosed = false;
            if (gateCollider != null) gateCollider.enabled = false;
            Sfx.Play("charge", 0.7f, 1.4f);
            Fx.Flare(new Vector2(Cfg.GateX, 3f), Cfg.Cyan, 7f, 0.5f);
            SetPostColor(postOpenColor);
        }

        public void CloseGate()
        {
            gateClosed = true;
            if (gateCollider != null) gateCollider.enabled = true;
            Sfx.Play("slam", 0.8f, 1.1f);
            Fx.Shake(0.5f);
            Fx.Ring(new Vector2(Cfg.GateX, 3f), Cfg.Magenta, 1f, 10f, 0.5f);
            SetPostColor(postClosedColor);
        }

        void SetPostColor(Color c)
        {
            if (gatePosts == null) return;
            for (int i = 0; i < gatePosts.Length; i++) if (gatePosts[i] != null) gatePosts[i].color = c;
        }

        /// <summary>Đạn có bị chặn bởi tường cổng không.</summary>
        public bool IsBlocked(Vector2 pos, float radius)
        {
            return gateClosed && Mathf.Abs(pos.x - Cfg.GateX) < 0.4f + radius * 0.5f && pos.y > -1f;
        }

        // ------------------------------------------------------------ per-frame
        void Update()
        {
            // cổng: nhấp nháy khi đóng, mờ dần khi mở
            float target = gateClosed ? 1f : 0f;
            gateAlpha = Mathf.MoveTowards(gateAlpha, target, Time.deltaTime * 1.5f);
            float flick = gateClosed ? (0.8f + 0.2f * Mathf.Sin(Time.time * 22f) * Mathf.Sin(Time.time * 7f)) : 1f;
            if (gateBar != null) gateBar.color = Cfg.A(Color.white, gateAlpha * flick);
            if (gateGlow != null) gateGlow.color = Cfg.A(Cfg.Magenta, 0.45f * gateAlpha * flick);

            if (cam == null) cam = Camera.main;
            if (gateClosed && Random.value < 0.4f && cam != null && Mathf.Abs(cam.transform.position.x - Cfg.GateX) < 24f)
                Fx.Emit(Fx.S_Glow, new Vector2(Cfg.GateX + Random.Range(-0.2f, 0.2f), Random.Range(0.5f, 12f)), new Vector2(Random.Range(-0.6f, 0.6f), Random.Range(-1f, 3f)),
                        new Vector2(0.4f, 0.4f), Vector2.zero, Cfg.A(Cfg.Magenta, 0.9f), Cfg.A(Cfg.Magenta, 0f), 0.6f);

            // hạt bụi năng lượng bay lơ lửng quanh camera
            moteT -= Time.deltaTime;
            if (moteT <= 0f && cam != null)
            {
                moteT = 0.09f;
                Vector3 cp = cam.transform.position;
                float hw = cam.orthographicSize * cam.aspect;
                Color c = Random.value < 0.5f ? Cfg.Cyan : Cfg.Magenta;
                Fx.Emit(Fx.S_Glow, new Vector2(cp.x + Random.Range(-hw, hw), cp.y + Random.Range(-cam.orthographicSize, cam.orthographicSize)),
                        new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(0.3f, 1.2f)), new Vector2(0.22f, 0.22f), new Vector2(0f, 0f),
                        Cfg.A(c, 0.6f), Cfg.A(c, 0f), Random.Range(1.5f, 3f));
            }
        }

        // ------------------------------------------------------------ gizmos (chỉ để dễ chỉnh trong Scene view)
        void OnDrawGizmos()
        {
            Vector3 a, b;
            if (limitLeft != null) { Gizmos.color = Color.cyan; a = limitLeft.position; Gizmos.DrawLine(a + Vector3.down * 6f, a + Vector3.up * 20f); }
            if (limitRight != null) { Gizmos.color = Color.magenta; a = limitRight.position; Gizmos.DrawLine(a + Vector3.down * 6f, a + Vector3.up * 20f); }
            if (gate != null)
            {
                Gizmos.color = Color.red;
                a = gate.position;
                Gizmos.DrawLine(a + Vector3.down * 6f, a + Vector3.up * 20f);
                Gizmos.color = Color.yellow;                         // điểm kích hoạt boss
                b = a + Vector3.right * bossTriggerOffset;
                Gizmos.DrawLine(b + Vector3.down * 2f, b + Vector3.up * 8f);
            }
            if (bossSpawn != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(bossSpawn.position + Vector3.up * 2f, 1.5f);
            }
        }
    }
}
