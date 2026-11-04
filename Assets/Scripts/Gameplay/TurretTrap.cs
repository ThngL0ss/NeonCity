using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Súng tự động: đứng yên, khi người chơi vào tầm thì nòng quay ngắm (xoay được cả 180° - mặc định nửa vòng tròn phía trên chân súng),
    /// tụ năng lượng (đầu nòng sáng lên báo hiệu) rồi bắn đạn. Né bằng cách nhảy / lướt (I) hoặc núp sau bục.
    /// PHÁ HUỶ ĐƯỢC: chém (J) hoặc ném phi tiêu (K) trúng thân súng; ít máu hơn trụ triệu hồi, nổ và rơi xu khi hết máu.
    /// Đặt chân súng ở mặt đất hoặc trên bục; Facing = 1 / -1 chỉ là hướng nòng lúc nghỉ và hướng hình thân súng.
    /// </summary>
    public class TurretTrap : Trap
    {
        [Header("Hướng bắn")]
        [Tooltip("1 = nòng nghỉ ở bên phải, -1 = bên trái (hướng lúc chưa thấy người chơi)")] public int facing = -1;
        [Tooltip("true = nòng ngắm theo người chơi trong cung xoay, false = đứng yên ở hướng nghỉ")] public bool aimAtPlayer = true;
        [Tooltip("Góc tâm của cung xoay (độ). 90 = thẳng lên trời, 0 = sang phải, 180 = sang trái")] public float aimCenterAngle = 90f;
        [Tooltip("Độ rộng cung xoay của nòng (độ). 180 = quay được nửa vòng tròn; 360 = quay tròn")] public float aimArc = 180f;
        [Tooltip("Tốc độ xoay nòng (độ / giây)")] public float turnSpeed = 220f;

        [Header("Phá hủy được")]
        [Tooltip("Máu của súng (trụ triệu hồi có 420)")] public float health = 110f;
        [Tooltip("Số xu rơi ra khi bị phá")] public int rewardCoins = 8;
        [Tooltip("Hộp trúng đòn (rộng, cao) và vị trí tâm so với chân súng")] public Vector2 bodyHitSize = new Vector2(1.3f, 1.5f);
        public Vector2 bodyHitOffset = new Vector2(0f, 0.8f);
        [Tooltip("Vị trí thanh máu so với chân súng")] public Vector2 barOffset = new Vector2(0f, 2.1f);

        [Header("Bắn")]
        [Tooltip("Chỉ bắn khi người chơi cách bẫy không quá khoảng này (theo trục ngang)")] public float range = 22f;
        [Tooltip("Thời gian nghỉ giữa các loạt bắn (giây)")] public float fireInterval = 2.4f;
        [Tooltip("Thời gian tụ năng lượng báo hiệu trước khi bắn (giây)")] public float chargeTime = 0.55f;
        [Tooltip("Số viên mỗi loạt")] public int burstCount = 1;
        [Tooltip("Khoảng cách giữa các viên trong loạt (giây)")] public float burstGap = 0.14f;
        public float projectileSpeed = 10f;
        public Color boltColor = new Color(1f, 0.2f, 0.25f, 1f);
        [Tooltip("Lệch pha lúc bắt đầu (giây)")] public float startOffset = 0f;

        [Header("Vị trí nòng so với chân bẫy")]
        public float barrelHeight = 0.9f;
        public float muzzleLength = 1.0f;

        [Header("Hình ảnh (tự tìm theo tên nếu để trống)")]
        public SpriteRenderer baseSr;       // "Visual"
        public Transform barrel;            // "Barrel" (quay theo hướng bắn)
        public SpriteRenderer muzzleGlow;   // "MuzzleGlow" (sáng lên khi tụ năng lượng)

        float cd;
        float chargeT = -1f;
        int burstLeft;
        float burstT;
        float rel;                           // góc nòng so với tâm cung (độ), nằm trong [-arc/2, arc/2]
        Vector2 aimDir;
        SpriteRenderer barrelSr;
        TurretBody body;

        float HalfArc { get { return Mathf.Clamp(aimArc, 0f, 360f) * 0.5f; } }
        float RestRel { get { return facing >= 0 ? -HalfArc : HalfArc; } }

        void Awake()
        {
            if (baseSr == null) baseSr = FindSr("Visual");
            if (barrel == null) barrel = transform.Find("Barrel");
            if (barrel != null) barrelSr = barrel.GetComponent<SpriteRenderer>();
            if (muzzleGlow == null) muzzleGlow = FindSr("MuzzleGlow");
            facing = facing >= 0 ? 1 : -1;
            rel = Mathf.Clamp(RestRel, -HalfArc, HalfArc);
            UpdateAimDir();
            cd = fireInterval * 0.5f + startOffset;
            if (baseSr != null) baseSr.flipX = facing < 0;
            if (barrel != null) barrel.localPosition = new Vector3(0f, barrelHeight, 0f);
            Face();

            // thân súng có máu: bị chém / phi tiêu trúng như trụ triệu hồi
            body = GetComponent<TurretBody>();
            if (body == null) body = gameObject.AddComponent<TurretBody>();
            body.Setup(this, health, bodyHitSize, bodyHitOffset, barOffset);
        }

        void UpdateAimDir()
        {
            float a = (aimCenterAngle + rel) * Mathf.Deg2Rad;
            aimDir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        void Face()
        {
            if (barrel != null)
            {
                barrel.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg);
                if (barrelSr != null) barrelSr.flipY = aimDir.x < 0f;
            }
            if (muzzleGlow != null)
                muzzleGlow.transform.localPosition = new Vector3(0f, barrelHeight, 0f) + (Vector3)(aimDir * muzzleLength);
        }

        /// <summary>Góc (so với tâm cung) mà nòng muốn quay tới để nhắm vào điểm target; bị giới hạn trong cung xoay.</summary>
        float WantRel(Vector2 pivot, Vector2 target)
        {
            Vector2 d = target - pivot;
            float a = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            float r = Mathf.DeltaAngle(aimCenterAngle, a);
            float h = HalfArc;
            return h >= 179.5f ? r : Mathf.Clamp(r, -h, h);
        }

        void Update()
        {
            if (!Running) return;
            if (body != null && body.dead) return;
            var pl = Target;
            if (pl == null) return;
            float dt = Time.deltaTime;

            Vector2 pivot = (Vector2)transform.position + new Vector2(0f, barrelHeight);
            bool inRange = Mathf.Abs(pl.Position.x - transform.position.x) <= range && Mathf.Abs(pl.Center.y - pivot.y) <= 14f;

            float want = (aimAtPlayer && inRange) ? WantRel(pivot, pl.Center) : Mathf.Clamp(RestRel, -HalfArc, HalfArc);
            rel = Mathf.MoveTowards(rel, want, Mathf.Max(10f, turnSpeed) * dt);
            UpdateAimDir();
            Face();

            float glowA = 0f;
            if (burstLeft > 0)
            {
                burstT -= dt;
                if (burstT <= 0f)
                {
                    Fire(pivot);
                    burstLeft--;
                    burstT = burstGap;
                    if (burstLeft <= 0) cd = fireInterval;
                }
                glowA = 0.9f;
            }
            else if (chargeT >= 0f)
            {
                chargeT += dt;
                glowA = Mathf.Clamp01(chargeT / Mathf.Max(0.05f, chargeTime));
                if (chargeT >= chargeTime) { chargeT = -1f; burstLeft = Mathf.Max(1, burstCount); burstT = 0f; }
            }
            else
            {
                cd -= dt;
                if (cd <= 0f)
                {
                    if (inRange) { chargeT = 0f; Sfx.PlayAt("charge", transform.position, 0.35f, 1.6f); }
                    else cd = 0f;
                }
            }

            if (muzzleGlow != null)
            {
                float pulse = chargeT >= 0f ? 0.6f + 0.4f * Mathf.Sin(Time.time * 40f) : 1f;
                muzzleGlow.color = Cfg.A(boltColor, glowA * pulse);
                float sz = 0.5f + glowA * 0.9f;
                muzzleGlow.transform.localScale = new Vector3(sz, sz, 1f);
            }
        }

        void Fire(Vector2 pivot)
        {
            Vector2 pos = pivot + aimDir * muzzleLength;
            Projectile.Bolt(pos, aimDir, projectileSpeed, damage, boltColor);
            Sfx.PlayAt("bolt", pos, 0.5f, 0.9f);
            Fx.Sparks(pos, aimDir, boltColor, 4, 8f, 30f, 0.2f);
            Fx.Flare(pos, boltColor, 0.8f, 0.12f);
        }

        void OnDrawGizmosSelected()
        {
            Vector2 p = (Vector2)transform.position + new Vector2(0f, barrelHeight);
            float h = Mathf.Clamp(aimArc, 0f, 360f) * 0.5f;
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
            for (int s = -1; s <= 1; s += 2)
            {
                float a = (aimCenterAngle + s * h) * Mathf.Deg2Rad;
                Gizmos.DrawLine(p, p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Mathf.Min(range, 6f));
            }
            Gizmos.color = Color.yellow;
            Vector2 c = (Vector2)transform.position + bodyHitOffset;
            Gizmos.DrawWireCube(c, bodyHitSize);
        }
    }
}
