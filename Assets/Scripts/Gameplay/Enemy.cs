using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>Lớp cơ sở cho quái nhỏ. Số quái đang sống (Live.Count) được trụ triệu hồi dùng để giới hạn.</summary>
    public abstract class Enemy : Hittable
    {
        public static readonly List<Enemy> Live = new List<Enemy>();

        public Tower owner;
        [Tooltip("Nhân với tỉ lệ rơi vật phẩm hồi máu chung (Level ▸ Drop Chance). 2 = rơi gấp đôi")]
        public float dropChanceMul = 1f;
        protected Flasher flasher;
        protected float flashT;
        protected Color accent = Cfg.Orange;
        protected float dmgMul = 1f, spdMul = 1f;     // hệ số sức mạnh (do trụ đặt khi sinh quái)

        public override bool Targetable { get { return !dead; } }

        /// <summary>Độ cao (y) khi trụ triệu hồi sinh quái này ra (quái bay cao hơn quái đi bộ).</summary>
        public abstract float SpawnHeight { get; }

        /// <summary>Màu đặc trưng (dùng cho hiệu ứng triệu hồi và tia lửa).</summary>
        public virtual Color AccentColor { get { return Cfg.Orange; } }

        /// <summary>Sinh một quái từ Prefab bất kỳ (dùng cho bảng triệu hồi của trụ). power = hệ số sức mạnh.</summary>
        public static Enemy Spawn(GameObject prefab, Vector2 pos, Tower owner, float power)
        {
            if (prefab == null) return null;
            var go = Instantiate(prefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity, Level.Sub("Enemies"));
            go.name = prefab.name;
            var e = go.GetComponent<Enemy>();
            if (e == null) { Debug.LogError("Neon Breaker: Prefab " + prefab.name + " không có script quái (Walker/Flyer)."); Destroy(go); return null; }
            e.owner = owner;
            e.ApplyPower(power);
            return e;
        }

        /// <summary>
        /// Làm quái mạnh hơn. power = 1 là chỉ số gốc trên Prefab; 1.5 = máu x1.5, sát thương x1.3, nhanh hơn ~15%.
        /// Quái mạnh hơn sẽ ngả đỏ dần để người chơi nhận ra. Thời gian báo hiệu đòn giữ nguyên để vẫn công bằng.
        /// </summary>
        public void ApplyPower(float power)
        {
            power = Mathf.Max(0.2f, power);
            maxHp *= power;
            hp = maxHp;
            dmgMul = 1f + (power - 1f) * 0.6f;
            spdMul = 1f + (power - 1f) * 0.3f;
            float k = Mathf.Clamp01((power - 1f) * 0.9f);
            if (k > 0.001f)
            {
                var tint = Color.Lerp(Color.white, new Color(1f, 0.5f, 0.5f, 1f), k);
                var all = GetComponentsInChildren<SpriteRenderer>(true);
                for (int i = 0; i < all.Length; i++)
                    if (all[i].sharedMaterial != Art.AddMat) all[i].color = tint;
                if (flasher != null) flasher.Capture(gameObject);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!Live.Contains(this)) Live.Add(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Live.Remove(this);
        }

        protected bool PlayerActive
        {
            get { return Player.Instance != null && !Player.Instance.Dead && GameManager.Active; }
        }

        public override void TakeHit(float dmg, Vector2 dir, float knockPower, bool melee)
        {
            if (dead) return;
            hp -= dmg;
            flashT = 0.08f;
            Sfx.PlayAt("hit", transform.position, 0.8f, melee ? 1f : 1.3f);
            Fx.Sparks(Center, dir, accent, 7, 11f, 55f);
            Fx.Burst(Center, Cfg.A(accent, 0.9f), 4, 4f, 0.25f, 0.25f);
            Fx.Text(Center + new Vector2(0f, 0.9f), Mathf.RoundToInt(dmg).ToString(), Cfg.Yellow);
            OnHurt(dir, knockPower);
            if (hp <= 0f) Die(dir);
        }

        protected abstract void OnHurt(Vector2 dir, float knockPower);

        protected void Die(Vector2 dir)
        {
            if (dead) return;
            dead = true;
            Fx.Explosion(Center, accent, false);
            Fx.Sparks(Center, dir, Cfg.White, 10, 14f, 50f);
            Fx.Shake(0.12f);
            Sfx.PlayAt("enemyDie", transform.position, 0.9f);
            GameManager.Kills++;
            if (Random.value < Cfg.DropChance * dropChanceMul) Pickup.Drop(Center);
            Destroy(gameObject);
        }

        protected void UpdateFlash(float dt, float alpha = 1f)
        {
            if (flashT > 0f) flashT -= dt;
            if (flasher != null) flasher.Apply(flashT > 0f, alpha);
        }

        protected static float ClampX(float x)
        {
            return Mathf.Clamp(x, Cfg.LevelMinX + 1f, Cfg.GateX - 1.5f);
        }
    }
}
