// DepthShaderMatrixSetter.cs
// Feeds depth camera VIEW/PROJ matrices (rebuilt from public DepthFrameDesc fields)
// to a material each frame. The depth texture itself is already bound globally by
// EnvironmentDepthManager as _EnvironmentDepthTexture (and optionally _PreprocessedEnvironmentDepthTexture).

using UnityEngine;
using Meta.XR.EnvironmentDepth; // your namespace

public class DepthShaderMatrixSetter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnvironmentDepthManager depthManager; // assign in Inspector
    [SerializeField] private Material targetMaterial;              // material using your debug shader

    [Header("Eye Selection")]
    [SerializeField] private bool useRightEye = false; // false = Left(0), true = Right(1)

    private static readonly int DepthViewID = Shader.PropertyToID("_DepthCameraView");
    private static readonly int DepthProjID = Shader.PropertyToID("_DepthCameraProj");

    private void Reset()
    {
        if (!depthManager) depthManager = FindObjectOfType<EnvironmentDepthManager>();
    }

    private void LateUpdate()
    {
        if (!targetMaterial || !depthManager || !depthManager.IsDepthAvailable) return;

        int eye = useRightEye ? 1 : 0;
        var d = depthManager.frameDescriptors[eye];

        // Build projection matrix from FOV tangents + near/far (matches EnvironmentDepthUtils)
        Matrix4x4 proj = BuildProjectionFromTangents(
            d.fovLeftAngleTangent, d.fovRightAngleTangent,
            d.fovDownAngleTangent, d.fovTopAngleTangent,
            d.nearZ, d.farZ
        );

        // Build view matrix from pose with Z flip, then invert (matches EnvironmentDepthUtils)
        Matrix4x4 view = Matrix4x4.TRS(d.createPoseLocation, d.createPoseRotation, new Vector3(1f, 1f, -1f)).inverse;

        // Feed to material
        targetMaterial.SetMatrix(DepthViewID, view);
        targetMaterial.SetMatrix(DepthProjID, proj);

        // NOTE: We do NOT set any texture here. EnvironmentDepthManager already sets:
        //   Shader.SetGlobalTexture("_EnvironmentDepthTexture", depthRT);
        // and, when enabled, the preprocessed variant & reprojection matrices. 
    }

    private static Matrix4x4 BuildProjectionFromTangents(
        float tanLeft, float tanRight, float tanDown, float tanTop, float nearZ, float farZ)
    {
        // Same math as EnvironmentDepthUtils.CalculateDepthCameraMatrices() for proj. 
        float x = 2.0f / (tanRight + tanLeft);
        float y = 2.0f / (tanTop + tanDown);
        float a = (tanRight - tanLeft) / (tanRight + tanLeft);
        float b = (tanTop - tanDown) / (tanTop + tanDown);

        float c, d;
        if (float.IsInfinity(farZ))
        {
            c = -1.0f;
            d = -2.0f * nearZ;
        }
        else
        {
            c = -(farZ + nearZ) / (farZ - nearZ);
            d = -(2.0f * farZ * nearZ) / (farZ - nearZ);
        }

        Matrix4x4 m = new Matrix4x4();
        m.m00 = x; m.m01 = 0; m.m02 = a; m.m03 = 0;
        m.m10 = 0; m.m11 = y; m.m12 = b; m.m13 = 0;
        m.m20 = 0; m.m21 = 0; m.m22 = c; m.m23 = d;
        m.m30 = 0; m.m31 = 0; m.m32 = -1f; m.m33 = 0;
        return m;
    }
}
