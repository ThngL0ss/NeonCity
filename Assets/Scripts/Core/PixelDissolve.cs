using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Hiệu ứng tan / ghép lại TỪNG ĐIỂM ẢNH của một sprite (cinematic chết và hồi sinh).
    /// Cách chạy: sao chép sprite hiện tại của SpriteRenderer sang một texture đọc được, rồi mỗi khung hình
    /// xoá (tan) hoặc hiện (ghép) dần các điểm ảnh theo thứ tự (từ đầu xuống chân + ngẫu nhiên) và bắn hạt đúng màu điểm ảnh đó.
    /// Dùng: var d = PixelDissolve.Begin(sr, false); ... d.Step(0..1); ... d.End();
    /// </summary>
    public class PixelDissolve
    {
        struct Pt { public int x, y; public float key; public Color32 c; }

        readonly SpriteRenderer sr;
        readonly Sprite original;
        Texture2D tex;
        Sprite sprite;
        Color32[] px;
        Pt[] pts;
        int w, h, next;
        float ppu;
        Vector2 pivot;
        readonly bool assemble;

        /// <summary>assemble = false: tan dần (ban đầu đầy đủ). assemble = true: ghép dần (ban đầu trống).</summary>
        public static PixelDissolve Begin(SpriteRenderer target, bool assemble, float noise = 0.4f, bool topFirst = true)
        {
            if (target == null || target.sprite == null) return null;
            var d = new PixelDissolve(target, assemble);
            if (!d.Build(noise, topFirst)) return null;
            return d;
        }

        PixelDissolve(SpriteRenderer target, bool assemble)
        {
            sr = target;
            original = target.sprite;
            this.assemble = assemble;
        }

        bool Build(float noise, bool topFirst)
        {
            Rect r = original.rect;
            w = Mathf.RoundToInt(r.width);
            h = Mathf.RoundToInt(r.height);
            if (w <= 0 || h <= 0) return false;
            ppu = original.pixelsPerUnit;
            pivot = original.pivot;

            // sprite trong project không bật Read/Write: sao chép qua RenderTexture
            var src = original.texture;
            var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(r.x, r.y, w, h), 0, 0);
            tex.Apply(false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            px = tex.GetPixels32();
            var list = new List<Pt>(w * h / 2);
            int minY = h, maxY = 0;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var c = px[y * w + x];
                    if (c.a < 8) continue;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            float span = Mathf.Max(1, maxY - minY);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var c = px[y * w + x];
                    if (c.a < 8) continue;
                    float v = (y - minY) / span;                    // 0 = chân, 1 = đầu
                    float order = topFirst ? 1f - v : v;
                    float k = order * (1f - noise) + Random.value * noise;
                    list.Add(new Pt { x = x, y = y, key = k, c = c });
                }
            list.Sort((a, b) => a.key.CompareTo(b.key));
            pts = list.ToArray();
            next = 0;

            if (assemble)
            {
                for (int i = 0; i < pts.Length; i++) px[pts[i].y * w + pts[i].x] = new Color32(0, 0, 0, 0);
                tex.SetPixels32(px);
                tex.Apply(false);
            }
            sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(pivot.x / w, pivot.y / h), ppu, 0, SpriteMeshType.FullRect);
            sr.sprite = sprite;
            return true;
        }

        public int Count { get { return pts == null ? 0 : pts.Length; } }

        /// <summary>Vị trí thế giới của điểm ảnh (x, y) theo transform hiện tại của SpriteRenderer (có xoay / lật).</summary>
        Vector2 World(int x, int y)
        {
            return sr.transform.TransformPoint(new Vector3((x + 0.5f - pivot.x) / ppu, (y + 0.5f - pivot.y) / ppu, 0f));
        }

        /// <summary>Tiến độ u từ 0 đến 1. emitEvery = cứ bao nhiêu điểm ảnh thì bắn 1 hạt (càng lớn càng ít hạt).</summary>
        public void Step(float u, int emitEvery = 2, float particleSpeed = 3.5f, bool lift = true)
        {
            if (pts == null || sr == null) return;
            u = Mathf.Clamp01(u);
            bool changed = false;
            while (next < pts.Length && pts[next].key <= u * 1.001f)
            {
                var p = pts[next];
                if (!assemble)
                {
                    px[p.y * w + p.x] = new Color32(0, 0, 0, 0);
                    if (emitEvery > 0 && next % emitEvery == 0) EmitOut(p, particleSpeed, lift);
                }
                else
                {
                    px[p.y * w + p.x] = p.c;
                }
                next++;
                changed = true;
            }
            if (u >= 1f && next < pts.Length) { while (next < pts.Length) { var p = pts[next++]; px[p.y * w + p.x] = assemble ? p.c : new Color32(0, 0, 0, 0); } changed = true; }
            if (changed)
            {
                tex.SetPixels32(px);
                tex.Apply(false);
            }
        }

        void EmitOut(Pt p, float speed, bool lift)
        {
            Vector2 wp = World(p.x, p.y);
            Color c = new Color(p.c.r / 255f, p.c.g / 255f, p.c.b / 255f, 1f);
            Vector2 v = new Vector2(Random.Range(-1f, 1f), Random.Range(0.2f, 1.2f)) * speed * Random.Range(0.4f, 1f);
            if (!lift) v.y *= 0.2f;
            float life = Random.Range(0.5f, 1.1f);
            Fx.Emit(Fx.S_Square, wp, v, new Vector2(0.0625f, 0.0625f), new Vector2(0.03f, 0.03f), c, Cfg.A(c, 0f), life, lift ? -3f : 4f, 1.2f);
            if (Random.value < 0.08f)
                Fx.Emit(Fx.S_Glow, wp, v * 0.5f, new Vector2(0.4f, 0.4f), Vector2.zero, Cfg.A(c, 0.6f), Cfg.A(c, 0f), life * 0.7f);
        }

        /// <summary>Ghép lại (assemble): bắn các hạt từ xa bay vào đúng vị trí điểm ảnh sắp hiện. Gọi cùng lúc với Step.</summary>
        public void Converge(float u, float radius, float flyTime, int perCall)
        {
            if (pts == null || !assemble) return;
            int from = Mathf.Min(pts.Length - 1, Mathf.FloorToInt(u * pts.Length));
            for (int i = 0; i < perCall; i++)
            {
                int idx = Mathf.Clamp(from + Random.Range(0, Mathf.Max(1, pts.Length / 10)), 0, pts.Length - 1);
                var p = pts[idx];
                Vector2 to = World(p.x, p.y);
                Vector2 dir = Random.insideUnitCircle.normalized;
                Vector2 start = to + dir * radius * Random.Range(0.6f, 1f);
                Vector2 v = (to - start) / Mathf.Max(0.05f, flyTime);
                Color c = new Color(p.c.r / 255f, p.c.g / 255f, p.c.b / 255f, 1f);
                Fx.Emit(Fx.S_Square, start, v, new Vector2(0.0625f, 0.0625f), new Vector2(0.0625f, 0.0625f), Cfg.A(c, 0f), c, flyTime * 0.999f);
            }
        }

        /// <summary>Vị trí thế giới của một điểm ảnh ngẫu nhiên còn đang hiện (để bắn tia lửa / mảnh vỡ). null nếu không có.</summary>
        public Vector2 RandomPoint()
        {
            if (pts == null || pts.Length == 0) return sr != null ? (Vector2)sr.transform.position : Vector2.zero;
            var p = pts[Random.Range(0, pts.Length)];
            return World(p.x, p.y);
        }

        /// <summary>Trả sprite về ban đầu và huỷ texture tạm.</summary>
        public void End(bool restoreSprite = true)
        {
            if (sr != null && restoreSprite) sr.sprite = original;
            if (sprite != null) Object.Destroy(sprite);
            if (tex != null) Object.Destroy(tex);
            sprite = null; tex = null; pts = null;
        }
    }
}
