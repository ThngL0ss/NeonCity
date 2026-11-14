using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Điều khiển một địa điểm "thành phố" (Phố Neon, Rìa Phố Neon): không có trụ triệu hồi, không có boss.
    /// Việc nó làm:
    ///  * đọc 2 mốc giới hạn trái / phải trong Scene rồi báo cho game biết (camera, quái, NPC);
    ///  * rải vật phẩm lên đường mỗi lần vào (thùng dầu + nguyên liệu theo bảng "Loot" bên dưới — bấm F để nhặt);
    ///  * hiện tên địa điểm; rắc hạt bụi / khói quanh camera cho có không khí.
    /// Bố cục (nhà, đường, NPC, đống rác...) là các object thật trong Scene — bạn kéo thả để sửa. Cửa ra / vào: object có PlaceGate (mở bản đồ); luôn đủ 2 cửa ở đầu và cuối khu vực — thiếu thì tự tạo.
    /// Bảng Loot / số lượng chỉnh ngay trong Inspector của object này (không đụng BaseConfig).
    /// </summary>
    [DefaultExecutionOrder(20)]
    public class PlaceManager : MonoBehaviour
    {
        public static PlaceManager Inst;

        [Header("Địa điểm")]
        [Tooltip("id trùng tên Scene và trùng Places.cs (PhoNeon / RiaPhoNeon)")] public string placeId = Places.Neon;

        [Header("Các mốc trong Scene (kéo object vào đây)")]
        public Transform limitLeft;
        public Transform limitRight;

        [Header("Vật phẩm rải trên đường (bấm F để nhặt)")]
        [Tooltip("Số thùng dầu")] public int oilCans = 3;
        [Tooltip("Số nguyên liệu (lấy từ bảng Loot)")] public int lootCount = 10;
        [Tooltip("Khoảng cách tối thiểu giữa 2 vật phẩm (đơn vị thế giới)")] public float minGap = 4f;
        [Tooltip("Vật phẩm có thể xuất hiện: Item + trọng số + số lượng. Để trống = chỉ có thùng dầu")] public List<ScatterEntry> loot = new List<ScatterEntry>();

        [Header("Không khí (hạt bay quanh camera)")]
        public Color moteA = new Color(0.10f, 0.95f, 1.00f, 1f);
        public Color moteB = new Color(1.00f, 0.15f, 0.80f, 1f);
        [Tooltip("Cỡ hạt")] public float moteSize = 0.22f;
        [Tooltip("Tốc độ bay lên chậm nhất / nhanh nhất")] public Vector2 moteRise = new Vector2(0.3f, 1.2f);
        [Tooltip("Số giây giữa 2 hạt (nhỏ = dày)")] public float moteEvery = 0.09f;

        float moteT;
        Camera cam;

        public string Title { get { return Places.NameOf(placeId).ToUpper(); } }
        public Color TitleColor { get { var p = Places.Get(placeId); return p != null ? p.color : Cfg.Cyan; } }

        void Awake()
        {
            Inst = this;
            Cfg.MaxEnemiesAlive = 0;
            if (limitLeft != null) Cfg.LevelMinX = limitLeft.position.x;
            if (limitRight != null) Cfg.LevelMaxX = limitRight.position.x;
            Cfg.GateX = Cfg.LevelMaxX + 50f;           // không có cổng boss: đẩy mốc ra thật xa
            Cfg.BossTriggerX = Cfg.GateX + 50f;
            Cfg.AltarX = Cfg.GateX;
            GameFlow.CurrentPlaceId = placeId;
            cam = Camera.main;
        }

        void Start()
        {
            GameFlow.UnlockPlace(placeId);
            // đủ 2 cửa ở đầu và cuối khu vực (thiếu thì tự tạo); người chơi luôn xuất hiện cạnh cửa đầu
            var startGate = PlaceGate.EnsureTwoGates(Cfg.LevelMinX + 3f, Cfg.LevelMaxX - 3f, false);
            var pl = Player.Instance;
            if (startGate != null && pl != null)
            {
                pl.transform.position = new Vector3(startGate.transform.position.x + 2.6f, pl.transform.position.y, 0f);
                if (CameraRig.Inst != null) CameraRig.Inst.Snap();
            }
            StreetBuildings.Setup(placeId);            // lớp nhà 2 (làm tối) + lớp 3 (toà nhà vào được)
            Scatter();
            if (GameManager.State == GameState.Playing) GameManager.Banner(Title, TitleColor, 3.5f);
        }

        void OnDestroy() { if (Inst == this) Inst = null; }

        // ------------------------------------------------------------ rải vật phẩm
        void Scatter()
        {
            var prefab = Art.Bank != null ? Art.Bank.worldItemPrefab : null;
            if (prefab == null) return;
            float minX = Cfg.LevelMinX + 6f, maxX = Cfg.LevelMaxX - 6f;
            if (maxX - minX < 10f) return;
            var cfg = BaseConfig.Cur;
            int cans = Mathf.Max(0, oilCans);
            int mats = HasLoot() ? Mathf.Max(0, lootCount) : 0;
            var xs = new List<float>();
            int guard = 0;
            while (xs.Count < cans + mats && guard++ < 600)
            {
                float x = Random.Range(minX, maxX);
                bool ok = true;
                for (int i = 0; i < xs.Count && ok; i++) if (Mathf.Abs(xs[i] - x) < minGap) ok = false;
                if (ok) xs.Add(x);
            }
            for (int i = 0; i < xs.Count; i++)
            {
                bool can = i < cans;
                var go = Instantiate(prefab, new Vector3(xs[i], 0f, 0f), Quaternion.identity, Level.Sub("Pickups"));
                go.name = can ? "OilCan" : "Material";
                var w = go.GetComponent<WorldItem>();
                if (w == null) { Destroy(go); continue; }
                if (can) { w.Setup(null, 1, Random.Range(cfg.oilCanAmount.x, cfg.oilCanAmount.y + 1)); continue; }
                var e = PickLoot();
                if (e == null || e.item == null) { w.Setup(null, 1, Random.Range(cfg.oilCanAmount.x, cfg.oilCanAmount.y + 1)); continue; }
                w.Setup(e.item, Random.Range(Mathf.Max(1, e.minCount), Mathf.Max(e.minCount, e.maxCount) + 1), 0);
            }
        }

        bool HasLoot()
        {
            if (loot == null) return false;
            for (int i = 0; i < loot.Count; i++) if (loot[i] != null && loot[i].item != null && loot[i].weight > 0f) return true;
            return false;
        }

        ScatterEntry PickLoot()
        {
            float sum = 0f;
            for (int i = 0; i < loot.Count; i++) if (loot[i] != null && loot[i].item != null && loot[i].weight > 0f) sum += loot[i].weight;
            if (sum <= 0f) return null;
            float r = Random.value * sum;
            for (int i = 0; i < loot.Count; i++)
            {
                var e = loot[i];
                if (e == null || e.item == null || e.weight <= 0f) continue;
                r -= e.weight;
                if (r <= 0f) return e;
            }
            return null;
        }

        // ------------------------------------------------------------ không khí
        void Update()
        {
            if (cam == null) cam = Camera.main;
            moteT -= Time.deltaTime;
            if (moteT > 0f || cam == null) return;
            moteT = Mathf.Max(0.02f, moteEvery);
            Vector3 cp = cam.transform.position;
            float hw = cam.orthographicSize * cam.aspect;
            Color c = Random.value < 0.5f ? moteA : moteB;
            Fx.Emit(Fx.S_Glow, new Vector2(cp.x + Random.Range(-hw, hw), cp.y + Random.Range(-cam.orthographicSize, cam.orthographicSize)),
                    new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(moteRise.x, moteRise.y)), new Vector2(moteSize, moteSize), Vector2.zero,
                    Cfg.A(c, 0.6f), Cfg.A(c, 0f), Random.Range(1.5f, 3f));
        }

        void OnDrawGizmos()
        {
            if (limitLeft != null) { Gizmos.color = Color.cyan; Vector3 a = limitLeft.position; Gizmos.DrawLine(a + Vector3.down * 6f, a + Vector3.up * 20f); }
            if (limitRight != null) { Gizmos.color = Color.magenta; Vector3 a = limitRight.position; Gizmos.DrawLine(a + Vector3.down * 6f, a + Vector3.up * 20f); }
        }
    }
}
