using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Lớp nền cuộn thị sai. factorX = 0 → đứng yên trong thế giới; 1 → dính vào camera.
    /// Số nhỏ = nền xa (trôi chậm). followY = bám theo camera theo chiều dọc (dùng cho bầu trời).
    /// </summary>
    public class Parallax : MonoBehaviour
    {
        public float factorX = 0.2f;
        public bool followY;
        public float yOffset;
        [Tooltip("Làm tròn vị trí theo lưới pixel (1/16) để nền không rung")]
        public bool snapToPixel = true;
        [Tooltip("Làm tối lớp nền này để nổi bật phần phía trước (0 = giữ nguyên, 0.5 = tối đi một nửa). Chỉ áp dụng lúc chạy game, không sửa màu đã lưu. Còn một mức tối chung cho mọi lớp ở GameManager ▸ Background Extra Darken")]
        [Range(0f, 0.9f)] public float darken = 0.45f;

        Vector3 start;
        Camera cam;

        void Start()
        {
            start = transform.position;
            // tối riêng từng lớp (Darken) nhân với mức tối chung (GameManager ▸ Background Extra Darken)
            float extra = GameManager.Inst != null ? GameManager.Inst.backgroundExtraDarken : 0f;
            float k = (1f - darken) * (1f - extra);
            if (k < 0.999f)
            {
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Color c = sr.color;
                    sr.color = new Color(c.r * k, c.g * k, c.b * k, c.a);
                }
            }
        }

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            Vector3 c = cam.transform.position;
            float x = start.x + c.x * factorX;
            float y = followY ? c.y + yOffset : start.y;
            if (snapToPixel)
            {
                x = Mathf.Round(x * 16f) / 16f;
                y = Mathf.Round(y * 16f) / 16f;
            }
            transform.position = new Vector3(x, y, start.z);
        }
    }
}
