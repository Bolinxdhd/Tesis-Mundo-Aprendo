using System.Collections;
using UnityEngine;

namespace Bolin
{
    public class UIPanelTransition : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform target;
        [SerializeField] private bool playOnEnable = true;
        [SerializeField, Range(0.85f, 1f)] private float hiddenScale = 0.96f;
        [SerializeField, Min(0.05f)] private float duration = 0.24f;
        [SerializeField] private AnimationCurve easing = null;

        private Coroutine transitionRoutine;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (target == null) target = transform as RectTransform;
            if (easing == null || easing.length == 0) easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }

        private void OnEnable()
        {
            if (playOnEnable) Show();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            StartTransition(true);
        }

        public void Hide()
        {
            StartTransition(false);
        }

        public void SetImmediate(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }

            if (target != null) target.localScale = visible ? Vector3.one : Vector3.one * hiddenScale;
            if (!visible) gameObject.SetActive(false);
        }

        private void StartTransition(bool showing)
        {
            if (!isActiveAndEnabled || canvasGroup == null || target == null) return;
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            transitionRoutine = StartCoroutine(TransitionRoutine(showing));
        }

        private IEnumerator TransitionRoutine(bool showing)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = showing;
            float startAlpha = canvasGroup.alpha;
            float targetAlpha = showing ? 1f : 0f;
            Vector3 startScale = target.localScale;
            Vector3 targetScale = showing ? Vector3.one : Vector3.one * hiddenScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = easing.Evaluate(Mathf.Clamp01(elapsed / duration));
                canvasGroup.alpha = Mathf.LerpUnclamped(startAlpha, targetAlpha, t);
                target.localScale = Vector3.LerpUnclamped(startScale, targetScale, t);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            target.localScale = targetScale;
            canvasGroup.interactable = showing;
            canvasGroup.blocksRaycasts = showing;
            transitionRoutine = null;
            if (!showing) gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }
    }
}
