// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR.Samples;
using PassthroughCameraSamples;
using PassthroughCameraSamples.CameraToWorld;
using UnityEngine;
using UnityEngine.Assertions;

[MetaCodeSample("PassthroughCameraApiSamples-CameraToWorld")]
public class CameraToWorldManagerSimple : MonoBehaviour
{
    [Header("Passthrough Components")]
    [SerializeField] private WebCamTextureManager m_webCamTextureManager;
    [SerializeField] private CameraToWorldCameraCanvas m_cameraCanvas;
    [SerializeField] private float m_canvasDistance = 1f;

    [Header("Debug Visualization")]
    [Tooltip("Assign your debug quad here to align it with the passthrough canvas.")]
    [SerializeField] private Transform depthQuad;
    [SerializeField] private Transform depthQuadCopy;

    private PassthroughCameraEye CameraEye => m_webCamTextureManager.Eye;
    private Vector2Int CameraResolution => m_webCamTextureManager.RequestedResolution;

    private IEnumerator Start()
    {
        if (m_webCamTextureManager == null)
        {
            Debug.LogError($"PCA: {nameof(m_webCamTextureManager)} field is required for the component to operate properly");
            enabled = false;
            yield break;
        }

        // Make sure the manager is disabled in scene and enable it only when the required permissions have been granted
        Assert.IsFalse(m_webCamTextureManager.enabled);
        while (PassthroughCameraPermissions.HasCameraPermission != true)
        {
            yield return null;
        }

        // Set the 'requestedResolution' and enable the manager
        m_webCamTextureManager.RequestedResolution = PassthroughCameraUtils.GetCameraIntrinsics(CameraEye).Resolution;
        m_webCamTextureManager.enabled = true;

        // Wait until the WebCamTexture has been created by the manager
        while (m_webCamTextureManager.WebCamTexture == null)
        {
            yield return null;
        }

        // Ensure the camera feed starts playing
        m_webCamTextureManager.WebCamTexture.Play();
        m_cameraCanvas.ResumeStreamingFromCamera();

        ScaleCameraCanvas();
    }

    private void Update()
    {
        if (m_webCamTextureManager.WebCamTexture == null || !m_webCamTextureManager.WebCamTexture.isPlaying)
            return;

        // Keep the canvas positioned in front of the camera every frame
        var cameraPose = PassthroughCameraUtils.GetCameraPoseInWorld(CameraEye);
        m_cameraCanvas.transform.position = cameraPose.position + cameraPose.rotation * Vector3.forward * m_canvasDistance;
        m_cameraCanvas.transform.rotation = cameraPose.rotation;

        // NEW: Also update the depth quad to match the passthrough canvas
        UpdateDepthQuadTransform(depthQuad);
        UpdateDepthQuadTransform(depthQuadCopy);
    }

    /// <summary>
    /// This function ensures the depth quad's transform is identical to the passthrough canvas.
    /// </summary>
    private void UpdateDepthQuadTransform(Transform quad)
    {
        if (!quad || !m_cameraCanvas) return;

        // match pose + tiny forward/back offset to avoid z-fighting
        const float zOffset = 0.001f;
        quad.SetPositionAndRotation(
            m_cameraCanvas.transform.position + m_cameraCanvas.transform.forward * -zOffset,
            m_cameraCanvas.transform.rotation
        );

        // match size (UI canvas -> world-space size)
        var rt = m_cameraCanvas.GetComponentInChildren<RectTransform>();
        if (!rt) return;

        var corners = new Vector3[4];
        rt.GetWorldCorners(corners); // 0=BL,1=TL,2=TR,3=BR

        float worldWidth = Vector3.Distance(corners[3], corners[0]); // BL -> BR
        float worldHeight = Vector3.Distance(corners[1], corners[0]); // BL -> TL

        // primitive Quad, no parent: scale directly to desired world size
        quad.localScale = new Vector3(worldWidth, worldHeight, 1f);
    }

    /// <summary>
    /// Calculate the dimensions of the canvas based on the distance from the camera origin and the camera resolution.
    /// </summary>
    private void ScaleCameraCanvas()
    {
        var cameraCanvasRectTransform = m_cameraCanvas.GetComponentInChildren<RectTransform>();
        var leftSidePointInCamera = PassthroughCameraUtils.ScreenPointToRayInCamera(CameraEye, new Vector2Int(0, CameraResolution.y / 2));
        var rightSidePointInCamera = PassthroughCameraUtils.ScreenPointToRayInCamera(CameraEye, new Vector2Int(CameraResolution.x, CameraResolution.y / 2));
        var horizontalFoVDegrees = Vector3.Angle(leftSidePointInCamera.direction, rightSidePointInCamera.direction);
        var horizontalFoVRadians = horizontalFoVDegrees * Mathf.Deg2Rad;
        var newCanvasWidthInMeters = 2 * m_canvasDistance * Mathf.Tan(horizontalFoVRadians / 2);
        var localScale = (float)(newCanvasWidthInMeters / cameraCanvasRectTransform.sizeDelta.x);
        cameraCanvasRectTransform.localScale = new Vector3(localScale, localScale, localScale);
    }

}
