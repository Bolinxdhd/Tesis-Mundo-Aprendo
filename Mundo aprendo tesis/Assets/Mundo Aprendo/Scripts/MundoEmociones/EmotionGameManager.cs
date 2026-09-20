using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolin
{
    // Controla el mundo de emociones: muestra expresiones, valida respuestas y guarda estrellas.
    public class EmotionGameManager : MonoBehaviour
    {
        public const int WorldIndex = 3;
        public const string CompletedKey = "MundoAprendo_World_3_Completed";
        public const string StarsKey = "MundoAprendo_World_3_Stars";

        public event Action<EmotionType> OnRoundStarted;
        public event Action<bool> OnAnswerValidated;
        public event Action<int> OnActivityCompleted;

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
        [SerializeField, Tooltip("Panel de resultado compartido por todos los mundos.")] private WorldResultPanel resultPanelView;

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
            // Deja visibles los paneles correctos antes de que el alumno pulse Iniciar.
            WireSceneButtons();
            ConfigureResultPanel();
            PrepareInitialState();
        }

        private void Start()
        {
            // Activa la animacion decorativa de Nuna y arranca la musica ambiental.
            if (nunaRoot != null)
            {
                nunaRoutine = StartCoroutine(EmotionGameAnimationController.FloatAnchoredPosition(
                    nunaRoot,
                    nunaFloatDistance,
                    nunaFloatSpeed));
            }

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
            // Boton Iniciar: reinicia rondas, errores y abre el panel de juego.
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
            if (resultPanelView != null) resultPanelView.HideImmediate();
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
            // Recibe la emocion elegida desde un EmotionAnswerButton y la compara con la ronda actual.
            if (!acceptingAnswer || activityFinished || currentView == null) return;

            acceptingAnswer = false;
            SetAnswerButtonsInteractable(false);
            PulseAnswerButton(selectedEmotion);
            PlaySfx(buttonClip);

            bool isCorrect = selectedEmotion == currentView.emotion;
            OnAnswerValidated?.Invoke(isCorrect);

            if (isCorrect)
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
            // Boton Volver: regresa a seleccion de mundos usando el navegador central.
            SceneNavigation.LoadScene(MundoAprendoSceneNames.WorldSelection, this);
        }

        private void WireSceneButtons()
        {
            // Estos enlaces se mantienen aunque una escena haya perdido eventos
            // persistentes del Inspector. Si existen, se respetan para no duplicar
            // acciones configuradas manualmente.
            WireButtonIfEmpty("StartButton", StartActivity);
            WireButtonIfEmpty("BackButton", ReturnToWorldSelection);
        }

        private static void WireButtonIfEmpty(string objectName, UnityEngine.Events.UnityAction action)
        {
            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            foreach (Button button in buttons)
            {
                if (button == null || button.name != objectName) continue;
                if (button.onClick.GetPersistentEventCount() == 0)
                {
                    button.onClick.AddListener(action);
                }

                return;
            }
        }

        private IEnumerator BeginActivityRoutine()
        {
            // Hace fade del panel de juego antes de mostrar la primera ronda.
            if (gamePanelCanvasGroup != null)
            {
                gamePanelCanvasGroup.alpha = 0f;
                yield return EmotionGameAnimationController.FadeCanvasGroup(gamePanelCanvasGroup, 1f, 0.3f);
            }

            ShowNextRound();
            gameFlowRoutine = null;
        }

        private void ShowNextRound()
        {
            // Escoge una emocion disponible, actualiza progreso y habilita botones.
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
            OnRoundStarted?.Invoke(currentView.emotion);

            if (emotionAnimationRoutine != null) StopCoroutine(emotionAnimationRoutine);
            emotionAnimationRoutine = StartCoroutine(AnimateEmotionEntranceRoutine(currentView));
        }

        private IEnumerator CorrectAnswerRoutine()
        {
            // Mantiene el feedback de acierto y luego avanza o finaliza la actividad.
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
            // Tras un error, oculta feedback y devuelve el control al alumno.
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
            // Calcula estrellas, guarda progreso y muestra el panel final.
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
            ShowResultPanel(stars);
            OnActivityCompleted?.Invoke(stars);
        }

        /// <summary>Declara al panel compartido que acciones ofrece Mundo de Emociones.</summary>
        private void ConfigureResultPanel()
        {
            if (resultPanelView == null) return;

            resultPanelView.Bind(WorldResultPanel.ResultAction.Retry, RestartActivity);
            resultPanelView.Bind(WorldResultPanel.ResultAction.Next, null);
            resultPanelView.Bind(WorldResultPanel.ResultAction.BackToList, null);
            resultPanelView.Bind(WorldResultPanel.ResultAction.WorldSelection, ReturnToWorldSelection);
            resultPanelView.SetLabel(WorldResultPanel.ResultAction.Retry, "REINTENTAR");
            resultPanelView.SetLabel(WorldResultPanel.ResultAction.WorldSelection, "VOLVER A MUNDOS");
        }

        private void ShowResultPanel(int stars)
        {
            if (resultPanelView == null)
            {
                Debug.LogWarning("Mundo de Emociones: falta asignar el panel de resultado compartido.", this);
                return;
            }

            PlaySfx(starClip);
            resultPanelView.Show(stars, $"Aciertos: {correctAnswers}    Errores: {mistakes}", "Actividad completada");
        }

        private int CalculateStars(int errorCount)
        {
            // Usa la regla compartida de estrellas segun cantidad de errores.
            return StarRatingCalculator.FromMistakes(errorCount, threeStarMaxErrors, twoStarMaxErrors, oneStarMaxErrors);
        }

        private void SaveProgress(int stars)
        {
            // Conecta este mundo con WorldProgressRepository.
            WorldProgressRepository.SaveBestResult(WorldIndex, stars);
        }

        private void BuildAvailableViews()
        {
            // Filtra emociones configuradas y respeta si miedo esta habilitado.
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
            // Elige una emocion aleatoria evitando repetir la anterior cuando sea posible.
            if (availableViews.Count == 1) return availableViews[0];

            int startIndex = UnityEngine.Random.Range(0, availableViews.Count);
            for (int offset = 0; offset < availableViews.Count; offset++)
            {
                EmotionRoundView candidate = availableViews[(startIndex + offset) % availableViews.Count];
                if (!previousEmotion.HasValue || candidate.emotion != previousEmotion.Value) return candidate;
            }

            return availableViews[startIndex];
        }

        private IEnumerator AnimateEmotionEntranceRoutine(EmotionRoundView view)
        {
            // Delegado visual: entra la expresion con escala/fade.
            if (view?.animatedRect == null)
            {
                emotionAnimationRoutine = null;
                yield break;
            }

            yield return EmotionGameAnimationController.PlayEmotionEntrance(view, 0.3f);
            emotionAnimationRoutine = null;
        }

        private void ShowFeedback(string message, string nunaMessage, Sprite icon)
        {
            // Muestra texto/icono de acierto o reintento y actualiza el dialogo de Nuna.
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
            // Reutiliza el controlador de animacion para ocultar o mostrar feedback.
            yield return EmotionGameAnimationController.FadeCanvasGroup(feedbackCanvasGroup, targetAlpha, 0.2f);
        }

        private void HideFeedbackImmediate()
        {
            if (feedbackCanvasGroup != null) feedbackCanvasGroup.alpha = 0f;
            if (feedbackText != null) feedbackText.text = string.Empty;
            if (feedbackIcon != null) feedbackIcon.enabled = false;
        }

        private void HideAllEmotionViews()
        {
            // Apaga todas las expresiones antes de activar la ronda actual.
            foreach (EmotionRoundView view in emotionViews)
            {
                if (view?.rootObject != null) view.rootObject.SetActive(false);
            }
        }

        private void ConfigureFearVisibility()
        {
            // Oculta el boton de miedo si la escena no debe incluir esa emocion.
            foreach (EmotionAnswerButton answerButton in answerButtons)
            {
                if (answerButton == null) continue;
                answerButton.SetVisible(includeFear || answerButton.Emotion != EmotionType.Fear);
            }
        }

        private void SetAnswerButtonsInteractable(bool interactable)
        {
            // Bloquea o libera los botones segun el estado de la ronda.
            foreach (EmotionAnswerButton answerButton in answerButtons)
            {
                if (answerButton == null) continue;
                if (!includeFear && answerButton.Emotion == EmotionType.Fear) continue;
                answerButton.SetInteractable(interactable);
            }
        }

        private void PulseAnswerButton(EmotionType emotion)
        {
            // Da feedback visual sobre el boton que el alumno acaba de tocar.
            foreach (EmotionAnswerButton answerButton in answerButtons)
            {
                if (answerButton != null && answerButton.Emotion == emotion)
                {
                    answerButton.Pulse();
                    return;
                }
            }
        }

        private void StartAmbientMusic()
        {
            // Conecta el AudioSource de musica con el clip ambiental configurado.
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
            // Estado inicial: panel de inicio activo, juego oculto y progreso en cero.
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
            if (resultPanelView != null) resultPanelView.HideImmediate();
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
            // Cancela corutinas activas para evitar que una ronda anterior siga corriendo.
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
            if (resultPanelView == null) Debug.LogWarning("EmotionGameManager: falta el panel de resultado compartido.", this);
        }
    }
}
