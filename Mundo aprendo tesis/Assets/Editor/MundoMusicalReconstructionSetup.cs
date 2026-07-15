#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SurpriseBox.Scripts;
using Bolin;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bolin.Editor
{
    /// <summary>
    /// Persists the Mundo Musical presentation in the scene.  It intentionally does
    /// not manufacture sequences, scoring, or tones: those remain in
    /// MundoMusicalSequenceGame.
    /// </summary>
    public static class MundoMusicalReconstructionSetup
    {
        private const string ScenePath = "Assets/Scenes/MundoMusical.unity";
        private const string BackgroundPath = "Assets/Mundo Aprendo/Fondos/Mundo Musical/Mundo musical.png";
        private const string PanelPath = "Assets/Cartoon UI/Panels/Panel Light.png";
        private const string GreenButtonPath = "Assets/Cartoon UI/Buttons/Long Square/Long Square Green.png";
        private const string PurpleButtonPath = "Assets/Cartoon UI/Buttons/Long Square/Long Square Purple.png";
        private const string BlueButtonPath = "Assets/Cartoon UI/Buttons/Long Square/Long Square Blue.png";
        private const string StarPath = "Assets/Mundo Aprendo/Imagenes/Prefabs/MusicalStarFull.png";
        private const string StarOffPath = "Assets/Mundo Aprendo/Imagenes/Prefabs/MusicalStarEmpty.png";
        private const string NotePath = "Assets/Cartoon UI/Icons/Notes.png";
        private const string TamborcinHelloPath = "Assets/Mundo Aprendo/Personajes/_Poses/Tamborcin_saludo.png";
        private const string TamborcinListeningPath = "Assets/Mundo Aprendo/Personajes/_Poses/Tamborcin_animo.png";
        private const string TamborcinCelebratePath = "Assets/Mundo Aprendo/Personajes/_Poses/Tamborcin_alegre.png";

        // These names remain the internal musical identities.  The child only sees
        // the numerical labels below; changing a label must never affect a sequence.
        private static readonly string[] NoteNames = { "DO", "RE", "MI", "FA", "SOL", "LA", "SI" };
        private static readonly string[] KeyLabels = { "1", "2", "3", "4", "5", "6", "7" };
        private static readonly Color[] KeyColors =
        {
            new Color(0.96f, 0.44f, 0.35f, 1f),
            new Color(0.98f, 0.64f, 0.27f, 1f),
            new Color(0.98f, 0.82f, 0.27f, 1f),
            new Color(0.45f, 0.78f, 0.45f, 1f),
            new Color(0.35f, 0.66f, 0.95f, 1f),
            new Color(0.53f, 0.47f, 0.89f, 1f),
            new Color(0.85f, 0.44f, 0.75f, 1f)
        };

        private static readonly Color Ink = new Color(0.17f, 0.18f, 0.31f, 1f);
        private static readonly Color Cream = new Color(1f, 0.98f, 0.91f, 1f);

        private sealed class UiRefs
        {
            public Canvas canvas;
            public CanvasGroup musicalGroup;
            public CanvasGroup gameplayGroup;
            public RectTransform pianoFrame;
            public RectTransform characterRoot;
            public Image characterImage;
            public TMP_Text title;
            public TMP_Text status;
            public TMP_Text sequence;
            public TMP_Text errors;
            public GameObject startView;
            public GameObject gameplayPanel;
            public GameObject resultView;
            public Button startActivity;
            public Button listen;
            public Button topBack;
            public Button retryAttempt;
            public Button help;
            public Button replay;
            public Button nextWorld;
            public Button resultBack;
            public TMP_Text resultText;
            public readonly List<Button> keys = new();
            public readonly List<TMP_Text> keyLabels = new();
            public readonly List<Image> sequenceIndicators = new();
            public readonly List<TMP_Text> sequenceLabels = new();
            public readonly List<Image> resultStars = new();
            public UIStarDisplay resultStarDisplay;
            public CanvasGroup resultGroup;
            public StoryResultStarAnimation decorativeStars;
            public MusicalTutorialController tutorial;
        }

        [MenuItem("Mundo Aprendo/Reconstruir Mundo Musical")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Canvas canvas = FindRootCanvas(scene) ?? CreateRootCanvas(scene);
            ConfigureCanvas(canvas);

            MundoMusicalSequenceGame game = ComponentsInScene<MundoMusicalSequenceGame>(scene).FirstOrDefault();
            if (game == null)
            {
                GameObject gameObject = new GameObject("MundoMusicalSequenceGame");
                SceneManager.MoveGameObjectToScene(gameObject, scene);
                game = gameObject.AddComponent<MundoMusicalSequenceGame>();
            }

            AudioSource audioSource = game.GetComponent<AudioSource>();
            if (audioSource == null) audioSource = game.gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            Transform transitionOverlay = PreserveTransitionOverlay(scene, canvas.transform);
            RemoveLegacyMusicPresentation(scene, canvas.transform, game.transform, transitionOverlay);
            DisableLegacyDialogue(scene);

            UiRefs ui = BuildMusicUi(canvas, game);
            ConfigureTutorial(canvas, ui, game);
            ConfigureResultAnimation(ui);
            ConfigureLevelAnimation(ui);
            ConfigureGame(game, audioSource, ui);
            ConfigurePersistentListeners(game, ui);

            if (transitionOverlay != null) transitionOverlay.SetAsLastSibling();
            else if (ui.tutorial != null) ui.tutorial.transform.SetAsLastSibling();

            EnsureEventSystem(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("No se pudo guardar la reconstruccion de MundoMusical.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("MundoMusicalReconstructionSetup: UI musical, tutorial y referencias guardados.");
        }

        private static UiRefs BuildMusicUi(Canvas canvas, MundoMusicalSequenceGame game)
        {
            UiRefs ui = new UiRefs { canvas = canvas };
            RectTransform root = CreateUiObject("MusicalUI", canvas.transform).GetComponent<RectTransform>();
            Stretch(root);
            ui.musicalGroup = root.gameObject.AddComponent<CanvasGroup>();
            ui.musicalGroup.alpha = 1f;
            ui.musicalGroup.interactable = true;
            ui.musicalGroup.blocksRaycasts = true;

            Image background = CreateImage("FondoMundoMusical", root, LoadSprite(BackgroundPath), Color.white, false);
            Stretch(background.rectTransform);
            background.preserveAspect = false;

            CreateAmbientNote(root, new Vector2(-820f, 270f), 70f, 14f, 0.45f);
            CreateAmbientNote(root, new Vector2(800f, 230f), 88f, -11f, 0.44f);
            CreateAmbientNote(root, new Vector2(730f, -345f), 58f, 18f, 0.32f);

            BuildTopBar(root, ui);
            BuildCharacterArea(root, ui);
            BuildStartView(root, ui);
            BuildGameplay(root, ui);
            BuildResultView(root, ui);
            return ui;
        }

        private static void BuildTopBar(RectTransform root, UiRefs ui)
        {
            Image bar = CreatePanel("TopBar", root, new Vector2(0f, 465f), new Vector2(1800f, 104f), new Color(0.10f, 0.16f, 0.32f, 0.88f));
            ui.topBack = CreateButton("BotonVolver", "VOLVER", bar.rectTransform, new Vector2(-755f, 0f), new Vector2(205f, 62f), LoadSprite(BlueButtonPath), Cream);
            ui.title = CreateText("TituloMundoMusical", "MUNDO MUSICAL", bar.rectTransform, 42f, FontStyles.Bold, TextAlignmentOptions.Center, Cream);
            SetRect(ui.title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(610f, 70f));
            ui.sequence = CreateText("TextoSecuencia", "Secuencia: Inicio", bar.rectTransform, 25f, FontStyles.Bold, TextAlignmentOptions.Right, Cream);
            SetRect(ui.sequence.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(620f, 16f), new Vector2(420f, 44f));
            ui.errors = CreateText("TextoErrores", "Errores: 0", bar.rectTransform, 23f, FontStyles.Bold, TextAlignmentOptions.Right, new Color(1f, 0.88f, 0.70f, 1f));
            SetRect(ui.errors.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(620f, -24f), new Vector2(420f, 38f));
        }

        private static void BuildCharacterArea(RectTransform root, UiRefs ui)
        {
            ui.characterRoot = CreateUiObject("Tamborcin", root).GetComponent<RectTransform>();
            SetRect(ui.characterRoot, new Vector2(0.5f, 0.5f), new Vector2(-650f, 45f), new Vector2(400f, 505f));
            ui.characterImage = ui.characterRoot.gameObject.AddComponent<Image>();
            ui.characterImage.sprite = LoadSprite(TamborcinHelloPath);
            ui.characterImage.preserveAspect = true;
            ui.characterImage.raycastTarget = false;

            Image bubble = CreatePanel("DialogoTamborcin", root, new Vector2(-565f, -258f), new Vector2(525f, 126f), new Color(1f, 0.98f, 0.91f, 0.94f));
            ui.status = CreateText("TextoEstado", "Escucha con atencion y repite la melodia.", bubble.rectTransform, 25f, FontStyles.Bold, TextAlignmentOptions.Center, Ink);
            Stretch(ui.status.rectTransform, 22f, 17f);
        }

        private static void BuildStartView(RectTransform root, UiRefs ui)
        {
            Image panel = CreatePanel("StartView", root, new Vector2(245f, 18f), new Vector2(900f, 570f), new Color(0.11f, 0.20f, 0.39f, 0.93f));
            ui.startView = panel.gameObject;
            TMP_Text welcome = CreateText("TextoBienvenida", "Vamos a crear musica", panel.rectTransform, 48f, FontStyles.Bold, TextAlignmentOptions.Center, Cream);
            SetRect(welcome.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 155f), new Vector2(760f, 70f));
            TMP_Text instructions = CreateText("InstruccionesInicio", "Escucha la secuencia y toca las mismas teclas\nen el mismo orden.", panel.rectTransform, 30f, FontStyles.Normal, TextAlignmentOptions.Center, Cream);
            SetRect(instructions.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 42f), new Vector2(700f, 125f));
            ui.startActivity = CreateButton("BotonIniciarActividad", "INICIAR", panel.rectTransform, new Vector2(0f, -155f), new Vector2(310f, 78f), LoadSprite(GreenButtonPath), Cream);
        }

        private static void BuildGameplay(RectTransform root, UiRefs ui)
        {
            Image panel = CreatePanel("GameplayPanel", root, new Vector2(245f, -44f), new Vector2(1040f, 710f), new Color(0.08f, 0.15f, 0.31f, 0.92f));
            ui.gameplayPanel = panel.gameObject;
            ui.gameplayGroup = panel.gameObject.AddComponent<CanvasGroup>();

            TMP_Text heading = CreateText("EncabezadoJuego", "Escucha y repite la secuencia", panel.rectTransform, 36f, FontStyles.Bold, TextAlignmentOptions.Center, Cream);
            SetRect(heading.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 282f), new Vector2(850f, 60f));

            RectTransform progress = CreateUiObject("ProgresoSecuencia", panel.rectTransform).GetComponent<RectTransform>();
            SetRect(progress, new Vector2(0.5f, 0.5f), new Vector2(0f, 215f), new Vector2(760f, 64f));
            for (int index = 0; index < 8; index++)
            {
                Image indicator = CreateImage("Paso_" + (index + 1), progress, LoadSprite(NotePath), new Color(0.68f, 0.72f, 0.78f, 0.84f), false);
                SetRect(indicator.rectTransform, new Vector2(0.075f + index * 0.122f, 0.5f), Vector2.zero, new Vector2(48f, 48f));
                indicator.preserveAspect = true;
                ui.sequenceIndicators.Add(indicator);
                TMP_Text label = CreateText("Nota", string.Empty, indicator.rectTransform, 16f, FontStyles.Bold, TextAlignmentOptions.Center, Cream);
                Stretch(label.rectTransform);
                ui.sequenceLabels.Add(label);
            }

            // Keep a clean vertical rhythm: heading/progress, piano, then controls.
            // The lighter violet panel separates keys from the background without
            // competing with the child's colorful instrument.
            Image pianoBase = CreatePanel("PianoFrame", panel.rectTransform, new Vector2(0f, -28f), new Vector2(920f, 356f), new Color(0.20f, 0.16f, 0.31f, 0.94f));
            ui.pianoFrame = pianoBase.rectTransform;
            TMP_Text pianoCaption = CreateText("TituloPiano", "PIANO DE COLORES", pianoBase.rectTransform, 20f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.57f, 1f));
            SetRect(pianoCaption.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 133f), new Vector2(520f, 32f));

            // Only this container owns the layout position of each Button.  The
            // button itself stays fixed; feedback is applied to its KeyVisual child.
            RectTransform keyRow = CreateUiObject("KeysContainer", pianoBase.rectTransform).GetComponent<RectTransform>();
            SetRect(keyRow, new Vector2(0.5f, 0.5f), new Vector2(0f, -38f), new Vector2(820f, 228f));
            HorizontalLayoutGroup keyRowLayout = keyRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            keyRowLayout.padding = new RectOffset(28, 28, 0, 0);
            keyRowLayout.spacing = 14f;
            keyRowLayout.childAlignment = TextAnchor.MiddleCenter;
            keyRowLayout.childControlWidth = false;
            keyRowLayout.childControlHeight = false;
            keyRowLayout.childForceExpandWidth = false;
            keyRowLayout.childForceExpandHeight = false;

            for (int index = 0; index < NoteNames.Length; index++)
            {
                Button key = CreateButton("Key_" + (index + 1), string.Empty, keyRow, Vector2.zero, new Vector2(96f, 220f), null, Cream);
                // The root is only the stable hit/layout target.  Its visible child
                // is intentionally separate so visual feedback cannot move the row.
                key.image.sprite = BuiltinSprite();
                key.image.type = Image.Type.Sliced;
                key.image.color = new Color(1f, 1f, 1f, 0f);
                key.image.raycastTarget = true;
                key.transition = Selectable.Transition.None;

                // MusicalKeyVisual provides the complete key-specific feedback;
                // no second component may animate this layout-controlled Button.
                UIButtonFeedback genericFeedback = key.GetComponent<UIButtonFeedback>();
                if (genericFeedback != null) UnityEngine.Object.DestroyImmediate(genericFeedback);

                LayoutElement keyLayout = key.gameObject.AddComponent<LayoutElement>();
                keyLayout.minWidth = 96f;
                keyLayout.preferredWidth = 96f;
                keyLayout.flexibleWidth = 0f;
                keyLayout.minHeight = 220f;
                keyLayout.preferredHeight = 220f;
                keyLayout.flexibleHeight = 0f;

                Image keyVisualImage = CreateImage("KeyVisual", key.transform, BuiltinSprite(), KeyColors[index], true);
                keyVisualImage.type = Image.Type.Sliced;
                Stretch(keyVisualImage.rectTransform, 3f, 3f);
                key.targetGraphic = keyVisualImage;

                Image glow = CreateImage("Glow", keyVisualImage.rectTransform, BuiltinSprite(), new Color(1f, 0.98f, 0.68f, 0f), false);
                Stretch(glow.rectTransform, 7f, 7f);
                glow.type = Image.Type.Sliced;

                TMP_Text label = CreateText("NumberText", KeyLabels[index], key.transform, 42f, FontStyles.Bold, TextAlignmentOptions.Center, Cream);
                Stretch(label.rectTransform, 10f, 10f);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Overflow;
                label.enableAutoSizing = false;
                label.margin = new Vector4(8f, 8f, 8f, 8f);

                MusicalKeyVisual visual = key.gameObject.AddComponent<MusicalKeyVisual>();
                ConfigureKeyVisual(visual, key, keyVisualImage.rectTransform, keyVisualImage, glow);
                ui.keys.Add(key);
                ui.keyLabels.Add(label);
            }

            ui.listen = CreateButton("BotonEscuchar", "ESCUCHAR", panel.rectTransform, new Vector2(-250f, -314f), new Vector2(230f, 68f), LoadSprite(GreenButtonPath), Cream);
            ui.retryAttempt = CreateButton("BotonReintentar", "REINTENTAR", panel.rectTransform, new Vector2(0f, -314f), new Vector2(230f, 68f), LoadSprite(PurpleButtonPath), Cream);
            ui.help = CreateButton("BotonAyuda", "AYUDA", panel.rectTransform, new Vector2(250f, -314f), new Vector2(230f, 68f), LoadSprite(BlueButtonPath), Cream);
        }

        private static void BuildResultView(RectTransform root, UiRefs ui)
        {
            Image panel = CreatePanel("ResultView", root, new Vector2(245f, -5f), new Vector2(920f, 650f), new Color(0.10f, 0.18f, 0.36f, 0.97f));
            ui.resultView = panel.gameObject;
            ui.resultGroup = panel.gameObject.AddComponent<CanvasGroup>();

            TMP_Text title = CreateText("TituloResultado", "Excelente musica!", panel.rectTransform, 47f, FontStyles.Bold, TextAlignmentOptions.Center, Cream);
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 224f), new Vector2(700f, 68f));
            ui.resultText = CreateText("TextoResultado", "Actividad completada", panel.rectTransform, 28f, FontStyles.Normal, TextAlignmentOptions.Center, Cream);
            SetRect(ui.resultText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 158f), new Vector2(680f, 80f));

            RectTransform starsRoot = CreateUiObject("EstrellasResultado", panel.rectTransform).GetComponent<RectTransform>();
            SetRect(starsRoot, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(390f, 125f));
            Sprite fullStar = LoadSprite(StarPath);
            Sprite emptyStar = LoadSprite(StarOffPath);
            for (int index = 0; index < 3; index++)
            {
                Image star = CreateImage("EstrellaResultado_" + (index + 1), starsRoot, emptyStar, Color.white, false);
                SetRect(star.rectTransform, new Vector2(0.18f + index * 0.32f, 0.5f), Vector2.zero, new Vector2(112f, 112f));
                star.preserveAspect = true;
                ui.resultStars.Add(star);
            }
            ui.resultStarDisplay = starsRoot.gameObject.AddComponent<UIStarDisplay>();
            ConfigureStarDisplay(ui.resultStarDisplay, ui.resultStars, fullStar, emptyStar);

            RectTransform loopRoot = CreateUiObject("DecoracionEstrellas", panel.rectTransform).GetComponent<RectTransform>();
            Stretch(loopRoot);
            CanvasGroup loopGroup = loopRoot.gameObject.AddComponent<CanvasGroup>();
            Image main = CreateImage("EstrellaDecorativaCentral", loopRoot, fullStar, new Color(1f, 1f, 1f, 0f), false);
            SetRect(main.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 42f), new Vector2(178f, 178f));
            main.preserveAspect = true;
            List<RectTransform> floatingStars = new();
            List<Image> floatingImages = new();
            Vector2[] positions = { new(-180f, 36f), new(190f, 22f), new(-105f, -2f), new(125f, -42f), new(0f, 118f) };
            for (int index = 0; index < positions.Length; index++)
            {
                Image floating = CreateImage("EstrellaFlotante_" + (index + 1), loopRoot, fullStar, new Color(1f, 1f, 1f, 0f), false);
                SetRect(floating.rectTransform, new Vector2(0.5f, 0.5f), positions[index], new Vector2(58f, 58f));
                floating.preserveAspect = true;
                floatingStars.Add(floating.rectTransform);
                floatingImages.Add(floating);
            }
            ui.decorativeStars = loopRoot.gameObject.AddComponent<StoryResultStarAnimation>();
            ConfigureDecorativeStars(ui.decorativeStars, loopGroup, main, floatingStars, floatingImages);

            ui.replay = CreateButton("BotonRepetirActividad", "REPETIR", panel.rectTransform, new Vector2(-260f, -222f), new Vector2(230f, 68f), LoadSprite(PurpleButtonPath), Cream);
            ui.nextWorld = CreateButton("BotonSiguienteMundo", "SIGUIENTE", panel.rectTransform, new Vector2(0f, -222f), new Vector2(230f, 68f), LoadSprite(GreenButtonPath), Cream);
            ui.resultBack = CreateButton("BotonVolverResultado", "VOLVER", panel.rectTransform, new Vector2(260f, -222f), new Vector2(210f, 68f), LoadSprite(BlueButtonPath), Cream);
            ui.resultView.SetActive(false);
        }

        private static void ConfigureTutorial(Canvas canvas, UiRefs ui, MundoMusicalSequenceGame game)
        {
            RectTransform overlay = CreateUiObject("TutorialOverlay", canvas.transform).GetComponent<RectTransform>();
            Stretch(overlay);
            Image blocker = overlay.gameObject.AddComponent<Image>();
            blocker.sprite = BuiltinSprite();
            blocker.type = Image.Type.Sliced;
            blocker.color = new Color(0.035f, 0.06f, 0.16f, 0.88f);
            blocker.raycastTarget = true;
            CanvasGroup overlayGroup = overlay.gameObject.AddComponent<CanvasGroup>();
            ui.tutorial = overlay.gameObject.AddComponent<MusicalTutorialController>();

            Image tutorialCharacter = CreateImage("TamborcinTutorial", overlay, LoadSprite(TamborcinHelloPath), Color.white, false);
            SetRect(tutorialCharacter.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-520f, -22f), new Vector2(470f, 610f));
            tutorialCharacter.preserveAspect = true;
            Image dialogue = CreatePanel("DialogoTutorial", overlay, new Vector2(245f, 20f), new Vector2(930f, 510f), new Color(1f, 0.98f, 0.91f, 0.98f));
            TMP_Text step = CreateText("ProgresoTutorial", "1/5", dialogue.rectTransform, 25f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.27f, 0.32f, 0.58f, 1f));
            SetRect(step.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 186f), new Vector2(180f, 42f));
            TMP_Text tutorialText = CreateText("TextoTutorial", string.Empty, dialogue.rectTransform, 37f, FontStyles.Bold, TextAlignmentOptions.Center, Ink);
            SetRect(tutorialText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 55f), new Vector2(760f, 210f));
            Button next = CreateButton("BotonSiguienteTutorial", "SIGUIENTE", dialogue.rectTransform, new Vector2(0f, -168f), new Vector2(280f, 72f), LoadSprite(BlueButtonPath), Cream);
            Button start = CreateButton("BotonComenzarTutorial", "COMENZAR", dialogue.rectTransform, new Vector2(0f, -168f), new Vector2(300f, 72f), LoadSprite(GreenButtonPath), Cream);
            ClearAllListeners(next.onClick);
            ClearAllListeners(start.onClick);

            Sprite hello = LoadSprite(TamborcinHelloPath);
            Sprite listening = LoadSprite(TamborcinListeningPath) ?? hello;
            Sprite celebrate = LoadSprite(TamborcinCelebratePath) ?? hello;
            string[] texts =
            {
                "\u00a1Hola! Soy Tamborcin. Vamos a tocar una melod\u00eda.",
                "Primero escucha y observa las teclas que se iluminan.",
                "Despu\u00e9s, toca las mismas teclas en el mismo orden.",
                "Escucha con atenci\u00f3n y recuerda la secuencia.",
                "\u00a1Listo! Presiona Comenzar para tocar."
            };
            Sprite[] poses = { hello, listening, hello, listening, celebrate };
            SerializedObject tutorialSo = new SerializedObject(ui.tutorial);
            SetReference(tutorialSo, "overlayGroup", overlayGroup);
            SetReference(tutorialSo, "gameplayGroup", ui.musicalGroup);
            SetReference(tutorialSo, "characterRoot", tutorialCharacter.rectTransform);
            SetReference(tutorialSo, "characterImage", tutorialCharacter);
            SetReference(tutorialSo, "tutorialText", tutorialText);
            SetReference(tutorialSo, "stepCounterText", step);
            SetReference(tutorialSo, "nextButton", next);
            SetReference(tutorialSo, "startButton", start);
            SerializedProperty steps = tutorialSo.FindProperty("tutorialSteps");
            steps.arraySize = texts.Length;
            for (int index = 0; index < texts.Length; index++)
            {
                SerializedProperty item = steps.GetArrayElementAtIndex(index);
                item.FindPropertyRelative("text").stringValue = texts[index];
                item.FindPropertyRelative("characterPose").objectReferenceValue = poses[index];
            }
            SetFloat(tutorialSo, "fadeDuration", 0.24f);
            tutorialSo.ApplyModifiedPropertiesWithoutUndo();
            overlay.SetAsLastSibling();
        }

        private static void ConfigureResultAnimation(UiRefs ui)
        {
            if (ui.decorativeStars == null) return;
            // UIStarDisplay reveals three real stars first (about 1.3 seconds), then
            // this purely decorative loop starts.  They are not competing systems.
            SerializedObject so = new SerializedObject(ui.decorativeStars);
            SetFloat(so, "startDelay", 1.48f);
            SetFloat(so, "loopDuration", 2.3f);
            SetFloat(so, "travelDuration", 1.18f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureLevelAnimation(UiRefs ui)
        {
            MusicalLevelAnimationController controller = ui.musicalGroup.gameObject.AddComponent<MusicalLevelAnimationController>();
            Sprite idle = LoadSprite(TamborcinHelloPath);
            Sprite listening = LoadSprite(TamborcinListeningPath) ?? idle;
            Sprite celebrate = LoadSprite(TamborcinCelebratePath) ?? idle;
            SerializedObject so = new SerializedObject(controller);
            SetReference(so, "gameplayGroup", ui.gameplayGroup);
            SetReference(so, "pianoFrame", ui.pianoFrame);
            SetReference(so, "characterRoot", ui.characterRoot);
            SetReference(so, "characterImage", ui.characterImage);
            SetReference(so, "idlePose", idle);
            SetReference(so, "listeningPose", listening);
            SetReference(so, "encouragementPose", listening);
            SetReference(so, "celebrationPose", celebrate);
            SetReference(so, "resultGroup", ui.resultGroup);
            SetReference(so, "resultStarAnimation", ui.decorativeStars);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureGame(MundoMusicalSequenceGame game, AudioSource audioSource, UiRefs ui)
        {
            Sprite fullStar = LoadSprite(StarPath);
            Sprite emptyStar = LoadSprite(StarOffPath);
            MusicalLevelAnimationController level = ui.musicalGroup.GetComponent<MusicalLevelAnimationController>();
            SerializedObject so = new SerializedObject(game);
            SetBool(so, "playSequenceOnStart", false);
            SetReference(so, "rootCanvas", ui.canvas);
            SetReference(so, "tmpTitleText", ui.title);
            SetReference(so, "tmpStatusText", ui.status);
            SetReference(so, "tmpSequenceText", ui.sequence);
            SetReference(so, "startButton", ui.listen);
            SetReference(so, "nextButton", null);
            SetObjectArray(so.FindProperty("keyButtons"), ui.keys.Cast<UnityEngine.Object>().ToArray());
            SetObjectArray(so.FindProperty("tmpKeyLabels"), ui.keyLabels.Cast<UnityEngine.Object>().ToArray());
            SetReference(so, "audioSource", audioSource);
            SetReference(so, "startPanel", ui.startView);
            SetReference(so, "pianoPanel", ui.gameplayPanel);
            SetReference(so, "resultPanel", ui.resultView);
            SetReference(so, "startActivityButton", ui.startActivity);
            SetReference(so, "returnButton", ui.topBack);
            SetReference(so, "tmpResultText", ui.resultText);
            SetObjectArray(so.FindProperty("starImages"), ui.resultStars.Cast<UnityEngine.Object>().ToArray());
            SetReference(so, "fullStarSprite", fullStar);
            SetReference(so, "emptyStarSprite", emptyStar);
            SetReference(so, "starDisplay", ui.resultStarDisplay);
            SetObjectArray(so.FindProperty("sequenceStepIndicators"), ui.sequenceIndicators.Cast<UnityEngine.Object>().ToArray());
            SetObjectArray(so.FindProperty("sequenceStepLabels"), ui.sequenceLabels.Cast<UnityEngine.Object>().ToArray());
            SetReference(so, "tmpErrorsText", ui.errors);
            SetReference(so, "tutorialController", ui.tutorial);
            SetReference(so, "levelAnimationController", level);
            SetString(so, "nextWorldSceneName", "MundoCuentos_VozTest");
            SetFloat(so, "delayBeforeNextSequence", 0.85f);
            UpdatePianoLabels(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurePersistentListeners(MundoMusicalSequenceGame game, UiRefs ui)
        {
            // These are re-bound at runtime by MundoMusicalSequenceGame.  Clearing
            // persistent calls guarantees exactly one action for every game control.
            ClearAllListeners(ui.startActivity.onClick);
            ClearAllListeners(ui.listen.onClick);
            ClearAllListeners(ui.topBack.onClick);
            foreach (Button key in ui.keys) ClearAllListeners(key.onClick);

            ClearAllListeners(ui.retryAttempt.onClick);
            UnityEventTools.AddPersistentListener(ui.retryAttempt.onClick, game.RestartCurrentAttempt);
            ClearAllListeners(ui.help.onClick);
            UnityEventTools.AddPersistentListener(ui.help.onClick, game.PlayCurrentSequence);
            ClearAllListeners(ui.replay.onClick);
            UnityEventTools.AddPersistentListener(ui.replay.onClick, game.ReplayActivity);
            ClearAllListeners(ui.nextWorld.onClick);
            UnityEventTools.AddPersistentListener(ui.nextWorld.onClick, game.OpenNextWorld);
            ClearAllListeners(ui.resultBack.onClick);
            UnityEventTools.AddPersistentListener(ui.resultBack.onClick, game.ReturnToWorldSelection);
        }

        private static void UpdatePianoLabels(SerializedObject gameSo)
        {
            SerializedProperty pianoKeys = gameSo.FindProperty("pianoKeys");
            if (pianoKeys == null) return;
            for (int index = 0; index < Math.Min(NoteNames.Length, pianoKeys.arraySize); index++)
            {
                SerializedProperty key = pianoKeys.GetArrayElementAtIndex(index);
                SerializedProperty id = key.FindPropertyRelative("id");
                SerializedProperty label = key.FindPropertyRelative("displayName");
                if (id != null) id.stringValue = NoteNames[index];
                if (label != null) label.stringValue = KeyLabels[index];
            }
        }

        private static void ConfigureKeyVisual(MusicalKeyVisual visual, Button key, RectTransform visualRoot, Image visualImage, Image glow)
        {
            SerializedObject so = new SerializedObject(visual);
            SetReference(so, "keyButton", key);
            SetReference(so, "visualRoot", visualRoot);
            SetReference(so, "visualImage", visualImage);
            SetReference(so, "glowImage", glow);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureStarDisplay(UIStarDisplay display, IReadOnlyList<Image> stars, Sprite full, Sprite empty)
        {
            SerializedObject so = new SerializedObject(display);
            SetObjectArray(so.FindProperty("stars"), stars.Cast<UnityEngine.Object>().ToArray());
            SetReference(so, "earnedSprite", full);
            SetReference(so, "unearnedSprite", empty);
            SetFloat(so, "delayBetweenStars", 0.16f);
            SetFloat(so, "revealDuration", 0.28f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureDecorativeStars(
            StoryResultStarAnimation animation,
            CanvasGroup group,
            Image main,
            IReadOnlyList<RectTransform> floatingStars,
            IReadOnlyList<Image> floatingImages)
        {
            SerializedObject so = new SerializedObject(animation);
            SetReference(so, "loopGroup", group);
            SetReference(so, "mainStar", main.rectTransform);
            SetReference(so, "mainStarImage", main);
            SetObjectArray(so.FindProperty("floatingStars"), floatingStars.Cast<UnityEngine.Object>().ToArray());
            SetObjectArray(so.FindProperty("floatingStarImages"), floatingImages.Cast<UnityEngine.Object>().ToArray());
            SerializedProperty offsets = so.FindProperty("travelOffsets");
            Vector2[] values = { new(-90f, 138f), new(80f, 150f), new(-42f, 160f), new(118f, 112f), new(-120f, 108f) };
            offsets.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++) offsets.GetArrayElementAtIndex(index).vector2Value = values[index];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform PreserveTransitionOverlay(Scene scene, Transform canvas)
        {
            Transform overlay = ComponentsInScene<Transform>(scene).FirstOrDefault(item => item.name == "SceneTransitionOverlay");
            if (overlay == null) return null;
            if (overlay.parent != canvas) overlay.SetParent(canvas, false);
            return overlay;
        }

        private static void RemoveLegacyMusicPresentation(Scene scene, Transform canvas, Transform gameTransform, Transform transitionOverlay)
        {
            // Direct Canvas children are the old visual layer.  Preserve the controller
            // (if it happens to live there) and the fade overlay only.
            List<Transform> children = new List<Transform>();
            foreach (Transform child in canvas) children.Add(child);
            foreach (Transform child in children)
            {
                if (child == null || child == transitionOverlay || child == gameTransform || gameTransform.IsChildOf(child)) continue;
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }

            string[] legacyRoots = { "TutorialOverlay", "DialogoPersonajeCanvas", "DialogoTamborcin", "PanelJuegoMusicalLegacy" };
            foreach (string name in legacyRoots)
            {
                foreach (Transform target in ComponentsInScene<Transform>(scene).Where(item => item.name == name).ToArray())
                {
                    if (target == transitionOverlay || target == gameTransform || target.IsChildOf(gameTransform)) continue;
                    UnityEngine.Object.DestroyImmediate(target.gameObject);
                }
            }
        }

        private static void DisableLegacyDialogue(Scene scene)
        {
            foreach (GameplayDialogueFeedbackBridge bridge in ComponentsInScene<GameplayDialogueFeedbackBridge>(scene))
            {
                UnityEngine.Object.DestroyImmediate(bridge);
            }

            foreach (CharacterDialogueController dialogue in ComponentsInScene<CharacterDialogueController>(scene))
            {
                UnityEngine.Object.DestroyImmediate(dialogue);
            }
        }

        private static Canvas FindRootCanvas(Scene scene)
        {
            return ComponentsInScene<Canvas>(scene).FirstOrDefault(item => item.isRootCanvas);
        }

        private static Canvas CreateRootCanvas(Scene scene)
        {
            GameObject root = new GameObject("Canvas - Mundo Musical", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(root, scene);
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            return canvas;
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

        private static void EnsureEventSystem(Scene scene)
        {
            EventSystem[] systems = ComponentsInScene<EventSystem>(scene);
            if (systems.Length > 0) return;
            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            SceneManager.MoveGameObjectToScene(eventSystem, scene);
        }

        private static void CreateAmbientNote(RectTransform parent, Vector2 position, float size, float angle, float alpha)
        {
            Image note = CreateImage("NotaDecorativa", parent, LoadSprite(NotePath), new Color(1f, 0.96f, 0.62f, alpha), false);
            SetRect(note.rectTransform, new Vector2(0.5f, 0.5f), position, new Vector2(size, size));
            note.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            note.preserveAspect = true;
        }

        private static Image CreatePanel(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            Image panel = CreateImage(name, parent, LoadSprite(PanelPath), color, false);
            SetRect(panel.rectTransform, new Vector2(0.5f, 0.5f), position, size);
            panel.type = Image.Type.Sliced;
            return panel;
        }

        private static Button CreateButton(string name, string label, Transform parent, Vector2 position, Vector2 size, Sprite sprite, Color labelColor)
        {
            GameObject buttonObject = CreateUiObject(name, parent);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0.5f), position, size);
            Image image = buttonObject.AddComponent<Image>();
            image.sprite = sprite ?? BuiltinSprite();
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            buttonObject.AddComponent<UIButtonFeedback>();
            if (!string.IsNullOrWhiteSpace(label))
            {
                TMP_Text text = CreateText("Texto", label, rect, 23f, FontStyles.Bold, TextAlignmentOptions.Center, labelColor);
                Stretch(text.rectTransform);
            }
            return button;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool raycastTarget)
        {
            GameObject imageObject = CreateUiObject(name, parent);
            Image image = imageObject.AddComponent<Image>();
            image.sprite = sprite ?? BuiltinSprite();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        private static TMP_Text CreateText(string name, string value, Transform parent, float size, FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            GameObject textObject = CreateUiObject(name, parent);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rect, float horizontalInset = 0f, float verticalInset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontalInset, verticalInset);
            rect.offsetMax = new Vector2(-horizontalInset, -verticalInset);
            rect.localScale = Vector3.one;
        }

        private static void ClearAllListeners(UnityEventBase unityEvent)
        {
            if (unityEvent == null) return;
            unityEvent.RemoveAllListeners();
            for (int index = unityEvent.GetPersistentEventCount() - 1; index >= 0; index--)
            {
                UnityEventTools.RemovePersistentListener(unityEvent, index);
            }
        }

        private static void SetObjectArray(SerializedProperty property, IReadOnlyList<UnityEngine.Object> values)
        {
            if (property == null) return;
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index++) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }

        private static void SetReference(SerializedObject so, string name, UnityEngine.Object value)
        {
            SerializedProperty property = so.FindProperty(name);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void SetBool(SerializedObject so, string name, bool value)
        {
            SerializedProperty property = so.FindProperty(name);
            if (property != null) property.boolValue = value;
        }

        private static void SetFloat(SerializedObject so, string name, float value)
        {
            SerializedProperty property = so.FindProperty(name);
            if (property != null) property.floatValue = value;
        }

        private static void SetString(SerializedObject so, string name, string value)
        {
            SerializedProperty property = so.FindProperty(name);
            if (property != null) property.stringValue = value;
        }

        private static Sprite LoadSprite(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite BuiltinSprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        }

        private static T[] ComponentsInScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        }
    }
}
#endif
