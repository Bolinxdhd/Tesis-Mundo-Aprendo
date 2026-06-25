using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bolin
{
    public class SceneNavigation : MonoBehaviour
    {
        [SerializeField] private string targetSceneName = MundoAprendoSceneNames.WorldSelection;

        public void LoadTargetScene()
        {
            LoadScene(targetSceneName, this);
        }

        public void LoadMenu()
        {
            LoadScene(MundoAprendoSceneNames.Menu, this);
        }

        public void LoadWorldSelection()
        {
            LoadScene(MundoAprendoSceneNames.WorldSelection, this);
        }

        public static bool LoadScene(string sceneName, Object context = null)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("SceneNavigation: no se puede cargar una escena sin nombre.", context);
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"SceneNavigation: la escena '{sceneName}' no esta habilitada en Build Settings.", context);
                return false;
            }

            if (!UISceneTransition.TryLoadScene(sceneName)) SceneManager.LoadScene(sceneName);
            return true;
        }
    }
}
