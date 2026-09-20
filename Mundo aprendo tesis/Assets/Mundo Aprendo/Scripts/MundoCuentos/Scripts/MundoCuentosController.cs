using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bolin
{
    public enum MundoCuentosState
    {
        Library,
        StoryIntro,
        Narrating,
        ShowingPictogram,
        AskingQuestion,
        Listening,
        Processing,
        CorrectFeedback,
        RetryFeedback,
        MicrophoneError,
        RecognitionError,
        StoryComplete
    }

    [DisallowMultipleComponent]
    public sealed class MundoCuentosController : MonoBehaviour
    {
        private const string LogPrefix = "[MundoCuentos]";

        [Header("Datos")]
        [SerializeField] private List<CuentoData> cuentos = new();

        [Header("Componentes")]
        [SerializeField] private VoskVoiceRecognitionManager voiceRecognition;
        [SerializeField] private MundoCuentosUIController uiController;
        [SerializeField] private AudioSource activityAudioSource;

        [Header("Flujo")]
        [SerializeField, Min(0f)] private float correctFeedbackDuration = 1.25f;
        [SerializeField, Min(0f)] private float retryFeedbackDuration = 1.25f;
        [SerializeField, Min(1)] private int requiredCompletedStoriesToUnlockWorlds = 2;

        [Header("Estrellas por intentos fallidos")]
        [SerializeField, Min(0)] private int threeStarMaxErrors = 1;
        [SerializeField, Min(0)] private int twoStarMaxErrors = 3;
        [SerializeField, Min(0)] private int oneStarMaxErrors = 6;

        private readonly SpeechAnswerValidator answerValidator = new();
        private readonly StoryProgressRepository progressRepository = new();

        private Coroutine narrationRoutine;
        private Coroutine questionRoutine;
        private Coroutine feedbackRoutine;
        private CuentoData currentStory;
        private int currentStoryIndex = -1;
        private int currentPictogramIndex;
        private int currentAttempts;
        private int totalMistakes;
        private bool isProcessingAnswer;
        private bool listenWhenReady;
        private bool listenersBound;

        public event Action<bool> OnAnswerValidated;
        public event Action<CuentoData> OnStoryCompleted;
        public event Action<MundoCuentosState> OnStateChanged;

        public MundoCuentosState State { get; private set; } = MundoCuentosState.Library;
        public CuentoData CurrentStory => currentStory;
        public int CurrentPictogramIndex => currentPictogramIndex;

        private void Awake()
        {
            if (voiceRecognition == null) voiceRecognition = GetComponent<VoskVoiceRecognitionManager>();
            if (uiController == null) uiController = GetComponent<MundoCuentosUIController>();
            if (activityAudioSource == null) activityAudioSource = GetComponent<AudioSource>();

            ValidateConfiguration();
            ConfigureResultPanel();
        }

        private void OnEnable()
        {
            BindListeners();
        }

        private void Start()
        {
            voiceRecognition?.Initialize();
            ShowLibrary();
            uiController?.ShowTutorialIfNeeded();
        }

        private void OnDisable()
        {
            UnbindListeners();
            StopActiveFlow();
        }

        public void OpenStoryById(string storyId)
        {
            int index = FindStoryIndex(storyId);
            if (index < 0)
            {
                Debug.LogWarning($"{LogPrefix} No existe un cuento con id '{storyId}'.", this);
                return;
            }

            if (!IsStoryUnlocked(index))
            {
                Debug.LogWarning($"{LogPrefix} El cuento '{storyId}' todavía está bloqueado.", this);
                return;
            }

            StopActiveFlow();
            currentStoryIndex = index;
            currentStory = cuentos[index];
            currentPictogramIndex = 0;
            currentAttempts = 0;
            totalMistakes = 0;
            isProcessingAnswer = false;
            listenWhenReady = false;

            voiceRecognition?.ConfigureVocabulary(currentStory.ConstruirVocabulario());
            SetState(MundoCuentosState.StoryIntro);
            uiController?.ShowStoryIntro(currentStory);
            Debug.Log($"{LogPrefix} Cuento seleccionado: {currentStory.Id}", this);

            if (currentStory.NarracionAudio != null)
            {
                narrationRoutine = StartCoroutine(PlayNarrationRoutine());
            }
        }

        public void BeginActivity()
        {
            if (currentStory == null) return;
            if (State != MundoCuentosState.StoryIntro && State != MundoCuentosState.Narrating) return;

            StopNarration();
            ShowCurrentPictogram();
        }

        public void ShowLibrary()
        {
            StopActiveFlow();
            isProcessingAnswer = false;
            listenWhenReady = false;
            SetState(MundoCuentosState.Library);
            RefreshLibrary();
        }

        public void RepeatCurrentStory()
        {
            if (currentStory == null)
            {
                ShowLibrary();
                return;
            }

            OpenStoryById(currentStory.Id);
        }

        public void OpenNextStory()
        {
            if (currentStoryIndex < 0)
            {
                ShowLibrary();
                return;
            }

            int nextIndex = currentStoryIndex + 1;
            if (nextIndex >= cuentos.Count || !IsStoryUnlocked(nextIndex))
            {
                ShowLibrary();
                return;
            }

            OpenStoryById(cuentos[nextIndex].Id);
        }

        public void ReturnToWorldSelection()
        {
            StopActiveFlow();
            SceneNavigation.LoadScene(MundoAprendoSceneNames.WorldSelection, this);
        }

        public void RetryMicrophone()
        {
            if (currentStory == null)
            {
                ShowLibrary();
                return;
            }

            if (voiceRecognition == null)
            {
                HandleVoiceError(VoiceRecognitionErrorKind.RecognizerUnavailable,
                    "No se configuró el reconocimiento de voz.");
                return;
            }

            if (!voiceRecognition.IsReady)
            {
                listenWhenReady = true;
                uiController?.ShowPreparingRecognition("Preparando reconocimiento offline…");
                voiceRecognition.Initialize();
                return;
            }

            BeginListening();
        }

        private IEnumerator PlayNarrationRoutine()
        {
            voiceRecognition?.StopListening();
            SetState(MundoCuentosState.Narrating);
            uiController?.ShowNarrating();
            yield return PlayClipRoutine(currentStory.NarracionAudio);
            narrationRoutine = null;

            if (State == MundoCuentosState.Narrating) ShowCurrentPictogram();
        }

        private void ShowCurrentPictogram()
        {
            if (currentStory == null || currentStory.Pictogramas == null
                || currentPictogramIndex >= currentStory.Pictogramas.Count)
            {
                CompleteStory();
                return;
            }

            StopQuestion();
            voiceRecognition?.StopListening();
            PictogramaData pictogram = currentStory.Pictogramas[currentPictogramIndex];
            if (pictogram == null)
            {
                Debug.LogError($"{LogPrefix} Pictograma nulo en índice {currentPictogramIndex}.", this);
                HandleVoiceError(VoiceRecognitionErrorKind.Unknown, "El pictograma actual no está configurado.");
                return;
            }

            SetState(MundoCuentosState.ShowingPictogram);
            uiController?.ShowPictogram(pictogram, currentPictogramIndex, currentStory.Pictogramas.Count);
            Debug.Log($"{LogPrefix} Pictograma {currentPictogramIndex + 1}/{currentStory.Pictogramas.Count}: {pictogram.Concepto}", this);
            questionRoutine = StartCoroutine(AskQuestionRoutine(pictogram));
        }

        private IEnumerator AskQuestionRoutine(PictogramaData pictogram)
        {
            voiceRecognition?.StopListening();
            SetState(MundoCuentosState.AskingQuestion);
            uiController?.ShowAskingQuestion(pictogram.PreguntaAudio != null);

            if (pictogram.PreguntaAudio != null)
            {
                yield return PlayClipRoutine(pictogram.PreguntaAudio);
            }
            else
            {
                yield return null;
            }

            questionRoutine = null;
            if (State == MundoCuentosState.AskingQuestion) RequestListening();
        }

        private void RequestListening()
        {
            if (voiceRecognition == null)
            {
                HandleVoiceError(VoiceRecognitionErrorKind.RecognizerUnavailable,
                    "No se configuró el reconocimiento de voz.");
                return;
            }

            if (!voiceRecognition.IsReady)
            {
                listenWhenReady = true;
                uiController?.ShowPreparingRecognition("Preparando reconocimiento offline…");
                voiceRecognition.Initialize();
                return;
            }

            BeginListening();
        }

        private void BeginListening()
        {
            listenWhenReady = false;
            if (voiceRecognition == null || !voiceRecognition.StartListening()) return;

            isProcessingAnswer = false;
            SetState(MundoCuentosState.Listening);
            uiController?.ShowListening();
        }

        private void HandleRecognitionResult(IReadOnlyList<string> alternatives)
        {
            if (State != MundoCuentosState.Listening || isProcessingAnswer) return;
            if (alternatives == null || alternatives.Count == 0) return;

            isProcessingAnswer = true;
            voiceRecognition?.StopListening();
            SetState(MundoCuentosState.Processing);
            uiController?.ShowProcessing();
            uiController?.ShowRecognizedText(alternatives[0]);

            PictogramaData pictogram = currentStory?.Pictogramas?[currentPictogramIndex];
            SpeechValidationResult result = answerValidator.Validate(pictogram, alternatives);
            OnAnswerValidated?.Invoke(result.IsCorrect);

            if (result.IsCorrect)
            {
                feedbackRoutine = StartCoroutine(CorrectFeedbackRoutine(pictogram));
            }
            else
            {
                feedbackRoutine = StartCoroutine(RetryFeedbackRoutine(pictogram));
            }
        }

        private IEnumerator CorrectFeedbackRoutine(PictogramaData pictogram)
        {
            SetState(MundoCuentosState.CorrectFeedback);
            uiController?.ShowFeedback("¡Muy bien!", string.Empty, true);
            voiceRecognition?.StopListening();

            if (pictogram != null && pictogram.FeedbackCorrecto != null)
            {
                yield return PlayClipRoutine(pictogram.FeedbackCorrecto);
            }
            else if (correctFeedbackDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(correctFeedbackDuration);
            }

            feedbackRoutine = null;
            currentPictogramIndex++;
            currentAttempts = 0;
            isProcessingAnswer = false;

            if (currentStory == null || currentPictogramIndex >= currentStory.Pictogramas.Count)
            {
                CompleteStory();
            }
            else
            {
                ShowCurrentPictogram();
            }
        }

        private IEnumerator RetryFeedbackRoutine(PictogramaData pictogram)
        {
            currentAttempts++;
            totalMistakes++;
            SetState(MundoCuentosState.RetryFeedback);
            voiceRecognition?.StopListening();

            string concept = pictogram?.Concepto ?? "respuesta";
            string message;
            string hint = string.Empty;
            if (currentAttempts == 1)
            {
                message = "Inténtalo otra vez.";
            }
            else if (currentAttempts == 2)
            {
                message = "Escucha la pista y vuelve a intentarlo.";
                hint = !string.IsNullOrWhiteSpace(pictogram?.Ayuda)
                    ? pictogram.Ayuda
                    : BuildAutomaticHint(concept);
            }
            else
            {
                message = $"Es una {concept}. Di: {concept}.";
            }

            uiController?.ShowFeedback(message, hint, false);
            if (retryFeedbackDuration > 0f) yield return new WaitForSecondsRealtime(retryFeedbackDuration);

            feedbackRoutine = null;
            isProcessingAnswer = false;
            RequestListening();
        }

        private void CompleteStory()
        {
            StopActiveFlow();
            if (currentStory == null) return;

            int stars = StarRatingCalculator.FromMistakes(
                totalMistakes,
                threeStarMaxErrors,
                twoStarMaxErrors,
                oneStarMaxErrors);
            stars = Mathf.Max(1, stars);

            progressRepository.SaveStoryResult(currentStory.Id, 100, stars, true);
            SetState(MundoCuentosState.StoryComplete);
            bool hasNextStory = currentStoryIndex + 1 < cuentos.Count;
            uiController?.ShowStoryComplete(currentStory, stars, hasNextStory);
            OnStoryCompleted?.Invoke(currentStory);
            Debug.Log($"{LogPrefix} Cuento completado: {currentStory.Id}. Errores: {totalMistakes}. Estrellas: {stars}.", this);
        }

        private void HandleVoiceReady()
        {
            if (!listenWhenReady || currentStory == null) return;
            BeginListening();
        }

        private void HandleVoiceStatus(VoiceRecognitionState state, string message)
        {
            if (State == MundoCuentosState.AskingQuestion && !string.IsNullOrWhiteSpace(message))
            {
                uiController?.ShowPreparingRecognition(message);
            }
        }

        private void HandleVoiceError(VoiceRecognitionErrorKind kind, string message)
        {
            voiceRecognition?.StopListening();
            listenWhenReady = false;
            isProcessingAnswer = false;

            if (kind == VoiceRecognitionErrorKind.MicrophoneMissing)
            {
                SetState(MundoCuentosState.MicrophoneError);
                uiController?.ShowMicrophoneError();
                return;
            }

            SetState(MundoCuentosState.RecognitionError);
            uiController?.ShowRecognitionError(message);
        }

        private void HandlePrimaryAction()
        {
            switch (State)
            {
                case MundoCuentosState.StoryIntro:
                    BeginActivity();
                    break;
                case MundoCuentosState.MicrophoneError:
                case MundoCuentosState.RecognitionError:
                    RetryMicrophone();
                    break;
            }
        }

        private void HandleSecondaryAction()
        {
            if (State == MundoCuentosState.MicrophoneError || State == MundoCuentosState.RecognitionError)
            {
                ShowLibrary();
            }
        }

        private IEnumerator PlayClipRoutine(AudioClip clip)
        {
            voiceRecognition?.StopListening();
            if (activityAudioSource == null || clip == null) yield break;

            activityAudioSource.Stop();
            activityAudioSource.clip = clip;
            activityAudioSource.loop = false;
            activityAudioSource.Play();
            while (activityAudioSource != null && activityAudioSource.isPlaying) yield return null;
        }

        private void RefreshLibrary()
        {
            int completedStories = CountCompletedStories();
            bool otherWorldsUnlocked = completedStories >= Mathf.Max(1, requiredCompletedStoriesToUnlockWorlds);
            uiController?.ShowLibrary(cuentos, IsStoryUnlocked, completedStories, otherWorldsUnlocked);
        }

        private int CountCompletedStories()
        {
            HashSet<string> completedIds = new(StringComparer.OrdinalIgnoreCase);
            foreach (CuentoData story in cuentos)
            {
                if (story == null || string.IsNullOrWhiteSpace(story.Id)) continue;
                if (StoryProgressRepository.IsStoryCompleted(story.Id)) completedIds.Add(story.Id);
            }

            return completedIds.Count;
        }

        private bool IsStoryUnlocked(int index)
        {
            if (index <= 0) return index == 0;
            if (index >= cuentos.Count) return false;

            CuentoData previousStory = cuentos[index - 1];
            return previousStory != null && StoryProgressRepository.IsStoryCompleted(previousStory.Id);
        }

        private int FindStoryIndex(string storyId)
        {
            if (string.IsNullOrWhiteSpace(storyId)) return -1;
            for (int i = 0; i < cuentos.Count; i++)
            {
                CuentoData story = cuentos[i];
                if (story != null && string.Equals(story.Id, storyId, StringComparison.OrdinalIgnoreCase)) return i;
            }

            return -1;
        }

        private void ConfigureResultPanel()
        {
            uiController?.ConfigureResultActions(RepeatCurrentStory, OpenNextStory, ShowLibrary, ReturnToWorldSelection);
        }

        private void BindListeners()
        {
            if (listenersBound) return;
            listenersBound = true;

            if (uiController != null)
            {
                uiController.StorySelected += OpenStoryById;
                uiController.PrimaryActionRequested += HandlePrimaryAction;
                uiController.SecondaryActionRequested += HandleSecondaryAction;
                uiController.BackToLibraryRequested += ShowLibrary;
                uiController.BackToWorldsRequested += ReturnToWorldSelection;
                uiController.OtherWorldsRequested += ReturnToWorldSelection;
            }

            if (voiceRecognition != null)
            {
                voiceRecognition.OnReady += HandleVoiceReady;
                voiceRecognition.OnRecognitionResult += HandleRecognitionResult;
                voiceRecognition.OnStatusChanged += HandleVoiceStatus;
                voiceRecognition.OnError += HandleVoiceError;
            }
        }

        private void UnbindListeners()
        {
            if (!listenersBound) return;
            listenersBound = false;

            if (uiController != null)
            {
                uiController.StorySelected -= OpenStoryById;
                uiController.PrimaryActionRequested -= HandlePrimaryAction;
                uiController.SecondaryActionRequested -= HandleSecondaryAction;
                uiController.BackToLibraryRequested -= ShowLibrary;
                uiController.BackToWorldsRequested -= ReturnToWorldSelection;
                uiController.OtherWorldsRequested -= ReturnToWorldSelection;
            }

            if (voiceRecognition != null)
            {
                voiceRecognition.OnReady -= HandleVoiceReady;
                voiceRecognition.OnRecognitionResult -= HandleRecognitionResult;
                voiceRecognition.OnStatusChanged -= HandleVoiceStatus;
                voiceRecognition.OnError -= HandleVoiceError;
            }
        }

        private void StopActiveFlow()
        {
            voiceRecognition?.StopListening();
            if (activityAudioSource != null) activityAudioSource.Stop();
            StopNarration();
            StopQuestion();
            if (feedbackRoutine != null)
            {
                StopCoroutine(feedbackRoutine);
                feedbackRoutine = null;
            }
        }

        private void StopNarration()
        {
            if (narrationRoutine == null) return;
            StopCoroutine(narrationRoutine);
            narrationRoutine = null;
        }

        private void StopQuestion()
        {
            if (questionRoutine == null) return;
            StopCoroutine(questionRoutine);
            questionRoutine = null;
        }

        private void SetState(MundoCuentosState nextState)
        {
            if (State == nextState) return;
            State = nextState;
            Debug.Log($"{LogPrefix} Estado: {State}", this);
            OnStateChanged?.Invoke(State);
        }

        private static string BuildAutomaticHint(string concept)
        {
            string normalized = SpeechAnswerValidator.NormalizeText(concept);
            if (string.IsNullOrEmpty(normalized)) return "Mira con atención el pictograma.";
            string firstWord = normalized.Split(' ')[0];
            int length = Mathf.Min(2, firstWord.Length);
            return $"Empieza con {firstWord.Substring(0, length).ToUpperInvariant()}…";
        }

        private void ValidateConfiguration()
        {
            if (uiController == null) Debug.LogError($"{LogPrefix} Falta MundoCuentosUIController.", this);
            if (voiceRecognition == null) Debug.LogError($"{LogPrefix} Falta VoskVoiceRecognitionManager.", this);
            if (activityAudioSource == null) Debug.LogWarning($"{LogPrefix} Falta AudioSource para narración y feedback.", this);
            if (cuentos == null || cuentos.Count == 0) Debug.LogError($"{LogPrefix} No hay CuentoData configurados.", this);
        }
    }
}
