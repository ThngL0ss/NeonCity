using UnityEngine;

namespace NeonBreaker
{
    public enum BaseMode { None, Dialogue, Craft, Fusion, Map, Stash, Missions }

    /// <summary>
    /// Các bảng giao diện của căn cứ (và bản đồ): hội thoại, bàn chế tác, máy ghép linh kiện, bản đồ.
    /// Khi một bảng mở: người chơi đứng yên (PlayerLocked) nhưng thế giới vẫn chạy (Albert vẫn đi lại). ESC hoặc F đóng.
    /// </summary>
    public static class BaseUi
    {
        public static BaseMode Mode = BaseMode.None;
        public static bool IsOpen { get { return Mode != BaseMode.None; } }

        public static void ResetStatics()
        {
            Mode = BaseMode.None;
            MapUi.ResetStatics();
            DialogueUi.Reset();
        }

        public static void Open(BaseMode m)
        {
            if (m == BaseMode.None || IsOpen) return;
            if (GameManager.State != GameState.Playing || GameManager.Paused) return;
            var p = Player.Instance;
            if (p == null || p.Dead) return;
            Mode = m;
            GameManager.PlayerLocked = true;
            Sfx.Play("click", 0.8f, 1.2f);
            if (m == BaseMode.Craft) CraftUi.OnOpen();
            else if (m == BaseMode.Fusion) FusionUi.OnOpen();
            else if (m == BaseMode.Map) MapUi.OnOpen();
            else if (m == BaseMode.Stash) StashUi.OnOpen();
        }

        public static void Close()
        {
            if (!IsOpen) return;
            var was = Mode;
            Mode = BaseMode.None;
            if (was == BaseMode.Stash) StashUi.OnClose();
            GameManager.PlayerLocked = false;
            Sfx.Play("click", 0.8f, 0.9f);
        }

        /// <summary>GameManager gọi mỗi khung hình khi có bảng đang mở.</summary>
        public static void UpdateInput()
        {
            switch (Mode)
            {
                case BaseMode.Dialogue: DialogueUi.UpdateInput(); break;
                case BaseMode.Craft: CraftUi.UpdateInput(); break;
                case BaseMode.Fusion: FusionUi.UpdateInput(); break;
                case BaseMode.Map: MapUi.UpdateInput(); break;
                case BaseMode.Stash: StashUi.UpdateInput(); break;
                case BaseMode.Missions: MissionUi.UpdateInput(); break;
            }
        }

        public static void Draw(Hud h)
        {
            switch (Mode)
            {
                case BaseMode.Dialogue: DialogueUi.Draw(h); break;
                case BaseMode.Craft: CraftUi.Draw(h); break;
                case BaseMode.Fusion: FusionUi.Draw(h); break;
                case BaseMode.Map: MapUi.Draw(h); break;
                case BaseMode.Stash: StashUi.Draw(h); break;
                case BaseMode.Missions: MissionUi.Draw(h); break;
            }
        }

        // ------------------------------------------------------------ tiện ích vẽ dùng chung
        /// <summary>Vẽ một đoạn thẳng (toạ độ màn hình) bằng Texture trắng xoay quanh điểm đầu.</summary>
        public static void Line(Hud h, Vector2 a, Vector2 b, Color c, float thick)
        {
            var m = GUI.matrix;
            float ang = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            float len = (b - a).magnitude;
            GUIUtility.RotateAroundPivot(ang, a);
            h.Fill(new Rect(a.x, a.y - thick * 0.5f, len, thick), c);
            GUI.matrix = m;
        }

        /// <summary>Có click chuột trái vào r trong khung hình này không (và tiêu thụ sự kiện).</summary>
        public static bool Clicked(Rect r)
        {
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition)) { e.Use(); return true; }
            return false;
        }
    }
}
