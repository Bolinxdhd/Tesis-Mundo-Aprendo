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

        private bool isTransitioning;
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

        public static bool TryPlaySfx(AudioClip clip)
        {
            if (Instance == null || clip == null) return false;
            return Instance.PlaySfx(clip);
        }

        public bool PlaySfx(AudioClip clip)
        {
            if (sfxSource == null || clip == null) return false;
            sfxSource.PlayOneShot(clip);
            return true;
        }

        public void PlayMusic(AudioClip clip, bool restartIfSame = false)
        {
            if (musicSource == null || clip == null) return;
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
        }

        public void SetMusicVolume(float volume)
        {
            float clamped = Mathf.Clamp01(volume);
            if (musicSource != null) musicSource.volume = clamped;
            PlayerPrefs.SetFloat(MusicVolumeKey, clamped);
        }

        public void ApplySavedVolumes()
        {
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
            if (musicSource != null)
            {
                musicSource.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 1f));
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
            PlaySfx(clickSound);
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, timeTransition));
            if (ShowPanel != null) ShowPanel.SetActive(true);
            if (hidePanel != null) hidePanel.SetActive(false);
            isTransitioning = false;
        }
    }
}
