using UnityEngine;
using UnityEngine.UI;
 

public class DepthStatsRunner : MonoBehaviour
{
    [Header("Inputs")]
    public ComputeShader depthStatsCS;   // kernel: "DepthStats"
    public RenderTexture depthRT;        // meters in .r
    public float bandMin = 0.13f;
    public float bandMax = 0.27f;
    public float updateInterval = 0.25f;

    [Header("Events")]
    public StatsEvent OnStats;

    [Header("UI")]
    public Text debugText;

    private ComputeBuffer m_partials;              // float4 per group: (sum, count, sumSq, _)
    private int m_kernel;
    private float m_timer;

    private void Awake() { m_kernel = depthStatsCS.FindKernel("DepthStats"); }

    private void OnDestroy() { m_partials?.Dispose(); }

    private void Update()
    {
        m_timer += Time.deltaTime;
        if (m_timer < updateInterval) return;
        m_timer = 0f;

        if (!depthRT || !depthStatsCS) return;

        var (count, mean, stdPop, stdSample) = RunOnce();

        if (debugText)
            debugText.text =
                $"Count: {count:n0}\n" +
                $"Mean (m): {mean:0.###}\n" +
                $"s (pop): {stdPop:0.###}\n" +
                $"s (n-1): {stdSample:0.###}";

        if (OnStats != null)
        {
            var stats = new DepthStats
            {
                count = count,
                mean = mean,
                stdPop = stdPop,
                stdSample = stdSample
            };
            OnStats.Invoke(stats);
        }

    }

    private (long count, float mean, float stdPop, float stdSample) RunOnce()
    {
        int w = depthRT.width, h = depthRT.height;
        int gx = (w + 15) / 16, gy = (h + 15) / 16;
        var groups = gx * gy;

        if (m_partials == null || m_partials.count != groups)
        {
            m_partials?.Dispose();
            m_partials = new ComputeBuffer(groups, sizeof(float) * 4);
        }

        depthStatsCS.SetInts("_TexSize", w, h);
        depthStatsCS.SetFloat("_BandMin", bandMin);
        depthStatsCS.SetFloat("_BandMax", bandMax);
        depthStatsCS.SetTexture(m_kernel, "_DepthTex", depthRT);
        depthStatsCS.SetBuffer(m_kernel, "_GroupOut", m_partials);
        depthStatsCS.Dispatch(m_kernel, gx, gy, 1);

        var data = new float[groups * 4];
        m_partials.GetData(data);

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
