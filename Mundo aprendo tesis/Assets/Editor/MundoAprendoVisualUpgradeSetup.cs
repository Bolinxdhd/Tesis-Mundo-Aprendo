#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SurpriseBox.Scripts;
using Bolin;
using Jsgaona;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bolin.Editor
{
    public static class MundoAprendoVisualUpgradeSetup
    {
        private const string MenuScenePath = "Assets/Scenes/Menu.unity";
        private const string WorldSelectionScenePath = "Assets/Scenes/SeleccionMundos.unity";
        private const string MusicScenePath = "Assets/Scenes/MundoMusical.unity";
        private const string StoryScenePath = "Assets/Scenes/MundoCuentos_VozTest.unity";
        private const string SizeScenePath = "Assets/Scenes/MundoTamanos.unity";
        private const string EmotionScenePath = "Assets/Scenes/Mundos/MundoEmociones.unity";

        private const string LuliPath = "Assets/Mundo Aprendo/Personajes/Luli/Luli.png";
        private const string TamborcinPath = "Assets/Mundo Aprendo/Personajes/Tico El Tamborcito/tamborcin.png";
        private const string BiblioPath = "Assets/Mundo Aprendo/Personajes/BIBLIO EL Buho/BIBLIO EL Buho.png";
        private const string ExploradorPath = "Assets/Mundo Aprendo/Personajes/Guia/Guia.png";
        private const string NunaPath = "Assets/Mundo Aprendo/Personajes/Nuna la nube/Nuna la Nube.png";

        private const string MusicBackgroundPath = "Assets/Mundo Aprendo/Fondos/Mundo Musical/Mundo musical.png";
        private const string StoryBackgroundPath = "Assets/Mundo Aprendo/Fondos/Mundo biblioteca/Biblioteca.png";
        private const string EmotionBackgroundPath = "Assets/Mundo Aprendo/Fondos/Mundo de los sentimiento/Mundo sentimientos.png";
        private const string SpaceBackgroundPath = "Assets/Space_Exploration_GUI_Kit/Background_Images/extra large/home-background-extra-large.png";
        private const string FallbackSpaceBackgroundPath = "Assets/Mundo Aprendo/Imagenes/Baner fondo.png";

        private const string PanelSpritePath = "Assets/Cartoon UI/Panels/Panel Light.png";
        private const string ButtonGreenPath = "Assets/Cartoon UI/Buttons/Long Round/Long Round Green.png";
        private const string ButtonSkybluePath = "Assets/Cartoon UI/Buttons/Long Round/Long Round Skyblue.png";
        private const string ButtonYellowPath = "Assets/Cartoon UI/Buttons/Long Round/Long Round Yellow.png";
        private const string ButtonOrangePath = "Assets/Cartoon UI/Buttons/Long Round/Long Round Orange.png";
        private const string ButtonPurplePath = "Assets/Cartoon UI/Buttons/Long Round/Long Round Purple.png";
        private const string FullStarPath = "Assets/Cartoon UI/Icons/Star.png";
        private const string EmptyStarPath = "Assets/Cartoon UI/Icons/Star Off.png";
        private const string LockPath = "Assets/Cartoon UI/Icons/Gold Lock.png";

        private static readonly string[] PlanetPaths =
        {
            "Assets/Mundo Aprendo/Imagenes/Prefabs/Planet_0.png",
            "Assets/Mundo Aprendo/Imagenes/Prefabs/Planet_1.png",
            "Assets/Mundo Aprendo/Imagenes/Prefabs/Planet_2.png",
            "Assets/Mundo Aprendo/Imagenes/Prefabs/Planet_3.png"
        };

        private static readonly string[] WorldIconPaths =
        {
            "Assets/Space_Exploration_GUI_Kit/Picto_Icons/White/music-128.png",
            "Assets/Space_Exploration_GUI_Kit/Picto_Icons/White/book-128.png",
            "Assets/Space_Exploration_GUI_Kit/Picto_Icons/White/telescope-128.png",
            "Assets/Space_Exploration_GUI_Kit/Picto_Icons/White/heart-128.png"
        };

        private static readonly Color TextDark = new(0.21f, 0.18f, 0.34f, 1f);
        private static readonly Color TextLight = new(1f, 0.98f, 0.9f, 1f);
        private static readonly Color SoftBlue = new(0.45f, 0.73f, 0.88f, 1f);
        private static readonly Color SoftGreen = new(0.55f, 0.78f, 0.55f, 1f);
        private static readonly Color SoftYellow = new(0.98f, 0.8f, 0.38f, 1f);
        private static readonly Color SoftPurple = new(0.58f, 0.5f, 0.78f, 1f);

        [MenuItem("Mundo Aprendo/Aplicar mejora visual de mundos")]
        public static void ApplyFromMenu()
        {
            ApplyAll();
        }

        public static void ApplyAll()
        {
            PatchScene(MenuScenePath, PatchMenu);
            PatchScene(WorldSelectionScenePath, PatchWorldSelection);
            PatchScene(MusicScenePath, PatchMusicWorld);
            PatchScene(StoryScenePath, PatchStoryWorld);
            PatchScene(SizeScenePath, PatchSizeWorld);
            PatchScene(EmotionScenePath, PatchEmotionWorld);
            AssetDatabase.SaveAssets();
            Debug.Log("MundoAprendoVisualUpgradeSetup: mejoras visuales persistidas en las seis escenas.");
        }

        private static void PatchScene(string scenePath, Action<Scene> patch)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            patch(scene);
            ConfigureCanvasScalers(scene);
            EnsureSingleEventSystem(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"No se pudo guardar la escena {scenePath}.");
            }
        }

        private static void PatchMenu(Scene scene)
        {
            Canvas canvas = EnsureCanvas(scene, "Canvas");
            Sprite background = LoadSprite(SpaceBackgroundPath) ?? LoadSprite(FallbackSpaceBackgroundPath);
            EnsureBackground(canvas.transform, "FondoMenuEspacial", background, new Color(1f, 1f, 1f, 0.92f), false);
            EnsureCompanionImage(canvas.transform, "LuliMenuVisual", LoadSprite(LuliPath), new Vector2(0.06f, 0f), new Vector2(0.29f, 0.43f));

            StyleSceneButtons(scene);
            EnsureSceneDialogue(
                scene,
                "Luli",
                LoadSprite(LuliPath),
                new[]
                {
                    "¡Hola! Soy Luli. Bienvenido a Mundo Aprendo.",
                    "Pulsa Jugar para visitar los mundos y ganar estrellas."
                },
                "Explorar",
                null);
        }

        private static void PatchWorldSelection(Scene scene)
        {
            Canvas canvas = EnsureCanvas(scene, "Canvas - Seleccion Mundos");
            WorldSelectionManager manager = ComponentsInScene<WorldSelectionManager>(scene).FirstOrDefault();
            if (manager == null)
            {
                GameObject managerObject = new("WorldSelectionManager", typeof(WorldSelectionManager));
                manager = managerObject.GetComponent<WorldSelectionManager>();
            }

            DestroyIfExists(scene, "VisualSeleccionMundos");
            RectTransform root = CreateUiObject("VisualSeleccionMundos", canvas.transform).GetComponent<RectTransform>();
            Stretch(root);
            root.SetAsLastSibling();

            Sprite background = LoadSprite(SpaceBackgroundPath) ?? LoadSprite(FallbackSpaceBackgroundPath);
            Image backgroundImage = CreateImage("FondoSeleccionEspacial", root, background, new Color(0.92f, 0.96f, 1f, 1f), true);
            Stretch(backgroundImage.rectTransform);
            backgroundImage.preserveAspect = false;

            AddDecorativeStars(root);

            TMP_Text title = CreateText("TituloSeleccionMundos", "Elige un mundo", root, 54f, FontStyles.Bold, TextAlignmentOptions.Center, TextDark);
            SetAnchors(title.rectTransform, new Vector2(0.18f, 0.86f), new Vector2(0.82f, 0.96f), Vector2.zero, Vector2.zero);

            TMP_Text messageText = CreateText("MensajeSeleccionMundos", string.Empty, root, 30f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.44f, 0.18f, 0.36f, 1f));
            SetAnchors(messageText.rectTransform, new Vector2(0.22f, 0.08f), new Vector2(0.78f, 0.14f), Vector2.zero, Vector2.zero);

            string[] names = { "Mundo Musical", "Mundo de Cuentos", "Mundo de los Tamaños", "Mundo de las Emociones" };
            string[] scenes = { "MundoMusical", "MundoCuentos_VozTest", "MundoTamanos", "MundoEmociones" };
            Vector2[] positions =
            {
                new(-545f, 145f),
                new(-175f, -75f),
                new(225f, 145f),
                new(560f, -75f)
            };
            Color[] colors =
            {
                SoftBlue,
                new Color(0.9f, 0.58f, 0.68f, 1f),
                SoftGreen,
                SoftPurple
            };

            List<WorldSelectionManager.WorldData> worldData = new();
            List<WorldSelectionVisuals.WorldVisual> visualData = new();
            for (int i = 0; i < names.Length; i++)
            {
                CreateWorldPlanet(root, manager, i, names[i], scenes[i], positions[i], colors[i], worldData, visualData);
            }

            Button backButton = CreateButton("BotonVolverMenu", "Volver al menú", root, new Vector2(-190f, -465f), new Vector2(300f, 78f), LoadSprite(ButtonSkybluePath), TextLight);
            Button resetButton = CreateButton("BotonReiniciarProgreso", "Reiniciar progreso", root, new Vector2(190f, -465f), new Vector2(340f, 78f), LoadSprite(ButtonOrangePath), TextLight);
            ClearPersistentListeners(backButton.onClick);
            UnityEventTools.AddPersistentListener(backButton.onClick, manager.ReturnToMenu);
            ClearPersistentListeners(resetButton.onClick);
            UnityEventTools.AddPersistentListener(resetButton.onClick, manager.RequestResetProgress);

            GameObject confirmationPanel = CreateResetConfirmation(root, manager);
            AssignWorldSelectionManager(manager, worldData, messageText, confirmationPanel);
            AssignWorldSelectionVisuals(root.gameObject.AddComponent<WorldSelectionVisuals>(), visualData);

            EnsureSceneDialogue(
                scene,
                "Luli",
                LoadSprite(LuliPath),
                new[]
                {
                    "¡Hola! Soy Luli. Aquí están nuestros mundos de aprendizaje.",
                    "Haz clic en el planeta que brilla para comenzar.",
                    "Consigue estrellas para abrir los siguientes mundos. ¡Vamos a explorar!"
                },
                "¡A explorar!",
                null);
        }

        private static void PatchMusicWorld(Scene scene)
        {
            if (ComponentsInScene<Transform>(scene).Any(item => item.name == "MusicalUI"))
            {
                Debug.Log("MundoAprendoVisualUpgradeSetup: se conserva la reconstruccion especializada de Mundo Musical.");
                return;
            }

            Canvas canvas = EnsureCanvas(scene, "Canvas - Mundo Musical");
            EnsureBackground(canvas.transform, "FondoMundoMusicalNuevo", LoadSprite(MusicBackgroundPath), Color.white, false);
            StyleSceneButtons(scene);
            StylePianoKeys(scene);

            MundoMusicalSequenceGame manager = ComponentsInScene<MundoMusicalSequenceGame>(scene).FirstOrDefault();
            if (manager != null)
            {
                SerializedObject so = new(manager);
                SetBool(so, "playSequenceOnStart", false);
                SetFloat(so, "delayBeforeNextSequence", 1.25f);
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EnsureSceneDialogue(
                scene,
                "Tamborcin",
                LoadSprite(TamborcinPath),
                new[]
                {
                    "¡Hola! Soy Tamborcin. Primero escucha la secuencia de sonidos.",
                    "Después toca las teclas en el mismo orden.",
                    "Cuando estés listo, pulsa Comenzar. ¡Vamos a hacer música!"
                },
                "Comenzar",
                manager != null ? manager.StartActivity : null);
        }

        private static void PatchStoryWorld(Scene scene)
        {
            Canvas canvas = EnsureCanvas(scene, "Canvas - Mundo Cuentos Voz Test");
            EnsureBackground(canvas.transform, "FondoBibliotecaNuevo", LoadSprite(StoryBackgroundPath), Color.white, false);
            StyleSceneButtons(scene);
            FixStorySelectionLayout(scene);

            VoiceRecognitionTest manager = ComponentsInScene<VoiceRecognitionTest>(scene).FirstOrDefault();
            EnsureSceneDialogue(
                scene,
                "Biblio",
                LoadSprite(BiblioPath),
                new[]
                {
                    "¡Hola! Soy Biblio. Primero elige el cuento que quieras leer.",
                    "Pulsa Iniciar y espera la cuenta regresiva. Después lee el cuento en voz alta.",
                    "Cuando termines, pulsa Validar para conocer tus estrellas."
                },
                "¡A leer!",
                null);

            CharacterDialogueController dialogue = ComponentsInScene<CharacterDialogueController>(scene).FirstOrDefault();
            GameplayDialogueFeedbackBridge bridge = dialogue != null ? dialogue.GetComponent<GameplayDialogueFeedbackBridge>() : null;
            if (bridge != null && manager != null)
            {
                SerializedObject bridgeSo = new(bridge);
                SetReference(bridgeSo, "storyController", manager);
                bridgeSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void PatchSizeWorld(Scene scene)
        {
            StyleSceneButtons(scene);
            SizeWorldController manager = ComponentsInScene<SizeWorldController>(scene).FirstOrDefault();
            if (manager != null)
            {
                ConfigureSizeWorld(manager);
            }

            EnsureSceneDialogue(
                scene,
                "Explorador",
                LoadSprite(ExploradorPath),
                new[]
                {
                    "¡Hola! Soy el Explorador. Mira con atención los dos animales.",
                    "Haz clic en el animal que responda la pregunta.",
                    "Después pulsa Validar. ¡No importa si fallas, puedes intentarlo otra vez!"
                },
                "Comenzar",
                manager != null ? manager.StartActivity : null);
        }

        private static void PatchEmotionWorld(Scene scene)
        {
            Canvas canvas = EnsureCanvas(scene, "Canvas - Mundo Emociones");
            if (LoadSprite(EmotionBackgroundPath) != null)
            {
                EnsureBackground(canvas.transform, "FondoEmocionesSuave", LoadSprite(EmotionBackgroundPath), Color.white, false);
            }

            StyleSceneButtons(scene);
            EmotionGameManager manager = ComponentsInScene<EmotionGameManager>(scene).FirstOrDefault();
            if (manager != null)
            {
                SerializedObject so = new(manager);
                UIProgressBar progressBar = GetReference<UIProgressBar>(so, "roundProgressBar")
                                            ?? ComponentsInScene<UIProgressBar>(scene).FirstOrDefault();
                if (progressBar != null) progressBar.gameObject.SetActive(false);
                SetReference(so, "roundProgressBar", progressBar);
                TMP_Text progressText = GetReference<TMP_Text>(so, "progressText");
                if (progressText != null)
                {
                    progressText.fontSize = Mathf.Max(progressText.fontSize, 32f);
                    progressText.alignment = TextAlignmentOptions.Center;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EnsureSceneDialogue(
                scene,
                "Nuna",
                LoadSprite(NunaPath),
                new[]
                {
                    "¡Hola! Soy Nuna. Mira con atención la expresión del personaje.",
                    "Elige la emoción que está sintiendo.",
                    "Después pulsa Validar. ¡Vamos a conocer nuestras emociones!"
                },
                "Comenzar",
                manager != null ? manager.StartActivity : null);
        }

        private static void EnsureSceneDialogue(Scene scene, string guideName, Sprite characterSprite, string[] steps, string finalButtonText, UnityAction onIntroCompleted)
        {
            DestroyIfExists(scene, "DialogoPersonajeCanvas");

            GameObject canvasObject = new("DialogoPersonajeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(CharacterDialogueController), typeof(GameplayDialogueFeedbackBridge));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9000;
            ConfigureScaler(canvasObject.GetComponent<CanvasScaler>());
            EnsureCanvasAccessibility(canvas);
            EnsureSceneTransition(canvas);

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            Stretch(canvasRect);

            CanvasGroup group = canvasObject.GetComponent<CanvasGroup>();

            Image blocker = CreateImage("BloqueoRaycast", canvasRect, null, new Color(0.05f, 0.06f, 0.1f, 0.42f), true);
            Stretch(blocker.rectTransform);

            RectTransform characterRoot = CreateUiObject($"{guideName}Visual", canvasRect).GetComponent<RectTransform>();
            SetAnchors(characterRoot, new Vector2(0.02f, 0.02f), new Vector2(0.31f, 0.52f), Vector2.zero, Vector2.zero);
            Image character = characterRoot.gameObject.AddComponent<Image>();
            character.sprite = characterSprite;
            character.color = Color.white;
            character.preserveAspect = true;
            character.raycastTarget = false;
            characterRoot.gameObject.AddComponent<UIJuiceAnimator>();

            Image bubble = CreateImage("GloboDialogo", canvasRect, LoadSprite(PanelSpritePath), new Color(1f, 1f, 1f, 0.98f), true);
            SetAnchors(bubble.rectTransform, new Vector2(0.31f, 0.22f), new Vector2(0.91f, 0.69f), Vector2.zero, Vector2.zero);
            bubble.type = Image.Type.Sliced;

            TMP_Text text = CreateText("TextoDialogo", string.Empty, bubble.rectTransform, 36f, FontStyles.Bold, TextAlignmentOptions.TopLeft, TextDark);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.margin = new Vector4(28f, 24f, 28f, 24f);
            SetAnchors(text.rectTransform, new Vector2(0.05f, 0.27f), new Vector2(0.95f, 0.9f), Vector2.zero, Vector2.zero);

            Button nextButton = CreateButton("BotonDialogoSiguiente", "Siguiente", bubble.rectTransform, new Vector2(250f, -120f), new Vector2(290f, 82f), LoadSprite(ButtonGreenPath), TextLight);
            SetAnchors(nextButton.GetComponent<RectTransform>(), new Vector2(0.63f, 0.06f), new Vector2(0.94f, 0.25f), Vector2.zero, Vector2.zero);
            TMP_Text buttonText = nextButton.GetComponentInChildren<TMP_Text>(true);

            CharacterDialogueController dialogue = canvasObject.GetComponent<CharacterDialogueController>();
            SerializedObject so = new(dialogue);
            SetReference(so, "overlayGroup", group);
            SetReference(so, "characterRoot", characterRoot);
            SetReference(so, "characterImage", character);
            SetReference(so, "bubbleRoot", bubble.rectTransform);
            SetReference(so, "dialogueText", text);
            SetReference(so, "nextButton", nextButton);
            SetReference(so, "nextButtonText", buttonText);
            SetBool(so, "showIntroOnStart", true);
            SetString(so, "finalIntroButtonText", finalButtonText);
            SetString(so, "retryMessage", "¡Casi! Mira con atención e inténtalo otra vez.");
            SetString(so, "retryButtonText", "Intentar otra vez");
            SetString(so, "successButtonText", "Continuar");
            SetFloat(so, "successAutoHideSeconds", 0.85f);
            SerializedProperty stepsProperty = so.FindProperty("introSteps");
            stepsProperty.arraySize = steps.Length;
            for (int i = 0; i < steps.Length; i++)
            {
                SerializedProperty step = stepsProperty.GetArrayElementAtIndex(i);
                step.FindPropertyRelative("message").stringValue = steps[i];
                step.FindPropertyRelative("characterSprite").objectReferenceValue = characterSprite;
                step.FindPropertyRelative("buttonText").stringValue = i == steps.Length - 1 ? finalButtonText : "Siguiente";
            }

            SerializedProperty successMessages = so.FindProperty("successMessages");
            successMessages.arraySize = 3;
            successMessages.GetArrayElementAtIndex(0).stringValue = "¡Muy bien!";
            successMessages.GetArrayElementAtIndex(1).stringValue = "¡Lo lograste!";
            successMessages.GetArrayElementAtIndex(2).stringValue = "¡Excelente trabajo!";
            so.ApplyModifiedPropertiesWithoutUndo();

            ClearPersistentListeners(dialogue.OnIntroCompleted);
            if (onIntroCompleted != null)
            {
                UnityEventTools.AddPersistentListener(dialogue.OnIntroCompleted, onIntroCompleted);
            }

            GameplayDialogueFeedbackBridge bridge = canvasObject.GetComponent<GameplayDialogueFeedbackBridge>();
            SerializedObject bridgeSo = new(bridge);
            SetReference(bridgeSo, "dialogue", dialogue);
            SetReference(bridgeSo, "sizeWorldController", ComponentsInScene<SizeWorldController>(scene).FirstOrDefault());
            SetReference(bridgeSo, "emotionGameManager", ComponentsInScene<EmotionGameManager>(scene).FirstOrDefault());
            SetReference(bridgeSo, "musicalGame", ComponentsInScene<MundoMusicalSequenceGame>(scene).FirstOrDefault());
            SetReference(bridgeSo, "storyController", ComponentsInScene<VoiceRecognitionTest>(scene).FirstOrDefault());
            SetBool(bridgeSo, "showSuccessFeedback", true);
            SetBool(bridgeSo, "showRetryFeedback", true);
            bridgeSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateWorldPlanet(
            RectTransform parent,
            WorldSelectionManager manager,
            int index,
            string worldName,
            string sceneName,
            Vector2 position,
            Color tint,
            List<WorldSelectionManager.WorldData> worldData,
            List<WorldSelectionVisuals.WorldVisual> visualData)
        {
            Sprite planetSprite = LoadSprite(PlanetPaths[Mathf.Clamp(index, 0, PlanetPaths.Length - 1)]);
            Sprite fullStar = LoadSprite(FullStarPath);
            Sprite emptyStar = LoadSprite(EmptyStarPath);
            Sprite lockSprite = LoadSprite(LockPath);

            Button button = CreateButton($"Planeta-{index}-{worldName}", string.Empty, parent, position, new Vector2(330f, 330f), planetSprite, TextLight);
            Image planetImage = button.image;
            planetImage.color = tint;
            planetImage.preserveAspect = true;
            planetImage.type = Image.Type.Simple;

            RectTransform rect = button.GetComponent<RectTransform>();
            CanvasGroup group = button.gameObject.AddComponent<CanvasGroup>();
            UIJuiceAnimator juice = button.gameObject.AddComponent<UIJuiceAnimator>();
            SerializedObject juiceSo = new(juice);
            SetFloat(juiceSo, "phaseOffset", index * 0.9f);
            SetFloat(juiceSo, "speed", 0.72f + index * 0.08f);
            juiceSo.ApplyModifiedPropertiesWithoutUndo();

            Sprite iconSprite = LoadSprite(WorldIconPaths[Mathf.Clamp(index, 0, WorldIconPaths.Length - 1)]);
            Image icon = CreateImage("IconoMundo", rect, iconSprite, Color.white, false);
            SetAnchors(icon.rectTransform, new Vector2(0.34f, 0.39f), new Vector2(0.66f, 0.71f), Vector2.zero, Vector2.zero);
            icon.preserveAspect = true;

            TMP_Text title = CreateText("NombreMundo", worldName, rect, 30f, FontStyles.Bold, TextAlignmentOptions.Center, TextDark);
            SetAnchors(title.rectTransform, new Vector2(-0.1f, -0.17f), new Vector2(1.1f, 0.04f), Vector2.zero, Vector2.zero);

            Image[] stars = new Image[3];
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i] = CreateImage($"Estrella-{i + 1}", rect, emptyStar, Color.white, false);
                stars[i].preserveAspect = true;
                SetAnchors(stars[i].rectTransform, new Vector2(0.24f + i * 0.18f, -0.32f), new Vector2(0.38f + i * 0.18f, -0.18f), Vector2.zero, Vector2.zero);
            }

            GameObject lockGroup = CreateUiObject("EstadoBloqueado", rect);
            RectTransform lockRect = lockGroup.GetComponent<RectTransform>();
            SetAnchors(lockRect, new Vector2(0.18f, 0.18f), new Vector2(0.82f, 0.48f), Vector2.zero, Vector2.zero);
            Image lockBack = lockGroup.AddComponent<Image>();
            lockBack.color = new Color(0.1f, 0.08f, 0.16f, 0.72f);
            lockBack.type = Image.Type.Sliced;

            Image lockIcon = CreateImage("Candado", lockRect, lockSprite, Color.white, false);
            SetAnchors(lockIcon.rectTransform, new Vector2(0.12f, 0.24f), new Vector2(0.33f, 0.78f), Vector2.zero, Vector2.zero);
            lockIcon.preserveAspect = true;

            TMP_Text lockedText = CreateText("TextoBloqueado", "Bloqueado", lockRect, 25f, FontStyles.Bold, TextAlignmentOptions.Center, TextLight);
            SetAnchors(lockedText.rectTransform, new Vector2(0.34f, 0.1f), new Vector2(0.95f, 0.9f), Vector2.zero, Vector2.zero);

            ClearPersistentListeners(button.onClick);
            UnityEventTools.AddIntPersistentListener(button.onClick, manager.SelectWorld, index);

            worldData.Add(new WorldSelectionManager.WorldData
            {
                worldName = worldName,
                sceneName = sceneName,
                worldButton = button,
                worldNameText = title,
                lockedIcon = lockGroup,
                starImages = stars,
                fullStarSprite = fullStar,
                emptyStarSprite = emptyStar,
                progressGraphic = planetImage,
                baseProgressColor = tint
            });

            visualData.Add(new WorldSelectionVisuals.WorldVisual
            {
                visualRoot = rect,
                canvasGroup = group,
                planetImage = planetImage,
                lockedGroup = lockGroup,
                unlockedColor = tint,
                lockedColor = new Color(tint.r * 0.72f, tint.g * 0.72f, tint.b * 0.72f, 1f)
            });
        }

        private static GameObject CreateResetConfirmation(RectTransform parent, WorldSelectionManager manager)
        {
            GameObject panel = CreateUiObject("PanelConfirmacionReset", parent, typeof(Image), typeof(CanvasGroup), typeof(UIPanelTransition));
            RectTransform rect = panel.GetComponent<RectTransform>();
            SetAnchors(rect, new Vector2(0.33f, 0.34f), new Vector2(0.67f, 0.62f), Vector2.zero, Vector2.zero);
            Image image = panel.GetComponent<Image>();
            image.sprite = LoadSprite(PanelSpritePath);
            image.color = new Color(1f, 1f, 1f, 0.98f);
            image.type = Image.Type.Sliced;

            TMP_Text text = CreateText("TextoConfirmacion", "¿Quieres reiniciar el progreso?", rect, 32f, FontStyles.Bold, TextAlignmentOptions.Center, TextDark);
            SetAnchors(text.rectTransform, new Vector2(0.08f, 0.52f), new Vector2(0.92f, 0.9f), Vector2.zero, Vector2.zero);

            Button confirm = CreateButton("BotonConfirmarReset", "Sí, reiniciar", rect, new Vector2(-120f, -75f), new Vector2(250f, 68f), LoadSprite(ButtonOrangePath), TextLight);
            Button cancel = CreateButton("BotonCancelarReset", "Cancelar", rect, new Vector2(145f, -75f), new Vector2(230f, 68f), LoadSprite(ButtonSkybluePath), TextLight);
            ClearPersistentListeners(confirm.onClick);
            UnityEventTools.AddPersistentListener(confirm.onClick, manager.ResetProgress);
            ClearPersistentListeners(cancel.onClick);
            UnityEventTools.AddPersistentListener(cancel.onClick, manager.CancelResetProgress);

            panel.SetActive(false);
            return panel;
        }

        private static void AssignWorldSelectionManager(
            WorldSelectionManager manager,
            IReadOnlyList<WorldSelectionManager.WorldData> worlds,
            TMP_Text messageText,
            GameObject confirmationPanel)
        {
            SerializedObject so = new(manager);
            SerializedProperty worldsProperty = so.FindProperty("worlds");
            worldsProperty.arraySize = worlds.Count;

            for (int i = 0; i < worlds.Count; i++)
            {
                SerializedProperty worldProperty = worldsProperty.GetArrayElementAtIndex(i);
                WorldSelectionManager.WorldData world = worlds[i];
                worldProperty.FindPropertyRelative("worldName").stringValue = world.worldName;
                worldProperty.FindPropertyRelative("sceneName").stringValue = world.sceneName;
                worldProperty.FindPropertyRelative("worldButton").objectReferenceValue = world.worldButton;
                worldProperty.FindPropertyRelative("worldNameText").objectReferenceValue = world.worldNameText;
                worldProperty.FindPropertyRelative("lockedIcon").objectReferenceValue = world.lockedIcon;
                worldProperty.FindPropertyRelative("fullStarSprite").objectReferenceValue = world.fullStarSprite;
                worldProperty.FindPropertyRelative("emptyStarSprite").objectReferenceValue = world.emptyStarSprite;
                worldProperty.FindPropertyRelative("progressGraphic").objectReferenceValue = world.progressGraphic;
                worldProperty.FindPropertyRelative("baseProgressColor").colorValue = world.baseProgressColor;

                SerializedProperty stars = worldProperty.FindPropertyRelative("starImages");
                stars.arraySize = world.starImages.Length;
                for (int j = 0; j < world.starImages.Length; j++)
                {
                    stars.GetArrayElementAtIndex(j).objectReferenceValue = world.starImages[j];
                }
            }

            SetReference(so, "messageText", messageText);
            SetReference(so, "resetConfirmationPanel", confirmationPanel);
            SetReference(so, "resetConfirmationTransition", confirmationPanel.GetComponent<UIPanelTransition>());
            SetString(so, "lockedWorldMessage", "Primero completa el mundo anterior para desbloquear este planeta.");
            SetString(so, "menuSceneName", "Menu");
            SetBool(so, "configureWorldButtonsOnAwake", false);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignWorldSelectionVisuals(WorldSelectionVisuals visuals, IReadOnlyList<WorldSelectionVisuals.WorldVisual> worlds)
        {
            SerializedObject so = new(visuals);
            SerializedProperty worldsProperty = so.FindProperty("worlds");
            worldsProperty.arraySize = worlds.Count;
            for (int i = 0; i < worlds.Count; i++)
            {
                SerializedProperty item = worldsProperty.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("visualRoot").objectReferenceValue = worlds[i].visualRoot;
                item.FindPropertyRelative("canvasGroup").objectReferenceValue = worlds[i].canvasGroup;
                item.FindPropertyRelative("planetImage").objectReferenceValue = worlds[i].planetImage;
                item.FindPropertyRelative("lockedGroup").objectReferenceValue = worlds[i].lockedGroup;
                item.FindPropertyRelative("unlockedColor").colorValue = worlds[i].unlockedColor;
                item.FindPropertyRelative("lockedColor").colorValue = worlds[i].lockedColor;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSizeWorld(SizeWorldController manager)
        {
            SerializedObject so = new(manager);
            SetBool(so, "startAutomatically", false);
            SetFloat(so, "globalMinScale", 0.74f);
            SetFloat(so, "globalMaxScale", 1.05f);
            SetFloat(so, "horizontalPadding", 260f);
            SetFloat(so, "verticalPadding", 135f);
            SetFloat(so, "delayBeforeNextRound", 1.05f);

            Image backgroundImage = GetReference<Image>(so, "backgroundImage");
            Sprite currentBackground = backgroundImage != null ? backgroundImage.sprite : null;

            SerializedProperty range = so.FindProperty("verticalPositionRange");
            if (range != null) range.vector2Value = new Vector2(-60f, 95f);

            SerializedProperty habitats = so.FindProperty("habitats");
            habitats.arraySize = 3;
            ConfigureHabitat(
                habitats.GetArrayElementAtIndex(0),
                "Safari grande y pequeño",
                currentBackground,
                new[]
                {
                    Animal("Elefante", "Assets/Mundo Aprendo/MundoTamanos/Arte/Animal_Elefante.png", AnimalSizeType.Grande, 0.78f, 1.05f),
                    Animal("Jirafa", "Assets/Mundo Aprendo/MundoTamanos/Arte/Animal_Jirafa.png", AnimalSizeType.Grande, 0.76f, 1.02f),
                    Animal("Caballo", "Assets/Mundo Aprendo/MundoTamanos/Arte/Animal_Caballo.png", AnimalSizeType.Grande, 0.76f, 1f),
                    Animal("Conejo", "Assets/Mundo Aprendo/MundoTamanos/Arte/Animal_Conejo.png", AnimalSizeType.Pequeno, 0.62f, 0.82f),
                    Animal("Pollito", "Assets/Mundo Aprendo/MundoTamanos/Arte/Animal_Pollito.png", AnimalSizeType.Pequeno, 0.58f, 0.78f),
                    Animal("Ave", "Assets/Mundo Aprendo/MundoTamanos/Arte/Animal_Ave.png", AnimalSizeType.Pequeno, 0.58f, 0.8f)
                });
            ConfigureHabitat(
                habitats.GetArrayElementAtIndex(1),
                "Mar grande y pequeño",
                currentBackground,
                new[]
                {
                    Animal("Ballena", "Assets/Mundo Aprendo/MundoTamanos/Arte/Animal_Ballena.png", AnimalSizeType.Grande, 0.78f, 1.05f),
                    Animal("Tiburón", "Assets/Mundo Aprendo/MundoTamanos/Arte/Animal_Tiburon.png", AnimalSizeType.Grande, 0.76f, 1.02f),
                    Animal("Vaca", "Assets/Mundo Aprendo/MundoTamanos/Arte/Animal_Vaca.png", AnimalSizeType.Grande, 0.74f, 1f),
                    Animal("Pez", "Assets/Mundo Aprendo/MundoTamanos/Arte/Animal_Pez.png", AnimalSizeType.Pequeno, 0.58f, 0.78f),
                    Animal("Cangrejo", "Assets/Mundo Aprendo/MundoTamanos/Arte/Animal_Cangrejo.png", AnimalSizeType.Pequeno, 0.58f, 0.8f),
                    Animal("Mono", "Assets/Mundo Aprendo/MundoTamanos/Arte/Animal_Mono.png", AnimalSizeType.Pequeno, 0.62f, 0.82f)
                });
            ConfigureHabitat(
                habitats.GetArrayElementAtIndex(2),
                "Granja grande y pequeño",
                currentBackground,
                new[]
                {
                    Animal("Vaca", "Assets/Mundo Aprendo/MundoTamanos/Arte/Animal_Vaca.png", AnimalSizeType.Grande, 0.76f, 1.03f),
                    Animal("Caballo", "Assets/Mundo Aprendo/MundoTamanos/Arte/Animal_Caballo.png", AnimalSizeType.Grande, 0.74f, 1f),
                    Animal("Jirafa", "Assets/Mundo Aprendo/MundoTamanos/Arte/Animal_Jirafa.png", AnimalSizeType.Grande, 0.78f, 1.05f),
                    Animal("Conejo", "Assets/Mundo Aprendo/MundoTamanos/Arte/Animal_Conejo.png", AnimalSizeType.Pequeno, 0.6f, 0.82f),
                    Animal("Pollito", "Assets/Mundo Aprendo/MundoTamanos/Arte/Animal_Pollito.png", AnimalSizeType.Pequeno, 0.56f, 0.76f),
                    Animal("Pez", "Assets/Mundo Aprendo/MundoTamanos/Arte/Animal_Pez.png", AnimalSizeType.Pequeno, 0.58f, 0.78f)
                });

            foreach (string propertyName in new[] { "leftAnimalImage", "rightAnimalImage" })
            {
                Image animalImage = GetReference<Image>(so, propertyName);
                if (animalImage == null) continue;
                animalImage.preserveAspect = true;
                animalImage.raycastTarget = false;
                RectTransform rect = animalImage.rectTransform;
                rect.sizeDelta = new Vector2(310f, 270f);
                rect.pivot = new Vector2(0.5f, 0.5f);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static AnimalData Animal(string name, string path, AnimalSizeType type, float minScale, float maxScale)
        {
            return new AnimalData
            {
                animalName = name,
                animalSprite = LoadSprite(path),
                sizeType = type,
                minScale = minScale,
                maxScale = maxScale
            };
        }

        private static void ConfigureHabitat(SerializedProperty habitat, string name, Sprite background, IReadOnlyList<AnimalData> animals)
        {
            habitat.FindPropertyRelative("habitatName").stringValue = name;
            habitat.FindPropertyRelative("backgroundSprite").objectReferenceValue = background;
            SerializedProperty animalsProperty = habitat.FindPropertyRelative("animals");
            animalsProperty.arraySize = animals.Count;
            for (int i = 0; i < animals.Count; i++)
            {
                SerializedProperty animalProperty = animalsProperty.GetArrayElementAtIndex(i);
                animalProperty.FindPropertyRelative("animalName").stringValue = animals[i].animalName;
                animalProperty.FindPropertyRelative("animalSprite").objectReferenceValue = animals[i].animalSprite;
                animalProperty.FindPropertyRelative("sizeType").enumValueIndex = (int)animals[i].sizeType;
                animalProperty.FindPropertyRelative("minScale").floatValue = animals[i].minScale;
                animalProperty.FindPropertyRelative("maxScale").floatValue = animals[i].maxScale;
            }
        }

        private static void FixStorySelectionLayout(Scene scene)
        {
            foreach (Image image in ComponentsInScene<Image>(scene))
            {
                if (image == null || image.sprite == null) continue;
                string lowerName = image.gameObject.name.ToLowerInvariant();
                string parentName = image.transform.parent != null ? image.transform.parent.name.ToLowerInvariant() : string.Empty;
                if (lowerName.Contains("cuento") || parentName.Contains("cuento") || lowerName.Contains("icon"))
                {
                    image.preserveAspect = true;
                    image.raycastTarget = image.GetComponent<Button>() != null;
                    RectTransform rect = image.rectTransform;
                    if (rect.sizeDelta.x < 92f || rect.sizeDelta.y < 92f)
                    {
                        rect.sizeDelta = new Vector2(Mathf.Max(112f, rect.sizeDelta.x), Mathf.Max(112f, rect.sizeDelta.y));
                    }
                }
            }

            GridLayoutGroup grid = ComponentsInScene<GridLayoutGroup>(scene).FirstOrDefault(item => item.gameObject.name.Contains("Cuentos"));
            if (grid != null)
            {
                grid.cellSize = new Vector2(Mathf.Max(grid.cellSize.x, 320f), Mathf.Max(grid.cellSize.y, 292f));
                grid.spacing = new Vector2(Mathf.Max(grid.spacing.x, 24f), Mathf.Max(grid.spacing.y, 20f));
                grid.childAlignment = TextAnchor.MiddleCenter;
            }
        }

        private static void StyleSceneButtons(Scene scene)
        {
            Sprite[] palette =
            {
                LoadSprite(ButtonGreenPath),
                LoadSprite(ButtonSkybluePath),
                LoadSprite(ButtonYellowPath),
                LoadSprite(ButtonPurplePath)
            };

            int index = 0;
            foreach (Button button in ComponentsInScene<Button>(scene))
            {
                if (button == null || button.GetComponentInParent<CharacterDialogueController>() != null) continue;
                Sprite sprite = palette[index % palette.Length];
                index++;
                StyleButton(button, sprite, TextLight);
            }
        }

        private static void StylePianoKeys(Scene scene)
        {
            foreach (Button button in ComponentsInScene<Button>(scene))
            {
                string lower = button.gameObject.name.ToLowerInvariant();
                if (!lower.Contains("tecla") && !lower.Contains("key")) continue;
                RectTransform rect = button.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, 135f), Mathf.Max(rect.sizeDelta.y, 260f));
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.fontSize = Mathf.Max(label.fontSize, 56f);
                    label.alignment = TextAlignmentOptions.Center;
                    label.fontStyle |= FontStyles.Bold;
                }
            }
        }

        private static void StyleButton(Button button, Sprite sprite, Color textColor)
        {
            Image image = button.image != null ? button.image : button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }

            RectTransform rect = button.GetComponent<RectTransform>();
            string labelText = string.Empty;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                labelText = label.text ?? string.Empty;
                label.fontSize = Mathf.Clamp(Mathf.Max(label.fontSize, labelText.Length > 16 ? 30f : 32f), 28f, 40f);
                label.enableAutoSizing = true;
                label.fontSizeMin = 24f;
                label.fontSizeMax = labelText.Length > 18 ? 32f : 36f;
                label.alignment = TextAlignmentOptions.Center;
                label.color = textColor;
                label.fontStyle |= FontStyles.Bold;
                label.raycastTarget = false;
            }

            float minWidth = labelText.Length > 16 ? 310f : 230f;
            rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, minWidth), Mathf.Max(rect.sizeDelta.y, 76f));
            if (button.GetComponent<UIButtonFeedback>() == null)
            {
                button.gameObject.AddComponent<UIButtonFeedback>();
            }
        }

        private static void AddDecorativeStars(RectTransform parent)
        {
            Sprite star = LoadSprite(FullStarPath);
            Color[] colors =
            {
                new(1f, 0.86f, 0.32f, 0.8f),
                new(1f, 0.58f, 0.73f, 0.7f),
                new(0.52f, 0.82f, 1f, 0.72f),
                new(0.62f, 0.9f, 0.6f, 0.7f)
            };

            for (int i = 0; i < 34; i++)
            {
                float x = 0.03f + ((i * 0.173f) % 0.94f);
                float y = 0.16f + ((i * 0.289f) % 0.77f);
                Image image = CreateImage($"EstrellaDecorativa-{i + 1:00}", parent, star, colors[i % colors.Length], false);
                RectTransform rect = image.rectTransform;
                rect.anchorMin = new Vector2(x, y);
                rect.anchorMax = rect.anchorMin;
                rect.sizeDelta = Vector2.one * (18f + (i % 4) * 6f);
                rect.anchoredPosition = Vector2.zero;
                image.raycastTarget = false;
                UIJuiceAnimator animator = image.gameObject.AddComponent<UIJuiceAnimator>();
                SerializedObject so = new(animator);
                SetBool(so, "twinkle", true);
                SetFloat(so, "phaseOffset", i * 0.57f);
                SetFloat(so, "speed", 0.38f + (i % 5) * 0.08f);
                SetFloat(so, "floatDistance", 4f + i % 3);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void EnsureCompanionImage(Transform canvas, string name, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax)
        {
            DestroyChildIfExists(canvas, name);
            if (sprite == null) return;
            Image image = CreateImage(name, canvas, sprite, Color.white, false);
            SetAnchors(image.rectTransform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.transform.SetAsFirstSibling();
            image.transform.SetSiblingIndex(1);
            if (image.GetComponent<UIJuiceAnimator>() == null)
            {
                image.gameObject.AddComponent<UIJuiceAnimator>();
            }
        }

        private static void EnsureBackground(Transform canvas, string name, Sprite sprite, Color color, bool raycast)
        {
            DestroyChildIfExists(canvas, name);
            Image background = CreateImage(name, canvas, sprite, color, raycast);
            Stretch(background.rectTransform);
            background.preserveAspect = false;
            background.transform.SetAsFirstSibling();
        }

        private static Canvas EnsureCanvas(Scene scene, string preferredName)
        {
            Canvas canvas = ComponentsInScene<Canvas>(scene).FirstOrDefault(item => item.gameObject.name == preferredName)
                            ?? ComponentsInScene<Canvas>(scene).FirstOrDefault();
            if (canvas != null)
            {
                ConfigureScaler(canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>());
                if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
                EnsureCanvasAccessibility(canvas);
                EnsureSceneTransition(canvas);
                return canvas;
            }

            GameObject canvasObject = new(preferredName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            ConfigureScaler(canvasObject.GetComponent<CanvasScaler>());
            EnsureCanvasAccessibility(canvas);
            EnsureSceneTransition(canvas);
            return canvas;
        }

        private static void ConfigureCanvasScalers(Scene scene)
        {
            foreach (CanvasScaler scaler in ComponentsInScene<CanvasScaler>(scene))
            {
                ConfigureScaler(scaler);
                Canvas canvas = scaler.GetComponent<Canvas>();
                if (canvas != null && canvas.isRootCanvas)
                {
                    EnsureCanvasAccessibility(canvas);
                    EnsureSceneTransition(canvas);
                }
            }
        }

        private static void ConfigureScaler(CanvasScaler scaler)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private static void EnsureCanvasAccessibility(Canvas canvas)
        {
            CanvasColorBlindAccessibility accessibility = canvas.GetComponent<CanvasColorBlindAccessibility>() ?? canvas.gameObject.AddComponent<CanvasColorBlindAccessibility>();
            SerializedObject so = new(accessibility);
            SetReference(so, "targetCanvas", canvas);
            SetReference(so, "colorBlindDropdown", FindColorBlindDropdown(canvas));
            Graphic[] graphics = canvas.GetComponentsInChildren<Graphic>(true)
                .Where(graphic => graphic is Image || graphic is RawImage)
                .ToArray();
            SetObjectArray(so.FindProperty("targetGraphics"), graphics.Cast<UnityEngine.Object>().ToArray());
            so.ApplyModifiedPropertiesWithoutUndo();
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

        private static void EnsureSceneTransition(Canvas canvas)
        {
            Transform existing = FindChildRecursive(canvas.transform, "SceneTransitionOverlay");
            GameObject overlay = existing != null ? existing.gameObject : CreateUiObject("SceneTransitionOverlay", canvas.transform, typeof(Image), typeof(CanvasGroup), typeof(UISceneTransition));
            RectTransform rect = overlay.GetComponent<RectTransform>() ?? overlay.AddComponent<RectTransform>();
            Stretch(rect);

            Image image = overlay.GetComponent<Image>() ?? overlay.AddComponent<Image>();
            image.color = new Color(0.055f, 0.075f, 0.12f, 1f);
            image.raycastTarget = true;

            CanvasGroup group = overlay.GetComponent<CanvasGroup>() ?? overlay.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            UISceneTransition transition = overlay.GetComponent<UISceneTransition>() ?? overlay.AddComponent<UISceneTransition>();
            SerializedObject transitionSo = new(transition);
            SetReference(transitionSo, "fadeCanvasGroup", group);
            transitionSo.ApplyModifiedPropertiesWithoutUndo();
            overlay.transform.SetAsLastSibling();
        }

        private static void EnsureSingleEventSystem(Scene scene)
        {
            EventSystem[] systems = ComponentsInScene<EventSystem>(scene);
            if (systems.Length == 0)
            {
                GameObject eventSystem = new("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
                return;
            }

            for (int i = 1; i < systems.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(systems[i].gameObject, true);
            }
        }

        private static Button CreateButton(string name, string label, Transform parent, Vector2 anchoredPosition, Vector2 size, Sprite sprite, Color textColor)
        {
            GameObject buttonObject = CreateUiObject(name, parent, typeof(Image), typeof(Button), typeof(UIButtonFeedback));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = buttonObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = sprite == null ? new Color(1f, 1f, 1f, 0.01f) : Color.white;
            image.type = sprite == null ? Image.Type.Simple : Image.Type.Sliced;
            image.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            if (!string.IsNullOrEmpty(label))
            {
                TMP_Text text = CreateText("Texto", label, rect, 32f, FontStyles.Bold, TextAlignmentOptions.Center, textColor);
                Stretch(text.rectTransform, new Vector2(18f, 8f), new Vector2(-18f, -8f));
                text.enableAutoSizing = true;
                text.fontSizeMin = 24f;
                text.fontSizeMax = 36f;
            }

            return button;
        }

        private static TMP_Text CreateText(string name, string text, Transform parent, float fontSize, FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            GameObject textObject = CreateUiObject(name, parent, typeof(TextMeshProUGUI));
            TMP_Text tmp = textObject.GetComponent<TMP_Text>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            return tmp;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool raycastTarget)
        {
            GameObject imageObject = CreateUiObject(name, parent, typeof(Image));
            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycastTarget;
            if (sprite != null) image.preserveAspect = true;
            return image;
        }

        private static GameObject CreateUiObject(string name, Transform parent, params Type[] components)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            if (parent != null) gameObject.transform.SetParent(parent, false);
            foreach (Type component in components)
            {
                if (component == typeof(RectTransform)) continue;
                if (gameObject.GetComponent(component) == null) gameObject.AddComponent(component);
            }

            return gameObject;
        }

        private static void Stretch(RectTransform rect)
        {
            Stretch(rect, Vector2.zero, Vector2.zero);
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Sprite LoadSprite(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath)) return null;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null) return sprite;

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return null;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static T[] ComponentsInScene<T>(Scene scene) where T : Component
        {
            List<T> components = new();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                components.AddRange(root.GetComponentsInChildren<T>(true));
            }

            return components.ToArray();
        }

        private static GameObject FindObject(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform match = FindChildRecursive(root.transform, objectName);
                if (match != null) return match.gameObject;
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string objectName)
        {
            if (parent.name == objectName) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform match = FindChildRecursive(parent.GetChild(i), objectName);
                if (match != null) return match;
            }

            return null;
        }

        private static void DestroyIfExists(Scene scene, string objectName)
        {
            GameObject existing = FindObject(scene, objectName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing, true);
            }
        }

        private static void DestroyChildIfExists(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject, true);
            }
        }

        private static void ClearPersistentListeners(UnityEventBase unityEvent)
        {
            for (int i = unityEvent.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                UnityEventTools.RemovePersistentListener(unityEvent, i);
            }
        }

        private static T GetReference<T>(SerializedObject so, string propertyName) where T : UnityEngine.Object
        {
            SerializedProperty property = so.FindProperty(propertyName);
            return property != null ? property.objectReferenceValue as T : null;
        }

        private static void SetReference(SerializedObject so, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void SetBool(SerializedObject so, string propertyName, bool value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null) property.boolValue = value;
        }

        private static void SetString(SerializedObject so, string propertyName, string value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null) property.stringValue = value;
        }

        private static void SetFloat(SerializedObject so, string propertyName, float value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null) property.floatValue = value;
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
#endif
