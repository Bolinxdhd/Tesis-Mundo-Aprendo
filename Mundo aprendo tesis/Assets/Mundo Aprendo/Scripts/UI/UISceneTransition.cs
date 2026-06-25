using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bolin
{
    public class UISceneTransition : MonoBehaviour
    {
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField, Min(0.05f)] private float duration = 0.3f;

        private static UISceneTransition instance;
        private bool loading;

        private void Awake()
        {
            instance = this;
            if (fadeCanvasGroup == null) fadeCanvasGroup = GetComponent<CanvasGroup>();
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 1f;
                fadeCanvasGroup.blocksRaycasts = true;
                StartCoroutine(FadeRoutine(0f, false));
            }
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        public static bool TryLoadScene(string sceneName)
        {
            if (instance == null || !instance.isActiveAndEnabled || instance.fadeCanvasGroup == null) return false;
            return instance.BeginLoad(sceneName);
        }

        private bool BeginLoad(string sceneName)
        {
            if (loading) return true;
            loading = true;
            StartCoroutine(LoadRoutine(sceneName));
            return true;
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            yield return FadeRoutine(1f, true);
            SceneManager.LoadScene(sceneName);
        }

        private IEnumerator FadeRoutine(float targetAlpha, bool keepBlocking)
        {
            fadeCanvasGroup.blocksRaycasts = true;
            float startAlpha = fadeCanvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            fadeCanvasGroup.alpha = targetAlpha;
            fadeCanvasGroup.blocksRaycasts = keepBlocking;
        }
    }
}
