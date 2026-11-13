using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace NeonBreaker
{
    /// <summary>
    /// Đọc phím một lần mỗi frame. Chạy được với cả Input Manager cũ lẫn Input System mới.
    /// Polled once per frame; works with the legacy Input Manager and the new Input System.
    /// </summary>
    public static class GameInput
    {
        public static float Horizontal;
        public static bool DownHeld;
        public static bool JumpDown, JumpHeld, JumpUp;
        public static bool AttackDown, DashDown, ShurikenDown, HealDown;
        public static bool ConfirmDown, RestartDown, PauseDown, ClickDown, LangDown, CheatDown;
        /// <summary>B: mở / đóng menu nhân vật (Kho đồ · Skill · Player) · F: TƯƠNG TÁC (mở rương, cửa, nói chuyện, dùng máy...) · X: vứt vật phẩm (trong kho) · mũi tên / WASD: di chuyển ô chọn.</summary>
        public static bool InventoryDown, InteractDown, DiscardDown, NavLeftDown, NavRightDown, NavUpDown, NavDownDown;
        /// <summary>H: mở / đóng bảng chọn minibot.</summary>
        public static bool MinibotPanelDown;
        /// <summary>Q / E: chuyển tab con trong mục Skill.</summary>
        public static bool TabPrevDown, TabNextDown;
        /// <summary>Trong menu B: phím 1 / 2 / 3 chọn thẳng mục (0 = Kho đồ, 1 = Skill, 2 = Player; -1 = không bấm) · phím Tab đổi sang mục kế tiếp.</summary>
        public static int MenuTabPick = -1;
        public static bool MenuCycleDown;

        public static void Poll()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var k = Keyboard.current;
            if (k == null) { Clear(); return; }

            float h = 0f;
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) h -= 1f;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) h += 1f;
            Horizontal = h;
            DownHeld = k.sKey.isPressed || k.downArrowKey.isPressed;

            JumpDown = k.spaceKey.wasPressedThisFrame;
            JumpHeld = k.spaceKey.isPressed;
            JumpUp = k.spaceKey.wasReleasedThisFrame;
            AttackDown = k.jKey.wasPressedThisFrame;
            DashDown = k.iKey.wasPressedThisFrame;
            ShurikenDown = k.kKey.wasPressedThisFrame;
            HealDown = k.lKey.wasPressedThisFrame;

            ConfirmDown = k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame;
            RestartDown = k.rKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame;
            PauseDown = k.escapeKey.wasPressedThisFrame;
            CheatDown = k.pKey.wasPressedThisFrame;
            LangDown = k.vKey.wasPressedThisFrame;
            InventoryDown = k.bKey.wasPressedThisFrame;
            InteractDown = k.fKey.wasPressedThisFrame;
            DiscardDown = k.xKey.wasPressedThisFrame;
            MinibotPanelDown = k.hKey.wasPressedThisFrame;
            MenuTabPick = (k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame) ? 0
                        : (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame) ? 1
                        : (k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame) ? 2 : -1;
            MenuCycleDown = k.tabKey.wasPressedThisFrame;
            TabPrevDown = k.qKey.wasPressedThisFrame;
            TabNextDown = k.eKey.wasPressedThisFrame;
            NavLeftDown = k.aKey.wasPressedThisFrame || k.leftArrowKey.wasPressedThisFrame;
            NavRightDown = k.dKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame;
            NavUpDown = k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame;
            NavDownDown = k.sKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame;
            var m = Mouse.current;
            ClickDown = m != null && m.leftButton.wasPressedThisFrame;
#else
            float h = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
            Horizontal = h;
            DownHeld = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

            JumpDown = Input.GetKeyDown(KeyCode.Space);
            JumpHeld = Input.GetKey(KeyCode.Space);
            JumpUp = Input.GetKeyUp(KeyCode.Space);
            AttackDown = Input.GetKeyDown(KeyCode.J);
            DashDown = Input.GetKeyDown(KeyCode.I);
            ShurikenDown = Input.GetKeyDown(KeyCode.K);
            HealDown = Input.GetKeyDown(KeyCode.L);

            ConfirmDown = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
            RestartDown = Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return);
            PauseDown = Input.GetKeyDown(KeyCode.Escape);
            CheatDown = Input.GetKeyDown(KeyCode.P);
            LangDown = Input.GetKeyDown(KeyCode.V);
            InventoryDown = Input.GetKeyDown(KeyCode.B);
            InteractDown = Input.GetKeyDown(KeyCode.F);
            DiscardDown = Input.GetKeyDown(KeyCode.X);
            MinibotPanelDown = Input.GetKeyDown(KeyCode.H);
            MenuTabPick = (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) ? 0
                        : (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) ? 1
                        : (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) ? 2 : -1;
            MenuCycleDown = Input.GetKeyDown(KeyCode.Tab);
            TabPrevDown = Input.GetKeyDown(KeyCode.Q);
            TabNextDown = Input.GetKeyDown(KeyCode.E);
            NavLeftDown = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
            NavRightDown = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
            NavUpDown = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
            NavDownDown = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
            ClickDown = Input.GetMouseButtonDown(0);
#endif
        }

        static void Clear()
        {
            Horizontal = 0f;
            DownHeld = JumpDown = JumpHeld = JumpUp = false;
            AttackDown = DashDown = ShurikenDown = HealDown = false;
            ConfirmDown = RestartDown = PauseDown = ClickDown = LangDown = CheatDown = false;
            InventoryDown = InteractDown = DiscardDown = NavLeftDown = NavRightDown = NavUpDown = NavDownDown = false;
            MinibotPanelDown = TabPrevDown = TabNextDown = MenuCycleDown = false;
            MenuTabPick = -1;
        }
    }
}
