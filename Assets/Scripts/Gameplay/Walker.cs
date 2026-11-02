using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    // ================================================================== Walker
    /// <summary>
    /// Quái chạy bộ. Khi lại gần sẽ "gồng" 0.5s (nhấp nháy + cảnh báo) rồi lao tới.
    /// Né đòn lao rồi phản công trong lúc nó thở = lối chơi hit &amp; run.
    /// Chém trúng khi nó đang gồng sẽ làm nó choáng và hủy đòn lao.
    /// Một script này dùng cho cả 3 loại quái đi bộ (Walker, Heavy, Rusher): chúng chỉ khác nhau ở
    /// chỉ số + hình ảnh trên từng Prefab. Muốn tạo loại mới: nhân đôi một Prefab quái rồi sửa số trong Inspector.
    /// </summary>
    public class Walker : Enemy
    {
        enum S { Chase, Windup, Lunge, Recover, Stagger }

        S st = S.Chase;
        float stT, cd = 0.8f;
        int face = -1;
        bool lungeHit;
        Rigidbody2D rb;

        [Header("Chỉ số (chỉnh trên Prefab)")]
        [Tooltip("Máu (nhân thêm hệ số Power của trụ triệu hồi)")] public float health = 48f;
        public float moveSpeed = 3.7f;
        [Tooltip("Sát thương đòn lao")] public float lungeDamage = 13f;
        [Tooltip("Sát thương khi chạm người (không lao)")] public float touchDamage = 5f;
        [Tooltip("Phát hiện và đuổi người chơi trong khoảng này")] public float aggroRange = 20f;
        [Tooltip("Khoảng cách bắt đầu gồng đòn lao")] public float windupRange = 4.3f;
        [Tooltip("Thời gian báo hiệu trước khi lao (cơ hội né / chém ngắt). Nên >= 0.35 để người chơi kịp phản ứng")] public float windupTime = 0.5f;
        public float lungeSpeed = 13f;
        public float lungeTime = 0.28f;
        [Tooltip("Đứng thở sau khi lao = cơ hội phản công")] public float recoverTime = 0.75f;
        [Tooltip("Thời gian chờ trước khi được lao lần tiếp theo")] public float attackCooldown = 0.9f;
        [Tooltip("Thời gian choáng khi bị chém")] public float staggerTime = 0.3f;
        [Tooltip("Hệ số bị đẩy lùi khi bị chém (1 = bình thường, nhỏ = nặng cân hơn)")] public float knockbackMul = 0.8f;
        [Tooltip("Lực đẩy lùi người chơi khi trúng đòn lao / khi chạm vào quái")] public float lungeKnockback = 8f;
        public float touchKnockback = 5f;
        public float gravityScale = 4f;
        [Tooltip("Hộp trúng đòn của quái khi lao / khi chạm (đơn vị thế giới)")] public Vector2 lungeBoxSize = new Vector2(0.9f, 0.9f);
        public Vector2 touchBoxSize = new Vector2(0.8f, 0.8f);
        [Tooltip("Màu tia lửa khi bị đánh")] public Color accentColor = new Color(1f, 0.6f, 0.1f, 1f);
        [Tooltip("Độ cao (y) khi trụ triệu hồi sinh quái ra")] public float spawnHeight = 0.05f;

        public override float SpawnHeight { get { return spawnHeight; } }
        public override Color AccentColor { get { return accentColor; } }

        [Header("Hình ảnh (tự tìm nếu để trống)")]
        public Transform vis;
        public SpriteAnimator anim;
        public SpriteRenderer warn;                 // vầng đỏ nhấp nháy khi gồng đòn (tên object: "Warn")

        void Reset()
        {
            hitSize = new Vector2(1.0f, 0.95f);
            hitOffset = new Vector2(0f, 0.48f);
        }

        void Awake()
        {
            maxHp = hp = health;
            accent = accentColor;
            gameObject.layer = Cfg.LayerEnemy;

            rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.gravityScale = gravityScale;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
            if (GetComponent<Collider2D>() == null)
            {
                var col = gameObject.AddComponent<BoxCollider2D>();
                col.size = new Vector2(0.9f, 0.9f);
                col.offset = new Vector2(0f, 0.45f);
            }

            if (vis == null) vis = transform.Find("Visual");
            if (vis != null && anim == null) anim = vis.GetComponent<SpriteAnimator>();
            if (warn == null) { var t = transform.Find("Warn"); if (t == null && vis != null) t = vis.Find("Warn"); if (t != null) warn = t.GetComponent<SpriteRenderer>(); }
            flasher = new Flasher(gameObject);
        }

        void Update()
        {
            if (dead) return;
            float dt = Time.deltaTime;
            var p = Player.Instance;
            bool active = PlayerActive;
            Vector2 v = rb.Vel();
            cd -= dt;
            stT -= dt;

            float dx = active ? p.Position.x - transform.position.x : 0f;
            float adx = Mathf.Abs(dx);

            switch (st)
            {
                case S.Chase:
                    if (active && adx < aggroRange)
                    {
                        face = dx >= 0f ? 1 : -1;
                        float want = adx > 1.0f ? face * moveSpeed * spdMul : 0f;
                        v.x = Mathf.MoveTowards(v.x, want, 45f * dt);
                        bool sameLevel = Mathf.Abs(p.Position.y - transform.position.y) < 2.4f;
                        if (adx < windupRange && cd <= 0f && sameLevel)
                        {
                            st = S.Windup;
                            stT = windupTime;
                            Sfx.PlayAt("warn", transform.position, 0.55f, 1.2f);
                            Fx.Text(Center + new Vector2(0f, 1.3f), "!", Cfg.Red, 1.3f);
                        }
                    }
                    else v.x = Mathf.MoveTowards(v.x, 0f, 45f * dt);
                    break;

                case S.Windup:
                    v.x = 0f;
                    if (stT <= 0f)
                    {
                        st = S.Lunge;
                        stT = lungeTime;
                        lungeHit = false;
                        Sfx.PlayAt("dash", transform.position, 0.6f, 1.2f);
                        Fx.Sparks(Center, new Vector2(-face, 0.1f), Cfg.Orange, 6, 9f, 25f, 0.25f);
                    }
                    break;

                case S.Lunge:
                    v.x = face * lungeSpeed * spdMul;
                    if (stT > 0f && Random.value < 0.6f) Fx.Trail(Center, Cfg.A(Cfg.Orange, 0.8f), 0.9f, 0.2f);
                    if (!lungeHit && p != null && !p.Dead && Geo.Box(Center, lungeBoxSize).Overlaps(p.HurtRect))
                    {
                        lungeHit = true;
                        p.Hurt(lungeDamage * dmgMul, transform.position, lungeKnockback);
                    }
                    if (stT <= 0f)
                    {
                        st = S.Recover;
                        stT = recoverTime;
                        cd = attackCooldown;
                    }
                    break;

                case S.Recover:
                    v.x = Mathf.MoveTowards(v.x, 0f, 60f * dt);
                    if (stT <= 0f) st = S.Chase;
                    break;

                case S.Stagger:
                    v.x = Mathf.MoveTowards(v.x, 0f, 28f * dt);
                    if (stT <= 0f) st = S.Chase;
                    break;
            }

            // va chạm thân (nhẹ) khi không lao
            if (st != S.Lunge && p != null && !p.Dead && Geo.Box(Center, touchBoxSize).Overlaps(p.HurtRect))
                p.Hurt(touchDamage * dmgMul, transform.position, touchKnockback);

            // giữ trong khu vực
            Vector3 pos = transform.position;
            if (pos.x < Cfg.LevelMinX + 1f || pos.x > Cfg.GateX - 1.5f)
            {
                pos.x = ClampX(pos.x);
                transform.position = pos;
                v.x = 0f;
            }
            rb.SetVel(v);

            Animate(dt, v);
        }

        void Animate(float dt, Vector2 v)
        {
            if (vis != null)
            {
                float sx = 1f, sy = 1f;
                if (st == S.Windup) { sy = 0.9f; sx = 1.08f; }
                vis.localScale = new Vector3(face * sx, sy, 1f);
            }
            if (anim != null)
            {
                if (st == S.Windup) anim.Play("windup");
                else if (st == S.Lunge) anim.Play("lunge");
                else anim.Play("walk");
            }
            if (warn != null)
            {
                float wa = st == S.Windup ? 0.5f + 0.5f * Mathf.Sin(Time.time * 40f) : 0f;
                warn.color = Cfg.A(Cfg.Red, wa * 0.8f);
            }
            UpdateFlash(dt);
        }

        protected override void OnHurt(Vector2 dir, float knockPower)
        {
            if (st == S.Lunge) return;     // đang lao: không bị ngắt
            st = S.Stagger;
            stT = staggerTime;
            cd = Mathf.Max(cd, 0.5f);
            Vector2 v = rb.Vel();
            v.x = dir.x * knockPower * knockbackMul;
            rb.SetVel(v);
        }
    }
}
