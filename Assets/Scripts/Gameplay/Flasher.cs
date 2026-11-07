using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Làm đối tượng chớp trắng khi trúng đòn (đổi sang material "flash" - tô trắng toàn bộ sprite)
    /// và làm mờ / nhấp nháy (đổi alpha). Chỉ ảnh hưởng sprite dùng material thường;
    /// các vầng sáng (additive) và sprite bị tắt được bỏ qua.
    ///
    /// Capture() có thể gọi lại nhiều lần (ví dụ khi boss đổi phase): màu + material GỐC của từng sprite chỉ được nhớ
    /// ở lần đầu thấy sprite đó. Nếu nhớ lại ở thời điểm đang nhấp nháy / mờ thì sprite sẽ mất độ đậm vĩnh viễn
    /// (lỗi "boss phase 2 / 3 bị trong suốt" trước đây).
    /// </summary>
    public class Flasher
    {
        SpriteRenderer[] srs;
        Material[] baseMat;
        Color[] baseCol;
        readonly Dictionary<SpriteRenderer, Material> origMat = new Dictionary<SpriteRenderer, Material>();
        readonly Dictionary<SpriteRenderer, Color> origCol = new Dictionary<SpriteRenderer, Color>();
        bool lastFlash;
        float lastAlpha = 1f;
        bool tinted;
        Material tintMat;

        public Flasher(GameObject root) { Capture(root); }

        public void Capture(GameObject root)
        {
            Art.Init();
            srs = root.GetComponentsInChildren<SpriteRenderer>(true);
            baseMat = new Material[srs.Length];
            baseCol = new Color[srs.Length];
            for (int i = 0; i < srs.Length; i++)
            {
                var sr = srs[i];
                if (sr == null) continue;
                if (!origMat.ContainsKey(sr))
                {
                    Material m = sr.sharedMaterial;
                    if (m != null && m == Art.FlashMat) m = Art.DefMat;     // phòng khi sprite mới đang ở trạng thái nháy trắng
                    origMat[sr] = m;
                    origCol[sr] = sr.color;
                }
                baseMat[i] = origMat[sr];
                baseCol[i] = origCol[sr];
            }
            lastAlpha = -1f;        // ép Apply() chạy lại cho cả những sprite mới được thêm vào danh sách
            lastFlash = false;
            tinted = false;
        }

        public void Apply(bool flash, float alphaMul)
        {
            if (!tinted && flash == lastFlash && Mathf.Abs(alphaMul - lastAlpha) < 0.001f) return;
            tinted = false;
            lastFlash = flash;
            lastAlpha = alphaMul;
            Material fm = Art.FlashMat;
            for (int i = 0; i < srs.Length; i++)
            {
                var sr = srs[i];
                if (sr == null) continue;
                bool additive = baseMat[i] != null && baseMat[i] == Art.AddMat;
                if (!additive)
                {
                    if (fm != null) sr.sharedMaterial = flash ? fm : baseMat[i];
                    else if (flash) { /* chưa có flash material: bỏ qua hiệu ứng nháy trắng */ }
                }
                Color c = baseCol[i];
                c.a = baseCol[i].a * alphaMul;
                sr.color = c;
            }
        }

        /// <summary>Tô cả hình bằng một màu đặc (dùng cho hiệu ứng nhấp nháy 7 màu). Gọi Apply() sau đó để trở lại bình thường.</summary>
        public void Tint(Color c)
        {
            if (Art.FlashMat == null) { Apply(true, 1f); return; }
            if (tintMat == null)
            {
                tintMat = new Material(Art.FlashMat);
                tintMat.name = "FlashTint (runtime)";
                if (tintMat.HasProperty("_Tint")) tintMat.SetFloat("_Tint", 1f);
            }
            tinted = true;
            for (int i = 0; i < srs.Length; i++)
            {
                var sr = srs[i];
                if (sr == null) continue;
                bool additive = baseMat[i] != null && baseMat[i] == Art.AddMat;
                if (additive) continue;
                sr.sharedMaterial = tintMat;
                sr.color = new Color(c.r, c.g, c.b, baseCol[i].a);
            }
        }
    }
}
