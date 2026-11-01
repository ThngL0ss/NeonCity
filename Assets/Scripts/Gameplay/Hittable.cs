using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    /// <summary>Hình học 2D nhỏ dùng chung.</summary>
    public static class Geo
    {
        public static bool CircleRect(Vector2 c, float r, Rect rect)
        {
            float cx = Mathf.Clamp(c.x, rect.xMin, rect.xMax);
            float cy = Mathf.Clamp(c.y, rect.yMin, rect.yMax);
            float dx = c.x - cx, dy = c.y - cy;
            return dx * dx + dy * dy <= r * r;
        }

        public static Rect Box(Vector2 center, Vector2 size)
        {
            return new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);
        }
    }

    /// <summary>
    /// Mọi thứ người chơi có thể đánh trúng (quái nhỏ, trụ, boss).
    /// Va chạm đòn đánh được tính bằng hình chữ nhật (không dùng collider) => ổn định, dễ cân bằng.
    /// </summary>
    public abstract class Hittable : MonoBehaviour
    {
        public static readonly List<Hittable> All = new List<Hittable>();

        public Vector2 hitSize = Vector2.one;
        public Vector2 hitOffset = Vector2.zero;
        public float hp, maxHp;
        public bool dead;

        protected virtual void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        protected virtual void OnDisable() { All.Remove(this); }

        public Vector2 Center { get { return (Vector2)transform.position + hitOffset; } }
        public Rect HitRect { get { return Geo.Box(Center, hitSize); } }

        public abstract bool Targetable { get; }
        public abstract void TakeHit(float dmg, Vector2 dir, float knock, bool melee);

        public static List<Hittable> Snapshot() { return new List<Hittable>(All); }
    }
}
