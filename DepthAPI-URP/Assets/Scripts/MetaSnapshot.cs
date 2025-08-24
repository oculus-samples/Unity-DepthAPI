using System;
using System.IO;
using UnityEngine;
using PassthroughCameraSamples; // PassthroughCameraUtils

public class MetaSnapshot : MonoBehaviour
{

    [Tooltip("XR eye anchors in world space. Depth camera == these eye frames.")]
    public Transform depthAnchorLeft, depthAnchorRight;

    [Header("XR eye camera for depth intrinsics")]
    [SerializeField, Tooltip("XR eye camera used to derive depth intrinsics from the stereo projection.")]
    private Camera xrEyeCamera;

    public enum DepthFormat { UNKNOWN, EXR_METERS, R16_CLIP }
    [HideInInspector] public DepthFormat depthFormat = DepthFormat.EXR_METERS;
    [HideInInspector, Tooltip("Near plane, if using R16 clip-space depth.")]
    public float depthNear = 0.3f;
    [HideInInspector, Tooltip("Far plane, if using R16 clip-space depth.")]
    public float depthFar = 5.0f;

    // ---- Data structures (restored) ----
    [Serializable] public class CameraIntrinsics { public int w, h; public float fx, fy, cx, cy; }
    [Serializable] public class TransformRt { public float[] R; public float[] t; } // R: length 9 (row-major), t: length 3

    [Serializable]
    public class RGBDMeta
    {
        public string eye;                // "L" or "R"
        public CameraIntrinsics rgb, depth;
        public string depthFormat;        // "EXR_METERS" or "R16_CLIP"
        public float near, far;           // if clip-space
        public TransformRt T_depth;         // transform  depth 
        public TransformRt T_rgb;   // transform  rgb
        public double timestamp;          // seconds
        public string rgbPath;            // saved RGB file
        public string depthPath;          // saved depth file
    }

    // PUBLIC: called by RGBDCapture
    public void SaveMeta(string eyeTag, WebCamTexture camTex, string depthPath, string rgbPath)
    {
        var unityEye = (eyeTag == "L") ? Camera.StereoscopicEye.Left : Camera.StereoscopicEye.Right;
        var pEye = (eyeTag == "L") ? PassthroughCameraEye.Left : PassthroughCameraEye.Right;

        // Resolve XR camera
        var cam = xrEyeCamera != null ? xrEyeCamera : Camera.main;

        // --- Depth intrinsics from XR projection at preprocessed depth RT size ---
        int depthW = 0, depthH = 0;
        var preDepth = Shader.GetGlobalTexture("_PreprocessedEnvironmentDepthTexture") as RenderTexture;
        CameraIntrinsics depthK;
        depthW = preDepth.width; depthH = preDepth.height;
        depthK = KFromProjection(cam, unityEye, depthW, depthH);

        // --- RGB intrinsics from sensor intrinsics, scaled to current WebCamTexture size ---
        CameraIntrinsics rgbK;
        var info = PassthroughCameraUtils.GetCameraIntrinsics(pEye);
        float sx = (float)camTex.width / info.Resolution.x;
        float sy = (float)camTex.height / info.Resolution.y;
        rgbK = new CameraIntrinsics
        {
            w = camTex.width,
            h = camTex.height,
            fx = info.FocalLength.x * sx,
            fy = info.FocalLength.y * sy,
            cx = info.PrincipalPoint.x * sx,
            cy = info.PrincipalPoint.y * sy
        };

        // --- Extrinsics: depth (eye) ---
        Transform eyeTf = (eyeTag == "L") ? depthAnchorLeft : depthAnchorRight;
        Quaternion depthRot = eyeTf ? eyeTf.rotation : Quaternion.identity;
        Vector3 depthPos = eyeTf ? eyeTf.position : Vector3.zero;

        // RGB extrinsics from PCA ---
        Vector3 rgbPosW; Quaternion rgbRotW;
        if (!TryGetRgbPoseWorld(pEye, out rgbPosW, out rgbRotW))
        {
            Debug.LogWarning("[MetaSnapshot] PCA world pose not available; using identity.");
            rgbPosW = Vector3.zero; rgbRotW = Quaternion.identity;
        }

        // --- Assemble meta with separate world extrinsics for RGB and Depth ---
        var meta = new RGBDMeta
        {
            eye = eyeTag,
            rgb = rgbK,
            depth = depthK,
            depthFormat = depthFormat.ToString(),
            near = depthNear,
            far = depthFar,

            T_rgb = new TransformRt
            {
                R = R9FromQuaternion(rgbRotW),
                t = new[] { rgbPosW.x, rgbPosW.y, rgbPosW.z }
            },

            T_depth = new TransformRt
            {
                R = R9FromQuaternion(depthRot),
                t = new[] { depthPos.x, depthPos.y, depthPos.z }
            },

            timestamp = Time.realtimeSinceStartupAsDouble,
            rgbPath = rgbPath,
            depthPath = depthPath
        };

        var json = JsonUtility.ToJson(meta, true);
        var path = Path.Combine(Application.persistentDataPath, $"meta_{eyeTag}_{Time.frameCount}.json");
        File.WriteAllText(path, json);
        Debug.Log($"[MetaSnapshot] Wrote metadata → {path}");
    }

    // ---- Helpers ----
    private static bool TryGetRgbPoseWorld(PassthroughCameraEye eye, out Vector3 pos, out Quaternion rot)
    {
        try
        {
            // Use the PCA API that returns a UnityEngine.Pose
            Pose pose = PassthroughCameraUtils.GetCameraPoseInWorld(eye);
            pos = pose.position;
            rot = pose.rotation;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[MetaSnapshot] PCA world pose not available: {ex.Message}");
            pos = default; rot = default;
            return false;
        }
    }

    private static CameraIntrinsics KFromProjection(Camera cam, Camera.StereoscopicEye eye, int w, int h)
    {
        Matrix4x4 P = cam.GetStereoProjectionMatrix(eye);
        float fx = 0.5f * w * P[0, 0];
        float fy = 0.5f * h * P[1, 1];
        float cx = 0.5f * w * (1f + P[0, 2]);
        float cy = 0.5f * h * (1f + P[1, 2]);
        return new CameraIntrinsics { w = w, h = h, fx = fx, fy = fy, cx = cx, cy = cy };
    }

    private static float[,] Matrix3x3FromQuaternion(Quaternion q)
    {
        float x = q.x, y = q.y, z = q.z, w = q.w;
        float xx = x * x, yy = y * y, zz = z * z;
        float xy = x * y, xz = x * z, yz = y * z;
        float wx = w * x, wy = w * y, wz = w * z;
        return new float[,]
        {
            {1-2*(yy+zz), 2*(xy-wz),   2*(xz+wy)},
            {2*(xy+wz),   1-2*(xx+zz), 2*(yz-wx)},
            {2*(xz-wy),   2*(yz+wx),   1-2*(xx+yy)}
        };
    }

    // Helper: 3x3 rotation (row-major) from Quaternion
    static float[] R9FromQuaternion(Quaternion q)
    {
        q = q.normalized;
        float x = q.x, y = q.y, z = q.z, w = q.w;
        float xx = x * x, yy = y * y, zz = z * z;
        float xy = x * y, xz = x * z, yz = y * z;
        float wx = w * x, wy = w * y, wz = w * z;
        // row-major [ r00 r01 r02 r10 r11 r12 r20 r21 r22 ]
        return new float[]
        {
        1-2*(yy+zz), 2*(xy-wz),   2*(xz+wy),
        2*(xy+wz),   1-2*(xx+zz), 2*(yz-wx),
        2*(xz-wy),   2*(yz+wx),   1-2*(xx+yy)
        };
    }
    private static float[,] Identity3() => new float[,] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };
}