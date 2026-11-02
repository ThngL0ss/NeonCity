using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    // ================================================================== Flyer
    /// <summary>Máy bay không người lái: lơ lửng quanh người chơi, nạp đạn 0.55s rồi bắn một viên.</summary>
    public class Flyer : Enemy
    {
        Vector2 vel, kv;
        float fireT = 1.5f, chargeT, sideT, bobT;
        int side = 1;
        bool charging;

        [Header("Chỉ số (chỉnh trên Prefab Flyer)")]
        public float health = 30f;
        public float moveSpeed = 5.0f;
        [Tooltip("Sát thương mỗi viên đạn")] public float boltDamage = 9f;
        public float boltSpeed = 9.5f;
        [Tooltip("Thời gian giữa 2 lần bắn")] public float fireInterval = 2.7f;
        [Tooltip("Thời gian nạp đạn (báo hiệu) trước khi bắn")] public float chargeTime = 0.55f;
        [Tooltip("Chỉ bắn khi người chơi ở trong khoảng này (theo trục ngang)")] public float fireRange = 14f;
        [Tooltip("Vị trí lơ lửng so với người chơi: x = cách ngang, y = cao hơn")] public Vector2 hoverOffset = new Vector2(5.2f, 2.7f);
        [Tooltip("Không bay thấp hơn độ cao này")] public float minHeight = 1.8f;
        public Color accentColor = new Color(1f, 0.15f, 0.8f, 1f);
        [Tooltip("Độ cao (y) khi trụ triệu hồi sinh quái ra")] public float spawnHeight = 3.4f;

        public override float SpawnHeight { get { return spawnHeight; } }
        public override Color AccentColor { get { return accentColor; } }

        void Reset()
        {
            hitSize = new Vector2(1.0f, 0.9f);
            hitOffset = Vector2.zero;
        }

        [Header("Hình ảnh (tự tìm nếu để trống)")]
        public Transform vis;
        public SpriteAnimator anim;
        public SpriteRenderer chargeGlow;           // vầng đỏ phình to khi nạp đạn (tên object: "ChargeGlow")

        void Awake()
        {
            maxHp = hp = health;
            accent = accentColor;
            side = Random.value < 0.5f ? -1 : 1;
            bobT = Random.value * 6f;
            gameObject.layer = Cfg.LayerEnemy;

            if (vis == null) vis = transform.Find("Visual");
            if (vis != null && anim == null) anim = vis.GetComponent<SpriteAnimator>();
            if (chargeGlow == null) { var t = transform.Find("ChargeGlow"); if (t == null && vis != null) t = vis.Find("ChargeGlow"); if (t != null) chargeGlow = t.GetComponent<SpriteRenderer>(); }
            if (chargeGlow != null) chargeGlow.color = Cfg.A(Cfg.Red, 0f);
            flasher = new Flasher(gameObject);
        }

        void Update()
        {
            if (dead) return;
            float dt = Time.deltaTime;
            var p = Player.Instance;
            bool active = PlayerActive;
            bobT += dt;
            sideT -= dt;
            fireT -= dt;
            Vector2 pos = transform.position;

            if (active)
            {
                if (sideT <= 0f) { side = Random.value < 0.5f ? -1 : 1; sideT = Random.Range(3f, 6f); }
                Vector2 target = new Vector2(p.Position.x + side * hoverOffset.x, p.Position.y + hoverOffset.y + Mathf.Sin(bobT * 2f) * 0.5f);   // đủ thấp để nhảy + chém hoặc phi tiêu trúng
                Vector2 to = target - pos;
                Vector2 want = to.normalized * Mathf.Min(moveSpeed * spdMul, to.magnitude * 2.2f);
                vel = Vector2.Lerp(vel, want, 1f - Mathf.Exp(-3f * dt));

                if (!charging && fireT <= 0f && Mathf.Abs(p.Position.x - pos.x) < fireRange)
                {
                    charging = true;
                    chargeT = chargeTime;
                    Sfx.PlayAt("warn", pos, 0.45f, 1.5f);
                }
                if (charging)
                {
                    chargeT -= dt;
                    float k = 1f - Mathf.Clamp01(chargeT / Mathf.Max(0.01f, chargeTime));
                    if (chargeGlow != null)
                    {
                        chargeGlow.transform.localScale = new Vector3(0.4f + 1.4f * k, 0.4f + 1.4f * k, 1f);
                        chargeGlow.color = Cfg.A(Cfg.Red, 0.3f + 0.7f * k);
                    }
                    if (chargeT <= 0f)
                    {
                        charging = false;
                        fireT = fireInterval / spdMul * Random.Range(0.85f, 1.2f);
                        Vector2 aim = p.Center - pos;
                        Projectile.Bolt(pos, aim, boltSpeed, boltDamage * dmgMul, Cfg.Red);
                        Sfx.PlayAt("bolt", pos, 0.6f);
                        if (chargeGlow != null) chargeGlow.color = Cfg.A(Cfg.Red, 0f);
                    }
                }
            }
            else
            {
                vel = Vector2.Lerp(vel, Vector2.zero, 1f - Mathf.Exp(-3f * dt));
                charging = false;
                if (chargeGlow != null) chargeGlow.color = Cfg.A(Cfg.Red, 0f);
            }

            kv = Vector2.Lerp(kv, Vector2.zero, 1f - Mathf.Exp(-7f * dt));
            pos += (vel + kv) * dt;
            pos.y = Mathf.Max(pos.y, minHeight);
            pos.x = Mathf.Clamp(pos.x, Cfg.LevelMinX + 1f, Cfg.GateX - 1.5f);
            transform.position = pos;

            float face = active ? Mathf.Sign(p.Position.x - pos.x) : 1f;
            if (vis != null)
            {
                vis.localScale = new Vector3(face == 0f ? 1f : face, 1f, 1f);
                vis.localRotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(-vel.x * 2.5f, -12f, 12f));
            }
            if (anim != null) anim.Play(charging ? "charge" : "fly");

            if (Random.value < 0.25f) Fx.Trail(pos, Cfg.A(Cfg.Magenta, 0.5f), 0.4f, 0.2f);
            UpdateFlash(dt);
        }

        protected override void OnHurt(Vector2 dir, float knockPower)
        {
            kv = dir * knockPower * 1.4f;
            if (charging && Random.value < 0.5f) { charging = false; fireT = 0.8f; if (chargeGlow != null) chargeGlow.color = Cfg.A(Cfg.Red, 0f); }
        }
    }
}
