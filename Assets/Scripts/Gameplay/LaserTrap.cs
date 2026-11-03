using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Cổng laser: hai trụ phát và một tia điện chớp tắt theo chu kỳ (tắt → tia mảnh báo hiệu → bật gây sát thương).
    /// Chờ tia tắt rồi đi qua, hoặc lướt (I) xuyên qua lúc tia đang bật.
    /// Dọc (Vertical = bật): đặt chân ở mặt đất, tia cao Length đơn vị — chặn đường.
    /// Ngang (Vertical = tắt): tia nằm ngang ở độ cao Horizontal Height, rộng Length, giữa vị trí bẫy.
    /// </summary>
    public class LaserTrap : Trap
    {
        [Header("Hình dạng")]
        public bool vertical = true;
        [Tooltip("Dọc: chiều cao tia tính từ mặt đất. Ngang: tổng chiều rộng tia")] public float length = 5.5f;
        [Tooltip("Chỉ dùng khi nằm ngang: độ cao của tia so với chân bẫy")] public float horizontalHeight = 1.1f;
        [Tooltip("Độ dày tia")] public float thickness = 0.5f;

        [Header("Chu kỳ")]
        [Tooltip("true = tia luôn bật (bẫy tĩnh, chỉ lướt qua được)")] public bool alwaysOn = false;
        public float offTime = 1.8f;
        public float warnTime = 0.6f;
        public float onTime = 1.5f;
        public float startOffset = 0f;
        public Color color = new Color(1f, 0.15f, 0.8f, 1f);

        [Header("Hình ảnh (tự tìm theo tên nếu để trống)")]
        public Transform emitterA;
        public Transform emitterB;
        public SpriteRenderer beam;
        public SpriteRenderer beamGlow;

        float t;
        int phase = -1;
        Vector2 a, b;           // hai đầu tia (toạ độ cục bộ)

        void Awake()
        {
            if (emitterA == null) emitterA = transform.Find("EmitterA");
            if (emitterB == null) emitterB = transform.Find("EmitterB");
            if (beam == null) beam = FindSr("Beam");
            if (beamGlow == null) beamGlow = FindSr("BeamGlow");
            t = startOffset;
            Layout();
        }

        void Layout()
        {
            if (vertical) { a = new Vector2(0f, 0.5f); b = new Vector2(0f, Mathf.Max(1f, length)); }
            else { a = new Vector2(-length * 0.5f, horizontalHeight); b = new Vector2(length * 0.5f, horizontalHeight); }
            if (emitterA != null) emitterA.localPosition = a;
            if (emitterB != null) emitterB.localPosition = b;
            // sprite trụ phát hướng lên trên (0°): dọc = dưới bắn lên, trên bắn xuống; ngang = trái bắn sang phải, phải bắn sang trái
            if (emitterA != null) emitterA.localRotation = Quaternion.Euler(0f, 0f, vertical ? 0f : -90f);
            if (emitterB != null) emitterB.localRotation = Quaternion.Euler(0f, 0f, vertical ? 180f : 90f);
        }

        Rect HitRect()
        {
            Vector2 p = transform.position;
            Vector2 mid = p + (a + b) * 0.5f;
            Vector2 size = vertical ? new Vector2(thickness * 0.8f, Mathf.Abs(b.y - a.y)) : new Vector2(Mathf.Abs(b.x - a.x), thickness * 0.8f);
            return Geo.Box(mid, size);
        }

        void Update()
        {
            if (!Running) return;
            t += Time.deltaTime;

            int ph = 2;
            if (!alwaysOn)
            {
                float cycle = Mathf.Max(0.1f, offTime + warnTime + onTime);
                float l = t % cycle;
                ph = l < offTime ? 0 : (l < offTime + warnTime ? 1 : 2);
            }
            if (ph != phase)
            {
                phase = ph;
                if (phase == 1) Sfx.PlayAt("warn", transform.position, 0.3f, 1.4f);
                if (phase == 2) Sfx.PlayAt("laser", transform.position, 0.3f, 1.4f);
            }

            Vector2 mid = (a + b) * 0.5f;
            float len = Vector2.Distance(a, b);
            float flick = 0.8f + 0.2f * Mathf.Sin(Time.time * 60f);
            float w = 0f, alpha = 0f;
            if (phase == 1) { w = thickness * 0.2f; alpha = 0.3f + 0.3f * Mathf.Sin(Time.time * 35f); }
            else if (phase == 2) { w = thickness * flick; alpha = 1f; }

            if (beam != null)
            {
                beam.transform.localPosition = mid;
                beam.transform.localScale = vertical ? new Vector3(w, len, 1f) : new Vector3(len, w, 1f);
                beam.color = Cfg.A(Color.Lerp(color, Color.white, 0.55f), Mathf.Clamp01(alpha));
            }
            if (beamGlow != null)
            {
                beamGlow.transform.localPosition = mid;
                float g = w * 3.5f;
                beamGlow.transform.localScale = vertical ? new Vector3(g, len, 1f) : new Vector3(len, g, 1f);
                beamGlow.color = Cfg.A(color, Mathf.Clamp01(alpha * 0.45f));
            }

            if (phase == 2)
            {
                var pl = Target;
                if (pl != null && HitRect().Overlaps(pl.HurtRect))
                {
                    if (HurtPlayer((Vector2)transform.position + mid))
                        Fx.Sparks(pl.Center, Vector2.up, color, 8, 10f, 60f);
                }
                if (Random.value < 0.25f)
                {
                    Vector2 pt = (Vector2)transform.position + a + (b - a) * Random.value;
                    Fx.Emit(Fx.S_Glow, pt, new Vector2(Random.Range(-1f, 1f), Random.Range(0.5f, 2f)), new Vector2(0.25f, 0.25f), Vector2.zero,
                            Cfg.A(color, 0.9f), Cfg.A(color, 0f), 0.4f);
                }
            }
        }

        void OnDrawGizmosSelected()
        {
            Vector2 p = transform.position;
            Vector2 aa, bb;
            if (vertical) { aa = new Vector2(0f, 0.5f); bb = new Vector2(0f, length); }
            else { aa = new Vector2(-length * 0.5f, horizontalHeight); bb = new Vector2(length * 0.5f, horizontalHeight); }
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(p + aa, p + bb);
        }
    }
}
