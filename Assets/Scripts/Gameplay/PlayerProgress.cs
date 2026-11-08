using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Cấp bậc + cấp độ của người chơi, giữ xuyên các Scene (Player là object mới ở mỗi Scene nên không tự nhớ được).
    /// Lần đầu (ván mới): lấy đúng giá trị bạn đặt ở Player ▸ Power trong Prefab. Sau đó mọi Scene dùng giá trị đã nhớ ở đây.
    /// Bị trừ khi chết (xem DeathPenalty) và được lưu vào file lưu game.
    /// </summary>
    public static class PlayerProgress
    {
        public static bool Has;
        public static Rank PlayerRank = Rank.F;
        public static int PlayerLevel = 1;

        public static void Reset()
        {
            Has = false;
            PlayerRank = Rank.F;
            PlayerLevel = 1;
        }

        /// <summary>Player.Awake gọi: chưa có dữ liệu thì lấy từ Prefab, có rồi thì áp dụng lên Player.</summary>
        public static void Sync(Player p)
        {
            if (p == null) return;
            if (p.power == null) p.power = new PowerInfo();
            if (!Has)
            {
                PlayerRank = p.power.rank;
                PlayerLevel = PowerRules.ClampLevel(p.power.level);
                Has = true;
            }
            else
            {
                p.power.rank = PlayerRank;
                p.power.level = PlayerLevel;
            }
        }

        /// <summary>Đặt cấp bậc + cấp độ (nạp file lưu, bị phạt khi chết...). Áp dụng luôn lên Player đang có trong Scene.</summary>
        public static void Set(Rank r, int level)
        {
            Has = true;
            PlayerRank = r;
            PlayerLevel = PowerRules.ClampLevel(level);
            var p = Player.Instance;
            if (p != null && p.power != null)
            {
                p.power.rank = PlayerRank;
                p.power.level = PlayerLevel;
            }
        }
    }
}
