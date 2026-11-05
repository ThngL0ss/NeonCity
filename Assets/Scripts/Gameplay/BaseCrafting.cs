using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Chế tác vật phẩm ở bàn chế tác trong căn cứ: nguyên liệu + dầu + xu = vật phẩm (công thức ở BaseConfig ▸ Recipes).
    /// </summary>
    public static class BaseCrafting
    {
        /// <summary>Nguyên liệu còn thiếu / đủ: trả về lý do không chế tác được ("" nếu chế tác được).</summary>
        public static string Why(CraftRecipe r)
        {
            if (r == null || r.output == null) return Cfg.L("Công thức lỗi", "Broken recipe");
            if (!GameFlow.InBase) return Cfg.L("Chỉ chế tác được ở căn cứ", "You can only craft at the base");
            for (int i = 0; i < r.ingredients.Count; i++)
            {
                var ing = r.ingredients[i];
                if (ing == null || ing.item == null) continue;
                if (Inventory.Count(ing.item) < ing.count)
                    return Cfg.L("Thiếu " + ing.item.DisplayName, "Missing " + ing.item.DisplayName);
            }
            if (Inventory.Oil < r.oil) return Cfg.L("Thiếu dầu", "Not enough oil");
            if (Inventory.Coins < r.coins) return Cfg.L("Thiếu xu", "Not enough coins");
            return "";
        }

        public static bool CanCraft(CraftRecipe r) { return Why(r) == ""; }

        /// <summary>Chế tác. Trả về thông báo kết quả (ok = true nếu thành công).</summary>
        public static string Craft(CraftRecipe r, out bool ok)
        {
            ok = false;
            string why = Why(r);
            if (why != "") return why;

            var test = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<ItemData, int>>();
            test.Add(new System.Collections.Generic.KeyValuePair<ItemData, int>(r.output, Mathf.Max(1, r.outputCount)));
            // kiểm tra kho còn chỗ SAU khi trừ nguyên liệu (nguyên liệu trả chỗ trống): thử trừ rồi hoàn lại nếu hết chỗ
            for (int i = 0; i < r.ingredients.Count; i++)
                if (r.ingredients[i] != null && r.ingredients[i].item != null) Inventory.RemoveItem(r.ingredients[i].item, r.ingredients[i].count);
            if (!Inventory.CanAddAll(test))
            {
                for (int i = 0; i < r.ingredients.Count; i++)
                    if (r.ingredients[i] != null && r.ingredients[i].item != null) Inventory.Add(r.ingredients[i].item, r.ingredients[i].count);
                return Cfg.L("KHO ĐẦY — hãy dọn kho (B)", "BAG FULL — free some space (B)");
            }
            Inventory.SpendOil(r.oil);
            Inventory.Spend(r.coins);
            Inventory.Add(r.output, Mathf.Max(1, r.outputCount));
            BaseMissions.Crafted++;
            ok = true;
            return Cfg.L("Đã chế tác: ", "Crafted: ") + r.output.DisplayName + (r.outputCount > 1 ? "  x" + r.outputCount : "");
        }
    }
}
