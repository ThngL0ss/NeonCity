using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Trụ triệu hồi nhỏ. Khi người chơi lại gần, trụ liên tục triệu hồi quái.
    /// Cân bằng: tổng quái sống tối đa Cfg.MaxEnemiesAlive, mỗi trụ tối đa Cfg.MaxPerTower.
    /// Phá hủy hết các trụ để mở cổng vào khu boss.
    /// Số lượng và vị trí trụ do bạn đặt trong Scene (kéo prefab Tower vào).
    /// </summary>
    public class Tower : Hittable
    {
        public static readonly List<Tower> Towers = new List<Tower>();
        public static int Remaining { get { int n = 0; for (int i = 0; i < Towers.Count; i++) if (Towers[i] != null && !Towers[i].dead) n++; return n; } }

        [Header("Tham chiếu (tự tìm nếu để trống)")]
        public SpriteRenderer crystalGlow;
        public SpriteRenderer baseGlow;
        public Vector2 barOffset = new Vector2(0f, 5.0f);

        [Header("Độ mạnh quái do trụ này sinh ra (1 = chỉ số gốc trong GameConfig)")]
        [Tooltip("Bật = tự tăng dần theo thứ tự trụ từ trái sang phải (trụ gần boss nhất mạnh nhất)")]
        public bool autoByOrder = true;
        [Tooltip("Độ mạnh của trụ đầu tiên (xa boss nhất) khi bật Auto By Order")]
        public float powerFirst = 1.0f;
        [Tooltip("Độ mạnh của trụ cuối cùng (gần boss nhất) khi bật Auto By Order")]
        public float powerLast = 1.8f;
        [Tooltip("Độ mạnh của riêng trụ này khi TẮT Auto By Order")]
        public float power = 1.0f;

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
            maxHp = hp = Cfg.TowerHp;
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
            if (!autoByOrder) return;
            int n = Towers.Count, rank = 0;
            for (int i = 0; i < n; i++)
                if (Towers[i] != null && Towers[i] != this && Towers[i].transform.position.x < transform.position.x) rank++;
            float t = n > 1 ? rank / (float)(n - 1) : 0f;
            power = Mathf.Lerp(powerFirst, powerLast, t);
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
            bool near = dist < Cfg.TowerActivateRange;
            bar.SetVisible(barShowT > 0f || (near && hp < maxHp));
            if (!near) return;
            if (!activated) { activated = true; spawnT = 1.0f; Sfx.PlayAt("warn", transform.position, 0.6f, 0.7f); }

            spawnT -= dt;
            if (spawnT > 0f) return;

            if (Enemy.Live.Count >= Cfg.MaxEnemiesAlive || OwnedAlive() >= Cfg.MaxPerTower)
            {
                spawnT = 0.6f;                 // chờ có chỗ trống
                return;
            }
            SpawnOne();
            spawnT = Cfg.TowerSpawnInterval / Mathf.Sqrt(Mathf.Max(0.2f, power)) * Random.Range(0.9f, 1.15f);
        }

        void SpawnOne()
        {
            bool flyer = Random.value < Mathf.Clamp(0.30f + (power - 1f) * 0.2f, 0.15f, 0.6f);
            float dir = Random.value < 0.5f ? -1f : 1f;
            Vector2 pos = new Vector2(transform.position.x + dir * Random.Range(1.6f, 2.8f), flyer ? 3.4f : 0.05f);
            pos.x = Mathf.Clamp(pos.x, Cfg.LevelMinX + 1.5f, Cfg.GateX - 2f);
            Color c = flyer ? Cfg.Magenta : Cfg.Orange;
            Fx.Ring(pos + new Vector2(0f, 0.5f), c, 0.3f, 2.8f, 0.4f);
            Fx.Burst(pos + new Vector2(0f, 0.5f), c, 12, 6f, 0.4f, 0.3f);
            Fx.Flare(pos + new Vector2(0f, 0.5f), c, 2.2f, 0.3f);
            Fx.Beam(Center + new Vector2(0f, 0.8f), pos + new Vector2(0f, 0.5f), c, 0.12f, 0.18f);
            Sfx.PlayAt("spawn", pos, 0.7f);
            Enemy e = flyer ? (Enemy)Flyer.Spawn(pos, this) : Walker.Spawn(pos, this);
            if (e != null) e.ApplyPower(power);
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
            Gizmos.DrawLine(transform.position + Vector3.left * Cfg.TowerActivateRange, transform.position + Vector3.right * Cfg.TowerActivateRange);
        }
    }
}
