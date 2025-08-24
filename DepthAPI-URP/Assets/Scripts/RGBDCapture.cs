using System.Collections;
using System.IO;
using UnityEngine;
using PassthroughCameraSamples; // WebCamTextureManager, PassthroughCameraEye

public class RGBDCapture : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private OVRInput.RawButton _saveSnapshotButton = OVRInput.RawButton.A;

    [Header("References (assign in Inspector)")]
    [SerializeField] private DepthCaptureRaw depthCapture;
    [SerializeField] private WebCamTextureManager webcamManager;
    [SerializeField] private PassthroughSnapshot snapshot;
    [SerializeField] private MetaSnapshot metaSnapshot;

    [Header("Timing")]
    [Tooltip("Fresh frames to wait after switching the webcam eye before reading pixels.")]
    [Min(1)] public int framesToWaitForFreshImage = 6;
    [Tooltip("Seconds to wait for webcam init before giving up per eye.")]
    [Min(0.1f)] public float initTimeoutSeconds = 4f;

    private bool _isCapturing;

    private void Update()
    {
        if (_isCapturing || webcamManager == null || depthCapture == null || snapshot == null || metaSnapshot == null) return;
        if (OVRInput.GetDown(_saveSnapshotButton)) StartCoroutine(CaptureLeftThenRight());
    }

    private IEnumerator CaptureLeftThenRight()
    {
        _isCapturing = true;
        Debug.Log("[RGBDCapture] Capturing with a single webcam manager: LEFT then RIGHT…");
        yield return StartCoroutine(CaptureForEye(PassthroughCameraEye.Left, 0, "L"));
        yield return StartCoroutine(CaptureForEye(PassthroughCameraEye.Right, 1, "R"));
        Debug.Log($"[RGBDCapture] Done. Files in: {Application.persistentDataPath}");
        _isCapturing = false;
    }

    private IEnumerator CaptureForEye(PassthroughCameraEye eye, int sliceIndex, string eyeTag)
    {
        // Reinitialize the single WebCamTextureManager for the requested eye
        webcamManager.enabled = false;
        webcamManager.Eye = eye;
        yield return null; // ensure OnDisable runs
        webcamManager.enabled = true; // triggers InitializeWebCamTexture()

        // Wait until WebCamTexture exists and has delivered fresh frames
        float deadline = Time.unscaledTime + initTimeoutSeconds;
        while (webcamManager.WebCamTexture == null)
        {
            if (Time.unscaledTime > deadline) { Debug.LogWarning($"[RGBDCapture] Timeout waiting for webcam creation for {eyeTag}."); break; }
            yield return null;
        }

        var camTex = webcamManager.WebCamTexture;
        if (camTex != null)
        {
            int fresh = 0;
            while (fresh < framesToWaitForFreshImage && Time.unscaledTime <= deadline)
            {
                if (camTex.didUpdateThisFrame) fresh++;
                yield return null;
            }
            yield return new WaitForEndOfFrame(); // read after render
        }

        // 1) Save depth slice for this eye (0=L,1=R)
        string depthPath = SaveDepth(sliceIndex, eyeTag);

        yield return null;
    }

    private string SaveDepth(int eyeSlice, string eyeTag)
    {
        string path = depthCapture.SaveEnvironmentDepthTexture(eyeSlice);
        depthCapture.SavePreprocessedEnvironmentDepthTexture(eyeSlice);
        Debug.Log($"[RGBDCapture] Saved Depth ({eyeTag}) → {path}");
        return path;
    }

    private void SavePassthrough(WebCamTextureManager mgr, string eyeTag)
    {
        snapshot.SaveCurrentFrame();

    }
}