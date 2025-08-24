// EnvironmentDepthUnprojectionGlobals.cs
using UnityEngine;
using Meta.XR.EnvironmentDepth;

[DefaultExecutionOrder(10000)]
[RequireComponent(typeof(EnvironmentDepthManager))]
public class EnvironmentDepthUnprojectionGlobals : MonoBehaviour
{
    const int kNumViews = 2;
    static readonly int InvProjID = Shader.PropertyToID("_EnvironmentDepthInvProjectionMatrices");

    readonly Matrix4x4[] _invProj = new Matrix4x4[kNumViews];

    EnvironmentDepthManager _mgr;

    void Awake() => _mgr = GetComponent<EnvironmentDepthManager>();
    void OnEnable() => Application.onBeforeRender += OnBeforeRender;
    void OnDisable() => Application.onBeforeRender -= OnBeforeRender;

    void OnBeforeRender()
    {
        if (_mgr == null || !_mgr.IsDepthAvailable) return;

        for (int eye = 0; eye < kNumViews; eye++)
        {
            // Build depth camera projection & view (world->camera)
            EnvironmentDepthUtils.CalculateDepthCameraMatrices(
                _mgr.frameDescriptors[eye], out var proj, out var view);  // uses frameDescriptors[] from the manager
            _invProj[eye] = proj.inverse;

        }

        Shader.SetGlobalMatrixArray(InvProjID, _invProj);

    }
}
