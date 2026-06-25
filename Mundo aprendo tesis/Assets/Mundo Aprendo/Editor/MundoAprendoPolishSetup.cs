using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SurpriseBox.Scripts;
using Jsgaona;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bolin.Editor
{
    public static class MundoAprendoPolishSetup
    {
        private const string MenuScene = "Assets/Scenes/Menu.unity";
        private const string SelectionScene = "Assets/Scenes/SeleccionMundos.unity";
        private const string MusicalScene = "Assets/Scenes/MundoMusical.unity";
        private const string AudioManagerPrefabPath = "Assets/Mundo Aprendo/Prefabs/Systems/AudioManager.prefab";
        private static GameObject audioManagerPrefab;

        private static readonly string[] ScenePaths =
        {
            MenuScene,
            SelectionScene,
            MusicalScene,
            "Assets/Scenes/MundoCuentos_VozTest.unity",
            "Assets/Scenes/MundoTamanos.unity",
            "Assets/Scenes/Mundos/MundoEmociones.unity"
        };

        private static readonly HashSet<string> AnimatedPanelNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "StartPanel", "ResultPanel", "Panel-instrucciones", "Panel-ajustesonido",
            "Panel-ajustes", "Panel-opciones", "Panel-creditos", "Panel-seleccion-mundos"
        };

        [MenuItem("Mundo Aprendo/Aplicar mejoras visuales y tecnicas")]
        public static void ApplyProjectPolish()
        {
            audioManagerPrefab = EnsureAudioManagerPrefab();
            foreach (string scenePath in ScenePaths)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                {
                    Debug.LogWarning($"No se pudo mejorar una escena inexistente: {scenePath}");
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                if (scenePath == MusicalScene) MigrateMusicalTexts(scene);
                PolishScene(scene, scenePath);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, scenePath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
            Debug.Log("Mejoras visuales y tecnicas aplicadas de forma idempotente a las seis escenas.");
        }

        private static void PolishScene(Scene scene, string scenePath)
        {
            Canvas[] canvases = ComponentsInScene<Canvas>(scene);
            foreach (Canvas canvas in canvases)
            {
                ConfigureCanvas(canvas);
            }

            ConfigureButtons(scene);
            ConfigurePanels(scene);
            ConfigureFloatingElements(scene);
            ConfigureStarDisplays(scene);
            ConfigureWorldSpecificUi(scene, scenePath);

            ConfigureAudioManager(scene, scenePath == MenuScene);
            if (scenePath == SelectionScene) ConfigureWorldSelection(scene);
        }

        private static void ConfigureCanvas(Canvas canvas)
        {
            CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvas.gameObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (!canvas.isRootCanvas) return;

            CanvasColorBlindAccessibility accessibility = GetOrAdd<CanvasColorBlindAccessibility>(canvas.gameObject);
            SerializedObject accessibilitySo = new(accessibility);
            SetReference(accessibilitySo, "targetCanvas", canvas);
            SetReference(accessibilitySo, "colorBlindDropdown", FindColorBlindDropdown(canvas));

            Graphic[] graphics = canvas.GetComponentsInChildren<Graphic>(true)
                .Where(graphic => graphic is Image || graphic is RawImage)
                .ToArray();
            SetObjectArray(accessibilitySo.FindProperty("targetGraphics"), graphics.Cast<UnityEngine.Object>().ToArray());
            accessibilitySo.ApplyModifiedPropertiesWithoutUndo();

            EnsureSceneTransition(canvas);
        }

        private static void ConfigureButtons(Scene scene)
        {
            foreach (Button button in ComponentsInScene<Button>(scene))
            {
                GetOrAdd<UIButtonFeedback>(button.gameObject);
                if (button.targetGraphic is not Image image) continue;

                Shadow shadow = image.GetComponents<Shadow>().FirstOrDefault(component => component.GetType() == typeof(Shadow));
                if (shadow == null) shadow = image.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0.05f, 0.08f, 0.14f, 0.2f);
                shadow.effectDistance = new Vector2(0f, -4f);
                shadow.useGraphicAlpha = true;
            }
        }

        private static void ConfigurePanels(Scene scene)
        {
            foreach (RectTransform rect in ComponentsInScene<RectTransform>(scene))
            {
                if (!IsAnimatedPanel(rect.name)) continue;
                CanvasGroup group = GetOrAdd<CanvasGroup>(rect.gameObject);
                UIPanelTransition transition = GetOrAdd<UIPanelTransition>(rect.gameObject);
                SerializedObject transitionSo = new(transition);
                SetReference(transitionSo, "canvasGroup", group);
                SetReference(transitionSo, "target", rect);
                transitionSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ConfigureFloatingElements(Scene scene)
        {
            string[] names = { "Logo", "Imagen-mundo", "NunaGuide", "NunaBody", "Tamborcin" };
            foreach (RectTransform rect in ComponentsInScene<RectTransform>(scene))
            {
                if (names.Any(name => rect.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    GetOrAdd<UIFloatingAnimation>(rect.gameObject);
                }
            }
        }

        private static void ConfigureStarDisplays(Scene scene)
        {
            foreach (MonoBehaviour behaviour in ComponentsInScene<MonoBehaviour>(scene))
            {
                if (behaviour == null) continue;
                SerializedObject owner = new(behaviour);
                SerializedProperty stars = owner.FindProperty("starImages");
                SerializedProperty display = owner.FindProperty("starDisplay");
                if (stars == null || !stars.isArray || display == null) continue;

                UIStarDisplay starDisplay = CreateStarDisplay(
                    behaviour.gameObject,
                    stars,
                    owner.FindProperty("fullStarSprite"),
                    owner.FindProperty("emptyStarSprite"));
                display.objectReferenceValue = starDisplay;
                owner.ApplyModifiedPropertiesWithoutUndo();
            }

            WorldSelectionManager manager = FirstComponentInScene<WorldSelectionManager>(scene);
            if (manager == null) return;

            SerializedObject managerSo = new(manager);
            SerializedProperty worlds = managerSo.FindProperty("worlds");
            for (int i = 0; i < worlds.arraySize; i++)
            {
                SerializedProperty world = worlds.GetArrayElementAtIndex(i);
                SerializedProperty stars = world.FindPropertyRelative("starImages");
                UIStarDisplay display = CreateStarDisplay(
                    manager.gameObject,
                    stars,
                    world.FindPropertyRelative("fullStarSprite"),
                    world.FindPropertyRelative("emptyStarSprite"));
                world.FindPropertyRelative("starDisplay").objectReferenceValue = display;
            }
            managerSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static UIStarDisplay CreateStarDisplay(
            GameObject fallbackRoot,
            SerializedProperty stars,
            SerializedProperty earnedSprite,
            SerializedProperty unearnedSprite)
        {
            List<Image> images = new();
            for (int i = 0; i < stars.arraySize; i++)
            {
                if (stars.GetArrayElementAtIndex(i).objectReferenceValue is Image image) images.Add(image);
            }

            GameObject host = images.Count > 0 && images[0] != null && images[0].transform.parent != null
                ? images[0].transform.parent.gameObject
                : fallbackRoot;
            UIStarDisplay display = GetOrAdd<UIStarDisplay>(host);
            SerializedObject displaySo = new(display);
            SetObjectArray(displaySo.FindProperty("stars"), images.Cast<UnityEngine.Object>().ToArray());
            if (earnedSprite != null) displaySo.FindProperty("earnedSprite").objectReferenceValue = earnedSprite.objectReferenceValue;
            if (unearnedSprite != null) displaySo.FindProperty("unearnedSprite").objectReferenceValue = unearnedSprite.objectReferenceValue;
            displaySo.ApplyModifiedPropertiesWithoutUndo();
            return display;
        }

        private static void ConfigureAudioManager(Scene scene, bool configureOptions)
        {
            AudioManager manager = FirstComponentInScene<AudioManager>(scene);
            if (manager == null)
            {
                GameObject root = audioManagerPrefab != null
                    ? PrefabUtility.InstantiatePrefab(audioManagerPrefab, scene) as GameObject
                    : null;
                if (root == null)
                {
                    root = new GameObject("AudioManager");
                    SceneManager.MoveGameObjectToScene(root, scene);
                    manager = root.AddComponent<AudioManager>();
                }
                else
                {
                    manager = root.GetComponent<AudioManager>();
                }
            }

            AudioSource sfxSource = GetOrAdd<AudioSource>(manager.gameObject);
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;

            Transform musicTransform = manager.transform.Find("MusicSource");
            if (musicTransform == null)
            {
                GameObject musicObject = new("MusicSource");
                musicObject.transform.SetParent(manager.transform, false);
                musicTransform = musicObject.transform;
            }

            AudioSource musicSource = GetOrAdd<AudioSource>(musicTransform.gameObject);
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;

            SerializedObject managerSo = new(manager);
            SetReference(managerSo, "musicSource", musicSource);
            SetReference(managerSo, "sfxSource", sfxSource);
            managerSo.ApplyModifiedPropertiesWithoutUndo();

            if (audioManagerPrefab != null && PrefabUtility.GetPrefabInstanceStatus(manager.gameObject) == PrefabInstanceStatus.NotAPrefab)
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(manager.gameObject, AudioManagerPrefabPath, InteractionMode.AutomatedAction);
            }

            if (!configureOptions) return;
            foreach (AudioOptionsController options in ComponentsInScene<AudioOptionsController>(scene))
            {
                SerializedObject optionsSo = new(options);
                SetReference(optionsSo, "musicSource", musicSource);
                optionsSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ConfigureWorldSelection(Scene scene)
        {
            WorldSelectionManager manager = FirstComponentInScene<WorldSelectionManager>(scene);
            Canvas canvas = ComponentsInScene<Canvas>(scene).FirstOrDefault(item => item.isRootCanvas);
            if (manager == null || canvas == null) return;

            GameObject confirmation = FindDescendant(canvas.transform, "ResetProgressConfirmation")?.gameObject;
            if (confirmation == null) confirmation = CreateResetConfirmation(canvas.transform, manager);

            UIPanelTransition transition = GetOrAdd<UIPanelTransition>(confirmation);
            CanvasGroup group = GetOrAdd<CanvasGroup>(confirmation);
            SerializedObject transitionSo = new(transition);
            SetReference(transitionSo, "canvasGroup", group);
            SetReference(transitionSo, "target", confirmation.transform as RectTransform);
            transitionSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject managerSo = new(manager);
            SetReference(managerSo, "resetConfirmationPanel", confirmation);
            SetReference(managerSo, "resetConfirmationTransition", transition);
            managerSo.ApplyModifiedPropertiesWithoutUndo();

            Button resetButton = ComponentsInScene<Button>(scene)
                .FirstOrDefault(button => button.name.Equals("Button-reset-progreso", StringComparison.OrdinalIgnoreCase));
            if (resetButton != null)
            {
                resetButton.onClick.RemoveAllListeners();
                UnityEventTools.AddPersistentListener(resetButton.onClick, manager.RequestResetProgress);
                EditorUtility.SetDirty(resetButton);
            }

            RectTransform[] cards = ComponentsInScene<RectTransform>(scene)
                .Where(rect => rect.name.StartsWith("Card-Mundo", StringComparison.OrdinalIgnoreCase))
                .OrderBy(rect => rect.GetSiblingIndex())
                .ToArray();
            if (cards.Length > 0)
            {
                GameObject host = cards[0].parent != null ? cards[0].parent.gameObject : manager.gameObject;
                UIStaggeredEntrance entrance = GetOrAdd<UIStaggeredEntrance>(host);
                CanvasGroup[] groups = cards.Select(card => GetOrAdd<CanvasGroup>(card.gameObject)).ToArray();
                SerializedObject entranceSo = new(entrance);
                SetObjectArray(entranceSo.FindProperty("items"), cards.Cast<UnityEngine.Object>().ToArray());
                SetObjectArray(entranceSo.FindProperty("itemGroups"), groups.Cast<UnityEngine.Object>().ToArray());
                entranceSo.ApplyModifiedPropertiesWithoutUndo();
            }

            managerSo.Update();
            SerializedProperty worlds = managerSo.FindProperty("worlds");
            for (int i = 0; i < worlds.arraySize; i++)
            {
                SerializedProperty world = worlds.GetArrayElementAtIndex(i);
                if (world.FindPropertyRelative("worldButton").objectReferenceValue is Button worldButton && worldButton.targetGraphic != null)
                {
                    world.FindPropertyRelative("progressGraphic").objectReferenceValue = worldButton.targetGraphic;
                    world.FindPropertyRelative("baseProgressColor").colorValue = worldButton.targetGraphic.color;
                }
            }
            managerSo.ApplyModifiedPropertiesWithoutUndo();

            confirmation.SetActive(false);
        }

        private static void ConfigureWorldSpecificUi(Scene scene, string scenePath)
        {
            if (scenePath == MenuScene) ConfigureMenuEntrance(scene);
            else if (scenePath == MusicalScene) ConfigureMusicalProgress(scene);
            else if (scenePath.EndsWith("MundoCuentos_VozTest.unity", StringComparison.OrdinalIgnoreCase)) ConfigureStoryProgress(scene);
            else if (scenePath.EndsWith("MundoTamanos.unity", StringComparison.OrdinalIgnoreCase)) ConfigureSizeWorldLabels(scene);
            else if (scenePath.EndsWith("MundoEmociones.unity", StringComparison.OrdinalIgnoreCase)) ConfigureEmotionProgress(scene);
        }

        private static void ConfigureMenuEntrance(Scene scene)
        {
            Canvas canvas = ComponentsInScene<Canvas>(scene).FirstOrDefault(item => item.isRootCanvas);
            if (canvas == null) return;
            RectTransform logo = ComponentsInScene<RectTransform>(scene)
                .FirstOrDefault(item => item.name.Equals("menu", StringComparison.OrdinalIgnoreCase));
            if (logo != null) GetOrAdd<UIFloatingAnimation>(logo.gameObject);

            RectTransform[] mainButtons = ComponentsInScene<Button>(scene)
                .Where(button => button.gameObject.activeInHierarchy)
                .Select(button => button.transform as RectTransform)
                .Where(rect => rect != null)
                .Take(6)
                .ToArray();
            if (mainButtons.Length == 0) return;
            CanvasGroup[] groups = mainButtons.Select(item => GetOrAdd<CanvasGroup>(item.gameObject)).ToArray();
            UIStaggeredEntrance entrance = GetOrAdd<UIStaggeredEntrance>(canvas.gameObject);
            SerializedObject entranceSo = new(entrance);
            SetObjectArray(entranceSo.FindProperty("items"), mainButtons.Cast<UnityEngine.Object>().ToArray());
            SetObjectArray(entranceSo.FindProperty("itemGroups"), groups.Cast<UnityEngine.Object>().ToArray());
            entranceSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureMusicalProgress(Scene scene)
        {
            MundoMusicalSequenceGame manager = FirstComponentInScene<MundoMusicalSequenceGame>(scene);
            Canvas canvas = ComponentsInScene<Canvas>(scene).FirstOrDefault(item => item.isRootCanvas);
            if (manager == null || canvas == null) return;

            GameObject container = FindDescendant(canvas.transform, "SequenceProgressContainer")?.gameObject;
            if (container == null)
            {
                container = CreateUiObject("SequenceProgressContainer", canvas.transform);
                RectTransform rect = container.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -235f);
                rect.sizeDelta = new Vector2(900f, 96f);
                Image background = container.AddComponent<Image>();
                background.sprite = BuiltinUiSprite();
                background.type = Image.Type.Sliced;
                background.color = new Color(0.98f, 0.99f, 1f, 0.94f);
                Shadow shadow = container.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.18f);
                shadow.effectDistance = new Vector2(0f, -5f);

                for (int i = 0; i < 8; i++)
                {
                    GameObject step = CreateUiObject($"SequenceStep_{i + 1}", container.transform);
                    RectTransform stepRect = step.GetComponent<RectTransform>();
                    stepRect.anchorMin = stepRect.anchorMax = new Vector2(0.5f, 0.5f);
                    stepRect.anchoredPosition = new Vector2(-343f + i * 98f, 0f);
                    stepRect.sizeDelta = new Vector2(78f, 62f);
                    Image image = step.AddComponent<Image>();
                    image.sprite = BuiltinUiSprite();
                    image.type = Image.Type.Sliced;
                    image.color = new Color(0.68f, 0.72f, 0.78f, 0.8f);
                    CreateLabel("Note", step.transform, "DO", 26f, Vector2.zero, stepRect.sizeDelta, Color.white);
                }
            }

            Image[] indicators = container.transform.Cast<Transform>()
                .Where(item => item.name.StartsWith("SequenceStep_", StringComparison.Ordinal))
                .OrderBy(item => item.GetSiblingIndex())
                .Select(item => item.GetComponent<Image>())
                .ToArray();
            TMP_Text[] labels = indicators.Select(item => item != null ? item.GetComponentInChildren<TMP_Text>(true) : null).ToArray();
            SerializedObject managerSo = new(manager);
            SetObjectArray(managerSo.FindProperty("sequenceStepIndicators"), indicators.Cast<UnityEngine.Object>().ToArray());
            SetObjectArray(managerSo.FindProperty("sequenceStepLabels"), labels.Cast<UnityEngine.Object>().ToArray());
            managerSo.ApplyModifiedPropertiesWithoutUndo();
            GetOrAdd<CanvasGroup>(container);
            GetOrAdd<UIPanelTransition>(container);
        }

        private static void ConfigureStoryProgress(Scene scene)
        {
            VoiceRecognitionTest manager = FirstComponentInScene<VoiceRecognitionTest>(scene);
            Canvas canvas = ComponentsInScene<Canvas>(scene).FirstOrDefault(item => item.isRootCanvas);
            if (manager == null || canvas == null) return;

            UIProgressBar progressBar = EnsureProgressBar(canvas.transform, "ReadingProgressBar", new Vector2(0f, -150f), new Vector2(760f, 58f), new Color(0.2f, 0.66f, 0.62f, 1f));
            GameObject indicator = FindDescendant(canvas.transform, "MicrophoneListeningIndicator")?.gameObject;
            if (indicator == null)
            {
                indicator = CreateUiObject("MicrophoneListeningIndicator", canvas.transform);
                RectTransform rect = indicator.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
                rect.anchoredPosition = new Vector2(-190f, -145f);
                rect.sizeDelta = new Vector2(270f, 64f);
                Image image = indicator.AddComponent<Image>();
                image.sprite = BuiltinUiSprite();
                image.type = Image.Type.Sliced;
                image.color = new Color(0.22f, 0.62f, 0.66f, 0.96f);
                CreateLabel("Label", indicator.transform, "MICRÓFONO ACTIVO", 22f, Vector2.zero, rect.sizeDelta, Color.white);
                GetOrAdd<UIFloatingAnimation>(indicator);
            }
            indicator.SetActive(false);

            SerializedObject managerSo = new(manager);
            SetReference(managerSo, "readingProgressBar", progressBar);
            SetReference(managerSo, "microphoneListeningIndicator", indicator);
            managerSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureEmotionProgress(Scene scene)
        {
            EmotionGameManager manager = FirstComponentInScene<EmotionGameManager>(scene);
            Canvas canvas = ComponentsInScene<Canvas>(scene).FirstOrDefault(item => item.isRootCanvas);
            if (manager == null || canvas == null) return;
            UIProgressBar progressBar = EnsureProgressBar(canvas.transform, "EmotionRoundProgressBar", new Vector2(0f, -155f), new Vector2(720f, 58f), new Color(0.95f, 0.52f, 0.38f, 1f));
            SerializedObject managerSo = new(manager);
            SetReference(managerSo, "roundProgressBar", progressBar);
            managerSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSizeWorldLabels(Scene scene)
        {
            SizeWorldController manager = FirstComponentInScene<SizeWorldController>(scene);
            if (manager == null) return;
            SerializedObject managerSo = new(manager);
            RectTransform safeArea = managerSo.FindProperty("animalSafeArea").objectReferenceValue as RectTransform;
            Image leftImage = managerSo.FindProperty("leftAnimalImage").objectReferenceValue as Image;
            Image rightImage = managerSo.FindProperty("rightAnimalImage").objectReferenceValue as Image;
            if (safeArea == null || leftImage == null || rightImage == null) return;

            TMP_Text leftLabel = EnsureAnimalLabel(safeArea, "LeftAnimalName", new Vector2(0.28f, 0.1f));
            TMP_Text rightLabel = EnsureAnimalLabel(safeArea, "RightAnimalName", new Vector2(0.72f, 0.1f));
            Outline leftOutline = EnsureSelectionOutline(leftImage);
            Outline rightOutline = EnsureSelectionOutline(rightImage);
            SetReference(managerSo, "leftAnimalNameText", leftLabel);
            SetReference(managerSo, "rightAnimalNameText", rightLabel);
            SetReference(managerSo, "leftSelectionOutline", leftOutline);
            SetReference(managerSo, "rightSelectionOutline", rightOutline);
            managerSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TMP_Text EnsureAnimalLabel(Transform parent, string name, Vector2 anchor)
        {
            Transform existing = FindDescendant(parent, name);
            if (existing != null) return existing.GetComponentInChildren<TMP_Text>(true);
            GameObject labelRoot = CreateUiObject(name, parent);
            RectTransform rect = labelRoot.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(380f, 64f);
            Image background = labelRoot.AddComponent<Image>();
            background.sprite = BuiltinUiSprite();
            background.type = Image.Type.Sliced;
            background.color = new Color(0.97f, 0.99f, 1f, 0.9f);
            return CreateLabel("Text", labelRoot.transform, "Animal", 30f, Vector2.zero, rect.sizeDelta);
        }

        private static Outline EnsureSelectionOutline(Image image)
        {
            Outline outline = image.GetComponent<Outline>();
            if (outline == null) outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.2f, 0.75f, 0.42f, 1f);
            outline.effectDistance = new Vector2(5f, -5f);
            outline.useGraphicAlpha = true;
            outline.enabled = false;
            return outline;
        }

        private static UIProgressBar EnsureProgressBar(Transform parent, string name, Vector2 position, Vector2 size, Color fillColor)
        {
            Transform existing = FindDescendant(parent, name);
            if (existing != null) return existing.GetComponent<UIProgressBar>();
            GameObject root = CreateUiObject(name, parent);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image background = root.AddComponent<Image>();
            background.sprite = BuiltinUiSprite();
            background.type = Image.Type.Sliced;
            background.color = new Color(0.14f, 0.17f, 0.24f, 0.72f);

            GameObject fillObject = CreateUiObject("Fill", root.transform);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            Stretch(fillRect);
            fillRect.offsetMin = new Vector2(6f, 6f);
            fillRect.offsetMax = new Vector2(-6f, -6f);
            Image fill = fillObject.AddComponent<Image>();
            fill.sprite = BuiltinUiSprite();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;
            fill.color = fillColor;
            TMP_Text label = CreateLabel("Value", root.transform, "0%", 23f, Vector2.zero, size, Color.white);
            UIProgressBar progress = root.AddComponent<UIProgressBar>();
            SerializedObject progressSo = new(progress);
            SetReference(progressSo, "fillImage", fill);
            SetReference(progressSo, "valueText", label);
            progressSo.ApplyModifiedPropertiesWithoutUndo();
            return progress;
        }

        private static GameObject CreateResetConfirmation(Transform parent, WorldSelectionManager manager)
        {
            GameObject root = CreateUiObject("ResetProgressConfirmation", parent);
            RectTransform rect = root.GetComponent<RectTransform>();
            Stretch(rect);
            Image backdrop = root.AddComponent<Image>();
            backdrop.color = new Color(0.03f, 0.05f, 0.1f, 0.72f);

            GameObject dialog = CreateUiObject("Dialog", root.transform);
            RectTransform dialogRect = dialog.GetComponent<RectTransform>();
            dialogRect.anchorMin = dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.sizeDelta = new Vector2(720f, 390f);
            Image dialogImage = dialog.AddComponent<Image>();
            dialogImage.color = new Color(0.98f, 0.99f, 1f, 1f);
            Shadow shadow = dialog.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.25f);
            shadow.effectDistance = new Vector2(0f, -8f);

            CreateLabel("Title", dialog.transform, "¿Reiniciar el progreso?", 42f, new Vector2(0f, 92f), new Vector2(620f, 70f));
            CreateLabel("Message", dialog.transform, "Se borrarán las estrellas y los mundos volverán a bloquearse.", 28f, new Vector2(0f, 18f), new Vector2(610f, 90f));
            CreateDialogButton("CancelButton", dialog.transform, "CANCELAR", new Vector2(-170f, -110f), new Color(0.22f, 0.5f, 0.72f, 1f), manager.CancelResetProgress);
            CreateDialogButton("ConfirmButton", dialog.transform, "REINICIAR", new Vector2(170f, -110f), new Color(0.86f, 0.3f, 0.28f, 1f), manager.ResetProgress);
            return root;
        }

        private static void CreateDialogButton(string name, Transform parent, string label, Vector2 position, Color color, UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = CreateUiObject(name, parent);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(270f, 78f);
            Image image = buttonObject.AddComponent<Image>();
            image.color = color;
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            UnityEventTools.AddPersistentListener(button.onClick, action);
            GetOrAdd<UIButtonFeedback>(buttonObject);
            CreateLabel("Label", buttonObject.transform, label, 28f, Vector2.zero, rect.sizeDelta, Color.white);
        }

        private static TMP_Text CreateLabel(string name, Transform parent, string text, float size, Vector2 position, Vector2 dimensions, Color? color = null)
        {
            GameObject labelObject = CreateUiObject(name, parent);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = size;
            label.color = color ?? new Color(0.1f, 0.14f, 0.22f, 1f);
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        private static void EnsureSceneTransition(Canvas canvas)
        {
            Transform existing = FindDescendant(canvas.transform, "SceneTransitionOverlay");
            GameObject overlay = existing != null ? existing.gameObject : CreateUiObject("SceneTransitionOverlay", canvas.transform);
            RectTransform rect = GetOrAdd<RectTransform>(overlay);
            Stretch(rect);
            Image image = GetOrAdd<Image>(overlay);
            image.color = new Color(0.055f, 0.075f, 0.12f, 1f);
            image.raycastTarget = true;
            CanvasGroup group = GetOrAdd<CanvasGroup>(overlay);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            UISceneTransition transition = GetOrAdd<UISceneTransition>(overlay);
            SerializedObject transitionSo = new(transition);
            SetReference(transitionSo, "fadeCanvasGroup", group);
            transitionSo.ApplyModifiedPropertiesWithoutUndo();
            overlay.transform.SetAsLastSibling();
        }

        private static void MigrateMusicalTexts(Scene scene)
        {
            MundoMusicalSequenceGame manager = FirstComponentInScene<MundoMusicalSequenceGame>(scene);
            if (manager == null) return;

            SerializedObject managerSo = new(manager);
            GameObject legacyTitleObject = ReadGameObject(managerSo.FindProperty("legacyTitleText"));
            GameObject legacyStatusObject = ReadGameObject(managerSo.FindProperty("legacyStatusText"));
            GameObject legacySequenceObject = ReadGameObject(managerSo.FindProperty("legacySequenceText"));
            GameObject legacyResultObject = ReadGameObject(managerSo.FindProperty("legacyResultText"));
            List<GameObject> legacyKeyObjects = ReadTextObjects(managerSo.FindProperty("legacyKeyLabels"));

            Dictionary<GameObject, TextMeshProUGUI> converted = new();
            foreach (Text legacyText in ComponentsInScene<Text>(scene))
            {
                if (legacyText == null) continue;
                GameObject textObject = legacyText.gameObject;
                TextMeshProUGUI tmp = ConvertLegacyText(legacyText);
                converted[textObject] = tmp;
            }

            managerSo.Update();
            SetReference(managerSo, "tmpTitleText", ResolveConverted(legacyTitleObject, converted) ?? FindNamedComponent<TMP_Text>(scene, "Titulo"));
            SetReference(managerSo, "tmpStatusText", ResolveConverted(legacyStatusObject, converted) ?? FindNamedComponent<TMP_Text>(scene, "Estado"));
            SetReference(managerSo, "tmpSequenceText", ResolveConverted(legacySequenceObject, converted) ?? FindNamedComponent<TMP_Text>(scene, "Secuencia"));
            SetReference(managerSo, "tmpResultText", ResolveConverted(legacyResultObject, converted) ?? FindNamedComponent<TMP_Text>(scene, "TextoResultado"));

            TMP_Text[] resolvedKeys = legacyKeyObjects
                .Select(item => ResolveConverted(item, converted) as TMP_Text)
                .Where(item => item != null)
                .ToArray();
            if (resolvedKeys.Length == 0)
            {
                SerializedProperty keyButtons = managerSo.FindProperty("keyButtons");
                List<TMP_Text> labels = new();
                for (int i = 0; i < keyButtons.arraySize; i++)
                {
                    if (keyButtons.GetArrayElementAtIndex(i).objectReferenceValue is Button button)
                    {
                        labels.Add(button.GetComponentInChildren<TMP_Text>(true));
                    }
                }
                resolvedKeys = labels.ToArray();
            }
            SetObjectArray(managerSo.FindProperty("tmpKeyLabels"), resolvedKeys.Cast<UnityEngine.Object>().ToArray());
            managerSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TextMeshProUGUI ConvertLegacyText(Text source)
        {
            GameObject textObject = source.gameObject;
            string text = source.text;
            int fontSize = source.fontSize;
            Color color = source.color;
            bool raycastTarget = source.raycastTarget;
            bool maskable = source.maskable;
            bool enabled = source.enabled;
            bool richText = source.supportRichText;
            TextAlignmentOptions alignment = ConvertAlignment(source.alignment);
            FontStyles fontStyle = source.fontStyle switch
            {
                FontStyle.Bold => FontStyles.Bold,
                FontStyle.Italic => FontStyles.Italic,
                FontStyle.BoldAndItalic => FontStyles.Bold | FontStyles.Italic,
                _ => FontStyles.Normal
            };
            TextWrappingModes wrapping = source.horizontalOverflow == HorizontalWrapMode.Wrap
                ? TextWrappingModes.Normal
                : TextWrappingModes.NoWrap;
            TextOverflowModes overflow = source.verticalOverflow == VerticalWrapMode.Truncate
                ? TextOverflowModes.Truncate
                : TextOverflowModes.Overflow;

            UnityEngine.Object.DestroyImmediate(source, true);
            TextMeshProUGUI target = textObject.GetComponent<TextMeshProUGUI>();
            if (target == null) target = textObject.AddComponent<TextMeshProUGUI>();
            target.text = text;
            target.font = TMP_Settings.defaultFontAsset;
            target.fontSize = fontSize;
            target.color = color;
            target.raycastTarget = raycastTarget;
            target.maskable = maskable;
            target.enabled = enabled;
            target.richText = richText;
            target.alignment = alignment;
            target.fontStyle = fontStyle;
            target.textWrappingMode = wrapping;
            target.overflowMode = overflow;
            return target;
        }

        private static TextAlignmentOptions ConvertAlignment(TextAnchor alignment)
        {
            return alignment switch
            {
                TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
                TextAnchor.UpperCenter => TextAlignmentOptions.Top,
                TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
                TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
                TextAnchor.MiddleRight => TextAlignmentOptions.Right,
                TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
                TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
                TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
                _ => TextAlignmentOptions.Center
            };
        }

        private static GameObject ReadGameObject(SerializedProperty property)
        {
            return property?.objectReferenceValue is Component component ? component.gameObject : null;
        }

        private static List<GameObject> ReadTextObjects(SerializedProperty property)
        {
            List<GameObject> values = new();
            if (property == null || !property.isArray) return values;
            for (int i = 0; i < property.arraySize; i++)
            {
                values.Add(ReadGameObject(property.GetArrayElementAtIndex(i)));
            }
            return values;
        }

        private static TextMeshProUGUI ResolveConverted(GameObject gameObject, IReadOnlyDictionary<GameObject, TextMeshProUGUI> converted)
        {
            return gameObject != null && converted.TryGetValue(gameObject, out TextMeshProUGUI tmp) ? tmp : null;
        }

        private static T FindNamedComponent<T>(Scene scene, string objectName) where T : Component
        {
            return ComponentsInScene<T>(scene).FirstOrDefault(component => component.name == objectName);
        }

        private static GameObject EnsureAudioManagerPrefab()
        {
            EnsureAssetFolder("Assets/Mundo Aprendo/Prefabs");
            EnsureAssetFolder("Assets/Mundo Aprendo/Prefabs/Systems");
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(AudioManagerPrefabPath);
            if (existing != null) return existing;

            GameObject root = new("AudioManager");
            AudioManager manager = root.AddComponent<AudioManager>();
            AudioSource sfx = root.GetComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0f;
            GameObject musicObject = new("MusicSource");
            musicObject.transform.SetParent(root.transform, false);
            AudioSource music = musicObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.spatialBlend = 0f;
            SerializedObject managerSo = new(manager);
            SetReference(managerSo, "musicSource", music);
            SetReference(managerSo, "sfxSource", sfx);
            managerSo.ApplyModifiedPropertiesWithoutUndo();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, AudioManagerPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void EnsureAssetFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            string name = path.Substring(separator + 1);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static Sprite BuiltinUiSprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        }

        private static bool IsAnimatedPanel(string name)
        {
            if (AnimatedPanelNames.Contains(name)) return true;
            string normalized = name.ToLowerInvariant();
            return normalized.Contains("inicio") || normalized.Contains("result") || normalized.Contains("instruccion");
        }

        private static TMP_Dropdown FindColorBlindDropdown(Canvas canvas)
        {
            foreach (TMP_Dropdown dropdown in canvas.GetComponentsInChildren<TMP_Dropdown>(true))
            {
                Transform current = dropdown.transform;
                while (current != null && current != canvas.transform.parent)
                {
                    if (current.name.Equals("Daltonismo", StringComparison.OrdinalIgnoreCase)) return dropdown;
                    current = current.parent;
                }
            }
            return null;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static Transform FindDescendant(Transform root, string objectName)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName) return child;
            }
            return null;
        }

        private static T GetOrAdd<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static T FirstComponentInScene<T>(Scene scene) where T : Component
        {
            return ComponentsInScene<T>(scene).FirstOrDefault();
        }

        private static T[] ComponentsInScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
        }

        private static void SetReference(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void SetObjectArray(SerializedProperty property, UnityEngine.Object[] values)
        {
            if (property == null) return;
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
