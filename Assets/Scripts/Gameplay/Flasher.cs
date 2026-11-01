using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Làm đối tượng chớp trắng khi trúng đòn (đổi sang material "flash" - tô trắng toàn bộ sprite)
    /// và làm mờ / nhấp nháy (đổi alpha). Chỉ ảnh hưởng sprite dùng material thường;
    /// các vầng sáng (additive) và sprite bị tắt được bỏ qua.
    /// </summary>
    public class Flasher
    {
        SpriteRenderer[] srs;
        Material[] baseMat;
        Color[] baseCol;
        bool lastFlash;
        float lastAlpha = 1f;

        public Flasher(GameObject root) { Capture(root); }

        public void Capture(GameObject root)
        {
            Art.Init();
            srs = root.GetComponentsInChildren<SpriteRenderer>(true);
            baseMat = new Material[srs.Length];
            baseCol = new Color[srs.Length];
            for (int i = 0; i < srs.Length; i++)
            {
                baseMat[i] = srs[i].sharedMaterial;
                baseCol[i] = srs[i].color;
            }
            lastFlash = false;
            lastAlpha = 1f;
        }

        public void Apply(bool flash, float alphaMul)
        {
            if (flash == lastFlash && Mathf.Abs(alphaMul - lastAlpha) < 0.001f) return;
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
    }
}
