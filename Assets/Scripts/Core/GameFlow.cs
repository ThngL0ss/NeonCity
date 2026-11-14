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
        public const string MenuScene = "MainMenu";
        public const string TutorialScene = "Tutorial";
        public const string StoryScene = "Story";

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
        /// <summary>Đã xem cảnh mở đầu ở khu thử nghiệm Dyber (hoặc đã từng về căn cứ) chưa.</summary>
        public static bool IntroSeen;
        /// <summary>true = lần nạp Scene kế tiếp (Main) chạy cảnh mở đầu (GameManager đọc rồi tự tắt).</summary>
        public static bool PlayIntroOnLoad;
        /// <summary>Đang chơi màn hướng dẫn (chết thì làm lại màn, không bị phạt).</summary>
        public static bool InTutorial;
        /// <summary>Địa điểm đang chơi (id trong ván này) — dùng để ghi vào file lưu.</summary>
        public static string CurrentPlaceId = LevelScene;
        /// <summary>true = lần vào căn cứ kế tiếp là về bình thường từ bản đồ (xuất hiện ở cửa ra, KHÔNG có cảnh hồi sinh). BaseManager đọc rồi tự tắt.</summary>
        public static bool NormalArrival;
        /// <summary>Các địa điểm đã mở trên bản đồ.</summary>
        static readonly HashSet<string> unlockedPlaces = new HashSet<string>();

        /// <summary>Cảnh Base đã được thêm vào Build Settings / có trong project chưa.</summary>
        public static bool BaseAvailable { get { return Application.CanStreamedLevelBeLoaded(BaseScene); } }
        public static bool MenuAvailable { get { return Application.CanStreamedLevelBeLoaded(MenuScene); } }
        public static bool TutorialAvailable { get { return Application.CanStreamedLevelBeLoaded(TutorialScene); } }
        public static bool StoryAvailable { get { return Application.CanStreamedLevelBeLoaded(StoryScene); } }
        public static bool SceneAvailable(string scene) { return Application.CanStreamedLevelBeLoaded(scene); }

        public static void ResetStatics()
        {
            InBase = false;
            Carry = false;
            BaseDialogDone = false;
            Deaths = 0;
            BaseVisited = false;
            TutorialDone = false;
            IntroSeen = false;
            PlayIntroOnLoad = false;
            NormalArrival = false;
            InTutorial = false;
            CurrentPlaceId = LevelScene;
            unlockedPlaces.Clear();
            Town.ResetAll();
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
        public static void GoToBase(bool died = false, bool normalArrival = false)
        {
            NormalArrival = normalArrival && !died;      // về bình thường (đi từ bản đồ): xuất hiện ở cửa ra, không hồi sinh
            SkillSystem.RecallAllSilently();
            BaseMissions.BankRun();
            if (died) DeathPenalty.Apply();
            else DeathPenalty.Clear();
            CurrentPlaceId = BaseScene;
            IntroSeen = true;                 // về tới căn cứ nghĩa là đã trải qua cảnh mở đầu
            InTutorial = false;
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

        // ------------------------------------------------------------ menu chính / ván mới / tiếp tục
        /// <summary>Về menu chính. Đang ở căn cứ thì lưu trước. Dữ liệu trong bộ nhớ bị xoá (nạp lại từ ô lưu khi Tiếp tục).</summary>
        public static void GoToMainMenu()
        {
            if (InBase) SaveManager.AutoSave();
            Carry = false;
            GameManager.AutoStart = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(MenuScene);
        }

        /// <summary>
        /// Ván mới: tạo ô lưu mới. watchStory = true: xem cảnh cốt truyện (Scene Story) rồi tới màn hướng dẫn, cuối cùng là cảnh mở đầu ở khu Dyber (như trước).
        /// watchStory = false (Skip): bỏ qua cốt truyện + hướng dẫn, vào thẳng căn cứ ở lần hồi sinh đầu tiên (Albert chào, nhiệm vụ đầu tiên...).
        /// Trả về false nếu cả 10 ô đã đầy.
        /// </summary>
        public static bool StartNewGame(bool watchStory = true)
        {
            int slot = SaveManager.NewGame();
            if (slot < 0) return false;
            if (!watchStory) SkipToBase();
            else if (StoryAvailable) GoToStory();
            else EnterSaved();            // chưa có Scene Story (chưa chạy menu Editor): vào màn hướng dẫn như cũ
            return true;
        }

        /// <summary>Nạp Scene cốt truyện mở đầu (Story). Giữ dữ liệu ván mới vừa tạo. Cảnh này tự gọi FinishStory khi hết / bị bỏ qua.</summary>
        static void GoToStory()
        {
            Carry = true;
            GameManager.AutoStart = false;        // Scene Story tự đặt trạng thái Cinematic
            Time.timeScale = 1f;
            InTutorial = false;
            PlayIntroOnLoad = false;
            CurrentPlaceId = StoryScene;
            SceneManager.LoadScene(StoryScene);
        }

        /// <summary>Hết cảnh cốt truyện (hoặc bấm bỏ qua): sang màn hướng dẫn (hoặc chỗ kế tiếp theo trạng thái ván).</summary>
        public static void FinishStory()
        {
            EnterSaved();
        }

        /// <summary>Skip ở menu: coi như đã qua cốt truyện + hướng dẫn + cảnh mở đầu, vào căn cứ (lần hồi sinh đầu tiên) và lưu.</summary>
        static void SkipToBase()
        {
            TutorialDone = true;
            IntroSeen = true;
            InTutorial = false;
            PlayIntroOnLoad = false;
            SaveManager.AutoSave();
            EnterSaved();                 // TutorialDone + IntroSeen => căn cứ (không có Scene Base thì vào màn chơi)
        }

        /// <summary>Tiếp tục ván ở ô slot. Trả về false nếu ô trống / hỏng.</summary>
        public static bool ContinueGame(int slot)
        {
            if (!SaveManager.Load(slot)) return false;
            EnterSaved();
            return true;
        }

        /// <summary>
        /// Vào đúng chỗ theo trạng thái đang có: chưa xong hướng dẫn → màn hướng dẫn; xong hướng dẫn mà chưa xem cảnh mở đầu → khu Dyber (có cảnh mở đầu);
        /// còn lại → căn cứ. Chưa có Scene tương ứng thì bỏ qua bước đó.
        /// </summary>
        static void EnterSaved()
        {
            Carry = true;
            GameManager.AutoStart = true;
            Time.timeScale = 1f;
            InTutorial = false;
            PlayIntroOnLoad = false;
            if (!TutorialDone)
            {
                if (TutorialAvailable)
                {
                    InTutorial = true;
                    CurrentPlaceId = TutorialScene;
                    SceneManager.LoadScene(TutorialScene);
                    return;
                }
                TutorialDone = true;          // chưa có Scene hướng dẫn: coi như đã xong
            }
            if (!IntroSeen && SceneAvailable(LevelScene))
            {
                PlayIntroOnLoad = true;
                CurrentPlaceId = LevelScene;
                SceneManager.LoadScene(LevelScene);
                return;
            }
            if (BaseAvailable)
            {
                CurrentPlaceId = BaseScene;
                SceneManager.LoadScene(BaseScene);
                return;
            }
            CurrentPlaceId = LevelScene;
            SceneManager.LoadScene(LevelScene);
        }

        /// <summary>Chết trong màn hướng dẫn: nạp lại màn, giữ dữ liệu, không bị phạt.</summary>
        public static void RetryTutorial()
        {
            SkillSystem.RecallAllSilently();
            Carry = true;
            GameManager.AutoStart = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(TutorialScene);
        }

        /// <summary>Xong màn hướng dẫn (đi qua cánh cửa): lưu game rồi vào khu Dyber với cảnh mở đầu.</summary>
        public static void FinishTutorial()
        {
            SkillSystem.RecallAllSilently();
            TutorialDone = true;
            InTutorial = false;
            SaveManager.AutoSave();
            if (SceneAvailable(LevelScene))
            {
                PlayIntroOnLoad = true;
                CurrentPlaceId = LevelScene;
                Carry = true;
                GameManager.AutoStart = true;
                Time.timeScale = 1f;
                SceneManager.LoadScene(LevelScene);
            }
            else if (BaseAvailable) GoToBase();
        }

        /// <summary>Từ căn cứ tới một địa điểm trên bản đồ (Scene trùng id). Giữ dữ liệu, lưu game trước khi đi.</summary>
        public static bool GoToPlace(string sceneId)
        {
            if (string.IsNullOrEmpty(sceneId) || !SceneAvailable(sceneId)) return false;
            try
            {
                SkillSystem.RecallAllSilently();
                BaseMissions.BankRun();
                CurrentPlaceId = sceneId;
                SaveManager.AutoSave();
            }
            catch (System.Exception e)
            {
                Debug.LogError("Neon Breaker: lỗi khi chuẩn bị rời căn cứ (vẫn tiếp tục nạp Scene) — " + e);
            }
            Carry = true;
            GameManager.AutoStart = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneId);
            return true;
        }
    }
}
