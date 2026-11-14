using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Danh sách mọi vật phẩm (ItemData) trong game, để file lưu game tra lại vật phẩm theo TÊN asset.
    /// File: Assets/Resources/NeonBreaker/ItemDatabase.asset — menu Tools ▸ Neon Breaker ▸ "Cập nhật Nhiệm vụ + Phố Neon + Lưu game" tạo / làm mới nó
    /// (quét mọi ItemData trong project). Thêm vật phẩm mới thì chạy lại menu đó (hoặc kéo thẳng vào danh sách này).
    /// Chưa có file thì vẫn tra được vật phẩm đã nạp trong bộ nhớ (Resources.FindObjectsOfTypeAll) — nhưng nên có file cho chắc.
    /// </summary>
    [CreateAssetMenu(menuName = "Neon Breaker/Item Database", fileName = "ItemDatabase")]
    public class ItemDatabase : ScriptableObject
    {
        public List<ItemData> items = new List<ItemData>();

        static ItemDatabase cached;
        static Dictionary<string, ItemData> map;

        public static void ResetStatics() { cached = null; map = null; QuestItems.Reset(); }

        /// <summary>Mọi vật phẩm trong file ItemDatabase.asset (rỗng nếu chưa có file).</summary>
        public static List<ItemData> All()
        {
            if (cached == null) cached = Resources.Load<ItemDatabase>("NeonBreaker/ItemDatabase");
            var l = new List<ItemData>();
            if (cached != null) for (int i = 0; i < cached.items.Count; i++) if (cached.items[i] != null) l.Add(cached.items[i]);
            return l;
        }

        static void Build()
        {
            map = new Dictionary<string, ItemData>();
            if (cached == null) cached = Resources.Load<ItemDatabase>("NeonBreaker/ItemDatabase");
            if (cached != null)
                for (int i = 0; i < cached.items.Count; i++)
                    if (cached.items[i] != null) map[cached.items[i].name] = cached.items[i];
        }

        /// <summary>Id lưu trong file = tên asset của vật phẩm.</summary>
        public static string IdOf(ItemData it) { return it != null ? it.name : ""; }

        /// <summary>Tìm vật phẩm theo id đã lưu. null nếu không còn tồn tại.</summary>
        public static ItemData Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var q = QuestItems.Find(id);                         // vật phẩm nhiệm vụ do code tạo (không có file asset)
            if (q != null) return q;
            if (map == null) Build();
            ItemData it;
            if (map.TryGetValue(id, out it) && it != null) return it;

            // dự phòng: mọi ItemData đang được nạp trong bộ nhớ (do Scene / Prefab / config tham chiếu)
            var all = Resources.FindObjectsOfTypeAll<ItemData>();
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i].name == id) { map[id] = all[i]; return all[i]; }
            return null;
        }
    }
}
