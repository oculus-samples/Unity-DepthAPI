using UnityEngine;

[DisallowMultipleComponent]
public class HandCaptureSettingsBootstrapper : MonoBehaviour
{
    [Tooltip("Settings asset to apply on startup")] 
    public HandCaptureSettings settings;

    private void Awake()
    {
        if (settings != null)
        {
            settings.Apply();
        }
    }
}
