using System.Collections;
using UnityEngine;

namespace Bolin
{
    // Animaciones reutilizables del mundo de emociones; la logica queda en EmotionGameManager.
    internal static class EmotionGameAnimationController
    {
        public static IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float targetAlpha, float duration)
        {
            // Cambia suavemente la transparencia de paneles de juego o feedback.
            if (canvasGroup == null) yield break;

            float startAlpha = canvasGroup.alpha;
            float safeDuration = Mathf.Max(0.01f, duration);
            float elapsed = 0f;
            while (elapsed < safeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / safeDuration));
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }

        public static IEnumerator PlayEmotionEntrance(EmotionRoundView view, float duration)
        {
            // Entrada de la expresion: escala desde pequeno y aparece con fade.
            if (view?.animatedRect == null) yield break;

            Vector3 startScale = Vector3.one * 0.86f;
            Vector3 endScale = Vector3.one;
            view.animatedRect.localScale = startScale;
            if (view.canvasGroup != null) view.canvasGroup.alpha = 0f;

            float safeDuration = Mathf.Max(0.01f, duration);
            float elapsed = 0f;
            while (elapsed < safeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / safeDuration));
                view.animatedRect.localScale = Vector3.Lerp(startScale, endScale, t);
                if (view.canvasGroup != null) view.canvasGroup.alpha = t;
                yield return null;
            }

            view.animatedRect.localScale = endScale;
            if (view.canvasGroup != null) view.canvasGroup.alpha = 1f;
        }

        public static IEnumerator FloatAnchoredPosition(RectTransform target, float distance, float speed)
        {
            // Movimiento suave en loop para Nuna u otros elementos decorativos.
            if (target == null) yield break;

            yield return null;
            Vector2 basePosition = target.anchoredPosition;
            float safeSpeed = Mathf.Max(0.01f, speed);
            while (true)
            {
                float offset = Mathf.Sin(Time.unscaledTime * safeSpeed) * distance;
                target.anchoredPosition = basePosition + Vector2.up * offset;
                yield return null;
            }
        }
    }
}
