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
    /// Menu "Cập nhật Nhiệm vụ + Phố Neon + Lưu game (giữ nguyên chỉnh sửa của bạn)":
    ///   • 2 nguyên liệu mới (Thỏi sắt, Sợi cacbon) + danh sách ItemDatabase (cho file lưu game);
    ///   • Scene Base có sẵn: chỉ THÊM (nếu chưa có) BaseStash lên giá kệ "Shelf" + Hộp thư nhiệm vụ (Mailbox) — không đụng object nào khác;
    ///   • tạo Scene còn thiếu: MainMenu (menu chính), Tutorial (huấn luyện), PhoNeon (Phố Neon), RiaPhoNeon (Rìa Phố Neon), Story (cốt truyện mở đầu 10 cảnh) — Scene đã có thì GIỮ NGUYÊN;
    ///   • prefab NPC đi lại (người, robot, quái vô hại) — đã có thì giữ nguyên;
    ///   • thêm các Scene vào Build Settings, MainMenu đứng đầu.
    /// Chạy nhiều lần vẫn an toàn. Muốn dựng lại một Scene từ đầu: xoá file .unity của nó rồi chạy lại menu này.
    /// </summary>
    public static partial class NeonBuilder
    {
        static string MenuScenePath { get { return SceneDir + "MainMenu.unity"; } }
        static string TutorialScenePath { get { return SceneDir + "Tutorial.unity"; } }
        static string PhoNeonScenePath { get { return SceneDir + "PhoNeon.unity"; } }
        static string RimScenePath { get { return SceneDir + "RiaPhoNeon.unity"; } }

        // ================================================================ MENU
        [MenuItem("Tools/Neon Breaker/Cập nhật Nhiệm vụ + Phố Neon + Lưu game (giữ nguyên chỉnh sửa của bạn)", false, 17)]
        public static void UpdateStoryContent()
        {
            root = null;
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Neon Breaker", "Đang ở chế độ Play. Hãy dừng Play rồi chạy lại.", "OK");
                return;
            }
            if (!File.Exists(PrefabDir + "Player.prefab"))
            {
                EditorUtility.DisplayDialog("Neon Breaker", "Chưa có Prefab Player. Hãy chạy Build Project trước.", "OK");
                return;
            }
            try
            {
                Prepare();
                var sb = new StringBuilder();
                sb.AppendLine(StoryItems());
                sb.AppendLine(StoryItemDatabase());
                sb.AppendLine(StoryNpcPrefabs());
                sb.AppendLine(StoryBase());
                sb.AppendLine(EnsureStoryScene(MenuScenePath, "MainMenu", BuildMenuScene));
                sb.AppendLine(EnsureStoryScene(TutorialScenePath, "Tutorial", BuildTutorialScene));
                sb.AppendLine(EnsureStoryScene(PhoNeonScenePath, "PhoNeon", BuildPhoNeonScene));
                sb.AppendLine(EnsureStoryScene(RimScenePath, "RiaPhoNeon", BuildRimScene));
                sb.AppendLine(EnsureStoryScene(StoryScenePath, "Story", BuildStoryScene));
                sb.AppendLine(StoryBuildSettings());
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                string report = sb.ToString().TrimEnd();
                Debug.Log("Neon Breaker: " + report);
                EditorUtility.DisplayDialog("Neon Breaker", report, "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Neon Breaker: lỗi khi cập nhật nhiệm vụ / Phố Neon - " + e);
                EditorUtility.DisplayDialog("Neon Breaker", "Lỗi: " + e.Message + "\n(xem Console)", "OK");
            }
        }

        // ================================================================ NGUYÊN LIỆU + DATABASE
        static string StoryItems()
        {
            Directory.CreateDirectory(ItemDir.TrimEnd('/'));
            MakeItem("iron_ingot", "Thỏi sắt", "Iron Ingot",
                "Thỏi sắt đúc sẵn. Albert đòi mang về để tái tạo cơ thể cho bạn.", "A cast iron ingot. Albert wants it back at the base to rebuild your body.",
                ItemKind.Material, 99, 8, new Color(0.70f, 0.78f, 0.92f, 1f), null);
            MakeItem("carbon_fiber", "Sợi cacbon", "Carbon Fiber",
                "Cuộn sợi cacbon nhẹ mà bền. Nguyên liệu hồi sinh Albert cần.", "A light yet tough spool of carbon fiber. A resurrection material Albert needs.",
                ItemKind.Material, 99, 10, new Color(0.45f, 0.55f, 0.85f, 1f), null);
            if (missing.Count > 0)
                throw new System.Exception("Thiếu sprite: " + string.Join(", ", missing.ToArray()) + ". Chạy: python3 Tools/PixelArt/gen_story.py rồi thử lại.");
            return "• Nguyên liệu Thỏi sắt (Item_iron_ingot) + Sợi cacbon (Item_carbon_fiber): đã có / đã tạo (giữ nguyên nếu bạn đã chỉnh)";
        }

        static string StoryItemDatabase()
        {
            Directory.CreateDirectory(ResDir.TrimEnd('/'));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            var db = LoadOrCreate<ItemDatabase>(ResDir + "ItemDatabase.asset");
            var set = new List<ItemData>();
            foreach (var g in AssetDatabase.FindAssets("t:ItemData"))
            {
                var it = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(g));
                if (it != null && !set.Contains(it)) set.Add(it);
            }
            foreach (var old in db.items) if (old != null && !set.Contains(old)) set.Add(old);
            db.items = set;
            EditorUtility.SetDirty(db);
            return "• ItemDatabase (Assets/Resources/NeonBreaker/ItemDatabase.asset): " + set.Count + " vật phẩm — file lưu game tra vật phẩm bằng danh sách này. Thêm vật phẩm mới thì chạy lại menu này";
        }

        // ================================================================ PREFAB NPC
        static void MakeNpcPrefab(string name, string walkPrefix, int walkN, string idlePrefix, int idleN, float walkFps, float speed)
        {
            if (PrefabExists(name)) return;
            var go = new GameObject(name);
            var n = go.AddComponent<CityNpc>();
            n.speed = speed;
            var walk = Clip("walk", walkFps, true, Fr(walkPrefix, walkN));
            var idle = idlePrefix != null ? Clip("idle", 2f, true, Fr(idlePrefix, idleN)) : Clip("idle", 1f, true, Fr(walkPrefix, 1));
            Visual(go, "Visual", Vector2.zero, 16, "idle", idle, walk);
            if (missing.Count > 0)
            {
                Object.DestroyImmediate(go);
                throw new System.Exception("Thiếu sprite: " + string.Join(", ", missing.ToArray()) + ". Chạy: python3 Tools/PixelArt/gen_places.py (Scene Story: gen_opening.py), để Unity nhập ảnh xong rồi thử lại.");
            }
            SavePrefab(go, name);
        }

        static string StoryNpcPrefabs()
        {
            missing.Clear();
            MakeNpcPrefab("Npc_Human1", "npc_h1_walk_", 4, "npc_h1_idle_", 2, 8f, 1.5f);
            MakeNpcPrefab("Npc_Human2", "npc_h2_walk_", 4, "npc_h2_idle_", 2, 8f, 1.7f);
            MakeNpcPrefab("Npc_Human3", "npc_h3_walk_", 4, "npc_h3_idle_", 2, 8f, 1.3f);
            MakeNpcPrefab("Npc_Raider", "npc_raider_walk_", 4, "npc_raider_idle_", 2, 7f, 1.0f);
            MakeNpcPrefab("Npc_BotA", "npc_bot_a_roll_", 2, null, 0, 6f, 1.8f);
            MakeNpcPrefab("Npc_BotB", "npc_bot_b_walk_", 4, "npc_bot_b_idle_", 2, 7f, 1.1f);
            MakeNpcPrefab("Npc_Walker", "walker_walk_", 4, null, 0, 6f, 1.1f);
            MakeNpcPrefab("Npc_Rusher", "rusher_walk_", 4, null, 0, 7f, 1.5f);
            MakeNpcPrefab("Npc_Heavy", "heavy_walk_", 4, null, 0, 5f, 0.8f);
            MakeNpcPrefab("Npc_Flyer", "flyer_fly_", 2, null, 0, 8f, 1.4f);
            return "• Prefab NPC đi lại (Npc_Human1..3, Npc_Raider, Npc_BotA/B, Npc_Walker/Rusher/Heavy/Flyer): đã có / đã tạo";
        }

        // ================================================================ SCENE BASE: kho + hộp thư
        /// <summary>Hộp thư nhiệm vụ: cột + hộp neon + cờ báo. Mặc định ẩn tới khi Albert giao nhiệm vụ lần đầu (MissionBoard lo việc đó).</summary>
        static GameObject MakeMailbox(float x)
        {
            var go = new GameObject("Mailbox");
            go.transform.position = new Vector3(x, 0f, 0f);
            var mb = go.AddComponent<MissionBoard>();
            AddSr(Child(go, "Visual", Vector2.zero), Spr("prop_mailbox"), 10, false, Color.white);
            Glow(go, "Glow", new Vector2(0f, 1.4f), new Vector2(3.4f, 3.4f), A(Cfg.Cyan, 0.22f), 11);
            var flag = Child(go, "Flag", new Vector2(0.7f, 2.05f));
            AddSr(flag, Spr("prop_mailbox_flag"), 11, false, Color.white);
            mb.flag = flag.transform;
            return go;
        }

        static string StoryBase()
        {
            if (!File.Exists(BaseScenePath))
                return "• Scene Base: chưa có — chạy menu \"Cập nhật cấp bậc + dầu + căn cứ\" trước rồi chạy lại menu này để gắn kho + hộp thư";
            Scene sc = default(Scene);
            bool wasOpen = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).path == BaseScenePath) { sc = SceneManager.GetSceneAt(i); wasOpen = true; }
            if (!wasOpen) sc = EditorSceneManager.OpenScene(BaseScenePath, OpenSceneMode.Additive);

            string report;
            bool changed = false;
            missing.Clear();
            try { report = AddStoryToBase(sc, out changed); }
            finally
            {
                if (!wasOpen)
                {
                    if (changed) EditorSceneManager.SaveScene(sc);
                    EditorSceneManager.CloseScene(sc, true);
                }
            }
            if (wasOpen && changed)
            {
                EditorSceneManager.MarkSceneDirty(sc);
                report += "\n  (Scene Base đang mở: nhớ lưu bằng Ctrl+S.)";
            }
            return report;
        }

        static string AddStoryToBase(Scene sc, out bool changed)
        {
            changed = false;
            var sb = new StringBuilder();
            Transform props = null, shelf = null;
            bool hasStash = false, hasBoard = false;
            foreach (var r in sc.GetRootGameObjects())
            {
                if (r.name == "Props") props = r.transform;
                if (r.GetComponentInChildren<BaseStash>(true) != null) hasStash = true;
                if (r.GetComponentInChildren<MissionBoard>(true) != null) hasBoard = true;
                if (shelf == null)
                    foreach (var t in r.GetComponentsInChildren<Transform>(true))
                        if (t.name == "Shelf") { shelf = t; break; }
            }

            if (hasStash) sb.Append("• Scene Base đã có kho căn cứ (BaseStash) — giữ nguyên");
            else if (shelf != null)
            {
                shelf.gameObject.AddComponent<BaseStash>();
                changed = true;
                sb.Append("• Đã gắn kho căn cứ (BaseStash) lên object \"Shelf\" (giá kệ) — đứng gần bấm F");
            }
            else sb.Append("• Không thấy object \"Shelf\" trong Scene Base nên chưa gắn được kho — gắn component BaseStash vào object bạn muốn dùng làm kho");

            if (hasBoard) sb.Append("\n• Scene Base đã có Hộp thư nhiệm vụ (MissionBoard) — giữ nguyên, bạn tự chỉnh vị trí");
            else
            {
                var mb = MakeMailbox(12.2f);
                if (missing.Count > 0)
                {
                    Object.DestroyImmediate(mb);
                    throw new System.Exception("Thiếu sprite: " + string.Join(", ", missing.ToArray()) + ". Chạy: python3 Tools/PixelArt/gen_story.py rồi thử lại.");
                }
                SceneManager.MoveGameObjectToScene(mb, sc);
                if (props != null) mb.transform.SetParent(props, true);
                changed = true;
                sb.Append("\n• Đã thêm Hộp thư nhiệm vụ \"Mailbox\" (x = 12.2) — ẩn tới khi nói chuyện xong với Albert lần đầu");
            }
            return sb.ToString();
        }

        // ================================================================ DỰNG SCENE MỚI (chỉ khi file chưa có)
        delegate void SceneBuilder(Scene scene, string path);

        static string EnsureStoryScene(string path, string label, SceneBuilder build)
        {
            if (File.Exists(path))
            {
                AddSceneToBuildSettings(path);
                return "• Scene " + label + " đã có — giữ nguyên chỉnh sửa của bạn";
            }
            if (IsSceneOpen(path)) return "• Scene " + label + " đang mở nên chưa dựng được";
            missing.Clear();
            Directory.CreateDirectory(SceneDir.TrimEnd('/'));
            var prev = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try { build(scene, path); }
            finally
            {
                if (prev.IsValid() && prev.isLoaded) SceneManager.SetActiveScene(prev);
                EditorSceneManager.CloseScene(scene, true);
            }
            AddSceneToBuildSettings(path);
            return "• Đã dựng Scene " + label + ": " + path;
        }

        static void FinishScene(Scene scene, string path)
        {
            if (missing.Count > 0)
                throw new System.Exception("Thiếu sprite: " + string.Join(", ", missing.ToArray()) + ". Chạy: python3 Tools/PixelArt/gen_places.py (Scene Story: gen_opening.py), để Unity nhập ảnh xong rồi thử lại.");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, path)) throw new System.Exception("Không lưu được " + path);
        }

        /// <summary>Camera pixel + hệ thống (GameManager, Hud, Sfx, Fx), giống các Scene khác. Không chạy cảnh mở đầu Dyber ở các Scene này.</summary>
        static CameraRig StoryCamera(Color bg, float camX, float camY = 5f, bool withRig = true)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(camX, camY, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 270f / (2f * 16f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bg;
            cam.nearClipPlane = -50f;
            cam.farClipPlane = 50f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<PixelScreen>();
            var rig = withRig ? camGo.AddComponent<CameraRig>() : null;      // Scene Story không dùng CameraRig (StoryIntro tự điều khiển camera)

            var sys = new GameObject("Systems");
            var gm = sys.AddComponent<GameManager>();
            gm.bossPrefab = Prefab("Boss");
            gm.playIntro = false;
            sys.AddComponent<Hud>();
            var sfx = new GameObject("Sfx"); sfx.transform.SetParent(sys.transform, false); sfx.AddComponent<Sfx>();
            var fx = new GameObject("Fx"); fx.transform.SetParent(sys.transform, false); fx.AddComponent<Fx>();
            return rig;
        }

        static void BuildMenuScene(Scene scene, string path)
        {
            StoryCamera(new Color(0.02f, 0.02f, 0.06f, 1f), 0f);
            var go = new GameObject("MainMenu");
            go.AddComponent<MainMenuScreen>();
            FinishScene(scene, path);
        }

        // ------------------------------------------------------------ tiện ích dựng chung
        static void MakeLimits(Transform parent, float leftX, float rightX, out Transform left, out Transform right)
        {
            var marks = Group(parent, "Marks");
            left = Group(marks, "LimitLeft"); left.position = new Vector3(leftX, 0f, 0f);
            right = Group(marks, "LimitRight"); right.position = new Vector3(rightX, 0f, 0f);
            foreach (var wall in new[] { left, right })
            {
                var wc = wall.gameObject.AddComponent<BoxCollider2D>();
                wc.size = new Vector2(1f, 60f);
                wc.offset = new Vector2(wall == left ? -0.5f : 0.5f, 20f);
            }
        }

        static void MakeGround(Transform parent, string topSprite, string fillSprite, float centerX, float width)
        {
            var ground = Group(parent, "Ground");
            ground.position = new Vector3(centerX, 0f, 0f);
            var top = Tiled(ground, "Top", topSprite, new Vector2(centerX, -0.5f), new Vector2(width, 1f), 5);
            var fill = Tiled(ground, "Fill", fillSprite, new Vector2(centerX, -5f), new Vector2(width, 8f), 4);
            var gc = ground.gameObject.AddComponent<BoxCollider2D>();
            gc.size = new Vector2(width, 9f);
            gc.offset = new Vector2(0f, -4.5f);
            top.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            fill.transform.localPosition = new Vector3(0f, -5f, 0f);
        }

        static Parallax ParallaxLayer(Transform parent, string name, string sprite, float factor, int order, float darken, float centerX, float width)
        {
            var sr = Tiled(parent, name, sprite, new Vector2(centerX, -1f), new Vector2(width, sr0Height(sprite)), order);
            var p = sr.gameObject.AddComponent<Parallax>();
            p.factorX = factor;
            p.darken = darken;
            return p;
        }

        static void SkyLayer(Transform parent, string sprite, float darken)
        {
            var sky = Tiled(parent, "Sky", sprite, Vector2.zero, new Vector2(480f / 16f, 270f / 16f), -100);
            var p = sky.gameObject.AddComponent<Parallax>();
            p.factorX = 1f; p.followY = true; p.yOffset = 0f; p.darken = darken;
        }

        static Transform PropAnimAt(Transform parent, string name, string prefix, int frames, float fps, Vector2 pos, int order)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            holder.transform.position = new Vector3(pos.x, pos.y, 0f);
            Visual(holder, "Visual", Vector2.zero, order, "idle", Clip("idle", fps, true, Fr(prefix, frames)));
            return holder.transform;
        }

        static GameObject PlaceNpc(Transform parent, string prefab, float x, float y, float minX, float maxX)
        {
            var go = Inst(prefab, parent, new Vector2(x, y));
            var n = go.GetComponent<CityNpc>();
            if (n != null) { n.minX = Mathf.Max(3f, x - 20f); n.maxX = Mathf.Min(maxX - 3f, x + 20f); if (n.maxX <= n.minX) { n.minX = 0f; n.maxX = 0f; } }
            return go;
        }

        static ItemData StoryItem(string id) { return AssetDatabase.LoadAssetAtPath<ItemData>(ItemDir + "Item_" + id + ".asset"); }

        static void AddLoot(List<ScatterEntry> list, string id, float weight, int min, int max)
        {
            var it = StoryItem(id);
            if (it != null) list.Add(new ScatterEntry { item = it, weight = weight, minCount = min, maxCount = max });
        }

        /// <summary>Cổng về căn cứ (bấm F). Dùng lại hình cửa của căn cứ.</summary>
        static GameObject MakeBaseGate(Transform parent, float x)
        {
            var go = new GameObject("BaseGate");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, 0f, 0f);
            var g = go.AddComponent<PlaceGate>();
            g.kind = GateKind.ToBase;
            g.promptOffset = new Vector2(0f, 5.1f);
            g.glowColor = new Color(0.45f, 1f, 0.4f, 1f);
            AddSr(Child(go, "Visual", Vector2.zero), Spr("station_door"), 10, false, Color.white);
            g.glow = Glow(go, "Glow", new Vector2(0f, 2.2f), new Vector2(3.6f, 5f), A(g.glowColor, 0.25f), 11);
            return go;
        }

        // ================================================================ MÀN HƯỚNG DẪN
        static void BuildTutorialScene(Scene scene, string path)
        {
            var rig = StoryCamera(new Color(0.02f, 0.02f, 0.06f, 1f), 3f);
            const float L = -2f, R = 150f;

            Transform left, right;
            MakeLimits(null, L, R, out left, out right);

            var bg = Group(null, "Background");
            SkyLayer(bg, "bg_sky", 0.45f);
            ParallaxLayer(bg, "Far", "bg_far", 0.15f, -90, 0.45f, 80f, 240f);
            ParallaxLayer(bg, "Mid", "bg_mid", 0.35f, -80, 0.45f, 80f, 240f);
            ParallaxLayer(bg, "Near", "bg_near", 0.6f, -70, 0.45f, 80f, 240f);

            var env = Group(null, "Environment");
            MakeGround(env, "tile_ground_top", "tile_ground_fill", 74f, 170f);
            var plats = Group(env, "Platforms");
            Platform(plats, "tile_plat", 30f, 2.35f, 5f, "Platform_0");
            Platform(plats, "tile_plat", 38.5f, 2.35f, 5f, "Platform_1");

            // mốc các bước
            var marks = Group(null, "TutorialMarks");
            var moveGoal = Group(marks, "MoveGoal"); moveGoal.position = new Vector3(20f, 0f, 0f);
            var jumpGoal = Group(marks, "JumpGoal"); jumpGoal.position = new Vector3(38.5f, 2.6f, 0f);

            // biển chỉ đường nhấp nháy
            var props = Group(null, "Props");
            PropAnimAt(props, "Sign_Move", "tut_sign_", 2, 2.5f, new Vector2(20f, 0f), 11);
            PropAnimAt(props, "Sign_Jump", "tut_sign_", 2, 2.5f, new Vector2(38.5f, 2.6f), 11);
            PropAnimAt(props, "Sign_Fight", "tut_sign_", 2, 2.5f, new Vector2(58f, 0f), 11);
            Prop(props, "Crates_A", "prop_crate", 104f, 10);
            PropAnim(props, "Monitors", "prop_monitors_", 2, 2f, 109f, 8);
            PropAnim(props, "HologramTable", "prop_holo_", 2, 2.5f, 115f, 8);
            PropAnim(props, "Generator", "prop_generator_", 2, 6f, 123f, 8);
            Prop(props, "Barrel_A", "prop_barrel", 128f, 10);

            // quái luyện tập (đặt sẵn; hạ hết thì rào chắn mở)
            var enemyGroup = Group(null, "TrainingEnemies");
            var enemies = new List<Enemy>();
            string[] ep = { "Walker", "Walker", "Rusher", "Flyer" };
            float[] ex = { 66f, 74f, 80f, 86f };
            for (int i = 0; i < ep.Length; i++)
            {
                var proto = Prefab(ep[i]);
                if (proto == null) throw new System.Exception("Chưa có Prefab " + ep[i] + ". Hãy chạy Build Project trước.");
                float h = proto.GetComponent<Enemy>() != null ? proto.GetComponent<Enemy>().SpawnHeight : 0.05f;
                var go = Inst(ep[i], enemyGroup, new Vector2(ex[i], h));
                go.name = ep[i] + "_" + (i + 1);
                var en = go.GetComponent<Enemy>();
                if (en != null) enemies.Add(en);
            }

            // rào chắn sau khu quái (giống cổng boss ở Main)
            const float gx = 92f;
            var gate = Group(null, "FightBarrier");
            gate.position = new Vector3(gx, 0f, 0f);
            var gcol = gate.gameObject.AddComponent<BoxCollider2D>();
            gcol.size = new Vector2(0.8f, 18f);
            gcol.offset = new Vector2(0f, 9f);
            for (int i = 0; i < 3; i++) Plain(gate, "Post" + i, "gate_post", new Vector2(gx, 3.5f * i), 8);
            var bar = Tiled(gate, "Bar", "gate_bar", new Vector2(gx, 8f), new Vector2(0.375f, 16f), 7);
            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(gate, false);
            glowGo.transform.position = new Vector3(gx, 8f, 0f);
            glowGo.transform.localScale = new Vector3(3f, 18f, 1f);
            var gglow = AddSrScene(glowGo, Spr("fx_glow"), 6, new Color(1f, 0.15f, 0.8f, 0.45f));

            // cánh cửa cuối đường
            var doorGo = new GameObject("TutorialDoor");
            doorGo.transform.position = new Vector3(140f, 0f, 0f);
            var door = doorGo.AddComponent<PlaceGate>();
            door.kind = GateKind.EndTutorial;
            door.promptOffset = new Vector2(0f, 5.2f);
            door.glowColor = new Color(0.1f, 0.95f, 1f, 1f);
            Visual(doorGo, "Visual", Vector2.zero, 10, "idle", Clip("idle", 2f, true, Fr("tut_door_", 2)));
            door.glow = Glow(doorGo, "Glow", new Vector2(0f, 2f), new Vector2(4.4f, 5.2f), A(door.glowColor, 0.25f), 11);

            // người chơi
            var player = Inst("Player", null, new Vector2(3f, 0.05f));
            rig.target = player.transform;

            // bộ điều khiển
            var tgo = new GameObject("Tutorial");
            var tm = tgo.AddComponent<TutorialManager>();
            tm.limitLeft = left;
            tm.limitRight = right;
            tm.moveGoal = moveGoal;
            tm.jumpGoal = jumpGoal;
            tm.enemies = enemies.ToArray();
            tm.gateCollider = gcol;
            tm.gateBar = bar;
            tm.gateGlow = gglow;
            tm.door = door;

            FinishScene(scene, path);
        }

        // ================================================================ PHỐ NEON
        static void BuildPhoNeonScene(Scene scene, string path)
        {
            var rig = StoryCamera(new Color(0.02f, 0.01f, 0.05f, 1f), 8f);
            const float L = 0f, R = 200f;
            Transform left, right;
            MakeLimits(null, L, R, out left, out right);

            var bg = Group(null, "Background");
            SkyLayer(bg, "city_sky", 0.25f);
            ParallaxLayer(bg, "Far", "city_far", 0.15f, -90, 0.3f, 100f, 280f);
            ParallaxLayer(bg, "Mid", "city_mid", 0.35f, -80, 0.25f, 100f, 280f);

            var env = Group(null, "Environment");
            MakeGround(env, "city_floor_top", "city_floor_fill", 100f, 230f);

            // toà nhà cao tầng: hai hàng (hàng sau lớn hơn, tối hơn) — chỉ để trang trí
            var blds = Group(null, "Buildings");
            string[] bn = { "city_bld_a", "city_bld_b", "city_bld_c", "city_bld_d" };
            for (int i = 0; i < 16; i++)
            {
                float x = 11f + i * 12.8f + ((i * 7) % 5 - 2);
                var back = Plain(blds, "BuildingBack_" + i, bn[(i + 2) % 4], new Vector2(x + 6f, 0f), -65);
                back.transform.localScale = new Vector3(1.35f, 1.35f, 1f);
                back.color = new Color(0.5f, 0.5f, 0.62f, 1f);
                Plain(blds, "Building_" + i, bn[i % 4], new Vector2(x, 0f), -60);
            }

            // biển neon, bảng quảng cáo, đèn đường, máy bán hàng
            var props = Group(null, "Props");
            string[] sg = { "city_sign_a_", "city_sign_b_", "city_sign_c_" };
            for (int i = 0; i < 16; i++)
            {
                float x = 11f + i * 12.8f + ((i * 7) % 5 - 2);
                if (i % 2 == 0) PropAnimAt(props, "NeonSign_" + i, sg[(i / 2) % 3], 2, 2f + (i % 3), new Vector2(x + 3.4f, 4.6f + (i % 3) * 1.2f), -55);
                if (i % 3 == 1) PropAnimAt(props, "Billboard_" + i, "city_billboard_", 2, 1.5f, new Vector2(x - 0.5f, 7.2f + (i % 2) * 2f), -55);
            }
            for (int i = 0; i < 11; i++)
            {
                float x = 18f + i * 18.5f;
                var lamp = new GameObject("Lamp_" + i);
                lamp.transform.SetParent(props, false);
                lamp.transform.position = new Vector3(x, 0f, 0f);
                AddSr(Child(lamp, "Visual", Vector2.zero), Spr("city_lamp"), 9, false, Color.white);
                Glow(lamp, "Glow", new Vector2(0.35f, 3.2f), new Vector2(4.2f, 4.2f), A(new Color(1f, 0.85f, 0.5f), 0.3f), 10);
            }
            foreach (float vx in new[] { 33f, 78f, 123f, 168f })
            {
                var v = new GameObject("Vending_" + vx);
                v.transform.SetParent(props, false);
                v.transform.position = new Vector3(vx, 0f, 0f);
                AddSr(Child(v, "Visual", Vector2.zero), Spr("city_vending"), 10, false, Color.white);
                Glow(v, "Glow", new Vector2(0f, 1f), new Vector2(2.6f, 2.8f), A(Cfg.Cyan, 0.2f), 11);
            }
            Prop(props, "Barrel_A", "prop_barrel", 57f, 10);
            Prop(props, "Crates_A", "prop_crate", 96f, 10);
            Prop(props, "Crates_B", "prop_crate", 146f, 10);

            // NPC đi lại (không ai gây sự): người, robot, cả quái vô hại
            var npcs = Group(null, "Npcs");
            string[] np = { "Npc_Human1", "Npc_Human2", "Npc_Human3", "Npc_BotA", "Npc_BotB", "Npc_Human1", "Npc_Human2", "Npc_Human3", "Npc_BotA", "Npc_Walker",
                            "Npc_Human2", "Npc_BotB", "Npc_Human1", "Npc_Human3", "Npc_Rusher", "Npc_Human2", "Npc_BotA", "Npc_Human1", "Npc_Heavy", "Npc_BotB",
                            "Npc_Human3", "Npc_Human1" };
            for (int i = 0; i < np.Length; i++)
            {
                float x = 14f + i * 8.4f;
                PlaceNpc(npcs, np[i], x, 0.05f, L, R);
            }
            PlaceNpc(npcs, "Npc_Flyer", 64f, 2.6f, L, R);
            PlaceNpc(npcs, "Npc_Flyer", 150f, 3.0f, L, R);

            MakeBaseGate(props, 3f);

            var player = Inst("Player", null, new Vector2(8f, 0.05f));
            rig.target = player.transform;

            var pgo = new GameObject("PlaceManager");
            var pm = pgo.AddComponent<PlaceManager>();
            pm.placeId = Places.Neon;
            pm.limitLeft = left;
            pm.limitRight = right;
            pm.oilCans = 4;
            pm.lootCount = 12;
            AddLoot(pm.loot, "iron_ingot", 30f, 1, 2);
            AddLoot(pm.loot, "carbon_fiber", 26f, 1, 3);
            AddLoot(pm.loot, "scrap_metal", 20f, 1, 3);
            AddLoot(pm.loot, "copper_wire", 14f, 1, 2);
            AddLoot(pm.loot, "circuit_board", 8f, 1, 1);
            AddLoot(pm.loot, "energy_gel", 10f, 1, 2);
            pm.moteA = Cfg.Cyan;
            pm.moteB = Cfg.Magenta;

            FinishScene(scene, path);
        }

        // ================================================================ RÌA PHỐ NEON
        static void BuildRimScene(Scene scene, string path)
        {
            var rig = StoryCamera(new Color(0.03f, 0.03f, 0.02f, 1f), 8f);
            const float L = 0f, R = 200f;
            Transform left, right;
            MakeLimits(null, L, R, out left, out right);

            var bg = Group(null, "Background");
            SkyLayer(bg, "rim_sky", 0.35f);
            ParallaxLayer(bg, "Far", "rim_far", 0.15f, -90, 0.35f, 100f, 280f);
            ParallaxLayer(bg, "Mid", "rim_mid", 0.35f, -80, 0.3f, 100f, 280f);

            var env = Group(null, "Environment");
            MakeGround(env, "rim_floor_top", "rim_floor_fill", 100f, 230f);

            // ống xả của khu trên cao đổ xuống, nhỏ giọt chất thải
            var over = Group(null, "Overhead");
            float[] px = { 24f, 78f, 132f, 186f };
            foreach (float x in px)
            {
                Tiled(over, "Pipe_" + x, "base_pipe_h", new Vector2(x, 12.2f), new Vector2(26f, 0.5f), -20);
                PropAnimAt(over, "Drip_" + x, "rim_drip_", 2, 2.5f, new Vector2(x - 4f, 11.9f), -19);
                PropAnimAt(over, "DripB_" + x, "rim_drip_", 2, 2f, new Vector2(x + 6f, 11.9f), -19);
            }

            var props = Group(null, "Props");
            // đống rác công nghệ + ruồi nhặng
            string[] jn = { "rim_junk_a", "rim_junk_b", "rim_junk_c" };
            var flyFrames = new[] { Spr("rim_fly_0"), Spr("rim_fly_1") };
            for (int i = 0; i < 17; i++)
            {
                float x = 12f + i * 11.2f + ((i * 5) % 4 - 1);
                var holder = new GameObject("Junk_" + i);
                holder.transform.SetParent(props, false);
                holder.transform.position = new Vector3(x, 0f, 0f);
                var vis = Child(holder, "Visual", Vector2.zero);
                AddSr(vis, Spr(jn[i % 3]), 8, false, Color.white);
                if (i % 2 == 0)
                {
                    var swarm = Child(holder, "Flies", new Vector2(0f, 1.6f)).AddComponent<FlySwarm>();
                    swarm.frames = flyFrames;
                    swarm.count = 6 + (i % 3);
                }
            }
            // lều trại + thùng lửa
            float[] sx = { 30f, 76f, 118f, 164f };
            for (int i = 0; i < sx.Length; i++)
            {
                var shack = new GameObject("Shack_" + i);
                shack.transform.SetParent(props, false);
                shack.transform.position = new Vector3(sx[i], 0f, 0f);
                AddSr(Child(shack, "Visual", Vector2.zero), Spr("rim_shack"), 7, false, Color.white);
                Glow(shack, "Glow", new Vector2(0.4f, 1.2f), new Vector2(3.6f, 2.6f), A(Cfg.Orange, 0.14f), 8);
                var fire = PropAnimAt(props, "FireBarrel_" + i, "rim_fire_", 2, 6f, new Vector2(sx[i] + 3.9f, 0f), 10);
                Glow(fire.gameObject, "Glow", new Vector2(0f, 1.3f), new Vector2(3.4f, 3.4f), A(Cfg.Orange, 0.3f), 11);
            }
            // thùng độc hại
            float[] tx = { 19f, 52f, 88f, 101f, 141f, 177f };
            for (int i = 0; i < tx.Length; i++)
            {
                var b = new GameObject("ToxicBarrel_" + i);
                b.transform.SetParent(props, false);
                b.transform.position = new Vector3(tx[i], 0f, 0f);
                AddSr(Child(b, "Visual", Vector2.zero), Spr("rim_barrel_toxic"), 10, false, Color.white);
                Glow(b, "Glow", new Vector2(0f, 0.7f), new Vector2(2f, 1.6f), A(new Color(0.45f, 1f, 0.2f), 0.22f), 11);
                if (i % 2 == 1)
                {
                    var swarm = Child(b, "Flies", new Vector2(0f, 1.3f)).AddComponent<FlySwarm>();
                    swarm.frames = flyFrames;
                    swarm.count = 5;
                    swarm.radius = new Vector2(0.8f, 0.5f);
                }
            }
            Prop(props, "Crates_A", "prop_crate", 44f, 10);
            Prop(props, "Crates_B", "prop_crate", 110f, 10);
            Prop(props, "Barrel_A", "prop_barrel", 156f, 10);

            // NPC đeo mặt nạ mang vũ khí + vài người dân / robot / quái (không ai tấn công)
            var npcs = Group(null, "Npcs");
            string[] np = { "Npc_Raider", "Npc_Raider", "Npc_Human3", "Npc_Raider", "Npc_BotB", "Npc_Raider", "Npc_Walker", "Npc_Raider", "Npc_Human3", "Npc_Raider",
                            "Npc_Heavy", "Npc_Raider", "Npc_BotA", "Npc_Raider", "Npc_Human3", "Npc_Raider", "Npc_Rusher", "Npc_Raider" };
            for (int i = 0; i < np.Length; i++)
                PlaceNpc(npcs, np[i], 16f + i * 10.2f, 0.05f, L, R);
            PlaceNpc(npcs, "Npc_Flyer", 100f, 2.8f, L, R);

            MakeBaseGate(props, 3f);

            var player = Inst("Player", null, new Vector2(8f, 0.05f));
            rig.target = player.transform;

            var pgo = new GameObject("PlaceManager");
            var pm = pgo.AddComponent<PlaceManager>();
            pm.placeId = Places.Rim;
            pm.limitLeft = left;
            pm.limitRight = right;
            pm.oilCans = 5;
            pm.lootCount = 16;
            AddLoot(pm.loot, "iron_ingot", 35f, 1, 3);
            AddLoot(pm.loot, "carbon_fiber", 30f, 1, 3);
            AddLoot(pm.loot, "scrap_metal", 30f, 1, 4);
            AddLoot(pm.loot, "copper_wire", 15f, 1, 2);
            AddLoot(pm.loot, "circuit_board", 10f, 1, 2);
            AddLoot(pm.loot, "energy_gel", 8f, 1, 1);
            pm.moteA = new Color(0.55f, 0.50f, 0.28f, 1f);
            pm.moteB = new Color(0.45f, 0.75f, 0.25f, 1f);
            pm.moteSize = 0.4f;
            pm.moteRise = new Vector2(0.2f, 0.6f);
            pm.moteEvery = 0.07f;

            FinishScene(scene, path);
        }

        // ================================================================ BUILD SETTINGS
        static string StoryBuildSettings()
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            string[] ours = { MenuScenePath, TutorialScenePath, PhoNeonScenePath, RimScenePath, StoryScenePath };
            foreach (var p in ours)
            {
                if (!File.Exists(p)) continue;
                bool has = false;
                foreach (var s in list) if (s.path == p) { has = true; break; }
                if (!has) list.Add(new EditorBuildSettingsScene(p, true));
            }
            // MainMenu đứng đầu danh sách: game mở ra là vào menu chính
            int mi = list.FindIndex(s => s.path == MenuScenePath);
            if (mi > 0) { var m = list[mi]; list.RemoveAt(mi); list.Insert(0, m); }
            EditorBuildSettings.scenes = list.ToArray();
            return "• Build Settings: " + list.Count + " Scene" + (mi >= 0 ? " (MainMenu đứng đầu)" : "");
        }
    }
}
#endif
