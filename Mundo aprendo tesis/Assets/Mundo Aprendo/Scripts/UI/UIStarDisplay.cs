using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Bolin
{
    public class UIStarDisplay : MonoBehaviour
    {
        [SerializeField] private Image[] stars = new Image[3];
        [SerializeField] private Sprite earnedSprite;
        [SerializeField] private Sprite unearnedSprite;
        [SerializeField] private AudioClip revealClip;
        [SerializeField, Min(0f)] private float delayBetweenStars = 0.16f;
        [SerializeField, Min(0.05f)] private float revealDuration = 0.28f;
        [SerializeField, Range(1f, 1.4f)] private float overshootScale = 1.18f;
        [SerializeField, Range(0f, 20f)] private float entryRotation = 10f;
        [SerializeField, Range(0.2f, 1f)] private float unearnedAlpha = 0.45f;

        private Coroutine animationRoutine;

        public void ShowStars(int earnedStars, bool animate = true)
        {
            int clamped = Mathf.Clamp(earnedStars, 0, 3);
            if (!animate)
            {
                SetImmediate(clamped);
                return;
            }

            if (!isActiveAndEnabled)
            {
                SetImmediate(clamped);
                return;
            }

            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = StartCoroutine(AnimateRoutine(clamped));
        }

        public void SetImmediate(int earnedStars)
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }

            int clamped = Mathf.Clamp(earnedStars, 0, 3);
            for (int i = 0; i < stars.Length; i++) ApplyState(stars[i], i < clamped, true);
        }

        private IEnumerator AnimateRoutine(int earnedStars)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                Image star = stars[i];
                if (star == null) continue;
                ApplyState(star, i < earnedStars, false);
                star.rectTransform.localScale = Vector3.zero;
            }

            for (int i = 0; i < stars.Length; i++)
            {
                Image star = stars[i];
                if (star == null) continue;
                bool earned = i < earnedStars;
                if (earned) AudioManager.TryPlaySfx(revealClip);
                yield return RevealStarRoutine(star);
                if (delayBetweenStars > 0f) yield return new WaitForSecondsRealtime(delayBetweenStars);
            }

            animationRoutine = null;
        }

        private IEnumerator RevealStarRoutine(Image star)
        {
            RectTransform rect = star.rectTransform;
            Quaternion startRotation = Quaternion.Euler(0f, 0f, -entryRotation);
            Quaternion endRotation = Quaternion.identity;
            rect.localRotation = startRotation;
            float elapsed = 0f;

            while (elapsed < revealDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / revealDuration);
                float scale = t < 0.7f
                    ? Mathf.Lerp(0f, overshootScale, t / 0.7f)
                    : Mathf.Lerp(overshootScale, 1f, (t - 0.7f) / 0.3f);
                rect.localScale = Vector3.one * scale;
                rect.localRotation = Quaternion.Slerp(startRotation, endRotation, t);
                yield return null;
            }

            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private void ApplyState(Image star, bool earned, bool resetTransform)
        {
            if (star == null) return;
            star.sprite = earned ? earnedSprite : unearnedSprite;
            star.color = new Color(1f, 1f, 1f, earned ? 1f : unearnedAlpha);
            if (!resetTransform) return;
            star.rectTransform.localScale = Vector3.one;
            star.rectTransform.localRotation = Quaternion.identity;
        }

        private void OnDisable()
        {
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = null;
        }
    }
}
