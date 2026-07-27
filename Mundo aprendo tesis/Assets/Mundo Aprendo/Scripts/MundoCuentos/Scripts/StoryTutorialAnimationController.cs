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
        public Vector2 dialogueAnchoredPosition;
        [Min(0f)] public float minimumVisibleSeconds;
        public Sprite iconSprite;
    }

    /// <summary>
    /// One-time story tutorial. Its presentation intentionally uses the same
    /// entrance rhythm as Mundo Tamaños, without a target highlight or idle float.
    /// </summary>
    public class StoryTutorialAnimationController : MonoBehaviour
    {
        [Header("Overlay")]
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private CanvasGroup inputBlockerGroup;
        [SerializeField] private Image darkBackground;

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
        [SerializeField, Min(1f)] private float typewriterCharactersPerSecond = 42f;
        [SerializeField, Min(0f)] private float characterEntryDistance = 120f;
        [SerializeField, Min(0.05f)] private float entryDuration = 0.28f;
        [SerializeField, Min(0f)] private float waveDegrees = 4f;

        private Coroutine typeRoutine;
        private Coroutine entryRoutine;
        private string currentFullText = string.Empty;
        private int currentStepIndex;
        private bool isTyping;
        private Vector2 guideBasePosition;
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
            if (confirmSkipButton != null) confirmSkipButton.onClick.AddListener(SkipTutorial);
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
            StopRunningRoutines();
            IsActive = true;
            currentStepIndex = 0;
            CancelSkip();
            CaptureBaseState();
            SetGroup(overlayGroup, 1f, true);
            SetGroup(inputBlockerGroup, 1f, true);
            ApplyCurrentStep();
            PlayEntryAnimation();
        }

        public void NextStep()
        {
            if (!CanAdvanceStep()) return;

            if (isTyping) FinishTyping();
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

            if (isTyping) FinishTyping();
            currentStepIndex--;
            ApplyCurrentStep();
        }

        public void CompleteTutorial()
        {
            if (!IsActive) return;

            if (isTyping) FinishTyping();
            StoryProgressRepository.MarkTutorialCompleted();
            HideImmediate();
        }

        public void RequestSkip()
        {
            if (!IsActive) return;

            if (skipConfirmationRoot == null)
            {
                SkipTutorial();
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

        private void SkipTutorial()
        {
            if (!IsActive) return;

            StoryProgressRepository.MarkTutorialCompleted();
            HideImmediate();
        }

        private void HideImmediate(bool deactivateObject)
        {
            StopRunningRoutines();
            IsActive = false;
            ResetPresentationState();
            if (tutorialText != null) tutorialText.text = string.Empty;
            SetGroup(overlayGroup, 0f, false);
            SetGroup(inputBlockerGroup, 0f, false);
            SetGroup(skipConfirmationGroup, 0f, false);
            if (skipConfirmationRoot != null) skipConfirmationRoot.SetActive(false);
            if (deactivateObject) gameObject.SetActive(false);
        }

        private bool CanAdvanceStep()
        {
            if (!IsActive || tutorialSteps.Count == 0) return false;
            TutorialStepData step = tutorialSteps[Mathf.Clamp(currentStepIndex, 0, tutorialSteps.Count - 1)];
            return Time.unscaledTime - stepShownAt >= Mathf.Max(0f, step.minimumVisibleSeconds);
        }

        private void ApplyCurrentStep()
        {
            TutorialStepData step = tutorialSteps[Mathf.Clamp(currentStepIndex, 0, tutorialSteps.Count - 1)];
            stepShownAt = Time.unscaledTime;

            StartTyping(step.text);
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
        }

        private void StartTyping(string message)
        {
            currentFullText = message ?? string.Empty;
            if (typeRoutine != null) StopCoroutine(typeRoutine);
            typeRoutine = StartCoroutine(TypeRoutine());
        }

        private IEnumerator TypeRoutine()
        {
            isTyping = true;
            if (tutorialText != null) tutorialText.text = string.Empty;

            if (string.IsNullOrEmpty(currentFullText) || typewriterCharactersPerSecond <= 0f)
            {
                FinishTyping();
                yield break;
            }

            float delay = 1f / typewriterCharactersPerSecond;
            float elapsed = 0f;
            int visibleCharacters = 0;
            while (visibleCharacters < currentFullText.Length)
            {
                elapsed += Time.unscaledDeltaTime;
                int targetCount = Mathf.Min(currentFullText.Length, Mathf.FloorToInt(elapsed / delay));
                if (targetCount > visibleCharacters)
                {
                    visibleCharacters = targetCount;
                    if (tutorialText != null) tutorialText.text = currentFullText.Substring(0, visibleCharacters);
                }

                yield return null;
            }

            FinishTyping();
        }

        private void FinishTyping()
        {
            if (typeRoutine != null)
            {
                StopCoroutine(typeRoutine);
                typeRoutine = null;
            }

            isTyping = false;
            if (tutorialText != null) tutorialText.text = currentFullText;
        }

        private void PlayEntryAnimation()
        {
            if (entryRoutine != null) StopCoroutine(entryRoutine);
            if (!isActiveAndEnabled || (guideCharacter == null && dialoguePanel == null)) return;
            entryRoutine = StartCoroutine(EntryRoutine());
        }

        private IEnumerator EntryRoutine()
        {
            Vector2 startPosition = guideBasePosition + Vector2.left * characterEntryDistance;
            Vector3 startBubble = dialogueBaseScale * 0.92f;
            float elapsed = 0f;

            if (guideCharacter != null) guideCharacter.anchoredPosition = startPosition;
            if (dialoguePanel != null) dialoguePanel.localScale = startBubble;

            while (elapsed < entryDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / entryDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);

                if (guideCharacter != null)
                {
                    guideCharacter.anchoredPosition = Vector2.LerpUnclamped(startPosition, guideBasePosition, eased);
                    guideCharacter.localRotation = guideBaseRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 2f) * waveDegrees);
                }

                if (dialoguePanel != null) dialoguePanel.localScale = Vector3.LerpUnclamped(startBubble, dialogueBaseScale, eased);
                yield return null;
            }

            ResetPresentationState();
            entryRoutine = null;
        }

        private void StopRunningRoutines()
        {
            if (typeRoutine != null) StopCoroutine(typeRoutine);
            if (entryRoutine != null) StopCoroutine(entryRoutine);
            typeRoutine = null;
            entryRoutine = null;
            isTyping = false;
        }

        private void CaptureBaseState()
        {
            if (guideCharacter != null)
            {
                guideBasePosition = guideCharacter.anchoredPosition;
                guideBaseRotation = guideCharacter.localRotation;
            }

            if (dialoguePanel != null)
            {
                dialogueBasePosition = dialoguePanel.anchoredPosition;
                dialogueBaseScale = dialoguePanel.localScale;
            }
        }

        private void ResetPresentationState()
        {
            if (guideCharacter != null)
            {
                guideCharacter.anchoredPosition = guideBasePosition;
                guideCharacter.localRotation = guideBaseRotation;
            }

            if (dialoguePanel != null) dialoguePanel.localScale = dialogueBaseScale;
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
            IsActive = false;
        }
    }
}
