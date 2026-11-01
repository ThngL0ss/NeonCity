using System.Collections.Generic;
using UnityEngine;
using Fn = System.Func<float, float>;

namespace NeonBreaker
{
    /// <summary>
    /// Phát âm thanh/nhạc. Clip lấy từ AudioBank (file WAV thật do Build Project xuất ra - bạn thay được);
    /// âm nào thiếu sẽ được tổng hợp bằng code (synth) làm dự phòng.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        const int SR = 44100;
        static Sfx inst;
        public static float MusicVolume = 0.40f;
        public static float SfxVolume = 0.85f;
        public static Transform Listener;

        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        AudioSource[] pool;
        int poolIdx;
        AudioSource music;
        readonly AudioClip[] tracks = new AudioClip[2];
        int curTrack = -1, wantTrack = -1;
        float musicVol;

        enum Wave { Sine, Saw, Square, Tri }

        public static void Create(Transform parent)
        {
            var go = new GameObject("Sfx");
            if (parent != null) go.transform.SetParent(parent, false);
            inst = go.AddComponent<Sfx>();
        }

        static readonly string[] RequiredIds =
        {
            "jump", "land", "slash", "hit", "dash", "throw", "shurHit", "heal", "pickup", "hurt", "enemyDie", "towerHit", "towerDie",
            "spawn", "warn", "charge", "laser", "bolt", "bossRoar", "slam", "teleport", "shock", "drain", "phase", "victory", "defeat",
            "click", "music_explore", "music_boss"
        };

        bool initialized;

        void Awake() { Init(false); }

        /// <summary>
        /// forceSynth = true: bỏ qua AudioBank, tổng hợp lại toàn bộ (dùng khi xuất file WAV trong Editor).
        /// Bình thường: dùng clip trong AudioBank; chỉ tổng hợp bằng code cho những âm còn thiếu.
        /// </summary>
        public void Init(bool forceSynth)
        {
            if (initialized) return;
            initialized = true;
            if (!forceSynth) inst = this;

            pool = new AudioSource[14];
            for (int i = 0; i < pool.Length; i++)
            {
                pool[i] = gameObject.AddComponent<AudioSource>();
                pool[i].playOnAwake = false;
                pool[i].spatialBlend = 0f;
            }
            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.spatialBlend = 0f;
            music.volume = 0f;

            AudioBank bank = forceSynth ? null : AudioBank.Load();
            bool complete = bank != null;
            if (bank != null)
                for (int i = 0; i < RequiredIds.Length; i++)
                    if (bank.Get(RequiredIds[i]) == null) { complete = false; break; }

            if (!complete)
            {
                BuildSfx();
                // Track 0: nhạc khám phá (120 BPM, Am-F-C-G) | Track 1: nhạc boss (150 BPM, Dm-Bb-Gm-A)
                tracks[0] = BuildMusic("music_explore", 120f,
                    new[] { 33, 29, 36, 31 },
                    new[] { new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 60, 64, 67 }, new[] { 55, 59, 62 } }, false);
                tracks[1] = BuildMusic("music_boss", 150f,
                    new[] { 26, 22, 31, 33 },
                    new[] { new[] { 62, 65, 69 }, new[] { 58, 62, 65 }, new[] { 55, 58, 62 }, new[] { 57, 61, 64 } }, true);
            }

            if (bank != null)   // clip trong AudioBank luôn được ưu tiên
            {
                for (int i = 0; i < bank.entries.Count; i++)
                {
                    var e = bank.entries[i];
                    if (e == null || e.clip == null || string.IsNullOrEmpty(e.id)) continue;
                    if (e.id == "music_explore") tracks[0] = e.clip;
                    else if (e.id == "music_boss") tracks[1] = e.clip;
                    else clips[e.id] = e.clip;
                }
            }
        }

        /// <summary>Chỉ dùng trong Editor: tổng hợp toàn bộ âm thanh/nhạc bằng code và trả về để xuất ra file WAV.</summary>
        public static Dictionary<string, AudioClip> SynthAll()
        {
            var go = new GameObject("SfxExport");
            go.hideFlags = HideFlags.HideAndDontSave;
            var s = go.AddComponent<Sfx>();
            s.Init(true);
            var d = new Dictionary<string, AudioClip>(s.clips);
            d["music_explore"] = s.tracks[0];
            d["music_boss"] = s.tracks[1];
            DestroyImmediate(go);
            return d;
        }

        void OnDestroy() { if (inst == this) inst = null; }

        public static void ResetStatics()
        {
            inst = null;
            Listener = null;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float target = wantTrack >= 0 ? MusicVolume : 0f;
            if (wantTrack != curTrack)
            {
                musicVol = Mathf.MoveTowards(musicVol, 0f, dt * 1.4f);
                if (musicVol <= 0.001f)
                {
                    curTrack = wantTrack;
                    if (curTrack >= 0)
                    {
                        music.clip = tracks[curTrack];
                        music.Play();
                    }
                    else music.Stop();
                }
            }
            else
            {
                musicVol = Mathf.MoveTowards(musicVol, target, dt * 0.7f);
            }
            music.volume = musicVol;
        }

        // ------------------------------------------------------------ public API
        public static void Play(string name, float vol = 1f, float pitch = 1f)
        {
            if (inst == null) return;
            AudioClip c;
            if (!inst.clips.TryGetValue(name, out c)) return;
            var src = inst.pool[inst.poolIdx];
            inst.poolIdx = (inst.poolIdx + 1) % inst.pool.Length;
            src.pitch = pitch * Random.Range(0.96f, 1.04f);
            src.PlayOneShot(c, Mathf.Clamp01(vol * SfxVolume));
        }

        /// <summary>Phát âm thanh tại vị trí thế giới: càng xa camera càng nhỏ.</summary>
        public static void PlayAt(string name, Vector2 pos, float vol = 1f, float pitch = 1f)
        {
            float lx = Listener != null ? Listener.position.x : pos.x;
            float d = Mathf.Abs(pos.x - lx);
            float att = 1f - Mathf.Clamp01((d - 8f) / 26f);
            if (att <= 0.02f) return;
            Play(name, vol * att, pitch);
        }

        public static void Music(int track) { if (inst != null) inst.wantTrack = track; }
        public static void StopMusic() { if (inst != null) inst.wantTrack = -1; }

        // ------------------------------------------------------------ synth primitives
        static int S(float sec) { return (int)(sec * SR); }
        static float Hz(int midi) { return 440f * Mathf.Pow(2f, (midi - 69) / 12f); }

        static float Osc(Wave w, float ph)
        {
            ph -= Mathf.Floor(ph);
            switch (w)
            {
                case Wave.Sine: return Mathf.Sin(ph * 6.2831853f);
                case Wave.Saw: return ph * 2f - 1f;
                case Wave.Square: return ph < 0.5f ? 1f : -1f;
                default: return 4f * Mathf.Abs(ph - 0.5f) - 1f;
            }
        }

        static Fn K(float v) { return t => v; }
        static Fn Sweep(float a, float b, float dur) { return t => a * Mathf.Pow(b / a, Mathf.Clamp01(t / dur)); }
        static Fn Lin(float a, float b, float dur) { return t => Mathf.Lerp(a, b, Mathf.Clamp01(t / dur)); }
        static Fn Env(float dur, float atk, float k, float gain)
        {
            return t => gain * Mathf.Min(1f, t / Mathf.Max(atk, 0.0005f)) * Mathf.Exp(-k * t) * Mathf.Clamp01((dur - t) * 60f);
        }

        static float[] Tone(float dur, Fn freq, Wave w, Fn amp)
        {
            int n = S(dur);
            var d = new float[n];
            float ph = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                ph += freq(t) / SR;
                d[i] = Osc(w, ph) * amp(t);
            }
            return d;
        }

        static float[] Noise(float dur, Fn amp, Fn coef)
        {
            int n = S(dur);
            var d = new float[n];
            uint s = 2463534242u;
            float y = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                s ^= s << 13; s ^= s >> 17; s ^= s << 5;
                float x = (s & 0xFFFF) / 32768f - 1f;
                float c = Mathf.Clamp(coef(t), 0.005f, 1f);
                y += c * (x - y);
                float comp = 1f / Mathf.Max(0.15f, Mathf.Sqrt(c / (2f - c)));
                d[i] = y * comp * 0.5f * amp(t);
            }
            return d;
        }

        static void Mix(float[] dst, float[] src, int off, float gain, bool wrap = false)
        {
            int n = dst.Length;
            for (int i = 0; i < src.Length; i++)
            {
                int j = off + i;
                if (j >= n)
                {
                    if (!wrap) break;
                    j %= n;
                }
                dst[j] += src[i] * gain;
            }
        }

        static void LP(float[] d, float a)
        {
            float y = 0f;
            for (int i = 0; i < d.Length; i++) { y += a * (d[i] - y); d[i] = y; }
        }

        static float[] Buf(float dur) { return new float[S(dur)]; }

        void Add(string name, float[] data, float drive = 1.2f)
        {
            for (int i = 0; i < data.Length; i++) data[i] = (float)System.Math.Tanh(data[i] * drive) * 0.9f;
            var clip = AudioClip.Create(name, data.Length, 1, SR, false);
            clip.SetData(data, 0);
            clips[name] = clip;
        }

        // ------------------------------------------------------------ sound effects
        void BuildSfx()
        {
            float[] b;

            // di chuyển
            Add("jump", Tone(0.16f, Sweep(260f, 640f, 0.16f), Wave.Sine, Env(0.16f, 0.004f, 10f, 0.55f)));
            b = Noise(0.09f, Env(0.09f, 0.001f, 40f, 0.7f), K(0.12f));
            Mix(b, Tone(0.09f, Sweep(110f, 50f, 0.09f), Wave.Sine, Env(0.09f, 0.001f, 30f, 0.6f)), 0, 1f);
            Add("land", b);

            // chiến đấu
            b = Noise(0.17f, Env(0.17f, 0.01f, 16f, 0.8f), Lin(0.08f, 0.7f, 0.17f));
            Mix(b, Tone(0.14f, Sweep(1100f, 280f, 0.14f), Wave.Saw, Env(0.14f, 0.002f, 20f, 0.18f)), 0, 1f);
            Add("slash", b);

            b = Tone(0.13f, Sweep(250f, 80f, 0.13f), Wave.Square, Env(0.13f, 0.001f, 20f, 0.45f));
            Mix(b, Noise(0.07f, Env(0.07f, 0.001f, 45f, 0.8f), K(0.6f)), 0, 1f);
            Add("hit", b);

            b = Noise(0.25f, Env(0.25f, 0.02f, 9f, 0.9f), Lin(0.03f, 0.45f, 0.25f));
            Mix(b, Tone(0.22f, Sweep(220f, 1100f, 0.22f), Wave.Sine, Env(0.22f, 0.01f, 8f, 0.3f)), 0, 1f);
            Add("dash", b);

            b = Tone(0.14f, Sweep(1500f, 700f, 0.14f), Wave.Sine, Env(0.14f, 0.002f, 14f, 0.5f));
            Mix(b, Tone(0.14f, Sweep(760f, 380f, 0.14f), Wave.Tri, Env(0.14f, 0.002f, 14f, 0.3f)), 0, 1f);
            Add("throw", b);

            b = Tone(0.09f, K(1900f), Wave.Tri, Env(0.09f, 0.001f, 34f, 0.5f));
            Mix(b, Noise(0.05f, Env(0.05f, 0.001f, 50f, 0.5f), K(0.8f)), 0, 1f);
            Add("shurHit", b);

            // hồi máu & vật phẩm
            b = Buf(0.7f);
            int[] hn = { 72, 76, 79, 84 };
            for (int i = 0; i < hn.Length; i++)
            {
                Mix(b, Tone(0.35f, K(Hz(hn[i])), Wave.Sine, Env(0.35f, 0.004f, 6f, 0.33f)), S(i * 0.07f), 1f);
                Mix(b, Tone(0.35f, K(Hz(hn[i]) * 2f), Wave.Tri, Env(0.35f, 0.004f, 8f, 0.1f)), S(i * 0.07f + 0.12f), 1f);
            }
            Add("heal", b);

            b = Buf(0.28f);
            Mix(b, Tone(0.15f, K(880f), Wave.Sine, Env(0.15f, 0.002f, 12f, 0.45f)), 0, 1f);
            Mix(b, Tone(0.2f, K(1320f), Wave.Sine, Env(0.2f, 0.002f, 10f, 0.45f)), S(0.06f), 1f);
            Add("pickup", b);

            // bị đánh / quái chết
            b = Tone(0.3f, Sweep(330f, 70f, 0.3f), Wave.Saw, Env(0.3f, 0.002f, 9f, 0.55f));
            Mix(b, Noise(0.2f, Env(0.2f, 0.001f, 18f, 0.7f), K(0.5f)), 0, 1f);
            Add("hurt", b);

            b = Noise(0.42f, Env(0.42f, 0.003f, 8f, 0.9f), Lin(0.5f, 0.04f, 0.42f));
            Mix(b, Tone(0.38f, Sweep(230f, 40f, 0.38f), Wave.Sine, Env(0.38f, 0.002f, 7f, 0.5f)), 0, 1f);
            Add("enemyDie", b);

            // trụ triệu hồi
            b = Buf(0.3f);
            float[] mh = { 523f, 785f, 1319f };
            for (int i = 0; i < mh.Length; i++)
                Mix(b, Tone(0.3f, K(mh[i]), Wave.Sine, Env(0.3f, 0.001f, 16f, 0.22f)), 0, 1f);
            Mix(b, Noise(0.05f, Env(0.05f, 0.001f, 50f, 0.5f), K(0.7f)), 0, 1f);
            Add("towerHit", b);

            b = Noise(1.2f, Env(1.2f, 0.004f, 3f, 1f), Lin(0.5f, 0.02f, 1.2f));
            Mix(b, Tone(1.1f, Sweep(110f, 26f, 1.1f), Wave.Sine, Env(1.1f, 0.004f, 2.4f, 0.95f)), 0, 1f);
            Mix(b, Tone(0.6f, Sweep(900f, 60f, 0.6f), Wave.Saw, Env(0.6f, 0.004f, 5f, 0.25f)), 0, 1f);
            Add("towerDie", b, 1.4f);

            b = Tone(0.5f, Sweep(220f, 1400f, 0.5f), Wave.Sine, Env(0.5f, 0.05f, 3f, 0.35f));
            Mix(b, Tone(0.5f, Sweep(330f, 2100f, 0.5f), Wave.Tri, Env(0.5f, 0.08f, 3f, 0.14f)), 0, 1f);
            Mix(b, Noise(0.3f, Env(0.3f, 0.05f, 7f, 0.25f), Lin(0.1f, 0.9f, 0.3f)), S(0.2f), 1f);
            Add("spawn", b);

            // cảnh báo / skill kẻ địch
            b = Buf(0.28f);
            Mix(b, Tone(0.09f, K(880f), Wave.Square, Env(0.09f, 0.002f, 10f, 0.28f)), 0, 1f);
            Mix(b, Tone(0.09f, K(880f), Wave.Square, Env(0.09f, 0.002f, 10f, 0.28f)), S(0.14f), 1f);
            Add("warn", b);

            b = Tone(0.7f, Sweep(140f, 900f, 0.7f), Wave.Saw, Env(0.7f, 0.05f, 1f, 0.3f));
            Mix(b, Tone(0.7f, Sweep(280f, 1800f, 0.7f), Wave.Sine, Env(0.7f, 0.05f, 1f, 0.2f)), 0, 1f);
            Add("charge", b);

            b = Noise(0.45f, Env(0.45f, 0.002f, 5f, 0.8f), Lin(0.6f, 0.1f, 0.45f));
            Mix(b, Tone(0.4f, Sweep(1500f, 200f, 0.4f), Wave.Saw, Env(0.4f, 0.002f, 6f, 0.3f)), 0, 1f);
            Add("laser", b);

            b = Tone(0.2f, Sweep(900f, 300f, 0.2f), Wave.Sine, Env(0.2f, 0.002f, 11f, 0.5f));
            Mix(b, Tone(0.2f, Sweep(450f, 150f, 0.2f), Wave.Square, Env(0.2f, 0.002f, 11f, 0.15f)), 0, 1f);
            Add("bolt", b);

            // boss
            b = Tone(1.4f, t => 70f + 9f * Mathf.Sin(t * 42f), Wave.Saw, Env(1.4f, 0.1f, 1.2f, 0.7f));
            LP(b, 0.25f);
            Mix(b, Noise(1.2f, Env(1.2f, 0.1f, 1.6f, 0.5f), Lin(0.05f, 0.25f, 1.2f)), 0, 1f);
            Mix(b, Tone(1.3f, K(46f), Wave.Sine, Env(1.3f, 0.05f, 1.4f, 0.8f)), 0, 1f);
            Add("bossRoar", b, 1.5f);

            b = Tone(0.75f, Sweep(95f, 24f, 0.75f), Wave.Sine, Env(0.75f, 0.002f, 3.4f, 1f));
            Mix(b, Noise(0.55f, Env(0.55f, 0.002f, 5f, 1f), Lin(0.3f, 0.02f, 0.55f)), 0, 1f);
            Add("slam", b, 1.4f);

            b = Buf(0.4f);
            Mix(b, Tone(0.18f, Sweep(1800f, 200f, 0.18f), Wave.Sine, Env(0.18f, 0.002f, 8f, 0.5f)), 0, 1f);
            Mix(b, Tone(0.2f, Sweep(200f, 1800f, 0.2f), Wave.Sine, Env(0.2f, 0.002f, 8f, 0.5f)), S(0.18f), 1f);
            Add("teleport", b);

            b = Noise(0.36f, Env(0.36f, 0.01f, 6f, 0.7f), Lin(0.04f, 0.35f, 0.36f));
            Mix(b, Tone(0.34f, Sweep(130f, 60f, 0.34f), Wave.Saw, Env(0.34f, 0.002f, 7f, 0.4f)), 0, 1f);
            Add("shock", b);

            b = Tone(0.35f, Sweep(300f, 1100f, 0.35f), Wave.Sine, Env(0.35f, 0.01f, 5f, 0.4f));
            Mix(b, Tone(0.35f, Sweep(600f, 2200f, 0.35f), Wave.Tri, Env(0.35f, 0.01f, 5f, 0.15f)), 0, 1f);
            Add("drain", b);

            b = Tone(1.3f, Sweep(55f, 520f, 1.3f), Wave.Saw, Env(1.3f, 0.02f, 1.2f, 0.45f));
            Mix(b, Noise(1.3f, Env(1.3f, 0.02f, 2f, 0.6f), Lin(0.03f, 0.6f, 1.3f)), 0, 1f);
            Add("phase", b, 1.4f);

            // kết thúc game
            b = Buf(2.4f);
            int[] vn = { 60, 64, 67, 72, 76, 79, 84 };
            for (int i = 0; i < vn.Length; i++)
                Mix(b, Tone(0.7f, K(Hz(vn[i])), Wave.Saw, Env(0.7f, 0.005f, 4f, 0.2f)), S(i * 0.1f), 1f);
            int[] vc = { 72, 76, 79, 84 };
            for (int i = 0; i < vc.Length; i++)
                Mix(b, Tone(1.7f, K(Hz(vc[i])), Wave.Tri, Env(1.7f, 0.02f, 1.6f, 0.22f)), S(0.7f), 1f);
            LP(b, 0.6f);
            Add("victory", b);

            b = Buf(2.2f);
            int[] dn = { 69, 65, 62, 57 };
            for (int i = 0; i < dn.Length; i++)
                Mix(b, Tone(0.9f, K(Hz(dn[i])), Wave.Saw, Env(0.9f, 0.01f, 3f, 0.25f)), S(i * 0.38f), 1f);
            Mix(b, Tone(1.6f, Sweep(80f, 30f, 1.6f), Wave.Sine, Env(1.6f, 0.01f, 1.8f, 0.6f)), S(0.9f), 1f);
            LP(b, 0.35f);
            Add("defeat", b);

            Add("click", Tone(0.06f, K(1200f), Wave.Sine, Env(0.06f, 0.001f, 30f, 0.4f)));
        }

        // ------------------------------------------------------------ music
        static AudioClip BuildMusic(string name, float bpm, int[] roots, int[][] chords, bool intense)
        {
            float beat = 60f / bpm;
            float stepLen = beat / 4f;
            const int bars = 4;   // vòng lặp 4 ô nhịp, mỗi ô một hợp âm (giữ thời gian tổng hợp ngắn)
            int steps = bars * 16;
            int n = S(beat * 4f * bars);

            var drums = new float[n];
            var bass = new float[n];
            var pad = new float[n];
            var lead = new float[n];

            // trống
            float[] kick = Tone(0.28f, t => 42f + 120f * Mathf.Exp(-t * 28f), Wave.Sine, Env(0.28f, 0.001f, 11f, 1f));
            float[] snare = Noise(0.22f, Env(0.22f, 0.001f, 17f, 0.8f), K(0.55f));
            Mix(snare, Tone(0.2f, K(185f), Wave.Tri, Env(0.2f, 0.001f, 22f, 0.5f)), 0, 1f);
            float[] hat = Noise(0.05f, Env(0.05f, 0.0005f, 70f, 0.4f), K(1f));
            float[] ohat = Noise(0.14f, Env(0.14f, 0.0005f, 24f, 0.35f), K(1f));
            for (int b = 0; b < bars * 4; b++)
            {
                float t0 = b * beat;
                Mix(drums, kick, S(t0), 0.95f, true);
                if (b % 2 == 1) Mix(drums, snare, S(t0), 0.8f, true);
                Mix(drums, ohat, S(t0 + beat * 0.5f), 0.6f, true);
                if (intense)
                {
                    Mix(drums, hat, S(t0), 0.45f, true);
                    Mix(drums, hat, S(t0 + beat * 0.25f), 0.35f, true);
                    Mix(drums, hat, S(t0 + beat * 0.75f), 0.35f, true);
                    if (b % 4 == 3) Mix(drums, kick, S(t0 + beat * 0.75f), 0.7f, true);
                }
                else Mix(drums, hat, S(t0), 0.4f, true);
            }

            // bass
            var cache = new Dictionary<int, float[]>();
            for (int s = 0; s < steps; s++)
            {
                int bar = s / 16;
                int ci = bar % roots.Length;
                int step = s % 16;
                if (!intense && step % 2 != 0) continue;
                int midi = roots[ci] + (step % 8 == 6 ? 12 : 0) + (intense && step % 4 == 3 ? 12 : 0);
                float[] note;
                if (!cache.TryGetValue(midi, out note))
                {
                    note = Tone(stepLen * 1.7f, K(Hz(midi)), Wave.Saw, Env(stepLen * 1.7f, 0.004f, 11f, 0.6f));
                    LP(note, 0.2f);
                    cache[midi] = note;
                }
                Mix(bass, note, S(s * stepLen), 1f, true);
            }

            // pad
            for (int c = 0; c < 4; c++)
            {
                float t0 = c * 4f * beat;
                int[] ch = chords[c % chords.Length];
                float dur = 4f * beat + 0.3f;
                Fn padEnv = t => Mathf.Min(1f, t / 0.5f) * Mathf.Clamp01((dur - t) / 0.35f) * 0.05f;
                for (int k = 0; k < ch.Length; k++)
                {
                    float f = Hz(ch[k]);
                    Mix(pad, Tone(dur, K(f * 0.997f), Wave.Saw, padEnv), S(t0), 1f, true);
                    Mix(pad, Tone(dur, K(f * 1.003f), Wave.Saw, padEnv), S(t0), 1f, true);
                }
            }
            LP(pad, 0.07f);

            // arpeggio dẫn
            var leadCache = new Dictionary<int, float[]>();
            int[] pat = { 0, 1, 2, 1, 2, 1, 0, 2 };
            for (int s = 0; s < steps; s++)
            {
                int bar = s / 16;
                int[] ch = chords[bar % chords.Length];
                if (!intense && s % 2 != 0) continue;
                int pi = intense ? s : s / 2;
                int midi = ch[pat[pi % pat.Length] % ch.Length] + 12 + ((s % 16 == 15 || s % 16 == 7) ? 12 : 0);
                float[] note;
                if (!leadCache.TryGetValue(midi, out note))
                {
                    note = Tone(stepLen * 2.2f, K(Hz(midi)), Wave.Saw, Env(stepLen * 2.2f, 0.003f, 15f, 0.35f));
                    LP(note, 0.45f);
                    leadCache[midi] = note;
                }
                Mix(lead, note, S(s * stepLen), 1f, true);
                Mix(lead, note, S(s * stepLen + stepLen * 3f), 0.3f, true);   // echo
            }

            // trộn + sidechain theo nhịp kick
            var outBuf = new float[n];
            float peak = 0.0001f;
            for (int i = 0; i < n; i++)
            {
                float tb = (i / (float)SR) % beat;
                float sc = 1f - 0.7f * Mathf.Exp(-tb * 10f);
                float v = drums[i] * 0.85f + (bass[i] * 0.9f + pad[i]) * sc + lead[i] * 0.75f;
                v = (float)System.Math.Tanh(v * 1.1f);
                outBuf[i] = v;
                float av = Mathf.Abs(v);
                if (av > peak) peak = av;
            }
            float norm = 0.85f / peak;
            for (int i = 0; i < n; i++) outBuf[i] *= norm;

            var clip = AudioClip.Create(name, n, 1, SR, false);
            clip.SetData(outBuf, 0);
            return clip;
        }
    }
}
