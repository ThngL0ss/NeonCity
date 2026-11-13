using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Biểu tượng pixel-art (16 x 16) của từng khu vực trên bản đồ, vẽ thẳng bằng ô vuông IMGUI (không cần file ảnh):
    /// Căn cứ = mặt Albert, Phố Neon = các toà nhà thành phố, Rìa Phố Neon = mặt kẻ đeo mặt nạ, Khu thử nghiệm Dyber = mắt đỏ của máy.
    /// </summary>
    public static class MapIcons
    {
        static readonly string[] Albert =
        {
            "......KKKK......",
            "....KKWWWWKK....",
            "...KWWWWWWWWK...",
            "..KWWGWWWWGWWK..",
            "..KWWSSSSSSWWK..",
            "..KWSSSSSSSSWK..",
            "..KWSSSSSSSSWK..",
            "..KWKBBKKBBKWK..",
            "..KWBCCBBCCBWK..",
            "..KWKBBKKBBKWK..",
            "..KWSSSssSSSWK..",
            "..KWWWWWWWWWWK..",
            "..KGWWWWWWWWGK..",
            "...KWWWWWWWWK...",
            "....KWWWWWWK....",
            ".....KKKKKK.....",
        };

        static readonly string[] City =
        {
            "................",
            ".......KKKK.....",
            ".......KNNK.....",
            "..KKK..KDDK.....",
            "..KDK..KDNKKKK..",
            "..KNK..KDDKDDK..",
            "..KDKKKKDNKDMK..",
            "..KDDKDDDDKDDK..",
            "..KNDKDNDDKDDK..",
            ".KKDDKDDDMKDNKK.",
            ".KDDNKDDDDKDDDK.",
            ".KDDDKDNDDKDMDK.",
            ".KDMDKDDDDKDDDK.",
            ".KDDDKDDDNKDDDK.",
            ".KKKKKKKKKKKKKK.",
            "..VVVVVVVVVVVV..",
        };

        static readonly string[] Raider =
        {
            "................",
            "....KKKKKKKK....",
            "...KDDDDDDDDK...",
            "..KDDTDDDDTDDK..",
            "..KDDDDDDDDDDK..",
            ".KDDKKKKKKKKDDK.",
            ".KDKRRKKKKRRKDK.",
            ".KDKRRKKKKRRKDK.",
            ".KDDKKKKKKKKDDK.",
            ".KDDDKTKKTKDDDK.",
            "..KDDKTTTTKDDK..",
            "..KDDKKTTKKDDK..",
            "...KDDKKKKDDK...",
            "...KDTDDDDTDK...",
            "..KDDDDDDDDDDK..",
            "..KKKKKKKKKKKK..",
        };

        static readonly string[] Dyber =
        {
            "................",
            "...KKKKKKKKKK...",
            "..KDDDDDDDDDDK..",
            ".KDTTTTTTTTTTDK.",
            ".KDTddddddddTDK.",
            ".KDTdKKKKKKdTDK.",
            ".KDTdKRRRRKdTDK.",
            ".KDTdKRLLRKdTDK.",
            ".KDTdKRRRRKdTDK.",
            ".KDTdKKKKKKdTDK.",
            ".KDTddddddddTDK.",
            ".KDTTTTTTTTTTDK.",
            "..KDDDDDDDDDDK..",
            "..KDOKDDDDKODK..",
            "...KKKKKKKKKK...",
            "................",
        };

        static Color32 Pal(char c)
        {
            switch (c)
            {
                case 'K': return new Color32(18, 14, 28, 255);
                case 'W': return new Color32(238, 238, 246, 255);
                case 'G': return new Color32(168, 170, 190, 255);
                case 'S': return new Color32(236, 188, 144, 255);
                case 's': return new Color32(196, 142, 106, 255);
                case 'B': return new Color32(44, 44, 70, 255);
                case 'C': return new Color32(130, 232, 255, 255);
                case 'E': return new Color32(24, 20, 34, 255);
                case 'L': return new Color32(252, 252, 255, 255);
                case 'Y': return new Color32(250, 200, 70, 255);
                case 'M': return new Color32(255, 60, 200, 255);
                case 'N': return new Color32(40, 230, 255, 255);
                case 'D': return new Color32(58, 60, 78, 255);
                case 'd': return new Color32(34, 36, 50, 255);
                case 'R': return new Color32(255, 50, 60, 255);
                case 'r': return new Color32(150, 20, 40, 255);
                case 'O': return new Color32(255, 150, 40, 255);
                case 'T': return new Color32(104, 108, 132, 255);
                case 'U': return new Color32(30, 60, 120, 255);
                case 'P': return new Color32(120, 60, 200, 255);
                case 'g': return new Color32(80, 86, 110, 255);
                case 'V': return new Color32(20, 24, 40, 255);
            }
            return new Color32(255, 0, 255, 255);
        }

        static string[] GridOf(string placeId)
        {
            if (placeId == GameFlow.BaseScene) return Albert;
            if (placeId == Places.Neon) return City;
            if (placeId == Places.Rim) return Raider;
            if (placeId == Places.Dyber) return Dyber;
            return Dyber;
        }

        /// <summary>Vẽ biểu tượng của địa điểm placeId, tâm (cx, cy) theo toạ độ tham chiếu 1280 x 720, mỗi ô cell đơn vị. dim = làm mờ / xám (chưa mở).</summary>
        public static void Draw(Hud h, string placeId, float cx, float cy, float cell, float alpha, bool dim)
        {
            var g = GridOf(placeId);
            float x0 = cx - 8f * cell, y0 = cy - 8f * cell;
            for (int r = 0; r < g.Length; r++)
            {
                string row = g[r];
                int q = 0;
                while (q < row.Length)
                {
                    char ch = row[q];
                    int e = q;
                    while (e + 1 < row.Length && row[e + 1] == ch) e++;
                    if (ch != '.')
                    {
                        Color c = Pal(ch);
                        if (dim) { float v = (c.r + c.g + c.b) / 3f * 0.55f; c = new Color(v, v, v * 1.05f, 1f); }
                        c.a = alpha;
                        h.Fill(h.R(x0 + q * cell, y0 + r * cell, (e - q + 1) * cell, cell), c);
                    }
                    q = e + 1;
                }
            }
        }
    }
}
