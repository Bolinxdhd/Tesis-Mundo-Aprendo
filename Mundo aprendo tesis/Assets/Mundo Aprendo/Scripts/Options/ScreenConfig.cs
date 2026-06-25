using System;

namespace Bolin {

    [Serializable]
    public class ScreenConfig {
        
        // Anchura de la resolucion de pantalla
        public int Width;

        // Altura de la resolucion de pantalla
        public int Height;

        // Pantalla completa con bordes
        public int ScreenMode;

        // Sincronizacion vertical habilitada
        public int V_Sync;

        // Si esv erdadero los fotogramas se encuentran limitados
        public bool LimitFps;


        
        // Constructor sin parametros
        public ScreenConfig(){
            
        }

        
        // Constructor con parametros
        public ScreenConfig(int width, int height, int screenMode,
                int vSync, bool limitFps){
            Width = width;
            Height = height;
            ScreenMode = screenMode;
            V_Sync = vSync;
            LimitFps = limitFps;
        }
    }
}