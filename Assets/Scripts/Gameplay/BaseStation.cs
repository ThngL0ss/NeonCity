using UnityEngine;

namespace NeonBreaker
{
    public enum StationKind { Craft, Fusion, Door }

    /// <summary>
    /// Máy / cửa trong căn cứ mà người chơi bấm F để dùng: Craft = bàn chế tác, Fusion = máy ghép linh kiện, Door = cửa ra (mở bản đồ).
    /// Đặt trong Scene Base; vầng sáng (object con "Glow") nhấp nháy, sáng hơn khi bảng đang mở.
    /// </summary>
    public class BaseStation : MonoBehaviour, IInteractable
    {
        public StationKind kind;
        [Tooltip("Khoảng cách ngang tối đa để bấm F")] public float interactRange = 2.8f;
        public float interactHeight = 3f;
        [Tooltip("Vị trí nhãn gợi ý so với chân vật")] public Vector2 promptOffset = new Vector2(0f, 3.4f);
        public SpriteRenderer glow;
        public Color glowColor = Cfg.Cyan;

        float t;

        /// <summary>Cửa ra khoá cho tới khi nhận nhiệm vụ đầu tiên (nói chuyện với Albert rồi nhận ở hộp thư nhiệm vụ).</summary>
        public bool Locked { get { return kind == StationKind.Door && !BaseMissions.FirstMissionReceived; } }

        public bool CanInteract { get { return !BaseUi.IsOpen && GameManager.State == GameState.Playing && !GameManager.PlayerLocked; } }
        public Vector2 PromptPos { get { return (Vector2)transform.position + promptOffset; } }
        public bool InRange(Player p) { return Interactor.Near(p, transform.position, interactRange, interactHeight); }

        public string PromptText
        {
            get
            {
                switch (kind)
                {
                    case StationKind.Craft: return Cfg.L("[F] BÀN CHẾ TÁC", "[F] CRAFTING BENCH");
                    case StationKind.Fusion: return Cfg.L("[F] MÁY GHÉP LINH KIỆN", "[F] PART FUSION MACHINE");
                    default:
                        if (Locked) return Cfg.L("CỬA ĐANG KHOÁ — gặp Albert nhận nhiệm vụ", "DOOR LOCKED — get a mission from Albert first");
                        return Cfg.L("[F] RA KHỎI CĂN CỨ", "[F] LEAVE THE BASE");
                }
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
            if (Locked)
            {
                Fx.Text(PromptPos, Cfg.L("Chưa nhận nhiệm vụ — gặp Albert trước", "No mission yet — see Albert first"), Cfg.Orange, 1.4f);
                Sfx.Play("click", 0.7f, 0.6f);
                return;
            }
            switch (kind)
            {
                case StationKind.Craft: BaseUi.Open(BaseMode.Craft); break;
                case StationKind.Fusion: BaseUi.Open(BaseMode.Fusion); break;
                default: BaseUi.Open(BaseMode.Map); break;
            }
            Sfx.Play("charge", 0.4f, 1.6f);
        }

        void Update()
        {
            t += Time.deltaTime;
            bool active = (kind == StationKind.Craft && BaseUi.Mode == BaseMode.Craft) || (kind == StationKind.Fusion && BaseUi.Mode == BaseMode.Fusion) || (kind == StationKind.Door && BaseUi.Mode == BaseMode.Map);
            if (glow != null)
            {
                float a = 0.22f + 0.1f * Mathf.Sin(t * 2.4f) + (active ? 0.35f : 0f);
                glow.color = Cfg.A(Locked ? Cfg.Red : glowColor, a);
            }
            if (active && Random.value < 0.4f)
                Fx.Emit(Fx.S_Glow, (Vector2)transform.position + new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(0.5f, 2.6f)), new Vector2(0f, Random.Range(1f, 3f)),
                        new Vector2(0.3f, 0.3f), Vector2.zero, Cfg.A(glowColor, 0.9f), Cfg.A(glowColor, 0f), 0.6f);
        }
    }
}
