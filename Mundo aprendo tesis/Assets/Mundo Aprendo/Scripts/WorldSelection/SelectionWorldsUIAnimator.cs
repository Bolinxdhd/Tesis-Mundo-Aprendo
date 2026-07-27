using UnityEngine;

namespace Bolin
{
    /// <summary>Single update owner for the solar-system card animations.</summary>
    public sealed class SelectionWorldsUIAnimator : MonoBehaviour
    {
        [SerializeField] private WorldCardAnimator[] cards;

        public void Configure(WorldCardAnimator[] configuredCards)
        {
            cards = configuredCards;
        }

        private void Update()
        {
            if (cards == null) return;

            float deltaTime = Time.unscaledDeltaTime;
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i]?.Tick(deltaTime);
            }
        }
    }
}
