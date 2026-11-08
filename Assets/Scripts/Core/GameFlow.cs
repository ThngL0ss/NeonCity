using System.Collections.Generic;
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
        /// <summary>Đã xong màn hướng dẫn (ván mới bắt đầu bằng màn hướng dẫn).</summary>
        public static bool TutorialDone;
        /// <summary>Địa điểm đang chơi (id trong ván này) — dùng để ghi vào file lưu.</summary>
        public static string CurrentPlaceId = LevelScene;
        /// <summary>Các địa điểm đã mở trên bản đồ.</summary>
        static readonly HashSet<string> unlockedPlaces = new HashSet<string>();

        /// <summary>Cảnh Base đã được thêm vào Build Settings / có trong project chưa.</summary>
        public static bool BaseAvailable { get { return Application.CanStreamedLevelBeLoaded(BaseScene); } }

        public static void ResetStatics()
        {
            InBase = false;
            Carry = false;
            BaseDialogDone = false;
            Deaths = 0;
            BaseVisited = false;
            TutorialDone = false;
            CurrentPlaceId = LevelScene;
            unlockedPlaces.Clear();
            BaseMissions.ResetAll();
            Stash.ResetAll();
            PlayerProgress.Reset();
            SaveManager.Detach();
        }

        public static bool IsPlaceUnlocked(string id) { return unlockedPlaces.Contains(id); }
        public static void UnlockPlace(string id) { if (!string.IsNullOrEmpty(id)) unlockedPlaces.Add(id); }
        public static void ExportPlaces(List<string> dst) { dst.Clear(); foreach (var id in unlockedPlaces) dst.Add(id); }
        public static void ImportPlaces(List<string> src)
        {
            unlockedPlaces.Clear();
            if (src != null) for (int i = 0; i < src.Count; i++) if (!string.IsNullOrEmpty(src[i])) unlockedPlaces.Add(src[i]);
        }

        /// <summary>
        /// Chuyển về căn cứ. Giữ dữ liệu. died = true (bị hạ gục): áp hình phạt chết (DeathPenalty) rồi tự lưu game.
        /// </summary>
        public static void GoToBase(bool died = false)
        {
            SkillSystem.RecallAllSilently();
            BaseMissions.BankRun();
            if (died) DeathPenalty.Apply();
            else DeathPenalty.Clear();
            CurrentPlaceId = BaseScene;
            SaveManager.AutoSave();
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
            CurrentPlaceId = LevelScene;
            SaveManager.AutoSave();           // lưu lại khi rời căn cứ (kho căn cứ, đồ mang theo...)
            Carry = true;
            GameManager.AutoStart = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(LevelScene);
        }
    }
}
