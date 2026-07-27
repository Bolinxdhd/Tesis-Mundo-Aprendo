using UnityEngine;

namespace Bolin
{
    /// <summary>
    /// Animation data for one world portal card. It has no Update method; the
    /// four instances are ticked by SelectionWorldsUIAnimator to avoid
    /// competing UI transform writers and per-card animation loops.
    /// </summary>
    public sealed class WorldCardAnimator : MonoBehaviour
    {
        [SerializeField] private RectTransform motionRoot;
        [SerializeField] private RectTransform planetBody;
        [SerializeField] private RectTransform orbitBack;
        [SerializeField] private RectTransform orbitFront;
        [SerializeField] private RectTransform mainIcon;
        [SerializeField] private RectTransform worldLabel;
        [SerializeField] private CanvasGroup glow;
        [SerializeField] private RectTransform[] decorationRects;
        [SerializeField] private CanvasGroup[] decorationGroups;
        [SerializeField, Min(2.5f)] private float idleDuration = 3.2f;
        [SerializeField] private float phaseOffset;
        [SerializeField] private bool rotatePlanetBody = true;
        [SerializeField, Min(0f)] private float entranceDelay;

        private Vector2 baseMotionPosition;
        private Vector2 baseLabelPosition;
        private Vector2[] baseDecorationPositions;
        private bool cached;
        private bool hovered;
        private bool pressed;
        private bool locked;
        private float elapsed;
        private float hoverBlend;
        private float rejectionRemaining;

        public void Configure(
            RectTransform configuredMotionRoot,
            RectTransform configuredPlanetBody,
            RectTransform configuredOrbitBack,
            RectTransform configuredOrbitFront,
            RectTransform configuredMainIcon,
            RectTransform configuredWorldLabel,
            CanvasGroup configuredGlow,
            RectTransform[] configuredDecorationRects,
            CanvasGroup[] configuredDecorationGroups,
            float configuredIdleDuration,
            float configuredPhaseOffset,
            bool configuredRotatePlanetBody,
            float configuredEntranceDelay)
        {
            motionRoot = configuredMotionRoot;
            planetBody = configuredPlanetBody;
            orbitBack = configuredOrbitBack;
            orbitFront = configuredOrbitFront;
            mainIcon = configuredMainIcon;
            worldLabel = configuredWorldLabel;
            glow = configuredGlow;
            decorationRects = configuredDecorationRects;
            decorationGroups = configuredDecorationGroups;
            idleDuration = Mathf.Max(2.5f, configuredIdleDuration);
            phaseOffset = configuredPhaseOffset;
            rotatePlanetBody = configuredRotatePlanetBody;
            entranceDelay = Mathf.Max(0f, configuredEntranceDelay);
            cached = false;
            hovered = false;
            pressed = false;
            locked = false;
            elapsed = 0f;
            hoverBlend = 0f;
            rejectionRemaining = 0f;
        }

        public void SetHovered(bool value)
        {
            hovered = value;
        }

        public void SetPressed(bool value)
        {
            pressed = value;
        }

        public void SetLocked(bool value)
        {
            locked = value;
        }

        public void PlayLockedFeedback()
        {
            rejectionRemaining = 0.26f;
        }

        public void Tick(float unscaledDeltaTime)
        {
            CacheBaseTransforms();
            if (motionRoot == null) return;

            elapsed += Mathf.Max(0f, unscaledDeltaTime);
            float targetHover = hovered ? 1f : 0f;
            hoverBlend = Mathf.MoveTowards(hoverBlend, targetHover, unscaledDeltaTime * 5.2f);

            float cycle = elapsed / Mathf.Max(0.01f, idleDuration) * Mathf.PI * 2f + phaseOffset;
            float bob = Mathf.Sin(cycle) * 5.5f;
            float scalePulse = 1f + Mathf.Sin(cycle + 0.6f) * 0.005f;
            float hoverScale = locked ? 0.012f : 0.025f;
            float entrance = Mathf.Clamp01((elapsed - entranceDelay) / 0.34f);
            float entranceScale = Mathf.Lerp(0.90f, 1f, Mathf.SmoothStep(0f, 1f, entrance));
            float pressedScale = pressed && !locked ? 0.98f : 1f;

            float shake = 0f;
            if (rejectionRemaining > 0f)
            {
                rejectionRemaining = Mathf.Max(0f, rejectionRemaining - unscaledDeltaTime);
                float progress = 1f - rejectionRemaining / 0.26f;
                shake = Mathf.Sin(progress * Mathf.PI * 7f) * (1f - progress) * 10f;
            }

            motionRoot.anchoredPosition = baseMotionPosition + new Vector2(shake, bob);
            motionRoot.localScale = Vector3.one * (entranceScale * pressedScale * scalePulse * (1f + hoverBlend * hoverScale));

            if (planetBody != null && rotatePlanetBody) planetBody.localRotation = Quaternion.Euler(0f, 0f, elapsed * 1.1f);
            if (orbitBack != null) orbitBack.localRotation = Quaternion.Euler(0f, 0f, -elapsed * 1.15f);
            if (orbitFront != null) orbitFront.localRotation = Quaternion.Euler(0f, 0f, elapsed * 1.35f);
            if (mainIcon != null) mainIcon.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(cycle * 1.3f) * 1.25f);
            if (worldLabel != null) worldLabel.anchoredPosition = baseLabelPosition + Vector2.up * (hoverBlend * 4f);
            if (glow != null) glow.alpha = Mathf.SmoothStep(locked ? .08f : .20f, locked ? .25f : .78f, hoverBlend);

            AnimateDecorations(cycle);
        }

        private void CacheBaseTransforms()
        {
            if (cached) return;

            cached = true;
            if (motionRoot != null) baseMotionPosition = motionRoot.anchoredPosition;
            if (worldLabel != null) baseLabelPosition = worldLabel.anchoredPosition;
            int count = decorationRects == null ? 0 : decorationRects.Length;
            baseDecorationPositions = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                if (decorationRects[i] != null) baseDecorationPositions[i] = decorationRects[i].anchoredPosition;
            }
        }

        private void AnimateDecorations(float cycle)
        {
            if (decorationRects == null || decorationGroups == null) return;

            int count = Mathf.Min(decorationRects.Length, decorationGroups.Length);
            for (int i = 0; i < count; i++)
            {
                RectTransform decoration = decorationRects[i];
                CanvasGroup group = decorationGroups[i];
                if (decoration == null || group == null) continue;

                float staggered = Mathf.Repeat(elapsed * 0.42f + phaseOffset * 0.06f + i * 0.19f, 1f);
                float visible = Mathf.Clamp01(Mathf.Sin(staggered * Mathf.PI));
                float alpha = hoverBlend * visible * (locked ? 0.35f : 0.9f);
                group.alpha = alpha;
                Vector2 origin = i < baseDecorationPositions.Length ? baseDecorationPositions[i] : Vector2.zero;
                float side = i % 2 == 0 ? -1f : 1f;
                decoration.anchoredPosition = origin + new Vector2(side * staggered * 18f, staggered * 42f);
                decoration.localScale = Vector3.one * Mathf.Lerp(0.62f, 1.05f, staggered);
                decoration.localRotation = Quaternion.Euler(0f, 0f, side * (staggered * 22f + Mathf.Sin(cycle + i) * 4f));
            }
        }
    }
}
