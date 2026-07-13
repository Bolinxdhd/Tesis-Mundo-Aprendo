using System;
using UnityEngine;
using UnityEngine.UI;

namespace Bolin
{
    public class WorldSelectionVisuals : MonoBehaviour
    {
        [Serializable]
        public class WorldVisual
        {
            public RectTransform visualRoot;
            public CanvasGroup canvasGroup;
            public Image planetImage;
            public GameObject lockedGroup;
            public Color unlockedColor = Color.white;
            public Color lockedColor = new(0.72f, 0.72f, 0.78f, 1f);
        }

        [SerializeField] private WorldVisual[] worlds = Array.Empty<WorldVisual>();
        [SerializeField, Min(0.1f)] private float pulseSpeed = 0.85f;
        [SerializeField, Min(0f)] private float pulseScale = 0.025f;

        private void OnEnable()
        {
            Refresh();
        }

        private void Update()
        {
            float wave = Mathf.Sin(Time.unscaledTime * pulseSpeed);
            for (int i = 0; i < worlds.Length; i++)
            {
                WorldVisual world = worlds[i];
                if (world?.visualRoot == null) continue;

                bool unlocked = WorldProgressRepository.IsUnlocked(i);
                float scale = unlocked ? 1f + wave * pulseScale : 1f;
                world.visualRoot.localScale = Vector3.one * scale;
            }
        }

        public void Refresh()
        {
            for (int i = 0; i < worlds.Length; i++)
            {
                WorldVisual world = worlds[i];
                if (world == null) continue;

                bool unlocked = WorldProgressRepository.IsUnlocked(i);
                if (world.canvasGroup != null)
                {
                    world.canvasGroup.alpha = unlocked ? 1f : 0.62f;
                }

                if (world.planetImage != null)
                {
                    world.planetImage.color = unlocked ? world.unlockedColor : world.lockedColor;
                }

                if (world.lockedGroup != null)
                {
                    world.lockedGroup.SetActive(!unlocked);
                }
            }
        }
    }
}
