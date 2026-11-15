using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Lính đánh thuê đi cùng người chơi (thuê ở phòng kín của MerShop, xem MercDefs / MercUi). Một người đi cùng mỗi lần (Town.MercActive),
    /// xuất hiện ở mọi khu vực ngoài căn cứ. Không bị người chơi đánh trúng; tự tìm kẻ địch gần nhất và đánh (gần hoặc xa), tự dùng kỹ năng.
    /// Máu chỉ giảm khi bị RimFoe / RimBolt đánh trúng hoặc chạm vào quái (sát thương chạm đơn giản hoá). Hết máu thì gục tới lần vào khu vực kế tiếp.
    /// </summary>
    public class Merc : MonoBehaviour
    {
        public static readonly List<Merc> Live = new List<Merc>();

        public MercDef def;
        public bool temporary, skillOnly;
        public float hp, maxHp, shield;
        public bool dead;

        int face = 1;
        float animT, atkCd, skillCd, scanT, hurtCd, flashT, life;
        bool busy, invuln, specialUsed;
        float frenzyT, frenzyCd;
        Hittable target;
        readonly List<Hittable> tagged = new List<Hittable>();
        SpriteRenderer sr;
        Transform vis;
        Sprite idle, walkA, walkB;
        WorldBar bar;
        bool ranged { get { return def.ranged; } }

        public Vector2 Center { get { return (Vector2)transform.position + new Vector2(0f, 0.8f); } }
        public Rect HurtRect { get { return Geo.Box(Center, new Vector2(0.8f, 1.5f)); } }
        float Dmg { get { return def.Damage * (frenzyT > 0f ? 1.6f : 1f); } }
        float Speed
        {
            get
            {
                var pl = Player.Instance;
                float baseSpd = pl != null ? pl.moveSpeed : 8.2f;
                return def.speedFrac * baseSpd * (frenzyT > 0f ? 1.4f : 1f);
            }
        }

        // ------------------------------------------------------------ tạo / quản lý
        public static void ResetStatics() { Live.Clear(); }

        public static Merc Spawn(MercDef def, Vector2 pos, bool temporary = false, bool skillOnly = false)
        {
            Art.Init();
            var go = new GameObject("Merc_" + def.id);
            go.transform.position = new Vector3(pos.x, 0f, 0f);
            var m = go.AddComponent<Merc>();
            m.def = def; m.temporary = temporary; m.skillOnly = skillOnly;
            m.maxHp = def.MaxHp; m.hp = m.maxHp;
            m.idle = TownArt.Human(def.look, 0); m.walkA = TownArt.Human(def.look, 1); m.walkB = TownArt.Human(def.look, 2);
            var v = new GameObject("Visual");
            v.transform.SetParent(go.transform, false);
            m.vis = v.transform;
            m.sr = v.AddComponent<SpriteRenderer>();
            m.sr.sharedMaterial = Art.DefMat;
            m.sr.sortingOrder = 21;
            m.sr.sprite = m.idle;
            v.transform.localScale = new Vector3(def.look.scale, def.look.scale, 1f);
            m.bar = new WorldBar(go.transform, new Vector2(0f, 1.75f * def.look.scale + 0.3f), 1.0f, 0.1f, Cfg.Cyan, 40);
            m.bar.Set(1f);
            Live.Add(m);
            Fx.Ring(m.Center, Cfg.A(Cfg.Cyan, 0.8f), 0.3f, 2.0f, 0.3f);
            Fx.Text(m.Center + new Vector2(0f, 1.4f), def.id, Cfg.Cyan, 0.9f);
            return m;
        }

        /// <summary>Gọi khi vào khu vực (ngoài căn cứ): dựng lính đang được chọn cạnh người chơi.</summary>
        public static void SpawnActive()
        {
            Live.RemoveAll(x => x == null);
            if (Town.MercActive < 0 || !MercDefs.Owned(Town.MercActive)) return;
            if (GameFlow.InBase) return;
            var pl = Player.Instance;
            if (pl == null || pl.Dead) return;
            for (int i = 0; i < Live.Count; i++) if (!Live[i].temporary) return;
            Spawn(MercDefs.All[Town.MercActive], pl.Position + new Vector2(-2.5f * pl.Facing, 0f));
        }

        /// <summary>Người chơi vào / ra toà nhà: ẩn lính khi ở trong nhà, đặt lại cạnh người chơi khi ra.</summary>
        public static void SetInterior(bool inside)
        {
            var pl = Player.Instance;
            for (int i = 0; i < Live.Count; i++)
            {
                var m = Live[i];
                if (m == null) continue;
                if (m.temporary) { if (inside) Destroy(m.gameObject); continue; }
                m.gameObject.SetActive(!inside);
                if (!inside && pl != null) m.transform.position = new Vector3(pl.Position.x - 2f * pl.Facing, 0f, 0f);
            }
        }

        public static Merc Nearest(Vector2 from, float maxDist)
        {
            Merc best = null;
            float bd = maxDist;
            for (int i = 0; i < Live.Count; i++)
            {
                var m = Live[i];
                if (m == null || m.dead || !m.isActiveAndEnabled) continue;
                float d = Mathf.Abs(m.transform.position.x - from.x);
                if (d < bd) { bd = d; best = m; }
            }
            return best;
        }

        public static bool IsHostile(Hittable h)
        {
            if (h == null || h.dead || !h.Targetable) return false;
            if (h is ArenaFighter) return false;
            var f = h as RimFoe;
            if (f != null) return f.aggro;
            return true;
        }

        void OnEnable() { busy = false; invuln = false; }
        void OnDestroy() { Live.Remove(this); }

        // ------------------------------------------------------------ nhận đòn / hồi
        public void Hurt(float dmg, Vector2 from)
        {
            if (dead || invuln || hurtCd > 0f) return;
            hurtCd = 0.25f;
            if (shield > 0f) { float ab = Mathf.Min(shield, dmg); shield -= ab; dmg -= ab; }
            if (dmg <= 0f) return;
            hp -= dmg;
            flashT = 0.08f;
            Fx.Text(Center + new Vector2(0f, 1.0f), Mathf.RoundToInt(dmg).ToString(), Cfg.Orange, 0.9f);
            if (hp <= 0f)
            {
                if (def.skill == MercSkill.Furra && !specialUsed && !temporary)
                {
                    specialUsed = true;
                    hp = maxHp * 0.15f;
                    StartCoroutine(CoSummonAll());
                    return;
                }
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (dead) return;
            hp += amount;
            if (hp > maxHp) { shield += hp - maxHp; hp = maxHp; }
        }

        void Die()
        {
            if (dead) return;
            dead = true;
            Fx.Burst(Center, Cfg.A(Cfg.Cyan, 0.9f), 10, 5f, 0.5f, 0.3f);
            Fx.Text(Center + new Vector2(0f, 1.2f), Cfg.L(def.id + " gục xuống", def.id + " is down"), Cfg.Red, 1.0f);
            Live.Remove(this);
            Destroy(gameObject);
        }

        void Vanish()
        {
            if (dead) return;
            dead = true;
            Fx.Ring(Center, Cfg.A(Cfg.White, 0.8f), 0.3f, 2.2f, 0.3f);
            Fx.Burst(Center, Cfg.A(Cfg.Cyan, 0.9f), 8, 4f, 0.4f, 0.3f);
            Live.Remove(this);
            Destroy(gameObject);
        }

        // ------------------------------------------------------------ vòng lặp
        void Update()
        {
            if (dead) return;
            float dt = Time.deltaTime;
            bar.Set(maxHp > 0f ? hp / maxHp : 0f);
            if (flashT > 0f) flashT -= dt;
            sr.color = flashT > 0f ? Color.white : frenzyT > 0f ? new Color(1f, 0.6f, 0.6f) : Color.white;
            if (!GameManager.Active || BaseUi.IsOpen || Interior.Busy) return;
            var pl = Player.Instance;
            if (pl == null || pl.Dead) { Anim(false); return; }

            atkCd -= dt; skillCd -= dt; hurtCd -= dt; frenzyCd -= dt;
            if (temporary) { life += dt; if (life > 34f && !busy) { Vanish(); return; } }
            if (frenzyT > 0f)
            {
                frenzyT -= dt;
                Heal(maxHp * 0.05f * dt);
                CheckKills();
            }
            ContactDamage();

            if (busy) return;

            // dọn danh sách đã đánh
            scanT -= dt;
            if (scanT <= 0f) { scanT = 0.2f; target = FindTarget(); if (temporary && target == null && life > 6f && !skillOnly && NoHostilesNear()) { Vanish(); return; } }
            if (target != null && !IsHostile(target)) target = null;

            if (TrySkill(pl)) return;

            if (target != null)
            {
                float dx = target.transform.position.x - transform.position.x;
                float dist = Mathf.Abs(dx);
                if (Mathf.Abs(dx) > 0.05f) face = dx > 0f ? 1 : -1;
                bool useRanged = ranged || (def.both && dist > 3.2f);
                if (useRanged)
                {
                    float keep = def.skill == MercSkill.Furra ? 6f : 6.5f;
                    if (dist > keep + 1.5f) { Move(face * Speed * dt); Anim(true); }
                    else if (dist < keep - 2f) { Move(-face * Speed * 0.7f * dt); Anim(true); }
                    else Anim(false);
                    if (atkCd <= 0f && dist < 13f) Shoot(target);
                }
                else
                {
                    if (dist > 1.7f) { Move(face * Speed * dt); Anim(true); }
                    else
                    {
                        Anim(false);
                        if (atkCd <= 0f) Strike(target);
                    }
                }
                return;
            }

            // đi theo người chơi
            float want = pl.Position.x - pl.Facing * 2.3f;
            float fdx = want - transform.position.x;
            if (Mathf.Abs(pl.Position.x - transform.position.x) > 17f)
            {
                transform.position = new Vector3(pl.Position.x - 3f * pl.Facing, 0f, 0f);
                Fx.Ring(Center, Cfg.A(Cfg.Cyan, 0.7f), 0.3f, 1.8f, 0.25f);
            }
            else if (Mathf.Abs(fdx) > 0.8f) { face = fdx > 0f ? 1 : -1; Move(Mathf.Sign(fdx) * Mathf.Min(Mathf.Abs(fdx) * 4f, Speed) * dt); Anim(true); }
            else { face = pl.Facing; Anim(false); }
        }

        Hittable FindTarget()
        {
            Hittable best = null;
            float bd = 13f;
            var all = Hittable.All;
            for (int i = 0; i < all.Count; i++)
            {
                var h = all[i];
                if (!IsHostile(h)) continue;
                float dx = Mathf.Abs(h.transform.position.x - transform.position.x);
                float dy = Mathf.Abs(h.Center.y - Center.y);
                if (dy > 7f) continue;
                if (dx < bd) { bd = dx; best = h; }
            }
            return best;
        }

        bool NoHostilesNear()
        {
            return FindTarget() == null;
        }

        int CountHostiles(float range)
        {
            int n = 0;
            var all = Hittable.All;
            for (int i = 0; i < all.Count; i++)
            {
                var h = all[i];
                if (IsHostile(h) && Mathf.Abs(h.transform.position.x - transform.position.x) < range) n++;
            }
            return n;
        }

        void ContactDamage()
        {
            if (hurtCd > 0f || invuln) return;
            for (int i = 0; i < Enemy.Live.Count; i++)
            {
                var e = Enemy.Live[i];
                if (e == null || e.dead) continue;
                if (HurtRect.Overlaps(e.HitRect))
                {
                    int lv = e.power != null ? e.power.level : 1;
                    Hurt(5f * PowerRules.DamageMul(lv), e.Center);
                    hurtCd = 0.8f;
                    break;
                }
            }
        }

        void Move(float dx)
        {
            var p = transform.position;
            p.x = Mathf.Clamp(p.x + dx, Cfg.LevelMinX + 0.5f, Cfg.LevelMaxX - 0.5f);
            transform.position = p;
        }

        void Anim(bool moving)
        {
            float s = def.look.scale;
            vis.localScale = new Vector3(face * s, s, 1f);
            if (moving) { animT += Time.deltaTime * 8f; sr.sprite = ((int)animT % 2 == 0) ? walkA : walkB; }
            else sr.sprite = idle;
        }

        // ------------------------------------------------------------ đánh thường
        void Strike(Hittable t)
        {
            atkCd = def.skill == MercSkill.Furra ? 0.5f : 0.75f;
            Vector2 dir = new Vector2(face, 0.2f);
            Sfx.PlayAt("slash", transform.position, 0.6f, 1.1f);
            Fx.Sparks(Center + new Vector2(face * 1.1f, 0f), dir, Cfg.Cyan, 6, 9f, 40f);
            t.TakeHit(Dmg, dir, 8f, true);
            Tag(t);
        }

        void Shoot(Hittable t)
        {
            atkCd = def.skill == MercSkill.Connie ? 1.6f : def.skill == MercSkill.Furra ? 0.7f : 1.0f;
            Vector2 from = Center + new Vector2(face * 0.8f, 0.1f);
            Vector2 dir = (Vector2)t.Center - from;
            bool cannon = def.skill == MercSkill.Connie;
            MercBolt.Fire(this, from, dir, cannon ? 17f : 23f, Dmg, cannon ? 2.3f : 0f, cannon ? Cfg.Orange : Cfg.Cyan);
            Sfx.PlayAt(cannon ? "slam" : "bolt", transform.position, 0.6f, cannon ? 0.9f : 1.3f);
        }

        /// <summary>Ghi nhớ mục tiêu đã đánh (để Furra tính hạ / hỗ trợ hạ).</summary>
        public void Tag(Hittable t)
        {
            if (t != null && !tagged.Contains(t)) tagged.Add(t);
        }

        void CheckKills()
        {
            for (int i = tagged.Count - 1; i >= 0; i--)
            {
                if (tagged[i] == null || tagged[i].dead)
                {
                    tagged.RemoveAt(i);
                    if (frenzyT > 0f) { frenzyT = 15f; Fx.Text(Center + new Vector2(0f, 1.6f), Cfg.L("ĐIÊN CUỒNG!", "FRENZY!"), Cfg.Red, 1.1f); }
                }
            }
        }

        // ------------------------------------------------------------ kỹ năng
        bool TrySkill(Player pl)
        {
            if (def.skill == MercSkill.None || skillOnly) return false;
            switch (def.skill)
            {
                case MercSkill.Charge:
                    if (skillCd <= 0f && target != null)
                    {
                        float d = Mathf.Abs(target.transform.position.x - transform.position.x);
                        if (d > 2.5f && d < 11f) { StartCoroutine(CoCharge(target.transform.position.x >= transform.position.x ? 1 : -1)); return true; }
                    }
                    break;
                case MercSkill.Kaiz:
                    if (skillCd <= 0f && (pl.hp < pl.maxHp * 0.65f || pl.Stunned) && CountHostiles(14f) > 0) { StartCoroutine(CoKaiz()); return true; }
                    break;
                case MercSkill.Connie:
                    if (skillCd <= 0f && CountHostiles(15f) >= 2) { StartCoroutine(CoConnie()); return true; }
                    break;
                case MercSkill.Furra:
                    if (skillCd <= 0f && CountHostiles(14f) >= 3) { StartCoroutine(CoExecute()); return true; }
                    if (frenzyT <= 0f && frenzyCd <= 0f && CountHostiles(12f) >= 1)
                    {
                        frenzyT = 15f; frenzyCd = 32f;
                        Fx.Ring(Center, Cfg.A(Cfg.Red, 0.9f), 0.4f, 3.5f, 0.4f);
                        Fx.Text(Center + new Vector2(0f, 1.6f), Cfg.L("ĐIÊN CUỒNG!", "FRENZY!"), Cfg.Red, 1.2f);
                        Sfx.PlayAt("charge", transform.position, 0.8f, 0.8f);
                    }
                    break;
            }
            return false;
        }

        /// <summary>War-07: lao đi ~7 m đẩy enemy theo, choáng + sát thương liên tục, cuối đường đập đất hất tung.</summary>
        IEnumerator CoCharge(int dir)
        {
            busy = true; invuln = true; face = dir;
            Anim(true);
            Sfx.PlayAt("dash", transform.position, 0.9f, 0.7f);
            var carried = new List<Hittable>();
            float dist = 0f, tick = 0f;
            while (dist < 7f)
            {
                float dt = Time.deltaTime;
                float step = 17f * dt;
                dist += step;
                Move(dir * step);
                float fx = transform.position.x + dir * 1.3f;
                var all = Hittable.All;
                for (int i = 0; i < all.Count; i++)
                {
                    var h = all[i];
                    if (!IsHostile(h) || carried.Contains(h)) continue;
                    if (Mathf.Abs(h.transform.position.x - fx) < 1.7f && Mathf.Abs(h.Center.y - Center.y) < 3f) carried.Add(h);
                }
                tick -= dt;
                bool doTick = tick <= 0f;
                if (doTick) tick = 0.15f;
                for (int i = carried.Count - 1; i >= 0; i--)
                {
                    var h = carried[i];
                    if (h == null || h.dead) { carried.RemoveAt(i); continue; }
                    var p = h.transform.position; p.x = fx; h.transform.position = p;
                    StunTarget(h, 0.3f);
                    if (doTick) { h.TakeHit(Dmg * 0.35f, new Vector2(dir, 0.1f), 2f, true); Tag(h); }
                }
                Fx.Emit(Fx.S_Glow, Center, Vector2.zero, new Vector2(1.0f, 1.0f), Vector2.zero, Cfg.A(Cfg.Cyan, 0.5f), Cfg.A(Cfg.Cyan, 0f), 0.25f);
                Anim(true);
                yield return null;
            }
            // đập mạnh xuống đất, hất tung
            Fx.Explosion(Center + new Vector2(dir * 1.3f, -0.4f), Cfg.Cyan, true);
            Fx.Shake(0.5f);
            Sfx.PlayAt("slam", transform.position, 1f, 0.8f);
            for (int i = 0; i < carried.Count; i++)
            {
                var h = carried[i];
                if (h == null || h.dead) continue;
                h.TakeHit(Dmg * 1.6f, new Vector2(dir, 1f), 6f, true);
                Tag(h);
                if (h == null || h.dead) continue;
                StunTarget(h, 1.0f);
                var rb = h.GetComponent<Rigidbody2D>();
                if (rb != null) rb.SetVel(new Vector2(dir * 3f, 14f));
            }
            skillCd = 12f; atkCd = 0.4f;
            busy = false; invuln = false;
        }

        /// <summary>Kaiz: lao tới người chơi, hồi 20% máu đã mất, giáp = 50% máu đã mất của bản thân, đẩy lùi enemy quanh người chơi, giải trừ khống chế.</summary>
        IEnumerator CoKaiz()
        {
            busy = true; invuln = true;
            var pl = Player.Instance;
            Sfx.PlayAt("dash", transform.position, 0.9f, 0.9f);
            float t = 0f;
            while (pl != null && !pl.Dead && Mathf.Abs(pl.Position.x - transform.position.x) > 1.1f && t < 1.4f)
            {
                t += Time.deltaTime;
                float dx = pl.Position.x - transform.position.x;
                face = dx > 0f ? 1 : -1;
                Move(face * 24f * Time.deltaTime);
                Anim(true);
                Fx.Emit(Fx.S_Glow, Center, Vector2.zero, new Vector2(1.0f, 1.0f), Vector2.zero, Cfg.A(Cfg.Yellow, 0.5f), Cfg.A(Cfg.Yellow, 0f), 0.25f);
                yield return null;
            }
            if (pl != null && !pl.Dead)
            {
                float lost = Mathf.Max(0f, pl.maxHp - pl.hp);
                float heal = lost * 0.2f;
                pl.hp = Mathf.Min(pl.maxHp, pl.hp + heal);
                float myLost = Mathf.Max(0f, maxHp - hp);
                shield += myLost * 0.5f;
                pl.ClearControl();
                Fx.Text(pl.Center + new Vector2(0f, 1.6f), "+" + Mathf.RoundToInt(heal), Cfg.Lime, 1.2f);
                Fx.Ring(pl.Center, Cfg.A(Cfg.Yellow, 0.9f), 0.4f, 5f, 0.4f);
                Fx.Shake(0.3f);
                Sfx.PlayAt("heal", pl.Position, 0.8f, 1f);
                var all = Hittable.All;
                for (int i = 0; i < all.Count; i++)
                {
                    var h = all[i];
                    if (!IsHostile(h)) continue;
                    float dx = h.transform.position.x - pl.Position.x;
                    if (Mathf.Abs(dx) > 6f) continue;
                    float dirx = dx >= 0f ? 1f : -1f;
                    var f = h as RimFoe;
                    if (f != null) f.Push(dirx * 14f);
                    var rb = h.GetComponent<Rigidbody2D>();
                    if (rb != null) rb.SetVel(new Vector2(dirx * 14f, 4f));
                }
            }
            skillCd = 22f;
            busy = false; invuln = false;
        }

        /// <summary>Connie: gọi 2 máy bay thả bom từ 2 hướng ngược nhau.</summary>
        IEnumerator CoConnie()
        {
            busy = true;
            float cx = target != null ? target.transform.position.x : transform.position.x + face * 6f;
            Fx.Text(Center + new Vector2(0f, 1.6f), Cfg.L("OANH TẠC!", "AIR STRIKE!"), Cfg.Orange, 1.2f);
            Sfx.PlayAt("warn", transform.position, 0.8f, 1.2f);
            float bombDmg = def.Damage * 5f;
            float radius = 2.3f * 3f * 0.5f + 0.2f;      // vụ nổ đòn thường có bán kính ~2,3 (đường kính) => bom to hơn 3 lần
            MercPlane.Spawn(cx - 19f, 1, 8.4f, cx - 13f, cx + 13f, bombDmg, radius);
            MercPlane.Spawn(cx + 19f, -1, 9.4f, cx - 13f, cx + 13f, bombDmg, radius);
            yield return new WaitForSeconds(0.5f);
            skillCd = 16f; atkCd = 0.8f;
            busy = false;
        }

        /// <summary>Furra kỹ năng 1: kết liễu bậc B trở xuống; bậc A giảm 50% máu + sát thương 7s; bậc S trở lên choáng 3s.</summary>
        IEnumerator CoExecute()
        {
            busy = true; invuln = true;
            Fx.Ring(Center, Cfg.A(Cfg.Red, 0.9f), 0.5f, 9f, 0.6f);
            Fx.Shake(0.5f);
            Sfx.PlayAt("shock", transform.position, 1f, 0.6f);
            yield return new WaitForSeconds(0.35f);
            var list = new List<Hittable>(Hittable.All);
            for (int i = 0; i < list.Count; i++)
            {
                var h = list[i];
                if (!IsHostile(h)) continue;
                if (Mathf.Abs(h.transform.position.x - transform.position.x) > 15f) continue;
                Rank rk = Rank.S;
                var e = h as Enemy;
                var f = h as RimFoe;
                if (e != null && e.power != null) rk = e.power.rank;
                else if (f != null) rk = f.rank;
                if (rk <= Rank.B) { h.TakeHit(9999999f, new Vector2(face, 0.5f), 4f, true); Tag(h); }
                else if (rk == Rank.A)
                {
                    if (e != null) e.Weaken(7f, 0.5f);
                    else if (f != null) { f.Weaken(7f); f.TakeHit(f.hp * 0.5f, new Vector2(face, 0.2f), 0f, true); }
                    Tag(h);
                }
                else StunTarget(h, 3f);
            }
            skillCd = 28f;
            busy = false; invuln = false;
        }

        /// <summary>Furra kỹ năng 3 (một lần khi hết máu): triệu hồi mọi lính đánh thuê trong shop.</summary>
        IEnumerator CoSummonAll()
        {
            busy = true; invuln = true;
            Fx.Ring(Center, Cfg.A(Cfg.Yellow, 0.9f), 0.5f, 10f, 0.7f);
            Fx.Text(Center + new Vector2(0f, 2.0f), Cfg.L("TRIỆU HỒI!", "SUMMON!"), Cfg.Yellow, 1.4f);
            Sfx.PlayAt("victory", transform.position, 0.9f, 0.8f);
            yield return new WaitForSeconds(0.4f);
            int n = 0;
            for (int i = 0; i < MercDefs.All.Length; i++)
            {
                var d = MercDefs.All[i];
                if (d.skill == MercSkill.Furra) continue;
                bool only = d.skill == MercSkill.Kaiz || d.skill == MercSkill.Connie;
                float off = (n % 2 == 0 ? -1f : 1f) * (1.5f + (n / 2) * 1.1f);
                n++;
                var m = Spawn(d, new Vector2(transform.position.x + off, 0f), true, only);
                if (only) m.StartCoroutine(m.CoSkillOnly());
                yield return new WaitForSeconds(0.12f);
            }
            skillCd = Mathf.Max(skillCd, 3f);
            busy = false; invuln = false;
        }

        /// <summary>Kaiz / Connie được triệu hồi: dùng skill một lần rồi biến mất.</summary>
        IEnumerator CoSkillOnly()
        {
            yield return new WaitForSeconds(0.5f);
            if (def.skill == MercSkill.Kaiz) yield return CoKaiz();
            else if (def.skill == MercSkill.Connie) { target = FindTarget(); yield return CoConnie(); yield return new WaitForSeconds(3.2f); }
            yield return new WaitForSeconds(0.4f);
            Vanish();
        }

        static void StunTarget(Hittable h, float dur)
        {
            var e = h as Enemy;
            if (e != null) { e.Freeze(dur); return; }
            var f = h as RimFoe;
            if (f != null) f.Stun(dur);
        }
    }
}
