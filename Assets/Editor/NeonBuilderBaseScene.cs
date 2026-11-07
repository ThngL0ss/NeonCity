#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonBreaker.EditorTools
{
    /// <summary>
    /// Tạo Scene Base (căn cứ ngầm của Albert) = Assets/NeonBreaker/Scenes/Base.unity.
    /// Dựng trong một Scene phụ rồi lưu ra file, nên Scene Main (và mọi Scene đang mở) KHÔNG bị đụng tới.
    /// Menu "Cập nhật cấp bậc + dầu + căn cứ" chỉ tạo Base.unity khi nó CHƯA có; muốn dựng lại từ đầu dùng menu "Tạo lại Scene Căn cứ".
    /// Menu "Thêm máy tính khổng lồ vào Scene Base" chỉ thêm 1 object (ResurrectionComputer) vào Base.unity có sẵn, không đụng object nào khác.
    /// </summary>
    public static partial class NeonBuilder
    {
        static string BaseScenePath { get { return SceneDir + "Base.unity"; } }

        // kích thước căn cứ (đơn vị thế giới)
        const float BaseLeft = 0f, BaseRight = 120f, BaseCeil = 11.5f;

        // ================================================================ MENU
        [MenuItem("Tools/Neon Breaker/Tạo lại Scene Căn cứ Base (GHI ĐÈ Base.unity)", false, 15)]
        public static void RebuildBaseScene()
        {
            root = null;
            if (!File.Exists(PrefabDir + "Player.prefab"))
            {
                EditorUtility.DisplayDialog("Neon Breaker", "Chưa có Prefab Player. Hãy chạy Build Project trước.", "OK");
                return;
            }
            if (IsSceneOpen(BaseScenePath))
            {
                EditorUtility.DisplayDialog("Neon Breaker", "Scene Base đang được mở. Hãy lưu và đóng nó (hoặc mở Scene khác) rồi chạy lại.", "OK");
                return;
            }
            if (File.Exists(BaseScenePath) && !EditorUtility.DisplayDialog("Neon Breaker",
                    "Sẽ DỰNG LẠI Scene Base và ghi đè " + BaseScenePath + ".\nMọi chỉnh sửa của bạn trên Scene Base sẽ mất (Scene Main không bị ảnh hưởng). Tiếp tục?", "Ghi đè", "Huỷ")) return;
            try
            {
                Prepare();
                string report = EnsureBaseScene(true);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Neon Breaker: " + report);
                EditorUtility.DisplayDialog("Neon Breaker", report, "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Neon Breaker: lỗi khi dựng Scene Base - " + e);
                EditorUtility.DisplayDialog("Neon Breaker", "Lỗi: " + e.Message + "\n(xem Console)", "OK");
            }
        }

        static bool IsSceneOpen(string path)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).path == path) return true;
            return false;
        }

        static void AddSceneToBuildSettings(string path)
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in list) if (s.path == path) return;
            list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        [MenuItem("Tools/Neon Breaker/Thêm máy tính khổng lồ vào Scene Base (giữ nguyên chỉnh sửa của bạn)", false, 16)]
        public static void AddComputerToBase()
        {
            root = null;
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Neon Breaker", "Đang ở chế độ Play. Hãy dừng Play rồi chạy lại.", "OK");
                return;
            }
            if (!File.Exists(BaseScenePath))
            {
                EditorUtility.DisplayDialog("Neon Breaker", "Chưa có Scene Base. Hãy chạy menu \"Cập nhật cấp bậc + dầu + căn cứ\" trước.", "OK");
                return;
            }
            try
            {
                Prepare();
                missing.Clear();

                // dùng luôn Scene Base nếu đang mở, không thì mở thêm (Additive) rồi đóng lại sau khi lưu
                Scene sc = default(Scene);
                bool wasOpen = false;
                for (int i = 0; i < SceneManager.sceneCount; i++)
                    if (SceneManager.GetSceneAt(i).path == BaseScenePath) { sc = SceneManager.GetSceneAt(i); wasOpen = true; }
                if (!wasOpen) sc = EditorSceneManager.OpenScene(BaseScenePath, OpenSceneMode.Additive);

                string report;
                bool changed = false;
                try { report = AddComputerToScene(sc, out changed); }
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
                    report += "\n(Scene Base đang mở: nhớ lưu bằng Ctrl+S.)";
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Neon Breaker: " + report);
                EditorUtility.DisplayDialog("Neon Breaker", report, "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Neon Breaker: lỗi khi thêm máy tính khổng lồ - " + e);
                EditorUtility.DisplayDialog("Neon Breaker", "Lỗi: " + e.Message + "\n(xem Console)", "OK");
            }
        }

        /// <summary>Thêm máy tính khổng lồ cạnh lồng hồi sinh của Scene sc (nếu chưa có). Không đụng bất kỳ object nào khác.</summary>
        static string AddComputerToScene(Scene sc, out bool changed)
        {
            changed = false;
            ResurrectionCapsule cap = null;
            BaseManager bm = null;
            Transform props = null;
            foreach (var r in sc.GetRootGameObjects())
            {
                if (r.GetComponentInChildren<ResurrectionComputer>(true) != null)
                    return "• Scene Base đã có máy tính khổng lồ (ResurrectionComputer) — giữ nguyên, bạn tự chỉnh vị trí / kích thước trong Scene.";
                if (cap == null) cap = r.GetComponentInChildren<ResurrectionCapsule>(true);
                if (bm == null) bm = r.GetComponentInChildren<BaseManager>(true);
                if (props == null && r.name == "Props") props = r.transform;
            }
            if (cap == null) return "• Không tìm thấy ResurrectionCapsule trong Scene Base nên chưa thêm được máy tính.";

            float limitLeft = bm != null && bm.limitLeft != null ? bm.limitLeft.position.x : BaseLeft;
            var go = MakeResurrectionComputer(cap, limitLeft);
            if (missing.Count > 0)
            {
                Object.DestroyImmediate(go);
                throw new System.Exception("Thiếu sprite: " + string.Join(", ", missing.ToArray()) + ". Chạy: python3 Tools/PixelArt/gen_bigpc.py rồi để Unity import lại.");
            }
            SceneManager.MoveGameObjectToScene(go, sc);
            if (props != null) go.transform.SetParent(props, true);
            changed = true;
            return "• Đã thêm máy tính khổng lồ \"ResurrectionComputer\" cạnh lồng hồi sinh (x = " + go.transform.position.x.ToString("0.0") + "), nối bằng 2 bó dây cáp.\n" +
                   "  Chỉnh vị trí: kéo object ResurrectionComputer trong Scene Base (dây cáp đi theo vì là object con).";
        }

        /// <summary>
        /// Máy tính khổng lồ nối với lồng hồi sinh: thân máy + màn hình động + vầng sáng + 2 bó dây cáp (cáp thấp nối chân lồng, cáp cao nối khung lồng).
        /// Mặc định nằm bên TRÁI lồng; nếu sát mép trái căn cứ thì sang bên phải (lật hình). Toạ độ cổng cáp khớp với Tools/PixelArt/gen_bigpc.py.
        /// </summary>
        static GameObject MakeResurrectionComputer(ResurrectionCapsule cap, float limitLeftX)
        {
            const float half = 2.5f;          // nửa bề ngang máy (80 px / 16 / 2)
            const float gap = 1.4f;           // khe hở giữa máy và lồng (dây cáp bắc qua)
            const float capHalf = 1.5f;       // nửa bề ngang lồng (48 px / 16 / 2)
            float dist = half + gap + capHalf;
            Vector3 cp = cap.transform.position;
            int side = (cp.x - dist - half >= limitLeftX) ? -1 : 1;

            var go = new GameObject("ResurrectionComputer");
            go.transform.position = new Vector3(cp.x + side * dist, cp.y, 0f);
            go.transform.localScale = new Vector3(-side, 1f, 1f);          // sprite vẽ cáp ở bên PHẢI; máy bên phải lồng thì lật
            var comp = go.AddComponent<ResurrectionComputer>();
            comp.capsule = cap;

            AddSr(Child(go, "Body", Vector2.zero), Spr("bigpc_body"), 10, false, Color.white);
            Visual(go, "Screen", Vector2.zero, 11, "idle", Clip("idle", 4f, true, Fr("bigpc_screen_", 4)));
            comp.screenGlow = Glow(go, "ScreenGlow", new Vector2(0f, 5.25f), new Vector2(5.6f, 3.4f), A(Cfg.Cyan, 0.16f), 12);

            var low = Visual(go, "CableLow", new Vector2(half, 0f), 11, "idle", Clip("idle", 3f, true, Fr("bigpc_cable_low_", 2)));
            var high = Visual(go, "CableHigh", new Vector2(half, 4.0f), 11, "idle", Clip("idle", 3f, true, Fr("bigpc_cable_high_", 2)));
            comp.cables = new[] { low.GetComponent<SpriteRenderer>(), high.GetComponent<SpriteRenderer>() };
            comp.sparkPoints = new[]
            {
                Child(go, "Spark_Low", new Vector2(half + 0.9f, 0.5f)).transform,
                Child(go, "Spark_High", new Vector2(half + 0.9f, 5.0f)).transform
            };
            return go;
        }

        // ================================================================ ĐIỂM VÀO
        /// <summary>Tạo Base.unity nếu chưa có (hoặc ghi đè nếu overwrite). Trả về dòng báo cáo.</summary>
        static string EnsureBaseScene(bool overwrite)
        {
            if (File.Exists(BaseScenePath) && !overwrite)
            {
                AddSceneToBuildSettings(BaseScenePath);
                return "• Scene Base đã có — giữ nguyên (muốn dựng lại: menu \"Tạo lại Scene Căn cứ Base\"; muốn chỉ thêm máy tính khổng lồ cạnh lồng hồi sinh: menu \"Thêm máy tính khổng lồ vào Scene Base\")";
            }
            if (IsSceneOpen(BaseScenePath))
                return "• Scene Base đang mở nên chưa dựng lại — hãy đóng nó rồi chạy lại menu";
            if (EditorApplication.isPlaying) return "• Đang ở chế độ Play nên chưa dựng được Scene Base — hãy dừng Play rồi chạy lại";

            missing.Clear();
            Directory.CreateDirectory(SceneDir.TrimEnd('/'));
            MakeAlbertPrefab();

            var prevActive = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                BuildBaseScene(scene);
            }
            finally
            {
                if (prevActive.IsValid() && prevActive.isLoaded) SceneManager.SetActiveScene(prevActive);
                EditorSceneManager.CloseScene(scene, true);
            }
            AddSceneToBuildSettings(BaseScenePath);
            return "• Đã dựng Scene Base: " + BaseScenePath + " (đã thêm vào Build Settings)";
        }

        // ================================================================ PREFAB ALBERT
        static void MakeAlbertPrefab()
        {
            if (PrefabExists("Albert")) return;
            var go = new GameObject("Albert");
            go.AddComponent<AlbertNpc>();
            Visual(go, "Visual", Vector2.zero, 24, "idle",
                Clip("idle", 2f, true, Fr("albert_idle_", 2)),
                Clip("walk", 8f, true, Fr("albert_walk_", 4)),
                Clip("admire", 2f, true, Fr("albert_admire_", 2)));
            if (missing.Count > 0) { Object.DestroyImmediate(go); throw new System.Exception("Thiếu sprite: " + string.Join(", ", missing.ToArray()) + ". Chạy: python3 Tools/PixelArt/gen_base.py rồi thử lại."); }
            SavePrefab(go, "Albert");
        }

        // ================================================================ SCENE
        static Transform Prop(Transform parent, string name, string sprite, float x, int order)
        {
            var sr = Plain(parent, name, sprite, new Vector2(x, 0f), order);
            return sr.transform;
        }

        static Transform PropAnim(Transform parent, string name, string prefix, int frames, float fps, float x, int order)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            holder.transform.position = new Vector3(x, 0f, 0f);
            Visual(holder, "Visual", Vector2.zero, order, "idle", Clip("idle", fps, true, Fr(prefix, frames)));
            return holder.transform;
        }

        static BaseStation MakeStation(Transform parent, string name, StationKind kind, string sprite, float x, Vector2 glowPos, Vector2 glowSize, Color col, float promptY, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, 0f, 0f);
            var st = go.AddComponent<BaseStation>();
            st.kind = kind;
            st.glowColor = col;
            st.promptOffset = new Vector2(0f, promptY);
            var vis = Child(go, "Visual", Vector2.zero);
            AddSr(vis, Spr(sprite), order, false, Color.white);
            st.glow = Glow(go, "Glow", glowPos, glowSize, A(col, 0.25f), order + 1);
            return st;
        }

        static void BuildBaseScene(Scene scene)
        {
            // ---- Camera
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(15f, 5f, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 270f / (2f * 16f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.02f, 0.04f, 1f);
            cam.nearClipPlane = -50f;
            cam.farClipPlane = 50f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<PixelScreen>();
            var rig = camGo.AddComponent<CameraRig>();

            // ---- Systems (giống Scene Main)
            var sys = new GameObject("Systems");
            var gm = sys.AddComponent<GameManager>();
            gm.bossPrefab = Prefab("Boss");
            sys.AddComponent<Hud>();
            var sfx = new GameObject("Sfx"); sfx.transform.SetParent(sys.transform, false); sfx.AddComponent<Sfx>();
            var fx = new GameObject("Fx"); fx.transform.SetParent(sys.transform, false); fx.AddComponent<Fx>();

            float mid = (BaseLeft + BaseRight) * 0.5f;
            float span = BaseRight - BaseLeft + 30f;

            // ---- Giới hạn trái / phải (tường chặn vô hình)
            var marks = Group(null, "Marks");
            var left = Group(marks, "LimitLeft"); left.position = new Vector3(BaseLeft, 0f, 0f);
            var right = Group(marks, "LimitRight"); right.position = new Vector3(BaseRight, 0f, 0f);
            foreach (var wall in new[] { left, right })
            {
                var wc = wall.gameObject.AddComponent<BoxCollider2D>();
                wc.size = new Vector2(1f, 60f);
                wc.offset = new Vector2(wall == left ? -0.5f : 0.5f, 20f);
            }

            // ---- Môi trường: nền, cột chống, ống, sàn, trần
            var env = Group(null, "Environment");
            var back = Group(env, "Back");
            Tiled(back, "BackWall", "base_wall_back", new Vector2(mid, 5f), new Vector2(span, 14f), -100);
            Tiled(back, "Wainscot", "base_wainscot", new Vector2(mid, 1f), new Vector2(span, 2f), -40);
            Tiled(back, "PipeA", "base_pipe_h", new Vector2(mid, 9.6f), new Vector2(span, 0.5f), -38);
            Tiled(back, "PipeB", "base_pipe_h", new Vector2(mid, 10.3f), new Vector2(span, 0.5f), -38);
            foreach (float cx in new[] { 14f, 45f, 72f, 96f })
                Tiled(back, "Column_" + cx, "base_column", new Vector2(cx, BaseCeil * 0.5f), new Vector2(1f, BaseCeil), -45);

            var ground = Group(env, "Ground");
            ground.position = new Vector3(mid, 0f, 0f);
            var top = Tiled(ground, "Top", "base_floor_top", new Vector2(mid, -0.5f), new Vector2(span, 1f), 5);
            var fill = Tiled(ground, "Fill", "base_floor_fill", new Vector2(mid, -5f), new Vector2(span, 8f), 4);
            var gc = ground.gameObject.AddComponent<BoxCollider2D>();
            gc.size = new Vector2(span, 9f);
            gc.offset = new Vector2(0f, -4.5f);
            top.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            fill.transform.localPosition = new Vector3(0f, -5f, 0f);

            var ceil = Group(env, "Ceiling");
            Tiled(ceil, "Rock", "base_wall", new Vector2(mid, BaseCeil + 3f), new Vector2(span, 4f), 12);
            Tiled(ceil, "Beam", "base_ceiling", new Vector2(mid, BaseCeil + 0.5f), new Vector2(span, 1f), 13);
            var cc = ceil.gameObject.AddComponent<BoxCollider2D>();      // không cho nhảy / bay xuyên trần
            cc.size = new Vector2(span, 4f);
            cc.offset = new Vector2(mid, BaseCeil + 2f);

            // tường đá hai đầu (nằm ngoài giới hạn, che khi camera rung)
            Tiled(env, "RockLeft", "base_wall", new Vector2(BaseLeft - 5f, 5f), new Vector2(10f, 20f), 30);
            Tiled(env, "RockRight", "base_wall", new Vector2(BaseRight + 5f, 5f), new Vector2(10f, 20f), 30);

            // nhũ đá + dây cáp rủ từ trần
            var deco = Group(env, "CeilingDeco");
            float[] stx = { 11f, 24f, 37f, 52f, 63f, 80f, 91f, 104f, 116f };
            for (int i = 0; i < stx.Length; i++)
                Plain(deco, "Stalactite_" + i, "prop_stalactite_" + (i % 2), new Vector2(stx[i], BaseCeil), 14);
            float[] cbx = { 13f, 33f, 58f, 87f, 112f };
            for (int i = 0; i < cbx.Length; i++)
                Plain(deco, "Cable_" + i, "prop_cable_" + (i % 2), new Vector2(cbx[i], BaseCeil), 14);

            // ---- Đèn trần (hầm dài): bóng đèn + chùm sáng + vầng sáng, chập chờn nhẹ
            var lamps = Group(env, "Lamps");
            for (int i = 0; i < 11; i++)
            {
                float lx = 8f + i * 10f;
                var lamp = new GameObject("Lamp_" + i);
                lamp.transform.SetParent(lamps, false);
                lamp.transform.position = new Vector3(lx, BaseCeil, 0f);
                var bulb = Child(lamp, "Bulb", Vector2.zero);
                AddSr(bulb, Spr("base_lamp"), 15, false, Color.white);
                var coneGo = Child(lamp, "Cone", new Vector2(0f, -1.0f));
                coneGo.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
                var cone = AddSr(coneGo, Spr("base_lamp_cone"), 8, true, new Color(1f, 0.82f, 0.5f, 0.42f));
                var halo = Glow(lamp, "Glow", new Vector2(0f, -1.2f), new Vector2(3.2f, 3.2f), A(new Color(1f, 0.75f, 0.35f), 0.35f), 9);
                var fl = lamp.AddComponent<LampFlicker>();
                fl.targets = new[] { cone, halo };
                fl.gap = new Vector2(2f + (i % 4), 8f + (i % 5) * 2f);
            }

            // ---- Lồng kính hồi sinh (điểm xuất hiện) + cột thu lôi lên trần
            float capX = 8f;
            Tiled(env, "LightningRod", "base_conductor", new Vector2(capX, (5.0f + BaseCeil) * 0.5f), new Vector2(0.375f, BaseCeil - 5.0f), -20);
            var capGo = new GameObject("ResurrectionCapsule");
            capGo.transform.position = new Vector3(capX, 0f, 0f);
            var cap = capGo.AddComponent<ResurrectionCapsule>();
            AddSr(Child(capGo, "Back", Vector2.zero), Spr("capsule_back"), 9, false, Color.white);
            cap.glow = Glow(capGo, "Glow", new Vector2(0f, 1.8f), new Vector2(3.2f, 5.1f), A(new Color(0.1f, 0.95f, 1f), 0.12f), 10);
            cap.glass = AddSr(Child(capGo, "Glass", Vector2.zero), Spr("capsule_glass"), 30, false, new Color(1f, 1f, 1f, 0.55f));

            // ---- Công trình / phát minh của Albert
            var props = Group(null, "Props");
            var pois = new List<Transform>();
            Prop(props, "Crates_A", "prop_crate", 20.5f, 10);
            Prop(props, "Barrel_A", "prop_barrel", 22f, 10);
            pois.Add(Prop(props, "Shelf", "prop_shelf", 16.5f, 8));
            var craft = MakeStation(props, "CraftBench", StationKind.Craft, "station_craft", 28f, new Vector2(0f, 1.9f), new Vector2(4.5f, 3.2f), new Color(1f, 0.7f, 0.25f), 3.6f, 10);
            pois.Add(craft.transform);
            Prop(props, "Crates_B", "prop_crate", 33.5f, 10);
            Prop(props, "Crates_C", "prop_crate", 35f, 10);
            pois.Add(PropAnim(props, "TeslaCoil", "prop_tesla_", 2, 7f, 40.5f, 8));
            pois.Add(PropAnim(props, "Monitors", "prop_monitors_", 2, 2f, 50.5f, 8));
            pois.Add(PropAnim(props, "HologramTable", "prop_holo_", 2, 2.5f, 58f, 8));
            var fusion = MakeStation(props, "FusionMachine", StationKind.Fusion, "station_fusion", 66.5f, new Vector2(0f, 2.6f), new Vector2(5.5f, 5f), new Color(0.75f, 0.3f, 1f), 5.2f, 10);
            pois.Add(fusion.transform);
            pois.Add(Prop(props, "PrototypeCase", "prop_prototype", 77f, 8));
            pois.Add(Prop(props, "RobotArm", "prop_robot_arm", 84.5f, 8));
            pois.Add(PropAnim(props, "Generator", "prop_generator_", 2, 6f, 92.5f, 8));
            PropAnim(props, "Rack_A", "prop_rack_", 2, 2f, 98f, 8);
            PropAnim(props, "Rack_B", "prop_rack_", 2, 2.5f, 100.2f, 8);
            Prop(props, "Barrel_B", "prop_barrel", 102.5f, 10);
            Prop(props, "Rocks_A", "prop_rocks", 4f, 11);
            Prop(props, "Rocks_B", "prop_rocks", 116.5f, 11);

            // máy tính khổng lồ nối với lồng hồi sinh
            MakeResurrectionComputer(cap, BaseLeft).transform.SetParent(props, true);

            // cửa ra + biển chỉ lối
            var door = MakeStation(props, "ExitDoor", StationKind.Door, "station_door", 109f, new Vector2(0f, 2.2f), new Vector2(3.6f, 5f), new Color(0.45f, 1f, 0.4f), 5.1f, 10);
            Plain(props, "ExitSign", "base_sign_exit", new Vector2(109f, 5.2f), 11);

            // ---- Albert (Prefab) + các công trình ông hay đứng ngắm
            var albGo = (GameObject)PrefabUtility.InstantiatePrefab(Prefab("Albert"));
            albGo.transform.position = new Vector3(14f, 0f, 0f);
            var npc = albGo.GetComponent<AlbertNpc>();
            npc.minX = BaseLeft + 4f;
            npc.maxX = 102f;
            npc.interestPoints = pois.ToArray();

            // ---- Người chơi
            var player = Inst("Player", null, new Vector2(capX, 0.05f));
            rig.target = player.transform;

            // ---- BaseManager
            var bmGo = new GameObject("BaseManager");
            var bm = bmGo.AddComponent<BaseManager>();
            bm.limitLeft = left;
            bm.limitRight = right;
            bm.capsule = cap;
            bm.albert = npc;
            bm.exitDoor = door;

            if (missing.Count > 0)
                throw new System.Exception("Thiếu sprite: " + string.Join(", ", missing.ToArray()) + ". Chạy: python3 Tools/PixelArt/gen_base.py rồi thử lại.");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, BaseScenePath))
                throw new System.Exception("Không lưu được " + BaseScenePath);
        }
    }
}
#endif
