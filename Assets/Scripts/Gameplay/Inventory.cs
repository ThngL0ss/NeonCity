using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>Một ô trong kho: vật phẩm + số lượng.</summary>
    public class InvSlot
    {
        public ItemData item;
        public int count;
    }

    /// <summary>
    /// Kho đồ + ví tiền của người chơi (lớp static, không cần gắn vào object nào trong Scene).
    /// Số ô của kho chỉnh ở GameManager ▸ Inventory Slots; số xu ban đầu: GameManager ▸ Start Coins.
    /// Chỉ số buff của vật phẩm được cộng dồn ở đây rồi Player đọc qua Inventory.Mul(...) / Inventory.Stat(...).
    /// </summary>
    public static class Inventory
    {
        public static readonly List<InvSlot> Slots = new List<InvSlot>();
        public static int Coins;
        /// <summary>Dầu: nhiên liệu dùng để chế tác vật phẩm và ghép linh kiện ở căn cứ.</summary>
        public static int Oil;
        public static bool IsOpen;
        public static int Sel;
        public static string Msg = "";
        public static float MsgT;

        class TimedBuff { public ItemData src; public float left; }

        static readonly List<TimedBuff> timed = new List<TimedBuff>();
        static readonly float[] perm = new float[16];
        static readonly float[] total = new float[16];
        static int pendingCoins;
        static float coinTextT;

        public static int Capacity
        {
            get { return GameManager.Inst != null ? Mathf.Max(1, GameManager.Inst.inventorySlots) : 24; }
        }

        // ------------------------------------------------------------ reset
        public static void ResetAll()
        {
            Slots.Clear();
            timed.Clear();
            System.Array.Clear(perm, 0, perm.Length);
            System.Array.Clear(total, 0, total.Length);
            Coins = GameManager.Inst != null ? Mathf.Max(0, GameManager.Inst.startCoins) : 0;
            Oil = 0;
            pendingCoins = 0;
            coinTextT = 0f;
            IsOpen = false;
            Sel = 0;
            Msg = "";
            MsgT = 0f;
        }

        // ------------------------------------------------------------ chỉ số
        /// <summary>Tổng giá trị của một chỉ số (MaxHealth: số máu; các loại Pct: số phần trăm).</summary>
        public static float Stat(ItemStat s) { return total[(int)s]; }

        /// <summary>Hệ số nhân: 1 + phần trăm/100 (tối thiểu 0.1).</summary>
        public static float Mul(ItemStat s) { return Mathf.Max(0.1f, 1f + total[(int)s] / 100f); }

        static void AddBonuses(ItemData it, float mult)
        {
            if (it == null || it.bonuses == null) return;
            for (int i = 0; i < it.bonuses.Count; i++)
                if (it.bonuses[i] != null) total[(int)it.bonuses[i].stat] += it.bonuses[i].amount * mult;
        }

        static void Recompute()
        {
            System.Array.Copy(perm, total, perm.Length);
            for (int i = 0; i < Slots.Count; i++)
                if (Slots[i].item != null && Slots[i].item.kind == ItemKind.Passive) AddBonuses(Slots[i].item, Slots[i].count);
            for (int i = 0; i < timed.Count; i++) AddBonuses(timed[i].src, 1f);
            var pl = Player.Instance;
            if (pl != null) pl.RecalcStats();
        }

        // ------------------------------------------------------------ thêm / bớt vật phẩm
        static bool AddTo(List<InvSlot> list, ItemData it, int n)
        {
            int stack = Mathf.Max(1, it.maxStack);
            for (int i = 0; i < list.Count && n > 0; i++)
            {
                if (list[i].item != it || list[i].count >= stack) continue;
                int take = Mathf.Min(n, stack - list[i].count);
                list[i].count += take;
                n -= take;
            }
            while (n > 0)
            {
                if (list.Count >= Capacity) return false;
                int take = Mathf.Min(n, stack);
                list.Add(new InvSlot { item = it, count = take });
                n -= take;
            }
            return true;
        }

        /// <summary>Kho còn đủ chỗ cho tất cả vật phẩm này không (không thay đổi kho).</summary>
        public static bool CanAddAll(IList<KeyValuePair<ItemData, int>> items)
        {
            var tmp = new List<InvSlot>();
            for (int i = 0; i < Slots.Count; i++) tmp.Add(new InvSlot { item = Slots[i].item, count = Slots[i].count });
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Key == null || items[i].Value <= 0) continue;
                if (!AddTo(tmp, items[i].Key, items[i].Value)) return false;
            }
            return true;
        }

        public static bool Add(ItemData it, int n = 1)
        {
            if (it == null || n <= 0) return false;
            var one = new List<KeyValuePair<ItemData, int>> { new KeyValuePair<ItemData, int>(it, n) };
            if (!CanAddAll(one)) return false;
            AddTo(Slots, it, n);
            Recompute();
            return true;
        }

        public static int Count(ItemData it)
        {
            int c = 0;
            for (int i = 0; i < Slots.Count; i++) if (Slots[i].item == it) c += Slots[i].count;
            return c;
        }

        /// <summary>Lấy đi n vật phẩm loại it (từ nhiều ô nếu cần). false nếu không đủ (không lấy gì cả).</summary>
        public static bool RemoveItem(ItemData it, int n)
        {
            if (it == null || n <= 0 || Count(it) < n) return false;
            for (int i = Slots.Count - 1; i >= 0 && n > 0; i--)
            {
                if (Slots[i].item != it) continue;
                int k = Mathf.Min(n, Slots[i].count);
                Remove(i, k);
                n -= k;
            }
            return n == 0;
        }

        public static void Remove(int index, int n = 1)
        {
            if (index < 0 || index >= Slots.Count) return;
            Slots[index].count -= n;
            if (Slots[index].count <= 0) Slots.RemoveAt(index);
            Sel = Mathf.Clamp(Sel, 0, Mathf.Max(0, Slots.Count - 1));
            Recompute();
        }

        /// <summary>Bán n cái ở ô index. Trả về số xu nhận được (dành cho thương nhân sau này; chưa có shop trong Scene).</summary>
        public static int Sell(int index, int n = 1)
        {
            if (index < 0 || index >= Slots.Count) return 0;
            var s = Slots[index];
            n = Mathf.Clamp(n, 1, s.count);
            int gained = s.item.sellValue * n;
            Remove(index, n);
            Coins += gained;
            return gained;
        }

        public static int TotalSellValue()
        {
            int v = 0;
            for (int i = 0; i < Slots.Count; i++) if (Slots[i].item != null) v += Slots[i].item.sellValue * Slots[i].count;
            return v;
        }

        // ------------------------------------------------------------ dùng vật phẩm
        public static void Say(string msg)
        {
            Msg = msg;
            MsgT = 2.2f;
        }

        public static bool Use(int index)
        {
            if (index < 0 || index >= Slots.Count) return false;
            var it = Slots[index].item;
            var pl = Player.Instance;
            if (it == null) return false;
            if (it.kind == ItemKind.Passive) { Say(Cfg.L("Trang bị có tác dụng khi mang theo — không cần dùng.", "Gear works while carried — no need to use it.")); return false; }
            if (it.kind == ItemKind.Valuable) { Say(Cfg.L("Vật phẩm giá trị — bán cho thương nhân để lấy xu.", "Valuable — sell it to a merchant for coins.")); return false; }
            if (it.kind == ItemKind.Material) { Say(Cfg.L("Nguyên liệu — dùng ở bàn chế tác trong căn cứ.", "Material — used at the crafting bench in the base.")); return false; }
            if (pl == null || pl.Dead) return false;

            bool effect = false;
            if (it.healHp > 0f && pl.hp < pl.maxHp - 0.5f)
            {
                float before = pl.hp;
                pl.hp = Mathf.Min(pl.maxHp, pl.hp + it.healHp);
                Fx.Text(pl.Center + new Vector2(0f, 1.4f), "+" + Mathf.RoundToInt(pl.hp - before), Cfg.Lime, 1.2f);
                effect = true;
            }
            for (int i = 0; i < it.healCharges; i++)
                if (pl.AddHealCharge()) effect = true;
            if (it.bonuses != null && it.bonuses.Count > 0)
            {
                if (it.buffDuration > 0f)
                {
                    TimedBuff existing = null;
                    for (int i = 0; i < timed.Count; i++) if (timed[i].src == it) { existing = timed[i]; break; }
                    if (existing != null) existing.left = Mathf.Max(existing.left, it.buffDuration);
                    else timed.Add(new TimedBuff { src = it, left = it.buffDuration });
                }
                else
                {
                    for (int i = 0; i < it.bonuses.Count; i++)
                        if (it.bonuses[i] != null) perm[(int)it.bonuses[i].stat] += it.bonuses[i].amount;
                }
                effect = true;
            }
            if (!effect)
            {
                Say(Cfg.L("Dùng lúc này không có tác dụng (máu đầy / hết chỗ cộng lượt hồi).", "No effect right now (HP full / heal charges full)."));
                return false;
            }
            Sfx.Play("heal", 0.7f, 1.3f);
            Fx.Burst(pl.Center, it.color, 12, 5f, 0.45f, 0.3f);
            Say(Cfg.L("Đã dùng: ", "Used: ") + it.DisplayName);
            Remove(index, 1);
            return true;
        }

        // ------------------------------------------------------------ tiền
        /// <summary>Cộng xu. applyBonus = true: nhân với bonus "xu nhận được" từ vật phẩm.</summary>
        public static void AddCoins(int amount, bool applyBonus = true)
        {
            if (amount <= 0) return;
            if (applyBonus) amount = Mathf.Max(1, Mathf.RoundToInt(amount * Mul(ItemStat.CoinGainPct)));
            Coins += amount;
            pendingCoins += amount;
        }

        public static bool Spend(int amount)
        {
            if (amount < 0 || Coins < amount) return false;
            Coins -= amount;
            return true;
        }

        /// <summary>Cộng dầu. announce = hiện chữ nổi trên đầu người chơi.</summary>
        public static void AddOil(int amount, bool announce = true)
        {
            if (amount <= 0) return;
            Oil += amount;
            var pl = Player.Instance;
            if (announce && pl != null)
                Fx.Text(pl.Center + new Vector2(0f, 1.9f), "+" + amount + Cfg.L(" dầu", " oil"), new Color(0.95f, 0.75f, 0.25f, 1f), 1f);
        }

        public static bool SpendOil(int amount)
        {
            if (amount < 0 || Oil < amount) return false;
            Oil -= amount;
            return true;
        }

        // ------------------------------------------------------------ buff tạm (cho giao diện)
        public static int TimedCount { get { return timed.Count; } }
        public static ItemData TimedItem(int i) { return timed[i].src; }
        public static float TimedLeft(int i) { return timed[i].left; }

        // ------------------------------------------------------------ mỗi frame
        /// <summary>Gọi mỗi frame khi đang chơi (không tạm dừng): đếm ngược buff tạm, gộp chữ "+xu" nổi trên đầu.</summary>
        public static void Tick(float dt)
        {
            bool changed = false;
            for (int i = timed.Count - 1; i >= 0; i--)
            {
                timed[i].left -= dt;
                if (timed[i].left <= 0f) { timed.RemoveAt(i); changed = true; }
            }
            if (changed) Recompute();

            coinTextT -= dt;
            if (pendingCoins > 0 && coinTextT <= 0f)
            {
                var pl = Player.Instance;
                if (pl != null) Fx.Text(pl.Center + new Vector2(0f, 1.7f), "+" + pendingCoins + Cfg.L(" xu", " coins"), Cfg.Yellow, 1f);
                pendingCoins = 0;
                coinTextT = 0.3f;
            }
        }

        /// <summary>Gọi mỗi frame kể cả khi đang mở kho (dùng thời gian thật).</summary>
        public static void TickUi(float unscaledDt)
        {
            if (MsgT > 0f) MsgT -= unscaledDt;
        }
    }
}
