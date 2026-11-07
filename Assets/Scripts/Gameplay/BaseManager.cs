using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Điều phối Scene Base (căn cứ ngầm của Albert): đặt giới hạn, chạy cảnh hồi sinh trong lồng kính khi người chơi về căn cứ,
    /// đoạn thoại lần đầu với Albert, và các cuộc nói chuyện / nhiệm vụ sau đó.
    /// Scene Base do menu Tools ▸ Neon Breaker ▸ Cập nhật cấp bậc + dầu + căn cứ tạo ra (không đụng Scene Main).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class BaseManager : MonoBehaviour
    {
        public static BaseManager Inst;

        [Header("Các mốc trong Scene (kéo object vào đây)")]
        public Transform limitLeft;
        public Transform limitRight;
        [Tooltip("Nơi người chơi xuất hiện (trong lồng kính hồi sinh)")] public ResurrectionCapsule capsule;
        public AlbertNpc albert;
        [Tooltip("Cửa ra của căn cứ")] public BaseStation exitDoor;

        float dustT;

        void Awake()
        {
            Inst = this;
            GameFlow.InBase = true;
            GameFlow.BaseVisited = true;
            if (limitLeft != null) Cfg.LevelMinX = limitLeft.position.x;
            if (limitRight != null) Cfg.LevelMaxX = limitRight.position.x;
            Cfg.GateX = Cfg.LevelMaxX + 500f;
            Cfg.BossTriggerX = Cfg.GateX + 500f;
        }

        void OnDestroy() { if (Inst == this) Inst = null; }

        void Start()
        {
            StartCoroutine(Begin());
        }

        // ------------------------------------------------------------ vào căn cứ
        IEnumerator Begin()
        {
            yield return null;
            if (GameManager.State == GameState.Menu) GameManager.State = GameState.Playing;      // chạy thẳng Scene Base trong Unity để thử
            var pl = Player.Instance;
            if (pl == null) yield break;

            if (capsule != null) pl.transform.position = new Vector3(capsule.transform.position.x, 0.05f, 0f);
            if (CameraRig.Inst != null) CameraRig.Inst.Snap();
            if (albert != null && capsule != null) albert.Face(capsule.transform.position.x);

            if (GameManager.State == GameState.Playing)
            {
                GameManager.State = GameState.Cinematic;
                Cine.Play(Resurrect(pl));
                while (Cine.Active) yield return null;
                GameManager.State = GameState.Playing;
            }

            if (!GameFlow.BaseDialogDone)
            {
                GameFlow.BaseDialogDone = true;
                if (albert != null) albert.StartTalk(pl.Position.x);
                bool done = false;
                DialogueUi.Begin(FirstArrivalLines(), () => done = true);
                while (!done) yield return null;
                if (albert != null) albert.EndTalk();
            }
            else if (albert != null)
            {
                albert.Mutter(AfterDeathLine(), AfterDeathLineEn());
            }
            GameManager.Banner(Cfg.L("CĂN CỨ — BẤM F ĐỂ TƯƠNG TÁC", "BASE — PRESS F TO INTERACT"), Cfg.Orange, 3.5f);
        }

        string AfterDeathLine()
        {
            string[] l = { "Lại nữa à? Dữ liệu của cậu mờ thêm rồi đấy.", "Cậu sống dai thật... nhưng đừng thử vận may mãi.", "Đừng lao vào mà không chuẩn bị, XbAst-013!" };
            return l[Random.Range(0, l.Length)];
        }

        string AfterDeathLineEn()
        {
            string[] l = { "Again? Your data blurred a little more.", "You're stubborn... but don't push your luck.", "Don't rush in unprepared, XbAst-013!" };
            return l[Random.Range(0, l.Length)];
        }

        List<DlgLine> FirstArrivalLines()
        {
            Color a = Cfg.Yellow, p = Cfg.Cyan;
            return new List<DlgLine>
            {
                new DlgLine("ALBERT", Cfg.L("Chào mừng cậu trở về, XbAst-013. Cậu còn nhớ tôi không?",
                                            "Welcome back, XbAst-013. Do you still remember me?"), a),
                new DlgLine("XbAst-013", Cfg.L("Chào Albert, đừng đùa nữa, tôi không thể mất trí nổi đâu!",
                                               "Hi Albert, quit joking, there's no way I'd lose my mind!"), p),
                new DlgLine("ALBERT", Cfg.L("Nghiêm túc mà nói, cậu đang tự giết mình đấy! Mỗi lần cậu tái tạo là một lần dữ liệu của cậu mờ đi, ta chỉ có thể tái tạo cơ thể cho cậu chứ không thể tái cấu trúc dữ liệu được đâu",
                                            "Seriously, you're killing yourself! Every time you regenerate, your data fades a little. I can rebuild your body, but I can't restructure your data."), a),
                new DlgLine("XbAst-013", Cfg.L("Yên tâm đi lão già, không có lần sau đâu",
                                               "Don't worry, old man, there won't be a next time."), p),
            };
        }

        // ------------------------------------------------------------ nói chuyện với Albert (bấm F)
        public void TalkToAlbert()
        {
            var pl = Player.Instance;
            if (albert == null || pl == null) return;
            albert.StartTalk(pl.Position.x);
            Color a = Cfg.Yellow;
            var lines = new List<DlgLine>();

            if (!BaseMissions.StoryTold)
            {
                BaseMissions.StoryTold = true;
                lines.Add(new DlgLine("ALBERT", Cfg.L("Để ta nhắc lại cho cậu, phòng khi dữ liệu lại mờ thêm: cậu là XbAst-013 — bộ não của cậu được số hóa vào vi máy tính, và giữa cậu với ta có một mối liên kết mà không cỗ máy nào thay thế được.",
                                                      "Let me remind you, in case your data blurred again: you are XbAst-013 — your brain was digitized into a micro-computer, and there is a bond between you and me that no machine could replace."), a));
                lines.Add(new DlgLine("ALBERT", Cfg.L("Linh kiện bậc SS — " + CineScripts.StolenItem + " — là lõi của cậu. OVERLORD đã cướp nó và giam mình trong khu boss cuối thành phố.",
                                                      "Your SS-rank part — the " + CineScripts.StolenItem + " — is your core. OVERLORD stole it and locked itself in the boss zone at the far end of the city."), a));
                lines.Add(new DlgLine("ALBERT", Cfg.L("Đường tới đó bị 6 trụ triệu hồi chặn lại. Hạ quái để lấy linh kiện, nhặt dầu, rồi quay về đây — ta ghép và chế tác giúp cậu.",
                                                      "The way there is blocked by 6 summon towers. Take down enemies for parts, collect oil, then come back — I'll fuse and craft for you."), a));
            }

            var claimed = BaseMissions.TryClaim();
            if (claimed != null)
                lines.Add(new DlgLine("ALBERT", Cfg.L("Xuất sắc! Xong nhiệm vụ \"" + claimed.Title + "\". Nhận lấy: +" + claimed.rewardCoins + " xu, +" + claimed.rewardOil + " dầu.",
                                                      "Excellent! \"" + claimed.Title + "\" complete. Take this: +" + claimed.rewardCoins + " coins, +" + claimed.rewardOil + " oil."), a));

            var cur = BaseMissions.Current;
            if (cur == null)
                lines.Add(new DlgLine("ALBERT", Cfg.L("Ta hết việc để giao cho cậu rồi. Giờ chỉ còn lấy lại lõi của cậu thôi!", "I'm out of tasks for you. All that's left is taking your core back!"), a));
            else if (claimed != null)
                lines.Add(new DlgLine("ALBERT", Cfg.L("Nhiệm vụ mới: " + cur.Title + ". " + cur.Desc, "New task: " + cur.Title + ". " + cur.Desc), a));
            else
                lines.Add(new DlgLine("ALBERT", Cfg.L("Nhiệm vụ hiện tại: " + cur.Title + " (" + cur.Progress + "/" + cur.target + "). " + cur.Desc,
                                                      "Current task: " + cur.Title + " (" + cur.Progress + "/" + cur.target + "). " + cur.Desc), a));

            if (claimed == null && BaseMissions.StoryTold && Random.value < 0.6f)
            {
                string vi, en;
                Tip(out vi, out en);
                lines.Add(new DlgLine("ALBERT", Cfg.L(vi, en), a));
            }
            DialogueUi.Begin(lines, () => { if (albert != null) albert.EndTalk(); });
        }

        static void Tip(out string vi, out string en)
        {
            string[] v =
            {
                "Linh kiện bậc thấp ghép được lên bậc cao ở máy ghép, nhưng bậc A và S thì không — chúng chỉ rơi từ quái rất mạnh.",
                "Dầu là nhiên liệu của mọi thứ ở đây. Thùng dầu nằm dọc đường, rương cũng có dầu.",
                "Bàn chế tác làm được thuốc hồi máu và huyết thanh tăng sức. Nguyên liệu cứ nhặt hết đi, đừng bỏ phí.",
                "Mỗi trụ triệu hồi càng về cuối thì quái càng cao cấp. Đừng coi thường cấp độ.",
                "Bấm N để mở hệ thống Skill, H để chọn minibot. Minibot chỉ lưu trữ và sửa chữa được ở căn cứ.",
            };
            string[] e =
            {
                "Low-rank parts fuse into higher ranks at the machine, but A and S can't be fused — they only drop from very strong enemies.",
                "Oil fuels everything here. Oil cans lie along the road, and chests hold some too.",
                "The crafting bench makes healing vials and power serums. Pick up all the materials, don't waste them.",
                "The closer a tower is to the end, the higher-level its enemies. Don't underestimate levels.",
                "Press N for the Skill system, H to pick a minibot. Minibots can only be stored and repaired at the base.",
            };
            int i = Random.Range(0, v.Length);
            vi = v[i]; en = e[i];
        }

        // ------------------------------------------------------------ cảnh hồi sinh
        IEnumerator Resurrect(Player pl)
        {
            if (capsule == null) { yield return Cine.Wait(0.5f); yield break; }
            Vector2 cp = capsule.Center;
            Cine.SetBars(1f, 0.8f);
            Cine.Focus(cp + new Vector2(0f, 2.2f), 1.2f);
            if (CameraRig.Inst != null) CameraRig.Inst.Snap();

            var sr = pl.FreezeVisual("idle");
            PixelDissolve d = sr != null ? PixelDissolve.Begin(sr, true, 0.45f, false) : null;
            capsule.SetCharge(0f);
            if (albert != null) albert.Face(cp.x);
            yield return Cine.Wait(1.5f);

            // ---- sét giáng từ trên xuống vào lồng kính
            for (int i = 0; i < 3; i++)
            {
                Bolt(capsule.Top);
                capsule.SetCharge(0.35f + 0.25f * i);
                yield return Cine.Wait(i < 2 ? 0.6f : 0.35f);
            }

            // ---- lồng toả sét, người chơi được ghép dần từ vô số mảnh linh kiện
            const float dur = 3.4f;
            float t = 0f, arcT = 0f;
            Sfx.Play("charge", 0.8f, 0.6f);
            while (t < dur && !Cine.Skipping)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / dur);
                capsule.SetCharge(0.6f + 0.4f * u);
                arcT -= Time.unscaledDeltaTime;
                if (arcT <= 0f) { arcT = Mathf.Lerp(0.08f, 0.04f, u); Arc(cp); }
                if (d != null)
                {
                    d.Step(Mathf.Clamp01(u * 1.04f), 0);
                    d.Converge(u, 5f, 0.55f, 3);
                }
                if (Random.value < 0.85f)
                {
                    Vector2 from = cp + Random.insideUnitCircle.normalized * Random.Range(4f, 8f) + new Vector2(0f, 1.5f);
                    Vector2 to = pl.Center + new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.8f, 0.8f));
                    RobotBit.Fly(from, to, 0.7f);
                }
                Fx.Shake(0.1f + 0.2f * u);
                yield return null;
            }
            if (d != null) { d.Step(1f, 0); }

            // ---- chớp sáng: hồi sinh xong
            Fx.Flash(Cfg.White, 0.9f);
            Fx.Explosion(cp, Cfg.Cyan, true);
            Fx.Ring(cp, Cfg.White, 1f, 16f, 0.7f);
            Fx.Shake(0.9f);
            Sfx.Play("slam", 0.8f, 1.2f);
            Sfx.Play("spawn", 0.7f, 1.5f);
            capsule.Open();
            if (d != null) d.End(true);
            pl.ReleaseVisual();
            yield return Cine.Wait(0.9f);
            Cine.SetBars(0f, 0.6f);
            Cine.ReleaseFocus();
            Cine.CamSmooth = 1f;
            yield return Cine.Wait(0.4f);
        }

        void Bolt(Vector2 end)
        {
            Vector2 top = new Vector2(end.x + Random.Range(-1.5f, 1.5f), end.y + 14f);
            Vector2 prev = top;
            const int seg = 9;
            for (int i = 1; i <= seg; i++)
            {
                Vector2 p = Vector2.Lerp(top, end, i / (float)seg);
                if (i < seg) p.x += Random.Range(-1.3f, 1.3f);
                Fx.Beam(prev, p, Cfg.White, 0.5f, 0.3f);
                Fx.Beam(prev, p, Cfg.Cyan, 1.5f, 0.3f);
                prev = p;
            }
            Fx.Flash(Cfg.White, 0.6f);
            Fx.Shake(1f);
            Fx.Explosion(end, Cfg.Cyan, false);
            Fx.Ring(end, Cfg.A(Cfg.White, 0.9f), 0.5f, 7f, 0.4f);
            Sfx.Play("laser", 1f, 0.8f);
            Sfx.Play("slam", 0.7f, 1.3f);
        }

        void Arc(Vector2 c)
        {
            Vector2 a = c + Random.insideUnitCircle * 0.5f;
            float ang = Random.Range(0f, Mathf.PI * 2f);
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            float len = Random.Range(2.5f, 6.5f);
            Vector2 mid = a + dir * len * 0.5f + new Vector2(-dir.y, dir.x) * Random.Range(-0.7f, 0.7f);
            Vector2 b = a + dir * len;
            Fx.Beam(a, mid, Cfg.Cyan, 0.16f, 0.14f);
            Fx.Beam(mid, b, Cfg.Cyan, 0.16f, 0.14f);
            Fx.Sparks(b, dir, Cfg.Cyan, 2, 6f, 60f, 0.2f);
        }

        // ------------------------------------------------------------ không khí của căn cứ
        void Update()
        {
            dustT -= Time.deltaTime;
            var cam = Camera.main;
            if (dustT <= 0f && cam != null)
            {
                dustT = 0.12f;
                Vector3 cp = cam.transform.position;
                float hw = cam.orthographicSize * cam.aspect;
                Color c = Random.value < 0.6f ? new Color(1f, 0.85f, 0.55f, 1f) : Cfg.Cyan;
                Fx.Emit(Fx.S_Glow, new Vector2(cp.x + Random.Range(-hw, hw), cp.y + Random.Range(-cam.orthographicSize, cam.orthographicSize)),
                        new Vector2(Random.Range(-0.15f, 0.15f), Random.Range(-0.3f, 0.2f)), new Vector2(0.14f, 0.14f), Vector2.zero, Cfg.A(c, 0.5f), Cfg.A(c, 0f), Random.Range(2f, 4f));
            }
        }
    }
}
