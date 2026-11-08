using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Giá kệ / kho chứa trong căn cứ: người chơi đứng gần bấm F để cất / lấy vật phẩm + linh kiện (xem StashUi, Stash).
    /// Đồ cất ở đây không mất khi chết. Gắn vào object "Shelf" của Scene Base (menu Editor làm giúp, giữ nguyên chỉnh sửa của bạn).
    /// </summary>
    public class BaseStash : MonoBehaviour, IInteractable
    {
        [Tooltip("Khoảng cách ngang tối đa để bấm F")] public float interactRange = 2.6f;
        public float interactHeight = 3f;
        [Tooltip("Vị trí nhãn gợi ý so với chân vật")] public Vector2 promptOffset = new Vector2(0f, 3f);

        public bool CanInteract { get { return !BaseUi.IsOpen && GameManager.State == GameState.Playing && !GameManager.PlayerLocked; } }
        public Vector2 PromptPos { get { return (Vector2)transform.position + promptOffset; } }
        public string PromptText { get { return Cfg.L("[F] KHO CĂN CỨ", "[F] BASE STASH"); } }
        public bool InRange(Player p) { return Interactor.Near(p, transform.position, interactRange, interactHeight); }

        void OnEnable() { Interactor.Register(this); }
        void OnDisable() { Interactor.Unregister(this); }

        public void Interact()
        {
            BaseUi.Open(BaseMode.Stash);
            Sfx.Play("charge", 0.4f, 1.4f);
        }
    }
}
