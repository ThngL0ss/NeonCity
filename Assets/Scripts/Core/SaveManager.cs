using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Lưu / nạp game: 10 ô (slot), mỗi ván mới dùng một ô trống; người chơi xoá được từng ô.
    ///
    /// Cách hoạt động (đơn giản mà chắc):
    /// • Mỗi ô = một file JSON dễ đọc: Application.persistentDataPath/saves/slot_N.json (N = 0..9).
    /// • Ghi an toàn: ghi ra file .tmp → chép bản cũ thành .bak → đổi tên .tmp thành file chính. Mất điện giữa chừng vẫn còn .bak; khi file chính hỏng thì tự đọc .bak.
    /// • Chỉ lưu ở các "điểm an toàn": ngay sau khi chết (đã trừ hình phạt), khi về căn cứ, khi rời căn cứ, khi nhận thưởng nhiệm vụ,
    ///   khi đóng kho căn cứ, khi xong màn hướng dẫn. Không lưu giữa lúc đang đánh nhau ngoài màn.
    /// • Nội dung lưu: cấp bậc / cấp độ, xu, dầu, vật phẩm + trang bị, linh kiện, minibot, skill tree, kho căn cứ, nhiệm vụ, cờ cốt truyện.
    /// • Khi nạp: reset sạch mọi biến static rồi áp dữ liệu từ file, vào thẳng căn cứ.
    /// </summary>
    public static class SaveManager
    {
        public const int MaxSlots = 10;

        /// <summary>Ô đang chơi (-1 = chưa gắn với ô nào, ví dụ chạy thẳng một Scene để thử — không tự lưu).</summary>
        public static int Slot = -1;

        static string created = "";
        static float baseSeconds;
        static float sinceT;

        public static void ResetStatics()
        {
            Slot = -1;
            created = "";
            baseSeconds = 0f;
            sinceT = 0f;
        }

        /// <summary>Thôi gắn với ô hiện tại (sau đó AutoSave không ghi gì nữa).</summary>
        public static void Detach() { Slot = -1; }

        public static bool HasSlot { get { return Slot >= 0 && Slot < MaxSlots; } }

        // ------------------------------------------------------------ đường dẫn
        public static string Dir { get { return Path.Combine(Application.persistentDataPath, "saves"); } }
        public static string PathOf(int slot) { return Path.Combine(Dir, "slot_" + slot + ".json"); }

        public static bool Exists(int slot) { return slot >= 0 && slot < MaxSlots && (File.Exists(PathOf(slot)) || File.Exists(PathOf(slot) + ".bak")); }

        // ------------------------------------------------------------ đọc
        /// <summary>Đọc một ô (đọc .bak nếu file chính hỏng). null nếu ô trống hoặc hỏng hẳn.</summary>
        public static SaveData Read(int slot)
        {
            if (slot < 0 || slot >= MaxSlots) return null;
            string p = PathOf(slot);
            var d = TryRead(p);
            if (d == null) d = TryRead(p + ".bak");
            return d;
        }

        static SaveData TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                if (string.IsNullOrEmpty(json)) return null;
                var d = JsonUtility.FromJson<SaveData>(json);
                if (d == null || d.version > SaveData.CurrentVersion) return null;
                return d;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Neon Breaker: không đọc được " + path + " — " + e.Message);
                return null;
            }
        }

        /// <summary>Đọc cả 10 ô (phần tử null = ô trống).</summary>
        public static SaveData[] ReadAll()
        {
            var all = new SaveData[MaxSlots];
            for (int i = 0; i < MaxSlots; i++) all[i] = Read(i);
            return all;
        }

        public static int FirstFree()
        {
            for (int i = 0; i < MaxSlots; i++) if (!Exists(i)) return i;
            return -1;
        }

        public static int UsedCount()
        {
            int n = 0;
            for (int i = 0; i < MaxSlots; i++) if (Exists(i)) n++;
            return n;
        }

        // ------------------------------------------------------------ ghi / xoá
        static bool Write(int slot, SaveData d)
        {
            try
            {
                Directory.CreateDirectory(Dir);
                string p = PathOf(slot), tmp = p + ".tmp", bak = p + ".bak";
                File.WriteAllText(tmp, JsonUtility.ToJson(d, true));
                if (File.Exists(p)) File.Copy(p, bak, true);
                if (File.Exists(p)) File.Delete(p);
                File.Move(tmp, p);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Neon Breaker: không lưu được ô " + slot + " — " + e.Message);
                return false;
            }
        }

        public static void Delete(int slot)
        {
            if (slot < 0 || slot >= MaxSlots) return;
            try
            {
                string p = PathOf(slot);
                if (File.Exists(p)) File.Delete(p);
                if (File.Exists(p + ".bak")) File.Delete(p + ".bak");
                if (File.Exists(p + ".tmp")) File.Delete(p + ".tmp");
            }
            catch (Exception e) { Debug.LogWarning("Neon Breaker: không xoá được ô " + slot + " — " + e.Message); }
            if (Slot == slot) Slot = -1;
        }

        // ------------------------------------------------------------ ván mới / nạp
        /// <summary>
        /// Bắt đầu ván mới ở ô trống đầu tiên: xoá sạch trạng thái, ghi ngay file lưu đầu tiên.
        /// Trả về số ô, hoặc -1 nếu cả 10 ô đã đầy (giao diện sẽ yêu cầu xoá bớt một ô).
        /// </summary>
        public static int NewGame() { return NewGame(FirstFree()); }

        public static int NewGame(int slot)
        {
            if (slot < 0 || slot >= MaxSlots) return -1;
            WipeState();
            Slot = slot;
            created = DateTime.UtcNow.ToString("o");
            baseSeconds = 0f;
            sinceT = Time.realtimeSinceStartup;
            Delete(slot);
            Slot = slot;
            Save();
            return slot;
        }

        /// <summary>Nạp ô slot: xoá sạch trạng thái rồi áp dữ liệu từ file. false nếu ô trống / hỏng.</summary>
        public static bool Load(int slot)
        {
            var d = Read(slot);
            if (d == null) return false;
            WipeState();
            Apply(d);
            Slot = slot;
            return true;
        }

        static void WipeState()
        {
            Inventory.ResetAll();
            SkillSystem.ResetAll();
            GameMenu.ResetAll();
            GameFlow.ResetStatics();      // gồm cả Stash, PlayerProgress, nhiệm vụ, PartFusion
            Detach();
        }

        // ------------------------------------------------------------ lưu
        /// <summary>Lưu vào ô đang chơi. Không có ô thì bỏ qua (trả về false).</summary>
        public static bool AutoSave()
        {
            if (!HasSlot) return false;
            return Save();
        }

        public static bool Save()
        {
            if (!HasSlot) return false;
            SaveData d;
            try { d = Capture(); }
            catch (Exception e)
            {
                Debug.LogWarning("Neon Breaker: không chụp được trạng thái để lưu — " + e);
                return false;           // lỗi khi lưu không được làm hỏng việc chuyển cảnh
            }
            return Write(Slot, d);
        }

        // ------------------------------------------------------------ chụp / áp trạng thái
        public static SaveData Capture()
        {
            var d = new SaveData();
            d.slot = Slot;
            d.created = string.IsNullOrEmpty(created) ? DateTime.UtcNow.ToString("o") : created;
            created = d.created;
            d.savedAt = DateTime.UtcNow.ToString("o");
            d.playSeconds = baseSeconds + Mathf.Max(0f, Time.realtimeSinceStartup - sinceT);
            d.place = GameFlow.InBase ? GameFlow.BaseScene : GameFlow.CurrentPlaceId;

            d.tutorialDone = GameFlow.TutorialDone;
            d.baseDialogDone = GameFlow.BaseDialogDone;
            d.baseVisited = GameFlow.BaseVisited;
            d.deaths = GameFlow.Deaths;
            SetFlag(d.flags, "g.introSeen", GameFlow.IntroSeen ? 1 : 0);
            GameFlow.ExportPlaces(d.places);
            BaseMissions.Export(d);
            Town.Export(d);
            d.totalKills = BaseMissions.TotalKills;

            d.rank = (int)PlayerProgress.PlayerRank;
            d.level = PlayerProgress.PlayerLevel;
            d.coins = Inventory.Coins;
            d.oil = Inventory.Oil;
            d.perm = Inventory.PermBuffs();

            d.bag.Clear();
            for (int i = 0; i < Inventory.Slots.Count; i++)
            {
                var s = Inventory.Slots[i];
                if (s.item != null && s.count > 0) d.bag.Add(new SaveItem { id = ItemDatabase.IdOf(s.item), n = s.count });
            }
            d.equipped.Clear();
            for (int i = 0; i < Inventory.Equipped.Length; i++) d.equipped.Add(ItemDatabase.IdOf(Inventory.Equipped[i]));

            Stash.Export(d);
            SkillSystem.Export(d);
            return d;
        }

        static void Apply(SaveData d)
        {
            created = d.created ?? "";
            baseSeconds = d.playSeconds;
            sinceT = Time.realtimeSinceStartup;

            GameFlow.TutorialDone = d.tutorialDone;
            GameFlow.BaseDialogDone = d.baseDialogDone;
            GameFlow.BaseVisited = d.baseVisited;
            GameFlow.Deaths = d.deaths;
            GameFlow.IntroSeen = GetFlag(d.flags, "g.introSeen") != 0 || d.baseVisited;
            GameFlow.ImportPlaces(d.places);
            BaseMissions.Import(d);
            Town.Import(d);
            BaseMissions.TotalKills = Mathf.Max(BaseMissions.TotalKills, d.totalKills);

            PlayerProgress.Set((Rank)Mathf.Clamp(d.rank, 0, 7), d.level);
            Inventory.Coins = Mathf.Max(0, d.coins);
            Inventory.Oil = Mathf.Max(0, d.oil);
            Inventory.SetPermBuffs(d.perm);

            for (int i = 0; i < d.bag.Count; i++)
            {
                var it = ItemDatabase.Find(d.bag[i].id);
                if (it == null) { Debug.LogWarning("Neon Breaker: không tìm thấy vật phẩm \"" + d.bag[i].id + "\" khi nạp — bỏ qua."); continue; }
                Inventory.AddTo(Inventory.Slots, it, Mathf.Max(1, d.bag[i].n), int.MaxValue);
            }
            for (int i = 0; i < Inventory.Equipped.Length && i < d.equipped.Count; i++)
                Inventory.Equipped[i] = ItemDatabase.Find(d.equipped[i]);
            Inventory.RefreshStats();

            Stash.Import(d);
            SkillSystem.Import(d);
        }

        // ------------------------------------------------------------ tiện ích đọc / ghi cờ (SaveInt)
        public static void SetFlag(List<SaveInt> list, string key, int value)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].k == key) { list[i].v = value; return; }
            list.Add(new SaveInt { k = key, v = value });
        }

        public static int GetFlag(List<SaveInt> list, string key, int fallback = 0)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].k == key) return list[i].v;
            return fallback;
        }

        // ------------------------------------------------------------ hiển thị danh sách
        /// <summary>Định dạng thời gian chơi: 1g 05p.</summary>
        public static string FormatPlayTime(float sec)
        {
            int m = Mathf.FloorToInt(sec / 60f);
            return (m / 60) + "g " + (m % 60).ToString("00") + "p";
        }

        /// <summary>Thời điểm lưu theo giờ máy (dd/MM HH:mm), "?" nếu không đọc được.</summary>
        public static string FormatSavedAt(string iso)
        {
            DateTime t;
            if (!string.IsNullOrEmpty(iso) && DateTime.TryParse(iso, null, System.Globalization.DateTimeStyles.RoundtripKind, out t))
                return t.ToLocalTime().ToString("dd/MM HH:mm");
            return "?";
        }
    }
}
