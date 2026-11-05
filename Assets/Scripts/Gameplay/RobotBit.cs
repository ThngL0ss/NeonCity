using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Mảnh linh kiện robot nhỏ bay lượn trong cinematic (cảnh chết: thân xác vỡ thành mảnh; cảnh hồi sinh: mảnh bay vào ghép lại).
    /// Tự sinh lúc chạy bằng RobotBit.Spawn, tự huỷ khi hết thời gian. Sprite lấy từ ArtBank ▸ Robot Bits (không có thì bỏ qua).
    /// </summary>
    public class RobotBit : MonoBehaviour
    {
        Vector2 vel;
        float gravity, spin, life, maxLife, drag;
        bool homing;
        Vector2 target;
        SpriteRenderer sr;
        Color tint;

        /// <summary>Bay thường: vận tốc ban đầu, có trọng lực, mờ dần cuối đời.</summary>
        public static RobotBit Spawn(Vector2 pos, Vector2 velocity, float lifetime, float grav = 14f, Color? color = null)
        {
            var bank = Art.Bank;
            if (bank == null || bank.robotBits == null || bank.robotBits.Length == 0) return null;
            var s = bank.robotBits[Random.Range(0, bank.robotBits.Length)];
            if (s == null) return null;
            var go = new GameObject("RobotBit");
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            var b = go.AddComponent<RobotBit>();
            b.sr = go.AddComponent<SpriteRenderer>();
            b.sr.sprite = s;
            b.sr.sortingOrder = 70;
            Art.Init();
            b.sr.sharedMaterial = Art.DefMat;
            b.tint = color ?? Color.white;
            b.sr.color = b.tint;
            b.vel = velocity;
            b.gravity = grav;
            b.drag = 0.6f;
            b.spin = Random.Range(-540f, 540f);
            b.life = b.maxLife = Mathf.Max(0.1f, lifetime);
            return b;
        }

        /// <summary>Bay tới đích (lúc hồi sinh): đi thẳng từ vị trí xuất phát đến đích trong lifetime giây rồi biến mất.</summary>
        public static RobotBit Fly(Vector2 from, Vector2 to, float lifetime, Color? color = null)
        {
            var b = Spawn(from, Vector2.zero, lifetime, 0f, color);
            if (b == null) return null;
            b.homing = true;
            b.target = to;
            b.vel = (to - from) / Mathf.Max(0.1f, lifetime);
            b.drag = 0f;
            return b;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            life -= dt;
            if (life <= 0f) { Destroy(gameObject); return; }
            if (!homing)
            {
                vel.y -= gravity * dt;
                vel *= Mathf.Exp(-drag * dt);
            }
            transform.position += (Vector3)(vel * dt);
            transform.Rotate(0f, 0f, spin * dt * (homing ? 0.5f : 1f));
            float u = life / maxLife;
            float a = homing ? Mathf.Clamp01((1f - u) * 6f) : Mathf.Clamp01(u * 3f);
            sr.color = new Color(tint.r, tint.g, tint.b, a);
        }
    }
}
