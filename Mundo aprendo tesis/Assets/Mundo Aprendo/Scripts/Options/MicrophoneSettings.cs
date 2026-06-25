using UnityEngine;

namespace Bolin
{
    public static class MicrophoneSettings
    {
        public const string SelectedMicrophoneKey = "selected_microphone_device";

        public static string SelectedDeviceName
        {
            get => PlayerPrefs.GetString(SelectedMicrophoneKey, string.Empty);
            set
            {
                PlayerPrefs.SetString(SelectedMicrophoneKey, value ?? string.Empty);
                PlayerPrefs.Save();
            }
        }

        public static string GetAvailableSelectedDevice()
        {
            string selectedDevice = SelectedDeviceName;
            string[] devices = Microphone.devices;

            if (devices == null || devices.Length == 0)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(selectedDevice))
            {
                foreach (string device in devices)
                {
                    if (device == selectedDevice)
                    {
                        return selectedDevice;
                    }
                }
            }

            SelectedDeviceName = devices[0];
            return devices[0];
        }

        public static bool HasAnyMicrophone()
        {
            return Microphone.devices != null && Microphone.devices.Length > 0;
        }
    }
}
