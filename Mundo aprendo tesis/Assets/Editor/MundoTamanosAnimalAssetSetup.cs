#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bolin;
using UnityEditor;
using UnityEngine;

namespace Bolin.Editor
{
    /// <summary>
    /// Prepara los catorce animales definitivos desde el Editor: conserva GUID al
    /// renombrar, elimina únicamente el blanco conectado al borde y los importa
    /// como sprites reutilizables. No participa durante la ejecución del juego.
    /// </summary>
    public static class MundoTamanosAnimalAssetSetup
    {
        public const string AnimalDirectory = "Assets/Mundo Aprendo/Animales";

        private sealed class AnimalAssetDefinition
        {
            public AnimalAssetDefinition(string sourceFileName, string finalFileName, string displayName)
            {
                this.sourceFileName = sourceFileName;
                this.finalFileName = finalFileName;
                this.displayName = displayName;
            }

            public readonly string sourceFileName;
            public readonly string finalFileName;
            public readonly string displayName;
            public string FinalPath => AnimalDirectory + "/" + finalFileName;
        }

        private sealed class AnimalAssignment
        {
            public AnimalAssignment(string finalFileName, string displayName, bool isLarge, float minScale, float maxScale)
            {
                this.finalFileName = finalFileName;
                this.displayName = displayName;
                this.isLarge = isLarge;
                this.minScale = minScale;
                this.maxScale = maxScale;
            }

            public readonly string finalFileName;
            public readonly string displayName;
            public readonly bool isLarge;
            public readonly float minScale;
            public readonly float maxScale;
        }

        private static readonly AnimalAssetDefinition[] Definitions =
        {
            new("ChatGPT Image Jul 13, 2026, 11_58_49 PM (1).png", "Animal_Leon.png", "León"),
            new("ChatGPT Image Jul 13, 2026, 11_58_49 PM (2).png", "Animal_Ballena.png", "Ballena"),
            new("ChatGPT Image Jul 13, 2026, 11_58_49 PM (3).png", "Animal_Panda.png", "Panda"),
            new("ChatGPT Image Jul 13, 2026, 11_58_49 PM (4).png", "Animal_Mono.png", "Mono"),
            new("ChatGPT Image Jul 13, 2026, 11_58_49 PM (5).png", "Animal_Cocodrilo.png", "Cocodrilo"),
            new("ChatGPT Image Jul 13, 2026, 11_58_49 PM (6).png", "Animal_Koala.png", "Koala"),
            new("ChatGPT Image Jul 13, 2026, 11_58_49 PM (7).png", "Animal_Pinguino.png", "Pingüino"),
            new("ChatGPT Image Jul 13, 2026, 11_58_49 PM (8).png", "Animal_Delfin.png", "Delfín"),
            new("ChatGPT Image Jul 13, 2026, 11_59_26 PM (1).png", "Animal_Elefante.png", "Elefante"),
            new("ChatGPT Image Jul 13, 2026, 11_59_27 PM (2).png", "Animal_Cebra.png", "Cebra"),
            new("ChatGPT Image Jul 13, 2026, 11_59_27 PM (3).png", "Animal_Jirafa.png", "Jirafa"),
            new("ChatGPT Image Jul 13, 2026, 11_59_27 PM (4).png", "Animal_Conejo.png", "Conejo"),
            new("ChatGPT Image Jul 13, 2026, 11_59_28 PM (5).png", "Animal_Raton.png", "Ratón"),
            new("ChatGPT Image Jul 13, 2026, 11_59_28 PM (6).png", "Animal_Pez.png", "Pez")
        };

        // Se mantienen los tres habitats y el mismo criterio Grande/Pequeño.
        // Cada sprite definitivo se usa una sola vez, sin placeholders antiguos.
        private static readonly AnimalAssignment[][] HabitatAssignments =
        {
            new[]
            {
                new AnimalAssignment("Animal_Elefante.png", "Elefante", true, 0.78f, 1.05f),
                new AnimalAssignment("Animal_Leon.png", "León", true, 0.74f, 1.00f),
                new AnimalAssignment("Animal_Jirafa.png", "Jirafa", true, 0.76f, 1.03f),
                new AnimalAssignment("Animal_Cebra.png", "Cebra", true, 0.70f, 0.96f),
                new AnimalAssignment("Animal_Mono.png", "Mono", false, 0.61f, 0.81f),
                new AnimalAssignment("Animal_Conejo.png", "Conejo", false, 0.58f, 0.78f)
            },
            new[]
            {
                new AnimalAssignment("Animal_Ballena.png", "Ballena", true, 0.78f, 1.05f),
                new AnimalAssignment("Animal_Delfin.png", "Delfín", true, 0.70f, 0.96f),
                new AnimalAssignment("Animal_Pez.png", "Pez", false, 0.58f, 0.78f),
                new AnimalAssignment("Animal_Pinguino.png", "Pingüino", false, 0.60f, 0.80f)
            },
            new[]
            {
                new AnimalAssignment("Animal_Panda.png", "Panda", true, 0.72f, 0.98f),
                new AnimalAssignment("Animal_Cocodrilo.png", "Cocodrilo", true, 0.74f, 1.00f),
                new AnimalAssignment("Animal_Koala.png", "Koala", true, 0.68f, 0.92f),
                new AnimalAssignment("Animal_Raton.png", "Ratón", false, 0.55f, 0.74f)
            }
        };

        [MenuItem("Mundo Aprendo/Mundo Tamaños/Finalizar animales e interfaz")]
        public static void FinalizeAnimalsAndInterface()
        {
            MundoTamanosSafariReconstructionSetup.Apply();
        }

        public static IReadOnlyList<Sprite> PrepareFinalAnimalAssets()
        {
            AssetDatabase.Refresh();
            List<Sprite> sprites = new(Definitions.Length);

            foreach (AnimalAssetDefinition definition in Definitions)
            {
                string path = MoveToFinalPath(definition);
                RemoveConnectedWhiteBackgroundIfNeeded(path);
                ConfigureSpriteImporter(path);

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    throw new InvalidOperationException($"No se pudo importar como Sprite: {path}");
                }

                sprites.Add(sprite);
            }

            if (sprites.Distinct().Count() != Definitions.Length)
            {
                throw new InvalidOperationException("Los catorce animales definitivos deben conservar sprites distintos.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return sprites;
        }

        public static void ConfigureAnimalData(SizeWorldController controller)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));

            SerializedObject serialized = new(controller);
            SerializedProperty habitats = serialized.FindProperty("habitats");
            if (habitats == null || habitats.arraySize != HabitatAssignments.Length)
            {
                throw new InvalidOperationException("MundoTamanos debe conservar sus tres habitats para asignar los animales definitivos.");
            }

            HashSet<string> usedPaths = new(StringComparer.OrdinalIgnoreCase);
            for (int habitatIndex = 0; habitatIndex < HabitatAssignments.Length; habitatIndex++)
            {
                SerializedProperty animals = habitats.GetArrayElementAtIndex(habitatIndex).FindPropertyRelative("animals");
                AnimalAssignment[] assignments = HabitatAssignments[habitatIndex];
                animals.arraySize = assignments.Length;

                for (int animalIndex = 0; animalIndex < assignments.Length; animalIndex++)
                {
                    AnimalAssignment assignment = assignments[animalIndex];
                    string assetPath = AnimalDirectory + "/" + assignment.finalFileName;
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    if (sprite == null) throw new InvalidOperationException($"Falta el sprite definitivo: {assetPath}");

                    SerializedProperty animal = animals.GetArrayElementAtIndex(animalIndex);
                    animal.FindPropertyRelative("animalName").stringValue = assignment.displayName;
                    animal.FindPropertyRelative("animalSprite").objectReferenceValue = sprite;
                    animal.FindPropertyRelative("sizeType").enumValueIndex = assignment.isLarge ? 1 : 0;
                    animal.FindPropertyRelative("minScale").floatValue = assignment.minScale;
                    animal.FindPropertyRelative("maxScale").floatValue = assignment.maxScale;
                    usedPaths.Add(assetPath);
                }
            }

            if (usedPaths.Count != Definitions.Length)
            {
                throw new InvalidOperationException("La configuracion de habitats debe usar exactamente los catorce sprites definitivos.");
            }

            SerializedProperty correctDelay = serialized.FindProperty("correctAnswerDelay");
            if (correctDelay != null) correctDelay.floatValue = 3f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string MoveToFinalPath(AnimalAssetDefinition definition)
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(definition.FinalPath) != null)
            {
                return definition.FinalPath;
            }

            string sourcePath = AnimalDirectory + "/" + definition.sourceFileName;
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath) == null)
            {
                throw new InvalidOperationException($"No se encontro el archivo original del animal: {sourcePath}");
            }

            string error = AssetDatabase.MoveAsset(sourcePath, definition.FinalPath);
            if (!string.IsNullOrEmpty(error))
            {
                throw new InvalidOperationException($"No se pudo renombrar '{sourcePath}': {error}");
            }

            return definition.FinalPath;
        }

        private static void RemoveConnectedWhiteBackgroundIfNeeded(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException($"No se encontro TextureImporter: {assetPath}");

            bool readableBefore = importer.isReadable;
            if (!readableBefore)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (source == null) throw new InvalidOperationException($"No se pudo leer la textura: {assetPath}");

            Color32[] pixels = source.GetPixels32();
            if (!NeedsBackgroundRemoval(source.width, source.height, pixels))
            {
                if (!readableBefore)
                {
                    importer.isReadable = false;
                    importer.SaveAndReimport();
                }

                return;
            }

            BackupOriginalPng(assetPath);
            bool[] background = FloodFillBorderWhite(source.width, source.height, pixels);
            for (int index = 0; index < pixels.Length; index++)
            {
                if (background[index]) pixels[index].a = 0;
            }

            Texture2D transparent = new(source.width, source.height, TextureFormat.RGBA32, false, false);
            transparent.SetPixels32(pixels);
            transparent.Apply(false, false);
            File.WriteAllBytes(ToAbsolutePath(assetPath), transparent.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(transparent);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        private static void ConfigureSpriteImporter(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException($"No se encontro TextureImporter: {assetPath}");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            TextureImporterSettings settings = new();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        private static bool NeedsBackgroundRemoval(int width, int height, Color32[] pixels)
        {
            int[] corners = { 0, width - 1, (height - 1) * width, height * width - 1 };
            return corners.All(index => pixels[index].a > 245);
        }

        private static bool[] FloodFillBorderWhite(int width, int height, Color32[] pixels)
        {
            bool[] visited = new bool[pixels.Length];
            Queue<int> pending = new();

            void TryAdd(int index)
            {
                if (visited[index] || !LooksLikeWhiteBackground(pixels[index])) return;
                visited[index] = true;
                pending.Enqueue(index);
            }

            for (int x = 0; x < width; x++)
            {
                TryAdd(x);
                TryAdd((height - 1) * width + x);
            }

            for (int y = 1; y < height - 1; y++)
            {
                TryAdd(y * width);
                TryAdd(y * width + width - 1);
            }

            while (pending.Count > 0)
            {
                int index = pending.Dequeue();
                int x = index % width;
                int y = index / width;
                if (x > 0) TryAdd(index - 1);
                if (x < width - 1) TryAdd(index + 1);
                if (y > 0) TryAdd(index - width);
                if (y < height - 1) TryAdd(index + width);
            }

            return visited;
        }

        private static bool LooksLikeWhiteBackground(Color32 color)
        {
            int maximum = Math.Max(color.r, Math.Max(color.g, color.b));
            int minimum = Math.Min(color.r, Math.Min(color.g, color.b));
            return minimum >= 228 && maximum - minimum <= 18;
        }

        private static void BackupOriginalPng(string assetPath)
        {
            string source = ToAbsolutePath(assetPath);
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string directory = Path.Combine(projectRoot, "Backups", "MundoTamanosAnimalesOriginales");
            Directory.CreateDirectory(directory);
            string destination = Path.Combine(directory, Path.GetFileName(source));
            if (!File.Exists(destination)) File.Copy(source, destination, false);
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
#endif
