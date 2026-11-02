using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Giá trị dùng chung toàn game: ngôn ngữ, layer, mốc màn chơi (do Level đặt), bảng màu neon.
    /// Chỉ số cân bằng của từng nhân vật nằm trong Inspector của Prefab tương ứng, không còn ở đây.
    /// </summary>
    public static class Cfg
    {
        // ------------------------------------------------------------ Language
        public static bool Vietnamese = true;
        public static string L(string vi, string en) { return Vietnamese ? vi : en; }

        // ------------------------------------------------------------ Layers
        // Ground/platforms dùng layer 0 (Default). Các layer 8-10 chỉ dùng theo số, không cần đặt tên.
        public const int LayerPlayer = 8;
        public const int LayerEnemy = 9;
        public const int LayerItem = 10;
        public static int GroundMask { get { return 1 << 0; } }

        // ------------------------------------------------------------ Level layout
        public static float LevelMinX = -10f;
        public static float LevelMaxX = 170f;
        public static float GateX = 128f;          // cổng vào khu boss
        public static float BossTriggerX = 135f;   // đi qua mốc này (khi đã phá hết trụ) => boss xuất hiện
        public static float AltarX = 150f;         // tâm khu boss

        // ------------------------------------------------------------ Quái & vật phẩm (giá trị lấy từ object Level trong Scene)
        public static int MaxEnemiesAlive = 8;       // trần số quái nhỏ cùng lúc trên toàn màn
        public static float DropChance = 0.20f;      // tỉ lệ rơi vật phẩm hồi máu

        // Chỉ số của Player, quái, trụ, boss nằm trong Inspector của từng Prefab (Player, Walker, Flyer, Heavy, Rusher, Tower, Boss).

        // ------------------------------------------------------------ Palette (neon)
        public static readonly Color Cyan = new Color(0.10f, 0.95f, 1.00f, 1f);
        public static readonly Color Magenta = new Color(1.00f, 0.15f, 0.80f, 1f);
        public static readonly Color Purple = new Color(0.55f, 0.30f, 1.00f, 1f);
        public static readonly Color Lime = new Color(0.45f, 1.00f, 0.40f, 1f);
        public static readonly Color Orange = new Color(1.00f, 0.60f, 0.10f, 1f);
        public static readonly Color Red = new Color(1.00f, 0.20f, 0.25f, 1f);
        public static readonly Color Yellow = new Color(1.00f, 0.92f, 0.30f, 1f);
        public static readonly Color White = new Color(1f, 1f, 1f, 1f);
        public static readonly Color Night = new Color(0.020f, 0.025f, 0.075f, 1f);

        public static Color A(Color c, float a) { return new Color(c.r, c.g, c.b, a); }
    }

    /// <summary>Tương thích Unity 6 (linearVelocity) và các bản cũ (velocity).</summary>
    public static class PhysExt
    {
        public static Vector2 Vel(this Rigidbody2D rb)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }

        public static void SetVel(this Rigidbody2D rb, Vector2 v)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
        }
    }
}
