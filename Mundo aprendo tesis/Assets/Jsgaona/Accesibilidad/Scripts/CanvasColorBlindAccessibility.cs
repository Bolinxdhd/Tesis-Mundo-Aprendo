using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Jsgaona
{
    public class CanvasColorBlindAccessibility : MonoBehaviour
    {
        private const string PlayerPrefsModeKey = "Accessibility.ColorBlindMode";

        private enum ColorBlindMode
        {
            Normal = 0,
            Protanopia = 1,
            Deuteranopia = 2,
            Tritanopia = 3,
            AltoContraste = 4
        }

        [Header("Referencias de escena")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private TMP_Dropdown colorBlindDropdown;
        [SerializeField] private Graphic[] targetGraphics = System.Array.Empty<Graphic>();

        private readonly Dictionary<Graphic, Color> originalGraphicColors = new();
        private ColorBlindMode currentMode = ColorBlindMode.Normal;

        private void Start()
        {
            CaptureOriginalGraphicColors();
            SetupDropdown();
            LoadSavedMode();
            ApplyMode();
        }

        private void OnDestroy()
        {
            if (colorBlindDropdown != null)
            {
                colorBlindDropdown.onValueChanged.RemoveListener(SetMode);
            }
        }

        private void SetupDropdown()
        {
            if (colorBlindDropdown == null) return;

            colorBlindDropdown.onValueChanged.RemoveListener(SetMode);
            colorBlindDropdown.options.Clear();
            colorBlindDropdown.options.Add(new TMP_Dropdown.OptionData("Normal"));
            colorBlindDropdown.options.Add(new TMP_Dropdown.OptionData("Protanopia"));
            colorBlindDropdown.options.Add(new TMP_Dropdown.OptionData("Deuteranopia"));
            colorBlindDropdown.options.Add(new TMP_Dropdown.OptionData("Tritanopia"));
            colorBlindDropdown.options.Add(new TMP_Dropdown.OptionData("Alto contraste"));
            colorBlindDropdown.onValueChanged.AddListener(SetMode);
        }

        private void LoadSavedMode()
        {
            int savedMode = PlayerPrefs.GetInt(PlayerPrefsModeKey, 0);
            currentMode = (ColorBlindMode)Mathf.Clamp(savedMode, 0, 4);

            if (colorBlindDropdown != null)
            {
                colorBlindDropdown.SetValueWithoutNotify((int)currentMode);
                colorBlindDropdown.RefreshShownValue();
            }
        }

        private void SetMode(int modeIndex)
        {
            currentMode = (ColorBlindMode)Mathf.Clamp(modeIndex, 0, 4);
            PlayerPrefs.SetInt(PlayerPrefsModeKey, (int)currentMode);
            PlayerPrefs.Save();
            ApplyMode();
        }

        private void CaptureOriginalGraphicColors()
        {
            originalGraphicColors.Clear();
            foreach (Graphic graphic in targetGraphics)
            {
                if (graphic != null && (graphic is Image || graphic is RawImage))
                {
                    originalGraphicColors[graphic] = graphic.color;
                }
            }
        }

        private void OnValidate()
        {
            if (targetCanvas == null) targetCanvas = GetComponent<Canvas>();
        }

        private void ApplyMode()
        {
            foreach (KeyValuePair<Graphic, Color> item in originalGraphicColors)
            {
                if (item.Key == null) continue;
                item.Key.color = ConvertColor(item.Value, currentMode);
            }
        }

        private static Color ConvertColor(Color color, ColorBlindMode mode)
        {
            if (mode == ColorBlindMode.Normal) return color;

            Vector3 original = new(color.r, color.g, color.b);
            Vector3 filtered = mode switch
            {
                ColorBlindMode.Protanopia => ApplyMatrix(
                    original,
                    new Vector3(0.567f, 0.433f, 0f),
                    new Vector3(0.558f, 0.442f, 0f),
                    new Vector3(0f, 0.242f, 0.758f)),
                ColorBlindMode.Deuteranopia => ApplyMatrix(
                    original,
                    new Vector3(0.625f, 0.375f, 0f),
                    new Vector3(0.700f, 0.300f, 0f),
                    new Vector3(0f, 0.300f, 0.700f)),
                ColorBlindMode.Tritanopia => ApplyMatrix(
                    original,
                    new Vector3(0.950f, 0.050f, 0f),
                    new Vector3(0f, 0.433f, 0.567f),
                    new Vector3(0f, 0.475f, 0.525f)),
                ColorBlindMode.AltoContraste => ApplyHighContrast(original),
                _ => original
            };

            return new Color(
                Mathf.Clamp01(filtered.x),
                Mathf.Clamp01(filtered.y),
                Mathf.Clamp01(filtered.z),
                color.a);
        }

        private static Vector3 ApplyMatrix(Vector3 color, Vector3 r0, Vector3 r1, Vector3 r2)
        {
            return new Vector3(
                Vector3.Dot(r0, color),
                Vector3.Dot(r1, color),
                Vector3.Dot(r2, color));
        }

        private static Vector3 ApplyHighContrast(Vector3 color)
        {
            float luminance = Vector3.Dot(color, new Vector3(0.299f, 0.587f, 0.114f));
            float contrast = luminance >= 0.5f ? 1f : 0f;
            return Vector3.Lerp(new Vector3(luminance, luminance, luminance), new Vector3(contrast, contrast, contrast), 0.65f);
        }
    }
}
