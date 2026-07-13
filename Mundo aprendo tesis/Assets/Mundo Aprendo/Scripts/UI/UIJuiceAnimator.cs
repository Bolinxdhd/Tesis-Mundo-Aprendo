using UnityEngine;
using UnityEngine.UI;

namespace Bolin
{
    public class UIJuiceAnimator : MonoBehaviour
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private Graphic graphic;
        [SerializeField] private bool floatMotion = true;
        [SerializeField] private bool softRotation;
        [SerializeField] private bool twinkle;
        [SerializeField, Min(0f)] private float floatDistance = 8f;
        [SerializeField, Min(0.1f)] private float speed = 0.8f;
        [SerializeField, Min(0f)] private float scaleAmount = 0.025f;
        [SerializeField, Min(0f)] private float rotationDegrees = 2.5f;
        [SerializeField] private float phaseOffset;
        [SerializeField, Range(0f, 1f)] private float minimumAlpha = 0.55f;

        private Vector2 basePosition;
        private Vector3 baseScale;
        private Quaternion baseRotation;
        private Color baseColor = Color.white;

        private void Awake()
        {
            if (target == null) target = transform as RectTransform;
            if (graphic == null) graphic = GetComponent<Graphic>();
            CaptureBaseState();
        }

        private void OnEnable()
        {
            CaptureBaseState();
        }

        private void Update()
        {
            if (target == null) return;

            float wave = Mathf.Sin(Time.unscaledTime * speed + phaseOffset);
            if (floatMotion)
            {
                target.anchoredPosition = basePosition + Vector2.up * (wave * floatDistance);
                target.localScale = baseScale * (1f + wave * scaleAmount);
            }

            if (softRotation)
            {
                target.localRotation = baseRotation * Quaternion.Euler(0f, 0f, wave * rotationDegrees);
            }

            if (twinkle && graphic != null)
            {
                Color color = baseColor;
                color.a = Mathf.Lerp(minimumAlpha, baseColor.a, (wave + 1f) * 0.5f);
                graphic.color = color;
            }
        }

        public void PlaySuccessReaction()
        {
            if (target == null) return;
            target.localScale = baseScale * 1.08f;
        }

        public void PlayRetryReaction()
        {
            if (target == null) return;
            target.localRotation = baseRotation * Quaternion.Euler(0f, 0f, rotationDegrees * 1.8f);
        }

        private void CaptureBaseState()
        {
            if (target == null) return;
            basePosition = target.anchoredPosition;
            baseScale = target.localScale;
            baseRotation = target.localRotation;
            if (graphic != null) baseColor = graphic.color;
        }

        private void OnDisable()
        {
            if (target == null) return;
            target.anchoredPosition = basePosition;
            target.localScale = baseScale;
            target.localRotation = baseRotation;
            if (graphic != null) graphic.color = baseColor;
        }
    }
}
