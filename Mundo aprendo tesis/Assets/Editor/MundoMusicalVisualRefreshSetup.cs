#if UNITY_EDITOR
using System;
using System.Linq;
using Assets.SurpriseBox.Scripts;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bolin.Editor
{
    /// <summary>
    /// Restyles the already-wired Mundo Musical hierarchy in place.  It deliberately
    /// preserves every controller, tutorial object, button, key and scene reference;
    /// only persistent presentation objects, sprites and RectTransforms are changed.
    /// </summary>
    public static class MundoMusicalVisualRefreshSetup
    {
        private const string ScenePath = "Assets/Scenes/MundoMusical.unity";
        private const string BackgroundPath = "Assets/Mundo Aprendo/Fondos/Mundo Musical/Mundo musical.png";
        private const string TitleBannerPath = "Assets/MundoAprendo/UI/Generated/MundoTamanos/TitleBannerPurple.png";
        private const string InstructionPanelPath = "Assets/MundoAprendo/UI/Generated/MundoTamanos/InstructionPanelCream.png";
        private const string CardPath = "Assets/MundoAprendo/UI/Generated/MundoTamanos/AnimalCardCream.png";
        private const string PurpleButtonPath = "Assets/MundoAprendo/UI/Generated/MundoTamanos/AnswerButtonPurple.png";
        private const string GreenButtonPath = "Assets/MundoAprendo/UI/Generated/MundoTamanos/AnswerButtonGreen.png";
        private const string StarPanelPath = "Assets/MundoAprendo/UI/Generated/MundoTamanos/StarPanel.png";
        private const string BackArrowPath = "Assets/MundoAprendo/UI/Generated/MundoTamanos/BackArrow.png";
        private const string FullStarPath = "Assets/Hyper_Casual_UI/Sprites/Usar/rataing star.png";
        private const string EmptyStarPath = "Assets/Hyper_Casual_UI/Sprites/Usar/rataing star (1).png";
        private const string TamborcinHelloPath = "Assets/Mundo Aprendo/Personajes/_Poses/Tamborcin_saludo.png";

        private static readonly Color Ink = new(0.23f, 0.14f, 0.36f, 1f);
        private static readonly Color Purple = new(0.38f, 0.20f, 0.66f, 1f);
        private static readonly Color Cream = new(1f, 0.98f, 0.91f, 1f);
        private static readonly Color[] NumberColors =
        {
            new Color(0.88f, 0.28f, 0.42f, 1f),
            new Color(0.96f, 0.51f, 0.18f, 1f),
            new Color(0.93f, 0.70f, 0.15f, 1f),
            new Color(0.30f, 0.67f, 0.35f, 1f),
            new Color(0.20f, 0.53f, 0.88f, 1f),
            new Color(0.45f, 0.32f, 0.80f, 1f),
            new Color(0.78f, 0.29f, 0.62f, 1f)
        };

        [MenuItem("Mundo Aprendo/Mundo Musical/Aplicar rediseño persistente")]
        public static void Apply()
        {
            MundoMusicalGeneratedUiAssets.EnsureAssets();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Canvas canvas = ComponentsInScene<Canvas>(scene).FirstOrDefault(item => item.isRootCanvas);
            if (canvas == null)
            {
                throw new InvalidOperationException("MundoMusical no contiene un Canvas raíz.");
            }

            RectTransform musicalUi = RequireRect(scene, "MusicalUI");
            RectTransform tutorial = RequireRect(scene, "TutorialOverlay");
            Transform transition = ComponentsInScene<Transform>(scene).FirstOrDefault(item => item.name == "SceneTransitionOverlay");

            Sprite background = RequireSprite(BackgroundPath);
            Sprite titleBanner = RequireSprite(TitleBannerPath);
            Sprite instructionPanel = RequireSprite(InstructionPanelPath);
            Sprite card = RequireSprite(CardPath);
            Sprite purpleButton = RequireSprite(PurpleButtonPath);
            Sprite greenButton = RequireSprite(GreenButtonPath);
            Sprite starPanel = RequireSprite(StarPanelPath);
            Sprite backArrow = RequireSprite(BackArrowPath);
            Sprite star = RequireSprite(FullStarPath);
            Sprite emptyStar = RequireSprite(EmptyStarPath);
            Sprite tamborcinHello = RequireSprite(TamborcinHelloPath);
            Sprite key = RequireSprite(MundoMusicalGeneratedUiAssets.MusicKeyCreamPath);
            Sprite keyGlow = RequireSprite(MundoMusicalGeneratedUiAssets.MusicKeyGlowPath);
            Sprite sequenceBubble = RequireSprite(MundoMusicalGeneratedUiAssets.MusicSequenceBubblePath);
            Sprite pianoFrame = RequireSprite(MundoMusicalGeneratedUiAssets.MusicPianoFramePath);
            Sprite listenGlow = AssetDatabase.LoadAssetAtPath<Sprite>(MundoMusicalGeneratedUiAssets.MusicListenButtonOutlinePath)
                ?? RequireSprite(MundoMusicalGeneratedUiAssets.MusicListenGlowPath);
            Sprite titleNote = RequireSprite(MundoMusicalGeneratedUiAssets.MusicNoteGoldenPath);

            ConfigureCanvas(canvas);
            ConfigureMusicRoot(musicalUi, background);
            ConfigureTopNavigation(musicalUi, titleBanner, instructionPanel, purpleButton, starPanel, star, backArrow, titleNote);
            ConfigureCharacterAndStartView(musicalUi, instructionPanel, card, greenButton, tamborcinHello);
            ConfigureGameplay(musicalUi, instructionPanel, card, purpleButton, greenButton, key, keyGlow, sequenceBubble, pianoFrame);
            ConfigureResultView(musicalUi, card, purpleButton, greenButton);
            ConfigureTutorial(tutorial, card, purpleButton, greenButton, tamborcinHello);
            ConfigureProgressFeedback(scene, star, emptyStar, listenGlow);

            EnsureEventSystem(scene);
            if (transition != null) transition.SetAsLastSibling();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("No se pudo guardar el rediseño persistente de MundoMusical.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("MundoMusicalVisualRefreshSetup: rediseño persistente aplicado sin sustituir la lógica ni el tutorial.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void ConfigureCanvas(Canvas canvas)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        private static void ConfigureMusicRoot(RectTransform root, Sprite background)
        {
            Stretch(root);
            Image backgroundImage = RequireImage(root, "FondoMundoMusical");
            SetSprite(backgroundImage, background, Image.Type.Simple);
            Stretch(backgroundImage.rectTransform);
            backgroundImage.preserveAspect = false;
            backgroundImage.raycastTarget = false;

            // The old note decorations stay in the scene as a harmless hierarchy
            // record, but the new title note asset is used where it is legible.
            foreach (Transform decoration in root.Cast<Transform>().Where(item => item.name == "NotaDecorativa"))
            {
                Image image = decoration.GetComponent<Image>();
                if (image != null) image.enabled = false;
            }
        }

        private static void ConfigureTopNavigation(
            RectTransform root,
            Sprite titleBanner,
            Sprite instructionPanel,
            Sprite purpleButton,
            Sprite starPanel,
            Sprite star,
            Sprite backArrow,
            Sprite titleNote)
        {
            RectTransform topBar = RequireRect(root, "TopBar");
            Stretch(topBar);
            Image topBarImage = topBar.GetComponent<Image>();
            if (topBarImage != null)
            {
                topBarImage.color = Color.clear;
                topBarImage.raycastTarget = false;
            }

            Button backButton = RequireButton(topBar, "BotonVolver");
            SetButtonSprite(backButton, purpleButton, new Vector2(-815f, 452f), new Vector2(220f, 70f), Color.white);
            TMP_Text backText = backButton.GetComponentInChildren<TMP_Text>(true);
            if (backText != null)
            {
                SetText(backText, "VOLVER", 23f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
                SetRect(backText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(20f, 0f), new Vector2(144f, 54f));
            }
            Image backIcon = GetOrCreateImage(backButton.transform, "IconoVolver");
            SetSprite(backIcon, backArrow, Image.Type.Simple);
            SetRect(backIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-75f, 0f), new Vector2(38f, 38f));
            backIcon.preserveAspect = true;

            Image titlePlate = GetOrCreateImage(topBar, "PlacaTituloMusical");
            SetSprite(titlePlate, titleBanner, Image.Type.Sliced);
            SetRect(titlePlate.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 452f), new Vector2(700f, 126f));

            TMP_Text title = RequireText(root, "TituloMundoMusical");
            title.rectTransform.SetParent(titlePlate.transform, false);
            SetText(title, "Mundo Musical", 47f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            Stretch(title.rectTransform, 86f, 12f);

            Image leftNote = GetOrCreateImage(titlePlate.transform, "NotaTituloIzquierda");
            SetSprite(leftNote, titleNote, Image.Type.Simple);
            SetRect(leftNote.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-278f, 0f), new Vector2(65f, 65f));
            leftNote.preserveAspect = true;
            leftNote.raycastTarget = false;
            Image rightNote = GetOrCreateImage(titlePlate.transform, "NotaTituloDerecha");
            SetSprite(rightNote, titleNote, Image.Type.Simple);
            SetRect(rightNote.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(278f, 0f), new Vector2(65f, 65f));
            rightNote.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            rightNote.preserveAspect = true;
            rightNote.raycastTarget = false;

            Image starsPlate = GetOrCreateImage(topBar, "TopStars");
            SetSprite(starsPlate, starPanel, Image.Type.Sliced);
            SetRect(starsPlate.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(775f, 452f), new Vector2(260f, 76f));
            for (int index = 0; index < 3; index++)
            {
                Image starImage = GetOrCreateImage(starsPlate.transform, "EstrellaSuperior_" + (index + 1));
                SetSprite(starImage, star, Image.Type.Simple);
                SetRect(starImage.rectTransform, new Vector2(0.2f + index * 0.3f, 0.5f), Vector2.zero, new Vector2(55f, 55f));
                starImage.preserveAspect = true;
                starImage.raycastTarget = false;
            }

            Image sequencePill = GetOrCreateImage(root, "PildoraSecuencia");
            SetSprite(sequencePill, instructionPanel, Image.Type.Sliced);
            SetRect(sequencePill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(210f, 218f), new Vector2(430f, 68f));
            TMP_Text sequence = RequireText(root, "TextoSecuencia");
            sequence.rectTransform.SetParent(sequencePill.transform, false);
            SetText(sequence, "Secuencia: Inicio", 25f, FontStyles.Bold, Purple, TextAlignmentOptions.Center);
            Stretch(sequence.rectTransform, 20f, 8f);

            Image errorsPill = GetOrCreateImage(topBar, "PildoraErrores");
            SetSprite(errorsPill, purpleButton, Image.Type.Sliced);
            SetRect(errorsPill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(770f, 372f), new Vector2(250f, 50f));
            TMP_Text errors = RequireText(root, "TextoErrores");
            errors.rectTransform.SetParent(errorsPill.transform, false);
            SetText(errors, "Errores: 0", 20f, FontStyles.Bold, Cream, TextAlignmentOptions.Center);
            Stretch(errors.rectTransform, 12f, 6f);
        }

        private static void ConfigureCharacterAndStartView(
            RectTransform root,
            Sprite instructionPanel,
            Sprite card,
            Sprite greenButton,
            Sprite tamborcinHello)
        {
            RectTransform character = RequireRect(root, "Tamborcin");
            SetRect(character, new Vector2(0.5f, 0.5f), new Vector2(-760f, -188f), new Vector2(330f, 378f));
            Image characterImage = character.GetComponent<Image>();
            if (characterImage != null)
            {
                characterImage.sprite = tamborcinHello;
                characterImage.color = Color.white;
                characterImage.preserveAspect = true;
                characterImage.raycastTarget = false;
            }

            Image dialogue = RequireImage(root, "DialogoTamborcin");
            SetSprite(dialogue, instructionPanel, Image.Type.Sliced);
            SetRect(dialogue.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-650f, 132f), new Vector2(450f, 172f));
            TMP_Text status = RequireText(dialogue.rectTransform, "TextoEstado");
            SetText(status, "Escucha la secuencia y repite tocando los números.", 25f, FontStyles.Bold, Ink, TextAlignmentOptions.Center);
            Stretch(status.rectTransform, 22f, 18f);

            Image startView = RequireImage(root, "StartView");
            SetSprite(startView, card, Image.Type.Sliced);
            SetRect(startView.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(210f, -25f), new Vector2(1030f, 590f));

            TMP_Text welcome = RequireText(startView.rectTransform, "TextoBienvenida");
            SetText(welcome, "¡Vamos a crear música!", 46f, FontStyles.Bold, Purple, TextAlignmentOptions.Center);
            SetRect(welcome.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 155f), new Vector2(790f, 70f));
            TMP_Text instructions = RequireText(startView.rectTransform, "InstruccionesInicio");
            SetText(instructions, "Escucha una secuencia de números y toca\nlas mismas teclas en el mismo orden.", 30f, FontStyles.Normal, Ink, TextAlignmentOptions.Center);
            SetRect(instructions.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 34f), new Vector2(760f, 126f));

            Button start = RequireButton(startView.rectTransform, "BotonIniciarActividad");
            SetButtonSprite(start, greenButton, new Vector2(0f, -158f), new Vector2(320f, 82f), Color.white);
            SetButtonLabel(start, "INICIAR", 27f, Color.white);
        }

        private static void ConfigureGameplay(
            RectTransform root,
            Sprite instructionPanel,
            Sprite card,
            Sprite purpleButton,
            Sprite greenButton,
            Sprite keySprite,
            Sprite keyGlow,
            Sprite sequenceBubble,
            Sprite pianoFrameSprite)
        {
            Image gameplay = RequireImage(root, "GameplayPanel");
            SetSprite(gameplay, card, Image.Type.Sliced);
            SetRect(gameplay.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(210f, -30f), new Vector2(1080f, 742f));

            TMP_Text heading = RequireText(gameplay.rectTransform, "EncabezadoJuego");
            SetText(heading, "Escucha y repite los números", 37f, FontStyles.Bold, Purple, TextAlignmentOptions.Center);
            SetRect(heading.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 283f), new Vector2(880f, 60f));

            RectTransform progress = RequireRect(gameplay.rectTransform, "ProgresoSecuencia");
            SetRect(progress, new Vector2(0.5f, 0.5f), new Vector2(0f, 205f), new Vector2(640f, 78f));
            for (int index = 0; index < 8; index++)
            {
                Image step = RequireImage(progress, "Paso_" + (index + 1));
                SetSprite(step, sequenceBubble, Image.Type.Simple);
                step.color = new Color(0.75f, 0.72f, 0.82f, 0.95f);
                SetRect(step.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-245f + index * 70f, 0f), new Vector2(64f, 64f));
                step.preserveAspect = true;
                TMP_Text label = RequireText(step.rectTransform, "Nota");
                SetText(label, string.Empty, 31f, FontStyles.Bold, Purple, TextAlignmentOptions.Center);
                Stretch(label.rectTransform, 7f, 7f);

                if (index < 7)
                {
                    Image connector = GetOrCreateImage(progress, "ConectorSecuencia_" + (index + 1));
                    connector.sprite = BuiltinSprite();
                    connector.type = Image.Type.Sliced;
                    connector.color = new Color(0.52f, 0.31f, 0.78f, 0.42f);
                    connector.raycastTarget = false;
                    SetRect(connector.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-210f + index * 70f, 0f), new Vector2(18f, 7f));
                }
            }

            Image pianoFrame = RequireImage(gameplay.rectTransform, "PianoFrame");
            SetSprite(pianoFrame, pianoFrameSprite, Image.Type.Sliced);
            SetRect(pianoFrame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -50f), new Vector2(950f, 362f));
            TMP_Text pianoCaption = RequireText(pianoFrame.rectTransform, "TituloPiano");
            SetText(pianoCaption, "TOCA LOS NÚMEROS", 22f, FontStyles.Bold, new Color(1f, 0.92f, 0.55f, 1f), TextAlignmentOptions.Center);
            SetRect(pianoCaption.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 134f), new Vector2(550f, 34f));

            RectTransform keyRow = RequireRect(pianoFrame.rectTransform, "KeysContainer");
            SetRect(keyRow, new Vector2(0.5f, 0.5f), new Vector2(0f, -42f), new Vector2(830f, 224f));
            HorizontalLayoutGroup layout = keyRow.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.padding = new RectOffset(28, 28, 0, 0);
                layout.spacing = 13f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }

            for (int index = 0; index < 7; index++)
            {
                Button pianoKey = RequireButton(keyRow, "Key_" + (index + 1));
                RectTransform keyRect = pianoKey.GetComponent<RectTransform>();
                keyRect.sizeDelta = new Vector2(98f, 216f);
                keyRect.localScale = Vector3.one;
                LayoutElement element = pianoKey.GetComponent<LayoutElement>();
                if (element != null)
                {
                    element.minWidth = 98f;
                    element.preferredWidth = 98f;
                    element.flexibleWidth = 0f;
                    element.minHeight = 216f;
                    element.preferredHeight = 216f;
                    element.flexibleHeight = 0f;
                }

                Image rootImage = pianoKey.GetComponent<Image>();
                if (rootImage != null)
                {
                    rootImage.color = new Color(1f, 1f, 1f, 0.001f);
                    rootImage.raycastTarget = true;
                }

                Image visual = RequireImage(pianoKey.transform, "KeyVisual");
                SetSprite(visual, keySprite, Image.Type.Sliced);
                visual.color = Color.white;
                Stretch(visual.rectTransform, 2f, 2f);
                visual.raycastTarget = false;

                Image glow = RequireImage(visual.rectTransform, "Glow");
                SetSprite(glow, keyGlow, Image.Type.Sliced);
                glow.color = new Color(1f, 0.86f, 0.25f, 0f);
                Stretch(glow.rectTransform, 0f, 0f);
                glow.raycastTarget = false;

                TMP_Text number = RequireText(pianoKey.transform, "NumberText");
                SetText(number, (index + 1).ToString(), 45f, FontStyles.Bold, NumberColors[index], TextAlignmentOptions.Center);
                Stretch(number.rectTransform, 10f, 10f);
            }

            Button listen = RequireButton(gameplay.rectTransform, "BotonEscuchar");
            SetButtonSprite(listen, greenButton, new Vector2(-250f, -315f), new Vector2(230f, 68f), Color.white);
            SetButtonLabel(listen, "ESCUCHAR", 23f, Color.white);
            Button retry = RequireButton(gameplay.rectTransform, "BotonReintentar");
            SetButtonSprite(retry, purpleButton, new Vector2(0f, -315f), new Vector2(230f, 68f), Color.white);
            SetButtonLabel(retry, "REINTENTAR", 23f, Color.white);
            Button help = RequireButton(gameplay.rectTransform, "BotonAyuda");
            SetButtonSprite(help, instructionPanel, new Vector2(250f, -315f), new Vector2(230f, 68f), Ink);
            SetButtonLabel(help, "AYUDA", 23f, Ink);
        }

        private static void ConfigureProgressFeedback(Scene scene, Sprite fullStar, Sprite emptyStar, Sprite listenGlowSprite)
        {
            RectTransform topStars = RequireRect(scene, "TopStars");
            Image[] topStarImages = topStars.GetComponentsInChildren<Image>(true)
                .Where(image => image.name.StartsWith("EstrellaSuperior_", StringComparison.Ordinal))
                .OrderBy(image => image.name)
                .ToArray();
            if (topStarImages.Length != 3)
            {
                throw new InvalidOperationException("TopStars debe conservar exactamente tres estrellas persistentes.");
            }

            UIStarDisplay topStarDisplay = topStars.GetComponent<UIStarDisplay>();
            if (topStarDisplay == null) topStarDisplay = topStars.gameObject.AddComponent<UIStarDisplay>();
            SerializedObject topStarsSerialized = new(topStarDisplay);
            SetObjectReferenceArray(topStarsSerialized, "stars", topStarImages);
            topStarsSerialized.FindProperty("earnedSprite").objectReferenceValue = fullStar;
            topStarsSerialized.FindProperty("unearnedSprite").objectReferenceValue = emptyStar;
            topStarsSerialized.ApplyModifiedPropertiesWithoutUndo();
            topStarDisplay.SetImmediate(3);

            RectTransform gameplay = RequireRect(scene, "GameplayPanel");
            Button listen = RequireButton(gameplay, "BotonEscuchar");
            Image tutorialGlow = GetOrCreateImage(gameplay, "EscucharTutorialGlow");
            SetSprite(tutorialGlow, listenGlowSprite, Image.Type.Sliced);
            SetRect(tutorialGlow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-250f, -315f), new Vector2(252f, 86f));
            tutorialGlow.raycastTarget = false;
            tutorialGlow.rectTransform.SetSiblingIndex(Mathf.Max(0, listen.transform.GetSiblingIndex()));
            tutorialGlow.gameObject.SetActive(false);
            Outline tutorialGlowOutline = tutorialGlow.GetComponent<Outline>();
            if (tutorialGlowOutline == null) tutorialGlowOutline = tutorialGlow.gameObject.AddComponent<Outline>();
            tutorialGlowOutline.effectColor = Color.clear;
            tutorialGlowOutline.effectDistance = Vector2.zero;
            tutorialGlowOutline.useGraphicAlpha = false;
            tutorialGlowOutline.enabled = false;

            MusicalListenButtonGuide guide = listen.GetComponent<MusicalListenButtonGuide>();
            if (guide == null) guide = listen.gameObject.AddComponent<MusicalListenButtonGuide>();
            SerializedObject guideSerialized = new(guide);
            guideSerialized.FindProperty("listenButton").objectReferenceValue = listen;
            guideSerialized.FindProperty("glowRoot").objectReferenceValue = tutorialGlow.rectTransform;
            guideSerialized.FindProperty("glowImage").objectReferenceValue = tutorialGlow;
            guideSerialized.FindProperty("glowOutline").objectReferenceValue = tutorialGlowOutline;
            guideSerialized.FindProperty("glowColor").colorValue = Color.white;
            guideSerialized.FindProperty("glowIntensity").floatValue = 1f;
            guideSerialized.FindProperty("glowExpansion").floatValue = 0f;
            guideSerialized.FindProperty("borderThickness").floatValue = 0f;
            guideSerialized.FindProperty("borderOpacity").floatValue = 0f;
            guideSerialized.FindProperty("peakScale").floatValue = 1.08f;
            guideSerialized.FindProperty("minimumAlpha").floatValue = 0.46f;
            guideSerialized.FindProperty("maximumAlpha").floatValue = 1f;
            guideSerialized.ApplyModifiedPropertiesWithoutUndo();

            MundoMusicalSequenceGame manager = ComponentsInScene<MundoMusicalSequenceGame>(scene).SingleOrDefault();
            if (manager == null) throw new InvalidOperationException("MundoMusical no contiene su controlador de secuencias.");
            SerializedObject managerSerialized = new(manager);
            SerializedProperty topDisplayProperty = managerSerialized.FindProperty("topStarDisplay");
            SerializedProperty guideProperty = managerSerialized.FindProperty("listenButtonGuide");
            if (topDisplayProperty == null || guideProperty == null)
            {
                throw new InvalidOperationException("El controlador musical no expone las referencias de estrellas o guía visual.");
            }

            topDisplayProperty.objectReferenceValue = topStarDisplay;
            guideProperty.objectReferenceValue = guide;
            managerSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectReferenceArray(SerializedObject target, string propertyName, UnityEngine.Object[] values)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property == null || !property.isArray)
            {
                throw new InvalidOperationException($"No se encontró el arreglo serializado {propertyName}.");
            }

            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        private static void ConfigureResultView(RectTransform root, Sprite card, Sprite purpleButton, Sprite greenButton)
        {
            Image result = RequireImage(root, "ResultView");
            SetSprite(result, card, Image.Type.Sliced);
            SetRect(result.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(210f, -5f), new Vector2(940f, 650f));

            TMP_Text title = RequireText(result.rectTransform, "TituloResultado");
            SetText(title, "¡Excelente música!", 47f, FontStyles.Bold, Purple, TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 224f), new Vector2(720f, 68f));
            TMP_Text resultText = RequireText(result.rectTransform, "TextoResultado");
            SetText(resultText, "Actividad completada", 28f, FontStyles.Normal, Ink, TextAlignmentOptions.Center);
            SetRect(resultText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 156f), new Vector2(690f, 82f));

            Button replay = RequireButton(result.rectTransform, "BotonRepetirActividad");
            SetButtonSprite(replay, purpleButton, new Vector2(-260f, -222f), new Vector2(230f, 68f), Color.white);
            SetButtonLabel(replay, "REPETIR", 23f, Color.white);
            Button next = RequireButton(result.rectTransform, "BotonSiguienteMundo");
            SetButtonSprite(next, greenButton, new Vector2(0f, -222f), new Vector2(230f, 68f), Color.white);
            SetButtonLabel(next, "SIGUIENTE", 23f, Color.white);
            Button back = RequireButton(result.rectTransform, "BotonVolverResultado");
            SetButtonSprite(back, purpleButton, new Vector2(260f, -222f), new Vector2(210f, 68f), Color.white);
            SetButtonLabel(back, "VOLVER", 23f, Color.white);
        }

        private static void ConfigureTutorial(RectTransform tutorial, Sprite card, Sprite purpleButton, Sprite greenButton, Sprite tamborcinHello)
        {
            Stretch(tutorial);
            Image blocker = tutorial.GetComponent<Image>();
            if (blocker != null)
            {
                blocker.sprite = BuiltinSprite();
                blocker.type = Image.Type.Sliced;
                blocker.color = new Color(0.17f, 0.11f, 0.34f, 0.76f);
                blocker.raycastTarget = true;
            }

            Image tutorialCharacter = RequireImage(tutorial, "TamborcinTutorial");
            tutorialCharacter.sprite = tamborcinHello;
            tutorialCharacter.color = Color.white;
            tutorialCharacter.preserveAspect = true;
            tutorialCharacter.raycastTarget = false;
            SetRect(tutorialCharacter.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-600f, -60f), new Vector2(410f, 520f));

            Image dialogue = RequireImage(tutorial, "DialogoTutorial");
            SetSprite(dialogue, card, Image.Type.Sliced);
            SetRect(dialogue.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(230f, 20f), new Vector2(900f, 530f));

            TMP_Text progress = RequireText(dialogue.rectTransform, "ProgresoTutorial");
            SetText(progress, "1/5", 25f, FontStyles.Bold, Purple, TextAlignmentOptions.Center);
            SetRect(progress.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 186f), new Vector2(180f, 42f));
            TMP_Text text = RequireText(dialogue.rectTransform, "TextoTutorial");
            SetText(text, string.Empty, 36f, FontStyles.Bold, Ink, TextAlignmentOptions.Center);
            SetRect(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 52f), new Vector2(740f, 210f));

            Button next = RequireButton(dialogue.rectTransform, "BotonSiguienteTutorial");
            SetButtonSprite(next, purpleButton, new Vector2(0f, -172f), new Vector2(290f, 74f), Color.white);
            SetButtonLabel(next, "SIGUIENTE", 24f, Color.white);
            Button start = RequireButton(dialogue.rectTransform, "BotonComenzarTutorial");
            SetButtonSprite(start, greenButton, new Vector2(0f, -172f), new Vector2(310f, 74f), Color.white);
            SetButtonLabel(start, "COMENZAR", 24f, Color.white);
        }

        private static void SetButtonSprite(Button button, Sprite sprite, Vector2 position, Vector2 size, Color color)
        {
            RectTransform rect = button.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0.5f), position, size);
            Image image = button.GetComponent<Image>();
            if (image == null) return;
            SetSprite(image, sprite, Image.Type.Sliced);
            image.color = color;
            image.raycastTarget = true;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
        }

        private static void SetButtonLabel(Button button, string value, float size, Color color)
        {
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text == null) return;
            SetText(text, value, size, FontStyles.Bold, color, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, 14f, 8f);
        }

        private static void SetSprite(Image image, Sprite sprite, Image.Type type)
        {
            image.sprite = sprite;
            image.type = type;
            image.color = Color.white;
            image.raycastTarget = false;
        }

        private static void SetText(TMP_Text text, string value, float size, FontStyles style, Color color, TextAlignmentOptions alignment)
        {
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.enableAutoSizing = false;
            text.raycastTarget = false;
        }

        private static Image GetOrCreateImage(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
                gameObject.transform.SetParent(parent, false);
                child = gameObject.transform;
            }

            Image image = child.GetComponent<Image>();
            return image != null ? image : child.gameObject.AddComponent<Image>();
        }

        private static RectTransform RequireRect(Scene scene, string name)
        {
            RectTransform rect = ComponentsInScene<RectTransform>(scene).FirstOrDefault(item => item.name == name);
            if (rect == null) throw new InvalidOperationException($"Falta el objeto de jerarquía requerido: {name}");
            return rect;
        }

        private static RectTransform RequireRect(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null || child is not RectTransform rect)
            {
                throw new InvalidOperationException($"Falta el hijo de jerarquía requerido: {parent.name}/{name}");
            }

            return rect;
        }

        private static Image RequireImage(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            Image image = child != null ? child.GetComponent<Image>() : null;
            if (image == null) throw new InvalidOperationException($"Falta la imagen requerida: {parent.name}/{name}");
            return image;
        }

        private static TMP_Text RequireText(Transform parent, string name)
        {
            TMP_Text text = parent.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(item => item.name == name);
            if (text == null) throw new InvalidOperationException($"Falta el texto requerido: {parent.name}/{name}");
            return text;
        }

        private static Button RequireButton(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            Button button = child != null ? child.GetComponent<Button>() : null;
            if (button == null) throw new InvalidOperationException($"Falta el botón requerido: {parent.name}/{name}");
            return button;
        }

        private static Sprite RequireSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException($"No se pudo cargar el sprite: {path}");
            return sprite;
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

        private static void Stretch(RectTransform rect, float horizontalInset = 0f, float verticalInset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontalInset, verticalInset);
            rect.offsetMax = new Vector2(-horizontalInset, -verticalInset);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static Sprite BuiltinSprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        }

        private static void EnsureEventSystem(Scene scene)
        {
            if (ComponentsInScene<EventSystem>(scene).Length > 0) return;
            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            SceneManager.MoveGameObjectToScene(eventSystem, scene);
        }

        private static T[] ComponentsInScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        }
    }
}
#endif
