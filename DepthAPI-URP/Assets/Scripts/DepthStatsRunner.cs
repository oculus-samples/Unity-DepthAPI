using UnityEngine;
using UnityEngine.UI;

public class DepthStatsRunnerUI : MonoBehaviour
{
    [Header("Inputs")]
    public ComputeShader depthStatsCS;   // kernel name: "DepthStats"
    public RenderTexture depthRT;        // your RGBAFloat RT (meters in .r)
    [Tooltip("<= treated as black")]
    [Range(-1f, 0.01f)] public float threshold = 0f;
    public float updateInterval = 0.25f;

    [Header("UI References")]
    public Text debugText;               // assign your DebugText here

    ComputeBuffer partials;              // float2 per group: (sum, count)
    int kernel;
    float timer;

    void Awake() { kernel = depthStatsCS.FindKernel("DepthStats"); }
    void OnDestroy() { partials?.Dispose(); }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < updateInterval) return;
        timer = 0f;

        if (!depthRT || !depthStatsCS || !debugText) return;

        var (count, avg) = RunOnce();
        debugText.text = $"Non-black: {count:n0}\nAvg (m): {avg:0.###}";
    }

    (long count, float avg) RunOnce()
    {
        int w = depthRT.width, h = depthRT.height;
        int gx = (w + 15) / 16, gy = (h + 15) / 16;
        int groups = gx * gy;

        // allocate buffer (stride = sizeof(float2) = 8 bytes)
        if (partials == null || partials.count != groups)
        {
            partials?.Dispose();
            partials = new ComputeBuffer(groups, sizeof(float) * 2);
        }

        depthStatsCS.SetInts("_TexSize", w, h);
        depthStatsCS.SetFloat("_Threshold", threshold);
        depthStatsCS.SetTexture(kernel, "_DepthTex", depthRT);   // read-only SRV is fine
        depthStatsCS.SetBuffer(kernel, "_GroupOut", partials);

        depthStatsCS.Dispatch(kernel, gx, gy, 1);

        var tmp = new float[groups * 2];
        partials.GetData(tmp);

        double totalSum = 0.0;
        double totalCnt = 0.0;
        for (int i = 0; i < groups; i++)
        {
            totalSum += tmp[i * 2 + 0];
            totalCnt += tmp[i * 2 + 1];
        }

        long count = (long)System.Math.Round(totalCnt);
        float average = count > 0 ? (float)(totalSum / totalCnt) : 0f;
        return (count, average);
    }
}
