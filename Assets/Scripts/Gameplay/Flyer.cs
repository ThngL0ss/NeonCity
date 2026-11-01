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

        [Header("Hình ảnh (tự tìm nếu để trống)")]
        public Transform vis;
        public SpriteAnimator anim;
        public SpriteRenderer chargeGlow;           // vầng đỏ phình to khi nạp đạn (tên object: "ChargeGlow")

        /// <summary>Sinh một Flyer từ prefab trong ArtBank (ArtBank ▸ Flyer Prefab).</summary>
        public static Flyer Spawn(Vector2 pos, Tower owner)
        {
            var prefab = Art.Bank != null ? Art.Bank.flyerPrefab : null;
            if (prefab == null) { Debug.LogError("Neon Breaker: ArtBank chưa có Flyer Prefab."); return null; }
            var go = Instantiate(prefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity, Level.Sub("Enemies"));
            go.name = "Flyer";
            var f = go.GetComponent<Flyer>();
            if (f != null) f.owner = owner;
            return f;
        }

        void Awake()
        {
            maxHp = hp = Cfg.FlyerHp;
            hitSize = new Vector2(1.0f, 0.9f);
            hitOffset = Vector2.zero;
            accent = Cfg.Magenta;
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
                Vector2 target = new Vector2(p.Position.x + side * 5.2f, p.Position.y + 2.7f + Mathf.Sin(bobT * 2f) * 0.5f);   // đủ thấp để nhảy + chém hoặc phi tiêu trúng
                Vector2 to = target - pos;
                Vector2 want = to.normalized * Mathf.Min(Cfg.FlyerSpeed * spdMul, to.magnitude * 2.2f);
                vel = Vector2.Lerp(vel, want, 1f - Mathf.Exp(-3f * dt));

                if (!charging && fireT <= 0f && Mathf.Abs(p.Position.x - pos.x) < 14f)
                {
                    charging = true;
                    chargeT = 0.55f;
                    Sfx.PlayAt("warn", pos, 0.45f, 1.5f);
                }
                if (charging)
                {
                    chargeT -= dt;
                    float k = 1f - Mathf.Clamp01(chargeT / 0.55f);
                    if (chargeGlow != null)
                    {
                        chargeGlow.transform.localScale = new Vector3(0.4f + 1.4f * k, 0.4f + 1.4f * k, 1f);
                        chargeGlow.color = Cfg.A(Cfg.Red, 0.3f + 0.7f * k);
                    }
                    if (chargeT <= 0f)
                    {
                        charging = false;
                        fireT = Cfg.FlyerFireInterval / spdMul * Random.Range(0.85f, 1.2f);
                        Vector2 aim = p.Center - pos;
                        Projectile.Bolt(pos, aim, 9.5f, Cfg.FlyerBoltDamage * dmgMul, Cfg.Red);
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
            pos.y = Mathf.Max(pos.y, 1.8f);
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
