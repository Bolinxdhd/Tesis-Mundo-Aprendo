using System.Collections;
using UnityEngine;

namespace Bolin
{
    public class VoiceSetupOpener : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float delayBetweenWindows = 0.6f;

        private Coroutine openRoutine;

        public void OpenVoiceSetupWindows()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (openRoutine != null)
            {
                StopCoroutine(openRoutine);
            }

            openRoutine = StartCoroutine(OpenVoiceSetupWindowsRoutine());
#else
            Debug.LogWarning("VoiceSetupOpener: la configuracion automatica de voz solo esta disponible en Windows.");
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private IEnumerator OpenVoiceSetupWindowsRoutine()
        {
            OpenSettings("ms-settings:privacy-microphone");
            yield return new WaitForSeconds(delayBetweenWindows);

            OpenSettings("ms-settings:sound-devices");
            yield return new WaitForSeconds(delayBetweenWindows);

            OpenSettings("ms-settings:speech");
            openRoutine = null;
        }

        private static void OpenSettings(string uri)
        {
            Application.OpenURL(uri);
            Debug.Log($"VoiceSetupOpener: abriendo {uri}");
        }
#endif
    }
}
