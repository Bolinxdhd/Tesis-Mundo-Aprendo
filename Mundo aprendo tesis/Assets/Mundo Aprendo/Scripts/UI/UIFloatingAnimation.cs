using UnityEngine;

namespace Bolin
{
    public class UIFloatingAnimation : MonoBehaviour
    {
        [SerializeField] private RectTransform target;
        [SerializeField, Min(0f)] private float verticalDistance = 6f;
        [SerializeField, Min(0.1f)] private float speed = 0.8f;
        [SerializeField, Range(0f, 0.05f)] private float scaleAmount = 0.012f;
        [SerializeField] private float phaseOffset;

        private Vector2 basePosition;
        private Vector3 baseScale;

        private void Awake()
        {
            if (target == null) target = transform as RectTransform;
            if (target == null) return;
            basePosition = target.anchoredPosition;
            baseScale = target.localScale;
        }

        private void OnEnable()
        {
            if (target == null) return;
            basePosition = target.anchoredPosition;
            baseScale = target.localScale;
        }

        private void Update()
        {
            if (target == null) return;
            float wave = Mathf.Sin(Time.unscaledTime * speed + phaseOffset);
            target.anchoredPosition = basePosition + Vector2.up * (wave * verticalDistance);
            target.localScale = baseScale * (1f + wave * scaleAmount);
        }

        private void OnDisable()
        {
            if (target == null) return;
            target.anchoredPosition = basePosition;
            target.localScale = baseScale;
        }
    }
}
