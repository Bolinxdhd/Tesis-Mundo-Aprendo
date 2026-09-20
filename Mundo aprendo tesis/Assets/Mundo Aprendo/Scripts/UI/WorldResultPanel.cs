using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Bolin
{
    /// <summary>
    /// Panel de resultado compartido por todos los mundos de Mundo Aprendo.
    /// La escena solo aporta las referencias visuales; cada mundo decide que
    /// texto mostrar y que botones necesita. Asi los cuatro mundos se ven y se
    /// comportan igual sin duplicar codigo en cada controlador.
    /// </summary>
    [DisallowMultipleComponent]
    public class WorldResultPanel : MonoBehaviour
    {
        /// <summary>Acciones que puede ofrecer el panel. Cada mundo usa solo las que necesita.</summary>
        public enum ResultAction
        {
            Retry,
            Next,
            BackToList,
            WorldSelection
        }

        [Header("Estructura")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform card;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private UIStarDisplay starDisplay;
        [SerializeField] private StoryResultStarAnimation decoration;

        [Header("Botones (opcionales por mundo)")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button backToListButton;
        [SerializeField] private Button worldSelectionButton;

        [Header("Contenido por defecto")]
        [SerializeField] private string defaultTitle = "Actividad completada";

        [Header("Ritmo de la animacion")]
        [SerializeField, Min(0.05f)] private float fadeDuration = 0.26f;
        [SerializeField, Range(0.7f, 1f)] private float hiddenCardScale = 0.86f;
        [SerializeField, Range(1f, 1.3f)] private float cardOvershoot = 1.04f;
        [SerializeField, Min(0f)] private float starsDelay = 0.18f;
        [SerializeField, Min(0f)] private float buttonsDelay = 0.1f;
        [SerializeField, Min(0f)] private float delayBetweenButtons = 0.07f;

        [Header("Audio")]
        [SerializeField] private AudioClip showClip;
        [SerializeField, Range(0f, 1f)] private float showVolume = 0.55f;

        private static readonly ResultAction[] ButtonOrder =
        {
            ResultAction.Retry,
            ResultAction.Next,
            ResultAction.BackToList,
            ResultAction.WorldSelection
        };

        private Coroutine presentationRoutine;
        private bool baseStateCaptured;
        private Vector3 cardBaseScale = Vector3.one;

        /// <summary>True mientras el panel esta visible para el jugador.</summary>
        public bool IsVisible => gameObject.activeSelf;

        private void Awake()
        {
            CacheReferences();
            CaptureBaseState();
            ApplyHiddenState();
        }

        private void OnDisable()
        {
            StopPresentation();
        }

        /// <summary>
        /// Conecta una accion del panel. Pasar null oculta ese boton, que es como
        /// cada mundo elige cuantos botones muestra.
        /// </summary>
        public void Bind(ResultAction action, UnityAction callback)
        {
            Button button = GetButton(action);
            if (button == null) return;

            button.onClick.RemoveAllListeners();
            if (callback == null)
            {
                button.gameObject.SetActive(false);
                return;
            }

            button.onClick.AddListener(callback);
            button.gameObject.SetActive(true);
            button.interactable = true;
        }

        /// <summary>Cambia el texto visible de un boton sin tocar su accion.</summary>
        public void SetLabel(ResultAction action, string label)
        {
            Button button = GetButton(action);
            if (button == null || string.IsNullOrEmpty(label)) return;

            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null) text.text = label;
        }

        /// <summary>Muestra el resultado con la animacion completa.</summary>
        public void Show(int stars, string message, string title = null)
        {
            CacheReferences();
            CaptureBaseState();

            if (titleText != null) titleText.text = string.IsNullOrWhiteSpace(title) ? defaultTitle : title;
            if (messageText != null) messageText.text = message ?? string.Empty;

            gameObject.SetActive(true);
            StopPresentation();

            int clampedStars = Mathf.Clamp(stars, 0, 3);
            if (!isActiveAndEnabled)
            {
                // Sin componente activo no hay corutinas: se deja el estado final aplicado.
                ApplyVisibleState(clampedStars);
                return;
            }

            AudioManager.TryPlaySfx(showClip, showVolume);
            presentationRoutine = StartCoroutine(ShowRoutine(clampedStars));
        }

        /// <summary>Oculta el panel con un fundido corto.</summary>
        public void Hide()
        {
            if (!gameObject.activeSelf) return;

            StopPresentation();
            if (!isActiveAndEnabled)
            {
                HideImmediate();
                return;
            }

            presentationRoutine = StartCoroutine(HideRoutine());
        }

        /// <summary>Oculta el panel sin animacion (estado inicial de la escena).</summary>
        public void HideImmediate()
        {
            StopPresentation();
            if (starDisplay != null) starDisplay.SetImmediate(0);
            if (decoration != null) decoration.StopAndReset();
            ApplyHiddenState();
            gameObject.SetActive(false);
        }

        private IEnumerator ShowRoutine(int stars)
        {
            SetButtonsInteractable(false);
            SetButtonsVisualHidden();

            // El loop decorativo tiene su propio retardo interno, por eso arranca
            // junto con el panel y no despues de las estrellas reales.
            if (decoration != null) decoration.Play(stars);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = true;
            }

            if (card != null) card.localScale = cardBaseScale * hiddenCardScale;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                if (canvasGroup != null) canvasGroup.alpha = t;
                if (card != null) card.localScale = cardBaseScale * EvaluateEntranceScale(t);
                yield return null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (card != null) card.localScale = cardBaseScale;

            if (starsDelay > 0f) yield return new WaitForSecondsRealtime(starsDelay);
            if (starDisplay != null) starDisplay.ShowStars(stars);

            if (buttonsDelay > 0f) yield return new WaitForSecondsRealtime(buttonsDelay);
            yield return RevealButtonsRoutine();

            presentationRoutine = null;
        }

        private IEnumerator HideRoutine()
        {
            SetButtonsInteractable(false);
            if (starDisplay != null) starDisplay.SetImmediate(0);
            if (decoration != null) decoration.StopAndReset();

            float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
            Vector3 startScale = card != null ? card.localScale : cardBaseScale;
            Vector3 targetScale = cardBaseScale * hiddenCardScale;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                if (card != null) card.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            presentationRoutine = null;
            HideImmediate();
        }

        private IEnumerator RevealButtonsRoutine()
        {
            foreach (ResultAction action in ButtonOrder)
            {
                Button button = GetButton(action);
                if (button == null || !button.gameObject.activeSelf) continue;

                yield return PopButtonRoutine(button);
                if (delayBetweenButtons > 0f) yield return new WaitForSecondsRealtime(delayBetweenButtons);
            }

            SetButtonsInteractable(true);
        }

        private IEnumerator PopButtonRoutine(Button button)
        {
            RectTransform rect = button.transform as RectTransform;
            if (rect == null) yield break;

            const float duration = 0.16f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rect.localScale = Vector3.one * EvaluateEntranceScale(t);
                yield return null;
            }

            rect.localScale = Vector3.one;
        }

        /// <summary>Entrada con rebote corto: crece por encima de 1 y vuelve a su escala real.</summary>
        private float EvaluateEntranceScale(float t)
        {
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            return t < 0.75f
                ? Mathf.Lerp(hiddenCardScale, cardOvershoot, eased / 0.75f)
                : Mathf.Lerp(cardOvershoot, 1f, (t - 0.75f) / 0.25f);
        }

        private void ApplyVisibleState(int stars)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (card != null) card.localScale = cardBaseScale;
            if (starDisplay != null) starDisplay.SetImmediate(stars);
            if (decoration != null) decoration.Play(stars);
            SetButtonsInteractable(true);
        }

        private void ApplyHiddenState()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (card != null) card.localScale = cardBaseScale * hiddenCardScale;
        }

        private void SetButtonsVisualHidden()
        {
            foreach (ResultAction action in ButtonOrder)
            {
                Button button = GetButton(action);
                if (button == null || !button.gameObject.activeSelf) continue;
                if (button.transform is RectTransform rect) rect.localScale = Vector3.zero;
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            foreach (ResultAction action in ButtonOrder)
            {
                Button button = GetButton(action);
                if (button != null) button.interactable = interactable;
            }
        }

        private Button GetButton(ResultAction action)
        {
            switch (action)
            {
                case ResultAction.Retry: return retryButton;
                case ResultAction.Next: return nextButton;
                case ResultAction.BackToList: return backToListButton;
                case ResultAction.WorldSelection: return worldSelectionButton;
                default: return null;
            }
        }

        private void StopPresentation()
        {
            if (presentationRoutine != null) StopCoroutine(presentationRoutine);
            presentationRoutine = null;
        }

        private void CacheReferences()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (starDisplay == null) starDisplay = GetComponentInChildren<UIStarDisplay>(true);
            if (decoration == null) decoration = GetComponentInChildren<StoryResultStarAnimation>(true);
        }

        private void CaptureBaseState()
        {
            if (baseStateCaptured || card == null) return;
            cardBaseScale = card.localScale.sqrMagnitude < 0.0001f ? Vector3.one : card.localScale;
            baseStateCaptured = true;
        }
    }
}
