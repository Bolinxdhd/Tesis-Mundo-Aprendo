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
    public class UiTextLayoutPlayModeTests
    {
        private static readonly string[] Scenes =
        {
            "Menu", "SeleccionMundos", "MundoTamanos", "MundoMusical",
            "MundoEmociones", "MundoCuentos_VozTest"
        };

        [UnityTest]
        public IEnumerator TextStaysInsideItsBoxAtSupportedResolutions()
        {
            int originalWidth = Screen.width;
            int originalHeight = Screen.height;
            var problems = new List<string>();
            try
            {
                foreach (string sceneName in Scenes)
                {
                    yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                    yield return null;
                    foreach (var resolution in new[] { (1920, 1080), (1366, 768), (1280, 720) })
                    {
                        Screen.SetResolution(resolution.Item1, resolution.Item2, false);
                        yield return null;
                        Canvas.ForceUpdateCanvases();
                        Scan(sceneName, resolution.Item1, resolution.Item2, "initial", problems);
                        SetLongestDynamicExamples(sceneName);
                        Scan(sceneName, resolution.Item1, resolution.Item2, "dynamic", problems);
                        ScanTutorialMessages(sceneName, resolution.Item1, resolution.Item2, problems);
                        if (sceneName == "MundoCuentos_VozTest")
                        {
                            Component controller = SceneManager.GetActiveScene().GetRootGameObjects()
                                .SelectMany(root => root.GetComponentsInChildren(
                                    Type.GetType("Bolin.MundoCuentosController, Assembly-CSharp"), true)).Single();
                            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                            var stories = (IList)controller.GetType().GetField("cuentos", flags).GetValue(controller);
                            object ui = controller.GetType().GetField("uiController", flags).GetValue(controller);
                            TMP_Text narration = (TMP_Text)ui.GetType().GetField("narrationText", flags).GetValue(ui);
                            TMP_Text title = (TMP_Text)ui.GetType().GetField("storyTitleText", flags).GetValue(ui);
                            TMP_Text instruction = (TMP_Text)ui.GetType().GetField("questionText", flags).GetValue(ui);
                            RectTransform leftHeader = title.transform.parent as RectTransform;
                            RectTransform storyCard = SceneManager.GetActiveScene().GetRootGameObjects()
                                .SelectMany(root => root.GetComponentsInChildren<RectTransform>(true))
                                .Single(rect => rect.name == "StoryTextCard");
                            TMP_Text rightTitle = SceneManager.GetActiveScene().GetRootGameObjects()
                                .SelectMany(root => root.GetComponentsInChildren<TMP_Text>(true))
                                .Single(text => text.name == "TextoCabeceraLeeLaHistoria");
                            MethodInfo showStory = ui.GetType().GetMethod("ShowStoryIntro");
                            foreach (object story in stories)
                            {
                                showStory.Invoke(ui, new[] { story });
                                yield return null;
                                Canvas.ForceUpdateCanvases();
                                Assert.AreEqual("CabeceraHistoriaALeer", leftHeader.name);
                                Assert.AreEqual("PanelCuento", instruction.transform.parent.name);
                                AssertInside(title.rectTransform, leftHeader, "El título debe quedar dentro del recuadro verde izquierdo.");
                                AssertInside(rightTitle.rectTransform, rightTitle.transform.parent as RectTransform,
                                    "El encabezado derecho debe quedar dentro de su recuadro verde.");
                                Vector3[] headerCorners = new Vector3[4];
                                Vector3[] instructionCorners = new Vector3[4];
                                Vector3[] cardCorners = new Vector3[4];
                                leftHeader.GetWorldCorners(headerCorners);
                                instruction.rectTransform.GetWorldCorners(instructionCorners);
                                storyCard.GetWorldCorners(cardCorners);
                                Assert.LessOrEqual(instructionCorners[2].y, headerCorners[0].y,
                                    "La indicación debe quedar debajo del título.");
                                Assert.GreaterOrEqual(instructionCorners[0].y, cardCorners[2].y,
                                    "La indicación no debe tapar el texto del cuento.");
                                Assert.Greater(narration.rectTransform.rect.width, 2f, "El texto del cuento necesita ancho visible.");
                                Assert.Greater(narration.rectTransform.rect.height, 2f, "El texto del cuento necesita alto visible.");
                                Scan(sceneName, resolution.Item1, resolution.Item2, "story", problems);
                            }
                        }
                    }
                }
                Assert.IsEmpty(problems, string.Join("\n", problems.Distinct()));
            }
            finally
            {
                Screen.SetResolution(originalWidth, originalHeight, false);
            }
        }

        private static void AssertInside(RectTransform inner, RectTransform outer, string message)
        {
            Vector3[] inside = new Vector3[4];
            Vector3[] outside = new Vector3[4];
            inner.GetWorldCorners(inside);
            outer.GetWorldCorners(outside);
            Assert.GreaterOrEqual(inside[0].x, outside[0].x - 0.1f, message);
            Assert.GreaterOrEqual(inside[0].y, outside[0].y - 0.1f, message);
            Assert.LessOrEqual(inside[2].x, outside[2].x + 0.1f, message);
            Assert.LessOrEqual(inside[2].y, outside[2].y + 0.1f, message);
        }

        private static void ScanTutorialMessages(string sceneName, int width, int height, List<string> problems)
        {
            string typeName;
            string stepsField;
            string labelField;
            string messageField;
            switch (sceneName)
            {
                case "MundoTamanos":
                    typeName = "Bolin.CharacterDialogueController, Assembly-CSharp";
                    stepsField = "introSteps";
                    labelField = "dialogueText";
                    messageField = "message";
                    break;
                case "MundoEmociones":
                    typeName = "Bolin.CharacterDialogueController, Assembly-CSharp";
                    stepsField = "introSteps";
                    labelField = "dialogueText";
                    messageField = "message";
                    break;
                case "MundoMusical":
                    typeName = "Bolin.MusicalTutorialController, Assembly-CSharp";
                    stepsField = "tutorialSteps";
                    labelField = "tutorialText";
                    messageField = "text";
                    break;
                case "MundoCuentos_VozTest":
                    typeName = "Bolin.StoryTutorialAnimationController, Assembly-CSharp";
                    stepsField = "tutorialSteps";
                    labelField = "tutorialText";
                    messageField = "text";
                    break;
                default:
                    return;
            }

            Type type = Type.GetType(typeName);
            Assert.NotNull(type, typeName);
            Component tutorial = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren(type, true)).Single();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var steps = (IList)type.GetField(stepsField, flags).GetValue(tutorial);
            TMP_Text label = (TMP_Text)type.GetField(labelField, flags).GetValue(tutorial);
            foreach (object step in steps)
            {
                label.text = (string)step.GetType().GetField(messageField).GetValue(step);
                Scan(sceneName, width, height, "tutorial", problems);
            }
        }

        private static void SetLongestDynamicExamples(string sceneName)
        {
            string labelName = null;
            string sample = null;
            switch (sceneName)
            {
                case "MundoTamanos":
                    labelName = "Pregunta";
                    sample = "¿Qué animal es más pequeño que el elefante?";
                    break;
                case "MundoMusical":
                    labelName = "TextoEstado";
                    sample = "Escucha la secuencia y repite tocando los números.";
                    break;
                case "MundoEmociones":
                    labelName = "DialogueText";
                    sample = "Mira la situación y elige cómo se siente.";
                    break;
                case "MundoCuentos_VozTest":
                    labelName = "TituloCuento";
                    sample = "Luna, la coneja que compartía su zanahoria";
                    break;
            }
            if (labelName == null) return;
            foreach (TMP_Text label in SceneManager.GetActiveScene().GetRootGameObjects()
                         .SelectMany(root => root.GetComponentsInChildren<TMP_Text>(true)))
                if (label.name == labelName) label.text = sample;
        }

        private static void Scan(string sceneName, int width, int height, string state, List<string> problems)
        {
            foreach (TMP_Text label in SceneManager.GetActiveScene().GetRootGameObjects()
                         .SelectMany(root => root.GetComponentsInChildren<TMP_Text>(true)))
            {
                if (string.IsNullOrWhiteSpace(label.text)) continue;
                label.ForceMeshUpdate(true, true);
                Rect rect = label.rectTransform.rect;
                if (rect.width < 2f || rect.height < 2f) continue;
                if (label.isTextOverflowing)
                    problems.Add($"TEXT_OVERFLOW {sceneName} {width}x{height} {state} {label.name} box={rect.size} font={label.fontSize} text={label.text}");

                RectTransform parentPanel = label.transform.parent as RectTransform;
                while (parentPanel != null && parentPanel.GetComponent<Image>() == null)
                    parentPanel = parentPanel.parent as RectTransform;
                if (parentPanel == null) continue;
                Vector3[] boxCorners = new Vector3[4];
                Vector3[] panelCorners = new Vector3[4];
                label.rectTransform.GetWorldCorners(boxCorners);
                parentPanel.GetWorldCorners(panelCorners);
                const float tolerance = 6f;
                if (boxCorners[0].x < panelCorners[0].x - tolerance ||
                    boxCorners[0].y < panelCorners[0].y - tolerance ||
                    boxCorners[2].x > panelCorners[2].x + tolerance ||
                    boxCorners[2].y > panelCorners[2].y + tolerance)
                    problems.Add($"BOX_OUTSIDE_PANEL {sceneName} {width}x{height} {state} {label.name} parent={parentPanel.name} text={label.text}");
            }
        }
    }
}
#endif
