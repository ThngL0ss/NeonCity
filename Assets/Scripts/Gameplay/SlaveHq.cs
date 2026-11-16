using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// ReWolrld — trụ sở thứ 3 của Phe nô lệ (Rìa Phố Neon, KHÔNG vào được): nhà gỗ 3 tầng, mái chéo, cửa đóng kín nhưng ánh sáng vẫn hắt ra từ các cửa sổ
    /// (hình vẽ ở RimArt.FacadeSlave; tên viết đúng như tác giả: "ReWolrld"). Lúc chạy: quầng sáng ấm hắt ra từ từng cửa sổ (vài ô chập chờn như nến)
    /// và một điểm "XEM CỬA" (lời thoại tạm — sửa tuỳ ý).
    /// </summary>
    public static class SlaveHq
    {
        static readonly Color Warm = new Color(1f, 0.8f, 0.4f);

        /// <summary>Gọi từ StreetBuildings.Place (x = tâm mặt tiền = tâm cửa).</summary>
        public static void Place(Transform building, float x)
        {
            for (int i = 0; i < RimArt.SlaveWindowCount; i++)
            {
                Vector3 g = RimArt.SlaveWindowGlow(i);
                float w = g.z * 3.2f;
                var sr = Art.Spr(building, "WindowGlow", Art.Glow, new Vector2(g.x, g.y), new Vector2(w, w * 1.1f), Cfg.A(Warm, 0.22f), -54, true);
                if (i % 3 == 1) RyberBuilder.Flicker(sr, 0.55f);
            }
            Art.Spr(building, "DoorLight", Art.Glow, new Vector2(0f, 0.3f), new Vector2(5f, 1.6f), Cfg.A(Warm, 0.25f), -54, true);
            Hotspot.Make(building, new Vector3(x, 0f, 0f), "XEM CỬA", "EXAMINE THE DOOR", Examine, 2.4f);
        }

        static void Examine()
        {
            Color me = Cfg.Cyan;
            var l = new List<DlgLine>
            {
                new DlgLine("XbAst-013", Cfg.L("Cửa đóng kín… nhưng ánh sáng vẫn hắt ra từ các ô cửa sổ.", "The door is shut tight… but light still spills out of the windows."), me),
                new DlgLine("XbAst-013", Cfg.L("Bên trong vẫn có người. Chỉ là họ không định mở cửa.", "Someone is still inside. They just don't intend to open up."), me),
            };
            DialogueUi.Begin(l, null);
        }
    }
}
