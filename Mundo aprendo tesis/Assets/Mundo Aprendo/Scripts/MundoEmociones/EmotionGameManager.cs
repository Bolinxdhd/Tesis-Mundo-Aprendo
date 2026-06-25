using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolin
{
    public class EmotionGameManager : MonoBehaviour
    {
        public const int WorldIndex = 3;
        public const string CompletedKey = "MundoAprendo_World_3_Completed";
        public const string StarsKey = "MundoAprendo_World_3_Stars";

        [Header("Rondas")]
        [SerializeField] private List<EmotionRoundView> emotionViews = new();
        [SerializeField] private List<EmotionAnswerButton> answerButtons = new();
        [SerializeField] private bool includeFear;
        [SerializeField, Min(1)] private int totalRounds = 8;
        [SerializeField, Min(0f)] private float correctFeedbackDuration = 1.1f;
        [SerializeField, Min(0f)] private float retryFeedbackDuration = 0.85f;

        [Header("Estrellas por errores")]
        [SerializeField, Min(0)] private int threeStarMaxErrors = 1;
        [SerializeField, Min(0)] private int twoStarMaxErrors = 3;
        [SerializeField, Min(0)] private int oneStarMaxErrors = 5;

        [Header("Paneles existentes")]
        [SerializeField] private GameObject startPanel;
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private CanvasGroup gamePanelCanvasGroup;
        [SerializeField] private GameObject feedbackPanel;
        [SerializeField] private CanvasGroup feedbackCanvasGroup;
        [SerializeField] private EmotionResultPanel resultPanel;

        [Header("Textos existentes")]
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private UIProgressBar roundProgressBar;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private TMP_Text nunaDialogueText;

        [Header("Feedback existente")]
        [SerializeField] private Image feedbackIcon;
        [SerializeField] private Sprite correctIconSprite;
        [SerializeField] private Sprite retryIconSprite;

        [Header("Nuna")]
        [SerializeField] private RectTransform nunaRoot;
        [SerializeField, Min(0f)] private float nunaFloatDistance = 8f;
        [SerializeField, Min(0.1f)] private float nunaFloatSpeed = 1f;

        [Header("Audio")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip ambientMusic;
        [SerializeField] private AudioClip buttonClip;
        [SerializeField] private AudioClip correctClip;
        [SerializeField] private AudioClip incorrectClip;
        [SerializeField] private AudioClip starClip;
        [SerializeField] private AudioClip startClip;
        [SerializeField] private AudioClip finishClip;

        private readonly List<EmotionRoundView> availableViews = new();
        private Coroutine gameFlowRoutine;
        private Coroutine feedbackRoutine;
        private Coroutine emotionAnimationRoutine;
        private Coroutine nunaRoutine;
        private EmotionRoundView currentView;
        private EmotionType? previousEmotion;
        private int completedRounds;
        private int correctAnswers;
        private int mistakes;
        private bool acceptingAnswer;
        private bool activityFinished;

        private void Awake()
        {
            PrepareInitialState();
        }

        private void Start()
        {
            if (nunaRoot != null) nunaRoutine = StartCoroutine(NunaFloatRoutine());
            StartAmbientMusic();
        }

        private void OnDisable()
        {
            StopFlowRoutines();
            if (nunaRoutine != null)
            {
                StopCoroutine(nunaRoutine);
                nunaRoutine = null;
            }
        }

        public void StartActivity()
        {
            StopFlowRoutines();

            completedRounds = 0;
            correctAnswers = 0;
            mistakes = 0;
            previousEmotion = null;
            currentView = null;
            acceptingAnswer = false;
            activityFinished = false;

            if (startPanel != null) startPanel.SetActive(false);
            if (gamePanel != null) gamePanel.SetActive(true);
            resultPanel?.Hide();
            HideAllEmotionViews();
            HideFeedbackImmediate();
            ConfigureFearVisibility();
            SetAnswerButtonsInteractable(false);
            SetNunaDialogue("Mira la expresion y selecciona como se siente.");
            PlaySfx(startClip);

            gameFlowRoutine = StartCoroutine(BeginActivityRoutine());
        }

        public void RestartActivity()
        {
            StartActivity();
        }

        public void SubmitAnswer(EmotionType selectedEmotion)
        {
            if (!acceptingAnswer || activityFinished || currentView == null) return;

            acceptingAnswer = false;
            SetAnswerButtonsInteractable(false);
            PulseAnswerButton(selectedEmotion);
            PlaySfx(buttonClip);

            if (selectedEmotion == currentView.emotion)
            {
                correctAnswers++;
                ShowFeedback("Muy bien", "Muy bien. Reconociste la emocion.", correctIconSprite);
                PlaySfx(correctClip);
                gameFlowRoutine = StartCoroutine(CorrectAnswerRoutine());
                return;
            }

            mistakes++;
            ShowFeedback("Intentalo otra vez", "No pasa nada, intentalo otra vez.", retryIconSprite);
            PlaySfx(incorrectClip);
            feedbackRoutine = StartCoroutine(RetryAnswerRoutine());
        }

        public void ReturnToWorldSelection()
        {
            SceneNavigation.LoadScene(MundoAprendoSceneNames.WorldSelection, this);
        }

        private IEnumerator BeginActivityRoutine()
        {
            if (gamePanelCanvasGroup != null)
            {
                gamePanelCanvasGroup.alpha = 0f;
                float elapsed = 0f;
                const float duration = 0.3f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    gamePanelCanvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
                    yield return null;
                }

                gamePanelCanvasGroup.alpha = 1f;
            }

            ShowNextRound();
            gameFlowRoutine = null;
        }

        private void ShowNextRound()
        {
            BuildAvailableViews();
            if (availableViews.Count == 0)
            {
                Debug.LogError("EmotionGameManager: no hay emociones validas configuradas.", this);
                SetNunaDialogue("Faltan emociones por configurar.");
                return;
            }

            EmotionRoundView nextView = PickNextView();
            HideAllEmotionViews();
            currentView = nextView;
            previousEmotion = nextView.emotion;

            if (currentView.rootObject != null) currentView.rootObject.SetActive(true);
            if (instructionText != null) instructionText.text = "Como se siente?";
            if (progressText != null) progressText.text = $"{completedRounds + 1} / {totalRounds}";
            roundProgressBar?.SetProgress((completedRounds + 1f) / totalRounds, $"Ronda {completedRounds + 1} de {totalRounds}");
            SetNunaDialogue("Mira la expresion del personaje.");
            HideFeedbackImmediate();
            SetAnswerButtonsInteractable(true);
            acceptingAnswer = true;

            if (currentView.instructionAudio != null) PlaySfx(currentView.instructionAudio);
            if (emotionAnimationRoutine != null) StopCoroutine(emotionAnimationRoutine);
            emotionAnimationRoutine = StartCoroutine(AnimateEmotionEntranceRoutine(currentView));
        }

        private IEnumerator CorrectAnswerRoutine()
        {
            if (correctFeedbackDuration > 0f) yield return new WaitForSeconds(correctFeedbackDuration);
            yield return FadeFeedbackRoutine(0f);

            completedRounds++;
            roundProgressBar?.SetProgress(completedRounds / (float)totalRounds, $"Ronda {completedRounds} de {totalRounds}");
            if (completedRounds >= totalRounds)
            {
                CompleteActivity();
            }
            else
            {
                ShowNextRound();
            }

            gameFlowRoutine = null;
        }

        private IEnumerator RetryAnswerRoutine()
        {
            if (retryFeedbackDuration > 0f) yield return new WaitForSeconds(retryFeedbackDuration);
            yield return FadeFeedbackRoutine(0f);

            if (!activityFinished)
            {
                acceptingAnswer = true;
                SetAnswerButtonsInteractable(true);
            }

            feedbackRoutine = null;
        }

        private void CompleteActivity()
        {
            activityFinished = true;
            acceptingAnswer = false;
            SetAnswerButtonsInteractable(false);
            HideAllEmotionViews();
            HideFeedbackImmediate();

            int stars = CalculateStars(mistakes);
            SaveProgress(stars);
            PlaySfx(finishClip);
            SetNunaDialogue("Terminaste. Estoy orgullosa de ti.");

            if (gamePanel != null) gamePanel.SetActive(false);
            resultPanel?.Show(correctAnswers, mistakes, stars, sfxSource, starClip);
        }

        private int CalculateStars(int errorCount)
        {
            return StarRatingCalculator.FromMistakes(errorCount, threeStarMaxErrors, twoStarMaxErrors, oneStarMaxErrors);
        }

        private void SaveProgress(int stars)
        {
            WorldProgressRepository.SaveBestResult(WorldIndex, stars);
        }

        private void BuildAvailableViews()
        {
            availableViews.Clear();
            foreach (EmotionRoundView view in emotionViews)
            {
                if (view == null || view.rootObject == null) continue;
                if (!includeFear && view.emotion == EmotionType.Fear) continue;
                availableViews.Add(view);
            }
        }

        private EmotionRoundView PickNextView()
        {
            if (availableViews.Count == 1) return availableViews[0];

            int startIndex = Random.Range(0, availableViews.Count);
            for (int offset = 0; offset < availableViews.Count; offset++)
            {
                EmotionRoundView candidate = availableViews[(startIndex + offset) % availableViews.Count];
                if (!previousEmotion.HasValue || candidate.emotion != previousEmotion.Value) return candidate;
            }

            return availableViews[startIndex];
        }

        private IEnumerator AnimateEmotionEntranceRoutine(EmotionRoundView view)
        {
            if (view?.animatedRect == null)
            {
                emotionAnimationRoutine = null;
                yield break;
            }

            Vector3 startScale = Vector3.one * 0.86f;
            Vector3 endScale = Vector3.one;
            view.animatedRect.localScale = startScale;
            if (view.canvasGroup != null) view.canvasGroup.alpha = 0f;

            float elapsed = 0f;
            const float duration = 0.3f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                view.animatedRect.localScale = Vector3.Lerp(startScale, endScale, t);
                if (view.canvasGroup != null) view.canvasGroup.alpha = t;
                yield return null;
            }

            view.animatedRect.localScale = endScale;
            if (view.canvasGroup != null) view.canvasGroup.alpha = 1f;
            emotionAnimationRoutine = null;
        }

        private void ShowFeedback(string message, string nunaMessage, Sprite icon)
        {
            if (feedbackPanel != null) feedbackPanel.SetActive(true);
            if (feedbackText != null) feedbackText.text = message;
            if (feedbackIcon != null)
            {
                feedbackIcon.sprite = icon;
                feedbackIcon.enabled = icon != null;
            }

            if (feedbackCanvasGroup != null) feedbackCanvasGroup.alpha = 1f;
            SetNunaDialogue(nunaMessage);
        }

        private IEnumerator FadeFeedbackRoutine(float targetAlpha)
        {
            if (feedbackCanvasGroup == null) yield break;

            float startAlpha = feedbackCanvasGroup.alpha;
            float elapsed = 0f;
            const float duration = 0.2f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                feedbackCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            feedbackCanvasGroup.alpha = targetAlpha;
        }

        private void HideFeedbackImmediate()
        {
            if (feedbackCanvasGroup != null) feedbackCanvasGroup.alpha = 0f;
            if (feedbackText != null) feedbackText.text = string.Empty;
            if (feedbackIcon != null) feedbackIcon.enabled = false;
        }

        private void HideAllEmotionViews()
        {
            foreach (EmotionRoundView view in emotionViews)
            {
                if (view?.rootObject != null) view.rootObject.SetActive(false);
            }
        }

        private void ConfigureFearVisibility()
        {
            foreach (EmotionAnswerButton answerButton in answerButtons)
            {
                if (answerButton == null) continue;
                answerButton.SetVisible(includeFear || answerButton.Emotion != EmotionType.Fear);
            }
        }

        private void SetAnswerButtonsInteractable(bool interactable)
        {
            foreach (EmotionAnswerButton answerButton in answerButtons)
            {
                if (answerButton == null) continue;
                if (!includeFear && answerButton.Emotion == EmotionType.Fear) continue;
                answerButton.SetInteractable(interactable);
            }
        }

        private void PulseAnswerButton(EmotionType emotion)
        {
            foreach (EmotionAnswerButton answerButton in answerButtons)
            {
                if (answerButton != null && answerButton.Emotion == emotion)
                {
                    answerButton.Pulse();
                    return;
                }
            }
        }

        private IEnumerator NunaFloatRoutine()
        {
            yield return null;
            Vector2 basePosition = nunaRoot.anchoredPosition;
            while (true)
            {
                float offset = Mathf.Sin(Time.unscaledTime * nunaFloatSpeed) * nunaFloatDistance;
                nunaRoot.anchoredPosition = basePosition + Vector2.up * offset;
                yield return null;
            }
        }

        private void StartAmbientMusic()
        {
            if (musicSource == null || ambientMusic == null) return;
            musicSource.clip = ambientMusic;
            musicSource.loop = true;
            musicSource.Play();
        }

        private void PlaySfx(AudioClip clip)
        {
            if (sfxSource != null && clip != null) sfxSource.PlayOneShot(clip);
        }

        private void SetNunaDialogue(string message)
        {
            if (nunaDialogueText != null) nunaDialogueText.text = message;
        }

        private void PrepareInitialState()
        {
            StopFlowRoutines();
            completedRounds = 0;
            correctAnswers = 0;
            mistakes = 0;
            acceptingAnswer = false;
            activityFinished = false;
            currentView = null;
            previousEmotion = null;

            if (startPanel != null) startPanel.SetActive(true);
            if (gamePanel != null) gamePanel.SetActive(false);
            resultPanel?.Hide();
            HideAllEmotionViews();
            HideFeedbackImmediate();
            ConfigureFearVisibility();
            SetAnswerButtonsInteractable(false);
            if (progressText != null) progressText.text = $"0 / {totalRounds}";
            roundProgressBar?.SetProgress(0f, $"0 de {totalRounds}");
            if (instructionText != null) instructionText.text = "Como se siente?";
            SetNunaDialogue("Hola. Soy Nuna. Vamos a reconocer emociones.");
        }

        private void StopFlowRoutines()
        {
            if (gameFlowRoutine != null)
            {
                StopCoroutine(gameFlowRoutine);
                gameFlowRoutine = null;
            }

            if (feedbackRoutine != null)
            {
                StopCoroutine(feedbackRoutine);
                feedbackRoutine = null;
            }

            if (emotionAnimationRoutine != null)
            {
                StopCoroutine(emotionAnimationRoutine);
                emotionAnimationRoutine = null;
            }
        }

        private void OnValidate()
        {
            totalRounds = Mathf.Max(1, totalRounds);
            threeStarMaxErrors = Mathf.Max(0, threeStarMaxErrors);
            twoStarMaxErrors = Mathf.Max(threeStarMaxErrors, twoStarMaxErrors);
            oneStarMaxErrors = Mathf.Max(twoStarMaxErrors, oneStarMaxErrors);

            if (startPanel == null) Debug.LogWarning("EmotionGameManager: falta StartPanel.", this);
            if (gamePanel == null) Debug.LogWarning("EmotionGameManager: falta GamePanel.", this);
            if (resultPanel == null) Debug.LogWarning("EmotionGameManager: falta EmotionResultPanel.", this);
        }
    }
}
