using System;
using System.Linq;
using Assets.SurpriseBox.Scripts;
using Bolin;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bolin.Editor
{
    /// <summary>
    /// Copies only the persisted result-panel presentation authored in MundoTamanos
    /// into MundoMusical. Gameplay, tutorial and every other scene are untouched.
    /// </summary>
    public static class MundoMusicalResultPanelFromTamanosSetup
    {
        private const string SizeScenePath = "Assets/Scenes/MundoTamanos.unity";
        private const string MusicalScenePath = "Assets/Scenes/MundoMusical.unity";

        [MenuItem("Mundo Aprendo/Mundo Musical/Usar resultado de Mundo de los Tamaños")]
        public static void Apply()
        {
            Scene musicalScene = EditorSceneManager.OpenScene(MusicalScenePath, OpenSceneMode.Single);
            Scene sizeScene = EditorSceneManager.OpenScene(SizeScenePath, OpenSceneMode.Additive);
            RectTransform sizeResult = FindRequired<RectTransform>(sizeScene, "Panel-resultado");
            Transform sizeShadow = RequireChild(sizeResult, "SombraResultado");
            Transform sizeContent = RequireChild(sizeResult, "ContenidoResultado");

            RectTransform musicalResult = FindRequired<RectTransform>(musicalScene, "ResultView");
            MundoMusicalSequenceGame game = FindRequired<MundoMusicalSequenceGame>(musicalScene, null);

            DeleteChildren(musicalResult);
            CopyRect(sizeResult, musicalResult);
            CopyImage(sizeResult.GetComponent<Image>(), RequireComponent<Image>(musicalResult.gameObject));

            GameObject shadow = UnityEngine.Object.Instantiate(sizeShadow.gameObject, musicalResult, false);
            shadow.name = sizeShadow.name;
            GameObject content = UnityEngine.Object.Instantiate(sizeContent.gameObject, musicalResult, false);
            content.name = sizeContent.name;

            Button replayButton = RequireChild(content.transform, "Boton-reintentar").GetComponent<Button>();
            Button returnButton = RequireChild(content.transform, "Boton-volver-seleccion").GetComponent<Button>();
            ConfigureResultButton(replayButton, game.ReplayActivity);
            ClearPersistentListeners(returnButton);

            Transform starsRoot = RequireChild(content.transform, "ResultStars");
            UIStarDisplay resultStars = RequireComponent<UIStarDisplay>(starsRoot.gameObject);
            Image[] stars = Enumerable.Range(1, 3)
                .Select(index => RequireChild(starsRoot, "EstrellaResultado_" + index).GetComponent<Image>())
                .ToArray();
            TMP_Text resultText = RequireComponent<TMP_Text>(RequireChild(content.transform, "Texto-resultado").gameObject);

            SerializedObject gameSerialized = new(game);
            gameSerialized.FindProperty("resultPanel").objectReferenceValue = musicalResult.gameObject;
            gameSerialized.FindProperty("tmpResultText").objectReferenceValue = resultText;
            gameSerialized.FindProperty("returnButton").objectReferenceValue = returnButton;
            gameSerialized.FindProperty("starDisplay").objectReferenceValue = resultStars;
            SetObjectReferenceArray(gameSerialized, "starImages", stars);
            gameSerialized.ApplyModifiedPropertiesWithoutUndo();

            MusicalLevelAnimationController animationController = game.GetComponent<MusicalLevelAnimationController>();
            if (animationController != null)
            {
                SerializedObject animationSerialized = new(animationController);
                // The copied UIStarDisplay already owns Tamaños' star reveal. Do not retain
                // Mundo Musical's previous decorative star animation behind the new panel.
                animationSerialized.FindProperty("resultStarAnimation").objectReferenceValue = null;
                animationSerialized.ApplyModifiedPropertiesWithoutUndo();
            }

            musicalResult.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(musicalScene);
            EditorSceneManager.SaveScene(musicalScene);
            Debug.Log("MundoMusicalResultPanelFromTamanosSetup: panel de resultado de Tamaños aplicado solo a Mundo Musical.");
        }

        private static void ConfigureResultButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) throw new InvalidOperationException("Falta un botón del panel de resultado de Tamaños.");
            ClearPersistentListeners(button);
            UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        private static void ClearPersistentListeners(Button button)
        {
            if (button == null) throw new InvalidOperationException("Falta un botón del panel de resultado de Tamaños.");
            while (button.onClick.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(button.onClick, 0);
        }

        private static void DeleteChildren(Transform parent)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                UnityEngine.Object.DestroyImmediate(parent.GetChild(index).gameObject);
            }
        }

        private static void CopyRect(RectTransform source, RectTransform destination)
        {
            destination.anchorMin = source.anchorMin;
            destination.anchorMax = source.anchorMax;
            destination.pivot = source.pivot;
            destination.anchoredPosition = source.anchoredPosition;
            destination.sizeDelta = source.sizeDelta;
            destination.localScale = source.localScale;
            destination.localRotation = source.localRotation;
        }

        private static void CopyImage(Image source, Image destination)
        {
            if (source == null || destination == null) return;
            destination.sprite = source.sprite;
            destination.color = source.color;
            destination.type = source.type;
            destination.preserveAspect = source.preserveAspect;
            destination.fillCenter = source.fillCenter;
            destination.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            destination.raycastTarget = source.raycastTarget;
        }

        private static void SetObjectReferenceArray(SerializedObject target, string propertyName, UnityEngine.Object[] values)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property == null || !property.isArray) throw new InvalidOperationException($"No se encontró {propertyName}.");
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }

        private static Transform RequireChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null) throw new InvalidOperationException($"Falta {name} dentro de {parent.name}.");
            return child;
        }

        private static T FindRequired<T>(Scene scene, string objectName) where T : Component
        {
            T[] components = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
            T component = string.IsNullOrEmpty(objectName)
                ? components.SingleOrDefault()
                : components.SingleOrDefault(item => item.name == objectName);
            if (component == null) throw new InvalidOperationException($"No se encontró {objectName ?? typeof(T).Name} en {scene.path}.");
            return component;
        }

        private static T RequireComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null) throw new InvalidOperationException($"{gameObject.name} requiere {typeof(T).Name}.");
            return component;
        }
    }
}
