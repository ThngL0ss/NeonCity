using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>Một nhiệm vụ Albert giao. Tiến độ tính từ các bộ đếm tích luỹ trong BaseMissions.</summary>
    public class MissionDef
    {
        public string titleVi, titleEn, descVi, descEn;
        public int target;
        public System.Func<int> progress;
        public int rewardCoins, rewardOil;

        public string Title { get { return Cfg.L(titleVi, titleEn); } }
        public string Desc { get { return Cfg.L(descVi, descEn); } }
        public int Progress { get { return Mathf.Min(target, progress()); } }
        public bool Done { get { return progress() >= target; } }
    }

    /// <summary>
    /// Nhiệm vụ nối tiếp do Albert giao (nói chuyện bấm F). Hoàn thành thì quay lại nói chuyện để nhận thưởng, rồi sang nhiệm vụ kế.
    /// Muốn thêm / sửa nhiệm vụ: chỉnh danh sách trong Build().
    /// </summary>
    public static class BaseMissions
    {
        public static int Crafted, TotalKills, TotalTowers, BossKills;
        /// <summary>Albert đã kể cốt truyện (lần nói chuyện đầu tiên) chưa.</summary>
        public static bool StoryTold;
        /// <summary>Nhiệm vụ hiện tại (>= Count nghĩa là đã xong hết).</summary>
        public static int Index;
        static List<MissionDef> list;

        public static void ResetAll()
        {
            Crafted = TotalKills = TotalTowers = BossKills = 0;
            StoryTold = false;
            Index = 0;
            list = null;
        }

        /// <summary>Cộng thống kê ván vừa chơi vào bộ đếm tích luỹ (gọi khi chuyển cảnh).</summary>
        public static void BankRun()
        {
            TotalKills += GameManager.Kills;
            TotalTowers += GameManager.TowersDestroyed;
            GameManager.Kills = 0;
            GameManager.TowersDestroyed = 0;
        }

        public static List<MissionDef> All
        {
            get
            {
                if (list == null) list = Build();
                return list;
            }
        }

        static List<MissionDef> Build()
        {
            var l = new List<MissionDef>();
            l.Add(new MissionDef
            {
                titleVi = "Khởi động", titleEn = "Warm-up",
                descVi = "Hạ 12 con quái ngoài kia để cơ thể mới làm quen với chiến đấu.", descEn = "Take down 12 enemies out there so your new body gets used to combat.",
                target = 12, progress = () => TotalKills + GameManager.Kills, rewardCoins = 40, rewardOil = 6,
            });
            l.Add(new MissionDef
            {
                titleVi = "Thu hoạch linh kiện", titleEn = "Parts Harvest",
                descVi = "Phá 2 trụ triệu hồi. Quái bậc D / C rơi linh kiện — mang về ghép ở máy ghép của ta.", descEn = "Destroy 2 summon towers. D / C rank enemies drop parts — bring them back for my fusion machine.",
                target = 2, progress = () => TotalTowers + GameManager.TowersDestroyed, rewardCoins = 60, rewardOil = 10,
            });
            l.Add(new MissionDef
            {
                titleVi = "Bàn tay của kỹ sư", titleEn = "Engineer's Touch",
                descVi = "Ghép 1 lần ở máy ghép linh kiện (3 mảnh cùng bậc + dầu + xu).", descEn = "Fuse parts once at the fusion machine (3 parts of one rank + oil + coins).",
                target = 1, progress = () => PartFusion.FusedTotal, rewardCoins = 50, rewardOil = 8,
            });
            l.Add(new MissionDef
            {
                titleVi = "Thợ chế tác", titleEn = "Crafter",
                descVi = "Chế tác 2 vật phẩm ở bàn chế tác (nguyên liệu nhặt trên đường hoặc trong rương).", descEn = "Craft 2 items at the crafting bench (materials lie on the road or in chests).",
                target = 2, progress = () => Crafted, rewardCoins = 80, rewardOil = 12,
            });
            l.Add(new MissionDef
            {
                titleVi = "Phá hết trụ", titleEn = "Clear the Towers",
                descVi = "Phá hủy cả 6 trụ triệu hồi để mở cổng khu boss.", descEn = "Destroy all 6 summon towers to open the boss gate.",
                target = 6, progress = () => TotalTowers + GameManager.TowersDestroyed, rewardCoins = 120, rewardOil = 20,
            });
            l.Add(new MissionDef
            {
                titleVi = "Lấy lại thứ thuộc về cậu", titleEn = "Take It Back",
                descVi = "Hạ OVERLORD và đoạt lại " + CineScripts.StolenItemVi + " (bậc SS).", descEn = "Defeat OVERLORD and take back the " + CineScripts.StolenItemEn + " (SS rank).",
                target = 1, progress = () => BossKills, rewardCoins = 300, rewardOil = 50,
            });
            return l;
        }

        public static MissionDef Current { get { return Index < All.Count ? All[Index] : null; } }

        /// <summary>Nhận thưởng nhiệm vụ hiện tại nếu đã xong. Trả về nhiệm vụ vừa nhận (null nếu không).</summary>
        public static MissionDef TryClaim()
        {
            var m = Current;
            if (m == null || !m.Done) return null;
            if (m.rewardCoins > 0) Inventory.AddCoins(m.rewardCoins, false);
            if (m.rewardOil > 0) Inventory.AddOil(m.rewardOil, true);
            Index++;
            Sfx.Play("victory", 0.6f, 1.2f);
            return m;
        }
    }
}
