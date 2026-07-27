#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MundoAprendo.PlayModeTests
{
    public class ProjectPlayModeSmokeTests
    {
        private static readonly string[] SceneNames =
        {
            "Menu", "SeleccionMundos", "MundoMusical", "MundoCuentos_VozTest", "MundoTamanos", "MundoEmociones"
        };

        [UnityTest]
        public IEnumerator EveryConfiguredScene_LoadsWithCoreUi()
        {
            foreach (string sceneName in SceneNames)
            {
                yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                yield return null;

                Scene scene = SceneManager.GetActiveScene();
                Assert.AreEqual(sceneName, scene.name);
                GameObject[] roots = scene.GetRootGameObjects();
                Assert.IsNotEmpty(roots, $"{sceneName} no contiene objetos raiz.");
                Assert.IsTrue(roots.SelectMany(root => root.GetComponentsInChildren<Canvas>(true)).Any(canvas => canvas.isRootCanvas),
                    $"{sceneName} no contiene Canvas raiz.");
                Assert.IsTrue(roots.SelectMany(root => root.GetComponentsInChildren<EventSystem>(true)).Any(),
                    $"{sceneName} no contiene EventSystem.");
                Assert.IsTrue(roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Any(item => item.name == "SceneTransitionOverlay"), $"{sceneName} no contiene transicion visual.");
                Assert.IsTrue(roots.SelectMany(root => root.GetComponentsInChildren<Button>(true)).Any(),
                    $"{sceneName} no contiene botones.");
            }
        }

        [UnityTest]
        public IEnumerator ProgressRepository_KeepsBestStarsInPlayMode()
        {
            const string completedKey = "MundoAprendo_World_3_Completed";
            const string starsKey = "MundoAprendo_World_3_Stars";
            bool hadCompleted = PlayerPrefs.HasKey(completedKey);
            bool hadStars = PlayerPrefs.HasKey(starsKey);
            int oldCompleted = PlayerPrefs.GetInt(completedKey, 0);
            int oldStars = PlayerPrefs.GetInt(starsKey, 0);

            try
            {
                PlayerPrefs.DeleteKey(completedKey);
                PlayerPrefs.DeleteKey(starsKey);
                Type repository = Type.GetType("Bolin.WorldProgressRepository, Assembly-CSharp");
                Assert.NotNull(repository);
                MethodInfo save = repository.GetMethod("SaveBestResult", new[] { typeof(int), typeof(int), typeof(bool) });
                MethodInfo getStars = repository.GetMethod("GetStars", new[] { typeof(int) });
                Assert.NotNull(save);
                Assert.NotNull(getStars);

                save.Invoke(null, new object[] { 3, 3, true });
                save.Invoke(null, new object[] { 3, 1, true });
                Assert.AreEqual(3, (int)getStars.Invoke(null, new object[] { 3 }));
                Assert.AreEqual(1, PlayerPrefs.GetInt(completedKey));
                yield return null;
            }
            finally
            {
                if (hadCompleted) PlayerPrefs.SetInt(completedKey, oldCompleted);
                else PlayerPrefs.DeleteKey(completedKey);
                if (hadStars) PlayerPrefs.SetInt(starsKey, oldStars);
                else PlayerPrefs.DeleteKey(starsKey);
                PlayerPrefs.Save();
            }
        }

        [UnityTest]
        public IEnumerator StoryProgress_UsesStableStoryIdsAndRequiresTwoDifferentStories()
        {
            string[] keys =
            {
                "MundoCuentos_Completado_tres_cerditos",
                "MundoCuentos_Puntaje_tres_cerditos",
                "MundoCuentos_Estrellas_tres_cerditos",
                "MundoCuentos_Completado_conejo_luna",
                "MundoCuentos_Puntaje_conejo_luna",
                "MundoCuentos_Estrellas_conejo_luna",
                "MundoCuentos_CuentosRegistrados",
                "MundoAprendo_World_1_Completed",
                "MundoAprendo_World_1_Stars"
            };
            (bool had, int intValue, string stringValue)[] oldValues = keys
                .Select(key => (PlayerPrefs.HasKey(key), PlayerPrefs.GetInt(key, 0), PlayerPrefs.GetString(key, string.Empty)))
                .ToArray();

            try
            {
                foreach (string key in keys) PlayerPrefs.DeleteKey(key);

                Type repositoryType = Type.GetType("Bolin.StoryProgressRepository, Assembly-CSharp");
                Type storyType = Type.GetType("Bolin.CuentoData, Assembly-CSharp");
                Assert.NotNull(repositoryType);
                Assert.NotNull(storyType);

                object repository = Activator.CreateInstance(repositoryType);
                MethodInfo saveStory = repositoryType.GetMethod("SaveStoryResult");
                MethodInfo countCompleted = repositoryType.GetMethod("CountCompletedStories", BindingFlags.Public | BindingFlags.Static);
                Assert.NotNull(saveStory);
                Assert.NotNull(countCompleted);

                Type listType = typeof(System.Collections.Generic.List<>).MakeGenericType(storyType);
                object stories = Activator.CreateInstance(listType);
                MethodInfo add = listType.GetMethod("Add");
                add.Invoke(stories, new[] { CreateStoryData(storyType, "tres_cerditos", "Los tres cerditos") });
                add.Invoke(stories, new[] { CreateStoryData(storyType, "conejo_luna", "El conejo y la luna") });

                saveStory.Invoke(repository, new object[] { "tres_cerditos", 90, 3, true });
                saveStory.Invoke(repository, new object[] { "tres_cerditos", 50, 1, true });
                Assert.AreEqual(1, (int)countCompleted.Invoke(null, new[] { stories }));
                Assert.AreEqual(90, PlayerPrefs.GetInt("MundoCuentos_Puntaje_tres_cerditos"));
                Assert.AreEqual(3, PlayerPrefs.GetInt("MundoCuentos_Estrellas_tres_cerditos"));
                Assert.AreEqual(0, PlayerPrefs.GetInt("MundoAprendo_World_1_Completed", 0));

                saveStory.Invoke(repository, new object[] { "conejo_luna", 80, 2, true });
                Assert.AreEqual(2, (int)countCompleted.Invoke(null, new[] { stories }));
                Assert.AreEqual(1, PlayerPrefs.GetInt("MundoAprendo_World_1_Completed"));
                yield return null;
            }
            finally
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    if (!oldValues[i].had)
                    {
                        PlayerPrefs.DeleteKey(keys[i]);
                    }
                    else if (keys[i] == "MundoCuentos_CuentosRegistrados")
                    {
                        PlayerPrefs.SetString(keys[i], oldValues[i].stringValue);
                    }
                    else
                    {
                        PlayerPrefs.SetInt(keys[i], oldValues[i].intValue);
                    }
                }

                PlayerPrefs.Save();
            }
        }

        [UnityTest]
        public IEnumerator MenuMusic_IsScopedToMenuAndDoesNotDuplicate()
        {
            yield return SceneManager.LoadSceneAsync("Menu", LoadSceneMode.Single);
            yield return null;

            Type audioManagerType = Type.GetType("Bolin.AudioManager, Assembly-CSharp");
            Assert.NotNull(audioManagerType);
            Component[] menuManagers = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren(audioManagerType, true))
                .Cast<Component>().ToArray();
            Assert.AreEqual(1, menuManagers.Length);
            AudioSource menuMusic = menuManagers[0].GetComponentsInChildren<AudioSource>(true)
                .Single(source => source.gameObject.name == "MusicSource");
            Assert.NotNull(menuMusic.clip);

            yield return SceneManager.LoadSceneAsync("SeleccionMundos", LoadSceneMode.Single);
            yield return null;

            Component[] selectionManagers = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren(audioManagerType, true))
                .Cast<Component>().ToArray();
            Assert.AreEqual(1, selectionManagers.Length);
            AudioSource selectionMusic = selectionManagers[0].GetComponentsInChildren<AudioSource>(true)
                .Single(source => source.gameObject.name == "MusicSource");
            Assert.IsNull(selectionMusic.clip);

            yield return SceneManager.LoadSceneAsync("Menu", LoadSceneMode.Single);
            yield return null;
            Assert.AreEqual(1, SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren(audioManagerType, true)).Count());
        }

        [UnityTest]
        public IEnumerator StoryVoiceFilter_RejectsForeignWordsAndRepeatedFinalFragments()
        {
            yield return SceneManager.LoadSceneAsync("MundoCuentos_VozTest", LoadSceneMode.Single);
            yield return null;

            Type managerType = Type.GetType("Bolin.VoiceRecognitionTest, Assembly-CSharp");
            Type wordType = Type.GetType("Bolin.PalabraReconocida, Assembly-CSharp");
            Assert.NotNull(managerType);
            Assert.NotNull(wordType);
            Component manager = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren(managerType, true))
                .Cast<Component>().Single();
            Assert.NotNull(manager);
            managerType.GetMethod("ClearRecognizedText").Invoke(manager, null);

            MethodInfo append = managerType.GetMethod(
                "AppendFinalRecognizedFragment",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo wordsField = managerType.GetField(
                "palabrasMostradas",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(append);
            Assert.NotNull(wordsField);

            append.Invoke(manager, new object[] { "Habia una vez cuatro cerditos" });
            IList words = (IList)wordsField.GetValue(manager);
            FieldInfo isCorrectField = wordType.GetField("esCorrecta");
            Assert.NotNull(isCorrectField);
            Assert.AreEqual(4, words.Count);
            Assert.IsTrue(words.Cast<object>().All(word => (bool)isCorrectField.GetValue(word)));
            Assert.IsFalse(words.Cast<object>().Select(word => wordType.GetField("textoNormalizado").GetValue(word).ToString()).Contains("cuatro"));

            managerType.GetMethod("ClearRecognizedText").Invoke(manager, null);
            append.Invoke(manager, new object[] { "Habia una vez tres cerditos" });
            int countAfterFirstFinal = words.Count;
            append.Invoke(manager, new object[] { "Habia una vez tres cerditos" });
            Assert.AreEqual(countAfterFirstFinal, words.Count);

            append.Invoke(manager, new object[] { "ruido" });
            Assert.AreEqual(countAfterFirstFinal, words.Count);
        }

        [UnityTest]
        public IEnumerator StoryVoiceFilter_RebuildsVocabularyForEveryConfiguredStory()
        {
            yield return SceneManager.LoadSceneAsync("MundoCuentos_VozTest", LoadSceneMode.Single);
            yield return null;

            Type managerType = Type.GetType("Bolin.VoiceRecognitionTest, Assembly-CSharp");
            Type storyType = Type.GetType("Bolin.CuentoData, Assembly-CSharp");
            Type evaluatorType = Type.GetType("Bolin.ReadingEvaluator, Assembly-CSharp");
            Type wordType = Type.GetType("Bolin.PalabraReconocida, Assembly-CSharp");
            Assert.NotNull(managerType);
            Assert.NotNull(storyType);
            Assert.NotNull(evaluatorType);
            Assert.NotNull(wordType);

            Component manager = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren(managerType, true))
                .Cast<Component>().Single();
            FieldInfo storiesField = managerType.GetField("cuentosDisponibles", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo wordsField = managerType.GetField("palabrasMostradas", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo showReading = managerType.GetMethod("MostrarLectura", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo clear = managerType.GetMethod("ClearRecognizedText");
            MethodInfo append = managerType.GetMethod("AppendFinalRecognizedFragment", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo getNormalizedWords = evaluatorType.GetMethod("GetNormalizedWords", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(storiesField);
            Assert.NotNull(wordsField);
            Assert.NotNull(showReading);
            Assert.NotNull(clear);
            Assert.NotNull(append);
            Assert.NotNull(getNormalizedWords);

            IList stories = (IList)storiesField.GetValue(manager);
            Assert.GreaterOrEqual(stories.Count, 3);
            FieldInfo id = storyType.GetField("id");
            FieldInfo text = storyType.GetField("textoCompleto");
            FieldInfo normalizedText = wordType.GetField("textoNormalizado");
            Assert.NotNull(id);
            Assert.NotNull(text);
            Assert.NotNull(normalizedText);

            for (int index = 0; index < stories.Count; index++)
            {
                object selectedStory = stories[index];
                object otherStory = stories[(index + 1) % stories.Count];
                string[] selectedWords = (string[])getNormalizedWords.Invoke(null, new[] { text.GetValue(selectedStory) });
                string[] otherWords = (string[])getNormalizedWords.Invoke(null, new[] { text.GetValue(otherStory) });
                string allowedWord = selectedWords.First();
                string foreignWord = otherWords.First(word => Array.IndexOf(selectedWords, word) < 0);

                showReading.Invoke(manager, new[] { selectedStory });
                clear.Invoke(manager, null);
                append.Invoke(manager, new object[] { $"{allowedWord} {foreignWord}" });

                IList recognizedWords = (IList)wordsField.GetValue(manager);
                string[] recognizedNormalized = recognizedWords.Cast<object>()
                    .Select(word => normalizedText.GetValue(word).ToString())
                    .ToArray();
                Assert.Contains(allowedWord, recognizedNormalized, $"{id.GetValue(selectedStory)} debe conservar su vocabulario.");
                Assert.IsFalse(recognizedNormalized.Contains(foreignWord), $"{id.GetValue(selectedStory)} no debe aceptar vocabulario de otro cuento.");
            }
        }

        [UnityTest]
        public IEnumerator PanelTransition_OpensClosesAndRestoresInteraction()
        {
            Type panelType = Type.GetType("Bolin.UIPanelTransition, Assembly-CSharp");
            Assert.NotNull(panelType);
            GameObject panel = new("PanelTransitionTest", typeof(RectTransform), typeof(CanvasGroup));
            CanvasGroup group = panel.GetComponent<CanvasGroup>();
            Component transition = panel.AddComponent(panelType);
            SetPrivateField(transition, "canvasGroup", group);
            SetPrivateField(transition, "target", panel.GetComponent<RectTransform>());
            SetPrivateField(transition, "duration", 0.05f);

            panelType.GetMethod("SetImmediate").Invoke(transition, new object[] { false });
            Assert.IsFalse(panel.activeSelf);
            panelType.GetMethod("Show").Invoke(transition, null);
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.IsTrue(panel.activeSelf);
            Assert.AreEqual(1f, group.alpha, 0.01f);
            Assert.IsTrue(group.interactable && group.blocksRaycasts);

            panelType.GetMethod("Hide").Invoke(transition, null);
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.IsFalse(panel.activeSelf);
            UnityEngine.Object.Destroy(panel);
        }

        [UnityTest]
        public IEnumerator StarDisplay_UpdatesThreeExistingImages()
        {
            Type starType = Type.GetType("Bolin.UIStarDisplay, Assembly-CSharp");
            Assert.NotNull(starType);
            GameObject root = new("StarDisplayTest", typeof(RectTransform));
            Image[] stars = new Image[3];
            for (int i = 0; i < stars.Length; i++)
            {
                GameObject star = new($"Star{i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                star.transform.SetParent(root.transform, false);
                stars[i] = star.GetComponent<Image>();
            }

            Component display = root.AddComponent(starType);
            SetPrivateField(display, "stars", stars);
            starType.GetMethod("SetImmediate").Invoke(display, new object[] { 2 });
            Assert.AreEqual(1f, stars[0].color.a, 0.01f);
            Assert.AreEqual(1f, stars[1].color.a, 0.01f);
            Assert.Less(stars[2].color.a, 1f);
            starType.GetMethod("ShowStars").Invoke(display, new object[] { 3, true });
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsTrue(stars.All(star => Vector3.Distance(star.rectTransform.localScale, Vector3.one) < 0.01f));
            UnityEngine.Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator StoryTutorial_AppearsOnceAndBlocksLevelActions()
        {
            const string key = "MundoCuentos_TutorialVisto";
            bool hadKey = PlayerPrefs.HasKey(key);
            int previousValue = PlayerPrefs.GetInt(key, 0);

            try
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();

                yield return SceneManager.LoadSceneAsync("MundoCuentos_VozTest", LoadSceneMode.Single);
                yield return new WaitForSecondsRealtime(0.45f);

                Component tutorial = FindSingleInActiveScene("Bolin.StoryTutorialAnimationController");
                Assert.IsTrue((bool)GetPublicProperty(tutorial, "IsActive"));
                CanvasGroup overlayGroup = tutorial.GetComponent<CanvasGroup>();
                Assert.NotNull(overlayGroup);
                Assert.IsTrue(overlayGroup.blocksRaycasts);

                Component manager = FindSingleInActiveScene("Bolin.VoiceRecognitionTest");
                manager.GetType().GetMethod("StartListening").Invoke(manager, null);
                yield return null;
                Assert.IsFalse((bool)GetPrivateField(manager, "isListeningSession"));

                tutorial.GetType().GetMethod("CompleteTutorial").Invoke(tutorial, null);
                yield return new WaitForSecondsRealtime(0.35f);
                Assert.AreEqual(1, PlayerPrefs.GetInt(key, 0));
                Assert.IsFalse((bool)GetPublicProperty(tutorial, "IsActive"));

                yield return SceneManager.LoadSceneAsync("MundoCuentos_VozTest", LoadSceneMode.Single);
                yield return new WaitForSecondsRealtime(0.25f);

                Component reloadedTutorial = FindSingleInActiveScene("Bolin.StoryTutorialAnimationController");
                Assert.IsFalse((bool)GetPublicProperty(reloadedTutorial, "IsActive"));
                Assert.IsFalse(reloadedTutorial.gameObject.activeSelf);
            }
            finally
            {
                if (hadKey) PlayerPrefs.SetInt(key, previousValue);
                else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        [UnityTest]
        public IEnumerator MusicalTutorial_AppearsOnceAndPreparesPianoWithoutAutoPlaying()
        {
            const string key = "MundoMusical_TutorialVisto";
            bool hadKey = PlayerPrefs.HasKey(key);
            int previousValue = PlayerPrefs.GetInt(key, 0);

            try
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();

                yield return SceneManager.LoadSceneAsync("MundoMusical", LoadSceneMode.Single);
                yield return new WaitForSecondsRealtime(0.45f);

                Component tutorial = FindSingleInActiveScene("Bolin.MusicalTutorialController");
                Component game = FindSingleInActiveScene("Assets.SurpriseBox.Scripts.MundoMusicalSequenceGame");
                Assert.IsTrue((bool)GetPublicProperty(tutorial, "IsActive"));
                Assert.IsFalse((bool)GetPrivateField(game, "activityStarted"));
                Assert.IsNull(GetPrivateField(game, "sequenceRoutine"));

                for (int index = 0; index < 4; index++) tutorial.GetType().GetMethod("NextStep").Invoke(tutorial, null);
                tutorial.GetType().GetMethod("CompleteTutorial").Invoke(tutorial, null);
                yield return new WaitForSecondsRealtime(0.42f);

                Assert.AreEqual(1, PlayerPrefs.GetInt(key, 0));
                Assert.IsFalse((bool)GetPublicProperty(tutorial, "IsActive"));
                Assert.IsFalse(tutorial.gameObject.activeSelf);
                Assert.IsTrue((bool)GetPrivateField(game, "activityStarted"));
                Assert.IsFalse((bool)GetPrivateField(game, "acceptingInput"));
                Assert.IsNull(GetPrivateField(game, "sequenceRoutine"), "El tutorial solo prepara el piano; no debe reproducir automaticamente.");

                IList keys = (IList)GetPrivateField(game, "keyButtons");
                Assert.IsTrue(keys.Cast<Button>().All(button => !button.interactable));
                Button listen = (Button)GetPrivateField(game, "startButton");
                Assert.IsTrue(listen.gameObject.activeInHierarchy && listen.interactable);
                Component listenGuide = listen.GetComponent(Type.GetType("Bolin.MusicalListenButtonGuide, Assembly-CSharp"));
                Assert.NotNull(listenGuide, "ESCUCHAR debe tener una guía visual persistente tras el tutorial.");
                Image listenGlow = (Image)GetPrivateField(listenGuide, "glowImage");
                Assert.IsTrue((bool)GetPublicProperty(listenGuide, "IsGuiding"));
                Assert.IsTrue(listenGlow.gameObject.activeInHierarchy);
                Assert.Greater(listenGlow.color.a, 0.05f);

                game.GetType().GetMethod("PlayCurrentSequence").Invoke(game, null);
                yield return null;
                Assert.NotNull(GetPrivateField(game, "sequenceRoutine"));
                Assert.IsFalse(listenGlow.gameObject.activeSelf, "El brillo debe detenerse al pulsar Escuchar.");

                yield return SceneManager.LoadSceneAsync("MundoMusical", LoadSceneMode.Single);
                yield return new WaitForSecondsRealtime(0.28f);
                Component returningTutorial = FindSingleInActiveScene("Bolin.MusicalTutorialController");
                Component returningGame = FindSingleInActiveScene("Assets.SurpriseBox.Scripts.MundoMusicalSequenceGame");
                Assert.IsFalse((bool)GetPublicProperty(returningTutorial, "IsActive"));
                Assert.IsFalse(returningTutorial.gameObject.activeSelf);
                Assert.IsTrue((bool)GetPrivateField(returningGame, "activityStarted"));
                Assert.IsNull(GetPrivateField(returningGame, "sequenceRoutine"));
            }
            finally
            {
                if (hadKey) PlayerPrefs.SetInt(key, previousValue);
                else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        [UnityTest]
        public IEnumerator MusicalUi_StaysWithinCanvasAtRequiredResolutions()
        {
            const string tutorialKey = "MundoMusical_TutorialVisto";
            bool hadKey = PlayerPrefs.HasKey(tutorialKey);
            int previousValue = PlayerPrefs.GetInt(tutorialKey, 0);
            Vector2Int[] resolutions = { new(1920, 1080), new(1366, 768), new(1280, 720) };

            try
            {
                PlayerPrefs.SetInt(tutorialKey, 1);
                PlayerPrefs.Save();
                foreach (Vector2Int resolution in resolutions)
                {
                    Screen.SetResolution(resolution.x, resolution.y, false);
                    yield return SceneManager.LoadSceneAsync("MundoMusical", LoadSceneMode.Single);
                    yield return new WaitForSecondsRealtime(0.35f);
                    Canvas.ForceUpdateCanvases();

                    Canvas canvas = SceneManager.GetActiveScene().GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<Canvas>(true))
                        .Single(item => item.isRootCanvas);
                    RectTransform[] controls = SceneManager.GetActiveScene().GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<RectTransform>(false))
                        .Where(rect => rect.GetComponent<Button>() != null || rect.GetComponent<TMPro.TMP_Text>() != null)
                        .ToArray();
                    Assert.IsNotEmpty(controls);
                    foreach (RectTransform control in controls)
                    {
                        Vector2 point = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, control.TransformPoint(control.rect.center));
                        Assert.That(point.x, Is.InRange(-2f, Screen.width + 2f), $"{resolution}: {control.name} sale horizontalmente.");
                        Assert.That(point.y, Is.InRange(-2f, Screen.height + 2f), $"{resolution}: {control.name} sale verticalmente.");
                    }

                    RectTransform pianoFrame = SceneManager.GetActiveScene().GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<RectTransform>(true))
                        .Single(item => item.name == "PianoFrame");
                    Button[] pianoKeys = SceneManager.GetActiveScene().GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<Button>(true))
                        .Where(button => button.name.StartsWith("Key_", StringComparison.Ordinal))
                        .OrderBy(button => button.name)
                        .ToArray();
                    Assert.AreEqual(7, pianoKeys.Length, $"{resolution}: faltan teclas musicales.");

                    Vector3[] pianoCorners = new Vector3[4];
                    pianoFrame.GetWorldCorners(pianoCorners);
                    float pianoLeft = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, pianoCorners[0]).x;
                    float pianoRight = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, pianoCorners[2]).x;
                    float pianoBottom = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, pianoCorners[0]).y;
                    float pianoTop = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, pianoCorners[2]).y;
                    float previousRight = float.NegativeInfinity;
                    foreach (Button key in pianoKeys)
                    {
                        RectTransform keyRect = key.transform as RectTransform;
                        Vector3[] corners = new Vector3[4];
                        keyRect.GetWorldCorners(corners);
                        float left = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[0]).x;
                        float right = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[2]).x;
                        float bottom = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[0]).y;
                        float top = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[2]).y;
                        Assert.GreaterOrEqual(left, pianoLeft - 1f, $"{resolution}: {key.name} sale del piano.");
                        Assert.LessOrEqual(right, pianoRight + 1f, $"{resolution}: {key.name} sale del piano.");
                        Assert.GreaterOrEqual(bottom, pianoBottom - 1f, $"{resolution}: {key.name} sale del piano.");
                        Assert.LessOrEqual(top, pianoTop + 1f, $"{resolution}: {key.name} sale del piano.");
                        Assert.GreaterOrEqual(left, previousRight + 2f, $"{resolution}: {key.name} se superpone con la tecla anterior.");
                        previousRight = right;
                    }
                }
            }
            finally
            {
                if (hadKey) PlayerPrefs.SetInt(tutorialKey, previousValue);
                else PlayerPrefs.DeleteKey(tutorialKey);
                PlayerPrefs.Save();
            }
        }

        [UnityTest]
        public IEnumerator MusicalPiano_ShowsSevenFixedNumericKeysInPlayMode()
        {
            const string tutorialKey = "MundoMusical_TutorialVisto";
            bool hadKey = PlayerPrefs.HasKey(tutorialKey);
            int previousValue = PlayerPrefs.GetInt(tutorialKey, 0);
            string[] expectedLabels = { "1", "2", "3", "4", "5", "6", "7" };

            try
            {
                PlayerPrefs.SetInt(tutorialKey, 1);
                PlayerPrefs.Save();

                yield return SceneManager.LoadSceneAsync("MundoMusical", LoadSceneMode.Single);
                yield return new WaitForSecondsRealtime(0.35f);
                Canvas.ForceUpdateCanvases();

                Component game = FindSingleInActiveScene("Assets.SurpriseBox.Scripts.MundoMusicalSequenceGame");
                IList keys = (IList)GetPrivateField(game, "keyButtons");
                IList labels = (IList)GetPrivateField(game, "tmpKeyLabels");
                Assert.AreEqual(expectedLabels.Length, keys.Count);
                Assert.AreEqual(expectedLabels.Length, labels.Count);

                Canvas canvas = SceneManager.GetActiveScene().GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Canvas>(true))
                    .Single(item => item.isRootCanvas);
                EventSystem eventSystem = SceneManager.GetActiveScene().GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<EventSystem>(true))
                    .FirstOrDefault();
                Assert.NotNull(eventSystem);
                PointerEventData pointer = new(eventSystem);
                Type visualType = Type.GetType("Bolin.MusicalKeyVisual, Assembly-CSharp");
                Assert.NotNull(visualType);

                RectTransform pianoFrame = SceneManager.GetActiveScene().GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<RectTransform>(true))
                    .Single(item => item.name == "PianoFrame");
                Vector3[] pianoCorners = new Vector3[4];
                pianoFrame.GetWorldCorners(pianoCorners);
                float pianoLeft = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, pianoCorners[0]).x;
                float pianoRight = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, pianoCorners[2]).x;
                float pianoBottom = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, pianoCorners[0]).y;
                float pianoTop = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, pianoCorners[2]).y;

                Vector2[] basePositions = new Vector2[expectedLabels.Length];
                Vector3[] baseScales = new Vector3[expectedLabels.Length];
                Quaternion[] baseRotations = new Quaternion[expectedLabels.Length];
                float previousRight = float.NegativeInfinity;

                for (int index = 0; index < expectedLabels.Length; index++)
                {
                    Button key = keys[index] as Button;
                    TMPro.TMP_Text label = labels[index] as TMPro.TMP_Text;
                    Assert.NotNull(key);
                    Assert.NotNull(label);
                    Assert.AreEqual("Key_" + (index + 1), key.name);
                    Assert.AreEqual(expectedLabels[index], label.text);
                    Assert.IsTrue(key.gameObject.activeInHierarchy);
                    Assert.AreEqual(TMPro.TextWrappingModes.NoWrap, label.textWrappingMode);
                    Assert.AreEqual(TMPro.TextOverflowModes.Overflow, label.overflowMode);
                    Assert.AreEqual(1, key.GetComponentsInChildren<TMPro.TMP_Text>(true).Length);
                    Assert.NotNull(key.transform.Find("KeyVisual"));

                    RectTransform keyRect = key.transform as RectTransform;
                    Assert.NotNull(keyRect, "La tecla musical no usa RectTransform.");
                    basePositions[index] = keyRect.anchoredPosition;
                    baseScales[index] = keyRect.localScale;
                    baseRotations[index] = keyRect.localRotation;
                    Vector3[] corners = new Vector3[4];
                    keyRect.GetWorldCorners(corners);
                    float left = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[0]).x;
                    float right = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[2]).x;
                    float bottom = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[0]).y;
                    float top = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[2]).y;
                    Assert.Greater(right, left + 20f, $"{key.name} no tiene ancho visible.");
                    Assert.GreaterOrEqual(left, pianoLeft - 1f, $"{key.name} sale del piano por la izquierda.");
                    Assert.LessOrEqual(right, pianoRight + 1f, $"{key.name} sale del piano por la derecha.");
                    Assert.GreaterOrEqual(bottom, pianoBottom - 1f, $"{key.name} sale del piano por abajo.");
                    Assert.LessOrEqual(top, pianoTop + 1f, $"{key.name} sale del piano por arriba.");
                    Assert.GreaterOrEqual(left, previousRight + 2f, $"{key.name} se superpone con la tecla anterior.");
                    previousRight = right;
                }

                // The layout-controlled roots must not change after repeated hover
                // cycles or clicks.  Only their KeyVisual children may animate.
                for (int index = 0; index < keys.Count; index++)
                {
                    Button key = keys[index] as Button;
                    Component visual = key.GetComponent(visualType);
                    Assert.NotNull(visual);
                    visualType.GetMethod("SetLocked").Invoke(visual, new object[] { false, false });
                    for (int cycle = 0; cycle < 10; cycle++)
                    {
                        visualType.GetMethod("OnPointerEnter").Invoke(visual, new object[] { pointer });
                        yield return null;
                        visualType.GetMethod("OnPointerExit").Invoke(visual, new object[] { pointer });
                        yield return null;
                    }

                    visualType.GetMethod("OnPointerDown").Invoke(visual, new object[] { pointer });
                    yield return null;
                    visualType.GetMethod("OnPointerUp").Invoke(visual, new object[] { pointer });
                    yield return null;

                    RectTransform keyRect = key.transform as RectTransform;
                    Assert.That(keyRect.anchoredPosition.x, Is.EqualTo(basePositions[index].x).Within(0.001f));
                    Assert.That(keyRect.anchoredPosition.y, Is.EqualTo(basePositions[index].y).Within(0.001f));
                    Assert.That(keyRect.localScale.x, Is.EqualTo(baseScales[index].x).Within(0.001f));
                    Assert.That(keyRect.localScale.y, Is.EqualTo(baseScales[index].y).Within(0.001f));
                    Assert.That(Quaternion.Angle(keyRect.localRotation, baseRotations[index]), Is.LessThan(0.01f));
                }

                // A real sequence locks keys and drives the demonstration cue. The
                // button roots still retain their exact layout positions.
                game.GetType().GetMethod("PlayCurrentSequence").Invoke(game, null);
                yield return new WaitForSecondsRealtime(0.12f);
                Canvas.ForceUpdateCanvases();
                for (int index = 0; index < keys.Count; index++)
                {
                    RectTransform keyRect = (keys[index] as Button).transform as RectTransform;
                    Assert.That(keyRect.anchoredPosition.x, Is.EqualTo(basePositions[index].x).Within(0.001f));
                    Assert.That(keyRect.anchoredPosition.y, Is.EqualTo(basePositions[index].y).Within(0.001f));
                    Assert.That(keyRect.localScale.x, Is.EqualTo(baseScales[index].x).Within(0.001f));
                    Assert.That(keyRect.localScale.y, Is.EqualTo(baseScales[index].y).Within(0.001f));
                    Assert.That(Quaternion.Angle(keyRect.localRotation, baseRotations[index]), Is.LessThan(0.01f));
                }
            }
            finally
            {
                if (hadKey) PlayerPrefs.SetInt(tutorialKey, previousValue);
                else PlayerPrefs.DeleteKey(tutorialKey);
                PlayerPrefs.Save();
            }
        }

        [UnityTest]
        public IEnumerator WorldSelection_RefreshesVisibleStarsAndLocksWhenProgressChanges()
        {
            const string completedKey = "MundoAprendo_World_0_Completed";
            const string starsKey = "MundoAprendo_World_0_Stars";
            bool hadCompleted = PlayerPrefs.HasKey(completedKey);
            bool hadStars = PlayerPrefs.HasKey(starsKey);
            int oldCompleted = PlayerPrefs.GetInt(completedKey, 0);
            int oldStars = PlayerPrefs.GetInt(starsKey, 0);

            try
            {
                PlayerPrefs.DeleteKey(completedKey);
                PlayerPrefs.DeleteKey(starsKey);
                PlayerPrefs.Save();
                yield return SceneManager.LoadSceneAsync("SeleccionMundos", LoadSceneMode.Single);
                yield return null;

                Type managerType = Type.GetType("Bolin.WorldSelectionManager, Assembly-CSharp");
                Type repositoryType = Type.GetType("Bolin.WorldProgressRepository, Assembly-CSharp");
                Assert.NotNull(managerType);
                Assert.NotNull(repositoryType);
                Component manager = SceneManager.GetActiveScene().GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren(managerType, true))
                    .Cast<Component>().Single();
                Array worlds = (Array)GetPrivateField(manager, "worlds");
                Assert.AreEqual(4, worlds.Length);
                Button musical = (Button)worlds.GetValue(0).GetType().GetField("worldButton").GetValue(worlds.GetValue(0));
                Button stories = (Button)worlds.GetValue(1).GetType().GetField("worldButton").GetValue(worlds.GetValue(1));
                Assert.IsTrue(musical.interactable, "Mundo Musical siempre queda disponible.");
                Assert.IsFalse(stories.interactable);

                repositoryType.GetMethod("SaveBestResult", new[] { typeof(int), typeof(int), typeof(bool) })
                    .Invoke(null, new object[] { 0, 2, true });
                yield return null;
                Assert.IsTrue(stories.interactable, "Completar Musical debe desbloquear Cuentos sin recargar la escena.");
                Type sharedPanelType = Type.GetType("Bolin.WorldStarsPanelView, Assembly-CSharp");
                Type displayType = Type.GetType("Bolin.UIStarDisplay, Assembly-CSharp");
                Assert.NotNull(sharedPanelType);
                Assert.NotNull(displayType);
                Component sharedPanel = SceneManager.GetActiveScene().GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren(sharedPanelType, true))
                    .Cast<Component>().Single();
                Component sharedDisplay = sharedPanel.GetComponent(displayType);
                Assert.NotNull(sharedDisplay, "La seleccion debe usar un unico UIStarDisplay compartido.");
                sharedPanelType.GetMethod("FocusWorld").Invoke(sharedPanel, new object[] { 0 });
                yield return null;
                Image[] starImages = (Image[])GetPrivateField(sharedDisplay, "stars");
                Sprite fullStar = (Sprite)GetPrivateField(sharedDisplay, "earnedSprite");
                Assert.AreEqual(fullStar, starImages[0].sprite);
                Assert.AreEqual(fullStar, starImages[1].sprite);

                sharedPanelType.GetMethod("FocusWorld").Invoke(sharedPanel, new object[] { 2 });
                yield return null;
                Sprite emptyStar = (Sprite)GetPrivateField(sharedDisplay, "unearnedSprite");
                Assert.IsTrue(starImages.All(item => item.sprite == emptyStar), "Un mundo bloqueado muestra las tres estrellas vacias.");

                repositoryType.GetMethod("ResetWorld", new[] { typeof(int), typeof(bool) }).Invoke(null, new object[] { 0, true });
                yield return null;
                Assert.IsTrue(musical.interactable);
                Assert.IsFalse(stories.interactable, "Al borrar Musical, Cuentos debe volver a bloquearse inmediatamente.");
                sharedPanelType.GetMethod("FocusWorld").Invoke(sharedPanel, new object[] { 1 });
                yield return null;
                Assert.IsTrue(starImages.All(item => item.sprite == emptyStar), "El panel compartido se vacia cuando el foco vuelve a un mundo bloqueado.");
            }
            finally
            {
                if (hadCompleted) PlayerPrefs.SetInt(completedKey, oldCompleted);
                else PlayerPrefs.DeleteKey(completedKey);
                if (hadStars) PlayerPrefs.SetInt(starsKey, oldStars);
                else PlayerPrefs.DeleteKey(starsKey);
                PlayerPrefs.Save();
            }
        }

        [UnityTest]
        public IEnumerator MusicalResult_RevealsRealStarsBeforeDecorativeLoop()
        {
            const string tutorialKey = "MundoMusical_TutorialVisto";
            const string completedKey = "MundoAprendo_World_0_Completed";
            const string starsKey = "MundoAprendo_World_0_Stars";
            (bool had, int value)[] previous =
            {
                (PlayerPrefs.HasKey(tutorialKey), PlayerPrefs.GetInt(tutorialKey, 0)),
                (PlayerPrefs.HasKey(completedKey), PlayerPrefs.GetInt(completedKey, 0)),
                (PlayerPrefs.HasKey(starsKey), PlayerPrefs.GetInt(starsKey, 0))
            };

            try
            {
                PlayerPrefs.SetInt(tutorialKey, 1);
                PlayerPrefs.DeleteKey(completedKey);
                PlayerPrefs.DeleteKey(starsKey);
                PlayerPrefs.Save();
                yield return SceneManager.LoadSceneAsync("MundoMusical", LoadSceneMode.Single);
                yield return new WaitForSecondsRealtime(0.3f);

                Component game = FindSingleInActiveScene("Assets.SurpriseBox.Scripts.MundoMusicalSequenceGame");
                Component decorative = FindSingleInActiveScene("Bolin.StoryResultStarAnimation");
                int childrenBefore = decorative.GetComponentsInChildren<Transform>(true).Length;
                game.GetType().GetMethod("CompleteActivity").Invoke(game, null);
                yield return new WaitForSecondsRealtime(0.55f);

                Image mainDecoration = (Image)GetPrivateField(decorative, "mainStarImage");
                Assert.LessOrEqual(mainDecoration.color.a, 0.01f, "La decoracion no debe adelantarse a las estrellas reales.");
                Image[] finalStars = (Image[])GetPrivateField(game, "starImages");
                Assert.IsTrue(finalStars.Any(star => star.rectTransform.localScale.x > 0.4f));

                yield return new WaitForSecondsRealtime(1.15f);
                Assert.Greater(mainDecoration.color.a, 0.1f);
                Assert.AreEqual(childrenBefore, decorative.GetComponentsInChildren<Transform>(true).Length,
                    "La celebracion reutiliza objetos persistentes; no instancia estrellas en runtime.");
            }
            finally
            {
                string[] keys = { tutorialKey, completedKey, starsKey };
                for (int index = 0; index < keys.Length; index++)
                {
                    if (previous[index].had) PlayerPrefs.SetInt(keys[index], previous[index].value);
                    else PlayerPrefs.DeleteKey(keys[index]);
                }
                PlayerPrefs.Save();
            }
        }

        [UnityTest]
        public IEnumerator MusicalListen_CancelsPendingAutoAdvanceBeforeReplay()
        {
            const string tutorialKey = "MundoMusical_TutorialVisto";
            bool hadKey = PlayerPrefs.HasKey(tutorialKey);
            int previousValue = PlayerPrefs.GetInt(tutorialKey, 0);

            try
            {
                PlayerPrefs.SetInt(tutorialKey, 1);
                PlayerPrefs.Save();
                yield return SceneManager.LoadSceneAsync("MundoMusical", LoadSceneMode.Single);
                yield return new WaitForSecondsRealtime(0.3f);

                Component game = FindSingleInActiveScene("Assets.SurpriseBox.Scripts.MundoMusicalSequenceGame");
                SetPrivateField(game, "activityStarted", true);
                SetPrivateField(game, "activityFinished", false);
                SetPrivateField(game, "acceptingInput", true);
                SetPrivateField(game, "currentSequenceIndex", 0);
                SetPrivateField(game, "expectedStepIndex", 2);
                MethodInfo pressKey = game.GetType().GetMethod("OnKeyPressed", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(pressKey);
                pressKey.Invoke(game, new object[] { 2 });
                Assert.NotNull(GetPrivateField(game, "advanceRoutine"));

                game.GetType().GetMethod("PlayCurrentSequence").Invoke(game, null);
                yield return new WaitForSecondsRealtime(1.08f);
                Assert.IsNull(GetPrivateField(game, "advanceRoutine"));
                Assert.AreEqual(0, (int)GetPrivateField(game, "currentSequenceIndex"));
                Assert.IsFalse((bool)GetPrivateField(game, "activityFinished"));
            }
            finally
            {
                if (hadKey) PlayerPrefs.SetInt(tutorialKey, previousValue);
                else PlayerPrefs.DeleteKey(tutorialKey);
                PlayerPrefs.Save();
            }
        }

        [UnityTest]
        public IEnumerator MusicalWrongKey_FeedbackRemainsVisibleWhileInputLocks()
        {
            const string tutorialKey = "MundoMusical_TutorialVisto";
            bool hadKey = PlayerPrefs.HasKey(tutorialKey);
            int previousValue = PlayerPrefs.GetInt(tutorialKey, 0);

            try
            {
                PlayerPrefs.SetInt(tutorialKey, 1);
                PlayerPrefs.Save();
                yield return SceneManager.LoadSceneAsync("MundoMusical", LoadSceneMode.Single);
                yield return new WaitForSecondsRealtime(0.3f);

                Component game = FindSingleInActiveScene("Assets.SurpriseBox.Scripts.MundoMusicalSequenceGame");
                SetPrivateField(game, "activityStarted", true);
                SetPrivateField(game, "acceptingInput", true);
                SetPrivateField(game, "expectedStepIndex", 0);
                MethodInfo pressKey = game.GetType().GetMethod("OnKeyPressed", BindingFlags.Instance | BindingFlags.NonPublic);
                pressKey.Invoke(game, new object[] { 1 });
                yield return null;

                Button wrongKey = SceneManager.GetActiveScene().GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Button>(true))
                    .Single(button => button.name == "Key_2");
                Component visual = wrongKey.GetComponent(Type.GetType("Bolin.MusicalKeyVisual, Assembly-CSharp"));
                Assert.IsFalse(wrongKey.interactable);
                Assert.NotNull(GetPrivateField(visual, "feedbackRoutine"), "El feedback rojo no debe cancelarse al bloquear el piano.");
                Assert.AreEqual(2, (int)GetPrivateField(game, "currentStars"));

                Component topStars = (Component)GetPrivateField(game, "topStarDisplay");
                Image[] topStarImages = (Image[])GetPrivateField(topStars, "stars");
                Sprite emptyStar = (Sprite)GetPrivateField(topStars, "unearnedSprite");
                Assert.AreEqual(emptyStar, topStarImages[2].sprite,
                    "La tercera estrella visible superior debe vaciarse al primer error.");
            }
            finally
            {
                if (hadKey) PlayerPrefs.SetInt(tutorialKey, previousValue);
                else PlayerPrefs.DeleteKey(tutorialKey);
                PlayerPrefs.Save();
            }
        }

        [UnityTest]
        public IEnumerator StoryResultStarAnimation_ReusesPersistentStarsAndStopsLoop()
        {
            const string key = "MundoCuentos_TutorialVisto";
            bool hadKey = PlayerPrefs.HasKey(key);
            int previousValue = PlayerPrefs.GetInt(key, 0);

            try
            {
                Type progressType = Type.GetType("Bolin.StoryProgressRepository, Assembly-CSharp");
                Assert.NotNull(progressType);
                progressType.GetMethod("MarkTutorialCompleted").Invoke(null, new object[] { true });
                yield return SceneManager.LoadSceneAsync("MundoCuentos_VozTest", LoadSceneMode.Single);
                yield return null;

                Component manager = FindSingleInActiveScene("Bolin.VoiceRecognitionTest");
                string defaultStoryId = (string)progressType.GetField("DefaultStoryId").GetRawConstantValue();
                manager.GetType().GetMethod("OpenStoryById").Invoke(manager, new object[] { defaultStoryId });
                yield return null;

                MethodInfo showResult = manager.GetType().GetMethod("MostrarResultado", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(showResult);
                showResult.Invoke(manager, new object[] { 90, 3 });
                yield return null;

                Component animation = FindSingleInActiveScene("Bolin.StoryResultStarAnimation");
                RectTransform[] floatingStars = animation.GetComponentsInChildren<RectTransform>(true)
                    .Where(rect => rect.name.StartsWith("FloatingStar_", StringComparison.Ordinal))
                    .ToArray();
                Assert.GreaterOrEqual(floatingStars.Length, 5);

                Image[] floatingImages = floatingStars.Select(star => star.GetComponent<Image>()).Where(image => image != null).ToArray();
                Assert.AreEqual(floatingStars.Length, floatingImages.Length);
                int childCount = animation.transform.GetComponentsInChildren<Transform>(true).Length;
                Vector2[] initialPositions = floatingStars.Select(star => star.anchoredPosition).ToArray();

                yield return new WaitForSecondsRealtime(1.35f);

                Assert.AreEqual(childCount, animation.transform.GetComponentsInChildren<Transform>(true).Length);
                Assert.IsTrue(floatingImages.Any(image => image.color.a > 0.05f));
                Assert.IsTrue(floatingStars.Zip(initialPositions, (star, initial) =>
                    star.anchoredPosition.y > initial.y + 5f && Mathf.Abs(star.anchoredPosition.x - initial.x) > 0.5f).Any(moved => moved));

                animation.GetType().GetMethod("StopAndReset").Invoke(animation, null);
                yield return null;
                CanvasGroup loopGroup = animation.GetComponent<CanvasGroup>();
                Assert.NotNull(loopGroup);
                Assert.AreEqual(0f, loopGroup.alpha, 0.01f);
                Assert.IsTrue(floatingImages.All(image => image.color.a <= 0.01f));
                Assert.IsTrue(floatingStars.Zip(initialPositions, (star, initial) =>
                    Vector2.Distance(star.anchoredPosition, initial) < 0.1f).All(restored => restored));

                animation.GetType().GetMethod("Play").Invoke(animation, new object[] { 3 });
                yield return new WaitForSecondsRealtime(0.2f);
                Assert.AreEqual(childCount, animation.transform.GetComponentsInChildren<Transform>(true).Length);
                animation.GetType().GetMethod("StopAndReset").Invoke(animation, null);
            }
            finally
            {
                if (hadKey) PlayerPrefs.SetInt(key, previousValue);
                else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        [UnityTest]
        public IEnumerator SizeWorld_SafariUi_StartsOneRoundWithTwoInteractiveCards()
        {
            const string tutorialKey = "MundoTamanos_TutorialVisto";
            bool hadTutorialKey = PlayerPrefs.HasKey(tutorialKey);
            int previousTutorialValue = PlayerPrefs.GetInt(tutorialKey, 0);

            try
            {
                // Esta prueba verifica la ronda, no el tutorial: al marcarlo como visto
                // evita que el overlay bloquee las tarjetas en el modo Play.
                PlayerPrefs.SetInt(tutorialKey, 1);
                PlayerPrefs.Save();

                yield return SceneManager.LoadSceneAsync("MundoTamanos", LoadSceneMode.Single);
                yield return null;

                Component tutorial = FindSingleInActiveScene("Bolin.SizeWorldTutorialController");
                Assert.IsFalse((bool)GetPublicProperty(tutorial, "IsTutorialActive"));

                Component controller = FindSingleInActiveScene("Bolin.SizeWorldController");
                MethodInfo startActivity = controller.GetType().GetMethod("StartActivity", BindingFlags.Instance | BindingFlags.Public);
                Assert.NotNull(startActivity);
                startActivity.Invoke(controller, null);

                // La entrada de animales dura 0.65 s por configuracion; se espera un margen
                // para comprobar el estado final sin depender de un habitat aleatorio concreto.
                yield return new WaitForSecondsRealtime(0.9f);
                Canvas.ForceUpdateCanvases();

                Image background = (Image)GetPrivateField(controller, "backgroundImage");
                Image leftAnimal = (Image)GetPrivateField(controller, "leftAnimalImage");
                Image rightAnimal = (Image)GetPrivateField(controller, "rightAnimalImage");
                Button leftButton = (Button)GetPrivateField(controller, "leftAnimalButton");
                Button rightButton = (Button)GetPrivateField(controller, "rightAnimalButton");
                RectTransform safeArea = (RectTransform)GetPrivateField(controller, "animalSafeArea");
                TMPro.TMP_Text question = (TMPro.TMP_Text)GetPrivateField(controller, "questionText");
                TMPro.TMP_Text leftName = (TMPro.TMP_Text)GetPrivateField(controller, "leftAnimalNameText");
                TMPro.TMP_Text rightName = (TMPro.TMP_Text)GetPrivateField(controller, "rightAnimalNameText");

                Assert.NotNull(background.sprite, "La ronda debe aplicar un fondo de Mundo Safari al iniciar.");
                Assert.IsTrue(background.enabled);
                Assert.NotNull(leftAnimal.sprite);
                Assert.NotNull(rightAnimal.sprite);
                Assert.IsTrue(leftAnimal.rectTransform.IsChildOf(safeArea));
                Assert.IsTrue(rightAnimal.rectTransform.IsChildOf(safeArea));
                Assert.IsTrue(leftButton.gameObject.activeInHierarchy && leftButton.interactable);
                Assert.IsTrue(rightButton.gameObject.activeInHierarchy && rightButton.interactable);
                Assert.IsFalse(string.IsNullOrWhiteSpace(question.text));
                Assert.IsFalse(string.IsNullOrWhiteSpace(leftName.text));
                Assert.IsFalse(string.IsNullOrWhiteSpace(rightName.text));

                TMPro.TMP_Text leftAnswerLabel = leftButton.GetComponentsInChildren<TMPro.TMP_Text>(true)
                    .SingleOrDefault(item => item.name == "AnswerLabel");
                TMPro.TMP_Text rightAnswerLabel = rightButton.GetComponentsInChildren<TMPro.TMP_Text>(true)
                    .SingleOrDefault(item => item.name == "AnswerLabel");
                Image leftAnswerIcon = leftButton.GetComponentsInChildren<Image>(true)
                    .SingleOrDefault(item => item.name == "AnswerIcon");
                Image rightAnswerIcon = rightButton.GetComponentsInChildren<Image>(true)
                    .SingleOrDefault(item => item.name == "AnswerIcon");
                Assert.NotNull(leftAnswerLabel, "La respuesta izquierda necesita una etiqueta visual dinamica.");
                Assert.NotNull(rightAnswerLabel, "La respuesta derecha necesita una etiqueta visual dinamica.");
                Assert.NotNull(leftAnswerIcon, "La respuesta izquierda necesita un icono visual dinamico.");
                Assert.NotNull(rightAnswerIcon, "La respuesta derecha necesita un icono visual dinamico.");
                Assert.AreEqual(leftName.text, leftAnswerLabel.text, "La etiqueta inferior debe repetir el animal izquierdo de la ronda.");
                Assert.AreEqual(rightName.text, rightAnswerLabel.text, "La etiqueta inferior debe repetir el animal derecho de la ronda.");
                Assert.AreEqual(leftAnimal.sprite, leftAnswerIcon.sprite, "El icono inferior debe copiar el animal izquierdo de la ronda.");
                Assert.AreEqual(rightAnimal.sprite, rightAnswerIcon.sprite, "El icono inferior debe copiar el animal derecho de la ronda.");

                Transform bottomStars = SceneManager.GetActiveScene().GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .SingleOrDefault(item => item.name == "BottomStars");
                Assert.NotNull(bottomStars, "La UI debe mantener el panel inferior de estrellas.");
                Image[] lowerStars = bottomStars.GetComponentsInChildren<Image>(true)
                    .Where(item => item.name.StartsWith("Estrella", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                Assert.AreEqual(3, lowerStars.Length, "El panel inferior debe mostrar tres estrellas.");
                Assert.IsTrue(lowerStars.All(item => item.sprite != null), "Las estrellas inferiores deben sincronizar su sprite al iniciar la ronda.");
            }
            finally
            {
                if (hadTutorialKey) PlayerPrefs.SetInt(tutorialKey, previousTutorialValue);
                else PlayerPrefs.DeleteKey(tutorialKey);
                PlayerPrefs.Save();
            }
        }

        [UnityTest]
        public IEnumerator ActiveControls_RemainInsideCanvasAtRequiredResolutions()
        {
            Vector2Int[] resolutions =
            {
                new(1920, 1080),
                new(1366, 768),
                new(1280, 720)
            };

            foreach (Vector2Int resolution in resolutions)
            {
                Screen.SetResolution(resolution.x, resolution.y, false);
                yield return null;
                foreach (string sceneName in SceneNames)
                {
                    yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                    yield return new WaitForSecondsRealtime(0.9f);
                    Canvas.ForceUpdateCanvases();

                    Scene scene = SceneManager.GetActiveScene();
                    GameObject[] roots = scene.GetRootGameObjects();
                    RectTransform[] controls = roots
                        .SelectMany(root => root.GetComponentsInChildren<RectTransform>(false))
                        .Where(rect => rect.GetComponent<Button>() != null || rect.GetComponent<TMPro.TMP_Text>() != null)
                        .ToArray();
                    foreach (RectTransform control in controls)
                    {
                        Canvas canvas = control.GetComponentInParent<Canvas>();
                        if (canvas == null || !canvas.isRootCanvas && canvas.rootCanvas == null) continue;
                        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, control.TransformPoint(control.rect.center));
                        Assert.That(screenPoint.x, Is.InRange(-2f, Screen.width + 2f),
                            $"{sceneName} {resolution.x}x{resolution.y}: {control.name} queda fuera horizontalmente.");
                        Assert.That(screenPoint.y, Is.InRange(-2f, Screen.height + 2f),
                            $"{sceneName} {resolution.x}x{resolution.y}: {control.name} queda fuera verticalmente.");
                    }
                }
            }
        }

        private static object CreateStoryData(Type storyType, string id, string title)
        {
            object story = Activator.CreateInstance(storyType);
            storyType.GetField("id").SetValue(story, id);
            storyType.GetField("titulo").SetValue(story, title);
            storyType.GetField("textoCompleto").SetValue(story, "Texto de prueba para el cuento.");
            return story;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"No existe el campo {fieldName}.");
            field.SetValue(target, value);
        }

        private static object GetPrivateField(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"No existe el campo {fieldName}.");
            return field.GetValue(target);
        }

        private static T FindSingleInActiveScene<T>() where T : Component
        {
            return SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .Single();
        }

        private static Component FindSingleInActiveScene(string typeName)
        {
            Type type = Type.GetType($"{typeName}, Assembly-CSharp");
            Assert.NotNull(type, $"No se encontro el tipo {typeName}.");
            return SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren(type, true).Cast<Component>())
                .Single();
        }

        private static object GetPublicProperty(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(property, $"No existe la propiedad {propertyName}.");
            return property.GetValue(target);
        }
    }
}
#endif
