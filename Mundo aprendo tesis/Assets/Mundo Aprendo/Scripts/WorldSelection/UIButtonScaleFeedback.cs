using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Bolin
{
    /// <summary>Small unscaled hover/press response used by the two bottom controls.</summary>
    public sealed class UIButtonScaleFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private RectTransform target;
        [SerializeField, Range(1f, 1.15f)] private float hoverScale = 1.04f;
        [SerializeField, Range(0.8f, 1f)] private float pressedScale = 0.96f;
        [SerializeField, Min(0.04f)] private float transitionDuration = 0.12f;

        private Coroutine transitionRoutine;
        private bool highlighted;

        private void Awake()
        {
            if (target == null) target = transform as RectTransform;
        }

        private void OnDisable()
        {
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            transitionRoutine = null;
            if (target != null) target.localScale = Vector3.one;
        }

        public void OnPointerEnter(PointerEventData eventData) { highlighted = true; AnimateTo(hoverScale); }
        public void OnPointerExit(PointerEventData eventData) { highlighted = false; AnimateTo(1f); }
        public void OnPointerDown(PointerEventData eventData) { AnimateTo(pressedScale); }
        public void OnPointerUp(PointerEventData eventData) { AnimateTo(highlighted ? hoverScale : 1f); }
        public void OnSelect(BaseEventData eventData) { highlighted = true; AnimateTo(hoverScale); }
        public void OnDeselect(BaseEventData eventData) { highlighted = false; AnimateTo(1f); }

        private void AnimateTo(float scale)
        {
            if (target == null) return;
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            transitionRoutine = StartCoroutine(AnimateScale(scale));
        }

        private IEnumerator AnimateScale(float targetScale)
        {
            Vector3 start = target.localScale;
            Vector3 end = Vector3.one * targetScale;
            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                target.localScale = Vector3.Lerp(start, end, Mathf.Clamp01(elapsed / transitionDuration));
                yield return null;
            }

            target.localScale = end;
            transitionRoutine = null;
        }
    }
}
