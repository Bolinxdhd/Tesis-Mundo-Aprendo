using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Bolin
{
    /// <summary>
    /// Owns presentation-only motion for Mundo Musical. The sequence, validation,
    /// stars and audio remain owned by MundoMusicalSequenceGame.
    /// </summary>
    public class MusicalLevelAnimationController : MonoBehaviour
    {
        [Header("Entrada y personaje")]
        [SerializeField] private CanvasGroup gameplayGroup;
        [SerializeField] private RectTransform pianoFrame;
        [SerializeField] private RectTransform characterRoot;
        [SerializeField] private Image characterImage;
        [SerializeField] private Sprite idlePose;
        [SerializeField] private Sprite listeningPose;
        [SerializeField] private Sprite encouragementPose;
        [SerializeField] private Sprite celebrationPose;

        [Header("Resultado")]
        [SerializeField] private CanvasGroup resultGroup;
        [SerializeField] private StoryResultStarAnimation resultStarAnimation;

        [Header("Ritmo visual")]
        [SerializeField, Min(0.05f)] private float entranceDuration = 0.28f;
        [SerializeField, Min(0.05f)] private float characterIdleDistance = 9f;
        [SerializeField, Min(0.05f)] private float characterIdleSpeed = 1.25f;
        [SerializeField, Range(0.01f, 0.2f)] private float pianoPulseAmount = 0.055f;

        private Vector3 pianoBaseScale = Vector3.one;
        private Vector3 characterBaseScale = Vector3.one;
        private Vector2 characterBasePosition;
        private Coroutine entranceRoutine;
        private Coroutine idleRoutine;
        private Coroutine pianoRoutine;
        private Coroutine characterReactionRoutine;

        private void Awake()
        {
            CaptureBaseState();
            ApplyPose(idlePose);
        }

        private void OnEnable()
        {
            StartCharacterIdle();
        }

        private void Start()
        {
            PlayInitialEntrance();
        }

        public void PlayInitialEntrance()
        {
            StopRoutine(ref entranceRoutine);
            entranceRoutine = StartCoroutine(InitialEntranceRoutine());
        }

        public void PlaySequenceCue()
        {
            ApplyPose(listeningPose != null ? listeningPose : idlePose);
            PulsePiano(1f);
        }

        public void PlayCorrectFeedback()
        {
            StartCharacterReaction(encouragementPose != null ? encouragementPose : idlePose, 0.62f, 1.08f);
            PulsePiano(0.7f);
        }

        public void PlayMistakeFeedback()
        {
            StartCharacterReaction(listeningPose != null ? listeningPose : idlePose, 0.45f, 0.96f);
            PulsePiano(0.42f);
        }

        public void PlayResult(int earnedStars)
        {
            ApplyPose(celebrationPose != null ? celebrationPose : encouragementPose);
            if (resultGroup != null)
            {
                resultGroup.alpha = 1f;
                resultGroup.interactable = true;
                resultGroup.blocksRaycasts = true;
            }

            resultStarAnimation?.Play(earnedStars);
        }

        public void StopResultPresentation()
        {
            resultStarAnimation?.StopAndReset();
            if (resultGroup != null)
            {
                resultGroup.alpha = 0f;
                resultGroup.interactable = false;
                resultGroup.blocksRaycasts = false;
            }

            ApplyPose(idlePose);
        }

        private IEnumerator InitialEntranceRoutine()
        {
            if (gameplayGroup != null)
            {
                gameplayGroup.alpha = 0f;
                gameplayGroup.interactable = true;
                gameplayGroup.blocksRaycasts = true;
            }

            if (pianoFrame != null) pianoFrame.localScale = pianoBaseScale * 0.94f;
            if (characterRoot != null)
            {
                characterRoot.anchoredPosition = characterBasePosition + Vector2.left * 95f;
                characterRoot.localScale = characterBaseScale * 0.92f;
            }

            float elapsed = 0f;
            while (elapsed < entranceDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / entranceDuration));
                if (gameplayGroup != null) gameplayGroup.alpha = t;
                if (pianoFrame != null) pianoFrame.localScale = Vector3.Lerp(pianoBaseScale * 0.94f, pianoBaseScale, t);
                if (characterRoot != null)
                {
                    characterRoot.anchoredPosition = Vector2.Lerp(characterBasePosition + Vector2.left * 95f, characterBasePosition, t);
                    characterRoot.localScale = Vector3.Lerp(characterBaseScale * 0.92f, characterBaseScale, t);
                }
                yield return null;
            }

            if (gameplayGroup != null) gameplayGroup.alpha = 1f;
            if (pianoFrame != null) pianoFrame.localScale = pianoBaseScale;
            if (characterRoot != null)
            {
                characterRoot.anchoredPosition = characterBasePosition;
                characterRoot.localScale = characterBaseScale;
            }

            entranceRoutine = null;
        }

        private void StartCharacterIdle()
        {
            if (idleRoutine != null || characterRoot == null) return;
            idleRoutine = StartCoroutine(CharacterIdleRoutine());
        }

        private IEnumerator CharacterIdleRoutine()
        {
            while (true)
            {
                float wave = Mathf.Sin(Time.unscaledTime * characterIdleSpeed);
                characterRoot.anchoredPosition = characterBasePosition + Vector2.up * (wave * characterIdleDistance);
                characterRoot.localRotation = Quaternion.Euler(0f, 0f, wave * 1.3f);
                yield return null;
            }
        }

        private void StartCharacterReaction(Sprite pose, float duration, float scaleMultiplier)
        {
            StopRoutine(ref characterReactionRoutine);
            characterReactionRoutine = StartCoroutine(CharacterReactionRoutine(pose, duration, scaleMultiplier));
        }

        private IEnumerator CharacterReactionRoutine(Sprite pose, float duration, float scaleMultiplier)
        {
            ApplyPose(pose);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pop = 1f + Mathf.Sin(t * Mathf.PI) * (scaleMultiplier - 1f);
                if (characterRoot != null) characterRoot.localScale = characterBaseScale * pop;
                yield return null;
            }

            if (characterRoot != null) characterRoot.localScale = characterBaseScale;
            ApplyPose(idlePose);
            characterReactionRoutine = null;
        }

        private void PulsePiano(float intensity)
        {
            if (pianoFrame == null) return;
            StopRoutine(ref pianoRoutine);
            pianoRoutine = StartCoroutine(PianoPulseRoutine(Mathf.Clamp01(intensity)));
        }

        private IEnumerator PianoPulseRoutine(float intensity)
        {
            const float duration = 0.28f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float wave = Mathf.Sin(t * Mathf.PI);
                pianoFrame.localScale = pianoBaseScale * (1f + wave * pianoPulseAmount * intensity);
                yield return null;
            }

            pianoFrame.localScale = pianoBaseScale;
            pianoRoutine = null;
        }

        private void ApplyPose(Sprite pose)
        {
            if (characterImage == null || pose == null) return;
            characterImage.sprite = pose;
            characterImage.preserveAspect = true;
        }

        private void CaptureBaseState()
        {
            if (pianoFrame != null) pianoBaseScale = pianoFrame.localScale;
            if (characterRoot != null)
            {
                characterBaseScale = characterRoot.localScale;
                characterBasePosition = characterRoot.anchoredPosition;
            }
        }

        private void StopRoutine(ref Coroutine routine)
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
        }

        private void OnDisable()
        {
            if (entranceRoutine != null) StopCoroutine(entranceRoutine);
            if (idleRoutine != null) StopCoroutine(idleRoutine);
            if (pianoRoutine != null) StopCoroutine(pianoRoutine);
            if (characterReactionRoutine != null) StopCoroutine(characterReactionRoutine);
            entranceRoutine = null;
            idleRoutine = null;
            pianoRoutine = null;
            characterReactionRoutine = null;
            resultStarAnimation?.StopAndReset();
        }
    }
}
