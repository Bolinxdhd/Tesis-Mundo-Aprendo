using TMPro;
using UnityEngine;

namespace Bolin {

public class ManagerVideoDropdow : MonoBehaviour
{
    [SerializeField] private ManagerVideo managerVideo;
    [SerializeField] private TMP_Dropdown dropdown;

    private void Awake()
    {
        if (managerVideo == null)
        {
            managerVideo = GetComponent<ManagerVideo>();
        }
    }

    private void Start()
    {
        if (managerVideo == null || dropdown == null) return;

        dropdown.options.Clear();
        for (int i = 0; i < managerVideo.Limit; i++)
        {
            dropdown.options.Add(new TMP_Dropdown.OptionData(managerVideo.GetInfoResolution(i)));
        }

        int currentResolution = managerVideo.GetResolution();
        if (currentResolution >= 0)
        {
            dropdown.SetValueWithoutNotify(currentResolution);
        }

        dropdown.onValueChanged.AddListener(managerVideo.ApplyChangeResolution);
        dropdown.RefreshShownValue();
    }

    private void OnDestroy()
    {
        if (dropdown != null && managerVideo != null)
        {
            dropdown.onValueChanged.RemoveListener(managerVideo.ApplyChangeResolution);
        }
    }
}
}
