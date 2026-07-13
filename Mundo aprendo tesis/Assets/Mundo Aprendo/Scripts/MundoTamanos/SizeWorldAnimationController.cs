using System.Collections;
using UnityEngine;

namespace Bolin
{
    // Rutinas visuales usadas por SizeWorldController para entrada, acierto, error y escala.
    internal static class SizeWorldAnimationController
    {
        public static IEnumerator PlayRoundEntrance(
            RectTransform leftRect,
            RectTransform rightRect,
            Vector2 leftPosition,
            Vector2 rightPosition,
            float offscreenPadding,
            float duration)
        {
            // Mueve los dos animales desde fuera de pantalla hasta su posicion de ronda.
            if (leftRect == null || rightRect == null) yield break;

            Vector2 leftStart = new(leftPosition.x - offscreenPadding, leftPosition.y);
            Vector2 rightStart = new(rightPosition.x + offscreenPadding, rightPosition.y);
            leftRect.anchoredPosition = leftStart;
            rightRect.anchoredPosition = rightStart;

            float safeDuration = Mathf.Max(0.01f, duration);
            float elapsed = 0f;
            while (elapsed < safeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                float eased = Mathf.SmoothStep(0f, 1f, t);

                leftRect.anchoredPosition = Vector2.Lerp(leftStart, leftPosition, eased);
                rightRect.anchoredPosition = Vector2.Lerp(rightStart, rightPosition, eased);
                yield return null;
            }

            leftRect.anchoredPosition = leftPosition;
            rightRect.anchoredPosition = rightPosition;
        }

        public static IEnumerator Pulse(RectTransform target, float duration, float scaleMultiplier)
        {
            // Aumenta y regresa la escala para destacar una respuesta correcta.
            if (target == null) yield break;

            Vector3 baseScale = target.localScale;
            Vector3 pulseScale = baseScale * scaleMultiplier;
            yield return Scale(target, baseScale, pulseScale, duration);
            yield return Scale(target, pulseScale, baseScale, duration);
            target.localScale = baseScale;
        }

        public static IEnumerator ShakeHorizontal(RectTransform target, float duration, float strength, float frequency)
        {
            // Sacude horizontalmente una opcion para indicar error.
            if (target == null) yield break;

            Vector2 basePosition = target.anchoredPosition;
            float safeDuration = Mathf.Max(0.01f, duration);
            float elapsed = 0f;
            while (elapsed < safeDuration)
            {
                elapsed += Time.deltaTime;
                float dampedStrength = strength * (1f - Mathf.Clamp01(elapsed / safeDuration));
                target.anchoredPosition = basePosition + Vector2.right * (Mathf.Sin(elapsed * frequency) * dampedStrength);
                yield return null;
            }

            target.anchoredPosition = basePosition;
        }

        public static void ApplyDepthScale(
            RectTransform target,
            float yPosition,
            Vector2 verticalPositionRange,
            float animalMinScale,
            float animalMaxScale,
            float globalMinScale,
            float globalMaxScale)
        {
            // Convierte la posicion vertical en escala para simular cercania.
            if (target == null) return;

            float normalizedDepth = Mathf.InverseLerp(verticalPositionRange.y, verticalPositionRange.x, yPosition);
            float safeAnimalMin = Mathf.Max(0.1f, animalMinScale);
            float safeAnimalMax = Mathf.Max(safeAnimalMin, animalMaxScale);
            float scale = Mathf.Lerp(safeAnimalMin, safeAnimalMax, normalizedDepth);
            scale = Mathf.Clamp(scale, globalMinScale, globalMaxScale);
            target.localScale = Vector3.one * scale;
        }

        private static IEnumerator Scale(RectTransform target, Vector3 startScale, Vector3 endScale, float duration)
        {
            // Interpola escala con suavizado y deja el valor final exacto.
            float safeDuration = Mathf.Max(0.01f, duration);
            float elapsed = 0f;
            while (elapsed < safeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                target.localScale = Vector3.Lerp(startScale, endScale, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            target.localScale = endScale;
        }
    }
}
