using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolin
{
    // Tipo pedagogico que se usa para comparar animales dentro del mundo de tamanos.
    public enum AnimalSizeType
    {
        Pequeno,
        Grande
    }

    [Serializable]
    // Datos de un animal jugable: sprite, nombre, tamano correcto y escala visual.
    public class AnimalData
    {
        public string animalName;
        public Sprite animalSprite;
        public AnimalSizeType sizeType;

        [Header("Escala visual")]
        public float minScale = 0.6f;
        public float maxScale = 1.2f;
    }

    [Serializable]
    // Agrupa fondo y animales para armar rondas por habitat.
    public class HabitatData
    {
        public string habitatName;
        public Sprite backgroundSprite;
        public List<AnimalData> animals = new();
    }

    // Controla el mundo de tamanos: elige animales, valida respuestas, anima rondas y guarda progreso.
    public class SizeWorldController : MonoBehaviour
    {
        private const int WorldIndex = 2;

        public event Action OnRoundStarted;
        public event Action<bool> OnAnswerValidated;
        public event Action<int> OnActivityCompleted;

        [Header("Habitats")]
        [SerializeField] private List<HabitatData> habitats = new();

        [Header("Referencias UI")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image leftAnimalImage;
        [SerializeField] private Image rightAnimalImage;
        [SerializeField] private TextMeshProUGUI questionText;
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private Button leftAnimalButton;
        [SerializeField] private Button rightAnimalButton;
        [SerializeField] private TextMeshProUGUI leftAnimalNameText;
        [SerializeField] private TextMeshProUGUI rightAnimalNameText;
        [SerializeField] private Outline leftSelectionOutline;
        [SerializeField] private Outline rightSelectionOutline;
        [SerializeField] private RectTransform animalSafeArea;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private Button returnButton;

        [Header("Estrellas")]
        [SerializeField] private Image[] starImages = new Image[3];
        [SerializeField] private Sprite fullStarSprite;
        [SerializeField] private Sprite emptyStarSprite;
        [SerializeField] private UIStarDisplay starDisplay;

        [Header("Animacion")]
        [SerializeField, Min(0.05f)] private float entranceDuration = 0.65f;
        [SerializeField, Min(0.05f)] private float correctPulseDuration = 0.18f;
        [SerializeField, Min(0f)] private float delayBeforeNextRound = 1f;

        [Header("Posicion y escala")]
        [SerializeField] private Vector2 verticalPositionRange = new(-190f, 170f);
        [SerializeField, Min(0.1f)] private float globalMinScale = 0.55f;
        [SerializeField, Min(0.1f)] private float globalMaxScale = 1.25f;
        [SerializeField, Min(0f)] private float horizontalPadding = 130f;
        [SerializeField, Min(0f)] private float verticalPadding = 80f;
        [SerializeField, Min(0f)] private float offscreenPadding = 420f;

        [Header("Flujo")]
        [SerializeField, Min(1)] private int roundsToComplete = 5;
        [SerializeField] private string returnSceneName = "SeleccionMundos";
        [SerializeField] private bool startAutomatically = true;

        private AnimalData biggerAnimal;
        private AnimalData smallerAnimal;
        private AnimalData targetAnimal;
        private AnimalData leftRoundAnimal;
        private AnimalData rightRoundAnimal;
        private Coroutine roundRoutine;
        private int completedRounds;
        private int mistakeCount;
        private int currentStars = 3;
        private bool acceptingAnswer;
        private bool activityFinished;
        private Image lastSelectedImage;

        private void Awake()
        {
            // Conecta botones y deja lista la escena antes de iniciar rondas.
            ConfigureButtons();
            PrepareInitialState();
        }

        private void Start()
        {
            if (startAutomatically)
            {
                StartActivity();
            }
        }

        private void OnDisable()
        {
            StopRoundRoutine();
        }

        public void StartActivity()
        {
            // Reinicia contadores/estrellas y comienza la primera ronda.
            StopRoundRoutine();

            completedRounds = 0;
            mistakeCount = 0;
            currentStars = 3;
            acceptingAnswer = false;
            activityFinished = false;

            if (resultPanel != null) resultPanel.SetActive(false);
            UpdateStarsUi();
            StartNextRound();
        }

        public void RestartActivity()
        {
            StartActivity();
        }

        public void SelectLeftAnimal()
        {
            // Boton del animal izquierdo: envia su AnimalData como respuesta.
            SubmitAnimalChoice(leftRoundAnimal);
        }

        public void SelectRightAnimal()
        {
            // Boton del animal derecho: envia su AnimalData como respuesta.
            SubmitAnimalChoice(rightRoundAnimal);
        }

        public void ReturnToWorldSelection()
        {
            // Boton Volver: regresa a la escena de seleccion usando SceneNavigation.
            if (string.IsNullOrWhiteSpace(returnSceneName))
            {
                Debug.LogWarning("SizeWorldController: no hay escena de retorno configurada.");
                return;
            }

            SceneNavigation.LoadScene(returnSceneName, this);
        }

        public void RegisterMistake()
        {
            // Cada error resta una estrella y actualiza la UI.
            mistakeCount++;
            currentStars = Mathf.Clamp(3 - mistakeCount, 0, 3);
            UpdateStarsUi();
        }

        public void CompleteActivity()
        {
            // Finaliza el mundo, guarda el mejor resultado y muestra el panel de cierre.
            if (activityFinished) return;

            StopRoundRoutine();
            activityFinished = true;
            acceptingAnswer = false;
            SetAnswerButtonsInteractable(false);
            SaveProgress();
            UpdateStarsUi();

            if (feedbackText != null)
            {
                feedbackText.text = "Actividad completada";
            }

            if (resultText != null)
            {
                resultText.text = $"Actividad completada\nEstrellas obtenidas: {currentStars}/3";
            }

            if (resultPanel != null)
            {
                resultPanel.SetActive(true);
            }

            OnActivityCompleted?.Invoke(currentStars);
        }

        private void SubmitAnimalChoice(AnimalData selectedAnimal)
        {
            // Compara el animal tocado con el objetivo grande/pequeno de la ronda.
            if (!acceptingAnswer || activityFinished || selectedAnimal == null || targetAnimal == null) return;

            acceptingAnswer = false;
            SetAnswerButtonsInteractable(false);

            bool isCorrect = ReferenceEquals(selectedAnimal, targetAnimal);
            OnAnswerValidated?.Invoke(isCorrect);
            lastSelectedImage = ReferenceEquals(selectedAnimal, leftRoundAnimal) ? leftAnimalImage : rightAnimalImage;
            ShowSelectionOutline(selectedAnimal, isCorrect);
            if (isCorrect)
            {
                completedRounds++;
                if (feedbackText != null) feedbackText.text = "Muy bien";

                if (roundRoutine != null)
                {
                    StopCoroutine(roundRoutine);
                }

                roundRoutine = StartCoroutine(CorrectAnswerRoutine());
                return;
            }

            RegisterMistake();
            if (feedbackText != null) feedbackText.text = "Intenta otra vez";

            if (roundRoutine != null)
            {
                StopCoroutine(roundRoutine);
            }

            roundRoutine = StartCoroutine(EnableRetryRoutine());
        }

        private IEnumerator CorrectAnswerRoutine()
        {
            // Pulsa el animal correcto y decide si pasar de ronda o completar la actividad.
            yield return PulseTargetAnimalRoutine();
            yield return new WaitForSeconds(delayBeforeNextRound);

            if (completedRounds >= roundsToComplete)
            {
                CompleteActivity();
            }
            else
            {
                StartNextRound();
            }

            roundRoutine = null;
        }

        private IEnumerator EnableRetryRoutine()
        {
            // Sacude el animal elegido, espera un poco y reactiva botones para reintentar.
            yield return ShakeSelectedAnimalRoutine();
            yield return new WaitForSeconds(Mathf.Max(0f, delayBeforeNextRound - 0.3f));

            if (!activityFinished)
            {
                HideSelectionOutlines();
                acceptingAnswer = true;
                SetAnswerButtonsInteractable(true);
            }

            roundRoutine = null;
        }

        private void StartNextRound()
        {
            // Selecciona habitat y dos animales comparables, formula pregunta y lanza la entrada animada.
            if (activityFinished) return;

            HabitatData habitat = GetRandomPlayableHabitat();
            if (habitat == null)
            {
                SetQuestion("Configura al menos un habitat con un animal grande y uno pequeno.");
                SetAnswerButtonsInteractable(false);
                return;
            }

            if (backgroundImage != null)
            {
                backgroundImage.sprite = habitat.backgroundSprite;
                backgroundImage.enabled = habitat.backgroundSprite != null;
                backgroundImage.preserveAspect = false;
            }

            AnimalData leftAnimal;
            AnimalData rightAnimal;
            PickTwoComparableAnimals(habitat, out leftAnimal, out rightAnimal);
            leftRoundAnimal = leftAnimal;
            rightRoundAnimal = rightAnimal;

            biggerAnimal = leftAnimal.sizeType == AnimalSizeType.Grande ? leftAnimal : rightAnimal;
            smallerAnimal = ReferenceEquals(biggerAnimal, leftAnimal) ? rightAnimal : leftAnimal;

            bool askForBiggerAnimal = UnityEngine.Random.value < 0.5f;
            targetAnimal = askForBiggerAnimal ? biggerAnimal : smallerAnimal;
            SetQuestion(askForBiggerAnimal
                ? $"Que animal es mas grande que {smallerAnimal.animalName}?"
                : $"Que animal es mas pequeno que {biggerAnimal.animalName}?");

            if (feedbackText != null)
            {
                feedbackText.text = string.Empty;
            }

            ConfigureAnimalImage(leftAnimalImage, leftAnimal);
            ConfigureAnimalImage(rightAnimalImage, rightAnimal);
            if (leftAnimalNameText != null) leftAnimalNameText.text = leftAnimal.animalName;
            if (rightAnimalNameText != null) rightAnimalNameText.text = rightAnimal.animalName;
            HideSelectionOutlines();
            OnRoundStarted?.Invoke();

            Vector2 leftPosition = GetAnimalPosition(true);
            Vector2 rightPosition = GetAnimalPosition(false);
            ApplyDepthScale(leftAnimalImage, leftAnimal, leftPosition.y);
            ApplyDepthScale(rightAnimalImage, rightAnimal, rightPosition.y);

            StopRoundRoutine();
            roundRoutine = StartCoroutine(AnimateRoundEntranceRoutine(leftPosition, rightPosition));
        }

        private IEnumerator AnimateRoundEntranceRoutine(Vector2 leftPosition, Vector2 rightPosition)
        {
            // Bloquea respuestas mientras los animales entran desde fuera de pantalla.
            acceptingAnswer = false;
            SetAnswerButtonsInteractable(false);

            RectTransform leftRect = GetRect(leftAnimalImage);
            RectTransform rightRect = GetRect(rightAnimalImage);
            if (leftRect == null || rightRect == null)
            {
                acceptingAnswer = true;
                SetAnswerButtonsInteractable(true);
                roundRoutine = null;
                yield break;
            }

            yield return SizeWorldAnimationController.PlayRoundEntrance(
                leftRect,
                rightRect,
                leftPosition,
                rightPosition,
                offscreenPadding,
                entranceDuration);
            acceptingAnswer = true;
            SetAnswerButtonsInteractable(true);
            roundRoutine = null;
        }

        private IEnumerator PulseTargetAnimalRoutine()
        {
            // Feedback positivo conectado al controlador de animacion de Tamanos.
            Image targetImage = GetTargetImage();
            RectTransform targetRect = GetRect(targetImage);
            if (targetRect == null) yield break;

            yield return SizeWorldAnimationController.Pulse(targetRect, correctPulseDuration, 1.08f);
        }

        private IEnumerator ShakeSelectedAnimalRoutine()
        {
            // Feedback de error sobre el ultimo animal seleccionado.
            RectTransform rect = GetRect(lastSelectedImage);
            if (rect == null) yield break;

            yield return SizeWorldAnimationController.ShakeHorizontal(rect, 0.3f, 10f, 42f);
        }

        private void ShowSelectionOutline(AnimalData selectedAnimal, bool correct)
        {
            // Enmarca la opcion tocada con color verde si acerto o naranja si fallo.
            HideSelectionOutlines();
            Outline outline = ReferenceEquals(selectedAnimal, leftRoundAnimal) ? leftSelectionOutline : rightSelectionOutline;
            if (outline == null) return;
            outline.effectColor = correct ? new Color(0.2f, 0.75f, 0.42f, 1f) : new Color(0.95f, 0.68f, 0.24f, 1f);
            outline.effectDistance = new Vector2(5f, -5f);
            outline.enabled = true;
        }

        private void HideSelectionOutlines()
        {
            if (leftSelectionOutline != null) leftSelectionOutline.enabled = false;
            if (rightSelectionOutline != null) rightSelectionOutline.enabled = false;
        }

        private HabitatData GetRandomPlayableHabitat()
        {
            // Busca habitats que tengan al menos un animal grande y uno pequeno con sprite.
            List<HabitatData> playableHabitats = new();
            foreach (HabitatData habitat in habitats)
            {
                if (habitat?.animals == null || habitat.animals.Count < 2) continue;

                bool hasBigAnimal = false;
                bool hasSmallAnimal = false;
                foreach (AnimalData animal in habitat.animals)
                {
                    if (animal != null && animal.animalSprite != null && !string.IsNullOrWhiteSpace(animal.animalName))
                    {
                        if (animal.sizeType == AnimalSizeType.Grande)
                        {
                            hasBigAnimal = true;
                        }
                        else
                        {
                            hasSmallAnimal = true;
                        }
                    }
                }

                if (hasBigAnimal && hasSmallAnimal)
                {
                    playableHabitats.Add(habitat);
                }
            }

            if (playableHabitats.Count == 0) return null;
            return playableHabitats[UnityEngine.Random.Range(0, playableHabitats.Count)];
        }

        private static void PickTwoComparableAnimals(HabitatData habitat, out AnimalData firstAnimal, out AnimalData secondAnimal)
        {
            // Escoge una pareja grande/pequena para que la pregunta tenga una respuesta clara.
            List<AnimalData> bigAnimals = new();
            List<AnimalData> smallAnimals = new();
            foreach (AnimalData animal in habitat.animals)
            {
                if (animal == null || animal.animalSprite == null || string.IsNullOrWhiteSpace(animal.animalName))
                {
                    continue;
                }

                if (animal.sizeType == AnimalSizeType.Grande)
                {
                    bigAnimals.Add(animal);
                }
                else
                {
                    smallAnimals.Add(animal);
                }
            }

            if (bigAnimals.Count > 0 && smallAnimals.Count > 0)
            {
                AnimalData bigAnimal = bigAnimals[UnityEngine.Random.Range(0, bigAnimals.Count)];
                AnimalData smallAnimal = smallAnimals[UnityEngine.Random.Range(0, smallAnimals.Count)];

                if (UnityEngine.Random.value < 0.5f)
                {
                    firstAnimal = bigAnimal;
                    secondAnimal = smallAnimal;
                }
                else
                {
                    firstAnimal = smallAnimal;
                    secondAnimal = bigAnimal;
                }

                return;
            }

            List<AnimalData> validAnimals = new();
            foreach (AnimalData animal in habitat.animals)
            {
                if (animal != null && animal.animalSprite != null && !string.IsNullOrWhiteSpace(animal.animalName))
                {
                    validAnimals.Add(animal);
                }
            }

            int firstIndex = UnityEngine.Random.Range(0, validAnimals.Count);
            int secondIndex = UnityEngine.Random.Range(0, validAnimals.Count - 1);
            if (secondIndex >= firstIndex)
            {
                secondIndex++;
            }

            firstAnimal = validAnimals[firstIndex];
            secondAnimal = validAnimals[secondIndex];
        }

        private void ConfigureAnimalImage(Image image, AnimalData animal)
        {
            // Aplica sprite y tamano base antes de posicionar el animal en el area segura.
            if (image == null) return;

            image.sprite = animal.animalSprite;
            image.enabled = animal.animalSprite != null;
            image.preserveAspect = true;
            image.SetNativeSize();
            ClampAnimalRectSize(image);
        }

        private void ClampAnimalRectSize(Image image)
        {
            // Evita que sprites grandes se salgan del area jugable.
            RectTransform rect = GetRect(image);
            if (rect == null || animalSafeArea == null) return;

            float maxWidth = Mathf.Max(120f, animalSafeArea.rect.width * 0.34f);
            float maxHeight = Mathf.Max(120f, animalSafeArea.rect.height * 0.58f);
            Vector2 size = rect.sizeDelta;
            if (size.x <= 0f || size.y <= 0f) return;

            float factor = Mathf.Min(1f, maxWidth / size.x, maxHeight / size.y);
            rect.sizeDelta = size * factor;
        }

        private Vector2 GetAnimalPosition(bool leftSide)
        {
            // Calcula una posicion aleatoria controlada para cada lado de la pantalla.
            Rect area = GetSafePlayAreaRect();

            float halfWidth = area.width * 0.5f;
            float minX = area.xMin + horizontalPadding;
            float maxX = area.xMax - horizontalPadding;
            float x;
            if (leftSide)
            {
                float leftMax = Mathf.Min(maxX, Mathf.Min(-horizontalPadding, -halfWidth * 0.12f));
                x = minX <= leftMax ? UnityEngine.Random.Range(minX, leftMax) : area.center.x - area.width * 0.25f;
            }
            else
            {
                float rightMin = Mathf.Max(minX, Mathf.Max(horizontalPadding, halfWidth * 0.12f));
                x = rightMin <= maxX ? UnityEngine.Random.Range(rightMin, maxX) : area.center.x + area.width * 0.25f;
            }

            float minY = Mathf.Max(area.yMin + verticalPadding, verticalPositionRange.x);
            float maxY = Mathf.Min(area.yMax - verticalPadding, verticalPositionRange.y);
            if (minY > maxY)
            {
                minY = area.yMin + verticalPadding;
                maxY = area.yMax - verticalPadding;
            }

            float y = UnityEngine.Random.Range(minY, maxY);
            return new Vector2(x, y);
        }

        private Rect GetSafePlayAreaRect()
        {
            Rect area = animalSafeArea != null ? animalSafeArea.rect : new Rect(-960f, -540f, 1920f, 1080f);
            return area;
        }

        private void ApplyDepthScale(Image image, AnimalData animal, float yPosition)
        {
            // Simula profundidad: mas abajo se ve mas grande, mas arriba se ve mas pequeno.
            RectTransform rect = GetRect(image);
            if (rect == null) return;

            SizeWorldAnimationController.ApplyDepthScale(
                rect,
                yPosition,
                verticalPositionRange,
                animal.minScale,
                animal.maxScale,
                globalMinScale,
                globalMaxScale);
        }

        private Image GetTargetImage()
        {
            if (ReferenceEquals(targetAnimal, leftRoundAnimal))
            {
                return leftAnimalImage;
            }

            if (ReferenceEquals(targetAnimal, rightRoundAnimal))
            {
                return rightAnimalImage;
            }

            return rightAnimalImage;
        }

        private static RectTransform GetRect(Image image)
        {
            return image != null ? image.rectTransform : null;
        }

        private void ConfigureButtons()
        {
            // Une los botones de respuesta y retorno con los metodos publicos del controlador.
            if (leftAnimalButton == null && leftAnimalImage != null)
            {
                leftAnimalButton = leftAnimalImage.GetComponent<Button>();
            }

            if (rightAnimalButton == null && rightAnimalImage != null)
            {
                rightAnimalButton = rightAnimalImage.GetComponent<Button>();
            }

            if (leftAnimalButton != null)
            {
                leftAnimalButton.onClick.RemoveAllListeners();
                leftAnimalButton.onClick.AddListener(SelectLeftAnimal);
            }

            if (rightAnimalButton != null)
            {
                rightAnimalButton.onClick.RemoveAllListeners();
                rightAnimalButton.onClick.AddListener(SelectRightAnimal);
            }

            if (returnButton != null)
            {
                returnButton.onClick.RemoveAllListeners();
                returnButton.onClick.AddListener(ReturnToWorldSelection);
            }
        }

        private void PrepareInitialState()
        {
            // Estado de reposo: sin ronda activa, sin resultado visible y estrellas completas.
            currentStars = 3;
            mistakeCount = 0;
            completedRounds = 0;
            acceptingAnswer = false;
            activityFinished = false;
            UpdateStarsUi();

            if (feedbackText != null)
            {
                feedbackText.text = string.Empty;
            }

            if (resultPanel != null)
            {
                resultPanel.SetActive(false);
            }
        }

        private void UpdateStarsUi()
        {
            // Sincroniza estrellas manuales o UIStarDisplay segun lo configurado en escena.
            if (starDisplay != null) starDisplay.SetImmediate(currentStars);
            if (starImages == null) return;

            for (int i = 0; i < starImages.Length; i++)
            {
                Image starImage = starImages[i];
                if (starImage == null) continue;

                Sprite targetSprite = i < currentStars ? fullStarSprite : emptyStarSprite;
                if (targetSprite != null)
                {
                    starImage.sprite = targetSprite;
                    starImage.color = Color.white;
                }
                else
                {
                    starImage.color = i < currentStars ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                }
            }
        }

        private void SaveProgress()
        {
            // Guarda el mejor puntaje del mundo de tamanos en el progreso global.
            int bestStars = WorldProgressRepository.SaveBestResult(WorldIndex, currentStars);
            Debug.Log($"SizeWorldController: progreso guardado. Estrellas actuales: {currentStars}. Mejor puntaje: {bestStars}.");
        }

        private void SetAnswerButtonsInteractable(bool interactable)
        {
            if (leftAnimalButton != null) leftAnimalButton.interactable = interactable;
            if (rightAnimalButton != null) rightAnimalButton.interactable = interactable;
        }

        private void SetQuestion(string message)
        {
            if (questionText != null)
            {
                questionText.text = message;
            }
        }

        private void StopRoundRoutine()
        {
            // Detiene animaciones/esperas pendientes de la ronda actual.
            if (roundRoutine == null) return;

            StopCoroutine(roundRoutine);
            roundRoutine = null;
        }
    }
}
