// FILE TỰ SINH bởi Tools/gen_config.py - đừng sửa tay. Thêm/bớt thông số ở Cfg.cs rồi chạy lại script.
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Toàn bộ thông số cân bằng game, chỉnh trực tiếp trong Inspector.
    /// File asset: Assets/Resources/NeonBreaker/GameConfig.asset (do Build Project tạo).
    /// Khi bấm Play, các giá trị này được ghi vào Cfg (nơi code đọc).
    /// </summary>
    [CreateAssetMenu(menuName = "Neon Breaker/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Language")]
        public bool Vietnamese = true;

        [Header("Player")]
        public float PlayerMaxHp = 100f;
        public float PlayerSpeed = 8.2f;
        public float PlayerAccelGround = 95f;
        public float PlayerAccelAir = 55f;
        public float JumpVelocity = 15.5f;
        public float GravityUp = 3.4f;
        public float GravityDown = 4.8f;
        public float CoyoteTime = 0.10f;
        public float JumpBuffer = 0.12f;
        public float MeleeDamage = 20f;
        public float MeleeReach = 2.1f;
        public float MeleeHeight = 2.1f;
        public float MeleeWindup = 0.07f;
        public float MeleeDuration = 0.28f;
        public float MeleeCooldown = 0.36f;
        public float MeleeMoveMul = 0.40f;
        public float DashSpeed = 25f;
        public float DashTime = 0.19f;
        public float DashCooldown = 1.5f;
        public float DashInvulnExtra = 0.06f;
        public float ShurikenDamage = 13f;
        public float ShurikenSpeed = 21f;
        public float ShurikenRange = 13f;
        public float ShurikenCooldown = 0.55f;
        public int ShurikenPierce = 2;
        public int HealStartCharges = 3;
        public int HealMaxCharges = 5;
        public float HealAmount = 38f;
        public float HealChannel = 0.50f;
        public float HealCooldown = 1.2f;
        public float HurtInvuln = 0.85f;

        [Header("Enemies")]
        public int MaxEnemiesAlive = 8;
        public int MaxPerTower = 3;
        public float DropChance = 0.20f;
        public float TowerActivateRange = 16f;
        public float TowerHp = 420f;
        public float TowerSpawnInterval = 4.2f;
        public float WalkerAggroRange = 20f;
        public float WalkerWindupRange = 4.3f;
        public float WalkerWindup = 0.5f;
        public float WalkerLungeSpeed = 13f;
        public float WalkerLungeTime = 0.28f;
        public float WalkerRecover = 0.75f;
        public float WalkerHp = 48f;
        public float WalkerSpeed = 3.7f;
        public float WalkerLungeDamage = 13f;
        public float WalkerTouchDamage = 5f;
        public float FlyerHp = 30f;
        public float FlyerSpeed = 5.0f;
        public float FlyerBoltDamage = 9f;
        public float FlyerFireInterval = 2.7f;

        [Header("Boss")]
        public float BossP1Hp = 2800f;
        public float BossP2Hp = 1500f;
        public float BossP1Speed = 2.0f;
        public float BossP2Speed = 5.4f;
        public float BossSlamDamage = 18f;
        public float BossTouchDamage = 12f;
        public float BossBoltDamage = 10f;
        public float BossSlashDamage = 24f;
        public float BossLanceDamage = 22f;
        public float BossLifesteal = 0.60f;

        public static GameConfig Load()
        {
            return Resources.Load<GameConfig>("NeonBreaker/GameConfig");
        }

        /// <summary>Ghi mọi giá trị trong asset này vào Cfg.</summary>
        public void Apply()
        {
            Cfg.Vietnamese = Vietnamese;
            Cfg.PlayerMaxHp = PlayerMaxHp;
            Cfg.PlayerSpeed = PlayerSpeed;
            Cfg.PlayerAccelGround = PlayerAccelGround;
            Cfg.PlayerAccelAir = PlayerAccelAir;
            Cfg.JumpVelocity = JumpVelocity;
            Cfg.GravityUp = GravityUp;
            Cfg.GravityDown = GravityDown;
            Cfg.CoyoteTime = CoyoteTime;
            Cfg.JumpBuffer = JumpBuffer;
            Cfg.MeleeDamage = MeleeDamage;
            Cfg.MeleeReach = MeleeReach;
            Cfg.MeleeHeight = MeleeHeight;
            Cfg.MeleeWindup = MeleeWindup;
            Cfg.MeleeDuration = MeleeDuration;
            Cfg.MeleeCooldown = MeleeCooldown;
            Cfg.MeleeMoveMul = MeleeMoveMul;
            Cfg.DashSpeed = DashSpeed;
            Cfg.DashTime = DashTime;
            Cfg.DashCooldown = DashCooldown;
            Cfg.DashInvulnExtra = DashInvulnExtra;
            Cfg.ShurikenDamage = ShurikenDamage;
            Cfg.ShurikenSpeed = ShurikenSpeed;
            Cfg.ShurikenRange = ShurikenRange;
            Cfg.ShurikenCooldown = ShurikenCooldown;
            Cfg.ShurikenPierce = ShurikenPierce;
            Cfg.HealStartCharges = HealStartCharges;
            Cfg.HealMaxCharges = HealMaxCharges;
            Cfg.HealAmount = HealAmount;
            Cfg.HealChannel = HealChannel;
            Cfg.HealCooldown = HealCooldown;
            Cfg.HurtInvuln = HurtInvuln;
            Cfg.MaxEnemiesAlive = MaxEnemiesAlive;
            Cfg.MaxPerTower = MaxPerTower;
            Cfg.DropChance = DropChance;
            Cfg.TowerActivateRange = TowerActivateRange;
            Cfg.TowerHp = TowerHp;
            Cfg.TowerSpawnInterval = TowerSpawnInterval;
            Cfg.WalkerAggroRange = WalkerAggroRange;
            Cfg.WalkerWindupRange = WalkerWindupRange;
            Cfg.WalkerWindup = WalkerWindup;
            Cfg.WalkerLungeSpeed = WalkerLungeSpeed;
            Cfg.WalkerLungeTime = WalkerLungeTime;
            Cfg.WalkerRecover = WalkerRecover;
            Cfg.WalkerHp = WalkerHp;
            Cfg.WalkerSpeed = WalkerSpeed;
            Cfg.WalkerLungeDamage = WalkerLungeDamage;
            Cfg.WalkerTouchDamage = WalkerTouchDamage;
            Cfg.FlyerHp = FlyerHp;
            Cfg.FlyerSpeed = FlyerSpeed;
            Cfg.FlyerBoltDamage = FlyerBoltDamage;
            Cfg.FlyerFireInterval = FlyerFireInterval;
            Cfg.BossP1Hp = BossP1Hp;
            Cfg.BossP2Hp = BossP2Hp;
            Cfg.BossP1Speed = BossP1Speed;
            Cfg.BossP2Speed = BossP2Speed;
            Cfg.BossSlamDamage = BossSlamDamage;
            Cfg.BossTouchDamage = BossTouchDamage;
            Cfg.BossBoltDamage = BossBoltDamage;
            Cfg.BossSlashDamage = BossSlashDamage;
            Cfg.BossLanceDamage = BossLanceDamage;
            Cfg.BossLifesteal = BossLifesteal;
        }
    }
}
