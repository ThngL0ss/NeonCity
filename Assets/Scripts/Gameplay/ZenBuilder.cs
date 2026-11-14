using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// ZenMonastery: trụ sở chính của phe Hòa hảo (chùa 1 tầng, mặt tiền kiểu chùa tháp). Bên trái và bên phải là các tu sĩ ngồi thiền (áo nâu);
    /// bên phải, trên bệ cao ở cuối là Shengu (áo cà sa đỏ). Hội thoại + nhiệm vụ phụ "đưa hộp đen đến DesTown4" (xem Town.Shengu).
    /// </summary>
    public static class ZenBuilder
    {
        public const string Id = "ZenMonastery";
        static readonly Color Red = new Color(0.92f, 0.28f, 0.22f);
        static readonly Color Brown = new Color(0.95f, 0.75f, 0.35f);

        public static Room Hall() { return Interior.GetRoom("zen", Build); }

        static string Me { get { return "XbAst-013"; } }
        static string ShenguName { get { return "SHENGU"; } }
        static string MonkName { get { return Cfg.L("TU SĨ", "MONK"); } }

        static Room Build()
        {
            var r = new Room("zen", "ZENMONASTERY", "ZENMONASTERY", Interior.Origin(2, 0), 66f, RoomStyle.Zen());
            r.entryX = 5f;
            RoomKit.ExitDoor(r, 2.4f, "RA NGOÀI", "LEAVE", Interior.Exit, 0x7A4A2B, 0xEFE6C8);
            RoomKit.Plate(r, 33f, 8.2f, "ZENCITY", 3, 0x1A100A, 0xF2D27A, 0xE8B84A);

            // đèn lồng treo + cây
            for (int i = 0; i < 5; i++)
            {
                r.Put("Lantern", TownArt.Lantern(0xD83A2A), 9f + i * 12f, 8.4f, -26);
                r.Glow(9f + i * 12f, 9.0f, 3f, 3f, new Color(1f, 0.45f, 0.2f, 0.25f), -25);
            }
            RoomKit.Plant(r, 6f, 0x5FA050); RoomKit.Plant(r, 31f, 0x5FA050);

            // bên trái: 5 tu sĩ ngồi thiền, chỉ 1 người đáp lời
            float[] lx = { 9f, 13.5f, 18f, 22.5f, 27f };
            for (int i = 0; i < lx.Length; i++)
            {
                r.Put("Cushion", TownArt.Cushion(0x9A2A2A), lx[i], 0f, -22);
                var m = r.Npc(TownLooks.MonkN(i), lx[i], TownNpc.Mode.Sit, MonkName, null);
                var mm = m;
                if (i == 2) m.SetTalk(() => Monk(mm, Cfg.L("Cậu có vẻ hấp tấp… Tĩnh tâm và nhìn sâu vào trong tâm trí của cậu nào…", "You seem hasty… Be still, and look deep into your own mind…")));
                else m.SetTalk(() => Monk(mm, "….."));
            }
            // bên phải: 4 tu sĩ ngồi thiền
            float[] rx = { 37f, 41.5f, 46f, 50.5f };
            for (int i = 0; i < rx.Length; i++)
            {
                r.Put("Cushion", TownArt.Cushion(0x9A2A2A), rx[i], 0f, -22);
                var m = r.Npc(TownLooks.MonkN(i + 1), rx[i], TownNpc.Mode.Sit, MonkName, null);
                var mm = m;
                m.SetTalk(() => Monk(mm, "….."));
            }

            // bệ cao cuối bên phải: Shengu
            r.Quad("Dais", 55f, 0f, 9f, 1f, new Color(0.35f, 0.2f, 0.12f), -26);
            r.Quad("DaisTop", 55f, 0.9f, 9f, 0.12f, new Color(0.9f, 0.7f, 0.3f), -25);
            r.Quad("DaisStep", 53.4f, 0f, 1.6f, 0.5f, new Color(0.3f, 0.18f, 0.1f), -26);
            var dg = new GameObject("DaisCol");
            dg.transform.SetParent(r.t, false);
            var bc = dg.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(9f, 1f);
            bc.offset = new Vector2(59.5f, 0.5f);
            r.Put("Cushion", TownArt.Cushion(0xE8B84A), 59.5f, 1f, -22);
            var sh = r.Npc(TownLooks.Shengu, 59.5f, TownNpc.Mode.Stand, ShenguName, null, -1f, -1f, true);
            sh.transform.position += new Vector3(0f, 1f, 0f);
            sh.range = 3.2f;
            sh.SetTalk(TalkShengu);
            return r;
        }

        static void Monk(TownNpc m, string text)
        {
            DialogueUi.Begin(new List<DlgLine> { new DlgLine(MonkName, text, Brown, m.transform) }, null);
        }

        // ------------------------------------------------------------ Shengu
        static DlgLine S(Transform who, string vi, string en) { return new DlgLine(ShenguName, Cfg.L(vi, en), Red, who); }
        static DlgLine P(string vi, string en)
        {
            var pl = Player.Instance != null ? Player.Instance.transform : null;
            return new DlgLine(Me, Cfg.L(vi, en), Cfg.Cyan, pl);
        }

        static void TalkShengu()
        {
            // tìm lại chính NPC Shengu (người gần nhất đang đứng trên bệ)
            TownNpc sn = null;
            var all = Object.FindObjectsOfType<TownNpc>();
            for (int i = 0; i < all.Length; i++) if (all[i].npcName == ShenguName) { sn = all[i]; break; }
            Transform who = sn != null ? sn.transform : null;

            switch (Town.Shengu)
            {
                case Town.ShenguNone: FirstMeeting(who); break;
                case Town.ShenguDeclined: Offer(who, true); break;
                case Town.ShenguQuest:
                    if (!QuestItems.Has(QuestItems.BlackBox))
                    {
                        var l = new List<DlgLine>
                        {
                            S(who, "Hộp đen đâu rồi? Đây, cầm lấy.", "Where's the black box? Here, take it."),
                            S(who, "Hãy đưa cái này đến DesTown4 và bảo rằng Shengu có một yêu cầu.", "Bring this to DesTown4 and tell them Shengu has a request.")
                        };
                        l[0].onShow = () => QuestItems.Give(QuestItems.BlackBox);
                        DialogueUi.Begin(l, null);
                    }
                    else
                        DialogueUi.Begin(new List<DlgLine> { S(who, "Hãy đưa cái hộp này đến DesTown4 và bảo rằng Shengu có một yêu cầu.", "Bring the box to DesTown4 and tell them Shengu has a request.") }, null);
                    break;
                case Town.ShenguDelivered: Finale(who); break;
                default: DialogueUi.Begin(new List<DlgLine> { S(who, "….", "….") }, null); break;
            }
        }

        static void FirstMeeting(Transform who)
        {
            var l = new List<DlgLine>();
            l.Add(S(who, "Chào mừng cậu tới với ZenCity. Nơi đây chỉ có sự hòa hợp và giác ngộ! Hãy chung sống hòa bình với nhau.",
                         "Welcome to ZenCity. Here there is only harmony and enlightenment! Let us live together in peace."));
            l.Add(P("Chào… (Suy nghĩ: đây là Phe Hòa hảo mà lão già nhắc tới sao?)", "Hello… (Thinking: is this the Peace faction the old man mentioned?)"));
            l.Add(S(who, "….", "…."));
            l.Add(S(who, "Cậu là “kẻ đó” phải không?", "You are “that one”, aren't you?"));
            l.Add(P("Sao ông biết?", "How do you know?"));
            l.Add(S(who, "Ta có thể nhìn thấu tâm can! Một sự hỗn loạn, dầu máy… đáng thương!", "I can see through your heart! Chaos, machine oil… how pitiful!"));
            var give = S(who, "Ta sẽ cho cậu thứ này", "I'll give you this");
            give.onShow = () => { Town.Shengu = Town.ShenguDeclined; QuestItems.Give(QuestItems.Core6Fake); };
            l.Add(give);
            l.Add(P("Cái này… Ông cho ta sao?", "This… You're giving it to me?"));
            l.Add(S(who, "Nó thuộc về cậu.", "It belongs to you."));
            l.Add(S(who, "Chỉ có điều...", "Only..."));
            l.Add(S(who, "Cậu có thể giúp ta một việc nhỏ được không?", "Could you do a small favor for me?"));
            l.Add(S(who, "Hãy đưa cái này đến DesTown4 và bảo rằng Shengu có một yêu cầu", "Bring this to DesTown4 and tell them Shengu has a request"));
            l.Add(AskHelp(who));
            DialogueUi.Begin(l, null);      // nhận lõi (giả) → ShenguDeclined; nhận việc thì chuyển sang ShenguQuest
        }

        static DlgLine AskHelp(Transform who)
        {
            string[] opts = { Cfg.L("ta đâu có rảnh", "I'm not free"), Cfg.L("Được thôi", "Sure") };
            return DlgLine.Ask(ShenguName, Cfg.L("Cậu sẽ giúp ta chứ?", "Will you help me?"), Red, who, opts, i =>
            {
                if (i == 1)
                {
                    Town.Shengu = Town.ShenguQuest;
                    QuestItems.Give(QuestItems.BlackBox);
                    GameManager.Banner(Cfg.L("NHIỆM VỤ PHỤ: Đưa hộp đen đến DesTown4", "SIDE QUEST: Deliver the black box to DesTown4"), Cfg.Magenta, 3f);
                }
                DialogueUi.Close();
            });
        }

        /// <summary>Gặp lại sau khi từ chối: hỏi lại việc nhờ.</summary>
        static void Offer(Transform who, bool again)
        {
            var l = new List<DlgLine>
            {
                S(who, "Cậu đổi ý rồi à? Hãy đưa cái này đến DesTown4 và bảo rằng Shengu có một yêu cầu.", "Changed your mind? Bring this to DesTown4 and tell them Shengu has a request."),
                AskHelp(who)
            };
            DialogueUi.Begin(l, null);
        }

        static void Finale(Transform who)
        {
            var l = new List<DlgLine>();
            l.Add(S(who, "Cậu đã quay lại! Cảm ơn.", "You're back! Thank you."));
            l.Add(P("Không có gì, có qua có lại thôi.", "No problem, one good turn deserves another."));
            var give = S(who, "Đây là dành cho cậu", "This is for you");
            give.onShow = () =>
            {
                QuestItems.Take(QuestItems.Core6Fake);
                QuestItems.Give(QuestItems.Core6);
                Town.Shengu = Town.ShenguDone;
            };
            l.Add(give);
            l.Add(P("???", "???"));
            l.Add(P("Vậy cái ban đầu lão đưa cho ta…", "Then the first one you gave me…"));
            l.Add(S(who, "Là đồ giả! Không phải do ta không tin cậu, mà vì lời nói dối đôi khi còn tốt hơn lời nói thật. HAHAHA", "A fake! It's not that I didn't trust you — a lie is sometimes better than the truth. HAHAHA"));
            l.Add(P("Hừ… Đúng là lão tu hú!", "Hmph… What a sly old cuckoo!"));
            l.Add(S(who, "Này! Ta đọc được tâm can của cậu đó!!!", "Hey! I can read your heart, you know!!!"));
            l.Add(P("Thì?", "So?"));
            l.Add(P("Lão cũng chơi ta 1 vố còn gì", "You played a trick on me too, didn't you"));
            DialogueUi.Begin(l, null);
        }
    }
}
