using System;
using System.Linq;
using Assets.SurpriseBox.Scripts;
using Bolin;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bolin.Editor
{
    /// <summary>
    /// Installs the authored transparent outline asset behind Escuchar. It only
    /// changes MundoMusical and reuses MusicalListenButtonGuide's existing pulse.
    /// </summary>
    public static class MundoMusicalListenOutlineAssetSetup
    {
        private const string ScenePath = "Assets/Scenes/MundoMusical.unity";
        private const string OutlineAssetPath = MundoMusicalGeneratedUiAssets.MusicListenButtonOutlinePath;

        [MenuItem("Mundo Aprendo/Mundo Musical/Aplicar contorno transparente a Escuchar")]
        public static void Apply()
        {
            Sprite outlineSprite = ImportOutlineSprite();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Button listenButton = ComponentsInScene<Button>(scene).Single(item => item.name == "BotonEscuchar");
            RectTransform outlineRoot = ComponentsInScene<RectTransform>(scene).Single(item => item.name == "EscucharTutorialGlow");
            Image outlineImage = outlineRoot.GetComponent<Image>();
            MusicalListenButtonGuide guide = listenButton.GetComponent<MusicalListenButtonGuide>();
            if (outlineImage == null || guide == null)
            {
                throw new InvalidOperationException("Escuchar debe conservar su imagen de contorno y MusicalListenButtonGuide.");
            }

            RectTransform listenRect = listenButton.transform as RectTransform;
            if (listenRect == null) throw new InvalidOperationException("BotonEscuchar requiere RectTransform.");

            outlineImage.sprite = outlineSprite;
            outlineImage.type = Image.Type.Sliced;
            outlineImage.preserveAspect = false;
            outlineImage.color = Color.white;
            outlineImage.raycastTarget = false;
            outlineRoot.anchorMin = listenRect.anchorMin;
            outlineRoot.anchorMax = listenRect.anchorMax;
            outlineRoot.pivot = new Vector2(0.5f, 0.5f);
            outlineRoot.anchoredPosition = listenRect.anchoredPosition;
            outlineRoot.sizeDelta = new Vector2(252f, 86f);
            outlineRoot.SetSiblingIndex(Mathf.Max(0, listenButton.transform.GetSiblingIndex()));
            outlineRoot.gameObject.SetActive(false);

            Outline legacyOutline = outlineRoot.GetComponent<Outline>();
            if (legacyOutline != null) legacyOutline.enabled = false;

            SerializedObject guideSerialized = new(guide);
            guideSerialized.FindProperty("glowColor").colorValue = Color.white;
            guideSerialized.FindProperty("glowIntensity").floatValue = 1f;
            guideSerialized.FindProperty("glowExpansion").floatValue = 0f;
            guideSerialized.FindProperty("borderThickness").floatValue = 0f;
            guideSerialized.FindProperty("borderOpacity").floatValue = 0f;
            guideSerialized.FindProperty("peakScale").floatValue = 1.08f;
            guideSerialized.FindProperty("minimumAlpha").floatValue = 0.46f;
            guideSerialized.FindProperty("maximumAlpha").floatValue = 1f;
            guideSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("MundoMusicalListenOutlineAssetSetup: contorno transparente aplicado a Escuchar sin reemplazar su animación.");
        }

        private static Sprite ImportOutlineSprite()
        {
            AssetDatabase.ImportAsset(OutlineAssetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(OutlineAssetPath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException($"No se pudo importar {OutlineAssetPath}.");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = new Vector4(210f, 210f, 210f, 210f);
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(OutlineAssetPath);
            if (sprite == null) throw new InvalidOperationException($"No se encontró el sprite transparente {OutlineAssetPath}.");
            return sprite;
        }

        private static T[] ComponentsInScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        }
    }
}
