using UnityEngine;

namespace Bolin
{
    /// <summary>
    /// Controla únicamente la presentación del tutorial inicial de Tamaños.
    /// SizeWorldController continúa siendo la única fuente de rondas, validación,
    /// estrellas y progreso del nivel.
    /// </summary>
    public sealed class SizeWorldTutorialController : MonoBehaviour
    {
        public const string TutorialSeenKey = "MundoTamanos_TutorialVisto";

        [SerializeField] private SizeWorldController sizeWorldController;
        [SerializeField] private CharacterDialogueController dialogueController;
        [SerializeField] private GameObject tutorialOverlay;

        private bool gameplayStarted;

        public bool IsTutorialActive => tutorialOverlay != null && tutorialOverlay.activeInHierarchy &&
                                        dialogueController != null && dialogueController.IsVisible;

        public static bool IsTutorialCompleted()
        {
            return PlayerPrefs.GetInt(TutorialSeenKey, 0) == 1;
        }

        public static void MarkTutorialCompleted(bool save = true)
        {
            PlayerPrefs.SetInt(TutorialSeenKey, 1);
            if (save) PlayerPrefs.Save();
        }

        public static void ResetTutorialState(bool save = true)
        {
            PlayerPrefs.DeleteKey(TutorialSeenKey);
            if (save) PlayerPrefs.Save();
        }

        private void Awake()
        {
            if (dialogueController != null)
            {
                dialogueController.OnIntroCompleted.RemoveListener(HandleIntroCompleted);
                dialogueController.OnIntroCompleted.AddListener(HandleIntroCompleted);
            }
        }

        private void Start()
        {
            if (IsTutorialCompleted())
            {
                HideTutorialAndStartGameplay();
                return;
            }

            if (dialogueController == null)
            {
                // No se deja el nivel bloqueado si falta solo la capa visual del tutorial.
                HideTutorialAndStartGameplay();
                return;
            }

            dialogueController.ShowIntro();
        }

        private void HandleIntroCompleted()
        {
            if (gameplayStarted) return;

            MarkTutorialCompleted();
            HideTutorialAndStartGameplay();
        }

        private void HideTutorialAndStartGameplay()
        {
            if (gameplayStarted) return;
            gameplayStarted = true;

            if (dialogueController != null)
            {
                dialogueController.HideImmediate();
            }

            if (tutorialOverlay != null)
            {
                tutorialOverlay.SetActive(false);
            }

            if (sizeWorldController != null)
            {
                sizeWorldController.StartActivity();
            }
        }

        private void OnDisable()
        {
            if (dialogueController != null)
            {
                dialogueController.OnIntroCompleted.RemoveListener(HandleIntroCompleted);
            }
        }
    }
}
