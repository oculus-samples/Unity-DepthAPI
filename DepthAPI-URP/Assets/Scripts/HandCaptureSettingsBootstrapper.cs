using UnityEngine;
using PassthroughCameraSamples;

[DisallowMultipleComponent]
public class HandCaptureSettingsBootstrapper : MonoBehaviour
{
    [Tooltip("Settings asset to apply on startup")]
    public HandCaptureSettings settings;

    public WebCamTextureManager camMgr;

    private void Awake()
    {
        if (settings != null)
        {
            settings.Apply();

            if (camMgr != null)
            {
                camMgr.Eye = settings.eye;
            }
        }
    }
}
