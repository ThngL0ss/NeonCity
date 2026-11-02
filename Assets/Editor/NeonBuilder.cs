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
    /// Menu Tools ▸ Neon Breaker ▸ Build Project: tạo (một lần) toàn bộ file thật để bạn chỉnh trong Unity:
    ///   Materials, Audio (WAV), ScriptableObject (ArtBank / AudioBank), Prefab, và Scene Main.unity.
    /// Sau khi tạo xong, mọi thứ là file bình thường - bạn sửa / thay / kéo thả thoải mái. Không cần chạy lại.
    /// </summary>
    public static class NeonBuilder
    {
        // Thư mục gốc được TỰ TÌM (theo vị trí file Art/manifest.json) nên project của bạn đặt gói ở đâu cũng chạy,
        // ví dụ Assets/NeonBreaker/ hoặc Assets/Assets/NeonBreaker/.
        static string root;
        static string Root { get { if (root == null) root = FindRoot(); return root; } }
        static string ArtDir { get { return Root + "Art/"; } }
        static string MatDir { get { return Root + "Materials/"; } }
        static string AudioDir { get { return Root + "Audio/"; } }
        static string PrefabDir { get { return Root + "Prefabs/"; } }
        static string SceneDir { get { return Root + "Scenes/"; } }
        static string ScenePath { get { return SceneDir + "Main.unity"; } }
        static string ResDir { get { return Root.Substring(0, Root.Length - "NeonBreaker/".Length) + "Resources/NeonBreaker/"; } }

        static string FindRoot()
        {
            foreach (var guid in AssetDatabase.FindAssets("manifest"))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.EndsWith("/NeonBreaker/Art/manifest.json"))
                    return p.Substring(0, p.Length - "Art/manifest.json".Length);
            }
            return "Assets/NeonBreaker/";
        }

        static Material matPixel, matAdd, matFlash;
        static PhysicsMaterial2D matNoFriction;
        static readonly List<string> missing = new List<string>();

        // ------------------------------------------------------------ menu
        [MenuItem("Tools/Neon Breaker/Build Project (tạo Prefab + Scene)", false, 1)]
        public static void BuildAll()
        {
            root = null;
            if (!Directory.Exists(ArtDir.TrimEnd('/')))
            {
                EditorUtility.DisplayDialog("Neon Breaker", "Không tìm thấy thư mục NeonBreaker/Art (chứa các file PNG và manifest.json) trong project.\nHãy chép đủ thư mục Assets của gói vào project (gộp vào Assets, KHÔNG tạo Assets/Assets).", "OK");
                return;
            }
            if (File.Exists(ScenePath) || File.Exists(PrefabDir + "Player.prefab"))
            {
                if (!EditorUtility.DisplayDialog("Neon Breaker",
                    "Đã có Scene / Prefab được tạo trước đó.\nBuild lại sẽ GHI ĐÈ các file Prefab và Scene mặc định (Main.unity, Prefabs/*).\nMọi chỉnh sửa của bạn trên các file đó sẽ mất. Tiếp tục?",
                    "Ghi đè", "Huỷ")) return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                ShowProgress("Chuẩn bị...", 0.05f);
                Prepare();
                ShowProgress("Âm thanh...", 0.2f);
                BuildAudio(false);
                ShowProgress("ScriptableObject...", 0.35f);
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                BuildPrefabs();
                ShowProgress("Prefab...", 0.65f);
                BuildBanks();
                ShowProgress("Scene...", 0.8f);
                BuildScene();
                AddToBuildSettings();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();
                Debug.Log("Neon Breaker: Build Project xong. Mở " + ScenePath + " rồi bấm Play.");
                EditorUtility.DisplayDialog("Neon Breaker", "Xong! Scene: " + ScenePath + "\nBấm Play để chơi.", "OK");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError("Neon Breaker: Build Project lỗi - " + e);
                EditorUtility.DisplayDialog("Neon Breaker", "Lỗi: " + e.Message + "\n(xem Console)", "OK");
            }
        }

        [MenuItem("Tools/Neon Breaker/Thêm quái Heavy + Rusher (không đụng Scene)", false, 10)]
        public static void AddNewEnemies()
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
                // sửa hộp trúng đòn của Walker/Flyer nếu Prefab cũ còn giá trị mặc định (1x1 ở chân)
                EditPrefab("Walker", go =>
                {
                    var w = go.GetComponent<Walker>();
                    if (w != null && w.hitSize == Vector2.one && w.hitOffset == Vector2.zero) { w.hitSize = new Vector2(1.0f, 0.95f); w.hitOffset = new Vector2(0f, 0.48f); }
                });
                EditPrefab("Flyer", go =>
                {
                    var f = go.GetComponent<Flyer>();
                    if (f != null && f.hitSize == Vector2.one && f.hitOffset == Vector2.zero) { f.hitSize = new Vector2(1.0f, 0.9f); f.hitOffset = Vector2.zero; }
                });
                MakeWalkerVariant("Heavy", false);
                MakeWalkerVariant("Rusher", false);
                // nếu bảng triệu hồi của Prefab Tower còn trống thì điền sẵn 4 loại quái
                EditPrefab("Tower", go =>
                {
                    var t = go.GetComponent<Tower>();
                    if (t != null && (t.spawnTable == null || t.spawnTable.Count == 0)) FillTowerTable(t);
                });
                if (missing.Count > 0) throw new System.Exception("Thiếu sprite: " + string.Join(", ", missing.ToArray()));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Neon Breaker: đã có Prefab Heavy + Rusher và bảng triệu hồi của Tower. Scene của bạn không bị thay đổi.");
                EditorUtility.DisplayDialog("Neon Breaker", "Xong! Đã tạo Prefab Heavy + Rusher trong " + PrefabDir + "\nBảng triệu hồi của Prefab Tower đã có 4 loại quái.\nScene của bạn không bị thay đổi.", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Neon Breaker: lỗi khi thêm quái - " + e);
                EditorUtility.DisplayDialog("Neon Breaker", "Lỗi: " + e.Message + "\n(xem Console)", "OK");
            }
        }

        [MenuItem("Tools/Neon Breaker/Chỉ tạo lại Prefab", false, 20)]
        public static void BuildPrefabsOnly()
        {
            root = null;
            if (!EditorUtility.DisplayDialog("Neon Breaker", "Ghi đè các Prefab mặc định bằng bản gốc?", "Ghi đè", "Huỷ")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Prepare();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildPrefabs();
            BuildBanks();
            AssetDatabase.SaveAssets();
            Debug.Log("Neon Breaker: đã tạo lại Prefab.");
        }

        [MenuItem("Tools/Neon Breaker/Chỉ tạo lại Scene Main", false, 21)]
        public static void BuildSceneOnly()
        {
            root = null;
            if (!EditorUtility.DisplayDialog("Neon Breaker", "Ghi đè Scene Main.unity bằng bản gốc?", "Ghi đè", "Huỷ")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Prepare();
            if (!File.Exists(PrefabDir + "Player.prefab")) { BuildPrefabsOnlyQuiet(); }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildScene();
            AddToBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("Neon Breaker: đã tạo lại Scene.");
        }

        [MenuItem("Tools/Neon Breaker/Xuất lại âm thanh WAV (ghi đè)", false, 22)]
        public static void ExportAudioOverwrite()
        {
            root = null;
            if (!EditorUtility.DisplayDialog("Neon Breaker", "Tổng hợp lại toàn bộ âm thanh và ghi đè file WAV trong Audio/ ?", "Ghi đè", "Huỷ")) return;
            Prepare();
            BuildAudio(true);
            BuildBanks();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Neon Breaker/Mở Scene Main", false, 40)]
        public static void OpenMain()
        {
            root = null;
            if (!File.Exists(ScenePath)) { EditorUtility.DisplayDialog("Neon Breaker", "Chưa có Scene. Hãy chọn Build Project trước.", "OK"); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }

        static void BuildPrefabsOnlyQuiet()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildPrefabs();
            BuildBanks();
        }

        static void ShowProgress(string msg, float t) { EditorUtility.DisplayProgressBar("Neon Breaker", msg, t); }

        // ------------------------------------------------------------ prepare: layers, folders, import, materials
        static void Prepare()
        {
            EnsureLayers();
            foreach (var d in new[] { MatDir, AudioDir, PrefabDir, SceneDir, ResDir })
                Directory.CreateDirectory(d.TrimEnd('/'));
            AssetDatabase.Refresh();

            foreach (var f in Directory.GetFiles(ArtDir.TrimEnd('/'), "*.png"))
                AssetDatabase.ImportAsset(f.Replace('\\', '/'), ImportAssetOptions.ForceUpdate);

            matPixel = MakeMat("PixelSprite", "Sprites/Default");
            matAdd = MakeMat("Additive", "NeonBreaker/SpriteAdditive");
            matFlash = MakeMat("Flash", "NeonBreaker/SpriteFlash");

            string pm = MatDir + "NoFriction.physicsMaterial2D";
            matNoFriction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(pm);
            if (matNoFriction == null)
            {
                matNoFriction = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
                AssetDatabase.CreateAsset(matNoFriction, pm);
            }
            missing.Clear();
        }

        static void EnsureLayers()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;
            var tm = new SerializedObject(assets[0]);
            var layers = tm.FindProperty("layers");
            string[] names = { "Player", "Enemy", "Item" };
            bool changed = false;
            for (int i = 0; i < names.Length; i++)
            {
                var p = layers.GetArrayElementAtIndex(8 + i);
                if (string.IsNullOrEmpty(p.stringValue)) { p.stringValue = names[i]; changed = true; }
            }
            if (changed) { tm.ApplyModifiedProperties(); AssetDatabase.SaveAssets(); }
        }

        static Material MakeMat(string name, string shaderName)
        {
            string path = MatDir + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var sh = Shader.Find(shaderName);
            if (sh == null) { Debug.LogWarning("Neon Breaker: không tìm thấy shader " + shaderName + " - dùng Sprites/Default."); sh = Shader.Find("Sprites/Default"); }
            if (m == null)
            {
                m = new Material(sh);
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != sh) m.shader = sh;
            return m;
        }

        // ------------------------------------------------------------ sprite helpers
        static Sprite Spr(string name)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + name + ".png");
            if (s == null) { missing.Add(name); Debug.LogError("Neon Breaker: thiếu sprite " + name + ".png trong " + ArtDir); }
            return s;
        }

        static Sprite[] Fr(string prefix, int n)
        {
            var a = new Sprite[n];
            for (int i = 0; i < n; i++) a[i] = Spr(prefix + i);
            return a;
        }

        static SpriteClip Clip(string name, float fps, bool loop, params Sprite[] frames)
        {
            return new SpriteClip { name = name, fps = fps, loop = loop, frames = frames };
        }

        // ------------------------------------------------------------ gameobject helpers
        static GameObject Child(GameObject parent, string name, Vector2 pos)
        {
            var g = new GameObject(name);
            if (parent != null) g.transform.SetParent(parent.transform, false);
            g.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            return g;
        }

        static SpriteRenderer AddSr(GameObject g, Sprite s, int order, bool additive, Color col)
        {
            var sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
            sr.color = col;
            sr.sharedMaterial = additive ? matAdd : matPixel;
            return sr;
        }

        /// <summary>Vầng sáng cộng sáng (sprite fx_glow 1 đơn vị, phóng theo scale).</summary>
        static SpriteRenderer Glow(GameObject parent, string name, Vector2 pos, Vector2 size, Color col, int order)
        {
            var g = Child(parent, name, pos);
            g.transform.localScale = new Vector3(size.x, size.y, 1f);
            return AddSr(g, Spr("fx_glow"), order, true, col);
        }

        static SpriteAnimator Visual(GameObject parent, string name, Vector2 pos, int order, string startClip, params SpriteClip[] clips)
        {
            var g = Child(parent, name, pos);
            AddSr(g, clips[0].frames[0], order, false, Color.white);
            var a = g.AddComponent<SpriteAnimator>();
            a.clips = clips;
            a.startClip = startClip;
            return a;
        }

        /// <summary>Mở Prefab trong chế độ cô lập (không đụng Scene đang mở), chạy hàm sửa, rồi lưu lại.</summary>
        static void EditPrefab(string name, System.Action<GameObject> edit)
        {
            string path = PrefabDir + name + ".prefab";
            if (!File.Exists(path)) return;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                edit(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        /// <summary>Điền bảng triệu hồi mặc định của trụ: Walker, Flyer, Rusher (từ trụ có Power >= 1.1), Heavy (Power >= 1.3).</summary>
        static void FillTowerTable(Tower t)
        {
            t.spawnTable = new List<Tower.SpawnEntry>
            {
                new Tower.SpawnEntry { prefab = Prefab("Walker"), weight = 45f, minPower = 0f },
                new Tower.SpawnEntry { prefab = Prefab("Flyer"), weight = 25f, minPower = 0f },
                new Tower.SpawnEntry { prefab = Prefab("Rusher"), weight = 15f, minPower = 1.1f },
                new Tower.SpawnEntry { prefab = Prefab("Heavy"), weight = 15f, minPower = 1.3f },
            };
        }

        /// <summary>
        /// Tạo quái biến thể từ Prefab Walker: nhân bản file, đổi bộ hình (heavy_*/rusher_*), chỉ số, hộp va chạm.
        /// Heavy  = máu trâu, sát thương nhỏ, chậm.   Rusher = máu yếu, sát thương lớn, nhanh.
        /// overwrite = false: nếu Prefab đã có thì giữ nguyên (không đè lên chỉnh sửa của bạn).
        /// </summary>
        static void MakeWalkerVariant(string name, bool overwrite)
        {
            string src = PrefabDir + "Walker.prefab", dst = PrefabDir + name + ".prefab";
            if (File.Exists(dst))
            {
                if (!overwrite) return;
                AssetDatabase.DeleteAsset(dst);
            }
            if (!AssetDatabase.CopyAsset(src, dst)) throw new System.Exception("Không nhân bản được " + src);
            AssetDatabase.Refresh();

            var root = PrefabUtility.LoadPrefabContents(dst);
            try
            {
                root.name = name;
                var w = root.GetComponent<Walker>();
                var col = root.GetComponent<BoxCollider2D>();
                var rb = root.GetComponent<Rigidbody2D>();
                var visT = root.transform.Find("Visual");
                var anim = visT.GetComponent<SpriteAnimator>();
                var aura = root.transform.Find("Aura");
                var warn = visT.Find("Warn");
                string pre = name.ToLowerInvariant();

                anim.clips = new[]
                {
                    Clip("walk", name == "Heavy" ? 6f : 14f, true, Fr(pre + "_walk_", 4)),
                    Clip("windup", 8f, true, Spr(pre + "_windup")),
                    Clip("lunge", 8f, true, Spr(pre + "_lunge")),
                };
                anim.startClip = "walk";
                visT.GetComponent<SpriteRenderer>().sprite = anim.clips[0].frames[0];

                if (name == "Heavy")
                {
                    w.health = 220f; w.moveSpeed = 2.0f; w.lungeDamage = 8f; w.touchDamage = 3f;
                    w.aggroRange = 18f; w.windupRange = 3.8f; w.windupTime = 0.75f;
                    w.lungeSpeed = 8.5f; w.lungeTime = 0.35f; w.recoverTime = 1.0f; w.attackCooldown = 1.4f;
                    w.staggerTime = 0.12f; w.knockbackMul = 0.25f; w.gravityScale = 6f;
                    w.lungeBoxSize = new Vector2(2.4f, 1.7f); w.touchBoxSize = new Vector2(2.2f, 1.5f);
                    w.accentColor = new Color(0.55f, 0.3f, 1f, 1f);
                    w.spawnHeight = 0.05f; w.dropChanceMul = 2.5f;
                    w.hitSize = new Vector2(2.3f, 1.8f); w.hitOffset = new Vector2(0f, 0.9f);
                    col.size = new Vector2(2.2f, 1.7f); col.offset = new Vector2(0f, 0.85f);
                    aura.localPosition = new Vector3(0f, 1.0f, 0f); aura.localScale = new Vector3(4.2f, 3.2f, 1f);
                    aura.GetComponent<SpriteRenderer>().color = new Color(0.55f, 0.3f, 1f, 0.25f);
                    warn.localPosition = new Vector3(0.4f, 1.1f, 0f); warn.localScale = new Vector3(4.6f, 3.6f, 1f);
                }
                else
                {
                    w.health = 18f; w.moveSpeed = 7.0f; w.lungeDamage = 24f; w.touchDamage = 8f;
                    w.aggroRange = 22f; w.windupRange = 5.5f; w.windupTime = 0.4f;
                    w.lungeSpeed = 21f; w.lungeTime = 0.3f; w.recoverTime = 0.6f; w.attackCooldown = 0.7f;
                    w.staggerTime = 0.4f; w.knockbackMul = 1.4f; w.gravityScale = 4f;
                    w.lungeBoxSize = new Vector2(1.6f, 0.9f); w.touchBoxSize = new Vector2(1.4f, 0.8f);
                    w.accentColor = new Color(0.45f, 1f, 0.4f, 1f);
                    w.spawnHeight = 0.05f; w.dropChanceMul = 0.8f;
                    w.hitSize = new Vector2(1.5f, 1.0f); w.hitOffset = new Vector2(0f, 0.5f);
                    col.size = new Vector2(1.5f, 0.9f); col.offset = new Vector2(0f, 0.45f);
                    aura.localPosition = new Vector3(0f, 0.5f, 0f); aura.localScale = new Vector3(2.6f, 1.6f, 1f);
                    aura.GetComponent<SpriteRenderer>().color = new Color(0.45f, 1f, 0.4f, 0.25f);
                    warn.localPosition = new Vector3(0.3f, 0.6f, 0f); warn.localScale = new Vector3(2.6f, 1.8f, 1f);
                }
                PrefabUtility.SaveAsPrefabAsset(root, dst);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static GameObject SavePrefab(GameObject go, string name)
        {
            var p = PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + name + ".prefab");
            Object.DestroyImmediate(go);
            // Script phải nằm trong file trùng tên class (Walker.cs, Flyer.cs...) thì Unity mới lưu được vào Prefab
            int bad = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(p);
            if (bad > 0)
                throw new System.Exception("Prefab " + name + " bị mất " + bad + " script (class phải nằm trong file cùng tên). Kiểm tra lỗi biên dịch trong Console.");
            return p;
        }

        static GameObject Prefab(string name) { return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + name + ".prefab"); }

        static Color A(Color c, float a) { c.a = a; return c; }

        // ------------------------------------------------------------ prefabs
        static void BuildPrefabs()
        {
            var cyan = new Color(0.15f, 0.95f, 1f, 1f);
            var magenta = new Color(1f, 0.15f, 0.8f, 1f);
            var purple = new Color(0.6f, 0.25f, 1f, 1f);
            var red = new Color(1f, 0.2f, 0.25f, 1f);
            var lime = new Color(0.45f, 1f, 0.3f, 1f);

            // ---- Player
            {
                var go = new GameObject("Player");
                go.layer = 8;
                var rb = go.AddComponent<Rigidbody2D>();
                rb.freezeRotation = true;
                rb.gravityScale = 4.8f;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
                var col = go.AddComponent<BoxCollider2D>();
                col.size = new Vector2(0.62f, 1.5f);
                col.offset = new Vector2(0f, 0.75f);
                col.sharedMaterial = matNoFriction;
                Glow(go, "Aura", new Vector2(0f, 0.8f), new Vector2(2.6f, 2.8f), A(cyan, 0.2f), 21);
                var pl = go.AddComponent<Player>();
                var vis = Visual(go, "Visual", Vector2.zero, 26, "idle",
                    Clip("idle", 4f, true, Fr("player_idle_", 2)),
                    Clip("run", 14f, true, Fr("player_run_", 6)),
                    Clip("jump", 8f, true, Spr("player_jump")),
                    Clip("fall", 8f, true, Spr("player_fall")),
                    Clip("dash", 8f, true, Spr("player_dash")),
                    Clip("attack", 12f, false, Fr("player_attack_", 3)),
                    Clip("heal", 8f, true, Spr("player_heal")),
                    Clip("hurt", 8f, true, Spr("player_hurt")));
                pl.visual = vis.transform;
                pl.anim = vis;
                SavePrefab(go, "Player");
            }

            // ---- Walker
            {
                var go = new GameObject("Walker");
                go.layer = 9;
                var rb = go.AddComponent<Rigidbody2D>();
                rb.freezeRotation = true;
                rb.gravityScale = 4f;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
                var col = go.AddComponent<BoxCollider2D>();
                col.size = new Vector2(0.9f, 0.9f);
                col.offset = new Vector2(0f, 0.45f);
                Glow(go, "Aura", new Vector2(0f, 0.5f), new Vector2(2.2f, 1.8f), A(red, 0.22f), 21);
                var w = go.AddComponent<Walker>();
                w.hitSize = new Vector2(1.0f, 0.95f);
                w.hitOffset = new Vector2(0f, 0.48f);
                var vis = Visual(go, "Visual", Vector2.zero, 24, "walk",
                    Clip("walk", 8f, true, Fr("walker_walk_", 4)),
                    Clip("windup", 8f, true, Spr("walker_windup")),
                    Clip("lunge", 8f, true, Spr("walker_lunge")));
                var warn = Glow(vis.gameObject, "Warn", new Vector2(0.1f, 0.7f), new Vector2(2.4f, 2.4f), A(red, 0f), 27);
                w.vis = vis.transform;
                w.anim = vis;
                w.warn = warn;
                SavePrefab(go, "Walker");
            }

            // ---- Flyer
            {
                var go = new GameObject("Flyer");
                go.layer = 9;
                Glow(go, "Aura", Vector2.zero, new Vector2(2.6f, 2.2f), A(magenta, 0.25f), 21);
                var f = go.AddComponent<Flyer>();
                f.hitSize = new Vector2(1.0f, 0.9f);
                f.hitOffset = Vector2.zero;
                var vis = Visual(go, "Visual", Vector2.zero, 24, "fly",
                    Clip("fly", 8f, true, Fr("flyer_fly_", 2)),
                    Clip("charge", 8f, true, Spr("flyer_charge")));
                var cg = Glow(go, "ChargeGlow", new Vector2(0.2f, 0f), new Vector2(0.4f, 0.4f), A(red, 0f), 27);
                f.vis = vis.transform;
                f.anim = vis;
                f.chargeGlow = cg;
                SavePrefab(go, "Flyer");
            }

            // ---- Heavy + Rusher (nhân bản từ Walker, đổi hình + chỉ số)
            MakeWalkerVariant("Heavy", true);
            MakeWalkerVariant("Rusher", true);

            // ---- Tower
            {
                var go = new GameObject("Tower");
                var t = go.AddComponent<Tower>();
                t.hitSize = new Vector2(1.7f, 4.2f);
                t.hitOffset = new Vector2(0f, 2.1f);
                t.barOffset = new Vector2(0f, 5.2f);
                Visual(go, "Visual", Vector2.zero, 20, "idle", Clip("idle", 6f, true, Fr("tower_idle_", 4)));
                t.crystalGlow = Glow(go, "CrystalGlow", new Vector2(0f, 3.7f), new Vector2(2.6f, 2.6f), A(magenta, 0.5f), 21);
                t.baseGlow = Glow(go, "BaseGlow", new Vector2(0f, 0.4f), new Vector2(3.2f, 1.0f), A(purple, 0.4f), 19);
                FillTowerTable(t);
                SavePrefab(go, "Tower");
            }

            // ---- Boss
            {
                var go = new GameObject("Boss");
                var b = go.AddComponent<Boss>();
                var aura = Glow(go, "Aura", new Vector2(0f, 2.5f), new Vector2(8f, 8f), A(purple, 0.3f), 21);
                var root = Child(go, "Visual", Vector2.zero);
                var p1 = Visual(root, "Phase1", Vector2.zero, 26, "idle",
                    Clip("idle", 3f, true, Fr("boss1_idle_", 2)),
                    Clip("walk", 6f, true, Fr("boss1_walk_", 4)),
                    Clip("crouch", 8f, true, Spr("boss1_crouch")),
                    Clip("air", 8f, true, Spr("boss1_air")));
                var p2 = Visual(root, "Phase2", Vector2.zero, 26, "idle",
                    Clip("idle", 4f, true, Fr("boss2_idle_", 2)),
                    Clip("walk", 10f, true, Fr("boss2_walk_", 4)),
                    Clip("slash", 8f, true, Spr("boss2_slash")),
                    Clip("cast", 8f, true, Spr("boss2_cast")),
                    Clip("air", 8f, true, Spr("boss2_air")));
                p2.gameObject.SetActive(false);
                b.vis = root.transform;
                b.animP1 = p1;
                b.animP2 = p2;
                b.aura = aura;
                SavePrefab(go, "Boss");
            }

            // ---- Projectiles
            {
                var go = new GameObject("Shuriken");
                go.AddComponent<Projectile>();
                var v = Child(go, "Visual", Vector2.zero);
                AddSr(v, Spr("proj_shuriken"), 45, false, Color.white);
                Glow(go, "Glow", Vector2.zero, new Vector2(2f, 2f), A(cyan, 0.5f), 43);
                SavePrefab(go, "Shuriken");
            }
            {
                var go = new GameObject("Bolt");
                go.AddComponent<Projectile>();
                var v = Child(go, "Visual", Vector2.zero);
                AddSr(v, Spr("proj_bolt"), 45, false, Color.white);
                Glow(go, "Glow", Vector2.zero, new Vector2(1.9f, 1.9f), A(red, 0.55f), 43);
                SavePrefab(go, "Bolt");
            }
            {
                var go = new GameObject("Wave");
                go.AddComponent<Projectile>();
                Visual(go, "Visual", Vector2.zero, 45, "wave", Clip("wave", 10f, true, Fr("proj_wave_", 2)));
                Glow(go, "Glow", new Vector2(0f, 0.8f), new Vector2(2.8f, 2.4f), A(purple, 0.5f), 43);
                SavePrefab(go, "Wave");
            }

            // ---- Pickup
            {
                var go = new GameObject("HealPickup");
                go.layer = 10;
                go.AddComponent<Pickup>();
                Visual(go, "Visual", Vector2.zero, 46, "idle", Clip("idle", 4f, true, Fr("pickup_", 2)));
                Glow(go, "Glow", Vector2.zero, new Vector2(2.2f, 2.2f), A(lime, 0.55f), 45);
                SavePrefab(go, "HealPickup");
            }

            AssetDatabase.SaveAssets();
            if (missing.Count > 0)
                throw new System.Exception("Thiếu sprite: " + string.Join(", ", missing.ToArray()) + ". Kiểm tra thư mục " + ArtDir);
        }

        // ------------------------------------------------------------ audio
        static readonly string[] MusicIds = { "music_explore", "music_boss" };

        static void BuildAudio(bool overwrite)
        {
            bool allExist = true;
            var d = new Dictionary<string, AudioClip>();
            // tổng hợp chỉ khi có file thiếu (hoặc ép ghi đè)
            foreach (var id in AllAudioIds())
                if (!File.Exists(AudioDir + id + ".wav")) { allExist = false; break; }
            if (!overwrite && allExist) return;

            d = Sfx.SynthAll();
            int n = 0;
            foreach (var kv in d)
            {
                if (kv.Value == null) continue;
                string path = AudioDir + kv.Key + ".wav";
                if (File.Exists(path) && !overwrite) continue;
                WriteWav(path, kv.Value);
                n++;
            }
            AssetDatabase.Refresh();

            foreach (var kv in d)
            {
                string path = AudioDir + kv.Key + ".wav";
                var imp = AssetImporter.GetAtPath(path) as AudioImporter;
                if (imp == null) continue;
                bool music = kv.Key.StartsWith("music_");
                var s = imp.defaultSampleSettings;
                s.loadType = music ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
                s.quality = 0.7f;
                imp.defaultSampleSettings = s;
                imp.loadInBackground = false;
                imp.SaveAndReimport();
            }
            Debug.Log("Neon Breaker: đã xuất " + n + " file WAV vào " + AudioDir);
        }

        static IEnumerable<string> AllAudioIds()
        {
            string[] sfx =
            {
                "jump", "land", "slash", "hit", "dash", "throw", "shurHit", "heal", "pickup", "hurt", "enemyDie", "towerHit", "towerDie",
                "spawn", "warn", "charge", "laser", "bolt", "bossRoar", "slam", "teleport", "shock", "drain", "phase", "victory", "defeat", "click"
            };
            foreach (var s in sfx) yield return s;
            foreach (var m in MusicIds) yield return m;
        }

        static void WriteWav(string path, AudioClip clip)
        {
            int ch = clip.channels, sr = clip.frequency;
            var data = new float[clip.samples * ch];
            clip.GetData(data, 0);
            int bytes = data.Length * 2;
            using (var fs = new FileStream(path, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + bytes);
                bw.Write(Encoding.ASCII.GetBytes("WAVE"));
                bw.Write(Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16);
                bw.Write((short)1);
                bw.Write((short)ch);
                bw.Write(sr);
                bw.Write(sr * ch * 2);
                bw.Write((short)(ch * 2));
                bw.Write((short)16);
                bw.Write(Encoding.ASCII.GetBytes("data"));
                bw.Write(bytes);
                for (int i = 0; i < data.Length; i++)
                    bw.Write((short)(Mathf.Clamp(data[i], -1f, 1f) * 32767f));
            }
        }

        // ------------------------------------------------------------ ScriptableObjects
        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null)
            {
                a = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(a, path);
            }
            return a;
        }

        static void BuildBanks()
        {
            // ArtBank
            var ab = LoadOrCreate<ArtBank>(ResDir + "ArtBank.asset");
            ab.square = Spr("fx_square");
            ab.circle = Spr("fx_circle");
            ab.glow = Spr("fx_glow");
            ab.ring = Spr("fx_ring");
            ab.arc = Spr("fx_arc");
            ab.diamond = Spr("fx_diamond");
            ab.triangle = Spr("fx_triangle");
            ab.star = Spr("fx_star");
            ab.spriteMaterial = matPixel;
            ab.additiveMaterial = matAdd;
            ab.flashMaterial = matFlash;
            ab.walkerPrefab = Prefab("Walker");
            ab.flyerPrefab = Prefab("Flyer");
            ab.bossPrefab = Prefab("Boss");
            ab.shurikenPrefab = Prefab("Shuriken");
            ab.boltPrefab = Prefab("Bolt");
            ab.wavePrefab = Prefab("Wave");
            ab.pickupPrefab = Prefab("HealPickup");
            EditorUtility.SetDirty(ab);

            // AudioBank
            var au = LoadOrCreate<AudioBank>(ResDir + "AudioBank.asset");
            if (au.entries == null) au.entries = new List<AudioBank.Entry>();
            foreach (var id in AllAudioIds())
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + id + ".wav");
                AudioBank.Entry e = null;
                for (int i = 0; i < au.entries.Count; i++) if (au.entries[i] != null && au.entries[i].id == id) { e = au.entries[i]; break; }
                if (e == null) { e = new AudioBank.Entry { id = id }; au.entries.Add(e); }
                if (e.clip == null) e.clip = clip;       // không đè clip bạn đã tự gán
            }
            EditorUtility.SetDirty(au);

            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------ scene
        static GameObject Inst(string prefabName, Transform parent, Vector2 pos)
        {
            var p = Prefab(prefabName);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(p);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            return go;
        }

        static SpriteRenderer Tiled(Transform parent, string name, string sprite, Vector2 pos, Vector2 size, int order)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.position = new Vector3(pos.x, pos.y, 0f);
            var sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = Spr(sprite);
            sr.sharedMaterial = matPixel;
            sr.sortingOrder = order;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = size;
            return sr;
        }

        static SpriteRenderer Plain(Transform parent, string name, string sprite, Vector2 pos, int order, bool additive = false)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.position = new Vector3(pos.x, pos.y, 0f);
            var sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = Spr(sprite);
            sr.sharedMaterial = additive ? matAdd : matPixel;
            sr.sortingOrder = order;
            return sr;
        }

        static Transform Group(Transform parent, string name)
        {
            var g = new GameObject(name);
            if (parent != null) g.transform.SetParent(parent, false);
            return g.transform;
        }

        static void Platform(Transform parent, string sprite, float x, float y, float w, string name)
        {
            var sr = Tiled(parent, name, sprite, new Vector2(x, y), new Vector2(w, 0.5f), 6);
            var col = sr.gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(w, 0.5f);
            col.usedByEffector = true;
            var eff = sr.gameObject.AddComponent<PlatformEffector2D>();
            eff.useOneWay = true;
            eff.useSideFriction = false;
            eff.surfaceArc = 160f;
        }

        static void BuildScene()
        {
            missing.Clear();
            var scene = SceneManager.GetActiveScene();

            // ---- Camera
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(2f, 5f, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 270f / (2f * 16f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.06f, 1f);
            cam.nearClipPlane = -50f;
            cam.farClipPlane = 50f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<PixelScreen>();
            var rig = camGo.AddComponent<CameraRig>();

            // ---- Systems
            var sys = new GameObject("Systems");
            var gm = sys.AddComponent<GameManager>();
            gm.bossPrefab = Prefab("Boss");
            sys.AddComponent<Hud>();
            var sfx = new GameObject("Sfx"); sfx.transform.SetParent(sys.transform, false); sfx.AddComponent<Sfx>();
            var fx = new GameObject("Fx"); fx.transform.SetParent(sys.transform, false); fx.AddComponent<Fx>();

            // ---- Level
            var levelGo = new GameObject("Level");
            var level = levelGo.AddComponent<Level>();

            var marks = Group(levelGo.transform, "Marks");
            var left = Group(marks, "LimitLeft"); left.position = new Vector3(-10f, 0f, 0f);
            var right = Group(marks, "LimitRight"); right.position = new Vector3(170f, 0f, 0f);
            var bossSpawn = Group(marks, "BossSpawn"); bossSpawn.position = new Vector3(150f, 0f, 0f);
            foreach (var wall in new[] { left, right })
            {
                var wc = wall.gameObject.AddComponent<BoxCollider2D>();
                wc.size = new Vector2(1f, 60f);
                wc.offset = new Vector2(wall == left ? -0.5f : 0.5f, 20f);
            }
            level.limitLeft = left;
            level.limitRight = right;
            level.bossSpawn = bossSpawn;

            // nền thị sai
            var bg = Group(levelGo.transform, "Background");
            var sky = Tiled(bg, "Sky", "bg_sky", new Vector2(0f, 0f), new Vector2(480f / 16f, 270f / 16f), -100);
            var skyP = sky.gameObject.AddComponent<Parallax>();
            skyP.factorX = 1f; skyP.followY = true; skyP.yOffset = 0f;
            AddParallax(bg, "Far", "bg_far", 0.15f, -90);
            AddParallax(bg, "Mid", "bg_mid", 0.35f, -80);
            AddParallax(bg, "Near", "bg_near", 0.6f, -70);

            // mặt đất
            var ground = Group(levelGo.transform, "Ground");
            ground.position = new Vector3(80f, 0f, 0f);
            Tiled(ground, "Top", "tile_ground_top", new Vector2(80f, -0.5f), new Vector2(180f, 1f), 5);
            Tiled(ground, "Fill", "tile_ground_fill", new Vector2(80f, -5f), new Vector2(180f, 8f), 4);
            var gc = ground.gameObject.AddComponent<BoxCollider2D>();
            gc.size = new Vector2(180f, 9f);
            gc.offset = new Vector2(0f, -4.5f);
            ground.GetChild(0).localPosition = new Vector3(0f, -0.5f, 0f);
            ground.GetChild(1).localPosition = new Vector3(0f, -5f, 0f);

            // bục
            var plats = Group(levelGo.transform, "Platforms");
            float[,] pl1 = { { 16, 6 }, { 36, 5 }, { 60, 7 }, { 84, 5 }, { 104, 6 }, { 120, 5 } };
            float[,] pl2 = { { 24, 5 }, { 46, 6 }, { 72, 5 }, { 94, 6 } };
            for (int i = 0; i < pl1.GetLength(0); i++) Platform(plats, "tile_plat", pl1[i, 0], 2.35f, pl1[i, 1], "Platform_L1_" + i);
            for (int i = 0; i < pl2.GetLength(0); i++) Platform(plats, "tile_plat", pl2[i, 0], 4.75f, pl2[i, 1], "Platform_L2_" + i);

            // khu boss
            var arena = Group(levelGo.transform, "BossArena");
            Platform(arena, "tile_plat_arena", 138f, 2.35f, 6f, "ArenaPlatform_0");
            Platform(arena, "tile_plat_arena", 162f, 2.35f, 6f, "ArenaPlatform_1");
            Platform(arena, "tile_plat_arena", 150f, 4.75f, 7f, "ArenaPlatform_2");
            Plain(arena, "PillarL", "arena_pillar", new Vector2(134f, 0f), 3);
            Plain(arena, "PillarR", "arena_pillar", new Vector2(166f, 0f), 3);
            Plain(arena, "Rune", "arena_rune", new Vector2(150f, 0.75f), 6);

            // cổng
            var gate = Group(levelGo.transform, "Gate");
            gate.position = new Vector3(128f, 0f, 0f);
            var gcol = gate.gameObject.AddComponent<BoxCollider2D>();
            gcol.size = new Vector2(0.8f, 18f);
            gcol.offset = new Vector2(0f, 9f);
            var posts = new List<SpriteRenderer>();
            for (int i = 0; i < 3; i++) posts.Add(Plain(gate, "Post" + i, "gate_post", new Vector2(128f, 3.5f * i), 8));
            var bar = Tiled(gate, "Bar", "gate_bar", new Vector2(128f, 8f), new Vector2(0.375f, 16f), 7);
            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(gate, false);
            glowGo.transform.position = new Vector3(128f, 8f, 0f);
            glowGo.transform.localScale = new Vector3(3f, 18f, 1f);
            var gglow = AddSrScene(glowGo, Spr("fx_glow"), 6, new Color(1f, 0.15f, 0.8f, 0.45f));
            level.gate = gate;
            level.gateCollider = gcol;
            level.gateBar = bar;
            level.gateGlow = gglow;
            level.gatePosts = posts.ToArray();

            // trụ triệu hồi
            var towers = Group(levelGo.transform, "Towers");
            float[] tx = { 8f, 28f, 52f, 78f, 99f, 112f };
            for (int i = 0; i < tx.Length; i++)
            {
                var t = Inst("Tower", towers, new Vector2(tx[i], 0f));
                t.name = "Tower_" + (i + 1);
            }

            // người chơi
            var player = Inst("Player", null, new Vector2(2f, 0.05f));
            rig.target = player.transform;

            if (missing.Count > 0)
                throw new System.Exception("Thiếu sprite: " + string.Join(", ", missing.ToArray()));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        static SpriteRenderer AddSrScene(GameObject g, Sprite s, int order, Color col)
        {
            var sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
            sr.color = col;
            sr.sharedMaterial = matAdd;
            return sr;
        }

        static void AddParallax(Transform parent, string name, string sprite, float factor, int order)
        {
            var sr = Tiled(parent, name, sprite, new Vector2(80f, -1f), new Vector2(240f, sr0Height(sprite)), order);
            var p = sr.gameObject.AddComponent<Parallax>();
            p.factorX = factor;
        }

        static float sr0Height(string sprite)
        {
            var s = Spr(sprite);
            return s != null ? s.rect.height / s.pixelsPerUnit : 6f;
        }

        static void AddToBuildSettings()
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in list) if (s.path == ScenePath) return;
            list.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
#endif
