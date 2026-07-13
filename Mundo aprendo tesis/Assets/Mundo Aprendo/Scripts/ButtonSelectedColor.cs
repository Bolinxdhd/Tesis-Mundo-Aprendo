using UnityEngine;
using UnityEngine.UI;

namespace Bolin {

public class ButtonSelectedColor : ButtonSelected
{
    [SerializeField] private Image buttonImage; // Referencia al componente Image del botón
    [SerializeField] private Color zoomColor = Color.yellow; // Color para el botón seleccionado
    private Color originalColor; // Color original del botón
    private bool IsZoomedIn = false; // Indica si el botón está actualmente ampliado

    private void Start()
    {
        if (buttonImage == null)
        {
            buttonImage = GetComponent<Image>();
        }

        if (buttonImage != null)
        {
            originalColor = buttonImage.color;
        }
    }

    protected override void Event()
    {
        if (buttonImage == null) return;

        IsZoomedIn = !IsZoomedIn;
        if (IsZoomedIn)
        {
            buttonImage.color = zoomColor; // Cambiar al color de zoom
        }
        else
        {
            buttonImage.color = originalColor; // Restaurar el color original
        }
    }
}
}
