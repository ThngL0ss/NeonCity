using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Vật phẩm nằm trên đường (thùng dầu, nguyên liệu chế tác): đứng gần rồi bấm F để nhặt.
    /// Level tự rải ngẫu nhiên khi vào màn (Level ▸ Scatter Oil Cans / Scatter Materials); bảng vật phẩm ở BaseConfig ▸ Scatter.
    /// Prefab: ArtBank ▸ World Item Prefab (menu Tools ▸ Neon Breaker ▸ Cập nhật cấp bậc + dầu + căn cứ tự tạo).
    /// </summary>
    public class WorldItem : MonoBehaviour, IInteractable
    {
        public static readonly List<WorldItem> All = new List<WorldItem>();

        [Tooltip("Vật phẩm nhận được (để trống nếu là thùng dầu)")] public ItemData item;
        public int count = 1;
        [Tooltip("Số dầu nhận được (> 0 = thùng dầu)")] public int oil = 0;
        [Tooltip("Khoảng cách ngang tối đa để bấm F nhặt")] public float interactRange = 1.8f;
        public float interactHeight = 2.2f;

        SpriteRenderer sr;
        SpriteRenderer glow;
        float t;
        bool taken;

        public string DisplayName
        {
            get
            {
                if (oil > 0) return Cfg.L("THÙNG DẦU", "OIL CAN") + " +" + oil;
                return item != null ? item.DisplayName + (count > 1 ? "  x" + count : "") : "?";
            }
        }

        // ---- IInteractable
        public bool CanInteract { get { return !taken; } }
        public Vector2 PromptPos { get { return (Vector2)transform.position + new Vector2(0f, 1.3f); } }
        public string PromptText { get { return "[F] " + Cfg.L("NHẶT ", "TAKE ") + DisplayName; } }
        public bool InRange(Player p) { return Interactor.Near(p, transform.position, interactRange, interactHeight); }

        void OnEnable() { if (!All.Contains(this)) All.Add(this); Interactor.Register(this); }
        void OnDisable() { All.Remove(this); Interactor.Unregister(this); }

        void Awake()
        {
            var v = transform.Find("Visual");
            if (v != null) sr = v.GetComponent<SpriteRenderer>();
            var g = transform.Find("Glow");
            if (g != null) glow = g.GetComponent<SpriteRenderer>();
            t = Random.value * 6f;
        }

        /// <summary>Đặt nội dung và hình hiển thị (từ icon vật phẩm hoặc icon dầu).</summary>
        public void Setup(ItemData it, int n, int oilAmount)
        {
            item = it;
            count = Mathf.Max(1, n);
            oil = oilAmount;
            if (sr == null) Awake();
            Sprite s = null;
            Color c = Color.white;
            if (oil > 0) { s = Art.Bank != null ? Art.Bank.oilIcon : null; c = new Color(0.95f, 0.75f, 0.25f, 1f); }
            else if (item != null) { s = item.icon; c = item.color; }
            if (sr != null && s != null) sr.sprite = s;
            if (glow != null) glow.color = Cfg.A(c, 0.35f);
        }

        void Update()
        {
            t += Time.deltaTime;
            if (sr != null) sr.transform.localPosition = new Vector3(0f, 0.55f + 0.08f * Mathf.Sin(t * 3f), 0f);
            if (glow != null)
            {
                var c = glow.color;
                c.a = 0.25f + 0.12f * Mathf.Sin(t * 4f);
                glow.color = c;
            }
        }

        public void Interact()
        {
            if (taken || !GameManager.Active) return;
            Vector2 c = (Vector2)transform.position + new Vector2(0f, 0.7f);
            if (oil > 0)
            {
                Inventory.AddOil(oil, true);
            }
            else if (item != null)
            {
                if (!Inventory.Add(item, count))
                {
                    Fx.Text(PromptPos, Cfg.L("KHO ĐẦY — hãy dọn kho (B)", "BAG FULL — free some space (B)"), Cfg.Red, 1.1f);
                    Sfx.Play("click", 0.7f, 0.7f);
                    return;
                }
                Fx.Text(c + new Vector2(0f, 0.9f), item.DisplayName + (count > 1 ? "  x" + count : ""), item.color, 1.05f);
            }
            taken = true;
            Sfx.Play("pickup", 0.8f, 1.1f);
            Fx.Burst(c, new Color(1f, 0.9f, 0.4f, 1f), 10, 5f, 0.4f, 0.28f);
            Destroy(gameObject);
        }

        // ------------------------------------------------------------ rải ngẫu nhiên khi vào màn
        /// <summary>Rải thùng dầu + nguyên liệu dọc đường, tránh trụ và tránh nằm sát nhau. Bỏ qua nếu ArtBank chưa có World Item Prefab.</summary>
        public static void Scatter(int cans, int materials, float minX, float maxX)
        {
            var prefab = Art.Bank != null ? Art.Bank.worldItemPrefab : null;
            if (prefab == null || maxX - minX < 10f) return;
            var cfg = BaseConfig.Cur;
            var xs = new List<float>();
            int total = Mathf.Max(0, cans) + Mathf.Max(0, materials);
            int guard = 0;
            while (xs.Count < total && guard++ < 400)
            {
                float x = Random.Range(minX, maxX);
                bool ok = true;
                for (int i = 0; i < xs.Count && ok; i++) if (Mathf.Abs(xs[i] - x) < 5f) ok = false;
                for (int i = 0; i < Tower.Towers.Count && ok; i++)
                    if (Tower.Towers[i] != null && Mathf.Abs(Tower.Towers[i].transform.position.x - x) < 4f) ok = false;
                if (ok) xs.Add(x);
            }
            for (int i = 0; i < xs.Count; i++)
            {
                bool can = i < cans || cfg.scatter == null || cfg.scatter.Count == 0;
                var go = Instantiate(prefab, new Vector3(xs[i], 0f, 0f), Quaternion.identity, Level.Sub("Pickups"));
                go.name = can ? "OilCan" : "Material";
                var w = go.GetComponent<WorldItem>();
                if (w == null) { Destroy(go); continue; }
                if (can) w.Setup(null, 1, Random.Range(cfg.oilCanAmount.x, cfg.oilCanAmount.y + 1));
                else
                {
                    var e = PickEntry(cfg);
                    if (e == null || e.item == null) { w.Setup(null, 1, Random.Range(cfg.oilCanAmount.x, cfg.oilCanAmount.y + 1)); continue; }
                    w.Setup(e.item, Random.Range(Mathf.Max(1, e.minCount), Mathf.Max(e.minCount, e.maxCount) + 1), 0);
                }
            }
        }

        static ScatterEntry PickEntry(BaseConfig cfg)
        {
            float sum = 0f;
            for (int i = 0; i < cfg.scatter.Count; i++) if (cfg.scatter[i] != null && cfg.scatter[i].item != null && cfg.scatter[i].weight > 0f) sum += cfg.scatter[i].weight;
            if (sum <= 0f) return null;
            float r = Random.value * sum;
            for (int i = 0; i < cfg.scatter.Count; i++)
            {
                var e = cfg.scatter[i];
                if (e == null || e.item == null || e.weight <= 0f) continue;
                r -= e.weight;
                if (r <= 0f) return e;
            }
            return null;
        }
    }
}
