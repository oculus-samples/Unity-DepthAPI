using UnityEngine;
using UnityEngine.UI;
using PassthroughCameraSamples;

public class PassthroughUndistortBinder : MonoBehaviour
{
    [Header("Refs")]
    public WebCamTextureManager webCam;
    public PassthroughCameraEye eye = PassthroughCameraEye.Left;
    public Material baseMaterial;               // assign Mat_PassthroughUndistort (the asset)

    [Range(0, 1)] public float strength = 1f;

    // The runtime instance the UI uses AND the capture will blit with.
    public Material RuntimeMaterial { get; private set; }

    RawImage _img;
    Renderer _rend;
    bool _logged;

    void Awake()
    {
        _img = GetComponent<RawImage>();
        _rend = GetComponent<Renderer>();

        if (baseMaterial != null)
            RuntimeMaterial = new Material(baseMaterial); // clone once
        if (_img && RuntimeMaterial) _img.material = RuntimeMaterial;
        if (_rend && RuntimeMaterial) _rend.material = RuntimeMaterial;
    }

    void Update()
    {
        if (webCam == null || webCam.WebCamTexture == null || RuntimeMaterial == null) return;

        var wct = webCam.WebCamTexture;
        var intr = PassthroughCameraUtils.GetCameraIntrinsics(eye);
        var dist = PassthroughCameraUtils.GetLensDistortion(eye);

        // Expecting radtan: k1,k2,p1,p2,(k3)
        Vector4 k = Vector4.zero; float k3 = 0f;
        if (dist != null && dist.Length >= 4)
        {
            k = new Vector4(dist[0], dist[1], dist[2], dist[3]);
            if (dist.Length >= 5) k3 = dist[4];
        }

        // Push params to the **runtime** material (not the asset!)
        RuntimeMaterial.SetVector("_FxFyCxCy",
            new Vector4(intr.FocalLength.x, intr.FocalLength.y, intr.PrincipalPoint.x, intr.PrincipalPoint.y));
        RuntimeMaterial.SetVector("_Resolution", new Vector4(wct.width, wct.height, 0, 0));
        RuntimeMaterial.SetVector("_K1K2P1P2", k);
        RuntimeMaterial.SetFloat("_K3", k3);
        RuntimeMaterial.SetFloat("_Strength", strength);
        RuntimeMaterial.SetTexture("_MainTex", wct);

        if (!_logged)
        {
            _logged = true;
            Debug.Log($"[UndistortBinder] res={wct.width}x{wct.height} fx,fy={intr.FocalLength} cx,cy={intr.PrincipalPoint}  " +
                      $"dist.len={(dist?.Length ?? 0)}  k={k} k3={k3} strength={strength}");
        }
    }
}
