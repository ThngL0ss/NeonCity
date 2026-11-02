using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Vật phẩm hồi máu duy nhất trong game. Nhặt được = cộng dồn 1 lượt vào kỹ năng Hồi máu (L).
    /// Nếu đã đầy lượt thì hồi trực tiếp một ít máu.
    /// </summary>
    public class Pickup : MonoBehaviour
    {
        Vector2 vel;
        float age;
        [Header("Thông số vật phẩm hồi máu")]
        [Tooltip("Thời gian tồn tại trên sàn trước khi biến mất (giây)")] public float lifetime = 18f;
        [Tooltip("Độ cao (y) của sàn nơi vật phẩm nằm")] public float floorY = 0.55f;
        [Tooltip("Hút về phía người chơi khi lại gần trong khoảng này")] public float magnetRange = 3.6f;
        [Tooltip("Tốc độ hút (xa nhất, gần nhất)")] public Vector2 magnetSpeed = new Vector2(3f, 16f);
        [Tooltip("Bán kính nhặt")] public float pickupRadius = 0.5f;
        [Tooltip("Lượng máu hồi trực tiếp khi nhặt lúc đã đầy lượt hồi")] public float directHeal = 15f;
        public float gravity = 26f;
        [Header("Hình ảnh (tự tìm object con \"Visual\" nếu để trống)")]
        public Transform icon;
        SpriteRenderer[] srs;

        /// <summary>Thả vật phẩm hồi máu từ prefab (ArtBank ▸ Pickup Prefab).</summary>
        public static void Drop(Vector2 pos)
        {
            var prefab = Art.Bank != null ? Art.Bank.pickupPrefab : null;
            if (prefab == null) { Debug.LogError("Neon Breaker: ArtBank chưa có Pickup Prefab."); return; }
            var go = Instantiate(prefab, new Vector3(pos.x, pos.y + 0.5f, 0f), Quaternion.identity, Level.Sub("Pickups"));
            go.name = "HealPickup";
            go.layer = Cfg.LayerItem;
            var p = go.GetComponent<Pickup>();
            if (p == null) return;
            p.vel = new Vector2(Random.Range(-2.2f, 2.2f), Random.Range(6f, 8.5f));
            Fx.Ring(pos, Cfg.A(Cfg.Lime, 0.9f), 0.3f, 1.8f, 0.35f);
        }

        void Awake()
        {
            if (icon == null) icon = transform.Find("Visual");
            srs = GetComponentsInChildren<SpriteRenderer>(true);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            Vector2 pos = transform.position;

            var pl = Player.Instance;
            bool grounded = pos.y <= floorY + 0.001f && vel.y <= 0f;

            if (!grounded)
            {
                vel.y -= gravity * dt;
                pos += vel * dt;
                if (pos.y <= floorY)
                {
                    pos.y = floorY;
                    if (vel.y < -4f) vel = new Vector2(vel.x * 0.5f, -vel.y * 0.35f);
                    else vel = Vector2.zero;
                }
            }
            else vel.x = Mathf.MoveTowards(vel.x, 0f, 8f * dt);

            // nam châm kéo về phía người chơi khi lại gần
            if (pl != null && !pl.Dead && age > 0.5f)
            {
                Vector2 to = pl.Center - pos;
                float d = to.magnitude;
                float mr = magnetRange * Inventory.Mul(ItemStat.MagnetRangePct);
                if (d < mr)
                {
                    pos += to.normalized * Mathf.Lerp(magnetSpeed.x, magnetSpeed.y, 1f - d / mr) * dt;
                    vel = Vector2.zero;
                }
                if (Geo.CircleRect(pos, pickupRadius, pl.HurtRect)) { Collect(pl, pos); return; }
            }

            transform.position = pos;
            if (icon != null)
                icon.localPosition = new Vector3(0f, 0.12f * Mathf.Sin(Time.time * 4f), 0f);

            float remain = lifetime - age;
            bool vis = remain > 4f || Mathf.Sin(Time.time * 18f) > 0f;
            for (int i = 0; i < srs.Length; i++) if (srs[i] != null) srs[i].enabled = vis;
            if (age >= lifetime) Destroy(gameObject);
        }

        void Collect(Player pl, Vector2 pos)
        {
            Sfx.Play("pickup", 0.9f);
            if (pl.AddHealCharge())
            {
                Fx.Text(pl.Center + new Vector2(0f, 1.5f), Cfg.L("+1 LƯỢT HỒI", "+1 HEAL"), Cfg.Lime, 1.1f);
            }
            else
            {
                float before = pl.hp;
                pl.hp = Mathf.Min(pl.maxHp, pl.hp + directHeal);
                Fx.Text(pl.Center + new Vector2(0f, 1.5f), "+" + Mathf.RoundToInt(pl.hp - before), Cfg.Lime, 1.1f);
            }
            Fx.Burst(pos, Cfg.Lime, 14, 6f, 0.45f, 0.3f);
            Fx.Ring(pos, Cfg.A(Cfg.Lime, 0.9f), 0.3f, 2.0f, 0.3f);
            Destroy(gameObject);
        }
    }
}
