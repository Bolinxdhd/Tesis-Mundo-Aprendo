using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Bolin
{

    public struct VideoState
    {
        public int hz;
        public int height;
        public int width;
        public FullScreenMode mode;
        public int vSyncCount;
        public int targetFrameRate; // -1 = sin límite
        public bool isFpsLimited;   // true si estás forzando un límite (p.ej. 60)
        public override string ToString() =>
            $"{width}x{height} @ {hz} Hz | {mode} | vSync:{vSyncCount} | targetFPS:{(targetFrameRate < 0 ? "unlimited" : targetFrameRate.ToString())}";
    }


    public class ManagerVideo : MonoBehaviour
    {

        // Listado de todas las posibles resoluciones del juego
        private Resolution[] resolutions;

        // Referencia del script "ScreenConfig"
        private ScreenConfig settings = new();


        // Se emplea para poder almacenar el nombre del fichero que almcacena la configuracion de video
        private readonly string filePathVideo = "/screenSettings.json";

        // Almacena la ruta del fichero de configuracion de video
        private string filePath;

        [SerializeField] private Toggle vSyncToggle;
        [SerializeField] private Toggle limitFpsToggle;

        public int Limit => resolutions == null ? 0 : resolutions.Length;



        // Constructor sin parametros
        private void Awake()
        {
            // Se obtiene todas las posibles resoluciones de pantalla del monitor
            resolutions = Screen.resolutions;
            filePath = Application.persistentDataPath + filePathVideo;
            LoadOrCreateScreenSettings();
        }


        // Permite cargar o crear la configuracion de pantalla
        private void LoadOrCreateScreenSettings()
        {
            // Si el archivo existe, lo carga
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                settings = JsonUtility.FromJson<ScreenConfig>(json);

                // Asegura que la resolución guardada existe; si no, corrige y guarda
                EnsureValidResolution();

                // Si no existe el archivo, crear uno con valores predeterminados
            }
            else
            {
                InitialAdjust();
                SaveScreenSettings();
                // Debug.Log($"[ManagerVideo] Video Creado: {GetCurrentVideoState()}");
            }
            SetResolutionScreen();
            ActiveVsync();
            ActiveLimitFps();
            SyncVideoToggles();
        }


        // Permite guardar los cambios con respecto al ajuste de pantalla
        private void SaveScreenSettings()
        {
            string json = JsonUtility.ToJson(settings, true);
            File.WriteAllText(filePath, json);
            // Debug.Log($"[Video] Guardado JSON en: {filePath}\n{json}");
        }


        // Valores iniciales de la configuracion de pantalla
        private void InitialAdjust()
        {
            settings = new(
                Screen.currentResolution.width,
                Screen.currentResolution.height,
                (int)FullScreenMode.ExclusiveFullScreen,
                0,
                false
            );
        }


        // Se emplea este metodo para poder actualizar y aplicar los cambios de monitor
        public void ApplyChangeResolution(int index)
        {
            if (resolutions == null || resolutions.Length == 0) return;

            index = Mathf.Clamp(index, 0, resolutions.Length - 1);
            Resolution newResolution = resolutions[index];

            // Guarda en settings
            settings.Width = newResolution.width;
            settings.Height = newResolution.height;
            SaveScreenSettings();

            // Aplica resolución y modo de pantalla actual guardado
#if UNITY_2022_2_OR_NEWER
            // En 2022.2+ SetResolution no acepta hz explícito; la combinación (res,modo) debe existir.
            Screen.SetResolution(settings.Width, settings.Height, (FullScreenMode)settings.ScreenMode);
#else
            // En versiones con overload preferido:
            Screen.SetResolution(settings.Width, settings.Height, (FullScreenMode)settings.ScreenMode, newResolution.refreshRate);
#endif
        }

        /*
        // Se emplea este metodo para poder actualizar y aplicar los cambios de monitor
        public void ApplyChangeResolution(int index){
            Resolution newResolution = resolutions[index];
            // Se fija la resolucion de la pantalla y se guarda la configuracion
            settings.Width = newResolution.width;
            settings.Height = newResolution.height;
            SaveScreenSettings();
        }
        */


        // Se emplea para fijar la resolucion de la pantalla
        private void SetResolutionScreen()
        {
            // Se fija la resolucion de la pantalla
            Screen.SetResolution(settings.Width, settings.Height, (FullScreenMode)settings.ScreenMode);
        }


        // Permite obtener el indice de resolucion de la pantalla que se esta usando
        public int GetResolution()
        {
            if (resolutions == null || resolutions.Length == 0) return -1;

            // Se recorre todas las resoluciones de pantalla
            for (int i = 0; i < resolutions.Length; i++)
            {
                // Se compara la resolucion actual con la almacenada
                if (resolutions[i].width == settings.Width
                        && resolutions[i].height == settings.Height)
                {
                    return i;
                }
            }
            return -1;
        }


        // Permite obtener la informacion descriptiva de la pantalla
        public string GetInfoResolution(int index)
        {
            if (resolutions == null || resolutions.Length == 0)
            {
                return $"{Screen.currentResolution.width}x{Screen.currentResolution.height} @ {GetHz(Screen.currentResolution)} Hz";
            }
            if (index < 0 || index >= resolutions.Length)
            {
                int found = GetResolution();
                index = Mathf.Clamp(found < 0 ? 0 : found, 0, resolutions.Length - 1);
            }

            Resolution resol = resolutions[index];
            int hz = GetHz(resol);
            return $"{resol.width}x{resol.height} @ {hz} Hz";
        }


        // Helper para obtener los Hz de forma compatible
        private static int GetHz(Resolution r)
        {
#if UNITY_2022_2_OR_NEWER
            // En 2022.2+ existe refreshRateRatio (float). Se redondea al entero más cercano.
            return Mathf.RoundToInt((float)r.refreshRateRatio.value);
#else
            // En versiones anteriores es un int
            return r.refreshRate;
#endif
        }


        // Overload para currentResolution (por si lo quieres usar en retornos tempranos)
        private static int GetHz(Resolution r, bool isCurrent)
        {
#if UNITY_2022_2_OR_NEWER
            return Mathf.RoundToInt((float)r.refreshRateRatio.value);
#else
            return r.refreshRate;
#endif
        }


        public void SaveCurrentEffectiveStateToJson()
        {
            var cur = GetCurrentVideoState(); // tu struct
            settings.Width = cur.width;
            settings.Height = cur.height;
            settings.ScreenMode = (int)cur.mode;
            settings.V_Sync = cur.vSyncCount;
            settings.LimitFps = cur.isFpsLimited;
            SaveScreenSettings();
        }


        private void EnsureValidResolution()
        {
            if (resolutions == null || resolutions.Length == 0) return;

            int idx = GetResolution();
            if (idx == -1)
            {
                int fallback = GetClosestToCurrent();
                settings.Width = resolutions[fallback].width;
                settings.Height = resolutions[fallback].height;
                SaveScreenSettings();
            }
        }

        private int GetClosestToCurrent()
        {
            if (resolutions == null || resolutions.Length == 0) return 0;

            int cw = Screen.currentResolution.width;
            int ch = Screen.currentResolution.height;

            int best = 0;
            int bestScore = int.MaxValue;
            for (int i = 0; i < resolutions.Length; i++)
            {
                int dw = Mathf.Abs(resolutions[i].width - cw);
                int dh = Mathf.Abs(resolutions[i].height - ch);
                int score = dw + dh;
                if (score < bestScore) { best = i; bestScore = score; }
            }
            return best;
        }


        public static VideoState GetCurrentVideoState()
        {
            var r = Screen.currentResolution;
            return new VideoState
            {
                width = Screen.width,
                height = Screen.height,
                hz = GetHz(r),
                mode = Screen.fullScreenMode,
                vSyncCount = QualitySettings.vSyncCount,
                targetFrameRate = Application.targetFrameRate,
                isFpsLimited = Application.targetFrameRate > 0
            };
        }


        // Se emplea para fijar el tamanio de la pantalla
        public void SetScreenMode()
        {
            // Alterna entre FullScreenWindow y ExclusiveFullScreen
            switch (settings.ScreenMode)
            {
                case 0:
                    Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                    settings.ScreenMode = 1;
                    break;
                case 1:
                    Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                    settings.ScreenMode = 0;
                    break;
            }
            SaveScreenSettings();
        }


        // Se emplea para poder activar/Descativar la sincornizacion vertical
        public void SetV_Sync()
        {
            SetV_Sync(vSyncToggle == null ? GetSettingScreen(3) == 0 : vSyncToggle.isOn);
        }

        public void SetV_Sync(bool enabled)
        {
            QualitySettings.vSyncCount = enabled ? 1 : 0;
            settings.V_Sync = QualitySettings.vSyncCount;
            SaveScreenSettings();
        }


        public void SetLimitFps()
        {
            SetLimitFps(limitFpsToggle == null ? !settings.LimitFps : limitFpsToggle.isOn);
        }

        public void SetLimitFps(bool enabled)
        {
            Application.targetFrameRate = enabled ? 60 : -1;
            settings.LimitFps = enabled;
            SaveScreenSettings();
        }


        // Permite devolver el indice de valor a modificar en resolucion de pantalla
        public int GetSettingScreen(int index)
        {
            return index switch
            {
                0 => settings.Width,
                1 => settings.Height,
                2 => settings.ScreenMode,
                3 => settings.V_Sync,
                _ => settings.LimitFps ? 0 : 1
            };
        }


        // Permite resetear los valores por defecto
        public void ResetDefaults()
        {
            InitialAdjust();
            SaveScreenSettings();
            SetResolutionScreen();
            ActiveVsync();
            ActiveLimitFps();
            SyncVideoToggles();
        }


        // Permite gestionar si se encuentra habilitado o no la sincorizacion vertical
        private void ActiveVsync()
        {
            QualitySettings.vSyncCount = GetSettingScreen(3);
        }


        // Permite gestionar si se encuentra habilitado o no el limite de FPS
        private void ActiveLimitFps()
        {
            // Application.targetFrameRate = GetSettingScreen(4) == 0 ? -1 : 60;
            Application.targetFrameRate = settings.LimitFps ? 60 : -1;
        }


        private void SyncVideoToggles()
        {
            if (vSyncToggle != null)
            {
                vSyncToggle.SetIsOnWithoutNotify(settings.V_Sync > 0);
            }

            if (limitFpsToggle != null)
            {
                limitFpsToggle.SetIsOnWithoutNotify(settings.LimitFps);
            }
        }
    }
}
