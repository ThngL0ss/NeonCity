using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Kẻ địch dựng bằng code cho Rìa Phố Neon: robot canh cửa Phòng thí nghiệm Dyber, lính áo giáp trắng đuổi cậu bé, đám cướp ở bãi rác.
    /// AI đơn giản: đứng chờ (Idle) → bị đánh thức (Wake) thì lao vào kẻ gần nhất (người chơi hoặc lính đánh thuê) → đánh gần hoặc bắn xa.
    /// Retreat() cho kẻ địch quay về chỗ cũ (robot canh cửa). Cấp độ / cấp bậc quyết định máu + sát thương (PowerRules).
    /// </summary>
    public class RimFoe : Hittable
    {
        public enum Kind { LabRobot, Guard, Robber }

        public static readonly List<RimFoe> Live = new List<RimFoe>();
        public static void ResetStatics() { Live.Clear(); }

        public Kind kind;
        public Rank rank = Rank.C;
        public int level = 1;
        public bool ranged;
        public float damage = 10f, speed = 4f, size = 1f, reach = 1.7f, homeX;
        public float minX = -1e9f, maxX = 1e9f;
        public int coinMin = 1, coinMax = 3;
        public float oilChance = 0.3f;
        public System.Action<RimFoe> onDied;
        public System.Action<RimFoe> onHurt;                  // bị người chơi / lính đánh thuê đánh trúng
        public bool aggro;

        enum S { Idle, Chase, Windup, Recover, Retreat }
        S st = S.Idle;
        float t, kb, stunT, flashT, animT, shootCd, weakT;
        int face = 1;
        Look look;
        int eye = 0x35E8FF;
        SpriteRenderer sr;
        Transform vis;
        Sprite idle, walkA, walkB, robot;
        WorldBar bar;
        Color accent = Cfg.Orange;
        float bobT;

        public override bool Targetable { get { return !dead; } }

        // ------------------------------------------------------------ tạo
        public static RimFoe Spawn(Transform parent, Kind kind, Look look, float x, int level, Rank rank, bool ranged)
        {
            Art.Init();
            var go = new GameObject("Foe_" + kind);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, 0f, 0f);
            var f = go.AddComponent<RimFoe>();
            f.kind = kind; f.level = PowerRules.ClampLevel(level); f.rank = rank; f.ranged = ranged; f.look = look; f.homeX = x;
            float hp0, dmg0, spd, scale = 1f;
            switch (kind)
            {
                case Kind.LabRobot: hp0 = ranged ? 90f : 140f; dmg0 = ranged ? 9f : 12f; spd = ranged ? 3.6f : 4.4f; f.accent = Cfg.Cyan; f.coinMin = 3; f.coinMax = 8; f.oilChance = 0.6f; scale = 1.15f; break;
                case Kind.Guard: hp0 = 150f; dmg0 = 14f; spd = 5.4f; f.accent = Cfg.White; f.coinMin = 4; f.coinMax = 9; f.oilChance = 0.2f; break;
                default: hp0 = rank >= Rank.B ? 120f : 95f; dmg0 = rank >= Rank.B ? 14f : 10f; spd = ranged ? 3.8f : 4.8f; f.accent = Cfg.Red; f.coinMin = 2; f.coinMax = rank >= Rank.B ? 7 : 5; f.oilChance = 0.25f; break;
            }
            f.maxHp = hp0 * PowerRules.HpMul(f.level);
            f.hp = f.maxHp;
            f.damage = dmg0 * PowerRules.DamageMul(f.level);
            f.speed = spd * Mathf.Min(1.4f, PowerRules.SpeedMul(f.level));
            f.size = scale;
            f.reach = ranged ? 7.5f : 1.8f * scale;
            f.hitSize = new Vector2(0.9f * scale, 1.6f * scale);
            f.hitOffset = new Vector2(0f, 0.8f * scale);
            var v = new GameObject("Visual");
            v.transform.SetParent(go.transform, false);
            f.vis = v.transform;
            f.sr = v.AddComponent<SpriteRenderer>();
            f.sr.sharedMaterial = Art.DefMat;
            f.sr.sortingOrder = 22;
            if (kind == Kind.LabRobot) f.SetEye(false);
            else { f.idle = TownArt.Human(look, 0); f.walkA = TownArt.Human(look, 1); f.walkB = TownArt.Human(look, 2); f.sr.sprite = f.idle; }
            v.transform.localScale = new Vector3(scale, scale, 1f);
            f.bar = new WorldBar(go.transform, new Vector2(0f, 1.75f * scale + 0.3f), 1.1f * scale, 0.12f, Cfg.Red, 40);
            f.bar.Set(1f);
            return f;
        }

        /// <summary>Robot: đổi màu mắt (đỏ = đang thù địch).</summary>
        public void SetEye(bool red)
        {
            eye = red ? 0xFF2A2A : 0x35E8FF;
            robot = TownArt.Robot(0, eye);
            if (sr != null) sr.sprite = robot;
        }

        public void FaceDir(int f) { face = f >= 0 ? 1 : -1; ApplyFace(); }

        void ApplyFace()
        {
            if (vis != null) vis.localScale = new Vector3(face * size, size, 1f);
        }

        // ------------------------------------------------------------ điều khiển từ bên ngoài
        public void Wake() { if (dead) return; aggro = true; if (st == S.Idle || st == S.Retreat) st = S.Chase; }
        public void Retreat() { if (dead) return; aggro = false; st = S.Retreat; }
        public bool AtHome { get { return Mathf.Abs(transform.position.x - homeX) < 0.4f; } }

        public void Stun(float dur)
        {
            if (dead) return;
            stunT = Mathf.Max(stunT, dur);
            if (st == S.Windup) st = S.Chase;
            Fx.Text(Center + new Vector2(0f, 1.2f * size), Cfg.L("CHOÁNG!", "STUNNED!"), Cfg.Yellow, 1.0f);
        }

        public void Weaken(float dur) { weakT = Mathf.Max(weakT, dur); }

        public void Push(float vx) { kb = vx; }

        // ------------------------------------------------------------ nhận đòn
        public override void TakeHit(float dmg, Vector2 dir, float knock, bool melee)
        {
            if (dead) return;
            hp -= dmg;
            flashT = 0.08f;
            Sfx.PlayAt("hit", transform.position, 0.8f, melee ? 1f : 1.3f);
            Fx.Sparks(Center, dir, accent, 7, 11f, 55f);
            Fx.Text(Center + new Vector2(0f, 0.9f * size), Mathf.RoundToInt(dmg).ToString(), Cfg.Yellow);
            kb += Mathf.Sign(dir.x == 0f ? 1f : dir.x) * knock * 0.5f / size;
            if (!aggro) Wake();
            if (onHurt != null) onHurt(this);
            if (hp <= 0f) Die(dir);
        }

        void Die(Vector2 dir)
        {
            if (dead) return;
            dead = true;
            Fx.Explosion(Center, accent, false);
            Fx.Sparks(Center, dir, Cfg.White, 10, 14f, 50f);
            Fx.Shake(0.12f);
            Sfx.PlayAt("enemyDie", transform.position, 0.9f);
            int coins = Random.Range(coinMin, coinMax + 1);
            if (coins > 0) Coin.Drop(Center, coins);
            if (Random.value < oilChance) OilDrop.Drop(Center, Random.Range(1, 4));
            if (onDied != null) onDied(this);
            Destroy(gameObject);
        }

        /// <summary>Biến mất không rơi đồ (kẻ địch hết thù địch / dọn dẹp).</summary>
        public void Vanish()
        {
            if (dead) return;
            dead = true;
            Fx.Burst(Center, Cfg.A(accent, 0.9f), 8, 5f, 0.5f, 0.3f);
            Fx.Ring(Center, Cfg.A(Cfg.White, 0.7f), 0.4f, 2.5f, 0.3f);
            Destroy(gameObject);
        }

        protected override void OnEnable() { base.OnEnable(); if (!Live.Contains(this)) Live.Add(this); }
        protected override void OnDisable() { base.OnDisable(); Live.Remove(this); }

        // ------------------------------------------------------------ vòng lặp
        void Update()
        {
            if (dead) return;
            bar.Set(maxHp > 0f ? hp / maxHp : 0f);
            if (flashT > 0f) flashT -= Time.deltaTime;
            Color c = Color.white;
            if (weakT > 0f) { weakT -= Time.deltaTime; c = new Color(0.7f, 0.7f, 1f); }
            if (flashT > 0f) c = Color.white;
            else if (st == S.Windup) c = Color.Lerp(Color.white, Cfg.Yellow, 0.5f + 0.5f * Mathf.Sin(Time.time * 40f));
            sr.color = c;

            if (BaseUi.IsOpen || !GameManager.Active || Interior.Busy) return;
            float dt = Time.deltaTime;
            if (stunT > 0f) { stunT -= dt; Anim(false); return; }
            if (Mathf.Abs(kb) > 0.05f) { Move(kb * dt); kb = Mathf.MoveTowards(kb, 0f, 26f * dt); }
            shootCd -= dt;
            float dmgNow = damage * (weakT > 0f ? 0.5f : 1f);

            if (st == S.Retreat)
            {
                float dx = homeX - transform.position.x;
                if (Mathf.Abs(dx) < 0.3f) { st = S.Idle; Anim(false); if (kind == Kind.LabRobot) SetEye(false); return; }
                face = dx > 0f ? 1 : -1;
                Move(face * speed * 0.9f * dt);
                Anim(true);
                return;
            }
            if (st == S.Idle) { Anim(false); return; }

            Vector2 tp;
            Merc tm;
            if (!PickTarget(out tp, out tm)) { Anim(false); return; }
            float ddx = tp.x - transform.position.x, dist = Mathf.Abs(ddx);

            switch (st)
            {
                case S.Chase:
                    if (Mathf.Abs(ddx) > 0.05f) face = ddx > 0f ? 1 : -1;
                    if (ranged)
                    {
                        if (dist > reach) { Move(face * speed * dt); Anim(true); }
                        else if (dist < reach * 0.45f) { Move(-face * speed * 0.7f * dt); Anim(true); }
                        else Anim(false);
                        if (dist <= reach + 1f && shootCd <= 0f)
                        {
                            shootCd = Random.Range(1.3f, 2.0f);
                            Vector2 from = Center + new Vector2(face * 0.7f, 0.15f);
                            RimBolt.Fire(from, tp - from, 15f, dmgNow, kind == Kind.LabRobot ? Cfg.Cyan : Cfg.Orange);
                            Sfx.PlayAt("bolt", transform.position, 0.6f, 1.2f);
                        }
                    }
                    else if (dist > reach * 0.85f) { Move(face * speed * dt); Anim(true); }
                    else { st = S.Windup; t = kind == Kind.LabRobot ? 0.4f : 0.3f; Anim(false); }
                    break;
                case S.Windup:
                    t -= dt; Anim(false);
                    if (t <= 0f)
                    {
                        Rect r = Geo.Box(Center + new Vector2(face * reach * 0.5f, 0f), new Vector2(reach, 1.6f * size));
                        Sfx.PlayAt("slash", transform.position, 0.7f, 0.8f);
                        Fx.Sparks(Center + new Vector2(face * reach * 0.6f, 0f), new Vector2(face, 0f), accent, 5, 9f, 40f);
                        var pl = Player.Instance;
                        if (pl != null && !pl.Dead && r.Overlaps(pl.HurtRect)) pl.Hurt(dmgNow, Center, 7f);
                        var m = Merc.Nearest(Center, reach + 1f);
                        if (m != null && r.Overlaps(m.HurtRect)) m.Hurt(dmgNow, Center);
                        st = S.Recover; t = 0.6f;
                    }
                    break;
                case S.Recover:
                    t -= dt; Anim(false);
                    if (t <= 0f) st = S.Chase;
                    break;
            }
        }

        bool PickTarget(out Vector2 pos, out Merc merc)
        {
            pos = Vector2.zero; merc = null;
            var pl = Player.Instance;
            bool plOk = pl != null && !pl.Dead;
            var m = Merc.Nearest(transform.position, 9f);
            if (plOk) pos = pl.Center;
            if (m != null && (!plOk || Mathf.Abs(m.Center.x - transform.position.x) < Mathf.Abs(pos.x - transform.position.x) - 1f)) { merc = m; pos = m.Center; return true; }
            return plOk;
        }

        void Move(float dx)
        {
            var p = transform.position;
            p.x = Mathf.Clamp(p.x + dx, minX, maxX);
            transform.position = p;
        }

        void Anim(bool moving)
        {
            ApplyFace();
            if (kind == Kind.LabRobot)
            {
                sr.sprite = robot;
                bobT += moving ? Time.deltaTime * 14f : 0f;
                vis.localPosition = new Vector3(0f, moving ? Mathf.Abs(Mathf.Sin(bobT)) * 0.08f : 0f, 0f);
                return;
            }
            if (moving) { animT += Time.deltaTime * 8f; sr.sprite = ((int)animT % 2 == 0) ? walkA : walkB; }
            else sr.sprite = idle;
        }
    }
}
