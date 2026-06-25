using UnityEngine;
namespace Bolin
{
    public class ButtonUI : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private GameObject ShowPanel;
        [SerializeField] private GameObject hidePanel;
        public void OnClick()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.managePanel(clickSound, ShowPanel, hidePanel);
                return;
            }

            if (ShowPanel != null) ShowPanel.SetActive(true);
            if (hidePanel != null) hidePanel.SetActive(false);
        }
    }
}
