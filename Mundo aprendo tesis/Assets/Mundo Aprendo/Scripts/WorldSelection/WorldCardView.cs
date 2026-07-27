using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bolin
{
    /// <summary>
    /// Bridges Unity UI input to a world portal card's visual animator. It never loads
    /// scenes or writes progress, so the existing WorldSelectionManager remains
    /// the single owner of selection mechanics.
    /// </summary>
    public sealed class WorldCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField, Range(0, WorldProgressRepository.WorldCount - 1)] private int worldIndex;
        [SerializeField] private Button worldButton;
        [SerializeField] private CanvasGroup visualCanvasGroup;
        [SerializeField] private Image planetBody;
        [SerializeField] private GameObject lockBadge;
        [SerializeField] private WorldCardAnimator cardAnimator;
        [SerializeField] private WorldStarsPanelView starsPanel;
        [SerializeField] private Color unlockedColor = Color.white;
        [SerializeField] private Color lockedColor = new(0.58f, 0.58f, 0.67f, 1f);

        private bool pointerInside;
        private bool selected;

        public void Configure(
            int configuredWorldIndex,
            Button configuredButton,
            CanvasGroup configuredVisualCanvasGroup,
            Image configuredPlanetBody,
            GameObject configuredLockBadge,
            WorldCardAnimator configuredCardAnimator,
            WorldStarsPanelView configuredStarsPanel,
            Color configuredUnlockedColor,
            Color configuredLockedColor)
        {
            worldIndex = Mathf.Clamp(configuredWorldIndex, 0, WorldProgressRepository.WorldCount - 1);
            worldButton = configuredButton;
            visualCanvasGroup = configuredVisualCanvasGroup;
            planetBody = configuredPlanetBody;
            lockBadge = configuredLockBadge;
            cardAnimator = configuredCardAnimator;
            starsPanel = configuredStarsPanel;
            unlockedColor = configuredUnlockedColor;
            lockedColor = configuredLockedColor;
        }

        private void OnEnable()
        {
            WorldProgressRepository.ProgressChanged += RefreshVisualState;
            RefreshVisualState();
        }

        private void OnDisable()
        {
            WorldProgressRepository.ProgressChanged -= RefreshVisualState;
            cardAnimator?.SetPressed(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            Focus(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            UpdateHoverState();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Focus(true);
            if (!WorldProgressRepository.IsUnlocked(worldIndex))
            {
                cardAnimator?.PlayLockedFeedback();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            cardAnimator?.SetPressed(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            cardAnimator?.SetPressed(false);
        }

        public void OnSelect(BaseEventData eventData)
        {
            selected = true;
            Focus(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            selected = false;
            UpdateHoverState();
        }

        private void Focus(bool animate)
        {
            starsPanel?.FocusWorld(worldIndex);
            if (animate) cardAnimator?.SetHovered(true);
        }

        private void UpdateHoverState()
        {
            cardAnimator?.SetHovered(pointerInside || selected);
        }

        private void RefreshVisualState()
        {
            bool unlocked = WorldProgressRepository.IsUnlocked(worldIndex);
            if (visualCanvasGroup != null) visualCanvasGroup.alpha = unlocked ? 1f : 0.68f;
            if (planetBody != null) planetBody.color = unlocked ? unlockedColor : lockedColor;
            if (lockBadge != null) lockBadge.SetActive(!unlocked);
            if (worldButton != null) worldButton.interactable = unlocked;
            cardAnimator?.SetLocked(!unlocked);
        }
    }
}
