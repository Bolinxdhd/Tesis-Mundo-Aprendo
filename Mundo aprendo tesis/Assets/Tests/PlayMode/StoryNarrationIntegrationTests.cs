#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
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
    public class StoryNarrationIntegrationTests
    {
        [UnityTest]
        public IEnumerator LeavingDuringCountdown_CancelsNarrationAndKeepsMusic()
        {
            yield return SceneManager.LoadSceneAsync("MundoCuentos_VozTest", LoadSceneMode.Single);
            yield return null;

            Type controllerType = Type.GetType("Bolin.MundoCuentosController, Assembly-CSharp");
            Type uiType = Type.GetType("Bolin.MundoCuentosUIController, Assembly-CSharp");
            Component controller = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren(controllerType, true)).Single();
            Component ui = controller.GetComponent(uiType);
            AudioSource narration = controller.GetComponent<AudioSource>();
            AudioSource music = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<AudioSource>(true))
                .Single(source => source.gameObject.name == "MusicSource");
            Button start = (Button)uiType.GetField("primaryButton", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(ui);

            controllerType.GetMethod("OpenStoryById").Invoke(controller, new object[] { "luna" });
            start.onClick.Invoke();
            start.onClick.Invoke();
            Assert.IsFalse(narration.isPlaying);
            Assert.IsTrue(music.isPlaying);

            controllerType.GetMethod("ShowLibrary").Invoke(controller, null);
            yield return new WaitForSecondsRealtime(5.2f);
            Assert.IsFalse(narration.isPlaying);
            Assert.IsTrue(music.isPlaying);
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator AllFiveStories_WaitNarrateAndResumeMusic()
        {
            string[] ids = { "luna", "tito", "milo", "sofi", "roco" };
            string[] keys = ids.Take(4).Select(id => $"MundoCuentos_Completado_{id}").ToArray();
            bool[] hadKeys = keys.Select(PlayerPrefs.HasKey).ToArray();
            int[] previous = keys.Select(key => PlayerPrefs.GetInt(key)).ToArray();

            try
            {
                foreach (string key in keys) PlayerPrefs.SetInt(key, 1);
                yield return SceneManager.LoadSceneAsync("MundoCuentos_VozTest", LoadSceneMode.Single);
                yield return null;

                Type controllerType = Type.GetType("Bolin.MundoCuentosController, Assembly-CSharp");
                Type uiType = Type.GetType("Bolin.MundoCuentosUIController, Assembly-CSharp");
                Assert.NotNull(controllerType);
                Assert.NotNull(uiType);
                Component controller = SceneManager.GetActiveScene().GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren(controllerType, true)).Single();
                Component ui = controller.GetComponent(uiType);
                AudioSource narration = controller.GetComponent<AudioSource>();
                AudioSource music = SceneManager.GetActiveScene().GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<AudioSource>(true))
                    .Single(source => source.gameObject.name == "MusicSource");
                Button start = (Button)uiType.GetField("primaryButton", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(ui);
                TMP_Text status = (TMP_Text)uiType.GetField("statusText", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(ui);
                MethodInfo open = controllerType.GetMethod("OpenStoryById");
                MethodInfo showLibrary = controllerType.GetMethod("ShowLibrary");
                PropertyInfo state = controllerType.GetProperty("State");
                Assert.NotNull(narration);
                Assert.NotNull(music);
                Assert.NotNull(start);
                Assert.NotNull(status);

                for (int i = 0; i < ids.Length; i++)
                {
                    open.Invoke(controller, new object[] { ids[i] });
                    Assert.IsTrue(start.gameObject.activeSelf, ids[i]);
                    Assert.IsTrue(music.isPlaying, ids[i]);
                    start.onClick.Invoke();
                    Assert.AreEqual("Narrating", state.GetValue(controller).ToString(), ids[i]);
                    StringAssert.Contains("5", status.text, ids[i]);
                    Assert.IsFalse(narration.isPlaying, ids[i]);

                    yield return new WaitForSecondsRealtime(5.2f);
                    Assert.IsTrue(narration.isPlaying, ids[i]);
                    Assert.AreEqual($"Cuento0{i + 1}_{char.ToUpperInvariant(ids[i][0])}{ids[i].Substring(1)}",
                        narration.clip.name, ids[i]);
                    Assert.IsFalse(music.isPlaying, ids[i]);
                    float musicPosition = music.time;

                    float deadline = Time.realtimeSinceStartup + 60f;
                    while (state.GetValue(controller).ToString() == "Narrating" && Time.realtimeSinceStartup < deadline)
                        yield return null;

                    Assert.AreNotEqual("Narrating", state.GetValue(controller).ToString(), ids[i]);
                    Assert.IsTrue(music.isPlaying, ids[i]);
                    Assert.Less(Mathf.Abs(music.time - musicPosition), 1f, ids[i]);
                }

                open.Invoke(controller, new object[] { "luna" });
                start.onClick.Invoke();
                yield return new WaitForSecondsRealtime(5.2f);
                Assert.IsTrue(narration.isPlaying);
                showLibrary.Invoke(controller, null);
                Assert.IsFalse(narration.isPlaying);
                Assert.IsTrue(music.isPlaying);
            }
            finally
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    if (hadKeys[i]) PlayerPrefs.SetInt(keys[i], previous[i]);
                    else PlayerPrefs.DeleteKey(keys[i]);
                }
                PlayerPrefs.Save();
            }
        }
    }
}
#endif
