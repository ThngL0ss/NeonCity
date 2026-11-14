using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonBreaker
{
    /// <summary>Bảng màu của một căn phòng.</summary>
    public class RoomStyle
    {
        public Color wall, wainscot, pilaster, ceiling, floor, neon, light;

        public static RoomStyle Corporate()
        {
            return new RoomStyle
            {
                wall = new Color(0.78f, 0.82f, 0.90f), wainscot = new Color(0.20f, 0.42f, 0.48f), pilaster = new Color(0.62f, 0.67f, 0.78f),
                ceiling = new Color(0.30f, 0.34f, 0.44f), floor = new Color(0.28f, 0.31f, 0.40f), neon = new Color(0.20f, 0.90f, 1f), light = new Color(0.75f, 0.95f, 1f)
            };
        }
        public static RoomStyle Robo()
        {
            return new RoomStyle
            {
                wall = new Color(0.18f, 0.20f, 0.30f), wainscot = new Color(0.12f, 0.13f, 0.22f), pilaster = new Color(0.26f, 0.28f, 0.42f),
                ceiling = new Color(0.08f, 0.09f, 0.16f), floor = new Color(0.10f, 0.11f, 0.19f), neon = new Color(1f, 0.20f, 0.70f), light = new Color(1f, 0.55f, 0.90f)
            };
        }
        public static RoomStyle Zen()
        {
            return new RoomStyle
            {
                wall = new Color(0.90f, 0.84f, 0.66f), wainscot = new Color(0.48f, 0.29f, 0.17f), pilaster = new Color(0.69f, 0.13f, 0.13f),
                ceiling = new Color(0.30f, 0.18f, 0.11f), floor = new Color(0.40f, 0.25f, 0.15f), neon = new Color(0.95f, 0.75f, 0.30f), light = new Color(1f, 0.85f, 0.55f)
            };
        }
        public static RoomStyle Des()
        {
            return new RoomStyle
            {
                wall = new Color(0.09f, 0.09f, 0.13f), wainscot = new Color(0.05f, 0.05f, 0.08f), pilaster = new Color(0.14f, 0.14f, 0.20f),
                ceiling = new Color(0.03f, 0.03f, 0.05f), floor = new Color(0.06f, 0.06f, 0.09f), neon = new Color(1f, 0.10f, 0.24f), light = new Color(1f, 0.3f, 0.35f)
            };
        }
        public static RoomStyle Arena()
        {
            return new RoomStyle
            {
                wall = new Color(0.55f, 0.43f, 0.28f), wainscot = new Color(0.30f, 0.23f, 0.16f), pilaster = new Color(0.75f, 0.62f, 0.42f),
                ceiling = new Color(0.12f, 0.10f, 0.20f), floor = new Color(0.48f, 0.38f, 0.25f), neon = new Color(1f, 0.20f, 0.70f), light = new Color(0.7f, 0.9f, 1f)
            };
        }
    }

    /// <summary>
    /// Một căn phòng (một tầng của toà nhà) dựng bằng code, nằm xa phố ở toạ độ x lớn (ox..ox+width), sàn ở y = 0.
    /// Toạ độ "lx" là toạ độ ngang trong phòng (0 = mép trái). Có sàn + 2 bức tường chắn (collider).
    /// </summary>
    public class Room
    {
        public readonly string id, titleVi, titleEn;
        public readonly float ox, width;
        public readonly Transform t;
        public readonly List<TownNpc> npcs = new List<TownNpc>();
        public float entryX = 3f;
        public RoomStyle style;

        public Room(string id, string titleVi, string titleEn, float ox, float width, RoomStyle style)
        {
            this.id = id; this.titleVi = titleVi; this.titleEn = titleEn; this.ox = ox; this.width = width; this.style = style;
            var go = new GameObject("Room_" + id);
            go.transform.SetParent(Interior.Root, false);
            go.transform.position = new Vector3(ox, 0f, 0f);
            t = go.transform;
            BuildShell();
        }

        public string Title { get { return Cfg.L(titleVi, titleEn); } }
        public float X(float lx) { return ox + lx; }

        public SpriteRenderer Quad(string name, float x, float y, float w, float h, Color c, int order, bool additive = false)
        {
            return Art.Spr(t, name, additive ? Art.Glow : Art.Square, new Vector2(x + w * 0.5f, y + h * 0.5f), new Vector2(w, h), c, order, additive);
        }

        /// <summary>Đặt Sprite (pivot giữa đáy) tại (lx, y).</summary>
        public SpriteRenderer Put(string name, Sprite s, float lx, float y, int order, bool flip = false)
        {
            var sr = Art.Spr(t, name, s, new Vector2(lx, y), Vector2.one, Color.white, order);
            if (flip) sr.transform.localScale = new Vector3(-1f, 1f, 1f);
            return sr;
        }

        public SpriteRenderer Glow(float lx, float y, float w, float h, Color c, int order = -44)
        {
            return Art.Spr(t, "Glow", Art.Glow, new Vector2(lx, y), new Vector2(w, h), c, order, true);
        }

        void BuildShell()
        {
            var s = style;
            Quad("Wall", -2f, -5f, width + 4f, 22f, s.wall, -50);
            Quad("Wainscot", -2f, 0f, width + 4f, 2.6f, s.wainscot, -49);
            Quad("WainscotLine", -2f, 2.6f, width + 4f, 0.12f, Cfg.A(s.neon, 0.8f), -48);
            for (float x = 4f; x < width - 1f; x += 8f) Quad("Pilaster", x, 2.7f, 0.5f, 7.8f, s.pilaster, -48);
            Quad("Ceiling", -2f, 10.6f, width + 4f, 8f, s.ceiling, -47);
            Quad("CeilingNeon", -2f, 10.5f, width + 4f, 0.14f, s.neon, -46);
            Quad("Floor", -2f, -5f, width + 4f, 5f, s.floor, -40);
            Quad("FloorEdge", -2f, -0.12f, width + 4f, 0.12f, Cfg.A(s.neon, 0.9f), -39);
            for (float x = 3f; x < width - 2f; x += 9f)
            {
                Quad("Lamp", x, 10.25f, 3f, 0.3f, s.light, -45);
                Glow(x + 1.5f, 8.4f, 8f, 5.5f, Cfg.A(s.light, 0.16f), -44);
            }
            // sàn + tường chắn
            var g = new GameObject("Ground");
            g.transform.SetParent(t, false);
            var bc = g.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(width + 8f, 4f);
            bc.offset = new Vector2(width * 0.5f, -2f);
            for (int i = 0; i < 2; i++)
            {
                var w = new GameObject(i == 0 ? "WallL" : "WallR");
                w.transform.SetParent(t, false);
                var wc = w.AddComponent<BoxCollider2D>();
                wc.size = new Vector2(1f, 40f);
                wc.offset = new Vector2(i == 0 ? -0.5f : width + 0.5f, 15f);
            }
        }

        public TownNpc Npc(Look look, float lx, TownNpc.Mode mode, string name = null, System.Action talk = null, float minLx = -1f, float maxLx = -1f, bool faceLeft = false)
        {
            var n = TownNpc.Spawn(t, look, ox + lx, mode, name, talk, minLx >= 0f ? ox + minLx : -1e9f, maxLx >= 0f ? ox + maxLx : -1e9f, faceLeft);
            npcs.Add(n);
            return n;
        }

        public Hotspot Spot(float lx, float y, string vi, string en, System.Action use, float range = 1.8f)
        {
            return Hotspot.Make(t, new Vector3(ox + lx, y, 0f), vi, en, use, range);
        }
    }

    /// <summary>
    /// Quản lý việc VÀO / RA toà nhà và đổi tầng. Phòng nằm trong cùng Scene nhưng ở x lớn (1000+); vào nhà = mờ màn hình, ẩn khung cảnh phố,
    /// dịch người chơi + giới hạn camera sang phòng, rồi sáng lại. Ra nhà làm ngược lại. Không đụng gì vào Scene của bạn (chỉ tắt / bật tạm các nhóm
    /// Background, Environment, Buildings, Props, Npcs khi ở trong nhà).
    /// </summary>
    public static class Interior
    {
        public static bool Inside { get; private set; }
        public static bool Busy { get; private set; }
        public static Room Current { get; private set; }
        public static string BuildingId = "";

        static GameObject rootGo;
        static float returnX, savedMin, savedMax;
        static readonly List<GameObject> hidden = new List<GameObject>();
        static readonly Dictionary<string, Room> rooms = new Dictionary<string, Room>();
        static readonly string[] streetGroups = { "Background", "Environment", "Buildings", "Props", "Npcs", "TownLayers" };

        public static void ResetStatics()
        {
            Inside = false; Busy = false; Current = null; BuildingId = "";
            rootGo = null;
            hidden.Clear();
            rooms.Clear();
        }

        public static Transform Root
        {
            get
            {
                if (rootGo == null) rootGo = new GameObject("Interiors");
                return rootGo.transform;
            }
        }

        /// <summary>Gốc toạ độ x của một phòng: mỗi toà nhà cách nhau 500, mỗi tầng cách nhau 100.</summary>
        public static float Origin(int building, int floor) { return 1000f + building * 500f + floor * 100f; }

        /// <summary>Lấy phòng đã dựng (hoặc dựng mới bằng build nếu chưa có / đã bị huỷ cùng Scene).</summary>
        public static Room GetRoom(string key, System.Func<Room> build)
        {
            Room r;
            if (rooms.TryGetValue(key, out r) && r != null && r.t != null) return r;
            r = build();
            rooms[key] = r;
            return r;
        }

        // ------------------------------------------------------------ chuyển cảnh mờ dần
        static IEnumerator Fade(System.Action mid)
        {
            Busy = true;
            GameManager.PlayerLocked = true;
            Cine.SetFade(1f, 0.22f);
            float t = 0f;
            while (t < 0.28f) { t += Time.unscaledDeltaTime; yield return null; }
            try { mid(); }
            catch (System.Exception e) { Debug.LogError("Neon Breaker: lỗi khi đổi phòng — " + e); }
            if (CameraRig.Inst != null) CameraRig.Inst.Snap();
            yield return null;
            if (CameraRig.Inst != null) CameraRig.Inst.Snap();
            Cine.SetFade(0f, 0.3f);
            t = 0f;
            while (t < 0.3f) { t += Time.unscaledDeltaTime; yield return null; }
            GameManager.PlayerLocked = false;
            Busy = false;
        }

        static void Place(float x)
        {
            var pl = Player.Instance;
            if (pl == null) return;
            pl.transform.position = new Vector3(x, 0.35f, 0f);
            var rb = pl.GetComponent<Rigidbody2D>();
            if (rb != null) rb.SetVel(Vector2.zero);
        }

        static void SetRoom(Room r, float lx)
        {
            Current = r;
            Cfg.LevelMinX = r.ox;
            Cfg.LevelMaxX = r.ox + r.width;
            Place(r.ox + lx);
        }

        static void HideStreet()
        {
            hidden.Clear();
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                for (int k = 0; k < streetGroups.Length; k++)
                    if (roots[i].name == streetGroups[k] && roots[i].activeSelf) { roots[i].SetActive(false); hidden.Add(roots[i]); }
        }

        static void ShowStreet()
        {
            for (int i = 0; i < hidden.Count; i++) if (hidden[i] != null) hidden[i].SetActive(true);
            hidden.Clear();
        }

        /// <summary>Vào toà nhà: build() trả về phòng đầu tiên (tầng 1), lx = chỗ đứng khi bước vào, doorX = x ngoài phố để quay ra.</summary>
        public static void Enter(string buildingId, float doorX, System.Func<Room> build, float lx = -1f)
        {
            if (Busy || Inside || GameManager.Inst == null) return;
            GameManager.Inst.StartCoroutine(Fade(() =>
            {
                returnX = doorX;
                savedMin = Cfg.LevelMinX; savedMax = Cfg.LevelMaxX;
                BuildingId = buildingId;
                HideStreet();
                Inside = true;
                var r = build();
                SetRoom(r, lx >= 0f ? lx : r.entryX);
                GameManager.Banner(r.Title, r.style.neon, 2.6f);
            }));
        }

        /// <summary>Đi sang phòng khác trong cùng toà nhà (cầu thang): dựng phòng nếu chưa có.</summary>
        public static void Go(System.Func<Room> build, float lx, bool banner = true)
        {
            if (Busy || !Inside || GameManager.Inst == null) return;
            GameManager.Inst.StartCoroutine(Fade(() =>
            {
                var r = build();
                SetRoom(r, lx);
                if (banner) GameManager.Banner(r.Title, r.style.neon, 2.0f);
            }));
        }

        /// <summary>Ra phố, đứng trước cửa toà nhà.</summary>
        public static void Exit()
        {
            if (Busy || !Inside || GameManager.Inst == null) return;
            GameManager.Inst.StartCoroutine(Fade(() =>
            {
                Inside = false;
                Current = null;
                ShowStreet();
                Cfg.LevelMinX = savedMin; Cfg.LevelMaxX = savedMax;
                Place(returnX);
                var pm = PlaceManager.Inst;
                if (pm != null) GameManager.Banner(pm.Title, pm.TitleColor, 2.0f);
            }));
        }
    }
}
