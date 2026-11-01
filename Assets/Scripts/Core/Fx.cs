using System.Collections.Generic;
using UnityEngine;

namespace NeonBreaker
{
    public class FloatText
    {
        public Vector2 pos;
        public string text;
        public Color color;
        public float life, maxLife, size;
    }

    /// <summary>
    /// Hệ thống hiệu ứng: hạt, vòng sóng, tia, chữ nổi, rung camera, hit-stop, flash màn hình.
    /// Tất cả dùng một pool SpriteRenderer additive => rực rỡ kiểu neon, không cần asset.
    /// </summary>
    public class Fx : MonoBehaviour
    {
        public const int S_Glow = 0, S_Ring = 1, S_Square = 2, S_Diamond = 3, S_Arc = 4, S_Star = 5, S_Circle = 6, S_Tri = 7;

        struct P
        {
            public bool alive, align;
            public Vector2 pos, vel, size0, size1;
            public float life, maxLife, gravity, drag, rot, rotSpeed;
            public Color c0, c1;
        }

        const int Max = 1200;
        static Fx inst;
        P[] ps = new P[Max];
        SpriteRenderer[] srs = new SpriteRenderer[Max];
        Sprite[] sprites;
        int cursor;

        public static List<FloatText> Texts = new List<FloatText>();
        public static float Trauma;
        public static float HitStopTimer;
        public static Color FlashColor = Color.white;
        public static float FlashAlpha;

        public static void Create(Transform parent)
        {
            Art.Init();
            var go = new GameObject("Fx");
            if (parent != null) go.transform.SetParent(parent, false);
            inst = go.AddComponent<Fx>();
        }

        void Awake()
        {
            inst = this;
            Art.Init();
            sprites = new Sprite[] { Art.Glow, Art.Ring, Art.Square, Art.Diamond, Art.Arc, Art.Star, Art.Circle, Art.Triangle };
            for (int i = 0; i < Max; i++)
            {
                var go = new GameObject("p");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sharedMaterial = Art.AddMat;
                sr.sortingOrder = 40;
                sr.enabled = false;
                srs[i] = sr;
            }
        }

        void OnDestroy() { if (inst == this) inst = null; }

        public static void ResetStatics()
        {
            inst = null;
            Texts = new List<FloatText>();
            Trauma = 0f;
            HitStopTimer = 0f;
            FlashAlpha = 0f;
        }

        public static void Clear()
        {
            Texts.Clear();
            Trauma = 0f;
            HitStopTimer = 0f;
            FlashAlpha = 0f;
            if (inst == null) return;
            for (int i = 0; i < Max; i++)
            {
                inst.ps[i].alive = false;
                inst.srs[i].enabled = false;
            }
        }

        void Update()
        {
            float udt = Time.unscaledDeltaTime;
            if (HitStopTimer > 0f) HitStopTimer -= udt;
            if (Trauma > 0f) Trauma = Mathf.Max(0f, Trauma - 1.7f * udt);
            if (FlashAlpha > 0f) FlashAlpha = Mathf.Max(0f, FlashAlpha - 2.6f * udt);

            float dt = Time.deltaTime;
            for (int i = 0; i < Max; i++)
            {
                if (!ps[i].alive) continue;
                ps[i].life -= dt;
                if (ps[i].life <= 0f)
                {
                    ps[i].alive = false;
                    srs[i].enabled = false;
                    continue;
                }
                ps[i].vel.y -= ps[i].gravity * dt;
                if (ps[i].drag > 0f) ps[i].vel *= Mathf.Max(0f, 1f - ps[i].drag * dt);
                ps[i].pos += ps[i].vel * dt;
                float t = 1f - ps[i].life / ps[i].maxLife;
                if (ps[i].align)
                {
                    if (ps[i].vel.sqrMagnitude > 0.0001f)
                        ps[i].rot = Mathf.Atan2(ps[i].vel.y, ps[i].vel.x) * Mathf.Rad2Deg;
                }
                else ps[i].rot += ps[i].rotSpeed * dt;

                Transform tr = srs[i].transform;
                tr.position = new Vector3(ps[i].pos.x, ps[i].pos.y, 0f);
                tr.rotation = Quaternion.Euler(0f, 0f, ps[i].rot);
                Vector2 s = Vector2.Lerp(ps[i].size0, ps[i].size1, t);
                tr.localScale = new Vector3(s.x, s.y, 1f);
                srs[i].color = Color.Lerp(ps[i].c0, ps[i].c1, t);
            }

            for (int i = Texts.Count - 1; i >= 0; i--)
            {
                var ft = Texts[i];
                ft.life -= dt;
                ft.pos.y += 1.6f * dt;
                if (ft.life <= 0f) Texts.RemoveAt(i);
            }
        }

        // ------------------------------------------------------------ low level
        public static void Emit(int spr, Vector2 pos, Vector2 vel, Vector2 size0, Vector2 size1, Color c0, Color c1,
                                float life, float gravity = 0f, float drag = 0f, float rot = 0f, float rotSpeed = 0f, bool align = false)
        {
            if (inst == null) return;
            int i = inst.cursor;
            inst.cursor = (inst.cursor + 1) % Max;
            inst.ps[i] = new P
            {
                alive = true, align = align, pos = pos, vel = vel, size0 = size0, size1 = size1,
                life = life, maxLife = Mathf.Max(0.01f, life), gravity = gravity, drag = drag, rot = rot, rotSpeed = rotSpeed,
                c0 = c0, c1 = c1
            };
            var sr = inst.srs[i];
            sr.sprite = inst.sprites[spr];
            sr.enabled = true;
            Transform tr = sr.transform;
            tr.position = new Vector3(pos.x, pos.y, 0f);
            tr.rotation = Quaternion.Euler(0f, 0f, rot);
            tr.localScale = new Vector3(size0.x, size0.y, 1f);
            sr.color = c0;
        }

        // ------------------------------------------------------------ helpers
        /// <summary>Nổ hạt tròn phát sáng theo mọi hướng.</summary>
        public static void Burst(Vector2 pos, Color c, int count, float speed, float life, float size, float gravity = 0f, float drag = 2.2f)
        {
            for (int i = 0; i < count; i++)
            {
                float a = Random.value * 6.2831853f;
                float sp = speed * Random.Range(0.25f, 1f);
                Vector2 v = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * sp;
                float sz = size * Random.Range(0.6f, 1.2f);
                Emit(S_Glow, pos, v, new Vector2(sz, sz), new Vector2(0f, 0f), c, Cfg.A(c, 0f), life * Random.Range(0.6f, 1f), gravity, drag);
            }
        }

        /// <summary>Tia lửa kéo dài theo hướng bay, tỏa theo hướng dir.</summary>
        public static void Sparks(Vector2 pos, Vector2 dir, Color c, int count, float speed = 12f, float spread = 55f, float life = 0.35f)
        {
            float baseAng = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            for (int i = 0; i < count; i++)
            {
                float a = (baseAng + Random.Range(-spread, spread)) * Mathf.Deg2Rad;
                float sp = speed * Random.Range(0.4f, 1f);
                Vector2 v = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * sp;
                Emit(S_Square, pos, v, new Vector2(0.55f, 0.07f), new Vector2(0.05f, 0.03f), Color.Lerp(c, Color.white, 0.5f), Cfg.A(c, 0f),
                     life * Random.Range(0.6f, 1f), 6f, 2.5f, 0f, 0f, true);
            }
        }

        public static void Ring(Vector2 pos, Color c, float s0, float s1, float dur)
        {
            Emit(S_Ring, pos, Vector2.zero, new Vector2(s0, s0), new Vector2(s1, s1), c, Cfg.A(c, 0f), dur);
        }

        public static void Flare(Vector2 pos, Color c, float size, float dur)
        {
            Emit(S_Glow, pos, Vector2.zero, new Vector2(size, size), new Vector2(size * 1.6f, size * 1.6f), c, Cfg.A(c, 0f), dur);
        }

        public static void Trail(Vector2 pos, Color c, float size, float life)
        {
            Emit(S_Glow, pos, Vector2.zero, new Vector2(size, size), Vector2.zero, c, Cfg.A(c, 0f), life);
        }

        /// <summary>Vệt chém hình lưỡi liềm. facing = +1 / -1.</summary>
        public static void Slash(Vector2 pos, int facing, Color c, float scale = 1f, float rot = 0f)
        {
            Emit(S_Arc, pos, new Vector2(facing * 2.5f, 0f), new Vector2(facing * 2.3f * scale, 2.5f * scale),
                 new Vector2(facing * 3.0f * scale, 2.9f * scale), Color.Lerp(c, Color.white, 0.4f), Cfg.A(c, 0f), 0.2f, 0f, 3f, rot);
        }

        /// <summary>Tia sáng thẳng từ a tới b.</summary>
        public static void Beam(Vector2 a, Vector2 b, Color c, float width, float life)
        {
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 0.01f) return;
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            Vector2 mid = (a + b) * 0.5f;
            Emit(S_Square, mid, Vector2.zero, new Vector2(len, width), new Vector2(len, 0f), Color.Lerp(c, Color.white, 0.6f), Cfg.A(c, 0f), life, 0f, 0f, ang);
            Emit(S_Square, mid, Vector2.zero, new Vector2(len, width * 3.5f), new Vector2(len, 0f), Cfg.A(c, 0.35f), Cfg.A(c, 0f), life, 0f, 0f, ang);
        }

        public static void Ghost(Vector2 pos, Vector2 size, Color c, float life, int spr = S_Square)
        {
            Emit(spr, pos, Vector2.zero, size, size * 0.8f, Cfg.A(c, 0.55f), Cfg.A(c, 0f), life);
        }

        /// <summary>Vụ nổ neon nhiều lớp. big=true cho trụ / boss.</summary>
        public static void Explosion(Vector2 pos, Color c, bool big)
        {
            Burst(pos, c, big ? 46 : 18, big ? 15f : 9f, big ? 0.9f : 0.55f, big ? 0.55f : 0.34f, 0f, 1.8f);
            Burst(pos, Cfg.White, big ? 16 : 6, big ? 9f : 5f, 0.4f, big ? 0.4f : 0.22f, 0f, 2.5f);
            Sparks(pos, Vector2.up, c, big ? 26 : 10, big ? 20f : 13f, 180f, big ? 0.7f : 0.45f);
            Ring(pos, c, 0.3f, big ? 7f : 3f, big ? 0.55f : 0.35f);
            Flare(pos, Color.Lerp(c, Color.white, 0.5f), big ? 5f : 2.2f, big ? 0.35f : 0.22f);
        }

        public static void Text(Vector2 pos, string s, Color c, float size = 1f)
        {
            Texts.Add(new FloatText { pos = pos + new Vector2(Random.Range(-0.3f, 0.3f), 0f), text = s, color = c, life = 0.85f, maxLife = 0.85f, size = size });
            if (Texts.Count > 40) Texts.RemoveAt(0);
        }

        public static void Shake(float amount) { Trauma = Mathf.Min(1f, Trauma + amount); }
        public static void HitStop(float dur) { HitStopTimer = Mathf.Max(HitStopTimer, dur); }
        public static void Flash(Color c, float a) { FlashColor = c; FlashAlpha = Mathf.Max(FlashAlpha, a); }
    }
}
