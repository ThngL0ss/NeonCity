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

        // ------------------------------------------------------------ chỉ số (chỉnh trong Inspector của Prefab Player)
        [Header("Cấp bậc & cấp độ (chưa áp dụng vào chỉ số)")]
        [Tooltip("Cấp bậc F..SSS và cấp độ 1..200 của người chơi. Hiện chỉ lưu dữ liệu, chưa ảnh hưởng chiến đấu")] public PowerInfo power = new PowerInfo();

        [Header("Sinh tồn")]
        [Tooltip("Máu tối đa")] public float maxHealth = 100f;
        [Tooltip("Thời gian bất tử sau khi trúng đòn (giây)")] public float hurtInvuln = 0.85f;
        [Tooltip("Số lượt hồi máu lúc bắt đầu")] public int healStartCharges = 3;
        [Tooltip("Số lượt hồi máu tối đa (nhặt vật phẩm cộng dồn đến mức này)")] public int healMaxCharges = 5;
        [Tooltip("Kích thước vùng nhận sát thương (hurtbox)")] public Vector2 hurtBoxSize = new Vector2(0.68f, 1.4f);
        [Tooltip("Lực nảy lên khi bị đánh lúc đang đứng đất / đang ở trên không")] public Vector2 hurtHopSpeed = new Vector2(5.5f, 3f);

        [Header("Di chuyển & nhảy")]
        public float moveSpeed = 8.2f;
        public float accelGround = 95f;
        public float accelAir = 55f;
        public float jumpVelocity = 15.5f;
        [Tooltip("Trọng lực khi đang bay lên và giữ nút nhảy (nhỏ = nhảy cao hơn)")] public float gravityUp = 3.4f;
        [Tooltip("Trọng lực bình thường / khi rơi")] public float gravityDown = 4.8f;
        [Tooltip("Cho phép nhảy trễ sau khi rời mép bục (giây)")] public float coyoteTime = 0.10f;
        [Tooltip("Nhớ nút nhảy bấm sớm trước khi chạm đất (giây)")] public float jumpBuffer = 0.12f;

        [Header("Chém cận chiến (J) - tầm ngắn, buộc phải áp sát")]
        public float meleeDamage = 20f;
        [Tooltip("Tầm chém (đơn vị thế giới)")] public float meleeReach = 2.1f;
        public float meleeHeight = 2.1f;
        [Tooltip("Từ lúc bấm đến lúc đòn trúng (giây)")] public float meleeWindup = 0.07f;
        [Tooltip("Tổng thời gian một đòn chém")] public float meleeDuration = 0.28f;
        public float meleeCooldown = 0.36f;
        [Tooltip("Hệ số tốc độ chạy khi đang chém")] public float meleeMoveMul = 0.40f;
        [Tooltip("Tốc độ bước tới khi chém")] public float meleeStepSpeed = 6.5f;
        [Tooltip("Lực đẩy lùi kẻ địch")] public float meleeKnockback = 7f;

        [Header("Lướt (I) - bất tử trong lúc lướt")]
        public float dashSpeed = 25f;
        public float dashTime = 0.19f;
        public float dashCooldown = 1.5f;
        [Tooltip("Thời gian bất tử kéo dài thêm sau khi lướt xong")] public float dashInvulnExtra = 0.06f;

        [Header("Phi tiêu (K)")]
        public float shurikenDamage = 13f;
        public float shurikenSpeed = 21f;
        public float shurikenRange = 13f;
        public float shurikenCooldown = 0.55f;
        [Tooltip("Số kẻ địch phi tiêu xuyên qua thêm")] public int shurikenPierce = 2;

        [Header("Hồi máu (L)")]
        public float healAmount = 38f;
        [Tooltip("Thời gian đứng yên khi hồi (rủi ro)")] public float healChannel = 0.50f;
        public float healCooldown = 1.2f;

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

        public float AtkCdFrac { get { return Mathf.Clamp01(atkCd / meleeCooldown); } }
        public float DashCdFrac { get { return Mathf.Clamp01(dashCd / DashCooldownNow); } }
        /// <summary>Thời gian hồi lướt thực tế (đã tính vật phẩm).</summary>
        public float DashCooldownNow { get { return dashCooldown * Inventory.Mul(ItemStat.DashCooldownPct); } }
        public float ShurCdFrac { get { return Mathf.Clamp01(shurCd / shurikenCooldown); } }
        public float HealCdFrac { get { return Mathf.Clamp01(healCd / healCooldown); } }
        public bool IsHealing { get { return healT > 0f; } }
        public bool IsDashing { get { return dashT > 0f; } }
        public bool Stunned { get { return stunT > 0f; } }
        float stunT;

        public float VelocityX { get { return rb != null ? rb.Vel().x : 0f; } }
        public Vector2 Position { get { return transform.position; } }
        public Vector2 Center { get { return (Vector2)transform.position + new Vector2(0f, 0.75f); } }
        public Rect HurtRect { get { return Geo.Box(Center, hurtBoxSize); } }
        public bool Invulnerable { get { return Dead || invulnT > 0f || hurtT > 0f || GameManager.Cheat; } }

        // ------------------------------------------------------------ visual (object thật trong prefab Player)
        [Header("Hình ảnh (tự tìm object con \"Visual\" nếu để trống)")]
        public Transform visual;
        public SpriteAnimator anim;
        Flasher flasher;

        void Awake()
        {
            Instance = this;
            maxHp = maxHealth + Inventory.Stat(ItemStat.MaxHealth);
            hp = maxHp;
            healCharges = healStartCharges;
            gameObject.layer = Cfg.LayerPlayer;

            rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.gravityScale = gravityDown;
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
            stunT -= dt;
            bool canAct = GameManager.State == GameState.Playing && !GameManager.Paused && !GameManager.PlayerLocked && !SkillSystem.PanelOpen && stunT <= 0f;

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
            if (grounded) coyote = coyoteTime; else coyote -= dt;

            // timers
            jumpBuf -= dt;
            dashCd -= dt; atkCd -= dt; shurCd -= dt; healCd -= dt;
            if (GameManager.Cheat) { dashCd = 0f; atkCd = 0f; shurCd = 0f; healCd = 0f; }   // chế độ thử nghiệm (P): không hồi chiêu
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
                v.x = dashDir * dashSpeed;
                v.y = 0f;
                rb.gravityScale = 0f;
                if (ghostT <= 0f)
                {
                    ghostT = 0.025f;
                    Fx.Ghost(Center, new Vector2(0.8f, 1.6f), Cfg.Cyan, 0.28f);
                    Fx.Emit(Fx.S_Glow, Center, Vector2.zero, new Vector2(0.9f, 0.9f), Vector2.zero, Cfg.A(Cfg.Magenta, 0.8f), Cfg.A(Cfg.Magenta, 0f), 0.3f);
                }
                if (dashT <= 0f) rb.gravityScale = gravityDown;
            }
            else
            {
                rb.gravityScale = (v.y > 0f && GameInput.JumpHeld && canAct) ? gravityUp : gravityDown;
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

            if (GameInput.JumpDown) jumpBuf = jumpBuffer;

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

            float mul = atkT > 0f ? meleeMoveMul : 1f;
            float target = hx * moveSpeed * Inventory.Mul(ItemStat.MoveSpeedPct) * mul;
            float accel = grounded ? accelGround : accelAir;
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
                    v.y = jumpVelocity;
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
            dashT = dashTime;
            dashCd = DashCooldownNow;
            invulnT = dashTime + dashInvulnExtra;
            atkT = 0f;
            atkPending = false;
            Sfx.Play("dash", 0.8f);
            Fx.Ring(Center, Cfg.A(Cfg.Cyan, 0.9f), 0.5f, 2.4f, 0.25f);
            Fx.Sparks(Center, new Vector2(-dashDir, 0.1f), Cfg.Cyan, 10, 14f, 30f, 0.3f);
        }

        void StartAttack(ref Vector2 v)
        {
            atkT = meleeDuration;
            atkCd = meleeCooldown;
            atkHitT = meleeWindup;
            atkPending = true;
            v.x = Facing * meleeStepSpeed;                        // bước tới nhẹ khi chém
            Sfx.Play("slash", 0.7f);
        }

        Rect MeleeRect()
        {
            float x0 = transform.position.x + Facing * 0.05f;
            float x1 = transform.position.x + Facing * (0.05f + meleeReach);
            float xmin = Mathf.Min(x0, x1);
            float w = Mathf.Abs(x1 - x0);
            float ymin = transform.position.y - 0.1f;
            return new Rect(xmin, ymin, w, meleeHeight);
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
                h.TakeHit(meleeDamage * GameManager.DamageMul * Inventory.Mul(ItemStat.MeleeDamagePct), new Vector2(Facing, 0.15f).normalized, meleeKnockback, true);
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
            shurCd = shurikenCooldown;
            Vector2 p = new Vector2(transform.position.x + Facing * 0.55f, transform.position.y + 0.98f);
            Projectile.Shuriken(p, new Vector2(Facing, 0f), shurikenSpeed, shurikenDamage * GameManager.DamageMul * Inventory.Mul(ItemStat.ShurikenDamagePct), shurikenPierce, shurikenRange);
            Sfx.Play("throw", 0.7f);
            Fx.Burst(p, Cfg.A(Cfg.Cyan, 0.9f), 4, 4f, 0.2f, 0.2f);
        }

        void TryStartHeal()
        {
            if (healCharges <= 0 && !GameManager.Cheat)
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
            if (!GameManager.Cheat) healCharges--;
            healT = healChannel;
            healCd = healCooldown + healChannel;
            atkT = 0f;
            atkPending = false;
            GameManager.HealsUsed++;
            Sfx.Play("heal", 0.9f);
            Fx.Ring(Center, Cfg.A(Cfg.Lime, 0.9f), 0.4f, 2.6f, 0.45f);
        }

        void FinishHeal()
        {
            float before = hp;
            hp = Mathf.Min(maxHp, hp + healAmount * Inventory.Mul(ItemStat.HealPowerPct));
            int got = Mathf.RoundToInt(hp - before);
            Fx.Text(Center + new Vector2(0f, 1.3f), "+" + got, Cfg.Lime, 1.3f);
            Fx.Burst(Center, Cfg.Lime, 16, 6f, 0.5f, 0.3f);
            Fx.Flash(Cfg.Lime, 0.18f);
        }

        /// <summary>Tính lại máu tối đa khi vật phẩm thay đổi (nhặt / dùng / hết buff). Tăng thêm bao nhiêu thì hồi bấy nhiêu.</summary>
        public void RecalcStats()
        {
            float newMax = Mathf.Max(1f, maxHealth + Inventory.Stat(ItemStat.MaxHealth));
            if (Mathf.Abs(newMax - maxHp) < 0.01f) return;
            float diff = newMax - maxHp;
            maxHp = newMax;
            if (diff > 0f) hp += diff;
            hp = Mathf.Min(hp, maxHp);
        }

        public bool AddHealCharge()
        {
            if (healCharges >= healMaxCharges) return false;
            healCharges++;
            return true;
        }

        /// <summary>Làm người chơi choáng (không di chuyển / đánh / lướt / hồi máu được) trong dur giây.</summary>
        public void Stun(float dur)
        {
            if (Dead || dur <= 0f) return;
            stunT = Mathf.Max(stunT, dur);
            dashT = 0f; healT = 0f; atkT = 0f;
            atkPending = false;
            Fx.Text(Center + new Vector2(0f, 1.5f), Cfg.L("CHOÁNG!", "STUNNED!"), Cfg.Yellow, 1.3f);
            Sfx.Play("warn", 0.7f, 1.7f);
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
            dmg *= Inventory.Mul(ItemStat.DamageTakenPct);

            hp -= dmg;
            hurtT = hurtInvuln;
            flashT = 0.09f;
            atkT = 0f;
            atkPending = false;
            GameManager.DamageTaken += dmg;

            float dir = transform.position.x >= from.x ? 1f : -1f;
            Vector2 v = rb.Vel();
            v.x = dir * knock;
            v.y = grounded ? hurtHopSpeed.x : Mathf.Max(v.y, hurtHopSpeed.y);
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

        /// <summary>
        /// Giết người chơi bất kể đang né / có giáp / vừa bị đánh (dùng cho vụ nổ tự huỷ của boss).
        /// Chế độ thử nghiệm (phím P) vẫn bảo vệ người chơi. Trả về true nếu người chơi đã chết.
        /// </summary>
        public bool Kill(Vector2 from)
        {
            if (Dead) return true;
            if (!GameManager.Active || GameManager.Cheat) return false;
            hp = 0f;
            flashT = 0.2f;
            Fx.Text(Center + new Vector2(0f, 1.2f), "-9999", Cfg.Red, 1.6f);
            Die();
            return true;
        }

        void Die()
        {
            Dead = true;
            rb.SetVel(Vector2.zero);
            rb.gravityScale = 0f;
            rb.simulated = false;
            stunT = 0f;
            Fx.Sparks(Center, Vector2.up, Cfg.Cyan, 14, 10f, 90f, 0.5f);
            Fx.Shake(0.8f);
            Sfx.Play("enemyDie", 1f, 0.7f);
            Sfx.Play("hurt", 1f, 0.6f);
            GameManager.OnPlayerDead();       // chạy cinematic chết (thân xác vẫn hiện để cảnh quay dùng)
        }

        /// <summary>Chuẩn bị hình cho cinematic chết: dừng hoạt ảnh, giữ khung "hurt", bỏ nhấp nháy. Trả về SpriteRenderer của thân.</summary>
        public SpriteRenderer FreezeVisual(string clipName = "hurt")
        {
            if (visual == null) return null;
            visual.gameObject.SetActive(true);
            var sr = visual.GetComponent<SpriteRenderer>();
            if (anim != null)
            {
                Sprite hurt = null;
                if (anim.clips != null)
                    for (int i = 0; i < anim.clips.Length; i++)
                        if (anim.clips[i] != null && anim.clips[i].name == clipName && anim.clips[i].frames != null && anim.clips[i].frames.Length > 0) { hurt = anim.clips[i].frames[0]; break; }
                anim.enabled = false;
                if (sr != null && hurt != null) sr.sprite = hurt;
            }
            if (flasher != null) flasher.Apply(false, 1f);
            visual.localScale = new Vector3(Facing >= 0 ? 1f : -1f, 1f, 1f);
            visual.localRotation = Quaternion.identity;
            return sr;
        }

        /// <summary>Trả hoạt ảnh bình thường sau FreezeVisual (cảnh hồi sinh ở căn cứ).</summary>
        public void ReleaseVisual()
        {
            if (anim != null) anim.enabled = true;
            if (visual != null) visual.localRotation = Quaternion.identity;
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
            if (stunT > 0f)
            {
                clip = "hurt";
                if (Random.value < 0.5f)
                {
                    float a = Time.time * 12f;
                    Vector2 hp = Center + new Vector2(Mathf.Cos(a) * 0.5f, 0.95f + Mathf.Sin(a) * 0.15f);
                    Fx.Emit(Fx.S_Star, hp, Vector2.zero, new Vector2(0.28f, 0.28f), new Vector2(0.1f, 0.1f), Cfg.Yellow, Cfg.A(Cfg.Yellow, 0f), 0.3f);
                }
            }
            else if (hurtT > hurtInvuln - 0.18f) clip = "hurt";
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
