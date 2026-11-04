#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NeonBreaker.EditorTools
{
    /// <summary>
    /// Phần mở rộng của NeonBuilder cho hệ thống Skill: Prefab linh kiện (Part), SkillConfig, 9 kỹ năng mẫu (2 nhánh).
    /// Chạy được nhiều lần: chỉ tạo cái còn thiếu, KHÔNG ghi đè file bạn đã chỉnh. Không đụng Scene.
    /// </summary>
    public static partial class NeonBuilder
    {
        static string SkillDir { get { return Root + "Skills/"; } }

        // ================================================================ MENU
        [MenuItem("Tools/Neon Breaker/Thêm hệ thống Skill (không đụng Scene)", false, 13)]
        public static void AddSkillSystem()
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
                string report = BuildSkillAssets();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Neon Breaker: đã thêm hệ thống Skill.\n" + report);
                EditorUtility.DisplayDialog("Neon Breaker",
                    "Xong! Hệ thống Skill đã sẵn sàng.\n\n" + report +
                    "\n\nScene của bạn không bị thay đổi. Bấm Play: phím N mở menu Skill, phím H mở bảng minibot.\n" +
                    "Mẹo: bật chế độ thử nghiệm (P) để mở khoá kỹ năng chưa có điều kiện.", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Neon Breaker: lỗi khi thêm hệ thống Skill - " + e);
                EditorUtility.DisplayDialog("Neon Breaker", "Lỗi: " + e.Message + "\n(xem Console)", "OK");
            }
        }

        // ================================================================ ASSETS
        /// <summary>Tạo (nếu thiếu) Prefab Part, 9 SkillNodeData, SkillConfig và gán ArtBank. Trả về báo cáo ngắn.</summary>
        static string BuildSkillAssets()
        {
            missing.Clear();
            Directory.CreateDirectory(SkillDir.TrimEnd('/'));

            MakePartPrefab();
            var nodes = MakeSkillNodes();

            // ---- ArtBank: gán Prefab + biểu tượng linh kiện
            var ab = LoadOrCreate<ArtBank>(ResDir + "ArtBank.asset");
            if (ab.partPrefab == null) ab.partPrefab = Prefab("Part");
            if (ab.partIcon == null) ab.partIcon = Spr("part_0");
            EditorUtility.SetDirty(ab);

            // ---- SkillConfig
            var cfg = LoadOrCreate<SkillConfig>(ResDir + "SkillConfig.asset");
            int addedNodes = 0, addedBots = 0;
            if (cfg.nodes == null) cfg.nodes = new List<SkillNodeData>();
            foreach (var n in nodes)
                if (n != null && !cfg.nodes.Contains(n)) { cfg.nodes.Add(n); addedNodes++; }

            if (cfg.craftables == null) cfg.craftables = new List<CraftEntry>();
            var kinds = new[]
            {
                new[] { "Walker", "Walker", "Walker" },
                new[] { "Flyer", "Flyer", "Flyer" },
                new[] { "Heavy", "Heavy", "Heavy" },
                new[] { "Rusher", "Rusher", "Rusher" },
            };
            foreach (var k in kinds)
            {
                var prefab = Prefab(k[0]);
                if (prefab == null) continue;     // Heavy/Rusher chưa tạo: chạy menu "Thêm quái Heavy + Rusher" rồi chạy lại menu này
                bool have = false;
                foreach (var ce in cfg.craftables)
                    if (ce != null && (ce.enemyPrefab == prefab || ce.Id == k[0])) { have = true; break; }
                if (have) continue;
                var en = prefab.GetComponent<Enemy>();
                var col = en != null ? en.AccentColor : Color.white;
                cfg.craftables.Add(new CraftEntry
                {
                    enemyPrefab = prefab,
                    id = "",
                    nameVi = k[1],
                    nameEn = k[2],
                    partsCost = 10,
                    partsDropChance = 1f,
                    partsDropMin = 1,
                    partsDropMax = 1,
                    partColor = new Color(col.r, col.g, col.b, 1f),
                    craftable = true,
                });
                addedBots++;
            }
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();

            if (missing.Count > 0)
                throw new System.Exception("Thiếu sprite: " + string.Join(", ", missing.ToArray()) + ". Chạy: python3 Tools/PixelArt/gen_skill.py rồi thử lại.");

            return "• Prefab Part (linh kiện) + ArtBank đã gán\n• " + cfg.nodes.Count + " kỹ năng trong SkillConfig (mới thêm " + addedNodes + ")\n• " +
                   cfg.craftables.Count + " loại quái chế tạo được (mới thêm " + addedBots + ")\n• Thư mục kỹ năng: " + SkillDir + "\n• Cấu hình: " + ResDir + "SkillConfig.asset";
        }

        // ---------------------------------------------------------------- Prefab linh kiện
        static void MakePartPrefab()
        {
            if (PrefabExists("Part")) return;
            var go = new GameObject("Part");
            go.layer = Cfg.LayerItem;
            var p = go.AddComponent<PartDrop>();
            p.restHeight = 0.4f;
            p.pickupRadius = 0.55f;
            Visual(go, "Visual", Vector2.zero, 46, "spin", Clip("spin", 8f, true, Fr("part_", 4)));
            Glow(go, "Glow", Vector2.zero, new Vector2(1.5f, 1.5f), A(new Color(0.6f, 0.9f, 1f), 0.35f), 45);
            SavePrefab(go, "Part");
        }

        // ---------------------------------------------------------------- 9 kỹ năng mẫu
        static SkillNodeData MakeSkillNode(string id, string nameVi, string nameEn, string descVi, string descEn, SkillBranch branch,
                                           SkillNodeData parent, int order, bool startUnlocked, int maxLevel, int costBase, int costGrowth,
                                           string icon, params SkillEffect[] effects)
        {
            string path = SkillDir + "Skill_" + id + ".asset";
            var n = AssetDatabase.LoadAssetAtPath<SkillNodeData>(path);
            if (n == null)
            {
                n = ScriptableObject.CreateInstance<SkillNodeData>();
                n.id = id;
                n.nameVi = nameVi; n.nameEn = nameEn;
                n.descVi = descVi; n.descEn = descEn;
                n.branch = branch;
                n.parent = parent;
                n.order = order;
                n.startUnlocked = startUnlocked;
                n.maxLevel = maxLevel;
                n.upgradeCostBase = costBase;
                n.upgradeCostGrowth = costGrowth;
                n.icon = Spr(icon);
                n.effects = new List<SkillEffect>(effects);
                n.conditions = new List<UnlockCondition>();
                if (!startUnlocked)
                    n.conditions.Add(new UnlockCondition
                    {
                        type = UnlockType.Manual,
                        noteVi = "Điều kiện mở khoá sẽ được bổ sung sau",
                        noteEn = "Unlock condition will be added later",
                    });
                AssetDatabase.CreateAsset(n, path);
            }
            else if (n.icon == null)
            {
                n.icon = Spr(icon);
                EditorUtility.SetDirty(n);
            }
            return n;
        }

        static SkillEffect Ef(SkillStat s, float perLevel) { return new SkillEffect { stat = s, perLevel = perLevel }; }

        static List<SkillNodeData> MakeSkillNodes()
        {
            var list = new List<SkillNodeData>();
            const SkillBranch P = SkillBranch.Passive, Ac = SkillBranch.Active;

            // ---------------- nhánh BỊ ĐỘNG
            var parts = MakeSkillNode(SkillSystem.IdParts, "Thu thập linh kiện", "Scavenge Parts",
                "Khi bạn kết liễu quái, quái rơi ra linh kiện. Gom đủ linh kiện để chế tạo lại loại quái đã từng bị hạ thành minibot (mục Chế tạo).",
                "When you finish off an enemy it drops parts. Collect enough to craft any enemy type you have defeated into a minibot.",
                P, null, 0, true, 1, 0, 0, "skill_parts");
            var scav = MakeSkillNode("passive_scavenger", "Nhặt nhạnh", "Scavenger",
                "Quái rơi nhiều linh kiện hơn mỗi cấp.", "Enemies drop more parts per level.",
                P, parts, 1, false, 5, 60, 40, "skill_scavenger", Ef(SkillStat.PartsDropPct, 20f));
            var eff = MakeSkillNode("passive_efficient", "Chế tạo tiết kiệm", "Efficient Crafting",
                "Giảm số linh kiện cần để chế tạo minibot mỗi cấp.", "Fewer parts are needed to craft a minibot per level.",
                P, parts, 2, false, 5, 80, 50, "skill_efficient", Ef(SkillStat.CraftCostPct, -6f));

            // ---------------- nhánh CHỦ ĐỘNG
            var bot = MakeSkillNode(SkillSystem.IdMinibot, "Triệu hồi minibot", "Summon Minibot",
                "Có ít nhất 1 minibot thì bấm H mở bảng chọn, bấm vào minibot để triệu hồi nó cùng chiến đấu. Sau 5 giây hết giao tranh, bấm vào minibot để thu hồi.",
                "With at least 1 minibot press H to open the selection panel and click a minibot to summon it. Five seconds out of combat, click it to recall it.",
                Ac, null, 0, true, 1, 0, 0, "skill_minibot");
            var squad = MakeSkillNode("active_squad", "Biên đội", "Squad",
                "Triệu hồi thêm 1 minibot cùng lúc mỗi cấp.", "Summon 1 more minibot at the same time per level.",
                Ac, bot, 1, false, 2, 120, 100, "skill_squad", Ef(SkillStat.MaxSummoned, 1f));
            var slots = MakeSkillNode("active_slots", "Bảng chọn mở rộng", "Wider Panel",
                "Thêm 1 ô cho bảng chọn minibot (phím H) mỗi cấp.", "Adds 1 slot to the minibot panel (key H) per level.",
                Ac, bot, 2, false, 4, 70, 50, "skill_slots", Ef(SkillStat.PanelSlots, 1f));
            var tact = MakeSkillNode("active_tactics", "Rút lui chiến thuật", "Tactical Retreat",
                "Rút ngắn thời gian chờ sau giao tranh trước khi thu hồi được minibot.", "Shortens the out-of-combat wait before you can recall a minibot.",
                Ac, bot, 3, false, 3, 90, 60, "skill_tactics", Ef(SkillStat.RecallDelayPct, -15f));
            var grow = MakeSkillNode("active_growth", "Huấn luyện", "Training",
                "Minibot nhận nhiều độ trưởng thành hơn khi hỗ trợ và tiêu diệt quái.", "Minibots gain more maturity from assists and kills.",
                Ac, bot, 4, false, 5, 80, 50, "skill_growth", Ef(SkillStat.MaturityGainPct, 20f));
            var rep = MakeSkillNode("active_repair", "Sửa chữa nhanh", "Quick Repair",
                "Minibot hồi máu trong lưu trữ nhanh hơn.", "Minibots heal faster while in storage.",
                Ac, bot, 5, false, 4, 70, 40, "skill_repair", Ef(SkillStat.StorageHealPct, 25f));

            list.AddRange(new[] { parts, scav, eff, bot, squad, slots, tact, grow, rep });
            return list;
        }
    }
}
#endif
