#if UNITY_EDITOR
using System;
using System.Linq;
using Bolin;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bolin.Editor
{
    public static class MundoCuentosExpansionSetup
    {
        private const string StoryScenePath = "Assets/Scenes/MundoCuentos_VozTest.unity";
        private const string LibraryBackgroundPath = "Assets/Mundo Aprendo/Fondos/Mundo biblioteca/Biblioteca.png";
        private const string FullStarPath = "Assets/Mundo Aprendo/Imagenes/Prefabs/Star_Full.png";
        private const string EmptyStarPath = "Assets/Mundo Aprendo/Imagenes/Prefabs/Star_Empty.png";
        private const string BiblioIntroPath = "Assets/Mundo Aprendo/Personajes/_Poses/Biblio_saludo.png";
        private const string BiblioSuccessPath = "Assets/Mundo Aprendo/Personajes/_Poses/Biblio_alegre.png";
        private const string BiblioEncouragementPath = "Assets/Mundo Aprendo/Personajes/_Poses/Biblio_animo.png";
        private const string ThreePigsIconPath = "Assets/Mundo Aprendo/MundoCuentos/Arte/Pictograma_TresCerditos.png";
        private const string RabbitIconPath = "Assets/Mundo Aprendo/MundoCuentos/Arte/Pictograma_ConejoLuna.png";
        private const string TurtleIconPath = "Assets/Mundo Aprendo/MundoCuentos/Arte/Pictograma_TortugaAmable.png";
        private const string PanelBrownPath = "Assets/Cartoon UI/Panels/Panel Brown.png";
        private const string PanelLightPath = "Assets/Cartoon UI/Panels/Panel Light.png";
        private const string ButtonGreenPath = "Assets/Cartoon UI/Buttons/Long Round/Long Round Green.png";
        private const string ButtonBluePath = "Assets/Cartoon UI/Buttons/Long Round/Long Round Blue.png";
        private const string ButtonPurplePath = "Assets/Cartoon UI/Buttons/Long Round/Long Round Purple.png";
        private const string ButtonYellowPath = "Assets/Cartoon UI/Buttons/Long Round/Long Round Yellow.png";
        private const string ButtonOrangePath = "Assets/Cartoon UI/Buttons/Long Round/Long Round Orange.png";
        private const string VoiceOnPath = "Assets/Cartoon UI/White Icons/White Icons/White Icons 2/White Voice On.png";
        private const string DefaultStoryText =
            "Había una vez tres cerditos que vivían cerca del bosque. Cada uno construyó una casa para protegerse del lobo. El primer cerdito hizo una casa de paja, el segundo una casa de madera y el tercero una casa de ladrillos. Cuando llegó el lobo, solo la casa de ladrillos permaneció firme. Los tres cerditos entraron en ella y estuvieron seguros.";
        private const string RabbitStoryText =
            "Un conejo pequeño miraba la luna cada noche. Pensaba que era una lámpara brillante que cuidaba el bosque. Una noche, el cielo se llenó de nubes y la luna desapareció. El conejo esperó con paciencia. Cuando las nubes se alejaron, la luna volvió a brillar y el conejo regresó feliz a su casa.";
        private const string TurtleStoryText =
            "Una tortuga caminaba lentamente junto al río. En el camino encontró a un pajarito que no podía llegar a su nido. La tortuga lo llevó sobre su caparazón hasta el árbol. El pajarito agradeció su ayuda y desde ese día fueron grandes amigos.";

        private static readonly Color Purple = new(0.33f, 0.24f, 0.55f, 1f);
        private static readonly Color Pink = new(0.9f, 0.45f, 0.58f, 1f);
        private static readonly Color Blue = new(0.22f, 0.5f, 0.72f, 1f);
        private static readonly Color Cream = new(1f, 0.96f, 0.84f, 1f);
        private static readonly Color Card = new(1f, 1f, 1f, 0.95f);

        [MenuItem("Mundo Aprendo/Aplicar ampliacion Mundo Cuentos")]
        public static void ApplyFromMenu()
        {
            Apply();
        }

        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(StoryScenePath, OpenSceneMode.Single);
            PatchScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"No se pudo guardar {StoryScenePath}.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("MundoCuentosExpansionSetup: selector, lector y resultado persistidos en MundoCuentos_VozTest.");
        }

        private static void PatchScene(Scene scene)
        {
            VoiceRecognitionTest manager = ComponentsInScene<VoiceRecognitionTest>(scene).Single();
            RectTransform canvas = RequireRect(scene, "Canvas - Mundo Cuentos Voz Test");
            NormalizeCanvas(canvas);
            EnsureLibraryBackground(scene, canvas);

            DestroyIfExists(scene, "SupportModePanel");
            DestroyIfExists(scene, "ReadingProgressBar");
            DestroyIfExists(scene, "TextoPorcentaje");
            DestroyIfExists(scene, "TutorialOverlay");
            DestroyIfExists(scene, "StoryTutorialOverlay");
            DestroyIfExists(scene, "Contenido");

            RectTransform readingPanel = EnsurePanel(canvas, "PanelLecturaCuento", false, new Color(1f, 1f, 1f, 0f));
            EnsureReadingBaseObjects(scene, readingPanel);
            MoveReadingObjectsIntoPanel(scene, readingPanel);

            RectTransform selectionPanel = EnsurePdfSelectionPanel(canvas);
            RectTransform resultPanel = EnsurePdfResultPanel(canvas);

            ScrollRect recognizedScroll = EnsureExistingScroll(scene, "RecognizedTextCard", "ScrollViewLecturaReconocida", "TextoLecturaReconocida");
            ScrollRect storyScroll = EnsureExistingScroll(scene, "StoryTextCard", "ScrollViewCuerpoCuento", "TextoHistoria");

            Button startButton = RequireComponent<Button>(scene, "BotonIniciarVoz");
            Button stopButton = RequireComponent<Button>(scene, "BotonDetenerVoz");
            Button validateButton = RequireComponent<Button>(scene, "BotonValidarLectura");
            Button clearButton = RequireComponent<Button>(scene, "BotonLimpiarTexto");
            Button backToSelectionButton = RequireComponent<Button>(scene, "BotonVolver");
            ClearPersistentListeners(startButton.onClick);
            UnityEventTools.AddPersistentListener(startButton.onClick, manager.StartListening);
            ClearPersistentListeners(stopButton.onClick);
            UnityEventTools.AddPersistentListener(stopButton.onClick, manager.StopListening);
            ClearPersistentListeners(validateButton.onClick);
            UnityEventTools.AddPersistentListener(validateButton.onClick, manager.ValidateReading);
            ClearPersistentListeners(clearButton.onClick);
            UnityEventTools.AddPersistentListener(clearButton.onClick, manager.RestartReading);
            EnsurePdfReadingPanelLayout(
                scene,
                readingPanel,
                storyScroll,
                recognizedScroll,
                startButton,
                stopButton,
                clearButton,
                validateButton,
                backToSelectionButton);

            ClearPersistentListeners(backToSelectionButton.onClick);
            UnityEventTools.AddPersistentListener(backToSelectionButton.onClick, manager.MostrarSeleccionCuentos);

            Button previousMenuButton = RequireComponent<Button>(scene, "BotonVolverMenuAnterior");
            Button otherWorldsButton = RequireComponent<Button>(scene, "BotonVerOtrosMundos");
            ClearPersistentListeners(previousMenuButton.onClick);
            UnityEventTools.AddPersistentListener(previousMenuButton.onClick, manager.ReturnToMenu);
            ClearPersistentListeners(otherWorldsButton.onClick);
            UnityEventTools.AddPersistentListener(otherWorldsButton.onClick, manager.OpenOtherWorlds);

            Button cardOne = RequireComponent<Button>(scene, "TarjetaCuento_tres_cerditos");
            Button cardTwo = RequireComponent<Button>(scene, "TarjetaCuento_conejo_luna");
            Button cardThree = RequireComponent<Button>(scene, "TarjetaCuento_tortuga_amable");
            ClearPersistentListeners(cardOne.onClick);
            UnityEventTools.AddStringPersistentListener(cardOne.onClick, manager.OpenStoryById, StoryProgressRepository.DefaultStoryId);
            ClearPersistentListeners(cardTwo.onClick);
            UnityEventTools.AddStringPersistentListener(cardTwo.onClick, manager.OpenStoryById, "conejo_luna");
            ClearPersistentListeners(cardThree.onClick);
            UnityEventTools.AddStringPersistentListener(cardThree.onClick, manager.OpenStoryById, "tortuga_amable");

            Button resultRepeatButton = RequireComponent<Button>(scene, "BotonRepetirCuento");
            Button resultNextButton = RequireComponent<Button>(scene, "BotonSiguienteCuento");
            Button resultStoriesButton = RequireComponent<Button>(scene, "BotonVolverACuentos");
            Button resultWorldsButton = RequireComponent<Button>(scene, "BotonVolverAMundosResultado");
            ClearPersistentListeners(resultRepeatButton.onClick);
            UnityEventTools.AddPersistentListener(resultRepeatButton.onClick, manager.RepeatCurrentStory);
            ClearPersistentListeners(resultNextButton.onClick);
            UnityEventTools.AddPersistentListener(resultNextButton.onClick, manager.OpenNextStory);
            ClearPersistentListeners(resultStoriesButton.onClick);
            UnityEventTools.AddPersistentListener(resultStoriesButton.onClick, manager.MostrarSeleccionCuentos);
            ClearPersistentListeners(resultWorldsButton.onClick);
            UnityEventTools.AddPersistentListener(resultWorldsButton.onClick, manager.ReturnToWorldsFromResult);

            StoryTutorialAnimationController tutorial = EnsureStoryTutorial(scene, canvas, selectionPanel, readingPanel, startButton, validateButton);
            RectTransform resultGuide = RequireRect(scene, "GuideCharacter_Biblio_Result");
            StoryLevelAnimationController levelAnimator = ConfigureLevelAnimator(
                manager,
                selectionPanel,
                readingPanel,
                resultPanel,
                RequireObject(scene, "MicrophoneListeningIndicator").transform as RectTransform,
                resultGuide,
                new[] { cardOne.transform as RectTransform, cardTwo.transform as RectTransform, cardThree.transform as RectTransform },
                new[] { startButton.transform as RectTransform, stopButton.transform as RectTransform, clearButton.transform as RectTransform, validateButton.transform as RectTransform });
            UIStarDisplay resultStarDisplay = RequireComponent<UIStarDisplay>(scene, "StarsContainer");
            StoryResultStarAnimation resultAnimation = RequireComponent<StoryResultStarAnimation>(scene, "DecorativeStarLoop");
            Image[] readingStars = RequireRect(scene, "PanelEstrellas")
                .GetComponentsInChildren<Image>(true)
                .Where(image => image.name.StartsWith("Star_", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            SerializedObject so = new(manager);
            SetReference(so, "storySelectionPanel", selectionPanel.gameObject);
            SetReference(so, "readingPanel", readingPanel.gameObject);
            SetReference(so, "resultPanel", resultPanel.gameObject);
            SetReference(so, "selectionProgressText", RequireComponent<TMP_Text>(scene, "TextoProgresoCuentos"));
            SetReference(so, "otherWorldsUnlockText", RequireComponent<TMP_Text>(scene, "TextoDesbloqueoOtrosMundos"));
            SetReference(so, "backToPreviousMenuButton", previousMenuButton);
            SetReference(so, "otherWorldsButton", otherWorldsButton);
            SetReference(so, "selectedStoryTitleText", RequireComponent<TMP_Text>(scene, "TituloCuento"));
            SetReference(so, "selectedStoryIconImage", RequireComponent<Image>(scene, "ImagenCuentoSeleccionado"));
            SetReference(so, "storyText", RequireComponent<TMP_Text>(scene, "TextoHistoria"));
            SetReference(so, "recognizedText", RequireComponent<TMP_Text>(scene, "TextoLecturaReconocida"));
            SetReference(so, "recognizedPlaceholderText", RequireComponent<TMP_Text>(scene, "TextoLecturaPlaceholder"));
            SetReference(so, "partialRecognizedText", RequireComponent<TMP_Text>(scene, "TextoLecturaParcial"));
            SetReference(so, "finalRecognizedTextDisplay", RequireComponent<TMP_Text>(scene, "TextoLecturaFinalInterna"));
            SetReference(so, "statusText", RequireComponent<TMP_Text>(scene, "TextoEstado"));
            SetReference(so, "readingResultText", RequireComponent<TMP_Text>(scene, "TextoResultadoLectura"));
            SetReference(so, "countdownText", RequireComponent<TMP_Text>(scene, "TextoContador"));
            SetReference(so, "errorText", RequireComponent<TMP_Text>(scene, "ErrorText"));
            SetReference(so, "starsResultText", RequireComponent<TMP_Text>(scene, "StarsResultText"));
            SetReference(so, "resultStoryTitleText", RequireComponent<TMP_Text>(scene, "TextoResultadoCuento"));
            SetReference(so, "resultScoreText", RequireComponent<TMP_Text>(scene, "TextoResultadoPuntaje"));
            SetReference(so, "resultStarsText", RequireComponent<TMP_Text>(scene, "TextoResultadoEstrellas"));
            SetReference(so, "resultMessageText", RequireComponent<TMP_Text>(scene, "TextoResultadoMensaje"));
            SetReference(so, "resultRepeatButton", resultRepeatButton);
            SetReference(so, "resultNextStoryButton", resultNextButton);
            SetReference(so, "resultBackToStoriesButton", resultStoriesButton);
            SetReference(so, "resultBackToWorldsButton", resultWorldsButton);
            SetReference(so, "startButton", startButton);
            SetReference(so, "stopButton", stopButton);
            SetReference(so, "retryButton", null);
            SetReference(so, "validateButton", validateButton);
            SetReference(so, "clearButton", clearButton);
            SetReference(so, "backToSelectionButton", backToSelectionButton);
            SetReference(so, "recognizedTextScrollRect", recognizedScroll);
            SetReference(so, "storyBodyScrollRect", storyScroll);
            SetReference(so, "microphoneListeningIndicator", RequireObject(scene, "MicrophoneListeningIndicator"));
            SetObjectArray(so.FindProperty("starImages"), readingStars.Cast<UnityEngine.Object>().ToArray());
            SetReference(so, "fullStarSprite", LoadSprite(FullStarPath));
            SetReference(so, "emptyStarSprite", LoadSprite(EmptyStarPath));
            SetReference(so, "starDisplay", resultStarDisplay);
            SetReference(so, "tutorialController", tutorial);
            SetReference(so, "levelAnimationController", levelAnimator);
            SetReference(so, "resultStarAnimation", resultAnimation);

            SerializedProperty stories = so.FindProperty("cuentosDisponibles");
            stories.arraySize = 3;
            ConfigureStory(stories.GetArrayElementAtIndex(0), StoryProgressRepository.DefaultStoryId, "Los tres cerditos", DefaultStoryText, LoadSprite(ThreePigsIconPath), 1);
            ConfigureStory(stories.GetArrayElementAtIndex(1), "conejo_luna", "El conejo y la luna", RabbitStoryText, LoadSprite(RabbitIconPath), 2);
            ConfigureStory(stories.GetArrayElementAtIndex(2), "tortuga_amable", "La tortuga amable", TurtleStoryText, LoadSprite(TurtleIconPath), 3);

            SerializedProperty cards = so.FindProperty("cuentoCards");
            cards.arraySize = 3;
            ConfigureCard(cards.GetArrayElementAtIndex(0), StoryProgressRepository.DefaultStoryId, cardOne);
            ConfigureCard(cards.GetArrayElementAtIndex(1), "conejo_luna", cardTwo);
            ConfigureCard(cards.GetArrayElementAtIndex(2), "tortuga_amable", cardThree);

            so.ApplyModifiedPropertiesWithoutUndo();

            selectionPanel.gameObject.SetActive(true);
            readingPanel.gameObject.SetActive(false);
            resultPanel.gameObject.SetActive(false);
            otherWorldsButton.gameObject.SetActive(false);
        }

        private static StoryTutorialAnimationController EnsureStoryTutorial(
            Scene scene,
            RectTransform canvas,
            RectTransform selectionPanel,
            RectTransform readingPanel,
            Button startButton,
            Button validateButton)
        {
            foreach (CharacterDialogueController dialogue in ComponentsInScene<CharacterDialogueController>(scene))
            {
                SerializedObject dialogueSo = new(dialogue);
                SerializedProperty showIntro = dialogueSo.FindProperty("showIntroOnStart");
                if (showIntro != null) showIntro.boolValue = false;
                dialogueSo.ApplyModifiedPropertiesWithoutUndo();
            }

            DestroyIfExists(scene, "StoryTutorialOverlay");
            DestroyIfExists(scene, "TutorialOverlay");
            GameObject root = new("TutorialOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(StoryTutorialAnimationController));
            root.transform.SetParent(canvas, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect, Vector2.zero, Vector2.zero);
            root.transform.SetAsLastSibling();
            CanvasGroup overlayGroup = root.GetComponent<CanvasGroup>();
            StoryTutorialAnimationController controller = root.GetComponent<StoryTutorialAnimationController>();

            GameObject inputBlocker = new("InputBlocker", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            inputBlocker.transform.SetParent(rootRect, false);
            Image inputImage = inputBlocker.GetComponent<Image>();
            inputImage.color = new Color(0f, 0f, 0f, 0f);
            inputImage.raycastTarget = true;
            CanvasGroup inputGroup = inputBlocker.GetComponent<CanvasGroup>();
            Stretch(inputBlocker.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            Image dark = EnsureComponent<Image>(CreateUiObject("DarkBackground", rootRect));
            dark.color = new Color(0.04f, 0.05f, 0.08f, 0.42f);
            dark.raycastTarget = false;
            Stretch(dark.rectTransform, Vector2.zero, Vector2.zero);

            Image highlight = EnsureComponent<Image>(CreateUiObject("HighlightArea", rootRect));
            highlight.color = new Color(1f, 0.85f, 0.22f, 0.24f);
            highlight.raycastTarget = false;
            highlight.gameObject.SetActive(false);

            Image guide = EnsureComponent<Image>(CreateUiObject("GuideCharacter_Biblio", rootRect));
            guide.sprite = LoadSprite(BiblioIntroPath);
            guide.color = Color.white;
            guide.preserveAspect = true;
            guide.raycastTarget = false;
            RectTransform guideRect = guide.rectTransform;
            guideRect.anchorMin = Vector2.zero;
            guideRect.anchorMax = Vector2.zero;
            guideRect.pivot = Vector2.zero;
            guideRect.anchoredPosition = new Vector2(90f, 54f);
            guideRect.sizeDelta = new Vector2(330f, 410f);
            EnsureComponent<UIJuiceAnimator>(guide.gameObject);

            Image dialoguePanel = EnsureComponent<Image>(CreateUiObject("DialoguePanel", rootRect));
            dialoguePanel.color = Card;
            dialoguePanel.raycastTarget = true;
            RectTransform dialogueRect = dialoguePanel.rectTransform;
            dialogueRect.anchorMin = new Vector2(0.5f, 0f);
            dialogueRect.anchorMax = new Vector2(0.5f, 0f);
            dialogueRect.pivot = new Vector2(0.5f, 0f);
            dialogueRect.anchoredPosition = new Vector2(210f, 76f);
            dialogueRect.sizeDelta = new Vector2(980f, 286f);

            TMP_Text tutorialText = CreateText("TutorialText", string.Empty, dialogueRect, 34f, FontStyles.Bold, Purple, TextAlignmentOptions.MidlineLeft, 170f);
            Stretch(tutorialText.rectTransform, new Vector2(44f, 92f), new Vector2(-44f, -54f));
            tutorialText.enableAutoSizing = true;
            tutorialText.fontSizeMin = 28f;
            tutorialText.fontSizeMax = 36f;
            TMP_Text counter = CreateText("TutorialStepCounter", "1/5", dialogueRect, 22f, FontStyles.Bold, Pink, TextAlignmentOptions.Center, 32f);
            SetAnchors(counter.rectTransform, new Vector2(0.84f, 0.79f), new Vector2(0.96f, 0.94f), Vector2.zero, Vector2.zero);

            Button previous = CreateButton("Button_Previous", "Anterior", dialogueRect, new Vector2(170f, 58f), Blue);
            Button next = CreateButton("Button_Next", "Siguiente", dialogueRect, new Vector2(190f, 58f), new Color(0.22f, 0.58f, 0.42f, 1f));
            Button finish = CreateButton("Button_Finish", "Comenzar", dialogueRect, new Vector2(190f, 58f), Pink);
            SetManualButtonRect(previous, new Vector2(0f, 0f), new Vector2(44f, 28f));
            SetManualButtonRect(next, new Vector2(1f, 0f), new Vector2(-44f, 28f));
            SetManualButtonRect(finish, new Vector2(1f, 0f), new Vector2(-44f, 28f));

            Button skip = CreateButton("Button_Skip", "Omitir", rootRect, new Vector2(150f, 48f), new Color(0.33f, 0.24f, 0.55f, 0.82f));
            SetManualButtonRect(skip, new Vector2(1f, 1f), new Vector2(-42f, -42f));

            GameObject confirmRoot = new("SkipConfirmation", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            confirmRoot.transform.SetParent(rootRect, false);
            RectTransform confirmRect = confirmRoot.GetComponent<RectTransform>();
            confirmRect.anchorMin = new Vector2(0.5f, 0.5f);
            confirmRect.anchorMax = new Vector2(0.5f, 0.5f);
            confirmRect.pivot = new Vector2(0.5f, 0.5f);
            confirmRect.anchoredPosition = Vector2.zero;
            confirmRect.sizeDelta = new Vector2(620f, 260f);
            Image confirmImage = confirmRoot.GetComponent<Image>();
            confirmImage.color = new Color(1f, 1f, 1f, 0.98f);
            CanvasGroup confirmGroup = confirmRoot.GetComponent<CanvasGroup>();
            TMP_Text confirmText = CreateText("SkipConfirmationText", "Quieres omitir el tutorial?", confirmRect, 30f, FontStyles.Bold, Purple, TextAlignmentOptions.Center, 92f);
            SetAnchors(confirmText.rectTransform, new Vector2(0.08f, 0.52f), new Vector2(0.92f, 0.88f), Vector2.zero, Vector2.zero);
            Button cancelSkip = CreateButton("Button_CancelSkip", "Cancelar", confirmRect, new Vector2(190f, 58f), Blue);
            Button confirmSkip = CreateButton("Button_ConfirmSkip", "Omitir", confirmRect, new Vector2(190f, 58f), Pink);
            SetManualButtonRect(cancelSkip, new Vector2(0.28f, 0f), new Vector2(0f, 34f));
            SetManualButtonRect(confirmSkip, new Vector2(0.72f, 0f), new Vector2(0f, 34f));

            SerializedObject so = new(controller);
            SetReference(so, "overlayGroup", overlayGroup);
            SetReference(so, "inputBlockerGroup", inputGroup);
            SetReference(so, "darkBackground", dark);
            SetReference(so, "highlightArea", highlight.rectTransform);
            SetReference(so, "highlightImage", highlight);
            SetReference(so, "guideCharacter", guideRect);
            SetReference(so, "guideCharacterImage", guide);
            SetReference(so, "dialoguePanel", dialogueRect);
            SetReference(so, "tutorialText", tutorialText);
            SetReference(so, "stepCounterText", counter);
            SetReference(so, "previousButton", previous);
            SetReference(so, "nextButton", next);
            SetReference(so, "finishButton", finish);
            SetReference(so, "skipButton", skip);
            SetReference(so, "skipConfirmationRoot", confirmRoot);
            SetReference(so, "skipConfirmationGroup", confirmGroup);
            SetReference(so, "confirmSkipButton", confirmSkip);
            SetReference(so, "cancelSkipButton", cancelSkip);
            SetTutorialSteps(
                so.FindProperty("tutorialSteps"),
                LoadSprite(BiblioIntroPath),
                LoadSprite(BiblioSuccessPath),
                selectionPanel,
                startButton.transform as RectTransform,
                validateButton.transform as RectTransform);
            so.ApplyModifiedPropertiesWithoutUndo();

            overlayGroup.alpha = 0f;
            overlayGroup.interactable = false;
            overlayGroup.blocksRaycasts = false;
            inputGroup.alpha = 0f;
            inputGroup.interactable = false;
            inputGroup.blocksRaycasts = false;
            confirmGroup.alpha = 0f;
            confirmGroup.interactable = false;
            confirmGroup.blocksRaycasts = false;
            confirmRoot.SetActive(false);
            root.SetActive(true);
            return controller;
        }

        private static StoryLevelAnimationController ConfigureLevelAnimator(
            VoiceRecognitionTest manager,
            RectTransform selectionPanel,
            RectTransform readingPanel,
            RectTransform resultPanel,
            RectTransform microphoneIndicator,
            RectTransform guideCharacter,
            RectTransform[] storyCards,
            RectTransform[] actionButtons)
        {
            StoryLevelAnimationController animator = EnsureComponent<StoryLevelAnimationController>(manager.gameObject);
            EnsurePanelTransition(selectionPanel);
            EnsurePanelTransition(readingPanel);
            EnsurePanelTransition(resultPanel);

            foreach (RectTransform card in storyCards)
            {
                if (card == null) continue;
                // El hover, click y entrada de los libros comparten el mismo RectTransform.
                // No se agrega un flotado continuo aqui para no competir por escala/posicion.
                UIFloatingAnimation floating = card.GetComponent<UIFloatingAnimation>();
                if (floating != null) UnityEngine.Object.DestroyImmediate(floating);
            }

            SerializedObject so = new(animator);
            SetReference(so, "selectionPanel", selectionPanel);
            SetReference(so, "readingPanel", readingPanel);
            SetReference(so, "resultPanel", resultPanel);
            SetReference(so, "microphoneIndicator", microphoneIndicator);
            SetReference(so, "guideCharacter", guideCharacter);
            SetObjectArray(so.FindProperty("storyCards"), storyCards.Cast<UnityEngine.Object>().ToArray());
            SetObjectArray(so.FindProperty("actionButtons"), actionButtons.Cast<UnityEngine.Object>().ToArray());
            so.ApplyModifiedPropertiesWithoutUndo();
            return animator;
        }

        private static void EnsurePanelTransition(RectTransform panel)
        {
            if (panel == null) return;
            CanvasGroup group = EnsureComponent<CanvasGroup>(panel.gameObject);
            UIPanelTransition transition = EnsureComponent<UIPanelTransition>(panel.gameObject);
            SerializedObject so = new(transition);
            SetReference(so, "canvasGroup", group);
            SetReference(so, "target", panel);
            SerializedProperty playOnEnable = so.FindProperty("playOnEnable");
            if (playOnEnable != null) playOnEnable.boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetManualButtonRect(Button button, Vector2 anchor, Vector2 anchoredPosition)
        {
            RectTransform rect = button.transform as RectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
        }

        private static void SetTutorialSteps(SerializedProperty property, Sprite introSprite, Sprite successSprite, RectTransform selectionPanel, RectTransform startButton, RectTransform validateButton)
        {
            string[] texts =
            {
                "Hola! Soy Biblio. Aqui vamos a leer cuentos juntos.",
                "Elige uno de estos libros para comenzar.",
                "Cuando veas la historia, presiona el microfono y leela en voz alta.",
                "Cuando termines, presiona Validar para descubrir tus estrellas.",
                "Muy bien! Puedes repetir los cuentos todas las veces que quieras."
            };
            RectTransform[] highlights = { null, selectionPanel, startButton, validateButton, null };

            property.arraySize = texts.Length;
            for (int i = 0; i < texts.Length; i++)
            {
                SerializedProperty step = property.GetArrayElementAtIndex(i);
                step.FindPropertyRelative("text").stringValue = texts[i];
                step.FindPropertyRelative("biblioSprite").objectReferenceValue = i == texts.Length - 1 ? successSprite : introSprite;
                step.FindPropertyRelative("highlightedElement").objectReferenceValue = highlights[i];
                step.FindPropertyRelative("dialogueAnchoredPosition").vector2Value = new Vector2(210f, 76f);
                step.FindPropertyRelative("minimumVisibleSeconds").floatValue = 0f;
                step.FindPropertyRelative("iconSprite").objectReferenceValue = null;
            }
        }

        private static void NormalizeCanvas(RectTransform canvas)
        {
            canvas.anchorMin = Vector2.zero;
            canvas.anchorMax = Vector2.one;
            canvas.offsetMin = Vector2.zero;
            canvas.offsetMax = Vector2.zero;
            canvas.pivot = new Vector2(0.5f, 0.5f);
            canvas.localScale = Vector3.one;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        private static void EnsureReadingBaseObjects(Scene scene, RectTransform readingPanel)
        {
            Sprite fullStar = LoadSprite(FullStarPath);
            Sprite emptyStar = LoadSprite(EmptyStarPath);

            EnsureBaseText(scene, readingPanel, "TituloCuento", "Los tres cerditos", 34f, FontStyles.Bold, Purple, TextAlignmentOptions.Center);
            EnsureBaseImage(scene, readingPanel, "ImagenCuentoSeleccionado", null, Color.white, false);
            EnsureBaseText(scene, readingPanel, "TextoHistoria", DefaultStoryText, 26f, FontStyles.Normal, new Color(0.18f, 0.14f, 0.12f, 1f), TextAlignmentOptions.TopLeft);
            EnsureBaseText(scene, readingPanel, "TextoLecturaReconocida", string.Empty, 24f, FontStyles.Normal, new Color(0.15f, 0.18f, 0.2f, 1f), TextAlignmentOptions.TopLeft);
            EnsureBaseText(scene, readingPanel, "TextoLecturaParcial", string.Empty, 18f, FontStyles.Normal, Blue, TextAlignmentOptions.Center);
            EnsureBaseText(scene, readingPanel, "TextoLecturaFinalInterna", string.Empty, 1f, FontStyles.Normal, Color.clear, TextAlignmentOptions.TopLeft);
            EnsureBaseText(scene, readingPanel, "TextoEstado", "Elige un cuento para comenzar.", 22f, FontStyles.Bold, Blue, TextAlignmentOptions.Center);
            EnsureBaseText(scene, readingPanel, "TextoResultadoLectura", string.Empty, 21f, FontStyles.Bold, Purple, TextAlignmentOptions.Center);
            EnsureBaseText(scene, readingPanel, "TextoContador", string.Empty, 24f, FontStyles.Bold, Pink, TextAlignmentOptions.Center);

            EnsureBasePanel(scene, readingPanel, "StoryTextCard", Card, true);
            EnsureBasePanel(scene, readingPanel, "RecognizedTextCard", Card, true);
            RectTransform starsPanel = EnsureBasePanel(scene, readingPanel, "PanelEstrellas", new Color(1f, 1f, 1f, 0f), false);
            for (int i = 0; i < 3; i++)
            {
                Image star = EnsureBaseImage(scene, starsPanel, $"Star_{i + 1}", emptyStar ?? fullStar, Color.white, false);
                star.preserveAspect = true;
            }

            EnsureBaseButton(scene, readingPanel, "BotonIniciarVoz", "INICIAR", new Color(0.22f, 0.58f, 0.42f, 1f));
            EnsureBaseButton(scene, readingPanel, "BotonDetenerVoz", "DETENER", new Color(0.74f, 0.38f, 0.22f, 1f));
            EnsureBaseButton(scene, readingPanel, "BotonValidarLectura", "VALIDAR", Pink);
            EnsureBaseButton(scene, readingPanel, "BotonLimpiarTexto", "LIMPIAR", Blue);
            EnsureBaseButton(scene, readingPanel, "BotonVolver", "VOLVER", Purple);

            RectTransform microphone = EnsureBasePanel(scene, readingPanel, "MicrophoneListeningIndicator", new Color(0.18f, 0.68f, 0.58f, 0.18f), true);
            EnsureBaseText(scene, microphone, "TextoMicrofonoActivo", "Microfono activo", 20f, FontStyles.Bold, new Color(0.06f, 0.4f, 0.34f, 1f), TextAlignmentOptions.Center);
            microphone.gameObject.SetActive(false);
        }

        private static RectTransform EnsureBasePanel(Scene scene, Transform parent, string name, Color color, bool raycastTarget)
        {
            GameObject go = FindObject(scene, name) ?? CreateUiObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            Image image = EnsureComponent<Image>(go);
            image.color = color;
            image.raycastTarget = raycastTarget;
            return rect;
        }

        private static TMP_Text EnsureBaseText(Scene scene, Transform parent, string name, string value, float fontSize, FontStyles style, Color color, TextAlignmentOptions alignment)
        {
            GameObject go = FindObject(scene, name) ?? CreateUiObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            TMP_Text text = go.GetComponent<TMP_Text>() ?? go.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        private static Image EnsureBaseImage(Scene scene, Transform parent, string name, Sprite sprite, Color color, bool raycastTarget)
        {
            GameObject go = FindObject(scene, name) ?? CreateUiObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            Image image = EnsureComponent<Image>(go);
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycastTarget;
            image.preserveAspect = true;
            return image;
        }

        private static Button EnsureBaseButton(Scene scene, Transform parent, string name, string label, Color color)
        {
            GameObject go = FindObject(scene, name) ?? CreateUiObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(150f, 58f);
            Image image = EnsureComponent<Image>(go);
            image.color = color;
            image.raycastTarget = true;
            Button button = EnsureComponent<Button>(go);
            button.targetGraphic = image;
            EnsureComponent<UIButtonFeedback>(go);
            TMP_Text text = go.GetComponentInChildren<TMP_Text>(true);
            if (text == null) text = CreateText("Text", label, rect, 22f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, 58f);
            text.text = label;
            text.fontSize = 22f;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
            return button;
        }

        // Reconstruye el selector como librero persistente, siguiendo el primer mockup del PDF.
        private static RectTransform EnsurePdfSelectionPanel(RectTransform canvas)
        {
            RectTransform panel = EnsurePanel(canvas, "PanelSeleccionCuentos", true, Color.clear);
            RemoveCompositionComponents(panel.gameObject);
            ClearChildren(panel);
            Image panelImage = EnsureComponent<Image>(panel.gameObject);
            panelImage.color = Color.clear;
            panelImage.raycastTarget = false;

            RectTransform shelf = CreatePdfPanel(
                "LibreroCentral",
                panel,
                new Vector2(0.31f, 0.12f),
                new Vector2(0.69f, 0.86f),
                LoadSprite(PanelBrownPath),
                new Color(0.72f, 0.46f, 0.24f, 0.98f));
            CreatePdfPanel(
                "RepisaSuperior",
                shelf,
                new Vector2(0.06f, 0.49f),
                new Vector2(0.94f, 0.53f),
                null,
                new Color(0.48f, 0.28f, 0.12f, 0.9f));

            CreatePdfText(
                "TituloPanelSeleccionCuentos",
                "MUNDO DE CUENTOS",
                shelf,
                new Vector2(0.08f, 0.91f),
                new Vector2(0.92f, 0.98f),
                38f,
                FontStyles.Bold,
                new Color(1f, 0.94f, 0.74f, 1f),
                TextAlignmentOptions.Center);
            CreatePdfText(
                "InstruccionSeleccionCuentos",
                "Elige un libro para comenzar",
                shelf,
                new Vector2(0.1f, 0.855f),
                new Vector2(0.9f, 0.91f),
                23f,
                FontStyles.Bold,
                Color.white,
                TextAlignmentOptions.Center);

            CreatePdfBookCard(
                "TarjetaCuento_tres_cerditos",
                shelf,
                new Vector2(0.12f, 0.55f),
                new Vector2(0.34f, 0.84f),
                new Color(0.37f, 0.51f, 0.7f, 1f),
                "Los tres\ncerditos",
                LoadSprite(ThreePigsIconPath));
            CreatePdfBookCard(
                "TarjetaCuento_conejo_luna",
                shelf,
                new Vector2(0.39f, 0.55f),
                new Vector2(0.61f, 0.84f),
                new Color(0.46f, 0.73f, 0.3f, 1f),
                "El conejo\ny la luna",
                LoadSprite(RabbitIconPath));
            CreatePdfBookCard(
                "TarjetaCuento_tortuga_amable",
                shelf,
                new Vector2(0.66f, 0.55f),
                new Vector2(0.88f, 0.84f),
                new Color(0.69f, 0.18f, 0.18f, 1f),
                "La tortuga\namable",
                LoadSprite(TurtleIconPath));

            CreatePdfText(
                "TextoProgresoCuentos",
                "Cuentos completados: 0/2",
                shelf,
                new Vector2(0.1f, 0.40f),
                new Vector2(0.9f, 0.45f),
                21f,
                FontStyles.Bold,
                new Color(1f, 0.91f, 0.55f, 1f),
                TextAlignmentOptions.Center);
            CreatePdfText(
                "TextoDesbloqueoOtrosMundos",
                "Completa 2 cuentos para visitar los otros mundos.",
                shelf,
                new Vector2(0.08f, 0.34f),
                new Vector2(0.92f, 0.40f),
                18f,
                FontStyles.Normal,
                Color.white,
                TextAlignmentOptions.Center);

            Button backButton = CreatePdfButton(
                "BotonVolverMenuAnterior",
                "REGRESAR",
                shelf,
                new Vector2(0.11f, 0.12f),
                new Vector2(0.45f, 0.27f),
                LoadSprite(ButtonGreenPath),
                Color.white,
                new Color(0.12f, 0.19f, 0.18f, 1f));
            Button worldsButton = CreatePdfButton(
                "BotonVerOtrosMundos",
                "OTROS MUNDOS",
                shelf,
                new Vector2(0.55f, 0.12f),
                new Vector2(0.89f, 0.27f),
                LoadSprite(ButtonPurplePath),
                Color.white,
                Color.white);
            backButton.gameObject.SetActive(true);
            worldsButton.gameObject.SetActive(true);

            RectTransform dialogue = CreatePdfPanel(
                "DialogoBiblioSeleccion",
                panel,
                new Vector2(0.66f, 0.45f),
                new Vector2(0.94f, 0.57f),
                LoadSprite(PanelLightPath),
                new Color(1f, 0.98f, 0.84f, 0.98f));
            CreatePdfText(
                "TextoDialogoBiblioSeleccion",
                "¡Hola! Elige un libro y leamos juntos.",
                dialogue,
                new Vector2(0.09f, 0.12f),
                new Vector2(0.91f, 0.88f),
                20f,
                FontStyles.Bold,
                Purple,
                TextAlignmentOptions.Center);
            Image guide = CreatePdfImage(
                "GuideCharacter_Biblio_Selection",
                panel,
                new Vector2(0.71f, 0.05f),
                new Vector2(0.94f, 0.46f),
                LoadSprite(BiblioSuccessPath),
                Color.white);
            UIJuiceAnimator guideAnimator = EnsureComponent<UIJuiceAnimator>(guide.gameObject);
            ConfigureJuiceAnimator(guideAnimator, guide.rectTransform, guide, 8f, 0.8f, true);
            return panel;
        }

        // Reconstruye la ventana final como el modal dorado del tercer mockup del PDF.
        private static RectTransform EnsurePdfResultPanel(RectTransform canvas)
        {
            RectTransform panel = EnsurePanel(canvas, "PanelResultadoCuento", false, new Color(0f, 0f, 0f, 0.2f));
            RemoveCompositionComponents(panel.gameObject);
            ClearChildren(panel);
            Image panelImage = EnsureComponent<Image>(panel.gameObject);
            panelImage.color = new Color(0.05f, 0.08f, 0.16f, 0.2f);
            panelImage.raycastTarget = true;

            RectTransform card = CreatePdfPanel(
                "TarjetaResultadoCuento",
                panel,
                new Vector2(0.27f, 0.19f),
                new Vector2(0.73f, 0.80f),
                LoadSprite(PanelBrownPath),
                new Color(0.96f, 0.68f, 0.22f, 0.99f));
            RectTransform headline = CreatePdfPanel(
                "CapsulaResultado",
                card,
                new Vector2(0.28f, 0.82f),
                new Vector2(0.72f, 0.95f),
                LoadSprite(ButtonYellowPath),
                new Color(1f, 0.95f, 0.53f, 1f));
            CreatePdfText(
                "TituloResultadoPrincipal",
                "¡MUY BIEN!",
                headline,
                Vector2.zero,
                Vector2.one,
                34f,
                FontStyles.Bold,
                new Color(0.16f, 0.14f, 0.12f, 1f),
                TextAlignmentOptions.Center);
            CreatePdfText(
                "TextoResultadoCuento",
                "Cuento",
                card,
                new Vector2(0.12f, 0.72f),
                new Vector2(0.88f, 0.80f),
                24f,
                FontStyles.Bold,
                Purple,
                TextAlignmentOptions.Center);
            CreatePdfText(
                "TextoResultadoPuntaje",
                "Puntaje: 0 puntos",
                card,
                new Vector2(0.18f, 0.66f),
                new Vector2(0.82f, 0.72f),
                20f,
                FontStyles.Bold,
                new Color(0.45f, 0.25f, 0.13f, 1f),
                TextAlignmentOptions.Center);
            RectTransform starsContainer = CreateResultStarsContainer(card);
            SetAnchors(starsContainer, new Vector2(0.13f, 0.43f), new Vector2(0.87f, 0.63f), Vector2.zero, Vector2.zero);
            CreatePdfText(
                "TextoResultadoEstrellas",
                string.Empty,
                card,
                Vector2.zero,
                Vector2.zero,
                1f,
                FontStyles.Normal,
                Color.clear,
                TextAlignmentOptions.Center);
            CreatePdfText(
                "TextoResultadoMensaje",
                "Muy bien. Estas listo para seguir leyendo.",
                card,
                new Vector2(0.12f, 0.30f),
                new Vector2(0.88f, 0.42f),
                20f,
                FontStyles.Bold,
                Purple,
                TextAlignmentOptions.Center);
            CreateDecorativeStarsLoop(starsContainer);

            CreatePdfButton(
                "BotonRepetirCuento",
                "REPETIR",
                card,
                new Vector2(0.08f, 0.15f),
                new Vector2(0.31f, 0.27f),
                LoadSprite(ButtonBluePath),
                Color.white,
                Color.white);
            CreatePdfButton(
                "BotonSiguienteCuento",
                "SIGUIENTE CUENTO",
                card,
                new Vector2(0.35f, 0.15f),
                new Vector2(0.65f, 0.27f),
                LoadSprite(ButtonGreenPath),
                Color.white,
                new Color(0.12f, 0.19f, 0.18f, 1f));
            CreatePdfButton(
                "BotonVolverACuentos",
                "CUENTOS",
                card,
                new Vector2(0.69f, 0.15f),
                new Vector2(0.92f, 0.27f),
                LoadSprite(ButtonPurplePath),
                Color.white,
                Color.white);
            CreatePdfButton(
                "BotonVolverAMundosResultado",
                "VOLVER A MUNDOS",
                card,
                new Vector2(0.32f, 0.045f),
                new Vector2(0.68f, 0.125f),
                LoadSprite(ButtonYellowPath),
                new Color(1f, 0.95f, 0.53f, 1f),
                new Color(0.16f, 0.14f, 0.12f, 1f));

            Image guide = CreatePdfImage(
                "GuideCharacter_Biblio_Result",
                panel,
                new Vector2(0.055f, 0.04f),
                new Vector2(0.29f, 0.43f),
                LoadSprite(BiblioSuccessPath),
                Color.white);
            return panel;
        }

        private static Button CreatePdfBookCard(
            string name,
            RectTransform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color,
            string title,
            Sprite icon)
        {
            GameObject root = CreateUiObject(name, parent);
            RectTransform rect = root.GetComponent<RectTransform>();
            SetAnchors(rect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            Image image = EnsureComponent<Image>(root);
            image.sprite = LoadSprite(PanelBrownPath);
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = true;
            Button button = EnsureComponent<Button>(root);
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.94f, 0.7f, 1f);
            colors.pressedColor = new Color(0.85f, 0.82f, 1f, 1f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            EnsureComponent<UIButtonFeedback>(root);

            CreatePdfImage("IconoCuento", rect, new Vector2(0.2f, 0.63f), new Vector2(0.8f, 0.9f), icon, Color.white);
            CreatePdfText("TituloTarjetaCuento", title, rect, new Vector2(0.08f, 0.33f), new Vector2(0.92f, 0.62f), 21f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            CreatePdfText("EstadoTarjetaCuento", "Pendiente", rect, new Vector2(0.08f, 0.17f), new Vector2(0.92f, 0.30f), 16f, FontStyles.Bold, new Color(1f, 0.94f, 0.72f, 1f), TextAlignmentOptions.Center);
            CreatePdfText("EstrellasTarjetaCuento", "Sin estrellas", rect, new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.17f), 15f, FontStyles.Normal, Color.white, TextAlignmentOptions.Center);
            return button;
        }

        private static RectTransform CreatePdfPanel(
            string name,
            RectTransform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Sprite sprite,
            Color color)
        {
            GameObject root = CreateUiObject(name, parent);
            RectTransform rect = root.GetComponent<RectTransform>();
            SetAnchors(rect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            Image image = EnsureComponent<Image>(root);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static Image CreatePdfImage(
            string name,
            RectTransform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Sprite sprite,
            Color color)
        {
            GameObject root = CreateUiObject(name, parent);
            RectTransform rect = root.GetComponent<RectTransform>();
            SetAnchors(rect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            Image image = EnsureComponent<Image>(root);
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text CreatePdfText(
            string name,
            string value,
            RectTransform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            float fontSize,
            FontStyles style,
            Color color,
            TextAlignmentOptions alignment)
        {
            GameObject root = CreateUiObject(name, parent);
            RectTransform rect = root.GetComponent<RectTransform>();
            SetAnchors(rect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            TMP_Text text = root.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.enableAutoSizing = false;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreatePdfButton(
            string name,
            string label,
            RectTransform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Sprite sprite,
            Color imageColor,
            Color textColor)
        {
            GameObject root = CreateUiObject(name, parent);
            RectTransform rect = root.GetComponent<RectTransform>();
            SetAnchors(rect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            Image image = EnsureComponent<Image>(root);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = imageColor;
            image.raycastTarget = true;
            Button button = EnsureComponent<Button>(root);
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.96f, 0.72f, 1f);
            colors.pressedColor = new Color(0.84f, 0.84f, 0.96f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.75f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            EnsureComponent<UIButtonFeedback>(root);
            TMP_Text text = CreatePdfText("Texto", label, rect, new Vector2(0.08f, 0.1f), new Vector2(0.92f, 0.9f), 21f, FontStyles.Bold, textColor, TextAlignmentOptions.Center);
            text.enableAutoSizing = true;
            text.fontSizeMin = 14f;
            text.fontSizeMax = 23f;
            return button;
        }

        private static void RemoveCompositionComponents(GameObject target)
        {
            foreach (LayoutGroup layout in target.GetComponents<LayoutGroup>()) UnityEngine.Object.DestroyImmediate(layout);
            foreach (ContentSizeFitter fitter in target.GetComponents<ContentSizeFitter>()) UnityEngine.Object.DestroyImmediate(fitter);
            foreach (LayoutElement element in target.GetComponents<LayoutElement>()) UnityEngine.Object.DestroyImmediate(element);
        }

        private static void ConfigureJuiceAnimator(UIJuiceAnimator animator, RectTransform target, Graphic graphic, float distance, float speed, bool rotate)
        {
            SerializedObject so = new(animator);
            SetReference(so, "target", target);
            SetReference(so, "graphic", graphic);
            SerializedProperty floatMotion = so.FindProperty("floatMotion");
            SerializedProperty softRotation = so.FindProperty("softRotation");
            SerializedProperty floatDistance = so.FindProperty("floatDistance");
            SerializedProperty animatorSpeed = so.FindProperty("speed");
            if (floatMotion != null) floatMotion.boolValue = true;
            if (softRotation != null) softRotation.boolValue = rotate;
            if (floatDistance != null) floatDistance.floatValue = distance;
            if (animatorSpeed != null) animatorSpeed.floatValue = speed;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureLibraryBackground(Scene scene, RectTransform canvas)
        {
            GameObject root = FindObject(scene, "FondoBibliotecaNuevo") ?? CreateUiObject("FondoBibliotecaNuevo", canvas);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.SetParent(canvas, false);
            Stretch(rect, Vector2.zero, Vector2.zero);
            rect.SetAsFirstSibling();
            Image image = EnsureComponent<Image>(root);
            image.sprite = LoadSprite(LibraryBackgroundPath);
            image.color = Color.white;
            image.preserveAspect = false;
            image.raycastTarget = false;
        }

        private static RectTransform EnsureSelectionPanel(RectTransform canvas, VoiceRecognitionTest manager)
        {
            RectTransform panel = EnsurePanel(canvas, "PanelSeleccionCuentos", true, Cream);
            ClearChildren(panel);

            VerticalLayoutGroup rootLayout = EnsureComponent<VerticalLayoutGroup>(panel.gameObject);
            rootLayout.padding = new RectOffset(70, 70, 42, 42);
            rootLayout.spacing = 18f;
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = false;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            CreateText("TituloPanelSeleccionCuentos", "MUNDO DE CUENTOS", panel, 46f, FontStyles.Bold, Purple, TextAlignmentOptions.Center, 64f);
            CreateText("InstruccionSeleccionCuentos", "Elige el cuento que deseas leer", panel, 28f, FontStyles.Bold, Blue, TextAlignmentOptions.Center, 42f);
            CreateText("TextoProgresoCuentos", "Cuentos completados: 0/2", panel, 24f, FontStyles.Bold, Pink, TextAlignmentOptions.Center, 36f);

            ScrollRect scroll = CreateScroll("ScrollCuentosDisponibles", panel, 430f);
            GridLayoutGroup grid = EnsureComponent<GridLayoutGroup>(scroll.content.gameObject);
            grid.cellSize = new Vector2(315f, 285f);
            grid.spacing = new Vector2(28f, 24f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;

            CreateStoryCard("TarjetaCuento_tres_cerditos", "Los tres cerditos", "Pendiente", scroll.content, new Color(0.98f, 0.71f, 0.32f, 1f));
            CreateStoryCard("TarjetaCuento_conejo_luna", "El conejo y la luna", "Corto para probar", scroll.content, new Color(0.46f, 0.74f, 0.94f, 1f));
            CreateStoryCard("TarjetaCuento_tortuga_amable", "La tortuga amable", "Corto para probar", scroll.content, new Color(0.46f, 0.78f, 0.48f, 1f));

            CreateText("TextoDesbloqueoOtrosMundos", "Completa 2 cuentos para visitar los otros mundos.", panel, 22f, FontStyles.Bold, Purple, TextAlignmentOptions.Center, 34f);

            GameObject row = CreateUiObject("FilaBotonesSeleccionCuentos", panel);
            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(820f, 76f);
            HorizontalLayoutGroup rowLayout = EnsureComponent<HorizontalLayoutGroup>(row);
            rowLayout.spacing = 28f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = true;

            CreateButton("BotonVolverMenuAnterior", "VOLVER", rowRect, new Vector2(300f, 64f), Blue);
            CreateButton("BotonVerOtrosMundos", "VER OTROS MUNDOS", rowRect, new Vector2(360f, 64f), Pink);
            return panel;
        }

        private static RectTransform EnsureResultPanel(RectTransform canvas)
        {
            RectTransform panel = EnsurePanel(canvas, "PanelResultadoCuento", false, new Color(0.18f, 0.15f, 0.3f, 0.94f));
            ClearChildren(panel);

            GameObject card = CreateUiObject("TarjetaResultadoCuento", panel);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(760f, 520f);
            cardRect.anchoredPosition = Vector2.zero;

            Image image = EnsureComponent<Image>(card);
            image.color = Card;
            VerticalLayoutGroup layout = EnsureComponent<VerticalLayoutGroup>(card);
            layout.padding = new RectOffset(44, 44, 42, 42);
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;

            CreateText("TituloResultadoCuento", "RESULTADO", cardRect, 46f, FontStyles.Bold, Purple, TextAlignmentOptions.Center, 66f);
            CreateText("TextoResultadoCuento", "Cuento", cardRect, 30f, FontStyles.Bold, Blue, TextAlignmentOptions.Center, 44f);
            CreateText("TextoResultadoPuntaje", "Puntaje: 0 puntos", cardRect, 34f, FontStyles.Bold, Pink, TextAlignmentOptions.Center, 54f);
            RectTransform starsContainer = CreateResultStarsContainer(cardRect);
            CreateText("TextoResultadoEstrellas", "☆☆☆", cardRect, 48f, FontStyles.Bold, new Color(1f, 0.76f, 0.12f, 1f), TextAlignmentOptions.Center, 70f);
            CreateText("TextoResultadoMensaje", "Regresando a los cuentos...", cardRect, 26f, FontStyles.Bold, Purple, TextAlignmentOptions.Center, 140f);
            CreateDecorativeStarsLoop(starsContainer);
            return panel;
        }

        private static RectTransform CreateResultStarsContainer(RectTransform parent)
        {
            Sprite fullStar = LoadSprite(FullStarPath);
            Sprite emptyStar = LoadSprite(EmptyStarPath);
            GameObject root = CreateUiObject("StarsContainer", parent);
            RectTransform rect = root.GetComponent<RectTransform>();
            LayoutElement layout = EnsureComponent<LayoutElement>(root);
            layout.preferredHeight = 106f;

            HorizontalLayoutGroup group = EnsureComponent<HorizontalLayoutGroup>(root);
            group.spacing = 22f;
            group.childAlignment = TextAnchor.MiddleCenter;
            group.childControlWidth = false;
            group.childControlHeight = false;

            Image[] stars = new Image[3];
            for (int i = 0; i < stars.Length; i++)
            {
                GameObject starObject = CreateUiObject($"MainStar_0{i + 1}", rect);
                Image starImage = EnsureComponent<Image>(starObject);
                starImage.sprite = emptyStar ?? fullStar;
                starImage.color = new Color(1f, 1f, 1f, 0.45f);
                starImage.preserveAspect = true;
                LayoutElement starLayout = EnsureComponent<LayoutElement>(starObject);
                starLayout.preferredWidth = 82f;
                starLayout.preferredHeight = 82f;
                stars[i] = starImage;
            }

            UIStarDisplay display = EnsureComponent<UIStarDisplay>(root);
            SerializedObject displaySo = new(display);
            SetObjectArray(displaySo.FindProperty("stars"), stars.Cast<UnityEngine.Object>().ToArray());
            SetReference(displaySo, "earnedSprite", fullStar);
            SetReference(displaySo, "unearnedSprite", emptyStar ?? fullStar);
            displaySo.ApplyModifiedPropertiesWithoutUndo();
            return rect;
        }

        private static void CreateDecorativeStarsLoop(RectTransform starsContainer)
        {
            Sprite fullStar = LoadSprite(FullStarPath);
            GameObject loop = new("DecorativeStarLoop", typeof(RectTransform), typeof(LayoutElement), typeof(CanvasGroup));
            loop.transform.SetParent(starsContainer.parent, false);
            RectTransform loopRect = loop.GetComponent<RectTransform>();
            LayoutElement loopLayout = loop.GetComponent<LayoutElement>();
            loopLayout.ignoreLayout = true;
            loopRect.anchorMin = new Vector2(0.5f, 0.5f);
            loopRect.anchorMax = new Vector2(0.5f, 0.5f);
            loopRect.pivot = new Vector2(0.5f, 0.5f);
            loopRect.anchoredPosition = new Vector2(0f, 72f);
            loopRect.sizeDelta = new Vector2(360f, 230f);
            loopRect.localScale = Vector3.one;

            CanvasGroup group = loop.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            Image mainImage = CreateStarImage("MainCelebrationStar", loopRect, fullStar, new Vector2(0f, 0f), new Vector2(112f, 112f), 0.78f);
            RectTransform[] floatingRects = new RectTransform[5];
            Image[] floatingImages = new Image[5];
            Vector2[] positions =
            {
                new(-58f, -36f),
                new(52f, -30f),
                new(-8f, -48f),
                new(92f, -12f),
                new(-96f, -10f)
            };

            for (int i = 0; i < floatingRects.Length; i++)
            {
                Image image = CreateStarImage($"FloatingStar_0{i + 1}", loopRect, fullStar, positions[i], new Vector2(34f, 34f), 0f);
                floatingRects[i] = image.rectTransform;
                floatingImages[i] = image;
            }

            StoryResultStarAnimation animation = EnsureComponent<StoryResultStarAnimation>(loop);
            SerializedObject so = new(animation);
            SetReference(so, "loopGroup", group);
            SetReference(so, "mainStar", mainImage.rectTransform);
            SetReference(so, "mainStarImage", mainImage);
            SetObjectArray(so.FindProperty("floatingStars"), floatingRects.Cast<UnityEngine.Object>().ToArray());
            SetObjectArray(so.FindProperty("floatingStarImages"), floatingImages.Cast<UnityEngine.Object>().ToArray());
            SetVector2Array(so.FindProperty("travelOffsets"), new[]
            {
                new Vector2(-78f, 136f),
                new Vector2(68f, 152f),
                new Vector2(-24f, 164f),
                new Vector2(96f, 118f),
                new Vector2(-108f, 108f)
            });
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Image CreateStarImage(string name, RectTransform parent, Sprite sprite, Vector2 position, Vector2 size, float alpha)
        {
            GameObject go = CreateUiObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = EnsureComponent<Image>(go);
            image.sprite = sprite;
            image.color = new Color(1f, 1f, 1f, alpha);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static void MoveReadingObjectsIntoPanel(Scene scene, RectTransform readingPanel)
        {
            string[] names =
            {
                "StoryVisualDesign", "StoryHeader", "TituloCuento", "StoryTextCard", "RecognizedTextCard", "BotonIniciarVoz",
                "BotonDetenerVoz", "BotonLimpiarTexto", "BotonValidarLectura", "BotonVolver",
                "MicrophoneListeningIndicator", "PanelEstrellas", "TextoContador",
                "ErrorText", "TextoEstado", "TextoResultadoLectura", "StarsResultText"
            };

            foreach (string name in names)
            {
                GameObject item = FindObject(scene, name);
                if (item == null || item.transform == readingPanel || item.transform.IsChildOf(readingPanel)) continue;
                item.transform.SetParent(readingPanel, true);
            }

            GameObject icon = FindObject(scene, "ImagenCuentoSeleccionado") ?? CreateUiObject("ImagenCuentoSeleccionado", readingPanel);
            RectTransform iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.05f, 0.78f);
            iconRect.anchorMax = new Vector2(0.16f, 0.94f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            Image iconImage = EnsureComponent<Image>(icon);
            iconImage.color = new Color(1f, 1f, 1f, 0.22f);
            iconImage.preserveAspect = true;
            icon.transform.SetParent(readingPanel, true);
            icon.transform.SetAsFirstSibling();
        }

        // Reconstruye el lector como libro abierto persistente, conforme al segundo mockup del PDF.
        private static void EnsurePdfReadingPanelLayout(
            Scene scene,
            RectTransform readingPanel,
            ScrollRect storyScroll,
            ScrollRect recognizedScroll,
            Button startButton,
            Button stopButton,
            Button clearButton,
            Button validateButton,
            Button backToSelectionButton)
        {
            RemoveCompositionComponents(readingPanel.gameObject);
            Image background = EnsureComponent<Image>(readingPanel.gameObject);
            background.color = Color.clear;
            background.raycastTarget = false;

            // Los objetos funcionales se desacoplan primero por MoveReadingObjectsIntoPanel;
            // asi se puede retirar la composicion anterior sin destruir scrolls ni referencias.
            DestroyIfExists(scene, "PanelEncabezado");
            DestroyIfExists(scene, "ContenedorPrincipal");
            DestroyIfExists(scene, "StoryVisualDesign");
            DestroyIfExists(scene, "StoryHeader");
            DestroyIfExists(scene, "EncabezadoMundo");

            RectTransform book = CreatePdfPanel(
                "ContenedorPrincipal",
                readingPanel,
                new Vector2(0.045f, 0.095f),
                new Vector2(0.955f, 0.91f),
                LoadSprite(PanelBrownPath),
                new Color(0.72f, 0.48f, 0.28f, 0.98f));
            RectTransform leftPage = CreatePdfPanel(
                "PanelCuento",
                book,
                new Vector2(0.025f, 0.035f),
                new Vector2(0.485f, 0.965f),
                LoadSprite(PanelLightPath),
                new Color(0.25f, 0.34f, 0.45f, 1f));
            CreatePdfPanel(
                "LomoLibro",
                book,
                new Vector2(0.485f, 0.035f),
                new Vector2(0.515f, 0.965f),
                null,
                new Color(0.67f, 0.70f, 0.70f, 1f));
            RectTransform rightPage = CreatePdfPanel(
                "PanelLectura",
                book,
                new Vector2(0.515f, 0.035f),
                new Vector2(0.975f, 0.965f),
                LoadSprite(PanelLightPath),
                new Color(0.25f, 0.34f, 0.45f, 1f));

            RectTransform storyHeader = CreatePdfPanel(
                "CabeceraHistoriaALeer",
                leftPage,
                new Vector2(0.27f, 0.87f),
                new Vector2(0.73f, 0.96f),
                LoadSprite(ButtonOrangePath),
                new Color(0.75f, 0.50f, 0.29f, 1f));
            CreatePdfText(
                "TextoCabeceraHistoriaALeer",
                "Historia a leer",
                storyHeader,
                new Vector2(0.08f, 0.1f),
                new Vector2(0.92f, 0.9f),
                26f,
                FontStyles.Bold,
                new Color(0.13f, 0.11f, 0.1f, 1f),
                TextAlignmentOptions.Center);

            TMP_Text title = RequireComponent<TMP_Text>(scene, "TituloCuento");
            StylePdfExistingText(title, leftPage, new Vector2(0.18f, 0.77f), new Vector2(0.92f, 0.85f), 27f, FontStyles.Bold, new Color(1f, 0.94f, 0.74f, 1f), TextAlignmentOptions.Center);
            Image storyIcon = RequireComponent<Image>(scene, "ImagenCuentoSeleccionado");
            storyIcon.transform.SetParent(leftPage, false);
            SetAnchors(storyIcon.rectTransform, new Vector2(0.06f, 0.775f), new Vector2(0.17f, 0.855f), Vector2.zero, Vector2.zero);
            storyIcon.preserveAspect = true;
            storyIcon.raycastTarget = false;

            RectTransform storyCard = RequireRect(scene, "StoryTextCard");
            storyCard.SetParent(leftPage, false);
            SetAnchors(storyCard, new Vector2(0.055f, 0.06f), new Vector2(0.945f, 0.75f), Vector2.zero, Vector2.zero);
            StylePdfCard(storyCard, new Color(0.72f, 0.50f, 0.30f, 1f));
            StyleScrollText(scene, storyScroll, "TextoHistoria", 24f, new Color(0.16f, 0.12f, 0.1f, 1f));

            RectTransform voiceHeader = CreatePdfPanel(
                "CabeceraLeeLaHistoria",
                rightPage,
                new Vector2(0.27f, 0.87f),
                new Vector2(0.73f, 0.96f),
                LoadSprite(ButtonOrangePath),
                new Color(0.75f, 0.50f, 0.29f, 1f));
            CreatePdfText(
                "TextoCabeceraLeeLaHistoria",
                "Lee la historia",
                voiceHeader,
                new Vector2(0.08f, 0.1f),
                new Vector2(0.92f, 0.9f),
                26f,
                FontStyles.Bold,
                new Color(0.13f, 0.11f, 0.1f, 1f),
                TextAlignmentOptions.Center);

            RectTransform microphone = RequireRect(scene, "MicrophoneListeningIndicator");
            microphone.SetParent(rightPage, false);
            SetAnchors(microphone, new Vector2(0.07f, 0.77f), new Vector2(0.93f, 0.84f), Vector2.zero, Vector2.zero);
            StylePdfCard(microphone, new Color(0.34f, 0.76f, 0.45f, 0.98f));
            ClearChildren(microphone);
            CreatePdfImage("IconoMicrofonoActivo", microphone, new Vector2(0.03f, 0.14f), new Vector2(0.11f, 0.86f), LoadSprite(VoiceOnPath), Color.white);
            CreatePdfText("TextoMicrofonoActivo", "Estoy escuchando...", microphone, new Vector2(0.14f, 0.12f), new Vector2(0.94f, 0.88f), 19f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);

            TMP_Text status = RequireComponent<TMP_Text>(scene, "TextoEstado");
            StylePdfExistingText(status, rightPage, new Vector2(0.07f, 0.70f), new Vector2(0.93f, 0.76f), 18f, FontStyles.Bold, new Color(1f, 0.94f, 0.74f, 1f), TextAlignmentOptions.Center);

            RectTransform recognizedCard = RequireRect(scene, "RecognizedTextCard");
            recognizedCard.SetParent(rightPage, false);
            SetAnchors(recognizedCard, new Vector2(0.055f, 0.365f), new Vector2(0.945f, 0.69f), Vector2.zero, Vector2.zero);
            StylePdfCard(recognizedCard, new Color(0.72f, 0.50f, 0.30f, 1f));
            StyleScrollText(scene, recognizedScroll, "TextoLecturaReconocida", 22f, new Color(0.15f, 0.18f, 0.2f, 1f));
            TMP_Text placeholder = GetOrCreatePdfText(scene, "TextoLecturaPlaceholder", recognizedScroll.viewport);
            StylePdfExistingText(placeholder, recognizedScroll.viewport, new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.88f), 20f, FontStyles.Italic, new Color(0.35f, 0.35f, 0.35f, 0.8f), TextAlignmentOptions.Center);
            TMP_Text partialText = RequireComponent<TMP_Text>(scene, "TextoLecturaParcial");
            StylePdfExistingText(partialText, rightPage, new Vector2(0.07f, 0.333f), new Vector2(0.93f, 0.36f), 16f, FontStyles.Italic, new Color(0.92f, 0.91f, 0.82f, 1f), TextAlignmentOptions.Center);
            TMP_Text finalInternal = RequireComponent<TMP_Text>(scene, "TextoLecturaFinalInterna");
            StylePdfExistingText(finalInternal, rightPage, Vector2.zero, Vector2.zero, 1f, FontStyles.Normal, Color.clear, TextAlignmentOptions.TopLeft);

            RectTransform resultContainer = CreatePdfPanel(
                "ContenedorResultado",
                rightPage,
                new Vector2(0.055f, 0.18f),
                new Vector2(0.945f, 0.325f),
                LoadSprite(PanelLightPath),
                new Color(1f, 1f, 1f, 0.32f));
            RectTransform starsPanel = RequireRect(scene, "PanelEstrellas");
            starsPanel.SetParent(resultContainer, false);
            SetAnchors(starsPanel, new Vector2(0.25f, 0.55f), new Vector2(0.75f, 0.95f), Vector2.zero, Vector2.zero);
            RemoveCompositionComponents(starsPanel.gameObject);
            HorizontalLayoutGroup starsLayout = EnsureComponent<HorizontalLayoutGroup>(starsPanel.gameObject);
            starsLayout.spacing = 16f;
            starsLayout.childAlignment = TextAnchor.MiddleCenter;
            starsLayout.childControlWidth = false;
            starsLayout.childControlHeight = false;
            foreach (Image star in starsPanel.GetComponentsInChildren<Image>(true))
            {
                RectTransform starRect = star.rectTransform;
                starRect.sizeDelta = new Vector2(46f, 46f);
                LayoutElement layout = EnsureComponent<LayoutElement>(star.gameObject);
                layout.preferredWidth = 46f;
                layout.preferredHeight = 46f;
            }
            TMP_Text evaluation = RequireComponent<TMP_Text>(scene, "TextoResultadoLectura");
            StylePdfExistingText(evaluation, resultContainer, new Vector2(0.07f, 0.23f), new Vector2(0.93f, 0.55f), 17f, FontStyles.Bold, Purple, TextAlignmentOptions.Center);
            TMP_Text error = GetOrCreatePdfText(scene, "ErrorText", resultContainer);
            StylePdfExistingText(error, resultContainer, new Vector2(0.07f, 0.04f), new Vector2(0.93f, 0.22f), 16f, FontStyles.Bold, new Color(0.72f, 0.12f, 0.12f, 1f), TextAlignmentOptions.Center);
            TMP_Text starsResult = GetOrCreatePdfText(scene, "StarsResultText", resultContainer);
            StylePdfExistingText(starsResult, resultContainer, Vector2.zero, Vector2.zero, 1f, FontStyles.Normal, Color.clear, TextAlignmentOptions.Center);

            RectTransform controls = CreatePdfPanel(
                "PanelControles",
                rightPage,
                new Vector2(0.04f, 0.025f),
                new Vector2(0.96f, 0.16f),
                null,
                Color.clear);
            StylePdfExistingButton(startButton, controls, new Vector2(0.01f, 0.04f), new Vector2(0.24f, 0.96f), "INICIAR\nMICROFONO", LoadSprite(ButtonGreenPath), Color.white, new Color(0.12f, 0.19f, 0.18f, 1f));
            StylePdfExistingButton(stopButton, controls, new Vector2(0.26f, 0.04f), new Vector2(0.45f, 0.96f), "DETENER", LoadSprite(ButtonOrangePath), Color.white, new Color(0.16f, 0.14f, 0.12f, 1f));
            StylePdfExistingButton(clearButton, controls, new Vector2(0.47f, 0.04f), new Vector2(0.68f, 0.96f), "REINICIAR", LoadSprite(ButtonBluePath), Color.white, Color.white);
            StylePdfExistingButton(validateButton, controls, new Vector2(0.70f, 0.04f), new Vector2(0.99f, 0.96f), "VALIDAR\nLECTURA", LoadSprite(ButtonGreenPath), Color.white, new Color(0.12f, 0.19f, 0.18f, 1f));
            StylePdfExistingButton(backToSelectionButton, readingPanel, new Vector2(0.055f, 0.025f), new Vector2(0.17f, 0.08f), "VOLVER", LoadSprite(ButtonPurplePath), Color.white, Color.white);

            TMP_Text countdown = RequireComponent<TMP_Text>(scene, "TextoContador");
            StylePdfExistingText(countdown, readingPanel, new Vector2(0.37f, 0.915f), new Vector2(0.63f, 0.975f), 20f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            Image guide = CreatePdfImage(
                "GuideCharacter_Biblio_Reading",
                book,
                new Vector2(0.43f, 0.005f),
                new Vector2(0.57f, 0.25f),
                LoadSprite(BiblioIntroPath),
                Color.white);
            UIJuiceAnimator guideAnimator = EnsureComponent<UIJuiceAnimator>(guide.gameObject);
            ConfigureJuiceAnimator(guideAnimator, guide.rectTransform, guide, 5f, 0.9f, true);
            readingPanel.gameObject.SetActive(false);
        }

        private static void StylePdfExistingButton(
            Button button,
            RectTransform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            string label,
            Sprite sprite,
            Color imageColor,
            Color textColor)
        {
            RectTransform rect = button.transform as RectTransform;
            rect.SetParent(parent, false);
            SetAnchors(rect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            RemoveCompositionComponents(button.gameObject);
            ClearChildren(rect);
            Image image = EnsureComponent<Image>(button.gameObject);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = imageColor;
            image.raycastTarget = true;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            EnsureComponent<UIButtonFeedback>(button.gameObject);
            TMP_Text text = CreatePdfText("Texto", label, rect, new Vector2(0.08f, 0.1f), new Vector2(0.92f, 0.9f), 20f, FontStyles.Bold, textColor, TextAlignmentOptions.Center);
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = 21f;
        }

        private static void StylePdfExistingText(
            TMP_Text text,
            RectTransform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            float fontSize,
            FontStyles style,
            Color color,
            TextAlignmentOptions alignment)
        {
            RectTransform rect = text.rectTransform;
            rect.SetParent(parent, false);
            SetAnchors(rect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            RemoveCompositionComponents(text.gameObject);
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.enableAutoSizing = false;
            text.raycastTarget = false;
        }

        private static void StylePdfCard(RectTransform card, Color color)
        {
            RemoveCompositionComponents(card.gameObject);
            Image image = EnsureComponent<Image>(card.gameObject);
            image.sprite = LoadSprite(PanelLightPath);
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = true;
        }

        private static void StyleScrollText(Scene scene, ScrollRect scroll, string textName, float fontSize, Color color)
        {
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;
            if (scroll.viewport != null && scroll.viewport.GetComponent<RectMask2D>() == null) scroll.viewport.gameObject.AddComponent<RectMask2D>();
            TMP_Text text = RequireComponent<TMP_Text>(scene, textName);
            text.transform.SetParent(scroll.content, false);
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Normal;
            text.color = color;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = false;
            text.margin = new Vector4(22f, 18f, 22f, 18f);
            ContentSizeFitter fitter = EnsureComponent<ContentSizeFitter>(scroll.content.gameObject);
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            VerticalLayoutGroup layout = EnsureComponent<VerticalLayoutGroup>(scroll.content.gameObject);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private static TMP_Text GetOrCreatePdfText(Scene scene, string name, RectTransform parent)
        {
            GameObject root = FindObject(scene, name) ?? CreateUiObject(name, parent);
            TMP_Text text = root.GetComponent<TMP_Text>() ?? root.AddComponent<TextMeshProUGUI>();
            text.transform.SetParent(parent, false);
            return text;
        }

        private static void EnsureReadingPanelLayout(
            Scene scene,
            RectTransform readingPanel,
            ScrollRect storyScroll,
            ScrollRect recognizedScroll,
            Button startButton,
            Button stopButton,
            Button clearButton,
            Button validateButton,
            Button backToSelectionButton)
        {
            readingPanel.gameObject.SetActive(true);
            Image background = EnsureComponent<Image>(readingPanel.gameObject);
            background.color = new Color(0.96f, 0.91f, 0.78f, 1f);

            Transform legacyHeader = readingPanel.Find("EncabezadoMundo");
            if (legacyHeader != null) UnityEngine.Object.DestroyImmediate(legacyHeader.gameObject, true);

            RectTransform header = EnsureChildPanel(readingPanel, "PanelEncabezado", new Vector2(0.04f, 0.915f), new Vector2(0.96f, 0.985f), new Color(0.33f, 0.24f, 0.55f, 0.92f));
            VerticalLayoutGroup headerLayout = EnsureComponent<VerticalLayoutGroup>(header.gameObject);
            headerLayout.padding = new RectOffset(14, 14, 5, 5);
            headerLayout.spacing = 0f;
            headerLayout.childAlignment = TextAnchor.MiddleCenter;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = true;
            headerLayout.childForceExpandHeight = false;
            TMP_Text titleHeader = EnsureLabel(header, "TituloMundo", "MUNDO DE CUENTOS", 32f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            TMP_Text subtitleHeader = EnsureLabel(header, "SubtituloMundo", "Lee en voz alta y valida tu lectura", 18f, FontStyles.Normal, new Color(1f, 0.95f, 0.78f, 1f), TextAlignmentOptions.Center);
            EnsureComponent<LayoutElement>(titleHeader.gameObject).preferredHeight = 36f;
            EnsureComponent<LayoutElement>(subtitleHeader.gameObject).preferredHeight = 24f;

            RectTransform backRect = backToSelectionButton.transform as RectTransform;
            backRect.SetParent(readingPanel, false);
            SetAnchors(backRect, new Vector2(0.04f, 0.84f), new Vector2(0.18f, 0.9f), Vector2.zero, Vector2.zero);
            SetButtonLabel(backToSelectionButton, "VOLVER");

            RectTransform mainContainer = EnsureChildPanel(readingPanel, "ContenedorPrincipal", new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.825f), new Color(1f, 1f, 1f, 0f));
            HorizontalLayoutGroup mainLayout = EnsureComponent<HorizontalLayoutGroup>(mainContainer.gameObject);
            mainLayout.spacing = 28f;
            mainLayout.padding = new RectOffset(0, 0, 0, 0);
            mainLayout.childAlignment = TextAnchor.MiddleCenter;
            mainLayout.childControlWidth = true;
            mainLayout.childControlHeight = true;
            mainLayout.childForceExpandWidth = true;
            mainLayout.childForceExpandHeight = true;

            RectTransform storyPanel = EnsureSceneChildPanel(scene, mainContainer, "PanelCuento", new Color(1f, 0.98f, 0.9f, 0.98f));
            RectTransform voicePanel = EnsureSceneChildPanel(scene, mainContainer, "PanelLectura", new Color(0.9f, 0.98f, 0.98f, 0.98f));
            LayoutElement storyPanelLayout = EnsureComponent<LayoutElement>(storyPanel.gameObject);
            storyPanelLayout.preferredWidth = 830f;
            storyPanelLayout.flexibleWidth = 1f;
            storyPanelLayout.flexibleHeight = 1f;
            LayoutElement voicePanelLayout = EnsureComponent<LayoutElement>(voicePanel.gameObject);
            voicePanelLayout.preferredWidth = 830f;
            voicePanelLayout.flexibleWidth = 1f;
            voicePanelLayout.flexibleHeight = 1f;

            TMP_Text storyHeader = EnsureLabel(storyPanel, "CabeceraCuento", "CUENTO", 28f, FontStyles.Bold, new Color(0.63f, 0.25f, 0.22f, 1f), TextAlignmentOptions.Center);
            SetAnchors(storyHeader.rectTransform, new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.985f), Vector2.zero, Vector2.zero);

            TMP_Text title = RequireComponent<TMP_Text>(scene, "TituloCuento");
            title.transform.SetParent(storyPanel, false);
            title.fontSize = 32f;
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.33f, 0.24f, 0.55f, 1f);
            title.alignment = TextAlignmentOptions.Center;
            title.textWrappingMode = TextWrappingModes.Normal;
            SetAnchors(title.rectTransform, new Vector2(0.05f, 0.805f), new Vector2(0.95f, 0.895f), Vector2.zero, Vector2.zero);

            RectTransform storyCard = RequireRect(scene, "StoryTextCard");
            storyCard.SetParent(storyPanel, false);
            SetAnchors(storyCard, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.785f), Vector2.zero, Vector2.zero);
            EnsureCardImage(storyCard, new Color(1f, 1f, 1f, 0.9f));
            Stretch(storyScroll.transform as RectTransform, new Vector2(14f, 12f), new Vector2(-14f, -12f));
            storyScroll.horizontal = false;
            storyScroll.vertical = true;
            EnsureScrollText(scene, storyScroll, "TextoHistoria", 26f);

            TMP_Text voiceHeader = EnsureLabel(voicePanel, "CabeceraTuLectura", "TU LECTURA", 28f, FontStyles.Bold, new Color(0.08f, 0.4f, 0.46f, 1f), TextAlignmentOptions.Center);
            SetAnchors(voiceHeader.rectTransform, new Vector2(0.05f, 0.905f), new Vector2(0.95f, 0.985f), Vector2.zero, Vector2.zero);

            GameObject mic = RequireObject(scene, "MicrophoneListeningIndicator");
            RectTransform micRect = mic.transform as RectTransform;
            micRect.SetParent(voicePanel, false);
            SetAnchors(micRect, new Vector2(0.07f, 0.825f), new Vector2(0.93f, 0.895f), Vector2.zero, Vector2.zero);

            TMP_Text status = RequireComponent<TMP_Text>(scene, "TextoEstado");
            status.transform.SetParent(voicePanel, false);
            status.fontSize = 20f;
            status.alignment = TextAlignmentOptions.Center;
            status.textWrappingMode = TextWrappingModes.Normal;
            SetAnchors(status.rectTransform, new Vector2(0.07f, 0.748f), new Vector2(0.93f, 0.818f), Vector2.zero, Vector2.zero);

            RectTransform recognizedCard = RequireRect(scene, "RecognizedTextCard");
            recognizedCard.SetParent(voicePanel, false);
            SetAnchors(recognizedCard, new Vector2(0.04f, 0.35f), new Vector2(0.96f, 0.738f), Vector2.zero, Vector2.zero);
            EnsureCardImage(recognizedCard, new Color(1f, 1f, 1f, 0.9f));
            Stretch(recognizedScroll.transform as RectTransform, new Vector2(14f, 12f), new Vector2(-14f, -12f));
            recognizedScroll.horizontal = false;
            recognizedScroll.vertical = true;
            EnsureScrollText(scene, recognizedScroll, "TextoLecturaReconocida", 24f);
            TMP_Text placeholder = EnsureLabel(recognizedScroll.content, "TextoLecturaPlaceholder", "Aquí aparecerán las palabras que leas.", 23f, FontStyles.Italic, new Color(0.43f, 0.43f, 0.43f, 0.78f), TextAlignmentOptions.Center);
            Stretch(placeholder.rectTransform, new Vector2(12f, 18f), new Vector2(-12f, -18f));

            TMP_Text partialText = EnsureLabel(voicePanel, "TextoLecturaParcial", string.Empty, 18f, FontStyles.Normal, new Color(0.32f, 0.32f, 0.32f, 1f), TextAlignmentOptions.Center);
            SetAnchors(partialText.rectTransform, new Vector2(0.07f, 0.325f), new Vector2(0.93f, 0.348f), Vector2.zero, Vector2.zero);

            TMP_Text finalInternalText = EnsureLabel(voicePanel, "TextoLecturaFinalInterna", string.Empty, 1f, FontStyles.Normal, Color.clear, TextAlignmentOptions.TopLeft);
            SetAnchors(finalInternalText.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);

            RectTransform resultContainer = EnsureChildPanel(voicePanel, "ContenedorResultado", new Vector2(0.04f, 0.075f), new Vector2(0.96f, 0.315f), new Color(1f, 1f, 1f, 0.62f));

            RectTransform starsPanel = RequireRect(scene, "PanelEstrellas");
            starsPanel.SetParent(resultContainer, false);
            SetAnchors(starsPanel, new Vector2(0.2f, 0.58f), new Vector2(0.8f, 0.94f), Vector2.zero, Vector2.zero);
            HorizontalLayoutGroup starsLayout = EnsureComponent<HorizontalLayoutGroup>(starsPanel.gameObject);
            starsLayout.spacing = 20f;
            starsLayout.childAlignment = TextAnchor.MiddleCenter;
            starsLayout.childControlWidth = false;
            starsLayout.childControlHeight = false;
            foreach (Image star in starsPanel.GetComponentsInChildren<Image>(true))
            {
                RectTransform starRect = star.transform as RectTransform;
                starRect.sizeDelta = new Vector2(48f, 48f);
                LayoutElement starLayout = EnsureComponent<LayoutElement>(star.gameObject);
                starLayout.preferredWidth = 48f;
                starLayout.preferredHeight = 48f;
            }

            TMP_Text result = RequireComponent<TMP_Text>(scene, "TextoResultadoLectura");
            result.transform.SetParent(resultContainer, false);
            result.fontSize = 21f;
            result.alignment = TextAlignmentOptions.Center;
            result.textWrappingMode = TextWrappingModes.Normal;
            SetAnchors(result.rectTransform, new Vector2(0.06f, 0.22f), new Vector2(0.94f, 0.54f), Vector2.zero, Vector2.zero);

            TMP_Text starsResult = FindObject(scene, "StarsResultText")?.GetComponent<TMP_Text>();
            if (starsResult != null)
            {
                starsResult.transform.SetParent(resultContainer, false);
                starsResult.text = string.Empty;
                starsResult.fontSize = 1f;
                starsResult.alignment = TextAlignmentOptions.Center;
                starsResult.color = Color.clear;
                SetAnchors(starsResult.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            }

            TMP_Text error = FindObject(scene, "ErrorText")?.GetComponent<TMP_Text>();
            if (error != null)
            {
                error.transform.SetParent(resultContainer, false);
                error.fontSize = 18f;
                error.alignment = TextAlignmentOptions.Center;
                SetAnchors(error.rectTransform, new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.2f), Vector2.zero, Vector2.zero);
            }

            RectTransform controls = EnsureChildPanel(voicePanel, "PanelControles", new Vector2(0.04f, 0.015f), new Vector2(0.96f, 0.065f), new Color(1f, 1f, 1f, 0f));
            HorizontalLayoutGroup controlLayout = EnsureComponent<HorizontalLayoutGroup>(controls.gameObject);
            controlLayout.spacing = 12f;
            controlLayout.childAlignment = TextAnchor.MiddleCenter;
            controlLayout.childControlWidth = true;
            controlLayout.childControlHeight = true;
            controlLayout.childForceExpandWidth = true;
            controlLayout.childForceExpandHeight = true;

            ConfigureControlButton(startButton, controls, "INICIAR", new Color(0.22f, 0.58f, 0.42f, 1f));
            ConfigureControlButton(stopButton, controls, "DETENER", new Color(0.74f, 0.38f, 0.22f, 1f));
            ConfigureControlButton(clearButton, controls, "LIMPIAR", new Color(0.22f, 0.5f, 0.72f, 1f));
            ConfigureControlButton(validateButton, controls, "VALIDAR", new Color(0.9f, 0.45f, 0.58f, 1f));

            RectTransform countdown = RequireComponent<TMP_Text>(scene, "TextoContador").rectTransform;
            countdown.SetParent(readingPanel, false);
            SetAnchors(countdown, new Vector2(0.34f, 0.84f), new Vector2(0.66f, 0.9f), Vector2.zero, Vector2.zero);

            GameObject icon = FindObject(scene, "ImagenCuentoSeleccionado");
            if (icon != null)
            {
                RectTransform iconRect = icon.transform as RectTransform;
                iconRect.SetParent(storyPanel, false);
                SetAnchors(iconRect, new Vector2(0.04f, 0.805f), new Vector2(0.15f, 0.895f), Vector2.zero, Vector2.zero);
            }

            readingPanel.gameObject.SetActive(false);
        }

        private static Button CreateStoryCard(string name, string title, string state, RectTransform parent, Color iconColor)
        {
            GameObject root = CreateUiObject(name, parent);
            Image bg = EnsureComponent<Image>(root);
            bg.color = Card;
            Button button = EnsureComponent<Button>(root);
            EnsureComponent<UIButtonFeedback>(root);
            button.targetGraphic = bg;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.96f, 0.86f, 1f);
            colors.pressedColor = new Color(0.92f, 0.86f, 1f, 1f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            VerticalLayoutGroup layout = EnsureComponent<VerticalLayoutGroup>(root);
            layout.padding = new RectOffset(22, 22, 20, 20);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;

            GameObject icon = CreateUiObject("IconoCuento", root.transform);
            RectTransform iconRect = icon.GetComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(170f, 118f);
            LayoutElement iconLayout = EnsureComponent<LayoutElement>(icon);
            iconLayout.preferredHeight = 118f;
            Image iconImage = EnsureComponent<Image>(icon);
            iconImage.color = iconColor;
            iconImage.preserveAspect = true;

            CreateText("TituloTarjetaCuento", title, root.GetComponent<RectTransform>(), 26f, FontStyles.Bold, Purple, TextAlignmentOptions.Center, 42f);
            CreateText("EstadoTarjetaCuento", state, root.GetComponent<RectTransform>(), 22f, FontStyles.Bold, Blue, TextAlignmentOptions.Center, 34f);
            CreateText("EstrellasTarjetaCuento", "Sin estrellas", root.GetComponent<RectTransform>(), 21f, FontStyles.Bold, Pink, TextAlignmentOptions.Center, 34f);
            return button;
        }

        private static TMP_Text FindOrCreateReadingText(Scene scene, RectTransform parent, string name, string value, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject existing = FindObject(scene, name);
            if (existing != null)
            {
                existing.transform.SetParent(parent, true);
                return existing.GetComponent<TMP_Text>() ?? existing.AddComponent<TextMeshProUGUI>();
            }

            GameObject go = CreateUiObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TMP_Text text = go.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = 22f;
            text.fontStyle = FontStyles.Bold;
            text.color = Pink;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static ScrollRect EnsureExistingScroll(Scene scene, string cardName, string scrollName, string textName)
        {
            ScrollRect scroll = ComponentsInScene<ScrollRect>(scene).FirstOrDefault(item => item.name == scrollName);
            if (scroll != null)
            {
                scroll.horizontal = false;
                scroll.vertical = true;
                if (scroll.viewport != null && scroll.viewport.GetComponent<RectMask2D>() == null)
                {
                    scroll.viewport.gameObject.AddComponent<RectMask2D>();
                }

                return scroll;
            }

            RectTransform card = RequireRect(scene, cardName);
            TMP_Text text = RequireComponent<TMP_Text>(scene, textName);
            return MundoAprendoRequestedScrollHelper.ConfigureScrollableTextCard(card, text, scrollName, "Content", textName);
        }

        private static ScrollRect CreateScroll(string name, RectTransform parent, float preferredHeight)
        {
            GameObject scrollObject = CreateUiObject(name, parent);
            Image scrollImage = EnsureComponent<Image>(scrollObject);
            scrollImage.color = new Color(1f, 1f, 1f, 0.2f);
            ScrollRect scroll = EnsureComponent<ScrollRect>(scrollObject);
            LayoutElement layout = EnsureComponent<LayoutElement>(scrollObject);
            layout.preferredHeight = preferredHeight;

            GameObject viewportObject = CreateUiObject("Viewport", scrollObject.transform);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            Stretch(viewport, Vector2.zero, Vector2.zero);
            Image viewportImage = EnsureComponent<Image>(viewportObject);
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            EnsureComponent<RectMask2D>(viewportObject);

            GameObject contentObject = CreateUiObject("Content", viewportObject.transform);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            ContentSizeFitter fitter = EnsureComponent<ContentSizeFitter>(contentObject);
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 26f;
            return scroll;
        }

        private static RectTransform EnsurePanel(RectTransform canvas, string name, bool active, Color color)
        {
            GameObject panel = FindObject(canvas.gameObject.scene, name) ?? CreateUiObject(name, canvas);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.SetParent(canvas, false);
            Stretch(rect, Vector2.zero, Vector2.zero);
            Image image = EnsureComponent<Image>(panel);
            image.color = color;
            panel.SetActive(active);
            return rect;
        }

        private static TMP_Text CreateText(string name, string value, RectTransform parent, float fontSize, FontStyles style, Color color, TextAlignmentOptions alignment, float preferredHeight)
        {
            GameObject go = CreateUiObject(name, parent);
            TMP_Text text = go.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.enableAutoSizing = false;
            LayoutElement layout = EnsureComponent<LayoutElement>(go);
            layout.preferredHeight = preferredHeight;
            return text;
        }

        private static Button CreateButton(string name, string label, RectTransform parent, Vector2 size, Color color)
        {
            GameObject root = CreateUiObject(name, parent);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            LayoutElement layout = EnsureComponent<LayoutElement>(root);
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;

            Image image = EnsureComponent<Image>(root);
            image.color = color;
            Button button = EnsureComponent<Button>(root);
            EnsureComponent<UIButtonFeedback>(root);
            button.targetGraphic = image;
            CreateText("Text", label, rect, 24f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, size.y);
            return button;
        }

        private static RectTransform EnsureChildPanel(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            Transform existing = parent.Find(name);
            GameObject panel = existing != null ? existing.gameObject : CreateUiObject(name, parent);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetAnchors(rect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            Image image = EnsureComponent<Image>(panel);
            image.color = color;
            return rect;
        }

        private static RectTransform EnsureSceneChildPanel(Scene scene, RectTransform parent, string name, Color color)
        {
            GameObject panel = FindObject(scene, name) ?? CreateUiObject(name, parent);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = EnsureComponent<Image>(panel);
            image.color = color;
            return rect;
        }

        private static TMP_Text EnsureLabel(RectTransform parent, string name, string value, float fontSize, FontStyles style, Color color, TextAlignmentOptions alignment)
        {
            Transform existing = parent.Find(name);
            GameObject labelObject = existing != null ? existing.gameObject : CreateUiObject(name, parent);
            TMP_Text text = labelObject.GetComponent<TMP_Text>() ?? labelObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.enableAutoSizing = false;
            return text;
        }

        private static void ConfigureControlButton(Button button, RectTransform parent, string label, Color color)
        {
            RectTransform rect = button.transform as RectTransform;
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;

            LayoutElement layout = EnsureComponent<LayoutElement>(button.gameObject);
            layout.minWidth = 120f;
            layout.preferredWidth = 145f;
            layout.preferredHeight = 58f;

            Image image = EnsureComponent<Image>(button.gameObject);
            image.color = color;
            image.raycastTarget = true;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;

            SetButtonLabel(button, label);
        }

        private static void SetButtonLabel(Button button, string label)
        {
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text == null)
            {
                text = CreateText("Texto", label, button.transform as RectTransform, 24f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, 58f);
            }

            text.text = label;
            text.fontSize = 22f;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = false;
            Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
        }

        private static void EnsureCardImage(RectTransform rect, Color color)
        {
            Image image = EnsureComponent<Image>(rect.gameObject);
            image.color = color;
            image.raycastTarget = true;
        }

        private static void EnsureScrollText(Scene scene, ScrollRect scroll, string textName, float fontSize)
        {
            TMP_Text text = RequireComponent<TMP_Text>(scene, textName);
            text.transform.SetParent(scroll.content, false);
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = false;
            text.margin = new Vector4(12f, 10f, 12f, 10f);
            Stretch(text.rectTransform, Vector2.zero, Vector2.zero);

            ContentSizeFitter fitter = EnsureComponent<ContentSizeFitter>(scroll.content.gameObject);
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            VerticalLayoutGroup layout = EnsureComponent<VerticalLayoutGroup>(scroll.content.gameObject);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject go = new(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        private static void DestroyIfExists(Scene scene, string name)
        {
            GameObject existing = FindObject(scene, name);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing, true);
        }

        private static void ConfigureStory(SerializedProperty property, string id, string title, string text, Sprite icon, int order)
        {
            property.FindPropertyRelative("id").stringValue = id;
            property.FindPropertyRelative("titulo").stringValue = title;
            property.FindPropertyRelative("textoCompleto").stringValue = text;
            property.FindPropertyRelative("icono").objectReferenceValue = icon;
            property.FindPropertyRelative("orden").intValue = order;
            property.FindPropertyRelative("disponible").boolValue = true;
            property.FindPropertyRelative("claveProgreso").stringValue = StoryProgressRepository.GetStoryScoreKey(id);
        }

        private static void ConfigureCard(SerializedProperty property, string cuentoId, Button button)
        {
            property.FindPropertyRelative("cuentoId").stringValue = cuentoId;
            property.FindPropertyRelative("button").objectReferenceValue = button;
            property.FindPropertyRelative("iconImage").objectReferenceValue = button.transform.Find("IconoCuento")?.GetComponent<Image>();
            property.FindPropertyRelative("titleText").objectReferenceValue = button.transform.Find("TituloTarjetaCuento")?.GetComponent<TMP_Text>();
            property.FindPropertyRelative("completedText").objectReferenceValue = button.transform.Find("EstadoTarjetaCuento")?.GetComponent<TMP_Text>();
            property.FindPropertyRelative("starsText").objectReferenceValue = button.transform.Find("EstrellasTarjetaCuento")?.GetComponent<TMP_Text>();
        }

        private static void ClearPersistentListeners(UnityEvent unityEvent)
        {
            for (int i = unityEvent.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                UnityEventTools.RemovePersistentListener(unityEvent, i);
            }
        }

        private static void SetReference(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) throw new InvalidOperationException($"No existe la propiedad serializada {propertyName}.");
            property.objectReferenceValue = value;
        }

        private static void SetObjectArray(SerializedProperty property, UnityEngine.Object[] values)
        {
            if (property == null) throw new InvalidOperationException("No existe la propiedad serializada de arreglo.");
            property.arraySize = values?.Length ?? 0;
            for (int i = 0; i < property.arraySize; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static void SetVector2Array(SerializedProperty property, Vector2[] values)
        {
            if (property == null) throw new InvalidOperationException("No existe la propiedad serializada de Vector2[].");
            property.arraySize = values?.Length ?? 0;
            for (int i = 0; i < property.arraySize; i++)
            {
                property.GetArrayElementAtIndex(i).vector2Value = values[i];
            }
        }

        private static Sprite LoadSprite(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null) return null;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name) return child;
            }

            return null;
        }

        private static T RequireComponent<T>(Scene scene, string objectName) where T : Component
        {
            GameObject go = RequireObject(scene, objectName);
            T component = go.GetComponent<T>();
            if (component == null) throw new InvalidOperationException($"{objectName} no tiene {typeof(T).Name}.");
            return component;
        }

        private static GameObject RequireObject(Scene scene, string objectName)
        {
            GameObject go = FindObject(scene, objectName);
            if (go == null) throw new InvalidOperationException($"No se encontro {objectName} en {scene.path}.");
            return go;
        }

        private static RectTransform RequireRect(Scene scene, string objectName)
        {
            return RequireComponent<RectTransform>(scene, objectName);
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

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            return target.GetComponent<T>() ?? target.AddComponent<T>();
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }

        private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }
    }

    internal static class MundoAprendoRequestedScrollHelper
    {
        public static ScrollRect ConfigureScrollableTextCard(
            RectTransform card,
            TMP_Text text,
            string scrollName,
            string contentName,
            string textName)
        {
            GameObject scrollObject = new(scrollName, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            RectTransform scrollRectTransform = (RectTransform)scrollObject.transform;
            scrollRectTransform.SetParent(card, false);
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.offsetMin = new Vector2(14f, 12f);
            scrollRectTransform.offsetMax = new Vector2(-14f, -12f);

            Image scrollRaycast = scrollObject.GetComponent<Image>();
            scrollRaycast.color = new Color(1f, 1f, 1f, 0.001f);

            GameObject viewportObject = new("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            RectTransform viewport = (RectTransform)viewportObject.transform;
            viewport.SetParent(scrollRectTransform, false);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            viewportObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.001f);

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

            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;
            return scroll;
        }
    }
}
#endif
