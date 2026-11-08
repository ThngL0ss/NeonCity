using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>Minibot đang ở đâu: lưu trữ (hồi máu dần) · bảng chọn (phím H) · đã triệu hồi ra màn chơi.</summary>
    public enum BotState { Storage, Panel, Out }

    /// <summary>Dữ liệu của một minibot (tồn tại suốt ván chơi, kể cả khi chưa / không được triệu hồi).</summary>
    public class MinibotData
    {
        public int uid;
        public string typeId = "";
        public Rank rank = Rank.F;
        public int level = 1;
        public float maturity;          // độ trưởng thành (điều kiện để tăng cấp độ)
        public float hp;
        public BotState state = BotState.Storage;
        public Minibot world;           // object trong màn chơi khi state == Out

        public CraftEntry Entry { get { return SkillSystem.Entry(typeId); } }
        public string Name { get { var e = Entry; return e != null ? e.DisplayName : typeId; } }
        public float MaxHp { get { return SkillSystem.MaxHp(this); } }
        public float Need { get { return SkillSystem.MaturityNeed(level); } }
        public bool IsMaxLevel { get { return level >= PowerRules.MaxLevel; } }
        public bool MaturityFull { get { return !IsMaxLevel && maturity >= Need - 0.001f; } }
    }

    /// <summary>
    /// Trạng thái chạy của hệ thống Skill: cây kỹ năng, linh kiện, chế tạo, kho minibot, bảng chọn, độ trưởng thành.
    /// Là static (không cần object trong Scene). Thông số chỉnh ở SkillConfig (Resources/NeonBreaker/SkillConfig.asset).
    /// </summary>
    public static class SkillSystem
    {
        public const string IdParts = "passive_parts";      // kỹ năng bị động khởi đầu: rơi / thu thập linh kiện
        public const string IdMinibot = "active_minibot";   // kỹ năng chủ động khởi đầu: triệu hồi minibot

        public static readonly List<MinibotData> Bots = new List<MinibotData>();
        static readonly int[] partCount = new int[PowerRules.PartRankCount];     // linh kiện theo cấp bậc F, D, C, B, A, S
        static readonly HashSet<string> defeated = new HashSet<string>();
        static readonly Dictionary<string, int> levels = new Dictionary<string, int>();
        static readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();

        public static int CraftedTotal, PartsCollected;
        public static bool PanelOpen;
        public static int Tab, SelBot, PanelSel;
        public static string Msg = "";
        public static float MsgT;

        static SkillConfig cfg;
        static float lastCombat = -999f, unlockT;
        static int uidCounter;

        public static SkillConfig Config
        {
            get
            {
                if (cfg == null)
                {
                    cfg = SkillConfig.Load();
                    if (cfg == null) cfg = ScriptableObject.CreateInstance<SkillConfig>();
                    for (int i = 0; i < cfg.nodes.Count; i++)
                    {
                        var n = cfg.nodes[i];
                        if (n != null && n.startUnlocked && !levels.ContainsKey(n.id)) levels[n.id] = 1;
                    }
                }
                return cfg;
            }
        }

        public static bool HasConfig { get { return Config.nodes.Count > 0 || Config.craftables.Count > 0; } }

        public static void ResetAll()
        {
            Bots.Clear();
            System.Array.Clear(partCount, 0, partCount.Length);
            defeated.Clear();
            levels.Clear();
            icons.Clear();
            CraftedTotal = 0; PartsCollected = 0; PartFusion.ResetAll();
            PanelOpen = false;
            Tab = 0; SelBot = 0; PanelSel = 0;
            Msg = ""; MsgT = 0f;
            lastCombat = -999f; unlockT = 0f; uidCounter = 0;
            cfg = null;
        }

        public static void Say(string msg) { Msg = msg; MsgT = 3.5f; }
        public static void TickUi(float unscaledDt) { if (MsgT > 0f) MsgT -= unscaledDt; }

        // ------------------------------------------------------------ linh kiện & quái đã hạ
        public static string IdOf(Enemy e)
        {
            if (e == null) return "";
            if (!string.IsNullOrEmpty(e.typeId)) return e.typeId;
            string n = e.name;
            int i = n.IndexOfAny(new[] { ' ', '(' });
            return i > 0 ? n.Substring(0, i) : n;
        }

        public static CraftEntry Entry(string id)
        {
            var list = Config.craftables;
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null && list[i].Id == id) return list[i];
            return null;
        }

        public static bool HasDefeated(string id) { return defeated.Contains(id); }

        // ---- linh kiện theo CẤP BẬC (F, D, C, B, A, S): quái bậc nào rơi linh kiện bậc đó; SS và SSS không rơi
        public static int Parts(Rank r) { int i = (int)r; return i >= 0 && i < partCount.Length ? partCount[i] : 0; }

        public static int PartsTotal { get { int s = 0; for (int i = 0; i < partCount.Length; i++) s += partCount[i]; return s; } }

        /// <summary>Chuỗi "D x3  C x1" liệt kê số linh kiện đang có theo bậc (chỉ bậc có > 0).</summary>
        public static string PartsSummary()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < partCount.Length; i++)
            {
                if (partCount[i] <= 0) continue;
                if (sb.Length > 0) sb.Append("   ");
                sb.Append((Rank)i).Append(" x").Append(partCount[i]);
            }
            return sb.Length > 0 ? sb.ToString() : "0";
        }

        public static void AddParts(Rank r, int n, bool announce, bool countCollected = true)
        {
            if (n <= 0 || !PowerRules.DropsParts(r)) return;
            partCount[(int)r] += n;
            if (countCollected) PartsCollected += n;
            if (announce)
            {
                var pl = Player.Instance;
                if (pl != null)
                    Fx.Text(pl.Center + new Vector2(0f, 1.6f), "+" + n + " " + Cfg.L("linh kiện bậc ", "part rank ") + r, Color.Lerp(PowerRules.RankColor(r), Color.white, 0.35f), 0.95f);
            }
        }

        /// <summary>Trừ n linh kiện bậc r. false (không trừ gì) nếu không đủ.</summary>
        public static bool SpendParts(Rank r, int n)
        {
            if (n < 0 || Parts(r) < n) return false;
            partCount[(int)r] -= n;
            return true;
        }

        /// <summary>Cấp bậc của loại quái này (lấy từ Enemy ▸ Power trên Prefab quái gốc). Chế tạo minibot cần linh kiện đúng bậc này.</summary>
        public static Rank RankOf(CraftEntry e)
        {
            var en = e != null && e.enemyPrefab != null ? e.enemyPrefab.GetComponent<Enemy>() : null;
            return (en != null && en.power != null) ? en.power.rank : Rank.F;
        }

        /// <summary>Enemy gọi khi bị hạ (bất kể ai hạ): ghi nhận loại quái đã hạ + rơi linh kiện.</summary>
        public static void OnEnemyKilled(Enemy e)
        {
            string id = IdOf(e);
            if (string.IsNullOrEmpty(id)) return;
            bool first = defeated.Add(id);
            var en = Entry(id);
            if (!Has(IdParts)) return;
            if (en != null && !en.craftable) return;
            if (first && en != null && en.enemyPrefab != null)
                GameManager.Banner(Cfg.L("ĐÃ GHI NHẬN: ", "RECORDED: ") + en.DisplayName + Cfg.L(" — có thể chế tạo minibot [B ▸ Skill]", " — minibot can be crafted [B ▸ Skill]"), Cfg.Cyan, 3f);
            Rank rk = e.power != null ? e.power.rank : Rank.F;
            if (!PowerRules.DropsParts(rk)) return;                 // quái bậc SS / SSS không rơi linh kiện
            float chance = en != null ? en.partsDropChance : 1f;
            if (Random.value > chance) return;
            int dmin = en != null ? en.partsDropMin : 1, dmax = en != null ? en.partsDropMax : 1;
            int lo = Mathf.Max(1, Mathf.Min(dmin, dmax));
            int hi = Mathf.Max(lo, Mathf.Max(dmin, dmax));
            float a = Random.Range(lo, hi + 1) * (1f + Stat(SkillStat.PartsDropPct) / 100f);
            int n = Mathf.FloorToInt(a);
            if (Random.value < a - n) n++;
            if (n > 0) PartDrop.Drop(e.Center, rk, n);
        }

        public static Sprite IconOf(CraftEntry e)
        {
            if (e == null || e.enemyPrefab == null) return null;
            string id = e.Id;
            Sprite s;
            if (icons.TryGetValue(id, out s)) return s;
            var v = e.enemyPrefab.transform.Find("Visual");
            var sr = v != null ? v.GetComponent<SpriteRenderer>() : null;
            s = sr != null ? sr.sprite : null;
            icons[id] = s;
            return s;
        }

        // ------------------------------------------------------------ cây kỹ năng
        public static SkillNodeData Node(string id)
        {
            var list = Config.nodes;
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null && list[i].id == id) return list[i];
            return null;
        }

        public static int LevelOf(SkillNodeData n) { int v; return n != null && levels.TryGetValue(n.id, out v) ? v : 0; }
        public static bool Unlocked(SkillNodeData n) { return LevelOf(n) > 0; }

        /// <summary>true nếu kỹ năng có id này đã mở (hoặc chưa có dữ liệu cây kỹ năng nên hệ thống chạy mặc định).</summary>
        public static bool Has(string id)
        {
            var n = Node(id);
            return n == null || Unlocked(n);
        }

        public static float Stat(SkillStat s)
        {
            float sum = 0f;
            var list = Config.nodes;
            for (int i = 0; i < list.Count; i++)
            {
                var n = list[i];
                int lv = LevelOf(n);
                if (lv <= 0 || n.effects == null) continue;
                for (int k = 0; k < n.effects.Count; k++)
                    if (n.effects[k] != null && n.effects[k].stat == s) sum += n.effects[k].perLevel * lv;
            }
            return sum;
        }

        public static bool ParentUnlocked(SkillNodeData n) { return n.parent == null || Unlocked(n.parent); }

        public static bool ConditionMet(UnlockCondition c)
        {
            switch (c.type)
            {
                case UnlockType.None: return true;
                case UnlockType.Manual: return false;
                case UnlockType.KillCount: return GameManager.Kills >= c.amount;
                case UnlockType.DefeatedEnemy: return HasDefeated(c.enemyId);
                case UnlockType.MinibotsCrafted: return CraftedTotal >= c.amount;
                case UnlockType.PartsCollected: return PartsCollected >= c.amount;
            }
            return false;
        }

        public static string ConditionText(UnlockCondition c)
        {
            string note = Cfg.Vietnamese ? c.noteVi : c.noteEn;
            if (!string.IsNullOrEmpty(note)) return note;
            switch (c.type)
            {
                case UnlockType.None: return Cfg.L("Không có điều kiện", "No condition");
                case UnlockType.Manual: return Cfg.L("Điều kiện sẽ được bổ sung sau", "Condition to be added later");
                case UnlockType.KillCount: return Cfg.L("Hạ ", "Defeat ") + c.amount + Cfg.L(" quái", " enemies") + "  (" + Mathf.Min(GameManager.Kills, c.amount) + "/" + c.amount + ")";
                case UnlockType.DefeatedEnemy: return Cfg.L("Hạ gục ", "Defeat ") + c.enemyId + (HasDefeated(c.enemyId) ? "  (OK)" : "");
                case UnlockType.MinibotsCrafted: return Cfg.L("Chế tạo ", "Craft ") + c.amount + " minibot  (" + Mathf.Min(CraftedTotal, c.amount) + "/" + c.amount + ")";
                case UnlockType.PartsCollected: return Cfg.L("Thu thập ", "Collect ") + c.amount + Cfg.L(" linh kiện", " parts") + "  (" + Mathf.Min(PartsCollected, c.amount) + "/" + c.amount + ")";
            }
            return "";
        }

        public static bool ConditionsMet(SkillNodeData n)
        {
            if (n.conditions == null || n.conditions.Count == 0) return false;
            for (int i = 0; i < n.conditions.Count; i++)
                if (n.conditions[i] == null || !ConditionMet(n.conditions[i])) return false;
            return true;
        }

        static void UnlockNode(SkillNodeData n, bool announce)
        {
            levels[n.id] = 1;
            if (announce)
            {
                GameManager.Banner(Cfg.L("KỸ NĂNG MỚI: ", "NEW SKILL: ") + n.DisplayName, Cfg.Lime, 3f);
                Sfx.Play("pickup", 0.8f, 1.3f);
            }
        }

        static void RefreshUnlocks()
        {
            var list = Config.nodes;
            for (int i = 0; i < list.Count; i++)
            {
                var n = list[i];
                if (n == null || Unlocked(n) || !ParentUnlocked(n) || !ConditionsMet(n)) continue;
                UnlockNode(n, true);
            }
        }

        /// <summary>Chế độ thử nghiệm (P): mở khoá kỹ năng bất kể điều kiện (vẫn cần mở kỹ năng cha trước).</summary>
        public static bool CanCheatUnlock(SkillNodeData n) { return GameManager.Cheat && n != null && !Unlocked(n) && ParentUnlocked(n); }

        public static void CheatUnlock(SkillNodeData n)
        {
            if (CanCheatUnlock(n)) UnlockNode(n, true);
        }

        public static bool CanUpgrade(SkillNodeData n)
        {
            int lv = LevelOf(n);
            return lv > 0 && lv < n.maxLevel;
        }

        public static bool Upgrade(SkillNodeData n)
        {
            if (!CanUpgrade(n)) return false;
            int cost = n.UpgradeCost(LevelOf(n));
            if (cost > 0 && !Inventory.Spend(cost))
            {
                Say(Cfg.L("Không đủ xu (cần " + cost + ")", "Not enough coins (need " + cost + ")"));
                Sfx.Play("click", 0.7f, 0.7f);
                return false;
            }
            levels[n.id] = LevelOf(n) + 1;
            Sfx.Play("pickup", 0.8f, 1.4f);
            Say(n.DisplayName + " -> Lv." + LevelOf(n));
            return true;
        }

        // ------------------------------------------------------------ giới hạn (có cộng kỹ năng)
        public static int PanelCapacity { get { return Mathf.Max(1, Config.panelSlots + Mathf.RoundToInt(Stat(SkillStat.PanelSlots))); } }
        public static int MaxSummoned { get { return Mathf.Max(1, Config.maxSummoned + Mathf.RoundToInt(Stat(SkillStat.MaxSummoned))); } }

        public static int PanelUsed { get { int n = 0; for (int i = 0; i < Bots.Count; i++) if (Bots[i].state != BotState.Storage) n++; return n; } }
        public static int SummonedCount { get { int n = 0; for (int i = 0; i < Bots.Count; i++) if (Bots[i].state == BotState.Out) n++; return n; } }

        /// <summary>
        /// Đang ở căn cứ chưa? Có cảnh Base thì chỉ ở trong căn cứ. Chưa có cảnh Base (chưa chạy menu cập nhật) thì dùng
        /// SkillConfig ▸ Anywhere Is Base để vẫn thử được.
        /// </summary>
        public static bool AtBase { get { return GameFlow.InBase || (Config.anywhereIsBase && !GameFlow.BaseAvailable); } }

        // ------------------------------------------------------------ chỉ số minibot
        static float BaseHealth(GameObject prefab)
        {
            if (prefab == null) return 40f;
            var w = prefab.GetComponent<Walker>();
            if (w != null) return w.health;
            var f = prefab.GetComponent<Flyer>();
            if (f != null) return f.health;
            return 40f;
        }

        public static float MaxHp(MinibotData d)
        {
            var e = d.Entry;
            float m = 1f + (d.level - 1) * Config.hpPerLevel;
            m *= 1f + Stat(SkillStat.MinibotHpPct) / 100f;
            m *= PowerRules.RankMul(d.rank);
            return Mathf.Max(1f, BaseHealth(e != null ? e.enemyPrefab : null) * m);
        }

        public static float DamageMul(MinibotData d)
        {
            float m = 1f + (d.level - 1) * Config.damagePerLevel;
            m *= 1f + Stat(SkillStat.MinibotDamagePct) / 100f;
            m *= PowerRules.RankMul(d.rank);
            return Mathf.Max(0.1f, m);
        }

        public static float MaturityNeed(int level) { return Mathf.Max(1f, Config.maturityBase + (level - 1) * Config.maturityPerLevel); }

        /// <summary>Cộng độ trưởng thành. Trả về true nếu vừa đầy (sẵn sàng tăng cấp).</summary>
        public static bool AddMaturity(MinibotData d, float amount)
        {
            if (d == null || amount <= 0f) return false;
            if (d.IsMaxLevel) { d.maturity = 0f; return false; }
            bool wasFull = d.MaturityFull;
            d.maturity = Mathf.Min(d.Need, d.maturity + amount * (1f + Stat(SkillStat.MaturityGainPct) / 100f));
            return !wasFull && d.MaturityFull;
        }

        // ------------------------------------------------------------ chế tạo
        public static int CraftCost(CraftEntry e)
        {
            return Mathf.Max(1, Mathf.RoundToInt(e.partsCost * (1f + Stat(SkillStat.CraftCostPct) / 100f)));
        }

        public static bool CanCraft(CraftEntry e)
        {
            return e != null && e.craftable && e.enemyPrefab != null && Has(IdParts) && HasDefeated(e.Id)
                   && Parts(RankOf(e)) >= CraftCost(e) && Bots.Count < Config.storageSlots;
        }

        public static MinibotData Craft(CraftEntry e)
        {
            if (e == null || !e.craftable || e.enemyPrefab == null) return null;
            if (!HasDefeated(e.Id)) { Say(Cfg.L("Chưa hạ gục loại quái này", "You have not defeated this enemy yet")); return null; }
            if (Bots.Count >= Config.storageSlots) { Say(Cfg.L("Lưu trữ minibot đã đầy", "Minibot storage is full")); return null; }
            int cost = CraftCost(e);
            Rank pr = RankOf(e);
            if (Parts(pr) < cost)
            {
                Say(Cfg.L("Thiếu linh kiện bậc " + pr + " (" + Parts(pr) + "/" + cost + ")", "Not enough rank " + pr + " parts (" + Parts(pr) + "/" + cost + ")"));
                Sfx.Play("click", 0.7f, 0.7f);
                return null;
            }
            SpendParts(pr, cost);
            var en = e.enemyPrefab.GetComponent<Enemy>();
            var d = new MinibotData();
            d.uid = ++uidCounter;
            d.typeId = e.Id;
            d.rank = (en != null && en.power != null) ? en.power.rank : Rank.F;   // cấp bậc giống quái gốc
            d.level = 1;                                                             // cấp độ khởi đầu = 1
            d.hp = d.MaxHp;
            d.state = BotState.Storage;
            Bots.Add(d);
            CraftedTotal++;
            SelBot = Bots.Count - 1;
            Sfx.Play("charge", 0.7f, 1.5f);
            Sfx.Play("pickup", 0.8f, 1.2f);
            Say(Cfg.L("Đã chế tạo minibot ", "Crafted minibot ") + e.DisplayName + "  [" + d.rank + "]  -> " + Cfg.L("lưu trữ", "storage"));
            return d;
        }

        public static void RemoveBot(MinibotData d)
        {
            int i = Bots.IndexOf(d);
            if (i < 0) return;
            Bots.RemoveAt(i);
            if (SelBot >= Bots.Count) SelBot = Mathf.Max(0, Bots.Count - 1);
        }

        // ------------------------------------------------------------ kho / bảng chọn / triệu hồi / thu hồi
        /// <summary>Lưu trữ -> bảng chọn (làm được ở mọi nơi).</summary>
        public static bool ToPanel(MinibotData d)
        {
            if (d == null || d.state != BotState.Storage) return false;
            if (PanelUsed >= PanelCapacity) { Say(Cfg.L("Bảng chọn đã đầy (" + PanelCapacity + " ô)", "Selection panel is full (" + PanelCapacity + " slots)")); Sfx.Play("click", 0.7f, 0.7f); return false; }
            d.state = BotState.Panel;
            Sfx.Play("click", 0.8f, 1.3f);
            return true;
        }

        /// <summary>Bảng chọn -> lưu trữ (chỉ ở căn cứ). Minibot được hồi máu dần khi nằm trong lưu trữ.</summary>
        public static bool ToStorage(MinibotData d)
        {
            if (d == null || d.state != BotState.Panel) return false;
            if (!AtBase) { Say(Cfg.L("Chỉ cất minibot về lưu trữ được khi ở căn cứ", "You can only store minibots at the base")); Sfx.Play("click", 0.7f, 0.7f); return false; }
            d.state = BotState.Storage;
            Sfx.Play("click", 0.8f, 0.9f);
            return true;
        }

        public static bool Summon(MinibotData d)
        {
            if (d == null || d.state != BotState.Panel || !Has(IdMinibot)) return false;
            var pl = Player.Instance;
            if (pl == null || pl.Dead || !GameManager.Active) return false;
            if (SummonedCount >= MaxSummoned)
            {
                Say(Cfg.L("Chỉ triệu hồi được tối đa " + MaxSummoned + " minibot cùng lúc", "Only " + MaxSummoned + " minibots can be summoned at once"));
                Sfx.Play("click", 0.7f, 0.7f);
                return false;
            }
            float side = Random.value < 0.5f ? -1f : 1f;
            var m = Minibot.Spawn(d, pl.Position + new Vector2(side * 1.6f, 0.15f));
            if (m == null) return false;
            d.world = m;
            d.state = BotState.Out;
            return true;
        }

        /// <summary>Số giây còn phải chờ (sau khi hết giao tranh) mới thu hồi được minibot.</summary>
        public static float RecallWait
        {
            get
            {
                float delay = Mathf.Max(0.5f, Config.recallDelay * (1f + Stat(SkillStat.RecallDelayPct) / 100f));
                return Mathf.Max(0f, delay - (Time.time - lastCombat));
            }
        }

        public static bool CanRecall { get { return RecallWait <= 0.001f; } }

        /// <summary>Thu hồi minibot đang triệu hồi về bảng chọn (giữ nguyên máu). Chỉ được khi đã hết giao tranh đủ lâu.</summary>
        public static bool Recall(MinibotData d)
        {
            if (d == null || d.state != BotState.Out || d.world == null) return false;
            if (!CanRecall)
            {
                Fx.Text(d.world.Center + new Vector2(0f, 1.4f), Cfg.L("ĐANG GIAO TRANH ", "IN COMBAT ") + Mathf.CeilToInt(RecallWait) + "s", Cfg.Orange, 1f);
                Sfx.Play("click", 0.6f, 0.7f);
                return false;
            }
            d.world.Recall();
            d.world = null;
            d.state = BotState.Panel;
            return true;
        }

        public static bool LevelUp(MinibotData d)
        {
            if (d == null) return false;
            if (d.state == BotState.Out) { Say(Cfg.L("Hãy thu hồi minibot trước khi tăng cấp", "Recall the minibot before levelling up")); return false; }
            if (d.IsMaxLevel) return false;
            float need = d.Need;
            if (d.maturity < need - 0.001f) { Say(Cfg.L("Chưa đủ độ trưởng thành", "Not enough maturity")); Sfx.Play("click", 0.7f, 0.7f); return false; }
            int cost = Config.levelUpCoinCost;
            if (cost > 0 && !Inventory.Spend(cost)) { Say(Cfg.L("Không đủ xu (cần " + cost + ")", "Not enough coins (need " + cost + ")")); return false; }
            float oldMax = d.MaxHp;
            d.maturity = Mathf.Max(0f, d.maturity - need);
            d.level++;
            d.hp = Mathf.Min(d.MaxHp, d.hp + Mathf.Max(0f, d.MaxHp - oldMax));
            Sfx.Play("charge", 0.8f, 1.6f);
            Sfx.Play("pickup", 0.9f, 1.5f);
            Say(d.Name + " -> Lv." + d.level);
            return true;
        }

        /// <summary>Thu hồi mọi minibot đang triệu hồi về bảng chọn (giữ máu). Dùng khi chuyển cảnh / người chơi gục.</summary>
        public static void RecallAllSilently()
        {
            for (int i = 0; i < Bots.Count; i++)
            {
                var d = Bots[i];
                if (d.state != BotState.Out) continue;
                if (d.world != null) { d.hp = Mathf.Clamp(d.world.hp, 1f, d.MaxHp); }
                d.world = null;
                d.state = BotState.Panel;
            }
            PanelOpen = false;
        }

        public static void TogglePanel()
        {
            if (PanelOpen) { PanelOpen = false; Sfx.Play("click", 0.8f, 0.9f); return; }
            if (!Has(IdMinibot)) return;
            if (Bots.Count == 0)
            {
                GameManager.Banner(Cfg.L("CHƯA CÓ MINIBOT — hạ quái lấy linh kiện rồi chế tạo trong mục Skill [B]", "NO MINIBOT YET — collect parts, then craft in the Skill tab [B]"), Cfg.Yellow, 3f);
                return;
            }
            if (PanelUsed == 0)
            {
                GameManager.Banner(Cfg.L("BẢNG CHỌN TRỐNG — mở [B] ▸ Skill ▸ Lưu trữ để đưa minibot ra bảng chọn", "SELECTION PANEL EMPTY — open [B] ▸ Skill ▸ Storage to move a minibot out"), Cfg.Yellow, 3f);
                return;
            }
            PanelOpen = true;
            Sfx.Play("click", 0.8f, 1.2f);
        }

        // ------------------------------------------------------------ chết / lưu game
        /// <summary>Chết: mất hết linh kiện đang mang. Trả về số linh kiện đã mất. (Bộ đếm PartsCollected cho điều kiện mở skill được giữ.)</summary>
        public static int ClearParts()
        {
            int n = PartsTotal;
            System.Array.Clear(partCount, 0, partCount.Length);
            return n;
        }

        /// <summary>Số minibot đang mang theo người (bảng chọn hoặc đang triệu hồi) — những con này mất khi chết; minibot trong lưu trữ ở căn cứ thì an toàn.</summary>
        public static int FieldBotCount
        {
            get { int n = 0; for (int i = 0; i < Bots.Count; i++) if (Bots[i].state != BotState.Storage) n++; return n; }
        }

        /// <summary>Chết: xoá mọi minibot không nằm trong lưu trữ. Trả về số minibot đã mất.</summary>
        public static int DropFieldBots()
        {
            int n = 0;
            for (int i = Bots.Count - 1; i >= 0; i--)
            {
                if (Bots[i].state == BotState.Storage) continue;
                Bots.RemoveAt(i);
                n++;
            }
            SelBot = Mathf.Clamp(SelBot, 0, Mathf.Max(0, Bots.Count - 1));
            PanelOpen = false;
            return n;
        }

        /// <summary>Ghi trạng thái skill / linh kiện / minibot vào d (lưu game).</summary>
        public static void Export(SaveData d)
        {
            var _ = Config;                                       // bảo đảm các skill khởi đầu đã có trong levels
            d.parts = (int[])partCount.Clone();
            d.crafted = CraftedTotal;
            d.partsCollected = PartsCollected;
            d.fused = PartFusion.FusedTotal;

            d.skills.Clear();
            foreach (var kv in levels) d.skills.Add(new SaveSkill { id = kv.Key, lv = kv.Value });
            d.defeated.Clear();
            foreach (var id in defeated) d.defeated.Add(id);

            d.bots.Clear();
            for (int i = 0; i < Bots.Count; i++)
            {
                var b = Bots[i];
                float hp = b.hp;
                if (b.state == BotState.Out && b.world != null) hp = b.world.hp;
                d.bots.Add(new SaveBot
                {
                    uid = b.uid, type = b.typeId, rank = (int)b.rank, level = b.level, maturity = b.maturity,
                    hp = hp, state = (int)(b.state == BotState.Out ? BotState.Panel : b.state),
                });
            }
        }

        /// <summary>Nạp trạng thái skill / linh kiện / minibot từ d (gọi sau ResetAll).</summary>
        public static void Import(SaveData d)
        {
            Bots.Clear();
            System.Array.Clear(partCount, 0, partCount.Length);
            defeated.Clear();
            levels.Clear();
            uidCounter = 0;

            if (d.parts != null)
                for (int i = 0; i < d.parts.Length && i < partCount.Length; i++) partCount[i] = Mathf.Max(0, d.parts[i]);
            CraftedTotal = d.crafted;
            PartsCollected = d.partsCollected;
            PartFusion.FusedTotal = d.fused;
            for (int i = 0; i < d.skills.Count; i++)
                if (d.skills[i] != null && !string.IsNullOrEmpty(d.skills[i].id) && d.skills[i].lv > 0) levels[d.skills[i].id] = d.skills[i].lv;
            for (int i = 0; i < d.defeated.Count; i++)
                if (!string.IsNullOrEmpty(d.defeated[i])) defeated.Add(d.defeated[i]);

            for (int i = 0; i < d.bots.Count; i++)
            {
                var s = d.bots[i];
                if (s == null || string.IsNullOrEmpty(s.type)) continue;
                var b = new MinibotData
                {
                    uid = s.uid, typeId = s.type, rank = (Rank)Mathf.Clamp(s.rank, 0, 7), level = PowerRules.ClampLevel(s.level),
                    maturity = Mathf.Max(0f, s.maturity), hp = s.hp,
                    state = s.state == (int)BotState.Storage ? BotState.Storage : BotState.Panel,
                };
                Bots.Add(b);
                if (b.uid > uidCounter) uidCounter = b.uid;
            }
            for (int i = 0; i < Bots.Count; i++)
            {
                if (Bots[i].uid <= 0) Bots[i].uid = ++uidCounter;
                Bots[i].hp = Mathf.Clamp(Bots[i].hp, 1f, Bots[i].MaxHp);
            }
            SelBot = 0; PanelSel = 0; PanelOpen = false;
            cfg = null;                                           // lần truy cập Config kế tiếp sẽ bù các skill khởi đầu còn thiếu
        }

        // ------------------------------------------------------------ cập nhật theo thời gian
        public static void NoteCombat() { lastCombat = Time.time; }

        public static void Tick(float dt)
        {
            if (GameManager.State != GameState.Playing) return;
            var pl = Player.Instance;
            if (pl != null && !pl.Dead) UpdateCombat(pl);

            float heal = Config.storageHealPctPerSec * (1f + Stat(SkillStat.StorageHealPct) / 100f) / 100f;
            for (int i = 0; i < Bots.Count; i++)
            {
                var d = Bots[i];
                if (d.state == BotState.Out) continue;
                float max = d.MaxHp;
                if (d.state == BotState.Storage && d.hp < max) d.hp = Mathf.Min(max, d.hp + max * heal * dt);
                if (d.hp > max) d.hp = max;
            }

            unlockT -= dt;
            if (unlockT <= 0f) { unlockT = 0.5f; RefreshUnlocks(); }
        }

        static void UpdateCombat(Player pl)
        {
            float r = Mathf.Max(4f, Config.combatRange);
            Vector2 pp = pl.Position;
            var list = Enemy.Live;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e == null || e.dead) continue;
                Vector2 d = (Vector2)e.transform.position - pp;
                if (Mathf.Abs(d.x) < r && Mathf.Abs(d.y) < r) { lastCombat = Time.time; return; }
            }
            var b = Boss.Instance;
            if (b != null && !b.dead && !b.Intro) lastCombat = Time.time;
        }
    }
}
