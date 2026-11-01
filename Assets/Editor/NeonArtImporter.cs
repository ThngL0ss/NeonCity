#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace NeonBreaker.EditorTools
{
    /// <summary>
    /// Tự cài đặt import cho mọi PNG trong Assets/NeonBreaker/Art theo file manifest.json
    /// (Sprite Single, Pixels Per Unit, pivot, Point filter, không nén => pixel art sắc nét).
    /// Muốn đổi cài đặt cho riêng 1 ảnh: sửa manifest.json (hoặc xoá tên ảnh khỏi manifest rồi tự chỉnh trong Inspector).
    /// </summary>
    public class NeonArtImporter : AssetPostprocessor
    {

        class Info
        {
            public float ppu = 16f;
            public Vector2 pivot = new Vector2(0.5f, 0.5f);
            public bool tile;
        }

        static Dictionary<string, Info> cache;
        static System.DateTime cacheTime;
        static string cachePath;

        static Dictionary<string, Info> Manifest(string ManifestPath)
        {
            if (!File.Exists(ManifestPath)) return new Dictionary<string, Info>();
            var t = File.GetLastWriteTimeUtc(ManifestPath);
            if (cache != null && t == cacheTime && cachePath == ManifestPath) return cache;
            cacheTime = t;
            cachePath = ManifestPath;
            cache = new Dictionary<string, Info>();
            string text = File.ReadAllText(ManifestPath);
            // mỗi mục:  "name.png": { "h": 32, "pivot": [0.5, 0.0], "ppu": 16, "tile": false, "w": 32 }
            var entry = new Regex("\"([^\"]+\\.png)\"\\s*:\\s*\\{([^}]*)\\}", RegexOptions.Singleline);
            foreach (Match m in entry.Matches(text))
            {
                var info = new Info();
                string body = m.Groups[2].Value;
                var mp = Regex.Match(body, "\"ppu\"\\s*:\\s*([0-9.]+)");
                if (mp.Success) info.ppu = float.Parse(mp.Groups[1].Value, CultureInfo.InvariantCulture);
                var mv = Regex.Match(body, "\"pivot\"\\s*:\\s*\\[\\s*([0-9.\\-]+)\\s*,\\s*([0-9.\\-]+)\\s*\\]");
                if (mv.Success)
                    info.pivot = new Vector2(float.Parse(mv.Groups[1].Value, CultureInfo.InvariantCulture),
                                             float.Parse(mv.Groups[2].Value, CultureInfo.InvariantCulture));
                info.tile = Regex.IsMatch(body, "\"tile\"\\s*:\\s*true");
                cache[m.Groups[1].Value] = info;
            }
            return cache;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.EndsWith(".png")) return;
            string dir = assetPath.Substring(0, assetPath.LastIndexOf('/') + 1);
            if (!dir.EndsWith("/NeonBreaker/Art/")) return;       // chỉ xử lý thư mục Art của gói, đặt ở đâu trong Assets cũng được
            Info info;
            if (!Manifest(dir + "manifest.json").TryGetValue(Path.GetFileName(assetPath), out info)) return;

            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = info.ppu;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.wrapMode = info.tile ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.Custom;
            s.spritePivot = info.pivot;
            s.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(s);
        }
    }
}
#endif
