using UnityEngine;

namespace NeonBreaker
{
    /// <summary>Bom rơi từ máy bay của Connie: rơi xuống đất rồi nổ diện rộng, chỉ trúng kẻ địch (không trúng đồng minh / người chơi).</summary>
    public class MercBomb : MonoBehaviour
    {
        float vy, dmg, radius;

        public static void Drop(Vector2 pos, float dmg, float radius)
        {
            var go = new GameObject("MercBomb");
            go.transform.position = pos;
            var b = go.AddComponent<MercBomb>();
            b.dmg = dmg; b.radius = radius;
            Art.Spr(go.transform, "Body", Art.Circle, Vector2.zero, new Vector2(0.5f, 0.5f), new Color(0.25f, 0.27f, 0.33f), 48);
            Art.Spr(go.transform, "Tip", Art.Circle, Vector2.zero, new Vector2(0.22f, 0.22f), Cfg.Red, 49);
        }

        void Update()
        {
            if (BaseUi.IsOpen || !GameManager.Active) return;
            vy -= 30f * Time.deltaTime;
            transform.position += new Vector3(0f, vy * Time.deltaTime, 0f);
            if (transform.position.y <= 0.15f) Boom();
        }

        void Boom()
        {
            Vector2 p = new Vector2(transform.position.x, 0.2f);
            Fx.Explosion(p, Cfg.Orange, true);
            Fx.Shake(0.45f);
            Sfx.PlayAt("slam", p, 0.9f, 0.7f);
            var snap = new System.Collections.Generic.List<Hittable>(Hittable.All);
            for (int i = 0; i < snap.Count; i++)
            {
                var h = snap[i];
                if (!Merc.IsHostile(h) || !Geo.CircleRect(p, radius, h.HitRect)) continue;
                h.TakeHit(dmg, new Vector2(h.transform.position.x >= p.x ? 1f : -1f, 0.6f), 9f, false);
            }
            Destroy(gameObject);
        }
    }
}
