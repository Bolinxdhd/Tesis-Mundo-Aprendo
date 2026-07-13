using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bolin
{
    public class CharacterDialogueController : MonoBehaviour, IPointerClickHandler
    {
        [Serializable]
        public class DialogueStep
        {
            [TextArea(2, 4)] public string message;
            public Sprite characterSprite;
            public string buttonText = "Siguiente";
        }

        private enum DialogueMode
        {
            Intro,
            Retry,
            Success
        }

        [Header("Referencias")]
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private RectTransform characterRoot;
        [SerializeField] private Image characterImage;
        [SerializeField] private RectTransform bubbleRoot;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text nextButtonText;

        [Header("Dialogos")]
        [SerializeField] private bool showIntroOnStart = true;
        [SerializeField] private List<DialogueStep> introSteps = new();
        [SerializeField] private string finalIntroButtonText = "Comenzar";
        [SerializeField, TextArea(2, 3)] private string retryMessage = "¡Casi! Mira con atención e inténtalo otra vez.";
        [SerializeField] private string retryButtonText = "Intentar otra vez";
        [SerializeField] private Sprite retryCharacterSprite;
        [SerializeField] private List<string> successMessages = new() { "¡Muy bien!", "¡Lo lograste!", "¡Excelente trabajo!" };
        [SerializeField] private string successButtonText = "Continuar";
        [SerializeField] private List<Sprite> successCharacterSprites = new();
        [SerializeField, Min(0f)] private float successAutoHideSeconds = 0.85f;

        [Header("Animacion")]
        [SerializeField, Min(1f)] private float typewriterCharactersPerSecond = 42f;
        [SerializeField, Min(0f)] private float characterEntryDistance = 120f;
        [SerializeField, Min(0.05f)] private float entryDuration = 0.28f;
        [SerializeField, Min(0f)] private float waveDegrees = 4f;

        [Header("Eventos")]
        [SerializeField] private UnityEvent onIntroCompleted = new();
        [SerializeField] private UnityEvent onRetryClosed = new();
        [SerializeField] private UnityEvent onSuccessClosed = new();

        private readonly List<DialogueStep> activeSteps = new();
        private Coroutine typeRoutine;
        private Coroutine entryRoutine;
        private Coroutine autoHideRoutine;
        private DialogueMode currentMode;
        private string currentFullText = string.Empty;
        private int currentStepIndex;
        private bool isTyping;
        private bool isVisible;
        private Vector2 characterBasePosition;
        private Vector3 characterBaseRotation;
        private Vector3 bubbleBaseScale = Vector3.one;

        public UnityEvent OnIntroCompleted => onIntroCompleted;
        public UnityEvent OnRetryClosed => onRetryClosed;
        public UnityEvent OnSuccessClosed => onSuccessClosed;
        public bool IsVisible => isVisible;

        private void Awake()
        {
            if (overlayGroup == null) overlayGroup = GetComponent<CanvasGroup>();
            if (nextButton != null) nextButton.onClick.AddListener(Advance);
            if (nextButtonText == null && nextButton != null) nextButtonText = nextButton.GetComponentInChildren<TMP_Text>(true);
            if (characterRoot != null)
            {
                characterBasePosition = characterRoot.anchoredPosition;
                characterBaseRotation = characterRoot.localEulerAngles;
            }

            if (bubbleRoot != null) bubbleBaseScale = bubbleRoot.localScale;
            HideImmediate();
        }

        private void Start()
        {
            if (showIntroOnStart)
            {
                ShowIntro();
            }
        }

        public void ShowIntro()
        {
            ShowSteps(DialogueMode.Intro, introSteps);
        }

        public void ShowRetryFeedback()
        {
            DialogueStep step = new()
            {
                message = retryMessage,
                characterSprite = retryCharacterSprite,
                buttonText = retryButtonText
            };
            ShowSteps(DialogueMode.Retry, new[] { step });
        }

        public void ShowSuccessFeedback()
        {
            string message = successMessages != null && successMessages.Count > 0
                ? successMessages[UnityEngine.Random.Range(0, successMessages.Count)]
                : "¡Muy bien!";
            DialogueStep step = new()
            {
                message = message,
                characterSprite = successCharacterSprites != null && successCharacterSprites.Count > 0
                    ? successCharacterSprites[UnityEngine.Random.Range(0, successCharacterSprites.Count)]
                    : null,
                buttonText = successButtonText
            };
            ShowSteps(DialogueMode.Success, new[] { step });

            if (successAutoHideSeconds > 0f)
            {
                if (autoHideRoutine != null) StopCoroutine(autoHideRoutine);
                autoHideRoutine = StartCoroutine(AutoHideSuccessRoutine());
            }
        }

        public void ShowFeedback(bool wasCorrect)
        {
            if (wasCorrect)
            {
                ShowSuccessFeedback();
            }
            else
            {
                ShowRetryFeedback();
            }
        }

        public void Advance()
        {
            if (!isVisible) return;

            if (isTyping)
            {
                FinishTyping();
                return;
            }

            currentStepIndex++;
            if (currentStepIndex < activeSteps.Count)
            {
                ShowCurrentStep();
                return;
            }

            CloseCurrentDialogue();
        }

        public void HideImmediate()
        {
            isVisible = false;
            StopRunningRoutines();
            if (overlayGroup != null)
            {
                overlayGroup.alpha = 0f;
                overlayGroup.interactable = false;
                overlayGroup.blocksRaycasts = false;
            }

            if (dialogueText != null) dialogueText.text = string.Empty;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isTyping)
            {
                FinishTyping();
            }
        }

        private void ShowSteps(DialogueMode mode, IEnumerable<DialogueStep> steps)
        {
            activeSteps.Clear();
            if (steps != null)
            {
                foreach (DialogueStep step in steps)
                {
                    if (step != null && !string.IsNullOrWhiteSpace(step.message))
                    {
                        activeSteps.Add(step);
                    }
                }
            }

            if (activeSteps.Count == 0)
            {
                if (mode == DialogueMode.Intro) onIntroCompleted?.Invoke();
                return;
            }

            StopRunningRoutines();
            currentMode = mode;
            currentStepIndex = 0;
            isVisible = true;
            gameObject.SetActive(true);

            if (overlayGroup != null)
            {
                overlayGroup.alpha = 1f;
                overlayGroup.interactable = true;
                overlayGroup.blocksRaycasts = true;
            }

            PlayEntryAnimation();
            ShowCurrentStep();
        }

        private void ShowCurrentStep()
        {
            DialogueStep step = activeSteps[currentStepIndex];
            if (characterImage != null && step.characterSprite != null)
            {
                characterImage.sprite = step.characterSprite;
                characterImage.enabled = true;
                characterImage.preserveAspect = true;
            }

            string label = step.buttonText;
            if (currentMode == DialogueMode.Intro && currentStepIndex == activeSteps.Count - 1)
            {
                label = string.IsNullOrWhiteSpace(finalIntroButtonText) ? "Comenzar" : finalIntroButtonText;
            }

            if (nextButtonText != null) nextButtonText.text = string.IsNullOrWhiteSpace(label) ? "Siguiente" : label;
            StartTyping(step.message);
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
            if (dialogueText != null) dialogueText.text = string.Empty;

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
                    if (dialogueText != null) dialogueText.text = currentFullText[..visibleCharacters];
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
            if (dialogueText != null) dialogueText.text = currentFullText;
        }

        private void CloseCurrentDialogue()
        {
            DialogueMode mode = currentMode;
            HideImmediate();

            switch (mode)
            {
                case DialogueMode.Intro:
                    onIntroCompleted?.Invoke();
                    break;
                case DialogueMode.Retry:
                    onRetryClosed?.Invoke();
                    break;
                case DialogueMode.Success:
                    onSuccessClosed?.Invoke();
                    break;
            }
        }

        private IEnumerator AutoHideSuccessRoutine()
        {
            yield return new WaitForSecondsRealtime(successAutoHideSeconds);
            if (isVisible && currentMode == DialogueMode.Success)
            {
                FinishTyping();
                CloseCurrentDialogue();
            }

            autoHideRoutine = null;
        }

        private void PlayEntryAnimation()
        {
            if (entryRoutine != null) StopCoroutine(entryRoutine);
            if (!isActiveAndEnabled || characterRoot == null || bubbleRoot == null) return;
            entryRoutine = StartCoroutine(EntryRoutine());
        }

        private IEnumerator EntryRoutine()
        {
            Vector2 startPosition = characterBasePosition + Vector2.left * characterEntryDistance;
            Vector2 endPosition = characterBasePosition;
            Vector3 startBubble = bubbleBaseScale * 0.92f;
            Vector3 endBubble = bubbleBaseScale;
            float elapsed = 0f;

            characterRoot.anchoredPosition = startPosition;
            bubbleRoot.localScale = startBubble;

            while (elapsed < entryDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / entryDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                characterRoot.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, eased);
                bubbleRoot.localScale = Vector3.LerpUnclamped(startBubble, endBubble, eased);
                characterRoot.localEulerAngles = characterBaseRotation + Vector3.forward * (Mathf.Sin(t * Mathf.PI * 2f) * waveDegrees);
                yield return null;
            }

            characterRoot.anchoredPosition = endPosition;
            characterRoot.localEulerAngles = characterBaseRotation;
            bubbleRoot.localScale = endBubble;
            entryRoutine = null;
        }

        private void StopRunningRoutines()
        {
            if (typeRoutine != null) StopCoroutine(typeRoutine);
            if (entryRoutine != null) StopCoroutine(entryRoutine);
            if (autoHideRoutine != null) StopCoroutine(autoHideRoutine);
            typeRoutine = null;
            entryRoutine = null;
            autoHideRoutine = null;
            isTyping = false;
        }

        private void OnDisable()
        {
            StopRunningRoutines();
        }
    }
}
