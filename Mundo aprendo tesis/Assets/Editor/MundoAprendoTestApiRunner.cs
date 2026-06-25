#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Bolin.Editor
{
    public static class MundoAprendoTestApiRunner
    {
        public static void RunPlayMode()
        {
            Run(TestMode.PlayMode, "Logs/MundoCuentosExpansionPlayModeApi.xml");
        }

        public static void RunEditMode()
        {
            Run(TestMode.EditMode, "Logs/MundoCuentosExpansionEditModeApi.xml");
        }

        private static void Run(TestMode mode, string resultPath)
        {
            string absolutePath = Path.GetFullPath(resultPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));

            TestRunnerApi api = ScriptableObject.CreateInstance<TestRunnerApi>();
            Callback callback = ScriptableObject.CreateInstance<Callback>();
            callback.ResultPath = absolutePath;
            api.RegisterCallbacks(callback);
            api.Execute(new ExecutionSettings(new Filter { testMode = mode }));

            double startedAt = EditorApplication.timeSinceStartup;
            EditorApplication.update += WaitForCompletion;

            void WaitForCompletion()
            {
                if (callback.Completed)
                {
                    api.UnregisterCallbacks(callback);
                    UnityEngine.Object.DestroyImmediate(callback);
                    UnityEngine.Object.DestroyImmediate(api);
                    EditorApplication.update -= WaitForCompletion;
                    EditorApplication.Exit(callback.Failed ? 1 : 0);
                    return;
                }

                if (EditorApplication.timeSinceStartup - startedAt < 300d) return;

                Debug.LogError($"MundoAprendoTestApiRunner: timeout ejecutando {mode}.");
                api.UnregisterCallbacks(callback);
                UnityEngine.Object.DestroyImmediate(callback);
                UnityEngine.Object.DestroyImmediate(api);
                EditorApplication.update -= WaitForCompletion;
                EditorApplication.Exit(1);
            }
        }

        private sealed class Callback : ScriptableObject, ICallbacks
        {
            public string ResultPath { get; set; }
            public bool Completed { get; private set; }
            public bool Failed { get; private set; }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                Debug.Log($"MundoAprendoTestApiRunner: iniciando {testsToRun.Name}.");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                TestRunnerApi.SaveResultToFile(result, ResultPath);
                Failed = result.FailCount > 0;
                Completed = true;
                Debug.Log($"MundoAprendoTestApiRunner: finalizado. Passed={result.PassCount}, Failed={result.FailCount}, Skipped={result.SkipCount}. XML={ResultPath}");
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }
        }
    }
}
#endif
