using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolin
{
    public class EmotionResultPanel : MonoBehaviour
    {
        [SerializeField] private GameObject rootObject;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text resultTitle;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private Image[] starImages = new Image[3];
        [SerializeField] private Sprite fullStarSprite;
        [SerializeField] private Sprite emptyStarSprite;
        [SerializeField] private UIStarDisplay starDisplay;
        [SerializeField, Min(0f)] private float fadeDuration = 0.25f;
        [SerializeField, Min(0f)] private float delayBetweenStars = 0.18f;

        private Coroutine showRoutine;

        public void Show(int correctAnswers, int mistakes, int stars, AudioSource sfxSource, AudioClip starClip)
        {
            if (rootObject == null) return;

            rootObject.SetActive(true);
            if (resultTitle != null) resultTitle.text = "Actividad completada";
            if (scoreText != null)
            {
                scoreText.text = $"Aciertos: {correctAnswers}\nErrores: {mistakes}\nEstrellas: {stars}/3";
            }

            if (showRoutine != null) StopCoroutine(showRoutine);
            showRoutine = StartCoroutine(ShowRoutine(Mathf.Clamp(stars, 0, 3), sfxSource, starClip));
        }

        public void Hide()
        {
            if (showRoutine != null)
            {
                StopCoroutine(showRoutine);
                showRoutine = null;
            }

            if (rootObject != null) rootObject.SetActive(false);
        }

        private IEnumerator ShowRoutine(int stars, AudioSource sfxSource, AudioClip starClip)
        {
            if (starDisplay != null)
            {
                starDisplay.ShowStars(stars, true);
                yield break;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    canvasGroup.alpha = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, fadeDuration));
                    yield return null;
                }

                canvasGroup.alpha = 1f;
            }

            for (int i = 0; i < starImages.Length; i++)
            {
                Image starImage = starImages[i];
                if (starImage == null) continue;

                starImage.gameObject.SetActive(false);
                starImage.sprite = i < stars ? fullStarSprite : emptyStarSprite;
                starImage.color = Color.white;
            }

            for (int i = 0; i < starImages.Length; i++)
            {
                Image starImage = starImages[i];
                if (starImage == null) continue;

                starImage.gameObject.SetActive(true);
                if (i < stars && sfxSource != null && starClip != null)
                {
                    sfxSource.PlayOneShot(starClip);
                }

                if (delayBetweenStars > 0f) yield return new WaitForSeconds(delayBetweenStars);
            }

            showRoutine = null;
        }

        private void OnValidate()
        {
            if (rootObject == null) Debug.LogWarning($"{name}: falta asignar Root Object del resultado.", this);
            if (starImages == null || starImages.Length != 3) Debug.LogWarning($"{name}: deben existir tres estrellas.", this);
        }
    }
}
