using UnityEngine;

namespace Bolin
{
    public class ButtonClickSound : MonoBehaviour
    {
        [SerializeField] private AudioClip clickSound;

        public void Configure(AudioClip clip)
        {
            clickSound = clip;
        }

        public void PlayClick()
        {
            AudioManager.TryPlaySfx(clickSound);
        }
    }
}
