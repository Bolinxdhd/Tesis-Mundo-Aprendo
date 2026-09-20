using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bolin
{
    [Serializable]
    public class PictogramaData
    {
        [SerializeField] private string id;
        [SerializeField] private Sprite imagen;
        [SerializeField] private string concepto;
        [SerializeField] private List<string> respuestasAceptadas = new();
        [SerializeField, TextArea(2, 4)] private string ayuda;
        [SerializeField] private AudioClip preguntaAudio;
        [SerializeField] private AudioClip feedbackCorrecto;

        public string Id => id;
        public Sprite Imagen => imagen;
        public string Concepto => concepto;
        public IReadOnlyList<string> RespuestasAceptadas => respuestasAceptadas;
        public string Ayuda => ayuda;
        public AudioClip PreguntaAudio => preguntaAudio;
        public AudioClip FeedbackCorrecto => feedbackCorrecto;

        public IEnumerable<string> EnumerarFrasesDeReconocimiento()
        {
            if (!string.IsNullOrWhiteSpace(concepto)) yield return concepto;
            if (respuestasAceptadas == null) yield break;

            foreach (string respuesta in respuestasAceptadas)
            {
                if (!string.IsNullOrWhiteSpace(respuesta)) yield return respuesta;
            }
        }
    }
}
