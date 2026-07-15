#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Bolin.Editor
{
    /// <summary>
    /// Generates the small, reusable raster UI kit used exclusively by MundoTamanos.
    /// This is an Editor-only authoring utility: no texture is created by gameplay code.
    /// Every output is imported as a single Sprite with a non-zero 9-slice border.
    /// </summary>
    public static class MundoTamanosGeneratedUiAssets
    {
        public const string AssetFolder = "Assets/MundoAprendo/UI/Generated/MundoTamanos";

        public const string TitleBannerPath = AssetFolder + "/TitleBannerPurple.png";
        public const string InstructionPanelPath = AssetFolder + "/InstructionPanelCream.png";
        public const string AnimalCardPath = AssetFolder + "/AnimalCardCream.png";
        public const string RoundedFramePath = AssetFolder + "/RoundedFrame.png";
        public const string HeaderTurquoisePath = AssetFolder + "/AnimalHeaderTurquoise.png";
        public const string HeaderPurplePath = AssetFolder + "/AnimalHeaderPurple.png";
        public const string AnswerGreenPath = AssetFolder + "/AnswerButtonGreen.png";
        public const string AnswerPurplePath = AssetFolder + "/AnswerButtonPurple.png";
        public const string BackCirclePath = AssetFolder + "/BackCircle.png";
        public const string StarPanelPath = AssetFolder + "/StarPanel.png";
        public const string SoftShadowPath = AssetFolder + "/SoftShadow.png";
        public const string BackArrowPath = AssetFolder + "/BackArrow.png";
        public const string HomeIconPath = AssetFolder + "/HomeIcon.png";

        private enum AssetKind
        {
            TitleBanner,
            InstructionPanel,
            AnimalCard,
            RoundedFrame,
            HeaderTurquoise,
            HeaderPurple,
            AnswerGreen,
            AnswerPurple,
            BackCircle,
            StarPanel,
            SoftShadow,
            BackArrow,
            HomeIcon
        }

        private readonly struct AssetDefinition
        {
            public readonly string Path;
            public readonly int Width;
            public readonly int Height;
            public readonly int Border;
            public readonly AssetKind Kind;

            public AssetDefinition(string path, int width, int height, int border, AssetKind kind)
            {
                Path = path;
                Width = width;
                Height = height;
                Border = border;
                Kind = kind;
            }
        }

        private static readonly AssetDefinition[] Definitions =
        {
            new(TitleBannerPath, 640, 216, 54, AssetKind.TitleBanner),
            new(InstructionPanelPath, 640, 188, 48, AssetKind.InstructionPanel),
            new(AnimalCardPath, 512, 512, 54, AssetKind.AnimalCard),
            new(RoundedFramePath, 512, 320, 44, AssetKind.RoundedFrame),
            new(HeaderTurquoisePath, 512, 152, 42, AssetKind.HeaderTurquoise),
            new(HeaderPurplePath, 512, 152, 42, AssetKind.HeaderPurple),
            new(AnswerGreenPath, 512, 172, 48, AssetKind.AnswerGreen),
            new(AnswerPurplePath, 512, 172, 48, AssetKind.AnswerPurple),
            new(BackCirclePath, 256, 256, 54, AssetKind.BackCircle),
            new(StarPanelPath, 512, 172, 46, AssetKind.StarPanel),
            new(SoftShadowPath, 512, 320, 48, AssetKind.SoftShadow),
            new(BackArrowPath, 256, 256, 38, AssetKind.BackArrow),
            new(HomeIconPath, 256, 256, 38, AssetKind.HomeIcon)
        };

        [MenuItem("Mundo Aprendo/Generar UI propia de Mundo de los Tamaños")]
        public static void GenerateFromMenu()
        {
            EnsureAssets();
            Debug.Log($"UI propia de MundoTamanos generada en {AssetFolder}.");
        }

        /// <summary>
        /// Writes the PNG source assets and applies consistent 2D/UI sprite import settings.
        /// It is safe to call from a scene setup script before it requests any of the sprites.
        /// </summary>
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
                ConfigureSprite(definition.Path, definition.Border);
            }

            AssetDatabase.SaveAssets();
        }

        private static void WriteAsset(AssetDefinition definition)
        {
            Texture2D texture = CreateTexture(definition.Width, definition.Height, definition.Kind);
            try
            {
                File.WriteAllBytes(ToAbsolutePath(definition.Path), texture.EncodeToPNG());
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
                AssetKind.TitleBanner => DrawTitleBanner(x, y, width, height),
                AssetKind.InstructionPanel => DrawInstructionPanel(x, y, width, height),
                AssetKind.AnimalCard => DrawAnimalCard(x, y, width, height),
                AssetKind.RoundedFrame => DrawRoundedFrame(x, y, width, height),
                AssetKind.HeaderTurquoise => DrawColoredHeader(x, y, width, height, new Color(0.08f, 0.70f, 0.68f), new Color(0.03f, 0.47f, 0.55f)),
                AssetKind.HeaderPurple => DrawColoredHeader(x, y, width, height, new Color(0.67f, 0.34f, 0.91f), new Color(0.39f, 0.16f, 0.67f)),
                AssetKind.AnswerGreen => DrawAnswerButton(x, y, width, height, new Color(0.55f, 0.86f, 0.22f), new Color(0.20f, 0.59f, 0.12f)),
                AssetKind.AnswerPurple => DrawAnswerButton(x, y, width, height, new Color(0.70f, 0.39f, 0.95f), new Color(0.40f, 0.15f, 0.70f)),
                AssetKind.BackCircle => DrawBackCircle(x, y, width, height),
                AssetKind.StarPanel => DrawStarPanel(x, y, width, height),
                AssetKind.SoftShadow => DrawSoftShadow(x, y, width, height),
                AssetKind.BackArrow => DrawBackArrow(x, y, width, height),
                AssetKind.HomeIcon => DrawHomeIcon(x, y, width, height),
                _ => Color.clear
            };
        }

        private static Color DrawTitleBanner(float x, float y, int width, int height)
        {
            Color transparent = Color.clear;
            float outer = RoundedRectangleDistance(x, y, width, height, 70f, 7f);
            if (outer > 1.2f) return transparent;

            Color edge = new Color(1f, 0.98f, 0.82f, 1f);
            Color darkEdge = new Color(0.27f, 0.10f, 0.49f, 1f);
            Color top = new Color(0.59f, 0.36f, 0.90f, 1f);
            Color bottom = new Color(0.30f, 0.13f, 0.61f, 1f);
            float inner = RoundedRectangleDistance(x, y + 5f, width, height, 60f, 19f);
            Color result = outer > -7f ? edge : darkEdge;
            if (inner <= 0f)
            {
                result = VerticalGradient(top, bottom, y, height);
                result = ApplyTopSheen(result, x, y, width, height, 60f, 23f, 0.18f);
            }

            // Edge confetti accents remain inside the 9-slice margins.
            float leftDot = CircleDistance(x, y, 70f, height * 0.66f, 11f);
            float rightDot = CircleDistance(x, y, width - 70f, height * 0.34f, 9f);
            if (leftDot < 0f) result = Color.Lerp(result, new Color(0.89f, 0.72f, 1f, 1f), 0.72f);
            if (rightDot < 0f) result = Color.Lerp(result, new Color(1f, 0.92f, 0.52f, 1f), 0.72f);
            return WithCoverage(result, outer);
        }

        private static Color DrawInstructionPanel(float x, float y, int width, int height)
        {
            float outer = RoundedRectangleDistance(x, y, width, height, 54f, 5f);
            if (outer > 1.2f) return Color.clear;

            Color outerStroke = new Color(1f, 0.99f, 0.93f, 1f);
            Color goldStroke = new Color(0.95f, 0.57f, 0.14f, 1f);
            Color fillTop = new Color(1f, 0.97f, 0.78f, 1f);
            Color fillBottom = new Color(1f, 0.88f, 0.58f, 1f);
            float gold = RoundedRectangleDistance(x, y + 2f, width, height, 48f, 12f);
            float inner = RoundedRectangleDistance(x, y + 4f, width, height, 43f, 21f);
            Color result = outer > -7f ? outerStroke : goldStroke;
            if (inner <= 0f)
            {
                result = VerticalGradient(fillTop, fillBottom, y, height);
                result = ApplyTopSheen(result, x, y, width, height, 43f, 27f, 0.23f);
            }

            return WithCoverage(result, outer);
        }

        private static Color DrawAnimalCard(float x, float y, int width, int height)
        {
            float outer = RoundedRectangleDistance(x, y, width, height, 66f, 7f);
            if (outer > 1.2f) return Color.clear;

            Color outerStroke = new Color(0.97f, 1f, 1f, 1f);
            Color warmLine = new Color(0.87f, 0.72f, 0.40f, 1f);
            Color fillTop = new Color(1f, 0.99f, 0.88f, 1f);
            Color fillBottom = new Color(1f, 0.91f, 0.64f, 1f);
            float line = RoundedRectangleDistance(x, y + 3f, width, height, 59f, 16f);
            float inner = RoundedRectangleDistance(x, y + 5f, width, height, 52f, 24f);
            Color result = outer > -8f ? outerStroke : warmLine;
            if (inner <= 0f)
            {
                result = VerticalGradient(fillTop, fillBottom, y, height);
                result = ApplyTopSheen(result, x, y, width, height, 52f, 29f, 0.16f);
                float vignette = Mathf.Clamp01((-inner - 3f) / 46f);
                result = Color.Lerp(new Color(0.92f, 0.82f, 0.53f, 1f), result, vignette);
            }

            return WithCoverage(result, outer);
        }

        private static Color DrawRoundedFrame(float x, float y, int width, int height)
        {
            float outer = RoundedRectangleDistance(x, y, width, height, 58f, 5f);
            if (outer > 1.2f) return Color.clear;

            float inner = RoundedRectangleDistance(x, y, width, height, 47f, 18f);
            if (inner <= 0f) return Color.clear;

            Color frame = Color.Lerp(new Color(1f, 1f, 1f, 0.95f), new Color(0.92f, 0.97f, 1f, 0.86f), y / height);
            return WithCoverage(frame, outer);
        }

        private static Color DrawColoredHeader(float x, float y, int width, int height, Color top, Color bottom)
        {
            float outer = RoundedRectangleDistance(x, y, width, height, 43f, 6f);
            if (outer > 1.2f) return Color.clear;

            Color whiteStroke = new Color(0.97f, 1f, 0.98f, 1f);
            Color darkStroke = Color.Lerp(bottom, new Color(0.10f, 0.12f, 0.28f, 1f), 0.48f);
            float colored = RoundedRectangleDistance(x, y + 3f, width, height, 37f, 15f);
            Color result = outer > -7f ? whiteStroke : darkStroke;
            if (colored <= 0f)
            {
                result = VerticalGradient(top, bottom, y, height);
                result = ApplyTopSheen(result, x, y, width, height, 37f, 19f, 0.27f);
            }

            return WithCoverage(result, outer);
        }

        private static Color DrawAnswerButton(float x, float y, int width, int height, Color top, Color bottom)
        {
            float outer = RoundedRectangleDistance(x, y, width, height, 61f, 7f);
            if (outer > 1.2f) return Color.clear;

            Color whiteStroke = new Color(1f, 1f, 0.94f, 1f);
            Color darkStroke = Color.Lerp(bottom, new Color(0.10f, 0.10f, 0.20f, 1f), 0.50f);
            float colored = RoundedRectangleDistance(x, y + 4f, width, height, 54f, 17f);
            Color result = outer > -7f ? whiteStroke : darkStroke;
            if (colored <= 0f)
            {
                result = VerticalGradient(top, bottom, y, height);
                result = ApplyTopSheen(result, x, y, width, height, 54f, 23f, 0.28f);

                // A quieter center keeps dynamically written answer text legible.
                float centerGlow = Mathf.Clamp01(1f - Mathf.Abs(x - width * 0.5f) / (width * 0.39f));
                result = Color.Lerp(result, new Color(1f, 1f, 1f, 1f), centerGlow * 0.055f);
            }

            return WithCoverage(result, outer);
        }

        private static Color DrawBackCircle(float x, float y, int width, int height)
        {
            float cx = width * 0.5f;
            float cy = height * 0.5f;
            float distance = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            float outerRadius = width * 0.47f;
            if (distance > outerRadius + 1.2f) return Color.clear;

            Color outerStroke = new Color(1f, 1f, 0.93f, 1f);
            Color amberStroke = new Color(0.90f, 0.49f, 0.08f, 1f);
            Color top = new Color(1f, 0.84f, 0.28f, 1f);
            Color bottom = new Color(0.97f, 0.50f, 0.09f, 1f);
            Color result = distance > outerRadius - 8f ? outerStroke : amberStroke;
            if (distance < outerRadius - 18f)
            {
                result = Color.Lerp(bottom, top, Mathf.Clamp01(y / height));
                float topHighlight = Mathf.Clamp01((y - height * 0.56f) / (height * 0.27f));
                result = Color.Lerp(result, Color.white, topHighlight * 0.22f);
            }

            return WithCoverage(result, distance - outerRadius);
        }

        private static Color DrawStarPanel(float x, float y, int width, int height)
        {
            float outer = RoundedRectangleDistance(x, y, width, height, 55f, 6f);
            if (outer > 1.2f) return Color.clear;

            Color whiteStroke = new Color(1f, 0.98f, 0.87f, 1f);
            Color brownStroke = new Color(0.43f, 0.21f, 0.08f, 1f);
            Color top = new Color(0.70f, 0.42f, 0.13f, 0.92f);
            Color bottom = new Color(0.38f, 0.17f, 0.08f, 0.94f);
            float inner = RoundedRectangleDistance(x, y + 2f, width, height, 48f, 16f);
            Color result = outer > -7f ? whiteStroke : brownStroke;
            if (inner <= 0f)
            {
                result = VerticalGradient(top, bottom, y, height);
                result = ApplyTopSheen(result, x, y, width, height, 48f, 20f, 0.19f);
            }

            return WithCoverage(result, outer);
        }

        private static Color DrawSoftShadow(float x, float y, int width, int height)
        {
            float d = RoundedRectangleDistance(x, y + height * 0.06f, width, height, 58f, 14f);
            float alpha = Mathf.Clamp01(1f - Mathf.Max(0f, d) / 42f) * 0.34f;
            alpha *= Mathf.Clamp01((-d + 58f) / 58f);
            return new Color(0.08f, 0.06f, 0.14f, alpha);
        }

        private static Color DrawBackArrow(float x, float y, int width, int height)
        {
            Vector2 p = new(x - width * 0.5f, y - height * 0.5f);
            float shaft = SignedBoxDistance(p - new Vector2(12f, 0f), new Vector2(53f, 18f));
            float head = SignedTriangleDistance(p, new Vector2(-56f, 0f), new Vector2(-4f, 58f), new Vector2(-4f, -58f));
            float distance = Mathf.Min(shaft, head);
            if (distance > 1.2f) return Color.clear;
            Color arrow = Color.Lerp(new Color(1f, 0.94f, 0.67f, 1f), Color.white, Mathf.Clamp01(y / height) * 0.45f);
            return WithCoverage(arrow, distance);
        }

        private static Color DrawHomeIcon(float x, float y, int width, int height)
        {
            Vector2 p = new(x - width * 0.5f, y - height * 0.5f);
            float baseDistance = SignedBoxDistance(p - new Vector2(0f, -22f), new Vector2(49f, 47f));
            float roofDistance = SignedTriangleDistance(p, new Vector2(-66f, -3f), new Vector2(0f, 62f), new Vector2(66f, -3f));
            float doorway = SignedBoxDistance(p - new Vector2(0f, -44f), new Vector2(12f, 25f));
            float house = Mathf.Min(baseDistance, roofDistance);
            if (house > 1.2f || doorway < 0f) return Color.clear;
            Color icon = Color.Lerp(new Color(1f, 0.95f, 0.69f, 1f), Color.white, Mathf.Clamp01(y / height) * 0.45f);
            return WithCoverage(icon, house);
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
            float coverage = Mathf.Clamp01(0.5f - signedDistance);
            color.a *= coverage;
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

        private static float SignedBoxDistance(Vector2 point, Vector2 halfSize)
        {
            Vector2 q = new(Mathf.Abs(point.x) - halfSize.x, Mathf.Abs(point.y) - halfSize.y);
            return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f);
        }

        // Negative inside, positive outside. The winding is intentionally normalized here.
        private static float SignedTriangleDistance(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            float d0 = DistanceToSegment(point, a, b);
            float d1 = DistanceToSegment(point, b, c);
            float d2 = DistanceToSegment(point, c, a);
            float distance = Mathf.Min(d0, Mathf.Min(d1, d2));
            bool inside = PointInTriangle(point, a, b, c);
            return inside ? -distance : distance;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float denominator = Vector2.Dot(ab, ab);
            float t = denominator <= 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(point - a, ab) / denominator);
            return (point - (a + ab * t)).magnitude;
        }

        private static bool PointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            float ab = Cross(b - a, point - a);
            float bc = Cross(c - b, point - b);
            float ca = Cross(a - c, point - c);
            return (ab >= 0f && bc >= 0f && ca >= 0f) || (ab <= 0f && bc <= 0f && ca <= 0f);
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
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
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.SaveAndReimport();
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                throw new InvalidOperationException("No se pudo determinar la raíz del proyecto Unity para generar la UI de MundoTamanos.");
            }

            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
#endif
