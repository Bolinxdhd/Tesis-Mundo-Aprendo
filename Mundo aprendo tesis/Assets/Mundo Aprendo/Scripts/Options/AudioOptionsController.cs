using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bolin
{
    public class AudioOptionsController : MonoBehaviour
    {
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private AudioClip musicClip;
        [SerializeField] private AudioSource musicSource;

        [Header("Microfono")]
        [SerializeField] private TMP_Dropdown microphoneDropdown;
        [SerializeField] private Button refreshMicrophonesButton;
        [SerializeField] private Button testMicrophoneButton;
        [SerializeField] private Slider microphoneLevelSlider;
        [SerializeField] private TMP_Text microphoneStatusText;
        [SerializeField] private TMP_Text microphoneFrequencyText;
        [SerializeField, Min(0.5f)] private float microphoneTestDuration = 5f;
        [SerializeField, Range(64, 4096)] private int frequencySampleWindow = 1024;

        private Coroutine microphoneTestRoutine;
        private string currentTestDevice;

        private void Awake()
        {
            float masterVolume = PlayerPrefs.GetFloat(AudioManager.MasterVolumeKey, 1f);
            float musicVolume = PlayerPrefs.GetFloat(AudioManager.MusicVolumeKey, 1f);

            SetupSlider(masterSlider, masterVolume);
            SetupSlider(musicSlider, musicVolume);
            ApplyMasterVolume(masterVolume);
            ApplyMusicVolume(musicVolume);

            bool isMenuScene = SceneManager.GetActiveScene().name == MundoAprendoSceneNames.Menu;
            if (AudioManager.Instance != null && isMenuScene)
            {
                AudioManager.Instance.PlayMusic(musicClip, true);
            }
            else if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopMusic();
            }
            else if (isMenuScene && musicSource != null && musicClip != null)
            {
                musicSource.clip = musicClip;
                musicSource.loop = true;
                musicSource.Play();
            }
            else if (musicSource != null)
            {
                musicSource.Stop();
                musicSource.clip = null;
            }

            RefreshMicrophoneList();
        }

        private void OnEnable()
        {
            if (masterSlider != null) masterSlider.onValueChanged.AddListener(SetMasterVolume);
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(SetMusicVolume);
            if (microphoneDropdown != null) microphoneDropdown.onValueChanged.AddListener(SetMicrophoneByIndex);
            if (refreshMicrophonesButton != null) refreshMicrophonesButton.onClick.AddListener(RefreshMicrophoneList);
            if (testMicrophoneButton != null) testMicrophoneButton.onClick.AddListener(StartMicrophoneFrequencyTest);
        }

        private void OnDisable()
        {
            if (masterSlider != null) masterSlider.onValueChanged.RemoveListener(SetMasterVolume);
            if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
            if (microphoneDropdown != null) microphoneDropdown.onValueChanged.RemoveListener(SetMicrophoneByIndex);
            if (refreshMicrophonesButton != null) refreshMicrophonesButton.onClick.RemoveListener(RefreshMicrophoneList);
            if (testMicrophoneButton != null) testMicrophoneButton.onClick.RemoveListener(StartMicrophoneFrequencyTest);
            StopMicrophoneFrequencyTest();
        }

        private void OnDestroy()
        {
            StopMicrophoneFrequencyTest();
        }

        public void SetMasterVolume(float volume)
        {
            ApplyMasterVolume(volume);
            PlayerPrefs.SetFloat(AudioManager.MasterVolumeKey, volume);
        }

        public void SetMusicVolume(float volume)
        {
            ApplyMusicVolume(volume);
            PlayerPrefs.SetFloat(AudioManager.MusicVolumeKey, volume);
        }

        public void RefreshMicrophoneList()
        {
            if (microphoneDropdown == null) return;

            string[] devices = Microphone.devices;
            microphoneDropdown.onValueChanged.RemoveListener(SetMicrophoneByIndex);
            microphoneDropdown.options.Clear();

            if (devices == null || devices.Length == 0)
            {
                microphoneDropdown.options.Add(new TMP_Dropdown.OptionData("Sin microfonos"));
                microphoneDropdown.SetValueWithoutNotify(0);
                microphoneDropdown.interactable = false;
                SetMicrophoneStatus("No se detecto ningun microfono.");
                SetMicrophoneFrequencyText("Frecuencia: -- Hz");
                SetMicrophoneLevel(0f);
                microphoneDropdown.RefreshShownValue();
                microphoneDropdown.onValueChanged.AddListener(SetMicrophoneByIndex);
                return;
            }

            microphoneDropdown.interactable = true;
            foreach (string device in devices)
            {
                microphoneDropdown.options.Add(new TMP_Dropdown.OptionData(string.IsNullOrWhiteSpace(device) ? "Microfono predeterminado" : device));
            }

            string selectedDevice = MicrophoneSettings.GetAvailableSelectedDevice();
            int selectedIndex = 0;
            for (int i = 0; i < devices.Length; i++)
            {
                if (devices[i] == selectedDevice)
                {
                    selectedIndex = i;
                    break;
                }
            }

            microphoneDropdown.SetValueWithoutNotify(selectedIndex);
            microphoneDropdown.RefreshShownValue();
            microphoneDropdown.onValueChanged.AddListener(SetMicrophoneByIndex);
            SetMicrophoneStatus($"Microfono seleccionado: {devices[selectedIndex]}");
        }

        public void SetMicrophoneByIndex(int index)
        {
            string[] devices = Microphone.devices;
            if (devices == null || devices.Length == 0) return;

            index = Mathf.Clamp(index, 0, devices.Length - 1);
            MicrophoneSettings.SelectedDeviceName = devices[index];
            SetMicrophoneStatus($"Microfono seleccionado: {devices[index]}");
        }

        public void StartMicrophoneFrequencyTest()
        {
            StopMicrophoneFrequencyTest();

            string selectedDevice = MicrophoneSettings.GetAvailableSelectedDevice();
            if (string.IsNullOrWhiteSpace(selectedDevice))
            {
                SetMicrophoneStatus("No se detecto ningun microfono para probar.");
                return;
            }

            microphoneTestRoutine = StartCoroutine(MicrophoneFrequencyTestRoutine(selectedDevice));
        }

        public void StopMicrophoneFrequencyTest()
        {
            if (microphoneTestRoutine != null)
            {
                StopCoroutine(microphoneTestRoutine);
                microphoneTestRoutine = null;
            }

            if (!string.IsNullOrWhiteSpace(currentTestDevice) && Microphone.IsRecording(currentTestDevice))
            {
                Microphone.End(currentTestDevice);
            }

            currentTestDevice = string.Empty;
        }

        private IEnumerator MicrophoneFrequencyTestRoutine(string deviceName)
        {
            const int sampleRate = 44100;
            const int clipSeconds = 10;

            currentTestDevice = deviceName;
            SetMicrophoneStatus("Probando microfono. Habla o emite un sonido.");
            SetMicrophoneLevel(0f);

            AudioClip clip = Microphone.Start(deviceName, true, clipSeconds, sampleRate);
            float startupLimit = Time.realtimeSinceStartup + 2f;

            while (Microphone.GetPosition(deviceName) <= 0 && Time.realtimeSinceStartup < startupLimit)
            {
                yield return null;
            }

            if (Microphone.GetPosition(deviceName) <= 0)
            {
                SetMicrophoneStatus("El microfono tardo demasiado en activarse.");
                Microphone.End(deviceName);
                microphoneTestRoutine = null;
                yield break;
            }

            float[] samples = new float[Mathf.Max(64, frequencySampleWindow)];
            float finishTime = Time.realtimeSinceStartup + microphoneTestDuration;

            while (Time.realtimeSinceStartup < finishTime && Microphone.IsRecording(deviceName))
            {
                int micPosition = Microphone.GetPosition(deviceName);
                if (micPosition >= samples.Length)
                {
                    clip.GetData(samples, micPosition - samples.Length);
                    float rms = CalculateRms(samples);
                    float frequency = EstimateFrequency(samples, sampleRate);

                    SetMicrophoneLevel(Mathf.Clamp01(rms * 12f));
                    SetMicrophoneFrequencyText(frequency > 0f
                        ? $"Frecuencia aproximada: {frequency:0} Hz"
                        : "Frecuencia aproximada: -- Hz");
                }

                yield return null;
            }

            Microphone.End(deviceName);
            currentTestDevice = string.Empty;
            microphoneTestRoutine = null;
            SetMicrophoneStatus("Prueba de microfono finalizada.");
        }

        private static void ApplyMasterVolume(float volume)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetMasterVolume(volume);
            else AudioListener.volume = Mathf.Clamp01(volume);
        }

        private void ApplyMusicVolume(float volume)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(volume);
            else if (musicSource != null) musicSource.volume = Mathf.Clamp01(volume);
        }

        private static void SetupSlider(Slider slider, float value)
        {
            if (slider == null) return;

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.SetValueWithoutNotify(Mathf.Clamp01(value));
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

        private static float EstimateFrequency(float[] samples, int sampleRate)
        {
            if (samples == null || samples.Length < 128) return 0f;

            float rms = CalculateRms(samples);
            if (rms < 0.01f) return 0f;

            int minLag = Mathf.Max(1, sampleRate / 1000);
            int maxLag = Mathf.Min(samples.Length / 2, sampleRate / 70);
            int bestLag = 0;
            float bestCorrelation = 0f;

            for (int lag = minLag; lag <= maxLag; lag++)
            {
                float correlation = 0f;
                for (int i = 0; i < samples.Length - lag; i++)
                {
                    correlation += samples[i] * samples[i + lag];
                }

                if (correlation > bestCorrelation)
                {
                    bestCorrelation = correlation;
                    bestLag = lag;
                }
            }

            return bestLag <= 0 ? 0f : sampleRate / (float)bestLag;
        }

        private void SetMicrophoneStatus(string message)
        {
            if (microphoneStatusText != null)
            {
                microphoneStatusText.text = message;
            }
        }

        private void SetMicrophoneFrequencyText(string message)
        {
            if (microphoneFrequencyText != null)
            {
                microphoneFrequencyText.text = message;
            }
        }

        private void SetMicrophoneLevel(float value)
        {
            if (microphoneLevelSlider == null) return;

            microphoneLevelSlider.minValue = 0f;
            microphoneLevelSlider.maxValue = 1f;
            microphoneLevelSlider.wholeNumbers = false;
            microphoneLevelSlider.SetValueWithoutNotify(Mathf.Clamp01(value));
        }
    }
}
