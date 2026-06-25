using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolin
{
    [Serializable]
    public class PalabraReconocida
    {
        public int id;
        public string textoOriginal;
        public string textoNormalizado;
        public bool esCorrecta;
        public float tiempoCreacion;
    }

    [Serializable]
    public class CuentoData
    {
        public string id;
        public string titulo;

        [TextArea(5, 30)]
        public string textoCompleto;

        public Sprite icono;
    }

    [Serializable]
    public class CuentoCardView
    {
        public string cuentoId;
        public Button button;
        public Image iconImage;
        public TMP_Text titleText;
        public TMP_Text completedText;
        public TMP_Text starsText;
    }

    public class VoiceRecognitionTest : MonoBehaviour
    {
        [Header("Paneles")]
        [SerializeField] private GameObject storySelectionPanel;
        [SerializeField] private GameObject readingPanel;
        [SerializeField] private GameObject resultPanel;

        [Header("Seleccion de cuentos")]
        [SerializeField] private List<CuentoData> cuentosDisponibles = new();
        [SerializeField] private List<CuentoCardView> cuentoCards = new();
        [SerializeField] private TMP_Text selectionProgressText;
        [SerializeField] private TMP_Text otherWorldsUnlockText;
        [SerializeField] private Button backToPreviousMenuButton;
        [SerializeField] private Button otherWorldsButton;

        [Header("Lectura")]
        [SerializeField] private TMP_Text selectedStoryTitleText;
        [SerializeField] private Image selectedStoryIconImage;
        [SerializeField] private TMP_Text storyText;
        [SerializeField] private TMP_Text recognizedText;
        [SerializeField] private TMP_Text recognizedPlaceholderText;
        [SerializeField] private TMP_Text partialRecognizedText;
        [SerializeField] private TMP_Text finalRecognizedTextDisplay;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text readingResultText;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private TMP_Text starsResultText;

        [Header("Resultado")]
        [SerializeField] private TMP_Text resultStoryTitleText;
        [SerializeField] private TMP_Text resultScoreText;
        [SerializeField] private TMP_Text resultStarsText;
        [SerializeField] private TMP_Text resultMessageText;

        [Header("Botones UI")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button stopButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button validateButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button backToSelectionButton;

        [Header("Desplazamiento")]
        [SerializeField] private ScrollRect recognizedTextScrollRect;
        [SerializeField] private ScrollRect storyBodyScrollRect;

        [Header("Estrellas")]
        [SerializeField] private Image[] starImages = new Image[3];
        [SerializeField] private Sprite fullStarSprite;
        [SerializeField] private Sprite emptyStarSprite;
        [SerializeField] private UIStarDisplay starDisplay;
        [SerializeField] private GameObject microphoneListeningIndicator;

        [Header("Configuracion")]
        [SerializeField, TextArea(4, 10)] private string story =
            "Habia una vez tres cerditos que vivian en el bosque. Cada uno construyo una casa para protegerse del lobo. El primer cerdito hizo una casa de paja, el segundo una casa de madera y el tercero una casa de ladrillos. Cuando llego el lobo, soplo muy fuerte, pero la casa de ladrillos no se cayo. Los tres cerditos aprendieron que trabajar con esfuerzo trae buenos resultados.";
        [SerializeField] private string returnSceneName = "SeleccionMundos";
        [SerializeField] private string otherWorldsSceneName = "SeleccionMundos";
        [SerializeField, Min(1)] private int requiredCompletedStoriesToUnlock = 2;
        [SerializeField, Min(0.5f)] private float resultReturnDelay = 5f;

        [Header("Validacion de lectura")]
        [SerializeField, Range(0f, 1f)] private float successThreshold = 0.7f;
        [SerializeField, Range(0f, 1f)] private float threeStarsThreshold = 0.8f;
        [SerializeField, Range(0f, 1f)] private float twoStarsThreshold = 0.6f;
        [SerializeField, Range(0f, 1f)] private float oneStarThreshold = 0.4f;
        [SerializeField] private bool removeCommonWords = true;
        [SerializeField] private bool completeOnlyWithAtLeastOneStar = true;

        [Header("Preparacion")]
        [SerializeField, Min(0f)] private float startCountdownSeconds = 5f;

        [Header("Silencio")]
        [SerializeField, Min(1f)] private float silenceTimeout = 6f;
        [SerializeField, Min(5f)] private float maxListeningTime = 60f;
        [SerializeField, Min(1)] private int minWordsRequired = 3;

        [Header("Diagnostico de microfono")]
        [SerializeField, Min(0.1f)] private float microphonePreflightSeconds = 1.25f;
        [SerializeField, Range(0.001f, 0.1f)] private float minimumMicrophoneSignal = 0.01f;

        [Header("Palabras temporales")]
        [SerializeField, Min(0.1f)] private float incorrectWordLifetime = 4f;
        [SerializeField] private Color incorrectWordColor = new(0.72f, 0.12f, 0.12f, 1f);

        private ISpeechToTextService speechService;
        private ReadingEvaluator readingEvaluator;
        private StoryProgressRepository progressRepository;
        private Coroutine countdownRoutine;
        private Coroutine scrollRoutine;
        private Coroutine storyScrollRoutine;
        private Coroutine resultReturnRoutine;
        private string finalRecognizedText = string.Empty;
        private string partialRecognizedCandidate = string.Empty;
        private string[] expectedStoryWords = Array.Empty<string>();
        private readonly List<PalabraReconocida> palabrasMostradas = new();
        private readonly Dictionary<int, Coroutine> eliminacionesPendientes = new();
        private int nextRecognizedWordId;
        private int nextExpectedWordIndex;
        private CuentoData currentStory;
        private float listeningStartedAt;
        private float lastSpeechAt;
        private int currentStars;
        private bool isListeningSession;
        private bool isShuttingDown;
        private bool microphoneSignalDetectedBeforeListening;
        private bool validationProcessed;

        private void Awake()
        {
            readingEvaluator = new ReadingEvaluator(removeCommonWords);
            progressRepository = new StoryProgressRepository();
            speechService = new WindowsDictationSpeechService();

            EnsureDefaultStories();
            currentStory = cuentosDisponibles.Count > 0 ? cuentosDisponibles[0] : null;
            if (currentStory != null) ApplyStoryData(currentStory);
            else expectedStoryWords = ReadingEvaluator.GetNormalizedWords(story);

            SubscribeSpeechService();
            WarnMissingSceneReferences();
            WireOptionalButtons();
            ApplyInitialText();
            RefreshStoryCards();
            MostrarSeleccionCuentos();
        }

        private void OnDisable()
        {
            StopListeningInternal(false);
            CancelPendingWordRemovals();
            CancelPendingResultReturn();
        }

        private void OnDestroy()
        {
            isShuttingDown = true;
            StopListeningInternal(false);
            CancelPendingWordRemovals();
            CancelPendingResultReturn();
            DisposeSpeechService();
        }

        private void Update()
        {
            CheckListeningTimeouts();
            if (microphoneListeningIndicator != null)
            {
                microphoneListeningIndicator.SetActive(isListeningSession && speechService != null && speechService.IsListening);
            }
        }

        public void StartListening()
        {
            if (validationProcessed)
            {
                SetStatus("Ya se mostro el resultado. Vuelve a elegir un cuento para leer otra vez.");
                return;
            }

            if (countdownRoutine != null)
            {
                SetStatus("Prepara tu voz. El contador ya esta activo.");
                return;
            }

            string blockReason = GetRecognitionBlockReason();
            if (!string.IsNullOrWhiteSpace(blockReason))
            {
                SetText(errorText, blockReason);
                SetStatus(blockReason);
                return;
            }

            microphoneSignalDetectedBeforeListening = false;
            ClearRecognizedText();
            countdownRoutine = StartCoroutine(StartListeningAfterCountdownRoutine());
        }

        public void StopListening()
        {
            StopListeningInternal(true);
        }

        public void RetryReading()
        {
            StopListeningInternal(false);
            ClearRecognizedText();
            validationProcessed = false;
            SetReadingButtonsInteractable(true);
            StartListening();
        }

        public void ClearRecognizedText()
        {
            finalRecognizedText = string.Empty;
            partialRecognizedCandidate = string.Empty;
            nextExpectedWordIndex = 0;
            nextRecognizedWordId = 0;
            CancelPendingWordRemovals();
            palabrasMostradas.Clear();

            SetText(recognizedText, string.Empty);
            SetRecognizedPlaceholderVisible(true);
            SetText(partialRecognizedText, string.Empty);
            SetText(finalRecognizedTextDisplay, string.Empty);
            SetText(errorText, string.Empty);
            UpdateValidationUi(0f, string.Empty);
            UpdateStarsUi(0);
            SetCountdownText(string.Empty);
            SetStatus("Texto reconocido limpiado.");
            ResetRecognizedScrollToTop();
        }

        public void ValidateReading()
        {
            if (validationProcessed)
            {
                SetStatus("La lectura ya fue validada. Espera el resultado.");
                return;
            }

            StopListeningInternal(false);
            PromotePartialCandidate();
            validationProcessed = true;
            SetReadingButtonsInteractable(false);

            string textToEvaluate = GetTextForEvaluation();
            int recognizedWordCount = readingEvaluator.CountWords(textToEvaluate);

            if (recognizedWordCount < minWordsRequired)
            {
                validationProcessed = false;
                SetReadingButtonsInteractable(true);
                UpdateValidationUi(0f, "No escuche suficiente. Intenta leer nuevamente.");
                UpdateStarsUi(0);
                SetStatus("No escuche suficiente. Intenta leer nuevamente.");
                return;
            }

            ReadingEvaluationResult result = readingEvaluator.Evaluate(
                story,
                textToEvaluate,
                threeStarsThreshold,
                twoStarsThreshold,
                oneStarThreshold);

            int score = Mathf.RoundToInt(result.Similarity * 100f);
            string cuentoId = currentStory != null ? currentStory.id : StoryProgressRepository.DefaultStoryId;

            UpdateValidationUi(result.Similarity, GetStarsResultMessage(result.Stars));
            UpdateStarsUi(result.Stars);
            progressRepository.SaveStoryResult(cuentoId, score, result.Stars, completeOnlyWithAtLeastOneStar);
            RefreshStoryCards();
            MostrarResultado(score, result.Stars);
            SetStatus(GetStarsResultMessage(result.Stars));
        }

        public void ReturnToMenu()
        {
            StopListeningInternal(false);
            DisposeSpeechService();

            if (string.IsNullOrWhiteSpace(returnSceneName))
            {
                Debug.LogWarning("VoiceRecognitionTest: no hay escena de retorno configurada.");
                SetStatus("No hay escena de retorno configurada.");
                return;
            }

            SceneNavigation.LoadScene(returnSceneName, this);
        }

        public void MostrarSeleccionCuentos()
        {
            StopListeningInternal(false);
            CancelPendingWordRemovals();
            CancelPendingResultReturn();
            ClearRecognizedText();
            validationProcessed = false;
            SetReadingButtonsInteractable(true);

            if (storySelectionPanel != null) storySelectionPanel.SetActive(true);
            if (readingPanel != null) readingPanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);

            RefreshStoryCards();
            RefreshSelectionProgress();
        }

        public void OpenStoryById(string storyId)
        {
            CuentoData selected = cuentosDisponibles.Find(item => item != null && item.id == storyId);
            if (selected == null)
            {
                SetStatus($"No se encontro el cuento {storyId}.");
                return;
            }

            MostrarLectura(selected);
        }

        public void OpenOtherWorlds()
        {
            if (!StoryProgressRepository.HasUnlockedOtherWorlds(cuentosDisponibles, requiredCompletedStoriesToUnlock))
            {
                RefreshSelectionProgress();
                return;
            }

            SceneNavigation.LoadScene(otherWorldsSceneName, this);
        }

        private void MostrarLectura(CuentoData cuento)
        {
            StopListeningInternal(false);
            CancelPendingResultReturn();
            validationProcessed = false;
            SetReadingButtonsInteractable(true);
            currentStory = cuento;
            ApplyStoryData(cuento);
            ClearRecognizedText();
            ResetStoryScrollToTop();

            if (storySelectionPanel != null) storySelectionPanel.SetActive(false);
            if (readingPanel != null) readingPanel.SetActive(true);
            if (resultPanel != null) resultPanel.SetActive(false);

            SetStatus("Presiona Iniciar para comenzar.");
        }

        private void MostrarResultado(int score, int stars)
        {
            if (storySelectionPanel != null) storySelectionPanel.SetActive(false);
            if (readingPanel != null) readingPanel.SetActive(true);
            if (resultPanel != null) resultPanel.SetActive(true);

            string title = currentStory != null ? currentStory.titulo : "Cuento";
            SetText(resultStoryTitleText, title);
            SetText(resultScoreText, $"Puntaje: {score} puntos");
            SetText(resultStarsText, BuildStarsText(stars));
            SetText(resultMessageText, $"{GetStarsResultMessage(stars)}\n\nRegresando a los cuentos...");

            CancelPendingResultReturn();
            if (isActiveAndEnabled)
            {
                resultReturnRoutine = StartCoroutine(ReturnToSelectionAfterResultRoutine());
            }
        }

        private IEnumerator ReturnToSelectionAfterResultRoutine()
        {
            yield return new WaitForSecondsRealtime(resultReturnDelay);
            resultReturnRoutine = null;
            MostrarSeleccionCuentos();
        }

        private IEnumerator StartListeningAfterCountdownRoutine()
        {
            SetStartButtonInteractable(false);
            SetStatus("Prepara tu voz.");

            float remaining = Mathf.Max(0f, startCountdownSeconds);
            int lastShownSecond = -1;

            while (remaining > 0f)
            {
                int seconds = Mathf.Max(1, Mathf.CeilToInt(remaining));
                if (seconds != lastShownSecond)
                {
                    SetCountdownText($"Prepara tu voz\nEmpieza a leer cuando termine el contador\n{seconds}");
                    lastShownSecond = seconds;
                }

                remaining -= Time.deltaTime;
                yield return null;
            }

            SetCountdownText(string.Empty);
            SetStatus("Probando entrada del microfono...");

            bool microphoneHasSignal = false;
            yield return CheckSelectedMicrophoneSignal(hasSignal => microphoneHasSignal = hasSignal);

            if (!microphoneHasSignal)
            {
                string message = "No detecte sonido del microfono seleccionado. Revisa el microfono en opciones o intenta nuevamente.";
                SetStartButtonInteractable(true);
                countdownRoutine = null;
                SetText(errorText, message);
                SetStatus(message);
                yield break;
            }

            microphoneSignalDetectedBeforeListening = true;
            SetStatus("Activando reconocimiento de voz...");
            yield return new WaitForSeconds(0.2f);

            isListeningSession = true;
            listeningStartedAt = Time.time;
            lastSpeechAt = Time.time;
            speechService.StartListening();

            if (!speechService.IsListening)
            {
                isListeningSession = false;
                SetStartButtonInteractable(true);
                SetStatus("MICROFONO DETENIDO");
            }
            else
            {
                SetStatus("MICROFONO ACTIVO");
            }

            countdownRoutine = null;
        }

        private string GetRecognitionBlockReason()
        {
#if !(UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
            return "El reconocimiento de voz solo esta disponible en Windows para esta version del prototipo.";
#else
            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                return "No se detecto un microfono conectado.";
            }

            string selectedDevice = MicrophoneSettings.GetAvailableSelectedDevice();
            if (string.IsNullOrWhiteSpace(selectedDevice))
            {
                return "No se pudo preparar el microfono seleccionado.";
            }

            return string.Empty;
#endif
        }

        private void SubscribeSpeechService()
        {
            speechService.OnPartialResult += HandlePartialResult;
            speechService.OnFinalResult += HandleFinalResult;
            speechService.OnError += HandleSpeechError;
            speechService.OnStatusChanged += HandleSpeechStatusChanged;
        }

        private void DisposeSpeechService()
        {
            if (speechService == null) return;

            speechService.OnPartialResult -= HandlePartialResult;
            speechService.OnFinalResult -= HandleFinalResult;
            speechService.OnError -= HandleSpeechError;
            speechService.OnStatusChanged -= HandleSpeechStatusChanged;
            speechService.DisposeService();
            speechService = null;
        }

        private void HandlePartialResult(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || validationProcessed) return;

            partialRecognizedCandidate = text;
            lastSpeechAt = Time.time;
            SetText(partialRecognizedText, text);
            RefreshRecognizedTextUi();
            ScheduleScrollToEnd();
            SetStatus("Escuchando...");
        }

        private void HandleFinalResult(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || validationProcessed) return;

            partialRecognizedCandidate = string.Empty;
            AppendFinalRecognizedFragment(text);

            lastSpeechAt = Time.time;
            SetText(partialRecognizedText, string.Empty);
            RefreshRecognizedTextUi();
            PreviewReading();
        }

        private void HandleSpeechError(string message)
        {
            isListeningSession = false;
            SetStartButtonInteractable(true);
            SetCountdownText(string.Empty);
            SetText(errorText, message);
            SetStatus(message);
        }

        private void HandleSpeechStatusChanged(string status)
        {
            if (!isShuttingDown)
            {
                SetStatus(status);
            }
        }

        private void CheckListeningTimeouts()
        {
            if (!isListeningSession || speechService == null || !speechService.IsListening) return;

            if (Time.time - listeningStartedAt >= maxListeningTime)
            {
                PromotePartialCandidate();
                StopListeningInternal(false);
                SetStatus("Tiempo finalizado. Puedes validar tu lectura o reintentar.");
                return;
            }

            if (Time.time - lastSpeechAt < silenceTimeout) return;

            PromotePartialCandidate();
            StopListeningInternal(false);
            if (HandleNoTextAfterDetectedSignal()) return;

            if (readingEvaluator.CountWords(finalRecognizedText) < minWordsRequired)
            {
                SetStatus("No escuche suficiente. Intenta leer nuevamente.");
                UpdateValidationUi(0f, "No escuche suficiente. Intenta leer nuevamente.");
            }
            else
            {
                SetStatus("Se detuvo la escucha por silencio. Puedes validar tu lectura.");
            }
        }

        private bool HandleNoTextAfterDetectedSignal()
        {
            if (!microphoneSignalDetectedBeforeListening) return false;
            if (readingEvaluator.CountWords(finalRecognizedText) >= minWordsRequired) return false;

            string message = "El microfono recibe sonido, pero Windows no devolvio texto. Revisa el idioma y los permisos de voz en Windows.";
            SetStatus(message);
            SetText(errorText, message);
            UpdateValidationUi(0f, message);

            return true;
        }

        private IEnumerator CheckSelectedMicrophoneSignal(Action<bool> onComplete)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            string selectedDevice = MicrophoneSettings.GetAvailableSelectedDevice();
            if (string.IsNullOrWhiteSpace(selectedDevice))
            {
                onComplete?.Invoke(false);
                yield break;
            }

            const int sampleRate = 44100;
            const int clipSeconds = 2;
            AudioClip clip = null;

            try
            {
                clip = Microphone.Start(selectedDevice, true, clipSeconds, sampleRate);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"VoiceRecognitionTest: no se pudo iniciar prueba de microfono '{selectedDevice}'. {exception}");
                onComplete?.Invoke(false);
                yield break;
            }

            float startupLimit = Time.realtimeSinceStartup + 2f;
            while (Microphone.GetPosition(selectedDevice) <= 0 && Time.realtimeSinceStartup < startupLimit)
            {
                yield return null;
            }

            if (Microphone.GetPosition(selectedDevice) <= 0 || clip == null)
            {
                Microphone.End(selectedDevice);
                Debug.LogWarning($"VoiceRecognitionTest: el microfono '{selectedDevice}' no entrego muestras en la prueba previa.");
                onComplete?.Invoke(false);
                yield break;
            }

            float strongestSignal = 0f;
            float finishTime = Time.realtimeSinceStartup + microphonePreflightSeconds;
            float[] samples = new float[1024];

            while (Time.realtimeSinceStartup < finishTime && Microphone.IsRecording(selectedDevice))
            {
                int position = Microphone.GetPosition(selectedDevice);
                if (position >= samples.Length)
                {
                    clip.GetData(samples, position - samples.Length);
                    strongestSignal = Mathf.Max(strongestSignal, CalculateRms(samples));
                }

                yield return null;
            }

            Microphone.End(selectedDevice);
            bool hasSignal = strongestSignal >= minimumMicrophoneSignal;
            Debug.Log($"VoiceRecognitionTest: prueba previa microfono '{selectedDevice}', nivel maximo {strongestSignal:0.0000}, valido: {hasSignal}.");
            onComplete?.Invoke(hasSignal);
#else
            onComplete?.Invoke(false);
            yield break;
#endif
        }

        private static float CalculateRms(float[] samples)
        {
            if (samples == null || samples.Length == 0) return 0f;

            float sum = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                sum += samples[i] * samples[i];
            }

            return Mathf.Sqrt(sum / samples.Length);
        }

        private void PromotePartialCandidate()
        {
            if (string.IsNullOrWhiteSpace(partialRecognizedCandidate)) return;

            AppendFinalRecognizedFragment(partialRecognizedCandidate);
            partialRecognizedCandidate = string.Empty;
            SetText(partialRecognizedText, string.Empty);
            RefreshRecognizedTextUi();
        }

        private void StopListeningInternal(bool updateStatus)
        {
            if (countdownRoutine != null)
            {
                StopCoroutine(countdownRoutine);
                countdownRoutine = null;
            }

            isListeningSession = false;
            SetCountdownText(string.Empty);

            if (speechService != null)
            {
                speechService.StopListening();
            }

            if (!validationProcessed)
            {
                SetStartButtonInteractable(true);
            }

            if (updateStatus && !isShuttingDown)
            {
                SetStatus("MICROFONO DETENIDO");
            }
        }

        private string GetTextForEvaluation()
        {
            return finalRecognizedText;
        }

        private void ApplyInitialText()
        {
            if (currentStory != null) ApplyStoryData(currentStory);
            else if (storyText != null) storyText.text = story;
            else Debug.LogWarning("VoiceRecognitionTest: falta asignar Story Text.");

            SetText(recognizedText, string.Empty);
            SetText(partialRecognizedText, string.Empty);
            SetText(finalRecognizedTextDisplay, string.Empty);
            SetText(errorText, string.Empty);
            UpdateValidationUi(0f, string.Empty);
            UpdateStarsUi(0);
            SetCountdownText(string.Empty);
            ResetRecognizedScrollToTop();
            ResetStoryScrollToTop();

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            SetStatus("Elige un cuento para comenzar.");
#else
            SetStatus("El reconocimiento de voz solo esta disponible en Windows para esta version del prototipo.");
#endif
        }

        private void EnsureDefaultStories()
        {
            if (cuentosDisponibles.Count == 0)
            {
                cuentosDisponibles.Add(new CuentoData
                {
                    id = StoryProgressRepository.DefaultStoryId,
                    titulo = "Los tres cerditos",
                    textoCompleto = story
                });
                cuentosDisponibles.Add(new CuentoData
                {
                    id = "cuento_02",
                    titulo = "Cuento 02",
                    textoCompleto = "Texto pendiente de asignar desde el Inspector."
                });
            }

            for (int i = 0; i < cuentosDisponibles.Count; i++)
            {
                CuentoData cuento = cuentosDisponibles[i];
                if (cuento == null) continue;
                if (string.IsNullOrWhiteSpace(cuento.id)) cuento.id = i == 0 ? StoryProgressRepository.DefaultStoryId : $"cuento_{i + 1:00}";
                if (string.IsNullOrWhiteSpace(cuento.titulo)) cuento.titulo = i == 0 ? "Los tres cerditos" : $"Cuento {i + 1:00}";
                if (string.IsNullOrWhiteSpace(cuento.textoCompleto)) cuento.textoCompleto = "Texto pendiente de asignar desde el Inspector.";
            }
        }

        private void ApplyStoryData(CuentoData cuento)
        {
            if (cuento == null) return;

            story = cuento.textoCompleto;
            expectedStoryWords = ReadingEvaluator.GetNormalizedWords(story);
            SetText(selectedStoryTitleText, cuento.titulo);
            SetText(storyText, cuento.textoCompleto);

            if (selectedStoryIconImage != null)
            {
                selectedStoryIconImage.sprite = cuento.icono;
                selectedStoryIconImage.enabled = cuento.icono != null;
            }
        }

        private void WireOptionalButtons()
        {
            WireButtonIfEmpty(stopButton, StopListening);
            WireButtonIfEmpty(retryButton, RetryReading);
            WireButtonIfEmpty(clearButton, ClearRecognizedText);
            WireButtonIfEmpty(validateButton, ValidateReading);
            WireButtonIfEmpty(backToSelectionButton, MostrarSeleccionCuentos);
            WireButtonIfEmpty(backToPreviousMenuButton, ReturnToMenu);
            WireButtonIfEmpty(otherWorldsButton, OpenOtherWorlds);

            foreach (CuentoCardView card in cuentoCards)
            {
                if (card?.button == null || string.IsNullOrWhiteSpace(card.cuentoId)) continue;
                if (card.button.onClick.GetPersistentEventCount() > 0) continue;

                string capturedId = card.cuentoId;
                card.button.onClick.AddListener(() => OpenStoryById(capturedId));
            }
        }

        private void WarnMissingSceneReferences()
        {
            if (storySelectionPanel == null) Debug.LogWarning("VoiceRecognitionTest: falta PanelSeleccionCuentos.");
            if (readingPanel == null) Debug.LogWarning("VoiceRecognitionTest: falta PanelLecturaCuento.");
            if (resultPanel == null) Debug.LogWarning("VoiceRecognitionTest: falta PanelResultadoCuento.");
            if (recognizedTextScrollRect == null) Debug.LogWarning("VoiceRecognitionTest: falta asignar el Scroll View de la lectura reconocida.");
            if (storyBodyScrollRect == null) Debug.LogWarning("VoiceRecognitionTest: falta asignar el Scroll View del cuerpo del cuento.");
        }

        private static void WireButtonIfEmpty(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || button.onClick.GetPersistentEventCount() > 0) return;
            button.onClick.AddListener(action);
        }

        private void PreviewReading()
        {
            ReadingEvaluationResult result = readingEvaluator.Evaluate(
                story,
                finalRecognizedText,
                threeStarsThreshold,
                twoStarsThreshold,
                oneStarThreshold);

            string message = result.Similarity >= successThreshold
                ? "Muy bien, estas leyendo correctamente."
                : "Intentalo otra vez, lee con calma.";

            UpdateValidationUi(result.Similarity, message);
            UpdateStarsUi(result.Stars);
        }

        private void RefreshRecognizedTextUi()
        {
            string combinedText = BuildDisplayedWordsText(partialRecognizedCandidate);

            SetText(recognizedText, combinedText);
            SetRecognizedPlaceholderVisible(string.IsNullOrWhiteSpace(combinedText));
            SetText(finalRecognizedTextDisplay, finalRecognizedText);
        }

        private void SetRecognizedPlaceholderVisible(bool visible)
        {
            if (recognizedPlaceholderText == null) return;

            SetText(recognizedPlaceholderText, "Aquí aparecerán las palabras que leas.");
            recognizedPlaceholderText.gameObject.SetActive(visible);
        }

        private void AppendFinalRecognizedFragment(string fragment)
        {
            if (string.IsNullOrWhiteSpace(fragment)) return;

            finalRecognizedText = string.IsNullOrWhiteSpace(finalRecognizedText)
                ? fragment.Trim()
                : $"{finalRecognizedText}\n{fragment.Trim()}";

            string[] originalWords = fragment.Split(
                new[] { ' ', '\n', '\r', '\t' },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (string originalWord in originalWords)
            {
                string normalizedWord = ReadingEvaluator.NormalizeText(originalWord);
                if (string.IsNullOrWhiteSpace(normalizedWord)) continue;

                bool isCorrect = MatchExpectedWord(normalizedWord);
                PalabraReconocida word = new()
                {
                    id = ++nextRecognizedWordId,
                    textoOriginal = originalWord.Trim(),
                    textoNormalizado = normalizedWord,
                    esCorrecta = isCorrect,
                    tiempoCreacion = Time.unscaledTime
                };

                palabrasMostradas.Add(word);
                if (!isCorrect)
                {
                    eliminacionesPendientes[word.id] = StartCoroutine(RemoveIncorrectWordAfterDelay(word.id));
                }
            }

            RefreshRecognizedTextUi();
            ScheduleScrollToEnd();
        }

        private bool MatchExpectedWord(string normalizedWord)
        {
            if (expectedStoryWords == null || expectedStoryWords.Length == 0) return false;
            if (nextExpectedWordIndex >= expectedStoryWords.Length) return false;

            if (expectedStoryWords[nextExpectedWordIndex] == normalizedWord)
            {
                nextExpectedWordIndex++;
                return true;
            }

            int lookAheadLimit = Mathf.Min(expectedStoryWords.Length, nextExpectedWordIndex + 4);
            for (int index = nextExpectedWordIndex + 1; index < lookAheadLimit; index++)
            {
                if (expectedStoryWords[index] != normalizedWord) continue;

                nextExpectedWordIndex = index + 1;
                return true;
            }

            nextExpectedWordIndex++;
            return false;
        }

        private IEnumerator RemoveIncorrectWordAfterDelay(int wordId)
        {
            yield return new WaitForSecondsRealtime(incorrectWordLifetime);

            int index = palabrasMostradas.FindIndex(word => word.id == wordId);
            if (index >= 0)
            {
                palabrasMostradas.RemoveAt(index);
                RefreshRecognizedTextUi();
            }

            eliminacionesPendientes.Remove(wordId);
        }

        private string BuildDisplayedWordsText(string partialText)
        {
            StringBuilder builder = new();
            string incorrectColor = ColorUtility.ToHtmlStringRGB(incorrectWordColor);

            foreach (PalabraReconocida word in palabrasMostradas)
            {
                if (builder.Length > 0) builder.Append(' ');

                string safeText = EscapeRichText(word.textoOriginal);
                if (word.esCorrecta)
                {
                    builder.Append(safeText);
                }
                else
                {
                    builder.Append("<color=#").Append(incorrectColor).Append("><u>")
                        .Append(safeText).Append("</u></color>");
                }
            }

            if (!string.IsNullOrWhiteSpace(partialText))
            {
                if (builder.Length > 0) builder.Append(' ');
                builder.Append("<color=#666666>").Append(EscapeRichText(partialText.Trim())).Append("</color>");
            }

            return builder.ToString();
        }

        private static string EscapeRichText(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private void CancelPendingWordRemovals()
        {
            foreach (Coroutine routine in eliminacionesPendientes.Values)
            {
                if (routine != null) StopCoroutine(routine);
            }

            eliminacionesPendientes.Clear();

            if (scrollRoutine != null)
            {
                StopCoroutine(scrollRoutine);
                scrollRoutine = null;
            }
        }

        private void CancelPendingResultReturn()
        {
            if (resultReturnRoutine == null) return;
            StopCoroutine(resultReturnRoutine);
            resultReturnRoutine = null;
        }

        private void ScheduleScrollToEnd()
        {
            if (recognizedTextScrollRect == null || !isActiveAndEnabled) return;
            if (scrollRoutine != null) StopCoroutine(scrollRoutine);
            scrollRoutine = StartCoroutine(ScrollToEndAfterLayout());
        }

        private IEnumerator ScrollToEndAfterLayout()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            recognizedTextScrollRect.verticalNormalizedPosition = 0f;
            scrollRoutine = null;
        }

        private void ResetRecognizedScrollToTop()
        {
            if (recognizedTextScrollRect == null) return;
            recognizedTextScrollRect.StopMovement();
            recognizedTextScrollRect.verticalNormalizedPosition = 1f;

            if (!isActiveAndEnabled) return;
            if (scrollRoutine != null) StopCoroutine(scrollRoutine);
            scrollRoutine = StartCoroutine(ResetScrollAfterLayout(recognizedTextScrollRect, true));
        }

        private void ResetStoryScrollToTop()
        {
            if (storyBodyScrollRect == null) return;
            storyBodyScrollRect.StopMovement();
            storyBodyScrollRect.verticalNormalizedPosition = 1f;

            if (!isActiveAndEnabled) return;
            if (storyScrollRoutine != null) StopCoroutine(storyScrollRoutine);
            storyScrollRoutine = StartCoroutine(ResetScrollAfterLayout(storyBodyScrollRect, false));
        }

        private IEnumerator ResetScrollAfterLayout(ScrollRect target, bool recognized)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (target != null) target.verticalNormalizedPosition = 1f;

            if (recognized) scrollRoutine = null;
            else storyScrollRoutine = null;
        }

        private void UpdateValidationUi(float similarity, string result)
        {
            SetText(readingResultText, result);
        }

        private void UpdateStarsUi(int stars)
        {
            currentStars = Mathf.Clamp(stars, 0, 3);
            SetText(starsResultText, string.Empty);
            if (starDisplay != null) starDisplay.SetImmediate(currentStars);

            if (starImages == null || starImages.Length == 0) return;

            for (int i = 0; i < starImages.Length; i++)
            {
                Image starImage = starImages[i];
                if (starImage == null) continue;

                bool isFull = i < currentStars;
                Sprite targetSprite = isFull ? fullStarSprite : emptyStarSprite;

                if (targetSprite != null)
                {
                    starImage.sprite = targetSprite;
                    starImage.color = Color.white;
                }
                else
                {
                    starImage.color = isFull
                        ? new Color(1f, 0.86f, 0.25f, 1f)
                        : new Color(1f, 1f, 1f, 0.25f);
                }
            }
        }

        private void RefreshStoryCards()
        {
            foreach (CuentoCardView card in cuentoCards)
            {
                if (card == null || string.IsNullOrWhiteSpace(card.cuentoId)) continue;
                CuentoData cuento = cuentosDisponibles.Find(item => item != null && item.id == card.cuentoId);
                if (cuento == null) continue;

                SetText(card.titleText, cuento.titulo);
                if (card.iconImage != null)
                {
                    card.iconImage.sprite = cuento.icono;
                    card.iconImage.enabled = cuento.icono != null;
                }

                bool completed = StoryProgressRepository.IsStoryCompleted(cuento.id);
                int bestStars = StoryProgressRepository.GetBestStoryStars(cuento.id);
                int bestScore = StoryProgressRepository.GetBestStoryScore(cuento.id);
                SetText(card.completedText, completed ? "Completado" : "Pendiente");
                SetText(card.starsText, completed ? $"{BuildStarsText(bestStars)}  {bestScore} puntos" : "Sin estrellas");
            }
        }

        private void RefreshSelectionProgress()
        {
            int completed = StoryProgressRepository.CountCompletedStories(cuentosDisponibles);
            int required = Mathf.Max(1, requiredCompletedStoriesToUnlock);
            bool unlocked = completed >= required;

            SetText(selectionProgressText, $"Cuentos completados: {Mathf.Min(completed, required)}/{required}");
            SetText(otherWorldsUnlockText, unlocked
                ? "Ya puedes visitar los otros mundos."
                : $"Completa {required} cuentos para visitar los otros mundos.");

            if (otherWorldsButton != null)
            {
                otherWorldsButton.interactable = unlocked;
                otherWorldsButton.gameObject.SetActive(unlocked);
            }

            if (backToPreviousMenuButton != null)
            {
                backToPreviousMenuButton.gameObject.SetActive(!unlocked);
            }
        }

        private void SetReadingButtonsInteractable(bool value)
        {
            if (startButton != null) startButton.interactable = value;
            if (stopButton != null) stopButton.interactable = value;
            if (retryButton != null) retryButton.interactable = value;
            if (clearButton != null) clearButton.interactable = value;
            if (validateButton != null) validateButton.interactable = value;
            if (backToSelectionButton != null) backToSelectionButton.interactable = value;
        }

        private void SetStartButtonInteractable(bool isInteractable)
        {
            if (startButton != null && !validationProcessed)
            {
                startButton.interactable = isInteractable;
            }
        }

        private string GetStarsResultMessage(int stars)
        {
            return stars switch
            {
                3 => "Excelente lectura. Obtuviste 3 estrellas.",
                2 => "Muy bien. Obtuviste 2 estrellas.",
                1 => "Buen intento. Obtuviste 1 estrella.",
                _ => "Intentalo otra vez, lee con calma."
            };
        }

        private static string BuildStarsText(int stars)
        {
            int clamped = Mathf.Clamp(stars, 0, 3);
            return new string('★', clamped) + new string('☆', 3 - clamped);
        }

        private void SetStatus(string message)
        {
            SetText(statusText, message);
        }

        private void SetCountdownText(string message)
        {
            SetText(countdownText, message);
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
