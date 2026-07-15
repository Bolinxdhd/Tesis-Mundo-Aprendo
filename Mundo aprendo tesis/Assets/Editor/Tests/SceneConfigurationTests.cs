#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SurpriseBox.Scripts;
using Jsgaona;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bolin.Editor.Tests
{
    public class SceneConfigurationTests
    {
        private static readonly string[] RequiredScenes =
        {
            "Assets/Scenes/Menu.unity",
            "Assets/Scenes/SeleccionMundos.unity",
            "Assets/Scenes/MundoMusical.unity",
            "Assets/Scenes/MundoCuentos_VozTest.unity",
            "Assets/Scenes/MundoTamanos.unity",
            "Assets/Scenes/Mundos/MundoEmociones.unity"
        };

        [OneTimeTearDown]
        public void RestoreMenuScene()
        {
            EditorSceneManager.OpenScene(RequiredScenes[0], OpenSceneMode.Single);
        }

        [Test]
        public void BuildSettings_ContainEveryRequiredSceneOnceAndEnabled()
        {
            UnityEditor.EditorBuildSettingsScene[] configured = UnityEditor.EditorBuildSettings.scenes;
            foreach (string path in RequiredScenes)
            {
                Assert.AreEqual(1, configured.Count(scene => scene.path == path), $"Escena duplicada o ausente: {path}");
                Assert.IsTrue(configured.Single(scene => scene.path == path).enabled, $"Escena desactivada: {path}");
            }
        }

        [TestCaseSource(nameof(RequiredScenes))]
        public void Scene_HasResponsiveCanvasTransitionsAccessibilityAndButtonFeedback(string path)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            AssertNoMissingScripts(scene, path);

            Canvas[] rootCanvases = ComponentsInScene<Canvas>(scene).Where(canvas => canvas.isRootCanvas).ToArray();
            Assert.IsNotEmpty(rootCanvases, $"{path} no tiene Canvas raiz.");
            foreach (Canvas canvas in rootCanvases)
            {
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                Assert.NotNull(scaler, $"{path}: falta CanvasScaler.");
                Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
                Assert.AreEqual(new Vector2(1920f, 1080f), scaler.referenceResolution);
                Assert.AreEqual(0.5f, scaler.matchWidthOrHeight, 0.001f);
                Assert.NotNull(canvas.GetComponent<CanvasColorBlindAccessibility>(), $"{path}: falta accesibilidad serializada.");

                Transform overlay = canvas.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(item => item.name == "SceneTransitionOverlay");
                Assert.NotNull(overlay, $"{path}: falta SceneTransitionOverlay.");
                Assert.NotNull(overlay.GetComponent<UISceneTransition>());
                CanvasGroup transitionGroup = overlay.GetComponent<CanvasGroup>();
                Assert.NotNull(transitionGroup);
                Assert.AreEqual(0f, transitionGroup.alpha, 0.001f, $"{path}: el overlay debe quedar transparente en la vista de escena.");
            }

            foreach (Button button in ComponentsInScene<Button>(scene))
            {
                // Las teclas musicales usan MusicalKeyVisual para hover, click y brillo;
                // no se les agrega UIButtonFeedback porque ambos componentes escalarían
                // la misma tecla en paralelo.
                if (button.GetComponent<MusicalKeyVisual>() != null)
                {
                    Assert.NotNull(button.GetComponent<MusicalKeyVisual>(), $"{path}: {button.name} no tiene feedback de tecla.");
                    continue;
                }

                Assert.NotNull(button.GetComponent<UIButtonFeedback>(), $"{path}: {button.name} no tiene feedback tactil.");
            }

            Assert.AreEqual(1, ComponentsInScene<AudioManager>(scene).Length, $"{path}: debe contener un AudioManager de respaldo.");
        }

        [Test]
        public void MusicalWorld_UsesOnlyTextMeshProAndKeepsGameplayReferences()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/MundoMusical.unity", OpenSceneMode.Single);
            Assert.IsEmpty(ComponentsInScene<Text>(scene), "Mundo Musical aun contiene UnityEngine.UI.Text.");

            MundoMusicalSequenceGame manager = ComponentsInScene<MundoMusicalSequenceGame>(scene).Single();
            SerializedObject serialized = new(manager);
            Assert.NotNull(serialized.FindProperty("tmpTitleText").objectReferenceValue);
            Assert.NotNull(serialized.FindProperty("tmpStatusText").objectReferenceValue);
            Assert.NotNull(serialized.FindProperty("tmpSequenceText").objectReferenceValue);
            Assert.NotNull(serialized.FindProperty("tmpResultText").objectReferenceValue);
            Assert.AreEqual(7, serialized.FindProperty("tmpKeyLabels").arraySize);
            Assert.GreaterOrEqual(serialized.FindProperty("sequences").arraySize, 4);
            Assert.GreaterOrEqual(serialized.FindProperty("pianoKeys").arraySize, 7);
            Assert.AreEqual(8, serialized.FindProperty("sequenceStepIndicators").arraySize);
            Assert.NotNull(serialized.FindProperty("topStarDisplay").objectReferenceValue);
            Assert.NotNull(serialized.FindProperty("listenButtonGuide").objectReferenceValue);
        }

        [Test]
        public void Menu_HasNoAutomaticAnimationComponents()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity", OpenSceneMode.Single);
            Assert.IsEmpty(ComponentsInScene<Animator>(scene));
            Assert.IsEmpty(ComponentsInScene<Animation>(scene));
            Assert.IsEmpty(ComponentsInScene<UIStaggeredEntrance>(scene));
            Assert.IsEmpty(ComponentsInScene<UIFloatingAnimation>(scene));
        }

        [TestCase("Assets/Scenes/Menu.unity")]
        [TestCase("Assets/Scenes/MundoCuentos_VozTest.unity")]
        [TestCase("Assets/Scenes/MundoTamanos.unity")]
        public void CorrectedScenes_HaveNoBrokenPersistentButtonListeners(string path)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (Button button in ComponentsInScene<Button>(scene))
            {
                for (int index = 0; index < button.onClick.GetPersistentEventCount(); index++)
                {
                    Assert.NotNull(button.onClick.GetPersistentTarget(index), $"{path}: {button.name} tiene un target OnClick roto.");
                    Assert.IsNotEmpty(button.onClick.GetPersistentMethodName(index), $"{path}: {button.name} tiene un metodo OnClick vacio.");
                }
            }
        }

        [Test]
        public void EmotionWorld_HasFivePreparedViewsWithFearDisabled()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Mundos/MundoEmociones.unity", OpenSceneMode.Single);
            EmotionGameManager manager = ComponentsInScene<EmotionGameManager>(scene).Single();
            SerializedObject serialized = new(manager);
            Assert.IsFalse(serialized.FindProperty("includeFear").boolValue);
            Assert.AreEqual(8, serialized.FindProperty("totalRounds").intValue);

            SerializedProperty views = serialized.FindProperty("emotionViews");
            Assert.AreEqual(5, views.arraySize);
            Assert.NotNull(serialized.FindProperty("roundProgressBar").objectReferenceValue);
            for (int i = 0; i < views.arraySize; i++)
            {
                SerializedProperty view = views.GetArrayElementAtIndex(i);
                Assert.NotNull(view.FindPropertyRelative("rootObject").objectReferenceValue, $"Emocion {i} sin objeto de escena.");
                Assert.IsNotEmpty(view.FindPropertyRelative("displayName").stringValue, $"Emocion {i} sin nombre.");
            }
        }

        [Test]
        public void SizeWorld_HasThreeHabitatsAndValidAnimalPairs()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/MundoTamanos.unity", OpenSceneMode.Single);
            SizeWorldController manager = ComponentsInScene<SizeWorldController>(scene).Single();
            SerializedProperty habitats = new SerializedObject(manager).FindProperty("habitats");
            Assert.GreaterOrEqual(habitats.arraySize, 3);

            for (int i = 0; i < habitats.arraySize; i++)
            {
                SerializedProperty habitat = habitats.GetArrayElementAtIndex(i);
                Assert.NotNull(habitat.FindPropertyRelative("backgroundSprite").objectReferenceValue, $"Habitat {i} sin fondo.");
                SerializedProperty animals = habitat.FindPropertyRelative("animals");
                Assert.GreaterOrEqual(animals.arraySize, 2, $"Habitat {i} necesita al menos dos animales.");
                HashSet<int> sizes = new();
                for (int j = 0; j < animals.arraySize; j++)
                {
                    SerializedProperty animal = animals.GetArrayElementAtIndex(j);
                    Assert.NotNull(animal.FindPropertyRelative("animalSprite").objectReferenceValue, $"Animal {i}:{j} sin sprite.");
                    sizes.Add(animal.FindPropertyRelative("sizeType").enumValueIndex);
                }
                Assert.IsTrue(sizes.Contains(0) && sizes.Contains(1), $"Habitat {i} requiere animales pequenos y grandes.");
            }
            Assert.NotNull(new SerializedObject(manager).FindProperty("leftAnimalNameText").objectReferenceValue);
            Assert.NotNull(new SerializedObject(manager).FindProperty("rightAnimalNameText").objectReferenceValue);
            Assert.IsNull(ComponentsInScene<Transform>(scene).FirstOrDefault(item => item.name == "Panel-botones-inferior"));
            Assert.IsNull(ComponentsInScene<Transform>(scene).FirstOrDefault(item => item.name == "Boton-animal-izquierdo"));
            Assert.IsNull(ComponentsInScene<Transform>(scene).FirstOrDefault(item => item.name == "Boton-animal-derecho"));
            Assert.NotNull(ComponentsInScene<Button>(scene).SingleOrDefault(item => item.name == "OpcionAnimalIzquierda"));
            Assert.NotNull(ComponentsInScene<Button>(scene).SingleOrDefault(item => item.name == "OpcionAnimalDerecha"));
        }

        [Test]
        public void StoryWorld_HasStorySelectionReaderResultAndMicrophoneIndicator()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/MundoCuentos_VozTest.unity", OpenSceneMode.Single);
            VoiceRecognitionTest manager = ComponentsInScene<VoiceRecognitionTest>(scene).Single();
            SerializedObject serialized = new(manager);
            Assert.NotNull(serialized.FindProperty("microphoneListeningIndicator").objectReferenceValue);
            Assert.NotNull(serialized.FindProperty("recognizedTextScrollRect").objectReferenceValue);
            Assert.NotNull(serialized.FindProperty("storyBodyScrollRect").objectReferenceValue);
            Assert.NotNull(serialized.FindProperty("storySelectionPanel").objectReferenceValue);
            Assert.NotNull(serialized.FindProperty("readingPanel").objectReferenceValue);
            Assert.NotNull(serialized.FindProperty("resultPanel").objectReferenceValue);
            Assert.NotNull(serialized.FindProperty("otherWorldsButton").objectReferenceValue);
            Assert.NotNull(serialized.FindProperty("resultRepeatButton").objectReferenceValue);
            Assert.NotNull(serialized.FindProperty("resultNextStoryButton").objectReferenceValue);
            Assert.NotNull(serialized.FindProperty("resultBackToStoriesButton").objectReferenceValue);
            Assert.NotNull(serialized.FindProperty("resultBackToWorldsButton").objectReferenceValue);
            Assert.GreaterOrEqual(serialized.FindProperty("cuentosDisponibles").arraySize, 3);
            Assert.GreaterOrEqual(serialized.FindProperty("cuentoCards").arraySize, 3);
            Assert.IsNull(ComponentsInScene<Transform>(scene).FirstOrDefault(item => item.name == "SupportModePanel"));
            Assert.IsNull(ComponentsInScene<Transform>(scene).FirstOrDefault(item => item.name == "ReadingProgressBar"));
            Assert.IsNull(ComponentsInScene<Transform>(scene).FirstOrDefault(item => item.name == "TextoPorcentaje"));
            Assert.IsEmpty(ComponentsInScene<TMP_Text>(scene).Where(item => item.text.Contains("%")));

            ScrollRect recognizedScroll = ComponentsInScene<ScrollRect>(scene).Single(item => item.name == "ScrollViewLecturaReconocida");
            ScrollRect storyScroll = ComponentsInScene<ScrollRect>(scene).Single(item => item.name == "ScrollViewCuerpoCuento");
            foreach (ScrollRect scroll in new[] { recognizedScroll, storyScroll })
            {
                Assert.IsTrue(scroll.vertical);
                Assert.IsFalse(scroll.horizontal);
                Assert.NotNull(scroll.viewport.GetComponent<RectMask2D>());
                Assert.NotNull(scroll.content.GetComponent<ContentSizeFitter>());
                Assert.NotNull(scroll.content.GetComponent<VerticalLayoutGroup>());
            }
        }

        [Test]
        public void StoryDesign_IsPersistedAndVisibleInHierarchy()
        {
            Scene story = EditorSceneManager.OpenScene("Assets/Scenes/MundoCuentos_VozTest.unity", OpenSceneMode.Single);
            string[] storyObjects =
            {
                "PanelSeleccionCuentos", "PanelLecturaCuento", "PanelResultadoCuento",
                "LibreroCentral", "GuideCharacter_Biblio_Selection",
                "ContenedorPrincipal", "PanelCuento", "LomoLibro", "PanelLectura",
                "CabeceraHistoriaALeer", "CabeceraLeeLaHistoria",
                "ContenedorResultado", "PanelControles", "GuideCharacter_Biblio_Result",
                "TarjetaCuento_tres_cerditos", "TarjetaCuento_conejo_luna", "TarjetaCuento_tortuga_amable",
                "ScrollViewLecturaReconocida", "TextoLecturaPlaceholder", "ScrollViewCuerpoCuento",
                "BotonRepetirCuento", "BotonSiguienteCuento", "BotonVolverACuentos", "BotonVolverAMundosResultado",
                "TutorialOverlay"
            };
            foreach (string objectName in storyObjects)
            {
                Transform item = ComponentsInScene<Transform>(story).FirstOrDefault(transform => transform.name == objectName);
                Assert.NotNull(item, $"Cuentos: falta {objectName}.");
            }

            Assert.IsTrue(ComponentsInScene<Transform>(story).Single(item => item.name == "PanelSeleccionCuentos").gameObject.activeSelf);
            Assert.IsFalse(ComponentsInScene<Transform>(story).Single(item => item.name == "PanelLecturaCuento").gameObject.activeSelf);
            Assert.IsFalse(ComponentsInScene<Transform>(story).Single(item => item.name == "PanelResultadoCuento").gameObject.activeSelf);
            Assert.IsNull(ComponentsInScene<Transform>(story).FirstOrDefault(item => item.name == "StoryTutorialOverlay"));
            Assert.AreEqual(1, ComponentsInScene<MonoBehaviour>(story).Count(item => item is StoryTutorialAnimationController));

            CanvasScaler scaler = ComponentsInScene<CanvasScaler>(story).Single();
            Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
            Assert.AreEqual(new Vector2(1920f, 1080f), scaler.referenceResolution);
        }

        [Test]
        public void SizeDesign_IsPersistedAndVisibleInHierarchy()
        {
            Scene sizes = EditorSceneManager.OpenScene("Assets/Scenes/MundoTamanos.unity", OpenSceneMode.Single);
            string[] sizeObjects =
            {
                "Canvas_MundoTamanos", "Fondo-habitat", "HabitatDesignOverlay", "TopLeftArea", "TopStars", "Boton-volver",
                "Area-animales-segura", "AnimalStageLeft", "AnimalStageRight",
                "Animal-izquierdo", "Animal-derecho", "OpcionAnimalIzquierda", "OpcionAnimalDerecha",
                "LeftAnimalName", "RightAnimalName", "BarraMensajesInferior", "Panel-resultado",
                "AnswerButtons", "BottomStars", "GuiaMundoTamanos",
                "TutorialOverlay", "SizeWorldTutorialFlow", "SceneTransitionOverlay"
            };
            foreach (string objectName in sizeObjects)
            {
                Transform item = ComponentsInScene<Transform>(sizes).FirstOrDefault(transform => transform.name == objectName);
                Assert.NotNull(item, $"Tamanos: falta {objectName}.");
                if (objectName != "Panel-resultado")
                {
                    Assert.IsTrue(item.gameObject.activeSelf, $"Tamanos: {objectName} debe estar preparado al abrir la escena.");
                }
            }

            Transform resultPanel = ComponentsInScene<Transform>(sizes).Single(item => item.name == "Panel-resultado");
            Assert.IsFalse(resultPanel.gameObject.activeSelf, "El panel de resultado debe iniciar oculto.");
        }

        [Test]
        public void SizeWorld_SeparatesTutorialGuideTopNavigationAndFeedbackBar()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/MundoTamanos.unity", OpenSceneMode.Single);
            SizeWorldController manager = ComponentsInScene<SizeWorldController>(scene).Single();
            SerializedObject controllerSerialized = new(manager);

            RectTransform topLeftArea = ComponentsInScene<RectTransform>(scene).Single(item => item.name == "TopLeftArea");
            Button returnButton = ComponentsInScene<Button>(scene).Single(item => item.name == "Boton-volver");
            RectTransform topStars = ComponentsInScene<RectTransform>(scene).Single(item => item.name == "TopStars");
            Assert.IsTrue(returnButton.transform.IsChildOf(topLeftArea), "VOLVER debe pertenecer al bloque superior izquierdo.");
            Assert.IsTrue(topStars.IsChildOf(topLeftArea), "Las estrellas superiores deben pertenecer al bloque superior izquierdo.");
            Assert.Greater(returnButton.GetComponent<RectTransform>().rect.width, returnButton.GetComponent<RectTransform>().rect.height,
                "VOLVER debe conservar un formato horizontal legible.");
            Assert.Less(topStars.anchoredPosition.y + topStars.rect.height * 0.5f,
                returnButton.GetComponent<RectTransform>().anchoredPosition.y - returnButton.GetComponent<RectTransform>().rect.height * 0.5f,
                "VOLVER y las estrellas no deben superponerse.");

            TMP_Text returnLabel = returnButton.GetComponentsInChildren<TMP_Text>(true)
                .SingleOrDefault(item => item.name == "TextoVOLVER");
            Assert.NotNull(returnLabel);
            Assert.AreEqual("VOLVER", returnLabel.text);

            RectTransform messageBar = ComponentsInScene<RectTransform>(scene).Single(item => item.name == "BarraMensajesInferior");
            TMP_Text feedbackText = controllerSerialized.FindProperty("feedbackText").objectReferenceValue as TMP_Text;
            Assert.NotNull(feedbackText);
            Assert.IsTrue(feedbackText.rectTransform.IsChildOf(messageBar), "Los mensajes deben usar la barra inferior, no una ventana central.");
            Assert.Less(messageBar.anchoredPosition.x, 0f);
            Assert.Less(messageBar.anchoredPosition.y, 0f);
            Assert.IsNull(ComponentsInScene<Transform>(scene).FirstOrDefault(item => item.name == "FeedbackCard"));

            SizeWorldPresentationController presentation = ComponentsInScene<SizeWorldPresentationController>(scene).Single();
            SerializedObject presentationSerialized = new(presentation);
            Assert.IsFalse(presentationSerialized.FindProperty("showGameplayGuide").boolValue,
                "La guía solo debe aparecer en el tutorial inicial.");
            Assert.AreEqual(feedbackText, presentationSerialized.FindProperty("messageText").objectReferenceValue);

            RectTransform guide = ComponentsInScene<RectTransform>(scene).Single(item => item.name == "GuiaMundoTamanos");
            Transform gameplay = ComponentsInScene<Transform>(scene).Single(item => item.name == "GameplayPresentation");
            Assert.IsTrue(guide.IsChildOf(gameplay), "El personaje visible debe existir en la jerarquía persistente de gameplay.");
            Assert.IsTrue(guide.gameObject.activeSelf, "El personaje de Tamaños debe estar presente desde la apertura de la escena.");
            Image guideImage = guide.GetComponentInChildren<Image>(true);
            Assert.NotNull(guideImage);
            Assert.NotNull(guideImage.sprite);
            Assert.That(AssetDatabase.GetAssetPath(guideImage.sprite).Replace('\\', '/'),
                Does.EndWith("Mundo Aprendo/Personajes/_Poses/Explorador_animo.png"));

            Transform tutorialOverlay = ComponentsInScene<Transform>(scene).Single(item => item.name == "TutorialOverlay");
            Assert.AreEqual(1, ComponentsInScene<SizeWorldTutorialController>(scene).Length);
            Assert.IsEmpty(ComponentsInScene<GameplayDialogueFeedbackBridge>(scene),
                "No debe quedar un puente de diálogo para cada ronda, acierto o error.");
            Assert.IsEmpty(ComponentsInScene<CharacterDialogueController>(scene)
                    .Where(controller => !controller.transform.IsChildOf(tutorialOverlay)),
                "El diálogo guía solo puede existir dentro del TutorialOverlay.");
            Assert.IsEmpty(ComponentsInScene<Transform>(scene)
                    .Where(item => item.name.IndexOf("Guide", StringComparison.OrdinalIgnoreCase) >= 0 && !item.IsChildOf(tutorialOverlay)),
                "No debe quedar un personaje guía de gameplay fuera del tutorial.");

            SerializedProperty correctDelay = controllerSerialized.FindProperty("correctAnswerDelay");
            Assert.NotNull(correctDelay);
            Assert.AreEqual(3f, correctDelay.floatValue, 0.001f,
                "La pausa de tres segundos debe aplicarse solo después del acierto.");
        }

        [Test]
        public void SizeWorld_UsesFixedSerializedAnimalTransforms()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/MundoTamanos.unity", OpenSceneMode.Single);
            SizeWorldController controller = ComponentsInScene<SizeWorldController>(scene).Single();
            SerializedObject serialized = new(controller);

            SerializedProperty leftPosition = serialized.FindProperty("leftAnimalAnchoredPosition");
            SerializedProperty rightPosition = serialized.FindProperty("rightAnimalAnchoredPosition");
            SerializedProperty fixedScale = serialized.FindProperty("fixedAnimalScale");
            Assert.NotNull(leftPosition);
            Assert.NotNull(rightPosition);
            Assert.NotNull(fixedScale);
            Assert.AreEqual(new Vector2(-336f, -8f), leftPosition.vector2Value);
            Assert.AreEqual(new Vector2(336f, -8f), rightPosition.vector2Value);
            Assert.AreEqual(1f, fixedScale.floatValue, 0.001f);
        }

        [Test]
        public void SizeWorld_SafariReconstruction_WiresGameplayUiAndDistinctHabitatBackgrounds()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/MundoTamanos.unity", OpenSceneMode.Single);
            SizeWorldController manager = ComponentsInScene<SizeWorldController>(scene).Single();
            SerializedObject serialized = new(manager);

            string[] requiredUiFields =
            {
                "backgroundImage", "leftAnimalImage", "rightAnimalImage", "questionText", "feedbackText",
                "leftAnimalButton", "rightAnimalButton", "leftAnimalNameText", "rightAnimalNameText",
                "leftSelectionOutline", "rightSelectionOutline", "animalSafeArea", "resultPanel", "resultText",
                "returnButton", "starDisplay", "fullStarSprite", "emptyStarSprite"
            };
            foreach (string fieldName in requiredUiFields)
            {
                SerializedProperty property = serialized.FindProperty(fieldName);
                Assert.NotNull(property, $"SizeWorldController no expone el campo serializado {fieldName}.");
                Assert.NotNull(property.objectReferenceValue, $"Mundo Tamanos: falta la referencia {fieldName}.");
            }

            Image background = serialized.FindProperty("backgroundImage").objectReferenceValue as Image;
            Assert.AreEqual("Fondo-habitat", background.name);

            Button leftButton = serialized.FindProperty("leftAnimalButton").objectReferenceValue as Button;
            Button rightButton = serialized.FindProperty("rightAnimalButton").objectReferenceValue as Button;
            Button returnButton = serialized.FindProperty("returnButton").objectReferenceValue as Button;
            Assert.AreEqual("OpcionAnimalIzquierda", leftButton.name);
            Assert.AreEqual("OpcionAnimalDerecha", rightButton.name);
            Assert.AreEqual(0, leftButton.onClick.GetPersistentEventCount(), "La mecanica conecta la opcion izquierda en tiempo de ejecucion.");
            Assert.AreEqual(0, rightButton.onClick.GetPersistentEventCount(), "La mecanica conecta la opcion derecha en tiempo de ejecucion.");
            Assert.AreEqual(0, returnButton.onClick.GetPersistentEventCount(), "La mecanica conecta Volver en tiempo de ejecucion.");

            RectTransform safeArea = serialized.FindProperty("animalSafeArea").objectReferenceValue as RectTransform;
            Image leftAnimal = serialized.FindProperty("leftAnimalImage").objectReferenceValue as Image;
            Image rightAnimal = serialized.FindProperty("rightAnimalImage").objectReferenceValue as Image;
            Assert.IsTrue(leftAnimal.rectTransform.IsChildOf(safeArea));
            Assert.IsTrue(rightAnimal.rectTransform.IsChildOf(safeArea));

            SerializedProperty habitatList = serialized.FindProperty("habitats");
            Assert.GreaterOrEqual(habitatList.arraySize, 3);
            Sprite safari = null;
            Sprite sea = null;
            Sprite farm = null;
            for (int index = 0; index < habitatList.arraySize; index++)
            {
                SerializedProperty habitat = habitatList.GetArrayElementAtIndex(index);
                string habitatName = habitat.FindPropertyRelative("habitatName").stringValue;
                Sprite sprite = habitat.FindPropertyRelative("backgroundSprite").objectReferenceValue as Sprite;
                Assert.NotNull(sprite, $"Habitat {habitatName} sin fondo Safari reutilizable.");

                if (habitatName.IndexOf("safari", StringComparison.OrdinalIgnoreCase) >= 0) safari = sprite;
                if (habitatName.IndexOf("mar", StringComparison.OrdinalIgnoreCase) >= 0) sea = sprite;
                if (habitatName.IndexOf("granja", StringComparison.OrdinalIgnoreCase) >= 0) farm = sprite;
            }

            Assert.NotNull(safari, "Debe existir el habitat Safari.");
            Assert.NotNull(sea, "Debe existir el habitat Mar.");
            Assert.NotNull(farm, "Debe existir el habitat Granja.");
            Assert.AreNotEqual(safari, sea, "Safari y Mar no pueden reutilizar el mismo fondo.");
            Assert.AreNotEqual(safari, farm, "Safari y Granja no pueden reutilizar el mismo fondo.");
            Assert.AreNotEqual(sea, farm, "Mar y Granja no pueden reutilizar el mismo fondo.");
            Assert.That(AssetDatabase.GetAssetPath(safari).Replace('\\', '/'), Does.EndWith("Mundo Safari/Safari.png"));
            Assert.That(AssetDatabase.GetAssetPath(sea).Replace('\\', '/'), Does.EndWith("Mundo Safari/Mar.png"));
            Assert.That(AssetDatabase.GetAssetPath(farm).Replace('\\', '/'), Does.EndWith("Mundo Safari/Jungla.png"));

            SerializedProperty controllerStars = serialized.FindProperty("starImages");
            Assert.AreEqual(3, controllerStars.arraySize);
            for (int index = 0; index < controllerStars.arraySize; index++)
            {
                Assert.NotNull(controllerStars.GetArrayElementAtIndex(index).objectReferenceValue);
            }

            UIStarDisplay topStars = ComponentsInScene<Transform>(scene).Single(item => item.name == "TopStars").GetComponent<UIStarDisplay>();
            Assert.NotNull(topStars, "TopStars debe usar el visor comun de tres estrellas.");
            Assert.AreEqual(topStars, serialized.FindProperty("starDisplay").objectReferenceValue);
            AssertStarDisplayUsesRatingSprites(topStars);

            GameObject resultPanelObject = serialized.FindProperty("resultPanel").objectReferenceValue as GameObject;
            UIStarDisplay resultStars = resultPanelObject.GetComponentInChildren<UIStarDisplay>(true);
            Assert.NotNull(resultStars, "El resultado debe mostrar sus tres estrellas persistentes.");
            AssertStarDisplayUsesRatingSprites(resultStars);
        }

        [Test]
        public void SizeWorld_UsesGeneratedSlicedVisualAssetsAndPersistentAnswerPresentation()
        {
            const string generatedRoot = "Assets/MundoAprendo/UI/Generated/MundoTamanos/";
            string[] requiredGeneratedAssets =
            {
                "TitleBannerPurple.png", "InstructionPanelCream.png", "AnimalCardCream.png",
                "AnimalHeaderTurquoise.png", "AnimalHeaderPurple.png",
                "AnswerButtonGreen.png", "AnswerButtonPurple.png", "BackCircle.png",
                "StarPanel.png", "SoftShadow.png", "BackArrow.png"
            };
            string[] slicedGeneratedAssets =
            {
                "TitleBannerPurple.png", "InstructionPanelCream.png", "AnimalCardCream.png",
                "AnimalHeaderTurquoise.png", "AnimalHeaderPurple.png",
                "AnswerButtonGreen.png", "AnswerButtonPurple.png", "BackCircle.png",
                "StarPanel.png"
            };

            foreach (string fileName in requiredGeneratedAssets)
            {
                string assetPath = generatedRoot + fileName;
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                Assert.NotNull(sprite, $"Falta el recurso UI generado {assetPath}.");

                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                Assert.NotNull(importer, $"{assetPath} debe importarse como textura UI.");
                Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, $"{assetPath} debe ser Sprite (2D and UI).");
                Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode, $"{assetPath} debe ser un Sprite unico.");
            }

            foreach (string fileName in slicedGeneratedAssets)
            {
                TextureImporter importer = AssetImporter.GetAtPath(generatedRoot + fileName) as TextureImporter;
                Assert.NotNull(importer);
                Assert.Greater(importer.spriteBorder.x, 0f, $"{fileName} necesita borde horizontal para 9-slice.");
                Assert.Greater(importer.spriteBorder.y, 0f, $"{fileName} necesita borde vertical para 9-slice.");
            }

            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/MundoTamanos.unity", OpenSceneMode.Single);
            string[] framedNodes =
            {
                "TituloMundo", "PanelInstruccion", "AnimalStageLeft", "AnimalStageRight",
                "Boton-volver", "BottomStars"
            };
            foreach (string nodeName in framedNodes)
            {
                Transform node = ComponentsInScene<Transform>(scene).FirstOrDefault(item => item.name == nodeName);
                Assert.NotNull(node, $"Tamanos: falta el contenedor visual {nodeName}.");
                AssertContainsGeneratedSlicedImage(node, generatedRoot);
            }

            Button[] answerButtons =
            {
                ComponentsInScene<Button>(scene).Single(item => item.name == "OpcionAnimalIzquierda"),
                ComponentsInScene<Button>(scene).Single(item => item.name == "OpcionAnimalDerecha")
            };
            foreach (Button answerButton in answerButtons)
            {
                Transform answerVisual = answerButton.GetComponentsInChildren<Transform>(true)
                    .SingleOrDefault(item => item.name == "AnswerVisual");
                Assert.NotNull(answerVisual, $"{answerButton.name}: falta el boton visual persistente.");
                AssertContainsGeneratedSlicedImage(answerVisual, generatedRoot);

                TMP_Text answerLabel = answerButton.GetComponentsInChildren<TMP_Text>(true)
                    .SingleOrDefault(item => item.name == "AnswerLabel");
                Image answerIcon = answerButton.GetComponentsInChildren<Image>(true)
                    .SingleOrDefault(item => item.name == "AnswerIcon");
                Assert.NotNull(answerLabel, $"{answerButton.name}: falta la etiqueta dinamica de respuesta.");
                Assert.NotNull(answerIcon, $"{answerButton.name}: falta el icono dinamico del animal.");
            }

            Transform lowerStars = ComponentsInScene<Transform>(scene).Single(item => item.name == "BottomStars");
            Image[] lowerStarImages = lowerStars.GetComponentsInChildren<Image>(true)
                .Where(image => image.name.StartsWith("Estrella", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            Assert.AreEqual(3, lowerStarImages.Length, "El panel inferior debe conservar las tres estrellas visuales.");
            foreach (Image star in lowerStarImages)
            {
                Assert.NotNull(star.sprite, $"{star.name}: falta sprite de estrella persistente.");
            }

            Transform[] customUiRoots =
            {
                ComponentsInScene<Transform>(scene).Single(item => item.name == "TituloMundo"),
                ComponentsInScene<Transform>(scene).Single(item => item.name == "PanelInstruccion"),
                ComponentsInScene<Transform>(scene).Single(item => item.name == "AnimalStageLeft"),
                ComponentsInScene<Transform>(scene).Single(item => item.name == "AnimalStageRight"),
                ComponentsInScene<Transform>(scene).Single(item => item.name == "AnswerButtons"),
                ComponentsInScene<Transform>(scene).Single(item => item.name == "Boton-volver"),
                lowerStars
            };
            foreach (Image image in customUiRoots.SelectMany(root => root.GetComponentsInChildren<Image>(true)))
            {
                if (image.sprite == null) continue;
                string assetPath = AssetDatabase.GetAssetPath(image.sprite).Replace('\\', '/');
                Assert.IsFalse(assetPath.StartsWith("Assets/Cartoon UI/", StringComparison.Ordinal),
                    $"{image.name} aun depende de un panel/boton prefabricado de Cartoon UI: {assetPath}");
                Assert.IsFalse(assetPath.StartsWith("Assets/Hyper_Casual_UI/Sprites/Buttons/", StringComparison.Ordinal),
                    $"{image.name} aun depende de un boton prefabricado de Hyper Casual UI: {assetPath}");
            }
        }

        [Test]
        public void SelectionWorld_HasResetConfirmationAndFourStarDisplays()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SeleccionMundos.unity", OpenSceneMode.Single);
            WorldSelectionManager manager = ComponentsInScene<WorldSelectionManager>(scene).Single();
            SerializedObject serialized = new(manager);
            Assert.NotNull(serialized.FindProperty("resetConfirmationPanel").objectReferenceValue);
            Assert.NotNull(serialized.FindProperty("resetConfirmationTransition").objectReferenceValue);
            SerializedProperty worlds = serialized.FindProperty("worlds");
            Assert.AreEqual(4, worlds.arraySize);
            for (int i = 0; i < worlds.arraySize; i++)
            {
                Assert.NotNull(worlds.GetArrayElementAtIndex(i).FindPropertyRelative("starDisplay").objectReferenceValue);
            }
        }

        [Test]
        public void SelectionWorld_UsesOnlyVisiblePlanetReferencesAndNoLegacyCards()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SeleccionMundos.unity", OpenSceneMode.Single);
            Assert.NotNull(ComponentsInScene<Transform>(scene).SingleOrDefault(item => item.name == "VisualSeleccionMundos"));
            Assert.IsNull(ComponentsInScene<Transform>(scene).FirstOrDefault(item => item.name == "Panel-seleccion-mundos"));

            WorldSelectionManager manager = ComponentsInScene<WorldSelectionManager>(scene).Single();
            SerializedObject serialized = new(manager);
            Assert.IsFalse(serialized.FindProperty("configureWorldButtonsOnAwake").boolValue);
            SerializedProperty worlds = serialized.FindProperty("worlds");
            Assert.AreEqual(4, worlds.arraySize);
            for (int index = 0; index < worlds.arraySize; index++)
            {
                SerializedProperty world = worlds.GetArrayElementAtIndex(index);
                Button button = world.FindPropertyRelative("worldButton").objectReferenceValue as Button;
                Assert.NotNull(button, $"Mundo {index} sin boton visible.");
                Assert.AreEqual(1, button.onClick.GetPersistentEventCount(), $"Mundo {index} debe tener un unico listener persistente.");
                Assert.NotNull(world.FindPropertyRelative("worldNameText").objectReferenceValue);
                Assert.NotNull(world.FindPropertyRelative("lockedIcon").objectReferenceValue);
                Assert.NotNull(world.FindPropertyRelative("starDisplay").objectReferenceValue);
                SerializedProperty stars = world.FindPropertyRelative("starImages");
                Assert.AreEqual(3, stars.arraySize);
                for (int star = 0; star < stars.arraySize; star++) Assert.NotNull(stars.GetArrayElementAtIndex(star).objectReferenceValue);
            }

            WorldSelectionVisuals visuals = ComponentsInScene<WorldSelectionVisuals>(scene).Single();
            SerializedProperty visualWorlds = new SerializedObject(visuals).FindProperty("worlds");
            Assert.AreEqual(4, visualWorlds.arraySize);
            for (int index = 0; index < visualWorlds.arraySize; index++)
            {
                Assert.NotNull(visualWorlds.GetArrayElementAtIndex(index).FindPropertyRelative("lockedGroup").objectReferenceValue);
            }
        }

        [Test]
        public void MusicalReconstruction_HasSevenPersistentKeysAndIsolatedPresentation()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/MundoMusical.unity", OpenSceneMode.Single);
            Assert.NotNull(ComponentsInScene<Transform>(scene).SingleOrDefault(item => item.name == "MusicalUI"));
            Assert.NotNull(ComponentsInScene<Transform>(scene).SingleOrDefault(item => item.name == "TutorialOverlay"));
            Assert.NotNull(ComponentsInScene<Transform>(scene).SingleOrDefault(item => item.name == "PianoFrame"));
            Assert.NotNull(ComponentsInScene<Transform>(scene).SingleOrDefault(item => item.name == "ResultView"));
            Assert.IsEmpty(ComponentsInScene<GameplayDialogueFeedbackBridge>(scene));
            Assert.IsEmpty(ComponentsInScene<CharacterDialogueController>(scene));

            MundoMusicalSequenceGame manager = ComponentsInScene<MundoMusicalSequenceGame>(scene).Single();
            SerializedObject serialized = new(manager);
            Assert.NotNull(serialized.FindProperty("tutorialController").objectReferenceValue);
            Assert.NotNull(serialized.FindProperty("levelAnimationController").objectReferenceValue);
            GameObject resultPanel = serialized.FindProperty("resultPanel").objectReferenceValue as GameObject;
            UIStarDisplay starDisplay = serialized.FindProperty("starDisplay").objectReferenceValue as UIStarDisplay;
            UIStarDisplay topStarDisplay = serialized.FindProperty("topStarDisplay").objectReferenceValue as UIStarDisplay;
            Assert.NotNull(resultPanel);
            Assert.NotNull(starDisplay);
            Assert.NotNull(topStarDisplay);
            Assert.IsTrue(starDisplay.transform.IsChildOf(resultPanel.transform));
            Assert.AreEqual("TopStars", topStarDisplay.name);
            Assert.IsTrue(topStarDisplay.transform.IsChildOf(ComponentsInScene<Transform>(scene).Single(item => item.name == "TopBar")));

            SerializedProperty keyButtons = serialized.FindProperty("keyButtons");
            SerializedProperty pianoKeys = serialized.FindProperty("pianoKeys");
            Assert.AreEqual(7, keyButtons.arraySize);
            Assert.GreaterOrEqual(pianoKeys.arraySize, 7);
            RectTransform keysContainer = ComponentsInScene<RectTransform>(scene).Single(item => item.name == "KeysContainer");
            HorizontalLayoutGroup keysLayout = keysContainer.GetComponent<HorizontalLayoutGroup>();
            Assert.NotNull(keysLayout);
            Assert.AreEqual(7, keysContainer.childCount);
            Assert.IsFalse(keysLayout.childForceExpandWidth);
            Assert.IsFalse(keysLayout.childForceExpandHeight);

            string[] expectedLabels = { "1", "2", "3", "4", "5", "6", "7" };
            for (int index = 0; index < expectedLabels.Length; index++)
            {
                Button key = keyButtons.GetArrayElementAtIndex(index).objectReferenceValue as Button;
                Assert.NotNull(key);
                Assert.AreEqual("Key_" + (index + 1), key.name);
                Assert.NotNull(key.GetComponent<MusicalKeyVisual>());
                Assert.AreEqual(0, key.onClick.GetPersistentEventCount(), "Las teclas se conectan una vez por el controlador del juego.");
                Assert.AreEqual(expectedLabels[index], pianoKeys.GetArrayElementAtIndex(index).FindPropertyRelative("displayName").stringValue);
                Assert.NotNull(key.transform.Find("KeyVisual"));
                TMP_Text[] texts = key.GetComponentsInChildren<TMP_Text>(true);
                Assert.AreEqual(1, texts.Length, "Cada tecla debe tener solamente un numero visible.");
                Assert.AreEqual("NumberText", texts[0].name);
                Assert.AreEqual(TextWrappingModes.NoWrap, texts[0].textWrappingMode);
                Assert.AreEqual(TextOverflowModes.Overflow, texts[0].overflowMode);
                Assert.IsNull(key.GetComponent<UIButtonFeedback>(), "Las teclas usan solo MusicalKeyVisual.");
            }

            Button startActivity = serialized.FindProperty("startActivityButton").objectReferenceValue as Button;
            Button listen = serialized.FindProperty("startButton").objectReferenceValue as Button;
            Button back = serialized.FindProperty("returnButton").objectReferenceValue as Button;
            Assert.AreEqual(0, startActivity.onClick.GetPersistentEventCount());
            Assert.AreEqual(0, listen.onClick.GetPersistentEventCount());
            Assert.AreEqual(0, back.onClick.GetPersistentEventCount());
            Assert.AreEqual("Boton-volver-seleccion", back.name);
            Assert.AreEqual(1, ComponentsInScene<Button>(scene).Single(item => item.name == "Boton-reintentar").onClick.GetPersistentEventCount());
            Assert.IsNull(ComponentsInScene<Button>(scene).SingleOrDefault(item => item.name == "BotonSiguienteMundo"));
            Assert.IsNull(ComponentsInScene<Button>(scene).SingleOrDefault(item => item.name == "BotonVolverResultado"));
            Assert.AreEqual(0, ComponentsInScene<Button>(scene).Single(item => item.name == "BotonSiguienteTutorial").onClick.GetPersistentEventCount());
            Assert.AreEqual(0, ComponentsInScene<Button>(scene).Single(item => item.name == "BotonComenzarTutorial").onClick.GetPersistentEventCount());
        }

        [Test]
        public void MusicalVisualRefresh_PersistsVisualKitAndKeepsFiveStepTutorial()
        {
            const string visualRoot = "Assets/MundoAprendo/UI/Generated/MundoMusical/";
            string[] requiredAssets =
            {
                "MusicKeyCream.png", "MusicKeyGlow.png", "MusicSequenceBubble.png",
                "MusicPianoFrame.png", "MusicListenGlow.png", "MusicListenButtonOutline.png", "MusicNoteGolden.png"
            };
            foreach (string assetName in requiredAssets)
            {
                Sprite asset = AssetDatabase.LoadAssetAtPath<Sprite>(visualRoot + assetName);
                Assert.NotNull(asset, $"Mundo Musical: falta el recurso visual persistente {assetName}.");
            }

            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/MundoMusical.unity", OpenSceneMode.Single);
            string[] requiredHierarchy =
            {
                "PlacaTituloMusical", "NotaTituloIzquierda", "PildoraSecuencia", "TopStars",
                "Tamborcin", "PianoFrame", "EscucharTutorialGlow", "TutorialOverlay"
            };
            foreach (string objectName in requiredHierarchy)
            {
                Assert.NotNull(ComponentsInScene<Transform>(scene).SingleOrDefault(item => item.name == objectName),
                    $"Mundo Musical: falta {objectName} en la jerarquía persistente.");
            }

            Image pianoFrame = ComponentsInScene<Image>(scene).Single(item => item.name == "PianoFrame");
            Assert.That(AssetDatabase.GetAssetPath(pianoFrame.sprite).Replace('\\', '/'),
                Is.EqualTo(visualRoot + "MusicPianoFrame.png"));

            for (int index = 1; index <= 7; index++)
            {
                Transform key = ComponentsInScene<Transform>(scene).Single(item => item.name == "Key_" + index);
                Image keyVisual = key.Find("KeyVisual").GetComponent<Image>();
                Assert.That(AssetDatabase.GetAssetPath(keyVisual.sprite).Replace('\\', '/'),
                    Is.EqualTo(visualRoot + "MusicKeyCream.png"));
            }

            for (int index = 1; index <= 8; index++)
            {
                Image step = ComponentsInScene<Image>(scene).Single(item => item.name == "Paso_" + index);
                Assert.That(AssetDatabase.GetAssetPath(step.sprite).Replace('\\', '/'),
                    Is.EqualTo(visualRoot + "MusicSequenceBubble.png"));
            }

            Transform topStars = ComponentsInScene<Transform>(scene).Single(item => item.name == "TopStars");
            UIStarDisplay topDisplay = topStars.GetComponent<UIStarDisplay>();
            Assert.NotNull(topDisplay);
            SerializedProperty topStarImages = new SerializedObject(topDisplay).FindProperty("stars");
            Assert.AreEqual(3, topStarImages.arraySize);
            for (int index = 0; index < topStarImages.arraySize; index++)
            {
                Assert.NotNull(topStarImages.GetArrayElementAtIndex(index).objectReferenceValue);
            }

            Button listen = ComponentsInScene<Button>(scene).Single(item => item.name == "BotonEscuchar");
            MusicalListenButtonGuide guide = listen.GetComponent<MusicalListenButtonGuide>();
            Assert.NotNull(guide);
            Transform tutorialGlow = ComponentsInScene<Transform>(scene).Single(item => item.name == "EscucharTutorialGlow");
            Image tutorialGlowImage = tutorialGlow.GetComponent<Image>();
            Assert.NotNull(tutorialGlowImage);
            Assert.That(AssetDatabase.GetAssetPath(tutorialGlowImage.sprite).Replace('\\', '/'),
                Is.EqualTo(visualRoot + "MusicListenButtonOutline.png"));
            Assert.AreEqual(Image.Type.Sliced, tutorialGlowImage.type);
            Assert.NotNull(tutorialGlow.GetComponent<Outline>(), "El halo debe conservar un marco configurable.");
            SerializedObject guideSerialized = new(guide);
            Assert.NotNull(guideSerialized.FindProperty("glowOutline").objectReferenceValue);
            Assert.AreEqual(0f, guideSerialized.FindProperty("glowExpansion").floatValue);
            Assert.AreEqual(0f, guideSerialized.FindProperty("borderThickness").floatValue);

            Transform tutorialOverlay = ComponentsInScene<Transform>(scene).Single(item => item.name == "TutorialOverlay");
            MusicalTutorialController tutorial = ComponentsInScene<MusicalTutorialController>(scene).Single();
            SerializedObject tutorialSerialized = new(tutorial);
            SerializedProperty tutorialSteps = tutorialSerialized.FindProperty("tutorialSteps");
            Assert.IsTrue(tutorial.transform.IsChildOf(tutorialOverlay), "El tutorial debe seguir dentro de su overlay original.");
            Assert.NotNull(tutorialSteps);
            Assert.AreEqual(5, tutorialSteps.arraySize, "El rediseño no puede eliminar pasos del tutorial.");
        }

        private static void AssertNoMissingScripts(Scene scene, string path)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                {
                    Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject),
                        $"{path}: {item.name} tiene scripts faltantes.");
                }
            }
        }

        private static T[] ComponentsInScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        }

        private static void AssertContainsGeneratedSlicedImage(Transform container, string generatedRoot)
        {
            Image generatedImage = container.GetComponentsInChildren<Image>(true)
                .FirstOrDefault(image =>
                {
                    if (image.sprite == null || image.type != Image.Type.Sliced) return false;
                    string assetPath = AssetDatabase.GetAssetPath(image.sprite).Replace('\\', '/');
                    return assetPath.StartsWith(generatedRoot, StringComparison.Ordinal);
                });
            Assert.NotNull(generatedImage,
                $"{container.name}: debe contener una Image Sliced con un recurso generado en {generatedRoot}.");
        }

        private static void AssertStarDisplayUsesRatingSprites(UIStarDisplay display)
        {
            SerializedObject serialized = new(display);
            SerializedProperty stars = serialized.FindProperty("stars");
            Assert.NotNull(stars);
            Assert.AreEqual(3, stars.arraySize);
            for (int index = 0; index < stars.arraySize; index++)
            {
                Assert.NotNull(stars.GetArrayElementAtIndex(index).objectReferenceValue,
                    $"{display.name}: falta la estrella visual {index + 1}.");
            }

            Sprite earned = serialized.FindProperty("earnedSprite").objectReferenceValue as Sprite;
            Sprite unearned = serialized.FindProperty("unearnedSprite").objectReferenceValue as Sprite;
            Assert.NotNull(earned, $"{display.name}: falta sprite de estrella obtenida.");
            Assert.NotNull(unearned, $"{display.name}: falta sprite de estrella vacia.");
            Assert.AreNotEqual(earned, unearned, $"{display.name}: las estrellas obtenida y vacia deben diferenciarse.");
            Assert.That(AssetDatabase.GetAssetPath(earned).Replace('\\', '/'), Does.EndWith("Hyper_Casual_UI/Sprites/Usar/rataing star.png"));
            Assert.That(AssetDatabase.GetAssetPath(unearned).Replace('\\', '/'), Does.EndWith("Hyper_Casual_UI/Sprites/Usar/rataing star (1).png"));
        }
    }
}
#endif
