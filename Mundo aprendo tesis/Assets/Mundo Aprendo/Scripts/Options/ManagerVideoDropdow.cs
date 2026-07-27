using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        if (dropdown == null)
        {
            dropdown = FindDropdownInActiveScene();
        }
    }

    private void Start()
    {
        if (managerVideo == null || dropdown == null) return;

        dropdown.options.Clear();
        if (managerVideo.Limit <= 0)
        {
            dropdown.options.Add(new TMP_Dropdown.OptionData(managerVideo.GetInfoResolution(-1)));
            dropdown.SetValueWithoutNotify(0);
            dropdown.interactable = false;
            dropdown.RefreshShownValue();
            return;
        }

        dropdown.interactable = true;
        for (int i = 0; i < managerVideo.Limit; i++)
        {
            dropdown.options.Add(new TMP_Dropdown.OptionData(managerVideo.GetInfoResolution(i)));
        }

        int currentResolution = managerVideo.GetResolution();
        if (currentResolution >= 0)
        {
            dropdown.SetValueWithoutNotify(currentResolution);
        }

        dropdown.onValueChanged.RemoveListener(managerVideo.ApplyChangeResolution);
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

    private static TMP_Dropdown FindDropdownInActiveScene()
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == "Panel-ajustevideo")
                {
                    return candidate.GetComponentInChildren<TMP_Dropdown>(true);
                }
            }
        }

        return null;
    }
}
}
