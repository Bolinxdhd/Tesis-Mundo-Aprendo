using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Bolin
{
    /// <summary>
    /// Reproduce un efecto decorativo reutilizable alrededor de las estrellas de resultado.
    /// Las imagenes se asignan desde la escena; el componente no crea ni destruye objetos.
    /// </summary>
    public sealed class RatingStarCelebration : MonoBehaviour
    {
        [Header("Estrellas decorativas")]
        [SerializeField] private Image[] decorativeStars = new Image[5];

        [Header("Recorrido")]
        [SerializeField, Min(0.05f)] private float flightDuration = 0.9f;
        [SerializeField, Min(0f)] private float delayBetweenStars = 0.1f;
        [SerializeField, Min(0f)] private float delayBetweenLoops = 0.35f;
        [SerializeField, Min(1f)] private float verticalDistance = 82f;
        [SerializeField, Min(0f)] private float horizontalDistance = 46f;
        [SerializeField, Min(0f)] private float diagonalArc = 18f;

        [Header("Transformacion")]
        [SerializeField, Range(0.05f, 1f)] private float startScale = 0.3f;
        [SerializeField, Range(0.5f, 2f)] private float peakScale = 1.05f;
        [SerializeField, Range(10f, 540f)] private float rotationDegrees = 120f;
        [SerializeField, Range(0f, 1f)] private float celebrationAlpha = 1f;

        private DecorationState[] originalStates = System.Array.Empty<DecorationState>();
        private Coroutine celebrationRoutine;

        /// <summary>
        /// Inicia un unico loop visual cuando existe al menos una estrella obtenida.
        /// </summary>
        public void Play(int earnedStars)
        {
            StopAndReset();
            if (!isActiveAndEnabled || Mathf.Clamp(earnedStars, 0, 3) <= 0) return;

            CaptureOriginalStates();
            if (originalStates.Length == 0) return;

            HideAllStars();
            celebrationRoutine = StartCoroutine(CelebrationLoop());
        }

        /// <summary>
        /// Cancela el loop y devuelve exactamente las imagenes a su estado configurado.
        /// </summary>
        public void StopAndReset()
        {
            if (celebrationRoutine != null)
            {
                StopCoroutine(celebrationRoutine);
                celebrationRoutine = null;
            }

            RestoreOriginalStates();
        }

        private IEnumerator CelebrationLoop()
        {
            while (isActiveAndEnabled)
            {
                yield return PlayCycle();
                if (delayBetweenLoops > 0f)
                {
                    yield return new WaitForSecondsRealtime(delayBetweenLoops);
                }
            }

            celebrationRoutine = null;
        }

        private IEnumerator PlayCycle()
        {
            float maxDelay = Mathf.Max(0, originalStates.Length - 1) * delayBetweenStars;
            float cycleDuration = maxDelay + flightDuration;
            float elapsed = 0f;

            while (elapsed < cycleDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                for (int i = 0; i < originalStates.Length; i++)
                {
                    float localTime = elapsed - i * delayBetweenStars;
                    if (localTime <= 0f)
                    {
                        ApplyHiddenState(originalStates[i]);
                        continue;
                    }

                    float progress = Mathf.Clamp01(localTime / flightDuration);
                    ApplyFlightState(originalStates[i], i, progress);
                }

                yield return null;
            }

            for (int i = 0; i < originalStates.Length; i++)
            {
                ApplyHiddenState(originalStates[i]);
            }
        }

        private void CaptureOriginalStates()
        {
            if (decorativeStars == null)
            {
                originalStates = System.Array.Empty<DecorationState>();
                return;
            }

            int count = 0;
            for (int i = 0; i < decorativeStars.Length; i++)
            {
                if (decorativeStars[i] != null) count++;
            }

            originalStates = new DecorationState[count];
            int stateIndex = 0;
            for (int i = 0; i < decorativeStars.Length; i++)
            {
                Image image = decorativeStars[i];
                if (image == null) continue;

                RectTransform rect = image.rectTransform;
                originalStates[stateIndex++] = new DecorationState(
                    image,
                    image.enabled,
                    image.gameObject.activeSelf,
                    rect.anchoredPosition,
                    rect.localScale,
                    rect.localRotation,
                    image.color);
            }
        }

        private void ApplyFlightState(DecorationState state, int index, float progress)
        {
            if (state.image == null) return;

            RectTransform rect = state.image.rectTransform;
            state.image.gameObject.SetActive(true);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            float direction = index % 2 == 0 ? -1f : 1f;
            float horizontal = horizontalDistance + index % 3 * 10f;
            float vertical = verticalDistance + index % 2 * 12f;
            float arc = Mathf.Sin(progress * Mathf.PI) * diagonalArc * direction;
            rect.anchoredPosition = state.anchoredPosition + new Vector2(
                direction * horizontal * smoothProgress + arc,
                vertical * smoothProgress);

            Vector3 baseScale = IsNearZero(state.localScale) ? Vector3.one : state.localScale;
            float scale = Mathf.Lerp(startScale, peakScale, Mathf.Sin(progress * Mathf.PI));
            rect.localScale = baseScale * scale;
            rect.localRotation = state.localRotation * Quaternion.Euler(0f, 0f, direction * rotationDegrees * progress);

            Color color = state.color;
            float baseAlpha = state.color.a > 0.001f ? state.color.a : 1f;
            color.a = baseAlpha * celebrationAlpha * Mathf.Sin(progress * Mathf.PI);
            state.image.color = color;
            state.image.enabled = true;
        }

        private void ApplyHiddenState(DecorationState state)
        {
            if (state.image == null) return;

            RectTransform rect = state.image.rectTransform;
            state.image.gameObject.SetActive(true);
            rect.anchoredPosition = state.anchoredPosition;
            rect.localScale = IsNearZero(state.localScale) ? Vector3.one * startScale : state.localScale * startScale;
            rect.localRotation = state.localRotation;

            Color color = state.color;
            color.a = 0f;
            state.image.color = color;
            state.image.enabled = true;
        }

        private void RestoreOriginalStates()
        {
            for (int i = 0; i < originalStates.Length; i++)
            {
                DecorationState state = originalStates[i];
                if (state.image == null) continue;

                RectTransform rect = state.image.rectTransform;
                rect.anchoredPosition = state.anchoredPosition;
                rect.localScale = state.localScale;
                rect.localRotation = state.localRotation;
                state.image.color = state.color;
                state.image.enabled = state.enabled;
                state.image.gameObject.SetActive(state.activeSelf);
            }
        }

        private void HideAllStars()
        {
            for (int i = 0; i < originalStates.Length; i++)
            {
                ApplyHiddenState(originalStates[i]);
            }
        }

        private void OnDisable()
        {
            StopAndReset();
        }

        private static bool IsNearZero(Vector3 value)
        {
            return value.sqrMagnitude < 0.0001f;
        }

        private readonly struct DecorationState
        {
            public DecorationState(Image image, bool enabled, bool activeSelf, Vector2 anchoredPosition, Vector3 localScale, Quaternion localRotation, Color color)
            {
                this.image = image;
                this.enabled = enabled;
                this.activeSelf = activeSelf;
                this.anchoredPosition = anchoredPosition;
                this.localScale = localScale;
                this.localRotation = localRotation;
                this.color = color;
            }

            public readonly Image image;
            public readonly bool enabled;
            public readonly bool activeSelf;
            public readonly Vector2 anchoredPosition;
            public readonly Vector3 localScale;
            public readonly Quaternion localRotation;
            public readonly Color color;
        }
    }
}
