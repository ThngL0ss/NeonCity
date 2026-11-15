using UnityEngine;

namespace NeonBreaker
{
    public enum MercSkill { None, Charge, Kaiz, Connie, Furra }

    /// <summary>Một lính đánh thuê (Furra cho thuê ở phòng kín của MerShop). Các mô tả "vừa / trâu / nhanh..." đổi thành số ở MercDefs.</summary>
    public class MercDef
    {
        public string id;
        public int level;
        public Rank rank;
        public bool ranged, both;               // both = vừa đánh gần vừa đánh xa (Furra)
        public float hpMul, dmgBase, speedFrac; // máu = 100 × hpMul × HpMul(level); dame = dmgBase × DamageMul(level); tốc độ = speedFrac × tốc độ chạy của người chơi
        public string hpVi, hpEn, dmgVi, dmgEn, spdVi, spdEn;
        public string skillVi = "", skillEn = "";
        public int coins, oil, partsB, partsA;
        public MercSkill skill;
        public Look look;

        public float MaxHp { get { return 100f * hpMul * PowerRules.HpMul(level); } }
        public float Damage { get { return dmgBase * PowerRules.DamageMul(level); } }
        public string TypeText { get { return both ? Cfg.L("Gần + xa", "Melee + ranged") : ranged ? Cfg.L("Đánh xa", "Ranged") : Cfg.L("Cận chiến", "Melee"); } }
        public string HpText { get { return Cfg.L(hpVi, hpEn); } }
        public string DmgText { get { return Cfg.L(dmgVi, dmgEn); } }
        public string SpdText { get { return Cfg.L(spdVi, spdEn); } }
        public string SkillText { get { return Cfg.L(skillVi, skillEn); } }
    }

    /// <summary>Danh sách lính đánh thuê (thứ tự = bit trong Town.MercOwned).</summary>
    public static class MercDefs
    {
        public const int Furra = 9;

        public static readonly MercDef[] All =
        {
            D("War-01", 15, Rank.B, false, 1.0f, "vừa", "average", 14f, "vừa", "average", 0.45f, "khá chậm", "fairly slow", 300, look: 0),
            D("War-02", 18, Rank.B, false, 1.0f, "vừa", "average", 14f, "vừa", "average", 0.62f, "bình thường", "normal", 350, look: 1),
            D("War-03", 18, Rank.B, true, 0.8f, "khá yếu", "fairly weak", 20f, "khá to", "fairly big", 0.62f, "bình thường", "normal", 360, look: 2),
            D("War-04", 25, Rank.B, false, 1.8f, "trâu", "tanky", 18f, "khá mạnh", "fairly strong", 0.62f, "bình thường", "normal", 500, look: 3),
            D("War-05", 30, Rank.B, false, 1.8f, "trâu", "tanky", 18f, "khá mạnh", "fairly strong", 0.78f, "khá nhanh", "fairly fast", 750, look: 4),
            D("War-06", 36, Rank.B, false, 1.4f, "khá trâu", "fairly tanky", 23f, "mạnh", "strong", 0.92f, "nhanh", "fast", 900, look: 5),
            D("War-07", 43, Rank.A, false, 1.8f, "trâu", "tanky", 23f, "mạnh", "strong", 0.78f, "khá nhanh", "fairly fast", 1300, look: 6, skill: MercSkill.Charge,
              skillVi: "Lao đi trong vài mét, đẩy enemy theo và gây choáng, sát thương liên tục. Tới cuối đường đập mạnh xuống đất hất tung enemy đang bị đẩy. Lúc dùng skill không thể bị khống chế, làm chậm.",
              skillEn: "Charges forward a few metres, shoving enemies along, stunning and damaging them continuously. At the end slams the ground and launches them. Immune to control and slows while charging."),
            D("Kaiz", 60, Rank.A, false, 2.6f, "rất trâu", "very tanky", 23f, "mạnh", "strong", 0.78f, "khá nhanh", "fairly fast", 1500, oil: 100, kaiz: true,
              skillVi: "Lao đến phía người chơi, hồi 20% máu đã mất cho người chơi và tạo giáp bằng 50% số máu đã mất của bản thân. Khi lao tới, đẩy lùi toàn bộ enemy quanh người chơi và giải trừ hiệu ứng khống chế cho người chơi.",
              skillEn: "Rushes to the player, heals 20% of the player's missing health and gains a shield equal to 50% of his own missing health. Knocks back every enemy around the player and removes the player's control effects."),
            D("Connie", 60, Rank.A, true, 1.8f, "trâu", "tanky", 32f, "rất mạnh", "very strong", 0.92f, "nhanh", "fast", 1500, partsB: 50, connie: true,
              skillVi: "Gọi 2 máy bay thả bom đi từ 2 hướng ngược nhau (bom mạnh gấp 5 lần sát thương cơ bản, vùng nổ to hơn 3 lần vụ nổ của đòn đánh thường). Đòn đánh là súng pháo sát thương lớn, diện rộng. Không ảnh hưởng đồng minh.",
              skillEn: "Calls 2 bomber planes from opposite sides (bombs hit 5x base damage with an explosion 3x larger than a normal shot). Normal attack is a cannon with big area damage. Never hurts allies."),
            D("Furra", 100, Rank.S, false, 4.0f, "cực kỳ trâu", "extremely tanky", 32f, "rất mạnh", "very strong", 1.1f, "rất nhanh", "very fast", 5000, oil: 500, partsB: 80, partsA: 20, furra: true,
              skillVi: "[1] Kết liễu mọi kẻ thù từ bậc B trở xuống, giảm 50% máu và sát thương kẻ thù bậc A trong 7 giây, làm choáng kẻ thù bậc S trở lên trong 3 giây. [2] Điên cuồng 15 giây: tăng mạnh mọi chỉ số, hồi 5% máu mỗi giây (máu thừa thành giáp); hạ hoặc hỗ trợ hạ 1 mục tiêu thì đặt lại thời gian. [3] Một lần khi hết máu: triệu hồi tất cả lính đánh thuê trong shop (Kaiz và Connie chỉ xuất hiện dùng skill rồi biến mất), hồi 15% máu.",
              skillEn: "[1] Instantly kills every enemy of rank B and below, halves health and damage of rank-A enemies for 7s, stuns rank-S and above for 3s. [2] Frenzy for 15s: big boost to every stat, regenerates 5% health per second (overheal becomes shield); downing or assisting a target resets the timer. [3] Once, when his health runs out: summons every mercenary in the shop (Kaiz and Connie only appear to use their skill, then vanish) and recovers 15% health."),
        };

        static MercDef D(string id, int lv, Rank rank, bool ranged, float hpMul, string hpVi, string hpEn, float dmg, string dmgVi, string dmgEn, float spd, string spdVi, string spdEn, int coins,
                         int look = -1, int oil = 0, int partsB = 0, int partsA = 0, MercSkill skill = MercSkill.None, string skillVi = "", string skillEn = "",
                         bool kaiz = false, bool connie = false, bool furra = false)
        {
            var d = new MercDef
            {
                id = id, level = lv, rank = rank, ranged = ranged, hpMul = hpMul, hpVi = hpVi, hpEn = hpEn, dmgBase = dmg, dmgVi = dmgVi, dmgEn = dmgEn,
                speedFrac = spd, spdVi = spdVi, spdEn = spdEn, coins = coins, oil = oil, partsB = partsB, partsA = partsA, skill = skill, skillVi = skillVi, skillEn = skillEn
            };
            if (kaiz) { d.skill = MercSkill.Kaiz; d.look = RimLooks.Kaiz; }
            else if (connie) { d.skill = MercSkill.Connie; d.look = RimLooks.Connie; }
            else if (furra) { d.skill = MercSkill.Furra; d.look = RimLooks.Furra; d.both = true; }
            else d.look = RimLooks.War(look);
            return d;
        }

        public static bool Owned(int i) { return i >= 0 && i < All.Length && (Town.MercOwned & (1 << i)) != 0; }

        public static bool CanAfford(MercDef d)
        {
            return Inventory.Coins >= d.coins && Inventory.Oil >= d.oil && SkillSystem.Parts(Rank.B) >= d.partsB && SkillSystem.Parts(Rank.A) >= d.partsA;
        }

        /// <summary>Thuê (trả tiền / dầu / linh kiện). false nếu không đủ.</summary>
        public static bool Hire(int i)
        {
            if (i < 0 || i >= All.Length || Owned(i)) return false;
            var d = All[i];
            if (!CanAfford(d)) return false;
            Inventory.Coins -= d.coins;
            if (d.oil > 0) Inventory.Oil -= d.oil;
            if (d.partsB > 0) SkillSystem.SpendParts(Rank.B, d.partsB);
            if (d.partsA > 0) SkillSystem.SpendParts(Rank.A, d.partsA);
            Town.MercOwned |= 1 << i;
            if (Town.MercActive < 0) Town.MercActive = i;
            return true;
        }
    }
}
