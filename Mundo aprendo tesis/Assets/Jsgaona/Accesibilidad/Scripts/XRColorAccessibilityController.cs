using UnityEngine;

namespace Jsgaona {

    // Este script se puede usar para controlar la accesibilidad de color en aplicaciones XR.
    // Permite a los usuarios seleccionar diferentes modos de daltonismo y ajustar la intensidad del efecto.
    public class XRColorAccessibilityController : MonoBehaviour {
        
        // Lista de modos de daltonismo disponibles.
        // Puedes agregar más modos si tu shader los soporta.
        public enum ColorBlindMode {
            Off = 0,
            Protanopia = 1,
            Deuteranopia = 2,
            Tritanopia = 3,
            HighContrast = 4
        }


        // Material que se usará para aplicar el efecto de accesibilidad de color.
        [SerializeField] private Material accessibilityMaterial;

        // Modo de daltonismo seleccionado por el usuario.
        [SerializeField] private ColorBlindMode currentMode = ColorBlindMode.Off;

        // Intensidad del efecto de accesibilidad de color.
        [SerializeField] [Range(0f, 1f)] private float intensity = 1f;



        // Aplica la configuración inicial al iniciar la aplicación.
        private void Start() {
            ApplySettings();
        }


        // Aplica los cambios en tiempo real cuando se modifican los valores en el Inspector.
        private void OnValidate() {
            ApplySettings();
        }


        // Métodos públicos para cambiar el modo y la intensidad desde otros scripts o UI.
        public void SetMode(int modeIndex) {
            currentMode = (ColorBlindMode)modeIndex;
            ApplySettings();
        }


        // Permite ajustar la intensidad del efecto desde otros scripts o UI.
        public void SetIntensity(float value) {
            intensity = Mathf.Clamp01(value);
            ApplySettings();
        }


        // Aplica los ajustes al material de accesibilidad.
        private void ApplySettings() {
            if (accessibilityMaterial == null) return;
            accessibilityMaterial.SetFloat("_Mode", (float)currentMode);
            accessibilityMaterial.SetFloat("_Intensity", intensity);
        }
    }
}