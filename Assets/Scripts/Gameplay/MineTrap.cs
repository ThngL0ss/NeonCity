using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Mìn năng lượng: ban đầu TÀNG HÌNH. Khi người chơi giẫm lên mìn (đứng sát mìn và chân gần mặt đất) thì mìn lộ ra,
    /// đèn đỏ nhấp nháy rồi NỔ sau Fuse Time giây, gây sát thương trong bán kính Blast Radius (vòng đỏ cho biết vùng nổ).
    /// Chạy ra khỏi vòng đỏ hoặc lướt (I) để né. Mìn chỉ nổ MỘT lần rồi biến mất. Nhảy qua mìn thì không kích hoạt.
    /// Có thể bật Hurts Enemies để vụ nổ cũng gây sát thương cho quái.
    /// </summary>
    public class MineTrap : Trap
    {
        [Header("Kích nổ")]
        [Tooltip("Người chơi đứng cách tâm mìn trong khoảng ngang này (và đang ở sát mìn) thì coi là giẫm phải")] public float stepRadius = 0.9f;
        [Tooltip("Chân người chơi cao hơn chân mìn quá khoảng này thì không kích hoạt (nhảy qua mìn)")] public float stepHeight = 0.7f;
        [Tooltip("Thời gian đếm ngược sau khi mìn lộ ra, trước khi nổ (giây)")] public float fuseTime = 0.6f;

        [Header("Vụ nổ")]
        [Tooltip("Bán kính vùng nổ gây sát thương")] public float blastRadius = 2.8f;
        [Tooltip("true = vụ nổ cũng gây sát thương cho quái nhỏ (dụ quái vào bẫy)")] public bool hurtsEnemies = false;
        [Tooltip("Tâm vụ nổ / tâm mìn so với chân bẫy")] public Vector2 centerOffset = new Vector2(0f, 0.3f);

        [Header("Hình ảnh (tự tìm nếu để trống)")]
        public SpriteAnimator anim;
        public SpriteRenderer dangerGlow;    // object con "Danger": vòng đỏ vùng nổ

        enum S { Hidden, Fuse }
        S state = S.Hidden;
        float timer;

        Vector2 Center { get { return (Vector2)transform.position + centerOffset; } }

        void Awake()
        {
            if (anim == null) anim = FindAnim();
            if (dangerGlow == null) dangerGlow = FindSr("Danger");
            if (dangerGlow != null)
            {
                dangerGlow.transform.localPosition = centerOffset;
                dangerGlow.transform.localScale = new Vector3(blastRadius * 2f, blastRadius * 2f, 1f);
                dangerGlow.color = Cfg.A(Cfg.Red, 0f);
            }
            if (anim != null) anim.Play("idle");
            SetVisible(false);                 // tàng hình cho tới khi bị giẫm
        }

        void SetVisible(bool on)
        {
            for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(on);
        }

        void Update()
        {
            if (!GameManager.Active) return;
            float dt = Time.deltaTime;

            if (state == S.Hidden)
            {
                if (!Running) return;
                var pl = Target;
                if (pl == null) return;
                Vector2 feet = pl.Position;
                bool stepped = Mathf.Abs(feet.x - transform.position.x) <= stepRadius && feet.y - transform.position.y <= stepHeight && feet.y - transform.position.y >= -1f;
                if (!stepped) return;

                // lộ ra
                state = S.Fuse;
                timer = Mathf.Max(0.1f, fuseTime);
                SetVisible(true);
                if (anim != null) anim.Play("armed", true);
                Sfx.PlayAt("warn", Center, 0.6f, 1.8f);
                Fx.Flare(Center, Cfg.Red, 1.6f, 0.2f);
                Fx.Ring(Center, Cfg.A(Cfg.Red, 0.9f), 0.3f, 1.6f, 0.25f);
                Fx.Text(Center + new Vector2(0f, 1.3f), Cfg.L("MÌN!", "MINE!"), Cfg.Red, 1.2f);
                return;
            }

            // đang đếm ngược
            timer -= dt;
            if (dangerGlow != null)
                dangerGlow.color = Cfg.A(Cfg.Red, 0.15f + 0.2f * Mathf.Abs(Mathf.Sin(Time.time * 18f)));
            if (timer <= 0f) Explode(Target);
        }

        void Explode(Player pl)
        {
            Vector2 c = Center;
            Fx.Explosion(c, Cfg.Orange, true);
            Fx.Shake(0.35f);
            Sfx.PlayAt("slam", c, 0.9f, 1.1f);
            Fx.Ring(c, Cfg.A(Cfg.Red, 0.9f), 0.5f, blastRadius * 2f, 0.35f);

            if (pl != null && !pl.Dead && Geo.CircleRect(c, blastRadius, pl.HurtRect)) pl.Hurt(damage, c, knockback);
            HurtBotsCircle(c, blastRadius, c);

            if (hurtsEnemies)
            {
                var list = new System.Collections.Generic.List<Enemy>(Enemy.Live);
                for (int i = 0; i < list.Count; i++)
                {
                    var e = list[i];
                    if (e == null || e.dead || !Geo.CircleRect(c, blastRadius, e.HitRect)) continue;
                    e.TakeHit(damage * 2f, (e.Center - c).normalized, 6f, false);
                }
            }

            Destroy(gameObject);               // chỉ nổ một lần
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere((Vector2)transform.position + centerOffset, stepRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere((Vector2)transform.position + centerOffset, blastRadius);
        }
    }
}
