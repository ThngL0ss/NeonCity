#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NeonBreaker.EditorTools
{
    /// <summary>
    /// Phần mở rộng của NeonBuilder cho giai đoạn "cấp bậc + dầu + căn cứ":
    /// gán cấp bậc cho quái, Prefab dầu + vật phẩm nhặt bằng F, 4 nguyên liệu, công thức chế tác, BaseConfig, thêm nguyên liệu vào rương.
    /// Chạy được nhiều lần: chỉ tạo cái còn thiếu, KHÔNG ghi đè file bạn đã chỉnh. Không đụng Scene Main.
    /// </summary>
    public static partial class NeonBuilder
    {
        // ================================================================ MENU
        [MenuItem("Tools/Neon Breaker/Cập nhật cấp bậc + dầu + căn cứ (không đụng Scene Main)", false, 14)]
        public static void AddRanksOilBase()
        {
            root = null;
            if (!File.Exists(PrefabDir + "Walker.prefab"))
            {
                EditorUtility.DisplayDialog("Neon Breaker", "Chưa có Prefab Walker. Hãy chạy Build Project trước.", "OK");
                return;
            }
            try
            {
                Prepare();
                string report = BuildRankOilAssets();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Neon Breaker: đã cập nhật cấp bậc + dầu + căn cứ.\n" + report);
                EditorUtility.DisplayDialog("Neon Breaker",
                    "Xong!\n\n" + report + "\n\nScene Main của bạn không bị thay đổi.", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Neon Breaker: lỗi khi cập nhật cấp bậc + dầu + căn cứ - " + e);
                EditorUtility.DisplayDialog("Neon Breaker", "Lỗi: " + e.Message + "\n(xem Console)", "OK");
            }
        }

        // ================================================================ ASSETS
        static string BuildRankOilAssets()
        {
            missing.Clear();
            Directory.CreateDirectory(ItemDir.TrimEnd('/'));

            // ---- cấp bậc quái (chỉ đặt nếu còn là F, không ghi đè chỉnh sửa của bạn)
            int ranked = 0;
            ranked += SetEnemyRank("Walker", Rank.D);
            ranked += SetEnemyRank("Flyer", Rank.D);
            ranked += SetEnemyRank("Heavy", Rank.C);
            ranked += SetEnemyRank("Rusher", Rank.C);

            // ---- Prefab dầu + vật phẩm nhặt bằng F
            MakeOilPrefab();
            MakeWorldItemPrefab();

            // ---- nguyên liệu
            var common = new Color(0.75f, 0.82f, 0.95f, 1f);
            var uncommon = new Color(0.45f, 1f, 0.4f, 1f);
            var rare = new Color(0.10f, 0.95f, 1f, 1f);
            var mats = new Dictionary<string, ItemData>();
            mats["scrap_metal"] = MakeItem("scrap_metal", "Sắt vụn", "Scrap Metal", "Mảnh kim loại nhặt được. Nguyên liệu chế tác cơ bản.", "Salvaged metal. Basic crafting material.",
                ItemKind.Material, 99, 4, common, null);
            mats["copper_wire"] = MakeItem("copper_wire", "Dây đồng", "Copper Wire", "Cuộn dây đồng dẫn điện tốt.", "A coil of well-conducting copper wire.",
                ItemKind.Material, 99, 6, new Color(0.95f, 0.55f, 0.30f, 1f), null);
            mats["circuit_board"] = MakeItem("circuit_board", "Bo mạch", "Circuit Board", "Bo mạch còn nguyên vẹn. Cần cho các món chế tác nâng cao.", "An intact circuit board. Needed for advanced crafts.",
                ItemKind.Material, 99, 10, uncommon, null);
            mats["energy_gel"] = MakeItem("energy_gel", "Gel năng lượng", "Energy Gel", "Gel chứa năng lượng, dùng làm thuốc và huyết thanh.", "Energy-rich gel for potions and serums.",
                ItemKind.Material, 99, 8, rare, null);

            // ---- ArtBank
            var ab = LoadOrCreate<ArtBank>(ResDir + "ArtBank.asset");
            if (ab.oilPrefab == null) ab.oilPrefab = Prefab("Oil");
            if (ab.oilIcon == null) ab.oilIcon = Spr("oil_can");
            if (ab.worldItemPrefab == null) ab.worldItemPrefab = Prefab("WorldItem");
            if (ab.playerSkeleton == null) ab.playerSkeleton = Spr("player_skeleton");
            if (ab.robotBits == null || ab.robotBits.Length == 0) ab.robotBits = Fr("robot_bit_", 4);
            EditorUtility.SetDirty(ab);

            // ---- boss: bậc SS, cấp 1 (chỉ đặt nếu còn là F)
            EditPrefab("Boss", go =>
            {
                var b = go.GetComponent<Boss>();
                if (b == null) return;
                if (b.power == null) b.power = new PowerInfo(Rank.SS, 1);
                if (b.power.rank == Rank.F) { b.power.rank = Rank.SS; b.power.level = 1; }
            });

            // ---- BaseConfig: công thức chế tác + bảng rải vật phẩm
            var cfg = LoadOrCreate<BaseConfig>(ResDir + "BaseConfig.asset");
            int addedRecipes = 0;
            if (cfg.recipes == null) cfg.recipes = new List<CraftRecipe>();
            if (cfg.recipes.Count == 0)
            {
                cfg.recipes.Add(Recipe("heal_vial", 2, 4, 0, "scrap_metal", 1, "energy_gel", 1));
                cfg.recipes.Add(Recipe("charge_cell", 1, 6, 15, "copper_wire", 2, "energy_gel", 1));
                cfg.recipes.Add(Recipe("rage_serum", 1, 10, 30, "energy_gel", 2, "circuit_board", 1));
                cfg.recipes.Add(Recipe("speed_serum", 1, 10, 30, "energy_gel", 2, "copper_wire", 1));
                cfg.recipes.Add(Recipe("vital_shard", 1, 25, 80, "circuit_board", 2, "energy_gel", 3, "scrap_metal", 3));
                addedRecipes = cfg.recipes.Count;
            }
            if (cfg.scatter == null) cfg.scatter = new List<ScatterEntry>();
            if (cfg.scatter.Count == 0)
            {
                cfg.scatter.Add(new ScatterEntry { item = mats["scrap_metal"], weight = 40f, minCount = 1, maxCount = 3 });
                cfg.scatter.Add(new ScatterEntry { item = mats["copper_wire"], weight = 30f, minCount = 1, maxCount = 2 });
                cfg.scatter.Add(new ScatterEntry { item = mats["circuit_board"], weight = 15f, minCount = 1, maxCount = 1 });
                cfg.scatter.Add(new ScatterEntry { item = mats["energy_gel"], weight = 25f, minCount = 1, maxCount = 2 });
            }
            EditorUtility.SetDirty(cfg);

            // ---- rương: thêm nguyên liệu vào bảng rơi (nếu chưa có)
            int chestAdded = 0;
            EditPrefab("Chest", go =>
            {
                var ch = go.GetComponent<Chest>();
                if (ch == null) return;
                if (ch.lootTable == null) ch.lootTable = new List<LootEntry>();
                foreach (var kv in mats)
                {
                    bool have = false;
                    foreach (var le in ch.lootTable) if (le != null && le.item == kv.Value) { have = true; break; }
                    if (have) continue;
                    float w = kv.Key == "scrap_metal" ? 26f : kv.Key == "copper_wire" ? 20f : kv.Key == "circuit_board" ? 10f : 16f;
                    ch.lootTable.Add(Loot(kv.Value, w, 1, kv.Key == "scrap_metal" ? 3 : 2));
                    chestAdded++;
                }
            });

            AssetDatabase.SaveAssets();
            if (missing.Count > 0)
                throw new System.Exception("Thiếu sprite: " + string.Join(", ", missing.ToArray()) + ". Chạy: python3 Tools/PixelArt/gen_stage2.py và python3 Tools/PixelArt/gen_cine.py rồi thử lại.");

            return "• Cấp bậc quái: Walker, Flyer = D · Heavy, Rusher = C (đã đặt " + ranked + " quái còn ở bậc F)\n" +
                   "• Prefab Oil (giọt dầu) + WorldItem (thùng dầu / nguyên liệu nhặt bằng F), ArtBank đã gán\n" +
                   "• Boss = bậc SS cấp 1; ArtBank có sprite bộ khung robot + mảnh linh kiện cho cinematic\n" +
                   "• 4 nguyên liệu: Sắt vụn, Dây đồng, Bo mạch, Gel năng lượng (thêm vào " + chestAdded + " dòng trong bảng rơi của rương)\n" +
                   "• BaseConfig: " + cfg.recipes.Count + " công thức chế tác (mới thêm " + addedRecipes + "), ghép linh kiện + bảng vật phẩm rải đường\n" +
                   "• Cấu hình: " + ResDir + "BaseConfig.asset";
        }

        static int SetEnemyRank(string prefab, Rank r)
        {
            int n = 0;
            EditPrefab(prefab, go =>
            {
                var e = go.GetComponent<Enemy>();
                if (e == null) return;
                if (e.power == null) e.power = new PowerInfo();
                if (e.power.rank == Rank.F) { e.power.rank = r; n++; }
            });
            return n;
        }

        static CraftRecipe Recipe(string output, int count, int oil, int coins, params object[] ing)
        {
            var r = new CraftRecipe { output = ItemOf(output), outputCount = count, oil = oil, coins = coins, ingredients = new List<RecipeIngredient>() };
            for (int i = 0; i + 1 < ing.Length; i += 2)
                r.ingredients.Add(new RecipeIngredient { item = ItemOf((string)ing[i]), count = (int)ing[i + 1] });
            return r;
        }

        // ---------------------------------------------------------------- Prefab dầu + vật phẩm nhặt
        static void MakeOilPrefab()
        {
            if (PrefabExists("Oil")) return;
            var go = new GameObject("Oil");
            go.layer = Cfg.LayerItem;
            var o = go.AddComponent<OilDrop>();
            o.restHeight = 0.4f;
            Visual(go, "Visual", Vector2.zero, 46, "spin", Clip("spin", 6f, true, Fr("oil_drop_", 4)));
            Glow(go, "Glow", Vector2.zero, new Vector2(1.4f, 1.4f), A(new Color(1f, 0.72f, 0.2f), 0.35f), 45);
            SavePrefab(go, "Oil");
        }

        static void MakeWorldItemPrefab()
        {
            if (PrefabExists("WorldItem")) return;
            var go = new GameObject("WorldItem");
            go.AddComponent<WorldItem>();
            var vis = Child(go, "Visual", new Vector2(0f, 0.55f));
            AddSr(vis, Spr("oil_can"), 20, false, Color.white);
            Glow(go, "Glow", new Vector2(0f, 0.55f), new Vector2(2f, 2f), A(new Color(1f, 0.8f, 0.3f), 0.3f), 19);
            SavePrefab(go, "WorldItem");
        }
    }
}
#endif
