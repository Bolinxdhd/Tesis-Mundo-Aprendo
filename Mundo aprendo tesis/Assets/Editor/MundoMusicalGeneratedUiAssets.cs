#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Bolin.Editor
{
    /// <summary>
    /// Authoring-only sprite kit for the persisted Mundo Musical hierarchy.
    /// Nothing here runs in a player build or creates UI at runtime.
    /// </summary>
    public static class MundoMusicalGeneratedUiAssets
    {
        public const string AssetFolder = "Assets/MundoAprendo/UI/Generated/MundoMusical";
        public const string MusicKeyCreamPath = AssetFolder + "/MusicKeyCream.png";
        public const string MusicKeyGlowPath = AssetFolder + "/MusicKeyGlow.png";
        public const string MusicSequenceBubblePath = AssetFolder + "/MusicSequenceBubble.png";
        public const string MusicPianoFramePath = AssetFolder + "/MusicPianoFrame.png";
        public const string MusicListenGlowPath = AssetFolder + "/MusicListenGlow.png";
        public const string MusicListenButtonOutlinePath = AssetFolder + "/MusicListenButtonOutline.png";
        public const string MusicNoteGoldenPath = AssetFolder + "/MusicNoteGolden.png";

        private enum AssetKind
        {
            PianoKey,
            KeyGlow,
            SequenceBubble,
            PianoFrame,
            ListenGlow
        }

        private readonly struct AssetDefinition
        {
            public readonly string path;
            public readonly int width;
            public readonly int height;
            public readonly int border;
            public readonly AssetKind kind;

            public AssetDefinition(string path, int width, int height, int border, AssetKind kind)
            {
                this.path = path;
                this.width = width;
                this.height = height;
                this.border = border;
                this.kind = kind;
            }
        }

        private static readonly AssetDefinition[] Definitions =
        {
            new(MusicKeyCreamPath, 384, 768, 54, AssetKind.PianoKey),
            new(MusicKeyGlowPath, 384, 768, 54, AssetKind.KeyGlow),
            new(MusicSequenceBubblePath, 384, 384, 58, AssetKind.SequenceBubble),
            new(MusicPianoFramePath, 1024, 560, 58, AssetKind.PianoFrame),
            new(MusicListenGlowPath, 640, 220, 64, AssetKind.ListenGlow)
        };

        [MenuItem("Mundo Aprendo/Mundo Musical/Generar kit visual propio")]
        public static void GenerateFromMenu()
        {
            EnsureAssets();
            Debug.Log($"Kit visual de Mundo Musical generado en {AssetFolder}.");
        }

        public static void EnsureAssets()
        {
            Directory.CreateDirectory(ToAbsolutePath(AssetFolder));

            foreach (AssetDefinition definition in Definitions)
            {
                WriteAsset(definition);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            foreach (AssetDefinition definition in Definitions)
            {
                ConfigureSprite(definition.path, definition.border);
            }

            // The only generative artwork is copied into this same project folder before
            // this method is called.  It receives the exact same UI import policy.
            ConfigureSprite(MusicNoteGoldenPath, 0);
            AssetDatabase.SaveAssets();
        }

        private static void WriteAsset(AssetDefinition definition)
        {
            Texture2D texture = CreateTexture(definition.width, definition.height, definition.kind);
            try
            {
                File.WriteAllBytes(ToAbsolutePath(definition.path), texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static Texture2D CreateTexture(int width, int height, AssetKind kind)
        {
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
            {
                name = $"Generated_{kind}",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = Sample(kind, x + 0.5f, y + 0.5f, width, height);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static Color Sample(AssetKind kind, float x, float y, int width, int height)
        {
            return kind switch
            {
                AssetKind.PianoKey => DrawPianoKey(x, y, width, height),
                AssetKind.KeyGlow => DrawKeyGlow(x, y, width, height),
                AssetKind.SequenceBubble => DrawSequenceBubble(x, y, width, height),
                AssetKind.PianoFrame => DrawPianoFrame(x, y, width, height),
                AssetKind.ListenGlow => DrawListenGlow(x, y, width, height),
                _ => Color.clear
            };
        }

        private static Color DrawPianoKey(float x, float y, int width, int height)
        {
            float outer = RoundedRectangleDistance(x, y, width, height, 64f, 7f);
            if (outer > 1.2f) return Color.clear;

            Color whiteStroke = new(1f, 0.99f, 0.94f, 1f);
            Color purpleStroke = new(0.46f, 0.27f, 0.78f, 1f);
            Color top = new(1f, 0.99f, 0.91f, 1f);
            Color bottom = new(0.94f, 0.87f, 1f, 1f);
            float inner = RoundedRectangleDistance(x, y + 4f, width, height, 54f, 18f);

            Color result = outer > -7f ? whiteStroke : purpleStroke;
            if (inner <= 0f)
            {
                result = VerticalGradient(top, bottom, y, height);
                result = ApplyTopSheen(result, x, y, width, height, 54f, 22f, 0.24f);
                float center = Mathf.Clamp01(1f - Mathf.Abs(x - width * 0.5f) / (width * 0.55f));
                result = Color.Lerp(result, Color.white, center * 0.08f);
            }

            return WithCoverage(result, outer);
        }

        private static Color DrawKeyGlow(float x, float y, int width, int height)
        {
            float outer = RoundedRectangleDistance(x, y, width, height, 64f, 3f);
            if (outer > 1.2f) return Color.clear;

            float inner = RoundedRectangleDistance(x, y, width, height, 54f, 17f);
            float center = Mathf.Clamp01((-inner + 48f) / 48f);
            Color glow = Color.Lerp(new Color(1f, 0.73f, 0.18f, 0.16f), new Color(1f, 0.96f, 0.52f, 0.9f), Mathf.Clamp01(y / height));
            glow.a *= center;
            return WithCoverage(glow, outer);
        }

        private static Color DrawSequenceBubble(float x, float y, int width, int height)
        {
            float radius = width * 0.455f;
            float distance = CircleDistance(x, y, width * 0.5f, height * 0.5f, radius);
            if (distance > 1.2f) return Color.clear;

            Color whiteStroke = new(1f, 1f, 0.95f, 1f);
            Color lavenderStroke = new(0.57f, 0.35f, 0.88f, 1f);
            Color top = new(1f, 0.98f, 0.86f, 1f);
            Color bottom = new(0.92f, 0.83f, 1f, 1f);
            Color result = distance > -9f ? whiteStroke : lavenderStroke;
            if (distance < -20f)
            {
                float vertical = Mathf.Clamp01(y / height);
                result = Color.Lerp(bottom, top, vertical);
                float highlight = Mathf.Clamp01((y - height * 0.58f) / (height * 0.23f));
                result = Color.Lerp(result, Color.white, highlight * 0.35f);
            }

            return WithCoverage(result, distance);
        }

        private static Color DrawPianoFrame(float x, float y, int width, int height)
        {
            float outer = RoundedRectangleDistance(x, y, width, height, 70f, 7f);
            if (outer > 1.2f) return Color.clear;

            Color pearlStroke = new(1f, 0.99f, 0.91f, 1f);
            Color violetStroke = new(0.28f, 0.16f, 0.60f, 1f);
            Color top = new(0.49f, 0.32f, 0.83f, 1f);
            Color bottom = new(0.20f, 0.17f, 0.51f, 1f);
            float inner = RoundedRectangleDistance(x, y + 4f, width, height, 60f, 20f);

            Color result = outer > -8f ? pearlStroke : violetStroke;
            if (inner <= 0f)
            {
                result = VerticalGradient(top, bottom, y, height);
                result = ApplyTopSheen(result, x, y, width, height, 60f, 24f, 0.20f);
                float inset = RoundedRectangleDistance(x, y, width, height, 48f, 48f);
                if (inset <= 0f)
                {
                    result = Color.Lerp(result, new Color(0.14f, 0.12f, 0.35f, 1f), 0.27f);
                }
            }

            return WithCoverage(result, outer);
        }

        private static Color DrawListenGlow(float x, float y, int width, int height)
        {
            float distance = RoundedRectangleDistance(x, y, width, height, 68f, 28f);
            if (distance > 34f) return Color.clear;

            float outsideGlow = 1f - Mathf.Clamp01(Mathf.Max(0f, distance) / 34f);
            float insideGlow = Mathf.Clamp01((-distance + 24f) / 24f);
            float center = Mathf.Clamp01(1f - Mathf.Abs(x - width * 0.5f) / (width * 0.55f));
            Color color = Color.Lerp(new Color(1f, 0.62f, 0.05f, 0.78f), new Color(1f, 0.98f, 0.48f, 1f), center * 0.72f);
            color.a *= Mathf.Max(outsideGlow * 0.72f, insideGlow * 0.30f);
            return color;
        }

        private static Color VerticalGradient(Color top, Color bottom, float y, int height)
        {
            return Color.Lerp(bottom, top, Mathf.Clamp01(y / height));
        }

        private static Color ApplyTopSheen(Color source, float x, float y, int width, int height, float radius, float inset, float strength)
        {
            float insetDistance = RoundedRectangleDistance(x, y + height * 0.07f, width, height, Mathf.Max(1f, radius - inset * 0.35f), inset);
            if (insetDistance > 0f || y < height * 0.55f) return source;
            float sheen = Mathf.Clamp01((y - height * 0.55f) / (height * 0.35f));
            return Color.Lerp(source, Color.white, sheen * strength);
        }

        private static Color WithCoverage(Color color, float signedDistance)
        {
            color.a *= Mathf.Clamp01(0.5f - signedDistance);
            return color;
        }

        private static float RoundedRectangleDistance(float x, float y, int width, int height, float radius, float inset)
        {
            float halfWidth = Mathf.Max(1f, width * 0.5f - inset);
            float halfHeight = Mathf.Max(1f, height * 0.5f - inset);
            Vector2 point = new(x - width * 0.5f, y - height * 0.5f);
            Vector2 q = new(Mathf.Abs(point.x) - halfWidth + radius, Mathf.Abs(point.y) - halfHeight + radius);
            Vector2 outside = new(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
            return Mathf.Min(Mathf.Max(q.x, q.y), 0f) + outside.magnitude - radius;
        }

        private static float CircleDistance(float x, float y, float cx, float cy, float radius)
        {
            return Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - radius;
        }

        private static void ConfigureSprite(string assetPath, int border)
        {
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = border > 0 ? new Vector4(border, border, border, border) : Vector4.zero;
            importer.SaveAndReimport();
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                throw new InvalidOperationException("No se pudo determinar la raíz del proyecto Unity para generar la UI de Mundo Musical.");
            }

            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
#endif
