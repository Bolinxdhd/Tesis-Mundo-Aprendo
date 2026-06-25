using UnityEngine;
using UnityEngine.Accessibility;

namespace Jsgaona {

    // Se puede usar este script para generar una paleta de colores segura para daltónicos.
    // Ajusta el número de colores y el rango de luminancia según tus necesidades.
    public class ColorSafePaletteGenerator : MonoBehaviour {

        // Número de colores que deseas generar (máximo 12 para la paleta segura de daltónicos).
        [SerializeField] [Range(1, 12)] private int colorCount = 6;

        // Paleta generada (se llenará al hacer clic en "Generate Palette").
        [SerializeField] private Color[] palette;

        [Header("Luminance Range")]
        // Ajusta estos valores para controlar el brillo de los colores generados.
        [Range(0f, 1f)] public float minimumLuminance = 0.2f;

        // El valor máximo de luminancia no debe ser demasiado alto para evitar colores que sean
        // difíciles de distinguir para personas con ciertas formas de daltonismo.
        [Range(0f, 1f)] public float maximumLuminance = 0.8f;



        // Este método se ejecutará al hacer clic en el botón "Generate Palette" en el Inspector.
        [ContextMenu("Generate Palette")]
        public void GeneratePalette() {
            palette = new Color[colorCount];
            // Genera la paleta de colores segura para daltónicos.
            int uniqueColors = VisionUtility.GetColorBlindSafePalette(
                palette,
                minimumLuminance,
                maximumLuminance
            );
            // Imprime información en la consola para verificar los resultados.
            Debug.Log($"Colores solicitados: {colorCount}");
            Debug.Log($"Colores no ambiguos disponibles: {uniqueColors}");

            // Imprime los colores generados en la consola.
            for (int i = 0; i < palette.Length; i++) {
                Debug.Log($"Color {i}: {palette[i]}");
            }
        }
    }
}