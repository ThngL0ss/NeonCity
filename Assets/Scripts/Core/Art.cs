using UnityEngine;

namespace NeonBreaker
{
    /// <summary>
    /// Sprite/material dùng chung cho hiệu ứng. Ưu tiên asset trong ArtBank (file PNG thật);
    /// nếu chưa có thì dùng bản dự phòng sinh bằng code. Mọi sprite có kích thước 1 unit để dùng localScale làm kích thước.
    /// </summary>
    public static class Art
    {
        public static Sprite Square, Circle, Glow, Ring, Arc, Diamond, Triangle, Star;
        public static Texture2D Pixel;
        public static Material DefMat, AddMat, FlashMat;
        public static ArtBank Bank;
        static bool inited;

        /// <summary>Xóa mọi tham chiếu tĩnh. Bắt buộc khi tắt "Reload Domain" trong Enter Play Mode Settings.</summary>
        public static void ResetStatics()
        {
            inited = false;
            Square = Circle = Glow = Ring = Arc = Diamond = Triangle = Star = null;
            Pixel = null;
            DefMat = AddMat = FlashMat = null;
            Bank = null;
        }

        public static void Init()
        {
            // kiểm tra thêm Square/Pixel: nếu Unity đã hủy texture sau lần Play trước thì tạo lại
            if (inited && Square != null && Pixel != null && DefMat != null) return;
            inited = true;

            Pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            Pixel.SetPixel(0, 0, Color.white);
            Pixel.Apply();
            Pixel.filterMode = FilterMode.Point;

            Square = FromFunc(4, 4f, FilterMode.Point, (x, y) => 1f);
            Circle = FromFunc(64, 64f, FilterMode.Bilinear, (x, y) =>
            {
                float d = Mathf.Sqrt(x * x + y * y);            // 0..1 radius
                return Mathf.Clamp01((1f - d) * 32f);
            });
            Glow = FromFunc(128, 128f, FilterMode.Bilinear, (x, y) =>
            {
                float d = Mathf.Sqrt(x * x + y * y);
                float a = Mathf.Clamp01(1f - d);
                return a * a * (0.55f + 0.45f * a);
            });
            Ring = FromFunc(128, 128f, FilterMode.Bilinear, (x, y) =>
            {
                float d = Mathf.Sqrt(x * x + y * y);
                float core = Mathf.Clamp01(1f - Mathf.Abs(d - 0.90f) / 0.055f);
                float halo = Mathf.Clamp01(1f - Mathf.Abs(d - 0.88f) / 0.18f) * 0.35f;
                return Mathf.Max(core, halo);
            });
            Diamond = FromFunc(64, 64f, FilterMode.Bilinear, (x, y) =>
            {
                float d = Mathf.Abs(x) + Mathf.Abs(y);
                return Mathf.Clamp01((1f - d) * 24f);
            });
            Triangle = FromFunc(64, 64f, FilterMode.Bilinear, (x, y) =>
            {
                // x,y in -1..1 ; tam giác hướng lên: đáy y=-1, đỉnh y=+1
                float half = (1f - y) * 0.5f;                    // nửa bề rộng tại độ cao y
                float a1 = Mathf.Clamp01((half - Mathf.Abs(x)) * 24f);
                float a2 = Mathf.Clamp01((y + 1f) * 24f);
                return Mathf.Min(a1, a2);
            });
            Star = FromFunc(64, 64f, FilterMode.Bilinear, (x, y) =>
            {
                float d = Mathf.Sqrt(x * x + y * y);
                float ang = Mathf.Atan2(y, x);
                float c = Mathf.Abs(Mathf.Cos(2f * ang));
                float edge = 0.26f + 0.74f * c * c * c;
                float a = Mathf.Clamp01((edge - d) * 22f);
                float hole = Mathf.Clamp01((d - 0.13f) * 30f);
                return a * hole;
            });
            Arc = MakeArc();

            DefMat = new Material(FindShader("Sprites/Default"));
            Shader add = FindShader("NeonBreaker/SpriteAdditive");   // Assets/Resources/NeonAdditive.shader
            if (add == null) add = FindShader("Legacy Shaders/Particles/Additive");
            if (add == null) add = FindShader("Particles/Additive");
            if (add == null) add = FindShader("Mobile/Particles/Additive");
            if (add != null)
            {
                AddMat = new Material(add);
                // Shader legacy cần _TintColor; shader riêng của game thì không cần.
                if (AddMat.HasProperty("_TintColor")) AddMat.SetColor("_TintColor", new Color(1f, 1f, 1f, 1f));
            }
            else
            {
                AddMat = DefMat;   // không có shader additive -> vẫn chạy, chỉ bớt "phát sáng"
            }

            ApplyBank();
        }

        /// <summary>Sprite/material do bạn gán trong ArtBank (Resources/NeonBreaker/ArtBank.asset) thay cho bản dự phòng sinh bằng code.</summary>
        static void ApplyBank()
        {
            Bank = ArtBank.Load();
            if (Bank == null) return;
            if (Bank.square != null) Square = Bank.square;
            if (Bank.circle != null) Circle = Bank.circle;
            if (Bank.glow != null) Glow = Bank.glow;
            if (Bank.ring != null) Ring = Bank.ring;
            if (Bank.arc != null) Arc = Bank.arc;
            if (Bank.diamond != null) Diamond = Bank.diamond;
            if (Bank.triangle != null) Triangle = Bank.triangle;
            if (Bank.star != null) Star = Bank.star;
            if (Bank.spriteMaterial != null) DefMat = Bank.spriteMaterial;
            if (Bank.additiveMaterial != null) AddMat = Bank.additiveMaterial;
            FlashMat = Bank.flashMaterial;
        }

        static Shader FindShader(string n)
        {
            Shader s = Shader.Find(n);
            return s;
        }

        delegate float PixelFn(float x, float y);

        /// <summary>Tạo sprite vuông size x size, hàm trả về alpha với tọa độ -1..1.</summary>
        static Sprite FromFunc(int size, float ppu, FilterMode fm, PixelFn fn)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = fm;
            var px = new Color[size * size];
            for (int j = 0; j < size; j++)
            {
                for (int i = 0; i < size; i++)
                {
                    float x = ((i + 0.5f) / size) * 2f - 1f;
                    float y = ((j + 0.5f) / size) * 2f - 1f;
                    float a = Mathf.Clamp01(fn(x, y));
                    px[j * size + i] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu);
        }

        /// <summary>Vệt chém hình lưỡi liềm, mặt lồi quay sang phải.</summary>
        static Sprite MakeArc()
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[size * size];
            for (int j = 0; j < size; j++)
            {
                for (int i = 0; i < size; i++)
                {
                    float x = i + 0.5f - 64f;
                    float y = j + 0.5f - 64f;
                    float dOuter = Mathf.Sqrt(x * x + y * y);
                    float aOuter = Mathf.Clamp01((62f - dOuter) / 3f);
                    float ix = x + 30f;
                    float dInner = Mathf.Sqrt(ix * ix + y * y);
                    float aInner = Mathf.Clamp01((dInner - 56f) / 3f);
                    float a = Mathf.Min(aOuter, aInner);
                    // sáng hơn ở mép ngoài
                    float edge = Mathf.Clamp01((dOuter - 30f) / 32f);
                    px[j * size + i] = new Color(1f, 1f, 1f, a * (0.35f + 0.65f * edge));
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }

        /// <summary>Tạo một SpriteRenderer con có kích thước/ màu/ thứ tự vẽ.</summary>
        public static SpriteRenderer Spr(Transform parent, string name, Sprite s, Vector2 localPos, Vector2 size,
                                         Color c, int order, bool additive = false, float rotZ = 0f)
        {
            Init();
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.color = c;
            sr.sortingOrder = order;
            sr.sharedMaterial = additive ? AddMat : DefMat;
            return sr;
        }
    }

    /// <summary>Thanh máu nhỏ trong thế giới (trên đầu quái / trụ).</summary>
    public class WorldBar
    {
        readonly SpriteRenderer back, fill, glow;
        readonly Transform root;
        readonly float width;
        readonly Color col;

        public WorldBar(Transform parent, Vector2 localPos, float width, float height, Color col, int order = 60)
        {
            this.width = width;
            this.col = col;
            var go = new GameObject("WorldBar");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            root = go.transform;
            back = Art.Spr(root, "bg", Art.Square, Vector2.zero, new Vector2(width + 0.08f, height + 0.08f), new Color(0f, 0f, 0f, 0.75f), order);
            fill = Art.Spr(root, "fill", Art.Square, Vector2.zero, new Vector2(width, height), col, order + 1);
            glow = Art.Spr(root, "glow", Art.Glow, Vector2.zero, new Vector2(width * 1.2f, height * 4f), Cfg.A(col, 0.35f), order - 1, true);
        }

        public void Set(float t)
        {
            t = Mathf.Clamp01(t);
            fill.transform.localScale = new Vector3(Mathf.Max(0.0001f, width * t), fill.transform.localScale.y, 1f);
            fill.transform.localPosition = new Vector3(-(width * (1f - t)) * 0.5f, 0f, 0f);
        }

        public void SetVisible(bool v)
        {
            root.gameObject.SetActive(v);
        }
    }
}
