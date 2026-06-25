using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bolin
{
    public class UIButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private RectTransform target;
        [SerializeField] private AudioClip clickClip;
        [SerializeField, Range(1f, 1.1f)] private float hoverScale = 1.05f;
        [SerializeField, Range(0.9f, 1f)] private float pressedScale = 0.95f;
        [SerializeField, Min(0.05f)] private float transitionDuration = 0.12f;
        [SerializeField] private AnimationCurve easing = null;

        private Coroutine scaleRoutine;
        private Vector3 baseScale = Vector3.one;
        private bool pointerInside;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (target == null) target = transform as RectTransform;
            if (target != null) baseScale = target.localScale;
            if (easing == null || easing.length == 0) easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            if (IsInteractable()) AnimateTo(baseScale * hoverScale);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            AnimateTo(baseScale);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractable()) return;
            AnimateTo(baseScale * pressedScale);
            AudioManager.TryPlaySfx(clickClip);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            AnimateTo(pointerInside && IsInteractable() ? baseScale * hoverScale : baseScale);
        }

        private bool IsInteractable()
        {
            return button == null || button.IsInteractable();
        }

        private void AnimateTo(Vector3 targetScale)
        {
            if (!isActiveAndEnabled || target == null) return;
            if (scaleRoutine != null) StopCoroutine(scaleRoutine);
            scaleRoutine = StartCoroutine(ScaleRoutine(targetScale));
        }

        private IEnumerator ScaleRoutine(Vector3 targetScale)
        {
            Vector3 startScale = target.localScale;
            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = easing.Evaluate(Mathf.Clamp01(elapsed / transitionDuration));
                target.localScale = Vector3.LerpUnclamped(startScale, targetScale, t);
                yield return null;
            }

            target.localScale = targetScale;
            scaleRoutine = null;
        }

        private void OnDisable()
        {
            if (scaleRoutine != null) StopCoroutine(scaleRoutine);
            scaleRoutine = null;
            pointerInside = false;
            if (target != null) target.localScale = baseScale;
        }
    }
}
