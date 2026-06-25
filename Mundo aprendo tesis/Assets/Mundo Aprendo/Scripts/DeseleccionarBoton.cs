using UnityEngine;
using UnityEngine.EventSystems;


namespace Bolin
{
public class DeseleccionarBoton : MonoBehaviour
{
    public void QuitarSeleccion()
    {
        EventSystem.current.SetSelectedGameObject(null);
    }
}
}