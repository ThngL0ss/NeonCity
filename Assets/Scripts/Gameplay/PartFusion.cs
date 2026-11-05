using UnityEngine;

namespace NeonBreaker
{
    /// <summary>Kết quả một lần ghép linh kiện.</summary>
    public struct FusionResult
    {
        public bool ok;
        public bool lucky;
        public int made;
        public Rank to;
        public string message;
    }

    /// <summary>
    /// Ghép linh kiện bậc thấp thành bậc cao hơn (chỉ làm được ở căn cứ): 3 mảnh bậc D + 20 dầu + 50 xu = 1 mảnh bậc C.
    /// Chỉ ghép F->D, D->C, C->B (không ghép lên A và S). Có xác suất may mắn nhận thêm mảnh thành phẩm.
    /// Số mảnh / dầu / xu / xác suất chỉnh ở BaseConfig.
    /// </summary>
    public static class PartFusion
    {
        /// <summary>Tổng số lần ghép thành công trong ván (dùng cho nhiệm vụ).</summary>
        public static int FusedTotal;

        public static void ResetAll() { FusedTotal = 0; }

        public static bool CanFuseRank(Rank r)
        {
            Rank to;
            return PowerRules.FusionTarget(r, out to) && BaseConfig.Cur.RuleFor(r) != null;
        }

        public static FusionRule Rule(Rank r) { return BaseConfig.Cur.RuleFor(r); }

        /// <summary>Đủ mọi thứ để ghép chưa (đang ở căn cứ, đủ mảnh, dầu, xu)?</summary>
        public static bool CanFuse(Rank r)
        {
            var rule = Rule(r);
            Rank to;
            if (rule == null || !PowerRules.FusionTarget(r, out to)) return false;
            return GameFlow.InBase && SkillSystem.Parts(r) >= rule.partsNeeded && Inventory.Oil >= rule.oil && Inventory.Coins >= rule.coins;
        }

        public static FusionResult Fuse(Rank r)
        {
            var res = new FusionResult();
            Rank to;
            var rule = Rule(r);
            if (rule == null || !PowerRules.FusionTarget(r, out to))
            {
                res.message = Cfg.L("Linh kiện bậc " + r + " không ghép lên được", "Rank " + r + " parts cannot be fused further");
                return res;
            }
            res.to = to;
            if (!GameFlow.InBase) { res.message = Cfg.L("Chỉ ghép được ở căn cứ", "You can only fuse at the base"); return res; }
            if (SkillSystem.Parts(r) < rule.partsNeeded)
            {
                res.message = Cfg.L("Thiếu linh kiện bậc " + r + " (" + SkillSystem.Parts(r) + "/" + rule.partsNeeded + ")", "Not enough rank " + r + " parts (" + SkillSystem.Parts(r) + "/" + rule.partsNeeded + ")");
                return res;
            }
            if (Inventory.Oil < rule.oil) { res.message = Cfg.L("Thiếu dầu (" + Inventory.Oil + "/" + rule.oil + ")", "Not enough oil (" + Inventory.Oil + "/" + rule.oil + ")"); return res; }
            if (Inventory.Coins < rule.coins) { res.message = Cfg.L("Thiếu xu (" + Inventory.Coins + "/" + rule.coins + ")", "Not enough coins (" + Inventory.Coins + "/" + rule.coins + ")"); return res; }

            SkillSystem.SpendParts(r, rule.partsNeeded);
            Inventory.SpendOil(rule.oil);
            Inventory.Spend(rule.coins);

            var cfg = BaseConfig.Cur;
            int made = 1;
            bool lucky = Random.value < cfg.luckChance;
            if (lucky) made += Mathf.Max(0, cfg.luckBonusParts);
            SkillSystem.AddParts(to, made, false, false);
            FusedTotal++;

            res.ok = true;
            res.lucky = lucky;
            res.made = made;
            res.message = lucky
                ? Cfg.L("MAY MẮN! Nhận " + made + " linh kiện bậc " + to, "LUCKY! Got " + made + " rank " + to + " parts")
                : Cfg.L("Ghép thành công: 1 linh kiện bậc " + to, "Fused: 1 rank " + to + " part");
            return res;
        }
    }
}
