namespace NeonBreaker
{
    /// <summary>Ngoại hình có sẵn của các NPC trong thị trấn (vẽ bằng code, xem TownArt.Human).</summary>
    public static class TownLooks
    {
        // ---- CenterShop
        public static readonly Look Seller = new Look("seller") { top = 0x1F6F7B, accent = 0xE8F0FF, tie = 0xFF9A1A, pants = 0x1B1F2E, hair = 0x3A2A1E };
        public static readonly Look Guard = new Look("guard") { top = 0x2B3358, accent = 0xFFD23F, hat = 0x1B1F2E, pants = 0x14161F, hair = 0x201810, shoes = 0x0A0A10 };

        // ---- khách vãng lai
        static readonly Look[] customers =
        {
            new Look("cu0") { top = 0xB5486A, pants = 0x2A2A3C, hair = 0x241612, skin = 0xF0C8A0, hairStyle = 2 },
            new Look("cu1") { top = 0x3F8F5A, pants = 0x33384C, hair = 0x6A4A2A, skin = 0xD8A678 },
            new Look("cu2") { top = 0xD8A23A, pants = 0x26304A, hair = 0x1A1A1A, skin = 0xE8B890, hat = 0x7A2A2A },
            new Look("cu3") { top = 0x5A6AD8, pants = 0x3A2A2A, hair = 0xC8C8D0, skin = 0xC89870, hairStyle = 1 },
            new Look("cu4") { top = 0xA8A8B8, pants = 0x20202C, hair = 0x120E0A, skin = 0xB88862, hairStyle = 2 },
            new Look("cu5") { top = 0xD86A3A, pants = 0x2C3A2C, hair = 0x3A2A1E, skin = 0xF0C8A0, accent = 0xFFE2B0 },
            new Look("cu6") { top = 0x7A3AB0, pants = 0x1E1E2C, hair = 0x2A1B14, skin = 0xE0B080, shades = true },
        };
        public static Look Customer(int i) { return customers[((i % customers.Length) + customers.Length) % customers.Length]; }

        // ---- RoboStore
        public static readonly Look Receptionist = new Look("recep") { top = 0x40204A, accent = 0xFF2FB5, tie = 0xFF2FB5, pants = 0x14101E, hair = 0xE8E0D0, skin = 0xF0D0B0, hairStyle = 2 };
        public static readonly Look RoboGuard = new Look("robog") { top = 0x1A1D30, accent = 0xFF2FB5, hat = 0x0A0C1C, pants = 0x0E1020, shades = true, hair = 0x101018 };

        // ---- ZenMonastery
        public static readonly Look Monk = new Look("monk") { top = 0x7A4A2B, robe = true, hairStyle = 0, skin = 0xD8A678, shoes = 0x3A2515 };
        public static readonly Look Monk2 = new Look("monk2") { top = 0x8A5A33, robe = true, hairStyle = 0, skin = 0xE8B890, shoes = 0x3A2515 };
        public static readonly Look Monk3 = new Look("monk3") { top = 0x6A3E24, robe = true, hairStyle = 0, skin = 0xC89870, shoes = 0x3A2515 };
        public static Look MonkN(int i) { return (i % 3 == 0) ? Monk : (i % 3 == 1) ? Monk2 : Monk3; }
        public static readonly Look Shengu = new Look("shengu") { top = 0xB02020, robe = true, hairStyle = 0, skin = 0xD8A678, beard = 0xEFEFEF, shoes = 0x3A2515, accent = 0xE8B84A };

        // ---- DesTown4 / ArenaCyber
        public static readonly Look BlackSuit = new Look("bsuit") { top = 0x0C0C12, tie = 0xC8102E, pants = 0x08080C, shades = true, hair = 0x08080C, shoes = 0x050508 };
        public static readonly Look ArenaBouncer = new Look("bounc") { top = 0x0C0C12, tie = 0xC8102E, pants = 0x08080C, shades = true, hair = 0x08080C, shoes = 0x050508, skin = 0xC89870 };
        public static readonly Look Fighter1 = new Look("fig1") { top = 0x8A2A2A, accent = 0xFFD23F, pants = 0x2A1A1A, hair = 0x101010, skin = 0xC89870, hairStyle = 0, shoes = 0x1A0A0A };
        public static readonly Look Fighter2 = new Look("fig2") { top = 0x2A6A8A, accent = 0x35E8FF, pants = 0x14202A, hair = 0xE8E0D0, skin = 0xF0C8A0, hairStyle = 2, shoes = 0x0A1018 };
        public static readonly Look Fighter3 = new Look("fig3") { top = 0x3A3A48, accent = 0xFF2FB5, pants = 0x1A1A24, hair = 0x101018, skin = 0xB88862, hairStyle = 0, shades = true, shoes = 0x0A0A12 };
    }
}
