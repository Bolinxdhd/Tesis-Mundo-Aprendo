using System;
using UnityEngine;

namespace Bolin
{
    public enum EmotionType
    {
        Joy,
        Sadness,
        Anger,
        Surprise,
        Fear
    }

    [Serializable]
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
