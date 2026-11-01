using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Render thế giới game ở độ phân giải thấp (mặc định 480x270, 16 pixel = 1 đơn vị) rồi phóng to bằng Point filter
    /// => toàn bộ hình ảnh, hạt, hiệu ứng đều ra đúng kiểu pixel art. HUD vẫn vẽ sắc nét ở độ phân giải thật.
    /// Gắn vào Main Camera. Muốn tắt hiệu ứng pixel: bỏ tick "Enable Pixel Render".
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PixelScreen : MonoBehaviour
    {
        public static PixelScreen Inst;

        public bool enablePixelRender = true;
        public int refWidth = 480;
        public int refHeight = 270;
        public float pixelsPerUnit = 16f;
        [Tooltip("Bật = chỉ phóng to theo số nguyên (pixel đều tuyệt đối, có viền đen); tắt = lấp đầy cửa sổ.")]
        public bool integerScale = false;

        Camera cam;
        Camera screenCam;
        RenderTexture rt;

        public Rect ScreenRect { get; private set; }
        public float OrthoSize { get { return refHeight / (2f * pixelsPerUnit); } }
        public Camera WorldCamera { get { return cam; } }

        void Awake()
        {
            Inst = this;
            cam = GetComponent<Camera>();
            if (!enablePixelRender)
            {
                cam.orthographicSize = OrthoSize;
                return;
            }

            rt = new RenderTexture(refWidth, refHeight, 16, RenderTextureFormat.ARGB32);
            rt.name = "NeonPixelRT";
            rt.filterMode = FilterMode.Point;
            rt.antiAliasing = 1;
            rt.useMipMap = false;
            rt.Create();
            cam.targetTexture = rt;
            cam.orthographicSize = OrthoSize;

            // Camera phụ chỉ để Game view có thứ hiển thị ra màn hình (nền đen, không vẽ gì)
            var go = new GameObject("ScreenCam");
            go.transform.SetParent(transform, false);
            screenCam = go.AddComponent<Camera>();
            screenCam.clearFlags = CameraClearFlags.SolidColor;
            screenCam.backgroundColor = Color.black;
            screenCam.cullingMask = 0;
            screenCam.depth = cam.depth - 100f;
            screenCam.orthographic = true;
            UpdateRect();
        }

        void OnDestroy()
        {
            if (Inst == this) Inst = null;
            if (cam != null) cam.targetTexture = null;
            if (rt != null) { rt.Release(); Destroy(rt); rt = null; }
        }

        void UpdateRect()
        {
            float sw = Screen.width, sh = Screen.height;
            float s = Mathf.Min(sw / refWidth, sh / refHeight);
            if (integerScale && s >= 1f) s = Mathf.Floor(s);
            float w = refWidth * s, h = refHeight * s;
            ScreenRect = new Rect((sw - w) * 0.5f, (sh - h) * 0.5f, w, h);
        }

        void Update() { UpdateRect(); }

        void OnGUI()
        {
            if (rt == null || Event.current.type != EventType.Repaint) return;
            UpdateRect();
            int d = GUI.depth;
            GUI.depth = 1000;                              // vẽ dưới cùng, HUD (depth 0) vẽ đè lên
            GUI.DrawTexture(ScreenRect, rt, ScaleMode.StretchToFill, false);
            GUI.depth = d;
        }

        /// <summary>Đổi tọa độ thế giới sang tọa độ IMGUI (gốc ở góc trên trái màn hình).</summary>
        public static Vector2 WorldToGui(Vector3 world)
        {
            var main = Camera.main;
            if (Inst == null || Inst.rt == null)
            {
                if (main == null) return Vector2.zero;
                Vector3 sp = main.WorldToScreenPoint(world);
                return new Vector2(sp.x, Screen.height - sp.y);
            }
            Vector3 p = Inst.cam.WorldToScreenPoint(world);
            float u = p.x / Inst.refWidth;
            float v = p.y / Inst.refHeight;
            Rect r = Inst.ScreenRect;
            return new Vector2(r.x + u * r.width, r.y + (1f - v) * r.height);
        }
    }
}
