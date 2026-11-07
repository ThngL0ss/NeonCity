using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Giao diện hệ thống Skill (IMGUI, vẽ qua các hàm của Hud):
    ///  • Mục Skill của menu B (phím 2): tab CÂY SKILL · CHẾ TẠO · LƯU TRỮ MINIBOT. Q / E chuyển tab, chuột bấm các nút.
    ///  • Bảng chọn minibot (phím H): hiện ngay trên màn chơi, KHÔNG tạm dừng game. Bấm vào minibot để triệu hồi.
    ///  • Nhãn trên đầu minibot đang triệu hồi: bấm vào minibot để thu hồi (sau khi hết giao tranh đủ lâu).
    /// </summary>
    public static class SkillUi
    {
        const int TabCount = 4;      // tab cuối là chỗ dành sẵn cho tính năng sau
        const int Cols = 5;

        static readonly List<SkillNodeData> orderBuf = new List<SkillNodeData>();
        static readonly List<int> depthBuf = new List<int>();

        static Color Dim(Color c, float a) { return new Color(c.r, c.g, c.b, c.a * a); }

        static bool Hover(Rect r) { return r.Contains(Event.current.mousePosition); }

        static bool Click(Rect r)
        {
            var ev = Event.current;
            if (ev.type == EventType.MouseDown && ev.button == 0 && r.Contains(ev.mousePosition))
            {
                ev.Use();
                return true;
            }
            return false;
        }

        static Rect Pad(Rect r, float p) { return new Rect(r.x + p, r.y + p, r.width - p * 2f, r.height - p * 2f); }

        static string TabName(int i)
        {
            switch (i)
            {
                case 0: return Cfg.L("CÂY SKILL", "SKILL TREE");
                case 1: return Cfg.L("CHẾ TẠO", "CRAFT");
                case 2: return Cfg.L("LƯU TRỮ MINIBOT", "MINIBOT STORAGE");
            }
            return "...";
        }

        static void SetTab(int t)
        {
            t = Mathf.Clamp(t, 0, 2);
            if (t == SkillSystem.Tab) return;
            SkillSystem.Tab = t;
            Sfx.Play("click", 0.5f, 1.3f);
        }

        static Rank RankOf(CraftEntry e) { return SkillSystem.RankOf(e); }

        // ============================================================ nhập liệu bàn phím (khi mục Skill đang mở)
        public static void UpdateInput()
        {
            if (GameInput.TabPrevDown) SetTab(SkillSystem.Tab - 1);
            if (GameInput.TabNextDown) SetTab(SkillSystem.Tab + 1);
            if (SkillSystem.Tab == 2 && SkillSystem.Bots.Count > 0)
            {
                int sel = SkillSystem.SelBot;
                if (GameInput.NavLeftDown) sel--;
                if (GameInput.NavRightDown) sel++;
                if (GameInput.NavUpDown) sel -= Cols;
                if (GameInput.NavDownDown) sel += Cols;
                sel = Mathf.Clamp(sel, 0, SkillSystem.Bots.Count - 1);
                if (sel != SkillSystem.SelBot) Sfx.Play("click", 0.4f, 1.3f);
                SkillSystem.SelBot = sel;
            }
        }

        // ============================================================ nhập liệu khi bảng chọn minibot (H) đang mở
        static List<MinibotData> PanelItems()
        {
            var items = new List<MinibotData>();
            var bots = SkillSystem.Bots;
            for (int i = 0; i < bots.Count; i++) if (bots[i].state != BotState.Storage) items.Add(bots[i]);
            return items;
        }

        /// <summary>Bảng chọn đang mở: người chơi đứng yên. A / D đổi ô chọn, S triệu hồi (hoặc thu hồi nếu đã triệu hồi).</summary>
        public static void UpdatePanelInput()
        {
            var items = PanelItems();
            if (items.Count == 0) { SkillSystem.PanelOpen = false; return; }
            int sel = Mathf.Clamp(SkillSystem.PanelSel, 0, items.Count - 1);
            if (GameInput.NavLeftDown) sel--;
            if (GameInput.NavRightDown) sel++;
            sel = Mathf.Clamp(sel, 0, items.Count - 1);
            if (sel != SkillSystem.PanelSel) Sfx.Play("click", 0.4f, 1.3f);
            SkillSystem.PanelSel = sel;
            if (GameInput.NavDownDown)
            {
                var d = items[sel];
                if (d.state == BotState.Panel) SkillSystem.Summon(d);
                else if (d.state == BotState.Out) SkillSystem.Recall(d);
            }
        }

        // ============================================================ MỤC SKILL
        public static void DrawContent(Hud h)
        {
            DrawTabs(h);

            if (!SkillSystem.HasConfig)
            {
                h.TxtWrap(h.R(200, 220, 880, 120),
                          Cfg.L("Chưa có dữ liệu Skill. Hãy chạy menu  Tools > Neon Breaker > Thêm hệ thống Skill (không đụng Scene)  trong Unity rồi chơi lại.",
                                "No skill data yet. Run the menu  Tools > Neon Breaker > Add Skill system  in Unity, then play again."),
                          18, Cfg.Yellow, TextAnchor.UpperCenter);
            }
            else
            {
                switch (SkillSystem.Tab)
                {
                    case 0: DrawTree(h); break;
                    case 1: DrawCraft(h); break;
                    default: DrawStorage(h); break;
                }
            }

            if (SkillSystem.MsgT > 0f && !string.IsNullOrEmpty(SkillSystem.Msg))
                h.Txt(h.R(176, 620, 928, 22), SkillSystem.Msg, 15, Cfg.Yellow, TextAnchor.UpperLeft, false);
            h.Txt(h.R(176, 644, 940, 20),
                  Cfg.L(GameMenu.CommonHintVi + "   ·   [Q / E] chuyển tab con   ·   [A D W S] chọn minibot (tab Lưu trữ)   ·   chuột: bấm các nút",
                        GameMenu.CommonHintEn + "   ·   [Q / E] switch sub-tab   ·   [A D W S] select minibot (Storage tab)   ·   mouse: click buttons"),
                  13, Cfg.A(Color.white, 0.6f), TextAnchor.UpperLeft, false);
        }

        static void DrawTabs(Hud h)
        {
            for (int i = 0; i < TabCount; i++)
            {
                Rect r = h.R(176 + i * 232, 106, 222, 34);
                bool enabled = i < 3;
                bool sel = enabled && SkillSystem.Tab == i;
                bool hover = enabled && Hover(r);
                h.Fill(r, new Color(0.02f, 0.03f, 0.09f, 0.95f));
                if (sel) h.Fill(r, Cfg.A(Cfg.Cyan, 0.22f));
                else if (hover) h.Fill(r, Cfg.A(Cfg.Cyan, 0.10f));
                h.Outline(r, sel ? Cfg.Cyan : Cfg.A(Cfg.Cyan, enabled ? 0.4f : 0.15f), sel ? 2.5f : 1.5f);
                h.Txt(r, TabName(i), 16, enabled ? (sel ? Color.white : Cfg.A(Cfg.Cyan, 0.9f)) : Cfg.A(Color.white, 0.25f), TextAnchor.MiddleCenter);
                if (enabled && Click(r)) SetTab(i);
            }
        }

        // ============================================================ TAB 1: CÂY SKILL
        static void DrawTree(Hud h)
        {
            if (SkillSystem.Config.nodes.Count == 0)
            {
                h.Txt(h.R(176, 200, 928, 30), Cfg.L("Chưa có kỹ năng nào trong SkillConfig > Nodes", "No skills in SkillConfig > Nodes"), 16, Cfg.A(Color.white, 0.6f), TextAnchor.UpperCenter);
                return;
            }
            DrawBranch(h, SkillBranch.Passive, 176f, Cfg.L("NHÁNH BỊ ĐỘNG", "PASSIVE BRANCH"), Cfg.Lime);
            DrawBranch(h, SkillBranch.Active, 654f, Cfg.L("NHÁNH CHỦ ĐỘNG", "ACTIVE BRANCH"), Cfg.Magenta);
        }

        static void AddWithChildren(SkillNodeData n, int depth, SkillBranch br, List<SkillNodeData> all)
        {
            if (orderBuf.Contains(n) || depth > 6) return;
            orderBuf.Add(n);
            depthBuf.Add(depth);
            var kids = new List<SkillNodeData>();
            for (int i = 0; i < all.Count; i++)
                if (all[i] != null && all[i].parent == n && all[i].branch == br) kids.Add(all[i]);
            kids.Sort((a, b) => a.order.CompareTo(b.order));
            for (int i = 0; i < kids.Count; i++) AddWithChildren(kids[i], depth + 1, br, all);
        }

        static void BuildOrder(SkillBranch br)
        {
            orderBuf.Clear();
            depthBuf.Clear();
            var all = SkillSystem.Config.nodes;
            var roots = new List<SkillNodeData>();
            for (int i = 0; i < all.Count; i++)
            {
                var n = all[i];
                if (n == null || n.branch != br) continue;
                if (n.parent == null || n.parent.branch != br || !all.Contains(n.parent)) roots.Add(n);
            }
            roots.Sort((a, b) => a.order.CompareTo(b.order));
            for (int i = 0; i < roots.Count; i++) AddWithChildren(roots[i], 0, br, all);
        }

        static void DrawBranch(Hud h, SkillBranch br, float x, string title, Color color)
        {
            const float w = 450f;
            h.Txt(h.R(x, 148, w, 24), title, 17, color);
            h.Fill(h.R(x, 172, w, 2), Cfg.A(color, 0.5f));
            BuildOrder(br);
            int n = orderBuf.Count;
            if (n == 0)
            {
                h.Txt(h.R(x, 190, w, 24), Cfg.L("(chưa có kỹ năng)", "(no skills)"), 14, Cfg.A(Color.white, 0.5f), TextAnchor.UpperLeft, false);
                return;
            }
            float pitch = Mathf.Min(78f, 436f / n);
            float ch = pitch - 6f;
            for (int i = 0; i < n; i++) DrawNode(h, orderBuf[i], depthBuf[i], x, 180f + i * pitch, w, ch, color);
        }

        static void DrawNode(Hud h, SkillNodeData n, int depth, float x, float y, float w, float ch, Color color)
        {
            float ind = depth * 16f;
            int lv = SkillSystem.LevelOf(n);
            bool unlocked = lv > 0;
            Rect r = h.R(x + ind, y, w - ind, ch);
            h.Fill(r, new Color(0.02f, 0.03f, 0.09f, 0.92f));
            if (unlocked) h.Fill(r, Cfg.A(color, 0.08f));
            h.Outline(r, Cfg.A(color, unlocked ? 0.8f : 0.3f), 1.5f);

            if (depth > 0)
            {
                h.Fill(h.R(x + ind - 10f, y - 8f, 2f, ch * 0.5f + 8f), Cfg.A(color, 0.5f));
                h.Fill(h.R(x + ind - 10f, y + ch * 0.5f - 1f, 10f, 2f), Cfg.A(color, 0.5f));
            }

            Rect ic = h.R(x + ind + 6f, y + (ch - 40f) * 0.5f, 40f, 40f);
            h.Fill(ic, new Color(0.01f, 0.02f, 0.06f, 0.9f));
            h.Outline(ic, Cfg.A(color, unlocked ? 0.8f : 0.3f), 1.5f);
            if (n.icon != null) h.DrawSprite(n.icon, Pad(ic, 4f * h.s), unlocked ? Color.white : new Color(0.35f, 0.35f, 0.42f, 1f));
            else h.Txt(ic, (n.DisplayName + "?").Substring(0, 1), 20, Cfg.A(color, unlocked ? 1f : 0.4f), TextAnchor.MiddleCenter);

            float tx = x + ind + 54f;
            float tw = w - ind - 54f - 118f;
            string head = n.DisplayName + (unlocked ? "   Lv." + lv + "/" + n.maxLevel : "");
            h.Txt(h.R(tx, y + 3f, tw + 60f, 20f), head, 15, unlocked ? color : Cfg.A(Color.white, 0.55f), TextAnchor.UpperLeft, true);
            h.TxtWrap(h.R(tx, y + 22f, tw + 60f, 30f), n.Description, 11f, Cfg.A(Color.white, unlocked ? 0.8f : 0.5f));

            Rect btn = h.R(x + w - 114f, y + ch * 0.5f - 15f, 108f, 30f);
            if (unlocked)
            {
                if (SkillSystem.CanUpgrade(n))
                {
                    int cost = n.UpgradeCost(lv);
                    if (h.Button(btn, Cfg.L("NÂNG CẤP ", "UPGRADE ") + (cost > 0 ? cost + "xu" : ""), Cfg.Yellow, 12)) SkillSystem.Upgrade(n);
                }
                else if (n.maxLevel > 1)
                    h.Txt(btn, Cfg.L("TỐI ĐA", "MAX"), 14, Cfg.A(Cfg.Lime, 0.9f), TextAnchor.MiddleCenter);
                else
                    h.Txt(btn, Cfg.L("ĐÃ MỞ", "UNLOCKED"), 13, Cfg.A(Cfg.Lime, 0.8f), TextAnchor.MiddleCenter);
            }
            else
            {
                if (SkillSystem.CanCheatUnlock(n))
                {
                    if (h.Button(btn, Cfg.L("MỞ (THỬ NGHIỆM)", "UNLOCK (TEST)"), Cfg.Orange, 11)) SkillSystem.CheatUnlock(n);
                }
                else h.Txt(btn, Cfg.L("KHOÁ", "LOCKED"), 13, Cfg.A(Color.white, 0.4f), TextAnchor.MiddleCenter);

                string lockText;
                if (!SkillSystem.ParentUnlocked(n)) lockText = Cfg.L("Cần mở trước: ", "Unlock first: ") + n.parent.DisplayName;
                else if (n.conditions == null || n.conditions.Count == 0) lockText = Cfg.L("Điều kiện sẽ được bổ sung sau (bật chế độ thử nghiệm [P] để mở)", "Condition to be added later (use test mode [P] to unlock)");
                else
                {
                    var sb = new System.Text.StringBuilder();
                    for (int i = 0; i < n.conditions.Count; i++)
                    {
                        if (n.conditions[i] == null) continue;
                        if (sb.Length > 0) sb.Append("  ·  ");
                        sb.Append(SkillSystem.ConditionText(n.conditions[i]));
                    }
                    lockText = sb.ToString();
                }
                h.Txt(h.R(tx, y + ch - 17f, tw + 60f, 16f), lockText, 11, Cfg.A(Cfg.Orange, 0.9f), TextAnchor.UpperLeft, false);
            }
        }

        // ============================================================ TAB 2: CHẾ TẠO
        static void DrawCraft(Hud h)
        {
            var list = SkillSystem.Config.craftables;
            h.TxtWrap(h.R(176, 146, 928, 36),
                      Cfg.L("Quái bậc nào rơi LINH KIỆN bậc đó (SS, SSS không rơi). Đủ linh kiện đúng bậc thì chế tạo minibot của loại quái đã từng hạ: cấp bậc giống quái gốc, cấp độ 1, cất ở LƯU TRỮ MINIBOT. Ghép linh kiện lên bậc cao ở căn cứ.",
                            "Enemies drop PARTS of their own rank (SS, SSS drop none). Enough parts of that rank craft a minibot of a type you have defeated: same rank as the original, level 1, kept in MINIBOT STORAGE. Fuse parts into higher ranks at the base."),
                      13, Cfg.A(Color.white, 0.7f));

            if (!SkillSystem.Has(SkillSystem.IdParts))
            {
                h.Txt(h.R(176, 230, 928, 30), Cfg.L("Cần mở kỹ năng bị động \"Thu thập linh kiện\"", "Unlock the passive skill \"Part Collector\""), 17, Cfg.Orange, TextAnchor.UpperCenter);
                return;
            }
            if (list.Count == 0)
            {
                h.Txt(h.R(176, 230, 928, 30), Cfg.L("Chưa có loại quái nào trong SkillConfig > Craftables", "No entries in SkillConfig > Craftables"), 16, Cfg.A(Color.white, 0.6f), TextAnchor.UpperCenter);
                return;
            }

            float pitch = Mathf.Min(74f, 410f / list.Count);
            float ch = pitch - 6f;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e == null || !e.craftable) continue;
                DrawCraftRow(h, e, 190f + i * pitch, ch);
            }
            h.Txt(h.R(176, 596, 928, 22), Cfg.L("Lưu trữ minibot: ", "Minibot storage: ") + SkillSystem.Bots.Count + " / " + SkillSystem.Config.storageSlots, 14, Cfg.A(Color.white, 0.7f), TextAnchor.UpperLeft, false);
        }

        static void DrawCraftRow(Hud h, CraftEntry e, float y, float ch)
        {
            bool known = SkillSystem.HasDefeated(e.Id);
            int have = SkillSystem.Parts(RankOf(e));
            int cost = SkillSystem.CraftCost(e);
            bool can = SkillSystem.CanCraft(e);
            Color col = known ? Color.Lerp(e.partColor, Color.white, 0.25f) : new Color(0.5f, 0.52f, 0.6f, 1f);

            Rect r = h.R(176, y, 928, ch);
            h.Fill(r, new Color(0.02f, 0.03f, 0.09f, 0.92f));
            if (can) h.Fill(r, Cfg.A(Cfg.Lime, 0.07f));
            h.Outline(r, Cfg.A(col, known ? 0.7f : 0.25f), 1.5f);

            Rect ic = h.R(186, y + (ch - 48f) * 0.5f, 48f, 48f);
            h.Fill(ic, new Color(0.01f, 0.02f, 0.06f, 0.9f));
            h.Outline(ic, Cfg.A(col, known ? 0.8f : 0.3f), 1.5f);
            var spr = SkillSystem.IconOf(e);
            if (spr != null) h.DrawSprite(spr, Pad(ic, 3f * h.s), known ? Color.white : new Color(0.04f, 0.05f, 0.1f, 1f));

            string name = known ? e.DisplayName : "???";
            h.Txt(h.R(246, y + 6f, 300, 24), name, 19, known ? col : Cfg.A(Color.white, 0.45f));
            if (known)
            {
                Rank rk = RankOf(e);
                h.Txt(h.R(246, y + 6f, 480, 24), "[" + rk + "]", 15, PowerRules.RankColor(rk), TextAnchor.UpperRight);
                h.Txt(h.R(246, y + 32f, 160, 20), Cfg.L("Linh kiện " + RankOf(e) + ": ", "Parts " + RankOf(e) + ": ") + have + " / " + cost, 14, have >= cost ? Cfg.Lime : Cfg.A(Color.white, 0.8f), TextAnchor.UpperLeft, false);
                h.NeonBar(h.R(410, y + 36f, 316, 10), cost > 0 ? (float)have / cost : 1f, have >= cost ? Cfg.Lime : Cfg.Cyan, new Color(0f, 0f, 0f, 0.6f));
            }
            else
            {
                h.Txt(h.R(246, y + 32f, 600, 20), Cfg.L("Hạ gục loại quái này ít nhất 1 lần để mở công thức", "Defeat this enemy at least once to unlock the recipe"), 13, Cfg.A(Cfg.Orange, 0.85f), TextAnchor.UpperLeft, false);
            }

            if (known)
            {
                Rect btn = h.R(930, y + (ch - 40f) * 0.5f, 164f, 40f);
                if (h.Button(btn, Cfg.L("CHẾ TẠO", "CRAFT"), can ? Cfg.Lime : Cfg.A(Cfg.Red, 0.8f), 18)) SkillSystem.Craft(e);
            }
        }

        // ============================================================ TAB 3: LƯU TRỮ MINIBOT
        static void DrawStorage(Hud h)
        {
            var cfg = SkillSystem.Config;
            int cap = cfg.storageSlots;
            var bots = SkillSystem.Bots;

            h.Txt(h.R(176, 146, 440, 22), Cfg.L("LƯU TRỮ  ", "STORAGE  ") + bots.Count + " / " + cap, 15, Cfg.Cyan);
            h.Txt(h.R(176, 168, 440, 20), Cfg.L("Bảng chọn: ", "Panel: ") + SkillSystem.PanelUsed + "/" + SkillSystem.PanelCapacity
                  + Cfg.L("   ·   Triệu hồi: ", "   ·   Summoned: ") + SkillSystem.SummonedCount + "/" + SkillSystem.MaxSummoned,
                  13, Cfg.A(Color.white, 0.7f), TextAnchor.UpperLeft, false);

            int rows = Mathf.CeilToInt(cap / (float)Cols);
            float size = 80f, pitch = 88f, pitchY = Mathf.Min(86f, 420f / rows);
            float gx = 176f, gy = 194f;
            for (int i = 0; i < cap; i++)
            {
                int cx = i % Cols, cy = i / Cols;
                Rect r = h.R(gx + cx * pitch, gy + cy * pitchY, size, pitchY - 6f);
                MinibotData d = i < bots.Count ? bots[i] : null;
                bool sel = d != null && i == SkillSystem.SelBot;
                bool hover = Hover(r);
                Color border = d != null ? StateColor(d.state) : Cfg.Cyan;

                h.Fill(r, new Color(0.02f, 0.03f, 0.09f, 0.95f));
                if (sel) h.Fill(r, Cfg.A(border, 0.22f));
                else if (hover && d != null) h.Fill(r, Cfg.A(border, 0.10f));
                if (d != null)
                {
                    var spr = SkillSystem.IconOf(d.Entry);
                    Rect ic = new Rect(r.x + 4f * h.s, r.y + 3f * h.s, r.width - 8f * h.s, r.height - 30f * h.s);
                    if (spr != null) h.DrawSprite(spr, ic, Color.white);
                    h.Txt(new Rect(r.x + 3f * h.s, r.y + 2f * h.s, r.width, 16f * h.s), d.rank.ToString(), 11, PowerRules.RankColor(d.rank), TextAnchor.UpperLeft);
                    h.Txt(new Rect(r.x, r.y + 2f * h.s, r.width - 3f * h.s, 16f * h.s), "Lv." + d.level, 11, Color.white, TextAnchor.UpperRight);
                    h.NeonBar(new Rect(r.x + 4f * h.s, r.yMax - 14f * h.s, r.width - 8f * h.s, 5f * h.s), d.hp / Mathf.Max(1f, d.MaxHp), Cfg.Lime, new Color(0f, 0f, 0f, 0.6f));
                    h.Txt(new Rect(r.x, r.yMax - 12f * h.s, r.width, 12f * h.s), StateTag(d.state), 9, border, TextAnchor.UpperCenter, false, false);
                    if (d.MaturityFull) h.Txt(new Rect(r.x, r.y + 16f * h.s, r.width - 3f * h.s, 14f * h.s), Cfg.L("LÊN CẤP", "LEVEL UP"), 12, Cfg.Lime, TextAnchor.UpperRight);
                }
                h.Outline(r, sel ? Color.white : Cfg.A(border, d != null ? 0.75f : 0.25f), sel ? 3f : 1.5f);
                if (d != null && Click(r)) { SkillSystem.SelBot = i; Sfx.Play("click", 0.4f, 1.3f); }
            }

            DrawBotDetails(h);
        }

        static Color StateColor(BotState s)
        {
            switch (s)
            {
                case BotState.Out: return Cfg.Lime;
                case BotState.Panel: return Cfg.Cyan;
            }
            return new Color(0.7f, 0.74f, 0.85f, 1f);
        }

        static string StateTag(BotState s)
        {
            switch (s)
            {
                case BotState.Out: return Cfg.L("ĐANG RA", "OUT");
                case BotState.Panel: return Cfg.L("BẢNG CHỌN", "PANEL");
            }
            return Cfg.L("LƯU TRỮ", "STORED");
        }

        static void DrawBotDetails(Hud h)
        {
            Rect dp = h.R(640, 146, 464, 466);
            h.Fill(dp, new Color(0.02f, 0.03f, 0.09f, 0.7f));
            h.Outline(dp, Cfg.A(Cfg.Cyan, 0.4f), 1.5f);

            var bots = SkillSystem.Bots;
            if (SkillSystem.SelBot < 0 || SkillSystem.SelBot >= bots.Count)
            {
                h.TxtWrap(dp, Cfg.L("Chưa chọn minibot nào.\nChế tạo minibot ở tab CHẾ TẠO.", "No minibot selected.\nCraft one in the CRAFT tab."), 16, Cfg.A(Color.white, 0.5f), TextAnchor.MiddleCenter);
                return;
            }
            var d = bots[SkillSystem.SelBot];
            var cfg = SkillSystem.Config;
            Color sc = StateColor(d.state);

            Rect big = h.R(656, 160, 72, 72);
            h.Fill(big, new Color(0.01f, 0.02f, 0.06f, 0.9f));
            h.Outline(big, Cfg.A(sc, 0.8f), 2f);
            var spr = SkillSystem.IconOf(d.Entry);
            if (spr != null) h.DrawSprite(spr, Pad(big, 4f * h.s), Color.white);

            h.Txt(h.R(742, 158, 350, 32), d.Name, 24, Color.Lerp(PowerRules.RankColor(d.rank), Color.white, 0.2f));
            h.Txt(h.R(742, 190, 350, 22), Cfg.L("Cấp bậc ", "Rank ") + d.rank + "  (" + Cfg.L("cố định", "fixed") + ")   ·   " + Cfg.L("Cấp độ ", "Level ") + d.level + " / " + PowerRules.MaxLevel,
                  14, Cfg.A(Color.white, 0.85f), TextAnchor.UpperLeft, false);
            h.Txt(h.R(742, 212, 350, 20), StateTag(d.state), 13, sc, TextAnchor.UpperLeft);

            // máu
            float max = d.MaxHp;
            h.Txt(h.R(656, 246, 200, 18), Cfg.L("MÁU", "HP"), 13, Cfg.A(Cfg.Lime, 0.95f));
            h.Txt(h.R(656, 246, 436, 18), Mathf.CeilToInt(d.hp) + " / " + Mathf.CeilToInt(max), 13, Color.white, TextAnchor.UpperRight, false);
            h.NeonBar(h.R(656, 266, 436, 12), d.hp / Mathf.Max(1f, max), Cfg.Lime, new Color(0f, 0f, 0f, 0.6f));

            // độ trưởng thành
            string mt = d.IsMaxLevel ? Cfg.L("TỐI ĐA", "MAX") : Mathf.FloorToInt(d.maturity) + " / " + Mathf.CeilToInt(d.Need);
            h.Txt(h.R(656, 290, 300, 18), Cfg.L("ĐỘ TRƯỞNG THÀNH", "MATURITY"), 13, Cfg.A(Cfg.Yellow, 0.95f));
            h.Txt(h.R(656, 290, 436, 18), mt, 13, Color.white, TextAnchor.UpperRight, false);
            h.NeonBar(h.R(656, 310, 436, 12), d.IsMaxLevel ? 1f : d.maturity / d.Need, Cfg.Yellow, new Color(0f, 0f, 0f, 0.6f));
            h.TxtWrap(h.R(656, 326, 436, 34),
                      Cfg.L("Tăng khi minibot hỗ trợ / kết liễu quái. Đầy thanh thì được TĂNG CẤP ĐỘ (không thể tăng cấp bậc).",
                            "Grows when the minibot assists / finishes enemies. A full bar allows a LEVEL UP (rank can never increase)."),
                      11, Cfg.A(Color.white, 0.6f));

            // chỉ số
            h.Txt(h.R(656, 366, 436, 20), Cfg.L("Sát thương: x", "Damage: x") + SkillSystem.DamageMul(d).ToString("0.00")
                  + Cfg.L("      Máu tối đa: ", "      Max HP: ") + Mathf.RoundToInt(max), 14, Cfg.A(Color.white, 0.85f), TextAnchor.UpperLeft, false);

            // nút
            float by = 400f;
            if (d.state == BotState.Storage)
            {
                if (h.Button(h.R(656, by, 436, 38), Cfg.L("ĐƯA RA BẢNG CHỌN  [H]", "MOVE TO PANEL  [H]"), Cfg.Cyan, 17)) SkillSystem.ToPanel(d);
            }
            else if (d.state == BotState.Panel)
            {
                string lab = SkillSystem.AtBase ? Cfg.L("CẤT VỀ LƯU TRỮ (hồi máu dần)", "STORE (heals over time)") : Cfg.L("CHỈ CẤT ĐƯỢC Ở CĂN CỨ", "STORE AT BASE ONLY");
                if (h.Button(h.R(656, by, 436, 38), lab, SkillSystem.AtBase ? Cfg.Cyan : Cfg.A(Cfg.Red, 0.8f), 16)) SkillSystem.ToStorage(d);
            }
            else
            {
                h.TxtWrap(h.R(656, by, 436, 38), Cfg.L("Đang triệu hồi. Hết giao tranh " + Mathf.CeilToInt(SkillSystem.Config.recallDelay) + "s rồi bấm vào minibot trong màn chơi để thu hồi.",
                                                        "Summoned. Once out of combat, click the minibot in the world to recall it."), 12, Cfg.A(Color.white, 0.7f));
            }

            string up = Cfg.L("TĂNG CẤP ĐỘ", "LEVEL UP") + (cfg.levelUpCoinCost > 0 ? "  (" + cfg.levelUpCoinCost + " xu)" : "");
            bool ready = d.MaturityFull && d.state != BotState.Out;
            if (h.Button(h.R(656, by + 46f, 436, 38), up, ready ? Cfg.Lime : Cfg.A(Color.white, 0.4f), 17)) SkillSystem.LevelUp(d);

            h.TxtWrap(h.R(656, by + 92f, 436, 60),
                      Cfg.L("Minibot nằm trong LƯU TRỮ sẽ được hồi máu dần. Minibot bị phá huỷ khi đang triệu hồi sẽ biến mất và rơi ra linh kiện (nhiều hơn nếu cấp độ cao).",
                            "Minibots in STORAGE heal over time. A summoned minibot that is destroyed disappears and drops parts (more at higher levels)."),
                      11, Cfg.A(Color.white, 0.55f));
        }

        // ============================================================ BẢNG CHỌN MINIBOT (phím H) - không tạm dừng game
        public static void DrawPanel(Hud h)
        {
            if (!SkillSystem.PanelOpen || GameManager.State != GameState.Playing || GameManager.Paused) return;
            var items = PanelItems();
            if (items.Count == 0) { SkillSystem.PanelOpen = false; return; }
            SkillSystem.PanelSel = Mathf.Clamp(SkillSystem.PanelSel, 0, items.Count - 1);

            const float cw = 150f, chh = 92f, gap = 8f;
            float total = items.Count * cw + (items.Count - 1) * gap;
            float x0 = 640f - total * 0.5f;
            float y0 = 604f;

            Rect back = h.R(x0 - 10f, y0 - 30f, total + 20f, chh + 40f);
            h.Fill(back, new Color(0.01f, 0.02f, 0.06f, 0.82f));
            h.Outline(back, Cfg.A(Cfg.Cyan, 0.7f), 1.5f);
            h.Txt(h.R(x0, y0 - 27f, 420f, 20f), Cfg.L("BẢNG CHỌN MINIBOT  [H] đóng", "MINIBOT PANEL  [H] close"), 13, Cfg.Cyan, TextAnchor.UpperLeft);
            h.Txt(h.R(x0, y0 - 27f, total, 20f), Cfg.L("[A D] chọn  ·  [S] triệu hồi / thu hồi  ·  hoặc bấm chuột", "[A D] select  ·  [S] summon / recall  ·  or click") + "  ·  " + SkillSystem.SummonedCount + "/" + SkillSystem.MaxSummoned,
                  12, Cfg.A(Color.white, 0.65f), TextAnchor.UpperRight, false);

            for (int i = 0; i < items.Count; i++)
            {
                var d = items[i];
                Rect r = h.R(x0 + i * (cw + gap), y0, cw, chh);
                bool hover = Hover(r);
                bool out_ = d.state == BotState.Out;
                Color bc = out_ ? Cfg.Lime : Cfg.Cyan;
                h.Fill(r, new Color(0.02f, 0.03f, 0.09f, 0.95f));
                h.Fill(r, Cfg.A(bc, out_ ? 0.14f : (hover ? 0.22f : 0.06f)));

                var spr = SkillSystem.IconOf(d.Entry);
                Rect ic = h.R(x0 + i * (cw + gap) + 6f, y0 + 6f, 46f, 46f);
                if (spr != null) h.DrawSprite(spr, ic, out_ ? new Color(1f, 1f, 1f, 0.55f) : Color.white);
                h.Txt(h.R(x0 + i * (cw + gap) + 56f, y0 + 4f, 92f, 20f), d.Name, 15, Color.white);
                h.Txt(h.R(x0 + i * (cw + gap) + 56f, y0 + 24f, 92f, 18f), "[" + d.rank + "]  Lv." + d.level, 13, PowerRules.RankColor(d.rank), TextAnchor.UpperLeft, true);
                h.NeonBar(h.R(x0 + i * (cw + gap) + 8f, y0 + 58f, cw - 16f, 8f), d.hp / Mathf.Max(1f, d.MaxHp), Cfg.Lime, new Color(0f, 0f, 0f, 0.6f));
                string st = out_ ? Cfg.L("ĐANG TRIỆU HỒI", "SUMMONED") : Cfg.L("TRIỆU HỒI", "SUMMON");
                h.Txt(h.R(x0 + i * (cw + gap), y0 + 68f, cw, 20f), st, 13, bc, TextAnchor.UpperCenter);
                if (d.MaturityFull) h.Txt(h.R(x0 + i * (cw + gap), y0 + 4f, cw - 6f, 16f), Cfg.L("LÊN CẤP", "LEVEL UP"), 12, Cfg.Lime, TextAnchor.UpperRight);
                bool selected = i == SkillSystem.PanelSel;
                if (selected) h.Fill(r, Cfg.A(Cfg.Yellow, 0.10f));
                h.Outline(selected ? r : r, selected ? Cfg.Yellow : Cfg.A(bc, hover ? 1f : 0.7f), (hover || selected) ? 3f : 1.5f);

                if (Click(r))
                {
                    if (d.state == BotState.Panel) SkillSystem.Summon(d);
                    else if (d.state == BotState.Out) SkillSystem.Recall(d);
                }
            }
        }

        // ============================================================ NHÃN TRÊN ĐẦU MINIBOT ĐANG TRIỆU HỒI + bấm để thu hồi
        public static void DrawWorld(Hud h)
        {
            if (GameManager.State != GameState.Playing || GameManager.Paused || Minibot.Active.Count == 0) return;
            var list = new List<Minibot>(Minibot.Active);
            float wait = SkillSystem.RecallWait;
            for (int i = 0; i < list.Count; i++)
            {
                var m = list[i];
                if (m == null || m.dead || m.data == null) continue;
                Vector2 c = PixelScreen.WorldToGui(m.Center);
                Vector2 c2 = PixelScreen.WorldToGui(m.Center + Vector2.right);
                float ppu = Mathf.Max(8f, Mathf.Abs(c2.x - c.x));

                // vùng bấm: thân minibot + nhãn phía trên
                Rect click = new Rect(c.x - ppu * 0.95f, c.y - ppu * 1.7f, ppu * 1.9f, ppu * 2.4f);
                bool hover = click.Contains(Event.current.mousePosition);

                // nhãn
                float lw = 118f * h.s;
                Rect lab = new Rect(c.x - lw * 0.5f, c.y - ppu * 1.55f - 30f * h.s, lw, 14f * h.s);
                h.Txt(new Rect(lab.x, lab.y - 14f * h.s, lab.width, 16f * h.s), m.data.Name + "  Lv." + m.data.level, 12, Cfg.A(Cfg.Cyan, 0.95f), TextAnchor.UpperCenter);
                h.NeonBar(lab, m.hp / Mathf.Max(1f, m.maxHp), Cfg.Lime, new Color(0f, 0f, 0f, 0.65f));
                string tip;
                Color tc;
                if (wait <= 0.001f) { tip = Cfg.L("[CLICK] THU HỒI", "[CLICK] RECALL"); tc = hover ? Color.white : Cfg.A(Cfg.Lime, 0.9f); }
                else { tip = Cfg.L("GIAO TRANH  ", "COMBAT  ") + Mathf.CeilToInt(wait) + "s"; tc = Cfg.A(Cfg.Orange, 0.85f); }
                h.Txt(new Rect(lab.x - 20f * h.s, lab.yMax + 1f * h.s, lab.width + 40f * h.s, 14f * h.s), tip, 11, tc, TextAnchor.UpperCenter);
                if (hover && wait <= 0.001f) h.Outline(new Rect(c.x - ppu * 0.7f, c.y - ppu * 0.7f, ppu * 1.4f, ppu * 1.4f), Cfg.A(Cfg.Lime, 0.8f), 1.5f);

                if (Click(click)) SkillSystem.Recall(m.data);
            }
        }
    }
}
