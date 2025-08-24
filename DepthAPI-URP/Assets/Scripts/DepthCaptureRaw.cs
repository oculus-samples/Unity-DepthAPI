/* DepthCaptureRaw.cs  –  dump Environment Depth exactly as stored on GPU
 *   • R16_UNorm   → depth_raw_slice#.raw   (binary ushort w×h)
 *   • RGBAHalf    → depth_pre_lin_slice#.exr  (4×half-float)
 */
using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class DepthCaptureRaw : MonoBehaviour
{
    [Header("Fallback blit (use your shader here)")]
    [SerializeField] Shader depthCopyShader;                 // assign ShowDepthMapRaw in Inspector
    Material _depthCopyMat;

    public string SavePreprocessedEnvironmentDepthTexture(int eye)
    {
        // prefer linear-depth version; fallback to raw clip-depth
        RenderTexture src = Shader.GetGlobalTexture("_PreprocessedEnvironmentDepthTexture") as RenderTexture;
        bool preprocessed = true;

        Debug.Log($"[DEPTH] {src.width}×{src.height}  slices:{src.volumeDepth}  gfxFmt:{src.graphicsFormat}");

        // ----------------------------------------------------------------
        // copy ONE slice → 2-D RT of IDENTICAL graphics format
        // ----------------------------------------------------------------
        RenderTexture sliceRT = RenderTexture.GetTemporary(src.width, src.height, 0, src.graphicsFormat);
        Graphics.CopyTexture(src, eye, 0, sliceRT, 0, 0);

        Texture2D cpu = new Texture2D(sliceRT.width, sliceRT.height, TextureFormat.RGBAHalf, false, true);
        ReadInto(cpu, sliceRT);
        LogMinMaxPerChannel(cpu, $"pre_lin slice{eye}");

        byte[] exr = cpu.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat |
                                        Texture2D.EXRFlags.CompressZIP);
        string path = Path.Combine(Application.persistentDataPath,
                                    $"depth_pre_lin_slice{eye}_{Time.frameCount}.exr");
        File.WriteAllBytes(path, exr);
        Debug.Log($"Saved {path}  ({exr.Length / 1024f:F1} KB)");
        Destroy(cpu);

        RenderTexture.ReleaseTemporary(sliceRT);

        return path;

    }

    public string SaveEnvironmentDepthTexture(int eye)
    {
        var src = Shader.GetGlobalTexture("_EnvironmentDepthTexture") as RenderTexture;

        _depthCopyMat = new Material(depthCopyShader) { hideFlags = HideFlags.HideAndDontSave };
        _depthCopyMat.SetInt("_Slice", eye);

        var halfRT = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        try
        {

            // If your shader uses a different texture property name, bind it manually:
            _depthCopyMat.SetTexture("_EnvironmentDepthTexture", src);
            Graphics.Blit(null, halfRT, _depthCopyMat, 0);

            // Read back the ARGBHalf result
            var cpu = new Texture2D(halfRT.width, halfRT.height, TextureFormat.RGBAHalf, false, true);
            ReadInto(cpu, halfRT);
            LogMinMaxPerChannel(cpu, $"lin slice{eye} (via {depthCopyShader.name})");

            byte[] exr = cpu.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat | Texture2D.EXRFlags.CompressZIP);
            string exrPath = Path.Combine(Application.persistentDataPath, $"depth_lin_slice{eye}_{Time.frameCount}_blit.exr");
            File.WriteAllBytes(exrPath, exr);
            Debug.Log($"Saved {exrPath}  ({exr.Length / 1024f:F1} KB)");
            Destroy(cpu);

            return exrPath;
        }
        finally
        {
            RenderTexture.ReleaseTemporary(halfRT);
        }
    }


    /* helper: GPU->CPU copy via ReadPixels (for RGBAHalf path) */
    static void ReadInto(Texture2D tex, RenderTexture srcRT)
    {
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = srcRT;
        tex.ReadPixels(new Rect(0, 0, srcRT.width, srcRT.height), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
    }


    static void LogMinMax_R16(NativeArray<ushort> data, string label)
    {
        ushort min = ushort.MaxValue, max = 0;
        for (int i = 0; i < data.Length; i++)
        {
            ushort v = data[i];
            if (v < min) min = v;
            if (v > max) max = v;
        }
        float minN = min / 65535f, maxN = max / 65535f;
        Debug.Log($"[DEPTH][{label}] min raw:{min} ({minN:F6}) | max raw:{max} ({maxN:F6})");
    }

    static void LogMinMaxPerChannel(Texture2D tex, string label)
    {
        var pixels = tex.GetPixels();
        float minR = float.PositiveInfinity, minG = float.PositiveInfinity,
              minB = float.PositiveInfinity, minA = float.PositiveInfinity;
        float maxR = float.NegativeInfinity, maxG = float.NegativeInfinity,
              maxB = float.NegativeInfinity, maxA = float.NegativeInfinity;

        for (int i = 0; i < pixels.Length; i++)
        {
            var c = pixels[i];
            if (c.r < minR) minR = c.r; if (c.r > maxR) maxR = c.r;
            if (c.g < minG) minG = c.g; if (c.g > maxG) maxG = c.g;
            if (c.b < minB) minB = c.b; if (c.b > maxB) maxB = c.b;
            if (c.a < minA) minA = c.a; if (c.a > maxA) maxA = c.a;
        }

        Debug.Log($"[DEPTH][{label}] min R:{minR:F6} G:{minG:F6} B:{minB:F6} A:{minA:F6} | " +
                  $"max R:{maxR:F6} G:{maxG:F6} B:{maxB:F6} A:{maxA:F6}");
    }
}
