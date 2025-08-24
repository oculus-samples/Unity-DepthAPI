using System.Collections;
using System.IO;
using UnityEngine;
using PassthroughCameraSamples;

public class PassthroughUndistortCapture : MonoBehaviour
{
    [Header("References")]
    public PassthroughUndistortBinder binder;   // the binder you added on the RawImage/Quad

    [Header("Output (use WebCamTexture size to keep intrinsics valid)")]
    public int outputWidth = 0; // 0 = use WebCamTexture.width
    public int outputHeight = 0; // 0 = use WebCamTexture.height

    [Header("Input")]
    [SerializeField] private OVRInput.RawButton _saveSnapshotButton = OVRInput.RawButton.A;


    private void Update()
    {
        if (OVRInput.GetDown(_saveSnapshotButton)) SavePNG();
    }

    public void SavePNG() => StartCoroutine(CaptureAndSave());

    IEnumerator CaptureAndSave()
    {
        if (binder == null || binder.RuntimeMaterial == null || binder.webCam == null || binder.webCam.WebCamTexture == null)
        {
            Debug.LogWarning("Capture: binder/webcam/material not ready.");
            yield break;
        }

        var wct = binder.webCam.WebCamTexture;

        while (wct.width <= 16 || wct.height <= 16) yield return null;

        int w = outputWidth > 0 ? outputWidth : wct.width;
        int h = outputHeight > 0 ? outputHeight : wct.height;

        // keep material’s resolution in sync with the **output grid**
        binder.RuntimeMaterial.SetVector("_Resolution", new Vector4(w, h, 0, 0));
        binder.RuntimeMaterial.SetTexture("_MainTex", wct);

        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        try
        {
            binder.RuntimeMaterial.SetVector("_K1K2P1P2", new Vector4(0.35f, 0f, 0f, 0f)); // barrel warp
            binder.RuntimeMaterial.SetFloat("_K3", 0f);
            binder.RuntimeMaterial.SetFloat("_Strength", 1f);

            // use the **runtime** material instance
            Graphics.Blit(wct, rt, binder.RuntimeMaterial);

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            var bytes = tex.EncodeToPNG();
            string filename = $"PassthroughSnapshot_undistorted_{binder.eye}_{Time.frameCount}.png";
            string path = Path.Combine(Application.persistentDataPath, filename);
            File.WriteAllBytes(path, bytes);
            Debug.Log($"Saved undistorted PNG: {path}");
            Destroy(tex);
        }
        finally
        {
            RenderTexture.ReleaseTemporary(rt);
        }
    }

}
