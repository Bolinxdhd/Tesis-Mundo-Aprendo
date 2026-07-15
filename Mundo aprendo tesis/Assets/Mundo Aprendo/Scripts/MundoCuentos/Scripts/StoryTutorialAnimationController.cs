using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolin
{
    [Serializable]
    public class TutorialStepData
    {
        [TextArea(2, 4)] public string text;
        public Sprite biblioSprite;
        public RectTransform highlightedElement;
        public Vector2 dialogueAnchoredPosition;
        [Min(0f)] public float minimumVisibleSeconds;
        public Sprite iconSprite;
    }

    public class StoryTutorialAnimationController : MonoBehaviour
    {
        [Header("Overlay")]
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private CanvasGroup inputBlockerGroup;
        [SerializeField] private Image darkBackground;
        [SerializeField] private RectTransform highlightArea;
        [SerializeField] private Image highlightImage;

        [Header("Biblio")]
        [SerializeField] private RectTransform guideCharacter;
        [SerializeField] private Image guideCharacterImage;

        [Header("Dialogo")]
        [SerializeField] private RectTransform dialoguePanel;
        [SerializeField] private TMP_Text tutorialText;
        [SerializeField] private TMP_Text stepCounterText;
        [SerializeField] private Image stepIconImage;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button finishButton;
        [SerializeField] private Button skipButton;

        [Header("Confirmacion")]
        [SerializeField] private GameObject skipConfirmationRoot;
        [SerializeField] private CanvasGroup skipConfirmationGroup;
        [SerializeField] private Button confirmSkipButton;
        [SerializeField] private Button cancelSkipButton;

        [Header("Pasos")]
        [SerializeField] private List<TutorialStepData> tutorialSteps = new();

        [Header("Animacion")]
        [SerializeField, Min(0.05f)] private float fadeDuration = 0.22f;
        [SerializeField, Min(0.05f)] private float entryDuration = 0.35f;
        [SerializeField, Min(0f)] private float entryDistance = 130f;
        [SerializeField, Min(0f)] private float idleFloatDistance = 8f;
        [SerializeField, Min(0f)] private float idleRotationDegrees = 3f;
        [SerializeField, Min(0f)] private float highlightPadding = 20f;

        private readonly Vector3[] highlightCorners = new Vector3[4];
        private Coroutine visibilityRoutine;
        private Coroutine motionRoutine;
        private int currentStepIndex;
        private Vector2 guideBasePosition;
        private Vector3 guideBaseScale = Vector3.one;
        private Quaternion guideBaseRotation = Quaternion.identity;
        private Vector2 dialogueBasePosition;
        private Vector3 dialogueBaseScale = Vector3.one;
        private float stepShownAt;

        public bool IsActive { get; private set; }

        private void Awake()
        {
            if (overlayGroup == null) overlayGroup = GetComponent<CanvasGroup>();
            if (inputBlockerGroup == null) inputBlockerGroup = overlayGroup;
            if (guideCharacterImage == null && guideCharacter != null) guideCharacterImage = guideCharacter.GetComponent<Image>();

            if (previousButton != null) previousButton.onClick.AddListener(PreviousStep);
            if (nextButton != null) nextButton.onClick.AddListener(NextStep);
            if (finishButton != null) finishButton.onClick.AddListener(CompleteTutorial);
            if (skipButton != null) skipButton.onClick.AddListener(RequestSkip);
            if (confirmSkipButton != null) confirmSkipButton.onClick.AddListener(CompleteTutorial);
            if (cancelSkipButton != null) cancelSkipButton.onClick.AddListener(CancelSkip);

            EnsureDefaultSteps();
            CaptureBaseState();
            HideImmediate(false);
        }

        public void ShowIfNeeded()
        {
            if (StoryProgressRepository.IsTutorialCompleted())
            {
                HideImmediate();
                return;
            }

            ShowTutorial();
        }

        public void ShowTutorial()
        {
            EnsureDefaultSteps();
            if (tutorialSteps.Count == 0)
            {
                StoryProgressRepository.MarkTutorialCompleted();
                HideImmediate();
                return;
            }

            gameObject.SetActive(true);
            IsActive = true;
            currentStepIndex = 0;
            CancelSkip();
            CaptureBaseState();
            ApplyCurrentStep();
            StartVisibilityRoutine(true);
            StartMotionRoutine();
        }

        public void NextStep()
        {
            if (!CanAdvanceStep()) return;

            if (currentStepIndex < tutorialSteps.Count - 1)
            {
                currentStepIndex++;
                ApplyCurrentStep();
                return;
            }

            CompleteTutorial();
        }

        public void PreviousStep()
        {
            if (!IsActive || currentStepIndex <= 0) return;

            currentStepIndex--;
            ApplyCurrentStep();
        }

        public void CompleteTutorial()
        {
            StoryProgressRepository.MarkTutorialCompleted();
            StartVisibilityRoutine(false);
        }

        public void RequestSkip()
        {
            if (!IsActive) return;

            if (skipConfirmationRoot == null)
            {
                CompleteTutorial();
                return;
            }

            skipConfirmationRoot.SetActive(true);
            SetGroup(skipConfirmationGroup, 1f, true);
        }

        public void CancelSkip()
        {
            SetGroup(skipConfirmationGroup, 0f, false);
            if (skipConfirmationRoot != null) skipConfirmationRoot.SetActive(false);
        }

        public void HideImmediate()
        {
            HideImmediate(true);
        }

        private void HideImmediate(bool deactivateObject)
        {
            StopRunningRoutines();
            IsActive = false;
            SetGroup(overlayGroup, 0f, false);
            SetGroup(inputBlockerGroup, 0f, false);
            SetGroup(skipConfirmationGroup, 0f, false);
            if (highlightArea != null) highlightArea.gameObject.SetActive(false);
            if (skipConfirmationRoot != null) skipConfirmationRoot.SetActive(false);
            if (deactivateObject) gameObject.SetActive(false);
        }

        private bool CanAdvanceStep()
        {
            if (!IsActive) return false;
            TutorialStepData step = tutorialSteps[Mathf.Clamp(currentStepIndex, 0, tutorialSteps.Count - 1)];
            return Time.unscaledTime - stepShownAt >= Mathf.Max(0f, step.minimumVisibleSeconds);
        }

        private void ApplyCurrentStep()
        {
            TutorialStepData step = tutorialSteps[Mathf.Clamp(currentStepIndex, 0, tutorialSteps.Count - 1)];
            stepShownAt = Time.unscaledTime;

            if (tutorialText != null) tutorialText.text = step.text ?? string.Empty;
            if (stepCounterText != null) stepCounterText.text = $"{currentStepIndex + 1}/{tutorialSteps.Count}";

            if (guideCharacterImage != null)
            {
                guideCharacterImage.sprite = step.biblioSprite;
                guideCharacterImage.enabled = step.biblioSprite != null;
                guideCharacterImage.preserveAspect = true;
            }

            if (stepIconImage != null)
            {
                stepIconImage.sprite = step.iconSprite;
                stepIconImage.enabled = step.iconSprite != null;
            }

            if (dialoguePanel != null)
            {
                dialoguePanel.anchoredPosition = step.dialogueAnchoredPosition == Vector2.zero
                    ? dialogueBasePosition
                    : step.dialogueAnchoredPosition;
            }

            SetButtonVisible(previousButton, currentStepIndex > 0);
            SetButtonVisible(nextButton, currentStepIndex < tutorialSteps.Count - 1);
            SetButtonVisible(finishButton, currentStepIndex == tutorialSteps.Count - 1);
            UpdateHighlight(step);
        }

        private void UpdateHighlight(TutorialStepData step)
        {
            if (highlightArea == null) return;

            bool showHighlight = step.highlightedElement != null && step.highlightedElement.gameObject.activeInHierarchy;
            highlightArea.gameObject.SetActive(showHighlight);
            if (!showHighlight) return;

            RectTransform parent = highlightArea.parent as RectTransform;
            if (parent == null) return;

            step.highlightedElement.GetWorldCorners(highlightCorners);
            Vector2 min = WorldToLocal(parent, highlightCorners[0]);
            Vector2 max = WorldToLocal(parent, highlightCorners[2]);
            Vector2 size = max - min;
            highlightArea.anchoredPosition = min + size * 0.5f;
            highlightArea.sizeDelta = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y)) + Vector2.one * highlightPadding;

            if (highlightImage != null)
            {
                Color color = highlightImage.color;
                color.a = 0.24f;
                highlightImage.color = color;
            }
        }

        private static Vector2 WorldToLocal(RectTransform parent, Vector3 worldPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent,
                RectTransformUtility.WorldToScreenPoint(null, worldPosition),
                null,
                out Vector2 localPoint);
            return localPoint;
        }

        private void StartVisibilityRoutine(bool show)
        {
            if (visibilityRoutine != null) StopCoroutine(visibilityRoutine);
            visibilityRoutine = StartCoroutine(VisibilityRoutine(show));
        }

        private IEnumerator VisibilityRoutine(bool show)
        {
            if (show)
            {
                SetGroup(overlayGroup, 0f, true);
                SetGroup(inputBlockerGroup, 0f, true);
            }

            float startAlpha = overlayGroup != null ? overlayGroup.alpha : (show ? 0f : 1f);
            float targetAlpha = show ? 1f : 0f;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDuration));
                float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                SetGroup(overlayGroup, alpha, true);
                SetGroup(inputBlockerGroup, alpha, true);
                yield return null;
            }

            if (!show)
            {
                HideImmediate();
            }
            else
            {
                SetGroup(overlayGroup, 1f, true);
                SetGroup(inputBlockerGroup, 1f, true);
            }

            visibilityRoutine = null;
        }

        private void StartMotionRoutine()
        {
            if (motionRoutine != null) StopCoroutine(motionRoutine);
            motionRoutine = StartCoroutine(MotionRoutine());
        }

        private IEnumerator MotionRoutine()
        {
            if (guideCharacter != null)
            {
                guideCharacter.anchoredPosition = guideBasePosition + Vector2.down * entryDistance;
                guideCharacter.localScale = guideBaseScale * 0.88f;
            }

            if (dialoguePanel != null) dialoguePanel.localScale = dialogueBaseScale * 0.92f;

            float elapsed = 0f;
            while (elapsed < entryDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / entryDuration), 3f);
                if (guideCharacter != null)
                {
                    guideCharacter.anchoredPosition = Vector2.LerpUnclamped(guideBasePosition + Vector2.down * entryDistance, guideBasePosition, t);
                    guideCharacter.localScale = Vector3.LerpUnclamped(guideBaseScale * 0.88f, guideBaseScale, t);
                }

                if (dialoguePanel != null) dialoguePanel.localScale = Vector3.LerpUnclamped(dialogueBaseScale * 0.92f, dialogueBaseScale, t);
                yield return null;
            }

            while (IsActive)
            {
                float wave = Mathf.Sin(Time.unscaledTime * 1.8f);
                if (guideCharacter != null)
                {
                    guideCharacter.anchoredPosition = guideBasePosition + Vector2.up * (wave * idleFloatDistance);
                    guideCharacter.localRotation = guideBaseRotation * Quaternion.Euler(0f, 0f, wave * idleRotationDegrees);
                }

                yield return null;
            }

            motionRoutine = null;
        }

        private void StopRunningRoutines()
        {
            if (visibilityRoutine != null) StopCoroutine(visibilityRoutine);
            if (motionRoutine != null) StopCoroutine(motionRoutine);
            visibilityRoutine = null;
            motionRoutine = null;
        }

        private void CaptureBaseState()
        {
            if (guideCharacter != null)
            {
                guideBasePosition = guideCharacter.anchoredPosition;
                guideBaseScale = guideCharacter.localScale;
                guideBaseRotation = guideCharacter.localRotation;
            }

            if (dialoguePanel != null)
            {
                dialogueBasePosition = dialoguePanel.anchoredPosition;
                dialogueBaseScale = dialoguePanel.localScale;
            }
        }

        private void EnsureDefaultSteps()
        {
            if (tutorialSteps == null) tutorialSteps = new List<TutorialStepData>();
            if (tutorialSteps.Count > 0) return;

            tutorialSteps.Add(new TutorialStepData { text = "Hola! Soy Biblio. Aqui vamos a leer cuentos juntos." });
            tutorialSteps.Add(new TutorialStepData { text = "Elige uno de estos libros para comenzar." });
            tutorialSteps.Add(new TutorialStepData { text = "Cuando veas la historia, presiona el microfono y leela en voz alta." });
            tutorialSteps.Add(new TutorialStepData { text = "Cuando termines, presiona Validar para descubrir tus estrellas." });
            tutorialSteps.Add(new TutorialStepData { text = "Muy bien! Puedes repetir los cuentos todas las veces que quieras." });
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button != null) button.gameObject.SetActive(visible);
        }

        private static void SetGroup(CanvasGroup group, float alpha, bool interactive)
        {
            if (group == null) return;
            group.alpha = alpha;
            group.interactable = interactive;
            group.blocksRaycasts = interactive;
        }

        private void OnDisable()
        {
            StopRunningRoutines();
        }
    }
}
