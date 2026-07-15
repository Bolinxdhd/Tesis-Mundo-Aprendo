using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Bolin
{
    public class StoryResultStarAnimation : MonoBehaviour
    {
        [SerializeField] private CanvasGroup loopGroup;
        [SerializeField] private RectTransform mainStar;
        [SerializeField] private Image mainStarImage;
        [SerializeField] private RectTransform[] floatingStars = new RectTransform[0];
        [SerializeField] private Image[] floatingStarImages = new Image[0];
        [SerializeField] private Vector2[] travelOffsets = new Vector2[0];
        [SerializeField, Min(0f)] private float startDelay = 0.9f;
        [SerializeField, Min(0.5f)] private float loopDuration = 2.25f;
        [SerializeField, Min(0.25f)] private float travelDuration = 1.22f;
        [SerializeField, Range(0.1f, 1f)] private float startScale = 0.38f;
        [SerializeField, Range(0.5f, 1.5f)] private float peakScale = 1.05f;
        [SerializeField, Min(0f)] private float rotationDegrees = 175f;

        private static readonly Vector2[] DefaultOffsets =
        {
            new(-78f, 136f),
            new(68f, 152f),
            new(-24f, 164f),
            new(96f, 118f),
            new(-108f, 108f)
        };

        private Vector2[] basePositions = new Vector2[0];
        private Vector3[] baseScales = new Vector3[0];
        private Quaternion[] baseRotations = new Quaternion[0];
        private Color[] baseColors = new Color[0];
        private Vector3 mainBaseScale = Vector3.one;
        private Quaternion mainBaseRotation = Quaternion.identity;
        private Color mainBaseColor = Color.white;
        private Coroutine loopRoutine;
        private int currentEarnedStars;

        private void Awake()
        {
            if (loopGroup == null) loopGroup = GetComponent<CanvasGroup>();
            if (mainStarImage == null && mainStar != null) mainStarImage = mainStar.GetComponent<Image>();
            CaptureBaseState();
            StopAndReset();
        }

        public void Play(int earnedStars)
        {
            currentEarnedStars = Mathf.Clamp(earnedStars, 0, 3);
            if (currentEarnedStars <= 0)
            {
                StopAndReset();
                return;
            }

            CaptureBaseState();
            if (loopRoutine != null) StopCoroutine(loopRoutine);
            if (loopGroup != null)
            {
                loopGroup.alpha = 1f;
                loopGroup.interactable = false;
                loopGroup.blocksRaycasts = false;
            }

            if (mainStar != null)
            {
                mainStar.gameObject.SetActive(true);
                mainStar.localScale = mainBaseScale * Mathf.Lerp(0.92f, 1.12f, currentEarnedStars / 3f);
            }

            if (mainStarImage != null)
            {
                Color color = mainBaseColor;
                // The real UIStarDisplay reveals the earned stars first.  This
                // decorative layer stays hidden until its configured delay passes.
                color.a = 0f;
                mainStarImage.color = color;
            }

            loopRoutine = StartCoroutine(LoopRoutine());
        }

        public void StopAndReset()
        {
            if (loopRoutine != null) StopCoroutine(loopRoutine);
            loopRoutine = null;

            if (loopGroup != null)
            {
                loopGroup.alpha = 0f;
                loopGroup.interactable = false;
                loopGroup.blocksRaycasts = false;
            }

            if (mainStar != null)
            {
                mainStar.localScale = mainBaseScale;
                mainStar.localRotation = mainBaseRotation;
            }

            if (mainStarImage != null)
            {
                Color color = mainBaseColor;
                color.a = 0f;
                mainStarImage.color = color;
            }

            for (int i = 0; i < floatingStars.Length; i++)
            {
                RectTransform star = floatingStars[i];
                if (star == null) continue;

                star.anchoredPosition = i < basePositions.Length ? basePositions[i] : star.anchoredPosition;
                star.localScale = i < baseScales.Length ? baseScales[i] : Vector3.one;
                star.localRotation = i < baseRotations.Length ? baseRotations[i] : Quaternion.identity;

                Image image = GetFloatingImage(i);
                if (image == null) continue;
                Color color = i < baseColors.Length ? baseColors[i] : image.color;
                color.a = 0f;
                image.color = color;
            }
        }

        private IEnumerator LoopRoutine()
        {
            ResetFloatingAlpha();

            if (startDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(startDelay);
            }

            if (mainStarImage != null)
            {
                Color color = mainBaseColor;
                color.a = 0.86f;
                mainStarImage.color = color;
            }

            float time = 0f;
            while (true)
            {
                time += Time.unscaledDeltaTime;
                float intensity = Mathf.Lerp(0.9f, 1.16f, currentEarnedStars / 3f);

                for (int i = 0; i < floatingStars.Length; i++)
                {
                    RectTransform star = floatingStars[i];
                    Image image = GetFloatingImage(i);
                    if (star == null || image == null) continue;

                    float phaseOffset = i * (loopDuration / Mathf.Max(1, floatingStars.Length)) * 0.72f;
                    float local = Mathf.Repeat(time + phaseOffset, loopDuration);
                    bool traveling = local <= travelDuration;

                    if (!traveling)
                    {
                        SetImageAlpha(image, 0f);
                        continue;
                    }

                    float t = Mathf.Clamp01(local / travelDuration);
                    float eased = Mathf.SmoothStep(0f, 1f, t);
                    Vector2 offset = GetTravelOffset(i) * intensity;
                    Vector2 basePosition = i < basePositions.Length ? basePositions[i] : star.anchoredPosition;
                    star.anchoredPosition = basePosition + offset * eased;

                    float grow = t < 0.35f
                        ? Mathf.Lerp(startScale, peakScale, t / 0.35f)
                        : Mathf.Lerp(peakScale, 0.72f, (t - 0.35f) / 0.65f);
                    star.localScale = (i < baseScales.Length ? baseScales[i] : Vector3.one) * grow;
                    float direction = i % 2 == 0 ? -1f : 1f;
                    star.localRotation = (i < baseRotations.Length ? baseRotations[i] : Quaternion.identity)
                        * Quaternion.Euler(0f, 0f, direction * rotationDegrees * eased);

                    float alpha = t < 0.18f
                        ? Mathf.InverseLerp(0f, 0.18f, t)
                        : 1f - Mathf.SmoothStep(0.58f, 1f, t);
                    SetImageAlpha(image, alpha);
                }

                if (mainStar != null)
                {
                    float wave = Mathf.Sin(Time.unscaledTime * 2.2f);
                    mainStar.localRotation = mainBaseRotation * Quaternion.Euler(0f, 0f, wave * 4f);
                    mainStar.localScale = mainBaseScale * (Mathf.Lerp(0.92f, 1.12f, currentEarnedStars / 3f) + wave * 0.025f);
                }

                yield return null;
            }
        }

        private void ResetFloatingAlpha()
        {
            for (int i = 0; i < floatingStars.Length; i++)
            {
                Image image = GetFloatingImage(i);
                if (image != null) SetImageAlpha(image, 0f);
            }
        }

        private Image GetFloatingImage(int index)
        {
            if (index < floatingStarImages.Length && floatingStarImages[index] != null) return floatingStarImages[index];
            if (index < floatingStars.Length && floatingStars[index] != null) return floatingStars[index].GetComponent<Image>();
            return null;
        }

        private Vector2 GetTravelOffset(int index)
        {
            if (travelOffsets != null && index < travelOffsets.Length && travelOffsets[index] != Vector2.zero)
            {
                return travelOffsets[index];
            }

            return DefaultOffsets[index % DefaultOffsets.Length];
        }

        private static void SetImageAlpha(Image image, float alpha)
        {
            Color color = image.color;
            color.a = Mathf.Clamp01(alpha);
            image.color = color;
        }

        private void CaptureBaseState()
        {
            int count = floatingStars != null ? floatingStars.Length : 0;
            if (basePositions.Length != count)
            {
                basePositions = new Vector2[count];
                baseScales = new Vector3[count];
                baseRotations = new Quaternion[count];
                baseColors = new Color[count];
            }

            for (int i = 0; i < count; i++)
            {
                RectTransform star = floatingStars[i];
                if (star == null) continue;

                basePositions[i] = star.anchoredPosition;
                baseScales[i] = star.localScale;
                baseRotations[i] = star.localRotation;
                Image image = GetFloatingImage(i);
                baseColors[i] = image != null ? image.color : Color.white;
            }

            if (mainStar != null)
            {
                mainBaseScale = mainStar.localScale;
                mainBaseRotation = mainStar.localRotation;
            }

            if (mainStarImage != null) mainBaseColor = mainStarImage.color;
        }

        private void OnDisable()
        {
            StopAndReset();
        }
    }
}
