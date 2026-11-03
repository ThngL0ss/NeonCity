using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>Một dòng trong bảng vật phẩm của rương.</summary>
    [System.Serializable]
    public class LootEntry
    {
        [Tooltip("Kéo file vật phẩm (ItemData) vào đây")] public ItemData item;
        [Tooltip("Trọng số: càng lớn càng dễ rơi. 0 = không bao giờ rơi")] public float weight = 1f;
        [Tooltip("Số lượng ít nhất / nhiều nhất của mỗi lần rơi")] public int minCount = 1;
        public int maxCount = 1;
    }

    /// <summary>
    /// Rương đồ. Đứng gần rồi bấm T để mở: nhận vật phẩm vào kho + xu.
    /// Tự chỉnh vật phẩm: chọn rương trong Scene → Chest ▸ Loot Table (kéo ItemData vào ô Item, chỉnh Weight).
    ///  • Guaranteed: luôn có.   • Loot Table: bốc ngẫu nhiên theo trọng số, số lần bốc = Rolls Min..Max.
    /// Kho đầy thì rương không mở (để bạn dọn kho rồi quay lại).
    /// </summary>
    public class Chest : MonoBehaviour
    {
        public static readonly List<Chest> All = new List<Chest>();

        [Header("Bảng vật phẩm")]
        [Tooltip("Vật phẩm LUÔN có trong rương này")] public List<LootEntry> guaranteed = new List<LootEntry>();
        [Tooltip("Bảng bốc ngẫu nhiên theo trọng số")] public List<LootEntry> lootTable = new List<LootEntry>();
        [Tooltip("Số lần bốc từ Loot Table (ít nhất / nhiều nhất)")] public int rollsMin = 1;
        public int rollsMax = 2;
        [Tooltip("true: mỗi vật phẩm trong Loot Table chỉ ra tối đa 1 lần trong cùng một rương")] public bool noDuplicates = true;

        [Header("Xu")]
        public int coinsMin = 8;
        public int coinsMax = 20;

        [Header("Tương tác")]
        [Tooltip("Khoảng cách ngang tối đa để bấm T mở rương")] public float interactRange = 2.2f;
        [Tooltip("Chênh lệch độ cao tối đa")] public float interactHeight = 2.5f;
        public string nameVi = "RƯƠNG";
        public string nameEn = "CHEST";

        [Header("Hình ảnh (tự tìm nếu để trống)")]
        public SpriteAnimator anim;
        public SpriteRenderer glow;

        public bool Opened { get; private set; }
        public string DisplayName { get { return Cfg.Vietnamese ? nameVi : nameEn; } }
        public Vector2 PromptPos { get { return (Vector2)transform.position + new Vector2(0f, 1.9f); } }

        float t;

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Awake()
        {
            if (anim == null)
            {
                var v = transform.Find("Visual");
                if (v != null) anim = v.GetComponent<SpriteAnimator>();
            }
            if (glow == null)
            {
                var g = transform.Find("Glow");
                if (g != null) glow = g.GetComponent<SpriteRenderer>();
            }
            if (anim != null) anim.Play("closed");
        }

        void Update()
        {
            t += Time.deltaTime;
            if (glow != null)
            {
                float a = Opened ? 0.08f : 0.28f + 0.12f * Mathf.Sin(t * 3f);
                var c = glow.color;
                c.a = a;
                glow.color = c;
            }
        }

        // ------------------------------------------------------------ tìm rương gần nhất
        public static bool InRange(Chest c)
        {
            var pl = Player.Instance;
            if (c == null || pl == null || pl.Dead) return false;
            Vector2 d = pl.Position - (Vector2)c.transform.position;
            return Mathf.Abs(d.x) <= c.interactRange && Mathf.Abs(d.y) <= c.interactHeight;
        }

        /// <summary>Rương chưa mở gần người chơi nhất trong tầm tương tác (null nếu không có).</summary>
        public static Chest Nearest()
        {
            var pl = Player.Instance;
            if (pl == null || pl.Dead) return null;
            Chest best = null;
            float bd = 1e9f;
            for (int i = 0; i < All.Count; i++)
            {
                var c = All[i];
                if (c == null || c.Opened || !InRange(c)) continue;
                float d = Mathf.Abs(pl.Position.x - c.transform.position.x);
                if (d < bd) { bd = d; best = c; }
            }
            return best;
        }

        /// <summary>Gọi khi người chơi bấm T.</summary>
        public static void TryOpenNearest()
        {
            var c = Nearest();
            if (c != null) c.Open();
        }

        // ------------------------------------------------------------ mở rương
        List<KeyValuePair<ItemData, int>> Roll()
        {
            var result = new List<KeyValuePair<ItemData, int>>();
            for (int i = 0; i < guaranteed.Count; i++)
            {
                var e = guaranteed[i];
                if (e == null || e.item == null) continue;
                result.Add(new KeyValuePair<ItemData, int>(e.item, Count(e)));
            }

            var pool = new List<LootEntry>();
            for (int i = 0; i < lootTable.Count; i++)
                if (lootTable[i] != null && lootTable[i].item != null && lootTable[i].weight > 0f) pool.Add(lootTable[i]);

            int rolls = Random.Range(Mathf.Min(rollsMin, rollsMax), Mathf.Max(rollsMin, rollsMax) + 1);
            for (int r = 0; r < rolls && pool.Count > 0; r++)
            {
                float sum = 0f;
                for (int i = 0; i < pool.Count; i++) sum += pool[i].weight;
                float pick = Random.value * sum;
                int idx = pool.Count - 1;
                for (int i = 0; i < pool.Count; i++)
                {
                    pick -= pool[i].weight;
                    if (pick <= 0f) { idx = i; break; }
                }
                var e = pool[idx];
                result.Add(new KeyValuePair<ItemData, int>(e.item, Count(e)));
                if (noDuplicates) pool.RemoveAt(idx);
            }
            return result;
        }

        static int Count(LootEntry e)
        {
            int lo = Mathf.Max(1, Mathf.Min(e.minCount, e.maxCount));
            int hi = Mathf.Max(lo, Mathf.Max(e.minCount, e.maxCount));
            return Random.Range(lo, hi + 1);
        }

        public void Open()
        {
            if (Opened || !GameManager.Active) return;
            var loot = Roll();
            if (!Inventory.CanAddAll(loot))
            {
                Fx.Text(PromptPos, Cfg.L("KHO ĐẦY — hãy dọn kho (B)", "BAG FULL — free some space (B)"), Cfg.Red, 1.1f);
                Sfx.Play("click", 0.7f, 0.7f);
                return;
            }

            Opened = true;
            if (anim != null) anim.Play("open");
            Sfx.Play("pickup", 0.9f, 0.8f);
            Sfx.Play("charge", 0.5f, 1.6f);
            Vector2 c = (Vector2)transform.position + new Vector2(0f, 0.7f);
            Fx.Burst(c, Cfg.Yellow, 22, 7f, 0.6f, 0.35f);
            Fx.Ring(c, Cfg.A(Cfg.Yellow, 0.9f), 0.4f, 3f, 0.4f);

            for (int i = 0; i < loot.Count; i++)
            {
                if (loot[i].Key == null) continue;
                Inventory.Add(loot[i].Key, loot[i].Value);
                string n = loot[i].Key.DisplayName + (loot[i].Value > 1 ? "  x" + loot[i].Value : "");
                Fx.Text(c + new Vector2(0f, 0.9f + i * 0.75f), n, loot[i].Key.color, 1.05f);
            }

            int coins = Random.Range(Mathf.Min(coinsMin, coinsMax), Mathf.Max(coinsMin, coinsMax) + 1);
            if (coins > 0) Coin.Drop(c, coins);
        }
    }
}
