using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Danh sách âm thanh + nhạc nền. File asset: Assets/Resources/NeonBreaker/AudioBank.asset.
    /// Muốn đổi âm thanh: kéo AudioClip của bạn vào ô Clip tương ứng (giữ nguyên Id).
    /// Id "music_explore" và "music_boss" là hai bản nhạc nền (tự lặp).
    /// </summary>
    [CreateAssetMenu(menuName = "Neon Breaker/Audio Bank", fileName = "AudioBank")]
    public class AudioBank : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public string id;
            public AudioClip clip;
        }

        public List<Entry> entries = new List<Entry>();

        public static AudioBank Load()
        {
            return Resources.Load<AudioBank>("NeonBreaker/AudioBank");
        }

        public AudioClip Get(string id)
        {
            for (int i = 0; i < entries.Count; i++)
                if (entries[i] != null && entries[i].id == id) return entries[i].clip;
            return null;
        }
    }
}
