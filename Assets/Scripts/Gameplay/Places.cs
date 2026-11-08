using UnityEngine;

namespace NeonBreaker
{
    /// <summary>Một địa điểm trên bản đồ (và là một Scene). id trùng tên Scene.</summary>
    public class PlaceDef
    {
        public string id;                    // = tên Scene
        public string nameVi, nameEn;
        public string descVi, descEn;
        public int skulls;                   // độ nguy hiểm 1..5 (đầu lâu)
        public Vector2 map;                  // vị trí biểu tượng trên bản đồ (0..1, gốc trái-trên)
        public Color color = Cfg.Cyan;
        public bool unlockedAtStart;         // false = chỉ mở sau khi đã hoàn thành màn hướng dẫn (xem Unlocked)
        public Rect[] zones;                 // vùng tô trên bản đồ (toạ độ 0..1 trong khung bản đồ; nhiều hình chữ nhật ghép lại, ví dụ hình chữ L). null = chỉ vẽ biểu tượng
        public Vector2[] outline;            // đường viền vùng (các điểm 0..1, nối vòng kín)

        public string Name { get { return Cfg.L(nameVi, nameEn); } }
        public string Desc { get { return Cfg.L(descVi, descEn); } }
    }

    /// <summary>
    /// Danh sách địa điểm của game. Muốn thêm địa điểm mới: thêm một PlaceDef vào All (và tạo Scene trùng tên, thêm vào Build Settings).
    /// Mô tả khớp docs/STORY.md. Chưa nhắc tên phe nào trong lời thoại — chỉ mô tả bối cảnh chung.
    /// </summary>
    public static class Places
    {
        public const string Dyber = "Main";                 // khu thử nghiệm của Dyber (Scene Main)
        public const string Neon = "PhoNeon";
        public const string Rim = "RiaPhoNeon";

        public static readonly PlaceDef[] All =
        {
            new PlaceDef
            {
                id = Neon, skulls = 2, map = new Vector2(0.50f, 0.50f), color = Cfg.Magenta, unlockedAtStart = true,
                nameVi = "Phố Neon", nameEn = "Neon Street",
                descVi = "Trung tâm công nghệ, nơi nhiều thế lực cùng để mắt tới. Nhà cao tầng, đèn neon rực rỡ, người và robot qua lại tấp nập — không ai gây sự. Nhặt vật phẩm rải rác trên đường.",
                descEn = "The tech heart of the city, watched over by many powers. Tall towers, glaring neon, crowds of people and robots — nobody picks a fight. Scattered items lie along the street.",
            },
            new PlaceDef
            {
                id = Rim, skulls = 4, map = new Vector2(0.19f, 0.62f), color = Cfg.Orange, unlockedAtStart = true,
                // vùng hình chữ L ôm lấy Phố Neon từ rìa Tây tới rìa Nam: cánh dọc ở phía Tây, cánh ngang ở phía Nam
                zones = new[] { new Rect(0.06f, 0.26f, 0.22f, 0.68f), new Rect(0.28f, 0.74f, 0.30f, 0.20f) },
                outline = new[] { new Vector2(0.06f, 0.26f), new Vector2(0.28f, 0.26f), new Vector2(0.28f, 0.74f), new Vector2(0.58f, 0.74f), new Vector2(0.58f, 0.94f), new Vector2(0.06f, 0.94f) },
                nameVi = "Rìa Phố Neon", nameEn = "Neon Street Outskirts",
                descVi = "Vùng trũng sát Phố Neon, nơi chứa rác và khí thải của khu trên. Ổ của bọn cướp và lính đánh thuê. Tội phạm, dịch bệnh — rất nguy hiểm. Rác công nghệ chất đống, ruồi nhặng bay đầy.",
                descEn = "The lowland beside Neon Street, where the upper city dumps its garbage and exhaust. A den of bandits and hired guns. Crime and disease — very dangerous. Heaps of tech junk and swarms of flies.",
            },
            new PlaceDef
            {
                id = Dyber, skulls = 5, map = new Vector2(0.86f, 0.26f), color = Cfg.Red, unlockedAtStart = true,
                nameVi = "Khu thử nghiệm Dyber", nameEn = "Dyber Test Zone",
                descVi = "Nơi cất giữ lõi bị mất của bạn. Trụ triệu hồi dày đặc, canh giữ bởi OVERLORD.",
                descEn = "Where your stolen core is kept. Dense summon towers, guarded by OVERLORD.",
            },
        };

        public static PlaceDef Get(string id)
        {
            for (int i = 0; i < All.Length; i++) if (All[i].id == id) return All[i];
            return null;
        }

        public static bool Unlocked(PlaceDef p)
        {
            return p != null && (p.unlockedAtStart || GameFlow.IsPlaceUnlocked(p.id));
        }

        /// <summary>Tên hiển thị của một nơi lưu (Base, Tutorial hoặc địa điểm trên bản đồ).</summary>
        public static string NameOf(string id)
        {
            if (id == GameFlow.BaseScene) return Cfg.L("Căn cứ Albert", "Albert's Base");
            if (id == GameFlow.TutorialScene) return Cfg.L("Huấn luyện", "Training");
            var p = Get(id);
            return p != null ? p.Name : id;
        }
    }
}
