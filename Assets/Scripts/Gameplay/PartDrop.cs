using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Linh kiện rơi ra khi quái bị hạ (hoặc khi minibot bị phá huỷ). Rơi xuống sàn, bị hút về khi lại gần giống đồng xu.
    /// Nhặt = cộng linh kiện CÙNG CẤP BẬC với quái đã rơi ra nó (F, D, C, B, A, S) - dùng để chế tạo minibot trong mục Skill
    /// hoặc ghép lên bậc cao hơn ở căn cứ. Màu linh kiện theo cấp bậc.
    /// Prefab: ArtBank ▸ Part Prefab (menu Tools ▸ Neon Breaker ▸ Cập nhật cấp bậc + dầu + căn cứ tự tạo).
    /// </summary>
    public class PartDrop : Coin
    {
        [Tooltip("Cấp bậc của linh kiện (đặt lúc rơi ra)")] public Rank rank = Rank.F;

        /// <summary>Thả linh kiện bậc rank tại pos. amount lớn được tách thành vài mảnh (tối đa 4). Bậc SS / SSS không rơi.</summary>
        public static void Drop(Vector2 pos, Rank rank, int amount)
        {
            if (amount <= 0 || !PowerRules.DropsParts(rank)) return;
            Color tint = PowerRules.RankColor(rank);
            var prefab = Art.Bank != null ? Art.Bank.partPrefab : null;
            if (prefab == null)
            {
                SkillSystem.AddParts(rank, amount, true);     // chưa có Prefab: cộng thẳng để game vẫn chạy
                return;
            }
            int n = Mathf.Min(amount, 4);
            int each = amount / n;
            int extra = amount - each * n;
            for (int i = 0; i < n; i++)
            {
                var go = Instantiate(prefab, new Vector3(pos.x, pos.y + 0.4f, 0f), Quaternion.identity, Level.Sub("Pickups"));
                go.name = "Part";
                go.layer = Cfg.LayerItem;
                var c = go.GetComponent<PartDrop>();
                if (c == null) { Destroy(go); continue; }
                c.value = each + (i < extra ? 1 : 0);
                c.rank = rank;
                c.Launch(new Vector2(Random.Range(-3.2f, 3.2f), Random.Range(5f, 9f)));
                var srs = go.GetComponentsInChildren<SpriteRenderer>(true);
                for (int k = 0; k < srs.Length; k++)
                {
                    if (srs[k].sharedMaterial == Art.AddMat) srs[k].color = Cfg.A(tint, srs[k].color.a);
                    else srs[k].color = tint;
                }
            }
            Fx.Burst(pos, tint, 6, 4f, 0.3f, 0.25f);
        }

        protected override void Grant()
        {
            SkillSystem.AddParts(rank, value, true);
        }
    }
}
