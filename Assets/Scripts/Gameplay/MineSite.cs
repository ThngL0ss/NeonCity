using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Khu mỏ bỏ hoang (Rìa Phố Neon, KHÔNG vào được): mỏ từng thuộc Dyber, bỏ hoang sau một sự cố; hang quá tệ, có thể sập bất cứ lúc nào nên không ai dám vào.
    /// Bên ngoài là cửa hầm lớn bị niêm phong bằng các khung sắt (xem RimArt.FacadeMine). Lúc chạy: hai đèn cảnh báo đỏ chập chờn,
    /// thỉnh thoảng bụi và đá vụn rơi xuống từ vòm hầm khi người chơi ở gần, và một điểm "XEM CỬA HẦM" (lời thoại tạm — sửa tuỳ ý).
    /// </summary>
    public class MineSite : MonoBehaviour
    {
        float x, dustT = 2f;
        Vector2 dustPoint;

        /// <summary>Gọi từ StreetBuildings.Place (x = tâm mặt tiền = tâm cửa hầm).</summary>
        public static void Place(Transform building, float x)
        {
            var beacons = RimArt.MineBeacons();
            for (int i = 0; i < beacons.Length; i++)
            {
                var glow = Art.Spr(building, "Beacon", Art.Glow, beacons[i], new Vector2(3.4f, 3.4f), Cfg.A(new Color(1f, 0.15f, 0.12f), 0.5f), -54, true);
                RyberBuilder.Flicker(glow, 0.1f);
            }
            var go = new GameObject("MineSite");
            go.transform.SetParent(building, false);
            var s = go.AddComponent<MineSite>();
            s.x = x;
            s.dustPoint = RimArt.MineDustPoint();
            Hotspot.Make(building, new Vector3(x, 0f, 0f), "XEM CỬA HẦM", "EXAMINE THE TUNNEL", Examine, 3.2f);
        }

        static void Examine()
        {
            Color me = Cfg.Cyan;
            var l = new List<DlgLine>
            {
                new DlgLine("XbAst-013", Cfg.L("Cửa hầm bị niêm phong bằng những khung sắt.", "The tunnel mouth is sealed with iron frames."), me),
                new DlgLine("XbAst-013", Cfg.L("Cái hang này quá tệ… có thể sập bất cứ lúc nào. Chẳng trách không ai dám vào.", "This cave is in terrible shape… it could collapse at any moment. No wonder nobody dares go in."), me),
            };
            DialogueUi.Begin(l, null);
        }

        void Update()
        {
            if (BaseUi.IsOpen || !GameManager.Active || Interior.Busy) return;
            var pl = Player.Instance;
            if (pl == null || Mathf.Abs(pl.Position.x - x) > 18f) return;
            dustT -= Time.deltaTime;
            if (dustT > 0f) return;
            dustT = Random.Range(3.5f, 8f);

            var at = new Vector2(x + dustPoint.x + Random.Range(-4f, 4f), dustPoint.y + Random.Range(-0.4f, 0.4f));
            Fx.Burst(at, Cfg.A(new Color(0.62f, 0.56f, 0.5f), 0.4f), 4, 0.9f, 1.1f, 0.8f, 0f, 3f);
            int n = Random.Range(3, 6);
            for (int i = 0; i < n; i++)
            {
                var p = at + new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.2f, 0.2f));
                float sz = Random.Range(0.08f, 0.16f);
                Fx.Emit(Fx.S_Square, p, new Vector2(Random.Range(-0.4f, 0.4f), 0f), new Vector2(sz, sz), new Vector2(sz * 0.7f, sz * 0.7f),
                        new Color(0.5f, 0.45f, 0.42f, 0.95f), new Color(0.5f, 0.45f, 0.42f, 0f), 1.05f, 14f, 0f, Random.value * 90f, Random.Range(-240f, 240f));
            }
        }
    }
}
