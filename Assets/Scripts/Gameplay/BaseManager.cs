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

            bool normal = GameFlow.NormalArrival;          // về từ bản đồ (không phải bị hạ gục): xuất hiện ở cửa ra, không có cảnh hồi sinh
            GameFlow.NormalArrival = false;
            if (normal && exitDoor != null)
            {
                float dx = exitDoor.transform.position.x;
                float toward = capsule != null && capsule.transform.position.x < dx ? -1f : 1f;      // đứng phía trong căn cứ, cạnh cửa
                pl.transform.position = new Vector3(dx + toward * 2.4f, 0.05f, 0f);
                if (capsule != null) capsule.Open();
                if (albert != null) albert.Face(pl.transform.position.x);
            }
            else if (capsule != null) pl.transform.position = new Vector3(capsule.transform.position.x, 0.05f, 0f);
            if (CameraRig.Inst != null) CameraRig.Inst.Snap();
            if (!normal && albert != null && capsule != null) albert.Face(capsule.transform.position.x);

            if (!normal && GameManager.State == GameState.Playing)
            {
                GameManager.State = GameState.Cinematic;
                Cine.Play(Resurrect(pl));
                while (Cine.Active) yield return null;
                GameManager.State = GameState.Playing;
            }

            if (normal) { }
            else if (!GameFlow.BaseDialogDone)
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
            DeathPenalty.ShowReport();      // bảng "đã mất gì" sau khi chết (nếu có)
            if (!BaseMissions.FirstMissionReceived)
            {
                // lần hồi sinh đầu: chưa nhận nhiệm vụ thì chưa ra khỏi căn cứ được — chỉ đường tới Albert (BaseGuide)
                string tgt = BaseMissions.StoryTold && MissionBoard.Exists ? Cfg.L("ĐẾN HỘP THƯ NHIỆM VỤ, BẤM F", "GO TO THE MISSION MAILBOX, PRESS F")
                                                                          : Cfg.L("ĐẾN NÓI CHUYỆN VỚI ALBERT, BẤM F", "GO TALK TO ALBERT, PRESS F");
                GameManager.Banner(tgt, Cfg.Yellow, 5f);
            }
            else GameManager.Banner(Cfg.L("CĂN CỨ — BẤM F ĐỂ TƯƠNG TÁC", "BASE — PRESS F TO INTERACT"), Cfg.Orange, 3.5f);
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

            bool first = !BaseMissions.StoryTold;
            if (first)
            {
                BaseMissions.StoryTold = true;
                lines.Add(new DlgLine("ALBERT", Cfg.L("Ta không thể cứ mãi cho cậu tái sinh miễn phí được. Tự ra ngoài kiếm nguyên liệu về đây cho tôi. Không thì đừng mong có lần sau nhé!",
                                                      "I can't keep regenerating you for free forever. Go out and get the materials and bring them back to me. Otherwise, don't expect a next time!"), a));
                lines.Add(new DlgLine("XbAst-013", Cfg.L("Hừ, đúng là lão già khó ưa.", "Hmph, what a cranky old man."), Cfg.Cyan));
            }

            var claimed = first ? null : BaseMissions.TryClaim();
            if (claimed != null)
            {
                string rw = MissionUi.RewardText(claimed);
                string body = !string.IsNullOrEmpty(claimed.claimVi)
                    ? Cfg.L(claimed.claimVi, claimed.claimEn)
                    : Cfg.L("Xuất sắc! Xong nhiệm vụ \"" + claimed.Title + "\".", "Excellent! \"" + claimed.Title + "\" complete.");
                if (rw.Length > 0) body += Cfg.L(" Nhận lấy: ", " Take this: ") + rw + ".";
                lines.Add(new DlgLine("ALBERT", body, a));
            }

            var cur = BaseMissions.Current;
            if (!MissionBoard.Exists && cur != null && !BaseMissions.Accepted && !first)
                BaseMissions.Accepted = true;      // Scene Base chưa có hộp thư: nhiệm vụ được nhận tự động

            if (first)
            {
                // sau cuộc nói chuyện đầu tiên: hộp thư nhiệm vụ xuất hiện (xem DialogueUi.Begin bên dưới)
            }
            else if (cur == null)
                lines.Add(new DlgLine("ALBERT", Cfg.L("Ta hết việc để giao cho cậu rồi. Giờ chỉ còn lấy lại lõi của cậu thôi!", "I'm out of tasks for you. All that's left is taking your core back!"), a));
            else if (!BaseMissions.Accepted)
                lines.Add(new DlgLine("ALBERT", Cfg.L((claimed != null ? "Việc mới đã có ở hộp thư nhiệm vụ. " : "Có việc cho cậu ở hộp thư nhiệm vụ. ") + "Ra đó xem rồi nhận đi.",
                                                      (claimed != null ? "There's a new job in the mission mailbox. " : "There's a job waiting in the mission mailbox. ") + "Go take a look and accept it."), a));
            else
                lines.Add(new DlgLine("ALBERT", Cfg.L("Nhiệm vụ hiện tại: " + cur.Title + " — " + cur.ProgressText + ".",
                                                      "Current mission: " + cur.Title + " — " + cur.ProgressText + "."), a));

            if (!first && claimed == null && Random.value < 0.6f)
            {
                string vi, en;
                Tip(out vi, out en);
                lines.Add(new DlgLine("ALBERT", Cfg.L(vi, en), a));
            }
            DialogueUi.Begin(lines, () =>
            {
                if (albert != null) albert.EndTalk();
                if (first)
                {
                    BaseMissions.UnlockBoard();                       // hộp thư nhiệm vụ hiện ra (MissionBoard chạy hiệu ứng + chỉ đường)
                    if (!MissionBoard.Exists)
                    {
                        BaseMissions.BoardJustUnlocked = false;
                        BaseMissions.Accepted = true;                 // chưa có hộp thư trong Scene: nhận nhiệm vụ luôn
                        GameManager.Banner(Cfg.L("NHIỆM VỤ MỚI: " + (BaseMissions.Current != null ? BaseMissions.Current.Title : ""), "NEW MISSION: " + (BaseMissions.Current != null ? BaseMissions.Current.Title : "")), Cfg.Yellow, 5f);
                    }
                }
            });
        }

        static void Tip(out string vi, out string en)
        {
            string[] v =
            {
                "Linh kiện bậc thấp ghép được lên bậc cao ở máy ghép, nhưng bậc A và S thì không — chúng chỉ rơi từ quái rất mạnh.",
                "Dầu là nhiên liệu của mọi thứ ở đây. Thùng dầu nằm dọc đường, rương cũng có dầu.",
                "Bàn chế tác làm được thuốc hồi máu và huyết thanh tăng sức. Nguyên liệu cứ nhặt hết đi, đừng bỏ phí.",
                "Mỗi trụ triệu hồi càng về cuối thì quái càng cao cấp. Đừng coi thường cấp độ.",
                "Bấm B rồi chọn mục Skill để mở hệ thống Skill, bấm H để chọn minibot. Minibot chỉ lưu trữ và sửa chữa được ở căn cứ.",
            };
            string[] e =
            {
                "Low-rank parts fuse into higher ranks at the machine, but A and S can't be fused — they only drop from very strong enemies.",
                "Oil fuels everything here. Oil cans lie along the road, and chests hold some too.",
                "The crafting bench makes healing vials and power serums. Pick up all the materials, don't waste them.",
                "The closer a tower is to the end, the higher-level its enemies. Don't underestimate levels.",
                "Press B and open the Skill tab for the Skill system, H to pick a minibot. Minibots can only be stored and repaired at the base.",
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
