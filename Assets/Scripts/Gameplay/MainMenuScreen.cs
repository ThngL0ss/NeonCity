using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Đặt trong Scene MainMenu: bật menu chính (MainMenuUi) khi Scene nạp và tắt khi rời Scene.
    /// Scene này chỉ cần Camera + GameManager + Hud + (Sfx) — menu Editor "Cập nhật Nhiệm vụ + Phố Neon + Lưu game" tạo sẵn.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class MainMenuScreen : MonoBehaviour
    {
        void Start()
        {
            GameManager.State = GameState.Menu;
            MainMenuUi.Open();
        }

        void OnDestroy() { MainMenuUi.Close(); }
    }
}
