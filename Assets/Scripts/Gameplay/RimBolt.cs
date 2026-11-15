using UnityEngine;

namespace NeonBreaker
{
    /// <summary>Đạn của kẻ địch ở Rìa phố (cướp, robot): bay thẳng, trúng người chơi hoặc lính đánh thuê thì gây sát thương rồi tan.</summary>
    public class RimBolt : MonoBehaviour
    {
        Vector2 dir;
        float speed, dmg, life = 2.2f, radius = 0.22f;

        public static void Fire(Vector2 pos, Vector2 dir, float speed, float dmg, Color c)
        {
            var go = new GameObject("RimBolt");
            go.transform.position = pos;
            var b = go.AddComponent<RimBolt>();
            b.dir = dir.sqrMagnitude < 0.001f ? Vector2.right : dir.normalized;
            b.speed = speed; b.dmg = dmg;
            Art.Spr(go.transform, "Core", Art.Circle, Vector2.zero, new Vector2(0.34f, 0.34f), c, 31);
            Art.Spr(go.transform, "Hot", Art.Circle, Vector2.zero, new Vector2(0.16f, 0.16f), Color.white, 32);
            Art.Spr(go.transform, "Glow", Art.Glow, Vector2.zero, new Vector2(1.1f, 1.1f), Cfg.A(c, 0.5f), 30, true);
        }

        void Update()
        {
            if (BaseUi.IsOpen || !GameManager.Active) return;
            float dt = Time.deltaTime;
            transform.position += (Vector3)(dir * speed * dt);
            life -= dt;
            Vector2 p = transform.position;
            if (life <= 0f || p.x < Cfg.LevelMinX - 4f || p.x > Cfg.LevelMaxX + 4f) { Destroy(gameObject); return; }
            var pl = Player.Instance;
            if (pl != null && !pl.Dead && !pl.Invulnerable && Geo.CircleRect(p, radius, pl.HurtRect))
            {
                pl.Hurt(dmg, p - dir, 6f);
                Pop();
                return;
            }
            var m = Merc.Nearest(p, 1.4f);
            if (m != null && Geo.CircleRect(p, radius, m.HurtRect)) { m.Hurt(dmg, p - dir); Pop(); }
        }

        void Pop()
        {
            Fx.Sparks(transform.position, -dir, Cfg.Orange, 6, 8f, 80f, 0.25f);
            Destroy(gameObject);
        }
    }
}
