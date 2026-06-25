using System;
using UnityEngine;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using UnityEngine.Windows.Speech;
#endif

namespace Bolin
{
    public class WindowsDictationSpeechService : ISpeechToTextService
    {
        public event Action<string> OnPartialResult;
        public event Action<string> OnFinalResult;
        public event Action<string> OnError;
        public event Action<string> OnStatusChanged;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private DictationRecognizer dictationRecognizer;
        private bool stopRequested;
        private bool disposed;
#endif

        public bool IsListening { get; private set; }

        public void StartListening()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (disposed)
            {
                OnError?.Invoke("El reconocimiento de voz no esta listo. Intenta nuevamente.");
                Debug.LogWarning("WindowsDictationSpeechService: se intento iniciar despues de DisposeService.");
                return;
            }

            if (IsListening)
            {
                OnStatusChanged?.Invoke("El microfono ya esta escuchando.");
                return;
            }

            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                OnError?.Invoke("No se detecto un microfono conectado.");
                Debug.LogWarning("WindowsDictationSpeechService: no hay microfonos disponibles.");
                return;
            }

            string selectedDevice = MicrophoneSettings.GetAvailableSelectedDevice();
            if (string.IsNullOrWhiteSpace(selectedDevice))
            {
                OnError?.Invoke("No se pudo preparar el microfono seleccionado.");
                Debug.LogWarning("WindowsDictationSpeechService: no hay microfono seleccionado disponible.");
                return;
            }

            try
            {
                EnsureRecognizer();

                if (dictationRecognizer.Status != SpeechSystemStatus.Stopped)
                {
                    SafeStopRecognizer();
                    OnError?.Invoke("El microfono aun se esta preparando. Intenta otra vez en un momento.");
                    Debug.LogWarning($"WindowsDictationSpeechService: DictationRecognizer no estaba detenido. Estado: {dictationRecognizer.Status}.");
                    return;
                }

                stopRequested = false;
                Debug.LogWarning("WindowsDictationSpeechService: DictationRecognizer usa el microfono predeterminado de Windows. Unity no permite pasarle directamente el dispositivo elegido en Microphone.devices.");
                dictationRecognizer.Start();
                IsListening = true;
                OnStatusChanged?.Invoke("Microfono activo. Lee el cuento en voz alta.");
                Debug.Log($"WindowsDictationSpeechService: microfono seleccionado en opciones: {selectedDevice}.");
            }
            catch (Exception exception)
            {
                IsListening = false;
                OnError?.Invoke("Hubo un problema al activar el microfono. Usa el modo de apoyo o intenta nuevamente.");
                Debug.LogWarning($"WindowsDictationSpeechService: no se pudo iniciar DictationRecognizer. {exception}");
            }
#else
            IsListening = false;
            OnError?.Invoke("El reconocimiento de voz solo está disponible en Windows para esta versión del prototipo.");
#endif
        }

        public void StopListening()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            stopRequested = true;
            SafeStopRecognizer();
            IsListening = false;
            OnStatusChanged?.Invoke("Reconocimiento detenido.");
#else
            IsListening = false;
            OnStatusChanged?.Invoke("Reconocimiento detenido.");
#endif
        }

        public void DisposeService()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (disposed) return;

            stopRequested = true;
            SafeStopRecognizer();

            if (dictationRecognizer != null)
            {
                dictationRecognizer.DictationHypothesis -= HandleDictationHypothesis;
                dictationRecognizer.DictationResult -= HandleDictationResult;
                dictationRecognizer.DictationComplete -= HandleDictationComplete;
                dictationRecognizer.DictationError -= HandleDictationError;

                try
                {
                    dictationRecognizer.Dispose();
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"WindowsDictationSpeechService: error al liberar DictationRecognizer. {exception}");
                }

                dictationRecognizer = null;
            }

            IsListening = false;
            disposed = true;
#else
            IsListening = false;
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private void EnsureRecognizer()
        {
            if (dictationRecognizer != null) return;

            dictationRecognizer = new DictationRecognizer(ConfidenceLevel.Low, DictationTopicConstraint.Dictation)
            {
                InitialSilenceTimeoutSeconds = 15f,
                AutoSilenceTimeoutSeconds = 12f
            };
            dictationRecognizer.DictationHypothesis += HandleDictationHypothesis;
            dictationRecognizer.DictationResult += HandleDictationResult;
            dictationRecognizer.DictationComplete += HandleDictationComplete;
            dictationRecognizer.DictationError += HandleDictationError;
        }

        private void HandleDictationHypothesis(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            Debug.Log($"WindowsDictationSpeechService: parcial '{text}'.");
            OnPartialResult?.Invoke(text);
        }

        private void HandleDictationResult(string text, ConfidenceLevel confidence)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            Debug.Log($"WindowsDictationSpeechService: final '{text}', confianza {confidence}.");
            OnFinalResult?.Invoke(text);
        }

        private void HandleDictationComplete(DictationCompletionCause cause)
        {
            IsListening = false;

            if (stopRequested || cause == DictationCompletionCause.Complete)
            {
                OnStatusChanged?.Invoke("Reconocimiento detenido.");
                return;
            }

            string friendlyMessage = GetFriendlyCompletionMessage(cause);
            OnError?.Invoke(friendlyMessage);
            Debug.LogWarning($"WindowsDictationSpeechService: DictationRecognizer finalizo con causa {cause}.");
        }

        private void HandleDictationError(string error, int hresult)
        {
            IsListening = false;
            OnError?.Invoke("Hubo un problema con el reconocimiento de voz. Puedes reintentar o usar el modo de apoyo.");
            Debug.LogWarning($"WindowsDictationSpeechService: DictationError '{error}'. HResult: {hresult}.");
        }

        private void SafeStopRecognizer()
        {
            if (dictationRecognizer == null) return;

            try
            {
                if (dictationRecognizer.Status != SpeechSystemStatus.Stopped)
                {
                    dictationRecognizer.Stop();
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"WindowsDictationSpeechService: no se pudo detener DictationRecognizer. {exception}");
            }
        }

        private static string GetFriendlyCompletionMessage(DictationCompletionCause cause)
        {
            return cause.ToString() switch
            {
                "TimeoutExceeded" => "No se detecto voz durante un tiempo. Intenta leer nuevamente.",
                "PauseLimitExceeded" => "Hubo demasiado silencio. Intenta leer nuevamente.",
                "NetworkFailure" => "La conexion no esta disponible. Puedes usar el modo de apoyo.",
                "MicrophoneUnavailable" => "No se pudo usar el microfono. Revisa que este conectado.",
                "AudioQualityFailure" => "No se escucho con claridad. Intenta acercarte al microfono.",
                "Canceled" => "El reconocimiento se detuvo. Puedes intentarlo nuevamente.",
                _ => "Hubo un problema con el reconocimiento de voz. Puedes reintentar o usar el modo de apoyo."
            };
        }
#endif
    }
}
