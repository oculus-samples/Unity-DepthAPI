using UnityEngine;

public static class HandCaptureGlobals
{
    public static readonly int MinId = Shader.PropertyToID("_DepthMinMeters");
    public static readonly int MaxId = Shader.PropertyToID("_DepthMaxMeters");

    public static float MinMeters { get; private set; } = 0f;
    public static float MaxMeters { get; private set; } = 0f;

    // Thresholds used by CaptureManager and related logic
    public static float MeanThresholdMin { get; private set; } = 0f;
    public static float MeanThresholdMax { get; private set; } = 0f;
    public static float StdThresholdMin { get; private set; } = 0f;
    public static float StdThresholdMax { get; private set; } = 0f;

    // Passthrough camera eye selection: 0 = Left, 1 = Right
    public static int EyeIndex { get; private set; } = 0;

    public static void Apply(float minMeters, float maxMeters)
    {
        if (maxMeters < minMeters) maxMeters = minMeters;
        MinMeters = minMeters;
        MaxMeters = maxMeters;

        Shader.SetGlobalFloat(MinId, MinMeters);
        Shader.SetGlobalFloat(MaxId, MaxMeters);
    }

    public static void ApplyThresholds(float meanMin, float meanMax, float stdMin, float stdMax)
    {
        MeanThresholdMin = meanMin;
        MeanThresholdMax = meanMax;
        StdThresholdMin  = stdMin;
        StdThresholdMax  = stdMax;
    }

    public static void ApplyEyeIndex(int eyeIndex)
    {
        EyeIndex = Mathf.Clamp(eyeIndex, 0, 1);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() => Apply(MinMeters, MaxMeters);
}
