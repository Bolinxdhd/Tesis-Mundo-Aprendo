using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolin {

    // Clase abstracta que permite gestionar el control sobre los sliders
    public abstract class SliderUi : MonoBehaviour {

        // Referencia al texto donde se muestra el valor del slider
        [SerializeField] private TMP_Text txtValue;

        // Referencia al componente Slider
        protected Slider slider;



        // Metodo de llamada de Unity, se llama una unica vez al iniciar el aplicativo
        // Se declaran todos los componentes necesarios para el funcionamiento del script
        private void Awake(){
            slider = GetComponent<Slider>();
        }
        
        
        // Metodo de llamada de Unity, se llamada una unica vez al iniciar el app despues de Awake
        // Se inicializa las variables principales de modificacion
        protected virtual void Start() {
            // Asegurarse de que el slider este configurado entre 0 y 10
            slider.minValue = 0.0001f;
            slider.maxValue = 10;
        }


        // Metodo de llamada de Unity, se llama al momento de que el script asociado al objeto
        // es habilitado
        private void OnEnable() {
            slider.onValueChanged.AddListener(OnSliderValueChanged);
        }


        // Metodo de llamada de Unity, se llama al momento de que el script asociado al objeto
        // es deshabilitado o cuando se cierra una escena
        private void OnDisable() {
            slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }


        // Se emplea este metodo para poder incrementar el valor del slider
        public void IncreaseSlider(){
            if(slider.value >= 10) return;
            float value = slider.value + 1;
            OnSliderValueChanged(value);
        }


        // Se emplea este metodo para poder quitar el valor del slider
        public void DecreaseSlider(){
            if(slider.value <= 0.0001f) return;
            float value = slider.value - 1;
            OnSliderValueChanged(value);
        }


        // Este metodo es llamado cuando el valor del slider cambia
        public void OnSliderValueChanged(float value) {
            // Redondear el valor del slider a un numero entero y actualiza
            int roundedValue = Mathf.RoundToInt(value);
            slider.value = value;

            // Mostrar en UI en valor
            if (txtValue != null) txtValue.text = roundedValue.ToString();

            MakeChange(value);
        }


        // Metodo abstracto que se emplea para poder realizar los cambios del slider
        protected abstract void MakeChange(float volume);

        // Metodo que se emplea para poder resetear los valores
        public abstract void Reset();
    }
}