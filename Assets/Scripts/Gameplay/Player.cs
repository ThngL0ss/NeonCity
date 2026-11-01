using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Nhân vật người chơi.
    ///  A/D  di chuyển      Space  nhảy (giữ để nhảy cao, S+Space xuyên bục)
    ///  J    chém cận chiến  I      lướt (bất tử)
    ///  K    ném phi tiêu    L      hồi máu (có giới hạn lần dùng)
    /// </summary>
    public class Player : MonoBehaviour
    {
        public static Player Instance;

        // ------------------------------------------------------------ state
        public float hp, maxHp;
        public int healCharges;
        public bool Dead;
        public int Facing = 1;

        Rigidbody2D rb;
        BoxCollider2D col;
        bool grounded, wasGrounded;
        Collider2D groundCol;
        float lastVy;

        float coyote, jumpBuf;
        float dashT, dashCd, dashDir = 1f, invulnT, ghostT;
        float atkT, atkCd, atkHitT;
        bool atkPending;
        float shurCd;
        float healT, healCd;
        float hurtT, flashT, squashT;

        public float AtkCdFrac { get { return Mathf.Clamp01(atkCd / Cfg.MeleeCooldown); } }
        public float DashCdFrac { get { return Mathf.Clamp01(dashCd / Cfg.DashCooldown); } }
        public float ShurCdFrac { get { return Mathf.Clamp01(shurCd / Cfg.ShurikenCooldown); } }
        public float HealCdFrac { get { return Mathf.Clamp01(healCd / Cfg.HealCooldown); } }
        public bool IsHealing { get { return healT > 0f; } }
        public bool IsDashing { get { return dashT > 0f; } }

        public float VelocityX { get { return rb != null ? rb.Vel().x : 0f; } }
        public Vector2 Position { get { return transform.position; } }
        public Vector2 Center { get { return (Vector2)transform.position + new Vector2(0f, 0.75f); } }
        public Rect HurtRect { get { return Geo.Box(Center, new Vector2(0.68f, 1.4f)); } }
        public bool Invulnerable { get { return Dead || invulnT > 0f || hurtT > 0f || GameManager.GodMode; } }

        // ------------------------------------------------------------ visual (object thật trong prefab Player)
        [Header("Hình ảnh (tự tìm object con \"Visual\" nếu để trống)")]
        public Transform visual;
        public SpriteAnimator anim;
        Flasher flasher;

        void Awake()
        {
            Instance = this;
            maxHp = Cfg.PlayerMaxHp;
            hp = maxHp;
            healCharges = Cfg.HealStartCharges;
            gameObject.layer = Cfg.LayerPlayer;

            rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.gravityScale = Cfg.GravityDown;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

            col = GetComponent<BoxCollider2D>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider2D>();
                col.size = new Vector2(0.62f, 1.5f);
                col.offset = new Vector2(0f, 0.75f);
            }
            if (col.sharedMaterial == null)
            {
                var mat = new PhysicsMaterial2D("NoFriction");
                mat.friction = 0f;
                mat.bounciness = 0f;
                col.sharedMaterial = mat;
            }

            if (visual == null) { var t = transform.Find("Visual"); if (t != null) visual = t; }
            if (visual != null)
            {
                if (anim == null) anim = visual.GetComponent<SpriteAnimator>();
                flasher = new Flasher(gameObject);
            }
        }

        void OnDestroy() { if (Instance == this) Instance = null; }


        // ------------------------------------------------------------ main loop
        void Update()
        {
            if (Dead) { AnimateDead(); return; }

            float dt = Time.deltaTime;
            bool canAct = GameManager.State == GameState.Playing && !GameManager.Paused && !GameManager.PlayerLocked;

            Vector2 v = rb.Vel();

            // ground check
            Vector2 feet = (Vector2)transform.position + new Vector2(0f, -0.03f);
            Collider2D g = Physics2D.OverlapBox(feet, new Vector2(0.56f, 0.1f), 0f, Cfg.GroundMask);
            grounded = g != null && v.y <= 0.5f;
            groundCol = grounded ? g : null;

            if (grounded && !wasGrounded)
            {
                if (lastVy < -9f)
                {
                    squashT = 0.14f;
                    Sfx.Play("land", 0.6f);
                    Fx.Burst(transform.position, Cfg.A(Cfg.Cyan, 0.8f), 7, 4f, 0.3f, 0.28f, 0f, 3f);
                    Fx.Ring(transform.position + new Vector3(0f, 0.05f, 0f), Cfg.A(Cfg.Cyan, 0.7f), 0.4f, 1.8f, 0.22f);
                }
            }
            wasGrounded = grounded;
            lastVy = v.y;
            if (grounded) coyote = Cfg.CoyoteTime; else coyote -= dt;

            // timers
            jumpBuf -= dt;
            dashCd -= dt; atkCd -= dt; shurCd -= dt; healCd -= dt;
            hurtT -= dt; invulnT -= dt; flashT -= dt; squashT -= dt; ghostT -= dt;
            if (atkT > 0f) atkT -= dt;

            if (canAct) v = HandleInput(dt, v);
            else
            {
                if (dashT > 0f) dashT = 0f;
                v.x = Mathf.MoveTowards(v.x, 0f, 70f * dt);
            }

            // trạng thái đang hồi máu
            if (healT > 0f)
            {
                healT -= dt;
                if (Random.value < 0.6f)
                    Fx.Emit(Fx.S_Glow, Center + new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.6f, 0.4f)), new Vector2(0f, Random.Range(2.5f, 4.5f)),
                            new Vector2(0.3f, 0.3f), Vector2.zero, Cfg.A(Cfg.Lime, 0.9f), Cfg.A(Cfg.Lime, 0f), 0.5f);
                if (healT <= 0f) FinishHeal();
            }

            // lướt
            if (dashT > 0f)
            {
                dashT -= dt;
                v.x = dashDir * Cfg.DashSpeed;
                v.y = 0f;
                rb.gravityScale = 0f;
                if (ghostT <= 0f)
                {
                    ghostT = 0.025f;
                    Fx.Ghost(Center, new Vector2(0.8f, 1.6f), Cfg.Cyan, 0.28f);
                    Fx.Emit(Fx.S_Glow, Center, Vector2.zero, new Vector2(0.9f, 0.9f), Vector2.zero, Cfg.A(Cfg.Magenta, 0.8f), Cfg.A(Cfg.Magenta, 0f), 0.3f);
                }
                if (dashT <= 0f) rb.gravityScale = Cfg.GravityDown;
            }
            else
            {
                rb.gravityScale = (v.y > 0f && GameInput.JumpHeld && canAct) ? Cfg.GravityUp : Cfg.GravityDown;
            }

            // đòn chém: tính thời điểm trúng đòn
            if (atkPending)
            {
                atkHitT -= dt;
                if (atkHitT <= 0f)
                {
                    atkPending = false;
                    DoMeleeHit();
                }
            }

            rb.SetVel(v);
            Animate(dt, v);
        }

        Vector2 HandleInput(float dt, Vector2 v)
        {
            float hx = GameInput.Horizontal;
            bool dashing = dashT > 0f;
            bool healing = healT > 0f;

            if (!dashing && !healing && atkT <= 0f && hx != 0f) Facing = hx > 0f ? 1 : -1;

            if (GameInput.JumpDown) jumpBuf = Cfg.JumpBuffer;

            if (GameInput.DashDown && dashCd <= 0f && !healing) StartDash(hx);
            if (GameInput.AttackDown && atkCd <= 0f && !dashing && !healing) StartAttack(ref v);
            if (GameInput.ShurikenDown && shurCd <= 0f && !dashing && !healing) ThrowShuriken();
            if (GameInput.HealDown && healCd <= 0f && !dashing && !healing) TryStartHeal();

            dashing = dashT > 0f;
            healing = healT > 0f;

            if (dashing) return v;

            if (healing)
            {
                v.x = Mathf.MoveTowards(v.x, 0f, 80f * dt);
                return v;
            }

            float mul = atkT > 0f ? Cfg.MeleeMoveMul : 1f;
            float target = hx * Cfg.PlayerSpeed * mul;
            float accel = grounded ? Cfg.PlayerAccelGround : Cfg.PlayerAccelAir;
            v.x = Mathf.MoveTowards(v.x, target, accel * dt);

            if (jumpBuf > 0f && coyote > 0f)
            {
                if (GameInput.DownHeld && groundCol != null && groundCol.usedByEffector)
                {
                    StartCoroutine(DropThrough(groundCol));
                    jumpBuf = 0f;
                    coyote = 0f;
                }
                else
                {
                    v.y = Cfg.JumpVelocity;
                    jumpBuf = 0f;
                    coyote = 0f;
                    squashT = 0f;
                    Sfx.Play("jump", 0.6f);
                    Fx.Burst(transform.position, Cfg.A(Cfg.Cyan, 0.7f), 5, 3.5f, 0.25f, 0.22f, 0f, 3f);
                }
            }
            return v;
        }

        IEnumerator DropThrough(Collider2D platform)
        {
            if (platform == null) yield break;
            Physics2D.IgnoreCollision(col, platform, true);
            yield return new WaitForSeconds(0.32f);
            if (platform != null && col != null) Physics2D.IgnoreCollision(col, platform, false);
        }

        // ------------------------------------------------------------ skills
        void StartDash(float hx)
        {
            dashDir = hx != 0f ? Mathf.Sign(hx) : Facing;
            Facing = dashDir > 0f ? 1 : -1;
            dashT = Cfg.DashTime;
            dashCd = Cfg.DashCooldown;
            invulnT = Cfg.DashTime + Cfg.DashInvulnExtra;
            atkT = 0f;
            atkPending = false;
            Sfx.Play("dash", 0.8f);
            Fx.Ring(Center, Cfg.A(Cfg.Cyan, 0.9f), 0.5f, 2.4f, 0.25f);
            Fx.Sparks(Center, new Vector2(-dashDir, 0.1f), Cfg.Cyan, 10, 14f, 30f, 0.3f);
        }

        void StartAttack(ref Vector2 v)
        {
            atkT = Cfg.MeleeDuration;
            atkCd = Cfg.MeleeCooldown;
            atkHitT = Cfg.MeleeWindup;
            atkPending = true;
            v.x = Facing * 6.5f;                        // bước tới nhẹ khi chém
            Sfx.Play("slash", 0.7f);
        }

        Rect MeleeRect()
        {
            float x0 = transform.position.x + Facing * 0.05f;
            float x1 = transform.position.x + Facing * (0.05f + Cfg.MeleeReach);
            float xmin = Mathf.Min(x0, x1);
            float w = Mathf.Abs(x1 - x0);
            float ymin = transform.position.y - 0.1f;
            return new Rect(xmin, ymin, w, Cfg.MeleeHeight);
        }

        void DoMeleeHit()
        {
            Rect r = MeleeRect();
            Vector2 sc = r.center;
            Fx.Slash(sc + new Vector2(Facing * 0.1f, 0f), Facing, Cfg.Cyan, 0.95f);
            bool any = false;
            var list = Hittable.Snapshot();
            for (int i = 0; i < list.Count; i++)
            {
                var h = list[i];
                if (h == null || !h.Targetable) continue;
                if (!r.Overlaps(h.HitRect)) continue;
                h.TakeHit(Cfg.MeleeDamage, new Vector2(Facing, 0.15f).normalized, 7f, true);
                any = true;
            }
            if (any)
            {
                Fx.HitStop(0.045f);
                Fx.Shake(0.16f);
            }
        }

        void ThrowShuriken()
        {
            shurCd = Cfg.ShurikenCooldown;
            Vector2 p = new Vector2(transform.position.x + Facing * 0.55f, transform.position.y + 0.98f);
            Projectile.Shuriken(p, new Vector2(Facing, 0f));
            Sfx.Play("throw", 0.7f);
            Fx.Burst(p, Cfg.A(Cfg.Cyan, 0.9f), 4, 4f, 0.2f, 0.2f);
        }

        void TryStartHeal()
        {
            if (healCharges <= 0)
            {
                Fx.Text(Center + new Vector2(0f, 1.2f), Cfg.L("HẾT LƯỢT HỒI", "NO HEALS"), Cfg.A(Cfg.Red, 1f));
                healCd = 0.4f;
                return;
            }
            if (hp >= maxHp - 0.5f)
            {
                Fx.Text(Center + new Vector2(0f, 1.2f), Cfg.L("MÁU ĐẦY", "HP FULL"), Cfg.A(Cfg.Lime, 1f));
                healCd = 0.3f;
                return;
            }
            healCharges--;
            healT = Cfg.HealChannel;
            healCd = Cfg.HealCooldown + Cfg.HealChannel;
            atkT = 0f;
            atkPending = false;
            GameManager.HealsUsed++;
            Sfx.Play("heal", 0.9f);
            Fx.Ring(Center, Cfg.A(Cfg.Lime, 0.9f), 0.4f, 2.6f, 0.45f);
        }

        void FinishHeal()
        {
            float before = hp;
            hp = Mathf.Min(maxHp, hp + Cfg.HealAmount);
            int got = Mathf.RoundToInt(hp - before);
            Fx.Text(Center + new Vector2(0f, 1.3f), "+" + got, Cfg.Lime, 1.3f);
            Fx.Burst(Center, Cfg.Lime, 16, 6f, 0.5f, 0.3f);
            Fx.Flash(Cfg.Lime, 0.18f);
        }

        public bool AddHealCharge()
        {
            if (healCharges >= Cfg.HealMaxCharges) return false;
            healCharges++;
            return true;
        }

        /// <summary>Đẩy người chơi (không gây sát thương).</summary>
        public void Knock(Vector2 impulse)
        {
            if (Dead) return;
            Vector2 v = rb.Vel();
            v.x = impulse.x;
            v.y = impulse.y;
            rb.SetVel(v);
        }

        // ------------------------------------------------------------ damage
        /// <summary>Gây sát thương cho người chơi. Trả về lượng sát thương thực tế (0 nếu né được).</summary>
        public float Hurt(float dmg, Vector2 from, float knock = 7f)
        {
            if (Dead || !GameManager.Active) return 0f;
            if (Invulnerable) return 0f;

            hp -= dmg;
            hurtT = Cfg.HurtInvuln;
            flashT = 0.09f;
            atkT = 0f;
            atkPending = false;
            GameManager.DamageTaken += dmg;

            float dir = transform.position.x >= from.x ? 1f : -1f;
            Vector2 v = rb.Vel();
            v.x = dir * knock;
            v.y = grounded ? 5.5f : Mathf.Max(v.y, 3f);
            rb.SetVel(v);

            Sfx.Play("hurt", 0.9f);
            Fx.Sparks(Center, new Vector2(dir, 0.4f), Cfg.Red, 12, 12f, 60f);
            Fx.Burst(Center, Cfg.A(Cfg.Red, 0.9f), 8, 5f, 0.35f, 0.3f);
            Fx.Text(Center + new Vector2(0f, 1.2f), "-" + Mathf.RoundToInt(dmg), Cfg.Red, 1.2f);
            Fx.Shake(0.38f);
            Fx.HitStop(0.06f);
            Fx.Flash(Cfg.Red, 0.32f);

            if (hp <= 0f)
            {
                hp = 0f;
                Die();
            }
            return dmg;
        }

        void Die()
        {
            Dead = true;
            rb.SetVel(Vector2.zero);
            rb.gravityScale = 0f;
            rb.simulated = false;
            Fx.Explosion(Center, Cfg.Cyan, true);
            Fx.Explosion(Center, Cfg.Magenta, false);
            Fx.Shake(0.8f);
            Sfx.Play("enemyDie", 1f, 0.7f);
            Sfx.Play("hurt", 1f, 0.6f);
            if (visual != null) visual.gameObject.SetActive(false);
            GameManager.OnPlayerDead();
        }

        // ------------------------------------------------------------ animation
        void AnimateDead() { }

        void Animate(float dt, Vector2 v)
        {
            float sx = 1f, sy = 1f;
            if (squashT > 0f) { sx = 1.15f; sy = 0.85f; }
            if (visual != null) visual.localScale = new Vector3(Facing * sx, sy, 1f);

            // chọn clip theo trạng thái (tên clip khớp với danh sách Clips của SpriteAnimator trên prefab)
            string clip;
            if (hurtT > Cfg.HurtInvuln - 0.18f) clip = "hurt";
            else if (healT > 0f) clip = "heal";
            else if (dashT > 0f) clip = "dash";
            else if (atkT > 0f) clip = "attack";
            else if (!grounded) clip = v.y > 0.5f ? "jump" : "fall";
            else if (Mathf.Abs(v.x) > 0.6f) clip = "run";
            else clip = "idle";
            if (anim != null) anim.Play(clip);

            // nhấp nháy khi bị đánh / mờ khi lướt
            float alpha = 1f;
            if (dashT > 0f) alpha = 0.55f;
            else if (hurtT > 0f) alpha = (Mathf.Sin(Time.time * 48f) > 0f) ? 0.3f : 0.9f;
            if (flasher != null) flasher.Apply(flashT > 0f, alpha);
        }
    }
}
