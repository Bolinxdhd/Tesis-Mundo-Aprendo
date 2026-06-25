using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolin
{
    public class WorldSelectionManager : MonoBehaviour
    {
        [Serializable]
        public class WorldData
        {
            public string worldName;
            public string sceneName;
            public Button worldButton;
            public TMP_Text worldNameText;
            public GameObject lockedIcon;
            public Image[] starImages = new Image[3];
            public Sprite fullStarSprite;
            public Sprite emptyStarSprite;
            public UIStarDisplay starDisplay;
            public Graphic progressGraphic;
            public Color baseProgressColor = Color.white;
        }

        [Header("Mundos")]
        [SerializeField] private WorldData[] worlds = new WorldData[4];

        [Header("UI")]
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private string lockedWorldMessage = "Completa el mundo anterior para desbloquear este";
        [SerializeField] private float messageDuration = 2.5f;
        [SerializeField] private GameObject resetConfirmationPanel;
        [SerializeField] private UIPanelTransition resetConfirmationTransition;

        [Header("Navegacion")]
        [SerializeField] private string menuSceneName = "Menu";
        [SerializeField] private bool configureWorldButtonsOnAwake;

        private Coroutine messageRoutine;

        private void Awake()
        {
            ConfigureWorldButtons();
            RefreshWorlds();
            ClearMessage();
        }

        private void OnEnable()
        {
            RefreshWorlds();
        }

        private void OnValidate()
        {
            if (worlds == null || worlds.Length == 0)
            {
                worlds = new WorldData[4];
            }
        }

        public void SelectWorld(int worldIndex)
        {
            if (!IsValidWorldIndex(worldIndex))
            {
                Debug.LogWarning($"No se puede seleccionar el mundo {worldIndex}. El indice no existe en WorldSelectionManager.");
                return;
            }

            WorldData world = worlds[worldIndex];
            if (world == null)
            {
                Debug.LogWarning($"El mundo {worldIndex} no esta configurado en WorldSelectionManager.");
                return;
            }

            if (!IsWorldUnlocked(worldIndex))
            {
                ShowMessage(lockedWorldMessage);
                return;
            }

            if (string.IsNullOrWhiteSpace(world.sceneName))
            {
                Debug.LogWarning($"El mundo '{GetWorldDisplayName(worldIndex)}' esta desbloqueado, pero no tiene Scene Name configurado.");
                ShowMessage("Este mundo aun no tiene escena configurada.");
                return;
            }

            SceneNavigation.LoadScene(world.sceneName, this);
        }

        public void ResetProgress()
        {
            WorldProgressRepository.ResetAll();
            RefreshWorlds();
            ShowMessage("Progreso reiniciado");
            CancelResetProgress();
        }

        public void RequestResetProgress()
        {
            if (resetConfirmationTransition != null)
            {
                resetConfirmationTransition.Show();
            }
            else if (resetConfirmationPanel != null)
            {
                resetConfirmationPanel.SetActive(true);
            }
        }

        public void CancelResetProgress()
        {
            if (resetConfirmationTransition != null)
            {
                resetConfirmationTransition.Hide();
            }
            else if (resetConfirmationPanel != null)
            {
                resetConfirmationPanel.SetActive(false);
            }
        }

        public void CompleteWorldForTest(int worldIndex)
        {
            if (!IsValidWorldIndex(worldIndex))
            {
                Debug.LogWarning($"No se puede completar el mundo {worldIndex}. El indice no existe en WorldSelectionManager.");
                return;
            }

            WorldProgressRepository.SaveBestResult(worldIndex, WorldProgressRepository.GetStars(worldIndex));
            RefreshWorlds();
            ShowMessage($"{GetWorldDisplayName(worldIndex)} completado para prueba");
        }

        public void SetStarsForTest(int worldIndex, int stars)
        {
            if (!IsValidWorldIndex(worldIndex))
            {
                Debug.LogWarning($"No se pueden asignar estrellas al mundo {worldIndex}. El indice no existe en WorldSelectionManager.");
                return;
            }

            int clampedStars = Mathf.Clamp(stars, 0, 3);
            PlayerPrefs.SetInt(GetStarsKey(worldIndex), clampedStars);
            PlayerPrefs.Save();
            RefreshWorlds();
            ShowMessage($"{GetWorldDisplayName(worldIndex)}: {clampedStars} estrellas");
        }

        public void CompleteWorld0ForTest()
        {
            CompleteWorldForTest(0);
        }

        public void CompleteWorld1ForTest()
        {
            CompleteWorldForTest(1);
        }

        public void CompleteWorld2ForTest()
        {
            CompleteWorldForTest(2);
        }

        public void CompleteWorld3ForTest()
        {
            CompleteWorldForTest(3);
        }

        public void SetWorld0StarsForTest(int stars)
        {
            SetStarsForTest(0, stars);
        }

        public void SetWorld1StarsForTest(int stars)
        {
            SetStarsForTest(1, stars);
        }

        public void SetWorld2StarsForTest(int stars)
        {
            SetStarsForTest(2, stars);
        }

        public void SetWorld3StarsForTest(int stars)
        {
            SetStarsForTest(3, stars);
        }

        public void ReturnToMenu()
        {
            if (string.IsNullOrWhiteSpace(menuSceneName))
            {
                Debug.LogWarning("No hay una escena de menu configurada en WorldSelectionManager.");
                return;
            }

            SceneNavigation.LoadScene(menuSceneName, this);
        }

        public void RefreshWorlds()
        {
            if (worlds == null)
            {
                Debug.LogWarning("La lista de mundos no esta configurada en WorldSelectionManager.");
                return;
            }

            for (int i = 0; i < worlds.Length; i++)
            {
                RefreshWorld(i);
            }
        }

        private void ConfigureWorldButtons()
        {
            if (!configureWorldButtonsOnAwake || worlds == null) return;

            for (int i = 0; i < worlds.Length; i++)
            {
                WorldData world = worlds[i];
                if (world?.worldButton == null) continue;

                int index = i;
                world.worldButton.onClick.AddListener(() => SelectWorld(index));
            }
        }

        private void RefreshWorld(int worldIndex)
        {
            WorldData world = worlds[worldIndex];
            if (world == null)
            {
                Debug.LogWarning($"El mundo {worldIndex} no esta configurado en WorldSelectionManager.");
                return;
            }

            bool isUnlocked = IsWorldUnlocked(worldIndex);
            int stars = GetSavedStars(worldIndex);

            if (world.worldNameText != null)
            {
                world.worldNameText.text = GetWorldDisplayName(worldIndex);
            }

            if (world.lockedIcon != null)
            {
                world.lockedIcon.SetActive(!isUnlocked);
            }

            if (world.worldButton != null)
            {
                world.worldButton.interactable = true;
            }
            else
            {
                Debug.LogWarning($"El mundo '{GetWorldDisplayName(worldIndex)}' no tiene World Button asignado.");
            }

            RefreshProgressAppearance(world, stars, isUnlocked);
            RefreshStars(world, stars);
        }

        private static void RefreshProgressAppearance(WorldData world, int stars, bool isUnlocked)
        {
            if (world.progressGraphic == null) return;
            float progress = Mathf.Clamp01(stars / 3f);
            float brightness = isUnlocked ? Mathf.Lerp(0.68f, 1f, progress) : 0.46f;
            Color baseColor = world.baseProgressColor;
            world.progressGraphic.color = new Color(
                Mathf.Clamp01(baseColor.r * brightness),
                Mathf.Clamp01(baseColor.g * brightness),
                Mathf.Clamp01(baseColor.b * brightness),
                baseColor.a);
        }

        private void RefreshStars(WorldData world, int stars)
        {
            if (world.starDisplay != null) world.starDisplay.SetImmediate(stars);
            if (world.starImages == null) return;

            for (int i = 0; i < world.starImages.Length; i++)
            {
                Image starImage = world.starImages[i];
                if (starImage == null) continue;

                Sprite targetSprite = i < stars ? world.fullStarSprite : world.emptyStarSprite;
                if (targetSprite != null)
                {
                    starImage.sprite = targetSprite;
                    starImage.color = Color.white;
                }

                starImage.enabled = targetSprite != null || starImage.sprite != null;
            }
        }

        private bool IsWorldUnlocked(int worldIndex)
        {
            return WorldProgressRepository.IsUnlocked(worldIndex);
        }

        private int GetSavedStars(int worldIndex)
        {
            return WorldProgressRepository.GetStars(worldIndex);
        }

        private bool IsValidWorldIndex(int worldIndex)
        {
            return worlds != null && worldIndex >= 0 && worldIndex < worlds.Length;
        }

        private string GetWorldDisplayName(int worldIndex)
        {
            if (!IsValidWorldIndex(worldIndex) || worlds[worldIndex] == null)
            {
                return $"Mundo {worldIndex}";
            }

            string configuredName = worlds[worldIndex].worldName;
            return string.IsNullOrWhiteSpace(configuredName) ? $"Mundo {worldIndex + 1}" : configuredName;
        }

        private static string GetCompletedKey(int worldIndex)
        {
            return WorldProgressRepository.GetCompletedKey(worldIndex);
        }

        private static string GetStarsKey(int worldIndex)
        {
            return WorldProgressRepository.GetStarsKey(worldIndex);
        }

        private void ShowMessage(string message)
        {
            if (messageText == null)
            {
                Debug.LogWarning($"Mensaje de WorldSelectionManager: {message}. Falta asignar Message Text.");
                return;
            }

            if (messageRoutine != null)
            {
                StopCoroutine(messageRoutine);
            }

            messageRoutine = StartCoroutine(ShowMessageRoutine(message));
        }

        private IEnumerator ShowMessageRoutine(string message)
        {
            messageText.text = message;
            messageText.gameObject.SetActive(true);

            if (messageDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(messageDuration);
                ClearMessage();
            }

            messageRoutine = null;
        }

        private void ClearMessage()
        {
            if (messageText == null) return;

            messageText.text = string.Empty;
            messageText.gameObject.SetActive(false);
        }
    }
}
