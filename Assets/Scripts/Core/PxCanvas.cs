using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Bảng pixel nhỏ để VẼ BẰNG CODE (toạ độ y = 0 ở hàng TRÊN CÙNG) rồi đóng gói thành Sprite (lọc Point, 16 pixel = 1 đơn vị thế giới).
    /// Dùng cho nhà, NPC, đồ vật được tạo lúc chạy (không cần file ảnh). Có sẵn phông chữ 3x5 (A–Z, 0–9, vài dấu) cho biển hiệu.
    /// </summary>
    public class PxCanvas
    {
        public readonly int w, h;
        readonly Color32[] px;

        public PxCanvas(int w, int h) { this.w = w; this.h = h; px = new Color32[w * h]; }

        public static Color32 C(int rgb, int a = 255)
        {
            return new Color32((byte)((rgb >> 16) & 255), (byte)((rgb >> 8) & 255), (byte)(rgb & 255), (byte)a);
        }

        public static Color32 Mix(Color32 a, Color32 b, float t)
        {
            return new Color32((byte)Mathf.Lerp(a.r, b.r, t), (byte)Mathf.Lerp(a.g, b.g, t), (byte)Mathf.Lerp(a.b, b.b, t), 255);
        }

        public static Color32 Shade(Color32 a, float k)
        {
            return new Color32((byte)Mathf.Clamp(a.r * k, 0f, 255f), (byte)Mathf.Clamp(a.g * k, 0f, 255f), (byte)Mathf.Clamp(a.b * k, 0f, 255f), a.a);
        }

        public void Set(int x, int y, Color32 c)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            px[(h - 1 - y) * w + x] = c;
        }

        public Color32 Get(int x, int y)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return new Color32(0, 0, 0, 0);
            return px[(h - 1 - y) * w + x];
        }

        public bool Opaque(int x, int y) { return Get(x, y).a > 0; }

        public void Rect(int x, int y, int rw, int rh, Color32 c)
        {
            for (int j = 0; j < rh; j++)
                for (int i = 0; i < rw; i++) Set(x + i, y + j, c);
        }

        public void Frame(int x, int y, int rw, int rh, Color32 c)
        {
            for (int i = 0; i < rw; i++) { Set(x + i, y, c); Set(x + i, y + rh - 1, c); }
            for (int j = 0; j < rh; j++) { Set(x, y + j, c); Set(x + rw - 1, y + j, c); }
        }

        /// <summary>Tô dọc theo chiều cao từ a (trên) tới b (dưới).</summary>
        public void VGrad(int x, int y, int rw, int rh, Color32 a, Color32 b)
        {
            for (int j = 0; j < rh; j++)
            {
                var c = Mix(a, b, rh <= 1 ? 0f : j / (float)(rh - 1));
                for (int i = 0; i < rw; i++) Set(x + i, y + j, c);
            }
        }

        public void Line(int x0, int y0, int x1, int y1, Color32 c)
        {
            int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            for (int guard = 0; guard < 4096; guard++)
            {
                Set(x0, y0, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        public void Disc(int cx, int cy, int r, Color32 c)
        {
            for (int j = -r; j <= r; j++)
                for (int i = -r; i <= r; i++)
                    if (i * i + j * j <= r * r + r) Set(cx + i, cy + j, c);
        }

        /// <summary>Dán các pixel đặc của src lên canvas này tại (dx,dy), phóng to nguyên lần scale.</summary>
        public void Blit(PxCanvas src, int dx, int dy, int scale = 1)
        {
            for (int y = 0; y < src.h; y++)
                for (int x = 0; x < src.w; x++)
                {
                    var c = src.Get(x, y);
                    if (c.a == 0) continue;
                    Rect(dx + x * scale, dy + y * scale, scale, scale, c);
                }
        }

        /// <summary>Viền tối 1 pixel quanh mọi vùng đã vẽ (cho NPC / vật nhìn rõ trên nền).</summary>
        public void Outline(Color32 c)
        {
            var add = new List<int>();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (Opaque(x, y)) continue;
                    if (Opaque(x - 1, y) || Opaque(x + 1, y) || Opaque(x, y - 1) || Opaque(x, y + 1)) add.Add(y * w + x);
                }
            for (int i = 0; i < add.Count; i++) Set(add[i] % w, add[i] / w, c);
        }

        // ------------------------------------------------------------ chữ 3x5
        static Dictionary<char, string> font;

        static void Build()
        {
            font = new Dictionary<char, string>();
            string[] g =
            {
                "A010101111101101", "B110101110101110", "C011100100100011", "D110101101101110", "E111100110100111", "F111100110100100",
                "G011100101101011", "H101101111101101", "I111010010010111", "J001001001101010", "K101101110101101", "L100100100100111",
                "M101111111101101", "N110101101101101", "O010101101101010", "P110101110100100", "Q010101101111011", "R110101110101101",
                "S011100010001110", "T111010010010010", "U101101101101111", "V101101101101010", "W101101111111101", "X101101010101101",
                "Y101101010010010", "Z111001010100111",
                "0111101101101111", "1010110010010111", "2110001010100111", "3110001010001110", "4101101111001001",
                "5111100110001110", "6011100111101111", "7111001010010010", "8111101111101111", "9111101111001110",
                "-000000111000000", ".000000000000010", "!010010010000010", "?110001010000010", ":000010000010000", "+000010111010000",
                "/001001010100100", "@010101111100011",
            };
            for (int i = 0; i < g.Length; i++) font[g[i][0]] = g[i].Substring(1);
        }

        public static int TextWidth(string s, int scale = 1) { return s == null || s.Length == 0 ? 0 : (s.Length * 4 - 1) * scale; }

        /// <summary>Viết chữ in hoa (3x5, cách nhau 1 pixel). scale = phóng to nguyên lần.</summary>
        public void Text(int x, int y, string s, Color32 c, int scale = 1)
        {
            if (font == null) Build();
            if (string.IsNullOrEmpty(s)) return;
            s = s.ToUpperInvariant();
            for (int n = 0; n < s.Length; n++)
            {
                string bits;
                if (font.TryGetValue(s[n], out bits))
                {
                    for (int r = 0; r < 5; r++)
                        for (int k = 0; k < 3; k++)
                            if (bits[r * 3 + k] == '1') Rect(x + (n * 4 + k) * scale, y + r * scale, scale, scale, c);
                }
            }
        }

        /// <summary>Chữ căn giữa theo trục ngang quanh cx.</summary>
        public void TextC(int cx, int y, string s, Color32 c, int scale = 1)
        {
            Text(cx - TextWidth(s, scale) / 2, y, s, c, scale);
        }

        // ------------------------------------------------------------ đóng gói
        /// <summary>Tạo Sprite (16 px = 1 đơn vị, lọc Point). pivot mặc định: giữa đáy. Tài nguyên không bị Unity dọn khi đổi Scene.</summary>
        public Sprite Bake(float pivotX = 0.5f, float pivotY = 0f)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.SetPixels32(px);
            tex.Apply(false, false);
            var sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(pivotX, pivotY), 16f);
            sp.hideFlags = HideFlags.HideAndDontSave;
            return sp;
        }
    }
}
