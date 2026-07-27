using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolin
{
    [Serializable]
    public class MusicalTutorialStep
    {
        [TextArea(2, 4)] public string text;
        public Sprite characterPose;
    }

    /// <summary>
    /// Controls the one-time introduction for Mundo Musical. Its entrance and
    /// dialogue rhythm intentionally match the tutorial used in Mundo Tamaños.
    /// </summary>
    public class MusicalTutorialController : MonoBehaviour
    {
        public const string TutorialSeenKey = "MundoMusical_TutorialVisto";

        [Header("Grupos")]
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private CanvasGroup gameplayGroup;

        [Header("Personaje y dialogo")]
        [SerializeField] private RectTransform characterRoot;
        [SerializeField] private Image characterImage;
        [SerializeField] private RectTransform bubbleRoot;
        [SerializeField] private TMP_Text tutorialText;
        [SerializeField] private TMP_Text stepCounterText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button startButton;

        [Header("Pasos")]
        [SerializeField] private List<MusicalTutorialStep> tutorialSteps = new();

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
        private bool gameplayStateCaptured;
        private bool gameplayInteractable;
        private bool gameplayBlocksRaycasts;
        private Vector2 characterBasePosition;
        private Vector3 characterBaseRotation;
        private Vector3 bubbleBaseScale = Vector3.one;

        public bool IsActive { get; private set; }
        public event Action TutorialCompleted;

        public static bool IsTutorialCompleted()
        {
            return PlayerPrefs.GetInt(TutorialSeenKey, 0) == 1;
        }

        public static void MarkTutorialCompleted(bool save = true)
        {
            PlayerPrefs.SetInt(TutorialSeenKey, 1);
            if (save) PlayerPrefs.Save();
        }

        public static void ResetTutorialState(bool save = true)
        {
            PlayerPrefs.DeleteKey(TutorialSeenKey);
            if (save) PlayerPrefs.Save();
        }

        private void Awake()
        {
            if (overlayGroup == null) overlayGroup = GetComponent<CanvasGroup>();
            if (characterImage == null && characterRoot != null) characterImage = characterRoot.GetComponent<Image>();
            if (bubbleRoot == null && tutorialText != null) bubbleRoot = tutorialText.transform.parent as RectTransform;

            if (nextButton != null) nextButton.onClick.AddListener(NextStep);
            if (startButton != null) startButton.onClick.AddListener(CompleteTutorial);

            EnsureDefaultSteps();
            CaptureBaseState();
            HideImmediate(false);
        }

        private void Start()
        {
            ShowIfNeeded();
        }

        public void ShowIfNeeded()
        {
            if (IsTutorialCompleted())
            {
                HideImmediate(true);
                return;
            }

            ShowTutorial();
        }

        public void ShowTutorial()
        {
            if (IsTutorialCompleted())
            {
                HideImmediate(true);
                return;
            }

            EnsureDefaultSteps();
            if (tutorialSteps.Count == 0)
            {
                HideImmediate(true);
                return;
            }

            gameObject.SetActive(true);
            StopRunningRoutines();
            IsActive = true;
            currentStepIndex = 0;
            BlockGameplay();
            SetOverlayState(1f, true);
            ApplyCurrentStep();
            PlayEntryAnimation();
        }

        public void NextStep()
        {
            if (!IsActive || tutorialSteps.Count == 0) return;

            // A click may reveal the pending text and continue, keeping the
            // existing five-button flow usable for keyboard and accessibility input.
            if (isTyping) FinishTyping();
            if (currentStepIndex >= tutorialSteps.Count - 1) return;

            currentStepIndex++;
            ApplyCurrentStep();
        }

        public void CompleteTutorial()
        {
            if (!IsActive || tutorialSteps.Count == 0 || currentStepIndex != tutorialSteps.Count - 1) return;

            if (isTyping) FinishTyping();
            MarkTutorialCompleted();
            SetButtonsInteractable(false);
            HideImmediate(true);
            TutorialCompleted?.Invoke();
        }

        public void HideImmediate()
        {
            HideImmediate(true);
        }

        private void HideImmediate(bool deactivateObject)
        {
            StopRunningRoutines();
            IsActive = false;
            ResetPresentationState();
            if (tutorialText != null) tutorialText.text = string.Empty;
            SetOverlayState(0f, false);
            RestoreGameplay();

            if (deactivateObject) gameObject.SetActive(false);
        }

        private void ApplyCurrentStep()
        {
            if (tutorialSteps.Count == 0) return;

            MusicalTutorialStep step = tutorialSteps[Mathf.Clamp(currentStepIndex, 0, tutorialSteps.Count - 1)];
            StartTyping(step.text);
            if (stepCounterText != null) stepCounterText.text = $"{currentStepIndex + 1}/{tutorialSteps.Count}";

            if (characterImage != null)
            {
                characterImage.sprite = step.characterPose;
                characterImage.enabled = step.characterPose != null;
                characterImage.preserveAspect = true;
            }

            bool isLastStep = currentStepIndex == tutorialSteps.Count - 1;
            SetButtonVisible(nextButton, !isLastStep);
            SetButtonVisible(startButton, isLastStep);
            SetButtonsInteractable(true);
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
            if (!isActiveAndEnabled || (characterRoot == null && bubbleRoot == null)) return;
            entryRoutine = StartCoroutine(EntryRoutine());
        }

        private IEnumerator EntryRoutine()
        {
            Vector2 startPosition = characterBasePosition + Vector2.left * characterEntryDistance;
            Vector3 startBubble = bubbleBaseScale * 0.92f;
            float elapsed = 0f;

            if (characterRoot != null) characterRoot.anchoredPosition = startPosition;
            if (bubbleRoot != null) bubbleRoot.localScale = startBubble;

            while (elapsed < entryDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / entryDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);

                if (characterRoot != null)
                {
                    characterRoot.anchoredPosition = Vector2.LerpUnclamped(startPosition, characterBasePosition, eased);
                    characterRoot.localEulerAngles = characterBaseRotation + Vector3.forward * (Mathf.Sin(t * Mathf.PI * 2f) * waveDegrees);
                }

                if (bubbleRoot != null) bubbleRoot.localScale = Vector3.LerpUnclamped(startBubble, bubbleBaseScale, eased);
                yield return null;
            }

            ResetPresentationState();
            entryRoutine = null;
        }

        private void CaptureBaseState()
        {
            if (characterRoot != null)
            {
                characterBasePosition = characterRoot.anchoredPosition;
                characterBaseRotation = characterRoot.localEulerAngles;
            }

            if (bubbleRoot != null) bubbleBaseScale = bubbleRoot.localScale;
        }

        private void ResetPresentationState()
        {
            if (characterRoot != null)
            {
                characterRoot.anchoredPosition = characterBasePosition;
                characterRoot.localEulerAngles = characterBaseRotation;
            }

            if (bubbleRoot != null) bubbleRoot.localScale = bubbleBaseScale;
        }

        private void BlockGameplay()
        {
            if (gameplayGroup == null) return;

            if (!gameplayStateCaptured)
            {
                gameplayInteractable = gameplayGroup.interactable;
                gameplayBlocksRaycasts = gameplayGroup.blocksRaycasts;
                gameplayStateCaptured = true;
            }

            gameplayGroup.interactable = false;
            gameplayGroup.blocksRaycasts = false;
        }

        private void RestoreGameplay()
        {
            if (gameplayGroup == null || !gameplayStateCaptured) return;

            gameplayGroup.interactable = gameplayInteractable;
            gameplayGroup.blocksRaycasts = gameplayBlocksRaycasts;
            gameplayStateCaptured = false;
        }

        private void SetOverlayState(float alpha, bool blocksInput)
        {
            if (overlayGroup == null) return;

            overlayGroup.alpha = alpha;
            overlayGroup.interactable = blocksInput;
            overlayGroup.blocksRaycasts = blocksInput;
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (nextButton != null) nextButton.interactable = interactable;
            if (startButton != null) startButton.interactable = interactable;
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button != null) button.gameObject.SetActive(visible);
        }

        private void StopRunningRoutines()
        {
            if (typeRoutine != null) StopCoroutine(typeRoutine);
            if (entryRoutine != null) StopCoroutine(entryRoutine);
            typeRoutine = null;
            entryRoutine = null;
            isTyping = false;
        }

        private void EnsureDefaultSteps()
        {
            if (tutorialSteps == null) tutorialSteps = new List<MusicalTutorialStep>();
            if (tutorialSteps.Count > 0) return;

            tutorialSteps.Add(new MusicalTutorialStep { text = "¡Hola! Soy Tamborcin. Vamos a tocar una melodía." });
            tutorialSteps.Add(new MusicalTutorialStep { text = "Primero escucha y observa las teclas que se iluminan." });
            tutorialSteps.Add(new MusicalTutorialStep { text = "Después, toca las mismas teclas en el mismo orden." });
            tutorialSteps.Add(new MusicalTutorialStep { text = "Escucha con atención y recuerda la secuencia." });
            tutorialSteps.Add(new MusicalTutorialStep { text = "¡Listo! Presiona Comenzar para tocar." });
        }

        private void OnDisable()
        {
            StopRunningRoutines();
            if (IsActive)
            {
                IsActive = false;
                RestoreGameplay();
            }
        }
    }
}
