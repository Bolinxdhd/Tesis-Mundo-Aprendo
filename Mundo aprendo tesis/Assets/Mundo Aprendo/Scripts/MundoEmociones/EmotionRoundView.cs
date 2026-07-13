using System;
using UnityEngine;

namespace Bolin
{
    // Emocion que puede mostrarse y seleccionarse en el mundo de emociones.
    public enum EmotionType
    {
        Joy,
        Sadness,
        Anger,
        Surprise,
        Fear
    }

    [Serializable]
    // Referencias de una ronda: personaje/expresion visible, animacion y audio opcional.
    public class EmotionRoundView
    {
        public EmotionType emotion;
        public GameObject rootObject;
        public RectTransform animatedRect;
        public CanvasGroup canvasGroup;
        public string displayName;
        public AudioClip instructionAudio;
    }
}
