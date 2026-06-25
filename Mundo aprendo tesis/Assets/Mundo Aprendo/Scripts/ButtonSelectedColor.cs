using NUnit.Framework;
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
        originalColor = buttonImage.color; // Guardar el color original
    }
    protected override void Event()
    {
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