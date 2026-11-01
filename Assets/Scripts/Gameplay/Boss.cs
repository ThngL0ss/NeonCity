using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Boss cuối - 2 phase.
    ///  Phase 1 (chậm, máu trâu): 1 kỹ năng - NHẢY ĐẬP (có cảnh báo vùng rơi) + 2 sóng xung kích chạy sát đất.
    ///  Phase 2 (nhanh, máu ít hơn, HÚT MÁU khi đòn trúng người chơi), 3 kỹ năng:
    ///      1) Chớp bước chém   2) Chùm đạn quạt   3) Laser giáng từ trời
    /// </summary>
    public class Boss : Hittable
    {
        public static Boss Instance;

        public int phase = 1;
        public bool Intro = true;
        bool invul = true, transitioning;
        bool hidden;
        int facing = -1;
        float flashT, contactCd;
        string pose = "";           // "", "cast", "slash" - do kỹ năng đặt để chọn clip
        bool moving;
        float lastY;
        int lastSkill = -1;

        [Header("Hình ảnh (object con của prefab Boss - tự tìm theo tên nếu để trống)")]
        public Transform vis;                       // "Visual": lật trái/phải bằng scale.x
        public SpriteAnimator animP1;               // "Visual/Phase1": clip idle, walk, crouch, air
        public SpriteAnimator animP2;               // "Visual/Phase2": clip idle, walk, slash, cast, air
        public SpriteRenderer aura;                 // "Aura": vầng sáng sau lưng (đổi màu theo phase)
        Flasher flasher;
        Color auraBase = new Color(1f, 1f, 1f, 0.3f);
        Coroutine brain;

        public override bool Targetable { get { return !dead && !invul && !transitioning && !hidden; } }
        public bool Enraged { get { return phase == 2 && hp < maxHp * 0.4f; } }
        /// <summary>Tỉ lệ máu gộp cả 2 phase (0..1) cho thanh máu HUD.</summary>
        public float TotalFrac
        {
            get
            {
                float total = Cfg.BossP1Hp + Cfg.BossP2Hp;
                float cur = phase == 1 ? hp + Cfg.BossP2Hp : hp;
                return Mathf.Clamp01(cur / total);
            }
        }
        public float PhaseMarker { get { return Cfg.BossP2Hp / (Cfg.BossP1Hp + Cfg.BossP2Hp); } }

        void Awake()
        {
            Instance = this;
            maxHp = hp = Cfg.BossP1Hp;
            hitSize = new Vector2(2.8f, 4.2f);
            hitOffset = new Vector2(0f, 2.1f);

            if (vis == null) vis = transform.Find("Visual");
            if (vis != null)
            {
                if (animP1 == null) { var t = vis.Find("Phase1"); if (t != null) animP1 = t.GetComponent<SpriteAnimator>(); }
                if (animP2 == null) { var t = vis.Find("Phase2"); if (t != null) animP2 = t.GetComponent<SpriteAnimator>(); }
            }
            if (aura == null) { var t = transform.Find("Aura"); if (t == null && vis != null) t = vis.Find("Aura"); if (t != null) aura = t.GetComponent<SpriteRenderer>(); }
            flasher = new Flasher(gameObject);
            ApplyPhaseLook();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void ApplyPhaseLook()
        {
            if (animP1 != null) animP1.gameObject.SetActive(phase == 1);
            if (animP2 != null) animP2.gameObject.SetActive(phase == 2);
            if (aura != null) aura.color = Cfg.A(phase == 1 ? Cfg.Purple : Cfg.Red, 0.3f);
            hitSize = phase == 1 ? new Vector2(2.8f, 4.2f) : new Vector2(2.2f, 3.8f);
            hitOffset = new Vector2(0f, hitSize.y * 0.5f);
            if (flasher != null) flasher.Capture(gameObject);
        }

        // ------------------------------------------------------------ lifecycle
        public void StartIntro()
        {
            brain = StartCoroutine(IntroThenBrain());
        }

        IEnumerator IntroThenBrain()
        {
            Intro = true;
            invul = true;
            Sfx.Play("spawn", 1f, 0.6f);
            float lx = transform.position.x;
            Telegraph.Zone(new Rect(lx - 1.8f, 0f, 3.6f, 22f), Cfg.Magenta, 1.0f);
            yield return new WaitForSeconds(0.9f);

            // rơi xuống
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / 0.35f);
                SetPos(lx, Mathf.Lerp(22f, 0f, u * u));
                yield return null;
            }
            SetPos(lx, 0f);
            Fx.Explosion(new Vector2(lx, 0.5f), Cfg.Magenta, true);
            Fx.Ring(new Vector2(lx, 0.2f), Cfg.White, 1f, 16f, 0.6f);
            Fx.Shake(1f);
            Fx.Flash(Cfg.White, 0.5f);
            Sfx.Play("slam", 1f);
            Projectile.ClearHostile();
            yield return new WaitForSeconds(0.3f);
            Sfx.Play("bossRoar", 1f);
            Fx.Shake(0.7f);
            yield return new WaitForSeconds(1.5f);

            Intro = false;
            invul = false;
            brain = StartCoroutine(Brain());
        }

        void SetPos(float x, float y)
        {
            transform.position = new Vector3(x, y, 0f);
        }

        float ArenaClamp(float x) { return Mathf.Clamp(x, Cfg.GateX + 3f, Cfg.LevelMaxX - 3f); }

        Player P { get { return Player.Instance; } }
        bool PlayerAlive { get { return P != null && !P.Dead && GameManager.Active; } }

        void FacePlayer()
        {
            if (PlayerAlive) facing = P.Position.x >= transform.position.x ? 1 : -1;
        }

        // ------------------------------------------------------------ brain
        IEnumerator Brain()
        {
            while (!dead)
            {
                if (!PlayerAlive) { yield return null; continue; }
                if (phase == 1)
                {
                    yield return Walk(Random.Range(2.0f, 3.2f), Cfg.BossP1Speed);
                    if (!PlayerAlive) continue;
                    yield return Slam();
                    yield return new WaitForSeconds(1.2f);       // hở sườn: cửa sổ phản công
                }
                else
                {
                    float rush = Enraged ? 0.6f : 1f;
                    yield return Walk(Random.Range(0.8f, 1.5f) * rush, Cfg.BossP2Speed);
                    if (!PlayerAlive) continue;
                    int pick;
                    do { pick = Random.Range(0, 3); } while (pick == lastSkill);
                    lastSkill = pick;
                    if (pick == 0) yield return BlinkSlash();
                    else if (pick == 1) yield return BoltFan();
                    else yield return SkyLasers();
                    yield return new WaitForSeconds((Enraged ? 0.45f : 0.8f));
                }
            }
        }

        IEnumerator Walk(float dur, float speed)
        {
            float t = 0f;
            while (t < dur && !dead)
            {
                t += Time.deltaTime;
                moving = false;
                if (PlayerAlive)
                {
                    FacePlayer();
                    float dx = P.Position.x - transform.position.x;
                    if (Mathf.Abs(dx) > 1.6f)
                    {
                        float nx = ArenaClamp(transform.position.x + Mathf.Sign(dx) * speed * Time.deltaTime);
                        SetPos(nx, 0f);
                        moving = true;
                    }
                }
                yield return null;
            }
            moving = false;
        }

        // ---- Phase 1 skill: nhảy đập
        IEnumerator Slam()
        {
            Sfx.Play("warn", 0.9f, 0.6f);
            Sfx.Play("charge", 0.7f, 0.7f);
            // gồng + nhắm
            float t = 0f;
            float targetX = ArenaClamp(P.Position.x);
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                if (PlayerAlive) targetX = ArenaClamp(P.Position.x);
                FacePlayer();
                crouch = Mathf.Lerp(0f, 0.22f, t / 0.4f);
                yield return null;
            }
            // khóa vùng đáp, hiện cảnh báo
            Rect zone = Geo.Box(new Vector2(targetX, 1.2f), new Vector2(6.4f, 2.6f));
            Telegraph.Zone(new Rect(zone.x, 0f, zone.width, 0.35f), Cfg.Red, 0.75f);
            Telegraph.Zone(zone, Cfg.Red, 0.75f);
            crouch = 0f;

            float startX = transform.position.x;
            float lt = 0f;
            const float airTime = 0.75f;
            while (lt < airTime)
            {
                lt += Time.deltaTime;
                float u = Mathf.Clamp01(lt / airTime);
                SetPos(Mathf.Lerp(startX, targetX, u), 7f * 4f * u * (1f - u));
                yield return null;
            }
            SetPos(targetX, 0f);

            // đáp đất
            Fx.Explosion(new Vector2(targetX, 0.4f), Cfg.Magenta, true);
            Fx.Ring(new Vector2(targetX, 0.2f), Cfg.Red, 1f, 12f, 0.5f);
            Fx.Sparks(new Vector2(targetX, 0.3f), Vector2.up, Cfg.Orange, 20, 16f, 80f);
            Fx.Shake(0.9f);
            Fx.HitStop(0.06f);
            Sfx.Play("slam", 1f);
            if (PlayerAlive && zone.Overlaps(P.HurtRect)) Strike(Cfg.BossSlamDamage, 10f);
            Projectile.Wave(new Vector2(targetX - 2.2f, 0f), -1f, 9f, Cfg.BossSlamDamage * 0.8f, Cfg.Purple);
            Projectile.Wave(new Vector2(targetX + 2.2f, 0f), 1f, 9f, Cfg.BossSlamDamage * 0.8f, Cfg.Purple);
        }

        float crouch;

        // ---- Phase 2 skill 1: chớp bước chém
        IEnumerator BlinkSlash()
        {
            Sfx.Play("teleport", 0.9f);
            Fx.Explosion(Center, Cfg.Orange, false);
            hidden = true;
            yield return new WaitForSeconds(0.28f);

            if (!PlayerAlive) { hidden = false; yield break; }
            float side = Random.value < 0.5f ? -1f : 1f;
            float nx = ArenaClamp(P.Position.x + side * 3.0f);
            if (Mathf.Abs(nx - P.Position.x) < 1.5f) nx = ArenaClamp(P.Position.x - side * 3.0f);
            SetPos(nx, 0f);
            FacePlayer();
            hidden = false;
            Sfx.Play("teleport", 0.7f, 1.3f);
            Fx.Ring(new Vector2(nx, 1.2f), Cfg.Orange, 0.5f, 4f, 0.3f);

            float reach = 3.6f;
            Rect hit = new Rect(facing > 0 ? nx : nx - reach, 0f, reach, 2.8f);
            Sfx.Play("warn", 0.9f, 1.4f);
            float tel = Enraged ? 0.38f : 0.5f;
            Telegraph.Zone(hit, Cfg.Red, tel);
            float t = 0f;
            while (t < tel) { t += Time.deltaTime; FacePlayer(); yield return null; }

            // chém (hướng đã khóa theo vùng cảnh báo)
            pose = "slash";
            Sfx.Play("slash", 1f, 0.6f);
            Fx.Slash(new Vector2(hit.center.x, 1.5f), facing, Cfg.Orange, 2.6f);
            Fx.Slash(new Vector2(hit.center.x, 1.5f), facing, Cfg.White, 1.8f);
            Fx.Shake(0.5f);
            if (PlayerAlive && hit.Overlaps(P.HurtRect)) Strike(Cfg.BossSlashDamage, 11f);
            yield return new WaitForSeconds(0.55f);
            pose = "";
        }

        // ---- Phase 2 skill 2: chùm đạn quạt
        IEnumerator BoltFan()
        {
            pose = "cast";
            FacePlayer();
            Sfx.Play("charge", 0.8f, 1.1f);
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                FacePlayer();
                Fx.Trail(Muzzle, Cfg.A(Cfg.Orange, 0.8f), 0.4f + 1.2f * t, 0.12f);
                yield return null;
            }
            int volleys = Enraged ? 4 : 3;
            for (int v = 0; v < volleys; v++)
            {
                if (!PlayerAlive) break;
                FacePlayer();
                Vector2 m = Muzzle;
                Vector2 aim = P.Center - m;
                float baseAng = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
                for (int i = -2; i <= 2; i++)
                {
                    float a = (baseAng + i * 13f) * Mathf.Deg2Rad;
                    Projectile.Bolt(m, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 11f, Cfg.BossBoltDamage, Cfg.Orange, true);
                }
                Sfx.Play("bolt", 0.9f, 0.7f);
                Fx.Flare(m, Cfg.Orange, 2.4f, 0.2f);
                Fx.Shake(0.15f);
                yield return new WaitForSeconds(Enraged ? 0.42f : 0.55f);
            }
            yield return new WaitForSeconds(0.4f);
            pose = "";
        }

        Vector2 Muzzle { get { return new Vector2(transform.position.x + facing * 2.2f, 1.9f); } }

        // ---- Phase 2 skill 3: laser giáng từ trời
        IEnumerator SkyLasers()
        {
            pose = "cast";
            int waves = Enraged ? 2 : 1;
            for (int w = 0; w < waves; w++)
            {
                if (!PlayerAlive) { pose = ""; yield break; }
                Sfx.Play("charge", 0.9f, 0.9f);
                float px = P.Position.x;
                float[] offs = { -8f, -4f, 0f, 4f, 8f };
                var xs = new List<float>();
                for (int i = 0; i < offs.Length; i++)
                {
                    float o = offs[i] + (w == 1 ? 2f : 0f);
                    xs.Add(ArenaClamp(px + o + Random.Range(-0.8f, 0.8f)));
                }
                xs[2] = ArenaClamp(px + (w == 1 ? 1.2f : 0f));     // một cột nhắm thẳng vào người chơi

                for (int i = 0; i < xs.Count; i++)
                    Telegraph.Zone(new Rect(xs[i] - 0.9f, 0f, 1.8f, 18f), Cfg.Magenta, 1.0f);
                Sfx.Play("warn", 0.8f, 1.2f);
                yield return new WaitForSeconds(1.0f);

                Sfx.Play("laser", 1f);
                Fx.Shake(0.7f);
                Fx.Flash(Cfg.Magenta, 0.2f);
                bool hitDone = false;
                for (int i = 0; i < xs.Count; i++)
                {
                    float x = xs[i];
                    Fx.Beam(new Vector2(x, 18f), new Vector2(x, 0f), Cfg.Magenta, 1.6f, 0.3f);
                    Fx.Explosion(new Vector2(x, 0.4f), Cfg.Magenta, false);
                    if (!hitDone && PlayerAlive && new Rect(x - 0.9f, 0f, 1.8f, 18f).Overlaps(P.HurtRect))
                    {
                        hitDone = true;
                        Strike(Cfg.BossLanceDamage, 9f, new Vector2(x, 0f));
                    }
                }
                yield return new WaitForSeconds(0.45f);
            }
            yield return new WaitForSeconds(0.3f);
            pose = "";
        }

        // ------------------------------------------------------------ damage out / drain
        /// <summary>Gây sát thương lên người chơi; ở phase 2 hút máu theo tỉ lệ sát thương thực gây ra.</summary>
        void Strike(float dmg, float knock, Vector2? from = null)
        {
            Vector2 src = from.HasValue ? from.Value : (Vector2)transform.position;
            float dealt = P.Hurt(dmg, src, knock);
            if (dealt > 0f) Drain(dealt);
        }

        public void Drain(float dealt)
        {
            if (phase != 2 || dead || P == null) return;
            float heal = dealt * Cfg.BossLifesteal;
            hp = Mathf.Min(maxHp, hp + heal);
            Fx.Beam(P.Center, Center, Cfg.Lime, 0.18f, 0.35f);
            Fx.Burst(Center, Cfg.Lime, 10, 6f, 0.5f, 0.35f);
            Fx.Text(Center + new Vector2(0f, 2.4f), "+" + Mathf.RoundToInt(heal), Cfg.Lime, 1.2f);
            Sfx.Play("drain", 0.8f);
        }

        // ------------------------------------------------------------ damage in
        public override void TakeHit(float dmg, Vector2 dir, float knockPower, bool melee)
        {
            if (!Targetable) return;
            hp -= dmg;
            flashT = 0.07f;
            Sfx.Play("hit", 0.8f, 0.7f);
            Fx.Sparks(Center, dir, phase == 1 ? Cfg.Magenta : Cfg.Orange, 8, 13f, 60f);
            Fx.Burst(Center, Cfg.A(Cfg.White, 0.8f), 3, 4f, 0.25f, 0.28f);
            Fx.Text(Center + new Vector2(Random.Range(-0.5f, 0.5f), 1.9f), Mathf.RoundToInt(dmg).ToString(), Cfg.Yellow);
            if (hp <= 0f)
            {
                hp = 0f;
                if (phase == 1) StartCoroutine(Transition());
                else StartCoroutine(DeathSequence());
            }
        }

        IEnumerator Transition()
        {
            transitioning = true;
            if (brain != null) StopCoroutine(brain);
            hidden = false;
            crouch = 0f;
            pose = ""; moving = false;
            SetPos(transform.position.x, 0f);
            Projectile.ClearHostile();
            Sfx.Play("phase", 1f);
            Fx.Flash(Cfg.White, 0.6f);
            Fx.Shake(1f);
            for (int i = 0; i < 4; i++)
            {
                Fx.Explosion(Center + new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1.5f)), Cfg.Magenta, true);
                yield return new WaitForSeconds(0.25f);
            }
            // đẩy người chơi ra xa
            if (PlayerAlive) P.Knock(new Vector2(Mathf.Sign(P.Position.x - transform.position.x) * 14f, 7f));
            Fx.Ring(Center, Cfg.White, 1f, 20f, 0.7f);
            phase = 2;
            maxHp = hp = Cfg.BossP2Hp;
            ApplyPhaseLook();
            Sfx.Play("bossRoar", 1f, 1.3f);
            Fx.Shake(0.8f);
            GameManager.OnBossPhase2();
            yield return new WaitForSeconds(1.5f);
            transitioning = false;
            brain = StartCoroutine(Brain());
        }

        IEnumerator DeathSequence()
        {
            dead = true;
            if (brain != null) StopCoroutine(brain);
            hidden = false;
            crouch = 0f;
            SetPos(transform.position.x, 0f);
            Projectile.ClearHostile();
            Sfx.Play("phase", 1f, 0.7f);
            Fx.Shake(1f);
            for (int i = 0; i < 26; i++)
            {
                Vector2 c = Center + new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(-1.5f, 2f));
                Fx.Explosion(c, i % 2 == 0 ? Cfg.Orange : Cfg.Magenta, i % 4 == 0);
                Fx.Shake(0.4f);
                if (i % 3 == 0) Sfx.Play("enemyDie", 0.8f, Random.Range(0.6f, 1f));
                yield return new WaitForSeconds(0.1f);
            }
            Fx.Flash(Cfg.White, 1f);
            Fx.Explosion(Center, Cfg.White, true);
            Fx.Ring(Center, Cfg.Cyan, 1f, 26f, 1f);
            Sfx.Play("towerDie", 1f, 0.7f);
            Sfx.Play("slam", 1f, 0.8f);
            if (vis != null) vis.gameObject.SetActive(false);
            GameManager.OnBossDead();
            yield return new WaitForSeconds(0.5f);
            Destroy(gameObject);
        }

        // ------------------------------------------------------------ update (body animation + contact damage)
        void Update()
        {
            float dt = Time.deltaTime;
            if (flashT > 0f) flashT -= dt;
            contactCd -= dt;

            if (vis != null)
            {
                float breathe = 1f + 0.015f * Mathf.Sin(Time.time * 2.5f);
                float sy = (1f - crouch) * breathe;
                float sx = 1f + crouch * 0.4f;
                vis.localScale = new Vector3(facing * sx, sy, 1f);

                bool air = transform.position.y > 0.3f;
                string clip;
                if (air) clip = "air";
                else if (crouch > 0.01f) clip = "crouch";
                else if (pose != "") clip = pose;
                else if (moving) clip = "walk";
                else clip = "idle";
                var a = phase == 1 ? animP1 : animP2;
                if (a != null)
                {
                    if (!a.Has(clip)) clip = (clip == "crouch" || clip == "slash" || clip == "cast") ? "idle" : clip;
                    a.Play(clip);
                }
                if (aura != null)
                {
                    float hs = 1f + 0.06f * Mathf.Sin(Time.time * 3f);
                    aura.transform.localScale = new Vector3(hs, hs, 1f);
                }
            }

            float alpha = hidden ? 0.0f : (transitioning ? 0.65f + 0.35f * Mathf.Sin(Time.time * 30f) : 1f);
            if (Intro && transform.position.y > 15f) alpha = 0.3f;
            if (flasher != null) flasher.Apply(flashT > 0f || (transitioning && Mathf.Sin(Time.time * 24f) > 0.6f), Mathf.Clamp01(alpha));

            // sát thương khi chạm thân
            if (!dead && !invul && !transitioning && !hidden && PlayerAlive && contactCd <= 0f)
            {
                if (HitRect.Overlaps(P.HurtRect))
                {
                    contactCd = 0.9f;
                    Strike(Cfg.BossTouchDamage, 9f);
                }
            }

            // hạt khí lơ lửng
            if (!dead && Random.value < 0.3f)
            {
                Color c = phase == 1 ? Cfg.Purple : Cfg.Red;
                Fx.Emit(Fx.S_Glow, (Vector2)transform.position + new Vector2(Random.Range(-1.4f, 1.4f), Random.Range(0.3f, 3.6f)),
                        new Vector2(0f, Random.Range(0.5f, 2f)), new Vector2(0.4f, 0.4f), Vector2.zero, Cfg.A(c, 0.7f), Cfg.A(c, 0f), 0.6f);
            }
        }
    }
}
