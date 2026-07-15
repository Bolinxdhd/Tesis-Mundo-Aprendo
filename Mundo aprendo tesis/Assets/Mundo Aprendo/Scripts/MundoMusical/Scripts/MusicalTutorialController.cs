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
    /// Controls the one-time, non-highlighted introduction for Mundo Musical.
    /// It only owns presentation and input blocking; musical gameplay remains in
    /// MundoMusicalSequenceGame.
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
        [SerializeField] private TMP_Text tutorialText;
        [SerializeField] private TMP_Text stepCounterText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button startButton;

        [Header("Pasos")]
        [SerializeField] private List<MusicalTutorialStep> tutorialSteps = new();

        [Header("Transicion")]
        [SerializeField, Min(0.05f)] private float fadeDuration = 0.22f;

        private Coroutine visibilityRoutine;
        private int currentStepIndex;
        private bool gameplayStateCaptured;
        private bool gameplayInteractable;
        private bool gameplayBlocksRaycasts;
        private bool completionRequested;

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

            if (nextButton != null) nextButton.onClick.AddListener(NextStep);
            if (startButton != null) startButton.onClick.AddListener(CompleteTutorial);

            EnsureDefaultSteps();
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
            IsActive = true;
            currentStepIndex = 0;
            BlockGameplay();
            ApplyCurrentStep();
            StartVisibilityRoutine(true);
        }

        public void NextStep()
        {
            if (!IsActive || currentStepIndex >= tutorialSteps.Count - 1) return;

            currentStepIndex++;
            ApplyCurrentStep();
        }

        public void CompleteTutorial()
        {
            if (!IsActive || tutorialSteps.Count == 0 || currentStepIndex != tutorialSteps.Count - 1) return;

            // The key is deliberately written only from the final Comenzar action.
            MarkTutorialCompleted();
            completionRequested = true;
            SetButtonsInteractable(false);
            StartVisibilityRoutine(false);
        }

        public void HideImmediate()
        {
            HideImmediate(true);
        }

        private void HideImmediate(bool deactivateObject)
        {
            StopVisibilityRoutine();
            IsActive = false;
            SetOverlayState(0f, false);
            RestoreGameplay();

            if (deactivateObject) gameObject.SetActive(false);
        }

        private void ApplyCurrentStep()
        {
            if (tutorialSteps.Count == 0) return;

            MusicalTutorialStep step = tutorialSteps[Mathf.Clamp(currentStepIndex, 0, tutorialSteps.Count - 1)];
            if (tutorialText != null) tutorialText.text = step.text ?? string.Empty;
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

        private void StartVisibilityRoutine(bool show)
        {
            StopVisibilityRoutine();
            visibilityRoutine = StartCoroutine(VisibilityRoutine(show));
        }

        private IEnumerator VisibilityRoutine(bool show)
        {
            if (show) SetOverlayState(0f, true);

            float startAlpha = overlayGroup != null ? overlayGroup.alpha : (show ? 0f : 1f);
            float targetAlpha = show ? 1f : 0f;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDuration));
                SetOverlayState(Mathf.Lerp(startAlpha, targetAlpha, t), true);
                yield return null;
            }

            visibilityRoutine = null;
            if (show)
            {
                SetOverlayState(1f, true);
            }
            else
            {
                bool notifyCompletion = completionRequested;
                completionRequested = false;
                HideImmediate(true);
                if (notifyCompletion) TutorialCompleted?.Invoke();
            }
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

        private void StopVisibilityRoutine()
        {
            if (visibilityRoutine != null) StopCoroutine(visibilityRoutine);
            visibilityRoutine = null;
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
            StopVisibilityRoutine();
            if (IsActive)
            {
                IsActive = false;
                RestoreGameplay();
            }
        }
    }
}
