using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolin
{
    public class UIProgressBar : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_Text valueText;

        public void SetProgress(float normalizedValue, string label = null)
        {
            float clamped = Mathf.Clamp01(normalizedValue);
            if (fillImage != null) fillImage.fillAmount = clamped;
            if (valueText != null) valueText.text = string.IsNullOrWhiteSpace(label) ? $"{Mathf.RoundToInt(clamped * 100f)}%" : label;
        }
    }
}
