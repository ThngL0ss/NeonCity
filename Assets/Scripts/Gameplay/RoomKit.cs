using UnityEngine;

namespace NeonBreaker
{
    /// <summary>Các món nội thất dựng sẵn cho Room (cửa, quầy, cầu thang, cây, ghế, giá hàng...). Toạ độ lx = trong phòng.</summary>
    public static class RoomKit
    {
        /// <summary>Cửa ra ở lx: hình khung cửa + điểm bấm F gọi use.</summary>
        public static void ExitDoor(Room r, float lx, string vi, string en, System.Action use, int frame = 0x2A3550, int glass = 0x35E8FF)
        {
            r.Put("Door", TownArt.DoorFrame(40, 56, frame, glass), lx, 0f, -30);
            r.Glow(lx, 1.8f, 4.5f, 4.5f, Cfg.A(ToColor(glass), 0.25f), -29);
            r.Spot(lx, 0f, vi, en, use, 1.6f);
        }

        public static void StairsSpot(Room r, float lx, bool up, string vi, string en, System.Action use, int body = 0x5A6A8A, int arrow = 0x35E8FF)
        {
            r.Put(up ? "StairsUp" : "StairsDown", TownArt.Stairs(up, body, arrow), lx, 0f, -30);
            r.Spot(lx, 0f, vi, en, use, 2.2f);
        }

        /// <summary>Quầy: sprite Counter rộng wpx pixel (16 px = 1 đơn vị), vẽ ĐÈ lên NPC đứng sau quầy.</summary>
        public static void Counter(Room r, float lx, int wpx, int top = 0xCFD6E2, int body = 0x1F6F7B)
        {
            r.Put("Counter", TownArt.Counter(wpx, top, body), lx, 0f, 15);
        }

        public static void Plant(Room r, float lx, int leaf = 0x3FBF6A) { r.Put("Plant", TownArt.Plant(leaf), lx, 0f, -25); }
        public static void Bench(Room r, float lx, int wpx = 48, int wood = 0x7A4A2B) { r.Put("Bench", TownArt.Bench(wpx, wood), lx, 0f, -24); }
        public static void Shelf(Room r, float lx, int wpx, int hpx, int seed, int frame = 0x565C70) { r.Put("Shelf", TownArt.Shelf(wpx, hpx, seed, frame), lx, 0.1f, -33); }

        /// <summary>Biển treo tường (chữ in hoa).</summary>
        public static void Plate(Room r, float lx, float y, string text, int scale, int back, int fore, int border)
        {
            var sr = r.Put("Plate", TownArt.SignPlate(text, scale, back, fore, border), lx, y, -28);
            sr.transform.localPosition = new Vector3(lx, y, 0f);
        }

        public static void WindowBand(Room r, float x0, float x1, float y, float w, float h, Color glass)
        {
            for (float x = x0; x + w <= x1; x += w + 1.2f)
            {
                r.Quad("WinFrame", x - 0.1f, y - 0.1f, w + 0.2f, h + 0.2f, new Color(0.04f, 0.05f, 0.1f), -36);
                r.Quad("Win", x, y, w, h, glass, -35);
                r.Quad("WinGlint", x, y + h * 0.55f, w, 0.12f, Cfg.A(Color.white, 0.18f), -34);
            }
        }

        public static Color ToColor(int rgb) { return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f); }

        /// <summary>Lời thoại 1 câu cho NPC (tiện viết).</summary>
        public static DlgLine Say(string who, Transform npc, Color c, string vi, string en)
        {
            return new DlgLine(who, Cfg.L(vi, en), c, npc);
        }
    }
}
