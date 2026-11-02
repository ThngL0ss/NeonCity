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
            ClickDown = Input.GetMouseButtonDown(0);
#endif
        }

        static void Clear()
        {
            Horizontal = 0f;
            DownHeld = JumpDown = JumpHeld = JumpUp = false;
            AttackDown = DashDown = ShurikenDown = HealDown = false;
            ConfirmDown = RestartDown = PauseDown = ClickDown = LangDown = CheatDown = false;
        }
    }
}
