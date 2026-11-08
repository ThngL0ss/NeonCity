using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Hình phạt khi chết rồi được Albert hồi sinh ở căn cứ:
    ///   • MẤT hết vật phẩm trên người (kho đồ + trang bị đang gắn) và linh kiện đang mang;
    ///   • MẤT hết minibot mang theo (bảng chọn / đang triệu hồi);
    ///   • Cấp độ −LevelLoss (10), cấp bậc −RankLoss (1 bậc);
    ///   • GIỮ: đồ cất trong kho căn cứ, minibot trong lưu trữ ở căn cứ, tiến độ cây skill, xu, dầu, chỉ số vĩnh viễn từ vật phẩm đã dùng.
    /// GameFlow.GoToBase(died: true) gọi Apply() rồi tự lưu game.
    /// </summary>
    public static class DeathPenalty
    {
        public const int LevelLoss = 10;
        public const int RankLoss = 1;

        public struct Loss
        {
            public int items, parts, bots;
            public Rank rankFrom, rankTo;
            public int levelFrom, levelTo;
            public bool Any { get { return items > 0 || parts > 0 || bots > 0 || rankFrom != rankTo || levelFrom != levelTo; } }
        }

        /// <summary>Kết quả lần phạt gần nhất (BaseManager đọc để báo cho người chơi rồi gọi Clear()).</summary>
        public static Loss Last;
        public static bool Pending;
        /// <summary>Thời gian còn lại (giây) của bảng "đã mất gì" ở căn cứ (Hud vẽ, GameManager đếm ngược).</summary>
        public static float ReportT;
        public const float ReportLen = 10f;

        public static void Clear() { Last = new Loss(); Pending = false; ReportT = 0f; }

        /// <summary>Căn cứ gọi sau khi hồi sinh xong: bắt đầu hiện bảng báo mất mát (nếu có).</summary>
        public static void ShowReport()
        {
            if (!Pending) return;
            Pending = false;
            ReportT = ReportLen;
        }

        public static Loss Apply()
        {
            var l = new Loss();
            l.items = Inventory.ClearCarried();
            l.parts = SkillSystem.ClearParts();
            l.bots = SkillSystem.DropFieldBots();

            l.rankFrom = PlayerProgress.PlayerRank;
            l.levelFrom = PlayerProgress.PlayerLevel;
            int newRank = Mathf.Max(0, (int)l.rankFrom - RankLoss);
            int newLevel = Mathf.Max(PowerRules.MinLevel, l.levelFrom - LevelLoss);
            l.rankTo = (Rank)newRank;
            l.levelTo = newLevel;
            PlayerProgress.Set(l.rankTo, l.levelTo);

            Last = l;
            Pending = l.Any;
            return l;
        }

        /// <summary>Dòng tóm tắt những gì đã mất (rỗng nếu không mất gì).</summary>
        public static string Summary(Loss l)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (l.items > 0) parts.Add(l.items + Cfg.L(" vật phẩm", " items"));
            if (l.parts > 0) parts.Add(l.parts + Cfg.L(" linh kiện", " parts"));
            if (l.bots > 0) parts.Add(l.bots + " minibot");
            if (l.levelFrom != l.levelTo) parts.Add(Cfg.L("cấp độ ", "level ") + l.levelFrom + " → " + l.levelTo);
            if (l.rankFrom != l.rankTo) parts.Add(Cfg.L("cấp bậc ", "rank ") + l.rankFrom + " → " + l.rankTo);
            return string.Join("  ·  ", parts.ToArray());
        }
    }
}
