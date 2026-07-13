#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
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
    public static class MundoAprendoUrgentVisualCorrection
    {
        private const string MenuScenePath = "Assets/Scenes/Menu.unity";
        private const string WorldSelectionScenePath = "Assets/Scenes/SeleccionMundos.unity";
        private const string MusicScenePath = "Assets/Scenes/MundoMusical.unity";
        private const string StoryScenePath = "Assets/Scenes/MundoCuentos_VozTest.unity";
        private const string SizeScenePath = "Assets/Scenes/MundoTamanos.unity";
        private const string EmotionScenePath = "Assets/Scenes/Mundos/MundoEmociones.unity";

        private const string PoseFolder = "Assets/Mundo Aprendo/Personajes/_Poses";
        private const string LuliSheetPath = "Assets/Mundo Aprendo/Personajes/Luli/Luli.png";
        private const string TamborcinSheetPath = "Assets/Mundo Aprendo/Personajes/Tico El Tamborcito/tamborcin.png";
        private const string BiblioSheetPath = "Assets/Mundo Aprendo/Personajes/BIBLIO EL Buho/BIBLIO EL BUHOR.png";
        private const string ExploradorSheetPath = "Assets/Mundo Aprendo/Personajes/Guia/GUIAR.png";
        private const string NunaSheetPath = "Assets/Mundo Aprendo/Personajes/Nuna la nube/nuna La Nube (2).png";

        private const string MusicBackgroundPath = "Assets/Mundo Aprendo/Fondos/Mundo Musical/Mundo musical.png";
        private const string StoryBackgroundPath = "Assets/Mundo Aprendo/Fondos/Mundo biblioteca/Biblioteca.png";
        private const string EmotionBackgroundPath = "Assets/Mundo Aprendo/Fondos/Mundo de los sentimiento/Mundo sentimientos.png";
        private const string PanelSpritePath = "Assets/Cartoon UI/Panels/Panel Light.png";
        private const string ButtonGreenPath = "Assets/Cartoon UI/Buttons/Long Round/Long Round Green.png";
        private const string ButtonSkybluePath = "Assets/Cartoon UI/Buttons/Long Round/Long Round Skyblue.png";
        private const string ButtonOrangePath = "Assets/Cartoon UI/Buttons/Long Round/Long Round Orange.png";

        private static readonly Color TextDark = new(0.21f, 0.18f, 0.34f, 1f);
        private static readonly Color TextLight = new(1f, 0.98f, 0.9f, 1f);

        private enum PoseQuadrant
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        [MenuItem("Mundo Aprendo/Correccion visual urgente")]
        public static void ApplyFromMenu()
        {
            ApplyAll();
        }

        public static void ApplyAll()
        {
            EnsureGeneratedCharacterPoses();
            PatchScene(MenuScenePath, PatchMenu);
            PatchScene(WorldSelectionScenePath, PatchWorldSelection);
            PatchScene(MusicScenePath, PatchMusicWorld);
            PatchScene(StoryScenePath, PatchStoryWorld);
            PatchScene(SizeScenePath, PatchSizeWorld);
            PatchScene(EmotionScenePath, PatchEmotionWorld);
            AssetDatabase.SaveAssets();
            Debug.Log("MundoAprendoUrgentVisualCorrection: correccion visual urgente aplicada en escenas actuales.");
        }

        private static void PatchScene(string scenePath, Action<Scene> patch)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            RemoveRootIfExists(scene, "DialogoPersonajeCanvas");
            RemoveRootIfExists(scene, "TutorialOverlay");
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
            DestroyIfExists(scene, "DialogoPersonajeCanvas");
            DestroyIfExists(scene, "TutorialOverlay");
            DestroyIfExists(scene, "LuliMenuVisual");
            DestroyChildIfExists(canvas.transform, "FondoMenuEspacial");
            NormalizeMenuButtons(scene);
        }

        private static void PatchWorldSelection(Scene scene)
        {
            Canvas canvas = EnsureCanvas(scene, "Canvas - Seleccion Mundos");
            Transform visualRoot = FindObject(scene, "VisualSeleccionMundos")?.transform;
            if (visualRoot != null)
            {
                RemoveLayoutComponents(visualRoot);
                visualRoot.localScale = Vector3.one;
            }

            RectTransform selectionPanel = FindObject(scene, "Panel-seleccion-mundos")?.GetComponent<RectTransform>();
            if (selectionPanel != null)
            {
                RemoveLayoutComponents(selectionPanel);
                selectionPanel.anchorMin = new Vector2(0.04f, 0.10f);
                selectionPanel.anchorMax = new Vector2(0.96f, 0.88f);
                selectionPanel.offsetMin = Vector2.zero;
                selectionPanel.offsetMax = Vector2.zero;
                selectionPanel.localScale = Vector3.one;
            }

            RectTransform grid = FindObject(scene, "Mundos-grid")?.GetComponent<RectTransform>();
            if (grid != null)
            {
                RemoveLayoutComponents(grid);
                Stretch(grid);
            }

            foreach (RectTransform star in ComponentsInScene<RectTransform>(scene).Where(rect => rect.name.StartsWith("Estrella-fondo-", StringComparison.OrdinalIgnoreCase)))
            {
                RemoveLayoutComponents(star);
                star.localScale = Vector3.one;
            }

            NormalizeWorldSelectionCards(scene);

            CharacterPoseSet poses = CharacterPoseSet.Luli();
            CreateTutorialOverlay(
                scene,
                canvas,
                "Luli",
                poses,
                new[]
                {
                    "Hola, soy Luli. Aqui estan nuestros mundos de aprendizaje.",
                    "Elige el planeta que brilla para comenzar.",
                    "Consigue estrellas para abrir los siguientes mundos."
                },
                "A explorar",
                null);
        }

        private static void PatchMusicWorld(Scene scene)
        {
            Canvas canvas = EnsureCanvas(scene, "Canvas - Mundo Musical");
            EnsureBackground(canvas.transform, "FondoMundoMusicalNuevo", LoadSprite(MusicBackgroundPath), Color.white, false, true);
            NormalizeMusicPanels(scene);
            StylePianoKeys(scene);

            MundoMusicalSequenceGame manager = ComponentsInScene<MundoMusicalSequenceGame>(scene).FirstOrDefault();
            if (manager != null)
            {
                SerializedObject so = new(manager);
                SetBool(so, "playSequenceOnStart", false);
                SetFloat(so, "delayBeforeNextSequence", 1.25f);
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            CreateTutorialOverlay(
                scene,
                canvas,
                "Tamborcin",
                CharacterPoseSet.Tamborcin(),
                new[]
                {
                    "Hola, soy Tamborcin. Primero escucha la secuencia de sonidos.",
                    "Despues toca las teclas en el mismo orden.",
                    "Pulsa Comenzar cuando estes listo."
                },
                "Comenzar",
                manager != null ? manager.StartActivity : null);
        }

        private static void PatchStoryWorld(Scene scene)
        {
            Canvas canvas = EnsureCanvas(scene, "Canvas - Mundo Cuentos Voz Test");
            EnsureBackground(canvas.transform, "FondoBibliotecaNuevo", LoadSprite(StoryBackgroundPath), Color.white, false, true);
            PatchStoryCards(scene);

            VoiceRecognitionTest manager = ComponentsInScene<VoiceRecognitionTest>(scene).FirstOrDefault();
            CreateTutorialOverlay(
                scene,
                canvas,
                "Biblio",
                CharacterPoseSet.Biblio(),
                new[]
                {
                    "Hola, soy Biblio. Primero elige el cuento que quieras leer.",
                    "Pulsa Iniciar y espera la cuenta regresiva.",
                    "Despues lee en voz alta y valida tu lectura."
                },
                "A leer",
                null);

            WireStoryBridge(scene, manager);
        }

        private static void PatchSizeWorld(Scene scene)
        {
            SizeWorldController manager = ComponentsInScene<SizeWorldController>(scene).FirstOrDefault();
            if (manager != null)
            {
                ConfigureSizeWorldLayout(manager);
            }

            Canvas canvas = EnsureCanvas(scene, "Canvas - Mundo Tamanos");
            NormalizeSizePanels(scene);
            CreateTutorialOverlay(
                scene,
                canvas,
                "Explorador",
                CharacterPoseSet.Explorador(),
                new[]
                {
                    "Hola, soy el Explorador. Mira con atencion los dos animales.",
                    "Selecciona el animal que responda la pregunta.",
                    "Despues confirma tu respuesta para ganar estrellas."
                },
                "Comenzar",
                manager != null ? manager.StartActivity : null);
        }

        private static void PatchEmotionWorld(Scene scene)
        {
            Canvas canvas = EnsureCanvas(scene, "Canvas - Mundo Emociones");
            EnsureBackground(canvas.transform, "FondoEmocionesSuave", LoadSprite(EmotionBackgroundPath), Color.white, false, true);

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
                    progressText.fontSize = Mathf.Clamp(Mathf.Max(progressText.fontSize, 32f), 30f, 42f);
                    progressText.alignment = TextAlignmentOptions.Center;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            NormalizeEmotionPanels(scene);

            CreateTutorialOverlay(
                scene,
                canvas,
                "Nuna",
                CharacterPoseSet.Nuna(),
                new[]
                {
                    "Hola, soy Nuna. Mira la expresion del personaje.",
                    "Elige la emocion que esta sintiendo.",
                    "Pulsa Validar para continuar."
                },
                "Comenzar",
                manager != null ? manager.StartActivity : null);
        }

        private static void CreateTutorialOverlay(
            Scene scene,
            Canvas canvas,
            string guideName,
            CharacterPoseSet poses,
            string[] steps,
            string finalButtonText,
            UnityAction onIntroCompleted)
        {
            DestroyChildIfExists(canvas.transform, "TutorialOverlay");
            DestroyIfExists(scene, "DialogoPersonajeCanvas");

            GameObject overlay = CreateUiObject("TutorialOverlay", canvas.transform, typeof(CanvasGroup), typeof(CharacterDialogueController), typeof(GameplayDialogueFeedbackBridge));
            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            Stretch(overlayRect);
            overlay.transform.SetAsLastSibling();
            overlay.transform.localScale = Vector3.one;

            CanvasGroup group = overlay.GetComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;

            Image dim = CreateImage("DimBackground", overlayRect, null, new Color(0.04f, 0.05f, 0.08f, 0.26f), true);
            Stretch(dim.rectTransform);

            RectTransform characterContainer = CreateUiObject("CharacterContainer", overlayRect).GetComponent<RectTransform>();
            characterContainer.anchorMin = Vector2.zero;
            characterContainer.anchorMax = Vector2.zero;
            characterContainer.pivot = Vector2.zero;
            characterContainer.anchoredPosition = new Vector2(80f, 50f);
            characterContainer.sizeDelta = new Vector2(400f, 480f);
            characterContainer.localScale = Vector3.one;
            characterContainer.gameObject.AddComponent<UIJuiceAnimator>();

            Image characterImage = CreateImage("CharacterImage", characterContainer, poses.Intro, Color.white, false);
            Stretch(characterImage.rectTransform);
            characterImage.preserveAspect = true;

            Image bubble = CreateImage("DialogueBubble", overlayRect, LoadSprite(PanelSpritePath), new Color(1f, 1f, 1f, 0.98f), true);
            RectTransform bubbleRect = bubble.rectTransform;
            bubbleRect.anchorMin = new Vector2(0.5f, 0.5f);
            bubbleRect.anchorMax = new Vector2(0.5f, 0.5f);
            bubbleRect.pivot = new Vector2(0.5f, 0.5f);
            bubbleRect.anchoredPosition = new Vector2(230f, 40f);
            bubbleRect.sizeDelta = new Vector2(1050f, 360f);
            bubbleRect.localScale = Vector3.one;
            bubble.type = Image.Type.Sliced;

            TMP_Text dialogueText = CreateText("DialogueText", steps.Length > 0 ? steps[0] : string.Empty, bubbleRect, 38f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, TextDark);
            Stretch(dialogueText.rectTransform, new Vector2(56f, 106f), new Vector2(-56f, -42f));
            dialogueText.enableAutoSizing = true;
            dialogueText.fontSizeMin = 30f;
            dialogueText.fontSizeMax = 40f;
            dialogueText.alpha = 1f;

            Button nextButton = CreateButton("NextButton", "Siguiente", bubbleRect, Vector2.zero, new Vector2(220f, 70f), LoadSprite(ButtonGreenPath), TextLight);
            RectTransform nextRect = nextButton.GetComponent<RectTransform>();
            nextRect.anchorMin = new Vector2(1f, 0f);
            nextRect.anchorMax = new Vector2(1f, 0f);
            nextRect.pivot = new Vector2(1f, 0f);
            nextRect.anchoredPosition = new Vector2(-48f, 38f);
            nextRect.sizeDelta = new Vector2(220f, 70f);
            TMP_Text nextText = nextButton.GetComponentInChildren<TMP_Text>(true);

            RemoveLayoutComponents(overlay.transform);

            CharacterDialogueController dialogue = overlay.GetComponent<CharacterDialogueController>();
            SerializedObject so = new(dialogue);
            SetReference(so, "overlayGroup", group);
            SetReference(so, "characterRoot", characterContainer);
            SetReference(so, "characterImage", characterImage);
            SetReference(so, "bubbleRoot", bubbleRect);
            SetReference(so, "dialogueText", dialogueText);
            SetReference(so, "nextButton", nextButton);
            SetReference(so, "nextButtonText", nextText);
            SetBool(so, "showIntroOnStart", true);
            SetString(so, "finalIntroButtonText", finalButtonText);
            SetString(so, "retryMessage", "Casi lo logras. Observa con calma e intenta otra vez.");
            SetString(so, "retryButtonText", "Intentar otra vez");
            SetReference(so, "retryCharacterSprite", poses.Retry);
            SetString(so, "successButtonText", "Continuar");
            SetFloat(so, "successAutoHideSeconds", 0.85f);
            SetFloat(so, "typewriterCharactersPerSecond", 0f);
            SetIntroSteps(so.FindProperty("introSteps"), steps, poses.Intro, finalButtonText);
            SetStringArray(so.FindProperty("successMessages"), new[] { "Muy bien.", "Lo lograste.", "Excelente trabajo." });
            SetObjectArray(so.FindProperty("successCharacterSprites"), new UnityEngine.Object[] { poses.Success });
            so.ApplyModifiedPropertiesWithoutUndo();

            ClearPersistentListeners(dialogue.OnIntroCompleted);
            if (onIntroCompleted != null)
            {
                UnityEventTools.AddPersistentListener(dialogue.OnIntroCompleted, onIntroCompleted);
            }

            GameplayDialogueFeedbackBridge bridge = overlay.GetComponent<GameplayDialogueFeedbackBridge>();
            SerializedObject bridgeSo = new(bridge);
            SetReference(bridgeSo, "dialogue", dialogue);
            SetReference(bridgeSo, "sizeWorldController", ComponentsInScene<SizeWorldController>(scene).FirstOrDefault());
            SetReference(bridgeSo, "emotionGameManager", ComponentsInScene<EmotionGameManager>(scene).FirstOrDefault());
            SetReference(bridgeSo, "musicalGame", ComponentsInScene<MundoMusicalSequenceGame>(scene).FirstOrDefault());
            SetReference(bridgeSo, "storyController", ComponentsInScene<VoiceRecognitionTest>(scene).FirstOrDefault());
            SetBool(bridgeSo, "showSuccessFeedback", true);
            SetBool(bridgeSo, "showRetryFeedback", true);
            bridgeSo.ApplyModifiedPropertiesWithoutUndo();

            EnsureCanvasAccessibility(canvas);
        }

        private static void NormalizeMenuButtons(Scene scene)
        {
            string[] labels = { "Jugar", "Controles", "Opciones", "Salir" };
            float[] yPositions = { 135f, 35f, -65f, -165f };
            for (int i = 0; i < labels.Length; i++)
            {
                foreach (Button button in FindButtonsByLabel(scene, labels[i]))
                {
                    RectTransform rect = button.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = new Vector2(0f, yPositions[i]);
                    rect.sizeDelta = new Vector2(320f, 78f);
                    rect.localScale = Vector3.one;
                    StyleButton(button, LoadSprite(ButtonSkybluePath), TextLight);
                }
            }
        }

        private static void NormalizeWorldSelectionCards(Scene scene)
        {
            RectTransform[] cards = ComponentsInScene<RectTransform>(scene)
                .Where(rect => rect.name.StartsWith("Card-Mundo", StringComparison.OrdinalIgnoreCase))
                .OrderBy(rect => WorldCardOrder(rect.name))
                .ToArray();

            if (cards.Length == 0) return;

            float[] anchors = { 0.17f, 0.39f, 0.61f, 0.83f };
            for (int i = 0; i < cards.Length; i++)
            {
                RectTransform card = cards[i];
                RemoveLayoutComponents(card);
                SetManualRect(card, new Vector2(anchors[Mathf.Min(i, anchors.Length - 1)], 0.50f), Vector2.zero, new Vector2(310f, 430f));

                SetChildRect(card, "Icono", new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(74f, 74f));
                SetChildRect(card, "Imagen-mundo", new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(188f, 150f));
                SetChildRect(card, "Nombre", new Vector2(0.5f, 0.40f), Vector2.zero, new Vector2(270f, 54f));
                SetChildRect(card, "Mensaje", new Vector2(0.5f, 0.25f), Vector2.zero, new Vector2(260f, 78f));
                SetChildRect(card, "Candado", new Vector2(0.88f, 0.88f), Vector2.zero, new Vector2(54f, 54f));

                RectTransform stars = FindRect(card, "Estrellas");
                if (stars != null)
                {
                    SetManualRect(stars, new Vector2(0.5f, 0.08f), Vector2.zero, new Vector2(190f, 48f));
                    SetChildRect(stars, "Estrella-1", new Vector2(0.25f, 0.5f), Vector2.zero, new Vector2(42f, 42f));
                    SetChildRect(stars, "Estrella-2", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(42f, 42f));
                    SetChildRect(stars, "Estrella-3", new Vector2(0.75f, 0.5f), Vector2.zero, new Vector2(42f, 42f));
                }
            }
        }

        private static int WorldCardOrder(string name)
        {
            if (ContainsIgnoreCase(name, "Cuentos")) return 0;
            if (ContainsIgnoreCase(name, "Musical")) return 1;
            if (ContainsIgnoreCase(name, "Tama")) return 2;
            if (ContainsIgnoreCase(name, "Emociones")) return 3;
            return 9;
        }

        private static void NormalizeMusicPanels(Scene scene)
        {
            RectTransform startPanel = FindObject(scene, "PanelInicio")?.GetComponent<RectTransform>();
            if (startPanel != null)
            {
                RemoveLayoutComponents(startPanel);
                SetManualRect(startPanel, new Vector2(0.5f, 0.53f), Vector2.zero, new Vector2(760f, 420f));
                SetChildRect(startPanel, "Titulo", new Vector2(0.5f, 0.80f), Vector2.zero, new Vector2(660f, 66f));
                SetChildRect(startPanel, "Instruccion", new Vector2(0.5f, 0.53f), Vector2.zero, new Vector2(650f, 130f));
                SetChildRect(startPanel, "Button-iniciar", new Vector2(0.5f, 0.20f), Vector2.zero, new Vector2(300f, 78f));
            }

            RectTransform resultPanel = FindObject(scene, "PanelResultado")?.GetComponent<RectTransform>();
            if (resultPanel != null)
            {
                RemoveLayoutComponents(resultPanel);
                SetManualRect(resultPanel, new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(680f, 360f));
                SetChildRect(resultPanel, "TextoResultado", new Vector2(0.5f, 0.66f), Vector2.zero, new Vector2(560f, 120f));
                SetChildRect(resultPanel, "Button-volver-seleccion", new Vector2(0.5f, 0.25f), Vector2.zero, new Vector2(340f, 72f));
            }

            RectTransform stars = FindObject(scene, "PanelEstrellas")?.GetComponent<RectTransform>();
            if (stars != null)
            {
                RemoveLayoutComponents(stars);
                stars.anchorMin = new Vector2(1f, 1f);
                stars.anchorMax = new Vector2(1f, 1f);
                stars.pivot = new Vector2(1f, 1f);
                stars.anchoredPosition = new Vector2(-34f, -34f);
                stars.sizeDelta = new Vector2(170f, 50f);
                stars.localScale = Vector3.one;
                SetChildRect(stars, "Estrella-1", new Vector2(0.17f, 0.5f), Vector2.zero, new Vector2(42f, 42f));
                SetChildRect(stars, "Estrella-2", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(42f, 42f));
                SetChildRect(stars, "Estrella-3", new Vector2(0.83f, 0.5f), Vector2.zero, new Vector2(42f, 42f));
            }
        }

        private static void NormalizeSizePanels(Scene scene)
        {
            RectTransform content = FindObject(scene, "Contenido")?.GetComponent<RectTransform>();
            if (content != null)
            {
                RemoveLayoutComponents(content);
                SetChildRect(content, "Texto-resultado", new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(620f, 72f));
                SetChildRect(content, "Texto-feedback", new Vector2(0.5f, 0.50f), Vector2.zero, new Vector2(620f, 110f));
                SetChildRect(content, "Botones-resultado", new Vector2(0.5f, 0.20f), Vector2.zero, new Vector2(520f, 82f));
                RectTransform resultButtons = FindRect(content, "Botones-resultado");
                if (resultButtons != null)
                {
                    RemoveLayoutComponents(resultButtons);
                    SetChildRect(resultButtons, "Boton-reintentar", new Vector2(0.28f, 0.5f), Vector2.zero, new Vector2(220f, 70f));
                    SetChildRect(resultButtons, "Boton-volver-seleccion", new Vector2(0.72f, 0.5f), Vector2.zero, new Vector2(260f, 70f));
                }
            }

            RectTransform feedback = FindObject(scene, "Texto-feedback")?.GetComponent<RectTransform>();
            RemoveLayoutComponents(feedback);
        }

        private static void NormalizeEmotionPanels(Scene scene)
        {
            foreach (Button button in ComponentsInScene<Button>(scene))
            {
                if (!ContainsIgnoreCase(button.name, "Button")) continue;
                RemoveLayoutComponents(button.transform);
            }

            foreach (Image star in ComponentsInScene<Image>(scene).Where(image => image.name.StartsWith("Star", StringComparison.OrdinalIgnoreCase)))
            {
                RectTransform rect = star.rectTransform;
                RemoveLayoutComponents(rect);
                rect.sizeDelta = new Vector2(48f, 48f);
                rect.localScale = Vector3.one;
            }
        }

        private static void PatchStoryCards(Scene scene)
        {
            string[] names =
            {
                "TarjetaCuento_tres_cerditos",
                "TarjetaCuento_conejo_luna",
                "TarjetaCuento_tortuga_amable"
            };
            float[] anchors = { 0.25f, 0.5f, 0.75f };
            for (int i = 0; i < names.Length; i++)
            {
                GameObject card = FindObject(scene, names[i]);
                if (card == null) continue;
                RemoveLayoutComponentsUpTo(card.transform, "PanelSeleccionCuentos");
                RectTransform rect = card.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(anchors[i], 0.47f);
                rect.anchorMax = rect.anchorMin;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(300f, 240f);
                rect.localScale = Vector3.one;
            }
        }

        private static void ConfigureSizeWorldLayout(SizeWorldController manager)
        {
            SerializedObject so = new(manager);
            RectTransform safeArea = GetReference<RectTransform>(so, "animalSafeArea");
            if (safeArea != null)
            {
                Stretch(safeArea);
                RemoveLayoutComponents(safeArea);
            }

            Button leftButton = GetReference<Button>(so, "leftAnimalButton");
            Button rightButton = GetReference<Button>(so, "rightAnimalButton");
            ConfigureAnimalCard(leftButton, new Vector2(0.07f, 0.22f), new Vector2(0.46f, 0.72f));
            ConfigureAnimalCard(rightButton, new Vector2(0.54f, 0.22f), new Vector2(0.93f, 0.72f));

            ConfigureAnimalImage(GetReference<Image>(so, "leftAnimalImage"));
            ConfigureAnimalImage(GetReference<Image>(so, "rightAnimalImage"));
            ConfigureAnimalName(GetReference<TMP_Text>(so, "leftAnimalNameText"));
            ConfigureAnimalName(GetReference<TMP_Text>(so, "rightAnimalNameText"));

            TMP_Text questionText = GetReference<TMP_Text>(so, "questionText");
            if (questionText != null)
            {
                RectTransform header = questionText.rectTransform.parent as RectTransform;
                if (header != null)
                {
                    header.anchorMin = new Vector2(0.125f, 0.80f);
                    header.anchorMax = new Vector2(0.875f, 0.96f);
                    header.offsetMin = Vector2.zero;
                    header.offsetMax = Vector2.zero;
                    header.localScale = Vector3.one;
                    RemoveLayoutComponents(header);
                }

                questionText.alignment = TextAlignmentOptions.Center;
                questionText.enableAutoSizing = true;
                questionText.fontSizeMin = 28f;
                questionText.fontSizeMax = 42f;
            }

            Button returnButton = GetReference<Button>(so, "returnButton");
            if (returnButton != null)
            {
                SetTopLeftButton(returnButton, new Vector2(110f, -55f), new Vector2(170f, 60f));
                StyleButton(returnButton, LoadSprite(ButtonSkybluePath), TextLight);
            }

            foreach (Button button in FindButtonsByLabel(manager.gameObject.scene, "Validar"))
            {
                RectTransform rect = button.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 42f);
                rect.sizeDelta = new Vector2(300f, 80f);
                rect.localScale = Vector3.one;
                StyleButton(button, LoadSprite(ButtonGreenPath), TextLight);
                RemoveLayoutComponents(button.transform);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureAnimalCard(Button button, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (button == null) return;
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            RemoveLayoutComponents(button.transform);
            StyleButton(button, LoadSprite(PanelSpritePath), TextDark);
            if (button.image != null)
            {
                button.image.color = new Color(1f, 1f, 1f, 0.96f);
                button.image.type = Image.Type.Sliced;
            }
        }

        private static void ConfigureAnimalImage(Image image)
        {
            if (image == null) return;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.11f, 0.22f);
            rect.anchorMax = new Vector2(0.89f, 0.88f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private static void ConfigureAnimalName(TMP_Text label)
        {
            if (label == null) return;
            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(0.08f, 0.05f);
            rect.anchorMax = new Vector2(0.92f, 0.19f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 26f;
            label.fontSizeMax = 36f;
            label.color = TextDark;
        }

        private static void WireStoryBridge(Scene scene, VoiceRecognitionTest manager)
        {
            CharacterDialogueController dialogue = ComponentsInScene<CharacterDialogueController>(scene).FirstOrDefault();
            GameplayDialogueFeedbackBridge bridge = dialogue != null ? dialogue.GetComponent<GameplayDialogueFeedbackBridge>() : null;
            if (bridge == null || manager == null) return;
            SerializedObject bridgeSo = new(bridge);
            SetReference(bridgeSo, "storyController", manager);
            bridgeSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureGeneratedCharacterPoses()
        {
            Directory.CreateDirectory(PoseFolder);
            GeneratePose(LuliSheetPath, CharacterPoseSet.LuliIntroPath, PoseQuadrant.BottomRight);
            GeneratePose(LuliSheetPath, CharacterPoseSet.LuliSuccessPath, PoseQuadrant.TopRight);
            GeneratePose(LuliSheetPath, CharacterPoseSet.LuliRetryPath, PoseQuadrant.BottomLeft);

            GeneratePose(TamborcinSheetPath, CharacterPoseSet.TamborcinIntroPath, PoseQuadrant.BottomLeft);
            GeneratePose(TamborcinSheetPath, CharacterPoseSet.TamborcinSuccessPath, PoseQuadrant.TopRight);
            GeneratePose(TamborcinSheetPath, CharacterPoseSet.TamborcinRetryPath, PoseQuadrant.BottomRight);

            GeneratePose(BiblioSheetPath, CharacterPoseSet.BiblioIntroPath, PoseQuadrant.TopRight);
            GeneratePose(BiblioSheetPath, CharacterPoseSet.BiblioSuccessPath, PoseQuadrant.BottomLeft);
            GeneratePose(BiblioSheetPath, CharacterPoseSet.BiblioRetryPath, PoseQuadrant.BottomRight);

            GeneratePose(ExploradorSheetPath, CharacterPoseSet.ExploradorIntroPath, PoseQuadrant.TopRight);
            GeneratePose(ExploradorSheetPath, CharacterPoseSet.ExploradorSuccessPath, PoseQuadrant.BottomLeft);
            GeneratePose(ExploradorSheetPath, CharacterPoseSet.ExploradorRetryPath, PoseQuadrant.BottomRight);

            GeneratePose(NunaSheetPath, CharacterPoseSet.NunaIntroPath, PoseQuadrant.TopRight);
            GeneratePose(NunaSheetPath, CharacterPoseSet.NunaSuccessPath, PoseQuadrant.BottomLeft);
            GeneratePose(NunaSheetPath, CharacterPoseSet.NunaRetryPath, PoseQuadrant.BottomRight);
            AssetDatabase.Refresh();
        }

        private static void GeneratePose(string sourcePath, string outputPath, PoseQuadrant quadrant)
        {
            Texture2D source = LoadReadableTexture(sourcePath);
            if (source == null) return;

            RectInt quadrantRect = GetQuadrantRect(source.width, source.height, quadrant);
            Color[] pixels = source.GetPixels(quadrantRect.x, quadrantRect.y, quadrantRect.width, quadrantRect.height);
            RectInt trim = FindOpaqueBounds(pixels, quadrantRect.width, quadrantRect.height);
            Color[] trimmed = ExtractPixels(pixels, quadrantRect.width, trim);

            Texture2D output = new(trim.width, trim.height, TextureFormat.RGBA32, false);
            output.SetPixels(trimmed);
            output.Apply();

            File.WriteAllBytes(outputPath, output.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(output);
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);

            if (AssetImporter.GetAtPath(outputPath) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.spritePixelsPerUnit = 100f;
                importer.SaveAndReimport();
            }
        }

        private static Texture2D LoadReadableTexture(string sourcePath)
        {
            if (AssetImporter.GetAtPath(sourcePath) is not TextureImporter importer) return null;
            bool needsReimport = false;
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                needsReimport = true;
            }

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true;
                needsReimport = true;
            }

            if (needsReimport) importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
        }

        private static RectInt GetQuadrantRect(int width, int height, PoseQuadrant quadrant)
        {
            int halfWidth = width / 2;
            int halfHeight = height / 2;
            return quadrant switch
            {
                PoseQuadrant.TopLeft => new RectInt(0, halfHeight, halfWidth, height - halfHeight),
                PoseQuadrant.TopRight => new RectInt(halfWidth, halfHeight, width - halfWidth, height - halfHeight),
                PoseQuadrant.BottomLeft => new RectInt(0, 0, halfWidth, halfHeight),
                PoseQuadrant.BottomRight => new RectInt(halfWidth, 0, width - halfWidth, halfHeight),
                _ => new RectInt(0, 0, width, height)
            };
        }

        private static RectInt FindOpaqueBounds(Color[] pixels, int width, int height)
        {
            int minX = width;
            int minY = height;
            int maxX = 0;
            int maxY = 0;
            bool found = false;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (pixels[y * width + x].a <= 0.05f) continue;
                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                    found = true;
                }
            }

            if (!found) return new RectInt(0, 0, width, height);
            const int padding = 24;
            minX = Mathf.Max(0, minX - padding);
            minY = Mathf.Max(0, minY - padding);
            maxX = Mathf.Min(width - 1, maxX + padding);
            maxY = Mathf.Min(height - 1, maxY + padding);
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static Color[] ExtractPixels(Color[] pixels, int sourceWidth, RectInt bounds)
        {
            Color[] output = new Color[bounds.width * bounds.height];
            for (int y = 0; y < bounds.height; y++)
            {
                Array.Copy(
                    pixels,
                    (bounds.y + y) * sourceWidth + bounds.x,
                    output,
                    y * bounds.width,
                    bounds.width);
            }

            return output;
        }

        private static Canvas EnsureCanvas(Scene scene, string preferredName)
        {
            Canvas canvas = ComponentsInScene<Canvas>(scene).FirstOrDefault(item => item.gameObject.name == preferredName)
                            ?? ComponentsInScene<Canvas>(scene).FirstOrDefault(item => item.isRootCanvas)
                            ?? ComponentsInScene<Canvas>(scene).FirstOrDefault();
            if (canvas == null)
            {
                GameObject canvasObject = new(preferredName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            ConfigureScaler(canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>());
            if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
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
            scaler.referencePixelsPerUnit = 100f;
        }

        private static void EnsureCanvasAccessibility(Canvas canvas)
        {
            CanvasColorBlindAccessibility accessibility = canvas.GetComponent<CanvasColorBlindAccessibility>() ?? canvas.gameObject.AddComponent<CanvasColorBlindAccessibility>();
            SerializedObject so = new(accessibility);
            SetReference(so, "targetCanvas", canvas);
            Graphic[] graphics = canvas.GetComponentsInChildren<Graphic>(true)
                .Where(graphic => graphic is Image || graphic is RawImage)
                .ToArray();
            SetObjectArray(so.FindProperty("targetGraphics"), graphics.Cast<UnityEngine.Object>().ToArray());
            so.ApplyModifiedPropertiesWithoutUndo();
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

        private static void EnsureBackground(Transform canvas, string name, Sprite sprite, Color color, bool raycast, bool preserveAspect)
        {
            DestroyChildIfExists(canvas, name);
            Image background = CreateImage(name, canvas, sprite, color, raycast);
            Stretch(background.rectTransform);
            background.preserveAspect = preserveAspect;
            background.transform.SetAsFirstSibling();
        }

        private static void StylePianoKeys(Scene scene)
        {
            foreach (Button button in ComponentsInScene<Button>(scene))
            {
                string lower = button.gameObject.name.ToLowerInvariant();
                if (!lower.Contains("tecla") && !lower.Contains("key")) continue;
                RectTransform rect = button.GetComponent<RectTransform>();
                rect.localScale = Vector3.one;
                rect.sizeDelta = new Vector2(Mathf.Clamp(rect.sizeDelta.x, 110f, 145f), Mathf.Clamp(rect.sizeDelta.y, 220f, 280f));
                RemoveLayoutComponents(button.transform);
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.fontSize = Mathf.Clamp(Mathf.Max(label.fontSize, 52f), 48f, 60f);
                    label.alignment = TextAlignmentOptions.Center;
                    label.fontStyle |= FontStyles.Bold;
                }
            }
        }

        private static void SetTopLeftButton(Button button, Vector2 anchoredPosition, Vector2 size)
        {
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            RemoveLayoutComponents(button.transform);
        }

        private static void StyleButton(Button button, Sprite sprite, Color textColor)
        {
            if (button.GetComponent<UIButtonFeedback>() == null) button.gameObject.AddComponent<UIButtonFeedback>();
            Image image = button.image != null ? button.image : button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = sprite;
                image.type = sprite == null ? Image.Type.Simple : Image.Type.Sliced;
                image.color = Color.white;
                image.raycastTarget = true;
                button.targetGraphic = image;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null) return;
            label.enableAutoSizing = true;
            label.fontSizeMin = 22f;
            label.fontSizeMax = 34f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = textColor;
            label.fontStyle |= FontStyles.Bold;
            label.raycastTarget = false;
            Stretch(label.rectTransform, new Vector2(16f, 8f), new Vector2(-16f, -8f));
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
            rect.localScale = Vector3.one;

            Button button = buttonObject.GetComponent<Button>();
            StyleButton(button, sprite, textColor);
            if (!string.IsNullOrEmpty(label))
            {
                TMP_Text text = CreateText("Text", label, rect, 30f, FontStyles.Bold, TextAlignmentOptions.Center, textColor);
                Stretch(text.rectTransform, new Vector2(16f, 8f), new Vector2(-16f, -8f));
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
            tmp.rectTransform.localScale = Vector3.one;
            return tmp;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool raycastTarget)
        {
            GameObject imageObject = CreateUiObject(name, parent, typeof(Image));
            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycastTarget;
            image.preserveAspect = sprite != null;
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
            rect.localScale = Vector3.one;
        }

        private static void SetManualRect(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            if (rect == null) return;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static void SetChildRect(Transform parent, string childName, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            RectTransform rect = FindRect(parent, childName);
            if (rect == null) return;
            SetManualRect(rect, anchor, anchoredPosition, size);

            TMP_Text label = rect.GetComponent<TMP_Text>();
            if (label != null)
            {
                label.enableAutoSizing = true;
                label.fontSizeMin = Mathf.Max(18f, Mathf.Min(label.fontSize, 24f));
                label.fontSizeMax = Mathf.Clamp(Mathf.Max(label.fontSize, 30f), 28f, 42f);
                label.alignment = TextAlignmentOptions.Center;
            }

            Image image = rect.GetComponent<Image>();
            if (image != null) image.preserveAspect = true;
        }

        private static Sprite LoadSprite(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath)) return null;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null) return sprite;
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer) return null;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static void RemoveLayoutComponents(Transform root)
        {
            if (root == null) return;
            foreach (LayoutGroup component in root.GetComponentsInChildren<LayoutGroup>(true).Reverse())
            {
                UnityEngine.Object.DestroyImmediate(component, true);
            }

            foreach (ContentSizeFitter component in root.GetComponentsInChildren<ContentSizeFitter>(true).Reverse())
            {
                UnityEngine.Object.DestroyImmediate(component, true);
            }

            foreach (LayoutElement component in root.GetComponentsInChildren<LayoutElement>(true).Reverse())
            {
                UnityEngine.Object.DestroyImmediate(component, true);
            }
        }

        private static void RemoveLayoutComponentsUpTo(Transform start, string stopName)
        {
            Transform current = start;
            while (current != null)
            {
                RemoveLayoutComponents(current);
                if (current.name == stopName) break;
                current = current.parent;
            }
        }

        private static IEnumerable<Button> FindButtonsByLabel(Scene scene, string labelText)
        {
            return ComponentsInScene<Button>(scene).Where(button =>
            {
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                return label != null && label.text.Trim().IndexOf(labelText, StringComparison.OrdinalIgnoreCase) >= 0;
            });
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

        private static RectTransform FindRect(Transform root, string objectName)
        {
            Transform match = FindChildRecursive(root, objectName);
            return match != null ? match.GetComponent<RectTransform>() : null;
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

        private static bool ContainsIgnoreCase(string value, string expected)
        {
            return value != null && expected != null && value.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void DestroyIfExists(Scene scene, string objectName)
        {
            GameObject existing = FindObject(scene, objectName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing, true);
            }
        }

        private static void RemoveRootIfExists(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == objectName)
                {
                    UnityEngine.Object.DestroyImmediate(root, true);
                    return;
                }
            }
        }

        private static void DestroyChildIfExists(Transform parent, string childName)
        {
            if (parent == null) return;
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

        private static void SetIntroSteps(SerializedProperty property, string[] steps, Sprite sprite, string finalButtonText)
        {
            if (property == null) return;
            property.arraySize = steps.Length;
            for (int i = 0; i < steps.Length; i++)
            {
                SerializedProperty step = property.GetArrayElementAtIndex(i);
                step.FindPropertyRelative("message").stringValue = steps[i];
                step.FindPropertyRelative("characterSprite").objectReferenceValue = sprite;
                step.FindPropertyRelative("buttonText").stringValue = i == steps.Length - 1 ? finalButtonText : "Siguiente";
            }
        }

        private static void SetStringArray(SerializedProperty property, string[] values)
        {
            if (property == null) return;
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).stringValue = values[i];
            }
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

        private readonly struct CharacterPoseSet
        {
            public const string LuliIntroPath = PoseFolder + "/Luli_saludo.png";
            public const string LuliSuccessPath = PoseFolder + "/Luli_alegre.png";
            public const string LuliRetryPath = PoseFolder + "/Luli_animo.png";
            public const string TamborcinIntroPath = PoseFolder + "/Tamborcin_saludo.png";
            public const string TamborcinSuccessPath = PoseFolder + "/Tamborcin_alegre.png";
            public const string TamborcinRetryPath = PoseFolder + "/Tamborcin_animo.png";
            public const string BiblioIntroPath = PoseFolder + "/Biblio_saludo.png";
            public const string BiblioSuccessPath = PoseFolder + "/Biblio_alegre.png";
            public const string BiblioRetryPath = PoseFolder + "/Biblio_animo.png";
            public const string ExploradorIntroPath = PoseFolder + "/Explorador_saludo.png";
            public const string ExploradorSuccessPath = PoseFolder + "/Explorador_alegre.png";
            public const string ExploradorRetryPath = PoseFolder + "/Explorador_animo.png";
            public const string NunaIntroPath = PoseFolder + "/Nuna_saludo.png";
            public const string NunaSuccessPath = PoseFolder + "/Nuna_alegre.png";
            public const string NunaRetryPath = PoseFolder + "/Nuna_animo.png";

            public CharacterPoseSet(Sprite intro, Sprite success, Sprite retry)
            {
                Intro = intro;
                Success = success;
                Retry = retry;
            }

            public Sprite Intro { get; }
            public Sprite Success { get; }
            public Sprite Retry { get; }

            public static CharacterPoseSet Luli() => new(LoadSprite(LuliIntroPath), LoadSprite(LuliSuccessPath), LoadSprite(LuliRetryPath));
            public static CharacterPoseSet Tamborcin() => new(LoadSprite(TamborcinIntroPath), LoadSprite(TamborcinSuccessPath), LoadSprite(TamborcinRetryPath));
            public static CharacterPoseSet Biblio() => new(LoadSprite(BiblioIntroPath), LoadSprite(BiblioSuccessPath), LoadSprite(BiblioRetryPath));
            public static CharacterPoseSet Explorador() => new(LoadSprite(ExploradorIntroPath), LoadSprite(ExploradorSuccessPath), LoadSprite(ExploradorRetryPath));
            public static CharacterPoseSet Nuna() => new(LoadSprite(NunaIntroPath), LoadSprite(NunaSuccessPath), LoadSprite(NunaRetryPath));
        }
    }
}
#endif
