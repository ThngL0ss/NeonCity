using UnityEngine;

namespace NeonBreaker
{
    /// <summary>Vùng cảnh báo trên sàn / vòng cảnh báo trước khi boss ra đòn.</summary>
    public class Telegraph : MonoBehaviour
    {
        float t, dur;
        SpriteRenderer fill, edgeA, edgeB;
        Color col;
        bool ring;
        float ringSize;

        public static Telegraph Zone(Rect r, Color c, float dur)
        {
            var go = new GameObject("Telegraph");
            go.transform.SetParent(Level.Sub("Telegraphs"), false);
            go.transform.position = new Vector3(r.center.x, r.center.y, 0f);
            var tg = go.AddComponent<Telegraph>();
            tg.dur = Mathf.Max(0.05f, dur);
            tg.col = c;
            tg.fill = Art.Spr(go.transform, "fill", Art.Square, Vector2.zero, r.size, Cfg.A(c, 0.1f), 5, true);
            tg.edgeA = Art.Spr(go.transform, "l", Art.Square, new Vector2(-r.width * 0.5f, 0f), new Vector2(0.09f, r.height), Cfg.A(c, 0.9f), 6, true);
            tg.edgeB = Art.Spr(go.transform, "r", Art.Square, new Vector2(r.width * 0.5f, 0f), new Vector2(0.09f, r.height), Cfg.A(c, 0.9f), 6, true);
            return tg;
        }

        public static Telegraph Circle(Vector2 pos, float radius, Color c, float dur)
        {
            var go = new GameObject("TelegraphRing");
            go.transform.SetParent(Level.Sub("Telegraphs"), false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            var tg = go.AddComponent<Telegraph>();
            tg.dur = Mathf.Max(0.05f, dur);
            tg.col = c;
            tg.ring = true;
            tg.ringSize = radius * 2f;
            tg.fill = Art.Spr(go.transform, "ring", Art.Ring, Vector2.zero, new Vector2(tg.ringSize, tg.ringSize * 0.5f), Cfg.A(c, 0.9f), 6, true);
            tg.edgeA = Art.Spr(go.transform, "inner", Art.Glow, Vector2.zero, new Vector2(0.1f, 0.05f), Cfg.A(c, 0.5f), 5, true);
            return tg;
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            if (ring)
            {
                float s = ringSize * k;
                edgeA.transform.localScale = new Vector3(s, s * 0.5f, 1f);
                float pulse = 0.6f + 0.4f * Mathf.Sin(t * 30f);
                fill.color = Cfg.A(col, 0.5f + 0.5f * pulse);
            }
            else
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(t * (14f + 26f * k));
                fill.color = Cfg.A(col, 0.08f + 0.30f * k * (0.6f + 0.4f * pulse));
                edgeA.color = Cfg.A(col, 0.5f + 0.5f * pulse);
                edgeB.color = edgeA.color;
            }
            if (t >= dur) Destroy(gameObject);
        }
    }
}
