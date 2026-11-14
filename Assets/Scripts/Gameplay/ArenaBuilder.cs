using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// ArenaCyber (đấu trường chung của phe Diệt chủng và phe Tài phiệt): sảnh + đấu trường, dựng lúc chạy.
    /// Sảnh: một người áo đen (kính râm) chặn cửa vào đấu trường, hỏi "Anh đến đây với tư cách là…":
    ///  • Người xem  — trả vé, lên khán đài (bên trái, đứng trên khối cao), xem trận giữa hai đấu sĩ AI (góc A / góc B), cược 10% / 50% / 100% số xu đang có, xem xong có thể ra về;
    ///  • Đấu sĩ     — xuống sàn đấu, đánh với đối thủ ngày một mạnh (thắng được xu, thua thì chết như bình thường), thắng xong có thể ra về;
    ///  • Thông tin về DesTown4 — thắng liền 3 trận (đối thủ 1, 2, 3, không có xu), được 1000 xu + thẻ đấu sĩ (vật phẩm nhiệm vụ) và ArenaInfoDone.
    /// Toàn bộ luồng là coroutine chạy trên GameManager (xem Run); các biến static được reset ở ResetStatics.
    /// </summary>
    public static class ArenaBuilder
    {
        public const string Id = "ArenaCyber";
        const float HallW = 60f, StandsW = 19f, StandTop = 3.2f, FloorMin = 21f, FloorMax = 58.5f;
        const int EntryFee = 50;
        static readonly Color Dark = new Color(0.95f, 0.25f, 0.35f);
        static readonly Color Pa = new Color(1f, 0.85f, 0.35f);

        static TownNpc bouncer;
        static Coroutine sessionCo;
        static bool locked;             // đang có trận / tiền cược đang treo
        static int pick = -1;           // kết quả lựa chọn gần nhất (Ask)
        static bool duelWon;

        public static void ResetStatics() { bouncer = null; sessionCo = null; locked = false; pick = -1; duelWon = false; }

        public static Room Lobby() { return Interior.GetRoom("arena.lobby", BuildLobby); }
        public static Room Hall() { return Interior.GetRoom("arena.hall", BuildHall); }

        static string BName { get { return Cfg.L("NGƯỜI ÁO ĐEN", "MAN IN BLACK"); } }
        static string PaName { get { return Cfg.L("LOA ARENA", "ARENA PA"); } }

        // ================================================================ SẢNH
        static Room BuildLobby()
        {
            var r = new Room("arena.lobby", "ARENACYBER", "ARENACYBER", Interior.Origin(3, 0), 42f, RoomStyle.Arena());
            r.entryX = 5f;
            RoomKit.ExitDoor(r, 2.4f, "RA NGOÀI", "LEAVE", Interior.Exit, 0x3A2A1A, 0xFF2FB5);
            RoomKit.Plate(r, 20f, 8.4f, "ARENACYBER", 3, 0x0A0C1C, 0xFF2FB5, 0xFF2FB5);
            RoomKit.Plant(r, 8f); RoomKit.Plant(r, 28f);
            RoomKit.Bench(r, 14f); RoomKit.Bench(r, 22f);
            RoomKit.Plate(r, 14f, 6.0f, "FIGHT", 2, 0x1A1008, 0xFFD23F, 0xFFD23F);
            RoomKit.Plate(r, 22f, 6.0f, "BET", 2, 0x1A1008, 0x35E8FF, 0x35E8FF);

            // cửa vào đấu trường (người áo đen đứng chắn)
            r.Put("ArenaDoor", TownArt.DoorFrame(48, 64, 0x3A2A1A, 0xFF2A4A), 36f, 0f, -30);
            r.Glow(36f, 2.0f, 5.5f, 5.5f, new Color(1f, 0.15f, 0.25f, 0.28f), -29);
            RoomKit.Plate(r, 36f, 5.6f, "ARENA", 2, 0x0A0C1C, 0xFF2A4A, 0xFF2A4A);

            bouncer = r.Npc(TownLooks.ArenaBouncer, 33f, TownNpc.Mode.Stand, BName, TalkBouncer, -1f, -1f, true);
            bouncer.range = 2.6f;
            // vài khách đi lại cho đỡ vắng
            r.Npc(TownLooks.Customer(2), 12f, TownNpc.Mode.Wander, null, null, 8f, 26f);
            r.Npc(TownLooks.Customer(5), 24f, TownNpc.Mode.Wander, null, null, 10f, 28f, true);
            return r;
        }

        static void TalkBouncer()
        {
            if (sessionCo != null || Interior.Busy) return;
            Run(LobbyTalk());
        }

        // ================================================================ ĐẤU TRƯỜNG
        static Room BuildHall()
        {
            var r = new Room("arena.hall", "ARENACYBER", "ARENACYBER", Interior.Origin(3, 1), HallW, RoomStyle.Arena());
            r.entryX = 24f;
            var sand = new Color(0.80f, 0.66f, 0.42f);
            r.Quad("Sand", FloorMin - 1.5f, -5f, HallW - FloorMin + 3.5f, 5f, sand, -38);
            r.Quad("SandEdge", FloorMin - 1.5f, -0.12f, HallW - FloorMin + 3.5f, 0.12f, Cfg.A(Cfg.Orange, 0.9f), -37);
            RoomKit.Plate(r, 40f, 8.6f, "ARENACYBER", 3, 0x0A0C1C, 0xFF2FB5, 0xFF2FB5);
            for (int i = 0; i < 4; i++) r.Glow(28f + i * 9f, 9.3f, 8f, 6f, new Color(0.8f, 0.9f, 1f, 0.12f), -44);

            // khán đài (khối đặc, đứng lên được) — thấp bên trái, rào neon ngăn với sàn đấu
            var body = new Color(0.20f, 0.15f, 0.26f);
            r.Quad("StandBody", -2f, 0f, StandsW + 2f, StandTop, body, -38);
            for (int i = 1; i < 4; i++) r.Quad("StandRow", -2f, i * 0.8f, StandsW + 2f, 0.07f, Cfg.A(Cfg.Magenta, 0.35f), -37);
            r.Quad("StandTop", -2f, StandTop, StandsW + 2f, 0.14f, Cfg.Magenta, -36);
            var sc = new GameObject("StandsCol");
            sc.transform.SetParent(r.t, false);
            var sb = sc.AddComponent<BoxCollider2D>();
            sb.size = new Vector2(StandsW, StandTop);
            sb.offset = new Vector2(StandsW * 0.5f, StandTop * 0.5f);
            r.Quad("Barrier", StandsW, 0f, 0.8f, 6.2f, new Color(1f, 0.2f, 0.7f, 0.22f), -30);
            r.Quad("BarrierTop", StandsW, 6.0f, 0.8f, 0.2f, Cfg.Magenta, -29);
            var fc = new GameObject("BarrierCol");
            fc.transform.SetParent(r.t, false);
            var fb = fc.AddComponent<BoxCollider2D>();
            fb.size = new Vector2(0.8f, 22f);
            fb.offset = new Vector2(StandsW + 0.4f, 11f);

            // khán giả trên khán đài
            for (int i = 0; i < 9; i++)
            {
                var n = r.Npc(TownLooks.Customer(i + 3), 2f + i * 1.9f, i % 3 == 0 ? TownNpc.Mode.Wander : TownNpc.Mode.Stand, null, null, 1.5f, StandsW - 1.5f, false);
                n.transform.position += new Vector3(0f, StandTop, 0f);
            }

            // cửa về sảnh (sàn đấu) + lối rời khán đài — chỉ dùng được khi không có trận / không có tiền cược treo
            r.Put("BackDoor", TownArt.DoorFrame(40, 56, 0x3A2A1A, 0xFF2FB5), 23f, 0f, -30);
            var h1 = r.Spot(23f, 0f, "VỀ SẢNH", "BACK TO LOBBY", LeaveHall, 1.8f);
            h1.canUse = () => !locked;
            var h2 = r.Spot(3f, StandTop, "RỜI KHÁN ĐÀI", "LEAVE THE STANDS", LeaveHall, 1.8f);
            h2.canUse = () => !locked;
            RoomKit.Plant(r, 59f);
            return r;
        }

        static void LeaveHall()
        {
            StopAll();
            Interior.Go(Lobby, 31f);
        }

        // ================================================================ chạy coroutine
        static void Run(IEnumerator e)
        {
            if (GameManager.Inst == null || sessionCo != null) return;
            sessionCo = GameManager.Inst.StartCoroutine(Wrap(e));
        }

        static IEnumerator Wrap(IEnumerator e)
        {
            yield return e;
            sessionCo = null; locked = false;
            Cine.ReleaseFocus();
        }

        /// <summary>Dừng phiên đang chạy, dọn đấu sĩ / đạn còn sót.</summary>
        static void StopAll()
        {
            if (sessionCo != null && GameManager.Inst != null) GameManager.Inst.StopCoroutine(sessionCo);
            sessionCo = null; locked = false;
            var fs = Object.FindObjectsOfType<ArenaFighter>();
            for (int i = 0; i < fs.Length; i++) Object.Destroy(fs[i].gameObject);
            var bs = Object.FindObjectsOfType<ArenaBullet>();
            for (int i = 0; i < bs.Length; i++) Object.Destroy(bs[i].gameObject);
            Cine.ReleaseFocus();
            GameManager.PlayerLocked = false;
        }

        static bool PlayerOk { get { return Player.Instance != null && !Player.Instance.Dead; } }

        // ---- trợ giúp hội thoại / chuyển phòng trong coroutine
        static IEnumerator Talk(List<DlgLine> l)
        {
            bool fin = false;
            DialogueUi.Begin(l, () => fin = true);
            while (!fin) yield return null;
        }

        /// <summary>Hỏi lựa chọn; kết quả ở `pick` (-1 nếu bấm ESC).</summary>
        static IEnumerator Ask(string speaker, string text, Color col, Transform who, string[] opts)
        {
            pick = -1;
            bool fin = false;
            var l = new List<DlgLine> { DlgLine.Ask(speaker, text, col, who, opts, i => { pick = i; DialogueUi.Close(() => fin = true); }) };
            DialogueUi.Begin(l, () => fin = true);
            while (!fin) yield return null;
        }

        static IEnumerator Say(string speaker, string text, Color col, Transform who)
        {
            yield return Talk(new List<DlgLine> { new DlgLine(speaker, text, col, who) });
        }

        static IEnumerator GoTo(System.Func<Room> build, float lx, float y = 0.35f)
        {
            Interior.Go(build, lx, true, y);
            yield return null;
            while (Interior.Busy) yield return null;
        }

        static IEnumerator Wait(float sec)
        {
            float t = 0f;
            while (t < sec) { if (!BaseUi.IsOpen) t += Time.deltaTime; yield return null; }
        }

        // ================================================================ SẢNH: đối thoại người áo đen
        static IEnumerator LobbyTalk()
        {
            Transform who = bouncer != null ? bouncer.transform : null;
            string[] opts =
            {
                Cfg.L("Ta muốn xem các trận chiến (Người xem)", "I want to watch the fights (Spectator)"),
                Cfg.L("Ta muốn thách đấu (Đấu sĩ)", "I want to challenge someone (Fighter)"),
                Cfg.L("Ta muốn thông tin về DesTown4", "I want information about DesTown4"),
                Cfg.L("Không có gì…", "Nothing…")
            };
            yield return Ask(BName, Cfg.L("Anh đến đây với tư cách là…", "You're here as…"), Dark, who, opts);
            int c = pick;

            if (c == 0)
            {
                yield return Ask(BName, Cfg.L("Vé vào cửa " + EntryFee + " xu. Anh vào chứ?", "Entry is " + EntryFee + " coins. Coming in?"), Dark, who,
                    new[] { Cfg.L("Trả " + EntryFee + " xu", "Pay " + EntryFee + " coins"), Cfg.L("Thôi", "Never mind") });
                if (pick != 0) yield break;
                if (!Inventory.Spend(EntryFee))
                {
                    yield return Say(BName, Cfg.L("Không đủ xu thì đừng có vào.", "No coins, no entry."), Dark, who);
                    yield break;
                }
                Fx.Text(Player.Instance.Position + new Vector2(0f, 2.6f), "-" + EntryFee, Cfg.Yellow, 1f);
                yield return GoTo(Hall, 9f, StandTop + 0.35f);
                yield return SpectateLoop();
            }
            else if (c == 1)
            {
                yield return Ask(BName, Cfg.L("Thách đấu hả? Thắng thì có tiền. Thua thì… anh biết rồi đấy.", "A challenger, huh? Win and you get paid. Lose and… you know how it goes."), Dark, who,
                    new[] { Cfg.L("Vào đấu trường", "Enter the arena"), Cfg.L("Để tôi nghĩ lại", "Let me think it over") });
                if (pick != 0) yield break;
                yield return GoTo(Hall, 24f);
                yield return FighterLoop();
            }
            else if (c == 2)
            {
                if (Town.ArenaInfoDone)
                {
                    yield return Say(BName, Cfg.L("Mày đã có thẻ rồi còn gì.", "You already have the card."), Dark, who);
                    yield break;
                }
                yield return Ask(BName, Cfg.L("Được thôi, nếu mày thắng được liên tiếp 3 trận", "Fine, if you can win 3 fights in a row"), Dark, who,
                    new[] { Cfg.L("Chấp nhận", "Accept"), Cfg.L("Thôi khỏi", "Forget it") });
                if (pick != 0) yield break;
                yield return GoTo(Hall, 24f);
                yield return InfoLoop();
            }
        }

        // ================================================================ NGƯỜI XEM
        static ArenaFighter.Kind RandKind() { return (ArenaFighter.Kind)Random.Range(0, 3); }

        static IEnumerator SpectateLoop()
        {
            var hall = Hall();
            int round = 0;
            while (PlayerOk)
            {
                round++;
                var ka = RandKind(); var kb = RandKind();
                float pm = 1f + Mathf.Min(1.5f, 0.15f * (round - 1));
                var A = ArenaFighter.Spawn(hall.t, ka, hall.X(31f), hall.X(FloorMin), hall.X(FloorMax), pm, "A", new Color(0.75f, 0.95f, 1f));
                var B = ArenaFighter.Spawn(hall.t, kb, hall.X(50f), hall.X(FloorMin), hall.X(FloorMax), pm, "B", new Color(1f, 0.8f, 0.95f));
                A.FaceDir(1); B.FaceDir(-1);
                A.foe = B; B.foe = A;
                Cine.Focus(new Vector2(hall.X(40f), 2f), 0.6f);

                // chọn bên
                string an = ArenaFighter.KindName(ka), bn = ArenaFighter.KindName(kb);
                string head = Cfg.L("Trận " + round + "! Góc A: " + an + " — Góc B: " + bn + ". Anh cược bên nào?",
                                    "Fight " + round + "! Corner A: " + an + " — Corner B: " + bn + ". Which side?");
                string[] sides =
                {
                    Cfg.L("Cược góc A (" + an + ")", "Bet on A (" + an + ")"),
                    Cfg.L("Cược góc B (" + bn + ")", "Bet on B (" + bn + ")"),
                    Cfg.L("Chỉ xem, không cược", "Just watch")
                };
                int side = -1, bet = 0;
                if (Inventory.Coins > 0)
                {
                    yield return Ask(PaName, head, Pa, null, sides);
                    if (pick == 0 || pick == 1)
                    {
                        side = pick;
                        int c = Inventory.Coins;
                        int b10 = Mathf.Clamp(Mathf.RoundToInt(c * 0.1f), 1, c), b50 = Mathf.Clamp(Mathf.RoundToInt(c * 0.5f), 1, c);
                        string[] amts =
                        {
                            "10%  (" + b10 + Cfg.L(" xu)", " coins)"),
                            "50%  (" + b50 + Cfg.L(" xu)", " coins)"),
                            "100%  (" + c + Cfg.L(" xu)", " coins)")
                        };
                        yield return Ask(PaName, Cfg.L("Cược bao nhiêu?", "How much?"), Pa, null, amts);
                        bet = pick == 0 ? b10 : pick == 1 ? b50 : pick == 2 ? c : 0;
                        if (bet <= 0 || !Inventory.Spend(bet)) { side = -1; bet = 0; }
                    }
                }
                else
                    yield return Say(PaName, Cfg.L("Trận " + round + "! Góc A: " + an + " — Góc B: " + bn + ". Anh hết xu để cược rồi, cứ xem vậy.",
                                                   "Fight " + round + "! Corner A: " + an + " — Corner B: " + bn + ". You're out of coins, just watch."), Pa, null);
                if (!PlayerOk) yield break;

                // đếm ngược rồi đánh
                locked = true;
                Cine.Focus(new Vector2(hall.X(40f), 2f), 0.6f);
                yield return Countdown();
                A.fighting = true; B.fighting = true;
                float t = 0f;
                while (!A.dead && !B.dead && t < 90f)
                {
                    if (!PlayerOk) yield break;
                    if (!BaseUi.IsOpen) t += Time.deltaTime;
                    float mx = Mathf.Lerp(A.transform.position.x, B.transform.position.x, 0.5f);
                    Cine.Focus(new Vector2(mx, 2f), 0.8f);
                    yield return null;
                }
                bool aWon = B.dead && !A.dead, bWon = A.dead && !B.dead;
                if (A != null && !A.dead) Object.Destroy(A.gameObject);
                if (B != null && !B.dead) Object.Destroy(B.gameObject);
                yield return Wait(0.6f);
                Cine.ReleaseFocus();

                // kết quả cược
                if (bet > 0)
                {
                    bool decisive = aWon || bWon;
                    bool win = (side == 0 && aWon) || (side == 1 && bWon);
                    if (!decisive)
                    {
                        Inventory.AddCoins(bet, false);
                        GameManager.Banner(Cfg.L("HÒA — hoàn tiền cược", "DRAW — bet refunded"), Cfg.Yellow, 2.2f);
                    }
                    else if (win)
                    {
                        Inventory.AddCoins(bet * 2, false);
                        Sfx.Play("victory", 0.7f, 1.2f);
                        GameManager.Banner(Cfg.L("THẮNG CƯỢC +" + bet + " XU", "BET WON +" + bet + " COINS"), Cfg.Lime, 2.4f);
                    }
                    else
                    {
                        Sfx.Play("defeat", 0.5f, 1.2f);
                        GameManager.Banner(Cfg.L("THUA CƯỢC -" + bet + " XU", "BET LOST -" + bet + " COINS"), Cfg.Red, 2.4f);
                    }
                }
                else if (aWon || bWon)
                    GameManager.Banner(Cfg.L(aWon ? "GÓC A THẮNG" : "GÓC B THẮNG", aWon ? "CORNER A WINS" : "CORNER B WINS"), Pa, 2.2f);
                locked = false;
                yield return Wait(1.4f);

                yield return Ask(PaName, Cfg.L("Anh muốn xem tiếp chứ?", "Keep watching?"), Pa, null,
                    new[] { Cfg.L("Xem trận tiếp", "Next fight"), Cfg.L("Ra về", "Leave") });
                if (pick != 0) break;
            }
            if (PlayerOk) yield return GoTo(Lobby, 31f);
        }

        static IEnumerator Countdown()
        {
            string[] t = { "3", "2", "1", Cfg.L("ĐẤU!", "FIGHT!") };
            for (int i = 0; i < t.Length; i++)
            {
                GameManager.Banner(t[i], i == 3 ? Cfg.Red : Cfg.Yellow, 0.7f);
                Sfx.Play(i == 3 ? "slam" : "warn", 0.5f, i == 3 ? 1.2f : 1.5f);
                yield return Wait(i == 3 ? 0.4f : 0.75f);
            }
        }

        // ================================================================ ĐẤU SĨ
        /// <summary>Một trận đấu của người chơi với 1 đối thủ AI. Kết quả ở duelWon (false = chết / bị gián đoạn).</summary>
        static IEnumerator Duel(ArenaFighter.Kind kind, float power)
        {
            duelWon = false;
            var hall = Hall();
            var pl = Player.Instance;
            if (pl == null) yield break;
            float ex = pl.Position.x < hall.X(40f) ? hall.X(FloorMax - 6f) : hall.X(FloorMin + 6f);
            var f = ArenaFighter.Spawn(hall.t, kind, ex, hall.X(FloorMin), hall.X(FloorMax), power, "", Color.white);
            f.FaceDir(ex > pl.Position.x ? -1 : 1);
            locked = true;
            GameManager.PlayerLocked = true;
            yield return Countdown();
            GameManager.PlayerLocked = false;
            if (!PlayerOk) { if (f != null) Object.Destroy(f.gameObject); yield break; }
            f.fighting = true;
            while (!f.dead)
            {
                if (!PlayerOk) { Object.Destroy(f.gameObject); yield break; }
                yield return null;
            }
            duelWon = true;
            locked = false;
        }

        static IEnumerator FighterLoop()
        {
            while (PlayerOk)
            {
                int tier = Town.FighterWins;
                var kind = (ArenaFighter.Kind)(tier % 3);
                float power = Mathf.Min(4f, 1f + 0.3f * tier);
                GameManager.Banner(Cfg.L("TRẬN ĐẤU #" + (tier + 1) + " — " + ArenaFighter.KindName(kind), "FIGHT #" + (tier + 1) + " — " + ArenaFighter.KindName(kind)), Cfg.Orange, 2.2f);
                yield return Wait(1.6f);
                yield return Duel(kind, power);
                if (!duelWon) yield break;                  // đã chết: GameManager lo phần còn lại

                int reward = 150 + 100 * tier;
                Town.FighterWins++;
                Inventory.AddCoins(reward, false);
                Sfx.Play("victory", 0.8f, 1f);
                Fx.Text(Player.Instance.Position + new Vector2(0f, 2.6f), "+" + reward, Cfg.Yellow, 1.3f);
                GameManager.Banner(Cfg.L("THẮNG! +" + reward + " XU", "VICTORY! +" + reward + " COINS"), Cfg.Lime, 2.6f);
                yield return Wait(1.8f);

                yield return Ask(PaName, Cfg.L("Anh còn muốn đấu tiếp không?", "Fight again?"), Pa, null,
                    new[] { Cfg.L("Đấu tiếp", "Fight again"), Cfg.L("Ra về", "Leave") });
                if (pick != 0) break;
            }
            if (PlayerOk) yield return GoTo(Lobby, 31f);
        }

        // ================================================================ THÔNG TIN (3 trận liền)
        static IEnumerator InfoLoop()
        {
            ArenaFighter.Kind[] ks = { ArenaFighter.Kind.Brute, ArenaFighter.Kind.Chain, ArenaFighter.Kind.Cannon };
            for (int i = 0; i < ks.Length; i++)
            {
                GameManager.Banner(Cfg.L("TRẬN " + (i + 1) + "/3 — " + ArenaFighter.KindName(ks[i]), "FIGHT " + (i + 1) + "/3 — " + ArenaFighter.KindName(ks[i])), Cfg.Orange, 2.2f);
                yield return Wait(1.6f);
                yield return Duel(ks[i], 1f);
                if (!duelWon) yield break;
                if (i < ks.Length - 1)
                {
                    GameManager.Banner(Cfg.L("THẮNG! Trận kế tiếp sắp bắt đầu…", "WIN! Next fight is coming…"), Cfg.Lime, 2.0f);
                    yield return Wait(2.2f);
                }
            }
            Inventory.AddCoins(1000, false);
            Sfx.Play("victory", 0.9f, 1f);
            Fx.Text(Player.Instance.Position + new Vector2(0f, 2.6f), "+1000", Cfg.Yellow, 1.4f);
            GameManager.Banner(Cfg.L("THẮNG LIÊN TIẾP 3 TRẬN! +1000 XU", "3 WINS IN A ROW! +1000 COINS"), Cfg.Lime, 3f);
            yield return Wait(2.2f);
            yield return GoTo(Lobby, 31f);

            Transform who = bouncer != null ? bouncer.transform : null;
            var l = new List<DlgLine>();
            var a = new DlgLine(BName, Cfg.L("Cậu có tiềm năng đấy! Đây là thẻ thành viên dành cho đấu sĩ.", "You've got potential! Here's a member's card for fighters."), Dark, who);
            a.onShow = () => { QuestItems.Give(QuestItems.FighterCard); Town.ArenaInfoDone = true; };
            l.Add(a);
            l.Add(new DlgLine(BName, Cfg.L("Sau này muốn tiền thì cứ đến đây", "If you want money later, just come here"), Dark, who));
            l.Add(new DlgLine(BName, Cfg.L("Tấm thẻ này sẽ giúp mày có thêm thông tin về “chúng tao”", "This card will get you more information about “us”"), Dark, who));
            yield return Talk(l);
        }
    }
}
