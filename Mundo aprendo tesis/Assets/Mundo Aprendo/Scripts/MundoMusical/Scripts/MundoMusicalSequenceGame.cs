using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Assets.SurpriseBox.Scripts
{
    // Controla el mundo musical: reproduce secuencias, valida las teclas del alumno y guarda estrellas.
    public class MundoMusicalSequenceGame : MonoBehaviour
    {
        private const int WorldIndex = 0;

        public event Action<bool> OnAnswerValidated;
        public event Action<int> OnActivityCompleted;

        [Serializable]
        // Datos de cada tecla: nombre visual, color y sonido propio o generado por frecuencia.
        public class PianoKeyConfig
        {
            public string id = "Do";
            public string displayName = "1";
            public Color normalColor = new(0.95f, 0.95f, 0.9f, 1f);
            public Color highlightColor = new(1f, 0.78f, 0.25f, 1f);
            public float toneFrequency = 261.63f;
            public AudioClip audioClip;
        }

        [Serializable]
        // Paso individual de una secuencia; apunta a una tecla por indice.
        public class PianoStep
        {
            [Min(0)] public int keyIndex;
        }

        [Serializable]
        // Lista ordenada de notas que el jugador debe escuchar y repetir.
        public class PianoSequence
        {
            public string sequenceName = "Secuencia";
            [Min(0.1f)] public float noteDelay = 0.45f;
            public List<PianoStep> steps = new();
        }

        [Header("Configuracion")]
        [SerializeField] private bool playSequenceOnStart;
        [SerializeField] private bool repeatOnMistake = true;
        [SerializeField, Min(0.05f)] private float highlightDuration = 0.25f;
        [SerializeField, Min(0.05f)] private float mistakeReplayDelay = 0.65f;
        [SerializeField, Min(0.05f)] private float delayBeforeNextSequence = 0.8f;
        [SerializeField] private bool autoAdvanceSequence = true;

        [Header("Piano")]
        [SerializeField] private List<PianoKeyConfig> pianoKeys = new();

        [Header("Secuencias")]
        [SerializeField] private List<PianoSequence> sequences = new();

        [Header("Referencias UI de la escena")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private TMP_Text tmpTitleText;
        [SerializeField] private TMP_Text tmpStatusText;
        [SerializeField] private TMP_Text tmpSequenceText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private List<Button> keyButtons = new();
        [SerializeField] private List<TMP_Text> tmpKeyLabels = new();
        [SerializeField] private AudioSource audioSource;

        [SerializeField, HideInInspector, FormerlySerializedAs("titleText")] private Text legacyTitleText;
        [SerializeField, HideInInspector, FormerlySerializedAs("statusText")] private Text legacyStatusText;
        [SerializeField, HideInInspector, FormerlySerializedAs("sequenceText")] private Text legacySequenceText;
        [SerializeField, HideInInspector, FormerlySerializedAs("keyLabels")] private List<Text> legacyKeyLabels = new();

        [Header("Flujo de actividad")]
        [SerializeField] private GameObject startPanel;
        [SerializeField] private GameObject pianoPanel;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Button startActivityButton;
        [SerializeField] private Button returnButton;
        [SerializeField] private TMP_Text tmpResultText;
        [SerializeField, HideInInspector, FormerlySerializedAs("resultText")] private Text legacyResultText;
        [SerializeField] private string returnSceneName = "SeleccionMundos";

        [Header("Estrellas")]
        [SerializeField] private Image[] starImages = new Image[3];
        [SerializeField] private Sprite fullStarSprite;
        [SerializeField] private Sprite emptyStarSprite;
        [SerializeField] private Bolin.UIStarDisplay starDisplay;

        [Header("Progreso visual de la secuencia")]
        [SerializeField] private Image[] sequenceStepIndicators = Array.Empty<Image>();
        [SerializeField] private TMP_Text[] sequenceStepLabels = Array.Empty<TMP_Text>();
        [SerializeField] private Color completedStepColor = new(0.32f, 0.76f, 0.46f, 1f);
        [SerializeField] private Color currentStepColor = new(1f, 0.76f, 0.2f, 1f);
        [SerializeField] private Color pendingStepColor = new(0.68f, 0.72f, 0.78f, 0.8f);

        private readonly Dictionary<float, AudioClip> generatedTones = new();
        private Coroutine sequenceRoutine;
        private Coroutine advanceRoutine;
        private int currentSequenceIndex;
        private int expectedStepIndex;
        private int currentStars = 3;
        private int mistakeCount;
        private bool acceptingInput;
        private bool activityStarted;
        private bool activityFinished;

        private void Reset()
        {
            EnsureDefaultData();
        }

        private void OnValidate()
        {
            EnsureDefaultData();
        }

        private void Awake()
        {
            // Prepara datos, referencias de escena, botones y estado inicial del mundo.
            EnsureDefaultData();
            ResolveMissingSceneReferences();
            ConfigureButtons();
            RefreshUi();
            PrepareInitialState();
        }

        private void Start()
        {
            if (playSequenceOnStart)
            {
                StartActivity();
            }
        }

        private void OnDisable()
        {
            if (sequenceRoutine != null)
            {
                StopCoroutine(sequenceRoutine);
                sequenceRoutine = null;
            }

            if (advanceRoutine != null)
            {
                StopCoroutine(advanceRoutine);
                advanceRoutine = null;
            }
        }

        public void StartActivity()
        {
            // Boton Iniciar: reinicia puntaje, muestra el piano y reproduce la primera secuencia.
            StopRunningRoutines();

            activityStarted = true;
            activityFinished = false;
            acceptingInput = false;
            currentSequenceIndex = 0;
            expectedStepIndex = 0;
            mistakeCount = 0;
            currentStars = 3;

            if (startPanel != null) startPanel.SetActive(false);
            if (pianoPanel != null) pianoPanel.SetActive(true);
            if (resultPanel != null) resultPanel.SetActive(false);
            if (startButton != null) startButton.gameObject.SetActive(false);
            if (nextButton != null) nextButton.gameObject.SetActive(false);

            UpdateStarsUi();
            RefreshUi();
            PlayCurrentSequence();
        }

        public void RestartActivity()
        {
            StartActivity();
        }

        public void CompleteActivity()
        {
            // Cierra la actividad, guarda progreso global y muestra el panel final con estrellas.
            if (activityFinished) return;

            StopRunningRoutines();
            activityFinished = true;
            acceptingInput = false;
            SetButtonsInteractable(false);
            SetStatus("Actividad completada");
            SaveProgress();
            UpdateStarsUi();

            if (tmpResultText != null)
            {
                tmpResultText.text = $"Actividad completada\nEstrellas obtenidas: {currentStars}/3";
            }

            if (resultPanel != null)
            {
                resultPanel.SetActive(true);
            }

            if (starDisplay != null) starDisplay.ShowStars(currentStars, true);
            OnActivityCompleted?.Invoke(currentStars);
        }

        public void RegisterMistake()
        {
            // Baja estrellas segun errores y refresca la UI compartida de estrellas.
            mistakeCount++;
            currentStars = Mathf.Clamp(3 - mistakeCount, 0, 3);
            UpdateStarsUi();
        }

        public void UpdateStarsUi()
        {
            if (starDisplay != null) starDisplay.SetImmediate(currentStars);
            if (starImages == null) return;

            for (int i = 0; i < starImages.Length; i++)
            {
                Image starImage = starImages[i];
                if (starImage == null) continue;

                Sprite targetSprite = i < currentStars ? fullStarSprite : emptyStarSprite;
                if (targetSprite != null)
                {
                    starImage.sprite = targetSprite;
                    starImage.color = Color.white;
                }
                else
                {
                    starImage.color = i < currentStars ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                }
            }
        }

        public void ReturnToWorldSelection()
        {
            // Boton Volver: usa el navegador central de escenas para regresar a seleccion.
            if (string.IsNullOrWhiteSpace(returnSceneName))
            {
                Debug.LogWarning("No hay una escena de regreso configurada para Mundo Musical.");
                return;
            }

            Bolin.SceneNavigation.LoadScene(returnSceneName, this);
        }

        public void PlayCurrentSequence()
        {
            // Reproduce la secuencia actual para que el alumno la escuche antes de responder.
            if (sequences.Count == 0)
            {
                SetStatus("No hay secuencias configuradas.");
                return;
            }

            if (sequenceRoutine != null)
            {
                StopCoroutine(sequenceRoutine);
            }

            sequenceRoutine = StartCoroutine(PlaySequenceRoutine());
        }

        public void NextSequence()
        {
            // Avanza manualmente a la siguiente secuencia configurada.
            if (sequences.Count == 0) return;

            currentSequenceIndex = (currentSequenceIndex + 1) % sequences.Count;
            expectedStepIndex = 0;
            acceptingInput = false;
            RefreshUi();
        }

        public void SelectSequence(int index)
        {
            // Permite seleccionar una secuencia especifica desde botones o pruebas.
            if (index < 0 || index >= sequences.Count) return;

            currentSequenceIndex = index;
            expectedStepIndex = 0;
            acceptingInput = false;
            RefreshUi();
        }

        private IEnumerator PlaySequenceRoutine()
        {
            // Ilumina y suena cada tecla, luego habilita la entrada del jugador.
            PianoSequence sequence = GetCurrentSequence();
            if (sequence == null || sequence.steps.Count == 0)
            {
                SetStatus("La secuencia actual esta vacia.");
                yield break;
            }

            acceptingInput = false;
            expectedStepIndex = 0;
            SetButtonsInteractable(false);
            SetStatus("Escucha la secuencia");
            UpdateSequenceProgress(true, -1);

            yield return new WaitForSeconds(0.25f);

            for (int stepIndex = 0; stepIndex < sequence.steps.Count; stepIndex++)
            {
                PianoStep step = sequence.steps[stepIndex];
                UpdateSequenceProgress(true, stepIndex);
                int keyIndex = Mathf.Clamp(step.keyIndex, 0, pianoKeys.Count - 1);
                yield return HighlightKeyRoutine(keyIndex, sequence.noteDelay);
            }

            acceptingInput = true;
            SetButtonsInteractable(true);
            SetStatus("Repite la secuencia");
            UpdateSequenceProgress(false);
            sequenceRoutine = null;
        }

        private void OnKeyPressed(int keyIndex)
        {
            // Recibe el clic de una tecla, compara con el paso esperado y decide avance o error.
            if (keyIndex < 0 || keyIndex >= pianoKeys.Count) return;
            if (!activityStarted || activityFinished) return;

            PlayKeySound(keyIndex);
            StartCoroutine(FlashKeyRoutine(keyIndex));

            if (!acceptingInput) return;

            PianoSequence sequence = GetCurrentSequence();
            if (sequence == null || expectedStepIndex >= sequence.steps.Count) return;

            int expectedKey = Mathf.Clamp(sequence.steps[expectedStepIndex].keyIndex, 0, pianoKeys.Count - 1);
            if (keyIndex != expectedKey)
            {
                acceptingInput = false;
                expectedStepIndex = 0;
                RegisterMistake();
                OnAnswerValidated?.Invoke(false);
                SetStatus("Intenta otra vez");
                UpdateSequenceProgress(false);

                if (repeatOnMistake)
                {
                    sequenceRoutine = StartCoroutine(ReplayAfterMistakeRoutine());
                }

                return;
            }

            expectedStepIndex++;
            SetStatus($"{expectedStepIndex}/{sequence.steps.Count}");
            UpdateSequenceProgress(false);

            if (expectedStepIndex < sequence.steps.Count) return;

            acceptingInput = false;
            SetStatus("Muy bien");
            OnAnswerValidated?.Invoke(true);

            if (autoAdvanceSequence)
            {
                if (advanceRoutine != null)
                {
                    StopCoroutine(advanceRoutine);
                }

                advanceRoutine = StartCoroutine(AdvanceAfterSequenceCompleteRoutine());
            }
            else
            {
                SetButtonsInteractable(false);
            }
        }

        private IEnumerator AdvanceAfterSequenceCompleteRoutine()
        {
            // Espera un momento y pasa a la siguiente secuencia o termina el mundo.
            SetButtonsInteractable(false);
            yield return new WaitForSeconds(delayBeforeNextSequence);

            if (activityFinished)
            {
                advanceRoutine = null;
                yield break;
            }

            if (currentSequenceIndex < sequences.Count - 1)
            {
                currentSequenceIndex++;
                expectedStepIndex = 0;
                RefreshUi();
                PlayCurrentSequence();
            }
            else
            {
                CompleteActivity();
            }

            advanceRoutine = null;
        }

        private IEnumerator ReplayAfterMistakeRoutine()
        {
            // Tras un error, vuelve a tocar la secuencia para dar otra oportunidad.
            SetButtonsInteractable(false);
            yield return new WaitForSeconds(mistakeReplayDelay);
            PlayCurrentSequence();
        }

        private IEnumerator HighlightKeyRoutine(int keyIndex, float delay)
        {
            // Hace sonar e ilumina una tecla durante la demostracion.
            PlayKeySound(keyIndex);
            SetKeyColor(keyIndex, true);
            yield return new WaitForSeconds(highlightDuration);
            SetKeyColor(keyIndex, false);
            yield return new WaitForSeconds(Mathf.Max(0.05f, delay));
        }

        private IEnumerator FlashKeyRoutine(int keyIndex)
        {
            // Da feedback visual inmediato cuando el alumno toca una tecla.
            SetKeyColor(keyIndex, true);
            yield return new WaitForSeconds(highlightDuration);
            SetKeyColor(keyIndex, false);
        }

        private void EnsureDefaultData()
        {
            // Crea teclas y secuencias basicas si el Inspector no trae datos.
            pianoKeys ??= new List<PianoKeyConfig>();
            sequences ??= new List<PianoSequence>();

            if (pianoKeys.Count == 0)
            {
                pianoKeys.AddRange(new[]
                {
                    new PianoKeyConfig { id = "Do", displayName = "1", normalColor = new Color(0.96f, 0.96f, 0.9f, 1f), highlightColor = new Color(1f, 0.31f, 0.31f, 1f), toneFrequency = 261.63f },
                    new PianoKeyConfig { id = "Re", displayName = "2", normalColor = new Color(0.95f, 0.95f, 0.95f, 1f), highlightColor = new Color(1f, 0.58f, 0.22f, 1f), toneFrequency = 293.66f },
                    new PianoKeyConfig { id = "Mi", displayName = "3", normalColor = new Color(0.96f, 0.96f, 0.9f, 1f), highlightColor = new Color(1f, 0.89f, 0.24f, 1f), toneFrequency = 329.63f },
                    new PianoKeyConfig { id = "Fa", displayName = "4", normalColor = new Color(0.95f, 0.95f, 0.95f, 1f), highlightColor = new Color(0.35f, 0.78f, 0.45f, 1f), toneFrequency = 349.23f },
                    new PianoKeyConfig { id = "Sol", displayName = "5", normalColor = new Color(0.96f, 0.96f, 0.9f, 1f), highlightColor = new Color(0.25f, 0.62f, 1f, 1f), toneFrequency = 392f },
                    new PianoKeyConfig { id = "La", displayName = "6", normalColor = new Color(0.95f, 0.95f, 0.95f, 1f), highlightColor = new Color(0.67f, 0.45f, 1f, 1f), toneFrequency = 440f },
                    new PianoKeyConfig { id = "Si", displayName = "7", normalColor = new Color(0.96f, 0.96f, 0.9f, 1f), highlightColor = new Color(1f, 0.42f, 0.78f, 1f), toneFrequency = 493.88f },
                });
            }

            if (sequences.Count == 0)
            {
                sequences.AddRange(new[]
                {
                    new PianoSequence { sequenceName = "Inicio", steps = new List<PianoStep> { new() { keyIndex = 0 }, new() { keyIndex = 1 }, new() { keyIndex = 2 } } },
                    new PianoSequence { sequenceName = "Subida", steps = new List<PianoStep> { new() { keyIndex = 2 }, new() { keyIndex = 3 }, new() { keyIndex = 4 }, new() { keyIndex = 2 } } },
                    new PianoSequence { sequenceName = "Eco", steps = new List<PianoStep> { new() { keyIndex = 4 }, new() { keyIndex = 4 }, new() { keyIndex = 5 }, new() { keyIndex = 6 } } },
                    new PianoSequence { sequenceName = "Reto", steps = new List<PianoStep> { new() { keyIndex = 6 }, new() { keyIndex = 4 }, new() { keyIndex = 2 }, new() { keyIndex = 0 }, new() { keyIndex = 3 } } },
                });
            }
        }

        private void ResolveMissingSceneReferences()
        {
            // Completa referencias opcionales buscando componentes en la propia escena.
            audioSource ??= GetComponent<AudioSource>();

            if (tmpKeyLabels.Count != keyButtons.Count)
            {
                tmpKeyLabels.Clear();
                foreach (Button keyButton in keyButtons)
                {
                    tmpKeyLabels.Add(keyButton != null ? keyButton.GetComponentInChildren<TMP_Text>(true) : null);
                }
            }
        }

        private void ConfigureButtons()
        {
            // Conecta los botones del Inspector con las acciones de este controlador.
            if (startActivityButton != null)
            {
                startActivityButton.onClick.RemoveAllListeners();
                startActivityButton.onClick.AddListener(StartActivity);
            }

            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(PlayCurrentSequence);
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(NextSequence);
            }

            for (int i = 0; i < keyButtons.Count; i++)
            {
                Button keyButton = keyButtons[i];
                if (keyButton == null) continue;

                int index = i;
                keyButton.onClick.RemoveAllListeners();
                keyButton.onClick.AddListener(() => OnKeyPressed(index));
            }

            if (returnButton != null)
            {
                returnButton.onClick.RemoveAllListeners();
                returnButton.onClick.AddListener(ReturnToWorldSelection);
            }
        }

        private void RefreshUi()
        {
            // Refresca titulos, estado, etiquetas de teclas e indicadores de progreso.
            PianoSequence sequence = GetCurrentSequence();
            if (tmpTitleText != null) tmpTitleText.text = "Mundo Musical";
            if (tmpSequenceText != null) tmpSequenceText.text = sequence != null ? $"Secuencia: {sequence.sequenceName}" : "Sin secuencias";
            if (!activityStarted)
            {
                SetStatus("Presiona Iniciar");
            }

            UpdateSequenceProgress(false);

            for (int i = 0; i < keyButtons.Count; i++)
            {
                if (i < tmpKeyLabels.Count && tmpKeyLabels[i] != null && i < pianoKeys.Count)
                {
                    tmpKeyLabels[i].text = pianoKeys[i].displayName;
                    tmpKeyLabels[i].fontSize = Mathf.Max(tmpKeyLabels[i].fontSize, 54f);
                    tmpKeyLabels[i].fontStyle |= FontStyles.Bold;
                }

                SetKeyColor(i, false);
            }
        }

        private void PrepareInitialState()
        {
            // Deja visible el panel inicial y bloquea el piano hasta que se pulse Iniciar.
            activityStarted = false;
            activityFinished = false;
            acceptingInput = false;
            currentSequenceIndex = 0;
            expectedStepIndex = 0;
            currentStars = 3;
            mistakeCount = 0;

            if (startPanel != null) startPanel.SetActive(true);
            if (pianoPanel != null) pianoPanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);
            if (startButton != null) startButton.gameObject.SetActive(false);
            if (nextButton != null) nextButton.gameObject.SetActive(false);

            SetButtonsInteractable(false);
            UpdateStarsUi();
            UpdateSequenceProgress(false);
            SetStatus("Presiona Iniciar");
        }

        private void UpdateSequenceProgress(bool demonstrating, int demonstrationStep = -1)
        {
            // Pinta cada paso como pendiente, actual o completado segun el avance.
            PianoSequence sequence = GetCurrentSequence();
            int stepCount = sequence?.steps?.Count ?? 0;
            for (int i = 0; i < sequenceStepIndicators.Length; i++)
            {
                Image indicator = sequenceStepIndicators[i];
                TMP_Text label = i < sequenceStepLabels.Length ? sequenceStepLabels[i] : null;
                bool visible = i < stepCount;
                if (indicator != null) indicator.gameObject.SetActive(visible);
                if (label != null) label.gameObject.SetActive(visible);
                if (!visible) continue;

                int keyIndex = Mathf.Clamp(sequence.steps[i].keyIndex, 0, pianoKeys.Count - 1);
                if (label != null) label.text = pianoKeys.Count > 0 ? pianoKeys[keyIndex].displayName : string.Empty;

                Color stateColor;
                if (demonstrating)
                {
                    stateColor = i == demonstrationStep ? currentStepColor : pendingStepColor;
                }
                else if (i < expectedStepIndex)
                {
                    stateColor = completedStepColor;
                }
                else
                {
                    stateColor = i == expectedStepIndex ? currentStepColor : pendingStepColor;
                }

                if (indicator != null) indicator.color = stateColor;
            }
        }

        private void SaveProgress()
        {
            // Registra el mejor resultado del mundo musical en el repositorio global.
            Bolin.WorldProgressRepository.SaveBestResult(WorldIndex, currentStars);
        }

        private void StopRunningRoutines()
        {
            // Corta corutinas activas para evitar que una secuencia vieja siga modificando la UI.
            if (sequenceRoutine != null)
            {
                StopCoroutine(sequenceRoutine);
                sequenceRoutine = null;
            }

            if (advanceRoutine != null)
            {
                StopCoroutine(advanceRoutine);
                advanceRoutine = null;
            }
        }

        private PianoSequence GetCurrentSequence()
        {
            if (sequences.Count == 0) return null;
            currentSequenceIndex = Mathf.Clamp(currentSequenceIndex, 0, sequences.Count - 1);
            return sequences[currentSequenceIndex];
        }

        private void SetStatus(string message)
        {
            if (tmpStatusText != null) tmpStatusText.text = message;
        }

        private void SetButtonsInteractable(bool interactable)
        {
            foreach (Button keyButton in keyButtons)
            {
                if (keyButton != null) keyButton.interactable = interactable;
            }
        }

        private void SetKeyColor(int keyIndex, bool highlighted)
        {
            if (keyIndex < 0 || keyIndex >= keyButtons.Count || keyIndex >= pianoKeys.Count) return;

            Image image = keyButtons[keyIndex] != null ? keyButtons[keyIndex].GetComponent<Image>() : null;
            if (image == null) return;

            image.color = highlighted ? pianoKeys[keyIndex].highlightColor : pianoKeys[keyIndex].normalColor;
        }

        private void PlayKeySound(int keyIndex)
        {
            // Usa AudioClip asignado o genera un tono senoidal si no hay audio en la tecla.
            if (audioSource == null || keyIndex < 0 || keyIndex >= pianoKeys.Count) return;

            PianoKeyConfig key = pianoKeys[keyIndex];
            AudioClip clip = key.audioClip != null ? key.audioClip : GetGeneratedTone(key.toneFrequency);
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private AudioClip GetGeneratedTone(float frequency)
        {
            // Genera y cachea tonos simples para que el piano funcione aun sin clips importados.
            if (generatedTones.TryGetValue(frequency, out AudioClip clip)) return clip;

            const int sampleRate = 44100;
            const float duration = 0.24f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)sampleRate;
                float envelope = Mathf.Clamp01(1f - time / duration);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) * 0.28f * envelope;
            }

            clip = AudioClip.Create($"Tone {frequency:0.##}", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            generatedTones[frequency] = clip;
            return clip;
        }
    }
}
