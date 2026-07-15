using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Bolin
{
    /// <summary>
    /// Presentation-only feedback for one persistent piano key. The button root is
    /// owned by its layout group; this component only changes the separate
    /// <c>KeyVisual</c> child so presentation can never alter the piano layout.
    /// </summary>
    [DisallowMultipleComponent]
    public class MusicalKeyVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Referencias")]
        [SerializeField] private Button keyButton;
        [SerializeField] private RectTransform visualRoot;
        [SerializeField, FormerlySerializedAs("keyImage")] private Image visualImage;
        [SerializeField] private Image glowImage;

        [Header("Colores")]
        [SerializeField] private bool useInitialImageColorAsIdle = true;
        [SerializeField] private Color idleColor = Color.white;
        [SerializeField] private Color hoverColor = new(1f, 0.96f, 0.78f, 1f);
        [SerializeField] private Color demonstrationColor = new(1f, 0.78f, 0.25f, 1f);
        [SerializeField] private Color correctColor = new(0.42f, 0.88f, 0.56f, 1f);
        [SerializeField] private Color incorrectColor = new(1f, 0.48f, 0.48f, 1f);
        [SerializeField] private Color lockedColor = new(0.72f, 0.75f, 0.8f, 1f);

        [Header("Animacion visual")]
        [SerializeField, Range(0.96f, 1f)] private float pressedScale = 0.97f;
        [SerializeField, Range(1f, 1.03f)] private float hoverScale = 1.02f;
        [SerializeField, Range(1f, 1.03f)] private float cueScale = 1.02f;
        [SerializeField, Range(0f, 2f)] private float incorrectRotation = 1.5f;
        [SerializeField, Min(0.05f)] private float pressDuration = 0.09f;
        [SerializeField, Min(0.05f)] private float releaseDuration = 0.15f;
        [SerializeField, Min(0.05f)] private float demonstrationHold = 0.18f;

        [Header("Brillo")]
        [SerializeField, Range(0f, 1f)] private float hoverGlowAlpha = 0.22f;
        [SerializeField, Range(0f, 1f)] private float demonstrationGlowAlpha = 0.95f;
        [SerializeField, Range(0f, 1f)] private float correctGlowAlpha = 0.85f;
        [SerializeField, Range(0f, 1f)] private float incorrectGlowAlpha = 0.58f;

        private Coroutine feedbackRoutine;
        private Vector3 baseScale = Vector3.one;
        private Quaternion baseRotation = Quaternion.identity;
        private Color baseGlowColor = Color.white;
        private bool initialized;
        private bool isLocked;
        private bool pointerInside;
        private bool pointerPressed;

        public bool IsLocked => isLocked;

        private void Awake()
        {
            ResolveReferences();
            CaptureBaseState();
            ApplyRestingState();
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (!initialized) CaptureBaseState();
            ApplyRestingState();
        }

        /// <summary>
        /// Plays the visual cue used while the game demonstrates a note. Locking is
        /// intentionally controlled separately so the game can lock all keys together.
        /// </summary>
        public void PlayDemonstration()
        {
            StartFeedback(DemonstrationRoutine());
        }

        /// <summary>
        /// Plays positive or gentle retry feedback after the game has evaluated a press.
        /// </summary>
        public void PlayAttempt(bool correct)
        {
            StartFeedback(AttemptRoutine(correct));
        }

        /// <summary>
        /// Updates only this key's presentation and interactability. It never changes
        /// gameplay progress or starts a sequence.
        /// </summary>
        public void SetLocked(bool locked, bool preserveFeedback = false)
        {
            isLocked = locked;
            pointerInside = false;
            pointerPressed = false;

            if (keyButton != null) keyButton.interactable = !locked;

            // The game locks input as soon as it validates a note. Keeping the
            // already-running feedback alive lets the child see it finish before the
            // key returns to its disabled presentation.
            if (!preserveFeedback || feedbackRoutine == null)
            {
                StopFeedback();
                ApplyRestingState();
            }
        }

        /// <summary>
        /// Cancels transient feedback and returns to its normal or locked state.
        /// </summary>
        public void RestoreIdle()
        {
            pointerPressed = false;
            StopFeedback();
            ApplyRestingState();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!CanReceivePointerFeedback()) return;

            pointerInside = true;
            if (!pointerPressed)
            {
                StopFeedback();
                ApplyVisual(baseScale * hoverScale, baseRotation, hoverColor, hoverGlowAlpha);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            bool wasPointerPressed = pointerPressed;
            pointerPressed = false;

            // A quick mouse exit only cancels the press animation it started. Game
            // demonstration and answer feedback are allowed to complete on their own.
            if (wasPointerPressed) StopFeedback();
            if (feedbackRoutine == null) ApplyRestingState();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanReceivePointerFeedback()) return;

            pointerPressed = true;
            StartFeedback(PointerPressRoutine());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pointerPressed = false;
            StopFeedback();
            ApplyRestingState();
        }

        private IEnumerator DemonstrationRoutine()
        {
            yield return AnimateVisual(
                baseScale * cueScale,
                baseRotation,
                demonstrationColor,
                demonstrationGlowAlpha,
                pressDuration);

            if (demonstrationHold > 0f) yield return new WaitForSecondsRealtime(demonstrationHold);

            yield return AnimateVisual(
                baseScale,
                baseRotation,
                GetRestingColor(),
                GetRestingGlowAlpha(),
                releaseDuration);

            feedbackRoutine = null;
        }

        private IEnumerator AttemptRoutine(bool correct)
        {
            if (correct)
            {
                yield return AnimateVisual(
                    baseScale * pressedScale,
                    baseRotation,
                    correctColor,
                    correctGlowAlpha,
                    pressDuration);

                yield return AnimateVisual(
                    baseScale * cueScale,
                    baseRotation,
                    correctColor,
                    correctGlowAlpha,
                    releaseDuration * 0.5f);
            }
            else
            {
                yield return AnimateVisual(
                    baseScale * pressedScale,
                    Quaternion.Euler(0f, 0f, incorrectRotation),
                    incorrectColor,
                    incorrectGlowAlpha,
                    pressDuration);
            }

            yield return AnimateVisual(
                baseScale,
                baseRotation,
                GetRestingColor(),
                GetRestingGlowAlpha(),
                releaseDuration);

            feedbackRoutine = null;
        }

        private IEnumerator PointerPressRoutine()
        {
            yield return AnimateVisual(
                baseScale * pressedScale,
                baseRotation,
                demonstrationColor,
                hoverGlowAlpha,
                pressDuration);

            feedbackRoutine = null;
        }

        private IEnumerator AnimateVisual(Vector3 targetScale, Quaternion targetRotation, Color targetColor, float targetGlowAlpha, float duration)
        {
            Vector3 startScale = visualRoot != null ? visualRoot.localScale : baseScale;
            Quaternion startRotation = visualRoot != null ? visualRoot.localRotation : baseRotation;
            Color startColor = visualImage != null ? visualImage.color : targetColor;
            float startGlowAlpha = GetGlowAlpha();
            float safeDuration = Mathf.Max(0.01f, duration);
            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / safeDuration));
                ApplyVisual(
                    Vector3.LerpUnclamped(startScale, targetScale, t),
                    Quaternion.SlerpUnclamped(startRotation, targetRotation, t),
                    Color.LerpUnclamped(startColor, targetColor, t),
                    Mathf.LerpUnclamped(startGlowAlpha, targetGlowAlpha, t));
                yield return null;
            }

            ApplyVisual(targetScale, targetRotation, targetColor, targetGlowAlpha);
        }

        private void StartFeedback(IEnumerator routine)
        {
            if (!isActiveAndEnabled || routine == null) return;

            StopFeedback();
            feedbackRoutine = StartCoroutine(routine);
        }

        private void StopFeedback()
        {
            if (feedbackRoutine == null) return;

            StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
        }

        private void ResolveReferences()
        {
            if (keyButton == null) keyButton = GetComponent<Button>();

            if (visualRoot == null)
            {
                Transform visual = transform.Find("KeyVisual");
                visualRoot = visual as RectTransform;
            }

            // Never animate the root Button: it is the child controlled by the
            // HorizontalLayoutGroup. An incomplete legacy scene can still use color
            // feedback, but it deliberately receives no transform animation.
            if (visualRoot == transform as RectTransform) visualRoot = null;

            if (visualImage == null && visualRoot != null) visualImage = visualRoot.GetComponent<Image>();
            if (visualImage == null) visualImage = GetComponent<Image>();

            if (glowImage == null)
            {
                Transform glow = transform.Find("KeyVisual/Glow") ?? transform.Find("KeyVisual/Brillo") ?? transform.Find("Brillo");
                glowImage = glow != null ? glow.GetComponent<Image>() : null;
            }
        }

        private void CaptureBaseState()
        {
            // Child visuals are intentionally normalized. Every state restoration
            // uses these absolute values, so rapid pointer events cannot accumulate
            // scaling or rotation.
            baseScale = Vector3.one;
            baseRotation = Quaternion.identity;
            if (visualRoot != null)
            {
                visualRoot.localScale = baseScale;
                visualRoot.localRotation = baseRotation;
            }

            if (visualImage != null && useInitialImageColorAsIdle) idleColor = visualImage.color;
            if (glowImage != null)
            {
                baseGlowColor = glowImage.color;
                // Designers commonly save a glow hidden with alpha zero. Keep its RGB
                // tint but retain a usable maximum alpha for later feedback.
                if (baseGlowColor.a <= 0f) baseGlowColor.a = 1f;
            }
            initialized = true;
        }

        private bool CanReceivePointerFeedback()
        {
            return !isLocked && (keyButton == null || keyButton.IsInteractable());
        }

        private void ApplyRestingState()
        {
            ApplyVisual(baseScale, baseRotation, GetRestingColor(), GetRestingGlowAlpha());
        }

        private Color GetRestingColor()
        {
            if (isLocked || (keyButton != null && !keyButton.IsInteractable())) return lockedColor;
            return pointerInside ? hoverColor : idleColor;
        }

        private float GetRestingGlowAlpha()
        {
            if (isLocked || (keyButton != null && !keyButton.IsInteractable())) return 0f;
            return pointerInside ? hoverGlowAlpha : 0f;
        }

        private void ApplyVisual(Vector3 scale, Quaternion rotation, Color color, float glowAlpha)
        {
            if (visualRoot != null)
            {
                visualRoot.localScale = scale;
                visualRoot.localRotation = rotation;
            }

            if (visualImage != null) visualImage.color = color;
            SetGlowAlpha(glowAlpha);
        }

        private float GetGlowAlpha()
        {
            return glowImage != null && baseGlowColor.a > 0f
                ? Mathf.Clamp01(glowImage.color.a / baseGlowColor.a)
                : 0f;
        }

        private void SetGlowAlpha(float alpha)
        {
            if (glowImage == null) return;

            Color color = baseGlowColor;
            color.a = baseGlowColor.a * Mathf.Clamp01(alpha);
            glowImage.color = color;
        }

        private void OnDisable()
        {
            StopFeedback();
            pointerInside = false;
            pointerPressed = false;
            if (initialized) ApplyRestingState();
        }
    }
}
