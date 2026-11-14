using UnityEngine;

namespace NeonBreaker
{
    /// <summary>Đạn đại bác của đấu sĩ Cannon: bay thẳng, trúng mục tiêu (người chơi hoặc đấu sĩ đối thủ) thì gây sát thương rồi nổ. Lướt (bất tử) xuyên qua được.</summary>
    public class ArenaBullet : MonoBehaviour
    {
        ArenaFighter owner;
        Vector2 dir;
        float speed, dmg, radius, life = 3f;

        public static void Fire(ArenaFighter owner, Vector2 pos, Vector2 dir, float speed, float dmg, float diameter)
        {
            var go = new GameObject("ArenaBullet");
            go.transform.position = pos;
            var b = go.AddComponent<ArenaBullet>();
            b.owner = owner; b.dir = dir; b.speed = speed; b.dmg = dmg; b.radius = diameter * 0.5f;
            Art.Spr(go.transform, "Core", Art.Circle, Vector2.zero, new Vector2(diameter, diameter), new Color(1f, 0.5f, 0.9f, 1f), 31);
            Art.Spr(go.transform, "Hot", Art.Circle, Vector2.zero, new Vector2(diameter * 0.55f, diameter * 0.55f), Color.white, 32);
            Art.Spr(go.transform, "Glow", Art.Glow, Vector2.zero, new Vector2(diameter * 2.4f, diameter * 2.4f), Cfg.A(Cfg.Magenta, 0.5f), 30, true);
        }

        void Update()
        {
            if (BaseUi.IsOpen || !GameManager.Active) return;
            if (owner == null) { Destroy(gameObject); return; }
            float dt = Time.deltaTime;
            transform.position += (Vector3)(dir * speed * dt);
            life -= dt;
            Vector2 p = transform.position;
            if (life <= 0f || p.x < owner.minX - 3f || p.x > owner.maxX + 3f) { Pop(); return; }

            if (owner.foe != null)
            {
                if (!owner.foe.dead && Geo.CircleRect(p, radius, owner.foe.HitRect))
                {
                    owner.foe.TakeHit(dmg, dir, 12f, false);
                    Pop();
                }
            }
            else
            {
                var pl = Player.Instance;
                if (pl != null && !pl.Dead && !pl.Invulnerable && Geo.CircleRect(p, radius, pl.HurtRect))
                {
                    pl.Hurt(dmg, p - dir, 12f);
                    Pop();
                }
            }
        }

        void Pop()
        {
            Fx.Explosion(transform.position, Cfg.Magenta, false);
            Sfx.PlayAt("slam", transform.position, 0.6f, 1.4f);
            Destroy(gameObject);
        }
    }
}
