using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CaptureManager : MonoBehaviour
{
    [Header("Depth Stats Source")]
    public DepthStatsRunner depthStatsSource;   // Assign in Inspector

    [Header("Thresholds")]
    public float meanThresholdMin;
    public float meanThresholdMax;
    public float stdThresholdMin;
    public float stdThresholdMax;

    [Header("Capture Settings")]
    public int targetSamples = 10;
    public float minTriggerIntervalSeconds = 1.0f; // seconds

    [Header("UI")]
    public Text userFeedbackText;
    public float bandMin = 0.13f; // for distance guidance text

    [Header("Event")]
    public UnityEvent Triggered;  // External hooks when threshold condition met

    private bool _wasInRangeLastFrame = false;
    private float _lastTriggerTime = -999f;
    private int _capturedCount = 0;

    private void OnEnable()
    {
        if (depthStatsSource != null)
            depthStatsSource.OnStats.AddListener(OnStats);
    }

    private void OnDisable()
    {
        if (depthStatsSource != null)
            depthStatsSource.OnStats.RemoveListener(OnStats);
    }

    private void OnStats(DepthStats stats)
    {
        var meanInRange = stats.mean >= meanThresholdMin && stats.mean <= meanThresholdMax;
        var stdInRange = stats.stdPop >= stdThresholdMin && stats.stdPop <= stdThresholdMax;
        var inRangeNow = stats.count > 0 && meanInRange && stdInRange;

        // Throttled triggering: allow even if still in range, but not more than 1 per interval
        if (inRangeNow && _capturedCount < targetSamples)
        {
            if (Time.time - _lastTriggerTime >= minTriggerIntervalSeconds)
            {
                _lastTriggerTime = Time.time;
                _capturedCount++;
                Triggered?.Invoke();
            }
        }

        _wasInRangeLastFrame = inRangeNow;

        BuildUserFeedbackText(stats.count, stats.mean, stats.stdPop);
    }

    public void BuildUserFeedbackText(long count, float mean, float stdPop)
    {
        if (!userFeedbackText) return;

        var sb = new StringBuilder();

        // If we didn't measure anything:
        if (count == 0)
        {
            _ = sb.AppendLine("Move hand into the square");
            userFeedbackText.text = sb.ToString();
            return;
        }

        // Distance guidance
        if (mean > meanThresholdMax) _ = sb.AppendLine("Move hand closer");
        else if (mean < meanThresholdMin && mean >= bandMin) _ = sb.AppendLine("Move hand further");

        // Flatness guidance
        if (stdPop >= stdThresholdMax) _ = sb.AppendLine("Try to make your hand flatter");

        if (sb.Length == 0)
        {
            _ = sb.AppendLine("Looks good!");

            if (targetSamples > 0)
            {
                if (_capturedCount >= targetSamples)
                {
                    _ = sb.AppendLine("Collection complete!");
                }
                else
                {
                    int nextIndex = Mathf.Min(_capturedCount + 1, targetSamples);
                    _ = sb.AppendLine($"Capturing sample {nextIndex}/{targetSamples}");
                }
            }
        }

        userFeedbackText.text = sb.ToString();
    }

    public void ResetCaptureCount()
    {
        _capturedCount = 0;
        _lastTriggerTime = -999f;
    }
}
