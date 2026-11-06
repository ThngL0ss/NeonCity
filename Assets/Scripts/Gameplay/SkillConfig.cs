using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>Một loại quái có thể chế tạo thành minibot (bằng linh kiện rơi ra từ chính loại quái đó).</summary>
    [System.Serializable]
    public class CraftEntry
    {
        [Tooltip("Prefab quái gốc (Walker, Flyer, Heavy, Rusher...). Minibot lấy hình ảnh, chỉ số, cấp bậc từ Prefab này")] public GameObject enemyPrefab;
        [Tooltip("Mã loại. Để trống = tên Prefab. Phải trùng với Enemy ▸ Type Id (hoặc tên Prefab) của quái")] public string id = "";
        public string nameVi = "";
        public string nameEn = "";
        [Tooltip("Số linh kiện (đúng cấp bậc của quái gốc: Enemy ▸ Power ▸ Rank) cần để chế tạo 1 minibot")] public int partsCost = 10;
        [Tooltip("Xác suất quái loại này rơi linh kiện (cùng cấp bậc với quái) khi bị hạ (1 = luôn rơi). Quái bậc SS / SSS không rơi")] [Range(0f, 1f)] public float partsDropChance = 1f;
        [Tooltip("Số linh kiện rơi ra ít nhất / nhiều nhất mỗi lần")] public int partsDropMin = 1;
        public int partsDropMax = 1;
        [Tooltip("Màu đặc trưng của loại này (viền minibot, giao diện). Màu LINH KIỆN thì theo cấp bậc")] public Color partColor = Color.white;
        [Tooltip("Tắt = loại quái này không rơi linh kiện và không chế tạo được")] public bool craftable = true;

        public string Id { get { return !string.IsNullOrEmpty(id) ? id : (enemyPrefab != null ? enemyPrefab.name : ""); } }
        public string DisplayName
        {
            get
            {
                string n = Cfg.Vietnamese ? nameVi : nameEn;
                return !string.IsNullOrEmpty(n) ? n : Id;
            }
        }
    }

    /// <summary>
    /// Toàn bộ thông số hệ thống Skill (cây kỹ năng, chế tạo minibot, bảng chọn, độ trưởng thành...).
    /// File: Assets/Resources/NeonBreaker/SkillConfig.asset (menu Tools ▸ Neon Breaker ▸ Thêm hệ thống Skill tạo sẵn).
    /// Chỉnh số trực tiếp trong Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Neon Breaker/Skill Config", fileName = "SkillConfig")]
    public class SkillConfig : ScriptableObject
    {
        [Header("Danh sách quái có thể chế tạo thành minibot")]
        public List<CraftEntry> craftables = new List<CraftEntry>();

        [Header("Cây kỹ năng (kéo các file Skill Node vào đây)")]
        public List<SkillNodeData> nodes = new List<SkillNodeData>();

        [Header("Kho & bảng chọn minibot")]
        [Tooltip("Số minibot tối đa trong lưu trữ (tính cả minibot ở bảng chọn và đang triệu hồi)")] public int storageSlots = 30;
        [Tooltip("Số ô của bảng chọn (phím H)")] public int panelSlots = 4;
        [Tooltip("Số minibot được triệu hồi cùng lúc")] public int maxSummoned = 2;
        [Tooltip("Hồi máu trong lưu trữ: % máu tối đa mỗi giây")] public float storageHealPctPerSec = 3f;
        [Tooltip("Tạm thời coi MỌI NƠI là căn cứ (đưa minibot từ bảng chọn về lưu trữ chỉ làm được ở căn cứ). Tắt khi đã có căn cứ thật")] public bool anywhereIsBase = true;

        [Header("Giao tranh & thu hồi")]
        [Tooltip("Sau khi hết giao tranh bao nhiêu giây thì được bấm vào minibot để thu hồi")] public float recallDelay = 5f;
        [Tooltip("Có quái trong khoảng này quanh người chơi = đang giao tranh")] public float combatRange = 14f;

        [Header("Chỉ số minibot theo cấp độ")]
        [Tooltip("Máu tăng thêm mỗi cấp (0.06 = +6% máu gốc mỗi cấp)")] public float hpPerLevel = 0.06f;
        [Tooltip("Sát thương tăng thêm mỗi cấp")] public float damagePerLevel = 0.05f;
        [Tooltip("Khoảng cách minibot đứng cạnh người chơi khi không có địch")] public float followDistance = 3.2f;
        [Tooltip("Minibot đuổi địch trong khoảng này (tối đa)")] public float engageRange = 14f;
        [Tooltip("Minibot có tấn công trụ triệu hồi không")] public bool attackTowers = false;

        [Header("Độ trưởng thành (điều kiện để tăng cấp độ)")]
        [Tooltip("Độ trưởng thành nhận được khi minibot kết liễu quái")] public float maturityKill = 10f;
        [Tooltip("Độ trưởng thành nhận được khi minibot hỗ trợ (gây sát thương) mà quái bị người khác hạ")] public float maturityAssist = 4f;
        [Tooltip("Độ trưởng thành cần để lên cấp = Base + (cấp - 1) x PerLevel")] public float maturityBase = 30f;
        public float maturityPerLevel = 6f;
        [Tooltip("Số xu mất mỗi lần tăng cấp (0 = miễn phí)")] public int levelUpCoinCost = 0;

        [Header("Minibot bị phá huỷ")]
        [Tooltip("Số linh kiện rơi ra = Base + cấp độ x PerLevel (làm tròn)")] public float deathPartsBase = 1f;
        public float deathPartsPerLevel = 0.1f;

        public static SkillConfig Load() { return Resources.Load<SkillConfig>("NeonBreaker/SkillConfig"); }
    }
}
