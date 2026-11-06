using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Camera 2D bám theo người chơi: nhìn trước theo hướng chạy, rung khi trúng đòn.
    /// Kích thước camera do PixelScreen quyết định (480x270 pixel, 16 px = 1 đơn vị).
    /// Vị trí được làm tròn theo lưới pixel để hình không bị rung.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraRig : MonoBehaviour
    {
        public static CameraRig Inst;

        public Transform target;
        public bool bossMode;                       // (dành cho mở rộng) hiện không đổi zoom để giữ pixel đều
        [Tooltip("Số đơn vị mặt đất nhìn thấy phía dưới chân nhân vật")]
        public float groundMargin = 3.4f;
        public float lookAhead = 2.6f;

        Camera cam;
        float lookX, velX, velY;
        Vector2 pos;
        bool first = true;

        void Awake()
        {
            Inst = this;
            cam = GetComponent<Camera>();
        }

        void OnDestroy() { if (Inst == this) Inst = null; }

        void LateUpdate()
        {
            if (cam == null) return;
            if (target == null && Player.Instance != null) target = Player.Instance.transform;
            if (target == null) { Apply(pos); return; }

            float size = PixelScreen.Inst != null ? PixelScreen.Inst.OrthoSize : cam.orthographicSize;
            cam.orthographicSize = size;
            float halfW = size * cam.aspect;

            float face = 0f;
            var pl = Player.Instance;
            if (pl != null) face = pl.Facing * lookAhead + pl.VelocityX * 0.12f;
            lookX = Mathf.Lerp(lookX, face, 1f - Mathf.Exp(-3.0f * Time.unscaledDeltaTime));

            float tx = target.position.x + lookX;
            float ty = Mathf.Max(size - groundMargin, target.position.y + 1.4f);
            float sm = 1f;
            if (Cine.HasFocus)
            {
                // cinematic: camera bay tới điểm chỉ định
                tx = Cine.FocusPos.x;
                ty = Mathf.Max(size - groundMargin, Cine.FocusPos.y);
                sm = Cine.CamSmooth;
            }

            float minX = Cfg.LevelMinX + halfW;
            float maxX = Cfg.LevelMaxX - halfW;
            if (minX > maxX) { minX = maxX = (Cfg.LevelMinX + Cfg.LevelMaxX) * 0.5f; }
            tx = Mathf.Clamp(tx, minX, maxX);

            if (first)
            {
                pos = new Vector2(tx, ty);
                first = false;
            }
            else
            {
                pos.x = Mathf.SmoothDamp(pos.x, tx, ref velX, 0.16f * sm, Mathf.Infinity, Time.unscaledDeltaTime);
                pos.y = Mathf.SmoothDamp(pos.y, ty, ref velY, 0.22f * sm, Mathf.Infinity, Time.unscaledDeltaTime);
            }
            Apply(pos);
        }

        void Apply(Vector2 p)
        {
            float tr = Fx.Trauma * Fx.Trauma;
            Vector2 off = Vector2.zero;
            if (tr > 0.0001f)
            {
                float t = Time.unscaledTime * 38f;
                off.x = (Mathf.PerlinNoise(t, 0.3f) - 0.5f) * 2f * tr * 1.1f;
                off.y = (Mathf.PerlinNoise(0.7f, t) - 0.5f) * 2f * tr * 1.1f;
            }
            float x = Mathf.Round((p.x + off.x) * 16f) / 16f;     // bám lưới pixel
            float y = Mathf.Round((p.y + off.y) * 16f) / 16f;
            transform.position = new Vector3(x, y, -10f);
        }

        public void Snap() { first = true; }
    }
}
