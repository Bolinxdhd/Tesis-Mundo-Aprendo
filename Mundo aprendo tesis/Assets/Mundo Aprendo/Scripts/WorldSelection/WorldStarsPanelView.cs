using UnityEngine;

namespace Bolin
{
    /// <summary>
    /// Displays the real progress for the world currently being explored in the
    /// selection screen.  It is deliberately visual-only: persistence remains in
    /// <see cref="WorldProgressRepository"/> and navigation remains in
    /// <see cref="WorldSelectionManager"/>.
    /// </summary>
    public sealed class WorldStarsPanelView : MonoBehaviour
    {
        [SerializeField] private UIStarDisplay starDisplay;
        [SerializeField, Range(0, WorldProgressRepository.WorldCount - 1)] private int initialWorldIndex;

        private int focusedWorldIndex;

        public int FocusedWorldIndex => focusedWorldIndex;

        public void Configure(UIStarDisplay configuredDisplay, int configuredInitialWorldIndex)
        {
            starDisplay = configuredDisplay;
            initialWorldIndex = Mathf.Clamp(configuredInitialWorldIndex, 0, WorldProgressRepository.WorldCount - 1);
            focusedWorldIndex = initialWorldIndex;
        }

        private void Awake()
        {
            focusedWorldIndex = Mathf.Clamp(initialWorldIndex, 0, WorldProgressRepository.WorldCount - 1);
            Refresh();
        }

        private void OnEnable()
        {
            WorldProgressRepository.ProgressChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            WorldProgressRepository.ProgressChanged -= Refresh;
        }

        /// <summary>Called by a card hover or EventSystem focus event.</summary>
        public void FocusWorld(int worldIndex)
        {
            if (worldIndex < 0 || worldIndex >= WorldProgressRepository.WorldCount) return;

            focusedWorldIndex = worldIndex;
            Refresh();
        }

        public void Refresh()
        {
            if (starDisplay == null) return;

            bool unlocked = WorldProgressRepository.IsUnlocked(focusedWorldIndex);
            int stars = unlocked ? WorldProgressRepository.GetStars(focusedWorldIndex) : 0;
            starDisplay.SetImmediate(stars);
        }
    }
}
