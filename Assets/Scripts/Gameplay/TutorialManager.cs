using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Màn hướng dẫn của ván mới: một con đường dài thẳng tắp dạy lần lượt
    ///   di chuyển → nhảy → lướt → chém → phi tiêu → hạ quái → chế tạo vài minibot → bước qua cánh cửa cuối đường (chạy cảnh mở đầu rồi vào màn chơi chính).
    /// Mỗi bước hiện hướng dẫn ở giữa phía trên màn hình; làm xong bước nào tự sang bước kế. Không có boss, không có hình phạt khi chết
    /// (chết = nạp lại màn hướng dẫn, đồ và skill giữ nguyên). Bấm Tab hai lần liên tiếp để bỏ qua toàn bộ huấn luyện.
    /// Bố cục là object thật trong Scene (nền, bục nhảy, quái đặt sẵn, rào chắn, cửa) — bạn kéo thả để sửa; các yêu cầu chỉnh trong Inspector.
    /// </summary>
    [DefaultExecutionOrder(20)]
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Inst;

        public enum Step { Move, Jump, Dash, Attack, Shuriken, Fight, Craft, Door }
        const int StepCount = 8;

        [Header("Mốc trong Scene (kéo object vào đây)")]
        public Transform limitLeft;
        public Transform limitRight;
        [Tooltip("Bước 1 xong khi người chơi đi tới mốc này")] public Transform moveGoal;
        [Tooltip("Bước 2 xong khi người chơi đứng trên bục ở mốc này (đặt ở mặt trên của bục)")] public Transform jumpGoal;

        [Header("Yêu cầu từng bước")]
        [Tooltip("Số lần chém (J) phải bấm")] public int attackPresses = 3;
        [Tooltip("Số lần ném phi tiêu (K) phải bấm")] public int shurikenPresses = 2;
        [Tooltip("Số minibot phải chế tạo (B ▸ Skill ▸ Chế tạo). Hệ thống huấn luyện tự cấp đủ linh kiện cho bước này")] public int minibotGoal = 2;

        [Header("Quái luyện tập (đặt sẵn trong Scene — phải hạ hết thì rào chắn mới mở)")]
        public Enemy[] enemies;

        [Header("Rào chắn sau khu quái (mở khi bước hạ quái xong)")]
        public BoxCollider2D gateCollider;
        public SpriteRenderer gateBar;
        public SpriteRenderer gateGlow;

        [Header("Cánh cửa cuối đường (khoá tới khi xong bước chế tạo)")]
        public PlaceGate door;

        public Step step;
        int count;
        int craftedStart;
        float skipT;
        bool skipping;
        float gateAlpha = 1f;
        bool gateClosed = true;

        public bool DoorOpen { get { return step == Step.Door; } }
        public string Title { get { return Cfg.L("HUẤN LUYỆN", "TRAINING"); } }

        void Awake()
        {
            Inst = this;
            GameFlow.InTutorial = true;                 // chết thì làm lại màn, không bị phạt
            GameFlow.CurrentPlaceId = GameFlow.TutorialScene;
            Cfg.MaxEnemiesAlive = 0;
            if (limitLeft != null) Cfg.LevelMinX = limitLeft.position.x;
            if (limitRight != null) Cfg.LevelMaxX = limitRight.position.x;
            Cfg.GateX = Cfg.LevelMaxX + 50f;
            Cfg.BossTriggerX = Cfg.GateX + 50f;
            Cfg.AltarX = Cfg.GateX;
            if (gateCollider != null) gateCollider.enabled = true;
        }

        void Start()
        {
            if (GameManager.State == GameState.Playing)
                GameManager.Banner(Cfg.L("HUẤN LUYỆN — làm theo hướng dẫn trên màn hình", "TRAINING — follow the on-screen steps"), Cfg.Cyan, 4f);
        }

        void OnDestroy() { if (Inst == this) Inst = null; }

        // ------------------------------------------------------------ tiến trình
        int Alive()
        {
            int n = 0;
            if (enemies != null) for (int i = 0; i < enemies.Length; i++) if (enemies[i] != null) n++;
            return n;
        }

        int TotalEnemies { get { return enemies != null ? enemies.Length : 0; } }

        void Next()
        {
            if (step == Step.Door) return;
            step = (Step)((int)step + 1);
            count = 0;
            Sfx.Play("pickup", 0.8f, 1.5f);
            Sfx.Play("charge", 0.4f, 1.8f);
            var p = Player.Instance;
            if (p != null) Fx.Burst(p.Center + new Vector2(0f, 0.6f), Cfg.Lime, 12, 5f, 0.5f, 0.25f);
            OnEnter();
        }

        void OnEnter()
        {
            switch (step)
            {
                case Step.Craft:
                    OpenGate();
                    if (!PrepareCraft()) Next();            // không có công thức nào để dạy: bỏ qua bước này
                    break;
                case Step.Door:
                    GameManager.Banner(Cfg.L("CỬA CUỐI ĐƯỜNG ĐÃ MỞ", "THE DOOR AT THE END IS OPEN"), Cfg.Lime, 3.5f);
                    break;
                default:
                    GameManager.Banner(Cfg.L("BƯỚC HOÀN THÀNH", "STEP COMPLETE"), Cfg.Lime, 1.4f);
                    break;
            }
        }

        /// <summary>Tìm 1 loại quái đã hạ có công thức, cấp đủ linh kiện cho minibotGoal con. false nếu không dạy chế tạo được.</summary>
        bool PrepareCraft()
        {
            if (minibotGoal <= 0 || !SkillSystem.HasConfig || !SkillSystem.Has(SkillSystem.IdParts)) return false;
            var list = SkillSystem.Config.craftables;
            CraftEntry e = null;
            for (int i = 0; i < list.Count && e == null; i++)
                if (list[i] != null && list[i].craftable && list[i].enemyPrefab != null && SkillSystem.HasDefeated(list[i].Id)) e = list[i];
            if (e == null) return false;
            Rank rk = SkillSystem.RankOf(e);
            if (!PowerRules.DropsParts(rk)) return false;
            int need = SkillSystem.CraftCost(e) * minibotGoal;
            int have = SkillSystem.Parts(rk);
            if (have < need) SkillSystem.AddParts(rk, need - have, true, false);
            craftedStart = SkillSystem.CraftedTotal;
            GameManager.Banner(Cfg.L("RÀO CHẮN ĐÃ MỞ — HỆ THỐNG TẶNG LINH KIỆN ĐỂ BẠN TẬP CHẾ TẠO", "BARRIER OPEN — THE SYSTEM GAVE YOU PARTS TO PRACTISE CRAFTING"), Cfg.Cyan, 4f);
            return true;
        }

        void OpenGate()
        {
            gateClosed = false;
            if (gateCollider != null) gateCollider.enabled = false;
            Sfx.Play("charge", 0.7f, 1.4f);
            if (gateBar != null) Fx.Flare(gateBar.transform.position, Cfg.Cyan, 5f, 0.5f);
        }

        void Update()
        {
            UpdateGateVisual();
            if (!GameManager.Active) return;
            var p = Player.Instance;
            if (p == null || p.Dead) return;
            bool free = !GameManager.PlayerLocked;

            switch (step)
            {
                case Step.Move:
                    if (moveGoal == null || p.Position.x >= moveGoal.position.x) Next();
                    break;
                case Step.Jump:
                    if (jumpGoal == null || (Mathf.Abs(p.Position.x - jumpGoal.position.x) < 3.5f && p.Position.y > jumpGoal.position.y - 0.6f)) Next();
                    break;
                case Step.Dash:
                    if (free && GameInput.DashDown) Next();
                    break;
                case Step.Attack:
                    if (free && GameInput.AttackDown && ++count >= attackPresses) Next();
                    break;
                case Step.Shuriken:
                    if (free && GameInput.ShurikenDown && ++count >= shurikenPresses) Next();
                    break;
                case Step.Fight:
                    if (Alive() == 0) Next();
                    break;
                case Step.Craft:
                    if (SkillSystem.CraftedTotal - craftedStart >= minibotGoal) Next();
                    break;
            }

            // bỏ qua huấn luyện: bấm Tab hai lần trong 3 giây
            if (skipT > 0f) skipT -= Time.deltaTime;
            if (free && !skipping && GameInput.MenuCycleDown)
            {
                if (skipT > 0f)
                {
                    skipping = true;
                    GameManager.PlayerLocked = true;
                    StartCoroutine(SkipRoutine());
                }
                else
                {
                    skipT = 3f;
                    GameManager.Banner(Cfg.L("Bấm Tab lần nữa để BỎ QUA huấn luyện", "Press Tab again to SKIP the training"), Cfg.Orange, 2.5f);
                }
            }
        }

        System.Collections.IEnumerator SkipRoutine()
        {
            Cine.SetFade(1f, 0.6f);
            float w = 0f;
            while (w < 0.7f) { w += Time.unscaledDeltaTime; yield return null; }
            GameFlow.FinishTutorial();
        }

        void UpdateGateVisual()
        {
            float target = gateClosed ? 1f : 0f;
            gateAlpha = Mathf.MoveTowards(gateAlpha, target, Time.deltaTime * 1.5f);
            float flick = gateClosed ? (0.8f + 0.2f * Mathf.Sin(Time.time * 22f) * Mathf.Sin(Time.time * 7f)) : 1f;
            if (gateBar != null) gateBar.color = Cfg.A(Color.white, gateAlpha * flick);
            if (gateGlow != null) gateGlow.color = Cfg.A(Cfg.Magenta, 0.45f * gateAlpha * flick);
        }

        // ------------------------------------------------------------ giao diện (Hud gọi mỗi khung hình khi đang chơi)
        public static void DrawHud(Hud h)
        {
            var t = Inst;
            if (t == null || GameManager.State != GameState.Playing || BaseUi.IsOpen) return;
            t.Draw(h);
        }

        void Draw(Hud h)
        {
            string head, body, prog;
            float frac;
            Info(out head, out body, out prog, out frac);

            Rect r = h.R(340, 52, 600, 112);
            h.Fill(r, new Color(0.01f, 0.02f, 0.06f, 0.82f));
            h.Outline(r, Cfg.A(Cfg.Cyan, 0.7f), 2f);
            h.Txt(h.R(354, 56, 420, 20), Cfg.L("HUẤN LUYỆN  ·  BƯỚC ", "TRAINING  ·  STEP ") + ((int)step + 1) + " / " + StepCount, 13, Cfg.A(Cfg.Cyan, 0.8f), TextAnchor.UpperLeft, false);
            h.Txt(h.R(354, 72, 572, 28), head, 22, Cfg.Yellow);
            h.TxtWrap(h.R(354, 100, 572, 44), body, 15, Color.white);
            if (frac >= 0f)
            {
                h.NeonBar(h.R(354, 146, 360, 10), frac, Cfg.Lime, new Color(0f, 0f, 0f, 0.6f));
                h.Txt(h.R(724, 140, 200, 20), prog, 14, Cfg.Lime, TextAnchor.UpperLeft, false);
            }
            h.Txt(h.R(690, 144, 240, 18), Cfg.L("[Tab ×2] bỏ qua huấn luyện", "[Tab ×2] skip training"), 12, Cfg.A(Color.white, 0.45f), TextAnchor.UpperRight, false);
        }

        void Info(out string head, out string body, out string prog, out float frac)
        {
            prog = "";
            frac = -1f;
            var p = Player.Instance;
            switch (step)
            {
                case Step.Move:
                    head = Cfg.L("DI CHUYỂN", "MOVE");
                    body = Cfg.L("Giữ [A] / [D] (hoặc mũi tên trái / phải) để chạy. Đi tới biển chỉ đường phía trước.", "Hold [A] / [D] (or the left / right arrows) to run. Walk to the signpost ahead.");
                    if (moveGoal != null && p != null) { float x0 = limitLeft != null ? limitLeft.position.x : 0f; frac = Mathf.Clamp01((p.Position.x - x0) / Mathf.Max(1f, moveGoal.position.x - x0)); prog = Mathf.RoundToInt(frac * 100f) + "%"; }
                    break;
                case Step.Jump:
                    head = Cfg.L("NHẢY", "JUMP");
                    body = Cfg.L("Bấm [SPACE] để nhảy — giữ càng lâu nhảy càng cao. Nhảy lên các bục để tới chỗ có biển.", "Press [SPACE] to jump — hold longer to jump higher. Hop up the platforms to the signpost.");
                    break;
                case Step.Dash:
                    head = Cfg.L("LƯỚT", "DASH");
                    body = Cfg.L("Bấm [I] để lướt nhanh một đoạn — dùng để né đòn. Lướt có thời gian hồi.", "Press [I] to dash a short distance — use it to dodge. The dash has a cooldown.");
                    break;
                case Step.Attack:
                    head = Cfg.L("CHÉM", "SLASH");
                    body = Cfg.L("Bấm [J] để chém cận chiến. Hãy chém " + attackPresses + " lần.", "Press [J] to slash in melee. Slash " + attackPresses + " times.");
                    frac = attackPresses > 0 ? (float)count / attackPresses : 1f;
                    prog = count + " / " + attackPresses;
                    break;
                case Step.Shuriken:
                    head = Cfg.L("PHI TIÊU", "SHURIKEN");
                    body = Cfg.L("Bấm [K] để ném phi tiêu tầm xa. Ném " + shurikenPresses + " lần. (Bấm [L] để hồi máu khi có pin hồi năng lượng.)", "Press [K] to throw a shuriken at range. Throw " + shurikenPresses + " times. (Press [L] to heal when you have a charge cell.)");
                    frac = shurikenPresses > 0 ? (float)count / shurikenPresses : 1f;
                    prog = count + " / " + shurikenPresses;
                    break;
                case Step.Fight:
                    head = Cfg.L("HẠ GỤC QUÁI", "DEFEAT THE ENEMIES");
                    body = Cfg.L("Tiến lên và hạ hết lũ quái. Quái rơi linh kiện, xu và dầu — đi lại gần để nhặt, vật phẩm trên đường thì bấm [F].", "Advance and defeat them all. They drop parts, coins and oil — walk close to collect; press [F] for items on the ground.");
                    frac = TotalEnemies > 0 ? 1f - (float)Alive() / TotalEnemies : 1f;
                    prog = (TotalEnemies - Alive()) + " / " + TotalEnemies;
                    break;
                case Step.Craft:
                    head = Cfg.L("CHẾ TẠO MINIBOT", "CRAFT MINIBOTS");
                    body = Cfg.L("Bấm [B] ▸ mục SKILL ▸ tab CHẾ TẠO, tạo " + minibotGoal + " minibot từ linh kiện. Sau đó bấm [H] để mở bảng minibot và triệu hồi chúng ra chiến đấu.", "Press [B] ▸ SKILL ▸ CRAFT tab and craft " + minibotGoal + " minibots from parts. Then press [H] to open the minibot panel and summon them.");
                    int got = Mathf.Clamp(SkillSystem.CraftedTotal - craftedStart, 0, minibotGoal);
                    frac = minibotGoal > 0 ? (float)got / minibotGoal : 1f;
                    prog = got + " / " + minibotGoal;
                    break;
                default:
                    head = Cfg.L("BƯỚC QUA CÁNH CỬA", "STEP THROUGH THE DOOR");
                    body = Cfg.L("Đi tới cánh cửa ở cuối đường và bấm [F] để vào màn chơi chính. Huấn luyện kết thúc.", "Walk to the door at the end of the road and press [F] to enter the main level. Training ends.");
                    break;
            }
        }

        void OnDrawGizmos()
        {
            if (moveGoal != null) { Gizmos.color = Color.green; Vector3 a = moveGoal.position; Gizmos.DrawLine(a, a + Vector3.up * 6f); }
            if (jumpGoal != null) { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(jumpGoal.position + Vector3.up, 1f); }
        }
    }
}
