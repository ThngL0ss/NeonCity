using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Đồng xu rơi ra khi quái bị hạ (hoặc khi mở rương). Rơi xuống mặt đất / bục rồi bị hút về phía người chơi khi lại gần.
    /// Giá trị mỗi xu = Value. Prefab: ArtBank ▸ Coin Prefab (menu Tools ▸ Neon Breaker ▸ Thêm bẫy + kho đồ + rương sẽ tạo).
    /// </summary>
    public class Coin : MonoBehaviour
    {
        [Header("Thông số xu")]
        [Tooltip("Số xu nhận được khi nhặt (đặt lúc rơi ra, không cần sửa trong Prefab)")] public int value = 1;
        [Tooltip("Thời gian tồn tại trên sàn (giây)")] public float lifetime = 30f;
        [Tooltip("Khoảng cách từ mặt sàn lên tâm xu")] public float restHeight = 0.5f;
        [Tooltip("Hút về phía người chơi khi lại gần trong khoảng này")] public float magnetRange = 4.5f;
        [Tooltip("Tốc độ hút (xa nhất, gần nhất)")] public Vector2 magnetSpeed = new Vector2(4f, 20f);
        [Tooltip("Bán kính nhặt")] public float pickupRadius = 0.55f;
        public float gravity = 28f;
        [Header("Hình ảnh (tự tìm object con \"Visual\" nếu để trống)")]
        public Transform icon;

        Vector2 vel;
        float age;
        SpriteRenderer[] srs;

        /// <summary>Thả xu tại pos. amount lớn sẽ được tách thành vài đồng (tối đa 5) để nhìn đẹp hơn.</summary>
        public static void Drop(Vector2 pos, int amount)
        {
            if (amount <= 0) return;
            var prefab = Art.Bank != null ? Art.Bank.coinPrefab : null;
            if (prefab == null)
            {
                // chưa có prefab xu: cộng thẳng vào ví để game vẫn chạy được
                Inventory.AddCoins(amount);
                return;
            }
            int n = Mathf.Min(amount, 5);
            int each = amount / n;
            int extra = amount - each * n;
            for (int i = 0; i < n; i++)
            {
                var go = Instantiate(prefab, new Vector3(pos.x, pos.y + 0.4f, 0f), Quaternion.identity, Level.Sub("Pickups"));
                go.name = "Coin";
                go.layer = Cfg.LayerItem;
                var c = go.GetComponent<Coin>();
                if (c == null) { Destroy(go); continue; }
                c.value = each + (i < extra ? 1 : 0);
                c.vel = new Vector2(Random.Range(-3.2f, 3.2f), Random.Range(5f, 9f));
            }
            Fx.Burst(pos, Cfg.Yellow, 6, 4f, 0.3f, 0.25f);
        }

        void Awake()
        {
            if (icon == null) icon = transform.Find("Visual");
            srs = GetComponentsInChildren<SpriteRenderer>(true);
        }

        void Update()
        {
            if (!GameManager.Active) return;
            float dt = Time.deltaTime;
            age += dt;
            Vector2 pos = transform.position;
            var pl = Player.Instance;

            bool settled = false;
            if (vel.y <= 0f)
            {
                // tìm sàn ngay bên dưới (mặt đất hoặc bục)
                float fall = -vel.y * dt + 0.1f;
                RaycastHit2D hit = Physics2D.Raycast(pos + new Vector2(0f, restHeight), Vector2.down, restHeight + fall, Cfg.GroundMask);
                if (hit.collider != null)
                {
                    pos.y = hit.point.y + restHeight;
                    if (vel.y < -5f) vel = new Vector2(vel.x * 0.5f, -vel.y * 0.3f);
                    else { vel = new Vector2(vel.x, 0f); settled = true; }
                }
            }
            if (!settled)
            {
                vel.y -= gravity * dt;
                pos += vel * dt;
                if (pos.y < -0.5f) { pos.y = restHeight; vel = Vector2.zero; }
            }
            else vel.x = Mathf.MoveTowards(vel.x, 0f, 10f * dt);
            if (settled) pos.x += vel.x * dt;

            if (pl != null && !pl.Dead && age > 0.45f)
            {
                float mr = magnetRange * Inventory.Mul(ItemStat.MagnetRangePct);
                Vector2 to = pl.Center - pos;
                float d = to.magnitude;
                if (d < mr)
                {
                    pos += to.normalized * Mathf.Lerp(magnetSpeed.x, magnetSpeed.y, 1f - d / mr) * dt;
                    vel = Vector2.zero;
                }
                if (Geo.CircleRect(pos, pickupRadius, pl.HurtRect)) { Collect(pos); return; }
            }

            transform.position = pos;
            if (icon != null) icon.localPosition = new Vector3(0f, 0.08f * Mathf.Sin(Time.time * 5f + pos.x), 0f);

            float remain = lifetime - age;
            bool vis = remain > 5f || Mathf.Sin(Time.time * 18f) > 0f;
            for (int i = 0; i < srs.Length; i++) if (srs[i] != null) srs[i].enabled = vis;
            if (age >= lifetime) Destroy(gameObject);
        }

        void Collect(Vector2 pos)
        {
            Sfx.Play("pickup", 0.55f, 1.6f + Random.Range(-0.1f, 0.15f));
            Inventory.AddCoins(value);
            Fx.Burst(pos, Cfg.Yellow, 6, 4f, 0.3f, 0.22f);
            Destroy(gameObject);
        }
    }
}
