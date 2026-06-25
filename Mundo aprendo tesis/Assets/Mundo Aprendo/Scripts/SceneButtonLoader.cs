using UnityEngine;

namespace Bolin
{
    public class SceneButtonLoader : MonoBehaviour
    {
        [SerializeField] private string sceneName;

        public void LoadAssignedScene()
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("No hay una escena asignada para cargar.");
                return;
            }

            SceneNavigation.LoadScene(sceneName, this);
        }
    }
}
