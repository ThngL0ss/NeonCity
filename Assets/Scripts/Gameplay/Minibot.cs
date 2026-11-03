using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>Mục tiêu mà quái / bẫy có thể tấn công: người chơi hoặc một minibot.</summary>
    public struct FoeTarget
    {
        public bool Valid;
        public Player player;
        public Minibot bot;
        public Vector2 Pos;         // vị trí chân
        public Vector2 Center;
        public Rect HurtRect;

        public float Hurt(float dmg, Vector2 from, float knock)
        {
            if (player != null) return player.Hurt(dmg, from, knock);
            if (bot != null) return bot.Hurt(dmg, from, knock);
            return 0f;
        }
    }

    /// <summary>
    /// Minibot: quái được người chơi chế tạo, triệu hồi ra để cùng chiến đấu.
    /// Hình ảnh, chỉ số và kiểu đánh lấy từ Prefab quái gốc (Walker = lao chém, Flyer = bay + bắn). Máu và sát thương nhân theo cấp độ.
    /// Quái và bẫy tấn công minibot giống như tấn công người chơi. Minibot không bao giờ bị đòn của người chơi đánh trúng.
    /// </summary>
    public class Minibot : MonoBehaviour
    {
        public static readonly List<Minibot> Active = new List<Minibot>();

        public MinibotData data;
        public bool dead;
        public float hp, maxHp;

        enum S { Move, Windup, Lunge, Recover, Stagger }

        CraftEntry entry;
        bool flying;
        Rigidbody2D rb;
        Transform vis;
        SpriteAnimator anim;
        Flasher flasher;
        Vector2 hitSize = new Vector2(1f, 0.95f), hitOffset = new Vector2(0f, 0.48f);

        // chỉ số lấy từ Prefab quái gốc
        float moveSpeed = 3.7f, lungeDamage = 13f, windupRange = 4.3f, windupTime = 0.5f, lungeSpeed = 13f, lungeTime = 0.28f;
        float recoverTime = 0.75f, attackCooldown = 0.9f, lungeKnockback = 8f, knockbackMul = 0.8f;
        Vector2 lungeBox = new Vector2(0.9f, 0.9f);
        float boltDamage = 9f, boltSpeed = 9.5f, fireInterval = 2.7f, chargeTime = 0.55f, fireRange = 14f, minHeight = 1.8f;

        S st = S.Move;
        float stT, atkCd = 0.5f, invT, flashT, fireT = 1f, chargeT, sideT, bodyT;
        int face = 1, side = 1;
        bool charging;
        Vector2 vel, kv;
        readonly HashSet<Hittable> lungeHits = new HashSet<Hittable>();

        const float TeleportDist = 24f;

        public Vector2 Center { get { return (Vector2)transform.position + hitOffset; } }
        public Rect HurtRect { get { return Geo.Box(Center, hitSize); } }
        public Color Accent { get { return entry != null ? entry.partColor : Cfg.Cyan; } }

        void OnEnable() { if (!Active.Contains(this)) Active.Add(this); }
        void OnDisable() { Active.Remove(this); }

        // ------------------------------------------------------------ tạo / thu hồi
        public static Minibot Spawn(MinibotData d, Vector2 pos)
        {
            var e = d.Entry;
            if (e == null || e.enemyPrefab == null) return null;
            var go = new GameObject("Minibot_" + d.typeId);
            var parent = Level.Sub("Minibots");
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = e.enemyPrefab.transform.localScale;
            go.layer = Cfg.LayerEnemy;
            var m = go.AddComponent<Minibot>();
            m.Init(d, e);
            Fx.Ring(m.Center, Cfg.A(Cfg.Cyan, 0.9f), 0.4f, 3f, 0.4f);
            Fx.Burst(m.Center, Cfg.Cyan, 12, 6f, 0.4f, 0.3f);
            Sfx.PlayAt("teleport", pos, 0.7f, 1.3f);
            return m;
        }

        void Init(MinibotData d, CraftEntry e)
        {
            data = d;
            entry = e;
            var prefab = e.enemyPrefab;
            var en = prefab.GetComponent<Enemy>();
            var wk = prefab.GetComponent<Walker>();
            var fl = prefab.GetComponent<Flyer>();
            flying = fl != null && wk == null;
            if (en != null) { hitSize = en.hitSize; hitOffset = en.hitOffset; }

            if (wk != null)
            {
                moveSpeed = wk.moveSpeed; lungeDamage = wk.lungeDamage; windupRange = wk.windupRange; windupTime = wk.windupTime;
                lungeSpeed = wk.lungeSpeed; lungeTime = wk.lungeTime; recoverTime = wk.recoverTime; attackCooldown = wk.attackCooldown;
                lungeKnockback = wk.lungeKnockback; knockbackMul = wk.knockbackMul; lungeBox = wk.lungeBoxSize;
            }
            if (fl != null)
            {
                moveSpeed = fl.moveSpeed; boltDamage = fl.boltDamage; boltSpeed = fl.boltSpeed; fireInterval = fl.fireInterval;
                chargeTime = fl.chargeTime; fireRange = fl.fireRange; minHeight = fl.minHeight;
            }

            maxHp = d.MaxHp;
            hp = Mathf.Clamp(d.hp, 1f, maxHp);
            d.hp = hp;
            side = Random.value < 0.5f ? -1 : 1;
            bodyT = Random.value * 6f;

            if (!flying)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
                rb.freezeRotation = true;
                rb.gravityScale = wk != null ? wk.gravityScale : 4f;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
                var pc = prefab.GetComponent<BoxCollider2D>();
                var col = gameObject.AddComponent<BoxCollider2D>();
                col.size = pc != null ? pc.size : new Vector2(0.9f, 0.9f);
                col.offset = pc != null ? pc.offset : new Vector2(0f, 0.45f);
                if (pc != null) col.sharedMaterial = pc.sharedMaterial;
            }

            // hình ảnh: nhân bản object con "Visual" của Prefab quái gốc (chỉ hình, không mang theo script quái)
            var v = prefab.transform.Find("Visual");
            int order = 10;
            if (v != null)
            {
                var vgo = Instantiate(v.gameObject, transform);
                vgo.name = "Visual";
                vis = vgo.transform;
                anim = vgo.GetComponent<SpriteAnimator>();
                var srs = vgo.GetComponentsInChildren<SpriteRenderer>(true);
                for (int i = 0; i < srs.Length; i++)
                {
                    order = Mathf.Max(order, srs[i].sortingOrder);
                    if (srs[i].sharedMaterial != Art.AddMat) srs[i].color = Color.Lerp(srs[i].color, new Color(0.55f, 1f, 1f, srs[i].color.a), 0.4f);
                }
            }
            // vầng sáng xanh dưới chân để phân biệt đồng minh
            var g = new GameObject("AllyGlow");
            g.transform.SetParent(transform, false);
            g.transform.localPosition = new Vector3(0f, flying ? 0f : 0.08f, 0f);
            g.transform.localScale = new Vector3(1.8f, 0.6f, 1f);
            var gsr = g.AddComponent<SpriteRenderer>();
            gsr.sprite = Art.Glow;
            gsr.sharedMaterial = Art.AddMat;
            gsr.color = Cfg.A(Cfg.Cyan, 0.45f);
            gsr.sortingOrder = Mathf.Max(0, order - 1);

            flasher = new Flasher(gameObject);
        }

        /// <summary>Thu hồi về bảng chọn: giữ nguyên máu đang có.</summary>
        public void Recall()
        {
            if (dead) return;
            dead = true;
            if (data != null) data.hp = hp;
            Fx.Ring(Center, Cfg.A(Cfg.Cyan, 0.9f), 0.4f, 2.5f, 0.35f);
            Fx.Burst(Center, Cfg.Cyan, 10, 5f, 0.35f, 0.28f);
            Sfx.PlayAt("teleport", transform.position, 0.6f, 1.5f);
            Destroy(gameObject);
        }

        // ------------------------------------------------------------ nhận sát thương (từ quái, đạn, bẫy)
        public float Hurt(float dmg, Vector2 from, float knock)
        {
            if (dead || data == null || invT > 0f || !GameManager.Active) return 0f;
            hp -= dmg;
            data.hp = hp;
            invT = 0.5f;
            flashT = 0.09f;
            SkillSystem.NoteCombat();

            float dir = transform.position.x >= from.x ? 1f : -1f;
            if (flying) kv = new Vector2(dir * knock * 0.8f, 2f);
            else if (rb != null)
            {
                Vector2 v = rb.Vel();
                v.x = dir * knock * 0.7f;
                v.y = Mathf.Max(v.y, 3f);
                rb.SetVel(v);
                if (st != S.Lunge) { st = S.Stagger; stT = 0.25f; }
            }
            Sfx.PlayAt("hurt", transform.position, 0.55f, 1.25f);
            Fx.Sparks(Center, new Vector2(dir, 0.4f), Cfg.Cyan, 8, 10f, 60f);
            Fx.Text(Center + new Vector2(0f, 1f), "-" + Mathf.RoundToInt(dmg), Cfg.Orange, 1f);
            if (hp <= 0f) { hp = 0f; Die(); }
            return dmg;
        }

        void Die()
        {
            if (dead) return;
            dead = true;
            var c = SkillSystem.Config;
            Fx.Explosion(Center, Cfg.Cyan, false);
            Fx.Sparks(Center, Vector2.up, Cfg.White, 10, 12f, 60f);
            Sfx.PlayAt("enemyDie", transform.position, 0.8f, 1.25f);
            Fx.Text(Center + new Vector2(0f, 1.6f), Cfg.L("MINIBOT HỎNG", "MINIBOT LOST"), Cfg.Red, 1.2f);
            int n = Mathf.Max(0, Mathf.RoundToInt(c.deathPartsBase + data.level * c.deathPartsPerLevel));
            if (n > 0 && entry != null) PartDrop.Drop(Center, data.typeId, n, entry.partColor);
            SkillSystem.RemoveBot(data);
            Destroy(gameObject);
        }

        // ------------------------------------------------------------ độ trưởng thành
        public void CreditAssist()
        {
            if (dead || data == null) return;
            if (SkillSystem.AddMaturity(data, SkillSystem.Config.maturityAssist)) ReadyToLevel();
        }

        void CreditKill()
        {
            if (dead || data == null) return;
            // phần "hỗ trợ" đã được cộng khi quái chết; ở đây chỉ cộng phần chênh lệch cho người kết liễu
            float extra = SkillSystem.Config.maturityKill - SkillSystem.Config.maturityAssist;
            if (extra > 0f && SkillSystem.AddMaturity(data, extra)) ReadyToLevel();
        }

        void ReadyToLevel()
        {
            Fx.Text(Center + new Vector2(0f, 1.8f), Cfg.L("ĐỦ ĐỘ TRƯỞNG THÀNH!", "MATURITY FULL!"), Cfg.Lime, 1.1f);
            Sfx.PlayAt("pickup", transform.position, 0.6f, 1.5f);
        }

        // ------------------------------------------------------------ AI
        void Update()
        {
            if (dead || data == null || !GameManager.Active) return;
            float dt = Time.deltaTime;
            invT -= dt;
            atkCd -= dt;
            stT -= dt;
            var pl = Player.Instance;
            bool playerOk = pl != null && !pl.Dead;

            if (playerOk && (Mathf.Abs(pl.Position.x - transform.position.x) > TeleportDist || transform.position.y < -4f)) TeleportToPlayer(pl);

            Hittable tgt = FindTarget(pl);
            if (flying) UpdateFlyer(dt, pl, tgt);
            else UpdateWalker(dt, pl, tgt);
            UpdateVisual(dt);
            data.hp = hp;
        }

        void TeleportToPlayer(Player pl)
        {
            Fx.Burst(Center, Cfg.Cyan, 8, 5f, 0.3f, 0.25f);
            float s = Random.value < 0.5f ? -1f : 1f;
            transform.position = new Vector3(pl.Position.x + s * 1.5f, pl.Position.y + 0.3f, 0f);
            if (rb != null) rb.SetVel(Vector2.zero);
            vel = Vector2.zero; kv = Vector2.zero;
            Fx.Ring(Center, Cfg.A(Cfg.Cyan, 0.9f), 0.3f, 2.2f, 0.3f);
            Sfx.PlayAt("teleport", transform.position, 0.5f, 1.4f);
        }

        /// <summary>Địch gần nhất trong tầm (bỏ qua địch ở quá xa người chơi để minibot không chạy lạc).</summary>
        Hittable FindTarget(Player pl)
        {
            var c = SkillSystem.Config;
            float range = Mathf.Max(4f, c.engageRange);
            bool pOk = pl != null && !pl.Dead;
            Vector2 me = Center;
            Hittable best = null;
            float bd = range;

            var list = Enemy.Live;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e == null || e.dead) continue;
                if (pOk && Mathf.Abs(e.Center.x - pl.Position.x) > range * 1.25f) continue;
                float d = Vector2.Distance(me, e.Center);
                if (d < bd) { bd = d; best = e; }
            }
            var b = Boss.Instance;
            if (b != null && b.Targetable)
            {
                float d = Vector2.Distance(me, b.Center);
                if (d < bd) { bd = d; best = b; }
            }
            if (c.attackTowers)
            {
                var tl = Tower.Towers;
                for (int i = 0; i < tl.Count; i++)
                {
                    var t = tl[i];
                    if (t == null || t.dead || !t.Targetable) continue;
                    float d = Vector2.Distance(me, t.Center);
                    if (d < bd) { bd = d; best = t; }
                }
            }
            return best;
        }

        bool Grounded(Vector2 pos, Vector2 v)
        {
            if (v.y > 0.5f) return false;
            return Physics2D.Raycast(pos + new Vector2(0f, 0.1f), Vector2.down, 0.3f, Cfg.GroundMask).collider != null;
        }

        // ---- kiểu Walker: bám theo người chơi, thấy địch thì gồng rồi lao chém
        void UpdateWalker(float dt, Player pl, Hittable tgt)
        {
            Vector2 v = rb.Vel();
            Vector2 pos = transform.position;
            bool pOk = pl != null && !pl.Dead;
            float followDist = Mathf.Max(1.5f, SkillSystem.Config.followDistance);

            switch (st)
            {
                case S.Move:
                    float want = 0f;
                    if (tgt != null)
                    {
                        float dx = tgt.Center.x - Center.x;
                        float adx = Mathf.Abs(dx);
                        face = dx >= 0f ? 1 : -1;
                        want = adx > 1.1f ? face * moveSpeed * 1.15f : 0f;
                        bool sameLevel = Mathf.Abs(tgt.Center.y - Center.y) < 2.4f;
                        if (adx < windupRange && atkCd <= 0f && sameLevel)
                        {
                            st = S.Windup;
                            stT = windupTime;
                            Sfx.PlayAt("warn", pos, 0.4f, 1.5f);
                        }
                    }
                    else if (pOk)
                    {
                        float dx = pl.Position.x - pos.x;
                        float adx = Mathf.Abs(dx);
                        if (adx > followDist)
                        {
                            face = dx >= 0f ? 1 : -1;
                            want = face * moveSpeed * (adx > followDist * 3f ? 1.6f : 1.15f);
                        }
                        // người chơi đứng trên bục cao ngay cạnh: nhảy theo
                        if (pl.Position.y - pos.y > 1.6f && adx < 3f && Grounded(pos, v)) v.y = 11f;
                    }
                    v.x = Mathf.MoveTowards(v.x, want, 45f * dt);
                    break;

                case S.Windup:
                    v.x = 0f;
                    if (stT <= 0f)
                    {
                        st = S.Lunge;
                        stT = lungeTime;
                        lungeHits.Clear();
                        Sfx.PlayAt("dash", pos, 0.5f, 1.3f);
                        Fx.Sparks(Center, new Vector2(-face, 0.1f), Cfg.Cyan, 6, 9f, 25f, 0.25f);
                    }
                    break;

                case S.Lunge:
                    v.x = face * lungeSpeed;
                    if (Random.value < 0.6f) Fx.Trail(Center, Cfg.A(Cfg.Cyan, 0.8f), 0.9f, 0.2f);
                    LungeHit();
                    if (stT <= 0f)
                    {
                        st = S.Recover;
                        stT = recoverTime;
                        atkCd = attackCooldown;
                    }
                    break;

                case S.Recover:
                    v.x = Mathf.MoveTowards(v.x, 0f, 60f * dt);
                    if (stT <= 0f) st = S.Move;
                    break;

                case S.Stagger:
                    v.x = Mathf.MoveTowards(v.x, 0f, 28f * dt);
                    if (stT <= 0f) st = S.Move;
                    break;
            }

            if (pos.x < Cfg.LevelMinX + 1f || pos.x > Cfg.LevelMaxX - 1f)
            {
                pos.x = Mathf.Clamp(pos.x, Cfg.LevelMinX + 1f, Cfg.LevelMaxX - 1f);
                transform.position = pos;
                v.x = 0f;
            }
            rb.SetVel(v);
        }

        void LungeHit()
        {
            Rect box = Geo.Box(Center, lungeBox);
            float dmg = lungeDamage * SkillSystem.DamageMul(data);
            Vector2 dir = new Vector2(face, 0.25f);

            var list = Enemy.Live;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e == null || e.dead || lungeHits.Contains(e) || !box.Overlaps(e.HitRect)) continue;
                lungeHits.Add(e);
                Strike(e, dmg, dir, 6f * knockbackMul + 2f, true);
            }
            var b = Boss.Instance;
            if (b != null && b.Targetable && !lungeHits.Contains(b) && box.Overlaps(b.HitRect))
            {
                lungeHits.Add(b);
                Strike(b, dmg, dir, 3f, true);
            }
            if (SkillSystem.Config.attackTowers)
            {
                var tl = Tower.Towers;
                for (int i = 0; i < tl.Count; i++)
                {
                    var t = tl[i];
                    if (t == null || t.dead || !t.Targetable || lungeHits.Contains(t) || !box.Overlaps(t.HitRect)) continue;
                    lungeHits.Add(t);
                    Strike(t, dmg, dir, 3f, true);
                }
            }
        }

        /// <summary>Gây sát thương lên một mục tiêu và ghi nhận công trạng (hỗ trợ / kết liễu) cho độ trưởng thành.</summary>
        public void Strike(Hittable h, float dmg, Vector2 dir, float knock, bool melee)
        {
            if (h == null) return;
            bool was = h.dead;
            var en = h as Enemy;
            if (en != null) en.NoteMinibotHit(this);
            h.TakeHit(dmg, dir.normalized, knock, melee);
            SkillSystem.NoteCombat();
            if (!was && h.dead) CreditKill();
        }

        /// <summary>Đạn của minibot trúng một mục tiêu (Projectile gọi).</summary>
        public void OnShotHit(Hittable h, bool wasDead)
        {
            SkillSystem.NoteCombat();
            var en = h as Enemy;
            if (en != null) en.NoteMinibotHit(this);
            if (!wasDead && h.dead) CreditKill();
        }

        // ---- kiểu Flyer: lơ lửng cạnh người chơi, nạp đạn rồi bắn địch
        void UpdateFlyer(float dt, Player pl, Hittable tgt)
        {
            Vector2 pos = transform.position;
            bool pOk = pl != null && !pl.Dead;
            bodyT += dt;
            sideT -= dt;
            fireT -= dt;
            if (sideT <= 0f) { side = Random.value < 0.5f ? -1 : 1; sideT = Random.Range(3f, 6f); }

            Vector2 anchor = pOk ? pl.Position : pos;
            float follow = Mathf.Max(1.5f, SkillSystem.Config.followDistance);
            Vector2 want = new Vector2(anchor.x + side * (follow + 0.8f), anchor.y + 2.3f + Mathf.Sin(bodyT * 2f) * 0.4f);
            Vector2 to = want - pos;
            Vector2 w = to.normalized * Mathf.Min(moveSpeed * 1.2f, to.magnitude * 2.2f);
            vel = Vector2.Lerp(vel, w, 1f - Mathf.Exp(-3f * dt));
            kv = Vector2.Lerp(kv, Vector2.zero, 1f - Mathf.Exp(-7f * dt));
            pos += (vel + kv) * dt;
            pos.y = Mathf.Max(pos.y, minHeight);
            pos.x = Mathf.Clamp(pos.x, Cfg.LevelMinX + 1f, Cfg.LevelMaxX - 1f);
            transform.position = pos;

            if (tgt != null) face = tgt.Center.x >= pos.x ? 1 : -1;
            else if (pOk) face = pl.Position.x >= pos.x ? 1 : -1;

            bool inRange = tgt != null && Mathf.Abs(tgt.Center.x - pos.x) < fireRange;
            if (!charging && fireT <= 0f && inRange)
            {
                charging = true;
                chargeT = chargeTime;
                Sfx.PlayAt("warn", pos, 0.35f, 1.7f);
            }
            if (charging)
            {
                chargeT -= dt;
                if (!inRange) charging = false;
                else if (chargeT <= 0f)
                {
                    charging = false;
                    fireT = fireInterval * Random.Range(0.85f, 1.2f);
                    Vector2 aim = tgt.Center - pos;
                    Projectile.AllyBolt(pos, aim, boltSpeed, boltDamage * SkillSystem.DamageMul(data), Cfg.Cyan, this);
                    Sfx.PlayAt("bolt", pos, 0.45f, 1.3f);
                }
            }
            if (Random.value < 0.2f) Fx.Trail(pos, Cfg.A(Cfg.Cyan, 0.5f), 0.4f, 0.2f);
        }

        void UpdateVisual(float dt)
        {
            if (flashT > 0f) flashT -= dt;
            if (vis != null)
            {
                if (flying)
                {
                    vis.localScale = new Vector3(face, 1f, 1f);
                    vis.localRotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(-vel.x * 2.5f, -12f, 12f));
                }
                else
                {
                    float sx = 1f, sy = 1f;
                    if (st == S.Windup) { sy = 0.9f; sx = 1.08f; }
                    vis.localScale = new Vector3(face * sx, sy, 1f);
                }
            }
            if (anim != null)
            {
                if (flying) anim.Play(charging ? "charge" : "fly");
                else anim.Play(st == S.Windup ? "windup" : (st == S.Lunge ? "lunge" : "walk"));
            }
            if (flasher != null)
            {
                float alpha = invT > 0f ? (Mathf.Sin(Time.time * 40f) > 0f ? 1f : 0.55f) : 1f;
                flasher.Apply(flashT > 0f, alpha);
            }
        }

        // ------------------------------------------------------------ dùng chung cho quái / bẫy / đạn
        /// <summary>Mục tiêu gần nhất (theo trục ngang) trong tầm maxDx: người chơi hoặc một minibot.</summary>
        public static FoeTarget PickTarget(Vector2 from, float maxDx = 1000f)
        {
            FoeTarget t = new FoeTarget();
            if (!GameManager.Active) return t;
            float best = maxDx;
            var pl = Player.Instance;
            if (pl != null && !pl.Dead)
            {
                float d = Mathf.Abs(pl.Position.x - from.x);
                if (d < best)
                {
                    best = d;
                    t.Valid = true; t.player = pl; t.bot = null;
                    t.Pos = pl.Position; t.Center = pl.Center; t.HurtRect = pl.HurtRect;
                }
            }
            for (int i = 0; i < Active.Count; i++)
            {
                var b = Active[i];
                if (b == null || b.dead) continue;
                float d = Mathf.Abs(b.transform.position.x - from.x);
                if (d < best)
                {
                    best = d;
                    t.Valid = true; t.player = null; t.bot = b;
                    t.Pos = b.transform.position; t.Center = b.Center; t.HurtRect = b.HurtRect;
                }
            }
            return t;
        }

        /// <summary>Gây sát thương lên mọi minibot nằm trong vùng chữ nhật (đòn quái, bẫy, boss).</summary>
        public static void HurtInRect(Rect area, float dmg, Vector2 from, float knock)
        {
            for (int i = 0; i < Active.Count; i++)
            {
                var b = Active[i];
                if (b == null || b.dead) continue;
                if (area.Overlaps(b.HurtRect)) b.Hurt(dmg, from, knock);
            }
        }

        public static void HurtInCircle(Vector2 c, float r, float dmg, Vector2 from, float knock)
        {
            for (int i = 0; i < Active.Count; i++)
            {
                var b = Active[i];
                if (b == null || b.dead) continue;
                if (Geo.CircleRect(c, r, b.HurtRect)) b.Hurt(dmg, from, knock);
            }
        }
    }
}
