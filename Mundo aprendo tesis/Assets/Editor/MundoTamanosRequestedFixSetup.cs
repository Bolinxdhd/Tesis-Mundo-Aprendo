#if UNITY_EDITOR
using System;
using System.Linq;
using Bolin;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bolin.Editor
{
    /// <summary>
    /// Applies only the requested persistent correction to MundoTamanos.  It never
    /// rebuilds the scene, replaces the tutorial, or changes size-world mechanics.
    /// </summary>
    public static class MundoTamanosRequestedFixSetup
    {
        private const string ScenePath = "Assets/Scenes/MundoTamanos.unity";
        private const string GuidePath = "Assets/Mundo Aprendo/Personajes/_Poses/Explorador_animo.png";
        private const string StarPanelPath = "Assets/MundoAprendo/UI/Generated/MundoTamanos/StarPanel.png";
        private const string EmptyStarPath = "Assets/Hyper_Casual_UI/Sprites/Usar/rataing star (1).png";

        [MenuItem("Mundo Aprendo/Mundo Tamaños/Aplicar corrección solicitada")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SizeWorldController controller = ComponentsInScene<SizeWorldController>(scene).SingleOrDefault();
            if (controller == null)
            {
                throw new InvalidOperationException("MundoTamanos no contiene un SizeWorldController.");
            }

            Transform gameplay = ComponentsInScene<Transform>(scene).FirstOrDefault(item => item.name == "GameplayPresentation");
            RectTransform safeArea = ComponentsInScene<RectTransform>(scene).FirstOrDefault(item => item.name == "Area-animales-segura");
            if (gameplay == null || safeArea == null)
            {
                throw new InvalidOperationException("La presentación persistente de MundoTamanos no tiene GameplayPresentation o Area-animales-segura.");
            }

            Sprite guideSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GuidePath);
            if (guideSprite == null)
            {
                throw new InvalidOperationException($"No se encontró el personaje de Tamaños: {GuidePath}");
            }

            RectTransform guideRoot = ComponentsInScene<RectTransform>(scene).FirstOrDefault(item => item.name == "GuiaMundoTamanos");
            if (guideRoot == null)
            {
                GameObject guideObject = new GameObject("GuiaMundoTamanos", typeof(RectTransform));
                SceneManager.MoveGameObjectToScene(guideObject, scene);
                guideRoot = guideObject.GetComponent<RectTransform>();
                guideRoot.SetParent(gameplay, false);
            }

            SetRect(guideRoot, new Vector2(0.5f, 0.5f), new Vector2(-770f, -194f), new Vector2(330f, 380f));
            guideRoot.SetSiblingIndex(safeArea.GetSiblingIndex());

            Image guideImage = guideRoot.GetComponentInChildren<Image>(true);
            if (guideImage == null)
            {
                GameObject imageObject = new GameObject("ImagenGuia", typeof(RectTransform), typeof(Image));
                imageObject.transform.SetParent(guideRoot, false);
                guideImage = imageObject.GetComponent<Image>();
            }

            Stretch(guideImage.rectTransform, 3f, 3f);
            guideImage.sprite = guideSprite;
            guideImage.color = Color.white;
            guideImage.enabled = true;
            guideImage.preserveAspect = true;
            guideImage.raycastTarget = false;

            EnsureBottomStars(gameplay);

            // The controller owns the transforms at the start of every round, so their
            // serialized destinations must match the two persisted animal cards.
            SerializedObject controllerSerialized = new SerializedObject(controller);
            SetVector2(controllerSerialized, "leftAnimalAnchoredPosition", new Vector2(-336f, -8f));
            SetVector2(controllerSerialized, "rightAnimalAnchoredPosition", new Vector2(336f, -8f));
            SerializedProperty fixedScale = controllerSerialized.FindProperty("fixedAnimalScale");
            if (fixedScale != null) fixedScale.floatValue = 1f;
            controllerSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("No se pudo guardar la corrección solicitada de MundoTamanos.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("MundoTamanosRequestedFixSetup: personaje permanente y transform fijo guardados.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void SetVector2(SerializedObject target, string propertyName, Vector2 value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property != null) property.vector2Value = value;
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void Stretch(RectTransform rect, float horizontalInset, float verticalInset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontalInset, verticalInset);
            rect.offsetMax = new Vector2(-horizontalInset, -verticalInset);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void EnsureBottomStars(Transform gameplay)
        {
            Sprite panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(StarPanelPath);
            Sprite emptyStar = AssetDatabase.LoadAssetAtPath<Sprite>(EmptyStarPath);
            if (panelSprite == null || emptyStar == null)
            {
                throw new InvalidOperationException("No se pudieron cargar los recursos persistentes de estrellas de MundoTamanos.");
            }

            Transform existing = gameplay.Find("BottomStars");
            RectTransform root;
            if (existing == null)
            {
                GameObject gameObject = new GameObject("BottomStars", typeof(RectTransform), typeof(Image));
                gameObject.transform.SetParent(gameplay, false);
                root = gameObject.GetComponent<RectTransform>();
            }
            else
            {
                root = existing.GetComponent<RectTransform>();
                if (root == null)
                {
                    throw new InvalidOperationException("BottomStars debe usar RectTransform para conservar su diseño UI.");
                }
            }

            SetRect(root, new Vector2(0.5f, 0.5f), new Vector2(170f, -493f), new Vector2(350f, 88f));
            Image panel = root.GetComponent<Image>();
            if (panel == null) panel = root.gameObject.AddComponent<Image>();
            panel.sprite = panelSprite;
            panel.type = Image.Type.Sliced;
            panel.color = Color.white;
            panel.raycastTarget = false;

            for (int index = 0; index < 3; index++)
            {
                string name = "EstrellaInferior_" + (index + 1);
                Transform child = root.Find(name);
                Image star;
                if (child == null)
                {
                    GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
                    gameObject.transform.SetParent(root, false);
                    star = gameObject.GetComponent<Image>();
                }
                else
                {
                    star = child.GetComponent<Image>();
                    if (star == null) star = child.gameObject.AddComponent<Image>();
                }

                SetRect(star.rectTransform, new Vector2(0.2f + index * 0.3f, 0.5f), Vector2.zero, new Vector2(68f, 68f));
                star.sprite = emptyStar;
                star.color = Color.white;
                star.preserveAspect = true;
                star.raycastTarget = false;
            }
        }

        private static T[] ComponentsInScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        }
    }
}
#endif
