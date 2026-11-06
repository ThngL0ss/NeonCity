using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Thân súng tự động (TurretTrap) khi bị đánh: có máu, bị chém / phi tiêu trúng như trụ triệu hồi, nổ khi hết máu.
    /// Tự được TurretTrap thêm vào lúc chạy game (không cần gắn tay vào Prefab); mọi số chỉnh ở TurretTrap ▸ "Phá hủy được".
    /// </summary>
    public class TurretBody : Hittable
    {
        public TurretTrap turret;
        WorldBar bar;
        Flasher flasher;
        float flashT, barShowT;

        public override bool Targetable { get { return !dead && GameManager.State == GameState.Playing; } }

        public void Setup(TurretTrap t, float health, Vector2 size, Vector2 offset, Vector2 barOffset)
        {
            turret = t;
            maxHp = hp = Mathf.Max(1f, health);
            hitSize = size;
            hitOffset = offset;
            Art.Init();
            bar = new WorldBar(transform, barOffset, 1.5f, 0.13f, Cfg.Orange);
            bar.SetVisible(false);
            flasher = new Flasher(gameObject);
        }

        void Update()
        {
            if (dead) return;
            if (flashT > 0f) flashT -= Time.deltaTime;
            if (barShowT > 0f) barShowT -= Time.deltaTime;
            if (flasher != null) flasher.Apply(flashT > 0f, 1f);
            if (bar != null) bar.SetVisible(barShowT > 0f);
        }

        public override void TakeHit(float dmg, Vector2 dir, float knockPower, bool melee)
        {
            if (dead || !GameManager.Active) return;
            hp -= dmg;
            flashT = 0.07f;
            barShowT = 2.5f;
            if (bar != null) { bar.SetVisible(true); bar.Set(hp / maxHp); }
            Sfx.PlayAt("towerHit", transform.position, 0.7f, Random.Range(1.1f, 1.5f));
            Fx.Sparks(Center, dir, Cfg.Orange, 7, 11f, 60f);
            Fx.Burst(Center, Cfg.A(Cfg.Red, 0.9f), 4, 5f, 0.3f, 0.26f);
            Fx.Text(Center + new Vector2(0f, 1.0f), Mathf.RoundToInt(dmg).ToString(), Cfg.Yellow);
            if (hp <= 0f) Die();
        }

        void Die()
        {
            dead = true;
            Vector2 c = Center;
            Fx.Explosion(c, Cfg.Orange, true);
            Fx.Ring(c, Cfg.A(Cfg.White, 0.9f), 0.5f, 5f, 0.4f);
            Fx.Shake(0.4f);
            Sfx.PlayAt("towerDie", transform.position, 0.8f, 1.4f);
            int coins = turret != null ? turret.rewardCoins : 0;
            if (coins > 0) Coin.Drop(c, coins);
            Destroy(gameObject);
        }
    }
}
