using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    // ================================================================== Walker
    /// <summary>
    /// Quái chạy bộ. Khi lại gần sẽ "gồng" 0.5s (nhấp nháy + cảnh báo) rồi lao tới.
    /// Né đòn lao rồi phản công trong lúc nó thở = lối chơi hit &amp; run.
    /// Chém trúng khi nó đang gồng sẽ làm nó choáng và hủy đòn lao.
    /// </summary>
    public class Walker : Enemy
    {
        enum S { Chase, Windup, Lunge, Recover, Stagger }

        S st = S.Chase;
        float stT, cd = 0.8f;
        int face = -1;
        bool lungeHit;
        Rigidbody2D rb;

        [Header("Hình ảnh (tự tìm nếu để trống)")]
        public Transform vis;
        public SpriteAnimator anim;
        public SpriteRenderer warn;                 // vầng đỏ nhấp nháy khi gồng đòn (tên object: "Warn")

        /// <summary>Sinh một Walker từ prefab trong ArtBank (ArtBank ▸ Walker Prefab).</summary>
        public static Walker Spawn(Vector2 pos, Tower owner)
        {
            var prefab = Art.Bank != null ? Art.Bank.walkerPrefab : null;
            if (prefab == null) { Debug.LogError("Neon Breaker: ArtBank chưa có Walker Prefab."); return null; }
            var go = Instantiate(prefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity, Level.Sub("Enemies"));
            go.name = "Walker";
            var w = go.GetComponent<Walker>();
            if (w != null) w.owner = owner;
            return w;
        }

        void Awake()
        {
            maxHp = hp = Cfg.WalkerHp;
            hitSize = new Vector2(1.0f, 0.95f);
            hitOffset = new Vector2(0f, 0.48f);
            accent = Cfg.Orange;
            gameObject.layer = Cfg.LayerEnemy;

            rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.gravityScale = 4f;
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
                    if (active && adx < Cfg.WalkerAggroRange)
                    {
                        face = dx >= 0f ? 1 : -1;
                        float want = adx > 1.0f ? face * Cfg.WalkerSpeed * spdMul : 0f;
                        v.x = Mathf.MoveTowards(v.x, want, 45f * dt);
                        bool sameLevel = Mathf.Abs(p.Position.y - transform.position.y) < 2.4f;
                        if (adx < Cfg.WalkerWindupRange && cd <= 0f && sameLevel)
                        {
                            st = S.Windup;
                            stT = Cfg.WalkerWindup;
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
                        stT = Cfg.WalkerLungeTime;
                        lungeHit = false;
                        Sfx.PlayAt("dash", transform.position, 0.6f, 1.2f);
                        Fx.Sparks(Center, new Vector2(-face, 0.1f), Cfg.Orange, 6, 9f, 25f, 0.25f);
                    }
                    break;

                case S.Lunge:
                    v.x = face * Cfg.WalkerLungeSpeed * spdMul;
                    if (stT > 0f && Random.value < 0.6f) Fx.Trail(Center, Cfg.A(Cfg.Orange, 0.8f), 0.9f, 0.2f);
                    if (!lungeHit && p != null && !p.Dead && Geo.Box(Center, new Vector2(0.9f, 0.9f)).Overlaps(p.HurtRect))
                    {
                        lungeHit = true;
                        p.Hurt(Cfg.WalkerLungeDamage * dmgMul, transform.position, 8f);
                    }
                    if (stT <= 0f)
                    {
                        st = S.Recover;
                        stT = Cfg.WalkerRecover;
                        cd = 0.9f;
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
            if (st != S.Lunge && p != null && !p.Dead && Geo.Box(Center, new Vector2(0.8f, 0.8f)).Overlaps(p.HurtRect))
                p.Hurt(Cfg.WalkerTouchDamage * dmgMul, transform.position, 5f);

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
            stT = 0.3f;
            cd = Mathf.Max(cd, 0.5f);
            Vector2 v = rb.Vel();
            v.x = dir.x * knockPower * 0.8f;
            rb.SetVel(v);
        }
    }
}
