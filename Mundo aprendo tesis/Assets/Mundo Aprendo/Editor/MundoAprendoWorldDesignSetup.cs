using System;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bolin.Editor
{
    public static class MundoAprendoWorldDesignSetup
    {
        private const string StoryScenePath = "Assets/Scenes/MundoCuentos_VozTest.unity";
        private const string SizeScenePath = "Assets/Scenes/MundoTamanos.unity";

        private static readonly Color Ink = new(0.09f, 0.14f, 0.22f, 1f);
        private static readonly Color Teal = new(0.12f, 0.58f, 0.62f, 1f);
        private static readonly Color Coral = new(0.94f, 0.42f, 0.36f, 1f);
        private static readonly Color Gold = new(0.96f, 0.7f, 0.2f, 1f);
        private static readonly Color Sky = new(0.67f, 0.87f, 0.92f, 1f);

        [MenuItem("Mundo Aprendo/Redisenar Mundo de Cuentos y Tamanos")]
        public static void ApplyDesigns()
        {
            MundoAprendoPolishSetup.ApplyProjectPolish();

            Scene storyScene = EditorSceneManager.OpenScene(StoryScenePath, OpenSceneMode.Single);
            DesignStoryWorld(storyScene);
            EditorSceneManager.MarkSceneDirty(storyScene);
            EditorSceneManager.SaveScene(storyScene, StoryScenePath);

            Scene sizeScene = EditorSceneManager.OpenScene(SizeScenePath, OpenSceneMode.Single);
            DesignSizeWorld(sizeScene);
            EditorSceneManager.MarkSceneDirty(sizeScene);
            EditorSceneManager.SaveScene(sizeScene, SizeScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(StoryScenePath, OpenSceneMode.Single);
            Debug.Log("Mundo de Cuentos y Mundo de Tamanos redisenados con objetos persistentes y visibles en la jerarquia.");
        }

        [MenuItem("Mundo Aprendo/Diagnostico/Dump layouts Cuentos y Tamanos")]
        public static void DumpLayouts()
        {
            StringBuilder report = new();
            DumpScene(StoryScenePath, report);
            DumpScene(SizeScenePath, report);
            Debug.Log(report.ToString());
        }

        private static void DumpScene(string path, StringBuilder report)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            report.AppendLine($"=== {scene.name} ===");
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                RectTransform canvas = root.GetComponentInChildren<Canvas>(true)?.transform as RectTransform;
                if (canvas != null) DumpTransform(canvas, report, 0, 3);
            }
        }

        private static void DumpTransform(Transform transform, StringBuilder report, int depth, int maxDepth)
        {
            if (depth > maxDepth) return;
            string indent = new(' ', depth * 2);
            if (transform is RectTransform rect)
            {
                string details = $"pos={rect.anchoredPosition} size={rect.sizeDelta} anchors={rect.anchorMin}/{rect.anchorMax}";
                Image image = rect.GetComponent<Image>();
                TMP_Text text = rect.GetComponent<TMP_Text>();
                if (image != null) details += $" image={image.color} sprite={(image.sprite != null ? image.sprite.name : "null")}";
                if (text != null) details += $" text='{text.text}' font={text.fontSize}";
                report.AppendLine($"{indent}{rect.name} active={rect.gameObject.activeSelf} {details}");
            }

            foreach (Transform child in transform) DumpTransform(child, report, depth + 1, maxDepth);
        }

        private static void DesignStoryWorld(Scene scene)
        {
            Canvas canvas = ComponentsInScene<Canvas>(scene).First(item => item.isRootCanvas);
            Image background = FindComponent<Image>(scene, "Fondo");
            background.color = new Color(0.58f, 0.8f, 0.87f, 1f);

            GameObject decor = EnsureRect("StoryVisualDesign", canvas.transform);
            Stretch(decor.GetComponent<RectTransform>());
            decor.transform.SetSiblingIndex(background.transform.GetSiblingIndex() + 1);
            EnsureDecorPill(decor.transform, "CloudTopLeft", new Vector2(0f, -75f), new Vector2(360f, 120f), new Vector2(0f, 1f), new Color(0.94f, 0.98f, 1f, 0.5f));
            EnsureDecorPill(decor.transform, "CloudBottomRight", new Vector2(-20f, 45f), new Vector2(460f, 150f), new Vector2(1f, 0f), new Color(0.95f, 0.86f, 0.72f, 0.36f));
            GameObject header = EnsurePanel(decor.transform, "StoryHeader", new Color(0.12f, 0.48f, 0.62f, 0.98f));
            SetRect(header.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(1080f, 92f));
            EnsureLabel(header.transform, "Title", "MUNDO DE LOS CUENTOS", 42f, Color.white, new Vector2(0f, 12f), new Vector2(980f, 50f), FontStyles.Bold);
            EnsureLabel(header.transform, "Subtitle", "Lee, escucha y descubre", 21f, new Color(0.86f, 0.96f, 1f, 1f), new Vector2(0f, -27f), new Vector2(760f, 30f));

            RectTransform content = FindRect(scene, "Contenido");
            content.anchoredPosition = new Vector2(0f, -45f);
            content.sizeDelta = new Vector2(1660f, 790f);
            HorizontalLayoutGroup contentLayout = GetOrAdd<HorizontalLayoutGroup>(content.gameObject);
            contentLayout.padding = new RectOffset(12, 12, 10, 10);
            contentLayout.spacing = 42f;
            contentLayout.childAlignment = TextAnchor.MiddleCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childForceExpandHeight = false;

            RectTransform storyPanel = FindRect(scene, "PanelHistoria");
            RectTransform voicePanel = FindRect(scene, "PanelReconocimientoVoz");
            ConfigureMainPanel(storyPanel, new Color(1f, 0.96f, 0.84f, 0.99f), new Color(0.88f, 0.55f, 0.28f, 0.65f));
            ConfigureMainPanel(voicePanel, new Color(0.9f, 0.98f, 0.98f, 0.99f), new Color(0.14f, 0.58f, 0.62f, 0.65f));
            ConfigureVerticalPanel(storyPanel, 12f, new RectOffset(34, 34, 25, 25));
            ConfigureVerticalPanel(voicePanel, 7f, new RectOffset(32, 32, 20, 20));
            SetLayout(storyPanel.gameObject, 790f, 790f);
            SetLayout(voicePanel.gameObject, 790f, 790f);

            GameObject storyBadge = EnsureSectionBadge(storyPanel, "StorySectionBadge", "CUENTO", Coral);
            storyBadge.transform.SetAsFirstSibling();
            TMP_Text storyTitle = FindComponent<TMP_Text>(scene, "TituloCuento");
            storyTitle.fontSize = 42f;
            storyTitle.fontStyle = FontStyles.Bold;
            storyTitle.color = new Color(0.63f, 0.25f, 0.22f, 1f);
            SetLayout(storyTitle.gameObject, 56f);
            TMP_Text storyText = FindComponent<TMP_Text>(scene, "TextoHistoria");
            storyText.fontSize = 28f;
            storyText.color = Ink;
            storyText.alignment = TextAlignmentOptions.TopLeft;
            storyText.lineSpacing = 10f;
            storyText.margin = new Vector4(12f, 10f, 12f, 10f);
            WrapTextInCard(storyText, "StoryTextCard", new Color(1f, 0.99f, 0.94f, 0.82f), 600f);

            GameObject voiceBadge = EnsureSectionBadge(voicePanel, "VoiceSectionBadge", "TU LECTURA", Teal);
            voiceBadge.transform.SetAsFirstSibling();
            TMP_Text voiceTitle = FindComponent<TMP_Text>(scene, "Titulo");
            voiceTitle.text = "Escucha tu lectura";
            voiceTitle.fontSize = 36f;
            voiceTitle.fontStyle = FontStyles.Bold;
            voiceTitle.color = new Color(0.08f, 0.4f, 0.46f, 1f);
            SetLayout(voiceTitle.gameObject, 50f);

            TMP_Text recognized = FindComponent<TMP_Text>(scene, "TextoReconocido");
            recognized.text = "Aquí aparecerán las palabras que leas.";
            recognized.fontSize = 25f;
            recognized.color = new Color(0.25f, 0.34f, 0.43f, 1f);
            recognized.alignment = TextAlignmentOptions.TopLeft;
            recognized.margin = new Vector4(18f, 14f, 18f, 14f);
            WrapTextInCard(recognized, "RecognizedTextCard", new Color(1f, 1f, 1f, 0.88f), 112f);

            TMP_Text percentage = FindComponent<TMP_Text>(scene, "TextoPorcentaje");
            percentage.fontStyle = FontStyles.Bold;
            percentage.color = Teal;
            SetLayout(percentage.gameObject, 32f);
            TMP_Text result = FindComponent<TMP_Text>(scene, "TextoResultadoLectura");
            result.text = "Tu resultado aparecerá aquí.";
            result.color = Ink;
            SetLayout(result.gameObject, 38f);
            SetLayout(FindRect(scene, "PanelEstrellas").gameObject, 44f);
            TMP_Text countdown = FindComponent<TMP_Text>(scene, "TextoContador");
            countdown.text = "LISTO PARA LEER";
            countdown.fontStyle = FontStyles.Bold;
            countdown.color = Coral;
            SetLayout(countdown.gameObject, 32f);
            TMP_Text status = FindComponent<TMP_Text>(scene, "TextoEstado");
            status.text = "Presiona INICIAR cuando estés listo.";
            status.fontSize = 21f;
            status.color = Ink;
            SetLayout(status.gameObject, 48f);

            UIProgressBar progress = ComponentsInScene<UIProgressBar>(scene).First(item => item.name == "ReadingProgressBar");
            progress.transform.SetParent(voicePanel, false);
            progress.transform.SetSiblingIndex(recognized.transform.GetSiblingIndex() + 1);
            RectTransform progressRect = progress.transform as RectTransform;
            progressRect.sizeDelta = new Vector2(700f, 46f);
            SetLayout(progress.gameObject, 46f);
            progress.SetProgress(0f, "PROGRESO DE LECTURA");

            ConfigureStoryButtons(scene);

            RectTransform buttonRow = FindRect(scene, "FilaBotonesVoz");
            buttonRow.sizeDelta = new Vector2(700f, 58f);
            SetLayout(buttonRow.gameObject, 58f);
            HorizontalLayoutGroup rowLayout = GetOrAdd<HorizontalLayoutGroup>(buttonRow.gameObject);
            rowLayout.spacing = 10f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandWidth = true;
            SetLayout(FindRect(scene, "BotonVolver").gameObject, 46f);

            GameObject microphoneIndicator = FindRect(scene, "MicrophoneListeningIndicator").gameObject;
            microphoneIndicator.transform.SetParent(voicePanel, false);
            LayoutElement indicatorLayout = GetOrAdd<LayoutElement>(microphoneIndicator);
            indicatorLayout.ignoreLayout = true;
            SetRect(microphoneIndicator.transform as RectTransform, new Vector2(1f, 1f), new Vector2(-125f, -25f), new Vector2(220f, 44f));
            microphoneIndicator.SetActive(true);

            SetOverlayVisibleInEditor(scene);
        }

        private static void ConfigureStoryButtons(Scene scene)
        {
            StyleStoryButton(scene, "BotonIniciarVoz", new Color(0.18f, 0.66f, 0.46f, 1f), "INICIAR");
            StyleStoryButton(scene, "BotonDetenerVoz", Coral, "DETENER");
            StyleStoryButton(scene, "BotonLimpiarTexto", new Color(0.22f, 0.5f, 0.76f, 1f), "LIMPIAR");
            StyleStoryButton(scene, "BotonValidarLectura", Gold, "VALIDAR", Ink);
            StyleStoryButton(scene, "BotonVolver", new Color(0.34f, 0.3f, 0.58f, 1f), "VOLVER");
        }

        private static void StyleStoryButton(Scene scene, string name, Color color, string label, Color? textColor = null)
        {
            Button button = FindComponent<Button>(scene, name);
            if (button == null) return;
            StyleButton(button, color, label, 20f, textColor ?? Color.white);
            LayoutElement layout = GetOrAdd<LayoutElement>(button.gameObject);
            layout.preferredHeight = 56f;
            layout.flexibleWidth = 1f;
        }

        private static RectTransform EnsureAnimalStage(RectTransform parent, string name, Vector2 anchor, string label, Color accent)
        {
            GameObject stage = EnsurePanel(parent, name, new Color(1f, 1f, 1f, 0.2f));
            RectTransform rect = stage.transform as RectTransform;
            SetRect(rect, anchor, Vector2.zero, new Vector2(510f, 440f));
            Outline outline = GetOrAdd<Outline>(stage);
            outline.effectColor = new Color(1f, 1f, 1f, 0.72f);
            outline.effectDistance = new Vector2(3f, -3f);
            EnsureLabel(stage.transform, "OptionBadge", label, 22f, Color.white, new Vector2(0f, 180f), new Vector2(190f, 44f), FontStyles.Bold);
            TMP_Text badge = stage.transform.Find("OptionBadge")?.GetComponent<TMP_Text>();
            if (badge != null)
            {
                GameObject badgePanel = EnsurePanel(stage.transform, "BadgeBackground", accent);
                SetRect(badgePanel.transform as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(210f, 48f));
                badgePanel.transform.SetAsFirstSibling();
                badge.transform.SetAsLastSibling();
            }
            return rect;
        }

        private static void StyleAnimalName(Scene scene, string name, string previewText, Color color)
        {
            RectTransform root = FindRect(scene, name);
            root.sizeDelta = new Vector2(340f, 62f);
            Image image = root.GetComponent<Image>();
            image.sprite = BuiltinSprite();
            image.type = Image.Type.Sliced;
            image.color = color;
            AddSoftShadow(root.gameObject, 0.22f, new Vector2(0f, -4f));
            TMP_Text text = root.GetComponentInChildren<TMP_Text>(true);
            text.text = previewText.ToUpperInvariant();
            text.fontSize = 28f;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
        }

        private static void StyleChoiceButton(Scene scene, string name, string label, Color color)
        {
            Button button = FindComponent<Button>(scene, name);
            StyleButton(button, color, label, 27f);
            LayoutElement layout = GetOrAdd<LayoutElement>(button.gameObject);
            layout.preferredHeight = 98f;
            layout.flexibleWidth = 1f;
        }

        private static void StyleButton(Button button, Color color, string label, float fontSize, Color? textColor = null)
        {
            if (button == null) return;
            Image image = button.targetGraphic as Image ?? GetOrAdd<Image>(button.gameObject);
            image.sprite = BuiltinSprite();
            image.type = Image.Type.Sliced;
            image.color = color;
            button.targetGraphic = image;
            AddSoftShadow(button.gameObject, 0.24f, new Vector2(0f, -4f));
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.text = label;
                text.fontSize = fontSize;
                text.fontStyle = FontStyles.Bold;
                text.color = textColor ?? Color.white;
                text.alignment = TextAlignmentOptions.Center;
            }
            GetOrAdd<UIButtonFeedback>(button.gameObject);
        }

        private static void ConfigureMainPanel(RectTransform panel, Color color, Color outlineColor)
        {
            Image image = GetOrAdd<Image>(panel.gameObject);
            image.sprite = BuiltinSprite();
            image.type = Image.Type.Sliced;
            image.color = color;
            AddSoftShadow(panel.gameObject, 0.22f, new Vector2(0f, -7f));
            Outline outline = GetOrAdd<Outline>(panel.gameObject);
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;
        }

        private static void ConfigureVerticalPanel(RectTransform panel, float spacing, RectOffset padding)
        {
            VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(panel.gameObject);
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;
        }

        private static GameObject EnsureSectionBadge(RectTransform parent, string name, string text, Color color)
        {
            GameObject badge = EnsurePanel(parent, name, color);
            SetLayout(badge, 38f);
            EnsureLabel(badge.transform, "Label", text, 19f, Color.white, Vector2.zero, Vector2.zero, FontStyles.Bold, true);
            return badge;
        }

        private static void WrapTextInCard(TMP_Text text, string cardName, Color color, float height)
        {
            Transform parent = text.transform.parent;
            GameObject card = parent.name == cardName ? parent.gameObject : EnsurePanel(parent, cardName, color);
            if (text.transform.parent != card.transform)
            {
                int siblingIndex = text.transform.GetSiblingIndex();
                card.transform.SetSiblingIndex(siblingIndex);
                text.transform.SetParent(card.transform, false);
            }
            SetLayout(card, height);
            RectTransform rect = text.rectTransform;
            Stretch(rect);
            LayoutElement textLayout = GetOrAdd<LayoutElement>(text.gameObject);
            textLayout.ignoreLayout = true;
        }

        private static void EnsureDecorPill(Transform parent, string name, Vector2 position, Vector2 size, Vector2 anchor, Color color)
        {
            GameObject pill = EnsurePanel(parent, name, color);
            SetRect(pill.transform as RectTransform, anchor, position, size);
            pill.GetComponent<Image>().raycastTarget = false;
        }

        private static GameObject EnsurePanel(Transform parent, string name, Color color)
        {
            GameObject panel = EnsureRect(name, parent);
            Image image = GetOrAdd<Image>(panel);
            image.sprite = BuiltinSprite();
            image.type = Image.Type.Sliced;
            image.color = color;
            return panel;
        }

        private static GameObject EnsureRect(string name, Transform parent)
        {
            Transform existing = parent.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == name);
            if (existing != null) return existing.gameObject;
            GameObject gameObject = new(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static TMP_Text EnsureLabel(Transform parent, string name, string text, float size, Color color, Vector2 position, Vector2 dimensions, FontStyles style = FontStyles.Normal, bool stretch = false)
        {
            GameObject labelObject = EnsureRect(name, parent);
            TextMeshProUGUI label = GetOrAdd<TextMeshProUGUI>(labelObject);
            label.text = text;
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            if (stretch) Stretch(label.rectTransform);
            else SetRect(label.rectTransform, new Vector2(0.5f, 0.5f), position, dimensions);
            return label;
        }

        private static void AddSoftShadow(GameObject gameObject, float alpha, Vector2 distance)
        {
            Shadow shadow = gameObject.GetComponents<Shadow>().FirstOrDefault(item => item.GetType() == typeof(Shadow));
            if (shadow == null) shadow = gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.03f, 0.07f, 0.12f, alpha);
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private static void SetLayout(GameObject gameObject, float preferredHeight)
        {
            LayoutElement layout = GetOrAdd<LayoutElement>(gameObject);
            layout.preferredHeight = preferredHeight;
        }

        private static void SetLayout(GameObject gameObject, float preferredWidth, float preferredHeight)
        {
            LayoutElement layout = GetOrAdd<LayoutElement>(gameObject);
            layout.preferredWidth = preferredWidth;
            layout.preferredHeight = preferredHeight;
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void SetOverlayVisibleInEditor(Scene scene)
        {
            RectTransform overlay = FindRect(scene, "SceneTransitionOverlay");
            CanvasGroup group = overlay.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            overlay.SetAsLastSibling();
        }

        private static Sprite BuiltinSprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        }

        private static RectTransform FindRect(Scene scene, string name)
        {
            return FindComponent<RectTransform>(scene, name);
        }

        private static T FindComponent<T>(Scene scene, string name) where T : Component
        {
            return ComponentsInScene<T>(scene).FirstOrDefault(item => item.name == name);
        }

        private static T[] ComponentsInScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        }

        private static T GetOrAdd<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static void SetReference(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void DesignSizeWorld(Scene scene)
        {
            Canvas canvas = ComponentsInScene<Canvas>(scene).First(item => item.isRootCanvas);
            Image habitat = FindComponent<Image>(scene, "Fondo-habitat");
            habitat.color = Color.white;

            GameObject shade = EnsureRect("HabitatDesignOverlay", canvas.transform);
            Stretch(shade.GetComponent<RectTransform>());
            Image shadeImage = GetOrAdd<Image>(shade);
            shadeImage.color = new Color(0.04f, 0.12f, 0.16f, 0.1f);
            shadeImage.raycastTarget = false;
            shade.transform.SetSiblingIndex(habitat.transform.GetSiblingIndex() + 1);

            RectTransform topPanel = FindRect(scene, "Panel-superior");
            topPanel.anchorMin = new Vector2(0.11f, 1f);
            topPanel.anchorMax = new Vector2(0.89f, 1f);
            topPanel.anchoredPosition = new Vector2(0f, -92f);
            topPanel.sizeDelta = new Vector2(0f, 166f);
            ConfigureMainPanel(topPanel, new Color(0.96f, 1f, 1f, 0.96f), new Color(0.1f, 0.42f, 0.48f, 0.62f));
            TMP_Text title = FindComponent<TMP_Text>(scene, "Titulo");
            title.text = "MUNDO DE LOS TAMAÑOS";
            title.fontSize = 32f;
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.08f, 0.38f, 0.44f, 1f);
            title.alignment = TextAlignmentOptions.Center;
            TMP_Text question = FindComponent<TMP_Text>(scene, "Pregunta");
            question.fontSize = 36f;
            question.fontStyle = FontStyles.Bold;
            question.color = Ink;

            RectTransform safeArea = FindRect(scene, "Area-animales-segura");
            safeArea.anchorMin = new Vector2(0.05f, 0.23f);
            safeArea.anchorMax = new Vector2(0.95f, 0.81f);
            RectTransform leftStage = EnsureAnimalStage(safeArea, "AnimalStageLeft", new Vector2(0.27f, 0.55f), "OPCIÓN 1", new Color(0.18f, 0.58f, 0.78f, 1f));
            RectTransform rightStage = EnsureAnimalStage(safeArea, "AnimalStageRight", new Vector2(0.73f, 0.55f), "OPCIÓN 2", Coral);
            leftStage.SetSiblingIndex(0);
            rightStage.SetSiblingIndex(1);

            StyleAnimalName(scene, "LeftAnimalName", "Elefante", new Color(0.16f, 0.52f, 0.76f, 0.98f));
            StyleAnimalName(scene, "RightAnimalName", "Conejo", new Color(0.92f, 0.43f, 0.38f, 0.98f));

            GameObject feedbackCard = EnsurePanel(canvas.transform, "FeedbackCard", new Color(1f, 1f, 1f, 0.94f));
            RectTransform feedbackCardRect = feedbackCard.transform as RectTransform;
            feedbackCardRect.anchorMin = new Vector2(0.35f, 0.155f);
            feedbackCardRect.anchorMax = new Vector2(0.65f, 0.225f);
            feedbackCardRect.offsetMin = Vector2.zero;
            feedbackCardRect.offsetMax = Vector2.zero;
            TMP_Text feedback = FindComponent<TMP_Text>(scene, "Texto-feedback");
            feedback.text = "Elige con calma";
            feedback.fontSize = 34f;
            feedback.fontStyle = FontStyles.Bold;
            feedback.color = new Color(0.1f, 0.4f, 0.46f, 1f);
            feedback.transform.SetAsLastSibling();
            feedbackCard.transform.SetSiblingIndex(feedback.transform.GetSiblingIndex());

            RectTransform back = FindRect(scene, "Boton-volver");
            SetRect(back, new Vector2(0f, 1f), new Vector2(120f, -53f), new Vector2(190f, 64f));
            StyleButton(back.GetComponent<Button>(), new Color(0.34f, 0.3f, 0.58f, 1f), "VOLVER", 27f);

            RectTransform resultPanel = FindRect(scene, "Panel-resultado");
            Image resultOverlay = resultPanel.GetComponent<Image>();
            resultOverlay.color = new Color(0.03f, 0.08f, 0.13f, 0.78f);
            RectTransform resultContent = resultPanel.Find("Contenido") as RectTransform;
            ConfigureMainPanel(resultContent, new Color(1f, 0.97f, 0.84f, 1f), new Color(0.95f, 0.68f, 0.22f, 0.72f));
            resultContent.sizeDelta = new Vector2(760f, 430f);

            SetOverlayVisibleInEditor(scene);
        }
    }
}
