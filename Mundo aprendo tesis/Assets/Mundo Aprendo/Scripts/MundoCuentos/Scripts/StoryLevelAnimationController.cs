using System.Collections;
using UnityEngine;

namespace Bolin
{
    public class StoryLevelAnimationController : MonoBehaviour
    {
        [SerializeField] private RectTransform selectionPanel;
        [SerializeField] private RectTransform readingPanel;
        [SerializeField] private RectTransform resultPanel;
        [SerializeField] private RectTransform[] storyCards = new RectTransform[0];
        [SerializeField] private RectTransform[] actionButtons = new RectTransform[0];
        [SerializeField] private RectTransform microphoneIndicator;
        [SerializeField] private RectTransform guideCharacter;
        [SerializeField, Min(0.05f)] private float panelDuration = 0.24f;
        [SerializeField, Min(0.02f)] private float itemDelay = 0.055f;
        [SerializeField, Range(0.85f, 1f)] private float hiddenScale = 0.94f;

        private Coroutine panelRoutine;
        private Coroutine itemRoutine;
        private Coroutine pulseRoutine;

        public void PlaySelectionEntrance()
        {
            PlayPanel(selectionPanel);
            PlayItems(storyCards);
        }

        public void PlayReadingEntrance()
        {
            PlayPanel(readingPanel);
            PlayItems(actionButtons);
        }

        public void PlayStoryChanged()
        {
            PlayPanel(readingPanel);
        }

        public void PlayMicrophoneActivation()
        {
            PlayPulse(microphoneIndicator);
        }

        public void PlayResultEntrance()
        {
            PlayPanel(resultPanel);
            PlayPulse(guideCharacter);
        }

        public void StopVisuals()
        {
            if (panelRoutine != null) StopCoroutine(panelRoutine);
            if (itemRoutine != null) StopCoroutine(itemRoutine);
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            panelRoutine = null;
            itemRoutine = null;
            pulseRoutine = null;
        }

        private void PlayPanel(RectTransform panel)
        {
            if (panel == null || !panel.gameObject.activeInHierarchy) return;
            if (panelRoutine != null) StopCoroutine(panelRoutine);
            panelRoutine = StartCoroutine(PanelRoutine(panel));
        }

        private IEnumerator PanelRoutine(RectTransform panel)
        {
            Vector3 targetScale = Vector3.one;
            Vector3 startScale = targetScale * hiddenScale;
            CanvasGroup group = panel.GetComponent<CanvasGroup>();
            float startAlpha = group != null ? 0f : 1f;
            float elapsed = 0f;

            panel.localScale = startScale;
            if (group != null)
            {
                group.alpha = startAlpha;
                group.interactable = true;
                group.blocksRaycasts = true;
            }

            while (elapsed < panelDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / panelDuration), 3f);
                panel.localScale = Vector3.LerpUnclamped(startScale, targetScale, t);
                if (group != null) group.alpha = Mathf.Lerp(startAlpha, 1f, t);
                yield return null;
            }

            panel.localScale = targetScale;
            if (group != null) group.alpha = 1f;
            panelRoutine = null;
        }

        private void PlayItems(RectTransform[] items)
        {
            if (items == null || items.Length == 0) return;
            if (itemRoutine != null) StopCoroutine(itemRoutine);
            itemRoutine = StartCoroutine(ItemRoutine(items));
        }

        private IEnumerator ItemRoutine(RectTransform[] items)
        {
            foreach (RectTransform item in items)
            {
                if (item == null || !item.gameObject.activeInHierarchy) continue;
                yield return PulseOnce(item, 1.04f, 0.12f);
                if (itemDelay > 0f) yield return new WaitForSecondsRealtime(itemDelay);
            }

            itemRoutine = null;
        }

        private void PlayPulse(RectTransform target)
        {
            if (target == null || !target.gameObject.activeInHierarchy) return;
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = StartCoroutine(PulseRoutine(target));
        }

        private IEnumerator PulseRoutine(RectTransform target)
        {
            yield return PulseOnce(target, 1.12f, 0.18f);
            pulseRoutine = null;
        }

        private static IEnumerator PulseOnce(RectTransform target, float scale, float duration)
        {
            Vector3 baseScale = target.localScale;
            Vector3 peak = baseScale * scale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float wave = Mathf.Sin(t * Mathf.PI);
                target.localScale = Vector3.LerpUnclamped(baseScale, peak, wave);
                yield return null;
            }

            target.localScale = baseScale;
        }

        private void OnDisable()
        {
            StopVisuals();
        }
    }
}
