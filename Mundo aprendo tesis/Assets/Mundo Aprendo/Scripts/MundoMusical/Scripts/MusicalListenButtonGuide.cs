using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Bolin
{
    /// <summary>
    /// Presentation-only guide for the first action after the musical tutorial.
    /// It reuses the persistent EscucharTutorialGlow image authored in the scene;
    /// no UI objects are created while the game runs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MusicalListenButtonGuide : MonoBehaviour
    {
        [SerializeField] private Button listenButton;
        [SerializeField] private RectTransform glowRoot;
        [SerializeField] private Image glowImage;

        [Header("Color del indicador")]
        [Tooltip("Color del halo que señala el botón Escuchar.")]
        [SerializeField] private Color glowColor = new(1f, 0.84f, 0.15f, 1f);
        [Tooltip("Color del marco que perfila el indicador.")]
        [SerializeField] private Color borderColor = new(1f, 0.93f, 0.42f, 1f);

        [Header("Brillo")]
        [Tooltip("Hace el halo más tenue o más intenso sin modificar el ritmo de la animación.")]
        [SerializeField, Range(0f, 1f)] private float glowIntensity = 1f;
        [Tooltip("Extiende el halo hacia afuera del botón. Un valor mayor crea un brillo más amplio.")]
        [SerializeField, Range(0f, 48f)] private float glowExpansion = 12f;

        [Header("Marco")]
        [Tooltip("Grosor del perfil amarillo alrededor del indicador. Usa 0 para ocultarlo.")]
        [SerializeField, Range(0f, 12f)] private float borderThickness = 3f;
        [Tooltip("Transparencia del marco que perfila el indicador.")]
        [SerializeField, Range(0f, 1f)] private float borderOpacity = 0.9f;
        [SerializeField] private Outline glowOutline;

        [Header("Pulso")]
        [SerializeField, Min(0.2f)] private float pulseDuration = 0.82f;
        [SerializeField, Range(1f, 1.25f)] private float peakScale = 1.13f;
        [SerializeField, Range(0f, 1f)] private float minimumAlpha = 0.34f;
        [SerializeField, Range(0f, 1f)] private float maximumAlpha = 0.96f;

        private Coroutine pulseRoutine;
        private Vector3 baseScale = Vector3.one;
        private Vector2 baseSize;
        private bool baseLayoutCaptured;

        public bool IsGuiding => pulseRoutine != null;

        private void Awake()
        {
            ResolveReferences();
            CaptureBaseState();
            HideGuide();
        }

        private void OnEnable()
        {
            ResolveReferences();
            CaptureBaseState();
            HideGuide();
        }

        public void ShowGuide()
        {
            ResolveReferences();
            if (glowImage == null || glowRoot == null) return;

            CaptureBaseState();
            StopPulse();
            ApplyVisualStyle();
            glowImage.gameObject.SetActive(true);
            pulseRoutine = StartCoroutine(PulseRoutine());
        }

        public void HideGuide()
        {
            StopPulse();
            if (glowRoot != null)
            {
                glowRoot.localScale = baseScale;
                if (baseLayoutCaptured) glowRoot.sizeDelta = baseSize;
            }

            if (glowImage == null) return;
            Color hidden = glowColor;
            hidden.a = 0f;
            glowImage.color = hidden;
            glowImage.gameObject.SetActive(false);
        }

        private IEnumerator PulseRoutine()
        {
            while (true)
            {
                if (listenButton != null && !listenButton.gameObject.activeInHierarchy) break;

                float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / pulseDuration);
                if (glowRoot != null)
                {
                    glowRoot.localScale = Vector3.Lerp(baseScale, baseScale * peakScale, wave);
                }

                if (glowImage != null)
                {
                    Color color = glowColor;
                    color.a = Mathf.Clamp01(Mathf.Lerp(minimumAlpha, maximumAlpha, wave) * glowIntensity);
                    glowImage.color = color;
                }

                yield return null;
            }

            pulseRoutine = null;
            HideGuide();
        }

        private void ResolveReferences()
        {
            if (listenButton == null) listenButton = GetComponent<Button>();
            if (glowRoot == null)
            {
                Transform glow = transform.parent != null ? transform.parent.Find("EscucharTutorialGlow") : null;
                glowRoot = glow as RectTransform;
            }

            if (glowImage == null && glowRoot != null) glowImage = glowRoot.GetComponent<Image>();
            if (glowOutline == null && glowRoot != null) glowOutline = glowRoot.GetComponent<Outline>();
        }

        private void CaptureBaseState()
        {
            if (glowRoot != null) baseScale = Vector3.one;
            if (glowRoot != null && !baseLayoutCaptured)
            {
                baseSize = glowRoot.sizeDelta;
                baseLayoutCaptured = true;
            }
        }

        private void ApplyVisualStyle()
        {
            if (glowRoot != null && baseLayoutCaptured)
            {
                glowRoot.sizeDelta = baseSize + Vector2.one * (glowExpansion * 2f);
            }

            if (glowOutline == null) return;
            Color outlineColor = borderColor;
            outlineColor.a *= borderOpacity;
            glowOutline.effectColor = outlineColor;
            glowOutline.effectDistance = Vector2.one * borderThickness;
            glowOutline.enabled = borderThickness > 0.01f && borderOpacity > 0.01f;
        }

        private void StopPulse()
        {
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }

        private void OnDisable()
        {
            HideGuide();
        }
    }
}
