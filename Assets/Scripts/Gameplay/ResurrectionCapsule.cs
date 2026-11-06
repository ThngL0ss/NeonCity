using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Lồng kính hồi sinh ở căn cứ (nơi người chơi xuất hiện khi chết). Object con: "Glow" (vầng sáng bên trong), "Glass" (kính phía trước người chơi).
    /// BaseManager điều khiển hiệu ứng: SetCharge = độ sáng, Open = kính mờ đi khi hồi sinh xong.
    /// </summary>
    public class ResurrectionCapsule : MonoBehaviour
    {
        public SpriteRenderer glow;
        public SpriteRenderer glass;
        [Tooltip("Tâm bên trong lồng so với chân lồng")] public Vector2 centerOffset = new Vector2(0f, 1.8f);
        [Tooltip("Đỉnh lồng (nơi sét giáng vào) so với chân lồng")] public Vector2 topOffset = new Vector2(0f, 4.2f);

        float charge, targetCharge;
        float glassAlpha = 0.55f;
        Color glowCol = Cfg.Cyan;

        public Vector2 Center { get { return (Vector2)transform.position + centerOffset; } }
        public Vector2 Top { get { return (Vector2)transform.position + topOffset; } }

        void Awake()
        {
            if (glow == null) { var g = transform.Find("Glow"); if (g != null) glow = g.GetComponent<SpriteRenderer>(); }
            if (glass == null) { var g = transform.Find("Glass"); if (g != null) glass = g.GetComponent<SpriteRenderer>(); }
        }

        public void SetCharge(float c) { targetCharge = Mathf.Clamp01(c); }

        /// <summary>Kính mờ đi (hồi sinh xong).</summary>
        public void Open() { glassAlpha = 0.18f; targetCharge = 0f; }

        void Update()
        {
            charge = Mathf.MoveTowards(charge, targetCharge, Time.unscaledDeltaTime * 2.5f);
            float flick = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 31f) * Mathf.Sin(Time.unscaledTime * 9f);
            if (glow != null)
            {
                glow.color = Cfg.A(Color.Lerp(glowCol, Color.white, charge * 0.6f), (0.12f + 0.8f * charge) * flick);
                float sc = 3.2f + 2.2f * charge;
                glow.transform.localScale = new Vector3(sc, sc * 1.6f, 1f);
            }
            if (glass != null) glass.color = new Color(1f, 1f, 1f, glassAlpha + 0.25f * charge);
            if (charge > 0.05f && Random.value < charge * 0.5f)
                Fx.Emit(Fx.S_Glow, Center + new Vector2(Random.Range(-1f, 1f), Random.Range(-1.4f, 1.6f)), new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(0.5f, 2.5f)),
                        new Vector2(0.25f, 0.25f), Vector2.zero, Cfg.A(Cfg.Cyan, 0.9f), Cfg.A(Cfg.Cyan, 0f), 0.5f);
        }
    }
}
