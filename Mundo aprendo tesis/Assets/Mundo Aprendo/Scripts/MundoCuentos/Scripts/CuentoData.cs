using System.Collections.Generic;
using UnityEngine;

namespace Bolin
{
    [CreateAssetMenu(fileName = "Cuento", menuName = "Mundo Aprendo/Cuentos/Cuento")]
    public class CuentoData : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string titulo;
        [SerializeField] private Sprite portada;
        [SerializeField, TextArea(8, 24)] private string narracionTexto;
        [SerializeField] private AudioClip narracionAudio;
        [SerializeField] private List<PictogramaData> pictogramas = new();

        public string Id => id;
        public string Titulo => titulo;
        public Sprite Portada => portada;
        public string NarracionTexto => narracionTexto;
        public AudioClip NarracionAudio => narracionAudio;
        public IReadOnlyList<PictogramaData> Pictogramas => pictogramas;

        public IEnumerable<string> ConstruirVocabulario()
        {
            if (pictogramas == null) yield break;

            foreach (PictogramaData pictograma in pictogramas)
            {
                if (pictograma == null) continue;
                foreach (string frase in pictograma.EnumerarFrasesDeReconocimiento())
                {
                    yield return frase;
                }
            }
        }

        private void OnValidate()
        {
            id = id?.Trim();
            titulo = titulo?.Trim();
        }
    }
}
