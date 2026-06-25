using System;
using System.Collections.Generic;
using System.IO;
using Bolin;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bolin.Editor
{
    public static class MundoAprendoProjectSetup
    {
        private const string EmotionScenePath = "Assets/Scenes/Mundos/MundoEmociones.unity";
        private const string WorldSelectionScenePath = "Assets/Scenes/SeleccionMundos.unity";
        private const string PlaceholderFolder = "Assets/Mundo Aprendo/Art/Placeholders/Emociones";

        private static readonly string[] RequiredScenePaths =
        {
            "Assets/Scenes/Menu.unity",
            "Assets/Scenes/SeleccionMundos.unity",
            "Assets/Scenes/MundoMusical.unity",
            "Assets/Scenes/MundoCuentos_VozTest.unity",
            "Assets/Scenes/MundoTamanos.unity",
            EmotionScenePath
        };

        private static readonly Color Ink = new(0.12f, 0.15f, 0.22f, 1f);
        private static readonly Color Paper = new(0.98f, 0.98f, 0.94f, 0.97f);
        private static readonly Color Teal = new(0.1f, 0.58f, 0.58f, 1f);
        private static readonly Color Coral = new(0.92f, 0.38f, 0.34f, 1f);
        private static readonly Color Gold = new(0.94f, 0.7f, 0.18f, 1f);

        [MenuItem("Mundo Aprendo/Configurar proyecto completo")]
        public static void ConfigureProject()
        {
            EnsureFolders();
            EmotionPlaceholderAssets assets = LoadOrCreatePlaceholderAssets();
            Scene emotionScene = OpenOrCreateEmotionScene();
            GameObject sceneRoot = GetRootByName(emotionScene, "MundoEmociones");

            if (sceneRoot == null)
            {
                BuildEmotionScene(emotionScene, assets);
            }
            else
            {
                EditorSceneManager.MarkSceneDirty(emotionScene);
                EditorSceneManager.SaveScene(emotionScene, EmotionScenePath);
            }

            ConfigureWorldSelection();
            ConfigureBuildSettings();
            EditorSceneManager.OpenScene(EmotionScenePath, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateProjectInternal();
            Debug.Log("Mundo Aprendo configurado. MundoEmociones, navegacion y Build Settings estan listos.");
        }

        [MenuItem("Mundo Aprendo/Validar proyecto completo")]
        public static void ValidateProject()
        {
            ValidateProjectInternal();
        }

        private static void ValidateProjectInternal()
        {
            List<string> errors = new();
            ValidateBuildSettings(errors);
            ValidateWorldSelection(errors);
            ValidateExistingNavigation(errors);
            ValidateEmotionScene(errors);

            EditorSceneManager.OpenScene(EmotionScenePath, OpenSceneMode.Single);
            if (errors.Count > 0)
            {
                string report = string.Join("\n- ", errors);
                throw new InvalidOperationException($"Validacion de Mundo Aprendo fallida:\n- {report}");
            }

            Debug.Log("Validacion de Mundo Aprendo completada sin errores.");
        }

        private static void ValidateBuildSettings(List<string> errors)
        {
            HashSet<string> enabledScenes = new(StringComparer.OrdinalIgnoreCase);
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled) continue;
                if (!enabledScenes.Add(scene.path)) errors.Add($"Build Settings contiene la escena duplicada: {scene.path}");
            }

            foreach (string requiredPath in RequiredScenePaths)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(requiredPath) == null) errors.Add($"Falta la escena: {requiredPath}");
                if (!enabledScenes.Contains(requiredPath)) errors.Add($"La escena no esta habilitada en Build Settings: {requiredPath}");
            }
        }

        private static void ValidateWorldSelection(List<string> errors)
        {
            Scene scene = EditorSceneManager.OpenScene(WorldSelectionScenePath, OpenSceneMode.Single);
            WorldSelectionManager manager = GetComponentInScene<WorldSelectionManager>(scene);
            if (manager == null)
            {
                errors.Add("SeleccionMundos no contiene WorldSelectionManager.");
                return;
            }

            SerializedObject serialized = new(manager);
            SerializedProperty worlds = serialized.FindProperty("worlds");
            string[] expectedScenes =
            {
                MundoAprendoSceneNames.MusicalWorld,
                MundoAprendoSceneNames.StoriesWorld,
                MundoAprendoSceneNames.SizesWorld,
                MundoAprendoSceneNames.EmotionsWorld
            };

            if (worlds == null || worlds.arraySize != expectedScenes.Length)
            {
                errors.Add("WorldSelectionManager no tiene cuatro mundos.");
                return;
            }

            for (int i = 0; i < expectedScenes.Length; i++)
            {
                SerializedProperty world = worlds.GetArrayElementAtIndex(i);
                string route = world.FindPropertyRelative("sceneName").stringValue;
                if (route != expectedScenes[i]) errors.Add($"Ruta incorrecta en mundo {i}: '{route}'.");
                if (world.FindPropertyRelative("worldButton").objectReferenceValue == null) errors.Add($"Mundo {i} no tiene boton asignado.");

                SerializedProperty stars = world.FindPropertyRelative("starImages");
                if (stars == null || stars.arraySize != 3) errors.Add($"Mundo {i} no tiene tres estrellas asignadas.");
            }
        }

        private static void ValidateExistingNavigation(List<string> errors)
        {
            ValidateSceneButton("Assets/Scenes/Menu.unity", errors, "Button/Jugar");
            ValidateSceneButton("Assets/Scenes/MundoMusical.unity", errors, "Button-volver-seleccion");
            ValidateSceneButton("Assets/Scenes/MundoCuentos_VozTest.unity", errors, "BotonVolver");
            ValidateSceneButton("Assets/Scenes/MundoTamanos.unity", errors, "Boton-volver", "Boton-volver-seleccion");
        }

        private static void ValidateSceneButton(string scenePath, List<string> errors, params string[] acceptedNames)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            bool foundCandidate = false;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Button[] buttons = root.GetComponentsInChildren<Button>(true);
                foreach (Button button in buttons)
                {
                    foreach (string acceptedName in acceptedNames)
                    {
                        if (button.name != acceptedName) continue;
                        foundCandidate = true;
                        if (button.onClick.GetPersistentEventCount() > 0) return;
                    }
                }
            }

            if (!foundCandidate) errors.Add($"{scenePath}: no se encontro el boton de navegacion esperado.");
            else errors.Add($"{scenePath}: ningun boton de navegacion candidato tiene evento persistente.");
        }

        private static void ValidateEmotionScene(List<string> errors)
        {
            Scene scene = EditorSceneManager.OpenScene(EmotionScenePath, OpenSceneMode.Single);
            GameObject root = GetRootByName(scene, "MundoEmociones");
            if (root == null)
            {
                errors.Add("MundoEmociones no contiene su objeto raiz.");
                return;
            }

            string[] requiredObjectNames =
            {
                "Main Camera", "EventSystem", "Systems", "EmotionGameManager", "SceneNavigation", "Background",
                "SkyBackground", "DecorativeClouds", "Rainbow", "FloatingIslands", "Canvas", "SafeArea", "Header",
                "BackButton", "NunaGuide", "NunaBody", "NunaFace", "DialoguePanel", "StartPanel", "StartButton",
                "GamePanel", "EmotionDisplayArea", "Emotion_Alegria", "Emotion_Tristeza", "Emotion_Enojo",
                "Emotion_Sorpresa", "Emotion_Miedo", "FeedbackPanel", "AnswerButtons", "Button_Alegria",
                "Button_Tristeza", "Button_Enojo", "Button_Sorpresa", "Button_Miedo", "ResultPanel", "Star1",
                "Star2", "Star3", "RetryButton", "WorldsButton", "FadePanel", "AccessibilityCanvas", "Audio",
                "MusicSource", "SFXSource"
            };

            HashSet<string> hierarchyNames = new(StringComparer.Ordinal);
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform transform in transforms) hierarchyNames.Add(transform.name);
            foreach (string requiredName in requiredObjectNames)
            {
                if (!hierarchyNames.Contains(requiredName)) errors.Add($"MundoEmociones: falta {requiredName} en la jerarquia.");
            }

            foreach (Transform transform in transforms)
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0)
                {
                    errors.Add($"MundoEmociones: {transform.name} tiene un script faltante.");
                }
            }

            EmotionGameManager manager = root.GetComponentInChildren<EmotionGameManager>(true);
            if (manager == null)
            {
                errors.Add("MundoEmociones no contiene EmotionGameManager.");
                return;
            }

            SerializedObject serialized = new(manager);
            string[] requiredReferences =
            {
                "startPanel", "gamePanel", "gamePanelCanvasGroup", "feedbackPanel", "feedbackCanvasGroup",
                "resultPanel", "instructionText", "progressText", "feedbackText", "nunaDialogueText", "feedbackIcon",
                "correctIconSprite", "retryIconSprite", "nunaRoot", "musicSource", "sfxSource"
            };

            foreach (string propertyName in requiredReferences)
            {
                SerializedProperty property = serialized.FindProperty(propertyName);
                if (property == null || property.objectReferenceValue == null) errors.Add($"EmotionGameManager: referencia vacia {propertyName}.");
            }

            SerializedProperty emotionViews = serialized.FindProperty("emotionViews");
            if (emotionViews == null || emotionViews.arraySize != 5) errors.Add("EmotionGameManager necesita cinco EmotionRoundView.");
            else
            {
                for (int i = 0; i < emotionViews.arraySize; i++)
                {
                    SerializedProperty view = emotionViews.GetArrayElementAtIndex(i);
                    if (view.FindPropertyRelative("rootObject").objectReferenceValue == null) errors.Add($"EmotionRoundView {i}: rootObject vacio.");
                    if (view.FindPropertyRelative("animatedRect").objectReferenceValue == null) errors.Add($"EmotionRoundView {i}: animatedRect vacio.");
                    if (view.FindPropertyRelative("canvasGroup").objectReferenceValue == null) errors.Add($"EmotionRoundView {i}: canvasGroup vacio.");
                }
            }

            SerializedProperty answerButtons = serialized.FindProperty("answerButtons");
            if (answerButtons == null || answerButtons.arraySize != 5) errors.Add("EmotionGameManager necesita cinco botones de respuesta.");
            if (serialized.FindProperty("includeFear").boolValue) errors.Add("includeFear debe estar desactivado por defecto.");

            string[] requiredButtons = { "BackButton", "StartButton", "Button_Alegria", "Button_Tristeza", "Button_Enojo", "Button_Sorpresa", "Button_Miedo", "RetryButton", "WorldsButton" };
            Button[] sceneButtons = root.GetComponentsInChildren<Button>(true);
            foreach (string requiredButton in requiredButtons)
            {
                Button button = Array.Find(sceneButtons, candidate => candidate.name == requiredButton);
                if (button == null) errors.Add($"MundoEmociones: falta el boton {requiredButton}.");
                else if (button.onClick.GetPersistentEventCount() == 0) errors.Add($"MundoEmociones: {requiredButton} no tiene evento persistente.");
            }
        }

        private static Scene OpenOrCreateEmotionScene()
        {
            SceneAsset existingScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(EmotionScenePath);
            if (existingScene != null)
            {
                return EditorSceneManager.OpenScene(EmotionScenePath, OpenSceneMode.Single);
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = MundoAprendoSceneNames.EmotionsWorld;
            return scene;
        }

        private static void BuildEmotionScene(Scene scene, EmotionPlaceholderAssets assets)
        {
            GameObject sceneRoot = new("MundoEmociones");
            CreateCamera(sceneRoot.transform);
            CreateEventSystem(sceneRoot.transform);

            Transform systems = CreateEmpty("Systems", sceneRoot.transform).transform;
            GameObject managerObject = CreateEmpty("EmotionGameManager", systems);
            EmotionGameManager manager = managerObject.AddComponent<EmotionGameManager>();
            CreateEmpty("AudioManagerReference", systems);
            CreateEmpty("ProgressManagerReference", systems);
            GameObject navigationObject = CreateEmpty("SceneNavigation", systems);
            SceneNavigation navigation = navigationObject.AddComponent<SceneNavigation>();
            AssignString(navigation, "targetSceneName", MundoAprendoSceneNames.WorldSelection);

            CreateBackground(sceneRoot.transform, assets);

            Canvas canvas = CreateCanvas(sceneRoot.transform);
            RectTransform safeArea = CreateEmptyRect("SafeArea", canvas.transform);
            Stretch(safeArea);

            HeaderReferences header = CreateHeader(safeArea, navigation);
            NunaReferences nuna = CreateNunaGuide(safeArea, assets);
            StartPanelReferences startPanel = CreateStartPanel(safeArea, manager);
            GamePanelReferences gamePanel = CreateGamePanel(safeArea, manager, assets);
            ResultPanelReferences resultPanel = CreateResultPanel(safeArea, manager, navigation, assets);

            Image fadePanel = CreateImage("FadePanel", safeArea, null, new Color(0.04f, 0.06f, 0.1f, 0f));
            Stretch(fadePanel.rectTransform);
            fadePanel.raycastTarget = false;
            fadePanel.transform.SetAsLastSibling();

            CreateEmptyRect("AccessibilityCanvas", canvas.transform);

            Transform audioRoot = CreateEmpty("Audio", sceneRoot.transform).transform;
            AudioSource musicSource = CreateAudioSource("MusicSource", audioRoot, true, 0.28f);
            AudioSource sfxSource = CreateAudioSource("SFXSource", audioRoot, false, 0.75f);

            AssignManagerReferences(
                manager,
                header,
                nuna,
                startPanel,
                gamePanel,
                resultPanel,
                musicSource,
                sfxSource,
                assets);

            startPanel.root.SetActive(true);
            gamePanel.root.SetActive(false);
            resultPanel.root.SetActive(false);
            gamePanel.fearButton.gameObject.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, EmotionScenePath);
        }

        private static void CreateCamera(Transform parent)
        {
            GameObject cameraObject = new("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(parent, false);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.45f, 0.76f, 0.9f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
        }

        private static void CreateEventSystem(Transform parent)
        {
            GameObject eventSystem = new("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(parent, false);
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private static void CreateBackground(Transform parent, EmotionPlaceholderAssets assets)
        {
            Transform background = CreateEmpty("Background", parent).transform;

            SpriteRenderer sky = CreateSpriteRenderer("SkyBackground", background, assets.background, -100);
            sky.transform.localScale = new Vector3(2f, 2f, 1f);

            Transform decorativeClouds = CreateEmpty("DecorativeClouds", background).transform;
            SpriteRenderer cloudLeft = CreateSpriteRenderer("CloudLeft", decorativeClouds, assets.cloud, -80);
            cloudLeft.transform.position = new Vector3(-3.7f, 2.7f, 0f);
            cloudLeft.transform.localScale = Vector3.one * 0.75f;
            SpriteRenderer cloudRight = CreateSpriteRenderer("CloudRight", decorativeClouds, assets.cloud, -80);
            cloudRight.transform.position = new Vector3(3.8f, 3.1f, 0f);
            cloudRight.transform.localScale = Vector3.one * 0.58f;

            SpriteRenderer rainbow = CreateSpriteRenderer("Rainbow", background, assets.rainbow, -90);
            rainbow.transform.position = new Vector3(0.4f, 2.2f, 0f);
            rainbow.transform.localScale = Vector3.one * 1.45f;

            Transform islands = CreateEmpty("FloatingIslands", background).transform;
            SpriteRenderer islandLeft = CreateSpriteRenderer("IslandLeft", islands, assets.island, -70);
            islandLeft.transform.position = new Vector3(-3.8f, -3.25f, 0f);
            islandLeft.transform.localScale = Vector3.one * 0.72f;
            SpriteRenderer islandRight = CreateSpriteRenderer("IslandRight", islands, assets.island, -70);
            islandRight.transform.position = new Vector3(3.9f, -3f, 0f);
            islandRight.transform.localScale = Vector3.one * 0.62f;
        }

        private static Canvas CreateCanvas(Transform parent)
        {
            GameObject canvasObject = new("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static HeaderReferences CreateHeader(RectTransform parent, SceneNavigation navigation)
        {
            RectTransform header = CreatePanel("Header", parent, new Color(1f, 1f, 1f, 0.92f));
            SetAnchors(header, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -75f), new Vector2(0f, 150f));

            Button backButton = CreateCommandButton("BackButton", "Volver", header, new Vector2(190f, 72f), new Color(0.42f, 0.34f, 0.66f, 1f));
            SetFixedRect(backButton.GetComponent<RectTransform>(), new Vector2(-820f, 5f), new Vector2(190f, 72f));
            UnityEventTools.AddPersistentListener(backButton.onClick, navigation.LoadWorldSelection);

            TMP_Text title = CreateText("WorldTitle", "Mundo de las Emociones", header, 40f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetAnchors(title.rectTransform, new Vector2(0.2f, 0.48f), new Vector2(0.8f, 0.95f), Vector2.zero, Vector2.zero);

            TMP_Text instruction = CreateText("InstructionText", "Como se siente?", header, 30f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetAnchors(instruction.rectTransform, new Vector2(0.26f, 0.05f), new Vector2(0.74f, 0.5f), Vector2.zero, Vector2.zero);

            TMP_Text progress = CreateText("ProgressText", "0 / 8", header, 30f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetAnchors(progress.rectTransform, new Vector2(0.82f, 0.2f), new Vector2(0.96f, 0.78f), Vector2.zero, Vector2.zero);

            return new HeaderReferences(header.gameObject, instruction, progress, backButton);
        }

        private static NunaReferences CreateNunaGuide(RectTransform parent, EmotionPlaceholderAssets assets)
        {
            RectTransform guide = CreateEmptyRect("NunaGuide", parent);
            SetAnchors(guide, new Vector2(0.02f, 0.12f), new Vector2(0.29f, 0.84f), Vector2.zero, Vector2.zero);

            Image body = CreateImage("NunaBody", guide, assets.nunaBody, Color.white);
            SetFixedRect(body.rectTransform, new Vector2(0f, 105f), new Vector2(310f, 250f));
            body.preserveAspect = true;

            Image face = CreateImage("NunaFace", guide, assets.nunaFace, Color.white);
            SetFixedRect(face.rectTransform, new Vector2(0f, 105f), new Vector2(310f, 250f));
            face.preserveAspect = true;
            face.raycastTarget = false;

            RectTransform dialogue = CreatePanel("DialoguePanel", guide, Paper);
            SetFixedRect(dialogue, new Vector2(0f, -135f), new Vector2(430f, 150f));
            TMP_Text dialogueText = CreateText("DialogueText", "Hola. Soy Nuna. Vamos a reconocer emociones.", dialogue, 25f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetAnchors(dialogueText.rectTransform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f), Vector2.zero, Vector2.zero);

            return new NunaReferences(guide, body, face, dialogue, dialogueText);
        }

        private static StartPanelReferences CreateStartPanel(RectTransform parent, EmotionGameManager manager)
        {
            RectTransform startPanel = CreatePanel("StartPanel", parent, Paper);
            SetAnchors(startPanel, new Vector2(0.32f, 0.17f), new Vector2(0.94f, 0.81f), Vector2.zero, Vector2.zero);
            CanvasGroup canvasGroup = startPanel.gameObject.AddComponent<CanvasGroup>();

            TMP_Text welcomeTitle = CreateText("WelcomeTitle", "Conoce tus emociones", startPanel, 48f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetAnchors(welcomeTitle.rectTransform, new Vector2(0.08f, 0.66f), new Vector2(0.92f, 0.88f), Vector2.zero, Vector2.zero);

            TMP_Text welcomeInstruction = CreateText("WelcomeInstruction", "Observa cada expresion y elige como se siente.", startPanel, 32f, FontStyles.Normal, TextAlignmentOptions.Center);
            SetAnchors(welcomeInstruction.rectTransform, new Vector2(0.12f, 0.4f), new Vector2(0.88f, 0.65f), Vector2.zero, Vector2.zero);

            Button startButton = CreateCommandButton("StartButton", "INICIAR", startPanel, new Vector2(430f, 118f), Teal);
            SetFixedRect(startButton.GetComponent<RectTransform>(), new Vector2(0f, -145f), new Vector2(430f, 118f));
            UnityEventTools.AddPersistentListener(startButton.onClick, manager.StartActivity);

            return new StartPanelReferences(startPanel.gameObject, canvasGroup, welcomeTitle, welcomeInstruction, startButton);
        }

        private static GamePanelReferences CreateGamePanel(RectTransform parent, EmotionGameManager manager, EmotionPlaceholderAssets assets)
        {
            RectTransform gamePanel = CreateEmptyRect("GamePanel", parent);
            SetAnchors(gamePanel, new Vector2(0.3f, 0.08f), new Vector2(0.97f, 0.84f), Vector2.zero, Vector2.zero);
            CanvasGroup gameCanvasGroup = gamePanel.gameObject.AddComponent<CanvasGroup>();

            RectTransform displayArea = CreatePanel("EmotionDisplayArea", gamePanel, new Color(1f, 1f, 1f, 0.78f));
            SetAnchors(displayArea, new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.98f), Vector2.zero, Vector2.zero);

            EmotionViewReferences[] views =
            {
                CreateEmotionView("Emotion_Alegria", displayArea, assets.joy, new Color(1f, 0.88f, 0.42f, 1f)),
                CreateEmotionView("Emotion_Tristeza", displayArea, assets.sadness, new Color(0.52f, 0.76f, 0.94f, 1f)),
                CreateEmotionView("Emotion_Enojo", displayArea, assets.anger, new Color(0.95f, 0.48f, 0.4f, 1f)),
                CreateEmotionView("Emotion_Sorpresa", displayArea, assets.surprise, new Color(0.72f, 0.58f, 0.9f, 1f)),
                CreateEmotionView("Emotion_Miedo", displayArea, assets.fear, new Color(0.58f, 0.72f, 0.82f, 1f))
            };

            RectTransform feedbackPanel = CreatePanel("FeedbackPanel", gamePanel, new Color(0.98f, 0.98f, 0.94f, 0.96f));
            SetAnchors(feedbackPanel, new Vector2(0.24f, 0.27f), new Vector2(0.76f, 0.42f), Vector2.zero, Vector2.zero);
            CanvasGroup feedbackCanvasGroup = feedbackPanel.gameObject.AddComponent<CanvasGroup>();
            feedbackCanvasGroup.alpha = 0f;

            Image feedbackIcon = CreateImage("FeedbackIcon", feedbackPanel, assets.correctIcon, Color.white);
            SetFixedRect(feedbackIcon.rectTransform, new Vector2(-170f, 0f), new Vector2(72f, 72f));
            feedbackIcon.preserveAspect = true;
            TMP_Text feedbackText = CreateText("FeedbackText", string.Empty, feedbackPanel, 30f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetAnchors(feedbackText.rectTransform, new Vector2(0.2f, 0.1f), new Vector2(0.96f, 0.9f), Vector2.zero, Vector2.zero);

            RectTransform answerButtonsRoot = CreateEmptyRect("AnswerButtons", gamePanel);
            SetAnchors(answerButtonsRoot, new Vector2(0.03f, 0.01f), new Vector2(0.97f, 0.28f), Vector2.zero, Vector2.zero);
            GridLayoutGroup grid = answerButtonsRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(235f, 88f);
            grid.spacing = new Vector2(20f, 14f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.MiddleCenter;

            EmotionAnswerButton joyButton = CreateEmotionAnswerButton("Button_Alegria", "Alegria", EmotionType.Joy, answerButtonsRoot, manager, assets.joy, Gold);
            EmotionAnswerButton sadnessButton = CreateEmotionAnswerButton("Button_Tristeza", "Tristeza", EmotionType.Sadness, answerButtonsRoot, manager, assets.sadness, new Color(0.2f, 0.54f, 0.78f, 1f));
            EmotionAnswerButton angerButton = CreateEmotionAnswerButton("Button_Enojo", "Enojo", EmotionType.Anger, answerButtonsRoot, manager, assets.anger, Coral);
            EmotionAnswerButton surpriseButton = CreateEmotionAnswerButton("Button_Sorpresa", "Sorpresa", EmotionType.Surprise, answerButtonsRoot, manager, assets.surprise, new Color(0.54f, 0.38f, 0.75f, 1f));
            EmotionAnswerButton fearButton = CreateEmotionAnswerButton("Button_Miedo", "Miedo", EmotionType.Fear, answerButtonsRoot, manager, assets.fear, new Color(0.3f, 0.5f, 0.62f, 1f));

            return new GamePanelReferences(
                gamePanel.gameObject,
                gameCanvasGroup,
                displayArea,
                views,
                feedbackPanel.gameObject,
                feedbackCanvasGroup,
                feedbackIcon,
                feedbackText,
                new[] { joyButton, sadnessButton, angerButton, surpriseButton, fearButton },
                fearButton);
        }

        private static EmotionViewReferences CreateEmotionView(string name, Transform parent, Sprite sprite, Color accent)
        {
            RectTransform root = CreateEmptyRect(name, parent);
            Stretch(root);
            CanvasGroup canvasGroup = root.gameObject.AddComponent<CanvasGroup>();

            Image halo = CreateImage("ExpressionHalo", root, null, new Color(accent.r, accent.g, accent.b, 0.22f));
            SetFixedRect(halo.rectTransform, Vector2.zero, new Vector2(430f, 430f));
            halo.raycastTarget = false;

            Image character = CreateImage("Character", root, sprite, Color.white);
            SetFixedRect(character.rectTransform, Vector2.zero, new Vector2(420f, 420f));
            character.preserveAspect = true;
            character.raycastTarget = false;
            return new EmotionViewReferences(root.gameObject, character.rectTransform, canvasGroup, character);
        }

        private static EmotionAnswerButton CreateEmotionAnswerButton(
            string name,
            string label,
            EmotionType emotion,
            Transform parent,
            EmotionGameManager manager,
            Sprite iconSprite,
            Color color)
        {
            GameObject buttonObject = CreateUiObject(name, parent, typeof(Image), typeof(Button), typeof(EmotionAnswerButton));
            Image background = buttonObject.GetComponent<Image>();
            background.color = color;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.12f);
            button.colors = colors;

            Image icon = CreateImage("Icon", buttonObject.transform, iconSprite, Color.white);
            SetAnchors(icon.rectTransform, new Vector2(0.04f, 0.12f), new Vector2(0.34f, 0.88f), Vector2.zero, Vector2.zero);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            TMP_Text text = CreateText("Label", label, buttonObject.transform, 25f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetAnchors(text.rectTransform, new Vector2(0.32f, 0.08f), new Vector2(0.96f, 0.92f), Vector2.zero, Vector2.zero);
            text.color = Color.white;

            EmotionAnswerButton answerButton = buttonObject.GetComponent<EmotionAnswerButton>();
            SerializedObject serialized = new(answerButton);
            serialized.FindProperty("gameManager").objectReferenceValue = manager;
            serialized.FindProperty("emotion").enumValueIndex = (int)emotion;
            serialized.FindProperty("button").objectReferenceValue = button;
            serialized.FindProperty("pulseTarget").objectReferenceValue = buttonObject.GetComponent<RectTransform>();
            serialized.FindProperty("iconImage").objectReferenceValue = icon;
            serialized.FindProperty("labelText").objectReferenceValue = text;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(button.onClick, answerButton.Submit);
            return answerButton;
        }

        private static ResultPanelReferences CreateResultPanel(
            RectTransform parent,
            EmotionGameManager manager,
            SceneNavigation navigation,
            EmotionPlaceholderAssets assets)
        {
            RectTransform resultRoot = CreatePanel("ResultPanel", parent, Paper);
            SetAnchors(resultRoot, new Vector2(0.31f, 0.13f), new Vector2(0.95f, 0.83f), Vector2.zero, Vector2.zero);
            CanvasGroup canvasGroup = resultRoot.gameObject.AddComponent<CanvasGroup>();
            EmotionResultPanel resultPanel = resultRoot.gameObject.AddComponent<EmotionResultPanel>();

            TMP_Text title = CreateText("ResultTitle", "Actividad completada", resultRoot, 46f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetAnchors(title.rectTransform, new Vector2(0.1f, 0.76f), new Vector2(0.9f, 0.94f), Vector2.zero, Vector2.zero);

            TMP_Text score = CreateText("ScoreText", "Aciertos: 0\nErrores: 0", resultRoot, 30f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetAnchors(score.rectTransform, new Vector2(0.15f, 0.5f), new Vector2(0.85f, 0.75f), Vector2.zero, Vector2.zero);

            RectTransform starsContainer = CreateHorizontalGroup("StarsContainer", resultRoot, 22f);
            SetAnchors(starsContainer, new Vector2(0.27f, 0.34f), new Vector2(0.73f, 0.52f), Vector2.zero, Vector2.zero);
            Image[] stars = new Image[3];
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i] = CreateLayoutImage($"Star{i + 1}", starsContainer, assets.emptyStar, Color.white, new Vector2(94f, 94f));
                stars[i].preserveAspect = true;
            }

            RectTransform buttonRow = CreateHorizontalGroup("ResultButtons", resultRoot, 34f);
            SetAnchors(buttonRow, new Vector2(0.12f, 0.08f), new Vector2(0.88f, 0.28f), Vector2.zero, Vector2.zero);
            Button retry = CreateCommandButton("RetryButton", "Repetir", buttonRow, new Vector2(300f, 92f), Teal);
            Button worlds = CreateCommandButton("WorldsButton", "Mundos", buttonRow, new Vector2(300f, 92f), new Color(0.42f, 0.34f, 0.66f, 1f));
            UnityEventTools.AddPersistentListener(retry.onClick, manager.RestartActivity);
            UnityEventTools.AddPersistentListener(worlds.onClick, navigation.LoadWorldSelection);

            SerializedObject serialized = new(resultPanel);
            serialized.FindProperty("rootObject").objectReferenceValue = resultRoot.gameObject;
            serialized.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            serialized.FindProperty("resultTitle").objectReferenceValue = title;
            serialized.FindProperty("scoreText").objectReferenceValue = score;
            serialized.FindProperty("fullStarSprite").objectReferenceValue = assets.fullStar;
            serialized.FindProperty("emptyStarSprite").objectReferenceValue = assets.emptyStar;
            SerializedProperty starArray = serialized.FindProperty("starImages");
            starArray.arraySize = stars.Length;
            for (int i = 0; i < stars.Length; i++) starArray.GetArrayElementAtIndex(i).objectReferenceValue = stars[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return new ResultPanelReferences(resultRoot.gameObject, resultPanel, canvasGroup, title, score, stars, retry, worlds);
        }

        private static void AssignManagerReferences(
            EmotionGameManager manager,
            HeaderReferences header,
            NunaReferences nuna,
            StartPanelReferences startPanel,
            GamePanelReferences gamePanel,
            ResultPanelReferences resultPanel,
            AudioSource musicSource,
            AudioSource sfxSource,
            EmotionPlaceholderAssets assets)
        {
            SerializedObject serialized = new(manager);
            SerializedProperty views = serialized.FindProperty("emotionViews");
            views.arraySize = 5;
            ConfigureEmotionView(views.GetArrayElementAtIndex(0), EmotionType.Joy, "Alegria", gamePanel.views[0]);
            ConfigureEmotionView(views.GetArrayElementAtIndex(1), EmotionType.Sadness, "Tristeza", gamePanel.views[1]);
            ConfigureEmotionView(views.GetArrayElementAtIndex(2), EmotionType.Anger, "Enojo", gamePanel.views[2]);
            ConfigureEmotionView(views.GetArrayElementAtIndex(3), EmotionType.Surprise, "Sorpresa", gamePanel.views[3]);
            ConfigureEmotionView(views.GetArrayElementAtIndex(4), EmotionType.Fear, "Miedo", gamePanel.views[4]);

            SerializedProperty buttons = serialized.FindProperty("answerButtons");
            buttons.arraySize = gamePanel.answerButtons.Length;
            for (int i = 0; i < gamePanel.answerButtons.Length; i++)
            {
                buttons.GetArrayElementAtIndex(i).objectReferenceValue = gamePanel.answerButtons[i];
            }

            serialized.FindProperty("includeFear").boolValue = false;
            serialized.FindProperty("totalRounds").intValue = 8;
            serialized.FindProperty("startPanel").objectReferenceValue = startPanel.root;
            serialized.FindProperty("gamePanel").objectReferenceValue = gamePanel.root;
            serialized.FindProperty("gamePanelCanvasGroup").objectReferenceValue = gamePanel.canvasGroup;
            serialized.FindProperty("feedbackPanel").objectReferenceValue = gamePanel.feedbackPanel;
            serialized.FindProperty("feedbackCanvasGroup").objectReferenceValue = gamePanel.feedbackCanvasGroup;
            serialized.FindProperty("resultPanel").objectReferenceValue = resultPanel.component;
            serialized.FindProperty("instructionText").objectReferenceValue = header.instructionText;
            serialized.FindProperty("progressText").objectReferenceValue = header.progressText;
            serialized.FindProperty("feedbackText").objectReferenceValue = gamePanel.feedbackText;
            serialized.FindProperty("nunaDialogueText").objectReferenceValue = nuna.dialogueText;
            serialized.FindProperty("feedbackIcon").objectReferenceValue = gamePanel.feedbackIcon;
            serialized.FindProperty("correctIconSprite").objectReferenceValue = assets.correctIcon;
            serialized.FindProperty("retryIconSprite").objectReferenceValue = assets.retryIcon;
            serialized.FindProperty("nunaRoot").objectReferenceValue = nuna.root;
            serialized.FindProperty("musicSource").objectReferenceValue = musicSource;
            serialized.FindProperty("sfxSource").objectReferenceValue = sfxSource;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureEmotionView(SerializedProperty property, EmotionType emotion, string displayName, EmotionViewReferences view)
        {
            property.FindPropertyRelative("emotion").enumValueIndex = (int)emotion;
            property.FindPropertyRelative("rootObject").objectReferenceValue = view.root;
            property.FindPropertyRelative("animatedRect").objectReferenceValue = view.animatedRect;
            property.FindPropertyRelative("canvasGroup").objectReferenceValue = view.canvasGroup;
            property.FindPropertyRelative("displayName").stringValue = displayName;
        }

        private static void ConfigureWorldSelection()
        {
            SceneAsset selectionAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(WorldSelectionScenePath);
            if (selectionAsset == null)
            {
                Debug.LogError($"No se encontro {WorldSelectionScenePath}.");
                return;
            }

            Scene selectionScene = EditorSceneManager.OpenScene(WorldSelectionScenePath, OpenSceneMode.Single);
            WorldSelectionManager manager = GetComponentInScene<WorldSelectionManager>(selectionScene);
            if (manager == null)
            {
                Debug.LogError("SeleccionMundos no contiene WorldSelectionManager.");
                return;
            }

            SerializedObject serialized = new(manager);
            SerializedProperty worlds = serialized.FindProperty("worlds");
            if (worlds == null || worlds.arraySize < 4)
            {
                Debug.LogError("WorldSelectionManager necesita cuatro mundos configurados.", manager);
                return;
            }

            ConfigureWorldRoute(worlds.GetArrayElementAtIndex(0), "Mundo Musical", MundoAprendoSceneNames.MusicalWorld);
            ConfigureWorldRoute(worlds.GetArrayElementAtIndex(1), "Mundo de Cuentos", MundoAprendoSceneNames.StoriesWorld);
            ConfigureWorldRoute(worlds.GetArrayElementAtIndex(2), "Mundo de Tamanos", MundoAprendoSceneNames.SizesWorld);
            ConfigureWorldRoute(worlds.GetArrayElementAtIndex(3), "Mundo de Emociones", MundoAprendoSceneNames.EmotionsWorld);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(selectionScene);
            EditorSceneManager.SaveScene(selectionScene);
        }

        private static void ConfigureWorldRoute(SerializedProperty world, string displayName, string sceneName)
        {
            world.FindPropertyRelative("worldName").stringValue = displayName;
            world.FindPropertyRelative("sceneName").stringValue = sceneName;
        }

        private static void ConfigureBuildSettings()
        {
            List<EditorBuildSettingsScene> configured = new();
            HashSet<string> seenPaths = new(StringComparer.OrdinalIgnoreCase);

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (string.IsNullOrWhiteSpace(scene.path) || !seenPaths.Add(scene.path)) continue;
                configured.Add(scene);
            }

            foreach (string requiredPath in RequiredScenePaths)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(requiredPath) == null)
                {
                    Debug.LogError($"Build Settings: falta la escena requerida {requiredPath}.");
                    continue;
                }

                int index = configured.FindIndex(scene => string.Equals(scene.path, requiredPath, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    configured[index] = new EditorBuildSettingsScene(requiredPath, true);
                }
                else
                {
                    configured.Add(new EditorBuildSettingsScene(requiredPath, true));
                }
            }

            EditorBuildSettings.scenes = configured.ToArray();
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Scenes", "Mundos");
            EnsureFolder("Assets/Mundo Aprendo", "Art");
            EnsureFolder("Assets/Mundo Aprendo/Art", "Placeholders");
            EnsureFolder("Assets/Mundo Aprendo/Art/Placeholders", "Emociones");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }

        private static EmotionPlaceholderAssets LoadOrCreatePlaceholderAssets()
        {
            return new EmotionPlaceholderAssets
            {
                background = CreateBackgroundSprite(),
                cloud = CreateCloudSprite("DecorativeCloud", new Color(0.95f, 0.99f, 1f, 0.82f)),
                rainbow = CreateRainbowSprite(),
                island = CreateIslandSprite(),
                nunaBody = CreateCloudSprite("NunaBody", new Color(0.9f, 0.98f, 1f, 1f)),
                nunaFace = CreateNunaFaceSprite(),
                joy = CreateEmotionSprite("Emotion_Joy", EmotionType.Joy),
                sadness = CreateEmotionSprite("Emotion_Sadness", EmotionType.Sadness),
                anger = CreateEmotionSprite("Emotion_Anger", EmotionType.Anger),
                surprise = CreateEmotionSprite("Emotion_Surprise", EmotionType.Surprise),
                fear = CreateEmotionSprite("Emotion_Fear", EmotionType.Fear),
                correctIcon = CreateFeedbackIcon("Icon_Correct", true),
                retryIcon = CreateFeedbackIcon("Icon_Retry", false),
                fullStar = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Mundo Aprendo/Imagenes/Prefabs/Star_Full.png"),
                emptyStar = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Mundo Aprendo/Imagenes/Prefabs/Star_Empty.png")
            };
        }

        private static Sprite CreateBackgroundSprite()
        {
            string path = $"{PlaceholderFolder}/Background_Sky.png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            const int width = 960;
            const int height = 540;
            Texture2D texture = NewTransparentTexture(width, height);
            Color bottom = new(0.65f, 0.88f, 0.95f, 1f);
            Color top = new(0.25f, 0.62f, 0.86f, 1f);
            for (int y = 0; y < height; y++)
            {
                Color rowColor = Color.Lerp(bottom, top, y / (float)(height - 1));
                for (int x = 0; x < width; x++) texture.SetPixel(x, y, rowColor);
            }

            DrawCircle(texture, 110, 430, 54, new Color(1f, 0.88f, 0.42f, 0.9f));
            return SaveTextureAsSprite(texture, path, 100f);
        }

        private static Sprite CreateCloudSprite(string name, Color color)
        {
            string path = $"{PlaceholderFolder}/{name}.png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            Texture2D texture = NewTransparentTexture(512, 320);
            DrawCircle(texture, 152, 145, 82, color);
            DrawCircle(texture, 248, 190, 112, color);
            DrawCircle(texture, 356, 150, 88, color);
            DrawRoundedRect(texture, 92, 80, 330, 120, 55, color);
            return SaveTextureAsSprite(texture, path, 100f);
        }

        private static Sprite CreateNunaFaceSprite()
        {
            string path = $"{PlaceholderFolder}/NunaFace.png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            Texture2D texture = NewTransparentTexture(512, 320);
            DrawCircle(texture, 210, 170, 18, Color.white);
            DrawCircle(texture, 302, 170, 18, Color.white);
            DrawCircle(texture, 212, 170, 9, Ink);
            DrawCircle(texture, 304, 170, 9, Ink);
            DrawCircle(texture, 168, 124, 22, new Color(1f, 0.55f, 0.66f, 0.72f));
            DrawCircle(texture, 344, 124, 22, new Color(1f, 0.55f, 0.66f, 0.72f));
            DrawArc(texture, 256, 132, 50, 205f, 335f, 8, Ink);
            return SaveTextureAsSprite(texture, path, 100f);
        }

        private static Sprite CreateRainbowSprite()
        {
            string path = $"{PlaceholderFolder}/Rainbow.png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            Texture2D texture = NewTransparentTexture(640, 360);
            Color[] colors =
            {
                new(0.9f, 0.22f, 0.22f, 0.72f),
                new(1f, 0.58f, 0.18f, 0.72f),
                new(1f, 0.84f, 0.22f, 0.72f),
                new(0.2f, 0.7f, 0.38f, 0.72f),
                new(0.24f, 0.55f, 0.9f, 0.72f),
                new(0.55f, 0.35f, 0.78f, 0.72f)
            };

            for (int i = 0; i < colors.Length; i++)
            {
                DrawArc(texture, 320, 45, 270 - i * 22, 0f, 180f, 18, colors[i]);
            }

            return SaveTextureAsSprite(texture, path, 100f);
        }

        private static Sprite CreateIslandSprite()
        {
            string path = $"{PlaceholderFolder}/FloatingIsland.png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            Texture2D texture = NewTransparentTexture(512, 300);
            DrawRoundedRect(texture, 58, 150, 396, 72, 34, new Color(0.28f, 0.68f, 0.34f, 1f));
            DrawTriangle(texture, new Vector2(76, 170), new Vector2(436, 170), new Vector2(256, 28), new Color(0.48f, 0.3f, 0.18f, 1f));
            DrawCircle(texture, 150, 220, 24, new Color(0.2f, 0.55f, 0.25f, 1f));
            DrawCircle(texture, 360, 225, 30, new Color(0.2f, 0.55f, 0.25f, 1f));
            return SaveTextureAsSprite(texture, path, 100f);
        }

        private static Sprite CreateEmotionSprite(string name, EmotionType emotion)
        {
            string path = $"{PlaceholderFolder}/{name}.png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            Texture2D texture = NewTransparentTexture(512, 512);
            Color faceColor = emotion switch
            {
                EmotionType.Joy => new Color(1f, 0.78f, 0.28f, 1f),
                EmotionType.Sadness => new Color(0.56f, 0.78f, 0.95f, 1f),
                EmotionType.Anger => new Color(0.94f, 0.46f, 0.4f, 1f),
                EmotionType.Surprise => new Color(0.74f, 0.62f, 0.92f, 1f),
                _ => new Color(0.66f, 0.8f, 0.86f, 1f)
            };

            DrawCircle(texture, 256, 256, 188, faceColor);
            DrawCircle(texture, 184, 292, 28, Color.white);
            DrawCircle(texture, 328, 292, 28, Color.white);
            DrawCircle(texture, 184, 292, emotion == EmotionType.Surprise || emotion == EmotionType.Fear ? 14 : 10, Ink);
            DrawCircle(texture, 328, 292, emotion == EmotionType.Surprise || emotion == EmotionType.Fear ? 14 : 10, Ink);

            switch (emotion)
            {
                case EmotionType.Joy:
                    DrawArc(texture, 256, 226, 80, 205f, 335f, 12, Ink);
                    DrawCircle(texture, 132, 220, 28, new Color(1f, 0.42f, 0.5f, 0.58f));
                    DrawCircle(texture, 380, 220, 28, new Color(1f, 0.42f, 0.5f, 0.58f));
                    break;
                case EmotionType.Sadness:
                    DrawArc(texture, 256, 150, 70, 25f, 155f, 11, Ink);
                    DrawTriangle(texture, new Vector2(184, 252), new Vector2(166, 205), new Vector2(202, 205), new Color(0.2f, 0.58f, 0.92f, 0.9f));
                    DrawLine(texture, 150, 344, 212, 330, 10, Ink);
                    DrawLine(texture, 300, 330, 362, 344, 10, Ink);
                    break;
                case EmotionType.Anger:
                    DrawLine(texture, 142, 344, 216, 316, 13, Ink);
                    DrawLine(texture, 296, 316, 370, 344, 13, Ink);
                    DrawLine(texture, 190, 188, 322, 188, 13, Ink);
                    DrawCircle(texture, 132, 222, 26, new Color(0.78f, 0.12f, 0.12f, 0.42f));
                    DrawCircle(texture, 380, 222, 26, new Color(0.78f, 0.12f, 0.12f, 0.42f));
                    break;
                case EmotionType.Surprise:
                    DrawCircle(texture, 256, 188, 42, Ink);
                    DrawCircle(texture, 256, 194, 22, new Color(0.35f, 0.18f, 0.42f, 1f));
                    DrawLine(texture, 148, 362, 216, 362, 10, Ink);
                    DrawLine(texture, 296, 362, 364, 362, 10, Ink);
                    break;
                case EmotionType.Fear:
                    DrawArc(texture, 256, 188, 44, 25f, 155f, 9, Ink);
                    DrawLine(texture, 146, 356, 216, 338, 9, Ink);
                    DrawLine(texture, 296, 338, 366, 356, 9, Ink);
                    DrawCircle(texture, 126, 220, 20, new Color(0.5f, 0.38f, 0.68f, 0.35f));
                    DrawCircle(texture, 386, 220, 20, new Color(0.5f, 0.38f, 0.68f, 0.35f));
                    break;
            }

            return SaveTextureAsSprite(texture, path, 100f);
        }

        private static Sprite CreateFeedbackIcon(string name, bool correct)
        {
            string path = $"{PlaceholderFolder}/{name}.png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            Texture2D texture = NewTransparentTexture(192, 192);
            Color circleColor = correct ? new Color(0.2f, 0.7f, 0.38f, 1f) : new Color(0.28f, 0.55f, 0.82f, 1f);
            DrawCircle(texture, 96, 96, 78, circleColor);
            if (correct)
            {
                DrawLine(texture, 52, 96, 82, 66, 14, Color.white);
                DrawLine(texture, 82, 66, 142, 130, 14, Color.white);
            }
            else
            {
                DrawArc(texture, 96, 96, 46, 45f, 320f, 13, Color.white);
                DrawTriangle(texture, new Vector2(126, 145), new Vector2(158, 144), new Vector2(146, 116), Color.white);
            }

            return SaveTextureAsSprite(texture, path, 100f);
        }

        private static Texture2D NewTransparentTexture(int width, int height)
        {
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false);
            Color[] clearPixels = new Color[width * height];
            texture.SetPixels(clearPixels);
            return texture;
        }

        private static Sprite SaveTextureAsSprite(Texture2D texture, string path, float pixelsPerUnit)
        {
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void DrawCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
        {
            int radiusSquared = radius * radius;
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    if (x < 0 || x >= texture.width || y < 0 || y >= texture.height) continue;
                    int dx = x - centerX;
                    int dy = y - centerY;
                    if (dx * dx + dy * dy <= radiusSquared) texture.SetPixel(x, y, color);
                }
            }
        }

        private static void DrawRoundedRect(Texture2D texture, int x, int y, int width, int height, int radius, Color color)
        {
            DrawRect(texture, x + radius, y, width - radius * 2, height, color);
            DrawRect(texture, x, y + radius, width, height - radius * 2, color);
            DrawCircle(texture, x + radius, y + radius, radius, color);
            DrawCircle(texture, x + width - radius, y + radius, radius, color);
            DrawCircle(texture, x + radius, y + height - radius, radius, color);
            DrawCircle(texture, x + width - radius, y + height - radius, radius, color);
        }

        private static void DrawRect(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            int minX = Mathf.Clamp(x, 0, texture.width);
            int maxX = Mathf.Clamp(x + width, 0, texture.width);
            int minY = Mathf.Clamp(y, 0, texture.height);
            int maxY = Mathf.Clamp(y + height, 0, texture.height);
            for (int yy = minY; yy < maxY; yy++)
            {
                for (int xx = minX; xx < maxX; xx++) texture.SetPixel(xx, yy, color);
            }
        }

        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, int thickness, Color color)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            for (int i = 0; i <= steps; i++)
            {
                float t = steps == 0 ? 0f : i / (float)steps;
                DrawCircle(texture, Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)), Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)), thickness / 2, color);
            }
        }

        private static void DrawArc(Texture2D texture, int centerX, int centerY, int radius, float startDegrees, float endDegrees, int thickness, Color color)
        {
            int steps = Mathf.Max(24, Mathf.CeilToInt(Mathf.Abs(endDegrees - startDegrees) * 1.5f));
            Vector2 previous = Vector2.zero;
            for (int i = 0; i <= steps; i++)
            {
                float angle = Mathf.Lerp(startDegrees, endDegrees, i / (float)steps) * Mathf.Deg2Rad;
                Vector2 point = new(centerX + Mathf.Cos(angle) * radius, centerY + Mathf.Sin(angle) * radius);
                if (i > 0) DrawLine(texture, Mathf.RoundToInt(previous.x), Mathf.RoundToInt(previous.y), Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y), thickness, color);
                previous = point;
            }
        }

        private static void DrawTriangle(Texture2D texture, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, b.x, c.x)), 0, texture.width - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, b.x, c.x)), 0, texture.width - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, b.y, c.y)), 0, texture.height - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, b.y, c.y)), 0, texture.height - 1);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 p = new(x, y);
                    float d1 = Sign(p, a, b);
                    float d2 = Sign(p, b, c);
                    float d3 = Sign(p, c, a);
                    bool hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
                    bool hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
                    if (!(hasNegative && hasPositive)) texture.SetPixel(x, y, color);
                }
            }
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        private static SpriteRenderer CreateSpriteRenderer(string name, Transform parent, Sprite sprite, int sortingOrder)
        {
            GameObject gameObject = new(name, typeof(SpriteRenderer));
            gameObject.transform.SetParent(parent, false);
            SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static AudioSource CreateAudioSource(string name, Transform parent, bool loop, float volume)
        {
            GameObject audioObject = new(name, typeof(AudioSource));
            audioObject.transform.SetParent(parent, false);
            AudioSource source = audioObject.GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.volume = volume;
            return source;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            Image image = CreateImage(name, parent, null, color);
            return image.rectTransform;
        }

        private static Button CreateCommandButton(string name, string label, Transform parent, Vector2 size, Color color)
        {
            GameObject buttonObject = CreateUiObject(name, parent, typeof(Image), typeof(Button), typeof(LayoutElement));
            Image image = buttonObject.GetComponent<Image>();
            image.color = color;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.12f);
            button.colors = colors;

            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;
            buttonObject.GetComponent<RectTransform>().sizeDelta = size;

            TMP_Text text = CreateText("Label", label, buttonObject.transform, 28f, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            text.color = Color.white;
            return button;
        }

        private static TMP_Text CreateText(string name, string text, Transform parent, float fontSize, FontStyles style, TextAlignmentOptions alignment)
        {
            GameObject textObject = CreateUiObject(name, parent, typeof(TextMeshProUGUI));
            TMP_Text tmp = textObject.GetComponent<TMP_Text>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = Ink;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            GameObject imageObject = CreateUiObject(name, parent, typeof(Image));
            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return image;
        }

        private static Image CreateLayoutImage(string name, Transform parent, Sprite sprite, Color color, Vector2 size)
        {
            GameObject imageObject = CreateUiObject(name, parent, typeof(Image), typeof(LayoutElement));
            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            imageObject.GetComponent<RectTransform>().sizeDelta = size;
            LayoutElement layout = imageObject.GetComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;
            return image;
        }

        private static RectTransform CreateHorizontalGroup(string name, Transform parent, float spacing)
        {
            GameObject row = CreateUiObject(name, parent, typeof(HorizontalLayoutGroup));
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return row.GetComponent<RectTransform>();
        }

        private static GameObject CreateUiObject(string name, Transform parent, params Type[] components)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(CanvasRenderer));
            foreach (Type component in components)
            {
                if (gameObject.GetComponent(component) == null) gameObject.AddComponent(component);
            }

            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static GameObject CreateEmpty(string name, Transform parent)
        {
            GameObject gameObject = new(name);
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static RectTransform CreateEmptyRect(string name, Transform parent)
        {
            return CreateUiObject(name, parent).GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetFixedRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            if (size == Vector2.zero)
            {
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }

        private static GameObject GetRootByName(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root;
            }

            return null;
        }

        private static T GetComponentInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null) return component;
            }

            return null;
        }

        private static void AssignString(UnityEngine.Object target, string propertyName, string value)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null) property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private sealed class EmotionPlaceholderAssets
        {
            public Sprite background;
            public Sprite cloud;
            public Sprite rainbow;
            public Sprite island;
            public Sprite nunaBody;
            public Sprite nunaFace;
            public Sprite joy;
            public Sprite sadness;
            public Sprite anger;
            public Sprite surprise;
            public Sprite fear;
            public Sprite correctIcon;
            public Sprite retryIcon;
            public Sprite fullStar;
            public Sprite emptyStar;
        }

        private readonly struct HeaderReferences
        {
            public HeaderReferences(GameObject root, TMP_Text instructionText, TMP_Text progressText, Button backButton)
            {
                this.root = root;
                this.instructionText = instructionText;
                this.progressText = progressText;
                this.backButton = backButton;
            }

            public readonly GameObject root;
            public readonly TMP_Text instructionText;
            public readonly TMP_Text progressText;
            public readonly Button backButton;
        }

        private readonly struct NunaReferences
        {
            public NunaReferences(RectTransform root, Image body, Image face, RectTransform dialoguePanel, TMP_Text dialogueText)
            {
                this.root = root;
                this.body = body;
                this.face = face;
                this.dialoguePanel = dialoguePanel;
                this.dialogueText = dialogueText;
            }

            public readonly RectTransform root;
            public readonly Image body;
            public readonly Image face;
            public readonly RectTransform dialoguePanel;
            public readonly TMP_Text dialogueText;
        }

        private readonly struct StartPanelReferences
        {
            public StartPanelReferences(GameObject root, CanvasGroup canvasGroup, TMP_Text title, TMP_Text instruction, Button startButton)
            {
                this.root = root;
                this.canvasGroup = canvasGroup;
                this.title = title;
                this.instruction = instruction;
                this.startButton = startButton;
            }

            public readonly GameObject root;
            public readonly CanvasGroup canvasGroup;
            public readonly TMP_Text title;
            public readonly TMP_Text instruction;
            public readonly Button startButton;
        }

        private readonly struct EmotionViewReferences
        {
            public EmotionViewReferences(GameObject root, RectTransform animatedRect, CanvasGroup canvasGroup, Image image)
            {
                this.root = root;
                this.animatedRect = animatedRect;
                this.canvasGroup = canvasGroup;
                this.image = image;
            }

            public readonly GameObject root;
            public readonly RectTransform animatedRect;
            public readonly CanvasGroup canvasGroup;
            public readonly Image image;
        }

        private readonly struct GamePanelReferences
        {
            public GamePanelReferences(
                GameObject root,
                CanvasGroup canvasGroup,
                RectTransform displayArea,
                EmotionViewReferences[] views,
                GameObject feedbackPanel,
                CanvasGroup feedbackCanvasGroup,
                Image feedbackIcon,
                TMP_Text feedbackText,
                EmotionAnswerButton[] answerButtons,
                EmotionAnswerButton fearButton)
            {
                this.root = root;
                this.canvasGroup = canvasGroup;
                this.displayArea = displayArea;
                this.views = views;
                this.feedbackPanel = feedbackPanel;
                this.feedbackCanvasGroup = feedbackCanvasGroup;
                this.feedbackIcon = feedbackIcon;
                this.feedbackText = feedbackText;
                this.answerButtons = answerButtons;
                this.fearButton = fearButton;
            }

            public readonly GameObject root;
            public readonly CanvasGroup canvasGroup;
            public readonly RectTransform displayArea;
            public readonly EmotionViewReferences[] views;
            public readonly GameObject feedbackPanel;
            public readonly CanvasGroup feedbackCanvasGroup;
            public readonly Image feedbackIcon;
            public readonly TMP_Text feedbackText;
            public readonly EmotionAnswerButton[] answerButtons;
            public readonly EmotionAnswerButton fearButton;
        }

        private readonly struct ResultPanelReferences
        {
            public ResultPanelReferences(
                GameObject root,
                EmotionResultPanel component,
                CanvasGroup canvasGroup,
                TMP_Text title,
                TMP_Text score,
                Image[] stars,
                Button retryButton,
                Button worldsButton)
            {
                this.root = root;
                this.component = component;
                this.canvasGroup = canvasGroup;
                this.title = title;
                this.score = score;
                this.stars = stars;
                this.retryButton = retryButton;
                this.worldsButton = worldsButton;
            }

            public readonly GameObject root;
            public readonly EmotionResultPanel component;
            public readonly CanvasGroup canvasGroup;
            public readonly TMP_Text title;
            public readonly TMP_Text score;
            public readonly Image[] stars;
            public readonly Button retryButton;
            public readonly Button worldsButton;
        }
    }
}
