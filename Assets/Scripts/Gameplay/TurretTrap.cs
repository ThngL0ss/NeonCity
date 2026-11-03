using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Súng tự động: đứng yên, khi người chơi vào tầm thì tụ năng lượng (đầu nòng sáng lên báo hiệu) rồi bắn đạn.
    /// Né bằng cách nhảy / lướt (I) hoặc núp sau bục. Có thể bắn thẳng hoặc ngắm theo người chơi (giới hạn góc).
    /// Đặt chân súng ở mặt đất hoặc trên bục; Facing = 1 quay phải, -1 quay trái.
    /// </summary>
    public class TurretTrap : Trap
    {
        [Header("Hướng bắn")]
        [Tooltip("1 = quay sang phải, -1 = quay sang trái")] public int facing = -1;
        [Tooltip("true = nòng ngắm theo người chơi (trong giới hạn góc), false = bắn thẳng theo hướng quay")] public bool aimAtPlayer = true;
        [Tooltip("Góc ngắm tối đa lệch khỏi hướng quay (độ)")] public float aimMaxAngle = 40f;

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
        Vector2 aimDir;
        SpriteRenderer barrelSr;

        void Awake()
        {
            if (baseSr == null) baseSr = FindSr("Visual");
            if (barrel == null) barrel = transform.Find("Barrel");
            if (barrel != null) barrelSr = barrel.GetComponent<SpriteRenderer>();
            if (muzzleGlow == null) muzzleGlow = FindSr("MuzzleGlow");
            facing = facing >= 0 ? 1 : -1;
            aimDir = new Vector2(facing, 0f);
            cd = fireInterval * 0.5f + startOffset;
            if (baseSr != null) baseSr.flipX = facing < 0;
            if (barrel != null) barrel.localPosition = new Vector3(0f, barrelHeight, 0f);
            Face();
        }

        static Vector2 Rotate(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
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

        void Update()
        {
            if (!Running) return;
            var pl = Target;
            if (pl == null) return;
            float dt = Time.deltaTime;

            Vector2 pivot = (Vector2)transform.position + new Vector2(0f, barrelHeight);
            Vector2 fwd = new Vector2(facing, 0f);
            bool inRange = Mathf.Abs(pl.Position.x - transform.position.x) <= range && Mathf.Abs(pl.Center.y - pivot.y) <= 14f;

            Vector2 want = fwd;
            if (aimAtPlayer && inRange)
            {
                float ang = Mathf.Clamp(Vector2.SignedAngle(fwd, pl.Center - pivot), -aimMaxAngle, aimMaxAngle);
                want = Rotate(fwd, ang);
            }
            aimDir = Vector2.Lerp(aimDir, want, 1f - Mathf.Exp(-12f * dt)).normalized;
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
            Gizmos.color = Color.red;
            Vector2 p = (Vector2)transform.position + new Vector2(0f, barrelHeight);
            Gizmos.DrawLine(p, p + new Vector2(facing * range, 0f));
        }
    }
}
