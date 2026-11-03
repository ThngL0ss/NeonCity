using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Lớp cơ sở cho các bẫy. Mỗi loại bẫy (SpikeTrap, LaserTrap, SawTrap, TurretTrap, MineTrap) là một Prefab đặt trong Scene
    /// (Level/Traps). Chọn bẫy trong Scene rồi chỉnh số trong Inspector: sát thương, chu kỳ, tầm... Mọi thông số đều public.
    /// </summary>
    public abstract class Trap : MonoBehaviour
    {
        [Header("Sát thương (chung)")]
        [Tooltip("Sát thương gây cho người chơi mỗi lần trúng")] public float damage = 15f;
        [Tooltip("Lực đẩy lùi người chơi khi trúng")] public float knockback = 7f;

        [Header("Hoạt động (chung)")]
        [Tooltip("Bẫy chỉ chạy khi người chơi ở trong khoảng này theo trục ngang (0 = luôn chạy). Nằm ngoài khoảng, bẫy đứng yên.")]
        public float activeRange = 45f;

        /// <summary>Bẫy đang được phép chạy: game đang chơi (không tạm dừng) và người chơi ở đủ gần.</summary>
        protected bool Running
        {
            get
            {
                if (!GameManager.Active) return false;
                if (activeRange <= 0f) return true;
                var p = Player.Instance;
                return p != null && Mathf.Abs(p.Position.x - transform.position.x) <= activeRange;
            }
        }

        protected static Player Target
        {
            get
            {
                var p = Player.Instance;
                return (p != null && !p.Dead) ? p : null;
            }
        }

        /// <summary>Gây sát thương cho người chơi. Trả về true nếu thực sự trúng (không bị né bằng lướt / bất tử).</summary>
        protected bool HurtPlayer(Vector2 from)
        {
            var p = Target;
            if (p == null) return false;
            return p.Hurt(damage, from, knockback) > 0f;
        }

        /// <summary>Bẫy cũng gây sát thương cho minibot (đồng minh của người chơi) nằm trong vùng.</summary>
        protected void HurtBotsRect(Rect area, Vector2 from)
        {
            Minibot.HurtInRect(area, damage, from, knockback);
        }

        protected void HurtBotsCircle(Vector2 center, float radius, Vector2 from)
        {
            Minibot.HurtInCircle(center, radius, damage, from, knockback);
        }

        protected SpriteAnimator FindAnim()
        {
            var v = transform.Find("Visual");
            return v != null ? v.GetComponent<SpriteAnimator>() : null;
        }

        protected SpriteRenderer FindSr(string childName)
        {
            var t = transform.Find(childName);
            return t != null ? t.GetComponent<SpriteRenderer>() : null;
        }
    }
}
