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
    public static class SizeWorldSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/MundoTamanos.unity";
        private const string ArtFolder = "Assets/Mundo Aprendo/MundoTamanos/Arte";
        private const string WorldSelectionScenePath = "Assets/Scenes/SeleccionMundos.unity";

        private static readonly Color BackgroundColor = new(0.49f, 0.72f, 0.9f, 1f);
        private static readonly Color PanelColor = new(1f, 0.96f, 0.84f, 0.94f);
        private static readonly Color GreenButton = new(0.24f, 0.68f, 0.38f, 1f);
        private static readonly Color BlueButton = new(0.16f, 0.49f, 0.82f, 1f);

        [MenuItem("Tools/Mundo Aprendo/Preparar Mundo de Tamanos")]
        public static void Build()
        {
            EnsureFolders();
            SizeWorldSprites sprites = LoadOrCreateSprites();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MundoTamanos";

            CreateCamera();
            CreateEventSystem();

            GameObject controllerObject = new("SizeWorldController", typeof(SizeWorldController));
            SizeWorldController controller = controllerObject.GetComponent<SizeWorldController>();

            Canvas canvas = CreateCanvas();
            Image background = CreateImage("Fondo-habitat", canvas.transform, sprites.jungleBackground, Color.white);
            Stretch(background.rectTransform);
            background.preserveAspect = false;

            RectTransform topPanel = CreatePanel("Panel-superior", canvas.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -92f), new Vector2(0f, 184f));
            TMP_Text title = CreateText("Titulo", "Mundo de Tamanos", topPanel, 42f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetAnchors(title.rectTransform, new Vector2(0.08f, 0.44f), new Vector2(0.92f, 0.9f), Vector2.zero, Vector2.zero);

            TMP_Text question = CreateText("Pregunta", "Elige el animal correcto", topPanel, 34f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetAnchors(question.rectTransform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.5f), Vector2.zero, Vector2.zero);

            RectTransform starRow = CreateHorizontalGroup("Estrellas", topPanel, 14f);
            SetAnchors(starRow, new Vector2(0.78f, 0.2f), new Vector2(0.97f, 0.78f), Vector2.zero, Vector2.zero);
            Image[] stars = new Image[3];
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i] = CreateLayoutImage($"Estrella-{i + 1}", starRow, sprites.fullStar, Color.white, new Vector2(64f, 64f));
                stars[i].preserveAspect = true;
            }

            RectTransform safeArea = CreateEmptyRect("Area-animales-segura", canvas.transform);
            SetAnchors(safeArea, new Vector2(0.04f, 0.22f), new Vector2(0.96f, 0.82f), Vector2.zero, Vector2.zero);

            Image leftAnimal = CreateImage("Animal-izquierdo", safeArea, sprites.elephant, Color.white);
            SetFixedRect(leftAnimal.rectTransform, new Vector2(-420f, 0f), new Vector2(320f, 320f));
            leftAnimal.preserveAspect = true;
            leftAnimal.raycastTarget = false;

            Image rightAnimal = CreateImage("Animal-derecho", safeArea, sprites.rabbit, Color.white);
            SetFixedRect(rightAnimal.rectTransform, new Vector2(420f, 0f), new Vector2(280f, 280f));
            rightAnimal.preserveAspect = true;
            rightAnimal.raycastTarget = false;

            Button leftAnimalButton = CreateAnimalOptionOverlay("OpcionAnimalIzquierda", safeArea, new Vector2(0.04f, 0.08f), new Vector2(0.49f, 0.88f));
            Button rightAnimalButton = CreateAnimalOptionOverlay("OpcionAnimalDerecha", safeArea, new Vector2(0.51f, 0.08f), new Vector2(0.96f, 0.88f));

            TMP_Text feedback = CreateText("Texto-feedback", string.Empty, canvas.transform, 40f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetAnchors(feedback.rectTransform, new Vector2(0.22f, 0.15f), new Vector2(0.78f, 0.23f), Vector2.zero, Vector2.zero);
            feedback.color = new Color(0.12f, 0.24f, 0.22f, 1f);

            Button backButton = CreateButton("Boton-volver", "Volver", canvas.transform, new Vector2(210f, 70f), new Color(0.55f, 0.38f, 0.66f, 1f));
            SetAnchors(backButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(132f, -54f), new Vector2(210f, 70f));

            GameObject resultPanel = CreateResultPanel(canvas.transform, controller, out TMP_Text resultText);

            AssignControllerReferences(
                controller,
                sprites,
                background,
                leftAnimal,
                rightAnimal,
                question,
                feedback,
                leftAnimalButton,
                rightAnimalButton,
                safeArea,
                resultPanel,
                resultText,
                backButton,
                stars);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            UpdateWorldSelectionScene();
            AssetDatabase.SaveAssets();
            Debug.Log("MundoTamanos preparado en Assets/Scenes/MundoTamanos.unity.");
        }

        private static void AssignControllerReferences(
            SizeWorldController controller,
            SizeWorldSprites sprites,
            Image background,
            Image leftAnimal,
            Image rightAnimal,
            TMP_Text question,
            TMP_Text feedback,
            Button leftAnimalButton,
            Button rightAnimalButton,
            RectTransform safeArea,
            GameObject resultPanel,
            TMP_Text resultText,
            Button backButton,
            Image[] stars)
        {
            SerializedObject serializedController = new(controller);

            SerializedProperty habitats = serializedController.FindProperty("habitats");
            habitats.arraySize = 3;
            ConfigureHabitat(habitats.GetArrayElementAtIndex(0), "Selva", sprites.jungleBackground, new[]
            {
                new AnimalConfig("Elefante", sprites.elephant, AnimalSizeType.Grande, 0.78f, 1.08f),
                new AnimalConfig("Jirafa", sprites.giraffe, AnimalSizeType.Grande, 0.72f, 1.02f),
                new AnimalConfig("Mono", sprites.monkey, AnimalSizeType.Pequeno, 0.62f, 0.9f),
                new AnimalConfig("Ave", sprites.bird, AnimalSizeType.Pequeno, 0.58f, 0.86f)
            });

            ConfigureHabitat(habitats.GetArrayElementAtIndex(1), "Granja", sprites.farmBackground, new[]
            {
                new AnimalConfig("Caballo", sprites.horse, AnimalSizeType.Grande, 0.76f, 1.08f),
                new AnimalConfig("Vaca", sprites.cow, AnimalSizeType.Grande, 0.76f, 1.08f),
                new AnimalConfig("Conejo", sprites.rabbit, AnimalSizeType.Pequeno, 0.6f, 0.88f),
                new AnimalConfig("Pollito", sprites.chick, AnimalSizeType.Pequeno, 0.56f, 0.82f)
            });

            ConfigureHabitat(habitats.GetArrayElementAtIndex(2), "Oceano", sprites.oceanBackground, new[]
            {
                new AnimalConfig("Ballena", sprites.whale, AnimalSizeType.Grande, 0.78f, 1.08f),
                new AnimalConfig("Tiburon", sprites.shark, AnimalSizeType.Grande, 0.72f, 1.02f),
                new AnimalConfig("Pez", sprites.fish, AnimalSizeType.Pequeno, 0.56f, 0.82f),
                new AnimalConfig("Cangrejo", sprites.crab, AnimalSizeType.Pequeno, 0.56f, 0.82f)
            });

            serializedController.FindProperty("backgroundImage").objectReferenceValue = background;
            serializedController.FindProperty("leftAnimalImage").objectReferenceValue = leftAnimal;
            serializedController.FindProperty("rightAnimalImage").objectReferenceValue = rightAnimal;
            serializedController.FindProperty("questionText").objectReferenceValue = question;
            serializedController.FindProperty("feedbackText").objectReferenceValue = feedback;
            serializedController.FindProperty("leftAnimalButton").objectReferenceValue = leftAnimalButton;
            serializedController.FindProperty("rightAnimalButton").objectReferenceValue = rightAnimalButton;
            serializedController.FindProperty("animalSafeArea").objectReferenceValue = safeArea;
            serializedController.FindProperty("resultPanel").objectReferenceValue = resultPanel;
            serializedController.FindProperty("resultText").objectReferenceValue = resultText;
            serializedController.FindProperty("returnButton").objectReferenceValue = backButton;
            serializedController.FindProperty("fullStarSprite").objectReferenceValue = sprites.fullStar;
            serializedController.FindProperty("emptyStarSprite").objectReferenceValue = sprites.emptyStar;
            serializedController.FindProperty("roundsToComplete").intValue = 5;
            serializedController.FindProperty("returnSceneName").stringValue = "SeleccionMundos";
            serializedController.FindProperty("startAutomatically").boolValue = true;
            serializedController.FindProperty("verticalPositionRange").vector2Value = new Vector2(-170f, 120f);
            serializedController.FindProperty("globalMinScale").floatValue = 0.55f;
            serializedController.FindProperty("globalMaxScale").floatValue = 1.12f;
            serializedController.FindProperty("horizontalPadding").floatValue = 180f;
            serializedController.FindProperty("verticalPadding").floatValue = 70f;

            SerializedProperty starImages = serializedController.FindProperty("starImages");
            starImages.arraySize = stars.Length;
            for (int i = 0; i < stars.Length; i++)
            {
                starImages.GetArrayElementAtIndex(i).objectReferenceValue = stars[i];
            }

            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureHabitat(SerializedProperty habitatProperty, string name, Sprite background, IReadOnlyList<AnimalConfig> animals)
        {
            habitatProperty.FindPropertyRelative("habitatName").stringValue = name;
            habitatProperty.FindPropertyRelative("backgroundSprite").objectReferenceValue = background;

            SerializedProperty animalsProperty = habitatProperty.FindPropertyRelative("animals");
            animalsProperty.arraySize = animals.Count;
            for (int i = 0; i < animals.Count; i++)
            {
                SerializedProperty animalProperty = animalsProperty.GetArrayElementAtIndex(i);
                AnimalConfig animal = animals[i];
                animalProperty.FindPropertyRelative("animalName").stringValue = animal.name;
                animalProperty.FindPropertyRelative("animalSprite").objectReferenceValue = animal.sprite;
                animalProperty.FindPropertyRelative("sizeType").enumValueIndex = (int)animal.sizeType;
                animalProperty.FindPropertyRelative("minScale").floatValue = animal.minScale;
                animalProperty.FindPropertyRelative("maxScale").floatValue = animal.maxScale;
            }
        }

        private static GameObject CreateResultPanel(Transform parent, SizeWorldController controller, out TMP_Text resultText)
        {
            GameObject panel = CreateUiObject("Panel-resultado", parent, typeof(Image));
            Image image = panel.GetComponent<Image>();
            image.color = new Color(0.06f, 0.12f, 0.18f, 0.72f);
            Stretch(panel.GetComponent<RectTransform>());

            GameObject content = CreateUiObject("Contenido", panel.transform, typeof(Image), typeof(VerticalLayoutGroup));
            Image contentImage = content.GetComponent<Image>();
            contentImage.color = new Color(1f, 0.97f, 0.88f, 1f);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            SetFixedRect(contentRect, Vector2.zero, new Vector2(700f, 390f));

            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(44, 44, 44, 44);
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            resultText = CreateText("Texto-resultado", "Actividad completada", content.transform, 38f, FontStyles.Bold, TextAlignmentOptions.Center);
            resultText.GetComponent<LayoutElement>().preferredWidth = 620f;
            resultText.GetComponent<LayoutElement>().preferredHeight = 130f;

            RectTransform row = CreateHorizontalGroup("Botones-resultado", content.transform, 26f);
            row.GetComponent<LayoutElement>().preferredWidth = 620f;
            row.GetComponent<LayoutElement>().preferredHeight = 96f;

            Button restart = CreateButton("Boton-reintentar", "Reintentar", row, new Vector2(260f, 82f), BlueButton);
            Button back = CreateButton("Boton-volver-seleccion", "Volver", row, new Vector2(260f, 82f), new Color(0.55f, 0.38f, 0.66f, 1f));

            UnityEventTools.AddPersistentListener(restart.onClick, controller.StartActivity);
            UnityEventTools.AddPersistentListener(back.onClick, controller.ReturnToWorldSelection);

            panel.SetActive(false);
            return panel;
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new("Canvas - Mundo Tamanos", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystem = new("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private static Button CreateButton(string objectName, string label, Transform parent, Vector2 size, Color color)
        {
            GameObject buttonObject = CreateUiObject(objectName, parent, typeof(Image), typeof(Button), typeof(LayoutElement));
            Image image = buttonObject.GetComponent<Image>();
            image.color = color;

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.2f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.15f);
            button.colors = colors;

            TMP_Text text = CreateText("Texto", label, buttonObject.transform, 34f, FontStyles.Bold, TextAlignmentOptions.Center);
            text.color = Color.white;
            Stretch(text.rectTransform);
            return button;
        }

        private static Button CreateAnimalOptionOverlay(string objectName, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject overlay = CreateUiObject(objectName, parent, typeof(Image), typeof(Button), typeof(UIButtonFeedback));
            RectTransform rect = overlay.GetComponent<RectTransform>();
            SetAnchors(rect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            Image image = overlay.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.001f);
            image.raycastTarget = true;
            Button button = overlay.GetComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.86f, 0.9f, 1f, 0.9f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            return button;
        }

        private static TMP_Text CreateText(string objectName, string text, Transform parent, float fontSize, FontStyles style, TextAlignmentOptions alignment)
        {
            GameObject textObject = CreateUiObject(objectName, parent, typeof(TextMeshProUGUI), typeof(LayoutElement));
            TMP_Text tmp = textObject.GetComponent<TMP_Text>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = new Color(0.12f, 0.13f, 0.17f, 1f);
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;

            LayoutElement layout = textObject.GetComponent<LayoutElement>();
            layout.preferredHeight = fontSize + 20f;
            return tmp;
        }

        private static Image CreateImage(string objectName, Transform parent, Sprite sprite, Color color)
        {
            GameObject imageObject = CreateUiObject(objectName, parent, typeof(Image));
            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return image;
        }

        private static Image CreateLayoutImage(string objectName, Transform parent, Sprite sprite, Color color, Vector2 size)
        {
            GameObject imageObject = CreateUiObject(objectName, parent, typeof(Image), typeof(LayoutElement));
            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            LayoutElement layout = imageObject.GetComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;
            return image;
        }

        private static RectTransform CreatePanel(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject panel = CreateUiObject(objectName, parent, typeof(Image));
            Image image = panel.GetComponent<Image>();
            image.color = PanelColor;
            RectTransform rect = panel.GetComponent<RectTransform>();
            SetAnchors(rect, anchorMin, anchorMax, anchoredPosition, size);
            return rect;
        }

        private static RectTransform CreateHorizontalGroup(string objectName, Transform parent, float spacing)
        {
            GameObject row = CreateUiObject(objectName, parent, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return row.GetComponent<RectTransform>();
        }

        private static RectTransform CreateEmptyRect(string objectName, Transform parent)
        {
            GameObject rectObject = CreateUiObject(objectName, parent);
            return rectObject.GetComponent<RectTransform>();
        }

        private static GameObject CreateUiObject(string objectName, Transform parent, params Type[] components)
        {
            GameObject uiObject = new(objectName, typeof(RectTransform), typeof(CanvasRenderer));
            foreach (Type component in components)
            {
                if (uiObject.GetComponent(component) == null)
                {
                    uiObject.AddComponent(component);
                }
            }

            uiObject.transform.SetParent(parent, false);
            return uiObject;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetFixedRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            if (size == Vector2.zero)
            {
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }

        private static void UpdateWorldSelectionScene()
        {
            if (!File.Exists(WorldSelectionScenePath)) return;

            Scene activeScene = EditorSceneManager.GetActiveScene();
            Scene selectionScene = EditorSceneManager.OpenScene(WorldSelectionScenePath, OpenSceneMode.Single);
            WorldSelectionManager manager = null;
            foreach (GameObject root in selectionScene.GetRootGameObjects())
            {
                manager = root.GetComponentInChildren<WorldSelectionManager>(true);
                if (manager != null) break;
            }
            if (manager != null)
            {
                SerializedObject serializedManager = new(manager);
                SerializedProperty worlds = serializedManager.FindProperty("worlds");
                if (worlds != null && worlds.arraySize > 2)
                {
                    SerializedProperty world = worlds.GetArrayElementAtIndex(2);
                    world.FindPropertyRelative("sceneName").stringValue = "MundoTamanos";
                    world.FindPropertyRelative("worldName").stringValue = "Mundo de Tamanos";
                    serializedManager.ApplyModifiedPropertiesWithoutUndo();
                    EditorSceneManager.MarkSceneDirty(selectionScene);
                    EditorSceneManager.SaveScene(selectionScene);
                }
            }

            if (!string.IsNullOrWhiteSpace(activeScene.path))
            {
                EditorSceneManager.OpenScene(activeScene.path, OpenSceneMode.Single);
            }
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = new(EditorBuildSettings.scenes);
            foreach (EditorBuildSettingsScene scene in scenes)
            {
                if (scene.path == scenePath) return;
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Mundo Aprendo", "MundoTamanos");
            EnsureFolder("Assets/Mundo Aprendo/MundoTamanos", "Arte");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static SizeWorldSprites LoadOrCreateSprites()
        {
            return new SizeWorldSprites
            {
                fullStar = LoadSprite("Assets/Mundo Aprendo/Imagenes/Prefabs/Star_Full.png"),
                emptyStar = LoadSprite("Assets/Mundo Aprendo/Imagenes/Prefabs/Star_Empty.png"),
                jungleBackground = CreateBackgroundSprite("Habitat_Selva", new Color(0.39f, 0.73f, 0.44f, 1f), new Color(0.12f, 0.48f, 0.3f, 1f), 0),
                farmBackground = CreateBackgroundSprite("Habitat_Granja", new Color(0.75f, 0.9f, 0.58f, 1f), new Color(0.85f, 0.58f, 0.32f, 1f), 1),
                oceanBackground = CreateBackgroundSprite("Habitat_Oceano", new Color(0.35f, 0.72f, 0.95f, 1f), new Color(0.08f, 0.35f, 0.74f, 1f), 2),
                elephant = CreateAnimalSprite("Animal_Elefante", new Color(0.56f, 0.6f, 0.64f, 1f), new Color(0.36f, 0.39f, 0.43f, 1f), AnimalShape.Elephant),
                giraffe = CreateAnimalSprite("Animal_Jirafa", new Color(0.95f, 0.72f, 0.23f, 1f), new Color(0.48f, 0.24f, 0.12f, 1f), AnimalShape.Giraffe),
                monkey = CreateAnimalSprite("Animal_Mono", new Color(0.52f, 0.31f, 0.16f, 1f), new Color(0.9f, 0.66f, 0.42f, 1f), AnimalShape.Monkey),
                bird = CreateAnimalSprite("Animal_Ave", new Color(0.18f, 0.5f, 0.9f, 1f), new Color(1f, 0.72f, 0.2f, 1f), AnimalShape.Bird),
                horse = CreateAnimalSprite("Animal_Caballo", new Color(0.55f, 0.31f, 0.16f, 1f), new Color(0.2f, 0.12f, 0.07f, 1f), AnimalShape.Horse),
                cow = CreateAnimalSprite("Animal_Vaca", new Color(0.96f, 0.94f, 0.88f, 1f), new Color(0.16f, 0.16f, 0.16f, 1f), AnimalShape.Cow),
                rabbit = CreateAnimalSprite("Animal_Conejo", new Color(0.94f, 0.94f, 0.9f, 1f), new Color(0.98f, 0.58f, 0.68f, 1f), AnimalShape.Rabbit),
                chick = CreateAnimalSprite("Animal_Pollito", new Color(1f, 0.84f, 0.18f, 1f), new Color(1f, 0.45f, 0.14f, 1f), AnimalShape.Chick),
                whale = CreateAnimalSprite("Animal_Ballena", new Color(0.17f, 0.45f, 0.78f, 1f), new Color(0.72f, 0.88f, 1f, 1f), AnimalShape.Whale),
                shark = CreateAnimalSprite("Animal_Tiburon", new Color(0.42f, 0.52f, 0.6f, 1f), new Color(0.88f, 0.94f, 1f, 1f), AnimalShape.Shark),
                fish = CreateAnimalSprite("Animal_Pez", new Color(1f, 0.46f, 0.2f, 1f), new Color(1f, 0.83f, 0.24f, 1f), AnimalShape.Fish),
                crab = CreateAnimalSprite("Animal_Cangrejo", new Color(0.88f, 0.22f, 0.16f, 1f), new Color(1f, 0.56f, 0.26f, 1f), AnimalShape.Crab)
            };
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite CreateBackgroundSprite(string name, Color top, Color bottom, int variant)
        {
            string path = $"{ArtFolder}/{name}.png";
            Sprite existing = LoadSprite(path);
            if (existing != null) return existing;

            const int width = 960;
            const int height = 540;
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                float t = y / (float)(height - 1);
                Color color = Color.Lerp(bottom, top, t);
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }

            if (variant == 0)
            {
                DrawCircle(texture, 160, 160, 90, new Color(0.08f, 0.36f, 0.22f, 1f));
                DrawCircle(texture, 300, 190, 125, new Color(0.1f, 0.43f, 0.24f, 1f));
                DrawCircle(texture, 720, 170, 130, new Color(0.08f, 0.38f, 0.22f, 1f));
                DrawRect(texture, 0, 0, width, 120, new Color(0.18f, 0.5f, 0.2f, 1f));
            }
            else if (variant == 1)
            {
                DrawRect(texture, 0, 0, width, 150, new Color(0.42f, 0.72f, 0.28f, 1f));
                DrawRect(texture, 90, 120, 250, 130, new Color(0.68f, 0.34f, 0.18f, 1f));
                DrawRect(texture, 120, 250, 190, 130, new Color(0.88f, 0.24f, 0.2f, 1f));
                DrawTriangle(texture, new Vector2(95, 250), new Vector2(335, 250), new Vector2(215, 370), new Color(0.5f, 0.16f, 0.12f, 1f));
            }
            else
            {
                DrawRect(texture, 0, 0, width, 170, new Color(0.05f, 0.27f, 0.62f, 1f));
                DrawCircle(texture, 120, 330, 18, new Color(0.84f, 0.96f, 1f, 0.5f));
                DrawCircle(texture, 220, 270, 26, new Color(0.84f, 0.96f, 1f, 0.5f));
                DrawCircle(texture, 790, 310, 22, new Color(0.84f, 0.96f, 1f, 0.5f));
                DrawRect(texture, 0, 0, width, 60, new Color(0.86f, 0.72f, 0.38f, 1f));
            }

            return SaveTextureAsSprite(texture, path, 100f);
        }

        private static Sprite CreateAnimalSprite(string name, Color primary, Color secondary, AnimalShape shape)
        {
            string path = $"{ArtFolder}/{name}.png";
            Sprite existing = LoadSprite(path);
            if (existing != null) return existing;

            const int size = 512;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
            Clear(texture);

            switch (shape)
            {
                case AnimalShape.Elephant:
                    DrawCircle(texture, 236, 270, 118, primary);
                    DrawCircle(texture, 150, 282, 70, Color.Lerp(primary, secondary, 0.25f));
                    DrawCircle(texture, 330, 308, 64, Color.Lerp(primary, Color.white, 0.12f));
                    DrawRect(texture, 306, 180, 54, 110, primary);
                    DrawCircle(texture, 350, 172, 28, primary);
                    DrawLegs(texture, primary);
                    DrawEye(texture, 348, 330);
                    break;
                case AnimalShape.Giraffe:
                    DrawRect(texture, 164, 112, 170, 128, primary);
                    DrawRect(texture, 298, 200, 54, 176, primary);
                    DrawCircle(texture, 342, 386, 46, primary);
                    DrawLegs(texture, primary);
                    DrawSpots(texture, secondary);
                    DrawEye(texture, 358, 398);
                    break;
                case AnimalShape.Monkey:
                    DrawCircle(texture, 250, 260, 96, primary);
                    DrawCircle(texture, 250, 344, 76, primary);
                    DrawCircle(texture, 250, 336, 48, secondary);
                    DrawCircle(texture, 180, 342, 34, primary);
                    DrawCircle(texture, 320, 342, 34, primary);
                    DrawCircle(texture, 344, 230, 46, primary);
                    DrawEye(texture, 228, 360);
                    DrawEye(texture, 272, 360);
                    break;
                case AnimalShape.Bird:
                    DrawCircle(texture, 250, 260, 88, primary);
                    DrawCircle(texture, 316, 326, 48, primary);
                    DrawTriangle(texture, new Vector2(354, 328), new Vector2(414, 350), new Vector2(354, 372), secondary);
                    DrawCircle(texture, 212, 260, 54, Color.Lerp(primary, Color.white, 0.2f));
                    DrawEye(texture, 328, 342);
                    break;
                case AnimalShape.Horse:
                    DrawRect(texture, 142, 190, 220, 118, primary);
                    DrawRect(texture, 334, 270, 52, 100, primary);
                    DrawCircle(texture, 368, 374, 50, primary);
                    DrawTriangle(texture, new Vector2(338, 410), new Vector2(354, 458), new Vector2(374, 408), secondary);
                    DrawLegs(texture, primary);
                    DrawEye(texture, 384, 384);
                    break;
                case AnimalShape.Cow:
                    DrawRect(texture, 132, 194, 230, 120, primary);
                    DrawCircle(texture, 370, 326, 58, primary);
                    DrawCircle(texture, 190, 260, 28, secondary);
                    DrawCircle(texture, 280, 292, 32, secondary);
                    DrawLegs(texture, primary);
                    DrawEye(texture, 386, 342);
                    break;
                case AnimalShape.Rabbit:
                    DrawCircle(texture, 248, 250, 86, primary);
                    DrawCircle(texture, 274, 338, 62, primary);
                    DrawRect(texture, 244, 370, 26, 92, primary);
                    DrawRect(texture, 292, 370, 26, 92, primary);
                    DrawCircle(texture, 244, 238, 28, Color.Lerp(primary, secondary, 0.18f));
                    DrawEye(texture, 292, 354);
                    break;
                case AnimalShape.Chick:
                    DrawCircle(texture, 248, 250, 88, primary);
                    DrawCircle(texture, 280, 338, 54, primary);
                    DrawTriangle(texture, new Vector2(318, 334), new Vector2(374, 354), new Vector2(318, 374), secondary);
                    DrawEye(texture, 294, 354);
                    break;
                case AnimalShape.Whale:
                    DrawCircle(texture, 230, 260, 112, primary);
                    DrawRect(texture, 226, 188, 150, 130, primary);
                    DrawTriangle(texture, new Vector2(372, 260), new Vector2(468, 326), new Vector2(468, 194), primary);
                    DrawRect(texture, 168, 186, 120, 44, secondary);
                    DrawEye(texture, 176, 292);
                    break;
                case AnimalShape.Shark:
                    DrawTriangle(texture, new Vector2(92, 256), new Vector2(374, 348), new Vector2(374, 164), primary);
                    DrawTriangle(texture, new Vector2(352, 256), new Vector2(462, 324), new Vector2(462, 188), primary);
                    DrawTriangle(texture, new Vector2(250, 330), new Vector2(306, 438), new Vector2(326, 294), primary);
                    DrawRect(texture, 166, 214, 190, 40, secondary);
                    DrawEye(texture, 174, 282);
                    break;
                case AnimalShape.Fish:
                    DrawCircle(texture, 236, 260, 92, primary);
                    DrawTriangle(texture, new Vector2(310, 260), new Vector2(430, 340), new Vector2(430, 180), secondary);
                    DrawCircle(texture, 202, 278, 34, Color.Lerp(primary, Color.white, 0.25f));
                    DrawEye(texture, 190, 294);
                    break;
                case AnimalShape.Crab:
                    DrawCircle(texture, 252, 250, 78, primary);
                    DrawCircle(texture, 142, 320, 42, primary);
                    DrawCircle(texture, 362, 320, 42, primary);
                    DrawRect(texture, 124, 282, 110, 20, secondary);
                    DrawRect(texture, 276, 282, 110, 20, secondary);
                    DrawEye(texture, 230, 318);
                    DrawEye(texture, 274, 318);
                    break;
            }

            return SaveTextureAsSprite(texture, path, 100f);
        }

        private static Sprite SaveTextureAsSprite(Texture2D texture, string path, float pixelsPerUnit)
        {
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            return LoadSprite(path);
        }

        private static void Clear(Texture2D texture)
        {
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        private static void DrawRect(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            int minX = Mathf.Clamp(x, 0, texture.width - 1);
            int maxX = Mathf.Clamp(x + width, 0, texture.width);
            int minY = Mathf.Clamp(y, 0, texture.height - 1);
            int maxY = Mathf.Clamp(y + height, 0, texture.height);
            for (int yy = minY; yy < maxY; yy++)
            {
                for (int xx = minX; xx < maxX; xx++)
                {
                    texture.SetPixel(xx, yy, color);
                }
            }
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
                    if (dx * dx + dy * dy <= radiusSquared)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
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
                    if (IsInsideTriangle(p, a, b, c))
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private static bool IsInsideTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b);
            float d2 = Sign(p, b, c);
            float d3 = Sign(p, c, a);
            bool hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNegative && hasPositive);
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        private static void DrawEye(Texture2D texture, int x, int y)
        {
            DrawCircle(texture, x, y, 10, Color.white);
            DrawCircle(texture, x + 2, y, 5, Color.black);
        }

        private static void DrawLegs(Texture2D texture, Color color)
        {
            DrawRect(texture, 160, 96, 34, 116, color);
            DrawRect(texture, 286, 96, 34, 116, color);
            DrawRect(texture, 150, 88, 56, 22, Color.Lerp(color, Color.black, 0.14f));
            DrawRect(texture, 276, 88, 56, 22, Color.Lerp(color, Color.black, 0.14f));
        }

        private static void DrawSpots(Texture2D texture, Color color)
        {
            DrawCircle(texture, 198, 198, 20, color);
            DrawCircle(texture, 274, 178, 24, color);
            DrawCircle(texture, 320, 264, 18, color);
            DrawCircle(texture, 346, 386, 14, color);
        }

        private readonly struct AnimalConfig
        {
            public AnimalConfig(string name, Sprite sprite, AnimalSizeType sizeType, float minScale, float maxScale)
            {
                this.name = name;
                this.sprite = sprite;
                this.sizeType = sizeType;
                this.minScale = minScale;
                this.maxScale = maxScale;
            }

            public readonly string name;
            public readonly Sprite sprite;
            public readonly AnimalSizeType sizeType;
            public readonly float minScale;
            public readonly float maxScale;
        }

        private sealed class SizeWorldSprites
        {
            public Sprite fullStar;
            public Sprite emptyStar;
            public Sprite jungleBackground;
            public Sprite farmBackground;
            public Sprite oceanBackground;
            public Sprite elephant;
            public Sprite giraffe;
            public Sprite monkey;
            public Sprite bird;
            public Sprite horse;
            public Sprite cow;
            public Sprite rabbit;
            public Sprite chick;
            public Sprite whale;
            public Sprite shark;
            public Sprite fish;
            public Sprite crab;
        }

        private enum AnimalShape
        {
            Elephant,
            Giraffe,
            Monkey,
            Bird,
            Horse,
            Cow,
            Rabbit,
            Chick,
            Whale,
            Shark,
            Fish,
            Crab
        }
    }
}
