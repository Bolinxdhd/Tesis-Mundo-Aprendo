using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ionic.Zip;
using UnityEngine;
using Vosk;

namespace Bolin
{
    public enum VoiceRecognitionState
    {
        NotInitialized,
        Initializing,
        Ready,
        Listening,
        Error,
        Disposed
    }

    public enum VoiceRecognitionErrorKind
    {
        ModelMissing,
        InvalidModelArchive,
        NativeLibraryMissing,
        MicrophoneMissing,
        NotInitialized,
        RecognizerUnavailable,
        Unknown
    }

    [DisallowMultipleComponent]
    public sealed class VoskVoiceRecognitionManager : MonoBehaviour
    {
        private const string LogPrefix = "[MundoCuentos/Vosk]";
        private const string VoiceLogPrefix = "[MundoCuentos/Voice]";
        private const string MicrophoneLogPrefix = "[MundoCuentos/Mic]";
        private const float AudioLevelLogIntervalSeconds = 0.5f;

        [Header("Vosk")]
        [SerializeField] private string modelPath = "vosk-model-small-es-0.42.zip";
        [SerializeField, Range(1, 5)] private int maxAlternatives = 3;
        [SerializeField] private bool initializeOnStart = true;

        [Header("Diagnóstico temporal")]
        [Tooltip("Activa logs de PartialResult, Result, FinalResult y estado de Vosk.")]
        [SerializeField] private bool debugSpeechRecognition = true;
        [Tooltip("Incluye JSON RAW y enumera todas las alternativas reconocidas.")]
        [SerializeField] private bool debugTranscribeEverything = true;
        [Tooltip("Muestra recepción de audio y RMS como máximo dos veces por segundo.")]
        [SerializeField] private bool debugAudioLevel = true;
        [Tooltip("Si está desactivado, Vosk transcribe libremente; la validación por concepto permanece activa.")]
        [SerializeField] private bool useRestrictedGrammar = false;

        [Header("Captura")]
        [SerializeField] private VoiceProcessor voiceProcessor;
        [SerializeField, Min(8000)] private int sampleRate = 16000;
        [SerializeField, Min(128)] private int frameLength = 512;

        private readonly ConcurrentQueue<short[]> audioQueue = new();
        private readonly ConcurrentQueue<RecognitionMessage> recognitionQueue = new();
        private readonly ConcurrentQueue<Exception> workerErrorQueue = new();
        private readonly List<string> keyPhrases = new();

        private Model model;
        private VoskRecognizer recognizer;
        private CancellationTokenSource workerCancellation;
        private Task workerTask;
        private Coroutine initializationRoutine;
        private bool recognizerNeedsRebuild = true;
        private bool recognizerRestrictedGrammarSetting;
        private bool callbacksSubscribed;
        private bool disposed;
        private bool hasLoggedAudioReceived;
        private float nextAudioLevelLogAt;
        private string lastPartialResult = string.Empty;
        private volatile bool listening;

        private enum RecognitionMessageKind
        {
            Partial,
            Result,
            Final,
            SessionEnded
        }

        private readonly struct RecognitionMessage
        {
            public RecognitionMessage(RecognitionMessageKind kind, string payload = null)
            {
                Kind = kind;
                Payload = payload ?? string.Empty;
            }

            public RecognitionMessageKind Kind { get; }
            public string Payload { get; }
        }

        public event Action OnReady;
        public event Action<IReadOnlyList<string>> OnRecognitionResult;
        public event Action<VoiceRecognitionState, string> OnStatusChanged;
        public event Action<VoiceRecognitionErrorKind, string> OnError;

        public VoiceRecognitionState State { get; private set; } = VoiceRecognitionState.NotInitialized;
        public bool IsReady => model != null
            && !disposed
            && State != VoiceRecognitionState.Initializing
            && State != VoiceRecognitionState.Disposed;
        public bool IsListening => listening;
        public string ModelPath => modelPath;
        private bool DiagnosticsEnabled => debugSpeechRecognition || debugAudioLevel;

        private void Awake()
        {
            if (voiceProcessor == null) voiceProcessor = GetComponent<VoiceProcessor>();
            SubscribeVoiceProcessor();
        }

        private void Start()
        {
            if (initializeOnStart) Initialize();
        }

        private void Update()
        {
            while (workerErrorQueue.TryDequeue(out Exception exception))
            {
                HandleWorkerException(exception);
            }

            while (recognitionQueue.TryDequeue(out RecognitionMessage message))
            {
                switch (message.Kind)
                {
                    case RecognitionMessageKind.Partial:
                        DispatchPartialResult(message.Payload);
                        break;
                    case RecognitionMessageKind.Result:
                        DispatchRecognitionResult(message.Payload);
                        break;
                    case RecognitionMessageKind.Final:
                        DispatchFinalResult(message.Payload);
                        break;
                    case RecognitionMessageKind.SessionEnded:
                        LogSessionEnded();
                        break;
                }
            }
        }

        public void Initialize()
        {
            if (disposed || State == VoiceRecognitionState.Disposed) return;
            if (State == VoiceRecognitionState.Initializing || IsReady) return;

            if (voiceProcessor == null)
            {
                ReportError(VoiceRecognitionErrorKind.RecognizerUnavailable,
                    "No se encontró el procesador de audio requerido por Vosk.");
                return;
            }

            if (initializationRoutine != null) StopCoroutine(initializationRoutine);
            initializationRoutine = StartCoroutine(InitializeRoutine());
        }

        public void ConfigureVocabulary(IEnumerable<string> phrases)
        {
            if (listening)
            {
                Debug.LogWarning($"{LogPrefix} El vocabulario no se cambia durante una escucha activa.", this);
                return;
            }

            keyPhrases.Clear();
            HashSet<string> unique = new(StringComparer.OrdinalIgnoreCase);
            if (phrases != null)
            {
                foreach (string phrase in phrases)
                {
                    AddGrammarPhrase(unique, phrase);
                    string normalized = SpeechAnswerValidator.NormalizeText(phrase);
                    if (string.IsNullOrEmpty(normalized)) continue;
                    AddGrammarPhrase(unique, normalized);
                    AddGrammarPhrase(unique, $"una {normalized}");
                    AddGrammarPhrase(unique, $"un {normalized}");
                    AddGrammarPhrase(unique, $"veo {normalized}");
                    AddGrammarPhrase(unique, $"yo veo una {normalized}");
                    AddGrammarPhrase(unique, $"yo veo un {normalized}");
                }
            }

            keyPhrases.AddRange(unique);
            recognizerNeedsRebuild = true;
            Debug.Log($"{LogPrefix} Vocabulario del cuento configurado con {keyPhrases.Count} frases.", this);
        }

        public bool StartListening()
        {
            if (disposed) return false;
            if (!IsReady)
            {
                ReportError(VoiceRecognitionErrorKind.NotInitialized, "Vosk todavía no está inicializado.");
                return false;
            }

            if (listening) return true;
            if (workerTask != null && !workerTask.IsCompleted)
            {
                ReportError(VoiceRecognitionErrorKind.RecognizerUnavailable,
                    "La sesión anterior de reconocimiento todavía se está cerrando.");
                return false;
            }

            string[] devices = Microphone.devices;
            if (devices == null || devices.Length == 0)
            {
                ReportError(VoiceRecognitionErrorKind.MicrophoneMissing, "No se encontró un micrófono.");
                return false;
            }

            try
            {
                SelectConfiguredMicrophone();
                EnsureRecognizer();
                ClearQueues();
                recognizer.Reset();
                lastPartialResult = string.Empty;
                hasLoggedAudioReceived = false;
                nextAudioLevelLogAt = Time.unscaledTime;

                workerCancellation?.Dispose();
                workerCancellation = new CancellationTokenSource();
                listening = true;
                State = VoiceRecognitionState.Listening;
                if (DiagnosticsEnabled)
                {
                    Debug.Log($"{VoiceLogPrefix} ===== INICIO DE ESCUCHA =====", this);
                }

                voiceProcessor.StartRecording(sampleRate, frameLength, false);
                VoskRecognizer sessionRecognizer = recognizer;
                workerTask = Task.Run(
                    () => ProcessAudio(sessionRecognizer, workerCancellation.Token),
                    workerCancellation.Token);

                LogMicrophoneConfiguration(devices);
                LogVoskSessionState(true);
                RaiseStatus("Te escucho…");
                return true;
            }
            catch (DllNotFoundException exception)
            {
                listening = false;
                ReportError(VoiceRecognitionErrorKind.NativeLibraryMissing,
                    $"No se pudo cargar la DLL nativa de Vosk: {exception.Message}");
            }
            catch (Exception exception)
            {
                listening = false;
                ReportError(VoiceRecognitionErrorKind.RecognizerUnavailable,
                    $"No se pudo iniciar el reconocimiento: {exception.Message}");
            }

            return false;
        }

        public void StopListening()
        {
            if (!listening && (voiceProcessor == null || !voiceProcessor.IsRecording)) return;

            listening = false;
            workerCancellation?.Cancel();
            try
            {
                if (voiceProcessor != null && voiceProcessor.IsRecording) voiceProcessor.StopRecording();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"{VoiceLogPrefix} Error al cerrar el micrófono: {exception.Message}", this);
            }

            while (audioQueue.TryDequeue(out _)) { }
            if (!disposed && State != VoiceRecognitionState.Error)
            {
                State = model != null ? VoiceRecognitionState.Ready : VoiceRecognitionState.NotInitialized;
                RaiseStatus(State == VoiceRecognitionState.Ready ? "Reconocimiento preparado." : "Vosk no inicializado.");
            }

            if (debugSpeechRecognition)
            {
                Debug.Log($"{LogPrefix} Escuchando: NO", this);
            }
        }

        public bool RetryMicrophone()
        {
            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                ReportError(VoiceRecognitionErrorKind.MicrophoneMissing, "No se encontró un micrófono.");
                return false;
            }

            return StartListening();
        }

        private IEnumerator InitializeRoutine()
        {
            State = VoiceRecognitionState.Initializing;
            RaiseStatus("Preparando reconocimiento offline…");

            string archivePath = Path.Combine(Application.streamingAssetsPath, modelPath);
            string modelDirectoryName = Path.GetFileNameWithoutExtension(modelPath);
            string persistentRoot = Application.persistentDataPath;
            string extractedPath = Path.Combine(persistentRoot, modelDirectoryName);
            Debug.Log($"{LogPrefix} Modelo configurado: {modelPath}", this);
            if (debugSpeechRecognition)
            {
                Debug.Log($"{LogPrefix} Modelo: {modelPath}", this);
                Debug.Log($"{LogPrefix} Ruta: {archivePath}", this);
                Debug.Log($"{LogPrefix} Modelo cargado: NO", this);
                Debug.Log($"{LogPrefix} Recognizer creado: NO", this);
            }

            bool extractedModelIsValid = IsModelDirectoryValid(extractedPath);
            if (!extractedModelIsValid && !File.Exists(archivePath))
            {
                initializationRoutine = null;
                ReportError(VoiceRecognitionErrorKind.ModelMissing,
                    $"Modelo no encontrado en StreamingAssets: {archivePath}");
                yield break;
            }

            if (File.Exists(archivePath))
            {
                Debug.Log($"{LogPrefix} Modelo encontrado: {archivePath}", this);
            }

            Task<string> prepareTask = Task.Run(() => PrepareModelDirectory(archivePath, persistentRoot, extractedPath));
            while (!prepareTask.IsCompleted) yield return null;

            if (prepareTask.IsFaulted)
            {
                Exception exception = prepareTask.Exception?.GetBaseException();
                initializationRoutine = null;
                ReportError(VoiceRecognitionErrorKind.InvalidModelArchive,
                    $"No se pudo preparar el modelo Vosk: {exception?.Message ?? "ZIP inválido"}");
                yield break;
            }

            string preparedPath = prepareTask.Result;
            if (debugSpeechRecognition)
            {
                Debug.Log($"{LogPrefix} Ruta resuelta: {preparedPath}", this);
            }

            Task<Model> loadTask = Task.Run(() =>
            {
                global::Vosk.Vosk.SetLogLevel(-1);
                return new Model(preparedPath);
            });

            while (!loadTask.IsCompleted) yield return null;

            if (loadTask.IsFaulted)
            {
                Exception exception = loadTask.Exception?.GetBaseException();
                initializationRoutine = null;
                VoiceRecognitionErrorKind kind = exception is DllNotFoundException
                    ? VoiceRecognitionErrorKind.NativeLibraryMissing
                    : VoiceRecognitionErrorKind.InvalidModelArchive;
                ReportError(kind, $"Vosk no se pudo inicializar: {exception?.Message ?? "error desconocido"}");
                yield break;
            }

            model = loadTask.Result;
            recognizerNeedsRebuild = true;
            State = VoiceRecognitionState.Ready;
            initializationRoutine = null;
            Debug.Log($"{LogPrefix} Vosk inicializado con el modelo español pequeño.", this);
            if (debugSpeechRecognition)
            {
                Debug.Log($"{LogPrefix} Modelo cargado: SI", this);
                Debug.Log($"{LogPrefix} Grammar restringido: {(useRestrictedGrammar ? "SI" : "NO")}", this);
                Debug.Log($"{LogPrefix} MaxAlternatives: {maxAlternatives}", this);
                Debug.Log($"{LogPrefix} Escuchando: NO", this);
            }

            RaiseStatus("Reconocimiento preparado.");
            OnReady?.Invoke();
        }

        private static string PrepareModelDirectory(string archivePath, string persistentRoot, string extractedPath)
        {
            if (IsModelDirectoryValid(extractedPath)) return extractedPath;
            if (!File.Exists(archivePath)) throw new FileNotFoundException("No existe el modelo configurado.", archivePath);

            using FileStream stream = File.OpenRead(archivePath);
            using ZipFile archive = ZipFile.Read(stream);
            archive.ExtractAll(persistentRoot, ExtractExistingFileAction.OverwriteSilently);

            if (!IsModelDirectoryValid(extractedPath))
            {
                throw new InvalidDataException("El ZIP no contiene una estructura válida de modelo Vosk.");
            }

            return extractedPath;
        }

        private static bool IsModelDirectoryValid(string path)
        {
            return Directory.Exists(path)
                && File.Exists(Path.Combine(path, "am", "final.mdl"))
                && File.Exists(Path.Combine(path, "conf", "model.conf"));
        }

        private void EnsureRecognizer()
        {
            if (model == null) throw new InvalidOperationException("El modelo Vosk no está cargado.");
            bool grammarSettingChanged = recognizer != null
                && recognizerRestrictedGrammarSetting != useRestrictedGrammar;
            if (recognizer != null && !recognizerNeedsRebuild && !grammarSettingChanged) return;

            recognizer?.Dispose();
            string grammar = useRestrictedGrammar ? BuildGrammarJson() : string.Empty;
            recognizer = string.IsNullOrEmpty(grammar)
                ? new VoskRecognizer(model, sampleRate)
                : new VoskRecognizer(model, sampleRate, grammar);
            recognizer.SetMaxAlternatives(maxAlternatives);
            recognizerNeedsRebuild = false;
            recognizerRestrictedGrammarSetting = useRestrictedGrammar;
            Debug.Log($"{LogPrefix} Reconocedor preparado (MaxAlternatives={maxAlternatives}).", this);
            if (debugSpeechRecognition)
            {
                Debug.Log($"{LogPrefix} Recognizer creado: SI", this);
                Debug.Log($"{LogPrefix} Grammar restringido: {(!string.IsNullOrEmpty(grammar) ? "SI" : "NO")}", this);
                Debug.Log($"{LogPrefix} MaxAlternatives: {maxAlternatives}", this);
            }
        }

        private void ProcessAudio(VoskRecognizer sessionRecognizer, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (!audioQueue.TryDequeue(out short[] samples))
                    {
                        token.WaitHandle.WaitOne(20);
                        continue;
                    }

                    if (sessionRecognizer == null) continue;
                    if (sessionRecognizer.AcceptWaveform(samples, samples.Length))
                    {
                        recognitionQueue.Enqueue(new RecognitionMessage(
                            RecognitionMessageKind.Result,
                            sessionRecognizer.Result()));
                        lastPartialResult = string.Empty;
                        continue;
                    }

                    if (!debugSpeechRecognition) continue;

                    string partialJson = sessionRecognizer.PartialResult();
                    if (!TryGetPrimaryText(partialJson, out string partialText)) continue;
                    if (string.IsNullOrWhiteSpace(partialText)
                        || string.Equals(partialText, lastPartialResult, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    lastPartialResult = partialText;
                    recognitionQueue.Enqueue(new RecognitionMessage(
                        RecognitionMessageKind.Partial,
                        partialText));
                }
            }
            catch (OperationCanceledException)
            {
                // Cierre normal de la sesión.
            }
            catch (ObjectDisposedException)
            {
                // El objeto puede cerrarse durante una descarga de escena.
            }
            catch (Exception exception)
            {
                workerErrorQueue.Enqueue(exception);
            }
            finally
            {
                try
                {
                    if (sessionRecognizer != null)
                    {
                        recognitionQueue.Enqueue(new RecognitionMessage(
                            RecognitionMessageKind.Final,
                            sessionRecognizer.FinalResult()));
                    }
                }
                catch (ObjectDisposedException)
                {
                    // La descarga de escena puede liberar Vosk antes de completar el diagnóstico.
                }
                catch (Exception exception)
                {
                    workerErrorQueue.Enqueue(exception);
                }
                finally
                {
                    recognitionQueue.Enqueue(new RecognitionMessage(RecognitionMessageKind.SessionEnded));
                }
            }
        }

        private void DispatchRecognitionResult(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;

            try
            {
                List<string> alternatives = ParseAlternatives(json);
                if (debugSpeechRecognition)
                {
                    if (debugTranscribeEverything)
                    {
                        Debug.Log($"[MundoCuentos/Vosk/RESULT RAW] {json}", this);
                    }

                    string resultText = alternatives.Count > 0 ? alternatives[0] : "<vacío>";
                    Debug.Log($"[MundoCuentos/Vosk/RESULT] {resultText}", this);
                    if (debugTranscribeEverything)
                    {
                        for (int i = 0; i < alternatives.Count; i++)
                        {
                            Debug.Log($"[MundoCuentos/Vosk/ALT {i + 1}] {alternatives[i]}", this);
                        }
                    }
                }

                if (alternatives.Count == 0) return;
                if (listening) OnRecognitionResult?.Invoke(alternatives);
            }
            catch (Exception exception)
            {
                ReportError(VoiceRecognitionErrorKind.RecognizerUnavailable,
                    $"No se pudo interpretar el resultado de Vosk: {exception.Message}");
            }
        }

        private void DispatchPartialResult(string partialText)
        {
            if (!debugSpeechRecognition || string.IsNullOrWhiteSpace(partialText)) return;
            Debug.Log($"[MundoCuentos/Vosk/PARTIAL] {partialText}", this);
        }

        private void DispatchFinalResult(string json)
        {
            if (!debugSpeechRecognition) return;

            try
            {
                if (debugTranscribeEverything)
                {
                    Debug.Log($"[MundoCuentos/Vosk/FINAL RAW] {json}", this);
                }

                List<string> alternatives = ParseAlternatives(json);
                string finalText = alternatives.Count > 0 ? alternatives[0] : "<vacío>";
                Debug.Log($"[MundoCuentos/Vosk/FINAL] {finalText}", this);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"{LogPrefix} No se pudo interpretar FinalResult: {exception.Message}", this);
            }
        }

        private void LogSessionEnded()
        {
            if (!DiagnosticsEnabled) return;
            Debug.Log($"{VoiceLogPrefix} ===== FIN DE ESCUCHA =====", this);
        }

        private static List<string> ParseAlternatives(string json)
        {
            RecognitionResult parsed = new(json);
            List<string> alternatives = new();
            if (parsed.Phrases == null) return alternatives;

            foreach (RecognizedPhrase phrase in parsed.Phrases)
            {
                if (phrase == null || string.IsNullOrWhiteSpace(phrase.Text)) continue;
                alternatives.Add(phrase.Text.Trim());
            }

            return alternatives;
        }

        private static bool TryGetPrimaryText(string json, out string text)
        {
            text = string.Empty;
            if (string.IsNullOrWhiteSpace(json)) return false;

            try
            {
                List<string> alternatives = ParseAlternatives(json);
                if (alternatives.Count == 0) return false;
                text = alternatives[0];
                return true;
            }
            catch (Exception)
            {
                // Un JSON parcial incompleto no debe detener la sesión ni afectar al gameplay.
                return false;
            }
        }

        private void HandleWorkerException(Exception exception)
        {
            StopListening();
            ReportError(VoiceRecognitionErrorKind.RecognizerUnavailable,
                $"El reconocedor Vosk se detuvo: {exception.Message}");
        }

        private void SelectConfiguredMicrophone()
        {
            voiceProcessor.UpdateDevices();
            string configuredDevice = MicrophoneSettings.GetAvailableSelectedDevice();
            int deviceIndex = 0;
            if (!string.IsNullOrWhiteSpace(configuredDevice))
            {
                for (int i = 0; i < voiceProcessor.Devices.Count; i++)
                {
                    if (!string.Equals(voiceProcessor.Devices[i], configuredDevice, StringComparison.Ordinal)) continue;
                    deviceIndex = i;
                    break;
                }
            }

            voiceProcessor.ChangeDevice(deviceIndex);
        }

        private void LogMicrophoneConfiguration(IReadOnlyList<string> devices)
        {
            if (!DiagnosticsEnabled) return;

            int deviceCount = devices?.Count ?? 0;
            Debug.Log($"{MicrophoneLogPrefix} Dispositivos detectados: {deviceCount}", this);
            for (int i = 0; i < deviceCount; i++)
            {
                Debug.Log($"{MicrophoneLogPrefix} Device {i}: {devices[i]}", this);
            }

            Debug.Log($"{MicrophoneLogPrefix} Micrófono seleccionado: {voiceProcessor.CurrentDeviceName}", this);
            Debug.Log($"{MicrophoneLogPrefix} Sample rate solicitado: {sampleRate} Hz", this);
            string actualRate = voiceProcessor.ActualSampleRate > 0
                ? $"{voiceProcessor.ActualSampleRate} Hz"
                : "no disponible";
            string channels = voiceProcessor.Channels > 0
                ? voiceProcessor.Channels.ToString(CultureInfo.InvariantCulture)
                : "no disponible";
            Debug.Log($"{MicrophoneLogPrefix} Sample rate real: {actualRate}", this);
            Debug.Log($"{MicrophoneLogPrefix} Canales: {channels}", this);
            Debug.Log(
                $"{MicrophoneLogPrefix} VoiceProcessor: {(voiceProcessor.IsRecording ? "GRABANDO" : "SIN CAPTURA")}; "
                + $"FrameLength: {voiceProcessor.FrameLength}",
                this);
        }

        private void LogVoskSessionState(bool isListening)
        {
            if (!debugSpeechRecognition) return;

            Debug.Log($"{LogPrefix} Modelo cargado: {(model != null ? "SI" : "NO")}", this);
            Debug.Log($"{LogPrefix} Recognizer creado: {(recognizer != null ? "SI" : "NO")}", this);
            Debug.Log($"{LogPrefix} Grammar restringido: {(useRestrictedGrammar ? "SI" : "NO")}", this);
            Debug.Log($"{LogPrefix} MaxAlternatives: {maxAlternatives}", this);
            Debug.Log($"{LogPrefix} Escuchando: {(isListening ? "SI" : "NO")}", this);
        }

        private void SubscribeVoiceProcessor()
        {
            if (callbacksSubscribed || voiceProcessor == null) return;
            voiceProcessor.OnFrameCaptured += HandleFrameCaptured;
            callbacksSubscribed = true;
        }

        private void UnsubscribeVoiceProcessor()
        {
            if (!callbacksSubscribed || voiceProcessor == null) return;
            voiceProcessor.OnFrameCaptured -= HandleFrameCaptured;
            callbacksSubscribed = false;
        }

        private void HandleFrameCaptured(short[] samples)
        {
            if (!listening || samples == null || samples.Length == 0) return;

            audioQueue.Enqueue(samples);
            if (!DiagnosticsEnabled) return;

            if (!hasLoggedAudioReceived)
            {
                hasLoggedAudioReceived = true;
                Debug.Log($"{VoiceLogPrefix} Audio recibido.", this);
            }

            if (!debugAudioLevel || Time.unscaledTime < nextAudioLevelLogAt) return;
            nextAudioLevelLogAt = Time.unscaledTime + AudioLevelLogIntervalSeconds;

            double squareSum = 0d;
            for (int i = 0; i < samples.Length; i++)
            {
                double normalizedSample = samples[i] / 32768d;
                squareSum += normalizedSample * normalizedSample;
            }

            double rms = Math.Sqrt(squareSum / samples.Length);
            Debug.Log($"{MicrophoneLogPrefix} Audio RMS: {rms.ToString("0.000", CultureInfo.InvariantCulture)}", this);
        }

        private string BuildGrammarJson()
        {
            if (keyPhrases.Count == 0) return string.Empty;

            StringBuilder builder = new("[");
            for (int i = 0; i < keyPhrases.Count; i++)
            {
                if (i > 0) builder.Append(',');
                builder.Append('"').Append(EscapeJson(keyPhrases[i])).Append('"');
            }

            builder.Append(",\"[unk]\"]");
            return builder.ToString();
        }

        private static string EscapeJson(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static void AddGrammarPhrase(ISet<string> target, string phrase)
        {
            if (string.IsNullOrWhiteSpace(phrase)) return;
            string cleaned = CollapseWhitespace(phrase.Trim().ToLowerInvariant());
            if (!string.IsNullOrEmpty(cleaned)) target.Add(cleaned);
        }

        private static string CollapseWhitespace(string value)
        {
            StringBuilder builder = new(value.Length);
            bool previousWasWhitespace = false;
            foreach (char character in value)
            {
                if (char.IsWhiteSpace(character))
                {
                    if (!previousWasWhitespace) builder.Append(' ');
                    previousWasWhitespace = true;
                }
                else
                {
                    builder.Append(character);
                    previousWasWhitespace = false;
                }
            }

            return builder.ToString().Trim();
        }

        private void ClearQueues()
        {
            while (audioQueue.TryDequeue(out _)) { }
            while (recognitionQueue.TryDequeue(out _)) { }
            while (workerErrorQueue.TryDequeue(out _)) { }
        }

        private void RaiseStatus(string message)
        {
            OnStatusChanged?.Invoke(State, message);
        }

        private void ReportError(VoiceRecognitionErrorKind kind, string message)
        {
            if (!disposed) State = VoiceRecognitionState.Error;
            Debug.LogError($"{LogPrefix} {message}", this);
            OnStatusChanged?.Invoke(State, message);
            OnError?.Invoke(kind, message);
        }

        private void OnDisable()
        {
            StopListening();
        }

        private void OnDestroy()
        {
            disposed = true;
            StopListening();
            UnsubscribeVoiceProcessor();
            if (initializationRoutine != null) StopCoroutine(initializationRoutine);

            workerCancellation?.Cancel();
            try
            {
                workerTask?.Wait(500);
            }
            catch (Exception)
            {
                // El cierre continúa para liberar los handles nativos.
            }

            workerCancellation?.Dispose();
            recognizer?.Dispose();
            model?.Dispose();
            recognizer = null;
            model = null;
            State = VoiceRecognitionState.Disposed;
            Debug.Log($"{LogPrefix} Recursos liberados.", this);
        }
    }
}
