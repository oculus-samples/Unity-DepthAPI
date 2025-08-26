using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DepthStatsRunnerUI : MonoBehaviour
{
    [Header("Inputs")]
    public ComputeShader depthStatsCS;   // kernel: "DepthStats"
    public RenderTexture depthRT;        // meters in .r
    public float bandMin = 0.13f;
    public float bandMax = 0.27f;
    public float updateInterval = 0.25f;

    [Header("Event")]
    public UnityEvent ThresholdMet;  // <-- Hook functions here in Inspector
    public float meanThresholdMin;
    public float meanThresholdMax;
    public float stdThresholdMin;
    public float stdThresholdMax;
    bool _wasInRangeLastFrame = false;

    [Header("UI")]
    public Text debugText;
    public Text userFeedbackText;

    ComputeBuffer partials;              // float4 per group: (sum, count, sumSq, _)
    int kernel;
    float timer;

    void Awake() { kernel = depthStatsCS.FindKernel("DepthStats"); }
    void OnDestroy() { partials?.Dispose(); }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < updateInterval) return;
        timer = 0f;

        if (!depthRT || !debugText || !depthStatsCS) return;

        var (count, mean, stdPop, stdSample) = RunOnce();
        bool meanInRange = mean >= meanThresholdMin && mean <= meanThresholdMax;
        bool stdInRange = stdPop >= stdThresholdMin && stdPop <= stdThresholdMax;
        bool inRangeNow = count > 0 && meanInRange && stdInRange;

        if (inRangeNow && !_wasInRangeLastFrame)
            ThresholdMet?.Invoke();

        _wasInRangeLastFrame = inRangeNow;

        if (debugText)
            debugText.text =
                $"Count: {count:n0}\n" +
                $"Mean (m): {mean:0.###}\n" +
                $"σ (pop): {stdPop:0.###}\n" +
                $"σ (n-1): {stdSample:0.###}\n" +
                $"in range: {inRangeNow.ToString()}";

        BuildUserFeedbackText(count, mean, stdPop);

    }

    public void BuildUserFeedbackText(long count, float mean, float stdPop)
    {

        if (userFeedbackText)
        {
            var sb = new StringBuilder();

            // If we didn’t measure anything:
            if (count == 0)
            {
                sb.AppendLine("Move hand into the square");
                userFeedbackText.text = sb.ToString();
                return;
            }

            // Distance guidance
            if (mean > meanThresholdMax) sb.AppendLine("Move hand closer");
            else if (mean < meanThresholdMin && mean >= bandMin) sb.AppendLine("Move hand further");

            // Flatness guidance
            if (stdPop >= stdThresholdMax) sb.AppendLine("Try to make your hand flatter");

            if (sb.Length == 0) sb.AppendLine("✅ Looks good!");

            userFeedbackText.text = sb.ToString();
        }

        return;
    }

    (long count, float mean, float stdPop, float stdSample) RunOnce()
    {
        int w = depthRT.width, h = depthRT.height;
        int gx = (w + 15) / 16, gy = (h + 15) / 16;
        int groups = gx * gy;

        if (partials == null || partials.count != groups)
        {
            partials?.Dispose();
            partials = new ComputeBuffer(groups, sizeof(float) * 4);
        }

        depthStatsCS.SetInts("_TexSize", w, h);
        depthStatsCS.SetFloat("_BandMin", bandMin);
        depthStatsCS.SetFloat("_BandMax", bandMax);
        depthStatsCS.SetTexture(kernel, "_DepthTex", depthRT);
        depthStatsCS.SetBuffer(kernel, "_GroupOut", partials);
        depthStatsCS.Dispatch(kernel, gx, gy, 1);

        var data = new float[groups * 4];
        partials.GetData(data);

        double sum = 0.0, sumSq = 0.0, cnt = 0.0;
        for (int i = 0; i < groups; i++)
        {
            sum += data[i * 4 + 0];
            cnt += data[i * 4 + 1];
            sumSq += data[i * 4 + 2];
        }

        long n = (long)System.Math.Round(cnt);
        if (n <= 0) return (0, 0f, 0f, 0f);

        double mean = sum / cnt;
        // variance (population): E[x^2] - (E[x])^2
        double varPop = (sumSq / cnt) - (mean * mean);
        if (varPop < 0) varPop = 0; // guard numeric noise

        // sample variance: n/(n-1) * varPop
        double varSample = (n > 1) ? varPop * (cnt / (cnt - 1.0)) : 0.0;

        return (n, (float)mean, (float)System.Math.Sqrt(varPop), (float)System.Math.Sqrt(varSample));
    }
}
