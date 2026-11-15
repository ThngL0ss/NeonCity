using UnityEngine;

namespace NeonBreaker
{
    /// <summary>Đạn của lính đánh thuê: bay thẳng, trúng kẻ địch đầu tiên thì gây sát thương (aoe > 0: nổ diện rộng quanh điểm trúng). Không bao giờ trúng đồng minh.</summary>
    public class MercBolt : MonoBehaviour
    {
        Merc owner;
        Vector2 dir;
        float speed, dmg, aoe, life = 1.8f, radius = 0.25f;

        public static void Fire(Merc owner, Vector2 pos, Vector2 dir, float speed, float dmg, float aoeDiameter, Color c)
        {
            var go = new GameObject("MercBolt");
            go.transform.position = pos;
            var b = go.AddComponent<MercBolt>();
            b.owner = owner;
            b.dir = dir.sqrMagnitude < 0.001f ? Vector2.right : dir.normalized;
            b.speed = speed; b.dmg = dmg; b.aoe = aoeDiameter * 0.5f;
            float d = aoeDiameter > 0f ? 0.55f : 0.32f;
            b.radius = d * 0.5f + 0.05f;
            Art.Spr(go.transform, "Core", Art.Circle, Vector2.zero, new Vector2(d, d), c, 31);
            Art.Spr(go.transform, "Hot", Art.Circle, Vector2.zero, new Vector2(d * 0.45f, d * 0.45f), Color.white, 32);
            Art.Spr(go.transform, "Glow", Art.Glow, Vector2.zero, new Vector2(d * 2.6f, d * 2.6f), Cfg.A(c, 0.5f), 30, true);
        }

        void Update()
        {
            if (BaseUi.IsOpen || !GameManager.Active) return;
            float dt = Time.deltaTime;
            transform.position += (Vector3)(dir * speed * dt);
            life -= dt;
            Vector2 p = transform.position;
            if (life <= 0f || p.x < Cfg.LevelMinX - 4f || p.x > Cfg.LevelMaxX + 4f) { Destroy(gameObject); return; }
            if (p.y < -0.2f) { Pop(p); return; }
            var all = Hittable.All;
            for (int i = 0; i < all.Count; i++)
            {
                var h = all[i];
                if (!Merc.IsHostile(h)) continue;
                if (Geo.CircleRect(p, radius, h.HitRect))
                {
                    if (aoe <= 0f)
                    {
                        h.TakeHit(dmg, dir, 6f, false);
                        if (owner != null) owner.Tag(h);
                        Fx.Sparks(p, -dir, Cfg.Cyan, 5, 8f, 80f, 0.2f);
                        Destroy(gameObject);
                    }
                    else Pop(p);
                    return;
                }
            }
        }

        void Pop(Vector2 p)
        {
            if (aoe > 0f)
            {
                Fx.Explosion(p, Cfg.Orange, false);
                Fx.Shake(0.25f);
                var snap = new System.Collections.Generic.List<Hittable>(Hittable.All);
                for (int i = 0; i < snap.Count; i++)
                {
                    var h = snap[i];
                    if (!Merc.IsHostile(h) || !Geo.CircleRect(p, aoe, h.HitRect)) continue;
                    h.TakeHit(dmg, new Vector2(h.transform.position.x >= p.x ? 1f : -1f, 0.4f), 7f, false);
                    if (owner != null) owner.Tag(h);
                }
            }
            Destroy(gameObject);
        }
    }
}
