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
        public IEnumerator IncorrectRecognizedWords_ExpireIndependentlyAndClearCancelsTimers()
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
            SetPrivateField(manager, "incorrectWordLifetime", 0.1f);

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
            Assert.AreEqual(5, words.Count);
            Assert.AreEqual(1, words.Cast<object>().Count(word => !(bool)isCorrectField.GetValue(word)));

            yield return new WaitForSecondsRealtime(0.16f);
            Assert.AreEqual(4, words.Count);
            Assert.IsTrue(words.Cast<object>().All(word => (bool)isCorrectField.GetValue(word)));

            append.Invoke(manager, new object[] { "ruido" });
            managerType.GetMethod("ClearRecognizedText").Invoke(manager, null);
            yield return new WaitForSecondsRealtime(0.16f);
            Assert.IsEmpty(words);
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
    }
}
#endif
