using UnityEngine;
using UnityEngine.Serialization;

namespace CartoonUI
{
    public class Close : MonoBehaviour
    {
        [FormerlySerializedAs("gameObject")]
        [SerializeField] private GameObject targetObject;

        public void close()
        {
            if (targetObject != null)
            {
                targetObject.SetActive(false);
            }
        }
    }
}
