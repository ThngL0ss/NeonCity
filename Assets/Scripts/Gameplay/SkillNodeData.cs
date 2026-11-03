using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    public enum SkillBranch { Passive, Active }

    /// <summary>Loại điều kiện mở khoá một kỹ năng. Thêm loại mới: thêm vào enum này + hàm SkillSystem.ConditionMet / ConditionText.</summary>
    public enum UnlockType
    {
        None,   // Luôn thoả
        Manual,   // Chưa có điều kiện (sẽ bổ sung sau). Chỉ mở được bằng chế độ thử nghiệm (P)
        KillCount,   // Tổng số quái đã hạ trong ván >= Amount
        DefeatedEnemy,   // Đã hạ ít nhất 1 con quái có mã Enemy Id
        MinibotsCrafted,   // Đã chế tạo tổng cộng >= Amount minibot
        PartsCollected,   // Đã thu thập tổng cộng >= Amount linh kiện
    }

    /// <summary>Một điều kiện mở khoá. Kỹ năng tự mở khi MỌI điều kiện đều thoả (và kỹ năng cha đã mở).</summary>
    [System.Serializable]
    public class UnlockCondition
    {
        public UnlockType type = UnlockType.Manual;
        [Tooltip("Dùng cho KillCount / MinibotsCrafted / PartsCollected")] public int amount = 1;
        [Tooltip("Dùng cho DefeatedEnemy: mã loại quái, ví dụ Rusher")] public string enemyId = "";
        [Tooltip("Ghi chú hiện trong giao diện (tuỳ chọn)")] public string noteVi = "";
        public string noteEn = "";
    }

    /// <summary>Các chỉ số mà kỹ năng có thể cộng thêm (mỗi cấp kỹ năng cộng một lượng).</summary>
    public enum SkillStat
    {
        PartsDropPct,   // +% số linh kiện rơi ra từ quái
        CraftCostPct,   // -% linh kiện cần để chế tạo (dùng số âm)
        PanelSlots,   // + số ô của bảng chọn minibot
        MaxSummoned,   // + số minibot được triệu hồi cùng lúc
        RecallDelayPct,   // -% thời gian chờ để thu hồi sau giao tranh (dùng số âm)
        MaturityGainPct,   // +% độ trưởng thành nhận được
        StorageHealPct,   // +% tốc độ hồi máu trong lưu trữ
        MinibotHpPct,   // +% máu của minibot
        MinibotDamagePct,   // +% sát thương của minibot
    }

    [System.Serializable]
    public class SkillEffect
    {
        public SkillStat stat;
        [Tooltip("Lượng cộng thêm cho MỖI cấp của kỹ năng")] public float perLevel = 10f;
    }

    /// <summary>
    /// Một kỹ năng trong cây kỹ năng (Create ▸ Neon Breaker ▸ Skill Node). Thêm kỹ năng mới: tạo file này,
    /// chọn nhánh (Branch), kéo kỹ năng cha vào Parent (hoặc để trống), rồi kéo vào SkillConfig ▸ Nodes.
    /// </summary>
    [CreateAssetMenu(menuName = "Neon Breaker/Skill Node", fileName = "Skill_New")]
    public class SkillNodeData : ScriptableObject
    {
        [Tooltip("Mã duy nhất, ví dụ passive_parts")] public string id = "skill_new";
        public string nameVi = "Kỹ năng";
        public string nameEn = "Skill";
        [TextArea(2, 4)] public string descVi;
        [TextArea(2, 4)] public string descEn;
        public Sprite icon;
        public SkillBranch branch = SkillBranch.Passive;
        [Tooltip("Kỹ năng cha: phải mở cha trước. Để trống = kỹ năng gốc của nhánh")] public SkillNodeData parent;
        [Tooltip("Thứ tự hiển thị trong nhánh (nhỏ ở trên)")] public int order = 0;

        [Header("Mở khoá")]
        [Tooltip("Có sẵn ngay từ đầu ván (kỹ năng khởi đầu)")] public bool startUnlocked = false;
        [Tooltip("Điều kiện mở khoá (sẽ bổ sung sau). Không có điều kiện nào = chưa mở được trừ chế độ thử nghiệm")] public List<UnlockCondition> conditions = new List<UnlockCondition>();

        [Header("Nâng cấp")]
        [Tooltip("Số cấp tối đa")] public int maxLevel = 1;
        [Tooltip("Giá (xu) để lên cấp 2 = base; các cấp sau cộng thêm Growth mỗi cấp")] public int upgradeCostBase = 60;
        public int upgradeCostGrowth = 40;
        [Tooltip("Hiệu ứng khi kỹ năng đạt cấp (nhân với số cấp)")] public List<SkillEffect> effects = new List<SkillEffect>();

        public string DisplayName { get { return Cfg.Vietnamese ? nameVi : nameEn; } }
        public string Description { get { return Cfg.Vietnamese ? descVi : descEn; } }

        public int UpgradeCost(int currentLevel)
        {
            return Mathf.Max(0, upgradeCostBase + upgradeCostGrowth * Mathf.Max(0, currentLevel - 1));
        }
    }
}
