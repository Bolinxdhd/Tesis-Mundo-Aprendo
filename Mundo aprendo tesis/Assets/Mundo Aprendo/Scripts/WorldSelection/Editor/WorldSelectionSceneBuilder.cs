using System;
using System.Collections.Generic;
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
    public static class WorldSelectionSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/SeleccionMundos.unity";
        private const string GeneratedFolder = "Assets/Mundo Aprendo/WorldSelection/Generated";

        private static readonly Color BackgroundColor = new(0.08f, 0.12f, 0.18f, 1f);
        private static readonly Color PanelColor = new(0.96f, 0.95f, 0.9f, 1f);

        [MenuItem("Tools/Mundo Aprendo/Preparar Seleccion de Mundos")]
        public static void Build()
        {
            EnsureGeneratedFolder();

            Sprite fullStar = LoadOrCreateStarSprite("Star_Full", new Color(1f, 0.82f, 0.18f, 1f), new Color(1f, 0.95f, 0.55f, 1f));
            Sprite emptyStar = LoadOrCreateStarSprite("Star_Empty", new Color(0.42f, 0.43f, 0.48f, 1f), new Color(0.68f, 0.69f, 0.72f, 1f));
            Sprite lockSprite = LoadOrCreateLockSprite();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "SeleccionMundos";

            CreateCamera();
            CreateEventSystem();

            GameObject managerObject = new("WorldSelectionManager", typeof(WorldSelectionManager));
            WorldSelectionManager manager = managerObject.GetComponent<WorldSelectionManager>();

            Canvas canvas = CreateCanvas();
            CreateBackground(canvas.transform);

            GameObject rootPanel = CreatePanel(canvas.transform);
            TMP_Text titleText = CreateText("Titulo", "Selecciona un mundo", rootPanel.transform, 48f, FontStyles.Bold, TextAlignmentOptions.Center);
            titleText.GetComponent<LayoutElement>().preferredHeight = 78f;

            Transform grid = CreateGrid(rootPanel.transform).transform;
            List<WorldSelectionManager.WorldData> worldData = new();

            AddWorldCard(manager, worldData, grid, 0, "Mundo Musical", "MundoMusical", new Color(0.36f, 0.72f, 0.84f, 1f), fullStar, emptyStar, lockSprite);
            AddWorldCard(manager, worldData, grid, 1, "Mundo de Cuentos", "MundoCuentos_VozTest", new Color(0.86f, 0.46f, 0.55f, 1f), fullStar, emptyStar, lockSprite);
            AddWorldCard(manager, worldData, grid, 2, "Mundo de Tamanos", "MundoTamanos", new Color(0.48f, 0.72f, 0.42f, 1f), fullStar, emptyStar, lockSprite);
            AddWorldCard(manager, worldData, grid, 3, "Mundo de Emociones", "MundoEmociones", new Color(0.82f, 0.62f, 0.28f, 1f), fullStar, emptyStar, lockSprite);

            TMP_Text messageText = CreateText("Mensaje", string.Empty, rootPanel.transform, 28f, FontStyles.Bold, TextAlignmentOptions.Center);
            messageText.color = new Color(0.78f, 0.18f, 0.18f, 1f);
            messageText.GetComponent<LayoutElement>().preferredHeight = 54f;

            Transform bottomRow = CreateHorizontalRow("Botones-inferiores", rootPanel.transform, 66f, 20f).transform;
            Button backButton = CreateButton("Button-volver-menu", "Volver al menú", bottomRow, new Vector2(260f, 58f), new Color(0.36f, 0.48f, 0.62f, 1f));
            Button resetButton = CreateButton("Button-reset-progreso", "Reset progreso", bottomRow, new Vector2(260f, 58f), new Color(0.62f, 0.42f, 0.42f, 1f));

            UnityEventTools.AddPersistentListener(backButton.onClick, manager.ReturnToMenu);
            UnityEventTools.AddPersistentListener(resetButton.onClick, manager.ResetProgress);

            AssignManagerReferences(manager, worldData, messageText);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("SeleccionMundos preparada en Assets/Scenes/SeleccionMundos.unity.");
        }

        private static void AddWorldCard(
            WorldSelectionManager manager,
            List<WorldSelectionManager.WorldData> worldData,
            Transform parent,
            int index,
            string worldName,
            string sceneName,
            Color planetColor,
            Sprite fullStar,
            Sprite emptyStar,
            Sprite lockSprite)
        {
            GameObject card = CreateUiObject($"Card-{worldName}", parent, typeof(Image), typeof(Button), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            card.GetComponent<Image>().color = PanelColor;
            card.GetComponent<LayoutElement>().preferredWidth = 390f;
            card.GetComponent<LayoutElement>().preferredHeight = 330f;

            VerticalLayoutGroup layout = card.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 22, 20);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            Button button = card.GetComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;

            Sprite planetSprite = LoadOrCreateCircleSprite($"Planet_{index}", planetColor);
            Image planetImage = CreateImage("Imagen-mundo", card.transform, planetSprite, Color.white, new Vector2(130f, 130f));
            planetImage.preserveAspect = true;

            TMP_Text nameText = CreateText("Nombre", worldName, card.transform, 25f, FontStyles.Bold, TextAlignmentOptions.Center);
            nameText.GetComponent<LayoutElement>().preferredHeight = 44f;

            Transform starRow = CreateHorizontalRow("Estrellas", card.transform, 38f, 8f).transform;
            Image[] stars = new Image[3];
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i] = CreateImage($"Estrella-{i + 1}", starRow, emptyStar, Color.white, new Vector2(34f, 34f));
                stars[i].preserveAspect = true;
            }

            GameObject lockGroup = CreateLockGroup(card.transform, lockSprite);
            UnityEventTools.AddIntPersistentListener(button.onClick, manager.SelectWorld, index);

            worldData.Add(new WorldSelectionManager.WorldData
            {
                worldName = worldName,
                sceneName = sceneName,
                worldButton = button,
                worldNameText = nameText,
                lockedIcon = lockGroup,
                starImages = stars,
                fullStarSprite = fullStar,
                emptyStarSprite = emptyStar
            });
        }

        private static GameObject CreateLockGroup(Transform parent, Sprite lockSprite)
        {
            GameObject group = CreateUiObject("Candado", parent, typeof(Image), typeof(LayoutElement));
            group.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.24f);
            group.GetComponent<LayoutElement>().preferredHeight = 48f;

            HorizontalLayoutGroup layout = group.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 6, 6);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            Image lockImage = CreateImage("Icono", group.transform, lockSprite, Color.white, new Vector2(32f, 32f));
            lockImage.preserveAspect = true;

            TMP_Text text = CreateText("Texto", "Bloqueado", group.transform, 22f, FontStyles.Bold, TextAlignmentOptions.Center);
            text.color = Color.white;
            text.GetComponent<LayoutElement>().preferredWidth = 150f;
            text.GetComponent<LayoutElement>().preferredHeight = 34f;

            return group;
        }

        private static void AssignManagerReferences(WorldSelectionManager manager, IReadOnlyList<WorldSelectionManager.WorldData> worlds, TMP_Text messageText)
        {
            SerializedObject serializedManager = new(manager);
            SerializedProperty worldsProperty = serializedManager.FindProperty("worlds");
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

                SerializedProperty starsProperty = worldProperty.FindPropertyRelative("starImages");
                starsProperty.arraySize = world.starImages.Length;
                for (int starIndex = 0; starIndex < world.starImages.Length; starIndex++)
                {
                    starsProperty.GetArrayElementAtIndex(starIndex).objectReferenceValue = world.starImages[starIndex];
                }
            }

            serializedManager.FindProperty("messageText").objectReferenceValue = messageText;
            serializedManager.FindProperty("menuSceneName").stringValue = "Menu";
            serializedManager.FindProperty("configureWorldButtonsOnAwake").boolValue = false;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
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

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new("Canvas - Seleccion Mundos", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static void CreateBackground(Transform parent)
        {
            GameObject background = CreateUiObject("Fondo", parent, typeof(Image));
            Stretch(background.GetComponent<RectTransform>());
            background.GetComponent<Image>().color = BackgroundColor;

            for (int i = 0; i < 38; i++)
            {
                Image star = CreateImage($"Estrella-fondo-{i + 1}", background.transform, null, new Color(1f, 1f, 1f, 0.65f), new Vector2(6f + i % 3 * 2f, 6f + i % 3 * 2f));
                RectTransform rect = star.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2((i * 0.137f) % 0.96f, (i * 0.271f) % 0.9f);
                rect.anchorMax = rect.anchorMin;
                rect.anchoredPosition = Vector2.zero;
            }
        }

        private static GameObject CreatePanel(Transform parent)
        {
            GameObject panel = CreateUiObject("Panel-seleccion-mundos", parent, typeof(VerticalLayoutGroup));
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1320f, 880f);
            rect.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 24, 24);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return panel;
        }

        private static GameObject CreateGrid(Transform parent)
        {
            GameObject grid = CreateUiObject("Mundos-grid", parent, typeof(GridLayoutGroup), typeof(LayoutElement));
            grid.GetComponent<LayoutElement>().preferredHeight = 700f;

            GridLayoutGroup layout = grid.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(390f, 330f);
            layout.spacing = new Vector2(32f, 28f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 2;
            layout.childAlignment = TextAnchor.MiddleCenter;

            return grid;
        }

        private static GameObject CreateHorizontalRow(string objectName, Transform parent, float height, float spacing)
        {
            GameObject row = CreateUiObject(objectName, parent, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.GetComponent<LayoutElement>().preferredHeight = height;

            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            return row;
        }

        private static Button CreateButton(string objectName, string text, Transform parent, Vector2 size, Color color)
        {
            GameObject buttonObject = CreateUiObject(objectName, parent, typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.GetComponent<Image>().color = color;

            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;

            TMP_Text label = CreateText("Texto", text, buttonObject.transform, 24f, FontStyles.Bold, TextAlignmentOptions.Center);
            label.color = Color.white;
            Stretch(label.GetComponent<RectTransform>());

            return buttonObject.GetComponent<Button>();
        }

        private static TMP_Text CreateText(string objectName, string text, Transform parent, float size, FontStyles style, TextAlignmentOptions alignment)
        {
            GameObject textObject = CreateUiObject(objectName, parent, typeof(TextMeshProUGUI), typeof(LayoutElement));
            TMP_Text tmp = textObject.GetComponent<TMP_Text>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = new Color(0.12f, 0.14f, 0.16f, 1f);
            tmp.raycastTarget = false;
            textObject.GetComponent<LayoutElement>().preferredHeight = size + 12f;
            return tmp;
        }

        private static Image CreateImage(string objectName, Transform parent, Sprite sprite, Color color, Vector2 size)
        {
            GameObject imageObject = CreateUiObject(objectName, parent, typeof(Image), typeof(LayoutElement));
            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;

            LayoutElement layout = imageObject.GetComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            return image;
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

        private static void EnsureGeneratedFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Mundo Aprendo/WorldSelection"))
            {
                AssetDatabase.CreateFolder("Assets/Mundo Aprendo", "WorldSelection");
            }

            if (!AssetDatabase.IsValidFolder(GeneratedFolder))
            {
                AssetDatabase.CreateFolder("Assets/Mundo Aprendo/WorldSelection", "Generated");
            }
        }

        private static Sprite LoadOrCreateCircleSprite(string name, Color color)
        {
            string path = $"{GeneratedFolder}/{name}.png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            const int size = 128;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new(size / 2f, size / 2f);
            float radius = size * 0.46f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    if (distance > radius)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float highlight = Mathf.Clamp01(1f - distance / radius);
                    Color pixel = Color.Lerp(color * 0.82f, color, highlight * 0.65f + 0.25f);
                    pixel.a = 1f;
                    texture.SetPixel(x, y, pixel);
                }
            }

            return SaveTextureAsSprite(texture, path);
        }

        private static Sprite LoadOrCreateStarSprite(string name, Color color, Color highlight)
        {
            string path = $"{GeneratedFolder}/{name}.png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            const int size = 96;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(x, y) - center;
                    float angle = Mathf.Atan2(point.y, point.x) + Mathf.PI / 2f;
                    float radius = point.magnitude / (size * 0.43f);
                    float starRadius = 0.72f + 0.28f * Mathf.Cos(5f * angle);
                    texture.SetPixel(x, y, radius <= starRadius ? Color.Lerp(color, highlight, 1f - radius) : Color.clear);
                }
            }

            return SaveTextureAsSprite(texture, path);
        }

        private static Sprite LoadOrCreateLockSprite()
        {
            string path = $"{GeneratedFolder}/Lock.png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            const int size = 96;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
            Color iconColor = Color.white;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool body = x >= 24 && x <= 72 && y >= 32 && y <= 72;
                    float dx = x - 48f;
                    float dy = y - 34f;
                    float arcDistance = Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - 22f);
                    bool shackle = y >= 28 && y <= 58 && arcDistance <= 4f && x >= 26 && x <= 70;
                    bool cutout = x >= 36 && x <= 60 && y >= 32 && y <= 50;
                    texture.SetPixel(x, y, (body || shackle) && !cutout ? iconColor : Color.clear);
                }
            }

            return SaveTextureAsSprite(texture, path);
        }

        private static Sprite SaveTextureAsSprite(Texture2D texture, string path)
        {
            texture.Apply();
            System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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
    }
}
