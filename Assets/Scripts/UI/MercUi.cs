using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Bảng thuê lính đánh thuê (phòng kín của MerShop, mục "Sức mạnh" của Furra). Cột trái: danh sách 10 người; cột phải: chỉ số, giá, kỹ năng.
    /// ENTER / F = thuê (trừ xu, dầu, linh kiện) hoặc chọn đi cùng (mỗi lần một người, đi theo ở mọi khu vực ngoài căn cứ). W / S chọn, ESC thoát.
    /// </summary>
    public static class MercUi
    {
        static int sel;
        static string msg = "";
        static float msgT;
        static Vector2 lastMouse;

        public static void ResetStatics() { sel = 0; msg = ""; msgT = 0f; }
        public static void OnOpen() { sel = Mathf.Max(0, Town.MercActive); msg = ""; msgT = 0f; }

        static void Say(string m) { msg = m; msgT = 2.6f; }

        static void Act()
        {
            var d = MercDefs.All[sel];
            if (MercDefs.Owned(sel))
            {
                Town.MercActive = Town.MercActive == sel ? -1 : sel;
                Say(Town.MercActive == sel ? Cfg.L(d.id + " sẽ đi cùng bạn ở lần ra ngoài tới.", d.id + " will join you next time you head out.") : Cfg.L("Không ai đi cùng.", "Nobody will join you."));
                Sfx.Play("click", 0.6f, 1.3f);
                return;
            }
            if (!MercDefs.CanAfford(d)) { Say(Cfg.L("Không đủ để thuê.", "You can't afford that.")); Sfx.Play("click", 0.6f, 0.5f); return; }
            MercDefs.Hire(sel);
            Say(Cfg.L("Đã thuê " + d.id + "!", "Hired " + d.id + "!"));
            Sfx.Play("pickup", 0.8f, 1.1f);
        }

        public static void UpdateInput()
        {
            if (GameInput.PauseDown) { BaseUi.Close(); return; }
            if (msgT > 0f) msgT -= Time.unscaledDeltaTime;
            int d = (GameInput.NavDownDown ? 1 : 0) - (GameInput.NavUpDown ? 1 : 0);
            if (d != 0) { sel = (sel + d + MercDefs.All.Length) % MercDefs.All.Length; Sfx.Play("click", 0.3f, 1.7f); }
            if (GameInput.ConfirmDown || GameInput.InteractDown) Act();
        }

        static string Cost(MercDef d)
        {
            string s = d.coins + Cfg.L(" xu", " coins");
            if (d.oil > 0) s += " · " + d.oil + Cfg.L(" dầu", " oil");
            if (d.partsB > 0) s += " · " + d.partsB + Cfg.L(" linh kiện B", " B parts");
            if (d.partsA > 0) s += " · " + d.partsA + Cfg.L(" linh kiện A", " A parts");
            return s;
        }

        public static void Draw(Hud h)
        {
            h.Dim(0.55f);
            Rect panel = h.R(110, 56, 1060, 608);
            h.Fill(panel, new Color(0.01f, 0.02f, 0.07f, 0.96f));
            h.Outline(panel, Cfg.Orange, 3f);
            h.Txt(h.R(136, 66, 700, 40), Cfg.L("SỨC MẠNH — THUÊ LÍNH", "POWER — HIRE MERCENARIES"), 28, Cfg.Orange, TextAnchor.MiddleLeft);
            h.Txt(h.R(780, 66, 360, 40), Cfg.L("Xu: ", "Coins: ") + Inventory.Coins + Cfg.L("   Dầu: ", "   Oil: ") + Inventory.Oil, 22, Cfg.Yellow, TextAnchor.MiddleRight);
            h.Txt(h.R(136, 104, 1004, 22), Cfg.L("Linh kiện đang có:  B ×" + SkillSystem.Parts(Rank.B) + "   A ×" + SkillSystem.Parts(Rank.A) + "   ·   Thuê một lần, mỗi lần ra ngoài một người đi cùng.", "Parts owned:  B ×" + SkillSystem.Parts(Rank.B) + "   A ×" + SkillSystem.Parts(Rank.A) + "   ·   Hire once, one mercenary joins you each outing."), 14, Cfg.A(Color.white, 0.6f), TextAnchor.MiddleLeft, false);

            bool moved = (Event.current.mousePosition - lastMouse).sqrMagnitude > 1f;
            Rect box = h.R(136, 134, 420, 420);
            h.Outline(box, Cfg.A(Cfg.Yellow, 0.8f), 2f);
            for (int i = 0; i < MercDefs.All.Length; i++)
            {
                var d = MercDefs.All[i];
                Rect rr = h.R(140, 138 + i * 41, 412, 39);
                bool hover = rr.Contains(Event.current.mousePosition);
                if (hover && moved && Event.current.type == EventType.Repaint) sel = i;
                bool on = sel == i;
                h.Fill(rr, on ? Cfg.A(Cfg.Yellow, 0.18f) : new Color(0.03f, 0.05f, 0.12f, 0.8f));
                if (on) h.Outline(rr, Cfg.Yellow, 2f);
                Color rc = PowerRules.RankColor(d.rank);
                bool owned = MercDefs.Owned(i);
                h.Txt(new Rect(rr.x + 10f * h.s, rr.y, 150f * h.s, rr.height), d.id, 18, owned ? Cfg.Lime : Color.white, TextAnchor.MiddleLeft);
                h.Txt(new Rect(rr.x + 150f * h.s, rr.y, 120f * h.s, rr.height), "Lv." + d.level + "  " + d.rank, 15, rc, TextAnchor.MiddleLeft);
                string tag = Town.MercActive == i ? Cfg.L("ĐI CÙNG", "ACTIVE") : owned ? Cfg.L("ĐÃ THUÊ", "HIRED") : d.coins + Cfg.L(" xu", "");
                h.Txt(new Rect(rr.x + 270f * h.s, rr.y, 134f * h.s, rr.height), tag, 15, Town.MercActive == i ? Cfg.Cyan : owned ? Cfg.Lime : Cfg.Yellow, TextAnchor.MiddleRight);
                if (BaseUi.Clicked(rr)) sel = i;
            }
            if (Event.current.type == EventType.Repaint) lastMouse = Event.current.mousePosition;

            // chi tiết
            var c = MercDefs.All[Mathf.Clamp(sel, 0, MercDefs.All.Length - 1)];
            Rect det = h.R(580, 134, 560, 420);
            h.Fill(det, new Color(0.03f, 0.05f, 0.12f, 0.9f));
            h.Outline(det, Cfg.A(Color.white, 0.25f), 1.5f);
            h.Txt(h.R(596, 140, 520, 38), c.id, 30, Color.white, TextAnchor.MiddleLeft);
            h.Txt(h.R(596, 140, 524, 38), "Lv." + c.level + "  " + Cfg.L("Bậc ", "Rank ") + c.rank, 22, PowerRules.RankColor(c.rank), TextAnchor.MiddleRight);
            h.Txt(h.R(596, 184, 524, 26), Cfg.L("Kiểu đánh: ", "Style: ") + c.TypeText, 17, Cfg.Cyan, TextAnchor.MiddleLeft);
            h.Txt(h.R(596, 212, 524, 26), Cfg.L("Máu: ", "Health: ") + c.HpText + "   (" + Mathf.RoundToInt(c.MaxHp) + ")", 17, Color.white, TextAnchor.MiddleLeft);
            h.Txt(h.R(596, 240, 524, 26), Cfg.L("Sát thương: ", "Damage: ") + c.DmgText + "   (" + Mathf.RoundToInt(c.Damage) + ")", 17, Color.white, TextAnchor.MiddleLeft);
            h.Txt(h.R(596, 268, 524, 26), Cfg.L("Tốc độ: ", "Speed: ") + c.SpdText, 17, Color.white, TextAnchor.MiddleLeft);
            bool afford = MercDefs.CanAfford(c);
            h.Txt(h.R(596, 300, 524, 26), Cfg.L("Giá: ", "Price: ") + Cost(c), 17, MercDefs.Owned(sel) ? Cfg.A(Color.white, 0.5f) : afford ? Cfg.Yellow : Cfg.Red, TextAnchor.MiddleLeft);
            h.Txt(h.R(596, 332, 524, 22), c.skill == MercSkill.None ? Cfg.L("Kỹ năng: không có", "Skill: none") : Cfg.L("Kỹ năng:", "Skill:"), 16, Cfg.Magenta, TextAnchor.MiddleLeft);
            if (c.skill != MercSkill.None) h.TxtWrap(h.R(596, 356, 524, 192), c.SkillText, 14, Color.white);

            Rect act = h.R(136, 566, 420, 48);
            bool owned2 = MercDefs.Owned(sel);
            string lab = owned2 ? (Town.MercActive == sel ? Cfg.L("BỎ ĐI CÙNG  [ENTER]", "STAND DOWN  [ENTER]") : Cfg.L("CHỌN ĐI CÙNG  [ENTER]", "TAKE ALONG  [ENTER]")) : Cfg.L("THUÊ  [ENTER]", "HIRE  [ENTER]");
            if (h.Button(act, lab, owned2 ? Cfg.Cyan : afford ? Cfg.Lime : Cfg.A(Color.white, 0.4f), 22)) Act();
            if (msgT > 0f) h.Txt(h.R(580, 566, 560, 48), msg, 18, Cfg.Yellow, TextAnchor.MiddleLeft);
            h.Txt(h.R(136, 628, 1004, 24), Cfg.L("[W/S] chọn   [ENTER] / [F] thuê - chọn đi cùng   [ESC] thoát", "[W/S] select   [ENTER] / [F] hire - take along   [ESC] leave"), 14, Cfg.A(Color.white, 0.55f), TextAnchor.MiddleRight, false);
        }
    }
}
