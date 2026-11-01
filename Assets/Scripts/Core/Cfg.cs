using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Toàn bộ thông số cân bằng của game nằm ở đây. Muốn game dễ/khó hơn chỉ cần sửa file này.
    /// All balance numbers live here. Tweak this file to make the game easier / harder.
    /// </summary>
    public static class Cfg
    {
        // ------------------------------------------------------------ Language
        public static bool Vietnamese = true;
        public static string L(string vi, string en) { return Vietnamese ? vi : en; }

        // ------------------------------------------------------------ Layers
        // Ground/platforms dùng layer 0 (Default). Các layer 8-10 chỉ dùng theo số, không cần đặt tên.
        public const int LayerPlayer = 8;
        public const int LayerEnemy = 9;
        public const int LayerItem = 10;
        public static int GroundMask { get { return 1 << 0; } }

        // ------------------------------------------------------------ Level layout
        public static float LevelMinX = -10f;
        public static float LevelMaxX = 170f;
        public static float GateX = 128f;          // cổng vào khu boss
        public static float BossTriggerX = 135f;   // đi qua mốc này (khi đã phá hết trụ) => boss xuất hiện
        public static float AltarX = 150f;         // tâm khu boss

        // ------------------------------------------------------------ Player
        public static float PlayerMaxHp = 100f;
        public static float PlayerSpeed = 8.2f;
        public static float PlayerAccelGround = 95f;
        public static float PlayerAccelAir = 55f;
        public static float JumpVelocity = 15.5f;
        public static float GravityUp = 3.4f;
        public static float GravityDown = 4.8f;
        public static float CoyoteTime = 0.10f;
        public static float JumpBuffer = 0.12f;

        // Cận chiến (J) - tầm ngắn, buộc phải áp sát => hit & run
        public static float MeleeDamage = 20f;
        public static float MeleeReach = 2.1f;
        public static float MeleeHeight = 2.1f;
        public static float MeleeWindup = 0.07f;
        public static float MeleeDuration = 0.28f;
        public static float MeleeCooldown = 0.36f;
        public static float MeleeMoveMul = 0.40f;   // vừa chém vừa di chuyển chậm lại

        // Lướt (I) - bất tử trong lúc lướt
        public static float DashSpeed = 25f;
        public static float DashTime = 0.19f;
        public static float DashCooldown = 1.5f;
        public static float DashInvulnExtra = 0.06f;

        // Phi tiêu (K)
        public static float ShurikenDamage = 13f;
        public static float ShurikenSpeed = 21f;
        public static float ShurikenRange = 13f;
        public static float ShurikenCooldown = 0.55f;
        public static int ShurikenPierce = 2;

        // Hồi máu (L)
        public static int HealStartCharges = 3;
        public static int HealMaxCharges = 5;
        public static float HealAmount = 38f;
        public static float HealChannel = 0.50f;    // đứng yên 0.5s => rủi ro
        public static float HealCooldown = 1.2f;

        public static float HurtInvuln = 0.85f;

        // ------------------------------------------------------------ Enemies
        public static int MaxEnemiesAlive = 8;       // trần số quái nhỏ cùng lúc
        public static int MaxPerTower = 3;
        public static float DropChance = 0.20f;      // 20% rơi vật phẩm hồi máu

        public static float TowerActivateRange = 16f;
        public static float TowerHp = 420f;
        public static float TowerSpawnInterval = 4.2f;

        public static float WalkerAggroRange = 20f;
        public static float WalkerWindupRange = 4.3f;
        public static float WalkerWindup = 0.5f;     // thời gian báo hiệu trước khi lao tới (cơ hội né / chém ngắt)
        public static float WalkerLungeSpeed = 13f;
        public static float WalkerLungeTime = 0.28f;
        public static float WalkerRecover = 0.75f;   // sau khi lao: đứng thở => cơ hội phản công

        public static float WalkerHp = 48f;
        public static float WalkerSpeed = 3.7f;
        public static float WalkerLungeDamage = 13f;
        public static float WalkerTouchDamage = 5f;

        public static float FlyerHp = 30f;
        public static float FlyerSpeed = 5.0f;
        public static float FlyerBoltDamage = 9f;
        public static float FlyerFireInterval = 2.7f;

        // ------------------------------------------------------------ Boss
        public static float BossP1Hp = 2800f;        // phase 1: chậm, máu trâu
        public static float BossP2Hp = 1500f;        // phase 2: nhanh, máu ít hơn (nhưng hút máu)
        public static float BossP1Speed = 2.0f;
        public static float BossP2Speed = 5.4f;
        public static float BossSlamDamage = 18f;
        public static float BossTouchDamage = 12f;
        public static float BossBoltDamage = 10f;
        public static float BossSlashDamage = 24f;
        public static float BossLanceDamage = 22f;
        public static float BossLifesteal = 0.60f;   // hút 60% sát thương gây ra (phase 2)

        // ------------------------------------------------------------ Palette (neon)
        public static readonly Color Cyan = new Color(0.10f, 0.95f, 1.00f, 1f);
        public static readonly Color Magenta = new Color(1.00f, 0.15f, 0.80f, 1f);
        public static readonly Color Purple = new Color(0.55f, 0.30f, 1.00f, 1f);
        public static readonly Color Lime = new Color(0.45f, 1.00f, 0.40f, 1f);
        public static readonly Color Orange = new Color(1.00f, 0.60f, 0.10f, 1f);
        public static readonly Color Red = new Color(1.00f, 0.20f, 0.25f, 1f);
        public static readonly Color Yellow = new Color(1.00f, 0.92f, 0.30f, 1f);
        public static readonly Color White = new Color(1f, 1f, 1f, 1f);
        public static readonly Color Night = new Color(0.020f, 0.025f, 0.075f, 1f);

        public static Color A(Color c, float a) { return new Color(c.r, c.g, c.b, a); }
    }

    /// <summary>Tương thích Unity 6 (linearVelocity) và các bản cũ (velocity).</summary>
    public static class PhysExt
    {
        public static Vector2 Vel(this Rigidbody2D rb)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }

        public static void SetVel(this Rigidbody2D rb, Vector2 v)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
        }
    }
}
