using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Điểm bấm F bất kỳ trong thị trấn / toà nhà: cửa ra vào, cầu thang, bục trưng bày... Chỉ gọi hàm onUse khi người chơi bấm F.
    /// Tạo bằng Hotspot.Make (code); có thể bật / tắt bằng canUse để ẩn gợi ý khi chưa dùng được.
    /// </summary>
    public class Hotspot : MonoBehaviour, IInteractable
    {
        public string promptVi = "", promptEn = "";
        public System.Action onUse;
        public System.Func<bool> canUse;
        public float range = 1.8f, height = 3f;
        public Vector2 promptOffset = new Vector2(0f, 3.4f);

        public bool CanInteract
        {
            get
            {
                if (Interior.Busy || BaseUi.IsOpen || GameManager.State != GameState.Playing || GameManager.PlayerLocked) return false;
                return canUse == null || canUse();
            }
        }
        public Vector2 PromptPos { get { return (Vector2)transform.position + promptOffset; } }
        public string PromptText { get { return "[F] " + Cfg.L(promptVi, promptEn); } }
        public bool InRange(Player p) { return Interactor.Near(p, transform.position, range, height); }
        public void Interact() { Sfx.Play("click", 0.6f, 1.2f); if (onUse != null) onUse(); }

        void OnEnable() { Interactor.Register(this); }
        void OnDisable() { Interactor.Unregister(this); }

        public static Hotspot Make(Transform parent, Vector3 worldPos, string vi, string en, System.Action use, float range = 1.8f)
        {
            var go = new GameObject("Hotspot_" + vi);
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.position = worldPos;
            var h = go.AddComponent<Hotspot>();
            h.promptVi = vi; h.promptEn = en; h.onUse = use; h.range = range;
            return h;
        }
    }
}
