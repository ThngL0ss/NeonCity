using System;
using System.Collections.Generic;

namespace NeonBreaker
{
    // Các lớp dữ liệu để lưu game ra file JSON (JsonUtility). Chỉ chứa số / chuỗi / danh sách — không tham chiếu thẳng tới asset.
    // Vật phẩm được lưu theo TÊN asset (ItemData.name) và tra lại qua ItemDatabase khi nạp.

    [Serializable]
    public class SaveItem
    {
        public string id = "";
        public int n = 1;
    }

    [Serializable]
    public class SaveBot
    {
        public int uid;
        public string type = "";
        public int rank;
        public int level = 1;
        public float maturity;
        public float hp;
        public int state;             // BotState (Out được lưu thành Panel)
    }

    [Serializable]
    public class SaveSkill
    {
        public string id = "";
        public int lv;
    }

    /// <summary>Cặp tên - số nguyên: chỗ để mở rộng (nhiệm vụ, cờ cốt truyện...) mà không phải đổi định dạng file.</summary>
    [Serializable]
    public class SaveInt
    {
        public string k = "";
        public int v;
    }

    /// <summary>Toàn bộ dữ liệu của một ô lưu (slot).</summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int slot;
        public string created = "";       // thời điểm tạo ván (ISO 8601, UTC)
        public string savedAt = "";       // lần lưu gần nhất (ISO 8601, UTC)
        public float playSeconds;         // tổng thời gian chơi
        public string place = "";         // nơi lưu gần nhất (Base, Tutorial, ...)

        // ---- tiến trình cốt truyện
        public bool tutorialDone;
        public bool baseDialogDone;
        public bool baseVisited;
        public int deaths;
        public List<string> places = new List<string>();     // các địa điểm đã mở trên bản đồ
        public List<SaveInt> flags = new List<SaveInt>();    // nhiệm vụ + cờ khác

        // ---- người chơi
        public int rank;
        public int level = 1;
        public int coins;
        public int oil;
        public float[] perm = new float[0];                  // chỉ số vĩnh viễn từ vật phẩm đã dùng

        // ---- đồ mang theo
        public List<SaveItem> bag = new List<SaveItem>();
        public List<string> equipped = new List<string>();   // 8 ô, "" = trống
        public int[] parts = new int[0];                     // linh kiện F..S đang mang

        // ---- kho ở căn cứ
        public List<SaveItem> stash = new List<SaveItem>();
        public int[] stashParts = new int[0];

        // ---- skill + minibot
        public List<SaveSkill> skills = new List<SaveSkill>();
        public List<string> defeated = new List<string>();
        public List<SaveBot> bots = new List<SaveBot>();
        public int crafted;
        public int partsCollected;
        public int fused;
        public int totalKills;
    }
}
