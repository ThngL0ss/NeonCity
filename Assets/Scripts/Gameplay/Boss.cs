using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Boss cuối (OVERLORD, bậc SS, cấp 1) - 3 phase.
    ///  Phase 1 (chậm, máu trâu): 1 kỹ năng - NHẢY ĐẬP (có cảnh báo vùng rơi) + 2 sóng xung kích chạy sát đất.
    ///  Phase 2 (nhanh, máu ít hơn, HÚT MÁU khi đòn trúng người chơi), 3 kỹ năng:
    ///      1) Chớp bước chém   2) Chùm đạn quạt   3) Laser giáng từ trời
    ///  Phase 3 (tái sinh): hạ phase 2 xong boss gục xuống, hồi đầy máu, bất động 2 giây rồi NỔ 3 LẦN (sát thương lớn + sóng xung kích
    ///      làm người chơi choáng), sau đó đứng dậy chiến đấu với tốc độ + sát thương của phase 2, độ trâu của phase 1,
    ///      dùng kỹ năng của cả 2 phase + kỹ năng mới BÃO SÉT (cột sét nổ lan ra / thu vào theo nhịp).
    ///  Thanh máu chỉ có 2 mốc (phase 1 | phase 2); sang phase 3 thanh máu hồi đầy 100%.
    ///  Bảo hiểm: nếu người chơi vẫn hạ được máu phase 3 → boss dừng lại, nhấp nháy 7 màu rồi TỰ HUỶ bằng một vụ nổ khổng lồ
    ///      giết chắc người chơi (xem DoomSequence).
    /// </summary>
    public class Boss : Hittable
    {
        public static Boss Instance;

        public int phase = 1;
        public bool Intro = true;
        bool invul = true, transitioning, p3Seq, doom;
        float collapse;             // 0..1: mức gục xuống (phase 3)
        bool hidden;
        int facing = -1;
        float flashT, contactCd;
        string pose = "";           // "", "cast", "slash" - do kỹ năng đặt để chọn clip
        bool moving;
        float lastY;
        int lastSkill = -1;

        // ------------------------------------------------------------ chỉ số (chỉnh trong Inspector của Prefab Boss)
        [Header("Cấp bậc")]
        [Tooltip("Boss là bậc SS (không rơi linh kiện), cấp 1. Cấp độ nhân máu / sát thương / tốc độ của boss theo PowerRules (cấp 1 = không đổi)")]
        public PowerInfo power = new PowerInfo(Rank.SS, 1);

        [Header("Máu & di chuyển")]
        [Tooltip("Máu phase 1 (chậm, máu trâu)")] public float p1Hp = 2800f;
        [Tooltip("Máu phase 2 (nhanh, máu ít hơn nhưng hút máu)")] public float p2Hp = 1500f;
        [Tooltip("Máu phase 3 (độ trâu của phase 1)")] public float p3Hp = 2800f;
        public float p1Speed = 2.0f;
        public float p2Speed = 5.4f;
        [Tooltip("Tốc độ phase 3 (bằng phase 2)")] public float p3Speed = 5.4f;
        [Tooltip("Hộp trúng đòn phase 1 / phase 2 (rộng, cao)")] public Vector2 p1HitSize = new Vector2(2.8f, 4.2f);
        public Vector2 p2HitSize = new Vector2(2.2f, 3.8f);
        [Tooltip("Hộp trúng đòn phase 3")] public Vector2 p3HitSize = new Vector2(2.5f, 4.2f);
        [Tooltip("Hình boss phase 3 to hơn phase 2 bao nhiêu lần")] public float p3Scale = 1.12f;
        [Tooltip("Phase 2 vào trạng thái nổi giận khi máu dưới tỉ lệ này (0.4 = 40%)")] public float enrageHpFrac = 0.4f;

        [Header("Sát thương")]
        public float slamDamage = 18f;
        [Tooltip("Sát thương khi chạm thân boss")] public float touchDamage = 12f;
        [Tooltip("Giãn cách giữa 2 lần chạm gây sát thương (giây)")] public float touchCooldown = 0.9f;
        public float boltDamage = 10f;
        public float slashDamage = 24f;
        public float lanceDamage = 22f;
        [Tooltip("Hút bao nhiêu % sát thương gây ra thành máu (phase 2). 0.6 = 60%")] public float lifesteal = 0.60f;
        [Tooltip("Phase 3: hệ số nhân sát thương so với phase 2 (1 = bằng phase 2)")] public float p3DamageMul = 1f;
        [Tooltip("Phase 3: hút máu thấp hơn phase 2 vì máu đã rất trâu. 0 = không hút")] public float p3Lifesteal = 0.25f;

        [Header("Phase 1: nhảy đập")]
        [Tooltip("Thời gian đi bộ trước mỗi đòn (ngắn nhất, dài nhất)")] public Vector2 p1WalkTime = new Vector2(2.0f, 3.2f);
        [Tooltip("Thời gian nghỉ sau đòn đập = cơ hội phản công")] public float p1RecoverTime = 1.2f;
        [Tooltip("Thời gian gồng + ngắm trước khi nhảy")] public float slamChargeTime = 0.4f;
        [Tooltip("Thời gian bay trên không (cũng là thời gian cảnh báo vùng rơi)")] public float slamAirTime = 0.75f;
        public float slamJumpHeight = 7f;
        [Tooltip("Vùng sát thương khi đáp đất (rộng, cao)")] public Vector2 slamZoneSize = new Vector2(6.4f, 2.6f);
        public float waveSpeed = 9f;
        [Tooltip("Sát thương sóng xung kích = Slam Damage x hệ số này")] public float waveDamageMul = 0.8f;

        [Header("Phase 2: nhịp chiêu")]
        [Tooltip("Thời gian đi bộ trước mỗi chiêu (ngắn nhất, dài nhất)")] public Vector2 p2WalkTime = new Vector2(0.8f, 1.5f);
        [Tooltip("Khi nổi giận, thời gian đi bộ nhân với hệ số này (nhỏ = dồn dập hơn)")] public float enragedWalkMul = 0.6f;
        [Tooltip("Thời gian nghỉ sau mỗi chiêu (bình thường, nổi giận)")] public Vector2 p2RestTime = new Vector2(0.8f, 0.45f);

        [Header("Phase 2: chớp bước chém")]
        public float blinkDistance = 3.0f;
        public float slashReach = 3.6f;
        [Tooltip("Thời gian cảnh báo trước khi chém (bình thường, nổi giận)")] public Vector2 slashTelegraph = new Vector2(0.5f, 0.38f);

        [Header("Phase 2: chùm đạn quạt")]
        public float fanBoltSpeed = 11f;
        [Tooltip("Góc giữa 2 viên đạn cạnh nhau (độ). Mỗi loạt bắn 5 viên")] public float fanAngle = 13f;
        [Tooltip("Số loạt đạn (bình thường, nổi giận)")] public Vector2Int fanVolleys = new Vector2Int(3, 4);
        [Tooltip("Thời gian giữa các loạt (bình thường, nổi giận)")] public Vector2 fanGap = new Vector2(0.55f, 0.42f);

        [Header("Phase 2: laser từ trời")]
        [Tooltip("Thời gian cảnh báo trước khi laser giáng xuống")] public float laserTelegraph = 1.0f;
        [Tooltip("Khoảng cách giữa các cột laser (có 5 cột)")] public float laserSpacing = 4f;
        public float laserWidth = 1.8f;

        [Header("Phase 3: nhịp chiêu")]
        [Tooltip("Thời gian đi bộ trước mỗi chiêu (ngắn nhất, dài nhất)")] public Vector2 p3WalkTime = new Vector2(0.7f, 1.3f);
        [Tooltip("Thời gian nghỉ sau mỗi chiêu (bình thường, nổi giận)")] public Vector2 p3RestTime = new Vector2(0.7f, 0.4f);

        [Header("Phase 3: gục xuống, tái sinh, nổ 3 lần")]
        [Tooltip("Thời gian gục xuống sau khi hết máu phase 2 (giây)")] public float collapseTime = 1.2f;
        [Tooltip("Thời gian hồi đầy máu (giây)")] public float refillTime = 1.4f;
        [Tooltip("Thời gian bất động sau khi hồi đầy máu, trước khi nổ (giây)")] public float stillTime = 2f;
        [Tooltip("Bán kính mỗi vụ nổ (vòng đỏ báo trước). Vụ nổ thứ 3 lớn hơn")] public float blastRadius = 8.5f;
        [Tooltip("Sát thương mỗi vụ nổ")] public float blastDamage = 20f;
        [Tooltip("Thời gian vòng đỏ báo trước mỗi vụ nổ (giây)")] public float blastWarn = 0.45f;
        [Tooltip("Thời gian nghỉ giữa 2 vụ nổ (giây)")] public float blastGap = 0.5f;
        [Tooltip("Vụ nổ thứ 3: bán kính x hệ số này, sát thương x hệ số kế bên và gây choáng")] public float finalBlastRadiusMul = 1.4f;
        public float finalBlastDamageMul = 1.25f;
        [Tooltip("Thời gian người chơi bị choáng bởi sóng xung kích cuối (giây)")] public float stunTime = 2.2f;
        [Tooltip("Thời gian đứng dậy sau 3 vụ nổ (giây)")] public float standUpTime = 1.3f;

        [Header("Phase 3: tự huỷ (khi máu phase 3 về 0)")]
        [Tooltip("Thời gian boss đứng im nhấp nháy 7 màu trước khi nổ (giây)")] public float doomChargeTime = 3.4f;
        [Tooltip("Bán kính vụ nổ tự huỷ - phủ kín khu boss, người chơi không thể né")] public float doomRadius = 90f;

        [Header("Phase 3: kỹ năng mới - Bão sét")]
        [Tooltip("Số cột sét mỗi bên (mỗi cột báo trước rồi nổ lan theo nhịp)")] public int thunderSteps = 6;
        public float thunderSpacing = 3.2f;
        [Tooltip("Thời gian giữa 2 cột sét liên tiếp")] public float thunderGap = 0.2f;
        [Tooltip("Thời gian cảnh báo mỗi cột trước khi sét giáng")] public float thunderTelegraph = 0.55f;
        public float thunderWidth = 1.7f;
        public float thunderDamage = 20f;

        [Header("Hình ảnh (object con của prefab Boss - tự tìm theo tên nếu để trống)")]
        public Transform vis;                       // "Visual": lật trái/phải bằng scale.x
        public SpriteAnimator animP1;               // "Visual/Phase1": clip idle, walk, crouch, air
        public SpriteAnimator animP2;               // "Visual/Phase2": clip idle, walk, slash, cast, air
        public SpriteRenderer aura;                 // "Aura": vầng sáng sau lưng (đổi màu theo phase)
        Flasher flasher;
        Color auraBase = new Color(1f, 1f, 1f, 0.3f);
        Coroutine brain;

        public override bool Targetable { get { return !dead && !invul && !transitioning && !hidden; } }
        public bool Enraged { get { return phase >= 2 && hp < maxHp * enrageHpFrac; } }
        /// <summary>
        /// Tỉ lệ máu cho thanh máu HUD (0..1). Phase 1 + 2 chung một thanh có 2 mốc (phase 2 bên trái, phase 1 bên phải);
        /// sang phase 3 thanh máu hồi đầy 100% và chỉ tính máu phase 3.
        /// </summary>
        public float TotalFrac
        {
            get
            {
                if (phase >= 3) return Mathf.Clamp01(hp / Mathf.Max(1f, p3Hp));
                float total = p1Hp + p2Hp;
                float cur = phase == 1 ? hp + p2Hp : hp;
                return Mathf.Clamp01(cur / total);
            }
        }
        /// <summary>Vạch chia phase 2 | phase 1 trên thanh máu (0..1 tính từ trái). Phase 3 không có vạch chia.</summary>
        public float PhaseMarker { get { return phase >= 3 ? 0f : p2Hp / (p1Hp + p2Hp); } }
        float Lifesteal { get { return phase == 2 ? lifesteal : (phase == 3 ? p3Lifesteal : 0f); } }

        /// <summary>Cấp độ boss nhân máu / sát thương / tốc độ (cấp 1 = nhân 1, không đổi gì).</summary>
        void ApplyLevel()
        {
            int lv = power != null ? power.level : 1;
            float hm = PowerRules.HpMul(lv), dm = PowerRules.DamageMul(lv), sm = PowerRules.SpeedMul(lv);
            p1Hp *= hm; p2Hp *= hm; p3Hp *= hm;
            slamDamage *= dm; touchDamage *= dm; boltDamage *= dm; slashDamage *= dm; lanceDamage *= dm;
            thunderDamage *= dm; blastDamage *= dm;
            p1Speed *= sm; p2Speed *= sm; p3Speed *= sm;
        }

        void Awake()
        {
            Instance = this;
            if (power == null) power = new PowerInfo(Rank.SS, 1);
            ApplyLevel();
            maxHp = hp = p1Hp;
            hitSize = p1HitSize;
            hitOffset = new Vector2(0f, hitSize.y * 0.5f);

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
            if (animP2 != null) animP2.gameObject.SetActive(phase >= 2);
            if (aura != null) aura.color = Cfg.A(phase == 1 ? Cfg.Purple : (phase == 2 ? Cfg.Red : Cfg.Orange), 0.3f);
            hitSize = phase == 1 ? p1HitSize : (phase == 2 ? p2HitSize : p3HitSize);
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
                    yield return Walk(Random.Range(p1WalkTime.x, p1WalkTime.y), p1Speed);
                    if (!PlayerAlive) continue;
                    yield return Slam();
                    yield return new WaitForSeconds(p1RecoverTime);       // hở sườn: cửa sổ phản công
                }
                else if (phase == 3)
                {
                    float rush3 = Enraged ? enragedWalkMul : 1f;
                    yield return Walk(Random.Range(p3WalkTime.x, p3WalkTime.y) * rush3, p3Speed);
                    if (!PlayerAlive) continue;
                    int pick3;
                    do { pick3 = Random.Range(0, 5); } while (pick3 == lastSkill);
                    lastSkill = pick3;
                    if (pick3 == 0) yield return Slam();
                    else if (pick3 == 1) yield return BlinkSlash();
                    else if (pick3 == 2) yield return BoltFan();
                    else if (pick3 == 3) yield return SkyLasers();
                    else yield return ThunderSweep();
                    yield return new WaitForSeconds(Enraged ? p3RestTime.y : p3RestTime.x);
                }
                else
                {
                    float rush = Enraged ? enragedWalkMul : 1f;
                    yield return Walk(Random.Range(p2WalkTime.x, p2WalkTime.y) * rush, p2Speed);
                    if (!PlayerAlive) continue;
                    int pick;
                    do { pick = Random.Range(0, 3); } while (pick == lastSkill);
                    lastSkill = pick;
                    if (pick == 0) yield return BlinkSlash();
                    else if (pick == 1) yield return BoltFan();
                    else yield return SkyLasers();
                    yield return new WaitForSeconds(Enraged ? p2RestTime.y : p2RestTime.x);
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
            while (t < slamChargeTime)
            {
                t += Time.deltaTime;
                if (PlayerAlive) targetX = ArenaClamp(P.Position.x);
                FacePlayer();
                crouch = Mathf.Lerp(0f, 0.22f, t / Mathf.Max(0.01f, slamChargeTime));
                yield return null;
            }
            // khóa vùng đáp, hiện cảnh báo
            Rect zone = Geo.Box(new Vector2(targetX, 1.2f), slamZoneSize);
            Telegraph.Zone(new Rect(zone.x, 0f, zone.width, 0.35f), Cfg.Red, slamAirTime);
            Telegraph.Zone(zone, Cfg.Red, slamAirTime);
            crouch = 0f;

            float startX = transform.position.x;
            float lt = 0f;
            float airTime = Mathf.Max(0.05f, slamAirTime);
            while (lt < airTime)
            {
                lt += Time.deltaTime;
                float u = Mathf.Clamp01(lt / airTime);
                SetPos(Mathf.Lerp(startX, targetX, u), slamJumpHeight * 4f * u * (1f - u));
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
            if (PlayerAlive && zone.Overlaps(P.HurtRect)) Strike(slamDamage, 10f);
            Minibot.HurtInRect(zone, slamDamage, transform.position, 10f);
            Projectile.Wave(new Vector2(targetX - 2.2f, 0f), -1f, waveSpeed, slamDamage * waveDamageMul, Cfg.Purple);
            Projectile.Wave(new Vector2(targetX + 2.2f, 0f), 1f, waveSpeed, slamDamage * waveDamageMul, Cfg.Purple);
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
            float nx = ArenaClamp(P.Position.x + side * blinkDistance);
            if (Mathf.Abs(nx - P.Position.x) < 1.5f) nx = ArenaClamp(P.Position.x - side * blinkDistance);
            SetPos(nx, 0f);
            FacePlayer();
            hidden = false;
            Sfx.Play("teleport", 0.7f, 1.3f);
            Fx.Ring(new Vector2(nx, 1.2f), Cfg.Orange, 0.5f, 4f, 0.3f);

            float reach = slashReach;
            Rect hit = new Rect(facing > 0 ? nx : nx - reach, 0f, reach, 2.8f);
            Sfx.Play("warn", 0.9f, 1.4f);
            float tel = Enraged ? slashTelegraph.y : slashTelegraph.x;
            Telegraph.Zone(hit, Cfg.Red, tel);
            float t = 0f;
            while (t < tel) { t += Time.deltaTime; FacePlayer(); yield return null; }

            // chém (hướng đã khóa theo vùng cảnh báo)
            pose = "slash";
            Sfx.Play("slash", 1f, 0.6f);
            Fx.Slash(new Vector2(hit.center.x, 1.5f), facing, Cfg.Orange, 2.6f);
            Fx.Slash(new Vector2(hit.center.x, 1.5f), facing, Cfg.White, 1.8f);
            Fx.Shake(0.5f);
            if (PlayerAlive && hit.Overlaps(P.HurtRect)) Strike(slashDamage, 11f);
            Minibot.HurtInRect(hit, slashDamage, transform.position, 11f);
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
            int volleys = Enraged ? fanVolleys.y : fanVolleys.x;
            for (int v = 0; v < volleys; v++)
            {
                if (!PlayerAlive) break;
                FacePlayer();
                Vector2 m = Muzzle;
                Vector2 aim = P.Center - m;
                float baseAng = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
                for (int i = -2; i <= 2; i++)
                {
                    float a = (baseAng + i * fanAngle) * Mathf.Deg2Rad;
                    Projectile.Bolt(m, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), fanBoltSpeed, boltDamage, Cfg.Orange, true);
                }
                Sfx.Play("bolt", 0.9f, 0.7f);
                Fx.Flare(m, Cfg.Orange, 2.4f, 0.2f);
                Fx.Shake(0.15f);
                yield return new WaitForSeconds(Enraged ? fanGap.y : fanGap.x);
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
                float[] offs = { -2f * laserSpacing, -laserSpacing, 0f, laserSpacing, 2f * laserSpacing };
                var xs = new List<float>();
                for (int i = 0; i < offs.Length; i++)
                {
                    float o = offs[i] + (w == 1 ? 2f : 0f);
                    xs.Add(ArenaClamp(px + o + Random.Range(-0.8f, 0.8f)));
                }
                xs[2] = ArenaClamp(px + (w == 1 ? 1.2f : 0f));     // một cột nhắm thẳng vào người chơi

                for (int i = 0; i < xs.Count; i++)
                    Telegraph.Zone(new Rect(xs[i] - laserWidth * 0.5f, 0f, laserWidth, 18f), Cfg.Magenta, laserTelegraph);
                Sfx.Play("warn", 0.8f, 1.2f);
                yield return new WaitForSeconds(laserTelegraph);

                Sfx.Play("laser", 1f);
                Fx.Shake(0.7f);
                Fx.Flash(Cfg.Magenta, 0.2f);
                bool hitDone = false;
                for (int i = 0; i < xs.Count; i++)
                {
                    float x = xs[i];
                    Fx.Beam(new Vector2(x, 18f), new Vector2(x, 0f), Cfg.Magenta, 1.6f, 0.3f);
                    Fx.Explosion(new Vector2(x, 0.4f), Cfg.Magenta, false);
                    if (!hitDone && PlayerAlive && new Rect(x - laserWidth * 0.5f, 0f, laserWidth, 18f).Overlaps(P.HurtRect))
                    {
                        hitDone = true;
                        Strike(lanceDamage, 9f, new Vector2(x, 0f));
                    }
                    Minibot.HurtInRect(new Rect(x - laserWidth * 0.5f, 0f, laserWidth, 18f), lanceDamage, new Vector2(x, 0f), 9f);
                }
                yield return new WaitForSeconds(0.45f);
            }
            yield return new WaitForSeconds(0.3f);
            pose = "";
        }

        // ---- Phase 3 skill mới: BÃO SÉT - cột sét báo trước rồi nổ lan ra (hoặc thu vào) hai bên boss theo nhịp
        IEnumerator ThunderSweep()
        {
            pose = "cast";
            FacePlayer();
            Sfx.Play("charge", 0.9f, 0.7f);
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                FacePlayer();
                Fx.Trail(Center + new Vector2(0f, 1.2f), Cfg.A(Cfg.Cyan, 0.8f), 0.4f + 1.4f * t, 0.12f);
                if (Random.value < 0.5f) Fx.Sparks(Center + new Vector2(0f, 1.5f), Vector2.up, Cfg.Cyan, 2, 8f, 80f, 0.3f);
                yield return null;
            }
            float cx = transform.position.x;
            bool outward = Random.value < 0.6f;
            int n = Mathf.Max(2, thunderSteps + (Enraged ? 2 : 0));
            float gap = Enraged ? thunderGap * 0.8f : thunderGap;
            Sfx.Play("warn", 0.8f, 1.5f);
            for (int i = 0; i < n; i++)
            {
                if (dead) yield break;
                int k = outward ? i : (n - 1 - i);
                float d = 2.4f + k * thunderSpacing;
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = cx + side * d;
                    if (x < Cfg.GateX + 1f || x > Cfg.LevelMaxX - 1f) continue;
                    StartCoroutine(ThunderColumn(x, thunderTelegraph));
                }
                yield return new WaitForSeconds(gap);
            }
            yield return new WaitForSeconds(thunderTelegraph + 0.4f);
            pose = "";
        }

        IEnumerator ThunderColumn(float x, float tel)
        {
            Rect zone = new Rect(x - thunderWidth * 0.5f, 0f, thunderWidth, 16f);
            Telegraph.Zone(zone, Cfg.Cyan, tel);
            yield return new WaitForSeconds(tel);
            if (dead || !GameManager.Active) yield break;
            Fx.Beam(new Vector2(x, 16f), new Vector2(x, 0f), Cfg.Cyan, 1.3f, 0.28f);
            Fx.Beam(new Vector2(x, 16f), new Vector2(x, 0f), Cfg.White, 0.45f, 0.2f);
            Fx.Sparks(new Vector2(x, 0.3f), Vector2.up, Cfg.Cyan, 10, 14f, 50f);
            Fx.Explosion(new Vector2(x, 0.4f), Cfg.Cyan, false);
            Fx.Shake(0.25f);
            Sfx.PlayAt("laser", new Vector2(x, 1f), 0.55f, 1.6f);
            if (PlayerAlive && zone.Overlaps(P.HurtRect)) Strike(thunderDamage, 9f, new Vector2(x, 0f));
            Minibot.HurtInRect(zone, thunderDamage, new Vector2(x, 0f), 9f);
        }

        // ---- Chuyển phase 2 -> 3: gục xuống, hồi đầy máu, bất động, nổ 3 lần (choáng người chơi), đứng dậy
        IEnumerator Transition3()
        {
            transitioning = true;
            p3Seq = true;
            if (brain != null) StopCoroutine(brain);
            hidden = false;
            crouch = 0f;
            pose = ""; moving = false;
            SetPos(transform.position.x, 0f);
            Projectile.ClearHostile();
            Sfx.Play("phase", 1f, 0.6f);
            Fx.Flash(Cfg.White, 0.5f);
            Fx.Shake(1f);

            // 1) gục xuống
            float t = 0f;
            float fall = Mathf.Max(0.2f, collapseTime);
            while (t < fall)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / fall);
                collapse = u * u * (3f - 2f * u);
                if (Random.value < 0.35f) Fx.Explosion(Center + new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(-1f, 1.5f)), Cfg.Orange, false);
                yield return null;
            }
            collapse = 1f;
            Fx.Explosion(Center, Cfg.Magenta, true);
            Fx.Ring(new Vector2(transform.position.x, 0.2f), Cfg.White, 1f, 14f, 0.6f);
            Sfx.Play("slam", 1f, 0.7f);
            Fx.Shake(0.9f);
            yield return new WaitForSeconds(0.35f);

            // 2) sang phase 3 và hồi đầy máu
            phase = 3;
            maxHp = p3Hp;
            hp = 0f;
            ApplyPhaseLook();
            GameManager.OnBossPhase3();
            Sfx.Play("charge", 1f, 0.5f);
            t = 0f;
            float refill = Mathf.Max(0.2f, refillTime);
            while (t < refill)
            {
                t += Time.deltaTime;
                hp = Mathf.Lerp(0f, maxHp, t / refill);
                Vector2 from = Center + new Vector2(Random.Range(-7f, 7f), Random.Range(0f, 6f));
                if (Random.value < 0.6f) Fx.Beam(from, Center, Cfg.A(Cfg.Lime, 0.8f), 0.12f, 0.25f);
                if (Random.value < 0.5f) Fx.Burst(Center, Cfg.Lime, 3, 5f, 0.4f, 0.3f);
                yield return null;
            }
            hp = maxHp;

            // 3) bất động tụ năng lượng
            t = 0f;
            float still = Mathf.Max(0.1f, stillTime);
            float ringT = 0f;
            Sfx.Play("warn", 0.9f, 0.6f);
            while (t < still)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / still);
                ringT -= Time.deltaTime;
                if (ringT <= 0f)
                {
                    ringT = Mathf.Lerp(0.5f, 0.15f, u);
                    Fx.Ring(Center, Cfg.A(Cfg.Orange, 0.9f), 8f, 1f, 0.45f);
                    Sfx.Play("charge", 0.4f, 0.8f + u);
                }
                Fx.Shake(0.12f + 0.3f * u);
                if (aura != null) aura.color = Cfg.A(Color.Lerp(Cfg.Orange, Cfg.White, u), 0.35f + 0.5f * u);
                Fx.Emit(Fx.S_Glow, Center + Random.insideUnitCircle.normalized * Random.Range(4f, 7f), Vector2.zero, new Vector2(0.4f, 0.4f), Vector2.zero,
                        Cfg.A(Cfg.Orange, 0.9f), Cfg.A(Cfg.White, 0f), 0.4f);
                yield return null;
            }

            // 4) nổ 3 lần
            for (int i = 0; i < 3; i++)
            {
                bool last = i == 2;
                float radius = blastRadius * (last ? finalBlastRadiusMul : 1f);
                float dmg = blastDamage * (last ? finalBlastDamageMul : 1f);
                Fx.Ring(Center, Cfg.A(Cfg.Red, 0.95f), 0.5f, radius * 2f, Mathf.Max(0.1f, blastWarn));
                Sfx.Play("warn", 0.9f, 1f + 0.2f * i);
                yield return new WaitForSeconds(Mathf.Max(0.1f, blastWarn));

                Vector2 c = Center;
                Fx.Explosion(c, last ? Cfg.White : Cfg.Orange, true);
                Fx.Explosion(c + new Vector2(2f, 1f), Cfg.Magenta, true);
                Fx.Explosion(c + new Vector2(-2f, 0.5f), Cfg.Orange, true);
                Fx.Ring(c, Cfg.White, 1f, radius * 2.4f, 0.6f);
                Fx.Ring(c, Cfg.A(Cfg.Orange, 0.8f), 1f, radius * 1.6f, 0.45f);
                Fx.Flash(Cfg.White, last ? 0.7f : 0.45f);
                Fx.Shake(last ? 1f : 0.8f);
                Fx.HitStop(0.05f);
                Sfx.Play(last ? "towerDie" : "slam", 1f, last ? 0.7f : 0.85f);
                if (PlayerAlive && Geo.CircleRect(c, radius, P.HurtRect))
                {
                    P.Hurt(dmg * p3DamageMul, c, 16f);
                    if (last && !P.Dead) P.Stun(stunTime);          // sóng xung kích cuối làm người chơi choáng
                }
                Minibot.HurtInRect(new Rect(c.x - radius, 0f, radius * 2f, radius), dmg, c, 12f);
                if (!last) yield return new WaitForSeconds(Mathf.Max(0.05f, blastGap));
            }
            yield return new WaitForSeconds(0.5f);

            // 5) đứng dậy
            t = 0f;
            float up = Mathf.Max(0.2f, standUpTime);
            Sfx.Play("bossRoar", 1f, 0.9f);
            while (t < up)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / up);
                collapse = 1f - u * u * (3f - 2f * u);
                if (Random.value < 0.4f) Fx.Sparks(Center, Vector2.up, Cfg.Orange, 3, 10f, 70f);
                yield return null;
            }
            collapse = 0f;
            Fx.Ring(Center, Cfg.Orange, 1f, 18f, 0.6f);
            Fx.Shake(0.7f);
            if (aura != null) aura.color = Cfg.A(Cfg.Orange, 0.3f);
            p3Seq = false;
            transitioning = false;
            brain = StartCoroutine(Brain());
        }

        // ------------------------------------------------------------ damage out / drain
        /// <summary>Gây sát thương lên người chơi; ở phase 2 hút máu theo tỉ lệ sát thương thực gây ra.</summary>
        void Strike(float dmg, float knock, Vector2? from = null)
        {
            Vector2 src = from.HasValue ? from.Value : (Vector2)transform.position;
            if (phase == 3) dmg *= p3DamageMul;
            float dealt = P.Hurt(dmg, src, knock);
            if (dealt > 0f) Drain(dealt);
        }

        public void Drain(float dealt)
        {
            float ls = Lifesteal;
            if (ls <= 0f || dead || P == null) return;
            float heal = dealt * ls;
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
                else if (phase == 2) StartCoroutine(Transition3());
                else StartCoroutine(DoomSequence());
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
            maxHp = hp = p2Hp;
            ApplyPhaseLook();
            Sfx.Play("bossRoar", 1f, 1.3f);
            Fx.Shake(0.8f);
            GameManager.OnBossPhase2();
            yield return new WaitForSeconds(1.5f);
            transitioning = false;
            brain = StartCoroutine(Brain());
        }

        // ---- Bảo hiểm: hết máu phase 3 -> dừng lại, nhấp nháy 7 màu rồi nổ khổng lồ, giết chắc người chơi
        IEnumerator DoomSequence()
        {
            doom = true;
            transitioning = true;
            invul = true;
            if (brain != null) StopCoroutine(brain);
            hidden = false;
            crouch = 0f;
            pose = ""; moving = false;
            SetPos(transform.position.x, 0f);
            Projectile.ClearHostile();
            GameManager.OnBossDoom();
            Sfx.Play("phase", 1f, 0.5f);
            Fx.Flash(Cfg.White, 0.5f);
            Fx.Shake(1f);

            Color[] rainbow = { Cfg.Red, Cfg.Orange, Cfg.Yellow, Cfg.Lime, Cfg.Cyan, Cfg.Purple, Cfg.Magenta };
            Vector3 basePos = vis != null ? vis.localPosition : Vector3.zero;
            float charge = Mathf.Max(1f, doomChargeTime);
            float t = 0f, colT = 0f, ringT = 0f, beepT = 0f;
            int ci = 0;
            while (t < charge)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / charge);
                colT -= Time.deltaTime;
                if (colT <= 0f) { colT = Mathf.Lerp(0.2f, 0.045f, u); ci = (ci + 1) % rainbow.Length; }
                Color col = rainbow[ci];
                if (flasher != null) flasher.Tint(col);
                if (aura != null) aura.color = Cfg.A(col, 0.5f + 0.4f * u);
                if (vis != null) vis.localPosition = basePos + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * (0.03f + 0.12f * u);

                ringT -= Time.deltaTime;
                if (ringT <= 0f)
                {
                    ringT = Mathf.Lerp(0.45f, 0.12f, u);
                    Fx.Ring(Center, Cfg.A(col, 0.9f), 9f + 6f * u, 1f, 0.5f);
                }
                beepT -= Time.deltaTime;
                if (beepT <= 0f)
                {
                    beepT = Mathf.Lerp(0.35f, 0.08f, u);
                    Sfx.Play("charge", 0.5f, 0.7f + 1.3f * u);
                }
                Fx.Emit(Fx.S_Glow, Center + Random.insideUnitCircle.normalized * Random.Range(3f, 8f), Vector2.zero, new Vector2(0.45f, 0.45f), Vector2.zero,
                        Cfg.A(rainbow[Random.Range(0, rainbow.Length)], 0.95f), Cfg.A(Cfg.White, 0f), 0.45f);
                Fx.Shake(0.15f + 0.6f * u);
                yield return null;
            }

            // ---- nổ: phủ kín khu boss, sát thương vô hạn, bỏ qua né / giáp
            if (vis != null) vis.localPosition = basePos;
            Vector2 c = Center;
            Fx.Flash(Cfg.White, 1f);
            Fx.HitStop(0.12f);
            Fx.Shake(1f);
            Fx.Explosion(c, Cfg.White, true);
            for (int i = 0; i < rainbow.Length; i++)
                Fx.Ring(c, rainbow[i], 1f, doomRadius * (0.45f + 0.12f * i), 0.7f + 0.08f * i);
            float x0 = Cfg.GateX, x1 = Cfg.LevelMaxX;
            for (int i = 0; i < 28; i++)
            {
                Vector2 e = new Vector2(Mathf.Lerp(x0, x1, Random.value), Random.Range(0f, 9f));
                Fx.Explosion(e, rainbow[i % rainbow.Length], i % 3 == 0);
                Fx.Burst(e, rainbow[(i + 3) % rainbow.Length], 8, 14f, 0.8f, 0.5f);
            }
            Sfx.Play("towerDie", 1f, 0.5f);
            Sfx.Play("slam", 1f, 0.6f);
            Sfx.Play("phase", 1f, 0.4f);
            bool killed = P != null && (P.Dead || (PlayerAlive && P.Kill(c)));
            Minibot.HurtInRect(new Rect(c.x - doomRadius, 0f, doomRadius * 2f, 40f), 99999f, c, 20f);
            if (!killed)
            {
                // chế độ thử nghiệm (phím P): người chơi không chết -> boss gục như thường và bạn thắng
                doom = false;
                StartCoroutine(DeathSequence());
                yield break;
            }
            if (vis != null) vis.gameObject.SetActive(false);
            if (aura != null) aura.gameObject.SetActive(false);
            dead = true;
            BaseMissions.BossKills++;       // hạ được máu boss: tính là đã hạ boss (nhiệm vụ của Albert)
            yield return new WaitForSeconds(0.6f);
            Destroy(gameObject);
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
                float bodyScale = phase == 3 ? p3Scale : 1f;
                float sy = (1f - crouch) * breathe * (1f - 0.5f * collapse) * bodyScale;
                float sx = (1f + crouch * 0.4f + 0.25f * collapse) * bodyScale;
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

            bool flick = transitioning && !p3Seq && !doom;
            float alpha = hidden ? 0.0f : (flick ? 0.65f + 0.35f * Mathf.Sin(Time.time * 30f) : 1f);
            if (Intro && transform.position.y > 15f) alpha = 0.3f;
            if (flasher != null && !doom) flasher.Apply(flashT > 0f || (flick && Mathf.Sin(Time.time * 24f) > 0.6f), Mathf.Clamp01(alpha));

            // sát thương khi chạm thân
            if (!dead && !invul && !transitioning && !hidden && PlayerAlive && contactCd <= 0f)
            {
                if (HitRect.Overlaps(P.HurtRect))
                {
                    contactCd = touchCooldown;
                    Strike(touchDamage, 9f);
                }
            }

            if (!dead && !invul && !transitioning && !hidden && GameManager.Active)
                Minibot.HurtInRect(HitRect, touchDamage, transform.position, 9f);

            // hạt khí lơ lửng
            if (!dead && Random.value < 0.3f)
            {
                Color c = phase == 1 ? Cfg.Purple : (phase == 2 ? Cfg.Red : Cfg.Orange);
                Fx.Emit(Fx.S_Glow, (Vector2)transform.position + new Vector2(Random.Range(-1.4f, 1.4f), Random.Range(0.3f, 3.6f)),
                        new Vector2(0f, Random.Range(0.5f, 2f)), new Vector2(0.4f, 0.4f), Vector2.zero, Cfg.A(c, 0.7f), Cfg.A(c, 0f), 0.6f);
            }
        }
    }
}
