using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    public enum PKind { Shuriken, Bolt, Wave }

    /// <summary>Đạn: phi tiêu của người chơi, đạn năng lượng và sóng xung kích của kẻ địch.</summary>
    public class Projectile : MonoBehaviour
    {
        public static readonly List<Projectile> Live = new List<Projectile>();

        public PKind kind;
        public Vector2 vel;
        public float damage, radius = 0.3f, life = 6f, knock = 6f, range = 13f;
        public int pierce;
        public bool fromPlayer, lifesteal;
        public Color color = Color.white;

        float traveled, trailT, spin;
        bool hitPlayer;
        [Header("Hình ảnh (tự tìm object con \"Visual\" / \"Glow\" nếu để trống)")]
        public Transform vis;
        public SpriteRenderer glow;
        readonly HashSet<Hittable> hits = new HashSet<Hittable>();

        void Awake()
        {
            if (vis == null) vis = transform.Find("Visual");
            if (glow == null) { var g = transform.Find("Glow"); if (g != null) glow = g.GetComponent<SpriteRenderer>(); }
        }

        void OnEnable() { Live.Add(this); }
        void OnDisable() { Live.Remove(this); }

        // ------------------------------------------------------------ factories
        // Hình ảnh nằm trong prefab (ArtBank ▸ Shuriken/Bolt/Wave Prefab): object con "Visual" (sprite) và "Glow" (vầng sáng, tuỳ chọn).
        public static Projectile Shuriken(Vector2 pos, Vector2 dir, float speed, float damage, int pierce, float range)
        {
            var p = Make(Art.Bank != null ? Art.Bank.shurikenPrefab : null, pos, "Shuriken");
            if (p == null) return null;
            p.kind = PKind.Shuriken;
            p.vel = dir.normalized * speed;
            p.damage = damage;
            p.range = range;
            p.radius = 0.38f;
            p.pierce = pierce;
            p.fromPlayer = true;
            p.color = Cfg.Cyan;
            p.life = 3f;
            return p;
        }

        public static Projectile Bolt(Vector2 pos, Vector2 dir, float speed, float dmg, Color c, bool steal = false)
        {
            var p = Make(Art.Bank != null ? Art.Bank.boltPrefab : null, pos, "Bolt");
            if (p == null) return null;
            p.kind = PKind.Bolt;
            p.vel = dir.normalized * speed;
            p.damage = dmg;
            p.radius = 0.3f;
            p.fromPlayer = false;
            p.lifesteal = steal;
            p.color = c;
            p.life = 5f;
            if (p.glow != null) p.glow.color = Cfg.A(c, 0.55f);
            if (p.vis != null) p.vis.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(p.vel.y, p.vel.x) * Mathf.Rad2Deg);
            return p;
        }

        /// <summary>Sóng xung kích chạy sát mặt đất. Người chơi phải nhảy qua (hoặc lướt xuyên).</summary>
        public static Projectile Wave(Vector2 pos, float dirX, float speed, float dmg, Color c, bool steal = false)
        {
            var p = Make(Art.Bank != null ? Art.Bank.wavePrefab : null, new Vector2(pos.x, 0.55f), "Wave");
            if (p == null) return null;
            p.kind = PKind.Wave;
            p.vel = new Vector2(Mathf.Sign(dirX) * speed, 0f);
            p.damage = dmg;
            p.fromPlayer = false;
            p.lifesteal = steal;
            p.color = c;
            p.life = 9f;
            p.knock = 9f;
            if (p.glow != null) p.glow.color = Cfg.A(c, 0.5f);
            if (p.vis != null) p.vis.localScale = new Vector3(Mathf.Sign(dirX), 1f, 1f);
            return p;
        }

        static Projectile Make(GameObject prefab, Vector2 pos, string name)
        {
            if (prefab == null) { Debug.LogError("Neon Breaker: ArtBank chưa có prefab " + name + "."); return null; }
            var go = Instantiate(prefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity, Level.Sub("Projectiles"));
            go.name = name;
            var p = go.GetComponent<Projectile>();
            if (p == null) p = go.AddComponent<Projectile>();
            return p;
        }

        public static void ClearHostile()
        {
            var copy = new List<Projectile>(Live);
            for (int i = 0; i < copy.Count; i++)
            {
                if (copy[i] != null && !copy[i].fromPlayer)
                {
                    Fx.Burst(copy[i].transform.position, copy[i].color, 4, 3f, 0.3f, 0.25f);
                    Destroy(copy[i].gameObject);
                }
            }
        }

        // ------------------------------------------------------------ update
        void Update()
        {
            float dt = Time.deltaTime;
            life -= dt;
            Vector2 pos = transform.position;
            Vector2 step = vel * dt;
            pos += step;
            traveled += step.magnitude;
            transform.position = pos;

            if (kind == PKind.Shuriken)
            {
                spin += 900f * dt;
                if (vis != null) vis.localRotation = Quaternion.Euler(0f, 0f, spin);
            }

            trailT -= dt;
            if (trailT <= 0f)
            {
                trailT = kind == PKind.Wave ? 0.04f : 0.02f;
                if (kind == PKind.Wave)
                    Fx.Emit(Fx.S_Glow, pos + new Vector2(0f, Random.Range(0f, 0.9f)), new Vector2(-Mathf.Sign(vel.x) * 2f, Random.Range(0.5f, 3f)),
                            new Vector2(0.5f, 0.5f), Vector2.zero, Cfg.A(color, 0.8f), Cfg.A(color, 0f), 0.35f);
                else Fx.Trail(pos, Cfg.A(color, 0.8f), kind == PKind.Shuriken ? 0.6f : 0.5f, 0.18f);
            }

            bool dead = life <= 0f || pos.x < Cfg.LevelMinX - 6f || pos.x > Cfg.LevelMaxX + 6f || pos.y > 45f;
            if (kind == PKind.Shuriken && traveled > range) dead = true;
            if (kind != PKind.Wave && pos.y < 0.1f) dead = true;

            if (!dead && Level.Instance != null && Level.Instance.IsBlocked(pos, radius))
                dead = true;

            if (!dead)
            {
                if (fromPlayer) dead = HitEnemies(pos);
                else dead = HitPlayer(pos);
            }

            if (dead) Expire();
        }

        bool HitEnemies(Vector2 pos)
        {
            var list = Hittable.Snapshot();
            for (int i = 0; i < list.Count; i++)
            {
                var h = list[i];
                if (h == null || !h.Targetable || hits.Contains(h)) continue;
                if (!Geo.CircleRect(pos, radius, h.HitRect)) continue;
                hits.Add(h);
                h.TakeHit(damage, vel.normalized, 3f, false);
                Sfx.PlayAt("shurHit", pos, 0.8f);
                Fx.Sparks(pos, -vel.normalized, Cfg.Cyan, 8, 12f, 70f);
                Fx.HitStop(0.025f);
                pierce--;
                if (pierce < 0) return true;
            }
            return false;
        }

        bool HitPlayer(Vector2 pos)
        {
            var pl = Player.Instance;
            if (pl == null || pl.Dead || hitPlayer) return false;
            bool overlap;
            if (kind == PKind.Wave) overlap = Geo.Box(pos + new Vector2(0f, 0.2f), new Vector2(0.9f, 1.3f)).Overlaps(pl.HurtRect);
            else overlap = Geo.CircleRect(pos, radius, pl.HurtRect);
            if (!overlap) return false;

            float dealt = pl.Hurt(damage, pos, knock);
            if (dealt > 0f)
            {
                if (lifesteal && Boss.Instance != null) Boss.Instance.Drain(dealt);
                hitPlayer = true;
                return kind != PKind.Wave;   // sóng đi xuyên qua, chỉ gây một lần
            }
            return false;                    // đang bất tử (lướt) => đạn bay xuyên
        }

        void Expire()
        {
            if (kind != PKind.Wave) Fx.Burst(transform.position, color, 6, 5f, 0.3f, 0.3f);
            else Fx.Burst(transform.position, color, 10, 6f, 0.4f, 0.35f);
            Destroy(gameObject);
        }
    }
}
