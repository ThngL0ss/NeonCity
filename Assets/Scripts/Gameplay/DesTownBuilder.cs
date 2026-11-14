using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// DesTown4 (một trong 7 trụ sở chính của phe Diệt chủng): toà nhà đen cao, KHÔNG vào được. Hai người áo đen đeo kính râm chặn cửa.
    ///  • Chưa có thẻ đấu sĩ (xem ArenaBuilder): "!!!" → "Không phận sự miễn vào!" rồi đóng hội thoại;
    ///  • Có thẻ: "Ồ, hóa ra cậu là 1 trong các đấu sĩ." → hỏi muốn gì: "Đưa hộp đồ" (chỉ khi đang làm nhiệm vụ phụ của Shengu và có hộp đen) / "Không có gì".
    ///    Đưa hộp: lấy hộp đen khỏi kho, Town.Shengu = ShenguDelivered (quay lại ZenMonastery gặp Shengu để nhận lõi thật).
    /// </summary>
    public static class DesTownBuilder
    {
        public const string Id = "DesTown4";
        static readonly Color Red = new Color(0.95f, 0.22f, 0.32f);

        static string Guard { get { return Cfg.L("NGƯỜI ÁO ĐEN", "MAN IN BLACK"); } }

        /// <summary>Gọi từ StreetBuildings.Place: dựng hai vệ sĩ hai bên cửa (x = tâm toà nhà).</summary>
        public static void Place(Transform building, float x)
        {
            float[] dx = { -2.6f, 2.6f };
            for (int i = 0; i < dx.Length; i++)
            {
                TownNpc n = null;
                n = TownNpc.Spawn(building, TownLooks.BlackSuit, x + dx[i], TownNpc.Mode.Stand, Guard, () => Talk(n), -1e9f, -1e9f, i == 1);
            }
        }

        static void Talk(TownNpc n)
        {
            Transform who = n != null ? n.transform : null;
            var l = new List<DlgLine>();
            if (!QuestItems.Has(QuestItems.FighterCard))
            {
                l.Add(new DlgLine(Guard, "!!!", Red, who));
                l.Add(new DlgLine(Guard, Cfg.L("Không phận sự miễn vào!", "No entry for outsiders!"), Red, who));
                DialogueUi.Begin(l, null);
                return;
            }
            bool canDeliver = Town.Shengu == Town.ShenguQuest && QuestItems.Has(QuestItems.BlackBox);
            l.Add(new DlgLine(Guard, Cfg.L("Ồ, hóa ra cậu là 1 trong các đấu sĩ.", "Oh, so you're one of the fighters."), Red, who));
            string[] opts = canDeliver
                ? new[] { Cfg.L("Đưa hộp đồ", "Hand over the box"), Cfg.L("Không có gì", "Nothing") }
                : new[] { Cfg.L("Không có gì", "Nothing") };
            l.Add(DlgLine.Ask(Guard, Cfg.L("Được rồi, cậu muốn gì?", "All right, what do you want?"), Red, who, opts, i =>
            {
                if (canDeliver && i == 0) Deliver(who);
                else DialogueUi.Close();
            }));
            DialogueUi.Begin(l, null);
        }

        static void Deliver(Transform who)
        {
            var d = new DlgLine(Guard, Cfg.L("…… Được rồi, để đó. Bọn tao sẽ chuyển nó đi.", "…… All right, leave it there. We'll pass it on."), Red, who);
            d.onShow = () =>
            {
                QuestItems.Take(QuestItems.BlackBox);
                Town.Shengu = Town.ShenguDelivered;
                GameManager.Banner(Cfg.L("NHIỆM VỤ PHỤ: Quay lại gặp Shengu ở ZenMonastery", "SIDE QUEST: Return to Shengu at ZenMonastery"), Cfg.Magenta, 3f);
            };
            DialogueUi.Continue(new List<DlgLine> { d }, null);
        }
    }
}
