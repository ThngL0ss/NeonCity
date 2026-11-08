using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Kho chứa ở căn cứ (dữ liệu, lớp static): vật phẩm + linh kiện cất ở đây KHÔNG bị mất khi chết — chỉ đồ mang trên người mới mất.
    /// Người chơi cất / lấy đồ ở giá kệ trong căn cứ (BaseStash, bấm F). Số ô: hằng Capacity.
    /// </summary>
    public static class Stash
    {
        public const int Capacity = 60;

        public static readonly List<InvSlot> Items = new List<InvSlot>();
        static readonly int[] parts = new int[PowerRules.PartRankCount];

        public static void ResetAll()
        {
            Items.Clear();
            System.Array.Clear(parts, 0, parts.Length);
        }

        public static int Parts(Rank r) { int i = (int)r; return i >= 0 && i < parts.Length ? parts[i] : 0; }

        public static int PartsTotal { get { int s = 0; for (int i = 0; i < parts.Length; i++) s += parts[i]; return s; } }

        public static int Count(ItemData it)
        {
            int c = 0;
            for (int i = 0; i < Items.Count; i++) if (Items[i].item == it) c += Items[i].count;
            return c;
        }

        /// <summary>Kho còn chỗ cho n vật phẩm này không (không thay đổi gì).</summary>
        public static bool Fits(ItemData it, int n)
        {
            if (it == null || n <= 0) return false;
            var tmp = new List<InvSlot>();
            for (int i = 0; i < Items.Count; i++) tmp.Add(new InvSlot { item = Items[i].item, count = Items[i].count });
            return Inventory.AddTo(tmp, it, n, Capacity);
        }

        /// <summary>Cất n vật phẩm ở ô index của kho đồ vào kho căn cứ.</summary>
        public static bool Deposit(int bagIndex, int n)
        {
            if (bagIndex < 0 || bagIndex >= Inventory.Slots.Count) return false;
            var s = Inventory.Slots[bagIndex];
            var it = s.item;
            if (it == null) return false;
            n = Mathf.Clamp(n, 1, s.count);
            if (!Fits(it, n))
            {
                Inventory.Say(Cfg.L("Kho căn cứ đã đầy.", "The base stash is full."));
                Sfx.Play("click", 0.6f, 0.7f);
                return false;
            }
            Inventory.AddTo(Items, it, n, Capacity);
            Inventory.Remove(bagIndex, n);
            Sfx.Play("click", 0.6f, 1.2f);
            return true;
        }

        /// <summary>Lấy n vật phẩm ở ô index của kho căn cứ về kho đồ.</summary>
        public static bool Withdraw(int stashIndex, int n)
        {
            if (stashIndex < 0 || stashIndex >= Items.Count) return false;
            var s = Items[stashIndex];
            if (s.item == null) return false;
            n = Mathf.Clamp(n, 1, s.count);
            if (!Inventory.Add(s.item, n))
            {
                Inventory.Say(Cfg.L("Kho đồ trên người đã đầy.", "Your bag is full."));
                Sfx.Play("click", 0.6f, 0.7f);
                return false;
            }
            s.count -= n;
            if (s.count <= 0) Items.RemoveAt(stashIndex);
            Sfx.Play("click", 0.6f, 0.9f);
            return true;
        }

        /// <summary>Cất tối đa n linh kiện bậc r đang mang.</summary>
        public static bool DepositParts(Rank r, int n)
        {
            n = Mathf.Min(n, SkillSystem.Parts(r));
            if (n <= 0) return false;
            SkillSystem.SpendParts(r, n);
            parts[(int)r] += n;
            Sfx.Play("click", 0.6f, 1.2f);
            return true;
        }

        /// <summary>Lấy tối đa n linh kiện bậc r về người.</summary>
        public static bool WithdrawParts(Rank r, int n)
        {
            n = Mathf.Min(n, Parts(r));
            if (n <= 0) return false;
            parts[(int)r] -= n;
            SkillSystem.AddParts(r, n, false, false);
            Sfx.Play("click", 0.6f, 0.9f);
            return true;
        }

        // ------------------------------------------------------------ lưu game
        public static void Export(SaveData d)
        {
            d.stash.Clear();
            for (int i = 0; i < Items.Count; i++)
                if (Items[i].item != null && Items[i].count > 0) d.stash.Add(new SaveItem { id = ItemDatabase.IdOf(Items[i].item), n = Items[i].count });
            d.stashParts = (int[])parts.Clone();
        }

        public static void Import(SaveData d)
        {
            ResetAll();
            for (int i = 0; i < d.stash.Count; i++)
            {
                var it = ItemDatabase.Find(d.stash[i].id);
                if (it == null) { Debug.LogWarning("Neon Breaker: không tìm thấy vật phẩm \"" + d.stash[i].id + "\" khi nạp kho căn cứ — bỏ qua."); continue; }
                int n = Mathf.Max(1, d.stash[i].n);
                if (!Inventory.AddTo(Items, it, n, int.MaxValue)) continue;
            }
            if (d.stashParts != null)
                for (int i = 0; i < d.stashParts.Length && i < parts.Length; i++) parts[i] = Mathf.Max(0, d.stashParts[i]);
        }
    }
}
