using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>Luật ghép linh kiện: ghép N mảnh bậc "from" + dầu + xu = 1 mảnh bậc kế tiếp.</summary>
    [System.Serializable]
    public class FusionRule
    {
        [Tooltip("Bậc linh kiện đem ghép. Chỉ F, D, C ghép lên được (F->D, D->C, C->B); không ghép lên A và S")] public Rank from = Rank.D;
        [Tooltip("Số mảnh cần (ô tam giác: 3 ô)")] public int partsNeeded = 3;
        [Tooltip("Dầu cần")] public int oil = 20;
        [Tooltip("Xu cần")] public int coins = 50;
    }

    [System.Serializable]
    public class RecipeIngredient
    {
        public ItemData item;
        public int count = 1;
    }

    /// <summary>Công thức chế tác vật phẩm ở bàn chế tác trong căn cứ.</summary>
    [System.Serializable]
    public class CraftRecipe
    {
        [Tooltip("Vật phẩm tạo ra")] public ItemData output;
        public int outputCount = 1;
        [Tooltip("Nguyên liệu")] public List<RecipeIngredient> ingredients = new List<RecipeIngredient>();
        [Tooltip("Dầu cần")] public int oil = 5;
        [Tooltip("Xu cần (0 = miễn phí)")] public int coins = 0;
    }

    /// <summary>Một dòng trong bảng vật phẩm nằm rải rác trên đường (bấm F để nhặt).</summary>
    [System.Serializable]
    public class ScatterEntry
    {
        public ItemData item;
        [Tooltip("Trọng số: càng lớn càng hay gặp")] public float weight = 1f;
        public int minCount = 1;
        public int maxCount = 1;
    }

    /// <summary>
    /// Thông số căn cứ: ghép linh kiện, công thức chế tác, vật phẩm rải rác trên đường.
    /// File: Assets/Resources/NeonBreaker/BaseConfig.asset (menu Tools ▸ Neon Breaker ▸ Cập nhật cấp bậc + dầu + căn cứ tạo sẵn).
    /// </summary>
    [CreateAssetMenu(menuName = "Neon Breaker/Base Config", fileName = "BaseConfig")]
    public class BaseConfig : ScriptableObject
    {
        [Header("Ghép linh kiện (máy ghép trong căn cứ)")]
        public List<FusionRule> fusion = new List<FusionRule>
        {
            new FusionRule { from = Rank.F, partsNeeded = 3, oil = 10, coins = 20 },
            new FusionRule { from = Rank.D, partsNeeded = 3, oil = 20, coins = 50 },
            new FusionRule { from = Rank.C, partsNeeded = 3, oil = 40, coins = 120 },
        };
        [Tooltip("Xác suất may mắn khi ghép: nhận thêm linh kiện thành phẩm"), Range(0f, 1f)] public float luckChance = 0.15f;
        [Tooltip("Số linh kiện thành phẩm nhận thêm khi may mắn")] public int luckBonusParts = 2;

        [Header("Chế tác vật phẩm (bàn chế tác trong căn cứ)")]
        public List<CraftRecipe> recipes = new List<CraftRecipe>();

        [Header("Vật phẩm rải rác trên đường (bấm F để nhặt)")]
        public List<ScatterEntry> scatter = new List<ScatterEntry>();
        [Tooltip("Số dầu trong mỗi thùng dầu nằm trên đường (ít nhất / nhiều nhất)")] public Vector2Int oilCanAmount = new Vector2Int(6, 14);

        static BaseConfig cached;

        public static BaseConfig Cur
        {
            get
            {
                if (cached == null)
                {
                    cached = Resources.Load<BaseConfig>("NeonBreaker/BaseConfig");
                    if (cached == null) cached = CreateInstance<BaseConfig>();
                }
                return cached;
            }
        }

        public static void ResetStatics() { cached = null; }

        public FusionRule RuleFor(Rank from)
        {
            for (int i = 0; i < fusion.Count; i++)
                if (fusion[i] != null && fusion[i].from == from) return fusion[i];
            return null;
        }
    }
}
