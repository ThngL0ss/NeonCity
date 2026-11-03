#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonBreaker.EditorTools
{
    /// <summary>
    /// Phần mở rộng của NeonBuilder: bẫy, đồng xu, rương, vật phẩm (ItemData) và công cụ nâng cấp Scene
    /// (kéo dài map, giãn 6 trụ, thêm bục, đặt bẫy + rương) mà KHÔNG tạo lại Scene — chỉnh sửa khác của bạn được giữ nguyên.
    /// </summary>
    public static partial class NeonBuilder
    {
        // ---------------------------------------------------------------- bố cục map mới (chỉnh số ở đây nếu muốn đổi)
        const float NewGateX = 252f;                                                      // cổng khu boss (cũ: 128)
        static readonly float[] NewTowerX = { 20f, 63f, 107f, 150f, 193f, 236f };         // 6 trụ cách xa nhau ~43 đơn vị (cũ: 8,28,52,78,99,112)
        const float PlatL1 = 2.35f, PlatL2 = 4.75f, PlatL3 = 7.15f;                        // độ cao 3 tầng bục

        static string ItemDir { get { return Root + "Items/"; } }
        static bool PrefabExists(string name) { return File.Exists(PrefabDir + name + ".prefab"); }

        // ================================================================ MENU
        [MenuItem("Tools/Neon Breaker/Thêm bẫy + kho đồ + rương (không đụng Scene)", false, 11)]
        public static void AddTrapsAndItems()
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
                BuildEconomyAssets();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Neon Breaker: đã tạo Prefab bẫy (TrapSpike/Laser/Saw/Turret/Mine), Coin, Chest và 17 vật phẩm trong " + ItemDir + ". Scene không bị thay đổi.");
                EditorUtility.DisplayDialog("Neon Breaker",
                    "Xong! Đã tạo (nếu chưa có):\n• 5 Prefab bẫy, Prefab Coin, Prefab Chest\n• 17 vật phẩm trong " + ItemDir +
                    "\n• ArtBank đã gán Coin Prefab\n\nScene của bạn không bị thay đổi.\nBước tiếp theo: Tools ▸ Neon Breaker ▸ Mở rộng map + đặt bẫy + rương vào Scene Main.", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Neon Breaker: lỗi khi tạo bẫy / vật phẩm - " + e);
                EditorUtility.DisplayDialog("Neon Breaker", "Lỗi: " + e.Message + "\n(xem Console)", "OK");
            }
        }

        [MenuItem("Tools/Neon Breaker/Mở rộng map + đặt bẫy + rương vào Scene Main", false, 12)]
        public static void UpgradeMainScene()
        {
            root = null;
            if (!File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Neon Breaker", "Chưa có Scene Main.unity. Hãy chạy Build Project trước.", "OK");
                return;
            }
            if (!EditorUtility.DisplayDialog("Neon Breaker",
                "Sẽ chỉnh Scene Main.unity:\n• kéo dài map, giãn 6 trụ cách xa nhau, dời cổng + khu boss\n• thêm bục nhảy\n• đặt bẫy (nhóm Level/Traps) và rương (Level/Chests)\n\n" +
                "Các chỉnh sửa khác của bạn trong Scene được giữ nguyên. Nên commit git trước khi chạy để có thể quay lại.\nTiếp tục?", "Tiếp tục", "Huỷ")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                Prepare();
                BuildEconomyAssets();
                EditorSceneManager.OpenScene(ScenePath);
                string report = UpgradeScene(true);
                AssetDatabase.SaveAssets();
                Debug.Log("Neon Breaker: nâng cấp Scene xong.\n" + report);
                EditorUtility.DisplayDialog("Neon Breaker", "Xong!\n\n" + report + "\n\nBấm Play để thử.", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Neon Breaker: lỗi khi nâng cấp Scene - " + e);
                EditorUtility.DisplayDialog("Neon Breaker", "Lỗi: " + e.Message + "\n(xem Console). Scene chưa được lưu.", "OK");
            }
        }

        // ================================================================ ASSETS: item, coin, chest, trap
        static void BuildEconomyAssets()
        {
            missing.Clear();
            Directory.CreateDirectory(ItemDir.TrimEnd('/'));
            var items = MakeAllItems();
            MakeCoinPrefab();
            MakeChestPrefab(items);
            MakeTrapPrefabs();

            // quái Heavy / Rusher rơi nhiều xu hơn (chỉ chỉnh nếu còn giá trị mặc định)
            EditPrefab("Heavy", go =>
            {
                var e = go.GetComponent<Enemy>();
                if (e != null && e.coinMin == 1 && e.coinMax == 3) { e.coinMin = 3; e.coinMax = 6; e.coinChance = 1f; }
            });
            EditPrefab("Rusher", go =>
            {
                var e = go.GetComponent<Enemy>();
                if (e != null && e.coinMin == 1 && e.coinMax == 3) { e.coinMin = 2; e.coinMax = 4; }
            });

            var ab = LoadOrCreate<ArtBank>(ResDir + "ArtBank.asset");
            if (ab.coinPrefab == null) ab.coinPrefab = Prefab("Coin");
            if (ab.coinIcon == null) ab.coinIcon = Spr("coin_0");
            EditorUtility.SetDirty(ab);
            AssetDatabase.SaveAssets();

            if (missing.Count > 0)
                throw new System.Exception("Thiếu sprite: " + string.Join(", ", missing.ToArray()) + ". Chạy: python3 Tools/PixelArt/gen_extras.py rồi thử lại.");
        }

        // ---------------------------------------------------------------- vật phẩm
        static ItemStatBonus Bn(ItemStat s, float a) { return new ItemStatBonus { stat = s, amount = a }; }

        static ItemData MakeItem(string id, string nameVi, string nameEn, string descVi, string descEn, ItemKind kind,
                                 int maxStack, int sell, Color color, System.Action<ItemData> extra)
        {
            string path = ItemDir + "Item_" + id + ".asset";
            var it = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (it == null)
            {
                it = ScriptableObject.CreateInstance<ItemData>();
                it.nameVi = nameVi; it.nameEn = nameEn; it.descVi = descVi; it.descEn = descEn;
                it.kind = kind; it.maxStack = maxStack; it.sellValue = sell; it.color = color;
                it.icon = Spr("item_" + id);
                if (extra != null) extra(it);
                AssetDatabase.CreateAsset(it, path);
            }
            else if (it.icon == null)
            {
                it.icon = Spr("item_" + id);
                EditorUtility.SetDirty(it);
            }
            return it;
        }

        static Dictionary<string, ItemData> MakeAllItems()
        {
            var common = new Color(0.75f, 0.82f, 0.95f, 1f);
            var uncommon = new Color(0.45f, 1f, 0.4f, 1f);
            var rare = new Color(0.10f, 0.95f, 1f, 1f);
            var epic = new Color(1f, 0.25f, 0.85f, 1f);
            var legend = new Color(1f, 0.80f, 0.25f, 1f);
            var d = new Dictionary<string, ItemData>();

            // ---- trang bị (Passive): có tác dụng khi mang trong kho
            d["core_cell"] = MakeItem("core_cell", "Lõi năng lượng", "Energy Core", "Lõi pin dự phòng nuôi cơ thể bạn.", "A spare power core feeding your body.",
                ItemKind.Passive, 1, 60, rare, it => it.bonuses.Add(Bn(ItemStat.MaxHealth, 20f)));
            d["neon_armor"] = MakeItem("neon_armor", "Giáp neon", "Neon Armor", "Tấm giáp phát sáng làm giảm sát thương nhận vào.", "A glowing plate that softens incoming damage.",
                ItemKind.Passive, 1, 90, epic, it => it.bonuses.Add(Bn(ItemStat.DamageTakenPct, -12f)));
            d["volt_boots"] = MakeItem("volt_boots", "Giày tia chớp", "Volt Boots", "Đế giày dẫn điện giúp bạn chạy nhanh hơn.", "Conductive soles that make you run faster.",
                ItemKind.Passive, 1, 70, uncommon, it => it.bonuses.Add(Bn(ItemStat.MoveSpeedPct, 10f)));
            d["freq_blade"] = MakeItem("freq_blade", "Lưỡi dao tần số", "Frequency Blade", "Lưỡi dao rung tần số cao: chém đau hơn.", "A high-frequency edge: slashes hit harder.",
                ItemKind.Passive, 1, 80, rare, it => it.bonuses.Add(Bn(ItemStat.MeleeDamagePct, 15f)));
            d["shuriken_rail"] = MakeItem("shuriken_rail", "Ray phóng phi tiêu", "Shuriken Rail", "Bộ ray từ trường tăng sức mạnh phi tiêu.", "A magnetic rail that boosts shuriken damage.",
                ItemKind.Passive, 1, 75, rare, it => it.bonuses.Add(Bn(ItemStat.ShurikenDamagePct, 20f)));
            d["phase_coil"] = MakeItem("phase_coil", "Cuộn pha lướt", "Phase Coil", "Làm nguội động cơ lướt nhanh hơn.", "Cools the dash engine faster.",
                ItemKind.Passive, 1, 85, epic, it => it.bonuses.Add(Bn(ItemStat.DashCooldownPct, -15f)));
            d["coin_magnet"] = MakeItem("coin_magnet", "Nam châm xu", "Coin Magnet", "Hút vật phẩm từ xa hơn và nhặt được nhiều xu hơn.", "Pulls items from farther away and boosts coin income.",
                ItemKind.Passive, 1, 65, uncommon, it => { it.bonuses.Add(Bn(ItemStat.MagnetRangePct, 60f)); it.bonuses.Add(Bn(ItemStat.CoinGainPct, 10f)); });
            d["nano_kit"] = MakeItem("nano_kit", "Bộ nano hồi phục", "Nano Repair Kit", "Mỗi lần hồi máu (L) hồi được nhiều hơn.", "Every heal (L) restores more.",
                ItemKind.Passive, 1, 70, rare, it => it.bonuses.Add(Bn(ItemStat.HealPowerPct, 25f)));

            // ---- tiêu hao (Consumable): bấm DÙNG trong kho
            d["heal_vial"] = MakeItem("heal_vial", "Ống hồi máu", "Heal Vial", "Uống để hồi máu ngay.", "Drink to restore HP instantly.",
                ItemKind.Consumable, 9, 20, common, it => it.healHp = 40f);
            d["charge_cell"] = MakeItem("charge_cell", "Pin hồi năng lượng", "Charge Cell", "Cộng thêm 1 lượt hồi máu (phím L).", "Adds 1 heal charge (key L).",
                ItemKind.Consumable, 9, 25, uncommon, it => it.healCharges = 1);
            d["rage_serum"] = MakeItem("rage_serum", "Huyết thanh cuồng nộ", "Rage Serum", "Tăng sát thương chém và phi tiêu trong thời gian ngắn.", "Boosts melee and shuriken damage for a short time.",
                ItemKind.Consumable, 9, 30, rare, it => { it.bonuses.Add(Bn(ItemStat.MeleeDamagePct, 35f)); it.bonuses.Add(Bn(ItemStat.ShurikenDamagePct, 35f)); it.buffDuration = 30f; });
            d["speed_serum"] = MakeItem("speed_serum", "Huyết thanh tốc độ", "Speed Serum", "Chạy nhanh hơn và lướt nhanh hồi hơn trong thời gian ngắn.", "Run faster and dash more often for a short time.",
                ItemKind.Consumable, 9, 30, rare, it => { it.bonuses.Add(Bn(ItemStat.MoveSpeedPct, 30f)); it.bonuses.Add(Bn(ItemStat.DashCooldownPct, -20f)); it.buffDuration = 30f; });
            d["vital_shard"] = MakeItem("vital_shard", "Mảnh sinh lực", "Vital Shard", "Hấp thụ để tăng VĨNH VIỄN máu tối đa.", "Absorb to PERMANENTLY raise max HP.",
                ItemKind.Consumable, 9, 50, epic, it => it.bonuses.Add(Bn(ItemStat.MaxHealth, 10f)));

            // ---- vật phẩm giá trị (Valuable): để bán
            d["scrap_battery"] = MakeItem("scrap_battery", "Pin phế liệu", "Scrap Battery", "Đồ phế liệu, bán được vài xu.", "Junk worth a few coins.", ItemKind.Valuable, 99, 12, common, null);
            d["data_chip"] = MakeItem("data_chip", "Chip dữ liệu", "Data Chip", "Chip lưu dữ liệu cũ, có người trả tiền cho nó.", "An old data chip someone will pay for.", ItemKind.Valuable, 99, 35, uncommon, null);
            d["neon_crystal"] = MakeItem("neon_crystal", "Tinh thể neon", "Neon Crystal", "Tinh thể phát sáng quý hiếm.", "A rare glowing crystal.", ItemKind.Valuable, 50, 80, rare, null);
            d["ancient_core"] = MakeItem("ancient_core", "Lõi cổ đại", "Ancient Core", "Di vật của hệ thống triệu hồi cũ. Rất đáng giá.", "A relic of the old summoning grid. Very valuable.", ItemKind.Valuable, 20, 220, legend, null);

            AssetDatabase.SaveAssets();
            return d;
        }

        static ItemData ItemOf(string id) { return AssetDatabase.LoadAssetAtPath<ItemData>(ItemDir + "Item_" + id + ".asset"); }

        static LootEntry Loot(ItemData item, float weight, int min, int max)
        {
            return new LootEntry { item = item, weight = weight, minCount = min, maxCount = max };
        }

        // ---------------------------------------------------------------- Coin + Chest
        static void MakeCoinPrefab()
        {
            if (PrefabExists("Coin")) return;
            var go = new GameObject("Coin");
            go.layer = Cfg.LayerItem;
            go.AddComponent<Coin>();
            Visual(go, "Visual", Vector2.zero, 46, "spin", Clip("spin", 8f, true, Fr("coin_", 4)));
            Glow(go, "Glow", Vector2.zero, new Vector2(1.6f, 1.6f), A(new Color(1f, 0.85f, 0.2f), 0.40f), 45);
            SavePrefab(go, "Coin");
        }

        static void MakeChestPrefab(Dictionary<string, ItemData> it)
        {
            if (PrefabExists("Chest")) return;
            var go = new GameObject("Chest");
            var ch = go.AddComponent<Chest>();
            var vis = Visual(go, "Visual", Vector2.zero, 12, "closed",
                Clip("closed", 4f, true, Spr("chest_closed")),
                Clip("open", 4f, true, Spr("chest_open")));
            var glow = Glow(go, "Glow", new Vector2(0f, 0.8f), new Vector2(3.4f, 2.2f), A(new Color(1f, 0.85f, 0.2f), 0.3f), 11);
            ch.anim = vis;
            ch.glow = glow;
            ch.rollsMin = 1;
            ch.rollsMax = 2;
            ch.lootTable = new List<LootEntry>
            {
                Loot(it["scrap_battery"], 30f, 1, 3),
                Loot(it["data_chip"], 18f, 1, 2),
                Loot(it["neon_crystal"], 8f, 1, 1),
                Loot(it["ancient_core"], 2f, 1, 1),
                Loot(it["heal_vial"], 20f, 1, 2),
                Loot(it["charge_cell"], 12f, 1, 1),
                Loot(it["rage_serum"], 8f, 1, 1),
                Loot(it["speed_serum"], 8f, 1, 1),
                Loot(it["vital_shard"], 6f, 1, 1),
                Loot(it["core_cell"], 5f, 1, 1),
                Loot(it["neon_armor"], 4f, 1, 1),
                Loot(it["volt_boots"], 5f, 1, 1),
                Loot(it["freq_blade"], 5f, 1, 1),
                Loot(it["shuriken_rail"], 5f, 1, 1),
                Loot(it["phase_coil"], 4f, 1, 1),
                Loot(it["coin_magnet"], 5f, 1, 1),
                Loot(it["nano_kit"], 5f, 1, 1),
            };
            SavePrefab(go, "Chest");
        }

        // ---------------------------------------------------------------- 5 bẫy
        static void MakeTrapPrefabs()
        {
            var red = new Color(1f, 0.2f, 0.25f, 1f);
            var magenta = new Color(1f, 0.15f, 0.8f, 1f);
            var cyan = new Color(0.15f, 0.95f, 1f, 1f);

            if (!PrefabExists("TrapSpike"))
            {
                var go = new GameObject("TrapSpike");
                var sp = go.AddComponent<SpikeTrap>();
                sp.damage = 18f; sp.knockback = 7f;
                var vis = Visual(go, "Visual", Vector2.zero, 10, "down",
                    Clip("down", 4f, true, Spr("trap_spike_down")),
                    Clip("warn", 4f, true, Spr("trap_spike_warn")),
                    Clip("up", 4f, true, Spr("trap_spike_up")));
                sp.anim = vis;
                sp.glow = Glow(go, "Glow", new Vector2(0f, 0.5f), new Vector2(3.8f, 1.8f), A(red, 0f), 11);
                SavePrefab(go, "TrapSpike");
            }

            if (!PrefabExists("TrapLaser"))
            {
                var go = new GameObject("TrapLaser");
                var lt = go.AddComponent<LaserTrap>();
                lt.damage = 22f; lt.knockback = 9f;
                var ea = Child(go, "EmitterA", new Vector2(0f, 0.5f));
                AddSr(ea, Spr("trap_emitter"), 12, false, Color.white);
                var eb = Child(go, "EmitterB", new Vector2(0f, 5.5f));
                AddSr(eb, Spr("trap_emitter"), 12, false, Color.white);
                var beam = Child(go, "Beam", new Vector2(0f, 3f));
                var beamSr = AddSr(beam, Spr("fx_square"), 13, true, A(magenta, 0f));
                var bg = Child(go, "BeamGlow", new Vector2(0f, 3f));
                var bgSr = AddSr(bg, Spr("fx_glow"), 12, true, A(magenta, 0f));
                lt.emitterA = ea.transform; lt.emitterB = eb.transform; lt.beam = beamSr; lt.beamGlow = bgSr;
                SavePrefab(go, "TrapLaser");
            }

            if (!PrefabExists("TrapSaw"))
            {
                var go = new GameObject("TrapSaw");
                var st = go.AddComponent<SawTrap>();
                st.damage = 14f; st.knockback = 8f;
                var rail = Child(go, "Rail", Vector2.zero);
                AddSr(rail, Spr("fx_square"), 8, true, A(cyan, 0.35f));
                var saw = Child(go, "Saw", Vector2.zero);
                var sv = Child(saw, "Visual", Vector2.zero);
                AddSr(sv, Spr("trap_saw"), 14, false, Color.white);
                Glow(saw, "Glow", Vector2.zero, new Vector2(3.4f, 3.4f), A(cyan, 0.25f), 13);
                st.saw = saw.transform; st.rail = rail.transform;
                SavePrefab(go, "TrapSaw");
            }

            if (!PrefabExists("TrapTurret"))
            {
                var go = new GameObject("TrapTurret");
                var tt = go.AddComponent<TurretTrap>();
                tt.damage = 10f;
                var vis = Child(go, "Visual", Vector2.zero);
                var baseSr = AddSr(vis, Spr("trap_turret_base"), 12, false, Color.white);
                var barrel = Child(go, "Barrel", new Vector2(0f, 0.9f));
                AddSr(barrel, Spr("trap_turret_barrel"), 13, false, Color.white);
                var mg = Child(go, "MuzzleGlow", new Vector2(1f, 0.9f));
                mg.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
                var mgSr = AddSr(mg, Spr("fx_glow"), 14, true, A(red, 0f));
                tt.baseSr = baseSr; tt.barrel = barrel.transform; tt.muzzleGlow = mgSr;
                SavePrefab(go, "TrapTurret");
            }

            if (!PrefabExists("TrapMine"))
            {
                var go = new GameObject("TrapMine");
                var mt = go.AddComponent<MineTrap>();
                mt.damage = 30f; mt.knockback = 9f;
                var vis = Visual(go, "Visual", Vector2.zero, 12, "idle",
                    Clip("idle", 2f, true, Fr("trap_mine_idle_", 2)),
                    Clip("armed", 10f, true, Fr("trap_mine_armed_", 2)));
                var danger = Child(go, "Danger", new Vector2(0f, 0.3f));
                danger.transform.localScale = new Vector3(5.6f, 5.6f, 1f);
                var dSr = AddSr(danger, Spr("fx_ring"), 5, true, A(red, 0f));
                mt.anim = vis; mt.dangerGlow = dSr;
                SavePrefab(go, "TrapMine");
            }
        }

        // ================================================================ NÂNG CẤP SCENE
        static void Dirty(Object o)
        {
            if (o == null) return;
            EditorUtility.SetDirty(o);
            if (PrefabUtility.IsPartOfPrefabInstance(o)) PrefabUtility.RecordPrefabInstancePropertyModifications(o);
        }

        static void SetPos(Transform t, Vector3 p)
        {
            Undo.RecordObject(t, "Neon map");
            t.position = p;
            Dirty(t);
        }

        static void MoveX(Transform t, float dx) { if (t != null) SetPos(t, t.position + new Vector3(dx, 0f, 0f)); }

        static float PlatWidth(Transform p)
        {
            var sr = p.GetComponent<SpriteRenderer>();
            return (sr != null && sr.drawMode == SpriteDrawMode.Tiled) ? sr.size.x : 5f;
        }

        static float NudgeFromTowers(float x, float w, List<float> towerXs)
        {
            float min = w * 0.5f + 2.3f;
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < towerXs.Count; i++)
                    if (Mathf.Abs(x - towerXs[i]) < min) x = towerXs[i] + (x >= towerXs[i] ? min : -min);
            return x;
        }

        static bool PlatformFree(List<Transform> all, float x, float y, float w)
        {
            for (int i = 0; i < all.Count; i++)
            {
                var p = all[i];
                if (p == null || Mathf.Abs(p.position.y - y) > 0.3f) continue;
                if (Mathf.Abs(p.position.x - x) < (w + PlatWidth(p)) * 0.5f + 1.5f) return false;
            }
            return true;
        }

        static Transform AddPlatform(Transform plats, List<Transform> all, float x, float y, float w, string name)
        {
            Platform(plats, "tile_plat", x, y, w, name);
            var t = plats.GetChild(plats.childCount - 1);
            all.Add(t);
            return t;
        }

        static void SetProps(Object target, System.Action<SerializedObject> edit)
        {
            var so = new SerializedObject(target);
            edit(so);
            so.ApplyModifiedProperties();
        }

        static GameObject PlaceItem(string prefab, Transform parent, string name, float x, float y)
        {
            if (Prefab(prefab) == null) throw new System.Exception("Thiếu Prefab " + prefab + " (chạy menu Thêm bẫy + kho đồ + rương trước).");
            var go = Inst(prefab, parent, new Vector2(x, y));
            go.name = name;
            return go;
        }

        /// <summary>
        /// Nâng cấp Scene đang mở: dãn map (nếu chưa), thêm bục, đặt lại nhóm Traps + Chests. Trả về báo cáo ngắn.
        /// </summary>
        static string UpgradeScene(bool askBeforeReplace)
        {
            var scene = SceneManager.GetActiveScene();
            Level level = null;
            foreach (var g in scene.GetRootGameObjects()) { level = g.GetComponent<Level>(); if (level != null) break; }
            if (level == null) throw new System.Exception("Scene đang mở không có object Level (hãy mở Main.unity).");
            if (level.gate == null || level.limitLeft == null || level.limitRight == null || level.bossSpawn == null)
                throw new System.Exception("Object Level thiếu các mốc Gate / Limit Left / Limit Right / Boss Spawn.");

            var rep = new StringBuilder();
            Transform lv = level.transform;
            Transform plats = lv.Find("Platforms");
            if (plats == null) plats = Group(lv, "Platforms");
            Transform towers = lv.Find("Towers");

            // ---- đã có bẫy / rương từ lần trước?
            Transform oldTraps = lv.Find("Traps"), oldChests = lv.Find("Chests");
            if ((oldTraps != null || oldChests != null) && askBeforeReplace)
            {
                if (!EditorUtility.DisplayDialog("Neon Breaker", "Scene đã có nhóm Traps / Chests từ lần trước.\nXoá chúng và đặt lại (mất chỉnh sửa riêng của bạn trên các bẫy / rương đó)?", "Đặt lại", "Huỷ"))
                    throw new System.Exception("Đã huỷ.");
            }

            float oldGate = level.gate.position.x;
            bool needStretch = oldGate < NewGateX - 1f;
            var towerXs = new List<float>();
            var allPlats = new List<Transform>();
            var chestSpots = new List<Vector2>();

            // ---- 1. kéo dài map
            if (needStretch)
            {
                float left = level.limitLeft.position.x;
                float delta = NewGateX - oldGate;
                float k = (NewGateX - left) / Mathf.Max(1f, oldGate - left);

                MoveX(level.gate, delta);
                MoveX(level.bossSpawn, delta);
                MoveX(level.limitRight, delta);
                MoveX(lv.Find("BossArena"), delta);

                var ground = lv.Find("Ground");
                if (ground != null)
                {
                    float right = level.limitRight.position.x;
                    float width = right - left;
                    SetPos(ground, new Vector3((left + right) * 0.5f, ground.position.y, ground.position.z));
                    var gc = ground.GetComponent<BoxCollider2D>();
                    if (gc != null) { Undo.RecordObject(gc, "Neon map"); gc.size = new Vector2(width, gc.size.y); Dirty(gc); }
                    for (int i = 0; i < ground.childCount; i++)
                    {
                        var sr = ground.GetChild(i).GetComponent<SpriteRenderer>();
                        if (sr != null && sr.drawMode == SpriteDrawMode.Tiled) { Undo.RecordObject(sr, "Neon map"); sr.size = new Vector2(width, sr.size.y); Dirty(sr); }
                    }
                }

                // trụ: sắp theo X rồi đặt vào vị trí mới
                var tl = new List<Transform>();
                if (towers != null) for (int i = 0; i < towers.childCount; i++) tl.Add(towers.GetChild(i));
                tl.Sort((a, b) => a.position.x.CompareTo(b.position.x));
                for (int i = 0; i < tl.Count; i++)
                {
                    float nx = i < NewTowerX.Length ? NewTowerX[i] : left + (tl[i].position.x - left) * k;
                    SetPos(tl[i], new Vector3(nx, tl[i].position.y, tl[i].position.z));
                    towerXs.Add(nx);
                }

                // bục sẵn có: dãn theo tỉ lệ rồi tránh đè lên trụ
                for (int i = 0; i < plats.childCount; i++) allPlats.Add(plats.GetChild(i));
                foreach (var p in allPlats)
                {
                    float x = p.position.x;
                    float nx = x <= oldGate ? left + (x - left) * k : x + delta;
                    nx = NudgeFromTowers(nx, PlatWidth(p), towerXs);
                    SetPos(p, new Vector3(nx, p.position.y, p.position.z));
                }
                int oldCount = allPlats.Count;

                // nối các khoảng trống giữa hai bục liên tiếp bằng bục trung gian (giữ đường đi liên tục)
                var sorted = new List<Transform>(allPlats);
                sorted.Sort((a, b) => a.position.x.CompareTo(b.position.x));
                int mid = 0;
                for (int i = 0; i + 1 < sorted.Count; i++)
                {
                    var a = sorted[i]; var b = sorted[i + 1];
                    float wa = PlatWidth(a), wb = PlatWidth(b);
                    float ea = a.position.x + wa * 0.5f;
                    float gap = (b.position.x - wb * 0.5f) - ea;
                    if (gap <= 11f) continue;
                    int n = Mathf.FloorToInt(gap / 11f);
                    for (int j = 1; j <= n; j++)
                    {
                        float cx = ea + gap * j / (n + 1);
                        float ya = a.position.y, yb = b.position.y, y;
                        if (Mathf.Abs(ya - yb) > 0.3f) y = (j % 2 == 1) ? Mathf.Min(ya, yb) : Mathf.Max(ya, yb);
                        else y = (j % 2 == 1) ? ya : (ya < 3.5f ? PlatL2 : PlatL1);
                        cx = NudgeFromTowers(cx, 4.5f, towerXs);
                        if (!PlatformFree(allPlats, cx, y, 4.5f)) continue;
                        AddPlatform(plats, allPlats, cx, y, 4.5f, "Platform_Mid_" + (++mid));
                    }
                }

                // "hốc rương": cầu thang 3 bục dẫn lên bục tầng 3 có rương, đặt giữa hai trụ liên tiếp
                int stairs = 0;
                for (int i = 0; i + 1 < towerXs.Count; i++)
                {
                    float xc = (towerXs[i] + towerXs[i + 1]) * 0.5f;
                    float x1 = NudgeFromTowers(xc - 9f, 5f, towerXs), x2 = NudgeFromTowers(xc - 2f, 5f, towerXs), x3 = NudgeFromTowers(xc + 5f, 5f, towerXs);
                    if (PlatformFree(allPlats, x2, PlatL2, 5f) && PlatformFree(allPlats, x3, PlatL3, 5f))
                    {
                        if (PlatformFree(allPlats, x1, PlatL1, 5f)) AddPlatform(plats, allPlats, x1, PlatL1, 5f, "Platform_Stair_" + (i + 1) + "a");
                        AddPlatform(plats, allPlats, x2, PlatL2, 5f, "Platform_Stair_" + (i + 1) + "b");
                        AddPlatform(plats, allPlats, x3, PlatL3, 5f, "Platform_Stair_" + (i + 1) + "c");
                        chestSpots.Add(new Vector2(x3, PlatL3 + 0.25f));
                        stairs++;
                    }
                    else chestSpots.Add(new Vector2(x3, 0f));       // chỗ bị chiếm: đặt rương dưới đất
                }
                rep.AppendLine("• Map dài hơn: cổng " + oldGate.ToString("0") + " → " + NewGateX.ToString("0") + ", 6 trụ cách nhau ~43 đơn vị.");
                rep.AppendLine("• Bục: " + oldCount + " bục cũ dãn ra + " + (allPlats.Count - oldCount) + " bục mới (" + mid + " bục nối, " + stairs + " cầu thang lên hốc rương).");
            }
            else
            {
                rep.AppendLine("• Map đã được kéo dài từ trước (cổng ở x=" + oldGate.ToString("0") + ") — giữ nguyên.");
                if (towers != null) for (int i = 0; i < towers.childCount; i++) towerXs.Add(towers.GetChild(i).position.x);
                towerXs.Sort();
                for (int i = 0; i + 1 < towerXs.Count; i++)
                {
                    // dùng lại các bục L3 sẵn có làm chỗ đặt rương
                    float xc = (towerXs[i] + towerXs[i + 1]) * 0.5f;
                    Transform best = null; float bd = 1e9f;
                    for (int j = 0; j < plats.childCount; j++)
                    {
                        var p = plats.GetChild(j);
                        if (Mathf.Abs(p.position.y - PlatL3) > 0.3f) continue;
                        float d = Mathf.Abs(p.position.x - xc);
                        if (d < bd && d < 14f) { bd = d; best = p; }
                    }
                    chestSpots.Add(best != null ? new Vector2(best.position.x, PlatL3 + 0.25f) : new Vector2(xc + 5f, 0f));
                }
            }

            // ---- 2. bẫy
            if (oldTraps != null) Object.DestroyImmediate(oldTraps.gameObject);
            if (oldChests != null) Object.DestroyImmediate(oldChests.gameObject);
            var traps = Group(lv, "Traps");
            var chests = Group(lv, "Chests");
            int nTraps = 0;

            float[] spikeX = { 30f, 84f, 134f, 176f, 180f, 243f };
            for (int i = 0; i < spikeX.Length; i++)
            {
                var go = PlaceItem("TrapSpike", traps, "Spike_" + (i + 1), spikeX[i], 0f);
                float off = (i * 0.9f) % 2.4f;
                if (i == 4) off += 1.9f;                       // hai bẫy gai liền nhau luân phiên
                SetProps(go.GetComponent<SpikeTrap>(), so => so.FindProperty("startOffset").floatValue = off);
                nTraps++;
            }

            // cưa quay: (x, y, đường chạy)
            var saws = new[] { new Vector4(42f, 0.95f, 9f, 0f), new Vector4(120f, 0.9f, 0f, 4.2f), new Vector4(205f, 0.95f, 10f, 0f) };
            for (int i = 0; i < saws.Length; i++)
            {
                var go = PlaceItem("TrapSaw", traps, "Saw_" + (i + 1), saws[i].x, saws[i].y);
                var tv = new Vector2(saws[i].z, saws[i].w);
                SetProps(go.GetComponent<SawTrap>(), so =>
                {
                    so.FindProperty("travel").vector2Value = tv;
                    so.FindProperty("speed").floatValue = tv.y > 0.1f ? 3.5f : 4.5f + 0.4f * (tv.x > 9.5f ? 1 : 0);
                });
                nTraps++;
            }

            float[] mineX = { 56f, 126f, 184f };
            for (int i = 0; i < mineX.Length; i++) { PlaceItem("TrapMine", traps, "Mine_" + (i + 1), mineX[i], 0f); nTraps++; }

            // laser: cổng dọc ở 76 và 220, tia ngang sát đất ở 140
            var l1 = PlaceItem("TrapLaser", traps, "Laser_1", 76f, 0f);
            var l2 = PlaceItem("TrapLaser", traps, "Laser_2", 140f, 0f);
            var l3 = PlaceItem("TrapLaser", traps, "Laser_3", 220f, 0f);
            SetProps(l2.GetComponent<LaserTrap>(), so =>
            {
                so.FindProperty("vertical").boolValue = false;
                so.FindProperty("length").floatValue = 4.4f;
                so.FindProperty("horizontalHeight").floatValue = 0.9f;
                so.FindProperty("thickness").floatValue = 0.45f;
                so.FindProperty("onTime").floatValue = 1.2f;
            });
            SetProps(l3.GetComponent<LaserTrap>(), so => so.FindProperty("startOffset").floatValue = 1.2f);
            nTraps += 3;

            // súng tự động quay về bên trái (hướng người chơi đi tới): càng về cuối càng bắn nhiều viên
            float[] turX = { 98f, 170f, 228f };
            for (int i = 0; i < turX.Length; i++)
            {
                var go = PlaceItem("TrapTurret", traps, "Turret_" + (i + 1), turX[i], 0f);
                int burst = i + 1;
                SetProps(go.GetComponent<TurretTrap>(), so =>
                {
                    so.FindProperty("facing").intValue = -1;
                    so.FindProperty("burstCount").intValue = burst;
                    so.FindProperty("fireInterval").floatValue = 2.4f + 0.2f * burst;
                });
                nTraps++;
            }

            // ---- 3. rương
            int nChests = 0;
            var start = PlaceItem("Chest", chests, "Chest_Start", 6f, 0f);
            SetProps(start.GetComponent<Chest>(), so =>
            {
                so.FindProperty("rollsMin").intValue = 1;
                so.FindProperty("rollsMax").intValue = 1;
                so.FindProperty("coinsMin").intValue = 5;
                so.FindProperty("coinsMax").intValue = 10;
                SetGuaranteed(so, ItemOf("heal_vial"), 1, 2);
            });
            nChests++;
            for (int i = 0; i < chestSpots.Count; i++)
            {
                var go = PlaceItem("Chest", chests, "Chest_" + (i + 1), chestSpots[i].x, chestSpots[i].y);
                int idx = i;
                SetProps(go.GetComponent<Chest>(), so =>
                {
                    so.FindProperty("rollsMin").intValue = 1;
                    so.FindProperty("rollsMax").intValue = idx >= 2 ? 3 : 2;
                    so.FindProperty("coinsMin").intValue = 10 + 6 * idx;
                    so.FindProperty("coinsMax").intValue = 22 + 10 * idx;
                });
                nChests++;
            }
            var gateChest = PlaceItem("Chest", chests, "Chest_Gate", NewGateX - 6f, 0f);
            SetProps(gateChest.GetComponent<Chest>(), so =>
            {
                so.FindProperty("rollsMin").intValue = 2;
                so.FindProperty("rollsMax").intValue = 3;
                so.FindProperty("coinsMin").intValue = 40;
                so.FindProperty("coinsMax").intValue = 70;
                SetGuaranteed(so, ItemOf("charge_cell"), 1, 2);
            });
            nChests++;

            rep.AppendLine("• Bẫy: " + nTraps + " cái trong Level/Traps (6 gai, 3 cưa, 3 mìn, 3 laser, 3 súng).");
            rep.AppendLine("• Rương: " + nChests + " cái trong Level/Chests (bấm T khi đứng gần). Chỉnh bảng vật phẩm: chọn rương ▸ Chest ▸ Loot Table.");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return rep.ToString().TrimEnd();
        }

        static void SetGuaranteed(SerializedObject so, ItemData item, int min, int max)
        {
            if (item == null) return;
            var gl = so.FindProperty("guaranteed");
            gl.arraySize = 1;
            var e = gl.GetArrayElementAtIndex(0);
            e.FindPropertyRelative("item").objectReferenceValue = item;
            e.FindPropertyRelative("weight").floatValue = 1f;
            e.FindPropertyRelative("minCount").intValue = min;
            e.FindPropertyRelative("maxCount").intValue = max;
        }
    }
}
#endif
