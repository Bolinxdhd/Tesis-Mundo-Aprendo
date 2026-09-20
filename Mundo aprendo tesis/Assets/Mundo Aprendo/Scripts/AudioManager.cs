using System.Collections;
using UnityEngine;

namespace Bolin
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        public const string MasterVolumeKey = "master_volume";
        public const string MusicVolumeKey = "music_volume";

        [Header("Settings")]
        [SerializeField] private float timeTransition = 0.5f;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Interfaz")]
        [SerializeField] private AudioClip uiClickClip;
        [SerializeField, Range(0f, 1f)] private float uiClickVolume = 0.45f;

        private bool isTransitioning;
        private float currentMusicBaseVolume = 1f;
        private int lastUiClickFrame = -1;
        public static AudioManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (sfxSource == null) sfxSource = GetComponent<AudioSource>();
            ApplySavedVolumes();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static bool TryPlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (Instance == null || clip == null) return false;
            return Instance.PlaySfx(clip, volumeScale);
        }

        public static bool TryPlayUiClick(AudioClip fallbackClip = null)
        {
            return Instance != null && Instance.PlayUiClick(fallbackClip);
        }

        public bool PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (sfxSource == null || clip == null) return false;
            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
            return true;
        }

        public bool PlayUiClick(AudioClip fallbackClip = null)
        {
            AudioClip clip = uiClickClip != null ? uiClickClip : fallbackClip;
            if (clip == null || lastUiClickFrame == Time.frameCount) return false;

            lastUiClickFrame = Time.frameCount;
            return PlaySfx(clip, uiClickVolume);
        }

        public void PlayMusic(AudioClip clip, bool restartIfSame = false)
        {
            PlayMusic(clip, 1f, restartIfSame);
        }

        public void PlayMusic(AudioClip clip, float baseVolume, bool restartIfSame = false)
        {
            if (musicSource == null || clip == null) return;
            currentMusicBaseVolume = Mathf.Clamp01(baseVolume);
            ApplyMusicVolume(PlayerPrefs.GetFloat(MusicVolumeKey, 1f));
            if (!restartIfSame && musicSource.clip == clip && musicSource.isPlaying) return;

            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource == null) return;

            musicSource.Stop();
            musicSource.clip = null;
        }

        public void SetMasterVolume(float volume)
        {
            float clamped = Mathf.Clamp01(volume);
            AudioListener.volume = clamped;
            PlayerPrefs.SetFloat(MasterVolumeKey, clamped);
            PlayerPrefs.Save();
        }

        public void SetMusicVolume(float volume)
        {
            float clamped = Mathf.Clamp01(volume);
            ApplyMusicVolume(clamped);
            PlayerPrefs.SetFloat(MusicVolumeKey, clamped);
            PlayerPrefs.Save();
        }

        public void ApplySavedVolumes()
        {
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
            ApplyMusicVolume(PlayerPrefs.GetFloat(MusicVolumeKey, 1f));
        }

        private void ApplyMusicVolume(float userVolume)
        {
            if (musicSource != null)
            {
                musicSource.volume = currentMusicBaseVolume * Mathf.Clamp01(userVolume);
            }
        }

        public void managePanel(AudioClip clickSound, GameObject ShowPanel, GameObject hidePanel)
        {
            if (isTransitioning) return;
            StartCoroutine(Transition(clickSound, ShowPanel, hidePanel));
        }
        private IEnumerator Transition(AudioClip clickSound, GameObject ShowPanel, GameObject hidePanel)
        {
            isTransitioning = true;
            PlayUiClick(clickSound);
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, timeTransition));
            if (ShowPanel != null) ShowPanel.SetActive(true);
            if (hidePanel != null) hidePanel.SetActive(false);
            isTransitioning = false;
        }
    }
}
