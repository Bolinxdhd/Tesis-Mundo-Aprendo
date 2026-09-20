using Assets.SurpriseBox.Scripts;
using UnityEngine;

namespace Bolin
{
    public class GameplayDialogueFeedbackBridge : MonoBehaviour
    {
        [SerializeField] private CharacterDialogueController dialogue;
        [SerializeField] private SizeWorldController sizeWorldController;
        [SerializeField] private EmotionGameManager emotionGameManager;
        [SerializeField] private MundoMusicalSequenceGame musicalGame;
        [SerializeField] private MundoCuentosController storyController;
        [SerializeField] private bool showSuccessFeedback = true;
        [SerializeField] private bool showRetryFeedback = true;

        private void Awake()
        {
            if (dialogue == null) dialogue = GetComponent<CharacterDialogueController>();
        }

        private void OnEnable()
        {
            if (sizeWorldController != null) sizeWorldController.OnAnswerValidated += HandleAnswerValidated;
            if (emotionGameManager != null) emotionGameManager.OnAnswerValidated += HandleAnswerValidated;
            if (musicalGame != null)
            {
                musicalGame.OnAnswerValidated += HandleAnswerValidated;
                musicalGame.OnActivityCompleted += HandleActivityCompleted;
            }

            if (storyController != null) storyController.OnAnswerValidated += HandleAnswerValidated;
        }

        private void OnDisable()
        {
            if (sizeWorldController != null) sizeWorldController.OnAnswerValidated -= HandleAnswerValidated;
            if (emotionGameManager != null) emotionGameManager.OnAnswerValidated -= HandleAnswerValidated;
            if (musicalGame != null)
            {
                musicalGame.OnAnswerValidated -= HandleAnswerValidated;
                musicalGame.OnActivityCompleted -= HandleActivityCompleted;
            }

            if (storyController != null) storyController.OnAnswerValidated -= HandleAnswerValidated;
        }

        private void HandleAnswerValidated(bool wasCorrect)
        {
            if (dialogue == null) return;
            if (wasCorrect && showSuccessFeedback)
            {
                dialogue.ShowSuccessFeedback();
            }
            else if (!wasCorrect && showRetryFeedback)
            {
                dialogue.ShowRetryFeedback();
            }
        }

        private void HandleActivityCompleted(int stars)
        {
            if (showSuccessFeedback && dialogue != null)
            {
                dialogue.ShowSuccessFeedback();
            }
        }
    }
}
