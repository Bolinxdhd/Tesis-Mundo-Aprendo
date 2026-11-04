#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MundoAprendo.PlayModeTests
{
    public class EmotionSituationsPlayModeTests
    {
        [UnityTest]
        public IEnumerator IllustrationAndNuna_FitAtRequiredResolutions()
        {
            int originalWidth = Screen.width;
            int originalHeight = Screen.height;
            try
            {
                yield return SceneManager.LoadSceneAsync("MundoEmociones", LoadSceneMode.Single);
                yield return null;
                Type managerType = Type.GetType("Bolin.EmotionGameManager, Assembly-CSharp");
                Component manager = SceneManager.GetActiveScene().GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren(managerType, true)).Single();
                Button start = SceneManager.GetActiveScene().GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Button>(true))
                    .Single(button => button.name == "StartButton");
                start.onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.5f);

                RectTransform illustration = ((Image)Field(manager, "situationImage")).rectTransform;
                RectTransform nuna = (RectTransform)Field(manager, "nunaRoot");
                RectTransform canvas = SceneManager.GetActiveScene().GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Canvas>(true)).Single().GetComponent<RectTransform>();
                foreach ((int width, int height) in new[] { (1920, 1080), (1366, 768), (1280, 720) })
                {
                    Screen.SetResolution(width, height, false);
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    Vector3[] imageCorners = new Vector3[4];
                    Vector3[] nunaCorners = new Vector3[4];
                    Vector3[] canvasCorners = new Vector3[4];
                    illustration.GetWorldCorners(imageCorners);
                    nuna.GetWorldCorners(nunaCorners);
                    canvas.GetWorldCorners(canvasCorners);
                    Assert.GreaterOrEqual(imageCorners[0].x, canvasCorners[0].x, $"{width}x{height}");
                    Assert.LessOrEqual(imageCorners[2].x, canvasCorners[2].x, $"{width}x{height}");
                    Assert.GreaterOrEqual(imageCorners[0].y, canvasCorners[0].y, $"{width}x{height}");
                    Assert.LessOrEqual(imageCorners[2].y, canvasCorners[2].y, $"{width}x{height}");
                    Assert.Greater(imageCorners[0].x, nunaCorners[2].x, $"{width}x{height}");
                }
            }
            finally
            {
                Screen.SetResolution(originalWidth, originalHeight, false);
            }
        }

        [UnityTest]
        public IEnumerator TenSituations_AllowRetryCompleteAndReturnToWorlds()
        {
            const string completedKey = "MundoAprendo_World_3_Completed";
            const string starsKey = "MundoAprendo_World_3_Stars";
            bool hadCompleted = PlayerPrefs.HasKey(completedKey);
            bool hadStars = PlayerPrefs.HasKey(starsKey);
            int oldCompleted = PlayerPrefs.GetInt(completedKey);
            int oldStars = PlayerPrefs.GetInt(starsKey);

            try
            {
                yield return SceneManager.LoadSceneAsync("MundoEmociones", LoadSceneMode.Single);
                yield return null;

                Type managerType = Type.GetType("Bolin.EmotionGameManager, Assembly-CSharp");
                Assert.NotNull(managerType);
                Component manager = SceneManager.GetActiveScene().GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren(managerType, true)).Single();
                IList situations = (IList)Field(manager, "situations");
                IList answers = (IList)Field(manager, "answerButtons");
                IList expressions = (IList)Field(manager, "emotionViews");
                Image illustration = (Image)Field(manager, "situationImage");
                TMP_Text question = (TMP_Text)Field(manager, "instructionText");
                TMP_Text feedback = (TMP_Text)Field(manager, "feedbackText");
                RectTransform nuna = (RectTransform)Field(manager, "nunaRoot");
                GameObject gamePanel = (GameObject)Field(manager, "gamePanel");

                Assert.AreEqual(10, situations.Count);
                Assert.AreEqual(5, answers.Count);
                Assert.NotNull(illustration);
                Assert.NotNull(nuna);
                Assert.NotNull(nuna.GetComponentsInChildren<Image>(true).FirstOrDefault(image => image.sprite != null));
                Assert.IsTrue(illustration.preserveAspect);
                Type dialogueType = Type.GetType("Bolin.CharacterDialogueController, Assembly-CSharp");
                Component dialogue = SceneManager.GetActiveScene().GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren(dialogueType, true)).Single();
                for (int step = 0; step < 8 && (bool)dialogueType.GetProperty("IsVisible").GetValue(dialogue); step++)
                    dialogueType.GetMethod("Advance").Invoke(dialogue, null);
                Assert.IsFalse((bool)dialogueType.GetProperty("IsVisible").GetValue(dialogue));
                Button start = SceneManager.GetActiveScene().GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Button>(true))
                    .Single(button => button.name == "StartButton");
                start.onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.5f);

                for (int index = 0; index < situations.Count; index++)
                {
                    object situation = situations[index];
                    Sprite sprite = (Sprite)situation.GetType().GetField("illustration").GetValue(situation);
                    string expected = situation.GetType().GetField("emotion").GetValue(situation).ToString();
                    Assert.NotNull(sprite, $"Situación {index + 1}");
                    Assert.AreEqual($"{index + 1:00}_", sprite.name.Substring(0, 3));
                    Assert.AreSame(sprite, illustration.sprite);
                    Assert.IsTrue(illustration.enabled);
                    Assert.AreEqual("¿Cómo se siente?", question.text);
                    foreach (object expression in expressions)
                    {
                        GameObject root = (GameObject)expression.GetType().GetField("rootObject").GetValue(expression);
                        Assert.IsFalse(root.activeSelf, "La expresión no debe revelar la respuesta antes del clic.");
                    }

                    if (index == 0)
                    {
                        Button wrong = AnswerFor(answers, "Joy");
                        wrong.onClick.Invoke();
                        wrong.onClick.Invoke();
                        StringAssert.Contains("otra vez", feedback.text);
                        Assert.IsFalse((bool)dialogueType.GetProperty("IsVisible").GetValue(dialogue),
                            "Las ilustraciones antiguas no deben aparecer al equivocarse.");
                        yield return new WaitForSecondsRealtime(1.2f);
                        Assert.IsTrue(wrong.interactable);
                    }

                    Button correct = AnswerFor(answers, expected);
                    correct.onClick.Invoke();
                    correct.onClick.Invoke();
                    foreach (object expression in expressions)
                    {
                        GameObject root = (GameObject)expression.GetType().GetField("rootObject").GetValue(expression);
                        Assert.IsFalse(root.activeSelf, "La ilustración antigua no debe aparecer al acertar.");
                    }
                    Assert.AreSame(sprite, illustration.sprite, "La situación debe permanecer visible durante el acierto.");
                    Assert.IsFalse((bool)dialogueType.GetProperty("IsVisible").GetValue(dialogue),
                        "Las ilustraciones antiguas no deben aparecer al acertar.");
                    yield return new WaitForSecondsRealtime(1.7f);
                }

                Assert.IsFalse(gamePanel.activeSelf);
                Assert.AreEqual(1, PlayerPrefs.GetInt(completedKey));
                Assert.AreEqual(3, PlayerPrefs.GetInt(starsKey));

                managerType.GetMethod("RestartActivity").Invoke(manager, null);
                yield return new WaitForSecondsRealtime(0.5f);
                Assert.IsTrue(gamePanel.activeSelf);
                Assert.AreEqual("01_", illustration.sprite.name.Substring(0, 3));

                managerType.GetMethod("ReturnToWorldSelection").Invoke(manager, null);
                float deadline = Time.realtimeSinceStartup + 10f;
                while (SceneManager.GetActiveScene().name != "SeleccionMundos" && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.AreEqual("SeleccionMundos", SceneManager.GetActiveScene().name);
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

        private static object Field(Component component, string name)
        {
            return component.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(component);
        }

        private static Button AnswerFor(IList answers, string emotion)
        {
            foreach (object answer in answers)
            {
                if (answer.GetType().GetProperty("Emotion").GetValue(answer).ToString() == emotion)
                    return (Button)answer.GetType().GetProperty("Button").GetValue(answer);
            }

            throw new AssertionException($"Falta el botón de {emotion}.");
        }
    }
}
#endif
