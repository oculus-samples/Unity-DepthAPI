using UnityEngine;
using PassthroughCameraSamples;

// Draws the passthrough camera frustum slice between HandCaptureGlobals.MinMeters and MaxMeters at runtime.
// Uses LineRenderers so it renders in-headset on Quest. Attach to any GameObject in the scene.
public class HandCaptureFovSliceRuntime : MonoBehaviour
{
    [Header("Appearance")]
    [Min(0.0001f)] public float lineWidth = 0.005f;
    public Material lineMaterial; // assign in Inspector (URP Unlit/Color recommended)
    public Color nearColor = new Color(0.1f, 1f, 0.1f, 1f);
    public Color farColor  = new Color(1f, 0.2f, 0.2f, 1f);
    public Color edgeColor = new Color(1f, 0.9f, 0.2f, 1f);
    public Color centerColor = new Color(0.2f, 0.7f, 1f, 1f);

    [Header("Toggles")]
    public bool showNear = true;
    public bool showFar = true;
    public bool showEdges = true;
    public bool showCenter = true;

    private LineRenderer _nearLR;
    private LineRenderer _farLR;
    private LineRenderer _centerLR;
    private LineRenderer[] _edgeLRs;

    private static PassthroughCameraEye Eye => HandCaptureGlobals.EyeIndex == 0 ? PassthroughCameraEye.Left : PassthroughCameraEye.Right;

    private void Awake()
    {
        EnsureLineRenderers();
        ApplyStyle();
    }

    private void OnValidate()
    {
        ApplyStyle();
    }

    private void EnsureLineRenderers()
    {
        if (_nearLR == null) _nearLR = CreateLR("NearLoop");
        if (_farLR == null) _farLR = CreateLR("FarLoop");
        if (_centerLR == null) _centerLR = CreateLR("CenterRay");
        if (_edgeLRs == null || _edgeLRs.Length != 4)
        {
            _edgeLRs = new LineRenderer[4];
            for (int i = 0; i < 4; i++) _edgeLRs[i] = CreateLR($"Edge{i}");
        }

        _nearLR.loop = true; _nearLR.positionCount = 4;
        _farLR.loop = true;  _farLR.positionCount = 4;
        _centerLR.loop = false; _centerLR.positionCount = 2;
        foreach (var lr in _edgeLRs) { lr.loop = false; lr.positionCount = 2; }
    }

    private LineRenderer CreateLR(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.textureMode = LineTextureMode.Stretch;
        lr.alignment = LineAlignment.View;
        lr.numCapVertices = 0;
        lr.numCornerVertices = 0;
        if (lineMaterial) lr.material = lineMaterial;
        return lr;
    }

    private void ApplyStyle()
    {
        if (_nearLR == null) return;
        _nearLR.startWidth = _nearLR.endWidth = lineWidth;
        _farLR.startWidth = _farLR.endWidth = lineWidth;
        _centerLR.startWidth = _centerLR.endWidth = lineWidth;
        foreach (var lr in _edgeLRs) lr.startWidth = lr.endWidth = lineWidth;
        if (lineMaterial)
        {
            _nearLR.material = lineMaterial;
            _farLR.material = lineMaterial;
            _centerLR.material = lineMaterial;
            foreach (var lr in _edgeLRs) lr.material = lineMaterial;
        }

        _nearLR.startColor = _nearLR.endColor = nearColor;
        _farLR.startColor = _farLR.endColor = farColor;
        foreach (var lr in _edgeLRs) lr.startColor = lr.endColor = edgeColor;
        _centerLR.startColor = _centerLR.endColor = centerColor;
    }

    private void LateUpdate()
    {
        // Distances
        float dNear = Mathf.Max(0.0f, HandCaptureGlobals.MinMeters);
        float dFar  = Mathf.Max(dNear, HandCaptureGlobals.MaxMeters);

        // Visibility toggles
        if (_nearLR) _nearLR.gameObject.SetActive(showNear && dFar > 0f);
        if (_farLR) _farLR.gameObject.SetActive(showFar && dFar > 0f);
        if (_centerLR) _centerLR.gameObject.SetActive(showCenter && dFar > 0f);
        foreach (var lr in _edgeLRs) if (lr) lr.gameObject.SetActive(showEdges && dFar > 0f);

        if (dFar <= 0f) return;

        // Get camera pose and four corner rays (TL, TR, BR, BL)
        if (!PassthroughCameraUtils.IsSupported || !PassthroughCameraUtils.EnsureInitialized())
            return;

        var pose = PassthroughCameraUtils.GetCameraPoseInWorld(Eye);
        var res = PassthroughCameraUtils.GetCameraIntrinsics(Eye).Resolution;
        if (res.x <= 0 || res.y <= 0) return;

        var tl = PassthroughCameraUtils.ScreenPointToRayInCamera(Eye, new Vector2Int(0, res.y));
        var tr = PassthroughCameraUtils.ScreenPointToRayInCamera(Eye, new Vector2Int(res.x, res.y));
        var br = PassthroughCameraUtils.ScreenPointToRayInCamera(Eye, new Vector2Int(res.x, 0));
        var bl = PassthroughCameraUtils.ScreenPointToRayInCamera(Eye, new Vector2Int(0, 0));

        Vector3 o = pose.position;
        // Convert camera-space rays to world-space
        Vector3 dTL = pose.rotation * tl.direction;
        Vector3 dTR = pose.rotation * tr.direction;
        Vector3 dBR = pose.rotation * br.direction;
        Vector3 dBL = pose.rotation * bl.direction;

        // Compute near/far corners
        Vector3 nTL = o + dTL * dNear;
        Vector3 nTR = o + dTR * dNear;
        Vector3 nBR = o + dBR * dNear;
        Vector3 nBL = o + dBL * dNear;

        Vector3 fTL = o + dTL * dFar;
        Vector3 fTR = o + dTR * dFar;
        Vector3 fBR = o + dBR * dFar;
        Vector3 fBL = o + dBL * dFar;

        // Assign line positions
        if (_nearLR)
        {
            _nearLR.SetPosition(0, nTL);
            _nearLR.SetPosition(1, nTR);
            _nearLR.SetPosition(2, nBR);
            _nearLR.SetPosition(3, nBL);
        }
        if (_farLR)
        {
            _farLR.SetPosition(0, fTL);
            _farLR.SetPosition(1, fTR);
            _farLR.SetPosition(2, fBR);
            _farLR.SetPosition(3, fBL);
        }
        if (_edgeLRs != null && _edgeLRs.Length == 4)
        {
            _edgeLRs[0].SetPosition(0, nTL); _edgeLRs[0].SetPosition(1, fTL);
            _edgeLRs[1].SetPosition(0, nTR); _edgeLRs[1].SetPosition(1, fTR);
            _edgeLRs[2].SetPosition(0, nBR); _edgeLRs[2].SetPosition(1, fBR);
            _edgeLRs[3].SetPosition(0, nBL); _edgeLRs[3].SetPosition(1, fBL);
        }
        if (_centerLR)
        {
            Vector3 forward = pose.rotation * Vector3.forward;
            _centerLR.SetPosition(0, o + forward * dNear);
            _centerLR.SetPosition(1, o + forward * dFar);
        }
    }
}
