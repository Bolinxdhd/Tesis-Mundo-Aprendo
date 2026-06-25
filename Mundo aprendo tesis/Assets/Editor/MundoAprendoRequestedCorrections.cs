#if UNITY_EDITOR
using System;
using System.Linq;
using Bolin;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bolin.Editor
{
    public static class MundoAprendoRequestedCorrections
    {
        private const string MenuPath = "Assets/Scenes/Menu.unity";
        private const string StoryPath = "Assets/Scenes/MundoCuentos_VozTest.unity";
        private const string SizePath = "Assets/Scenes/MundoTamanos.unity";
        [MenuItem("Mundo Aprendo/Aplicar correcciones solicitadas")]
        public static void ApplyFromMenu()
        {
            ApplyAll();
        }

        public static void ApplyAll()
        {
            PatchScene(MenuPath, PatchMenu);
            PatchScene(StoryPath, PatchStoryWorld);
            PatchScene(SizePath, PatchSizeWorld);
            AssetDatabase.SaveAssets();
            Debug.Log("MundoAprendoRequestedCorrections: correcciones aplicadas y escenas guardadas.");
        }

        private static void PatchScene(string path, Action<Scene> patch)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            if (!wasLoaded)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }

            patch(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"No se pudo guardar la escena {path}.");
            }

            if (!wasLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void PatchMenu(Scene scene)
        {
            Animator[] animators = ComponentsInScene<Animator>(scene);
            Animation[] legacyAnimations = ComponentsInScene<Animation>(scene);
            UIStaggeredEntrance[] entrances = ComponentsInScene<UIStaggeredEntrance>(scene);
            UIFloatingAnimation[] floatingAnimations = ComponentsInScene<UIFloatingAnimation>(scene);

            string removedAnimatorNames = string.Join(", ", animators.Select(item => item.gameObject.name));
            foreach (Animator component in animators) UnityEngine.Object.DestroyImmediate(component, true);
            foreach (Animation component in legacyAnimations) UnityEngine.Object.DestroyImmediate(component, true);
            foreach (UIStaggeredEntrance component in entrances) UnityEngine.Object.DestroyImmediate(component, true);
            foreach (UIFloatingAnimation component in floatingAnimations) UnityEngine.Object.DestroyImmediate(component, true);

            Debug.Log($"Menu: eliminados {animators.Length} Animator ({removedAnimatorNames}), " +
                      $"{legacyAnimations.Length} Animation, {entrances.Length} entradas y " +
                      $"{floatingAnimations.Length} flotaciones. UIButtonFeedback y transiciones se conservaron.");
        }

        private static void PatchStoryWorld(Scene scene)
        {
            VoiceRecognitionTest manager = ComponentsInScene<VoiceRecognitionTest>(scene).Single();
            GameObject supportPanel = FindObject(scene, "SupportModePanel");
            if (supportPanel != null)
            {
                UnityEngine.Object.DestroyImmediate(supportPanel, true);
            }

            RectTransform recognizedCard = RequireRect(scene, "RecognizedTextCard");
            TMP_Text recognizedText = GetSerializedReference<TMP_Text>(manager, "recognizedText");
            ScrollRect recognizedScroll = ConfigureScrollableTextCard(
                recognizedCard,
                recognizedText,
                "ScrollViewLecturaReconocida",
                "Content",
                "TextoLecturaReconocida",
                250f);

            RectTransform storyCard = RequireRect(scene, "StoryTextCard");
            TMP_Text storyText = GetSerializedReference<TMP_Text>(manager, "storyText");
            ScrollRect storyScroll = ConfigureScrollableTextCard(
                storyCard,
                storyText,
                "ScrollViewCuerpoCuento",
                "Content",
                "TextoHistoria",
                600f);

            SerializedObject serializedManager = new(manager);
            serializedManager.FindProperty("recognizedTextScrollRect").objectReferenceValue = recognizedScroll;
            serializedManager.FindProperty("storyBodyScrollRect").objectReferenceValue = storyScroll;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("MundoCuentos_VozTest: eliminado SupportModePanel y creados los Scroll View persistentes del cuento y la lectura.");
        }

        private static void PatchSizeWorld(Scene scene)
        {
            SizeWorldController manager = ComponentsInScene<SizeWorldController>(scene).Single();
            GameObject bottomPanel = FindObject(scene, "Panel-botones-inferior");
            if (bottomPanel != null)
            {
                UnityEngine.Object.DestroyImmediate(bottomPanel, true);
            }

            RemoveOldAnimalButton(scene, "Animal-izquierdo");
            RemoveOldAnimalButton(scene, "Animal-derecho");

            RectTransform safeArea = RequireRect(scene, "Area-animales-segura");
            Button leftOption = CreateOptionOverlay(
                safeArea,
                "OpcionAnimalIzquierda",
                new Vector2(0.04f, 0.08f),
                new Vector2(0.49f, 0.88f),
                RequireRect(scene, "AnimalStageLeft"));
            Button rightOption = CreateOptionOverlay(
                safeArea,
                "OpcionAnimalDerecha",
                new Vector2(0.51f, 0.08f),
                new Vector2(0.96f, 0.88f),
                RequireRect(scene, "AnimalStageRight"));

            SerializedObject serializedManager = new(manager);
            serializedManager.FindProperty("leftAnimalButton").objectReferenceValue = leftOption;
            serializedManager.FindProperty("rightAnimalButton").objectReferenceValue = rightOption;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("MundoTamanos: eliminado Panel-botones-inferior y configuradas dos zonas clicables completas con bloqueo de ronda existente.");
        }

        private static ScrollRect ConfigureScrollableTextCard(
            RectTransform card,
            TMP_Text text,
            string scrollName,
            string contentName,
            string textName,
            float preferredCardHeight)
        {
            LayoutElement cardLayout = card.GetComponent<LayoutElement>();
            if (cardLayout != null) cardLayout.preferredHeight = preferredCardHeight;

            Transform existing = card.Find(scrollName);
            if (existing != null)
            {
                return existing.GetComponent<ScrollRect>();
            }

            GameObject scrollObject = new(scrollName, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            RectTransform scrollRectTransform = (RectTransform)scrollObject.transform;
            scrollRectTransform.SetParent(card, false);
            Stretch(scrollRectTransform, new Vector2(14f, 12f), new Vector2(-14f, -12f));
            Image scrollRaycast = scrollObject.GetComponent<Image>();
            scrollRaycast.color = new Color(1f, 1f, 1f, 0.001f);
            scrollRaycast.raycastTarget = true;

            GameObject viewportObject = new("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            RectTransform viewport = (RectTransform)viewportObject.transform;
            viewport.SetParent(scrollRectTransform, false);
            Stretch(viewport, Vector2.zero, Vector2.zero);
            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            viewportImage.raycastTarget = true;

            GameObject contentObject = new(contentName, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            RectTransform content = (RectTransform)contentObject.transform;
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            text.gameObject.name = textName;
            text.transform.SetParent(content, false);
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = false;
            text.raycastTarget = false;
            text.margin = new Vector4(8f, 6f, 8f, 6f);

            LayoutElement oldTextLayout = text.GetComponent<LayoutElement>();
            if (oldTextLayout != null) UnityEngine.Object.DestroyImmediate(oldTextLayout, true);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;

            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.elasticity = 0.08f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;
            scroll.scrollSensitivity = 24f;
            return scroll;
        }

        private static void RemoveOldAnimalButton(Scene scene, string objectName)
        {
            GameObject target = FindObject(scene, objectName);
            if (target == null) return;

            Button button = target.GetComponent<Button>();
            UIButtonFeedback feedback = target.GetComponent<UIButtonFeedback>();
            if (feedback != null) UnityEngine.Object.DestroyImmediate(feedback, true);
            if (button != null) UnityEngine.Object.DestroyImmediate(button, true);
        }

        private static Button CreateOptionOverlay(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            RectTransform feedbackTarget)
        {
            Transform old = parent.Find(name);
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject, true);

            GameObject overlay = new(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonFeedback));
            RectTransform rect = (RectTransform)overlay.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsLastSibling();

            Image image = overlay.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.001f);
            image.raycastTarget = true;

            Button button = overlay.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.86f, 0.9f, 1f, 0.9f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            UIButtonFeedback feedback = overlay.GetComponent<UIButtonFeedback>();
            SerializedObject serializedFeedback = new(feedback);
            serializedFeedback.FindProperty("button").objectReferenceValue = button;
            serializedFeedback.FindProperty("target").objectReferenceValue = feedbackTarget;
            serializedFeedback.ApplyModifiedPropertiesWithoutUndo();
            return button;
        }

        private static T GetSerializedReference<T>(UnityEngine.Object owner, string propertyName) where T : UnityEngine.Object
        {
            SerializedProperty property = new SerializedObject(owner).FindProperty(propertyName);
            T value = property != null ? property.objectReferenceValue as T : null;
            if (value == null) throw new InvalidOperationException($"Falta la referencia serializada {propertyName} en {owner.name}.");
            return value;
        }

        private static RectTransform RequireRect(Scene scene, string objectName)
        {
            GameObject gameObject = FindObject(scene, objectName);
            RectTransform rect = gameObject != null ? gameObject.GetComponent<RectTransform>() : null;
            if (rect == null) throw new InvalidOperationException($"No se encontro {objectName} en {scene.path}.");
            return rect;
        }

        private static GameObject FindObject(Scene scene, string objectName)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(item => item.name == objectName)?.gameObject;
        }

        private static T[] ComponentsInScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }
    }
}
#endif
