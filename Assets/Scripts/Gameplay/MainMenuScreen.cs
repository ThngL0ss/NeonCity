using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Đặt trong Scene MainMenu: bật menu chính (MainMenuUi) khi Scene nạp và tắt khi rời Scene.
    /// Scene này chỉ cần Camera + GameManager + Hud + (Sfx) — menu Editor "Cập nhật Nhiệm vụ + Phố Neon + Lưu game" tạo sẵn.
    ///
    /// Menu được VẼ Ở ĐÂY (OnGUI riêng, GUI.depth rất nhỏ = nằm trên cùng mọi OnGUI khác như PixelScreen / Hud), không phụ thuộc thứ tự vẽ của Hud.
    /// Nếu việc vẽ menu bị lỗi, thông báo lỗi hiện thẳng trên màn hình (và ở Console) thay vì để màn hình đen.
    /// Menu chỉ hiện khi chạy game (bấm Play) — ở chế độ Edit, Game view đen là bình thường vì menu vẽ bằng IMGUI lúc chạy.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class MainMenuScreen : MonoBehaviour
    {
        public static MainMenuScreen Inst;

        /// <summary>true = MainMenuScreen đang tự vẽ menu (Hud bỏ qua phần vẽ menu chính để khỏi vẽ hai lần).</summary>
        public static bool Drives { get { return Inst != null && Inst.isActiveAndEnabled; } }

        Hud hud;
        bool sceneHasHud;
        string error;
        bool errorLogged;

        void Awake()
        {
            Inst = this;
            try { Art.Init(); } catch (System.Exception e) { Fail(e); }

            if (GameManager.Inst == null)
                Debug.LogError("Neon Breaker: Scene MainMenu không có GameManager (đối tượng 'Systems') — phím bấm sẽ không chạy. " +
                               "Xoá MainMenu.unity rồi chạy lại menu Tools ▸ Neon Breaker ▸ Cập nhật Nhiệm vụ + Phố Neon + Lưu game.");

            sceneHasHud = FindObjectOfType<Hud>() != null;

            // Hud riêng chỉ để dùng các hàm vẽ (Fill, Txt, Button...). Tắt để nó không tự chạy OnGUI (Hud thật nằm ở 'Systems').
            hud = gameObject.AddComponent<Hud>();
            hud.enabled = false;

            Open();
        }

        void Start()
        {
            GameManager.State = GameState.Menu;          // chắc chắn sau GameManager.Start
            if (!MainMenuUi.Active) Open();
        }

        void Open()
        {
            GameManager.State = GameState.Menu;
            GameManager.Paused = false;
            try { MainMenuUi.Open(); }
            catch (System.Exception e) { Fail(e); }
            Debug.Log("Neon Breaker: menu chính đã bật (GameManager=" + (GameManager.Inst != null) + ", Art.Pixel=" + (Art.Pixel != null) +
                      ", Hud trong Scene=" + sceneHasHud + ")");
        }

        void OnDestroy()
        {
            MainMenuUi.Close();
            if (Inst == this) Inst = null;
        }

        void Fail(System.Exception e)
        {
            error = e.ToString();
            if (!errorLogged) { errorLogged = true; Debug.LogException(e); }
        }

        void OnGUI()
        {
            GUI.depth = -1000;                         // nhỏ hơn mọi OnGUI khác => vẽ trên cùng (PixelScreen đặt 1000, Hud mặc định 0)
            if (MainMenuUi.Active && hud != null)
            {
                if (hud.Prepare())
                {
                    try { MainMenuUi.Draw(hud); }
                    catch (System.Exception e) { Fail(e); }
                    GUI.color = Color.white;
                }
                else if (error == null) error = "Art.Pixel = null (Art.Init không tạo được texture).";
            }
            if (error != null) DrawError();
        }

        /// <summary>Hiện lỗi lên màn hình để biết chính xác chuyện gì xảy ra (chụp màn hình / chép chữ gửi lại).</summary>
        void DrawError()
        {
            var back = Texture2D.whiteTexture;
            GUI.color = new Color(0.06f, 0f, 0.02f, 0.96f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), back);
            GUI.color = Color.white;
            var st = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = Mathf.Max(12, Screen.height / 45), alignment = TextAnchor.UpperLeft };
            st.normal.textColor = new Color(1f, 0.45f, 0.45f, 1f);
            GUI.Label(new Rect(24, 20, Screen.width - 48, Screen.height - 40),
                      "NEON BREAKER - MENU CHINH BI LOI\n\n" + error + "\n\n(Mo Console de xem them. Chep doan chu nay gui lai de sua.)", st);
        }
    }
}
