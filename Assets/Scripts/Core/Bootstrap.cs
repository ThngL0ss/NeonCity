using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Dọn các biến static trước mỗi lần Play (để chạy nhiều lần liên tiếp vẫn đúng, kể cả khi tắt "Reload Domain").
    /// Game không còn tự dựng thế giới bằng code: mở Scene Assets/NeonBreaker/Scenes/Main.unity rồi bấm Play.
    /// </summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetAllStatics()
        {
            Time.timeScale = 1f;
            Art.ResetStatics();
            Fx.ResetStatics();
            Sfx.ResetStatics();
            GameManager.ResetStatics();
            PixelScreen.Inst = null;
            CameraRig.Inst = null;
            Level.Instance = null;
            Level.Root = null;
            Player.Instance = null;
            Boss.Instance = null;
            Hittable.All.Clear();
            Enemy.Live.Clear();
            Tower.Towers.Clear();
            Projectile.Live.Clear();
            Interactor.ResetStatics();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CheckScene()
        {
            if (GameManager.Inst == null)
                Debug.LogWarning("Neon Breaker: Scene đang mở không có GameManager. " +
                                 "Hãy mở Assets/NeonBreaker/Scenes/Main.unity (chưa có thì chọn menu Tools ▸ Neon Breaker ▸ Build Project).");
        }
    }
}
