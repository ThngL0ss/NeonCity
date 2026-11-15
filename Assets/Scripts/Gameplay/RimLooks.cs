namespace NeonBreaker
{
    /// <summary>Ngoại hình có sẵn của các NPC / kẻ địch / lính đánh thuê ở Rìa Phố Neon (vẽ bằng code, xem TownArt.Human).</summary>
    public static class RimLooks
    {
        // ---- MerShop
        /// <summary>Lễ tân: trang phục nâu, quấn khăn cổ xám che nửa mặt dưới.</summary>
        public static readonly Look MerClerk = new Look("merclerk") { top = 0x7A5230, pants = 0x4A3220, hair = 0x2A1B14, skin = 0xD8A678, mask = 0x8A8F96, shoes = 0x2A1E14 };
        /// <summary>Furra: da đen, áo khoác đen dài, bịt mắt.</summary>
        public static readonly Look Furra = new Look("furra") { robe = true, top = 0x0C0C10, pants = 0x050508, shoes = 0x050508, hair = 0x0A0A0A, skin = 0x3B2820, blind = true, scale = 1.1f };

        // ---- Lính đánh thuê (xem MercDefs)
        static readonly Look[] war =
        {
            new Look("war1") { top = 0x5A6A4A, accent = 0xC8C8C8, pants = 0x3A4030, helmet = true, hat = 0x555B66, skin = 0xD8A678 },
            new Look("war2") { top = 0x6A5A3A, accent = 0xC8C8C8, pants = 0x3A3226, helmet = true, hat = 0x5A5A4A, skin = 0xC89870 },
            new Look("war3") { top = 0x4A5A7A, accent = 0xE8E8E8, pants = 0x2A3040, hat = 0x2A3040, hair = 0x1A1A1A, skin = 0xE8B890, shades = true },
            new Look("war4") { top = 0x7A4A4A, accent = 0xD8D8D8, pants = 0x3A2A2A, helmet = true, hat = 0x6A4A4A, skin = 0xB88862, beard = 0x2A1B14 },
            new Look("war5") { top = 0x3A6A6A, accent = 0xE8E8E8, pants = 0x203A3A, helmet = true, hat = 0x4A6A6A, skin = 0xD8A678 },
            new Look("war6") { top = 0x6A3A7A, accent = 0xFFD23F, pants = 0x2E1E3A, helmet = true, hat = 0x4A3A6A, skin = 0xC89870 },
            new Look("war7") { top = 0xC8A02A, accent = 0xFFFFFF, pants = 0x5A4A1A, helmet = true, hat = 0xE8D080, skin = 0xB88862, scale = 1.08f },
        };
        public static Look War(int i) { return war[((i % war.Length) + war.Length) % war.Length]; }
        public static readonly Look Kaiz = new Look("kaiz") { top = 0x2A3A5A, accent = 0xFFD23F, pants = 0x1A2234, helmet = true, hat = 0x2A2A3A, skin = 0x6A4A38, scale = 1.18f };
        public static readonly Look Connie = new Look("connie") { top = 0xC86A1A, accent = 0xFFE2B0, pants = 0x3A3A2A, shades = true, hair = 0x2A1B14, hairStyle = 2, skin = 0xE0B080 };

        // ---- Lều trại Ryber
        public static readonly Look Rossie = new Look("rossie") { top = 0x6A4A3A, pants = 0x2A3040, hair = 0x2A1B14, hairStyle = 1, skin = 0xE0A880, sleeveless = true, shoes = 0x1A1410 };
        public static readonly Look MaskedGuard = new Look("rymask") { top = 0x4A3A2A, accent = 0x8A7A5A, pants = 0x2A2418, mask = 0x1E1E24, hat = 0x2A2418, hairStyle = 0, skin = 0xC89870 };
        static readonly Look[] crew =
        {
            new Look("ry0") { top = 0x6A5A48, accent = 0x8A3A2A, pants = 0x3A3026, hair = 0x3A2A1E, skin = 0xD8A678, hairStyle = 2 },
            new Look("ry1") { top = 0x4A5A3A, pants = 0x2E2A20, hair = 0x1A1A1A, skin = 0xC89870, beard = 0x1A1A1A, hat = 0x5A4A32 },
            new Look("ry2") { top = 0x7A3A32, accent = 0xC8B890, pants = 0x2A2620, hair = 0x6A4A2A, skin = 0xE8B890, sleeveless = true },
            new Look("ry3") { top = 0x5A5A62, pants = 0x32302A, hair = 0x8A8A8A, skin = 0xB88862, hairStyle = 2, beard = 0x8A8A8A },
            new Look("ry4") { top = 0x8A6A32, accent = 0x3A2A1A, pants = 0x2A3228, hair = 0x2A1B14, skin = 0xD8A678, mask = 0x6A5A3A },
        };
        public static Look Crew(int i) { return crew[((i % crew.Length) + crew.Length) % crew.Length]; }
        /// <summary>Cậu bé cầm túi xu (nhỏ hơn người lớn).</summary>
        public static readonly Look Kid = new Look("kid") { top = 0xB0603A, pants = 0x3A3A4A, hair = 0x3A2A1E, skin = 0xE8B890, scale = 0.68f, shoes = 0x2A2018 };
        /// <summary>Lính áo giáp trắng, mũ trắng (của "VIP").</summary>
        public static readonly Look WhiteGuard = new Look("wguard") { top = 0xDCDCE6, accent = 0x9AA0B0, pants = 0xB8B8C4, shoes = 0x555566, helmet = true, hat = 0xF2F2F8, skin = 0xE0B080 };

        // ---- Bãi rác
        public static readonly Look Beggar = new Look("beggar") { top = 0x6A5A48, pants = 0x4A4034, hair = 0x6A6A6A, hairStyle = 2, beard = 0x9A9A9A, skin = 0xC09070, shoes = 0x2A2418 };
        static readonly Look[] scav =
        {
            new Look("sc0") { top = 0x5A5048, pants = 0x3A342C, hair = 0x2A2A2A, skin = 0xC89870, hairStyle = 2, hat = 0x4A4034 },
            new Look("sc1") { top = 0x4A5048, pants = 0x2E322C, hair = 0x8A8A8A, skin = 0xD8A678, beard = 0x7A7A7A },
            new Look("sc2") { top = 0x6A4A3A, pants = 0x3A2E26, hair = 0x1A1410, skin = 0xB88862, hairStyle = 0 },
            new Look("sc3") { top = 0x585868, pants = 0x30303A, hair = 0x4A3A2A, skin = 0xE8B890, hairStyle = 2 },
        };
        public static Look Scav(int i) { return scav[((i % scav.Length) + scav.Length) % scav.Length]; }
        static readonly Look[] robbers =
        {
            new Look("rb0") { top = 0x5A2A2A, accent = 0x2A2A2A, pants = 0x2A2220, hair = 0x1A1A1A, skin = 0xC89870, mask = 0x3A3A3A },
            new Look("rb1") { top = 0x2A4A5A, pants = 0x20262A, hair = 0x3A2A1E, skin = 0xD8A678, beard = 0x2A1B14, hat = 0x2A2A30 },
            new Look("rb2") { top = 0x6A5A2A, accent = 0x3A2A1A, pants = 0x2A2618, hair = 0x6A4A2A, skin = 0xB88862, sleeveless = true },
            new Look("rb3") { top = 0x4A2A5A, pants = 0x261C2E, hair = 0x120E0A, skin = 0xE0B080, shades = true },
        };
        public static Look Robber(int i) { return robbers[((i % robbers.Length) + robbers.Length) % robbers.Length]; }
    }
}
