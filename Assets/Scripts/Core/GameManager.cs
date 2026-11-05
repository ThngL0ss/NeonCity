using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonBreaker
{
    public enum GameState { Menu, Playing, Won, Lost }

    /// <summary>
    /// Điều phối toàn bộ game: trạng thái, kích hoạt boss, thắng / thua, tạm dừng, hit-stop.
    /// Đặt trên object "Systems" trong Scene (Build Project tạo sẵn).
    /// Thế giới (nền, mặt đất, trụ, người chơi...) là các object thật trong Scene - bạn tự kéo thả chỉnh được.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Inst;
        public static GameState State = GameState.Menu;
        public static bool Paused, PlayerLocked;
        /// <summary>Chế độ thử nghiệm (phím P): người chơi bất tử, sát thương nhân lên, không hồi chiêu.</summary>
        public static bool Cheat;
        /// <summary>Hệ số sát thương của người chơi: 1 bình thường, Cheat Damage Mul khi bật chế độ thử nghiệm.</summary>
        public static float DamageMul { get { return Cheat ? (Inst != null ? Inst.cheatDamageMul : 3f) : 1f; } }
        public static bool AutoStart;                     // true = bỏ qua menu (dùng khi "Chơi lại")

        public static bool Active { get { return State == GameState.Playing && !Paused; } }

        // thống kê
        public static int Kills, HealsUsed, TowersDestroyed, TowerTotal;
        public static float DamageTaken, PlayTime;
        public static bool BossSpawned;

        // thông báo giữa màn hình
        public static string BannerText = "";
        public static Color BannerColor = Color.white;
        public static float BannerT, BannerMax = 1f;

        [Header("Chế độ thử nghiệm - bấm P trong game để bật/tắt")]
        [Tooltip("Sát thương của người chơi nhân với số này khi bật chế độ thử nghiệm")]
        public float cheatDamageMul = 3f;

        [Header("Ngôn ngữ")]
        [Tooltip("Bật = tiếng Việt, tắt = tiếng Anh (trong game bấm V để đổi)")]
        public bool startVietnamese = true;

        [Header("Kho đồ & tiền")]
        [Tooltip("Số ô của kho đồ (phím B)")] public int inventorySlots = 24;
        [Tooltip("Số xu người chơi có lúc bắt đầu")] public int startCoins = 0;

        [Header("Boss")]
        [Tooltip("Prefab boss. Để trống = lấy trong ArtBank.")]
        public GameObject bossPrefab;

        Transform runtime;

        /// <summary>Đưa mọi biến static về trạng thái ban đầu (Bootstrap gọi trước mỗi lần Play, kể cả khi tắt Reload Domain).</summary>
        public static void ResetStatics()
        {
            Inst = null;
            State = GameState.Menu;
            Paused = false;
            PlayerLocked = false;
            Cheat = false;
            AutoStart = false;
            Kills = 0; HealsUsed = 0; TowersDestroyed = 0; TowerTotal = 0;
            DamageTaken = 0f; PlayTime = 0f;
            BossSpawned = false;
            BannerText = "";
            BannerColor = Color.white;
            BannerT = 0f;
            BannerMax = 1f;
            Inventory.ResetAll();
            SkillSystem.ResetAll();
            GameFlow.ResetStatics();
        }

        public static void Banner(string text, Color c, float dur = 3f)
        {
            BannerText = text;
            BannerColor = c;
            BannerT = dur;
            BannerMax = dur;
        }

        // ------------------------------------------------------------ setup
        void Awake()
        {
            Inst = this;
            Time.timeScale = 1f;
            if (!GameFlow.Carry)
            {
                Inventory.ResetAll();
                SkillSystem.ResetAll();
            }
            else
            {
                // chuyển cảnh Main <-> Base: giữ kho đồ / skill / minibot, chỉ đóng các bảng đang mở
                Inventory.IsOpen = false;
                SkillSystem.MenuOpen = false;
                SkillSystem.PanelOpen = false;
            }
            GameFlow.Carry = false;
            GameFlow.InBase = false;

            Cfg.Vietnamese = startVietnamese;

            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
            Physics2D.gravity = new Vector2(0f, -9.81f);
            Physics2D.IgnoreLayerCollision(Cfg.LayerPlayer, Cfg.LayerEnemy, true);
            Physics2D.IgnoreLayerCollision(Cfg.LayerEnemy, Cfg.LayerEnemy, true);
            Physics2D.IgnoreLayerCollision(Cfg.LayerPlayer, Cfg.LayerItem, true);
            Physics2D.IgnoreLayerCollision(Cfg.LayerEnemy, Cfg.LayerItem, true);

            Art.Init();
            Fx.Clear();

            // Nhóm trống chứa các đối tượng sinh ra khi chơi (Enemies, Projectiles, ...)
            var rt = new GameObject("Runtime");
            runtime = rt.transform;
            Level.Root = runtime;

            Kills = 0; HealsUsed = 0; TowersDestroyed = 0;
            DamageTaken = 0f; PlayTime = 0f;
            BossSpawned = false;
            Paused = false; PlayerLocked = false;
            BannerT = 0f;
            State = AutoStart ? GameState.Playing : GameState.Menu;
        }

        void Start()
        {
            TowerTotal = Tower.Towers.Count;
            var cam = Camera.main;
            if (cam != null) Sfx.Listener = cam.transform;
            Sfx.Music(0);
            if (State == GameState.Playing)
                Banner(Cfg.L("PHÁ HỦY TẤT CẢ TRỤ TRIỆU HỒI", "DESTROY ALL SUMMON TOWERS"), Cfg.Cyan, 4f);
            AutoStart = false;
        }

        void OnDestroy() { if (Inst == this) Inst = null; }

        // ------------------------------------------------------------ flow
        public void StartGame()
        {
            if (State != GameState.Menu) return;
            State = GameState.Playing;
            Sfx.Play("click", 1f);
            Sfx.Music(0);
            Banner(Cfg.L("PHÁ HỦY TẤT CẢ TRỤ TRIỆU HỒI", "DESTROY ALL SUMMON TOWERS"), Cfg.Cyan, 4f);
        }

        /// <summary>Chơi lại: nạp lại Scene hiện tại (mọi thứ trở về đúng như bạn đã đặt trong Scene).</summary>
        public void Restart()
        {
            Time.timeScale = 1f;
            AutoStart = true;
            GameFlow.Carry = false;       // chơi lại từ đầu: xoá sạch dữ liệu (kho, skill, minibot)
            Sfx.Play("click", 1f);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>Mở kho đồ (phím B). Game tạm dừng trong lúc mở.</summary>
        public static void OpenInventory()
        {
            if (State != GameState.Playing || Paused) return;
            var p = Player.Instance;
            if (p == null || p.Dead) return;
            Inventory.IsOpen = true;
            Inventory.Sel = Mathf.Clamp(Inventory.Sel, 0, Mathf.Max(0, Inventory.Capacity - 1));
            Paused = true;
            Sfx.Play("click", 0.8f, 1.2f);
        }

        public static void CloseInventory()
        {
            if (!Inventory.IsOpen) return;
            Inventory.IsOpen = false;
            Paused = false;
            Sfx.Play("click", 0.8f, 0.9f);
        }

        /// <summary>Mở mục Skill (phím N): cây skill, chế tạo, lưu trữ minibot. Game tạm dừng trong lúc mở.</summary>
        public static void OpenSkillMenu()
        {
            if (State != GameState.Playing || Paused) return;
            var p = Player.Instance;
            if (p == null || p.Dead) return;
            SkillSystem.MenuOpen = true;
            SkillSystem.PanelOpen = false;
            SkillSystem.SelBot = Mathf.Clamp(SkillSystem.SelBot, 0, Mathf.Max(0, SkillSystem.Bots.Count - 1));
            Paused = true;
            Sfx.Play("click", 0.8f, 1.2f);
        }

        public static void CloseSkillMenu()
        {
            if (!SkillSystem.MenuOpen) return;
            SkillSystem.MenuOpen = false;
            Paused = false;
            Sfx.Play("click", 0.8f, 0.9f);
        }

        public void TogglePause()
        {
            if (State != GameState.Playing) return;
            if (SkillSystem.MenuOpen) { CloseSkillMenu(); return; }
            if (Inventory.IsOpen) { CloseInventory(); return; }
            Paused = !Paused;
            Sfx.Play("click", 0.8f);
        }

        void Update()
        {
            GameInput.Poll();
            if (GameInput.LangDown) Cfg.Vietnamese = !Cfg.Vietnamese;
            if (GameInput.CheatDown)
            {
                Cheat = !Cheat;
                Banner(Cheat ? Cfg.L("CHẾ ĐỘ THỬ NGHIỆM: BẬT (bất tử, sát thương x" + cheatDamageMul + ", không hồi chiêu)", "TEST MODE: ON (invincible, x" + cheatDamageMul + " damage, no cooldowns)")
                             : Cfg.L("CHẾ ĐỘ THỬ NGHIỆM: TẮT", "TEST MODE: OFF"), Cfg.Yellow, 2.5f);
                Sfx.Play("click", 0.8f, Cheat ? 1.4f : 0.8f);
            }

            if (BannerT > 0f) BannerT -= Time.unscaledDeltaTime;

            switch (State)
            {
                case GameState.Menu:
                    if (GameInput.ConfirmDown) StartGame();
                    break;
                case GameState.Playing:
                    if (SkillSystem.MenuOpen)
                    {
                        if (GameInput.PauseDown || GameInput.SkillMenuDown) CloseSkillMenu();
                        else SkillUi.UpdateInput();
                    }
                    else if (Inventory.IsOpen)
                    {
                        if (GameInput.PauseDown || GameInput.InventoryDown) CloseInventory();
                        else InventoryUi.UpdateInput();
                    }
                    else if (SkillSystem.PanelOpen)
                    {
                        // bảng chọn minibot (H): đứng yên, A / D chọn ô, S triệu hồi / thu hồi, H hoặc Esc đóng
                        if (GameInput.PauseDown || GameInput.MinibotPanelDown) SkillSystem.TogglePanel();
                        else SkillUi.UpdatePanelInput();
                    }
                    else if (GameInput.PauseDown) TogglePause();
                    else if (Paused && GameInput.ConfirmDown) TogglePause();
                    else if (!Paused && GameInput.InventoryDown) OpenInventory();
                    else if (!Paused && GameInput.SkillMenuDown) OpenSkillMenu();
                    else if (!Paused && GameInput.MinibotPanelDown) SkillSystem.TogglePanel();
                    else if (!Paused && !PlayerLocked && GameInput.InteractDown) Interactor.TryInteract();
                    if (!Paused)
                    {
                        PlayTime += Time.deltaTime;
                        CheckBossTrigger();
                        Inventory.Tick(Time.deltaTime);
                        SkillSystem.Tick(Time.deltaTime);
                    }
                    break;
                case GameState.Won:
                case GameState.Lost:
                    if (GameInput.RestartDown) Restart();
                    break;
            }

            Inventory.TickUi(Time.unscaledDeltaTime);
            SkillSystem.TickUi(Time.unscaledDeltaTime);

            float ts = 1f;
            if (Paused) ts = 0f;
            else if (Fx.HitStopTimer > 0f) ts = 0.04f;
            Time.timeScale = ts;
        }

        void CheckBossTrigger()
        {
            if (BossSpawned) return;
            var p = Player.Instance;
            if (p == null || p.Dead) return;
            if (Tower.Remaining > 0) return;
            if (p.Position.x >= Cfg.BossTriggerX) SpawnBoss();
        }

        void SpawnBoss()
        {
            GameObject prefab = bossPrefab;
            if (prefab == null && Art.Bank != null) prefab = Art.Bank.bossPrefab;
            if (prefab == null)
            {
                Debug.LogError("Neon Breaker: chưa có prefab Boss. Gán vào GameManager ▸ Boss Prefab hoặc ArtBank ▸ Boss Prefab.");
                BossSpawned = true;
                return;
            }
            BossSpawned = true;
            if (Level.Instance != null) Level.Instance.CloseGate();
            Sfx.Music(1);
            Banner(Cfg.L("CẢNH BÁO — BOSS XUẤT HIỆN", "WARNING — BOSS INCOMING"), Cfg.Red, 4f);
            Fx.Flash(Cfg.Red, 0.4f);
            Sfx.Play("warn", 1f, 0.8f);
            var go = Instantiate(prefab, new Vector3(Cfg.AltarX, 22f, 0f), Quaternion.identity, runtime);
            go.name = "Boss";
            var boss = go.GetComponent<Boss>();
            if (boss != null) boss.StartIntro();
        }

        // ------------------------------------------------------------ events
        public static void OnTowerDestroyed(Tower t)
        {
            TowersDestroyed++;
            int left = Tower.Remaining;
            if (left > 0)
            {
                Banner(Cfg.L("TRỤ ĐÃ BỊ PHÁ HỦY  ", "TOWER DESTROYED  ") + TowersDestroyed + "/" + TowerTotal, Cfg.Magenta, 2.2f);
            }
            else
            {
                Banner(Cfg.L("CỔNG KHU BOSS ĐÃ MỞ — TIẾN VỀ PHÍA TRƯỚC", "BOSS GATE OPEN — ADVANCE"), Cfg.Cyan, 4.5f);
                if (Level.Instance != null) Level.Instance.OpenGate();
            }
        }

        public static void OnBossPhase2()
        {
            Banner(Cfg.L("PHASE 2 — NÓ ĐANG HÚT MÁU BẠN!", "PHASE 2 — IT DRAINS YOUR LIFE!"), Cfg.Red, 4f);
        }

        public static void OnBossDead()
        {
            Sfx.StopMusic();
            Sfx.Play("victory", 1f);
            Banner(Cfg.L("BOSS ĐÃ BỊ TIÊU DIỆT", "BOSS DESTROYED"), Cfg.Cyan, 3f);
            if (Inst != null) Inst.StartCoroutine(Inst.EndRoutine(GameState.Won, 2.6f));
        }

        public static void OnPlayerDead()
        {
            Sfx.StopMusic();
            Sfx.Play("defeat", 1f);
            if (Inst != null) Inst.StartCoroutine(Inst.EndRoutine(GameState.Lost, 1.8f));
        }

        IEnumerator EndRoutine(GameState result, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (State == GameState.Playing)
            {
                Paused = false;
                Inventory.IsOpen = false;
                SkillSystem.MenuOpen = false;
                SkillSystem.PanelOpen = false;
                State = result;
            }
        }
    }
}
