using System.Collections;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>ToBase / Exit = cửa ra-vào của khu vực (cùng một việc: bấm F mở bản đồ); EndTutorial = cánh cửa cuối màn hướng dẫn.</summary>
    public enum GateKind { ToBase, EndTutorial, Exit }

    /// <summary>
    /// Cổng / cửa bấm F để đi qua:
    ///  • Cửa khu vực (ToBase / Exit) — MỖI khu vực (Phố Neon, Rìa Phố Neon, Khu thử nghiệm Dyber) có 2 cửa ở đầu và cuối. Người chơi vào khu vực thì xuất hiện ở cửa đầu;
    ///    muốn đi, bấm F ở cửa nào cũng được: mở BẢN ĐỒ (như cửa ra của căn cứ) để chọn điểm đến, kể cả Căn cứ. Giữ nguyên đồ đang mang.
    ///  • EndTutorial — cánh cửa cuối màn hướng dẫn: bước qua thì xong huấn luyện và chạy cảnh mở đầu (khoá cho tới khi làm xong các bước).
    /// Vầng sáng (object con "Glow", tuỳ chọn) nhấp nháy.
    /// PlaceManager tự tạo cửa còn thiếu ở đầu / cuối khu vực (nhân bản cửa có sẵn) nên Scene cũ không phải sửa.
    /// </summary>
    public class PlaceGate : MonoBehaviour, IInteractable
    {
        public GateKind kind = GateKind.Exit;
        [Tooltip("Khoảng cách ngang tối đa để bấm F")] public float interactRange = 2.4f;
        public float interactHeight = 3f;
        [Tooltip("Vị trí nhãn gợi ý so với chân vật")] public Vector2 promptOffset = new Vector2(0f, 4.4f);
        public SpriteRenderer glow;
        public Color glowColor = new Color(0.45f, 1f, 0.4f, 1f);
        [Tooltip("Cửa đóng chặt (ví dụ cửa cuối khu boss: chỉ mở được sau khi hạ boss)")] public bool bossLock;

        bool leaving;
        float t;

        public bool IsMapGate { get { return kind != GateKind.EndTutorial; } }
        public bool Locked
        {
            get
            {
                if (kind == GateKind.EndTutorial) return TutorialManager.Inst != null && !TutorialManager.Inst.DoorOpen;
                return bossLock;
            }
        }

        public bool CanInteract { get { return !leaving && !BaseUi.IsOpen && GameManager.State == GameState.Playing && !GameManager.PlayerLocked; } }
        public Vector2 PromptPos { get { return (Vector2)transform.position + promptOffset; } }
        public bool InRange(Player p) { return Interactor.Near(p, transform.position, interactRange, interactHeight); }

        public string PromptText
        {
            get
            {
                if (IsMapGate)
                {
                    if (Locked) return Cfg.L("CỬA ĐÓNG — hạ boss để mở", "DOOR SEALED — defeat the boss to open it");
                    return Cfg.L("[F] RA KHỎI KHU VỰC (MỞ BẢN ĐỒ)", "[F] LEAVE THE AREA (OPEN MAP)");
                }
                if (Locked) return Cfg.L("CỬA ĐANG KHOÁ — làm xong các bước huấn luyện", "DOOR LOCKED — finish the training steps");
                return Cfg.L("[F] BƯỚC QUA CỬA", "[F] STEP THROUGH THE DOOR");
            }
        }

        void OnEnable() { Interactor.Register(this); }
        void OnDisable() { Interactor.Unregister(this); }

        void Awake()
        {
            if (glow == null) { var g = transform.Find("Glow"); if (g != null) glow = g.GetComponent<SpriteRenderer>(); }
            t = Random.value * 6f;
        }

        public void Interact()
        {
            if (leaving) return;
            if (Locked)
            {
                Fx.Text(PromptPos, IsMapGate ? Cfg.L("Cửa đã đóng", "The door is sealed") : Cfg.L("Chưa xong huấn luyện", "Training not finished"), Cfg.Orange, 1.1f);
                Sfx.Play("click", 0.7f, 0.6f);
                return;
            }
            if (IsMapGate)
            {
                BaseUi.Open(BaseMode.Map);                 // mở bản đồ (mũi tên đứng ở nơi đang ở); đi hay ở do người chơi chọn
                Sfx.Play("charge", 0.5f, 1.5f);
                return;
            }
            leaving = true;
            GameManager.PlayerLocked = true;
            Sfx.Play("charge", 0.8f, 1.3f);
            StartCoroutine(Go());
        }

        IEnumerator Go()
        {
            Cine.SetFade(1f, 0.7f);
            float w = 0f;
            while (w < 0.8f) { w += Time.unscaledDeltaTime; yield return null; }
            GameFlow.FinishTutorial();
        }

        void Update()
        {
            t += Time.deltaTime;
            if (glow == null) return;
            float a = 0.22f + 0.1f * Mathf.Sin(t * 2.4f) + (leaving ? 0.4f : 0f);
            Color c = Locked ? Cfg.Red : glowColor;
            glow.color = Cfg.A(c, a);
        }

        // ------------------------------------------------------------ tạo cửa còn thiếu
        /// <summary>
        /// Tạo một cửa khu vực tại x (chân cửa y = 0). Có cửa mẫu thì nhân bản (giống hệt cửa đã có trong Scene), không thì vẽ cửa đơn giản bằng hình khối.
        /// </summary>
        public static PlaceGate Make(string name, float x, PlaceGate template, bool bossLocked = false)
        {
            PlaceGate g;
            if (template != null)
            {
                var go = Instantiate(template.gameObject, new Vector3(x, template.transform.position.y, 0f), Quaternion.identity, template.transform.parent);
                go.name = name;
                g = go.GetComponent<PlaceGate>();
            }
            else
            {
                var go = new GameObject(name);
                go.transform.position = new Vector3(x, 0f, 0f);
                g = go.AddComponent<PlaceGate>();
                g.promptOffset = new Vector2(0f, 5.1f);
                Art.Init();
                var tr = go.transform;
                Art.Spr(tr, "Back", Art.Square, new Vector2(0f, 2.3f), new Vector2(2.6f, 4.6f), new Color(0.04f, 0.06f, 0.10f, 1f), 9);
                Art.Spr(tr, "PostL", Art.Square, new Vector2(-1.35f, 2.4f), new Vector2(0.3f, 4.8f), new Color(0.26f, 0.32f, 0.42f, 1f), 10);
                Art.Spr(tr, "PostR", Art.Square, new Vector2(1.35f, 2.4f), new Vector2(0.3f, 4.8f), new Color(0.26f, 0.32f, 0.42f, 1f), 10);
                Art.Spr(tr, "Lintel", Art.Square, new Vector2(0f, 4.75f), new Vector2(3.0f, 0.34f), new Color(0.34f, 0.42f, 0.55f, 1f), 10);
                Art.Spr(tr, "Light", Art.Square, new Vector2(0f, 4.75f), new Vector2(0.9f, 0.12f), new Color(0.45f, 1f, 0.4f, 1f), 11);
                g.glow = Art.Spr(tr, "Glow", Art.Glow, new Vector2(0f, 2.2f), new Vector2(3.6f, 5f), new Color(0.45f, 1f, 0.4f, 0.25f), 11, true);
            }
            g.kind = GateKind.Exit;
            g.bossLock = bossLocked;
            return g;
        }

        /// <summary>
        /// Khu vực nào cũng có đủ 2 cửa: một ở đầu (người chơi xuất hiện cạnh đó) và một ở cuối. Cửa nào đã có sẵn trong Scene thì dùng, thiếu thì tạo.
        /// startX / endX = chân cửa đầu / cuối. Trả về cửa đầu.
        /// </summary>
        public static PlaceGate EnsureTwoGates(float startX, float endX, bool bossLockEnd)
        {
            var all = FindObjectsOfType<PlaceGate>();
            PlaceGate start = null, end = null, template = null;
            float mid = (startX + endX) * 0.5f;
            for (int i = 0; i < all.Length; i++)
            {
                var g = all[i];
                if (g == null || !g.IsMapGate) continue;
                if (template == null) template = g;
                float x = g.transform.position.x;
                if (x < mid) { if (start == null || x < start.transform.position.x) start = g; }
                else { if (end == null || x > end.transform.position.x) end = g; }
            }
            if (start == null) start = Make("Gate_Start", startX, template);
            if (end == null) end = Make("Gate_End", endX, template, bossLockEnd);
            return start;
        }
    }
}
