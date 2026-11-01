using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>Một đoạn hoạt ảnh: danh sách khung hình (Sprite) + tốc độ.</summary>
    [System.Serializable]
    public class SpriteClip
    {
        public string name;
        public float fps = 8f;
        public bool loop = true;
        public Sprite[] frames;
    }

    /// <summary>
    /// Phát hoạt ảnh theo khung cho SpriteRenderer. Không cần Animator Controller:
    /// bạn chỉnh danh sách Clips ngay trong Inspector (kéo thả sprite vào Frames).
    /// Script gameplay chỉ gọi Play("run") ... và tên clip phải khớp.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteAnimator : MonoBehaviour
    {
        public SpriteClip[] clips;
        public string startClip;

        SpriteRenderer sr;
        Dictionary<string, SpriteClip> map;
        SpriteClip cur;
        float t;

        public string Current { get { return cur != null ? cur.name : ""; } }
        public bool Finished { get; private set; }
        public SpriteRenderer Renderer { get { EnsureInit(); return sr; } }

        void EnsureInit()
        {
            if (map != null) return;
            sr = GetComponent<SpriteRenderer>();
            map = new Dictionary<string, SpriteClip>();
            if (clips != null)
                for (int i = 0; i < clips.Length; i++)
                    if (clips[i] != null && !string.IsNullOrEmpty(clips[i].name)) map[clips[i].name] = clips[i];
        }

        void Awake()
        {
            EnsureInit();
            if (!string.IsNullOrEmpty(startClip)) Play(startClip);
        }

        public bool Has(string clip)
        {
            EnsureInit();
            return map.ContainsKey(clip);
        }

        /// <summary>Phát clip. Nếu clip đang chạy thì giữ nguyên (trừ khi restart = true).</summary>
        public void Play(string clip, bool restart = false)
        {
            EnsureInit();
            if (cur != null && cur.name == clip && !restart) return;
            SpriteClip c;
            if (!map.TryGetValue(clip, out c) || c.frames == null || c.frames.Length == 0) return;
            cur = c;
            t = 0f;
            Finished = false;
            sr.sprite = c.frames[0];
        }

        void Update()
        {
            if (cur == null || cur.frames == null || cur.frames.Length == 0) return;
            t += Time.deltaTime;
            int n = cur.frames.Length;
            int f = (int)(t * Mathf.Max(0.01f, cur.fps));
            if (cur.loop) f %= n;
            else if (f >= n) { f = n - 1; Finished = true; }
            sr.sprite = cur.frames[f];
        }
    }
}
