using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Giọt dầu rơi ra khi hạ quái. Rơi xuống sàn, bị hút về khi lại gần giống đồng xu; nhặt = cộng dầu.
    /// Dầu là nhiên liệu để chế tác vật phẩm và ghép linh kiện ở căn cứ.
    /// Prefab: ArtBank ▸ Oil Prefab (menu Tools ▸ Neon Breaker ▸ Cập nhật cấp bậc + dầu + căn cứ tự tạo).
    /// </summary>
    public class OilDrop : Coin
    {
        public static void Drop(Vector2 pos, int amount)
        {
            if (amount <= 0) return;
            var prefab = Art.Bank != null ? Art.Bank.oilPrefab : null;
            if (prefab == null) { Inventory.AddOil(amount, true); return; }
            int n = Mathf.Min(amount, 3);
            int each = amount / n;
            int extra = amount - each * n;
            for (int i = 0; i < n; i++)
            {
                var go = Instantiate(prefab, new Vector3(pos.x, pos.y + 0.4f, 0f), Quaternion.identity, Level.Sub("Pickups"));
                go.name = "Oil";
                go.layer = Cfg.LayerItem;
                var c = go.GetComponent<OilDrop>();
                if (c == null) { Destroy(go); continue; }
                c.value = each + (i < extra ? 1 : 0);
                c.Launch(new Vector2(Random.Range(-3f, 3f), Random.Range(4.5f, 8f)));
            }
        }

        protected override void Grant()
        {
            Inventory.AddOil(value, true);
        }
    }
}
