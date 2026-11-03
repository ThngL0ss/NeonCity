using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Cưa quay: lưỡi cưa chạy qua lại trên một thanh ray giữa điểm đầu (vị trí của bẫy) và điểm cuối (Travel).
    /// Chạm lưỡi cưa là mất máu. Nhảy qua khi cưa xa, hoặc lướt (I) xuyên qua.
    /// Ví dụ: Travel (8, 0) = chạy ngang 8 đơn vị; Travel (0, 5) = chạy dọc lên xuống.
    /// Đặt bẫy ở độ cao tâm lưỡi cưa (khoảng y = 0.9 để cưa sát đất).
    /// </summary>
    public class SawTrap : Trap
    {
        [Header("Chuyển động")]
        [Tooltip("Điểm cuối so với điểm đầu (điểm đầu = vị trí của bẫy)")] public Vector2 travel = new Vector2(8f, 0f);
        [Tooltip("Tốc độ chạy trên ray (đơn vị/giây)")] public float speed = 4.5f;
        [Tooltip("Dừng ở mỗi đầu ray bao lâu (giây)")] public float endPause = 0.25f;
        [Tooltip("Vị trí xuất phát trên ray: 0 = điểm đầu, 1 = điểm cuối")] [Range(0f, 1f)] public float startPhase = 0f;

        [Header("Lưỡi cưa")]
        [Tooltip("Bán kính vùng gây sát thương")] public float radius = 0.85f;
        [Tooltip("Tốc độ quay (độ/giây)")] public float spinSpeed = 720f;

        [Header("Hình ảnh (tự tìm theo tên nếu để trống)")]
        public Transform saw;      // object con "Saw" (di chuyển); bên trong có "Visual" (quay)
        public Transform rail;     // object con "Rail" (thanh ray)

        float u;
        int dir = 1;
        float pause;
        Transform spinner;

        void Awake()
        {
            if (saw == null) saw = transform.Find("Saw");
            if (rail == null) rail = transform.Find("Rail");
            if (saw != null) spinner = saw.Find("Visual");
            u = startPhase;
            LayoutRail();
            PlaceSaw();
        }

        void LayoutRail()
        {
            if (rail == null) return;
            float len = travel.magnitude;
            rail.localPosition = travel * 0.5f;
            rail.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(travel.y, travel.x) * Mathf.Rad2Deg);
            rail.localScale = new Vector3(Mathf.Max(0.01f, len), 0.12f, 1f);
        }

        void PlaceSaw()
        {
            if (saw != null) saw.position = transform.position + (Vector3)(travel * u);
        }

        public Vector2 SawPos { get { return (Vector2)transform.position + travel * u; } }

        void Update()
        {
            if (!Running) return;
            float dt = Time.deltaTime;

            float len = travel.magnitude;
            if (len > 0.01f)
            {
                if (pause > 0f) pause -= dt;
                else
                {
                    u += dir * speed * dt / len;
                    if (u >= 1f) { u = 1f; dir = -1; pause = endPause; }
                    else if (u <= 0f) { u = 0f; dir = 1; pause = endPause; }
                }
            }
            PlaceSaw();
            if (spinner != null) spinner.Rotate(0f, 0f, -spinSpeed * dt);

            Vector2 p = SawPos;
            if (Random.value < 0.12f)
                Fx.Sparks(p + new Vector2(0f, -radius * 0.6f), new Vector2(Random.value < 0.5f ? -1f : 1f, 0.6f), Cfg.Cyan, 1, 7f, 40f, 0.2f);

            var pl = Target;
            if (pl != null && Geo.CircleRect(p, radius, pl.HurtRect)) HurtPlayer(p);
            HurtBotsCircle(p, radius, p);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector2 a = transform.position;
            Gizmos.DrawLine(a, a + travel);
            Gizmos.DrawWireSphere(a, radius);
            Gizmos.DrawWireSphere(a + travel, radius);
        }
    }
}
