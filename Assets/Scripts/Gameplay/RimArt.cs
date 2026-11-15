using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Hình vẽ (bằng code, không cần file ảnh) cho Rìa Phố Neon: mặt tiền MerShop / Phòng thí nghiệm / Lều trại / Bãi rác công nghệ
    /// và vài đồ vật nội thất (lều, đống lửa, thùng phuy, giường xếp, đống rác). Cùng quy ước với TownArt: 16 px = 1 đơn vị, pivot giữa đáy.
    /// </summary>
    public static class RimArt
    {
        public const int FacadeMer = 0, FacadeLab = 1, FacadeCamp = 2, FacadeJunk = 3;

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        static Sprite Cached(string key, System.Func<PxCanvas> make, float py = 0f)
        {
            Sprite s;
            if (cache.TryGetValue(key, out s) && s != null) return s;
            s = make().Bake(0.5f, py);
            cache[key] = s;
            return s;
        }

        static Color32 K(int rgb) { return PxCanvas.C(rgb); }
        static Color32 Sh(Color32 c, float k) { return PxCanvas.Shade(c, k); }
        static readonly Color32 Ink = PxCanvas.C(0x0A0A16);

        /// <summary>Kích thước (px) mặt tiền: x = rộng, y = cao.</summary>
        public static Vector2Int FacadeSize(int id)
        {
            switch (id)
            {
                case FacadeMer: return new Vector2Int(232, 128);
                case FacadeLab: return new Vector2Int(544, 192);
                case FacadeCamp: return new Vector2Int(448, 144);
                default: return new Vector2Int(736, 192);
            }
        }

        /// <summary>Cửa chính lệch khỏi tâm mặt tiền bao nhiêu đơn vị (lều trại: cổng nằm bên phải, đống lửa ở giữa).</summary>
        public static float DoorDx(int id) { return id == FacadeCamp ? 8f : 0f; }

        public static Sprite Facade(int id)
        {
            return Cached("rimfacade." + id, () =>
            {
                switch (id)
                {
                    case FacadeMer: return FacadeMerShop();
                    case FacadeLab: return FacadeLab_();
                    case FacadeCamp: return FacadeCamp_();
                    default: return FacadeJunk_();
                }
            });
        }

        // ================================================================ MERSHOP (1 tầng, tồi tàn)
        static PxCanvas FacadeMerShop()
        {
            int W = 232, H = 128;
            var c = new PxCanvas(W, H);
            var rnd = new System.Random(31);
            Color32 wood = K(0x5C4A38), woodD = K(0x3A2E22), rust = K(0x7A4A2A), tin = K(0x6E6A64);

            // tường ván cũ
            c.VGrad(0, 16, W, H - 24, wood, woodD);
            for (int y = 20; y < H - 8; y += 7) c.Rect(0, y, W, 1, Sh(woodD, 0.7f));
            for (int i = 0; i < 40; i++)
            {
                int x = rnd.Next(W - 12), y = 18 + rnd.Next(H - 40);
                c.Rect(x, y, 2 + rnd.Next(10), 1, Sh(wood, 0.8f + (float)rnd.NextDouble() * 0.4f));
            }
            // miếng vá
            int[] pc = { 0x6A5A44, 0x4A5A5A, 0x6A3A2A, 0x5A5A4A };
            for (int i = 0; i < 7; i++)
            {
                int x = 6 + rnd.Next(W - 44), y = 30 + rnd.Next(56), pw = 12 + rnd.Next(18), ph = 8 + rnd.Next(12);
                Color32 col = K(pc[i % pc.Length]);
                c.Rect(x, y, pw, ph, col);
                c.Frame(x, y, pw, ph, Sh(col, 0.6f));
                c.Set(x + 1, y + 1, tin); c.Set(x + pw - 2, y + 1, tin); c.Set(x + 1, y + ph - 2, tin); c.Set(x + pw - 2, y + ph - 2, tin);
            }

            // mái tôn gỉ, có lỗ thủng
            c.Rect(0, 4, W, 14, tin);
            for (int x = 0; x < W; x += 4) { c.Rect(x, 4, 1, 14, Sh(tin, 0.78f)); c.Rect(x + 2, 4, 1, 14, Sh(tin, 1.12f)); }
            for (int i = 0; i < 24; i++) c.Rect(rnd.Next(W - 4), 4 + rnd.Next(10), 3, 2 + rnd.Next(5), rust);
            c.Rect(150, 4, 16, 9, K(0x0A0A12));
            c.Rect(0, 17, W, 2, Sh(tin, 0.5f));
            c.Rect(6, 0, 30, 5, tin); c.Rect(6, 0, 30, 1, Sh(tin, 1.3f));          // tấm tôn dựng thêm
            c.Rect(196, 1, 26, 4, Sh(tin, 0.85f));

            // biển hiệu treo lệch
            c.Line(86, 19, 86, 25, K(0x3A3A3A)); c.Line(146, 19, 146, 23, K(0x3A3A3A));
            c.Rect(80, 25, 72, 18, K(0x241A12));
            c.Frame(80, 25, 72, 18, K(0x8A5A2A));
            c.Rect(81, 26, 70, 1, K(0x3A2A1A));
            c.TextC(116, 31, "MERSHOP", K(0xE8923A), 2);
            c.Rect(84, 44, 64, 1, Sh(K(0xE8923A), 0.45f));                          // dải đèn yếu ớt dưới biển
            c.Set(82, 27, tin); c.Set(149, 27, tin);

            // cửa tối + tấm rèm rách
            c.Rect(96, 60, 40, 4, K(0x2A2018));
            c.Rect(100, 64, 32, 56, K(0x050508));
            c.Rect(97, 64, 3, 56, K(0x3A2E22)); c.Rect(132, 64, 3, 56, K(0x3A2E22));
            c.Rect(122, 64, 10, 56, K(0x5A3A2A));
            c.Frame(122, 64, 10, 56, K(0x2A1810));
            for (int k = 0; k < 7; k++) c.Rect(100 + k * 4, 64, 3, 12 + rnd.Next(14), K(0x5A2222));
            c.Rect(126, 90, 2, 6, K(0x9A9AA2));

            // cửa sổ đóng ván
            for (int s = 0; s < 2; s++)
            {
                int wx = s == 0 ? 22 : 174;
                c.Rect(wx, 58, 36, 26, K(0x0C0B12));
                c.Rect(wx + 4, 74, 28, 2, Sh(K(0xE8B060), 0.5f));
                c.Frame(wx - 1, 57, 38, 28, K(0x2A2018));
                c.Line(wx - 3, 62, wx + 38, 78, K(0x6A5238));
                c.Line(wx - 3, 62, wx + 38, 64, K(0x7A6244));
                c.Line(wx - 3, 80, wx + 38, 70, K(0x5A4630));
            }

            // nền + đồ lặt vặt
            c.Rect(0, H - 8, W, 8, K(0x2A2420));
            c.Rect(0, H - 8, W, 1, K(0x3A322A));
            c.Rect(6, H - 24, 15, 16, K(0x6A5030)); c.Frame(6, H - 24, 15, 16, K(0x3A2A18)); c.Line(6, H - 24, 20, H - 9, K(0x3A2A18));
            c.Rect(208, H - 22, 9, 14, K(0x7A4A2A)); c.Rect(208, H - 17, 9, 1, K(0x3A2410)); c.Rect(208, H - 12, 9, 1, K(0x3A2410));
            c.Rect(34, H - 11, 4, 3, K(0x8A8A90)); c.Rect(190, H - 10, 5, 2, K(0x8A8A90));
            c.Outline(Ink);
            return c;
        }

        // ================================================================ PHÒNG THÍ NGHIỆM (tường dài, cổng neon lớn)
        static PxCanvas FacadeLab_()
        {
            int W = 544, H = 192;
            var c = new PxCanvas(W, H);
            Color32 steel = K(0x1E2536), steelD = K(0x10141F), org = K(0xFF7A1A), orgD = K(0x8A3A0C), cy = K(0x35E8FF);

            // tường dài
            c.VGrad(0, 24, W, H - 32, steel, steelD);
            for (int x = 0; x < W; x += 32) { c.Rect(x, 24, 1, H - 32, Sh(steelD, 0.7f)); c.Rect(x + 1, 24, 1, H - 32, Sh(steel, 1.25f)); }
            for (int y = 56; y < H - 8; y += 40) c.Rect(0, y, W, 1, Sh(steelD, 0.6f));
            // mái / gờ trên + dải neon
            c.Rect(0, 12, W, 12, K(0x2A3248));
            c.Rect(0, 12, W, 2, org);
            c.Rect(0, 24, W, 2, orgD);
            for (int x = 6; x < W; x += 24) c.Rect(x, 16, 2, 6, steelD);
            // ăng-ten + camera
            for (int k = 0; k < 6; k++)
            {
                int ax = 40 + k * 96;
                c.Rect(ax, 0, 1, 12, K(0x4A5470));
                c.Rect(ax - 2, 3, 5, 1, K(0x4A5470));
                c.Set(ax, 0, k % 2 == 0 ? org : cy);
            }

            // ô cửa sổ khe + biển số hai bên (chừa chỗ cho cổng ở giữa)
            int[][] spans = { new[] { 56, 176 }, new[] { 368, 488 } };
            int plate = 1;
            for (int s = 0; s < spans.Length; s++)
            {
                for (int x = spans[s][0]; x < spans[s][1]; x += 40)
                {
                    bool on = ((x / 40 + s) % 3) != 1;
                    c.Rect(x, 70, 24, 8, on ? Sh(org, 0.75f) : K(0x0A0C14));
                    c.Frame(x - 1, 69, 26, 10, Ink);
                    c.Rect(x, 110, 24, 8, on ? K(0x0A0C14) : Sh(org, 0.5f));
                    c.Frame(x - 1, 109, 26, 10, Ink);
                    c.Text(x + 5, 44, "A" + (plate < 10 ? "0" : "") + plate, Sh(org, 0.8f), 1);
                    plate++;
                }
            }
            // ống dẫn ngang
            c.Rect(0, 148, W, 4, K(0x2E3650)); c.Rect(0, 148, W, 1, K(0x4A5470));
            for (int x = 0; x < W; x += 48) c.Rect(x, 146, 3, 8, K(0x4A5470));

            // bồn chứa hai đầu
            for (int s = 0; s < 2; s++)
            {
                int tx = s == 0 ? 8 : 496;
                c.VGrad(tx, 100, 40, 84, K(0x3A4560), K(0x1A2034));
                c.Rect(tx + 4, 100, 4, 84, K(0x5A6A90));
                for (int y = 112; y < 180; y += 18) c.Rect(tx, y, 40, 2, K(0x0E1220));
                c.Rect(tx + 14, 120, 12, 4, org); c.Rect(tx + 14, 126, 12, 2, orgD);
                c.Frame(tx, 100, 40, 84, Ink);
            }

            // hai trụ cổng + đèn
            for (int s = 0; s < 2; s++)
            {
                int px = s == 0 ? 184 : 332;
                c.Rect(px, 40, 28, 144, K(0x2A3248));
                c.Rect(px, 40, 28, 3, K(0x4A5470));
                c.Rect(px + 12, 46, 4, 134, org);
                c.Rect(px + 11, 46, 1, 134, orgD); c.Rect(px + 16, 46, 1, 134, orgD);
                c.Disc(px + 14, 34, 5, org); c.Disc(px + 14, 34, 2, K(0xFFF2C0));
                c.Frame(px, 40, 28, 144, Ink);
            }

            // cổng neon lớn
            c.Rect(206, 56, 132, 20, K(0x07090F));
            c.Frame(206, 56, 132, 20, org);
            c.Frame(208, 58, 128, 16, orgD);
            c.TextC(272, 62, "LAB", org, 3);
            for (int i = 0; i < 8; i++) { c.Rect(212 + i * 4, 58, 2, 4, K(0xE8B020)); c.Rect(300 + i * 4, 58, 2, 4, K(0xE8B020)); }
            c.Rect(216, 80, 112, 104, K(0x05070C));
            c.Frame(216, 80, 112, 104, org);
            c.Frame(218, 82, 108, 102, orgD);
            for (int leaf = 0; leaf < 2; leaf++)
            {
                int lx = leaf == 0 ? 222 : 274;
                c.Rect(lx, 88, 48, 96, K(0x0E1220));
                c.Frame(lx, 88, 48, 96, Sh(org, 0.6f));
                for (int k = 0; k < 4; k++)
                {
                    int cy0 = 104 + k * 20;
                    if (leaf == 0) { c.Line(lx + 8, cy0, lx + 24, cy0 + 8, Sh(cy, 0.7f)); c.Line(lx + 8, cy0 + 16, lx + 24, cy0 + 8, Sh(cy, 0.7f)); }
                    else { c.Line(lx + 40, cy0, lx + 24, cy0 + 8, Sh(cy, 0.7f)); c.Line(lx + 40, cy0 + 16, lx + 24, cy0 + 8, Sh(cy, 0.7f)); }
                }
            }
            c.Rect(271, 88, 2, 96, org);

            // nền + dải cảnh báo
            c.Rect(0, 184, W, 8, K(0x2A3040));
            for (int x = 0; x < W; x += 16) c.Rect(x, 186, 8, 4, K(0xE8B020));
            c.Outline(Ink);
            return c;
        }

        // ================================================================ LỀU TRẠI (hàng rào dây thép gai, lều vá, đống lửa)
        static void DrawTent(PxCanvas c, int cx, int baseY, int w, int h, int pal, float shade, int seed)
        {
            int[] cols = { 0x5A5A3A, 0x8A7450, 0x3A4A5A, 0x6A3A2A, 0x4A5A44 };
            var rnd = new System.Random(seed);
            Color32 baseC = Sh(K(cols[pal % cols.Length]), shade);
            for (int y = 0; y < h; y++)
            {
                int hw = Mathf.Max(1, Mathf.RoundToInt(w * 0.5f * (y + 1) / h));
                for (int x = -hw; x <= hw; x++)
                {
                    float k = ((x + 1000) / 7) % 2 == 0 ? 1f : 0.84f;
                    if (x < -hw / 2) k += 0.08f;
                    c.Set(cx + x, baseY - h + y, Sh(baseC, k));
                }
                c.Set(cx - hw, baseY - h + y, Sh(baseC, 0.5f));
                c.Set(cx + hw, baseY - h + y, Sh(baseC, 0.5f));
            }
            // miếng vá + đường khâu
            for (int p = 0; p < 4; p++)
            {
                int y = h / 3 + rnd.Next(Mathf.Max(1, h * 2 / 3 - 8));
                int hw = Mathf.RoundToInt(w * 0.5f * (y + 1) / h);
                if (hw < 9) continue;
                int span = Mathf.Max(1, hw * 2 - 12);
                int px = cx - hw + 3 + rnd.Next(span);
                int pw = 5 + rnd.Next(5), ph = 4 + rnd.Next(4);
                Color32 pcol = Sh(K(cols[(pal + 1 + p) % cols.Length]), shade);
                c.Rect(px, baseY - h + y, pw, ph, pcol);
                c.Frame(px, baseY - h + y, pw, ph, Sh(pcol, 0.55f));
            }
            // cửa lều
            int ew = Mathf.Max(6, Mathf.RoundToInt(w * 0.28f)), eh = Mathf.Max(8, Mathf.RoundToInt(h * 0.6f));
            for (int y = 0; y < eh; y++)
            {
                int hw2 = Mathf.Max(1, Mathf.RoundToInt(ew * 0.5f * (y + 1) / eh));
                c.Rect(cx - hw2, baseY - eh + y, hw2 * 2 + 1, 1, K(0x07070B));
            }
            // cọc + cờ rách + dây chằng
            c.Rect(cx, baseY - h - 6, 1, 7, K(0x4A3A2A));
            c.Rect(cx + 1, baseY - h - 6, 4, 2, K(0x8A2A2A));
            c.Line(cx, baseY - h, cx - w / 2 - 8, baseY, Sh(K(0x6A6A6A), shade));
            c.Line(cx, baseY - h, cx + w / 2 + 8, baseY, Sh(K(0x6A6A6A), shade));
            c.Rect(cx - w / 2, baseY - 1, w + 1, 1, Sh(baseC, 0.45f));
        }

        static void DrawFlame(PxCanvas c, int cx, int baseY, int w, int h, Color32 col)
        {
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)h;
                float hw = (w * 0.5f) * Mathf.Sin(Mathf.Min(1f, t * 1.25f) * 1.5708f);
                if (t > 0.85f) hw *= Mathf.Max(0.2f, 1f - (t - 0.85f) * 3f);
                int ihw = Mathf.RoundToInt(hw);
                c.Rect(cx - ihw, baseY - h + y, ihw * 2 + 1, 1, col);
            }
        }

        static void DrawFire(PxCanvas c, int cx, int baseY)
        {
            // vòng đá
            for (int i = -2; i <= 2; i++) c.Disc(cx + i * 7, baseY - 2, 3, K(0x5A5A62));
            // củi chéo
            c.Line(cx - 14, baseY - 3, cx + 12, baseY - 9, K(0x4A2E1A));
            c.Line(cx + 14, baseY - 3, cx - 12, baseY - 9, K(0x5A3A22));
            c.Line(cx - 14, baseY - 4, cx + 12, baseY - 10, K(0x3A2412));
            // lửa
            DrawFlame(c, cx - 9, baseY - 6, 12, 22, K(0xD8341A));
            DrawFlame(c, cx + 9, baseY - 6, 12, 24, K(0xD8341A));
            DrawFlame(c, cx, baseY - 6, 22, 38, K(0xD8341A));
            DrawFlame(c, cx, baseY - 6, 15, 29, K(0xFF8A1A));
            DrawFlame(c, cx, baseY - 6, 8, 19, K(0xFFE45A));
            c.Set(cx - 14, baseY - 46, K(0xFFC040)); c.Set(cx + 10, baseY - 52, K(0xFFC040)); c.Set(cx + 3, baseY - 58, K(0xFF8A1A));
        }

        static PxCanvas FacadeCamp_()
        {
            int W = 448, H = 144;
            var c = new PxCanvas(W, H);
            // hàng lều sau (tối hơn) rồi lều trước
            DrawTent(c, 58, 120, 104, 78, 0, 0.62f, 3);
            DrawTent(c, 148, 120, 84, 62, 2, 0.62f, 5);
            DrawTent(c, 300, 120, 92, 68, 3, 0.62f, 8);
            DrawTent(c, 408, 120, 80, 58, 4, 0.62f, 13);
            DrawTent(c, 100, 126, 76, 54, 1, 1f, 21);
            DrawTent(c, 12, 128, 52, 38, 3, 0.9f, 22);
            // đống lửa lớn ở giữa trại
            DrawFire(c, 224, 126);

            // hàng rào dây thép gai
            int gx = 352;
            for (int x = 0; x < W; x += 28)
            {
                if (Mathf.Abs(x + 1 - gx) < 44) continue;
                int lean = (x / 28) % 3 - 1;
                c.Rect(x, 96, 3, 44, K(0x6A3A22));
                c.Rect(x, 96, 1, 44, K(0x8A4A2A));
                c.Set(x + lean, 95, K(0x8A4A2A));
            }
            int[] wy = { 104, 120 };
            for (int w = 0; w < wy.Length; w++)
                for (int x = 0; x < W; x++)
                {
                    if (Mathf.Abs(x - gx) < 40) continue;
                    int yy = wy[w] + Mathf.RoundToInt(Mathf.Sin(x / 28f * 3.1416f) * 1.5f);
                    c.Set(x, yy, K(0x9A9AA2));
                    if (x % 8 == 4) { c.Set(x, yy - 1, K(0xC8C8D0)); c.Set(x, yy + 1, K(0xC8C8D0)); c.Set(x + 1, yy - 1, K(0xC8C8D0)); c.Set(x - 1, yy + 1, K(0xC8C8D0)); }
                }

            // cổng sắt rỉ + đầu lâu sơn
            for (int s = -1; s <= 1; s += 2)
            {
                int pxx = gx + s * 38 - 3;
                c.Rect(pxx, 62, 6, 78, K(0x4A2A1A));
                c.Rect(pxx, 62, 1, 78, K(0x7A4A2A));
                c.Rect(pxx + 1, 54, 4, 8, K(0xE8E2D2));                          // đầu lâu nhỏ trên cọc
                c.Set(pxx + 1, 57, Ink); c.Set(pxx + 4, 57, Ink);
            }
            c.VGrad(gx - 32, 70, 64, 70, K(0x5A3A2A), K(0x38231A));
            for (int x = gx - 32; x < gx + 32; x += 10) c.Rect(x, 70, 1, 70, K(0x2A1810));
            var rnd = new System.Random(9);
            for (int i = 0; i < 14; i++) c.Rect(gx - 30 + rnd.Next(58), 72 + rnd.Next(50), 2, 6 + rnd.Next(14), K(0x8A4A22));
            for (int y = 76; y < 138; y += 14) { c.Set(gx - 29, y, K(0x9A9AA2)); c.Set(gx + 29, y, K(0x9A9AA2)); }
            c.Rect(gx - 32, 124, 64, 4, K(0x2A2018));
            c.Frame(gx - 32, 70, 64, 70, K(0x1A100A));
            // đầu lâu sơn trắng lớn
            c.Disc(gx, 92, 12, K(0xE8E2D2));
            c.Rect(gx - 7, 100, 14, 8, K(0xE8E2D2));
            c.Disc(gx - 5, 92, 3, Ink); c.Disc(gx + 5, 92, 3, Ink);
            c.Rect(gx - 1, 97, 3, 3, Ink);
            for (int i = 0; i < 4; i++) c.Rect(gx - 5 + i * 3, 104, 1, 4, Ink);
            c.Rect(gx - 7, 103, 14, 1, Ink);
            c.Rect(gx - 9, 108, 1, 9, K(0xB01E1E)); c.Rect(gx - 2, 108, 1, 14, K(0xB01E1E)); c.Rect(gx + 6, 108, 1, 7, K(0xB01E1E));
            c.Rect(0, H - 4, W, 4, K(0x3A2E22));
            c.Outline(Ink);
            return c;
        }

        // ================================================================ BÃI RÁC CÔNG NGHỆ
        static readonly int[] junkCols = { 0x6A6A74, 0x8A8A94, 0x7A4A2A, 0x4A5A6A, 0x5A3A4A, 0xA08A5A, 0x3A6A6A, 0x6A2A2A };

        static void Scrap(PxCanvas c, System.Random rnd, int x, int y, float bright)
        {
            Color32 col = Sh(K(junkCols[rnd.Next(junkCols.Length)]), bright * (0.8f + (float)rnd.NextDouble() * 0.4f));
            switch (rnd.Next(7))
            {
                case 0:
                    c.Rect(x, y, 5 + rnd.Next(10), 2 + rnd.Next(4), col);
                    break;
                case 1:
                    {
                        int r = 4 + rnd.Next(3);
                        c.Disc(x, y, r, Sh(K(0x16161C), bright + 0.3f));
                        c.Disc(x, y, Mathf.Max(1, r - 3), Sh(K(0x6A6A74), bright));
                        break;
                    }
                case 2:
                    c.Line(x, y, x + rnd.Next(-14, 15), y + rnd.Next(-6, 7), col);
                    break;
                case 3:
                    c.Rect(x, y, 12, 9, Sh(K(0x2A2A32), bright));
                    c.Rect(x + 2, y + 2, 8, 5, rnd.NextDouble() < 0.3 ? Sh(K(0x35E8FF), 0.5f) : K(0x101018));
                    break;
                case 4:
                    c.Rect(x, y + 4, 26, 8, col);
                    c.Rect(x + 5, y, 14, 5, col);
                    c.Rect(x + 7, y + 1, 10, 3, Sh(K(0x0C0C12), bright + 0.3f));
                    c.Disc(x + 6, y + 12, 3, Sh(K(0x16161C), bright + 0.3f));
                    c.Disc(x + 20, y + 12, 3, Sh(K(0x16161C), bright + 0.3f));
                    break;
                case 5:
                    c.Rect(x, y, 2 + rnd.Next(5), 1, Sh(rnd.NextDouble() < 0.5 ? K(0xFF2FB5) : K(0x35E8FF), bright + 0.5f));
                    break;
                default:
                    c.Rect(x, y, 7, 9, Sh(K(0x7A4A2A), bright));
                    c.Rect(x, y + 3, 7, 1, Sh(K(0x3A2410), bright));
                    c.Rect(x, y + 6, 7, 1, Sh(K(0x3A2410), bright));
                    break;
            }
        }

        static void PileLayer(PxCanvas c, System.Random rnd, int x0, int x1, int baseY, int amp, int col0, int col1, int debris, float bright, int bottom)
        {
            int n = x1 - x0;
            var top = new int[n];
            float p1 = (float)rnd.NextDouble() * 6f, p2 = (float)rnd.NextDouble() * 6f, p3 = (float)rnd.NextDouble() * 6f;
            int jag = 0;
            for (int i = 0; i < n; i++)
            {
                float s = 0.5f + 0.28f * Mathf.Sin(i * 0.021f + p1) + 0.18f * Mathf.Sin(i * 0.057f + p2) + 0.08f * Mathf.Sin(i * 0.13f + p3);
                if (i % 5 == 0) jag = rnd.Next(-3, 4);
                top[i] = Mathf.Clamp(baseY - Mathf.RoundToInt(s * amp) + jag, 2, bottom - 6);
            }
            Color32 a = K(col0), b = K(col1);
            for (int i = 0; i < n; i++)
                for (int y = top[i]; y < bottom; y++)
                {
                    float t = (y - top[i]) / (float)Mathf.Max(1, bottom - top[i]);
                    c.Set(x0 + i, y, PxCanvas.Mix(a, b, t));
                }
            for (int d = 0; d < debris; d++)
            {
                int i = rnd.Next(n);
                int y = top[i] + 1 + rnd.Next(Mathf.Max(1, bottom - top[i] - 4));
                Scrap(c, rnd, x0 + i, y, bright);
            }
        }

        static PxCanvas FacadeJunk_()
        {
            int W = 736, H = 192;
            var c = new PxCanvas(W, H);
            var rnd = new System.Random(77);
            PileLayer(c, rnd, 0, W, 112, 72, 0x2C2834, 0x1A1822, 260, 0.55f, H - 8);
            // cần cẩu gỉ phía sau
            c.Rect(560, 24, 6, 130, K(0x5A3A2A)); c.Rect(520, 24, 96, 5, K(0x6A4A34));
            c.Line(560, 24, 600, 29, K(0x6A4A34)); c.Line(526, 29, 526, 76, K(0x4A4A52)); c.Rect(523, 76, 7, 5, K(0x8A8A94));
            c.Set(560, 22, K(0xFF3A3A));
            PileLayer(c, rnd, 0, W, 158, 54, 0x3E3A46, 0x24202C, 340, 0.8f, H - 8);
            for (int k = 0; k < 6; k++) Scrap(c, rnd, 30 + k * 120 + rnd.Next(30), 150 + rnd.Next(14), 0.9f);
            c.Rect(0, H - 8, W, 8, K(0x2A2420));
            c.Rect(0, H - 8, W, 1, K(0x3A322A));
            c.Outline(Ink);
            return c;
        }

        /// <summary>Một gò rác đứng riêng (đặt phía trước bãi rác để thêm độ sâu).</summary>
        public static Sprite JunkPile(int w, int h, int seed)
        {
            return Cached("junkpile." + w + "." + h + "." + seed, () =>
            {
                var c = new PxCanvas(w, h);
                var rnd = new System.Random(seed * 17 + 3);
                var top = new int[w];
                int jag = 0;
                for (int i = 0; i < w; i++)
                {
                    float hump = Mathf.Pow(Mathf.Sin(Mathf.PI * (i + 0.5f) / w), 0.7f);
                    if (i % 4 == 0) jag = rnd.Next(-2, 3);
                    top[i] = Mathf.Clamp(h - Mathf.RoundToInt(hump * (h - 4)) + jag, 1, h - 2);
                }
                Color32 a = K(0x4A4652), b = K(0x2A2632);
                for (int i = 0; i < w; i++)
                    for (int y = top[i]; y < h; y++) c.Set(i, y, PxCanvas.Mix(a, b, (y - top[i]) / (float)Mathf.Max(1, h - top[i])));
                for (int d = 0; d < w / 3; d++)
                {
                    int i = rnd.Next(w);
                    Scrap(c, rnd, i, top[i] + 1 + rnd.Next(Mathf.Max(1, h - top[i] - 4)), 0.85f);
                }
                c.Outline(Ink);
                return c;
            });
        }

        // ================================================================ ĐỒ VẬT NỘI THẤT / NGOẠI CẢNH
        /// <summary>Lều vá đứng riêng (pivot giữa đáy).</summary>
        public static Sprite Tent(int pal, int w, int h, int seed)
        {
            return Cached("tent." + pal + "." + w + "." + h + "." + seed, () =>
            {
                var c = new PxCanvas(w + 20, h + 12);
                DrawTent(c, (w + 20) / 2, h + 10, w, h, pal, 1f, seed);
                c.Outline(Ink);
                return c;
            });
        }

        /// <summary>Đống lửa trại (pivot giữa đáy).</summary>
        public static Sprite Bonfire()
        {
            return Cached("bonfire", () =>
            {
                var c = new PxCanvas(48, 64);
                DrawFire(c, 24, 60);
                c.Outline(Ink);
                return c;
            });
        }

        public static Sprite Barrel(int col)
        {
            return Cached("barrel." + col, () =>
            {
                var c = new PxCanvas(16, 20);
                Color32 b = K(col);
                c.VGrad(1, 1, 14, 18, Sh(b, 1.15f), Sh(b, 0.75f));
                c.Rect(1, 5, 14, 1, Sh(b, 0.5f)); c.Rect(1, 13, 14, 1, Sh(b, 0.5f));
                c.Rect(3, 1, 2, 18, Sh(b, 1.3f));
                c.Rect(1, 0, 14, 1, Sh(b, 0.6f));
                c.Outline(Ink);
                return c;
            });
        }

        /// <summary>Giường xếp cũ.</summary>
        public static Sprite Cot()
        {
            return Cached("cot", () =>
            {
                var c = new PxCanvas(44, 14);
                c.Rect(0, 6, 44, 3, K(0x5A5A3A));
                c.Rect(0, 5, 44, 1, K(0x7A7A52));
                c.Rect(2, 9, 2, 5, K(0x3A3A42)); c.Rect(40, 9, 2, 5, K(0x3A3A42));
                c.Rect(2, 2, 12, 4, K(0xB8A888));
                c.Outline(Ink);
                return c;
            });
        }

        /// <summary>Hàng rào dây thép gai đứng riêng (w px).</summary>
        public static Sprite Wire(int w)
        {
            return Cached("wire." + w, () =>
            {
                var c = new PxCanvas(w, 30);
                for (int x = 0; x < w; x += 20) { c.Rect(x, 2, 3, 28, K(0x6A3A22)); c.Rect(x, 2, 1, 28, K(0x8A4A2A)); }
                int[] wy = { 8, 20 };
                for (int k = 0; k < wy.Length; k++)
                    for (int x = 0; x < w; x++)
                    {
                        int yy = wy[k] + Mathf.RoundToInt(Mathf.Sin(x / 20f * 3.1416f) * 1.5f);
                        c.Set(x, yy, K(0x9A9AA2));
                        if (x % 8 == 4) { c.Set(x, yy - 1, K(0xC8C8D0)); c.Set(x, yy + 1, K(0xC8C8D0)); }
                    }
                return c;
            });
        }

        /// <summary>Bao cát / chắn xếp chồng.</summary>
        public static Sprite Sandbags(int w)
        {
            return Cached("sandbags." + w, () =>
            {
                var c = new PxCanvas(w, 14);
                for (int row = 0; row < 2; row++)
                    for (int x = row * 5; x < w; x += 12)
                    {
                        int y = 13 - (row + 1) * 6;
                        c.Rect(x, y, 11, 6, K(row == 0 ? 0x8A7A52 : 0x7A6A44));
                        c.Frame(x, y, 11, 6, K(0x4A3E26));
                    }
                c.Outline(Ink);
                return c;
            });
        }

        /// <summary>Quầy tiếp tân gỗ cũ vá víu (MerShop). Pivot giữa đáy.</summary>
        public static Sprite ShabbyCounter(int wpx)
        {
            return Cached("shabbycounter." + wpx, () =>
            {
                var c = new PxCanvas(wpx, 26);
                c.VGrad(0, 4, wpx, 22, K(0x5C4A38), K(0x3A2E22));
                for (int x = 6; x < wpx; x += 12) c.Rect(x, 4, 1, 22, K(0x2A2018));
                c.Rect(0, 0, wpx, 5, K(0x8A6A44));
                c.Rect(0, 0, wpx, 1, K(0xB08A58));
                c.Rect(wpx / 2 - 4, 12, 9, 7, K(0x6A5A44)); c.Frame(wpx / 2 - 4, 12, 9, 7, K(0x2A2018));
                c.Outline(Ink);
                return c;
            });
        }
    }
}
