using System.Collections;
using UnityEngine;

namespace NeonBreaker
{
    public enum GateKind { ToBase, EndTutorial }

    /// <summary>
    /// Cổng / cửa bấm F để đi qua:
    ///  • ToBase     — cổng về căn cứ của Albert ở các địa điểm (Phố Neon, Rìa Phố Neon). Giữ nguyên đồ đang mang (không phải chết nên không bị phạt).
    ///  • EndTutorial — cánh cửa cuối màn hướng dẫn: bước qua thì xong huấn luyện và chạy cảnh mở đầu (khoá cho tới khi làm xong các bước).
    /// Vầng sáng (object con "Glow", tuỳ chọn) nhấp nháy.
    /// </summary>
    public class PlaceGate : MonoBehaviour, IInteractable
    {
        public GateKind kind = GateKind.ToBase;
        [Tooltip("Khoảng cách ngang tối đa để bấm F")] public float interactRange = 2.4f;
        public float interactHeight = 3f;
        [Tooltip("Vị trí nhãn gợi ý so với chân vật")] public Vector2 promptOffset = new Vector2(0f, 4.4f);
        public SpriteRenderer glow;
        public Color glowColor = new Color(0.45f, 1f, 0.4f, 1f);

        bool leaving;
        float t;

        public bool Locked { get { return kind == GateKind.EndTutorial && TutorialManager.Inst != null && !TutorialManager.Inst.DoorOpen; } }

        public bool CanInteract { get { return !leaving && !BaseUi.IsOpen && GameManager.State == GameState.Playing && !GameManager.PlayerLocked; } }
        public Vector2 PromptPos { get { return (Vector2)transform.position + promptOffset; } }
        public bool InRange(Player p) { return Interactor.Near(p, transform.position, interactRange, interactHeight); }

        public string PromptText
        {
            get
            {
                if (kind == GateKind.ToBase) return Cfg.L("[F] VỀ CĂN CỨ", "[F] BACK TO THE BASE");
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
                Fx.Text(PromptPos, Cfg.L("Chưa xong huấn luyện", "Training not finished"), Cfg.Orange, 1.1f);
                Sfx.Play("click", 0.7f, 0.6f);
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
            if (kind == GateKind.ToBase) GameFlow.GoToBase(false);
            else GameFlow.FinishTutorial();
        }

        void Update()
        {
            t += Time.deltaTime;
            if (glow == null) return;
            float a = 0.22f + 0.1f * Mathf.Sin(t * 2.4f) + (leaving ? 0.4f : 0f);
            Color c = Locked ? Cfg.Red : glowColor;
            glow.color = Cfg.A(c, a);
        }
    }
}
