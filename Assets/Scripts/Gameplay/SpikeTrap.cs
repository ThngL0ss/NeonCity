using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Bẫy gai sàn: thụt vào, báo hiệu (gai nhú lên + vầng sáng đỏ), rồi bật gai lên gây sát thương. Lặp lại theo chu kỳ.
    /// Nhảy qua khi gai thụt xuống, hoặc lướt (I) xuyên qua.
    /// Đặt chân bẫy sát mặt đất (y = 0) hoặc trên mặt bục.
    /// </summary>
    public class SpikeTrap : Trap
    {
        [Header("Chu kỳ gai")]
        [Tooltip("true = gai luôn nhô lên (bẫy tĩnh)")] public bool alwaysUp = false;
        [Tooltip("Thời gian gai thụt xuống, an toàn (giây)")] public float downTime = 1.6f;
        [Tooltip("Thời gian báo hiệu trước khi gai bật lên (giây)")] public float warnTime = 0.55f;
        [Tooltip("Thời gian gai nhô lên, nguy hiểm (giây)")] public float upTime = 1.0f;
        [Tooltip("Lệch pha lúc bắt đầu (giây) để nhiều bẫy không nhịp nhàng cùng lúc")] public float startOffset = 0f;

        [Header("Vùng gây sát thương (so với chân bẫy)")]
        public Vector2 hitSize = new Vector2(2.8f, 0.9f);
        public Vector2 hitOffset = new Vector2(0f, 0.45f);

        [Header("Hình ảnh (tự tìm nếu để trống)")]
        public SpriteAnimator anim;
        public SpriteRenderer glow;

        float t;
        int phase = -1;     // 0 thụt · 1 báo hiệu · 2 nhô lên

        void Awake()
        {
            if (anim == null) anim = FindAnim();
            if (glow == null) glow = FindSr("Glow");
            t = startOffset;
        }

        void Update()
        {
            if (!Running) return;
            t += Time.deltaTime;

            int ph = 2;
            if (!alwaysUp)
            {
                float cycle = Mathf.Max(0.1f, downTime + warnTime + upTime);
                float l = t % cycle;
                ph = l < downTime ? 0 : (l < downTime + warnTime ? 1 : 2);
            }
            if (ph != phase) { phase = ph; OnPhase(ph); }

            if (glow != null)
            {
                float a = 0f;
                if (phase == 1) a = 0.35f + 0.35f * Mathf.Sin(Time.time * 30f);
                else if (phase == 2) a = 0.3f;
                glow.color = Cfg.A(phase == 1 ? Cfg.Red : Cfg.Orange, Mathf.Clamp01(a));
            }

            if (phase == 2)
            {
                var pl = Target;
                Rect box = Geo.Box((Vector2)transform.position + hitOffset, hitSize);
                if (pl != null && box.Overlaps(pl.HurtRect))
                    HurtPlayer(transform.position);
                HurtBotsRect(box, transform.position);
            }
        }

        void OnPhase(int ph)
        {
            Vector2 pos = transform.position;
            if (ph == 0) { if (anim != null) anim.Play("down"); }
            else if (ph == 1)
            {
                if (anim != null) anim.Play("warn");
                Sfx.PlayAt("warn", pos, 0.3f, 1.8f);
            }
            else
            {
                if (anim != null) anim.Play("up");
                Sfx.PlayAt("slam", pos, 0.35f, 1.7f);
                Fx.Sparks(pos + new Vector2(0f, 0.3f), Vector2.up, Cfg.Orange, 6, 9f, 40f, 0.25f);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube((Vector2)transform.position + hitOffset, hitSize);
        }
    }
}
