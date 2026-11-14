using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Đấu sĩ ở ArenaCyber (AI đơn giản, đứng trên sàn, không có vật lý). Người chơi đánh trúng được như quái (Hittable).
    /// Có thể nhắm vào người chơi (foe = null) hoặc vào một đấu sĩ khác (khi người chơi chỉ ngồi xem và cược).
    ///  • Brute (đối thủ 1): chậm, sát thương vừa, máu vừa. Chiêu: lao ra sau lưng mục tiêu, 0,4 giây sau tung đòn gây choáng (có thể né).
    ///  • Chain (đối thủ 2): nhanh, sát thương vừa, máu thấp. Chiêu: ném xích; trúng thì choáng 0,3 giây rồi tự kéo mình tới và nổ (sát thương gấp 2).
    ///  • Cannon (đối thủ 3): nhanh, sát thương lớn, trâu máu, to gấp 1,5 lần. Chiêu: nạp 0,5 giây rồi bắn đạn lớn (1,2 lần cỡ người chơi, nhanh, sát thương gấp 2,5).
    /// Thông số gốc nằm trong Spawn(); powerMul nhân máu và (một phần) sát thương cho các trận "đấu sĩ" ngày một khó.
    /// </summary>
    public class ArenaFighter : Hittable
    {
        public enum Kind { Brute, Chain, Cannon }

        public Kind kind;
        public ArenaFighter foe;                    // null = nhắm vào người chơi
        public float minX, maxX;
        public float damage = 14f, speed = 3f, size = 1f;
        public Color tint = Color.white;
        public string tagText = "";
        public bool fighting;                       // false: đứng chờ, chưa bị đánh / chưa đánh
        public System.Action<ArenaFighter> onDied;

        enum S { Chase, Windup, Strike, Recover, DashBehind, Telegraph, ChainWind, ChainFly, Pull, CannonCharge }
        S st = S.Chase;
        float t, skillCd = 3f, kb, stunT, flashT, animT;
        int face = 1;
        float meleeReach = 1.8f, windup = 0.4f, recover = 0.7f;
        float behindX;
        Vector2 hookPos, hookDir;
        float hookTravel;
        Transform vis;
        SpriteRenderer sr, hookSr, lineSr;
        Sprite idle, walkA, walkB;
        WorldBar bar;
        Color accent = Cfg.Red;

        public override bool Targetable { get { return !dead && fighting; } }

        // ------------------------------------------------------------ tạo
        public static ArenaFighter Spawn(Transform parent, Kind kind, float x, float minX, float maxX, float powerMul, string tag, Color tint)
        {
            Art.Init();
            var go = new GameObject("Fighter_" + kind);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, 0f, 0f);
            var f = go.AddComponent<ArenaFighter>();
            f.kind = kind; f.minX = minX; f.maxX = maxX; f.tint = tint; f.tagText = tag;
            Look look;
            float hpBase;
            switch (kind)
            {
                case Kind.Brute: look = TownLooks.Fighter1; hpBase = 240f; f.damage = 14f; f.speed = 2.6f; f.size = 1.12f; f.meleeReach = 1.9f; f.windup = 0.5f; f.recover = 0.8f; f.accent = Cfg.Orange; break;
                case Kind.Chain: look = TownLooks.Fighter2; hpBase = 120f; f.damage = 14f; f.speed = 5.0f; f.size = 0.95f; f.meleeReach = 1.6f; f.windup = 0.22f; f.recover = 0.45f; f.accent = Cfg.Cyan; break;
                default: look = TownLooks.Fighter3; hpBase = 520f; f.damage = 24f; f.speed = 4.4f; f.size = 1.5f; f.meleeReach = 2.5f; f.windup = 0.45f; f.recover = 0.7f; f.accent = Cfg.Magenta; break;
            }
            powerMul = Mathf.Max(1f, powerMul);
            f.maxHp = hpBase * powerMul;
            f.hp = f.maxHp;
            f.damage *= 1f + 0.5f * (powerMul - 1f);
            f.hitSize = new Vector2(0.9f * f.size, 1.6f * f.size);
            f.hitOffset = new Vector2(0f, 0.8f * f.size);
            f.idle = TownArt.Human(look, 0); f.walkA = TownArt.Human(look, 1); f.walkB = TownArt.Human(look, 2);
            var v = new GameObject("Visual");
            v.transform.SetParent(go.transform, false);
            f.vis = v.transform;
            f.sr = v.AddComponent<SpriteRenderer>();
            f.sr.sharedMaterial = Art.DefMat;
            f.sr.sortingOrder = 20;
            f.sr.sprite = f.idle;
            v.transform.localScale = new Vector3(f.size, f.size, 1f);
            Color barCol = tag == "A" ? Cfg.Cyan : tag == "B" ? Cfg.Magenta : Cfg.Red;
            f.bar = new WorldBar(go.transform, new Vector2(0f, 1.75f * f.size + 0.35f), 1.3f * f.size, 0.14f, barCol, 40);
            if (!string.IsNullOrEmpty(tag))
                Art.Spr(go.transform, "Tag", TownArt.SignPlate(tag, 1, 0x0A0C1C, tag == "A" ? 0x35E8FF : 0xFF2FB5, tag == "A" ? 0x35E8FF : 0xFF2FB5),
                        new Vector2(0f, 1.75f * f.size + 0.95f), Vector2.one, Color.white, 41);
            return f;
        }

        // ------------------------------------------------------------ mục tiêu
        bool HasTarget { get { return foe != null ? !foe.dead : (Player.Instance != null && !Player.Instance.Dead); } }
        Vector2 TPos { get { return foe != null ? foe.Center : (Player.Instance != null ? Player.Instance.Center : Center); } }
        Rect TRect { get { return foe != null ? foe.HitRect : Player.Instance.HurtRect; } }

        void HurtTarget(float dmg, float knock, float stun)
        {
            if (foe != null)
            {
                if (!foe.dead) { foe.TakeHit(dmg, new Vector2(face, 0.2f), knock, true); if (stun > 0f) foe.Stun(stun); }
            }
            else if (Player.Instance != null)
            {
                float d = Player.Instance.Hurt(dmg, Center, knock);
                if (d > 0f && stun > 0f) Player.Instance.Stun(stun);
            }
        }

        public void Stun(float dur)
        {
            if (dead) return;
            stunT = Mathf.Max(stunT, dur);
            st = S.Recover; t = dur;
            CleanupChain();
            Fx.Text(Center + new Vector2(0f, 1.2f * size), Cfg.L("CHOÁNG!", "STUNNED!"), Cfg.Yellow, 1.0f);
        }

        // ------------------------------------------------------------ nhận đòn
        public override void TakeHit(float dmg, Vector2 dir, float knock, bool melee)
        {
            if (dead || !Targetable) return;
            hp -= dmg;
            flashT = 0.08f;
            Sfx.PlayAt("hit", transform.position, 0.8f, melee ? 1f : 1.3f);
            Fx.Sparks(Center, dir, accent, 7, 11f, 55f);
            Fx.Text(Center + new Vector2(0f, 0.9f * size), Mathf.RoundToInt(dmg).ToString(), Cfg.Yellow);
            kb += Mathf.Sign(dir.x == 0f ? 1f : dir.x) * knock * (kind == Kind.Cannon ? 0.25f : 0.6f) / size;
            if (hp <= 0f) Die(dir);
        }

        void Die(Vector2 dir)
        {
            if (dead) return;
            dead = true;
            Fx.Explosion(Center, accent, false);
            Fx.Sparks(Center, dir, Cfg.White, 10, 14f, 50f);
            Fx.Shake(0.15f);
            Sfx.PlayAt("enemyDie", transform.position, 0.9f);
            CleanupChain();
            if (onDied != null) onDied(this);
            Destroy(gameObject);
        }

        void OnDestroy() { CleanupChain(); }

        // ------------------------------------------------------------ vòng lặp
        void Update()
        {
            if (dead) return;
            bar.Set(maxHp > 0f ? hp / maxHp : 0f);
            if (flashT > 0f) flashT -= Time.deltaTime;
            Color c = tint;
            if (flashT > 0f) c = Color.white;
            else if (st == S.Windup || st == S.Telegraph || st == S.CannonCharge || st == S.ChainWind)
                c = Color.Lerp(tint, Cfg.Yellow, 0.5f + 0.5f * Mathf.Sin(Time.time * 40f));
            sr.color = c;

            if (BaseUi.IsOpen || !GameManager.Active) return;
            if (!fighting || !HasTarget) { Anim(false); return; }
            float dt = Time.deltaTime;

            if (stunT > 0f)
            {
                stunT -= dt;
                if (stunT <= 0f) { st = S.Chase; skillCd = Mathf.Max(skillCd, 0.5f); }
                Anim(false);
                return;
            }
            if (Mathf.Abs(kb) > 0.05f) { Move(kb * dt); kb = Mathf.MoveTowards(kb, 0f, 26f * dt); }

            Vector2 tp = TPos;
            float dx = tp.x - transform.position.x, dist = Mathf.Abs(dx);

            switch (st)
            {
                case S.Chase:
                    Face(dx);
                    skillCd -= dt;
                    if (TrySkill(tp, dist)) break;
                    if (dist > meleeReach * 0.8f) { Move(face * speed * dt); Anim(true); }
                    else { st = S.Windup; t = windup; Anim(false); }
                    break;
                case S.Windup:
                    t -= dt; Anim(false);
                    if (t <= 0f) { Strike(meleeReach, damage, 8f, 0f); st = S.Recover; t = recover; }
                    break;
                case S.Recover:
                    t -= dt; Anim(false);
                    if (t <= 0f) st = S.Chase;
                    break;
                case S.DashBehind:
                    t -= dt;
                    Move(Mathf.Sign(behindX - transform.position.x) * 28f * dt);
                    Fx.Emit(Fx.S_Glow, Center, Vector2.zero, new Vector2(0.9f, 0.9f), Vector2.zero, Cfg.A(accent, 0.5f), Cfg.A(accent, 0f), 0.25f);
                    if (Mathf.Abs(behindX - transform.position.x) < 0.35f || t <= 0f)
                    {
                        Face(TPos.x - transform.position.x);       // quay lại đối mặt, rồi báo hiệu 0,4 giây
                        st = S.Telegraph; t = 0.4f;
                        Fx.Text(Center + new Vector2(0f, 1.4f * size), "!", Cfg.Yellow, 1.4f);
                        Sfx.PlayAt("warn", transform.position, 0.7f, 1.6f);
                    }
                    break;
                case S.Telegraph:
                    t -= dt; Anim(false);
                    if (t <= 0f)
                    {
                        Strike(2.3f, damage, 9f, 0.8f);
                        Fx.Ring(Center + new Vector2(face * 1.0f, 0f), Cfg.Yellow, 0.5f, 3.2f, 0.25f);
                        st = S.Recover; t = 1.0f;
                    }
                    break;
                case S.ChainWind:
                    t -= dt; Anim(false);
                    Face(dx);
                    if (t <= 0f)
                    {
                        hookPos = Center; hookDir = new Vector2(face, 0f); hookTravel = 0f;
                        MakeChainVisual();
                        Sfx.PlayAt("throw", transform.position, 0.8f, 0.8f);
                        st = S.ChainFly;
                    }
                    break;
                case S.ChainFly:
                    {
                        float step = 26f * dt;
                        hookPos += hookDir * step; hookTravel += step;
                        UpdateChainVisual();
                        if (HasTarget && Geo.Box(hookPos, new Vector2(0.8f, 0.8f)).Overlaps(TRect))
                        {
                            if (foe != null) foe.Stun(0.3f); else Player.Instance.Stun(0.3f);
                            Sfx.PlayAt("shock", transform.position, 0.8f, 1.2f);
                            st = S.Pull; t = 1.3f;
                        }
                        else if (hookTravel > 11f || hookPos.x < minX - 1f || hookPos.x > maxX + 1f)
                        {
                            CleanupChain(); st = S.Recover; t = 0.8f;
                        }
                        break;
                    }
                case S.Pull:
                    t -= dt;
                    Face(dx);
                    Move(face * 30f * dt);
                    hookPos = TPos;
                    UpdateChainVisual();
                    Anim(true);
                    if (dist < 1.25f || t <= 0f)
                    {
                        Fx.Explosion(Center + new Vector2(face * 0.6f, 0f), Cfg.Orange, true);
                        Fx.Shake(0.4f);
                        Sfx.PlayAt("slam", transform.position, 0.9f, 1.1f);
                        if (Geo.CircleRect(Center, 2.4f, TRect)) HurtTarget(damage * 2f, 11f, 0f);
                        CleanupChain();
                        st = S.Recover; t = 0.9f;
                    }
                    break;
                case S.CannonCharge:
                    t -= dt; Anim(false);
                    Face(dx);
                    Fx.Emit(Fx.S_Glow, Muzzle, Vector2.zero, new Vector2(0.4f, 0.4f), new Vector2(1.8f, 1.8f), Cfg.A(accent, 0.9f), Cfg.A(accent, 0f), 0.2f);
                    if (t <= 0f)
                    {
                        Vector2 dir = (TPos - Muzzle);
                        if (dir.sqrMagnitude < 0.01f) dir = new Vector2(face, 0f);
                        ArenaBullet.Fire(this, Muzzle, dir.normalized, 24f, damage * 2.5f, 1.2f * 1.4f);
                        Fx.Shake(0.3f);
                        Sfx.PlayAt("laser", transform.position, 0.9f, 0.7f);
                        st = S.Recover; t = 0.8f;
                    }
                    break;
            }
        }

        Vector2 Muzzle { get { return Center + new Vector2(face * 0.9f * size, 0.15f * size); } }

        bool TrySkill(Vector2 tp, float dist)
        {
            if (skillCd > 0f) return false;
            switch (kind)
            {
                case Kind.Brute:
                    if (dist < 10f)
                    {
                        st = S.DashBehind; t = 0.7f; skillCd = 7f;
                        behindX = Mathf.Clamp(tp.x + face * 1.7f, minX + 0.5f, maxX - 0.5f);
                        Sfx.PlayAt("dash", transform.position, 0.8f, 0.8f);
                        return true;
                    }
                    break;
                case Kind.Chain:
                    if (dist > 3.2f && dist < 10f)
                    {
                        st = S.ChainWind; t = 0.3f; skillCd = 6f;
                        return true;
                    }
                    break;
                default:
                    if (dist > 4.5f)
                    {
                        st = S.CannonCharge; t = 0.5f; skillCd = 5f;
                        Sfx.PlayAt("charge", transform.position, 0.8f, 1.2f);
                        return true;
                    }
                    break;
            }
            return false;
        }

        void Strike(float reach, float dmg, float knock, float stun)
        {
            Rect r = Geo.Box(Center + new Vector2(face * reach * 0.5f, 0f), new Vector2(reach, 1.6f * size));
            Sfx.PlayAt("slash", transform.position, 0.8f, 0.7f);
            Fx.Sparks(Center + new Vector2(face * reach * 0.6f, 0f), new Vector2(face, 0f), accent, 6, 10f, 40f);
            if (HasTarget && r.Overlaps(TRect)) HurtTarget(dmg, knock, stun);
        }

        void Face(float dx) { if (Mathf.Abs(dx) > 0.05f) face = dx > 0f ? 1 : -1; }

        /// <summary>Quay mặt trái (-1) / phải (+1) lúc đứng chờ trước trận.</summary>
        public void FaceDir(int f)
        {
            face = f >= 0 ? 1 : -1;
            if (vis != null) vis.localScale = new Vector3(face * size, size, 1f);
        }

        public static string KindName(Kind k)
        {
            switch (k)
            {
                case Kind.Brute: return Cfg.L("Gã Khổng Lồ", "The Brute");
                case Kind.Chain: return Cfg.L("Kẻ Dùng Xích", "The Chainer");
                default: return Cfg.L("Pháo Thủ", "The Gunner");
            }
        }

        void Move(float dx)
        {
            var p = transform.position;
            p.x = Mathf.Clamp(p.x + dx, minX, maxX);
            transform.position = p;
        }

        void Anim(bool moving)
        {
            vis.localScale = new Vector3(face * size, size, 1f);
            if (moving) { animT += Time.deltaTime * 7f; sr.sprite = ((int)animT % 2 == 0) ? walkA : walkB; }
            else sr.sprite = idle;
        }

        // ------------------------------------------------------------ xích (Chain)
        void MakeChainVisual()
        {
            CleanupChain();
            var g = new GameObject("Hook");
            hookSr = Art.Spr(g.transform, "Hook", Art.Diamond, Vector2.zero, new Vector2(0.55f, 0.55f), Cfg.Cyan, 30);
            var l = new GameObject("ChainLine");
            lineSr = Art.Spr(l.transform, "Line", Art.Square, Vector2.zero, new Vector2(1f, 0.1f), Cfg.A(Cfg.Cyan, 0.85f), 29);
        }

        void UpdateChainVisual()
        {
            if (hookSr == null || lineSr == null) return;
            Vector2 a = Center;
            hookSr.transform.parent.position = hookPos;
            Vector2 d = hookPos - a;
            float len = d.magnitude;
            var lt = lineSr.transform.parent;
            lt.position = a + d * 0.5f;
            lt.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            lineSr.transform.localScale = new Vector3(Mathf.Max(0.01f, len), 0.1f, 1f);
        }

        void CleanupChain()
        {
            if (hookSr != null) Destroy(hookSr.transform.parent.gameObject);
            if (lineSr != null) Destroy(lineSr.transform.parent.gameObject);
            hookSr = null; lineSr = null;
        }
    }
}
