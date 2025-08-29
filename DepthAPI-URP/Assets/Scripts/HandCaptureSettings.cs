using UnityEngine;

[CreateAssetMenu(fileName = "HandCaptureSettings", menuName = "Settings/Hand Capture Settings")]
public class HandCaptureSettings : ScriptableObject
{
    [Min(0f)] public float minMeters = 0.17f;
    [Min(0f)] public float maxMeters = 0.30f;

    [Header("Capture Thresholds")]
    public float meanThresholdMin = 0f;
    public float meanThresholdMax = 0f;
    public float stdThresholdMin = 0f;
    public float stdThresholdMax = 0f;

    public void Apply()
    {
        // Let HandCaptureGlobals handle clamping and shader global updates
        HandCaptureGlobals.Apply(minMeters, maxMeters);
        // Also apply capture thresholds to globals
        HandCaptureGlobals.ApplyThresholds(meanThresholdMin, meanThresholdMax, stdThresholdMin, stdThresholdMax);
    }

}
