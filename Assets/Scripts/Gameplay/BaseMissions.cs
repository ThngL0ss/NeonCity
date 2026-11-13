using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>Một yêu cầu "nộp nguyên liệu" của nhiệm vụ (đếm trong kho đồ + kho căn cứ; nộp cho Albert thì bị trừ đi).</summary>
    public class MissionReq
    {
        public string nameVi, nameEn;
        public int need;
        public System.Func<int> have;
        public System.Action<int> consume;

        public string Name { get { return Cfg.L(nameVi, nameEn); } }
        public int Have { get { return Mathf.Min(need, Mathf.Max(0, have())); } }
        public bool Done { get { return have() >= need; } }
    }

    /// <summary>
    /// Một nhiệm vụ. Nhận ở hộp thư nhiệm vụ trong căn cứ, nộp / nhận thưởng khi nói chuyện với Albert.
    /// Hai kiểu: (1) bộ đếm tích luỹ (target + progress), (2) nộp nguyên liệu (reqs — hết nhiệm vụ thì nguyên liệu bị trừ).
    /// </summary>
    public class MissionDef
    {
        public string titleVi, titleEn, descVi, descEn;
        public int target;
        public System.Func<int> progress;
        public int rewardCoins, rewardOil;
        public List<MissionReq> reqs;
        /// <summary>Lời Albert khi nhận bàn giao (tuỳ chọn; trống = lời mặc định).</summary>
        public string claimVi, claimEn;

        /// <summary>Tiến độ dạng chữ: "Thỏi sắt 3/10, Sợi cacbon 0/20, Dầu 12/50" hoặc "(5/12)".</summary>
        public string ProgressText
        {
            get
            {
                if (reqs == null || reqs.Count == 0) return "(" + Progress + "/" + target + ")";
                var parts = new string[reqs.Count];
                for (int i = 0; i < reqs.Count; i++) parts[i] = reqs[i].Name + " " + reqs[i].Have + "/" + reqs[i].need;
                return string.Join(", ", parts);
            }
        }

        public string Title { get { return Cfg.L(titleVi, titleEn); } }
        public string Desc { get { return Cfg.L(descVi, descEn); } }
        public int Progress { get { return Mathf.Min(target, progress()); } }
        public bool Done
        {
            get
            {
                if (reqs != null && reqs.Count > 0)
                {
                    for (int i = 0; i < reqs.Count; i++) if (!reqs[i].Done) return false;
                    return true;
                }
                return progress() >= target;
            }
        }

        /// <summary>Gắn danh sách yêu cầu nộp nguyên liệu; tự tính target / progress tổng để các chỗ hiển thị cũ vẫn dùng được.</summary>
        public MissionDef WithReqs(params MissionReq[] r)
        {
            reqs = new List<MissionReq>(r);
            int total = 0;
            for (int i = 0; i < r.Length; i++) total += r[i].need;
            target = total;
            progress = () => { int sum = 0; for (int i = 0; i < reqs.Count; i++) sum += reqs[i].Have; return sum; };
            return this;
        }
    }

    /// <summary>
    /// Nhiệm vụ nối tiếp do Albert giao (nói chuyện bấm F). Hoàn thành thì quay lại nói chuyện để nhận thưởng, rồi sang nhiệm vụ kế.
    /// Muốn thêm / sửa nhiệm vụ: chỉnh danh sách trong Build().
    /// </summary>
    public static class BaseMissions
    {
        public static int Crafted, TotalKills, TotalTowers, BossKills;
        /// <summary>Albert đã kể cốt truyện (lần nói chuyện đầu tiên) chưa.</summary>
        public static bool StoryTold;
        /// <summary>Nhiệm vụ hiện tại (>= Count nghĩa là đã xong hết).</summary>
        public static int Index;
        /// <summary>Đã nhận nhiệm vụ hiện tại ở hộp thư chưa.</summary>
        public static bool Accepted;
        /// <summary>Đã nhận nhiệm vụ đầu tiên chưa (đang làm nó, hoặc đã xong và sang nhiệm vụ sau). Chưa nhận thì cửa ra của căn cứ bị khoá.</summary>
        public static bool FirstMissionReceived { get { return Accepted || Index > 0; } }
        /// <summary>Hộp thư nhiệm vụ đã xuất hiện (sau lần nói chuyện đầu với Albert).</summary>
        public static bool BoardUnlocked;
        /// <summary>Vừa mở khoá hộp thư: MissionBoard đọc để chạy hiệu ứng xuất hiện (không lưu).</summary>
        public static bool BoardJustUnlocked;
        /// <summary>Người chơi đã mở hộp thư lần nào chưa (tắt mũi tên chỉ đường) — lưu.</summary>
        public static bool BoardOpenedOnce;
        static List<MissionDef> list;

        public static void ResetAll()
        {
            Crafted = TotalKills = TotalTowers = BossKills = 0;
            StoryTold = false;
            Index = 0;
            Accepted = false;
            BoardUnlocked = false;
            BoardJustUnlocked = false;
            BoardOpenedOnce = false;
            list = null;
        }

        /// <summary>Mở khoá hộp thư nhiệm vụ (gọi sau lần nói chuyện đầu với Albert).</summary>
        public static void UnlockBoard()
        {
            if (BoardUnlocked) return;
            BoardUnlocked = true;
            BoardJustUnlocked = true;
        }

        /// <summary>Nhận nhiệm vụ hiện tại ở hộp thư. false nếu không có / đã nhận rồi.</summary>
        public static bool Accept()
        {
            if (Current == null || Accepted) return false;
            Accepted = true;
            Sfx.Play("victory", 0.45f, 1.4f);
            SaveManager.AutoSave();
            return true;
        }

        /// <summary>Ghi tiến độ nhiệm vụ + cờ cốt truyện vào d.flags (lưu game).</summary>
        public static void Export(SaveData d)
        {
            SaveManager.SetFlag(d.flags, "m.crafted", Crafted);
            SaveManager.SetFlag(d.flags, "m.kills", TotalKills);
            SaveManager.SetFlag(d.flags, "m.towers", TotalTowers);
            SaveManager.SetFlag(d.flags, "m.boss", BossKills);
            SaveManager.SetFlag(d.flags, "m.story", StoryTold ? 1 : 0);
            SaveManager.SetFlag(d.flags, "m.index", Index);
            SaveManager.SetFlag(d.flags, "m.accepted", Accepted ? 1 : 0);
            SaveManager.SetFlag(d.flags, "m.board", BoardUnlocked ? 1 : 0);
            SaveManager.SetFlag(d.flags, "m.boardSeen", BoardOpenedOnce ? 1 : 0);
        }

        /// <summary>Nạp tiến độ nhiệm vụ + cờ cốt truyện từ d.flags (gọi sau ResetAll).</summary>
        public static void Import(SaveData d)
        {
            Crafted = SaveManager.GetFlag(d.flags, "m.crafted");
            TotalKills = SaveManager.GetFlag(d.flags, "m.kills");
            TotalTowers = SaveManager.GetFlag(d.flags, "m.towers");
            BossKills = SaveManager.GetFlag(d.flags, "m.boss");
            StoryTold = SaveManager.GetFlag(d.flags, "m.story") != 0;
            Index = SaveManager.GetFlag(d.flags, "m.index");
            Accepted = SaveManager.GetFlag(d.flags, "m.accepted") != 0;
            BoardUnlocked = SaveManager.GetFlag(d.flags, "m.board") != 0 || StoryTold;
            BoardOpenedOnce = SaveManager.GetFlag(d.flags, "m.boardSeen") != 0;
            BoardJustUnlocked = false;
        }

        /// <summary>Cộng thống kê ván vừa chơi vào bộ đếm tích luỹ (gọi khi chuyển cảnh).</summary>
        public static void BankRun()
        {
            TotalKills += GameManager.Kills;
            TotalTowers += GameManager.TowersDestroyed;
            GameManager.Kills = 0;
            GameManager.TowersDestroyed = 0;
        }

        public static List<MissionDef> All
        {
            get
            {
                if (list == null) list = Build();
                return list;
            }
        }

        static List<MissionDef> Build()
        {
            var l = new List<MissionDef>();
            l.Add(new MissionDef
            {
                titleVi = "Nguyên liệu hồi sinh", titleEn = "Resurrection Materials",
                descVi = "Albert cần nguyên liệu để tái tạo cơ thể cho cậu. Kiếm đủ rồi mang về giao cho ông ấy. Đồ nằm rải rác ở Phố Neon và Rìa Phố Neon (rương cũng có thể có). Đồ cất trong kho căn cứ cũng được tính.",
                descEn = "Albert needs materials to rebuild your body. Gather enough and bring them to him. Items lie scattered around Neon Street and its Outskirts (chests may hold some too). Items in the base stash count too.",
                rewardCoins = 60, rewardOil = 0,
                claimVi = "Đủ cả rồi. Thôi được, lần sau ta lại tái tạo cho cậu. Nhưng đừng có chết nữa đấy!",
                claimEn = "That's everything. Fine, I'll rebuild you next time too. But stop dying on me!",
            }.WithReqs(
                new MissionReq { nameVi = "Thỏi sắt", nameEn = "Iron ingot", need = 10, have = () => Owned("Item_iron_ingot"), consume = n => Take("Item_iron_ingot", n) },
                new MissionReq { nameVi = "Sợi cacbon", nameEn = "Carbon fiber", need = 20, have = () => Owned("Item_carbon_fiber"), consume = n => Take("Item_carbon_fiber", n) },
                new MissionReq { nameVi = "Dầu", nameEn = "Oil", need = 50, have = () => Inventory.Oil, consume = n => Inventory.SpendOil(n) }));
            l.Add(new MissionDef
            {
                titleVi = "Khởi động", titleEn = "Warm-up",
                descVi = "Hạ 12 con quái ngoài kia để cơ thể mới làm quen với chiến đấu.", descEn = "Take down 12 enemies out there so your new body gets used to combat.",
                target = 12, progress = () => TotalKills + GameManager.Kills, rewardCoins = 40, rewardOil = 6,
            });
            l.Add(new MissionDef
            {
                titleVi = "Thu hoạch linh kiện", titleEn = "Parts Harvest",
                descVi = "Phá 2 trụ triệu hồi. Quái bậc D / C rơi linh kiện — mang về ghép ở máy ghép của ta.", descEn = "Destroy 2 summon towers. D / C rank enemies drop parts — bring them back for my fusion machine.",
                target = 2, progress = () => TotalTowers + GameManager.TowersDestroyed, rewardCoins = 60, rewardOil = 10,
            });
            l.Add(new MissionDef
            {
                titleVi = "Bàn tay của kỹ sư", titleEn = "Engineer's Touch",
                descVi = "Ghép 1 lần ở máy ghép linh kiện (3 mảnh cùng bậc + dầu + xu).", descEn = "Fuse parts once at the fusion machine (3 parts of one rank + oil + coins).",
                target = 1, progress = () => PartFusion.FusedTotal, rewardCoins = 50, rewardOil = 8,
            });
            l.Add(new MissionDef
            {
                titleVi = "Thợ chế tác", titleEn = "Crafter",
                descVi = "Chế tác 2 vật phẩm ở bàn chế tác (nguyên liệu nhặt trên đường hoặc trong rương).", descEn = "Craft 2 items at the crafting bench (materials lie on the road or in chests).",
                target = 2, progress = () => Crafted, rewardCoins = 80, rewardOil = 12,
            });
            l.Add(new MissionDef
            {
                titleVi = "Phá hết trụ", titleEn = "Clear the Towers",
                descVi = "Phá hủy cả 6 trụ triệu hồi để mở cổng khu boss.", descEn = "Destroy all 6 summon towers to open the boss gate.",
                target = 6, progress = () => TotalTowers + GameManager.TowersDestroyed, rewardCoins = 120, rewardOil = 20,
            });
            l.Add(new MissionDef
            {
                titleVi = "Lấy lại thứ thuộc về cậu", titleEn = "Take It Back",
                descVi = "Hạ OVERLORD và đoạt lại " + CineScripts.StolenItemVi + " (bậc SS).", descEn = "Defeat OVERLORD and take back the " + CineScripts.StolenItemEn + " (SS rank).",
                target = 1, progress = () => BossKills, rewardCoins = 300, rewardOil = 50,
            });
            return l;
        }

        public static MissionDef Current { get { return Index < All.Count ? All[Index] : null; } }

        /// <summary>Số vật phẩm (theo tên asset) đang có: trong kho đồ + kho căn cứ.</summary>
        public static int Owned(string assetName)
        {
            var it = ItemDatabase.Find(assetName);
            return it == null ? 0 : Inventory.Count(it) + Stash.Count(it);
        }

        /// <summary>Trừ n vật phẩm (theo tên asset): lấy trong kho đồ trước, thiếu thì lấy trong kho căn cứ.</summary>
        static void Take(string assetName, int n)
        {
            var it = ItemDatabase.Find(assetName);
            if (it == null || n <= 0) return;
            int fromBag = Mathf.Min(n, Inventory.Count(it));
            if (fromBag > 0) Inventory.RemoveItem(it, fromBag);
            int rest = n - fromBag;
            for (int i = Stash.Items.Count - 1; i >= 0 && rest > 0; i--)
            {
                if (Stash.Items[i].item != it) continue;
                int take = Mathf.Min(rest, Stash.Items[i].count);
                Stash.Items[i].count -= take;
                rest -= take;
                if (Stash.Items[i].count <= 0) Stash.Items.RemoveAt(i);
            }
        }

        /// <summary>Nhận thưởng nhiệm vụ hiện tại nếu đã xong. Trả về nhiệm vụ vừa nhận (null nếu không).</summary>
        public static MissionDef TryClaim()
        {
            var m = Current;
            if (m == null || !Accepted || !m.Done) return null;
            if (m.reqs != null)
                for (int i = 0; i < m.reqs.Count; i++) m.reqs[i].consume(m.reqs[i].need);   // nộp nguyên liệu cho Albert
            if (m.rewardCoins > 0) Inventory.AddCoins(m.rewardCoins, false);
            if (m.rewardOil > 0) Inventory.AddOil(m.rewardOil, true);
            Index++;
            Accepted = false;
            Sfx.Play("victory", 0.6f, 1.2f);
            SaveManager.AutoSave();
            return m;
        }
    }
}
