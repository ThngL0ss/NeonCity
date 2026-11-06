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
        [Header("Rơi xu (tiền)")]
        [Tooltip("Xác suất rơi xu khi bị hạ (1 = luôn rơi)"), Range(0f, 1f)] public float coinChance = 0.9f;
        [Tooltip("Số xu rơi ra ít nhất / nhiều nhất (quái do trụ mạnh sinh ra rơi nhiều hơn theo Power)")] public int coinMin = 1;
        public int coinMax = 3;
        [Header("Rơi dầu (nhiên liệu chế tác ở căn cứ)")]
        [Tooltip("Xác suất rơi dầu khi bị hạ (1 = luôn rơi)"), Range(0f, 1f)] public float oilChance = 0.35f;
        [Tooltip("Số dầu rơi ra ít nhất / nhiều nhất")] public int oilMin = 1;
        public int oilMax = 3;
        [Header("Hệ thống Skill / sức mạnh")]
        [Tooltip("Mã loại quái dùng cho chế tạo minibot. Để trống = tên Prefab (Walker, Flyer, Heavy, Rusher...)")] public string typeId = "";
        [Tooltip("CẤP BẬC (F..SSS): quyết định bậc linh kiện rơi ra (SS / SSS không rơi) và bậc của minibot chế tạo từ quái này. CẤP ĐỘ (1..200) do trụ triệu hồi đặt khi sinh quái; cấp càng cao quái càng khoẻ")] public PowerInfo power = new PowerInfo();
        protected Flasher flasher;
        protected float flashT;
        protected Color accent = Cfg.Orange;
        protected float dmgMul = 1f, spdMul = 1f;     // hệ số sức mạnh (do trụ đặt khi sinh quái)
        protected float coinScale = 1f;               // hệ số số xu rơi ra (= Power của trụ, tối thiểu 1)

        public override bool Targetable { get { return !dead; } }

        List<Minibot> minibotHitters;

        /// <summary>Minibot gây sát thương lên quái này (để chia công trạng độ trưởng thành khi quái chết).</summary>
        public void NoteMinibotHit(Minibot m)
        {
            if (m == null) return;
            if (minibotHitters == null) minibotHitters = new List<Minibot>(2);
            if (!minibotHitters.Contains(m)) minibotHitters.Add(m);
        }

        /// <summary>Độ cao (y) khi trụ triệu hồi sinh quái này ra (quái bay cao hơn quái đi bộ).</summary>
        public abstract float SpawnHeight { get; }

        /// <summary>Màu đặc trưng (dùng cho hiệu ứng triệu hồi và tia lửa).</summary>
        public virtual Color AccentColor { get { return Cfg.Orange; } }

        /// <summary>Sinh một quái từ Prefab bất kỳ (dùng cho bảng triệu hồi của trụ). power = hệ số sức mạnh.</summary>
        public static Enemy Spawn(GameObject prefab, Vector2 pos, Tower owner, int level)
        {
            if (prefab == null) return null;
            var go = Instantiate(prefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity, Level.Sub("Enemies"));
            go.name = prefab.name;
            var e = go.GetComponent<Enemy>();
            if (e == null) { Debug.LogError("Neon Breaker: Prefab " + prefab.name + " không có script quái (Walker/Flyer)."); Destroy(go); return null; }
            e.owner = owner;
            e.ApplyLevel(level);
            return e;
        }

        /// <summary>
        /// Đặt CẤP ĐỘ cho quái (1..200). Cấp 1 = chỉ số gốc trên Prefab; mỗi cấp thêm vài % máu / sát thương / tốc độ / xu rơi
        /// (xem PowerRules.HpMul, DamageMul, SpeedMul, RewardMul). Quái cấp cao ngả đỏ dần. Thời gian báo hiệu đòn giữ nguyên để vẫn công bằng.
        /// </summary>
        public void ApplyLevel(int level)
        {
            level = PowerRules.ClampLevel(level);
            if (power == null) power = new PowerInfo();
            power.level = level;
            maxHp *= PowerRules.HpMul(level);
            hp = maxHp;
            dmgMul = PowerRules.DamageMul(level);
            spdMul = PowerRules.SpeedMul(level);
            coinScale = PowerRules.RewardMul(level);
            float k = Mathf.Clamp01((level - 1) / 24f);
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
            if (Random.value < oilChance)
            {
                int olo = Mathf.Max(0, Mathf.Min(oilMin, oilMax)), ohi = Mathf.Max(olo, Mathf.Max(oilMin, oilMax));
                OilDrop.Drop(Center, Random.Range(olo, ohi + 1));
            }
            if (Random.value < coinChance)
            {
                int lo = Mathf.Max(0, Mathf.Min(coinMin, coinMax)), hi = Mathf.Max(lo, Mathf.Max(coinMin, coinMax));
                Coin.Drop(Center, Mathf.RoundToInt(Random.Range(lo, hi + 1) * coinScale));
            }
            if (minibotHitters != null)
                for (int i = 0; i < minibotHitters.Count; i++)
                    if (minibotHitters[i] != null) minibotHitters[i].CreditAssist();
            SkillSystem.OnEnemyKilled(this);
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
