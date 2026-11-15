using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Dựng thêm các tầng nhà cho Phố Neon / Rìa Phố Neon lúc chạy (không sửa Scene của bạn):
    ///  • Lớp 1 (xa nhất, đi theo người chơi): có sẵn trong Scene (nhóm Background ▸ Far / Mid).
    ///  • Lớp 2 (nhà cố định gần người chơi, KHÔNG vào được, làm tối): các nhà có sẵn của Scene (nhóm Buildings) — được nhân màu tối đi; Rìa phố: dãy nhà đổ nát dựng thêm.
    ///  • Lớp 3 (nhà cố định gần người chơi, VÀO ĐƯỢC, sáng rõ nhất): 5 toà nhà mặt tiền mới (CenterShop, RoboStore, ZenMonastery, DesTown4, ArenaCyber) ở Phố Neon (nhóm TownLayers).
    /// Rìa Phố Neon có cùng cách xếp lớp: lớp 3 = công trình mới (MerShop, Phòng thí nghiệm, Lều trại Ryber, Bãi rác công nghệ — xem RimDefs); lớp 2 = các lều (Shack_*) có sẵn của Scene bị làm tối + đẩy xuống sau, và dãy nhà đổ nát dựng thêm.
    /// Mỗi toà có cửa chính ở giữa: đứng trước cửa bấm F để vào (xem Interior). DesTown4 chỉ có hai vệ sĩ chặn cửa (xem DesTownBuilder); ArenaCyber vào được (xem ArenaBuilder).
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
            public System.Func<bool> canEnter;                   // có thì chỉ hiện nút VÀO khi hàm này trả true (ví dụ cổng trại chưa mở)
            public float doorDx;                                 // cửa lệch khỏi tâm mặt tiền (đơn vị)
            public System.Action<Transform, float> onPlace;      // hook dựng thêm (NPC...) sau khi đặt mặt tiền: (transform toà nhà, x)
        }

        /// <summary>facade >= RimBase nghĩa là hình của RimArt (id - RimBase), ngược lại là của TownArt.</summary>
        public const int RimBase = 100;

        /// <summary>Danh sách toà nhà lớp 3 ở Phố Neon (x = tâm toà nhà = vị trí cửa).</summary>
        public static List<Def> Defs()
        {
            var l = new List<Def>();
            l.Add(new Def { id = CenterShopBuilder.Id, vi = "CENTERSHOP", en = "CENTERSHOP", facade = TownArt.FacadeCenter, x = 45f, neon = Cfg.Cyan, entry = CenterShopBuilder.Floor1 });
            l.Add(new Def { id = RoboStoreBuilder.Id, vi = "ROBOSTORE", en = "ROBOSTORE", facade = TownArt.FacadeRobo, x = 87f, neon = Cfg.Magenta, entry = RoboStoreBuilder.Floor1 });
            l.Add(new Def { id = ZenBuilder.Id, vi = "ZENMONASTERY", en = "ZENMONASTERY", facade = TownArt.FacadeZen, x = 118f, neon = new Color(1f, 0.75f, 0.3f), entry = ZenBuilder.Hall });
            l.Add(new Def { id = DesTownBuilder.Id, vi = "DESTOWN4", en = "DESTOWN4", facade = TownArt.FacadeDes, x = 150f, neon = Cfg.Red, enterable = false, onPlace = DesTownBuilder.Place });
            l.Add(new Def { id = ArenaBuilder.Id, vi = "ARENACYBER", en = "ARENACYBER", facade = TownArt.FacadeArena, x = 178f, neon = Cfg.Magenta, entry = ArenaBuilder.Lobby });
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
            bool rim = d.facade >= RimBase;
            Vector2Int sz = rim ? RimArt.FacadeSize(d.facade - RimBase) : TownArt.FacadeSize(d.facade);
            var sr = Art.Spr(g.transform, "Facade", rim ? RimArt.Facade(d.facade - RimBase) : TownArt.Facade(d.facade), Vector2.zero, Vector2.one, Color.white, -55);
            // ánh sáng neon rơi xuống vỉa hè
            Art.Spr(g.transform, "Glow", Art.Glow, new Vector2(0f, 1.2f), new Vector2(sz.x / 16f * 0.9f, 4f), Cfg.A(d.neon, 0.18f), -54, true);
            if (d.enterable && d.entry != null)
            {
                var entry = d.entry;
                string id = d.id;
                float x = d.x + d.doorDx;
                var hs = Hotspot.Make(g.transform, new Vector3(x, 0f, 0f), "VÀO " + d.vi, "ENTER " + d.en, () => Interior.Enter(id, x, entry), 2.2f);
                if (d.canEnter != null) hs.canUse = d.canEnter;
            }
            if (d.onPlace != null) d.onPlace(g.transform, d.x);
        }

        // ------------------------------------------------------------ Rìa Phố Neon
        /// <summary>Các công trình lớp 3 của Rìa Phố Neon. Khu mỏ bỏ hoang và Trụ sở thứ 3 của Phe nô lệ chưa có thiết kế (tác giả chưa cung cấp) nên chưa dựng.</summary>
        public static List<Def> RimDefs()
        {
            var l = new List<Def>();
            l.Add(new Def { id = MerShopBuilder.Id, vi = "MERSHOP", en = "MERSHOP", facade = RimBase + RimArt.FacadeMer, x = 26f, neon = new Color(1f, 0.6f, 0.2f), entry = MerShopBuilder.Shop });
            l.Add(new Def { id = "DyberLab", vi = "PHÒNG THÍ NGHIỆM", en = "LABORATORY", facade = RimBase + RimArt.FacadeLab, x = 62f, neon = new Color(1f, 0.48f, 0.1f), enterable = false, onPlace = LabSite.Place });
            l.Add(new Def { id = RyberBuilder.Id, vi = "LỀU TRẠI", en = "CAMP", facade = RimBase + RimArt.FacadeCamp, x = 108f, neon = new Color(1f, 0.5f, 0.15f), entry = RyberBuilder.Camp, canEnter = () => Town.RyberOpen, doorDx = RimArt.DoorDx(RimArt.FacadeCamp), onPlace = RyberBuilder.Place });
            return l;
        }

        static void SetupRim()
        {
            var root = new GameObject("TownLayers");
            // lớp 2: các lều có sẵn của Scene (Props ▸ Shack_*) bị làm tối và đẩy xuống sau công trình mới
            var props = GameObject.Find("Props");
            if (props != null)
            {
                for (int i = 0; i < props.transform.childCount; i++)
                {
                    var ch = props.transform.GetChild(i);
                    if (!ch.name.StartsWith("Shack_")) continue;
                    var srs = ch.GetComponentsInChildren<SpriteRenderer>(true);
                    for (int k = 0; k < srs.Length; k++)
                    {
                        var c = srs[k].color;
                        srs[k].color = new Color(c.r * 0.55f, c.g * 0.55f, c.b * 0.6f, c.a);
                        srs[k].sortingOrder = -58;
                    }
                }
            }
            // lớp 3: công trình mới
            var defs = RimDefs();
            for (int i = 0; i < defs.Count; i++) Place(root.transform, defs[i]);
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
