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
        public void StoryAndSizeDesigns_ArePersistedAndVisibleInHierarchy()
        {
            Scene story = EditorSceneManager.OpenScene("Assets/Scenes/MundoCuentos_VozTest.unity", OpenSceneMode.Single);
            string[] storyObjects = { "PanelSeleccionCuentos", "PanelLecturaCuento", "PanelResultadoCuento", "PanelEncabezado", "ContenedorPrincipal", "PanelCuento", "PanelLectura", "ContenedorResultado", "PanelControles", "TarjetaCuento_tres_cerditos", "TarjetaCuento_conejo_luna", "TarjetaCuento_tortuga_amable", "ScrollViewLecturaReconocida", "TextoLecturaPlaceholder", "ScrollViewCuerpoCuento" };
            foreach (string objectName in storyObjects)
            {
                Transform item = ComponentsInScene<Transform>(story).FirstOrDefault(transform => transform.name == objectName);
                Assert.NotNull(item, $"Cuentos: falta {objectName}.");
            }

            Assert.IsTrue(ComponentsInScene<Transform>(story).Single(item => item.name == "PanelSeleccionCuentos").gameObject.activeSelf);
            Assert.IsFalse(ComponentsInScene<Transform>(story).Single(item => item.name == "PanelLecturaCuento").gameObject.activeSelf);
            Assert.IsFalse(ComponentsInScene<Transform>(story).Single(item => item.name == "PanelResultadoCuento").gameObject.activeSelf);

            Scene sizes = EditorSceneManager.OpenScene("Assets/Scenes/MundoTamanos.unity", OpenSceneMode.Single);
            string[] sizeObjects = { "HabitatDesignOverlay", "AnimalStageLeft", "AnimalStageRight", "LeftAnimalName", "RightAnimalName", "FeedbackCard" };
            foreach (string objectName in sizeObjects)
            {
                Transform item = ComponentsInScene<Transform>(sizes).FirstOrDefault(transform => transform.name == objectName);
                Assert.NotNull(item, $"Tamanos: falta {objectName}.");
                Assert.IsTrue(item.gameObject.activeSelf, $"Tamanos: {objectName} debe verse al abrir la escena.");
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
    }
}
#endif
