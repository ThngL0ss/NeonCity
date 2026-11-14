using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>Ngoại hình một NPC người (màu da / tóc / áo...). Hình được vẽ bằng code (xem TownArt.Human).</summary>
    public class Look
    {
        public string id;
        public int skin = 0xE8B890, hair = 0x2A1B14, top = 0x3355AA, accent = -1, pants = 0x222233, shoes = 0x111118;
        /// <summary>0 = đầu trọc, 1 = tóc ngắn, 2 = tóc dài.</summary>
        public int hairStyle = 1;
        public int hat = -1, beard = -1, tie = -1;
        public bool shades, robe;
        public Look(string id) { this.id = id; }
    }

    /// <summary>
    /// Toàn bộ hình ảnh của thị trấn (mặt tiền toà nhà, NPC, robot, đồ nội thất) được VẼ BẰNG CODE lúc chạy — không cần file ảnh, không đụng Scene.
    /// Mọi Sprite đều 16 px = 1 đơn vị, lọc Point, pivot giữa đáy (trừ khi ghi khác) và được lưu đệm.
    /// </summary>
    public static class TownArt
    {
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

        // ================================================================ NGƯỜI
        /// <summary>frame: 0 đứng, 1 / 2 hai nhịp bước. Kích thước 16x26 px (1 x 1,6 đơn vị).</summary>
        public static Sprite Human(Look l, int frame)
        {
            return Cached("h." + l.id + "." + frame, () => HumanCanvas(l, frame));
        }

        static PxCanvas HumanCanvas(Look l, int frame)
        {
            var c = new PxCanvas(16, 26);
            const int ox = 1, oy = 1;
            Color32 skin = K(l.skin), skinD = Sh(skin, 0.82f), hair = K(l.hair), top = K(l.top), topD = Sh(top, 0.72f), topL = Sh(top, 1.18f);
            Color32 pants = K(l.pants), pantsD = Sh(pants, 0.75f), shoes = K(l.shoes);
            Color32 acc = l.accent >= 0 ? K(l.accent) : topL;

            // chân / áo choàng dài
            if (l.robe)
            {
                c.Rect(ox + 3, oy + 7, 8, 15, top);
                c.Rect(ox + 2, oy + 16, 10, 6, top);
                c.Rect(ox + 10, oy + 7, 1, 15, topD);
                c.Rect(ox + 11, oy + 16, 1, 6, topD);
                c.Rect(ox + 7, oy + 8, 1, 14, topD);                     // nếp áo
                int sh = frame == 1 ? 1 : frame == 2 ? -1 : 0;
                c.Rect(ox + 4 + Mathf.Max(0, -sh), oy + 22, 3, 2, shoes);
                c.Rect(ox + 8 + Mathf.Max(0, sh), oy + 22, 3, 2, shoes);
            }
            else
            {
                int lx = ox + 4, rx = ox + 8, lLen = 7, rLen = 7;
                if (frame == 1) { lx -= 1; rx += 1; rLen = 6; }
                else if (frame == 2) { lx -= 1; rx += 1; lLen = 6; }
                c.Rect(lx, oy + 15, 3, lLen, pants);
                c.Rect(rx, oy + 15, 3, rLen, pants);
                c.Rect(lx + 2, oy + 15, 1, lLen, pantsD);
                c.Rect(rx + 2, oy + 15, 1, rLen, pantsD);
                c.Rect(lx, oy + 15 + lLen, 4, 2, shoes);
                c.Rect(rx, oy + 15 + rLen, 4, 2, shoes);
            }

            // thân + tay
            c.Rect(ox + 3, oy + 7, 8, 8, top);
            c.Rect(ox + 3, oy + 7, 1, 8, topL);
            c.Rect(ox + 10, oy + 7, 1, 8, topD);
            c.Rect(ox + 3, oy + 14, 8, 1, topD);
            if (l.accent >= 0 && !l.robe)
            {
                c.Rect(ox + 5, oy + 7, 4, 1, acc);                       // cổ áo
                c.Rect(ox + 3, oy + 11, 8, 1, acc);                      // sọc ngang
            }
            if (l.tie >= 0) { c.Rect(ox + 6, oy + 8, 2, 5, K(l.tie)); c.Rect(ox + 6, oy + 8, 2, 1, Sh(K(l.tie), 0.7f)); }
            int swing = frame == 1 ? 1 : frame == 2 ? -1 : 0;
            c.Rect(ox + 2, oy + 8 + (swing > 0 ? 1 : 0), 1, 6, topD);
            c.Rect(ox + 11, oy + 8 + (swing < 0 ? 1 : 0), 1, 6, topD);
            c.Rect(ox + 2, oy + 14 + (swing > 0 ? 1 : 0), 1, 1, skin);
            c.Rect(ox + 11, oy + 14 + (swing < 0 ? 1 : 0), 1, 1, skin);

            // đầu
            c.Rect(ox + 4, oy + 1, 6, 6, skin);
            c.Rect(ox + 9, oy + 1, 1, 6, skinD);
            if (l.hairStyle >= 1)
            {
                c.Rect(ox + 4, oy + 0, 6, 2, hair);
                c.Rect(ox + 4, oy + 2, 1, 2, hair);
                c.Rect(ox + 9, oy + 2, 1, 1, hair);
            }
            if (l.hairStyle == 2)
            {
                c.Rect(ox + 3, oy + 1, 1, 8, hair);
                c.Rect(ox + 10, oy + 1, 1, 8, hair);
            }
            if (l.hat >= 0)
            {
                Color32 hc = K(l.hat);
                c.Rect(ox + 4, oy + 0, 6, 2, hc);
                c.Rect(ox + 4, oy + 2, 7, 1, Sh(hc, 0.8f));              // vành mũ chìa ra phía trước
            }
            if (l.shades)
            {
                c.Rect(ox + 4, oy + 3, 6, 2, K(0x0C0C14));
                c.Set(ox + 5, oy + 3, K(0x5A6A8A));
                c.Set(ox + 8, oy + 3, K(0x5A6A8A));
            }
            else
            {
                c.Set(ox + 5, oy + 3, Ink);
                c.Set(ox + 8, oy + 3, Ink);
            }
            if (l.beard >= 0) c.Rect(ox + 4, oy + 5, 6, 2, K(l.beard));
            c.Outline(Ink);
            return c;
        }

        /// <summary>Người ngồi thiền (kiết già). 18x18 px.</summary>
        public static Sprite Sit(Look l)
        {
            return Cached("s." + l.id, () =>
            {
                var c = new PxCanvas(18, 18);
                Color32 skin = K(l.skin), skinD = Sh(skin, 0.82f), top = K(l.top), topD = Sh(top, 0.72f), topL = Sh(top, 1.15f), hair = K(l.hair);
                c.Rect(2, 14, 14, 3, topD);                              // chân xếp bằng tròn
                c.Rect(1, 15, 16, 2, top);
                c.Rect(3, 16, 2, 1, skin); c.Rect(13, 16, 2, 1, skin);   // bàn chân
                c.Rect(4, 7, 10, 8, top);
                c.Rect(4, 7, 1, 8, topL);
                c.Rect(13, 7, 1, 8, topD);
                c.Rect(7, 12, 4, 2, skin);                               // tay đặt trong lòng
                c.Rect(6, 1, 6, 6, skin);
                c.Rect(11, 1, 1, 6, skinD);
                if (l.hairStyle >= 1) c.Rect(6, 0, 6, 2, hair);
                if (l.beard >= 0) c.Rect(6, 5, 6, 2, K(l.beard));
                c.Rect(7, 3, 1, 1, Ink); c.Rect(10, 3, 1, 1, Ink);       // nhắm mắt
                c.Outline(Ink);
                return c;
            });
        }

        // ================================================================ ROBOT
        /// <summary>kind: 0 chiến đấu, 1 gia dụng, 2 giáo dục. 18x26 px.</summary>
        public static Sprite Robot(int kind, int eye)
        {
            return Cached("r." + kind + "." + eye, () => RobotCanvas(kind, eye));
        }

        public static PxCanvas RobotCanvas(int kind, int eye)
        {
            var c = new PxCanvas(18, 26);
            int body = kind == 0 ? 0x6A2A38 : kind == 1 ? 0x2F7F86 : 0xB89A2C;
            Color32 b = K(body), bD = Sh(b, 0.7f), bL = Sh(b, 1.2f), e = K(eye), metal = K(0x8C93A8), metalD = K(0x565C70);
            c.Rect(8, 0, 2, 2, metalD);                                  // ăng-ten
            c.Rect(5, 2, 8, 6, metal);
            c.Rect(5, 2, 1, 6, Sh(metal, 1.2f));
            c.Rect(12, 2, 1, 6, metalD);
            c.Rect(6, 4, 6, 2, K(0x10131E));                             // kính mắt
            c.Rect(7, 4, 1, 2, e); c.Rect(10, 4, 1, 2, e);
            c.Rect(3, 9, 12, 9, b);
            c.Rect(3, 9, 1, 9, bL);
            c.Rect(14, 9, 1, 9, bD);
            c.Rect(8, 11, 2, 2, e);                                      // đèn ngực
            c.Rect(1, 9, 2, 8, metalD);
            c.Rect(15, 9, 2, 8, metalD);
            c.Rect(1, 17, 2, 2, metal); c.Rect(15, 17, 2, 2, metal);
            if (kind == 0) { c.Rect(0, 8, 4, 2, bL); c.Rect(14, 8, 4, 2, bL); c.Set(0, 7, bL); c.Set(17, 7, bL); c.Rect(15, 14, 3, 3, K(0x2A2A36)); }
            else if (kind == 1) { c.Rect(5, 14, 8, 4, K(0xE8EEF2)); c.Rect(5, 14, 8, 1, K(0xB8C4CC)); }
            else { c.Rect(5, 14, 7, 4, K(0x2E4FB0)); c.Rect(5, 14, 7, 1, K(0x7FA0FF)); c.Rect(8, 14, 1, 4, K(0xF2EAD0)); }
            c.Rect(5, 19, 3, 5, metalD); c.Rect(10, 19, 3, 5, metalD);
            c.Rect(4, 24, 4, 2, metal); c.Rect(10, 24, 4, 2, metal);
            c.Outline(Ink);
            return c;
        }

        // ================================================================ NỘI THẤT / ĐỒ VẬT
        public static Sprite Counter(int wpx, int top, int body)
        {
            return Cached("ctr." + wpx + "." + top + "." + body, () =>
            {
                var c = new PxCanvas(wpx, 22);
                Color32 t = K(top), bd = K(body);
                c.Rect(0, 0, wpx, 4, t);
                c.Rect(0, 0, wpx, 1, Sh(t, 1.25f));
                c.Rect(0, 4, wpx, 18, bd);
                c.VGrad(1, 5, wpx - 2, 15, Sh(bd, 1.1f), Sh(bd, 0.7f));
                for (int x = 6; x < wpx - 4; x += 12) c.Rect(x, 8, 8, 10, Sh(bd, 0.6f));
                c.Rect(0, 21, wpx, 1, Ink);
                c.Outline(Ink);
                return c;
            });
        }

        /// <summary>Giá hàng nhiều ngăn có đồ màu mè. 16 px = 1 đơn vị.</summary>
        public static Sprite Shelf(int wpx, int hpx, int seed, int frameColor)
        {
            return Cached("shf." + wpx + "." + hpx + "." + seed + "." + frameColor, () =>
            {
                var c = new PxCanvas(wpx, hpx);
                Color32 f = K(frameColor);
                c.Rect(0, 0, wpx, hpx, Sh(f, 0.55f));
                c.Frame(0, 0, wpx, hpx, f);
                var rnd = new System.Random(seed);
                int rows = Mathf.Max(2, hpx / 18);
                int[] cols = { 0xFF4D6D, 0x35E0FF, 0xFFD23F, 0x7BFF6A, 0xC77DFF, 0xFF9A3C };
                for (int r = 0; r < rows; r++)
                {
                    int y0 = 2 + r * (hpx - 2) / rows, y1 = 2 + (r + 1) * (hpx - 2) / rows - 2;
                    c.Rect(1, y1 + 1, wpx - 2, 1, f);
                    int x = 3;
                    while (x < wpx - 6)
                    {
                        int bw = 3 + rnd.Next(5), bh = 4 + rnd.Next(Mathf.Max(1, y1 - y0 - 4));
                        bh = Mathf.Min(bh, y1 - y0);
                        var cc = K(cols[rnd.Next(cols.Length)]);
                        c.Rect(x, y1 - bh + 1, bw, bh, cc);
                        c.Rect(x, y1 - bh + 1, bw, 1, Sh(cc, 1.3f));
                        x += bw + 1 + rnd.Next(3);
                    }
                }
                return c;
            });
        }

        /// <summary>Cầu thang tượng trưng: khối bậc thang + mũi tên. up = đi lên (bậc cao dần về bên phải).</summary>
        public static Sprite Stairs(bool up, int body, int arrow)
        {
            return Cached("stairs." + up + "." + body + "." + arrow, () =>
            {
                var c = new PxCanvas(56, 80);
                Color32 b = K(body), bl = Sh(b, 1.25f), bd = Sh(b, 0.7f), ar = K(arrow);
                int steps = 7;
                for (int i = 0; i < steps; i++)
                {
                    int sx = i * 8, hgt = 8 + i * 9;
                    int x0 = up ? sx : 55 - sx - 7;
                    c.Rect(x0, 80 - hgt, 8, hgt, i % 2 == 0 ? b : bd);
                    c.Rect(x0, 80 - hgt, 8, 2, bl);
                }
                // tay vịn
                int ax0 = up ? 0 : 55, ax1 = up ? 55 : 0;
                c.Line(ax0, 66, ax1, 6, K(0xC8D2E6));
                c.Line(ax0, 67, ax1, 7, K(0x7C88A8));
                // mũi tên
                int cx = 28, cy = up ? 14 : 20;
                if (up) { c.Line(cx, cy - 8, cx - 7, cy, ar); c.Line(cx, cy - 8, cx + 7, cy, ar); c.Line(cx, cy - 8, cx, cy + 10, ar); c.Line(cx + 1, cy - 8, cx + 1, cy + 10, ar); }
                else { c.Line(cx, cy + 8, cx - 7, cy, ar); c.Line(cx, cy + 8, cx + 7, cy, ar); c.Line(cx, cy - 10, cx, cy + 8, ar); c.Line(cx + 1, cy - 10, cx + 1, cy + 8, ar); }
                c.Outline(Ink);
                return c;
            });
        }

        /// <summary>Khung cửa ra vào (w x h px) với ô sáng.</summary>
        public static Sprite DoorFrame(int wpx, int hpx, int frame, int glass)
        {
            return Cached("door." + wpx + "." + hpx + "." + frame + "." + glass, () =>
            {
                var c = new PxCanvas(wpx, hpx);
                Color32 f = K(frame), g = K(glass);
                c.Rect(0, 0, wpx, hpx, f);
                c.VGrad(3, 3, wpx - 6, hpx - 3, Sh(g, 1.25f), Sh(g, 0.55f));
                c.Rect(wpx / 2, 3, 1, hpx - 3, Sh(f, 0.8f));
                c.Rect(wpx / 2 - 5, hpx / 2, 2, 8, K(0xE8E8F0));
                c.Rect(wpx / 2 + 4, hpx / 2, 2, 8, K(0xE8E8F0));
                c.Outline(Ink);
                return c;
            });
        }

        public static Sprite Pedestal(int wpx, int topColor)
        {
            return Cached("ped." + wpx + "." + topColor, () =>
            {
                var c = new PxCanvas(wpx, 10);
                Color32 t = K(topColor);
                c.Rect(0, 0, wpx, 2, t);
                c.Rect(1, 2, wpx - 2, 8, K(0x2A2E44));
                c.Rect(1, 2, 1, 8, K(0x3C4262));
                c.Rect(2, 9, wpx - 4, 1, K(0x14162A));
                c.Outline(Ink);
                return c;
            });
        }

        /// <summary>Bảng giá nhỏ: số + ký hiệu xu vàng.</summary>
        public static Sprite PriceTag(int price, int col)
        {
            return Cached("price." + price + "." + col, () =>
            {
                string s = price.ToString();
                int w = PxCanvas.TextWidth(s) + 12;
                var c = new PxCanvas(w, 11);
                c.Rect(0, 0, w, 11, K(0x0B0D1A));
                c.Frame(0, 0, w, 11, K(col));
                c.Text(3, 3, s, K(0xF2F4FF));
                c.Disc(w - 5, 5, 2, K(0xFFD23F));
                return c;
            });
        }

        public static Sprite SignPlate(string text, int scale, int back, int fore, int border)
        {
            return Cached("plate." + text + "." + scale + "." + back + "." + fore + "." + border, () =>
            {
                int w = PxCanvas.TextWidth(text, scale) + 6 * scale, h = 5 * scale + 6 * scale;
                var c = new PxCanvas(w, h);
                c.Rect(0, 0, w, h, K(back));
                c.Frame(0, 0, w, h, K(border));
                c.Frame(1, 1, w - 2, h - 2, Sh(K(border), 0.55f));
                c.Text(3 * scale, 3 * scale + scale / 2, text, K(fore), scale);
                return c;
            }, 0.5f);
        }

        public static Sprite Plant(int leaf)
        {
            return Cached("plant." + leaf, () =>
            {
                var c = new PxCanvas(14, 24);
                Color32 l = K(leaf), ld = Sh(l, 0.65f);
                c.Rect(4, 16, 6, 8, K(0x5A3A2A)); c.Rect(4, 16, 6, 1, K(0x7A5240));
                c.Line(7, 16, 3, 5, ld); c.Line(7, 16, 11, 4, ld); c.Line(7, 16, 7, 1, ld);
                c.Disc(3, 5, 2, l); c.Disc(11, 4, 2, l); c.Disc(7, 2, 2, l); c.Disc(5, 9, 2, ld); c.Disc(9, 9, 2, ld);
                c.Outline(Ink);
                return c;
            });
        }

        public static Sprite Bench(int wpx, int wood)
        {
            return Cached("bench." + wpx + "." + wood, () =>
            {
                var c = new PxCanvas(wpx, 12);
                Color32 w = K(wood);
                c.Rect(0, 4, wpx, 3, w); c.Rect(0, 4, wpx, 1, Sh(w, 1.25f));
                c.Rect(2, 7, 2, 5, Sh(w, 0.6f)); c.Rect(wpx - 4, 7, 2, 5, Sh(w, 0.6f));
                c.Rect(0, 0, wpx, 3, Sh(w, 0.85f));
                c.Outline(Ink);
                return c;
            });
        }

        public static Sprite Lantern(int col)
        {
            return Cached("lant." + col, () =>
            {
                var c = new PxCanvas(9, 14);
                Color32 k = K(col);
                c.Rect(4, 0, 1, 3, K(0x2A2A2A));
                c.Rect(1, 3, 7, 8, k); c.Rect(2, 4, 5, 6, Sh(k, 1.35f)); c.Rect(1, 3, 7, 1, Sh(k, 0.6f)); c.Rect(1, 10, 7, 1, Sh(k, 0.6f));
                c.Rect(3, 11, 3, 3, K(0xE8B84A));
                c.Outline(Ink);
                return c;
            }, 0.5f);
        }

        public static Sprite Cushion(int col)
        {
            return Cached("cush." + col, () =>
            {
                var c = new PxCanvas(20, 5);
                Color32 k = K(col);
                c.Rect(1, 1, 18, 4, k); c.Rect(1, 1, 18, 1, Sh(k, 1.3f)); c.Rect(0, 2, 20, 2, k);
                c.Outline(Ink);
                return c;
            });
        }

        public static Sprite Crate(int col)
        {
            return Cached("crate." + col, () =>
            {
                var c = new PxCanvas(16, 16);
                Color32 k = K(col);
                c.Rect(0, 0, 16, 16, k); c.Frame(0, 0, 16, 16, Sh(k, 0.6f)); c.Line(0, 0, 15, 15, Sh(k, 0.6f)); c.Line(15, 0, 0, 15, Sh(k, 0.6f));
                c.Outline(Ink);
                return c;
            });
        }

        // ================================================================ MẶT TIỀN TOÀ NHÀ
        public const int FacadeCenter = 0, FacadeRobo = 1, FacadeZen = 2, FacadeDes = 3, FacadeArena = 4;

        /// <summary>Kích thước (px) mặt tiền: x = rộng, y = cao.</summary>
        public static Vector2Int FacadeSize(int id)
        {
            switch (id)
            {
                case FacadeCenter: return new Vector2Int(224, 176);
                case FacadeRobo: return new Vector2Int(288, 208);
                case FacadeZen: return new Vector2Int(256, 152);
                case FacadeDes: return new Vector2Int(176, 208);
                default: return new Vector2Int(320, 176);
            }
        }

        public static Sprite Facade(int id)
        {
            return Cached("facade." + id, () =>
            {
                switch (id)
                {
                    case FacadeCenter: return FacadeCenterShop();
                    case FacadeRobo: return FacadeRoboStore();
                    case FacadeZen: return FacadeZenMonastery();
                    case FacadeDes: return FacadeDesTown();
                    default: return FacadeArenaCyber();
                }
            });
        }

        static void Windows(PxCanvas c, int x, int y, int cols, int rows, int ww, int wh, int gx, int gy, Color32 lit, Color32 dark, int seed, float litProb)
        {
            var rnd = new System.Random(seed);
            for (int r = 0; r < rows; r++)
                for (int k = 0; k < cols; k++)
                {
                    int px = x + k * (ww + gx), py = y + r * (wh + gy);
                    bool on = rnd.NextDouble() < litProb;
                    c.Rect(px, py, ww, wh, on ? lit : dark);
                    if (on) c.Rect(px, py, ww, 1, Sh(lit, 1.2f));
                    c.Frame(px - 1, py - 1, ww + 2, wh + 2, Ink);
                }
        }

        static PxCanvas FacadeCenterShop()
        {
            var c = new PxCanvas(224, 176);
            Color32 wall = K(0xCFD6E2), wallD = K(0xA9B2C4), teal = K(0x14606B), cyan = K(0x35E8FF), navy = K(0x0B1630);
            c.VGrad(0, 8, 224, 168, Sh(wall, 1.08f), wallD);
            c.Rect(0, 0, 224, 8, teal);
            c.Rect(0, 7, 224, 1, cyan);
            c.Rect(0, 8, 6, 168, wallD); c.Rect(218, 8, 6, 168, wallD);
            // bảng hiệu
            c.Rect(46, 12, 132, 22, navy);
            c.Frame(46, 12, 132, 22, cyan);
            c.Frame(48, 14, 128, 18, Sh(cyan, 0.5f));
            c.TextC(112, 18, "CENTERSHOP", cyan, 3);
            // tầng 2: cửa sổ rèm
            Windows(c, 21, 44, 5, 1, 30, 36, 8, 0, K(0xF6E7B0), K(0x2A3550), 7, 0.8f);
            for (int k = 0; k < 5; k++)
                for (int j = 0; j < 36; j += 4) c.Rect(21 + k * 38, 44 + j, 30, 1, K(0xC9B77A));
            c.Rect(0, 90, 224, 8, teal);
            c.Rect(0, 90, 224, 1, cyan);
            c.Rect(0, 97, 224, 1, Sh(cyan, 0.5f));
            // tầng 1: 2 tủ kính + cửa kính ở giữa
            for (int s = 0; s < 2; s++)
            {
                int x0 = s == 0 ? 14 : 142;
                c.Rect(x0, 110, 68, 56, navy);
                c.Frame(x0, 110, 68, 56, Ink);
                c.Frame(x0 + 1, 111, 66, 54, Sh(cyan, 0.4f));
                c.Blit(ShelfCanvas(64, 50, 3 + s), x0 + 2, 112);
            }
            c.Rect(90, 118, 44, 58, K(0x0F1424));
            c.Rect(92, 120, 40, 56, teal);
            c.VGrad(94, 122, 17, 54, K(0x7FE8F0), K(0x1B6A78));
            c.VGrad(113, 122, 17, 54, K(0x7FE8F0), K(0x1B6A78));
            c.Rect(110, 122, 4, 54, K(0x0F1424));
            c.Rect(108, 146, 2, 10, K(0xE8F0FF)); c.Rect(114, 146, 2, 10, K(0xE8F0FF));
            for (int x = 86; x < 138; x += 6) c.Rect(x, 108, 3, 8, ((x / 6) % 2 == 0) ? cyan : K(0xF2FFFF));   // mái hiên sọc
            c.Rect(86, 116, 52, 2, Sh(cyan, 0.5f));
            c.Rect(0, 172, 224, 4, K(0x4A5068));
            c.Outline(Ink);
            return c;
        }

        static PxCanvas ShelfCanvas(int w, int h, int seed)
        {
            var c = new PxCanvas(w, h);
            var rnd = new System.Random(seed);
            int[] cols = { 0xFF4D6D, 0x35E0FF, 0xFFD23F, 0x7BFF6A, 0xC77DFF, 0xFF9A3C };
            for (int r = 0; r < 3; r++)
            {
                int y1 = (r + 1) * h / 3 - 3;
                c.Rect(0, y1 + 1, w, 1, K(0x5A6280));
                int x = 2;
                while (x < w - 6)
                {
                    int bw = 4 + rnd.Next(6), bh = 5 + rnd.Next(7);
                    var cc = K(cols[rnd.Next(cols.Length)]);
                    c.Rect(x, y1 - bh + 1, bw, bh, cc);
                    c.Rect(x, y1 - bh + 1, bw, 1, Sh(cc, 1.3f));
                    x += bw + 2;
                }
            }
            return c;
        }

        static PxCanvas FacadeRoboStore()
        {
            var c = new PxCanvas(288, 208);
            Color32 steel = K(0x2B2F45), steelL = K(0x3D4363), mag = K(0xFF2FB5), cyan = K(0x35E8FF), navy = K(0x0A0C1C);
            c.VGrad(0, 8, 288, 200, steelL, steel);
            c.Rect(0, 0, 288, 8, K(0x1A1D30));
            c.Rect(0, 7, 288, 1, mag);
            c.Rect(214, 0, 3, 8, K(0x565C70)); c.Disc(222, 2, 3, K(0x8C93A8));          // đĩa ăng-ten
            // biển hiệu
            c.Rect(80, 10, 128, 24, navy);
            c.Frame(80, 10, 128, 24, mag);
            c.Frame(82, 12, 124, 20, Sh(mag, 0.5f));
            c.TextC(144, 18, "ROBOSTORE", cyan, 3);
            // tầng 3 + 2: cửa sổ có áp phích robot
            for (int fl = 0; fl < 2; fl++)
            {
                int y0 = 40 + fl * 50;
                for (int k = 0; k < 3; k++)
                {
                    int x0 = 24 + k * 88;
                    c.Rect(x0, y0, 64, 42, K(0x14182E));
                    c.Frame(x0 - 1, y0 - 1, 66, 44, Ink);
                    c.Frame(x0, y0, 64, 42, mag);
                    c.VGrad(x0 + 2, y0 + 2, 60, 38, K(0x23294A), K(0x0F1226));
                    var rb = RobotCanvas((k + fl) % 3, k == 1 ? 0x35E8FF : 0xFF2FB5);
                    c.Blit(rb, x0 + 22, y0 + 2 + (38 - 26) / 2 - 1, 1);
                }
                c.Rect(0, y0 + 43, 288, 4, K(0x1A1D30));
                c.Rect(0, y0 + 43, 288, 1, Sh(cyan, 0.6f));
            }
            // tầng 1: 2 robot đứng gác hai bên cửa
            c.Rect(0, 142, 288, 66, steel);
            c.VGrad(0, 142, 288, 66, K(0x343A58), K(0x232740));
            for (int s = 0; s < 2; s++)
            {
                int x0 = s == 0 ? 36 : 214;
                c.Rect(x0 - 6, 146, 52, 62, K(0x11142A));
                c.Frame(x0 - 6, 146, 52, 62, mag);
                c.Blit(RobotCanvas(s == 0 ? 0 : 2, 0xFF2FB5), x0 + 2, 154, 2);
            }
            c.Rect(112, 148, 64, 60, K(0x0B0D1A));
            c.Frame(112, 148, 64, 60, cyan);
            c.VGrad(116, 152, 27, 56, K(0x5DD6E6), K(0x16505E));
            c.VGrad(145, 152, 27, 56, K(0x5DD6E6), K(0x16505E));
            c.Rect(143, 152, 2, 56, K(0x0B0D1A));
            c.Rect(100, 140, 88, 6, mag); c.Rect(100, 140, 88, 1, K(0xFF9CE0));
            c.Rect(0, 204, 288, 4, K(0x4A5068));
            c.Outline(Ink);
            return c;
        }

        static PxCanvas FacadeZenMonastery()
        {
            var c = new PxCanvas(256, 152);
            Color32 wood = K(0x7A4A2B), woodD = K(0x5A3520), red = K(0xB02020), redD = K(0x7A1414), tile = K(0x2C3A38), tileL = K(0x3F5450), paper = K(0xEFE6C8);
            // thân chính
            c.VGrad(8, 86, 240, 66, wood, woodD);
            for (int x = 8; x < 248; x += 16) c.Rect(x, 86, 1, 66, woodD);
            // cột đỏ
            foreach (int px in new[] { 10, 56, 96, 152, 192, 238 }) { c.Rect(px, 86, 8, 66, red); c.Rect(px, 86, 2, 66, Sh(red, 1.2f)); c.Rect(px + 6, 86, 2, 66, redD); }
            // cửa lùa giấy
            c.Rect(104, 98, 48, 54, K(0x3A2515));
            for (int s = 0; s < 2; s++)
            {
                int x0 = 106 + s * 23;
                c.Rect(x0, 100, 21, 52, paper);
                for (int j = 0; j < 52; j += 8) c.Rect(x0, 100 + j, 21, 1, K(0x8A6A3A));
                for (int i = 0; i < 21; i += 7) c.Rect(x0 + i, 100, 1, 52, K(0x8A6A3A));
            }
            // cửa sổ lưới 2 bên
            foreach (int wx in new[] { 24, 168 })
            {
                c.Rect(wx, 100, 32, 30, K(0x2A1A0E));
                c.Rect(wx + 2, 102, 28, 26, paper);
                for (int i = 0; i < 28; i += 7) c.Rect(wx + 2 + i, 102, 1, 26, K(0x8A6A3A));
                for (int j = 0; j < 26; j += 6) c.Rect(wx + 2, 102 + j, 28, 1, K(0x8A6A3A));
            }
            // mái tầng 1
            Roof(c, 0, 62, 256, 26, tile, tileL);
            // thân tầng 2 nhỏ hơn + bảng hiệu
            c.Rect(52, 42, 152, 22, wood);
            for (int x = 52; x < 204; x += 16) c.Rect(x, 42, 1, 22, woodD);
            foreach (int px in new[] { 54, 118, 192 }) c.Rect(px, 42, 8, 22, red);
            c.Rect(80, 46, 96, 15, K(0x1A100A));
            c.Frame(80, 46, 96, 15, K(0xE8B84A));
            c.TextC(128, 49, "ZENMONASTERY", K(0xF2D27A), 2);
            // mái tầng 2
            Roof(c, 36, 22, 184, 22, tile, tileL);
            // chóp
            c.Rect(124, 8, 8, 14, tile);
            c.Rect(126, 0, 4, 10, K(0xE8B84A));
            // đèn lồng
            c.Blit(LanternCanvas(0xD83A2A), 26, 90); c.Blit(LanternCanvas(0xD83A2A), 222, 90);
            c.Rect(0, 148, 256, 4, K(0x4A4034));
            c.Outline(Ink);
            return c;
        }

        static PxCanvas LanternCanvas(int col)
        {
            var c = new PxCanvas(9, 14);
            Color32 k = K(col);
            c.Rect(4, 0, 1, 3, K(0x2A2A2A));
            c.Rect(1, 3, 7, 8, k); c.Rect(2, 4, 5, 6, Sh(k, 1.35f));
            c.Rect(3, 11, 3, 3, K(0xE8B84A));
            return c;
        }

        static void Roof(PxCanvas c, int x, int y, int w, int h, Color32 tile, Color32 tileL)
        {
            for (int j = 0; j < h; j++)
            {
                int inset = (h - 1 - j) / 3;                         // mái xoè rộng dần xuống dưới
                int x0 = x + inset, x1 = x + w - inset;
                c.Rect(x0, y + j, x1 - x0, 1, (j % 4 == 0) ? tileL : tile);
            }
            for (int k = 0; k < 4; k++)                                   // góc mái cong vút
            {
                c.Set(x + k, y + h - 1 - k / 2 - 1, tileL); c.Set(x + w - 1 - k, y + h - 1 - k / 2 - 1, tileL);
            }
            c.Rect(x, y + h - 2, w, 2, K(0x1C2624));
        }

        static PxCanvas FacadeDesTown()
        {
            var c = new PxCanvas(176, 208);
            Color32 blk = K(0x0C0C12), blk2 = K(0x16161F), red = K(0xFF1A3C), redD = K(0x7A0C1E);
            c.VGrad(0, 10, 176, 198, blk2, blk);
            for (int x = 22; x < 160; x += 22) c.Rect(x, 10, 1, 198, K(0x1E1E2A));
            // đỉnh răng cưa
            for (int x = 0; x < 176; x += 16) c.Rect(x, 0, 12, 10, blk);
            for (int x = 0; x < 176; x += 16) c.Rect(x, 0, 12, 1, redD);
            // dải neon đỏ dọc
            c.Rect(6, 10, 3, 198, red); c.Rect(167, 10, 3, 198, red);
            c.Rect(5, 10, 1, 198, redD); c.Rect(9, 10, 1, 198, redD);
            c.Rect(166, 10, 1, 198, redD); c.Rect(170, 10, 1, 198, redD);
            // biển hiệu
            c.Rect(34, 20, 108, 22, K(0x050509));
            c.Frame(34, 20, 108, 22, red);
            c.TextC(88, 26, "DESTOWN4", red, 3);
            // cửa sổ khe hẹp
            var rnd = new System.Random(11);
            for (int r = 0; r < 6; r++)
                for (int k = 0; k < 5; k++)
                {
                    int x0 = 22 + k * 28, y0 = 54 + r * 24;
                    bool on = rnd.NextDouble() < 0.3;
                    c.Rect(x0, y0, 14, 8, on ? K(0xC8102E) : K(0x1C1C28));
                    c.Frame(x0 - 1, y0 - 1, 16, 10, Ink);
                }
            // cửa lớn
            c.Rect(60, 148, 56, 60, K(0x050509));
            c.Frame(60, 148, 56, 60, red);
            c.Frame(62, 150, 52, 58, redD);
            c.Rect(87, 150, 2, 58, redD);
            c.Rect(84, 176, 2, 8, red); c.Rect(90, 176, 2, 8, red);
            c.Rect(52, 144, 72, 4, red);
            c.Rect(0, 204, 176, 4, K(0x2A2A34));
            c.Outline(Ink);
            return c;
        }

        static PxCanvas FacadeArenaCyber()
        {
            var c = new PxCanvas(320, 176);
            Color32 sand = K(0xC9A878), sandD = K(0x9C7E54), sandL = K(0xE2C898);
            Color32 dark = K(0x14122B), mag = K(0xFF2FB5), cyan = K(0x35E8FF);
            // nửa trái: đấu trường La Mã (2 tầng vòm)
            c.VGrad(0, 40, 160, 136, sandL, sandD);
            c.Rect(0, 36, 160, 6, sandD); c.Rect(0, 36, 160, 1, sandL);
            for (int tier = 0; tier < 2; tier++)
            {
                int y0 = 58 + tier * 58, hh = 46;
                for (int k = 0; k < 6; k++)
                {
                    int x0 = 6 + k * 25;
                    c.Rect(x0, y0 + 10, 16, hh - 10, K(0x2A1E14));
                    for (int i = 0; i < 16; i++)
                    {
                        float t = (i - 7.5f) / 8f;
                        int top = y0 + 10 - Mathf.RoundToInt(Mathf.Sqrt(Mathf.Max(0f, 1f - t * t)) * 9f);
                        c.Rect(x0 + i, top, 1, y0 + 10 - top, K(0x2A1E14));
                    }
                    c.Rect(x0 - 3, y0 + 2, 2, hh, sandD);
                }
                c.Rect(0, y0 + hh, 160, 3, sandD);
            }
            // nửa phải: neon
            c.VGrad(160, 40, 160, 136, K(0x1E1A44), dark);
            for (int k = 0; k < 5; k++)
            {
                int x0 = 170 + k * 29;
                c.Rect(x0, 56, 20, 100, K(0x0C0B1E));
                c.Frame(x0, 56, 20, 100, k % 2 == 0 ? cyan : mag);
                for (int j = 0; j < 100; j += 10) c.Rect(x0 + 2, 60 + j, 16, 2, k % 2 == 0 ? Sh(cyan, 0.6f) : Sh(mag, 0.6f));
            }
            c.Rect(160, 36, 160, 4, cyan);
            c.Rect(159, 36, 2, 140, K(0xFFFFFF));
            // biển hiệu
            c.Rect(94, 6, 132, 24, K(0x07061A));
            c.Frame(94, 6, 132, 24, mag);
            c.Frame(96, 8, 128, 20, Sh(mag, 0.5f));
            c.TextC(160, 14, "ARENACYBER", cyan, 3);
            // cổng vòm lớn ở giữa
            c.Rect(136, 118, 48, 58, K(0x07061A));
            for (int i = 0; i < 48; i++)
            {
                float t = (i - 23.5f) / 24f;
                int top = 118 - Mathf.RoundToInt(Mathf.Sqrt(Mathf.Max(0f, 1f - t * t)) * 22f);
                c.Rect(136 + i, top, 1, 118 - top, K(0x07061A));
                c.Set(136 + i, top, i < 24 ? sandL : cyan);
                c.Set(136 + i, top - 1, i < 24 ? sandL : cyan);
            }
            c.Rect(136, 118, 2, 58, sandL); c.Rect(182, 118, 2, 58, cyan);
            c.Rect(0, 172, 320, 4, K(0x4A4034));
            c.Outline(Ink);
            return c;
        }

        // ================================================================ NHÀ ĐỔ NÁT (Rìa phố)
        public static Sprite Ruin(int seed)
        {
            return Cached("ruin." + seed, () => RuinCanvas(seed));
        }

        static PxCanvas RuinCanvas(int seed)
        {
            var rnd = new System.Random(seed * 131 + 7);
            int w = 96 + rnd.Next(5) * 8, h = 120 + rnd.Next(6) * 14;
            var c = new PxCanvas(w, h);
            Color32 a = K(0x3C3844), b = K(0x25222D);
            var top = new int[w];
            int cur = 6 + rnd.Next(14);
            for (int x = 0; x < w; x++)
            {
                if (x % 7 == 0) cur = Mathf.Clamp(cur + rnd.Next(-9, 10), 2, 44);
                top[x] = cur;
                for (int y = cur; y < h; y++)
                {
                    float t = (y - cur) / (float)Mathf.Max(1, h - cur);
                    c.Set(x, y, PxCanvas.Mix(a, b, t));
                }
            }
            for (int x = 0; x < w; x += 9) c.Rect(x, top[x] + 2, 1, h - top[x] - 2, Sh(b, 0.9f));          // khe nứt dọc
            for (int r = 0; r < 6; r++)
                for (int k = 0; k < 5; k++)
                {
                    int wx = 8 + k * 18 + rnd.Next(3), wy = 24 + r * 26;
                    if (wx + 10 >= w || wy + 14 >= h - 8 || wy < top[wx] + 8) continue;
                    c.Rect(wx, wy, 10, 14, K(0x0C0B12));
                    if (rnd.NextDouble() < 0.25) c.Rect(wx + 2, wy + 9, 6, 3, K(0x6A1A56));            // ánh neon lọt qua
                    c.Frame(wx - 1, wy - 1, 12, 16, K(0x1A1822));
                }
            for (int k = 0; k < 6; k++)                                                                 // thép chờ trơ ra
            {
                int x = 4 + rnd.Next(Mathf.Max(1, w - 8));
                c.Rect(x, Mathf.Max(0, top[x] - 5 - rnd.Next(6)), 1, 7, K(0x6A4A3A));
            }
            c.Rect(0, h - 3, w, 3, K(0x1C1A22));
            c.Outline(Ink);
            return c;
        }
    }
}
