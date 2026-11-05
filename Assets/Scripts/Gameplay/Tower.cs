using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Trụ triệu hồi nhỏ. Khi người chơi lại gần, trụ liên tục triệu hồi quái.
    /// Cân bằng: tổng quái sống tối đa = Level ▸ Max Enemies Alive; mỗi trụ tối đa = Max Alive (ô bên dưới).
    /// Phá hủy hết các trụ để mở cổng vào khu boss.
    /// Số lượng và vị trí trụ do bạn đặt trong Scene (kéo prefab Tower vào). Mỗi trụ chỉnh riêng được
    /// loại quái, số lượng, tốc độ triệu hồi và độ mạnh ngay trong Inspector.
    /// </summary>
    public class Tower : Hittable
    {
        /// <summary>Một dòng trong bảng triệu hồi: loại quái (Prefab) + tỉ lệ + độ mạnh riêng.</summary>
        [System.Serializable]
        public class SpawnEntry
        {
            [Tooltip("Prefab quái. Kéo từ Project: Walker, Flyer, Heavy, Rusher hoặc Prefab quái bạn tự tạo")]
            public GameObject prefab;
            [Tooltip("Trọng số: càng lớn càng hay được chọn (so với tổng trọng số của bảng)")]
            public float weight = 1f;
            [Tooltip("Cộng thêm vào CẤP ĐỘ của trụ cho riêng loại quái này (0 = cùng cấp với trụ)")]
            public int levelAdd = 0;
            [Tooltip("Chỉ triệu hồi loại quái này khi 'Độ ưu tiên loại quái' (Power) của trụ >= số này. 0 = luôn triệu hồi. Dùng để quái mạnh (Heavy, Rusher) chỉ xuất hiện ở các trụ gần boss. KHÔNG ảnh hưởng độ khoẻ của quái")]
            public float minPower = 0f;
        }

        bool Eligible(SpawnEntry e)
        {
            return e != null && e.prefab != null && e.weight > 0f && power >= e.minPower;
        }

        public static readonly List<Tower> Towers = new List<Tower>();
        public static int Remaining { get { int n = 0; for (int i = 0; i < Towers.Count; i++) if (Towers[i] != null && !Towers[i].dead) n++; return n; } }

        [Header("Tham chiếu (tự tìm nếu để trống)")]
        public SpriteRenderer crystalGlow;
        public SpriteRenderer baseGlow;
        public Vector2 barOffset = new Vector2(0f, 5.0f);

        [Header("Chỉ số của trụ")]
        [Tooltip("Máu của trụ")] public float health = 420f;
        [Tooltip("Người chơi lại gần trong khoảng này (theo trục ngang) thì trụ bắt đầu triệu hồi")] public float activateRange = 16f;

        [Header("Triệu hồi quái")]
        [Tooltip("Danh sách loại quái trụ này triệu hồi (thêm dòng bằng dấu +). Để trống = mặc định: Walker 70% / Flyer 30%")]
        public List<SpawnEntry> spawnTable = new List<SpawnEntry>();
        [Tooltip("Số quái do TRỤ NÀY sinh ra được sống cùng lúc tối đa")] public int maxAlive = 3;
        [Tooltip("Tổng số quái trụ này triệu hồi trong cả ván (0 = không giới hạn)")] public int totalSpawnLimit = 0;
        [Tooltip("Số giây giữa 2 lần triệu hồi")] public float spawnInterval = 4.2f;
        [Tooltip("Dao động ngẫu nhiên của thời gian triệu hồi (0.12 = ±12%)")] public float intervalJitter = 0.12f;
        [Tooltip("Số giây chờ trước lần triệu hồi đầu tiên khi người chơi vừa lại gần")] public float firstSpawnDelay = 1.0f;
        [Tooltip("Quái xuất hiện cách trụ trong khoảng này (min, max)")] public Vector2 spawnDistance = new Vector2(1.6f, 2.8f);
        [Tooltip("Bật = Power càng cao thì trụ triệu hồi càng nhanh")] public bool powerSpeedsUpSpawn = true;

        [Header("CẤP ĐỘ quái do trụ này sinh ra (cấp càng cao quái càng khoẻ)")]
        [Tooltip("0 = tự lấy theo thứ tự trụ từ trái sang phải trong bảng Level ▸ Tower Levels (mặc định 5, 5, 7, 7, 7, 10). Số > 0 = ép riêng trụ này")]
        public int levelOverride = 0;
        [Header("Độ ưu tiên loại quái (KHÔNG làm quái khoẻ hơn)")]
        [Tooltip("Bật = tự tăng dần theo thứ tự trụ: các trụ gần boss mở thêm loại quái mạnh trong Spawn Table (xem Min Power)")]
        public bool autoByOrder = true;
        [Tooltip("Độ ưu tiên của trụ đầu tiên (xa boss nhất) khi bật Auto By Order")]
        public float powerFirst = 1.0f;
        [Tooltip("Độ ưu tiên của trụ cuối cùng (gần boss nhất) khi bật Auto By Order")]
        public float powerLast = 1.8f;
        [Tooltip("Độ ưu tiên của riêng trụ này khi TẮT Auto By Order")]
        public float power = 1.0f;

        /// <summary>Cấp độ của quái do trụ này sinh ra (đã tính thứ tự trụ).</summary>
        public int SpawnLevel { get; private set; }

        int spawned;
        float spawnT = 1.2f, flashT, pulse, barShowT;
        bool activated;
        Flasher flasher;
        WorldBar bar;

        public override bool Targetable { get { return !dead; } }

        void Reset()
        {
            hitSize = new Vector2(1.7f, 4.2f);
            hitOffset = new Vector2(0f, 2.1f);
        }

        protected override void OnEnable() { base.OnEnable(); if (!Towers.Contains(this)) Towers.Add(this); }
        protected override void OnDisable() { base.OnDisable(); Towers.Remove(this); }

        void Awake()
        {
            maxHp = hp = health;
            pulse = Random.value * 6f;
            if (crystalGlow == null) { var t = transform.Find("CrystalGlow"); if (t != null) crystalGlow = t.GetComponent<SpriteRenderer>(); }
            if (baseGlow == null) { var t = transform.Find("BaseGlow"); if (t != null) baseGlow = t.GetComponent<SpriteRenderer>(); }
            Art.Init();
            bar = new WorldBar(transform, barOffset, 2.2f, 0.16f, Cfg.Magenta);
            bar.SetVisible(false);
            flasher = new Flasher(gameObject);
        }

        void Start()
        {
            int n = Towers.Count, rank = 0;
            for (int i = 0; i < n; i++)
                if (Towers[i] != null && Towers[i] != this && Towers[i].transform.position.x < transform.position.x) rank++;
            if (autoByOrder)
            {
                float t = n > 1 ? rank / (float)(n - 1) : 0f;
                power = Mathf.Lerp(powerFirst, powerLast, t);
            }
            SpawnLevel = levelOverride > 0 ? PowerRules.ClampLevel(levelOverride)
                                           : (Level.Instance != null ? Level.Instance.TowerLevel(rank) : 5);
        }

        int OwnedAlive()
        {
            int n = 0;
            for (int i = 0; i < Enemy.Live.Count; i++) if (Enemy.Live[i] != null && Enemy.Live[i].owner == this) n++;
            return n;
        }

        void Update()
        {
            if (dead) return;
            float dt = Time.deltaTime;
            pulse += dt;
            var p = Player.Instance;

            if (crystalGlow != null) crystalGlow.color = Cfg.A(Cfg.Magenta, 0.4f + 0.2f * Mathf.Sin(pulse * 3f));

            if (flashT > 0f) flashT -= dt;
            flasher.Apply(flashT > 0f, 1f);
            if (barShowT > 0f) barShowT -= dt;

            if (p == null || p.Dead || !GameManager.Active) return;

            float dist = Mathf.Abs(p.Position.x - transform.position.x);
            bool near = dist < activateRange;
            bar.SetVisible(barShowT > 0f || (near && hp < maxHp));
            if (!near) return;
            if (!activated) { activated = true; spawnT = firstSpawnDelay; Sfx.PlayAt("warn", transform.position, 0.6f, 0.7f); }

            spawnT -= dt;
            if (spawnT > 0f) return;

            if (totalSpawnLimit > 0 && spawned >= totalSpawnLimit) return;      // đã triệu hồi đủ số lượng cho cả ván
            if (Enemy.Live.Count >= Cfg.MaxEnemiesAlive || OwnedAlive() >= maxAlive)
            {
                spawnT = 0.6f;                 // chờ có chỗ trống
                return;
            }
            SpawnOne();
            float speedUp = powerSpeedsUpSpawn ? Mathf.Sqrt(Mathf.Max(0.2f, power)) : 1f;
            spawnT = spawnInterval / speedUp * Random.Range(1f - intervalJitter, 1f + intervalJitter);
        }

        /// <summary>Chọn ngẫu nhiên một loại quái theo trọng số trong bảng triệu hồi (trống thì dùng Walker/Flyer mặc định).</summary>
        GameObject PickPrefab(out int levelAdd)
        {
            levelAdd = 0;
            float total = 0f;
            for (int i = 0; i < spawnTable.Count; i++)
                if (Eligible(spawnTable[i])) total += spawnTable[i].weight;
            if (total > 0f)
            {
                float r = Random.value * total;
                for (int i = 0; i < spawnTable.Count; i++)
                {
                    var e = spawnTable[i];
                    if (!Eligible(e)) continue;
                    r -= e.weight;
                    if (r <= 0f) { levelAdd = e.levelAdd; return e.prefab; }
                }
                // sai số làm tròn: lấy dòng hợp lệ cuối cùng
                for (int i = spawnTable.Count - 1; i >= 0; i--)
                    if (Eligible(spawnTable[i])) { levelAdd = spawnTable[i].levelAdd; return spawnTable[i].prefab; }
            }
            if (Art.Bank == null) return null;
            bool flyer = Random.value < Mathf.Clamp(0.30f + (power - 1f) * 0.2f, 0.15f, 0.6f);
            return flyer ? Art.Bank.flyerPrefab : Art.Bank.walkerPrefab;
        }

        void SpawnOne()
        {
            int levelAdd;
            GameObject prefab = PickPrefab(out levelAdd);
            if (prefab == null) { Debug.LogWarning("Neon Breaker: Tower " + name + " chưa có prefab quái nào để triệu hồi (Spawn Table trống và ArtBank thiếu Walker/Flyer)."); spawnT = 2f; return; }
            var proto = prefab.GetComponent<Enemy>();
            float y = proto != null ? proto.SpawnHeight : 0.05f;
            Color c = proto != null ? proto.AccentColor : Cfg.Orange;

            float dir = Random.value < 0.5f ? -1f : 1f;
            Vector2 pos = new Vector2(transform.position.x + dir * Random.Range(spawnDistance.x, spawnDistance.y), y);
            pos.x = Mathf.Clamp(pos.x, Cfg.LevelMinX + 1.5f, Cfg.GateX - 2f);
            Fx.Ring(pos + new Vector2(0f, 0.5f), c, 0.3f, 2.8f, 0.4f);
            Fx.Burst(pos + new Vector2(0f, 0.5f), c, 12, 6f, 0.4f, 0.3f);
            Fx.Flare(pos + new Vector2(0f, 0.5f), c, 2.2f, 0.3f);
            Fx.Beam(Center + new Vector2(0f, 0.8f), pos + new Vector2(0f, 0.5f), c, 0.12f, 0.18f);
            Sfx.PlayAt("spawn", pos, 0.7f);
            int lv = SpawnLevel > 0 ? SpawnLevel : 5;
            if (Enemy.Spawn(prefab, pos, this, lv + levelAdd) != null) spawned++;
        }

        public override void TakeHit(float dmg, Vector2 dir, float knockPower, bool melee)
        {
            if (dead) return;
            hp -= dmg;
            flashT = 0.07f;
            barShowT = 2.5f;
            bar.SetVisible(true);
            bar.Set(hp / maxHp);
            Sfx.PlayAt("towerHit", transform.position, 0.8f, Random.Range(0.9f, 1.2f));
            Fx.Sparks(Center, dir, Cfg.Cyan, 8, 12f, 60f);
            Fx.Burst(Center, Cfg.A(Cfg.Magenta, 0.9f), 5, 5f, 0.3f, 0.28f);
            Fx.Text(Center + new Vector2(0f, 1.6f), Mathf.RoundToInt(dmg).ToString(), Cfg.Yellow);
            if (hp <= 0f) Die();
        }

        void Die()
        {
            dead = true;
            Vector2 c = Center;
            Fx.Explosion(c, Cfg.Magenta, true);
            Fx.Explosion(c + new Vector2(0f, 1f), Cfg.Cyan, true);
            Fx.Ring(c, Cfg.White, 1f, 14f, 0.6f);
            Fx.Shake(0.7f);
            Fx.HitStop(0.12f);
            Fx.Flash(Cfg.Magenta, 0.35f);
            Sfx.Play("towerDie", 1f);
            GameManager.OnTowerDestroyed(this);
            Destroy(gameObject);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.8f, 0.6f);
            Gizmos.DrawWireCube(Center, hitSize);
            Gizmos.color = new Color(1f, 1f, 0.2f, 0.25f);
            Gizmos.DrawLine(transform.position + Vector3.left * activateRange, transform.position + Vector3.right * activateRange);
        }
    }
}
