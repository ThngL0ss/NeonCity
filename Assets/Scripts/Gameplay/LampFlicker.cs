using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Đèn trần căn cứ chập chờn: thỉnh thoảng tối sầm rất nhanh rồi sáng lại. Gắn vào object đèn,
    /// kéo các SpriteRenderer cộng sáng (vầng sáng, chùm sáng) vào Targets; màu gốc được nhớ lúc bắt đầu.
    /// </summary>
    public class LampFlicker : MonoBehaviour
    {
        public SpriteRenderer[] targets;
        [Tooltip("Thời gian giữa hai lần chập chờn (ngắn nhất, dài nhất)")] public Vector2 gap = new Vector2(2f, 9f);
        [Tooltip("Độ sáng nhỏ nhất khi chập (0 = tắt hẳn)")] [Range(0f, 1f)] public float dip = 0.15f;
        [Tooltip("Độ lung linh nhẹ liên tục")] [Range(0f, 0.3f)] public float shimmer = 0.06f;

        Color[] baseCol;
        float timer, flickLeft, seed;

        void Awake()
        {
            seed = Random.value * 100f;
            timer = Random.Range(gap.x, gap.y);
            if (targets == null) return;
            baseCol = new Color[targets.Length];
            for (int i = 0; i < targets.Length; i++) baseCol[i] = targets[i] != null ? targets[i].color : Color.white;
        }

        void Update()
        {
            if (targets == null || baseCol == null) return;
            timer -= Time.deltaTime;
            if (timer <= 0f) { flickLeft = Random.Range(0.12f, 0.45f); timer = Random.Range(gap.x, gap.y); }

            float k = 1f - shimmer * 0.5f + shimmer * Mathf.PerlinNoise(Time.time * 3f, seed);
            if (flickLeft > 0f)
            {
                flickLeft -= Time.deltaTime;
                k *= Random.value < 0.5f ? dip : Mathf.Lerp(dip, 1f, 0.6f);
            }
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) continue;
                var c = baseCol[i];
                c.a *= k;
                targets[i].color = c;
            }
        }
    }
}
