using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Máy tính khổng lồ đặt cạnh lồng hồi sinh, nối với lồng bằng hai bó dây cáp (Scene Base).
    /// Màn hình / đèn của máy là hoạt ảnh sprite (SpriteAnimator trên object con "Screen"). Script này làm máy "sống" theo lồng:
    /// khi lồng đang nạp điện (hồi sinh) thì vầng sáng màn hình mạnh lên, dây cáp sáng nhấp nháy và phóng tia lửa điện dọc dây.
    /// Chỉ là hiệu ứng thị giác — không ảnh hưởng gameplay. Muốn thay đổi: chỉnh các số bên dưới trong Inspector.
    /// </summary>
    public class ResurrectionComputer : MonoBehaviour
    {
        [Tooltip("Lồng hồi sinh đi kèm (để trống = tự tìm trong Scene)")] public ResurrectionCapsule capsule;
        [Tooltip("Vầng sáng cộng sáng trước màn hình")] public SpriteRenderer screenGlow;
        [Tooltip("Các bó dây cáp (nhấp sáng khi lồng nạp điện)")] public SpriteRenderer[] cables;
        [Tooltip("Vị trí phóng tia lửa điện dọc dây khi nạp điện")] public Transform[] sparkPoints;
        [Tooltip("Độ sáng vầng sáng khi nghỉ (0..1)")] [Range(0f, 1f)] public float idleGlow = 0.16f;
        [Tooltip("Độ sáng thêm khi lồng nạp đầy (0..1)")] [Range(0f, 1f)] public float chargedGlow = 0.55f;

        Color glowBase = Cfg.Cyan;
        Color[] cableBase;

        void Awake()
        {
            if (capsule == null) capsule = FindObjectOfType<ResurrectionCapsule>();
            if (screenGlow != null) glowBase = Cfg.A(screenGlow.color, 1f);
            if (cables != null)
            {
                cableBase = new Color[cables.Length];
                for (int i = 0; i < cables.Length; i++) cableBase[i] = cables[i] != null ? cables[i].color : Color.white;
            }
        }

        void Update()
        {
            float c = capsule != null ? capsule.Charge : 0f;
            float t = Time.unscaledTime;
            float flick = 0.88f + 0.12f * Mathf.Sin(t * 27f) * Mathf.Sin(t * 8f);

            if (screenGlow != null)
            {
                float a = (idleGlow + chargedGlow * c) * flick;
                screenGlow.color = Cfg.A(Color.Lerp(glowBase, Color.white, c * 0.65f), a);
            }

            if (cables != null && cableBase != null)
            {
                for (int i = 0; i < cables.Length; i++)
                {
                    if (cables[i] == null) continue;
                    float pulse = 0.5f + 0.5f * Mathf.Sin(t * 14f + i * 1.7f);
                    cables[i].color = Color.Lerp(cableBase[i], Color.Lerp(cableBase[i], Cfg.Cyan, 0.7f), c * pulse);
                }
            }

            if (c > 0.05f && sparkPoints != null && sparkPoints.Length > 0 && Random.value < c * 0.45f)
            {
                var pt = sparkPoints[Random.Range(0, sparkPoints.Length)];
                if (pt != null)
                {
                    Vector2 p = (Vector2)pt.position + new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.25f, 0.25f));
                    Fx.Emit(Fx.S_Star, p, new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(0.4f, 2.2f)),
                            new Vector2(0.2f, 0.2f), Vector2.zero, Cfg.A(Cfg.Cyan, 0.95f), Cfg.A(Cfg.Cyan, 0f), 0.35f);
                }
            }
        }
    }
}
