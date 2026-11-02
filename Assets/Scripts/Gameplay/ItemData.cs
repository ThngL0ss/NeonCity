using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>Loại vật phẩm.</summary>
    public enum ItemKind
    {
        Passive,      // trang bị: mang theo trong kho là có tác dụng (không cần dùng)
        Consumable,   // tiêu hao: bấm DÙNG trong kho -> hồi máu / thêm lượt hồi / buff tạm hoặc vĩnh viễn rồi biến mất
        Valuable      // chỉ có giá trị, để bán cho thương nhân
    }

    /// <summary>Chỉ số mà vật phẩm có thể cộng / trừ. Các loại "Pct" tính theo phần trăm (15 = +15%, -15 = giảm 15%).</summary>
    public enum ItemStat
    {
        MaxHealth,            // cộng thẳng vào máu tối đa
        MoveSpeedPct,         // tốc độ chạy
        MeleeDamagePct,       // sát thương chém (J)
        ShurikenDamagePct,    // sát thương phi tiêu (K)
        DamageTakenPct,       // sát thương nhận vào (âm = giáp)
        DashCooldownPct,      // thời gian hồi chiêu lướt (âm = hồi nhanh hơn)
        HealPowerPct,         // lượng máu hồi mỗi lần bấm L
        CoinGainPct,          // lượng xu nhận được
        MagnetRangePct        // tầm hút vật phẩm / xu
    }

    [System.Serializable]
    public class ItemStatBonus
    {
        public ItemStat stat;
        [Tooltip("MaxHealth: số máu cộng thêm. Các chỉ số khác: phần trăm (15 = +15%, -15 = -15%)")]
        public float amount;
    }

    /// <summary>
    /// Dữ liệu một vật phẩm (file asset). Tạo mới: chuột phải trong Project ▸ Create ▸ Neon Breaker ▸ Item.
    /// Sau đó kéo vào Bảng vật phẩm của rương (Chest ▸ Loot Table).
    /// </summary>
    [CreateAssetMenu(menuName = "Neon Breaker/Item", fileName = "NewItem")]
    public class ItemData : ScriptableObject
    {
        [Header("Tên & mô tả")]
        public string nameVi = "Vật phẩm";
        public string nameEn = "Item";
        [TextArea(2, 4)] public string descVi;
        [TextArea(2, 4)] public string descEn;
        public Sprite icon;
        [Tooltip("Màu viền / tên trong kho (độ hiếm)")] public Color color = new Color(0.10f, 0.95f, 1.00f, 1f);

        [Header("Loại & giá")]
        public ItemKind kind = ItemKind.Valuable;
        [Tooltip("Số lượng tối đa xếp chung một ô")] public int maxStack = 99;
        [Tooltip("Giá bán cho thương nhân (1 cái)")] public int sellValue = 10;

        [Header("Hiệu ứng (Passive: có tác dụng khi mang theo · Consumable: khi dùng)")]
        public List<ItemStatBonus> bonuses = new List<ItemStatBonus>();

        [Header("Chỉ dành cho Consumable")]
        [Tooltip("Máu hồi ngay khi dùng")] public float healHp = 0f;
        [Tooltip("Số lượt hồi (phím L) cộng thêm khi dùng")] public int healCharges = 0;
        [Tooltip("Thời gian buff (giây). > 0: buff tạm thời. 0: các chỉ số ở trên cộng VĨNH VIỄN")] public float buffDuration = 0f;

        public string DisplayName
        {
            get { return Cfg.Vietnamese ? (string.IsNullOrEmpty(nameVi) ? name : nameVi) : (string.IsNullOrEmpty(nameEn) ? name : nameEn); }
        }

        public string Description
        {
            get
            {
                string d = Cfg.Vietnamese ? descVi : descEn;
                if (string.IsNullOrEmpty(d)) d = Cfg.Vietnamese ? descEn : descVi;
                return d ?? "";
            }
        }

        public static string StatLabel(ItemStat s, float amount)
        {
            string a = (amount >= 0f ? "+" : "") + amount.ToString("0.#");
            switch (s)
            {
                case ItemStat.MaxHealth: return a + Cfg.L(" máu tối đa", " max HP");
                case ItemStat.MoveSpeedPct: return a + Cfg.L("% tốc độ chạy", "% move speed");
                case ItemStat.MeleeDamagePct: return a + Cfg.L("% sát thương chém", "% melee damage");
                case ItemStat.ShurikenDamagePct: return a + Cfg.L("% sát thương phi tiêu", "% shuriken damage");
                case ItemStat.DamageTakenPct: return a + Cfg.L("% sát thương nhận vào", "% damage taken");
                case ItemStat.DashCooldownPct: return a + Cfg.L("% thời gian hồi lướt", "% dash cooldown");
                case ItemStat.HealPowerPct: return a + Cfg.L("% lượng máu hồi", "% healing");
                case ItemStat.CoinGainPct: return a + Cfg.L("% xu nhận được", "% coins gained");
                case ItemStat.MagnetRangePct: return a + Cfg.L("% tầm hút vật phẩm", "% pickup magnet range");
            }
            return a;
        }

        /// <summary>Các dòng mô tả hiệu ứng, mỗi dòng một chỉ số.</summary>
        public string EffectLines()
        {
            var sb = new StringBuilder();
            if (healHp > 0f) sb.AppendLine("• " + Cfg.L("Hồi ", "Heal ") + Mathf.RoundToInt(healHp) + " HP");
            if (healCharges != 0) sb.AppendLine("• " + (healCharges > 0 ? "+" : "") + healCharges + Cfg.L(" lượt hồi máu (L)", " heal charge(s) (L)"));
            if (bonuses != null)
                for (int i = 0; i < bonuses.Count; i++)
                    if (bonuses[i] != null && Mathf.Abs(bonuses[i].amount) > 0.0001f)
                        sb.AppendLine("• " + StatLabel(bonuses[i].stat, bonuses[i].amount));
            if (kind == ItemKind.Consumable && bonuses != null && bonuses.Count > 0)
                sb.AppendLine(buffDuration > 0f ? Cfg.L("  (kéo dài ", "  (lasts ") + buffDuration.ToString("0") + Cfg.L(" giây)", " s)") : Cfg.L("  (vĩnh viễn)", "  (permanent)"));
            return sb.ToString().TrimEnd();
        }

        public string KindLabel
        {
            get
            {
                switch (kind)
                {
                    case ItemKind.Passive: return Cfg.L("TRANG BỊ — có tác dụng khi mang theo", "GEAR — active while carried");
                    case ItemKind.Consumable: return Cfg.L("TIÊU HAO — bấm DÙNG", "CONSUMABLE — press USE");
                    default: return Cfg.L("VẬT PHẨM GIÁ TRỊ — bán cho thương nhân", "VALUABLE — sell to a merchant");
                }
            }
        }
    }
}
