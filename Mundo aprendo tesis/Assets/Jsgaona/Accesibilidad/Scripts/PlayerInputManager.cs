using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Assets.SurpriseBox.Scripts {

    // Se encarga de gestionar las entradas del jugador
    public class PlayerInputManager {
        private const string RebindsKey = "input_rebinds";
        
        // Variable estatica que contiene las acciones de entrada del jugador
        private static PlayerInputController inputAction;
        

        
        // Metodo que devuelve las acciones de entrada del jugador
        public static PlayerInputController GetInputActions() {
            if (inputAction == null) {
                inputAction = new PlayerInputController();
                LoadSavedRebinds();
                if (Application.isPlaying) {
                    inputAction.Enable();
                }
            }
            return inputAction;
        }

        public static void SaveRebinds() {
            if (inputAction == null) return;
            string json = inputAction.asset.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString(RebindsKey, json);
            PlayerPrefs.Save();
        }

        public static void LoadSavedRebinds() {
            if (inputAction == null || !PlayerPrefs.HasKey(RebindsKey)) return;
            string json = PlayerPrefs.GetString(RebindsKey);
            if (string.IsNullOrWhiteSpace(json)) return;
            inputAction.asset.LoadBindingOverridesFromJson(json);
        }

        public static void DeleteSavedRebinds() {
            PlayerPrefs.DeleteKey(RebindsKey);
            PlayerPrefs.Save();
        }

        public static void ResetAllRebinds() {
            PlayerInputController actions = GetInputActions();
            actions.asset.RemoveAllBindingOverrides();
            DeleteSavedRebinds();
        }

        public static void ReleaseInputActions() {
            if (inputAction == null) return;

            inputAction.Disable();

            if (inputAction.asset != null) {
                if (Application.isPlaying) {
                    inputAction.Dispose();
                } else {
                    Object.DestroyImmediate(inputAction.asset);
                }
            }

            inputAction = null;
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void RegisterEditorCleanup() {
            AssemblyReloadEvents.beforeAssemblyReload -= ReleaseInputActions;
            AssemblyReloadEvents.beforeAssemblyReload += ReleaseInputActions;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state) {
            if (state == PlayModeStateChange.ExitingPlayMode) {
                ReleaseInputActions();
            }
        }
#endif


        // Metodo que deshabilita las acciones de entrada del jugador
        public static void DisableInputActions() {
            inputAction?.Disable();
        }


        // Metodo que habilita las acciones de entrada del jugador
        public static void EnablePlayer() {
            GetInputActions().Character.Enable();
        }


        // Metodo que deshabilita las acciones de entrada del jugador
        public static void DisablePlayer() {
            GetInputActions().Character.Disable();
        }


        // Metodo que habilita las acciones de entrada de la interfaz de usuario
        public static void EnableUI() {
            GetInputActions().UserInterface.Enable();
        }


        // Metodo que deshabilita las acciones de entrada de la interfaz de usuario
        public static void DisableUI() {
            GetInputActions().UserInterface.Disable();
        }


        // Metodo que habilita las acciones de entrada de juego
        public static void EnableGameplay() {
            GetInputActions().GamePlay.Enable();
        }


        // Metodo que deshabilita las acciones de entrada de juego
        public static void DisableGameplay() {
            GetInputActions().GamePlay.Disable();
        }
    }
}
