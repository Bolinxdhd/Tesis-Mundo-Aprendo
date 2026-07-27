using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolin
{
    // Guarda una palabra reconocida para pintarla en pantalla y retirarla si fue incorrecta.
    [Serializable]
    public class PalabraReconocida
    {
        public int id;
        public string textoOriginal;
        public string textoNormalizado;
        public bool esCorrecta;
        public float tiempoCreacion;
    }

    // Datos editables de un cuento: id estable, textos e icono.
    [Serializable]
    public class CuentoData
    {
        public string id;
        public string titulo;

        [TextArea(5, 30)]
        public string textoCompleto;

        public Sprite icono;
        [Min(0)] public int orden;
        public bool disponible = true;
        [Tooltip("Clave de progreso existente asociada al cuento. Se deja visible para mantener los datos auditables desde el Inspector.")]
        public string claveProgreso;
    }

    // Une una tarjeta visual de seleccion con el id del cuento que debe abrir.
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

    // Controla el mundo de cuentos: selecciona cuento, escucha voz, valida lectura y guarda progreso.
    public class VoiceRecognitionTest : MonoBehaviour
    {
        public event Action<bool> OnReadingValidated;

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
        [SerializeField] private Button resultRepeatButton;
        [SerializeField] private Button resultNextStoryButton;
        [SerializeField] private Button resultBackToStoriesButton;
        [SerializeField] private Button resultBackToWorldsButton;

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

        [Header("Tutorial y animaciones")]
        [SerializeField] private StoryTutorialAnimationController tutorialController;
        [SerializeField] private StoryLevelAnimationController levelAnimationController;
        [SerializeField] private StoryResultStarAnimation resultStarAnimation;

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
        private readonly HashSet<string> selectedStoryVocabulary = new(StringComparer.Ordinal);
        private readonly List<string> finalRecognizedNormalizedWords = new();
        private readonly List<PalabraReconocida> palabrasMostradas = new();
        private readonly Dictionary<int, Coroutine> eliminacionesPendientes = new();
        private int nextRecognizedWordId;
        private CuentoData currentStory;
        private float listeningStartedAt;
        private float lastSpeechAt;
        private int currentStars;
        private bool isListeningSession;
        private bool isShuttingDown;
        private bool microphoneSignalDetectedBeforeListening;
        private bool validationProcessed;
        private bool IsTutorialActive => tutorialController != null && tutorialController.IsActive;

        private readonly struct FilteredStoryWord
        {
            public readonly string original;
            public readonly string normalized;

            public FilteredStoryWord(string original, string normalized)
            {
                this.original = original;
                this.normalized = normalized;
            }
        }

        private void Awake()
        {
            // Crea servicios de lectura/voz y prepara la UI inicial del mundo.
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

        private void Start()
        {
            tutorialController?.ShowIfNeeded();
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
            // Vigila silencios/tiempo maximo y mantiene visible el indicador de microfono.
            CheckListeningTimeouts();
            if (microphoneListeningIndicator != null)
            {
                microphoneListeningIndicator.SetActive(isListeningSession && speechService != null && speechService.IsListening);
            }
        }

        // Boton Iniciar: valida microfono, hace cuenta regresiva y activa reconocimiento.
        public void StartListening()
        {
            if (BlockActionDuringTutorial()) return;

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

        // Boton Detener: corta el reconocimiento y deja la lectura lista para validar.
        public void StopListening()
        {
            StopListeningInternal(true);
        }

        // Boton Repetir: limpia lo reconocido y vuelve a leer el mismo cuento.
        public void RetryReading()
        {
            if (BlockActionDuringTutorial()) return;

            RestartReading();
            StartListening();
        }

        // Boton Reiniciar: conserva el cuento, pero detiene la voz y deja una lectura limpia.
        public void RestartReading()
        {
            if (BlockActionDuringTutorial()) return;

            StopListeningInternal(false);
            ClearRecognizedText();
            validationProcessed = false;
            SetReadingButtonsInteractable(true);
            SetStatus("Lectura reiniciada. Presiona Iniciar cuando estes listo.");
        }

        // Boton Repetir de resultado: vuelve al mismo cuento sin reactivar el microfono.
        public void RepeatCurrentStory()
        {
            if (BlockActionDuringTutorial()) return;
            if (currentStory == null)
            {
                MostrarSeleccionCuentos();
                return;
            }

            MostrarLectura(currentStory);
        }

        // Boton Siguiente cuento: respeta el orden configurado en los datos de cada cuento.
        public void OpenNextStory()
        {
            if (BlockActionDuringTutorial()) return;

            CuentoData next = GetNextAvailableStory();
            if (next == null)
            {
                MostrarSeleccionCuentos();
                return;
            }

            MostrarLectura(next);
        }

        public void ReturnToWorldsFromResult()
        {
            ReturnToMenu();
        }

        // Boton Limpiar: borra texto reconocido, parciales, finales y palabras temporales.
        public void ClearRecognizedText()
        {
            finalRecognizedText = string.Empty;
            partialRecognizedCandidate = string.Empty;
            nextRecognizedWordId = 0;
            CancelPendingWordRemovals();
            finalRecognizedNormalizedWords.Clear();
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

        // Boton Validar: compara lectura esperada vs reconocida y guarda el resultado.
        public void ValidateReading()
        {
            if (BlockActionDuringTutorial()) return;

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
                OnReadingValidated?.Invoke(false);
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
            OnReadingValidated?.Invoke(result.Stars > 0);
        }

        // Boton Regresar: carga la escena configurada como retorno.
        public void ReturnToMenu()
        {
            if (BlockActionDuringTutorial()) return;

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

        // Vuelve al panel de seleccion de cuentos sin salir de la escena.
        public void MostrarSeleccionCuentos()
        {
            StopListeningInternal(false);
            CancelPendingWordRemovals();
            CancelPendingResultReturn();
            resultStarAnimation?.StopAndReset();
            ClearRecognizedText();
            validationProcessed = false;
            SetReadingButtonsInteractable(true);

            if (storySelectionPanel != null) storySelectionPanel.SetActive(true);
            if (readingPanel != null) readingPanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);

            RefreshStoryCards();
            RefreshSelectionProgress();
            levelAnimationController?.PlaySelectionEntrance();
        }

        // Abre un cuento usando el id enviado desde la tarjeta o boton del Inspector.
        public void OpenStoryById(string storyId)
        {
            if (BlockActionDuringTutorial()) return;

            CuentoData selected = cuentosDisponibles.Find(item => item != null && item.id == storyId);
            if (selected == null)
            {
                SetStatus($"No se encontro el cuento {storyId}.");
                return;
            }

            if (!selected.disponible)
            {
                SetStatus("Este cuento aun no esta disponible.");
                return;
            }

            MostrarLectura(selected);
        }

        // Intenta abrir otros mundos; requiere cuentos completados suficientes.
        public void OpenOtherWorlds()
        {
            if (BlockActionDuringTutorial()) return;

            if (!StoryProgressRepository.HasUnlockedOtherWorlds(cuentosDisponibles, requiredCompletedStoriesToUnlock))
            {
                RefreshSelectionProgress();
                return;
            }

            SceneNavigation.LoadScene(otherWorldsSceneName, this);
        }

        private void MostrarLectura(CuentoData cuento)
        {
            // Cambia de seleccion a lectura y aplica texto/icono del cuento elegido.
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
            levelAnimationController?.PlayReadingEntrance();
        }

        private void MostrarResultado(int score, int stars)
        {
            // Activa el panel final y programa el retorno automatico a seleccion.
            if (storySelectionPanel != null) storySelectionPanel.SetActive(false);
            if (readingPanel != null) readingPanel.SetActive(true);
            if (resultPanel != null) resultPanel.SetActive(true);

            string title = currentStory != null ? currentStory.titulo : "Cuento";
            SetText(resultStoryTitleText, title);
            SetText(resultScoreText, $"Puntaje: {score} puntos");
            SetText(resultStarsText, BuildStarsText(stars));
            string headline = stars > 0 ? "Muy bien!" : "Sigue intentando.";
            SetText(resultMessageText, $"{headline}\n{GetStarsResultMessage(stars)}\nElige que quieres hacer ahora.");
            UpdateStarsUi(stars, true);
            levelAnimationController?.PlayResultEntrance();
            resultStarAnimation?.Play(stars);

            if (resultNextStoryButton != null)
            {
                CuentoData nextStory = GetNextAvailableStory();
                SetButtonLabel(resultNextStoryButton, nextStory != null ? "SIGUIENTE CUENTO" : "VOLVER A CUENTOS");
            }

            CancelPendingResultReturn();
            if (!HasResultNavigationButtons() && isActiveAndEnabled)
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

        private bool HasResultNavigationButtons()
        {
            return resultRepeatButton != null
                || resultNextStoryButton != null
                || resultBackToStoriesButton != null
                || resultBackToWorldsButton != null;
        }

        private CuentoData GetNextAvailableStory()
        {
            List<CuentoData> availableStories = GetAvailableStories();
            if (availableStories.Count == 0) return null;

            int currentIndex = currentStory == null ? -1 : availableStories.IndexOf(currentStory);
            if (currentIndex >= 0 && currentIndex < availableStories.Count - 1)
            {
                return availableStories[currentIndex + 1];
            }

            return null;
        }

        private List<CuentoData> GetAvailableStories()
        {
            List<CuentoData> availableStories = new();
            foreach (CuentoData cuento in cuentosDisponibles)
            {
                if (cuento != null && cuento.disponible) availableStories.Add(cuento);
            }

            availableStories.Sort((left, right) =>
            {
                int order = left.orden.CompareTo(right.orden);
                return order != 0 ? order : string.CompareOrdinal(left.id, right.id);
            });
            return availableStories;
        }

        private static void SetButtonLabel(Button button, string value)
        {
            if (button == null) return;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = value;
        }

        private IEnumerator StartListeningAfterCountdownRoutine()
        {
            // Cuenta atras, prueba senal del microfono y arranca el servicio de voz.
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
                levelAnimationController?.PlayMicrophoneActivation();
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

        private bool BlockActionDuringTutorial()
        {
            if (!IsTutorialActive) return false;

            StopListeningInternal(false);
            SetStatus("Termina el tutorial de Biblio para comenzar.");
            return true;
        }

        private void SubscribeSpeechService()
        {
            // Conecta eventos del servicio de voz con este controlador de escena.
            speechService.OnPartialResult += HandlePartialResult;
            speechService.OnFinalResult += HandleFinalResult;
            speechService.OnError += HandleSpeechError;
            speechService.OnStatusChanged += HandleSpeechStatusChanged;
        }

        private void DisposeSpeechService()
        {
            // Desconecta eventos y libera el recognizer para evitar llamadas despues de cerrar.
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
            // Muestra una hipotesis temporal filtrada sin guardarla todavia como lectura final.
            if (string.IsNullOrWhiteSpace(text) || validationProcessed) return;

            partialRecognizedCandidate = BuildFilteredPartialCandidate(text);
            lastSpeechAt = Time.time;
            SetText(partialRecognizedText, partialRecognizedCandidate);
            RefreshRecognizedTextUi();
            ScheduleScrollToEnd();
            SetStatus("Escuchando...");
        }

        private void HandleFinalResult(string text)
        {
            // Agrega texto final al resultado acumulado y refresca la vista.
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
            // Recibe errores del servicio y desbloquea el boton de inicio.
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
            // Si hay silencio o se agota el tiempo, detiene la escucha sin perder texto.
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
            // Mide volumen real con Microphone antes de pedir dictado a Windows.
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
            // Convierte la ultima hipotesis parcial en texto final antes de validar.
            if (string.IsNullOrWhiteSpace(partialRecognizedCandidate)) return;

            AppendFinalRecognizedFragment(partialRecognizedCandidate);
            partialRecognizedCandidate = string.Empty;
            SetText(partialRecognizedText, string.Empty);
            RefreshRecognizedTextUi();
        }

        private void StopListeningInternal(bool updateStatus)
        {
            // Detiene countdown, servicio de voz e indicador sin cambiar de panel.
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
            // Carga el cuento inicial y deja limpios los textos de resultado.
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
            // Asegura cuentos basicos para que la escena no quede vacia si falta configuracion.
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
            // Copia datos del cuento seleccionado a los textos e iconos de lectura.
            if (cuento == null) return;

            story = cuento.textoCompleto;
            expectedStoryWords = ReadingEvaluator.GetNormalizedWords(story);
            selectedStoryVocabulary.Clear();
            foreach (string word in expectedStoryWords)
            {
                if (!string.IsNullOrWhiteSpace(word)) selectedStoryVocabulary.Add(word);
            }
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
            // Conecta botones por codigo solo si no tienen eventos persistentes del Inspector.
            WireButtonIfEmpty(startButton, StartListening);
            WireButtonIfEmpty(stopButton, StopListening);
            WireButtonIfEmpty(retryButton, RetryReading);
            WireButtonIfEmpty(clearButton, ClearRecognizedText);
            WireButtonIfEmpty(validateButton, ValidateReading);
            WireButtonIfEmpty(backToSelectionButton, MostrarSeleccionCuentos);
            WireButtonIfEmpty(backToPreviousMenuButton, ReturnToMenu);
            WireButtonIfEmpty(otherWorldsButton, OpenOtherWorlds);
            WireButtonIfEmpty(resultRepeatButton, RepeatCurrentStory);
            WireButtonIfEmpty(resultNextStoryButton, OpenNextStory);
            WireButtonIfEmpty(resultBackToStoriesButton, MostrarSeleccionCuentos);
            WireButtonIfEmpty(resultBackToWorldsButton, ReturnToWorldsFromResult);

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
            // Muestra una evaluacion provisional mientras llega texto reconocido.
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
            // Reconstruye el texto visible combinando palabras finales y parcial actual.
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
            // Conserva unicamente palabras del vocabulario del cuento actual.
            // Windows puede reenviar frases acumuladas, por eso se agrega solo la parte nueva.
            if (string.IsNullOrWhiteSpace(fragment)) return;

            List<FilteredStoryWord> allowedWords = GetAllowedWords(fragment);
            int firstNewWordIndex = GetFirstNewWordIndex(allowedWords);
            for (int index = firstNewWordIndex; index < allowedWords.Count; index++)
            {
                FilteredStoryWord allowedWord = allowedWords[index];
                PalabraReconocida word = new()
                {
                    id = ++nextRecognizedWordId,
                    textoOriginal = allowedWord.original,
                    textoNormalizado = allowedWord.normalized,
                    esCorrecta = true,
                    tiempoCreacion = Time.unscaledTime
                };

                palabrasMostradas.Add(word);
                finalRecognizedNormalizedWords.Add(allowedWord.normalized);
            }

            RebuildFinalRecognizedText();
            RefreshRecognizedTextUi();
            ScheduleScrollToEnd();
        }

        private string BuildFilteredPartialCandidate(string fragment)
        {
            List<FilteredStoryWord> allowedWords = GetAllowedWords(fragment);
            int firstNewWordIndex = GetFirstNewWordIndex(allowedWords);
            StringBuilder builder = new();
            for (int index = firstNewWordIndex; index < allowedWords.Count; index++)
            {
                if (builder.Length > 0) builder.Append(' ');
                builder.Append(allowedWords[index].original);
            }
            return builder.ToString();
        }

        private List<FilteredStoryWord> GetAllowedWords(string fragment)
        {
            List<FilteredStoryWord> allowedWords = new();
            if (string.IsNullOrWhiteSpace(fragment) || selectedStoryVocabulary.Count == 0) return allowedWords;

            string[] rawWords = fragment.Split(
                new[] { ' ', '\n', '\r', '\t' },
                StringSplitOptions.RemoveEmptyEntries);
            foreach (string rawWord in rawWords)
            {
                string[] normalizedWords = ReadingEvaluator.GetNormalizedWords(rawWord);
                foreach (string normalizedWord in normalizedWords)
                {
                    if (!selectedStoryVocabulary.Contains(normalizedWord)) continue;
                    string displayWord = normalizedWords.Length == 1 ? rawWord.Trim() : normalizedWord;
                    allowedWords.Add(new FilteredStoryWord(displayWord, normalizedWord));
                }
            }
            return allowedWords;
        }

        private int GetFirstNewWordIndex(List<FilteredStoryWord> incomingWords)
        {
            int maximumOverlap = Mathf.Min(finalRecognizedNormalizedWords.Count, incomingWords.Count);
            for (int overlap = maximumOverlap; overlap > 0; overlap--)
            {
                bool matches = true;
                int finalStart = finalRecognizedNormalizedWords.Count - overlap;
                for (int index = 0; index < overlap; index++)
                {
                    if (finalRecognizedNormalizedWords[finalStart + index] == incomingWords[index].normalized) continue;
                    matches = false;
                    break;
                }

                if (matches) return overlap;
            }

            return 0;
        }

        private void RebuildFinalRecognizedText()
        {
            StringBuilder builder = new();
            foreach (PalabraReconocida word in palabrasMostradas)
            {
                if (word == null || string.IsNullOrWhiteSpace(word.textoOriginal)) continue;
                if (builder.Length > 0) builder.Append(' ');
                builder.Append(word.textoOriginal);
            }
            finalRecognizedText = builder.ToString();
        }

        private IEnumerator RemoveIncorrectWordAfterDelay(int wordId)
        {
            // Retira automaticamente palabras incorrectas para no saturar el panel.
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

        private void UpdateStarsUi(int stars, bool animate = false)
        {
            currentStars = Mathf.Clamp(stars, 0, 3);
            SetText(starsResultText, string.Empty);
            if (starDisplay != null)
            {
                if (animate) starDisplay.ShowStars(currentStars, true);
                else starDisplay.SetImmediate(currentStars);
            }

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
            // Actualiza tarjetas con completado, mejor puntaje y estrellas por cuento.
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

                if (card.button != null) card.button.interactable = cuento.disponible;
                if (!cuento.disponible)
                {
                    SetText(card.completedText, "Muy pronto");
                    SetText(card.starsText, string.Empty);
                    continue;
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
            // Actualiza desbloqueo de otros mundos segun cuentos completados.
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
