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

            DestroyIfExists(scene, "SupportModePanel");
            DestroyIfExists(scene, "ReadingProgressBar");
            DestroyIfExists(scene, "TextoPorcentaje");

            RectTransform readingPanel = EnsurePanel(canvas, "PanelLecturaCuento", false, new Color(1f, 1f, 1f, 0f));
            MoveReadingObjectsIntoPanel(scene, readingPanel);

            RectTransform selectionPanel = EnsureSelectionPanel(canvas, manager);
            RectTransform resultPanel = EnsureResultPanel(canvas);

            ScrollRect recognizedScroll = EnsureExistingScroll(scene, "RecognizedTextCard", "ScrollViewLecturaReconocida", "TextoLecturaReconocida");
            ScrollRect storyScroll = EnsureExistingScroll(scene, "StoryTextCard", "ScrollViewCuerpoCuento", "TextoHistoria");

            Button startButton = RequireComponent<Button>(scene, "BotonIniciarVoz");
            Button stopButton = RequireComponent<Button>(scene, "BotonDetenerVoz");
            Button validateButton = RequireComponent<Button>(scene, "BotonValidarLectura");
            Button clearButton = RequireComponent<Button>(scene, "BotonLimpiarTexto");
            Button backToSelectionButton = RequireComponent<Button>(scene, "BotonVolver");
            EnsureReadingPanelLayout(
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
            SetReference(so, "errorText", FindOrCreateReadingText(scene, readingPanel, "ErrorText", string.Empty, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.14f)));
            SetReference(so, "starsResultText", FindOrCreateReadingText(scene, readingPanel, "StarsResultText", string.Empty, new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.2f)));
            SetReference(so, "resultStoryTitleText", RequireComponent<TMP_Text>(scene, "TextoResultadoCuento"));
            SetReference(so, "resultScoreText", RequireComponent<TMP_Text>(scene, "TextoResultadoPuntaje"));
            SetReference(so, "resultStarsText", RequireComponent<TMP_Text>(scene, "TextoResultadoEstrellas"));
            SetReference(so, "resultMessageText", RequireComponent<TMP_Text>(scene, "TextoResultadoMensaje"));
            SetReference(so, "startButton", startButton);
            SetReference(so, "stopButton", stopButton);
            SetReference(so, "retryButton", startButton);
            SetReference(so, "validateButton", validateButton);
            SetReference(so, "clearButton", clearButton);
            SetReference(so, "backToSelectionButton", backToSelectionButton);
            SetReference(so, "recognizedTextScrollRect", recognizedScroll);
            SetReference(so, "storyBodyScrollRect", storyScroll);
            SetReference(so, "microphoneListeningIndicator", RequireObject(scene, "MicrophoneListeningIndicator"));

            SerializedProperty stories = so.FindProperty("cuentosDisponibles");
            stories.arraySize = 3;
            ConfigureStory(stories.GetArrayElementAtIndex(0), StoryProgressRepository.DefaultStoryId, "Los tres cerditos", DefaultStoryText);
            ConfigureStory(stories.GetArrayElementAtIndex(1), "conejo_luna", "El conejo y la luna", RabbitStoryText);
            ConfigureStory(stories.GetArrayElementAtIndex(2), "tortuga_amable", "La tortuga amable", TurtleStoryText);

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
            CreateText("TextoResultadoEstrellas", "☆☆☆", cardRect, 48f, FontStyles.Bold, new Color(1f, 0.76f, 0.12f, 1f), TextAlignmentOptions.Center, 70f);
            CreateText("TextoResultadoMensaje", "Regresando a los cuentos...", cardRect, 26f, FontStyles.Bold, Purple, TextAlignmentOptions.Center, 140f);
            return panel;
        }

        private static void MoveReadingObjectsIntoPanel(Scene scene, RectTransform readingPanel)
        {
            string[] names =
            {
                "StoryVisualDesign", "StoryHeader", "StoryTextCard", "RecognizedTextCard", "BotonIniciarVoz",
                "BotonDetenerVoz", "BotonLimpiarTexto", "BotonValidarLectura", "BotonVolver",
                "MicrophoneListeningIndicator", "Star_1", "Star_2", "Star_3", "TextoContador",
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

        private static void ConfigureStory(SerializedProperty property, string id, string title, string text)
        {
            property.FindPropertyRelative("id").stringValue = id;
            property.FindPropertyRelative("titulo").stringValue = title;
            property.FindPropertyRelative("textoCompleto").stringValue = text;
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
