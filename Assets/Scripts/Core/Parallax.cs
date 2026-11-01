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

        Vector3 start;
        Camera cam;

        void Start() { start = transform.position; }

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
