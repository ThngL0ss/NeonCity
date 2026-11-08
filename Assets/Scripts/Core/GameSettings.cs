using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Cài đặt người chơi (lưu bằng PlayerPrefs, dùng chung mọi ván): ngôn ngữ, âm lượng nhạc, âm lượng hiệu ứng, toàn màn hình.
    /// Chỉnh ở menu chính ▸ CÀI ĐẶT (phím V trong game cũng đổi ngôn ngữ và được nhớ).
    /// </summary>
    public static class GameSettings
    {
        const string KMusic = "nb.music", KSfx = "nb.sfx", KFull = "nb.full", KLang = "nb.lang";
        static bool loaded;

        public static void ResetStatics() { loaded = false; }

        /// <summary>
        /// GameManager.Awake gọi mỗi khi nạp Scene. Lần đầu của lượt chạy: lấy ngôn ngữ mặc định từ Inspector (GameManager ▸ Start Vietnamese)
        /// rồi đè bằng giá trị đã lưu. Các Scene sau giữ nguyên lựa chọn hiện tại của người chơi.
        /// </summary>
        public static void OnSceneAwake(bool startVietnamese)
        {
            if (loaded) return;
            loaded = true;
            Cfg.Vietnamese = startVietnamese;
            try
            {
                if (PlayerPrefs.HasKey(KMusic)) Sfx.MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KMusic));
                if (PlayerPrefs.HasKey(KSfx)) Sfx.SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KSfx));
                if (PlayerPrefs.HasKey(KLang)) Cfg.Vietnamese = PlayerPrefs.GetInt(KLang) != 0;
                if (PlayerPrefs.HasKey(KFull)) Screen.fullScreen = PlayerPrefs.GetInt(KFull) != 0;
            }
            catch (System.Exception e) { Debug.LogWarning("Neon Breaker: không đọc được cài đặt — " + e.Message); }
        }

        /// <summary>Bật / tắt toàn màn hình và nhớ lựa chọn.</summary>
        public static void SetFullscreen(bool on)
        {
            Screen.fullScreen = on;
            try { PlayerPrefs.SetInt(KFull, on ? 1 : 0); PlayerPrefs.Save(); } catch (System.Exception) { }
        }

        public static void Save()
        {
            try
            {
                PlayerPrefs.SetFloat(KMusic, Sfx.MusicVolume);
                PlayerPrefs.SetFloat(KSfx, Sfx.SfxVolume);
                PlayerPrefs.SetInt(KLang, Cfg.Vietnamese ? 1 : 0);
                PlayerPrefs.Save();
            }
            catch (System.Exception e) { Debug.LogWarning("Neon Breaker: không lưu được cài đặt — " + e.Message); }
        }
    }
}
