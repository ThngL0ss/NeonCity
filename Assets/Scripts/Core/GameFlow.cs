using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonBreaker
{
    /// <summary>
    /// Luồng chuyển cảnh giữa màn chơi (Main) và căn cứ (Base). Là static nên dữ liệu (kho đồ, linh kiện, minibot, xu, dầu)
    /// được GIỮ NGUYÊN khi chuyển cảnh; chỉ "Chơi lại" (R ở màn thắng) mới xoá sạch.
    /// </summary>
    public static class GameFlow
    {
        public const string LevelScene = "Main";
        public const string BaseScene = "Base";

        /// <summary>Đang ở căn cứ (BaseManager đặt khi cảnh Base nạp xong).</summary>
        public static bool InBase;
        /// <summary>true = lần nạp cảnh kế tiếp giữ nguyên dữ liệu (GameManager đọc rồi tự tắt).</summary>
        public static bool Carry;
        /// <summary>Đã xem đoạn thoại đầu tiên khi về căn cứ chưa.</summary>
        public static bool BaseDialogDone;
        /// <summary>Số lần đã gục ngã (để Albert nhắc).</summary>
        public static int Deaths;
        /// <summary>Đã tới căn cứ ít nhất 1 lần.</summary>
        public static bool BaseVisited;

        /// <summary>Cảnh Base đã được thêm vào Build Settings / có trong project chưa.</summary>
        public static bool BaseAvailable { get { return Application.CanStreamedLevelBeLoaded(BaseScene); } }

        public static void ResetStatics()
        {
            InBase = false;
            Carry = false;
            BaseDialogDone = false;
            Deaths = 0;
            BaseVisited = false;
            BaseMissions.ResetAll();
        }

        /// <summary>Chuyển về căn cứ (sau khi chết). Giữ dữ liệu.</summary>
        public static void GoToBase()
        {
            SkillSystem.RecallAllSilently();
            BaseMissions.BankRun();
            Carry = true;
            GameManager.AutoStart = true;         // vào thẳng căn cứ (không hiện menu chính)
            Time.timeScale = 1f;
            SceneManager.LoadScene(BaseScene);
        }

        /// <summary>Từ căn cứ ra màn chơi. Giữ dữ liệu; vào thẳng màn chơi (không hiện menu / cinematic mở đầu).</summary>
        public static void GoToLevel()
        {
            SkillSystem.RecallAllSilently();
            BaseMissions.BankRun();
            Carry = true;
            GameManager.AutoStart = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(LevelScene);
        }
    }
}
