#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bolin;
using Jsgaona;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bolin.Editor
{
    /// <summary>
    /// Rebuilds the persisted presentation for MundoTamanos without changing the
    /// comparison, scoring, PlayerPrefs, or navigation logic in SizeWorldController.
    /// Habitat backgrounds and comparison logic stay untouched; the editor utility
    /// only assigns the fourteen approved animal sprites to the existing data model.
    /// </summary>
    public static class MundoTamanosSafariReconstructionSetup
    {
        private const string ScenePath = "Assets/Scenes/MundoTamanos.unity";
        private const string SafariPath = "Assets/Mundo Aprendo/Fondos/Mundo Safari/Safari.png";
        // Compatibilidad interna para las rutas que usan el constructor de resultado/tutorial.
        // Todas apuntan al kit PNG propio, no a botones preparados del proyecto.
        private const string PurpleButtonPath = "Assets/MundoAprendo/UI/Generated/MundoTamanos/AnswerButtonPurple.png";
        private const string OrangeButtonPath = "Assets/MundoAprendo/UI/Generated/MundoTamanos/AnswerButtonGreen.png";
        private const string FullStarPath = "Assets/Hyper_Casual_UI/Sprites/Usar/rataing star.png";
        private const string EmptyStarPath = "Assets/Hyper_Casual_UI/Sprites/Usar/rataing star (1).png";
        private const string GuideIdlePath = "Assets/Mundo Aprendo/Personajes/_Poses/Explorador_saludo.png";
        private const string GuideRetryPath = "Assets/Mundo Aprendo/Personajes/_Poses/Explorador_animo.png";
        private const string GuideCelebratePath = "Assets/Mundo Aprendo/Personajes/_Poses/Explorador_alegre.png";

        private static readonly Color Ink = new(0.18f, 0.13f, 0.19f, 1f);
        private static readonly Color Cream = new(1f, 0.97f, 0.84f, 0.98f);
        private static readonly Color Purple = new(0.43f, 0.23f, 0.72f, 1f);
        private static readonly Color Teal = new(0.12f, 0.68f, 0.64f, 1f);
        private static readonly Color SoftOverlay = new(0.05f, 0.15f, 0.09f, 0.18f);

        private sealed class UiReferences
        {
            public Canvas canvas;
            public CanvasGroup gameplayGroup;
            public Image background;
            public RectTransform title;
            public RectTransform instruction;
            public RectTransform leftCard;
            public RectTransform rightCard;
            public Outline leftOutline;
            public Outline rightOutline;
            public RectTransform animalSafeArea;
            public Image leftAnimal;
            public Image rightAnimal;
            public Button leftButton;
            public Button rightButton;
            public Button returnButton;
            public TMP_Text question;
            public TMP_Text feedback;
            public TMP_Text leftName;
            public TMP_Text rightName;
            public GameObject resultPanel;
            public TMP_Text resultText;
            public Button retryButton;
            public Button resultBackButton;
            public readonly List<Image> topStars = new();
            public readonly List<Image> bottomStars = new();
            public readonly List<Image> resultStars = new();
            public UIStarDisplay topStarDisplay;
            public UIStarDisplay resultStarDisplay;
            public TMP_Text leftAnswerLabel;
            public TMP_Text rightAnswerLabel;
            public Image leftAnswerIcon;
            public Image rightAnswerIcon;
        }

        [MenuItem("Mundo Aprendo/Reconstruir Mundo de los Tamaños con Safari")]
        public static void Apply()
        {
            // Los catorce dibujos definitivos se renombran e importan desde el Editor
            // antes de enlazarlos a los datos ya existentes de los habitats.
            MundoTamanosAnimalAssetSetup.PrepareFinalAnimalAssets();
            // Los marcos, tarjetas y botones se generan como PNG editables antes de
            // reconstruir la escena. El juego nunca crea estas texturas en runtime.
            MundoTamanosGeneratedUiAssets.EnsureAssets();
            Sprite safari = RequireSprite(SafariPath);
            Sprite fullStar = RequireSprite(FullStarPath);
            Sprite emptyStar = RequireSprite(EmptyStarPath);

            CreateSceneBackup();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SizeWorldController controller = ComponentsInScene<SizeWorldController>(scene).FirstOrDefault();
            if (controller == null)
            {
                throw new InvalidOperationException("MundoTamanos no contiene SizeWorldController; la reconstrucción se canceló para no alterar mecánicas.");
            }

            ClearOnlySizePresentation(scene, controller);
            EnsureEventSystem(scene);

            Canvas canvas = CreateRootCanvas(scene);
            ConfigureCanvas(canvas);
            UiReferences ui = BuildPersistedPresentation(canvas, safari, fullStar, emptyStar);
            ConfigureSizeController(controller, ui, fullStar, emptyStar);
            MundoTamanosAnimalAssetSetup.ConfigureAnimalData(controller);
            ConfigureTutorial(canvas.transform, controller);
            ConfigurePresentationController(canvas.transform, controller, ui);
            CreateSceneTransition(canvas.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("No se pudo guardar la reconstrucción persistente de MundoTamanos.");
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("MundoTamanosSafariReconstructionSetup: UI terminada, tutorial aislado y catorce animales definitivos enlazados.");
        }

        private static void CreateSceneBackup()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string source = Path.Combine(projectRoot, ScenePath.Replace('/', Path.DirectorySeparatorChar));
            string backupDirectory = Path.Combine(projectRoot, "Backups");
            Directory.CreateDirectory(backupDirectory);
            string destination = Path.Combine(backupDirectory, $"MundoTamanos_before_safari_ui_{DateTime.Now:yyyyMMdd_HHmmss}.unity");
            File.Copy(source, destination, false);
            Debug.Log($"MundoTamanosSafariReconstructionSetup: respaldo creado en {destination}");
        }

        private static void ClearOnlySizePresentation(Scene scene, SizeWorldController controller)
        {
            // The controller, AudioManager, camera, and EventSystem remain untouched.
            // Only old Canvas roots are presentation and are intentionally replaced.
            foreach (Canvas canvas in ComponentsInScene<Canvas>(scene).Where(item => item.isRootCanvas).ToArray())
            {
                if (controller.transform.IsChildOf(canvas.transform))
                {
                    controller.transform.SetParent(null, true);
                }

                UnityEngine.Object.DestroyImmediate(canvas.gameObject);
            }

            foreach (SizeWorldPresentationController presentation in ComponentsInScene<SizeWorldPresentationController>(scene).ToArray())
            {
                UnityEngine.Object.DestroyImmediate(presentation);
            }

            string[] orphanedPresentationRoots = { "TutorialOverlay", "SceneTransitionOverlay", "Panel-resultado", "GameplayPresentation" };
            foreach (string name in orphanedPresentationRoots)
            {
                foreach (Transform item in ComponentsInScene<Transform>(scene)
                    .Where(item => item != null && item.name == name)
                    .ToArray())
                {
                    if (item.gameObject != controller.gameObject)
                    {
                        UnityEngine.Object.DestroyImmediate(item.gameObject);
                    }
                }
            }
        }

        private static Canvas CreateRootCanvas(Scene scene)
        {
            GameObject root = new GameObject("Canvas_MundoTamanos", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(root, scene);
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            return canvas;
        }

        private static void ConfigureCanvas(Canvas canvas)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            CanvasColorBlindAccessibility accessibility = GetOrAdd<CanvasColorBlindAccessibility>(canvas.gameObject);
            SerializedObject accessibilitySo = new(accessibility);
            SetReference(accessibilitySo, "targetCanvas", canvas);
            accessibilitySo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static UiReferences BuildPersistedPresentation(Canvas canvas, Sprite safari, Sprite fullStar, Sprite emptyStar)
        {
            UiReferences ui = new() { canvas = canvas };
            RectTransform gameplay = CreateRect("GameplayPresentation", canvas.transform);
            Stretch(gameplay);
            ui.gameplayGroup = gameplay.gameObject.AddComponent<CanvasGroup>();
            ui.gameplayGroup.alpha = 1f;
            ui.gameplayGroup.interactable = true;
            ui.gameplayGroup.blocksRaycasts = true;

            ui.background = CreateImage("Fondo-habitat", gameplay, safari, Color.white, false);
            Stretch(ui.background.rectTransform);
            ui.background.preserveAspect = false;

            Image overlay = CreateImage("HabitatDesignOverlay", gameplay, BuiltinSprite(), SoftOverlay, false);
            Stretch(overlay.rectTransform);

            RectTransform header = CreateRect("HeaderArea", gameplay);
            Stretch(header);
            RectTransform topLeftArea = CreateRect("TopLeftArea", header);
            Stretch(topLeftArea);
            ui.returnButton = CreateBackTextButton(
                "Boton-volver",
                topLeftArea,
                new Vector2(-760f, 450f),
                new Vector2(250f, 82f),
                RequireSprite(MundoTamanosGeneratedUiAssets.AnswerPurplePath),
                RequireSprite(MundoTamanosGeneratedUiAssets.BackArrowPath));

            Image titleShadow = CreateImage("SombraTitulo", header, RequireSprite(MundoTamanosGeneratedUiAssets.SoftShadowPath), Color.white, false);
            titleShadow.type = Image.Type.Sliced;
            SetRect(titleShadow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(170f, 435f), new Vector2(742f, 126f));
            Image titlePlate = CreatePanel("TituloMundo", header, new Vector2(170f, 451f), new Vector2(730f, 126f), Color.white, RequireSprite(MundoTamanosGeneratedUiAssets.TitleBannerPath));
            ui.title = titlePlate.rectTransform;
            TMP_Text titleText = CreateText("Titulo", "MUNDO DE LOS TAMAÑOS", titlePlate.transform, 47f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            Stretch(titleText.rectTransform, 35f, 14f);

            RectTransform topStars = CreateRect("TopStars", topLeftArea);
            SetRect(topStars, new Vector2(0.5f, 0.5f), new Vector2(-760f, 340f), new Vector2(238f, 90f));
            Image starsPlate = CreateImage("MarcoEstrellas", topStars, RequireSprite(MundoTamanosGeneratedUiAssets.StarPanelPath), Color.white, false);
            starsPlate.type = Image.Type.Sliced;
            Stretch(starsPlate.rectTransform, 0f, 0f);
            ui.topStarDisplay = CreateStarDisplay(topStars, "EstrellaSuperior", fullStar, emptyStar, ui.topStars);

            Image instructionShadow = CreateImage("SombraInstruccion", header, RequireSprite(MundoTamanosGeneratedUiAssets.SoftShadowPath), Color.white, false);
            instructionShadow.type = Image.Type.Sliced;
            SetRect(instructionShadow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(170f, 309f), new Vector2(1030f, 112f));
            Image instructionPlate = CreatePanel("PanelInstruccion", header, new Vector2(170f, 324f), new Vector2(1010f, 106f), Color.white, RequireSprite(MundoTamanosGeneratedUiAssets.InstructionPanelPath));
            TMP_Text instruction = CreateText(
                "Pregunta",
                "Observa bien y elige el animal más grande",
                instructionPlate.transform,
                33f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                Ink);
            instruction.textWrappingMode = TextWrappingModes.Normal;
            Stretch(instruction.rectTransform, 30f, 12f);
            ui.instruction = instructionPlate.rectTransform;
            ui.question = instruction;

            ui.animalSafeArea = CreateRect("Area-animales-segura", gameplay);
            SetRect(ui.animalSafeArea, new Vector2(0.5f, 0.5f), new Vector2(170f, 2f), new Vector2(1320f, 438f));

            CardReferences leftCard = CreateAnimalCard(ui.animalSafeArea, "AnimalStageLeft", "LeftAnimalName", "OPCIÓN IZQUIERDA", new Vector2(-336f, 0f), Teal, fullStar);
            CardReferences rightCard = CreateAnimalCard(ui.animalSafeArea, "AnimalStageRight", "RightAnimalName", "OPCIÓN DERECHA", new Vector2(336f, 0f), Purple, fullStar);
            ui.leftCard = leftCard.root;
            ui.rightCard = rightCard.root;
            ui.leftOutline = leftCard.outline;
            ui.rightOutline = rightCard.outline;
            ui.leftName = leftCard.nameText;
            ui.rightName = rightCard.nameText;

            ui.leftAnimal = CreateImage("Animal-izquierdo", ui.animalSafeArea, null, Color.white, false);
            SetRect(ui.leftAnimal.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-336f, -8f), new Vector2(250f, 250f));
            ui.leftAnimal.preserveAspect = true;
            ui.leftAnimal.enabled = false;
            ui.rightAnimal = CreateImage("Animal-derecho", ui.animalSafeArea, null, Color.white, false);
            SetRect(ui.rightAnimal.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(336f, -8f), new Vector2(250f, 250f));
            ui.rightAnimal.preserveAspect = true;
            ui.rightAnimal.enabled = false;

            RectTransform answerButtons = CreateRect("AnswerButtons", gameplay);
            Stretch(answerButtons);
            AnswerVisualReferences leftAnswer = CreateChoiceButton(
                "OpcionAnimalIzquierda",
                answerButtons,
                new Vector2(-166f, -74f),
                leftCard.root,
                RequireSprite(MundoTamanosGeneratedUiAssets.AnswerGreenPath),
                string.Empty);
            AnswerVisualReferences rightAnswer = CreateChoiceButton(
                "OpcionAnimalDerecha",
                answerButtons,
                new Vector2(506f, -74f),
                rightCard.root,
                RequireSprite(MundoTamanosGeneratedUiAssets.AnswerPurplePath),
                string.Empty);
            ui.leftButton = leftAnswer.button;
            ui.rightButton = rightAnswer.button;
            ui.leftAnswerLabel = leftAnswer.label;
            ui.rightAnswerLabel = rightAnswer.label;
            ui.leftAnswerIcon = leftAnswer.icon;
            ui.rightAnswerIcon = rightAnswer.icon;

            Image feedbackCard = CreatePanel("BarraMensajesInferior", gameplay, new Vector2(-700f, -426f), new Vector2(500f, 74f), Color.white);
            TMP_Text feedback = CreateText("Texto-feedback", string.Empty, feedbackCard.transform, 27f, FontStyles.Bold, TextAlignmentOptions.Center, Ink);
            Stretch(feedback.rectTransform, 24f, 8f);
            ui.feedback = feedback;

            Image bottomStars = CreateImage("BottomStars", gameplay, RequireSprite(MundoTamanosGeneratedUiAssets.StarPanelPath), Color.white, false);
            bottomStars.type = Image.Type.Sliced;
            SetRect(bottomStars.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(170f, -493f), new Vector2(350f, 88f));
            for (int index = 0; index < 3; index++)
            {
                Image star = CreateImage("EstrellaInferior_" + (index + 1), bottomStars.transform, emptyStar, Color.white, false);
                SetRect(star.rectTransform, new Vector2(0.2f + index * 0.3f, 0.5f), Vector2.zero, new Vector2(68f, 68f));
                star.preserveAspect = true;
                ui.bottomStars.Add(star);
            }

            BuildResultPanel(canvas.transform, ui, fullStar, emptyStar);
            return ui;
        }

        private sealed class CardReferences
        {
            public RectTransform root;
            public Outline outline;
            public TMP_Text nameText;
        }

        private static CardReferences CreateAnimalCard(
            RectTransform parent,
            string cardName,
            string nameObject,
            string optionLabel,
            Vector2 position,
            Color accent,
            Sprite feedbackSprite)
        {
            Image shadow = CreateImage(cardName + "Shadow", parent, RequireSprite(MundoTamanosGeneratedUiAssets.SoftShadowPath), Color.white, false);
            shadow.type = Image.Type.Sliced;
            SetRect(shadow.rectTransform, new Vector2(0.5f, 0.5f), position + new Vector2(0f, -17f), new Vector2(604f, 434f));

            RectTransform card = CreateRect(cardName, parent);
            SetRect(card, new Vector2(0.5f, 0.5f), position, new Vector2(594f, 430f));
            Image cardImage = card.gameObject.AddComponent<Image>();
            cardImage.sprite = RequireSprite(MundoTamanosGeneratedUiAssets.AnimalCardPath);
            cardImage.type = Image.Type.Sliced;
            cardImage.color = Color.white;
            cardImage.raycastTarget = false;
            Outline outline = AddOutline(card.gameObject, accent, new Vector2(6f, -6f), false);

            Image cardBackground = CreateImage("CardAccentFrame", card, RequireSprite(MundoTamanosGeneratedUiAssets.RoundedFramePath), new Color(accent.r, accent.g, accent.b, 0.54f), false);
            Stretch(cardBackground.rectTransform, 16f, 18f);
            cardBackground.type = Image.Type.Sliced;

            Sprite headerSprite = optionLabel.IndexOf("IZQUIERDA", StringComparison.OrdinalIgnoreCase) >= 0
                ? RequireSprite(MundoTamanosGeneratedUiAssets.HeaderTurquoisePath)
                : RequireSprite(MundoTamanosGeneratedUiAssets.HeaderPurplePath);
            Image header = CreateImage("AnimalNameHeader", card, headerSprite, Color.white, false);
            header.type = Image.Type.Sliced;
            SetRect(header.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 211f), new Vector2(408f, 82f));
            TMP_Text nameText = CreateText(nameObject, string.Empty, header.transform, 31f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            Stretch(nameText.rectTransform, 18f, 8f);

            RectTransform imageContainer = CreateRect("AnimalImageContainer", card);
            SetRect(imageContainer, new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(500f, 286f));

            Image selectionFeedback = CreateImage("SelectionFeedback", card, feedbackSprite, new Color(1f, 1f, 1f, 0.36f), false);
            SetRect(selectionFeedback.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(238f, -163f), new Vector2(48f, 48f));
            selectionFeedback.preserveAspect = true;

            return new CardReferences
            {
                root = card,
                outline = outline,
                nameText = nameText
            };
        }

        private sealed class AnswerVisualReferences
        {
            public Button button;
            public TMP_Text label;
            public Image icon;
        }

        private static AnswerVisualReferences CreateChoiceButton(
            string name,
            RectTransform parent,
            Vector2 position,
            RectTransform visualTarget,
            Sprite answerSprite,
            string initialLabel)
        {
            GameObject buttonObject = new(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonFeedback));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            // Un unico boton invisible cubre la tarjeta y su respuesta inferior.
            // Asi ambos puntos llaman a la misma seleccion sin listeners duplicados.
            SetRect(rect, new Vector2(0.5f, 0.5f), position, new Vector2(610f, 640f));
            Image image = buttonObject.GetComponent<Image>();
            image.sprite = BuiltinSprite();
            image.color = new Color(1f, 1f, 1f, 0.001f);
            image.raycastTarget = true;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;

            UIButtonFeedback feedback = buttonObject.GetComponent<UIButtonFeedback>();
            SerializedObject feedbackSo = new(feedback);
            SetReference(feedbackSo, "button", button);
            SetReference(feedbackSo, "target", visualTarget);
            SetFloat(feedbackSo, "hoverScale", 1.025f);
            SetFloat(feedbackSo, "pressedScale", 0.98f);
            SetFloat(feedbackSo, "transitionDuration", 0.11f);
            feedbackSo.ApplyModifiedPropertiesWithoutUndo();

            Image answerVisual = CreateImage("AnswerVisual", rect, answerSprite, Color.white, false);
            answerVisual.type = Image.Type.Sliced;
            SetRect(answerVisual.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -235f), new Vector2(516f, 98f));

            TMP_Text label = CreateText("AnswerLabel", initialLabel, answerVisual.transform, 28f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetRect(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-38f, 0f), new Vector2(322f, 70f));

            Image iconPlate = CreateImage("AnswerIconPlate", answerVisual.transform, RequireSprite(MundoTamanosGeneratedUiAssets.BackCirclePath), new Color(1f, 1f, 1f, 0.96f), false);
            SetRect(iconPlate.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(202f, 0f), new Vector2(76f, 76f));
            Image icon = CreateImage("AnswerIcon", iconPlate.transform, null, Color.white, false);
            SetRect(icon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(55f, 55f));
            icon.preserveAspect = true;
            icon.enabled = false;

            return new AnswerVisualReferences
            {
                button = button,
                label = label,
                icon = icon
            };
        }

        private static void BuildResultPanel(Transform canvas, UiReferences ui, Sprite fullStar, Sprite emptyStar)
        {
            GameObject panel = CreateUiObject("Panel-resultado", canvas);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            Stretch(panelRect);
            Image blocker = panel.AddComponent<Image>();
            blocker.sprite = BuiltinSprite();
            blocker.color = new Color(0.03f, 0.06f, 0.11f, 0.72f);
            blocker.raycastTarget = true;

            Image resultShadow = CreateImage("SombraResultado", panel.transform, RequireSprite(MundoTamanosGeneratedUiAssets.SoftShadowPath), Color.white, false);
            resultShadow.type = Image.Type.Sliced;
            SetRect(resultShadow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(852f, 560f));
            Image content = CreatePanel("ContenidoResultado", panel.transform, new Vector2(0f, 0f), new Vector2(830f, 540f), Color.white);
            TMP_Text title = CreateText("TituloResultado", "¡LO LOGRASTE!", content.transform, 45f, FontStyles.Bold, TextAlignmentOptions.Center, Purple);
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 176f), new Vector2(690f, 70f));
            ui.resultText = CreateText("Texto-resultado", "Actividad completada", content.transform, 28f, FontStyles.Bold, TextAlignmentOptions.Center, Ink);
            SetRect(ui.resultText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 84f), new Vector2(670f, 105f));

            RectTransform resultStars = CreateRect("ResultStars", content.transform);
            SetRect(resultStars, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(350f, 118f));
            Image resultStarsPanel = CreateImage("MarcoEstrellasResultado", resultStars, RequireSprite(MundoTamanosGeneratedUiAssets.StarPanelPath), Color.white, false);
            resultStarsPanel.type = Image.Type.Sliced;
            Stretch(resultStarsPanel.rectTransform, 0f, 0f);
            ui.resultStarDisplay = CreateStarDisplay(resultStars, "EstrellaResultado", fullStar, emptyStar, ui.resultStars);
            ConfigureDecorativeRatingStars(resultStars, ui.resultStarDisplay, fullStar);

            ui.retryButton = CreateButton("Boton-reintentar", "JUGAR OTRA VEZ", content.transform, new Vector2(-184f, -184f), new Vector2(310f, 78f), RequireSprite(MundoTamanosGeneratedUiAssets.AnswerGreenPath), Color.white);
            ui.resultBackButton = CreateButton("Boton-volver-seleccion", "SELECCIÓN DE MUNDOS", content.transform, new Vector2(184f, -184f), new Vector2(330f, 78f), LoadSprite(PurpleButtonPath), Color.white);
            ui.resultPanel = panel;
            panel.SetActive(false);
        }

        private static UIStarDisplay CreateStarDisplay(RectTransform parent, string childPrefix, Sprite fullStar, Sprite emptyStar, ICollection<Image> output)
        {
            UIStarDisplay display = parent.gameObject.AddComponent<UIStarDisplay>();
            List<Image> stars = new();
            for (int index = 0; index < 3; index++)
            {
                Image star = CreateImage(childPrefix + "_" + (index + 1), parent, emptyStar, Color.white, false);
                SetRect(star.rectTransform, new Vector2(0.2f + index * 0.3f, 0.5f), Vector2.zero, new Vector2(78f, 78f));
                star.preserveAspect = true;
                stars.Add(star);
                output.Add(star);
            }

            ConfigureStarDisplay(display, stars, fullStar, emptyStar, null);
            return display;
        }

        private static void ConfigureDecorativeRatingStars(RectTransform starsRoot, UIStarDisplay display, Sprite fullStar)
        {
            RatingStarCelebration celebration = starsRoot.gameObject.AddComponent<RatingStarCelebration>();
            List<Image> decorative = new();
            Vector2[] origins =
            {
                new(-110f, -6f), new(-48f, 9f), new(0f, -12f), new(50f, 10f), new(112f, -5f)
            };
            for (int index = 0; index < origins.Length; index++)
            {
                Image star = CreateImage("FloatingRatingStar_" + (index + 1), starsRoot, fullStar, Color.white, false);
                SetRect(star.rectTransform, new Vector2(0.5f, 0.5f), origins[index], new Vector2(34f, 34f));
                star.preserveAspect = true;
                star.enabled = false;
                star.gameObject.SetActive(false);
                decorative.Add(star);
            }

            ConfigureStarDisplay(display, GetStarImages(display), fullStar, RequireSprite(EmptyStarPath), celebration);
            SerializedObject celebrationSo = new(celebration);
            SetObjectArray(celebrationSo.FindProperty("decorativeStars"), decorative.Cast<UnityEngine.Object>().ToArray());
            SetFloat(celebrationSo, "flightDuration", 0.92f);
            SetFloat(celebrationSo, "delayBetweenStars", 0.1f);
            SetFloat(celebrationSo, "delayBetweenLoops", 0.32f);
            celebrationSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSizeController(SizeWorldController controller, UiReferences ui, Sprite fullStar, Sprite emptyStar)
        {
            ClearAllListeners(ui.leftButton.onClick);
            ClearAllListeners(ui.rightButton.onClick);
            ClearAllListeners(ui.returnButton.onClick);
            ClearAllListeners(ui.retryButton.onClick);
            UnityEventTools.AddPersistentListener(ui.retryButton.onClick, controller.StartActivity);
            ClearAllListeners(ui.resultBackButton.onClick);
            UnityEventTools.AddPersistentListener(ui.resultBackButton.onClick, controller.ReturnToWorldSelection);

            SerializedObject serialized = new(controller);
            SetReference(serialized, "backgroundImage", ui.background);
            SetReference(serialized, "leftAnimalImage", ui.leftAnimal);
            SetReference(serialized, "rightAnimalImage", ui.rightAnimal);
            SetReference(serialized, "questionText", ui.question);
            SetReference(serialized, "feedbackText", ui.feedback);
            SetReference(serialized, "leftAnimalButton", ui.leftButton);
            SetReference(serialized, "rightAnimalButton", ui.rightButton);
            SetReference(serialized, "leftAnimalNameText", ui.leftName);
            SetReference(serialized, "rightAnimalNameText", ui.rightName);
            SetReference(serialized, "leftSelectionOutline", ui.leftOutline);
            SetReference(serialized, "rightSelectionOutline", ui.rightOutline);
            SetReference(serialized, "animalSafeArea", ui.animalSafeArea);
            SetReference(serialized, "resultPanel", ui.resultPanel);
            SetReference(serialized, "resultText", ui.resultText);
            SetReference(serialized, "returnButton", ui.returnButton);
            SetObjectArray(serialized.FindProperty("starImages"), ui.topStars.Cast<UnityEngine.Object>().ToArray());
            SetReference(serialized, "fullStarSprite", fullStar);
            SetReference(serialized, "emptyStarSprite", emptyStar);
            SetReference(serialized, "starDisplay", ui.topStarDisplay);

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureTutorial(Transform canvas, SizeWorldController controller)
        {
            GameObject overlay = CreateUiObject("TutorialOverlay", canvas);
            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            Stretch(overlayRect);
            Image blocker = overlay.AddComponent<Image>();
            blocker.sprite = BuiltinSprite();
            blocker.color = new Color(0.04f, 0.07f, 0.13f, 0.72f);
            blocker.raycastTarget = true;
            CanvasGroup group = overlay.AddComponent<CanvasGroup>();
            // El controlador de diálogo lo muestra en Start; dejarlo transparente
            // aquí mantiene visible la composición del nivel al abrir la escena.
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            RectTransform characterRoot = CreateRect("TutorialGuide", overlay.transform);
            SetRect(characterRoot, new Vector2(0.5f, 0.5f), new Vector2(-490f, -42f), new Vector2(410f, 535f));
            Image character = CreateImage("TutorialGuideImage", characterRoot, LoadSprite(GuideIdlePath), Color.white, false);
            Stretch(character.rectTransform, 12f, 12f);
            character.preserveAspect = true;

            Image bubble = CreatePanel("TutorialBubble", overlay.transform, new Vector2(258f, 38f), new Vector2(760f, 354f), Cream);
            AddOutline(bubble.gameObject, new Color(0.53f, 0.31f, 0.76f, 0.88f), new Vector2(4f, -4f), true);
            TMP_Text text = CreateText("TutorialText", string.Empty, bubble.transform, 33f, FontStyles.Bold, TextAlignmentOptions.Center, Ink);
            SetRect(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 52f), new Vector2(640f, 178f));
            Button next = CreateButton("NextButton", "SIGUIENTE", bubble.transform, new Vector2(0f, -116f), new Vector2(280f, 70f), LoadSprite(OrangeButtonPath), Color.white);
            TMP_Text nextText = next.GetComponentInChildren<TMP_Text>(true);

            CharacterDialogueController dialogue = overlay.AddComponent<CharacterDialogueController>();

            Sprite idle = LoadSprite(GuideIdlePath);
            Sprite retry = LoadSprite(GuideRetryPath) ?? idle;
            Sprite celebrate = LoadSprite(GuideCelebratePath) ?? idle;
            SerializedObject dialogueSo = new(dialogue);
            SetReference(dialogueSo, "overlayGroup", group);
            SetReference(dialogueSo, "characterRoot", characterRoot);
            SetReference(dialogueSo, "characterImage", character);
            SetReference(dialogueSo, "bubbleRoot", bubble.rectTransform);
            SetReference(dialogueSo, "dialogueText", text);
            SetReference(dialogueSo, "nextButton", next);
            SetReference(dialogueSo, "nextButtonText", nextText);
            // El gate de Tamaños decide si se muestra una sola vez o se inicia el nivel.
            SetBool(dialogueSo, "showIntroOnStart", false);
            SetString(dialogueSo, "finalIntroButtonText", "¡COMENZAR!");
            SetString(dialogueSo, "retryMessage", "¡Casi! Observa con atención e inténtalo otra vez.");
            SetString(dialogueSo, "retryButtonText", "Intentar otra vez");
            SetReference(dialogueSo, "retryCharacterSprite", retry);
            SetString(dialogueSo, "successButtonText", "Continuar");
            SetFloat(dialogueSo, "successAutoHideSeconds", 0.85f);
            ConfigureDialogueSteps(dialogueSo.FindProperty("introSteps"), new[]
            {
                ("Observa los dos animales.", idle, "Siguiente"),
                ("Mira si debes elegir el más grande o el más pequeño.", celebrate, "Siguiente"),
                ("Selecciona un animal.", idle, "Siguiente"),
                ("Tu selección se valida automáticamente.", celebrate, "Comenzar")
            });
            dialogueSo.ApplyModifiedPropertiesWithoutUndo();

            ClearAllListeners(dialogue.OnIntroCompleted);

            GameObject tutorialFlowObject = CreateUiObject("SizeWorldTutorialFlow", canvas);
            SizeWorldTutorialController tutorialFlow = tutorialFlowObject.AddComponent<SizeWorldTutorialController>();
            SerializedObject flowSo = new(tutorialFlow);
            SetReference(flowSo, "sizeWorldController", controller);
            SetReference(flowSo, "dialogueController", dialogue);
            SetReference(flowSo, "tutorialOverlay", overlay);
            flowSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurePresentationController(Transform canvas, SizeWorldController controller, UiReferences ui)
        {
            GameObject host = CreateUiObject("SizeWorldPresentation", canvas);
            SizeWorldPresentationController presentation = host.AddComponent<SizeWorldPresentationController>();
            SerializedObject serialized = new(presentation);
            SetReference(serialized, "controller", controller);
            SetReference(serialized, "titleTransform", ui.title);
            SetReference(serialized, "instructionTransform", ui.instruction);
            SetReference(serialized, "leftCardTransform", ui.leftCard);
            SetReference(serialized, "rightCardTransform", ui.rightCard);
            SetReference(serialized, "presentationCanvasGroup", ui.gameplayGroup);
            SetBool(serialized, "showGameplayGuide", false);
            SetReference(serialized, "messageText", ui.feedback);
            SetReference(serialized, "resultStarDisplay", ui.resultStarDisplay);
            SetReference(serialized, "leftAnswerLabel", ui.leftAnswerLabel);
            SetReference(serialized, "rightAnswerLabel", ui.rightAnswerLabel);
            SetReference(serialized, "leftAnswerIcon", ui.leftAnswerIcon);
            SetReference(serialized, "rightAnswerIcon", ui.rightAnswerIcon);
            SetReference(serialized, "leftSourceAnimalImage", ui.leftAnimal);
            SetReference(serialized, "rightSourceAnimalImage", ui.rightAnimal);
            SetReference(serialized, "leftSourceAnimalName", ui.leftName);
            SetReference(serialized, "rightSourceAnimalName", ui.rightName);
            SetObjectArray(serialized.FindProperty("sourceStarImages"), ui.topStars.Cast<UnityEngine.Object>().ToArray());
            SetObjectArray(serialized.FindProperty("bottomStarImages"), ui.bottomStars.Cast<UnityEngine.Object>().ToArray());
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateSceneTransition(Transform canvas)
        {
            GameObject overlay = CreateUiObject("SceneTransitionOverlay", canvas);
            RectTransform rect = overlay.GetComponent<RectTransform>();
            Stretch(rect);
            Image image = overlay.AddComponent<Image>();
            image.sprite = BuiltinSprite();
            image.color = new Color(0.03f, 0.05f, 0.11f, 1f);
            image.raycastTarget = true;
            CanvasGroup group = overlay.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            UISceneTransition transition = overlay.AddComponent<UISceneTransition>();
            SerializedObject serialized = new(transition);
            SetReference(serialized, "fadeCanvasGroup", group);
            SetFloat(serialized, "duration", 0.28f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            overlay.transform.SetAsLastSibling();
        }

        private static List<Image> GetStarImages(UIStarDisplay display)
        {
            List<Image> images = new();
            if (display == null) return images;
            SerializedProperty stars = new SerializedObject(display).FindProperty("stars");
            if (stars == null) return images;
            for (int index = 0; index < stars.arraySize; index++)
            {
                if (stars.GetArrayElementAtIndex(index).objectReferenceValue is Image image)
                {
                    images.Add(image);
                }
            }

            return images;
        }

        private static void ConfigureStarDisplay(UIStarDisplay display, IReadOnlyList<Image> stars, Sprite fullStar, Sprite emptyStar, RatingStarCelebration celebration)
        {
            if (display == null) return;
            SerializedObject serialized = new(display);
            SetObjectArray(serialized.FindProperty("stars"), stars.Cast<UnityEngine.Object>().ToArray());
            SetReference(serialized, "earnedSprite", fullStar);
            SetReference(serialized, "unearnedSprite", emptyStar);
            if (celebration != null) SetReference(serialized, "celebration", celebration);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            foreach (Image image in stars)
            {
                if (image != null) image.sprite = emptyStar;
            }
        }

        private static void EnsureEventSystem(Scene scene)
        {
            if (ComponentsInScene<EventSystem>(scene).Length > 0) return;
            GameObject eventSystem = new("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            SceneManager.MoveGameObjectToScene(eventSystem, scene);
        }

        private static Button CreateButton(string name, string label, Transform parent, Vector2 position, Vector2 size, Sprite sprite, Color labelColor)
        {
            GameObject buttonObject = new(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonFeedback));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0.5f), position, size);
            Image image = buttonObject.GetComponent<Image>();
            image.sprite = sprite ?? RequireSprite(MundoTamanosGeneratedUiAssets.AnswerGreenPath);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            TMP_Text text = CreateText("Texto", label, rect, 23f, FontStyles.Bold, TextAlignmentOptions.Center, labelColor);
            Stretch(text.rectTransform, 16f, 8f);
            return button;
        }

        private static Button CreateBackTextButton(string name, Transform parent, Vector2 position, Vector2 size, Sprite buttonSprite, Sprite iconSprite)
        {
            GameObject buttonObject = new(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonFeedback));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0.5f), position, size);
            Image image = buttonObject.GetComponent<Image>();
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;

            Image icon = CreateImage("IconoVolver", rect, iconSprite, Color.white, false);
            SetRect(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-76f, 0f), new Vector2(40f, 40f));
            icon.preserveAspect = true;

            TMP_Text label = CreateText("TextoVOLVER", "VOLVER", rect, 27f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetRect(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(24f, 0f), new Vector2(142f, 58f));
            return button;
        }

        private static Image CreatePanel(string name, Transform parent, Vector2 position, Vector2 size, Color color, Sprite sprite = null)
        {
            Image panel = CreateImage(name, parent, sprite ?? RequireSprite(MundoTamanosGeneratedUiAssets.InstructionPanelPath), color, false);
            panel.type = Image.Type.Sliced;
            SetRect(panel.rectTransform, new Vector2(0.5f, 0.5f), position, size);
            return panel;
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
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            return CreateUiObject(name, parent).GetComponent<RectTransform>();
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static Outline AddOutline(GameObject target, Color color, Vector2 distance, bool enabled)
        {
            Outline outline = target.GetComponent<Outline>() ?? target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
            outline.enabled = enabled;
            return outline;
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

        private static void ConfigureDialogueSteps(SerializedProperty steps, IReadOnlyList<(string message, Sprite sprite, string button)> values)
        {
            if (steps == null) return;
            steps.arraySize = values.Count;
            for (int index = 0; index < values.Count; index++)
            {
                SerializedProperty step = steps.GetArrayElementAtIndex(index);
                step.FindPropertyRelative("message").stringValue = values[index].message;
                step.FindPropertyRelative("characterSprite").objectReferenceValue = values[index].sprite;
                step.FindPropertyRelative("buttonText").stringValue = values[index].button;
            }
        }

        private static void ConfigureStringArray(SerializedProperty property, IReadOnlyList<string> values)
        {
            if (property == null) return;
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index++) property.GetArrayElementAtIndex(index).stringValue = values[index];
        }

        private static void ConfigureObjectArray(SerializedProperty property, IReadOnlyList<UnityEngine.Object> values)
        {
            if (property == null) return;
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index++) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }

        private static void SetObjectArray(SerializedProperty property, IReadOnlyList<UnityEngine.Object> values)
        {
            ConfigureObjectArray(property, values);
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

        private static void SetReference(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SetReference(serialized.FindProperty(propertyName), value);
        }

        private static void SetReference(SerializedProperty property, UnityEngine.Object value)
        {
            if (property != null) property.objectReferenceValue = value;
        }

        private static void SetBool(SerializedObject serialized, string propertyName, bool value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null) property.boolValue = value;
        }

        private static void SetFloat(SerializedObject serialized, string propertyName, float value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null) property.floatValue = value;
        }

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null) property.stringValue = value;
        }

        private static Sprite RequireSprite(string path)
        {
            Sprite sprite = LoadSprite(path);
            if (sprite == null) throw new InvalidOperationException($"No se encontró el sprite requerido: {path}");
            return sprite;
        }

        private static Sprite LoadSprite(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite BuiltinSprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        }

        private static T GetOrAdd<T>(GameObject gameObject) where T : Component
        {
            return gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();
        }

        private static T[] ComponentsInScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        }
    }
}
#endif
