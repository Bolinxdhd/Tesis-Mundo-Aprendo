using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Bolin
{
    [Serializable]
    public sealed class CuentoCardViewV2
    {
        [SerializeField] private string cuentoId;
        [SerializeField] private Button button;
        [SerializeField] private Image coverImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private string displayName;
        [SerializeField] private Image lockImage;
        [SerializeField] private Image[] starImages = new Image[3];
        [SerializeField] private TMP_Text completedText;
        [SerializeField] private TMP_Text starsText;

        [NonSerialized] private UnityAction clickAction;

        public Button Button => button;

        public void SetData(
            CuentoData cuento,
            bool unlocked,
            int stars,
            Sprite earnedStarSprite,
            Sprite unearnedStarSprite)
        {
            cuentoId = cuento != null ? cuento.Id : string.Empty;
            if (button != null) button.interactable = unlocked;

            if (titleText != null)
            {
                titleText.text = cuento == null
                    ? string.Empty
                    : string.IsNullOrWhiteSpace(displayName) ? cuento.Titulo : displayName;
            }

            if (completedText != null)
            {
                completedText.text = string.Empty;
                completedText.gameObject.SetActive(false);
            }

            if (starsText != null)
            {
                starsText.text = string.Empty;
                starsText.enabled = false;
            }

            if (lockImage != null)
            {
                lockImage.gameObject.SetActive(cuento != null && !unlocked);
            }

            int clampedStars = Mathf.Clamp(stars, 0, 3);
            for (int i = 0; i < starImages.Length; i++)
            {
                Image starImage = starImages[i];
                if (starImage == null) continue;

                bool visible = cuento != null && i < 3;
                starImage.gameObject.SetActive(visible);
                starImage.sprite = i < clampedStars ? earnedStarSprite : unearnedStarSprite;
                starImage.enabled = visible && starImage.sprite != null;
            }

            if (coverImage != null)
            {
                Sprite cover = cuento != null ? cuento.Portada : null;
                coverImage.sprite = cover;
                coverImage.enabled = cover != null;
            }

            if (button != null) button.gameObject.SetActive(cuento != null);
        }

        public void Bind(Action<string> callback)
        {
            Unbind();
            if (button == null || callback == null) return;
            clickAction = () => callback(cuentoId);
            button.onClick.AddListener(clickAction);
        }

        public void Unbind()
        {
            if (button != null && clickAction != null) button.onClick.RemoveListener(clickAction);
            clickAction = null;
        }

    }

    [DisallowMultipleComponent]
    public sealed class MundoCuentosUIController : MonoBehaviour
    {
        [Header("Paneles existentes")]
        [SerializeField] private GameObject libraryPanel;
        [SerializeField] private GameObject storyPanel;
        [SerializeField] private GameObject feedbackContainer;
        [SerializeField] private WorldResultPanel resultPanel;

        [Header("Biblioteca")]
        [SerializeField] private List<CuentoCardViewV2> storyCards = new();
        [SerializeField] private TMP_Text libraryProgressText;
        [SerializeField] private TMP_Text otherWorldsUnlockText;
        [SerializeField] private Button libraryBackButton;
        [SerializeField] private Button otherWorldsButton;
        [SerializeField] private Sprite earnedStarSprite;
        [SerializeField] private Sprite unearnedStarSprite;

        [Header("Cuento y pictograma")]
        [SerializeField] private GameObject narrationContainer;
        [SerializeField] private GameObject pictogramContainer;
        [SerializeField] private TMP_Text storyTitleText;
        [SerializeField] private TMP_Text narrationText;
        [SerializeField] private Image pictogramImage;
        [SerializeField] private TMP_Text pictogramPlaceholderText;
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private TMP_Text pictogramProgressText;
        [SerializeField] private TMP_Text recognizedText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private GameObject microphoneIndicator;

        [Header("Acciones reutilizadas")]
        [SerializeField] private Button primaryButton;
        [SerializeField] private Button secondaryButton;
        [SerializeField] private Button backToLibraryButton;

        [Header("Presentación existente")]
        [SerializeField] private StoryTutorialAnimationController tutorialController;
        [SerializeField] private StoryLevelAnimationController animationController;

        private bool listenersBound;

        public event Action<string> StorySelected;
        public event Action PrimaryActionRequested;
        public event Action SecondaryActionRequested;
        public event Action BackToLibraryRequested;
        public event Action BackToWorldsRequested;
        public event Action OtherWorldsRequested;

        private void OnEnable()
        {
            BindListeners();
        }

        private void OnDisable()
        {
            UnbindListeners();
        }

        public void ShowTutorialIfNeeded()
        {
            tutorialController?.ShowIfNeeded();
        }

        public void ShowLibrary(
            IReadOnlyList<CuentoData> stories,
            Func<int, bool> isUnlocked,
            int completedStories,
            bool otherWorldsUnlocked)
        {
            SetActive(libraryPanel, true);
            SetActive(storyPanel, false);
            resultPanel?.HideImmediate();
            SetMicrophoneIndicator(false);
            SetActive(feedbackContainer, true);

            int storyCount = stories?.Count ?? 0;
            for (int i = 0; i < storyCards.Count; i++)
            {
                CuentoData story = i < storyCount ? stories[i] : null;
                bool unlocked = story != null && (isUnlocked?.Invoke(i) ?? true);
                int stars = story != null ? StoryProgressRepository.GetBestStoryStars(story.Id) : 0;
                storyCards[i].SetData(story, unlocked, stars, earnedStarSprite, unearnedStarSprite);
            }

            if (libraryProgressText != null)
            {
                libraryProgressText.text = string.Empty;
                libraryProgressText.gameObject.SetActive(false);
            }

            if (otherWorldsUnlockText != null)
            {
                otherWorldsUnlockText.text = string.Empty;
                otherWorldsUnlockText.gameObject.SetActive(false);
            }

            if (otherWorldsButton != null)
            {
                otherWorldsButton.gameObject.SetActive(false);
                otherWorldsButton.interactable = false;
            }

            _ = completedStories;
            _ = otherWorldsUnlocked;

            animationController?.PlaySelectionEntrance();
        }

        public void ShowStoryIntro(CuentoData story)
        {
            SetActive(libraryPanel, false);
            SetActive(storyPanel, true);
            resultPanel?.HideImmediate();
            SetActive(feedbackContainer, true);
            SetMicrophoneIndicator(false);
            SetActive(narrationContainer, true);
            SetActive(pictogramContainer, false);

            SetText(storyTitleText, story != null ? story.Titulo : "Cuento");
            SetText(narrationText, story != null ? story.NarracionTexto : string.Empty);
            SetText(questionText, "Escucha o lee el cuento antes de comenzar.");
            SetText(pictogramProgressText, string.Empty);
            SetText(recognizedText, string.Empty);
            SetText(feedbackText, string.Empty);
            SetText(hintText, string.Empty);
            SetText(errorText, string.Empty);
            SetText(statusText, story != null && story.NarracionAudio != null
                ? "Escucha la narración."
                : "Cuando estés listo, comienza la actividad.");

            if (pictogramImage != null)
            {
                pictogramImage.sprite = story != null ? story.Portada : null;
                pictogramImage.enabled = pictogramImage.sprite != null;
            }

            SetText(pictogramPlaceholderText, string.Empty);
            SetActionButton(primaryButton, story == null || story.NarracionAudio == null, "Comenzar actividad");
            SetActionButton(secondaryButton, false, string.Empty);
            animationController?.PlayReadingEntrance();
        }

        public void ShowNarrating()
        {
            SetActive(narrationContainer, true);
            SetActive(pictogramContainer, false);
            SetText(statusText, "Escuchando la narración…");
            SetText(questionText, string.Empty);
            SetActionButton(primaryButton, false, string.Empty);
            SetActionButton(secondaryButton, false, string.Empty);
            SetMicrophoneIndicator(false);
        }

        public void ShowPictogram(PictogramaData pictogram, int index, int total)
        {
            SetActive(narrationContainer, false);
            SetActive(pictogramContainer, true);
            if (pictogramImage != null)
            {
                pictogramImage.sprite = pictogram?.Imagen;
                pictogramImage.enabled = pictogramImage.sprite != null;
            }

            bool usePlaceholder = pictogram == null || pictogram.Imagen == null;
            if (pictogramPlaceholderText != null)
            {
                pictogramPlaceholderText.gameObject.SetActive(usePlaceholder);
                pictogramPlaceholderText.text = usePlaceholder && pictogram != null
                    ? $"[{pictogram.Concepto?.ToUpperInvariant()}]"
                    : string.Empty;
            }

            SetText(questionText, "¿Qué ves aquí?");
            SetText(pictogramProgressText, $"{index + 1} / {Mathf.Max(1, total)}");
            SetText(recognizedText, string.Empty);
            SetText(feedbackText, string.Empty);
            SetText(hintText, string.Empty);
            SetText(errorText, string.Empty);
            SetText(statusText, "Observa el pictograma.");
            SetActionButton(primaryButton, false, string.Empty);
            SetActionButton(secondaryButton, false, string.Empty);
            SetMicrophoneIndicator(false);
            animationController?.PlayStoryChanged();
        }

        public void ShowAskingQuestion(bool hasQuestionAudio)
        {
            SetText(questionText, "¿Qué ves aquí?");
            SetText(statusText, hasQuestionAudio ? "Escucha la pregunta…" : "Preparando el micrófono…");
            SetMicrophoneIndicator(false);
        }

        public void ShowListening()
        {
            SetText(statusText, "Te escucho…");
            SetText(errorText, string.Empty);
            SetMicrophoneIndicator(true);
            animationController?.PlayMicrophoneActivation();
        }

        public void ShowProcessing()
        {
            SetText(statusText, "Revisando tu respuesta…");
            SetMicrophoneIndicator(false);
        }

        public void ShowRecognizedText(string value)
        {
            SetText(recognizedText, value);
        }

        public void ShowFeedback(string message, string hint, bool correct)
        {
            SetText(feedbackText, message);
            SetText(hintText, hint);
            SetText(statusText, correct ? "¡Muy bien!" : "Vamos a intentarlo otra vez.");
            SetText(errorText, string.Empty);
            SetMicrophoneIndicator(false);
        }

        public void ShowPreparingRecognition(string message)
        {
            SetText(statusText, string.IsNullOrWhiteSpace(message) ? "Preparando reconocimiento offline…" : message);
            SetMicrophoneIndicator(false);
        }

        public void ShowMicrophoneError()
        {
            SetText(errorText, "No se encontró un micrófono.");
            SetText(statusText, "Conecta un micrófono y vuelve a intentarlo.");
            SetText(feedbackText, string.Empty);
            SetText(hintText, string.Empty);
            SetMicrophoneIndicator(false);
            SetActionButton(primaryButton, true, "Reintentar");
            SetActionButton(secondaryButton, true, "Volver");
        }

        public void ShowRecognitionError(string message)
        {
            SetText(errorText, message);
            SetText(statusText, "No se pudo preparar el reconocimiento.");
            SetMicrophoneIndicator(false);
            SetActionButton(primaryButton, true, "Reintentar");
            SetActionButton(secondaryButton, true, "Volver");
        }

        public void ShowStoryComplete(CuentoData story, int stars, bool hasNextStory)
        {
            SetMicrophoneIndicator(false);
            SetActive(libraryPanel, false);
            SetActive(storyPanel, true);
            if (resultPanel == null)
            {
                SetText(feedbackText, "¡Cuento completado!");
                return;
            }

            string title = story != null ? story.Titulo : "Cuento completado";
            resultPanel.SetLabel(
                WorldResultPanel.ResultAction.Next,
                hasNextStory ? "SIGUIENTE CUENTO" : "VOLVER A CUENTOS");
            resultPanel.Show(stars, "¡Completaste todos los pictogramas!", title);
            animationController?.PlayResultEntrance();
        }

        public void ConfigureResultActions(
            UnityAction retry,
            UnityAction next,
            UnityAction backToLibrary,
            UnityAction backToWorlds)
        {
            if (resultPanel == null) return;
            resultPanel.Bind(WorldResultPanel.ResultAction.Retry, retry);
            resultPanel.Bind(WorldResultPanel.ResultAction.Next, next);
            resultPanel.Bind(WorldResultPanel.ResultAction.BackToList, backToLibrary);
            resultPanel.Bind(WorldResultPanel.ResultAction.WorldSelection, backToWorlds);
            resultPanel.SetLabel(WorldResultPanel.ResultAction.Retry, "REPETIR CUENTO");
            resultPanel.SetLabel(WorldResultPanel.ResultAction.Next, "SIGUIENTE CUENTO");
            resultPanel.SetLabel(WorldResultPanel.ResultAction.BackToList, "VOLVER A CUENTOS");
            resultPanel.SetLabel(WorldResultPanel.ResultAction.WorldSelection, "VOLVER A MUNDOS");
        }

        private void BindListeners()
        {
            if (listenersBound) return;
            listenersBound = true;

            if (primaryButton != null) primaryButton.onClick.AddListener(RaisePrimaryAction);
            if (secondaryButton != null) secondaryButton.onClick.AddListener(RaiseSecondaryAction);
            if (backToLibraryButton != null) backToLibraryButton.onClick.AddListener(RaiseBackToLibrary);
            if (libraryBackButton != null) libraryBackButton.onClick.AddListener(RaiseBackToWorlds);
            if (otherWorldsButton != null) otherWorldsButton.onClick.AddListener(RaiseOtherWorlds);
            foreach (CuentoCardViewV2 card in storyCards) card?.Bind(RaiseStorySelected);
        }

        private void UnbindListeners()
        {
            if (!listenersBound) return;
            listenersBound = false;

            if (primaryButton != null) primaryButton.onClick.RemoveListener(RaisePrimaryAction);
            if (secondaryButton != null) secondaryButton.onClick.RemoveListener(RaiseSecondaryAction);
            if (backToLibraryButton != null) backToLibraryButton.onClick.RemoveListener(RaiseBackToLibrary);
            if (libraryBackButton != null) libraryBackButton.onClick.RemoveListener(RaiseBackToWorlds);
            if (otherWorldsButton != null) otherWorldsButton.onClick.RemoveListener(RaiseOtherWorlds);
            foreach (CuentoCardViewV2 card in storyCards) card?.Unbind();
        }

        private void RaiseStorySelected(string storyId) => StorySelected?.Invoke(storyId);
        private void RaisePrimaryAction() => PrimaryActionRequested?.Invoke();
        private void RaiseSecondaryAction() => SecondaryActionRequested?.Invoke();
        private void RaiseBackToLibrary() => BackToLibraryRequested?.Invoke();
        private void RaiseBackToWorlds() => BackToWorldsRequested?.Invoke();
        private void RaiseOtherWorlds() => OtherWorldsRequested?.Invoke();

        private void SetMicrophoneIndicator(bool visible)
        {
            SetActive(microphoneIndicator, visible);
        }

        private static void SetActionButton(Button button, bool visible, string label)
        {
            if (button == null) return;
            button.gameObject.SetActive(visible);
            button.interactable = visible;
            TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>(true);
            if (buttonText != null && !string.IsNullOrEmpty(label)) buttonText.text = label;
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null) target.text = value ?? string.Empty;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active) target.SetActive(active);
        }
    }
}
