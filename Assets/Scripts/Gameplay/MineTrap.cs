using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Mìn năng lượng: nằm im, khi người chơi lại gần (Trigger Radius) thì đèn đỏ nhấp nháy rồi NỔ sau Fuse Time giây,
    /// gây sát thương trong bán kính Blast Radius (vòng đỏ trên sàn cho biết vùng nổ). Chạy ra khỏi vòng đỏ hoặc lướt (I) để né.
    /// Sau khi nổ, mìn nạp lại sau Rearm Time giây (0 = chỉ nổ một lần). Có thể dụ quái vào vùng nổ (Hurts Enemies).
    /// </summary>
    public class MineTrap : Trap
    {
        [Header("Kích nổ")]
        [Tooltip("Người chơi lại gần trong bán kính này thì mìn bắt đầu đếm ngược")] public float triggerRadius = 2.6f;
        [Tooltip("Thời gian đếm ngược trước khi nổ (giây)")] public float fuseTime = 0.85f;

        [Header("Vụ nổ")]
        [Tooltip("Bán kính vùng nổ gây sát thương")] public float blastRadius = 2.8f;
        [Tooltip("true = vụ nổ cũng gây sát thương cho quái nhỏ (dụ quái vào bẫy)")] public bool hurtsEnemies = false;
        [Tooltip("Sau khi nổ bao lâu thì mìn nạp lại (giây). 0 = chỉ nổ một lần rồi biến mất")] public float rearmTime = 10f;
        [Tooltip("Tâm vụ nổ / tâm mìn so với chân bẫy")] public Vector2 centerOffset = new Vector2(0f, 0.3f);

        [Header("Hình ảnh (tự tìm nếu để trống)")]
        public SpriteAnimator anim;
        public SpriteRenderer dangerGlow;    // object con "Danger": vòng đỏ vùng nổ

        enum S { Armed, Fuse, Gone }
        S state = S.Armed;
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
        }

        void SetVisible(bool on)
        {
            for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(on);
        }

        void Update()
        {
            if (!GameManager.Active) return;
            float dt = Time.deltaTime;

            if (state == S.Gone)
            {
                if (rearmTime <= 0f) return;
                timer -= dt;
                if (timer <= 0f)
                {
                    var p = Target;
                    if (p != null && Vector2.Distance(p.Center, Center) < triggerRadius + 1f) return;   // đợi người chơi rời xa rồi mới nạp lại
                    state = S.Armed;
                    SetVisible(true);
                    if (anim != null) anim.Play("idle", true);
                    Awake();
                }
                return;
            }

            if (!Running) return;
            var pl = Target;

            if (state == S.Armed)
            {
                if (pl != null && Vector2.Distance(pl.Center, Center) <= triggerRadius)
                {
                    state = S.Fuse;
                    timer = Mathf.Max(0.1f, fuseTime);
                    if (anim != null) anim.Play("armed", true);
                    Sfx.PlayAt("warn", Center, 0.5f, 1.7f);
                }
            }
            else if (state == S.Fuse)
            {
                timer -= dt;
                if (dangerGlow != null)
                    dangerGlow.color = Cfg.A(Cfg.Red, 0.15f + 0.2f * Mathf.Abs(Mathf.Sin(Time.time * 18f)));
                if (timer <= 0f) Explode(pl);
            }
        }

        void Explode(Player pl)
        {
            Vector2 c = Center;
            Fx.Explosion(c, Cfg.Orange, true);
            Fx.Shake(0.35f);
            Sfx.PlayAt("slam", c, 0.9f, 1.1f);
            Fx.Ring(c, Cfg.A(Cfg.Red, 0.9f), 0.5f, blastRadius * 2f, 0.35f);

            if (pl != null && !pl.Dead && Geo.CircleRect(c, blastRadius, pl.HurtRect)) pl.Hurt(damage, c, knockback);

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

            if (rearmTime <= 0f) { Destroy(gameObject); return; }
            state = S.Gone;
            timer = rearmTime;
            SetVisible(false);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere((Vector2)transform.position + centerOffset, triggerRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere((Vector2)transform.position + centerOffset, blastRadius);
        }
    }
}
