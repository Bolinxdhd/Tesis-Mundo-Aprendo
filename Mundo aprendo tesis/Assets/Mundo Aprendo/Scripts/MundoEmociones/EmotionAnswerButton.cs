using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolin
{
    // Boton de respuesta del mundo de emociones; envia su EmotionType al manager.
    public class EmotionAnswerButton : MonoBehaviour
    {
        [SerializeField] private EmotionGameManager gameManager;
        [SerializeField] private EmotionType emotion;
        [SerializeField] private Button button;
        [SerializeField] private RectTransform pulseTarget;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text labelText;
        [SerializeField, Min(0.05f)] private float pulseDuration = 0.12f;
        [SerializeField, Range(1f, 1.2f)] private float pulseScale = 1.06f;

        private Coroutine pulseRoutine;

        public EmotionType Emotion => emotion;
        public Button Button => button;

        public void Submit()
        {
            // Evento del boton: delega la validacion al EmotionGameManager.
            gameManager?.SubmitAnswer(emotion);
        }

        public void SetInteractable(bool interactable)
        {
            // El manager lo usa para bloquear respuestas durante feedback o transiciones.
            if (button != null) button.interactable = interactable;
        }

        public void SetVisible(bool visible)
        {
            // Permite ocultar respuestas opcionales como miedo.
            gameObject.SetActive(visible);
        }

        public void Pulse()
        {
            // Feedback visual cuando el alumno toca esta respuesta.
            if (!isActiveAndEnabled || pulseTarget == null) return;

            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
            }

            pulseRoutine = StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            // Escala el boton hacia arriba y luego lo devuelve a su tamano normal.
            Vector3 baseScale = Vector3.one;
            Vector3 enlargedScale = Vector3.one * pulseScale;
            float elapsed = 0f;

            while (elapsed < pulseDuration)
            {
                elapsed += Time.deltaTime;
                pulseTarget.localScale = Vector3.Lerp(baseScale, enlargedScale, Mathf.Clamp01(elapsed / pulseDuration));
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < pulseDuration)
            {
                elapsed += Time.deltaTime;
                pulseTarget.localScale = Vector3.Lerp(enlargedScale, baseScale, Mathf.Clamp01(elapsed / pulseDuration));
                yield return null;
            }

            pulseTarget.localScale = baseScale;
            pulseRoutine = null;
        }

        private void OnDisable()
        {
            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
                pulseRoutine = null;
            }

            if (pulseTarget != null) pulseTarget.localScale = Vector3.one;
        }

        private void OnValidate()
        {
            if (button == null) Debug.LogWarning($"{name}: falta asignar Button.", this);
            if (gameManager == null) Debug.LogWarning($"{name}: falta asignar EmotionGameManager.", this);
        }
    }
}
