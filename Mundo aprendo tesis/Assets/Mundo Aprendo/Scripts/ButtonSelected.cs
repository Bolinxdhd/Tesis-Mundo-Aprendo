using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Bolin
{

    // Componente que requiere que el GameObject tenga un componente Button
    [RequireComponent(typeof(Button))]

    // Se emplea para asignar el texto del botón seleccionado en el menú de selección de escenas
    public class ButtonSelected : MonoBehaviour
    {

        // Factor de zoom para el botón seleccionado
        [SerializeField] private float zoomFactor = 1.2f;

        // Velocidad de zoom para el botón seleccionado
        [SerializeField] private float zoomSpeed = 5f;


        // Escala original del botón
        private Vector3 originalScale;

        // Referencia a la coroutine de zoom
        private Coroutine zoomCoroutine;



        // Mtodo de llamada de Unity al inicializar el script
        private void Awake()
        {
            originalScale = transform.localScale;
        }


        // Aplica el zoom al botón seleccionado
        public void ZoomInButton()
        {
            if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
            // Se inicia la coroutine para escalar el botón y cambiar el tamaño de fuente
            zoomCoroutine = StartCoroutine(ScaleButton(originalScale * zoomFactor));
        }


        // Restaura el tamaño original
        public void ZoomOutButton()
        {
            if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
            // Se inicia la coroutine para restaurar la escala y el tamaño de fuente originales
            zoomCoroutine = StartCoroutine(ScaleButton(originalScale));
        }


        // Coroutine que interpola escala y texto
        private IEnumerator ScaleButton(Vector3 targetScale)
        {
            Event();
            Vector3 startScale = transform.localScale;
            float t = 0f;
            // Interpolacin
            while (t < 1f)
            {
                t += Time.deltaTime * zoomSpeed;
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }
            // Asegura que los valores finales sean exactos
            transform.localScale = targetScale;
        }
        protected virtual void Event()
        {

        }
    }
}
