using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Dựng thêm các tầng nhà cho Phố Neon / Rìa Phố Neon lúc chạy (không sửa Scene của bạn):
    ///  • Lớp 1 (xa nhất, đi theo người chơi): có sẵn trong Scene (nhóm Background ▸ Far / Mid).
    ///  • Lớp 2 (nhà cố định gần người chơi, KHÔNG vào được, làm tối): các nhà có sẵn của Scene (nhóm Buildings) — được nhân màu tối đi; Rìa phố: dãy nhà đổ nát dựng thêm.
    ///  • Lớp 3 (nhà cố định gần người chơi, VÀO ĐƯỢC, sáng rõ nhất): 5 toà nhà mặt tiền mới ở Phố Neon (nhóm TownLayers).
    /// Mỗi toà có cửa chính ở giữa: đứng trước cửa bấm F để vào (xem Interior). DesTown4 chỉ có hai vệ sĩ chặn cửa.
    /// </summary>
    public static class StreetBuildings
    {
        public class Def
        {
            public string id, vi, en;
            public int facade;
            public float x;
            public Color neon;
            public System.Func<Room> entry;
            public bool enterable = true;
        }

        /// <summary>Danh sách toà nhà lớp 3 ở Phố Neon (x = tâm toà nhà = vị trí cửa).</summary>
        public static List<Def> Defs()
        {
            var l = new List<Def>();
            l.Add(new Def { id = CenterShopBuilder.Id, vi = "CENTERSHOP", en = "CENTERSHOP", facade = TownArt.FacadeCenter, x = 45f, neon = Cfg.Cyan, entry = CenterShopBuilder.Floor1 });
            return l;
        }

        public static void Setup(string placeId)
        {
            if (GameObject.Find("TownLayers") != null) return;
            if (placeId == Places.Neon) SetupNeon();
            else if (placeId == Places.Rim) SetupRim();
        }

        // ------------------------------------------------------------ Phố Neon
        static void SetupNeon()
        {
            // lớp 2: làm tối các nhà có sẵn
            var blds = GameObject.Find("Buildings");
            if (blds != null)
            {
                var srs = blds.GetComponentsInChildren<SpriteRenderer>(true);
                for (int i = 0; i < srs.Length; i++)
                {
                    var c = srs[i].color;
                    srs[i].color = new Color(c.r * 0.6f, c.g * 0.6f, c.b * 0.64f, c.a);
                }
            }
            // lớp 3: các toà nhà vào được
            var root = new GameObject("TownLayers");
            var defs = Defs();
            for (int i = 0; i < defs.Count; i++) Place(root.transform, defs[i]);
        }

        static void Place(Transform root, Def d)
        {
            var g = new GameObject("Building_" + d.id);
            g.transform.SetParent(root, false);
            g.transform.position = new Vector3(d.x, 0f, 0f);
            Vector2Int sz = TownArt.FacadeSize(d.facade);
            var sr = Art.Spr(g.transform, "Facade", TownArt.Facade(d.facade), Vector2.zero, Vector2.one, Color.white, -55);
            // ánh sáng neon rơi xuống vỉa hè
            Art.Spr(g.transform, "Glow", Art.Glow, new Vector2(0f, 1.2f), new Vector2(sz.x / 16f * 0.9f, 4f), Cfg.A(d.neon, 0.18f), -54, true);
            if (d.enterable && d.entry != null)
            {
                var entry = d.entry;
                string id = d.id;
                float x = d.x;
                Hotspot.Make(g.transform, new Vector3(d.x, 0f, 0f), "VÀO " + d.vi, "ENTER " + d.en, () => Interior.Enter(id, x, entry), 2.2f);
            }
        }

        // ------------------------------------------------------------ Rìa Phố Neon
        static void SetupRim()
        {
            var root = new GameObject("TownLayers");
            // lớp 2: dãy nhà đổ nát, tối, không vào được (nằm sau lều trại / đống rác của Scene)
            for (int i = 0; i < 14; i++)
            {
                float x = 6f + i * 14.5f + ((i * 7) % 5 - 2);
                var sp = TownArt.Ruin(i % 5);
                var sr = Art.Spr(root.transform, "Ruin_" + i, sp, new Vector2(x, 0f), Vector2.one, new Color(0.5f, 0.48f, 0.5f, 1f), -62);
                if (i % 2 == 1) sr.transform.localScale = new Vector3(-1f, 1f, 1f);
            }
        }
    }
}
