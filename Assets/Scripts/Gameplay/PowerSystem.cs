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
    /// CHƯA áp dụng vào chỉ số chiến đấu của người chơi / quái - hiện chỉ lưu dữ liệu.
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
