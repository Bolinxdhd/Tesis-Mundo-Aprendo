using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolin
{
    /// <summary>
    /// Capa exclusivamente visual para el Mundo de los Tamaños. No crea rondas,
    /// no interpreta respuestas y no modifica los datos de animales: únicamente
    /// reacciona a los eventos ya emitidos por <see cref="SizeWorldController"/>.
    /// </summary>
    public class SizeWorldPresentationController : MonoBehaviour
    {
        [Header("Controlador de mecánica")]
        [SerializeField] private SizeWorldController controller;

        [Header("Elementos de entrada")]
        [SerializeField] private RectTransform titleTransform;
        [SerializeField] private RectTransform instructionTransform;
        [SerializeField] private RectTransform guideTransform;
        [SerializeField] private RectTransform leftCardTransform;
        [SerializeField] private RectTransform rightCardTransform;
        [Tooltip("Opcional. Se desvanece durante la entrada sin cambiar la lógica del nivel.")]
        [SerializeField] private CanvasGroup presentationCanvasGroup;

        [Header("Guía visual")]
        [SerializeField] private Image guideImage;
        [SerializeField] private Sprite guideIdleSprite;
        [SerializeField] private Sprite guideCelebrateSprite;
        [SerializeField] private Sprite guideRetrySprite;
        [Tooltip("La guia de gameplay permanece oculta; el personaje solo se usa dentro del tutorial inicial.")]
        [SerializeField] private bool showGameplayGuide;

        [Header("Resultado")]
        [SerializeField] private UIStarDisplay resultStarDisplay;

        [Header("Espejos visuales de la respuesta")]
        [Tooltip("Etiquetas inferiores que muestran el mismo nombre ya configurado por SizeWorldController.")]
        [SerializeField] private TMP_Text leftAnswerLabel;
        [SerializeField] private TMP_Text rightAnswerLabel;
        [Tooltip("Iconos inferiores que reflejan los sprites de los animales activos.")]
        [SerializeField] private Image leftAnswerIcon;
        [SerializeField] private Image rightAnswerIcon;
        [SerializeField] private Image leftSourceAnimalImage;
        [SerializeField] private Image rightSourceAnimalImage;
        [SerializeField] private TMP_Text leftSourceAnimalName;
        [SerializeField] private TMP_Text rightSourceAnimalName;

        [Header("Espejo visual de estrellas")]
        [Tooltip("Las estrellas inferiores solo reflejan las tres estrellas que ya controla SizeWorldController.")]
        [SerializeField] private Image[] sourceStarImages = new Image[0];
        [SerializeField] private Image[] bottomStarImages = new Image[0];

        [Header("Barra de mensajes")]
        [SerializeField] private TMP_Text messageText;

        [Header("Tiempos de presentación")]
        [SerializeField, Min(0.05f)] private float entranceDuration = 0.52f;
        [SerializeField, Min(0f)] private float guideIdleAmplitude = 7f;
        [SerializeField, Min(0.1f)] private float guideIdleFrequency = 1.25f;
        [SerializeField, Min(0.05f)] private float feedbackDuration = 0.34f;

        [Header("Desplazamientos de entrada")]
        [SerializeField, Min(0f)] private float headerEntranceOffset = 150f;
        [SerializeField, Min(0f)] private float guideEntranceOffset = 125f;
        [SerializeField, Min(0f)] private float cardEntranceOffset = 160f;

        private RectState titleState;
        private RectState instructionState;
        private RectState guideState;
        private RectState leftCardState;
        private RectState rightCardState;
        private Sprite originalGuideSprite;
        private float originalCanvasAlpha = 1f;
        private Coroutine entranceRoutine;
        private Coroutine guideIdleRoutine;
        private Coroutine guideReactionRoutine;
        private Coroutine mirrorRefreshRoutine;
        private Coroutine messageRefreshRoutine;
        private bool activityCompleted;
        private TMP_Text instructionText;

        private struct RectState
        {
            public Vector2 anchoredPosition;
            public Vector3 localScale;
            public Quaternion localRotation;

            public RectState(RectTransform transform)
            {
                anchoredPosition = transform != null ? transform.anchoredPosition : Vector2.zero;
                localScale = transform != null ? transform.localScale : Vector3.one;
                localRotation = transform != null ? transform.localRotation : Quaternion.identity;
            }
        }

        private void Awake()
        {
            if (controller == null)
            {
                controller = GetComponent<SizeWorldController>();
            }

            CaptureBaseState();
            if (showGameplayGuide)
            {
                SetGuideSprite(guideIdleSprite);
            }
            else
            {
                HideGameplayGuide();
            }
            SynchronizeVisualMirrors();
        }

        private void OnEnable()
        {
            SubscribeToController();
            SynchronizeVisualMirrors();
        }

        private void OnDisable()
        {
            UnsubscribeFromController();
            StopPresentationRoutines();
            StopMirrorRefreshRoutine();
            StopMessageRefreshRoutine();
            RestoreBaseState();

            if (guideImage != null)
            {
                guideImage.sprite = originalGuideSprite;
            }
        }

        private void SubscribeToController()
        {
            if (controller == null) return;

            // Quitar antes de añadir hace seguro reactivar el objeto sin listeners duplicados.
            controller.OnRoundStarted -= HandleRoundStarted;
            controller.OnAnswerValidated -= HandleAnswerValidated;
            controller.OnActivityCompleted -= HandleActivityCompleted;

            controller.OnRoundStarted += HandleRoundStarted;
            controller.OnAnswerValidated += HandleAnswerValidated;
            controller.OnActivityCompleted += HandleActivityCompleted;
        }

        private void UnsubscribeFromController()
        {
            if (controller == null) return;

            controller.OnRoundStarted -= HandleRoundStarted;
            controller.OnAnswerValidated -= HandleAnswerValidated;
            controller.OnActivityCompleted -= HandleActivityCompleted;
        }

        private void HandleRoundStarted()
        {
            activityCompleted = false;
            StopPresentationRoutines();
            RestoreBaseState();
            if (showGameplayGuide) SetGuideSprite(guideIdleSprite);
            RefreshInstructionCopy();
            SynchronizeVisualMirrors();
            ShowMessage("Selecciona un animal.");
            entranceRoutine = StartCoroutine(PlayRoundEntranceRoutine());
        }

        private void HandleAnswerValidated(bool isCorrect)
        {
            if (showGameplayGuide)
            {
                StopGuideIdleRoutine();
                StopGuideReactionRoutine();
                RestoreGuideState();
                guideReactionRoutine = StartCoroutine(PlayGuideReactionRoutine(isCorrect));
            }

            StartMessageRefresh(isCorrect
                ? "¡Muy bien! Preparando la siguiente pregunta…"
                : "Inténtalo otra vez.");
            StartMirrorRefreshRoutine();
        }

        private void HandleActivityCompleted(int stars)
        {
            activityCompleted = true;
            StopEntranceRoutine();
            if (showGameplayGuide)
            {
                StopGuideIdleRoutine();
                StopGuideReactionRoutine();
            }
            RestoreBaseState();

            if (resultStarDisplay != null)
            {
                resultStarDisplay.ShowStars(stars, true);
            }

            SynchronizeVisualMirrors();
            ShowMessage("¡Actividad completada!");

            if (showGameplayGuide)
            {
                guideReactionRoutine = StartCoroutine(PlayCompletionReactionRoutine());
            }
        }

        private IEnumerator PlayRoundEntranceRoutine()
        {
            PrepareEntranceState();
            float duration = Mathf.Max(0.05f, entranceDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                ApplyEntrance(titleTransform, titleState, Vector2.up * headerEntranceOffset, 0.9f, Evaluate(progress, 0f, 0.58f));
                ApplyEntrance(instructionTransform, instructionState, Vector2.zero, 0.94f, Evaluate(progress, 0.18f, 0.6f));
                if (showGameplayGuide)
                {
                    ApplyEntrance(guideTransform, guideState, Vector2.left * guideEntranceOffset, 0.92f, Evaluate(progress, 0.12f, 0.72f));
                }
                ApplyEntrance(leftCardTransform, leftCardState, Vector2.left * cardEntranceOffset, 0.9f, Evaluate(progress, 0.22f, 0.7f));
                ApplyEntrance(rightCardTransform, rightCardState, Vector2.right * cardEntranceOffset, 0.9f, Evaluate(progress, 0.26f, 0.7f));

                if (presentationCanvasGroup != null)
                {
                    presentationCanvasGroup.alpha = Mathf.Lerp(0f, originalCanvasAlpha, Mathf.SmoothStep(0f, 1f, progress));
                }

                yield return null;
            }

            RestoreBaseState();
            entranceRoutine = null;

            if (showGameplayGuide && !activityCompleted)
            {
                StartGuideIdleRoutine();
            }
        }

        private IEnumerator PlayGuideReactionRoutine(bool isCorrect)
        {
            if (!showGameplayGuide) yield break;
            SetGuideSprite(isCorrect ? guideCelebrateSprite : guideRetrySprite);

            if (guideTransform != null)
            {
                float duration = Mathf.Max(0.05f, feedbackDuration);
                float elapsed = 0f;
                float direction = isCorrect ? 1f : -1f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float progress = Mathf.Clamp01(elapsed / duration);
                    float wave = Mathf.Sin(progress * Mathf.PI);

                    if (isCorrect)
                    {
                        guideTransform.localScale = Vector3.Lerp(guideState.localScale, guideState.localScale * 1.1f, wave);
                        guideTransform.localRotation = guideState.localRotation * Quaternion.Euler(0f, 0f, direction * wave * 7f);
                    }
                    else
                    {
                        guideTransform.anchoredPosition = guideState.anchoredPosition + Vector2.right * (Mathf.Sin(progress * Mathf.PI * 4f) * 9f);
                        guideTransform.localRotation = guideState.localRotation * Quaternion.Euler(0f, 0f, direction * wave * 4f);
                    }

                    yield return null;
                }
            }

            RestoreGuideState();
            guideReactionRoutine = null;

            if (!activityCompleted)
            {
                SetGuideSprite(guideIdleSprite);
                StartGuideIdleRoutine();
            }
        }

        private IEnumerator PlayCompletionReactionRoutine()
        {
            if (!showGameplayGuide) yield break;
            SetGuideSprite(guideCelebrateSprite);

            if (guideTransform != null)
            {
                float duration = Mathf.Max(0.05f, feedbackDuration);
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float progress = Mathf.Clamp01(elapsed / duration);
                    float wave = Mathf.Sin(progress * Mathf.PI);
                    guideTransform.localScale = Vector3.Lerp(guideState.localScale, guideState.localScale * 1.12f, wave);
                    guideTransform.localRotation = guideState.localRotation * Quaternion.Euler(0f, 0f, wave * 8f);
                    yield return null;
                }

                RestoreGuideState();
            }

            guideReactionRoutine = null;
        }

        private IEnumerator PlayGuideIdleRoutine()
        {
            SetGuideSprite(guideIdleSprite);

            while (isActiveAndEnabled && !activityCompleted && guideTransform != null)
            {
                float bob = Mathf.Sin(Time.unscaledTime * guideIdleFrequency * Mathf.PI * 2f) * guideIdleAmplitude;
                guideTransform.anchoredPosition = guideState.anchoredPosition + Vector2.up * bob;
                guideTransform.localScale = guideState.localScale;
                guideTransform.localRotation = guideState.localRotation;
                yield return null;
            }

            RestoreGuideState();
            guideIdleRoutine = null;
        }

        private void PrepareEntranceState()
        {
            if (presentationCanvasGroup != null)
            {
                presentationCanvasGroup.alpha = 0f;
            }

            ApplyEntrance(titleTransform, titleState, Vector2.up * headerEntranceOffset, 0.9f, 0f);
            ApplyEntrance(instructionTransform, instructionState, Vector2.zero, 0.94f, 0f);
            ApplyEntrance(guideTransform, guideState, Vector2.left * guideEntranceOffset, 0.92f, 0f);
            ApplyEntrance(leftCardTransform, leftCardState, Vector2.left * cardEntranceOffset, 0.9f, 0f);
            ApplyEntrance(rightCardTransform, rightCardState, Vector2.right * cardEntranceOffset, 0.9f, 0f);
        }

        private static void ApplyEntrance(RectTransform target, RectState state, Vector2 offset, float startScale, float progress)
        {
            if (target == null) return;

            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress));
            target.anchoredPosition = Vector2.Lerp(state.anchoredPosition + offset, state.anchoredPosition, eased);
            target.localScale = Vector3.Lerp(state.localScale * startScale, state.localScale, eased);
            target.localRotation = state.localRotation;
        }

        private static float Evaluate(float totalProgress, float delay, float duration)
        {
            float safeDuration = Mathf.Max(0.01f, duration);
            return Mathf.Clamp01((totalProgress - delay) / safeDuration);
        }

        private void StartGuideIdleRoutine()
        {
            if (!showGameplayGuide || guideTransform == null || !isActiveAndEnabled || activityCompleted) return;

            StopGuideIdleRoutine();
            RestoreGuideState();
            guideIdleRoutine = StartCoroutine(PlayGuideIdleRoutine());
        }

        private void StopPresentationRoutines()
        {
            StopEntranceRoutine();
            StopGuideIdleRoutine();
            StopGuideReactionRoutine();
        }

        private void StartMirrorRefreshRoutine()
        {
            StopMirrorRefreshRoutine();
            if (isActiveAndEnabled)
            {
                mirrorRefreshRoutine = StartCoroutine(RefreshMirrorsNextFrame());
            }
        }

        private void StopMirrorRefreshRoutine()
        {
            if (mirrorRefreshRoutine == null) return;
            StopCoroutine(mirrorRefreshRoutine);
            mirrorRefreshRoutine = null;
        }

        private void StartMessageRefresh(string message)
        {
            StopMessageRefreshRoutine();
            if (isActiveAndEnabled)
            {
                messageRefreshRoutine = StartCoroutine(RefreshMessageNextFrame(message));
            }
        }

        private void StopMessageRefreshRoutine()
        {
            if (messageRefreshRoutine == null) return;
            StopCoroutine(messageRefreshRoutine);
            messageRefreshRoutine = null;
        }

        private IEnumerator RefreshMessageNextFrame(string message)
        {
            // SizeWorldController actualiza su feedback despues de emitir el evento.
            yield return null;
            ShowMessage(message);
            messageRefreshRoutine = null;
        }

        private IEnumerator RefreshMirrorsNextFrame()
        {
            // OnAnswerValidated se emite antes de que el controlador cambie las estrellas.
            // Esperar un frame mantiene este componente como un reflejo, no como otra fuente de puntaje.
            yield return null;
            SynchronizeVisualMirrors();
            mirrorRefreshRoutine = null;
        }

        private void SynchronizeVisualMirrors()
        {
            CopyAnswerVisual(leftSourceAnimalName, leftAnswerLabel, leftSourceAnimalImage, leftAnswerIcon);
            CopyAnswerVisual(rightSourceAnimalName, rightAnswerLabel, rightSourceAnimalImage, rightAnswerIcon);

            int count = Mathf.Min(sourceStarImages?.Length ?? 0, bottomStarImages?.Length ?? 0);
            for (int index = 0; index < count; index++)
            {
                Image source = sourceStarImages[index];
                Image target = bottomStarImages[index];
                if (source == null || target == null) continue;

                target.sprite = source.sprite;
                target.color = source.color;
                target.enabled = source.enabled;
                target.preserveAspect = true;
            }
        }

        private void RefreshInstructionCopy()
        {
            if (instructionText == null && instructionTransform != null)
            {
                instructionText = instructionTransform.GetComponentInChildren<TMP_Text>(true);
            }

            if (instructionText == null || string.IsNullOrWhiteSpace(instructionText.text)) return;

            // SizeWorldController conserva la decision pedagogica. Esta capa solo traduce
            // su pregunta actual a la redaccion visual solicitada para el nuevo panel.
            bool asksForSmaller = instructionText.text.IndexOf("pequen", StringComparison.OrdinalIgnoreCase) >= 0;
            bool asksForBigger = instructionText.text.IndexOf("grand", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!asksForSmaller && !asksForBigger) return;

            instructionText.text = asksForSmaller
                ? "Observa bien y elige el animal <color=#8048C8>más pequeño</color>"
                : "Observa bien y elige el animal <color=#8048C8>más grande</color>";
        }

        private static void CopyAnswerVisual(TMP_Text sourceName, TMP_Text targetName, Image sourceAnimal, Image targetIcon)
        {
            if (targetName != null && sourceName != null)
            {
                targetName.text = sourceName.text;
            }

            if (targetIcon == null || sourceAnimal == null) return;
            targetIcon.sprite = sourceAnimal.sprite;
            targetIcon.enabled = sourceAnimal.sprite != null;
            targetIcon.preserveAspect = true;
        }

        private void ShowMessage(string message)
        {
            if (messageText == null)
            {
                TMP_Text[] texts = transform.root.GetComponentsInChildren<TMP_Text>(true);
                foreach (TMP_Text text in texts)
                {
                    if (text != null && text.name == "Texto-feedback")
                    {
                        messageText = text;
                        break;
                    }
                }
            }

            if (messageText != null) messageText.text = message;
        }

        private void HideGameplayGuide()
        {
            if (guideTransform != null)
            {
                guideTransform.gameObject.SetActive(false);
            }
        }

        private void StopEntranceRoutine()
        {
            if (entranceRoutine == null) return;
            StopCoroutine(entranceRoutine);
            entranceRoutine = null;
        }

        private void StopGuideIdleRoutine()
        {
            if (guideIdleRoutine == null) return;
            StopCoroutine(guideIdleRoutine);
            guideIdleRoutine = null;
        }

        private void StopGuideReactionRoutine()
        {
            if (guideReactionRoutine == null) return;
            StopCoroutine(guideReactionRoutine);
            guideReactionRoutine = null;
        }

        private void CaptureBaseState()
        {
            titleState = new RectState(titleTransform);
            instructionState = new RectState(instructionTransform);
            guideState = new RectState(guideTransform);
            leftCardState = new RectState(leftCardTransform);
            rightCardState = new RectState(rightCardTransform);
            originalGuideSprite = guideImage != null ? guideImage.sprite : null;
            originalCanvasAlpha = presentationCanvasGroup != null ? presentationCanvasGroup.alpha : 1f;
        }

        private void RestoreBaseState()
        {
            RestoreRectState(titleTransform, titleState);
            RestoreRectState(instructionTransform, instructionState);
            RestoreGuideState();
            RestoreRectState(leftCardTransform, leftCardState);
            RestoreRectState(rightCardTransform, rightCardState);

            if (presentationCanvasGroup != null)
            {
                presentationCanvasGroup.alpha = originalCanvasAlpha;
            }
        }

        private void RestoreGuideState()
        {
            RestoreRectState(guideTransform, guideState);
        }

        private static void RestoreRectState(RectTransform target, RectState state)
        {
            if (target == null) return;
            target.anchoredPosition = state.anchoredPosition;
            target.localScale = state.localScale;
            target.localRotation = state.localRotation;
        }

        private void SetGuideSprite(Sprite requestedSprite)
        {
            if (guideImage != null && requestedSprite != null)
            {
                guideImage.sprite = requestedSprite;
            }
        }
    }
}
