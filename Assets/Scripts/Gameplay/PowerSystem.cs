using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Cấp bậc sức mạnh, từ thấp nhất F đến cao nhất SSS.
    /// Dùng chung cho người chơi và quái. Minibot giữ nguyên cấp bậc của quái gốc và KHÔNG thể tăng cấp bậc.
    /// </summary>
    public enum Rank { F = 0, D = 1, C = 2, B = 3, A = 4, S = 5, SS = 6, SSS = 7 }

    /// <summary>
    /// Cấp bậc + cấp độ (1..200) của một nhân vật. Gắn vào Player và Enemy (Inspector: "Power").
    /// QUÁI: cấp độ làm quái khoẻ hơn (máu, sát thương, tốc độ, xu rơi - xem PowerRules.HpMul...), cấp bậc quyết định loại linh kiện rơi ra.
    /// NGƯỜI CHƠI: chưa áp dụng vào chỉ số chiến đấu.
    /// Với minibot thì cấp độ ảnh hưởng máu và sát thương (xem SkillConfig).
    /// </summary>
    [System.Serializable]
    public class PowerInfo
    {
        [Tooltip("Cấp bậc: F (thấp nhất), D, C, B, A, S, SS, SSS (cao nhất)")] public Rank rank = Rank.F;
        [Tooltip("Cấp độ từ 1 đến 200")] [Range(PowerRules.MinLevel, PowerRules.MaxLevel)] public int level = 1;

        public PowerInfo() { }

        public PowerInfo(Rank r, int lv)
        {
            rank = r;
            level = PowerRules.ClampLevel(lv);
        }

        public string Label { get { return rank + "  Lv." + PowerRules.ClampLevel(level); } }
    }

    /// <summary>Quy tắc chung của hệ thống sức mạnh. Các hàm hệ số còn để trống (= 1) cho đến khi bạn quyết định cân bằng.</summary>
    public static class PowerRules
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 200;

        public static int ClampLevel(int level) { return Mathf.Clamp(level, MinLevel, MaxLevel); }

        public static string RankName(Rank r) { return r.ToString(); }

        /// <summary>Hệ số sức mạnh theo cấp bậc. Tạm thời = 1 (chưa áp dụng); chỉnh ở đây khi muốn cấp bậc ảnh hưởng chỉ số.</summary>
        public static float RankMul(Rank r) { return 1f; }

        // ------------------------------------------------------------ độ mạnh theo CẤP ĐỘ của quái (cấp 1 = chỉ số gốc ghi trên Prefab)
        // Mỗi cấp chỉ chênh nhau vài % nên cấp 5, 7, 10 không khác nhau quá xa. Muốn quái khoẻ lên nhanh hơn: tăng các số này.
        /// <summary>Máu thêm mỗi cấp (0.05 = +5% máu gốc mỗi cấp: cấp 5 = x1.20, cấp 10 = x1.45, cấp 200 = x10.95).</summary>
        public static float HpPerLevel = 0.05f;
        /// <summary>Sát thương thêm mỗi cấp (cấp 5 = x1.12, cấp 10 = x1.27).</summary>
        public static float DamagePerLevel = 0.03f;
        /// <summary>Tốc độ thêm mỗi cấp (cấp 10 = x1.05). Thời gian báo hiệu đòn không đổi nên vẫn công bằng.</summary>
        public static float SpeedPerLevel = 0.006f;
        /// <summary>Xu rơi thêm mỗi cấp.</summary>
        public static float RewardPerLevel = 0.04f;

        public static float HpMul(int level) { return 1f + (ClampLevel(level) - 1) * HpPerLevel; }
        public static float DamageMul(int level) { return 1f + (ClampLevel(level) - 1) * DamagePerLevel; }
        public static float SpeedMul(int level) { return 1f + (ClampLevel(level) - 1) * SpeedPerLevel; }
        public static float RewardMul(int level) { return 1f + (ClampLevel(level) - 1) * RewardPerLevel; }

        // ------------------------------------------------------------ linh kiện theo cấp bậc
        /// <summary>Cấp bậc cao nhất của LINH KIỆN (S). Quái SS và SSS không rơi linh kiện.</summary>
        public const Rank MaxPartRank = Rank.S;

        /// <summary>Quái cấp bậc này có rơi linh kiện không (F..S có, SS và SSS không).</summary>
        public static bool DropsParts(Rank r) { return r <= MaxPartRank; }

        /// <summary>Cấp bậc linh kiện nâng lên khi ghép. Chỉ ghép được F->D, D->C, C->B (không ghép lên A và S). Trả về false nếu không ghép được.</summary>
        public static bool FusionTarget(Rank from, out Rank to)
        {
            to = from;
            if (from == Rank.F) { to = Rank.D; return true; }
            if (from == Rank.D) { to = Rank.C; return true; }
            if (from == Rank.C) { to = Rank.B; return true; }
            return false;
        }

        public const int PartRankCount = 6;     // F, D, C, B, A, S

        public static Color RankColor(Rank r)
        {
            switch (r)
            {
                case Rank.F: return new Color(0.62f, 0.66f, 0.72f, 1f);
                case Rank.D: return new Color(0.45f, 1.00f, 0.40f, 1f);
                case Rank.C: return new Color(0.10f, 0.95f, 1.00f, 1f);
                case Rank.B: return new Color(0.35f, 0.55f, 1.00f, 1f);
                case Rank.A: return new Color(0.65f, 0.35f, 1.00f, 1f);
                case Rank.S: return new Color(1.00f, 0.15f, 0.80f, 1f);
                case Rank.SS: return new Color(1.00f, 0.60f, 0.10f, 1f);
                default: return new Color(1.00f, 0.88f, 0.25f, 1f);
            }
        }
    }
}
