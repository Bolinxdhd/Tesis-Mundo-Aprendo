#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Bolin;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bolin.Editor
{
    /// <summary>
    /// Rebuilds only the persisted world-selection presentation and its Inspector wiring.
    /// It deliberately leaves runtime progress logic, the Canvas, the EventSystem and the
    /// SceneTransitionOverlay untouched.
    /// </summary>
    public static class WorldSelectionProgressRepairSetup
    {
        private const string ScenePath = "Assets/Scenes/SeleccionMundos.unity";

        private const string SpaceBackgroundPath = "Assets/Space_Exploration_GUI_Kit/Background_Images/extra large/home-background-extra-large.png";
        private const string FallbackBackgroundPath = "Assets/Mundo Aprendo/Imagenes/Baner fondo.png";
        private const string PanelSpritePath = "Assets/Cartoon UI/Panels/Panel Light.png";
        private const string ButtonSkybluePath = "Assets/Cartoon UI/Buttons/Long Round/Long Round Skyblue.png";
        private const string ButtonOrangePath = "Assets/Cartoon UI/Buttons/Long Round/Long Round Orange.png";
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

        private static readonly string[] WorldNames =
        {
            "Mundo Musical",
            "Mundo de Cuentos",
            "Mundo de los Tama\u00f1os",
            "Mundo de las Emociones"
        };

        private static readonly string[] WorldScenes =
        {
            "MundoMusical",
            "MundoCuentos_VozTest",
            "MundoTamanos",
            "MundoEmociones"
        };

        private static readonly Vector2[] WorldPositions =
        {
            // Original authored planet layout from SeleccionMundos. Keep these
            // presentation coordinates independent from the progress wiring.
            new Vector2(-545f, 145f),
            new Vector2(-175f, -75f),
            new Vector2(225f, 145f),
            new Vector2(560f, -75f)
        };

        private static readonly Color[] WorldColors =
        {
            new Color(0.45f, 0.73f, 0.88f, 1f),
            new Color(0.90f, 0.58f, 0.68f, 1f),
            new Color(0.55f, 0.78f, 0.55f, 1f),
            new Color(0.58f, 0.50f, 0.78f, 1f)
        };

        private static readonly Color TextDark = new Color(0.21f, 0.18f, 0.34f, 1f);
        private static readonly Color TextLight = new Color(1f, 0.98f, 0.90f, 1f);

        [MenuItem("Mundo Aprendo/Reparar Seleccion Mundos y Progreso")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Canvas canvas = ComponentsInScene<Canvas>(scene).FirstOrDefault(item => item.isRootCanvas);
            if (canvas == null)
            {
                throw new InvalidOperationException("SeleccionMundos no tiene un Canvas raiz que se pueda preservar.");
            }

            ConfigureCanvasScaler(canvas);
            RemoveConflictingSelectionUi(scene);

            WorldSelectionManager manager = GetOrCreateManager(scene);
            RectTransform root = CreateUiObject("VisualSeleccionMundos", canvas.transform).GetComponent<RectTransform>();
            Stretch(root);
            root.SetAsLastSibling();

            Sprite backgroundSprite = LoadSprite(SpaceBackgroundPath) ?? LoadSprite(FallbackBackgroundPath);
            Image background = CreateImage("FondoSeleccionEspacial", root, backgroundSprite, Color.white, true);
            Stretch(background.rectTransform);
            background.preserveAspect = false;

            TMP_Text title = CreateText("TituloSeleccionMundos", "Elige un mundo", root, 54f, FontStyles.Bold, TextAlignmentOptions.Center, TextLight);
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 445f), new Vector2(860f, 80f));

            TMP_Text message = CreateText("MensajeSeleccionMundos", string.Empty, root, 28f, FontStyles.Bold, TextAlignmentOptions.Center, TextLight);
            SetRect(message.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -415f), new Vector2(1040f, 54f));
            message.gameObject.SetActive(false);

            Sprite fullStar = LoadSprite(FullStarPath);
            Sprite emptyStar = LoadSprite(EmptyStarPath);
            List<WorldSelectionManager.WorldData> worldData = new List<WorldSelectionManager.WorldData>();
            List<WorldSelectionVisuals.WorldVisual> visualData = new List<WorldSelectionVisuals.WorldVisual>();
            for (int index = 0; index < WorldNames.Length; index++)
            {
                CreateWorldPlanet(root, manager, index, fullStar, emptyStar, worldData, visualData);
            }

            Button backButton = CreateButton("BotonVolverMenu", "VOLVER AL MENU", root, new Vector2(-190f, -470f), new Vector2(300f, 76f), LoadSprite(ButtonSkybluePath), TextLight);
            ClearAllListeners(backButton.onClick);
            UnityEventTools.AddPersistentListener(backButton.onClick, manager.ReturnToMenu);

            Button resetButton = CreateButton("BotonReiniciarProgreso", "REINICIAR PROGRESO", root, new Vector2(190f, -470f), new Vector2(350f, 76f), LoadSprite(ButtonOrangePath), TextLight);
            ClearAllListeners(resetButton.onClick);
            UnityEventTools.AddPersistentListener(resetButton.onClick, manager.RequestResetProgress);

            GameObject confirmation = CreateResetConfirmation(root, manager);
            ConfigureManager(manager, worldData, message, confirmation);
            ConfigureVisuals(root.gameObject.AddComponent<WorldSelectionVisuals>(), visualData);

            // Keep the already-existing transition overlay over the rebuilt UI.
            Transform overlay = canvas.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == "SceneTransitionOverlay");
            if (overlay != null) overlay.SetAsLastSibling();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("No se pudo guardar la reparacion de SeleccionMundos.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("WorldSelectionProgressRepairSetup: SeleccionMundos reparada y guardada.");
        }

        private static void RemoveConflictingSelectionUi(Scene scene)
        {
            // These are presentation-only roots from earlier builders. Removing them prevents
            // stale star displays and persistent listeners from remaining behind the new UI.
            string[] names =
            {
                "VisualSeleccionMundos",
                "Panel-seleccion-mundos",
                "ResetProgressConfirmation",
                "PanelConfirmacionReset"
            };

            foreach (string objectName in names)
            {
                GameObject[] targets = ComponentsInScene<Transform>(scene)
                    .Where(item => string.Equals(item.name, objectName, StringComparison.Ordinal))
                    .Select(item => item.gameObject)
                    .Distinct()
                    .ToArray();

                foreach (GameObject target in targets)
                {
                    if (target != null) UnityEngine.Object.DestroyImmediate(target);
                }
            }
        }

        private static WorldSelectionManager GetOrCreateManager(Scene scene)
        {
            WorldSelectionManager manager = ComponentsInScene<WorldSelectionManager>(scene).FirstOrDefault();
            if (manager != null) return manager;

            GameObject managerObject = new GameObject("WorldSelectionManager");
            SceneManager.MoveGameObjectToScene(managerObject, scene);
            return managerObject.AddComponent<WorldSelectionManager>();
        }

        private static void CreateWorldPlanet(
            RectTransform parent,
            WorldSelectionManager manager,
            int index,
            Sprite fullStar,
            Sprite emptyStar,
            ICollection<WorldSelectionManager.WorldData> worldData,
            ICollection<WorldSelectionVisuals.WorldVisual> visualData)
        {
            Button button = CreateButton(
                "Planeta-" + index + "-" + WorldNames[index],
                string.Empty,
                parent,
                WorldPositions[index],
                new Vector2(330f, 330f),
                LoadSprite(PlanetPaths[index]),
                TextLight);

            Image planetImage = button.image;
            planetImage.color = WorldColors[index];
            planetImage.preserveAspect = true;
            planetImage.type = Image.Type.Simple;

            CanvasGroup canvasGroup = button.gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = button.gameObject.AddComponent<CanvasGroup>();

            RectTransform card = button.GetComponent<RectTransform>();
            Image icon = CreateImage("IconoMundo", card, LoadSprite(WorldIconPaths[index]), Color.white, false);
            SetRect(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 46f), new Vector2(86f, 86f));
            icon.preserveAspect = true;

            TMP_Text worldTitle = CreateText("NombreMundo", WorldNames[index], card, 28f, FontStyles.Bold, TextAlignmentOptions.Center, TextLight);
            SetRect(worldTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -104f), new Vector2(300f, 62f));

            UIStarDisplay starDisplay = CreateStarDisplay(card, fullStar, emptyStar, out Image[] stars);

            GameObject lockGroup = CreateLockGroup(card);
            lockGroup.SetActive(index != 0);

            ClearAllListeners(button.onClick);
            UnityEventTools.AddIntPersistentListener(button.onClick, manager.SelectWorld, index);

            worldData.Add(new WorldSelectionManager.WorldData
            {
                worldName = WorldNames[index],
                sceneName = WorldScenes[index],
                worldButton = button,
                worldNameText = worldTitle,
                lockedIcon = lockGroup,
                starImages = stars,
                fullStarSprite = fullStar,
                emptyStarSprite = emptyStar,
                starDisplay = starDisplay,
                progressGraphic = planetImage,
                baseProgressColor = WorldColors[index]
            });

            visualData.Add(new WorldSelectionVisuals.WorldVisual
            {
                visualRoot = card,
                canvasGroup = canvasGroup,
                planetImage = planetImage,
                lockedGroup = lockGroup,
                unlockedColor = WorldColors[index],
                lockedColor = new Color(WorldColors[index].r * 0.72f, WorldColors[index].g * 0.72f, WorldColors[index].b * 0.72f, 1f)
            });
        }

        private static UIStarDisplay CreateStarDisplay(RectTransform parent, Sprite fullStar, Sprite emptyStar, out Image[] stars)
        {
            RectTransform group = CreateUiObject("Estrellas", parent).GetComponent<RectTransform>();
            SetRect(group, new Vector2(0.5f, 0.5f), new Vector2(0f, -146f), new Vector2(176f, 48f));

            stars = new Image[3];
            for (int index = 0; index < stars.Length; index++)
            {
                stars[index] = CreateImage("Estrella-" + (index + 1), group, emptyStar, Color.white, false);
                SetRect(stars[index].rectTransform, new Vector2(0.2f + index * 0.3f, 0.5f), Vector2.zero, new Vector2(46f, 46f));
                stars[index].preserveAspect = true;
            }

            UIStarDisplay display = group.gameObject.AddComponent<UIStarDisplay>();
            SerializedObject serialized = new SerializedObject(display);
            SerializedProperty starProperty = serialized.FindProperty("stars");
            starProperty.arraySize = stars.Length;
            for (int index = 0; index < stars.Length; index++)
            {
                starProperty.GetArrayElementAtIndex(index).objectReferenceValue = stars[index];
            }

            serialized.FindProperty("earnedSprite").objectReferenceValue = fullStar;
            serialized.FindProperty("unearnedSprite").objectReferenceValue = emptyStar;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return display;
        }

        private static GameObject CreateLockGroup(RectTransform parent)
        {
            GameObject lockGroup = CreateUiObject("EstadoBloqueado", parent);
            RectTransform rect = lockGroup.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(220f, 92f));

            Image plate = lockGroup.AddComponent<Image>();
            plate.sprite = LoadSprite(PanelSpritePath) ?? BuiltinUiSprite();
            plate.type = Image.Type.Sliced;
            plate.color = new Color(0.12f, 0.10f, 0.20f, 0.84f);
            plate.raycastTarget = false;

            Image lockIcon = CreateImage("Candado", rect, LoadSprite(LockPath), Color.white, false);
            SetRect(lockIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-66f, 0f), new Vector2(44f, 54f));
            lockIcon.preserveAspect = true;

            TMP_Text text = CreateText("TextoBloqueado", "Completa el mundo anterior", rect, 18f, FontStyles.Bold, TextAlignmentOptions.Center, TextLight);
            SetRect(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(25f, 0f), new Vector2(150f, 66f));
            return lockGroup;
        }

        private static GameObject CreateResetConfirmation(RectTransform parent, WorldSelectionManager manager)
        {
            GameObject overlay = CreateUiObject("PanelConfirmacionReset", parent);
            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            Stretch(overlayRect);

            Image blocker = overlay.AddComponent<Image>();
            blocker.color = new Color(0.04f, 0.05f, 0.12f, 0.76f);

            CanvasGroup canvasGroup = overlay.AddComponent<CanvasGroup>();
            UIPanelTransition transition = overlay.AddComponent<UIPanelTransition>();
            ConfigureTransition(transition, canvasGroup, overlayRect);

            RectTransform panel = CreateUiObject("DialogoReset", overlay.transform).GetComponent<RectTransform>();
            SetRect(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(650f, 350f));
            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = LoadSprite(PanelSpritePath) ?? BuiltinUiSprite();
            panelImage.type = Image.Type.Sliced;
            panelImage.color = Color.white;

            TMP_Text title = CreateText("TituloConfirmacion", "Reiniciar el progreso?", panel, 36f, FontStyles.Bold, TextAlignmentOptions.Center, TextDark);
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(530f, 64f));

            TMP_Text body = CreateText("TextoConfirmacion", "Se borraran las estrellas y los mundos volveran a bloquearse.", panel, 24f, FontStyles.Normal, TextAlignmentOptions.Center, TextDark);
            SetRect(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), new Vector2(530f, 90f));

            Button confirm = CreateButton("BotonConfirmarReset", "SI, REINICIAR", panel, new Vector2(-145f, -100f), new Vector2(255f, 70f), LoadSprite(ButtonOrangePath), TextLight);
            ClearAllListeners(confirm.onClick);
            UnityEventTools.AddPersistentListener(confirm.onClick, manager.ResetProgress);

            Button cancel = CreateButton("BotonCancelarReset", "CANCELAR", panel, new Vector2(145f, -100f), new Vector2(235f, 70f), LoadSprite(ButtonSkybluePath), TextLight);
            ClearAllListeners(cancel.onClick);
            UnityEventTools.AddPersistentListener(cancel.onClick, manager.CancelResetProgress);

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            overlay.SetActive(false);
            return overlay;
        }

        private static void ConfigureManager(
            WorldSelectionManager manager,
            IReadOnlyList<WorldSelectionManager.WorldData> worlds,
            TMP_Text message,
            GameObject confirmation)
        {
            SerializedObject serialized = new SerializedObject(manager);
            SerializedProperty worldArray = serialized.FindProperty("worlds");
            worldArray.arraySize = worlds.Count;

            for (int index = 0; index < worlds.Count; index++)
            {
                WorldSelectionManager.WorldData world = worlds[index];
                SerializedProperty item = worldArray.GetArrayElementAtIndex(index);
                item.FindPropertyRelative("worldName").stringValue = world.worldName;
                item.FindPropertyRelative("sceneName").stringValue = world.sceneName;
                item.FindPropertyRelative("worldButton").objectReferenceValue = world.worldButton;
                item.FindPropertyRelative("worldNameText").objectReferenceValue = world.worldNameText;
                item.FindPropertyRelative("lockedIcon").objectReferenceValue = world.lockedIcon;
                item.FindPropertyRelative("fullStarSprite").objectReferenceValue = world.fullStarSprite;
                item.FindPropertyRelative("emptyStarSprite").objectReferenceValue = world.emptyStarSprite;
                item.FindPropertyRelative("starDisplay").objectReferenceValue = world.starDisplay;
                item.FindPropertyRelative("progressGraphic").objectReferenceValue = world.progressGraphic;
                item.FindPropertyRelative("baseProgressColor").colorValue = world.baseProgressColor;

                SerializedProperty stars = item.FindPropertyRelative("starImages");
                stars.arraySize = world.starImages.Length;
                for (int starIndex = 0; starIndex < world.starImages.Length; starIndex++)
                {
                    stars.GetArrayElementAtIndex(starIndex).objectReferenceValue = world.starImages[starIndex];
                }
            }

            serialized.FindProperty("messageText").objectReferenceValue = message;
            serialized.FindProperty("lockedWorldMessage").stringValue = "Completa el mundo anterior para desbloquear este planeta.";
            serialized.FindProperty("menuSceneName").stringValue = "Menu";
            serialized.FindProperty("configureWorldButtonsOnAwake").boolValue = false;
            serialized.FindProperty("resetConfirmationPanel").objectReferenceValue = confirmation;
            serialized.FindProperty("resetConfirmationTransition").objectReferenceValue = confirmation.GetComponent<UIPanelTransition>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureVisuals(WorldSelectionVisuals visuals, IReadOnlyList<WorldSelectionVisuals.WorldVisual> worlds)
        {
            SerializedObject serialized = new SerializedObject(visuals);
            SerializedProperty worldArray = serialized.FindProperty("worlds");
            worldArray.arraySize = worlds.Count;
            for (int index = 0; index < worlds.Count; index++)
            {
                WorldSelectionVisuals.WorldVisual world = worlds[index];
                SerializedProperty item = worldArray.GetArrayElementAtIndex(index);
                item.FindPropertyRelative("visualRoot").objectReferenceValue = world.visualRoot;
                item.FindPropertyRelative("canvasGroup").objectReferenceValue = world.canvasGroup;
                item.FindPropertyRelative("planetImage").objectReferenceValue = world.planetImage;
                item.FindPropertyRelative("lockedGroup").objectReferenceValue = world.lockedGroup;
                item.FindPropertyRelative("unlockedColor").colorValue = world.unlockedColor;
                item.FindPropertyRelative("lockedColor").colorValue = world.lockedColor;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureCanvasScaler(Canvas canvas)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private static void ConfigureTransition(UIPanelTransition transition, CanvasGroup group, RectTransform target)
        {
            SerializedObject serialized = new SerializedObject(transition);
            serialized.FindProperty("canvasGroup").objectReferenceValue = group;
            serialized.FindProperty("target").objectReferenceValue = target;
            serialized.FindProperty("playOnEnable").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button CreateButton(string name, string label, Transform parent, Vector2 position, Vector2 size, Sprite sprite, Color labelColor)
        {
            GameObject buttonObject = CreateUiObject(name, parent);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0.5f), position, size);

            Image image = buttonObject.AddComponent<Image>();
            image.sprite = sprite ?? BuiltinUiSprite();
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            buttonObject.AddComponent<UIButtonFeedback>();

            if (!string.IsNullOrWhiteSpace(label))
            {
                TMP_Text text = CreateText("Texto", label, rect, 24f, FontStyles.Bold, TextAlignmentOptions.Center, labelColor);
                Stretch(text.rectTransform);
            }

            return button;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool raycastTarget)
        {
            GameObject imageObject = CreateUiObject(name, parent);
            Image image = imageObject.AddComponent<Image>();
            image.sprite = sprite ?? BuiltinUiSprite();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        private static TMP_Text CreateText(string name, string value, Transform parent, float fontSize, FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            GameObject textObject = CreateUiObject(name, parent);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject uiObject = new GameObject(name, typeof(RectTransform));
            uiObject.transform.SetParent(parent, false);
            return uiObject;
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

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void ClearAllListeners(UnityEventBase unityEvent)
        {
            unityEvent.RemoveAllListeners();
            for (int index = unityEvent.GetPersistentEventCount() - 1; index >= 0; index--)
            {
                UnityEventTools.RemovePersistentListener(unityEvent, index);
            }
        }

        private static Sprite LoadSprite(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite BuiltinUiSprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        }

        private static T[] ComponentsInScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
        }
    }
}
#endif
