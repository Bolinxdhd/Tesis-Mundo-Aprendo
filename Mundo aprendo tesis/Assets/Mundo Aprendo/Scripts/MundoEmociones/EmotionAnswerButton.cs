using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolin
{
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
            gameManager?.SubmitAnswer(emotion);
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null) button.interactable = interactable;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void Pulse()
        {
            if (!isActiveAndEnabled || pulseTarget == null) return;

            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
            }

            pulseRoutine = StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
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
