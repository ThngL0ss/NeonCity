using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Đàn ruồi nhặng bay lượn quanh một điểm (đống rác, thùng độc). Chỉ để trang trí: không va chạm, không gây hại.
    /// Gán 1–2 khung hình (vỗ cánh) vào Frames; số con, bán kính, tốc độ chỉnh trong Inspector.
    /// </summary>
    public class FlySwarm : MonoBehaviour
    {
        public Sprite[] frames;
        [Range(1, 24)] public int count = 7;
        [Tooltip("Bán kính ngang / dọc của vùng bay (đơn vị thế giới)")] public Vector2 radius = new Vector2(1.1f, 0.7f);
        [Tooltip("Tốc độ bay lượn")] public float speed = 2.2f;
        [Tooltip("Số khung hình vỗ cánh mỗi giây")] public float flapFps = 18f;
        public int sortingOrder = 20;

        SpriteRenderer[] flies;
        Vector4[] seed;           // x,y: tần số; z,w: pha

        void Start()
        {
            if (frames == null || frames.Length == 0) return;
            flies = new SpriteRenderer[count];
            seed = new Vector4[count];
            for (int i = 0; i < count; i++)
            {
                flies[i] = Art.Spr(transform, "Fly_" + i, frames[0], Vector2.zero, Vector2.one, Color.white, sortingOrder);
                seed[i] = new Vector4(Random.Range(0.6f, 1.6f), Random.Range(0.8f, 2.1f), Random.value * 6.28f, Random.value * 6.28f);
            }
        }

        void Update()
        {
            if (flies == null) return;
            float t = Time.time * speed;
            int f = frames.Length > 1 ? (int)(Time.time * flapFps) % frames.Length : 0;
            for (int i = 0; i < flies.Length; i++)
            {
                if (flies[i] == null) continue;
                var s = seed[i];
                float x = Mathf.Sin(t * s.x + s.z) * radius.x + Mathf.Sin(t * 2.3f * s.y + s.w) * radius.x * 0.25f;
                float y = Mathf.Cos(t * s.y + s.w) * radius.y + Mathf.Sin(t * 3.1f * s.x + s.z) * radius.y * 0.2f;
                flies[i].transform.localPosition = new Vector3(x, y, 0f);
                flies[i].sprite = frames[f];
            }
        }
    }
}
