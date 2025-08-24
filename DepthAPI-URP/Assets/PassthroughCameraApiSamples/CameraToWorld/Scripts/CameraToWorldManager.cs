// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace PassthroughCameraSamples.CameraToWorld
{
    [MetaCodeSample("PassthroughCameraApiSamples-CameraToWorld")]
    public class CameraToWorldManager : MonoBehaviour
    {
        [SerializeField] private WebCamTextureManager m_webCamTextureManager;
        private PassthroughCameraEye CameraEye => m_webCamTextureManager.Eye;
        private Vector2Int CameraResolution => m_webCamTextureManager.RequestedResolution;
        [SerializeField] private GameObject m_centerEyeAnchor;
        [SerializeField] private GameObject m_headMarker;
        [SerializeField] private GameObject m_cameraMarker;
        [SerializeField] private GameObject m_rayMarker;

        [SerializeField] private CameraToWorldCameraCanvas m_cameraCanvas;
        [SerializeField] private float m_canvasDistance = 1f;

        [SerializeField] private Vector3 m_headSpaceDebugShift = new(0, -.15f, .4f);
        private GameObject m_rayGo1, m_rayGo2, m_rayGo3, m_rayGo4;

        private bool m_isDebugOn;
        private bool m_snapshotTaken;
        private OVRPose m_snapshotHeadPose;

        private void Awake() => OVRManager.display.RecenteredPose += RecenterCallBack;

        private IEnumerator Start()
        {
            if (m_webCamTextureManager == null)
            {
                Debug.LogError($"PCA: {nameof(m_webCamTextureManager)} field is required "
                            + $"for the component {nameof(CameraToWorldManager)} to operate properly");
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

            ScaleCameraCanvas();

            m_rayGo1 = m_rayMarker;
            m_rayGo2 = Instantiate(m_rayMarker);
            m_rayGo3 = Instantiate(m_rayMarker);
            m_rayGo4 = Instantiate(m_rayMarker);
            UpdateRaysRendering();
        }

        private void Update()
        {
            if (m_webCamTextureManager.WebCamTexture == null)
                return;

            if (OVRInput.GetDown(OVRInput.Button.One))
            {
                m_snapshotTaken = !m_snapshotTaken;
                if (m_snapshotTaken)
                {
                    // Asking the canvas to make a snapshot before stopping WebCamTexture
                    m_cameraCanvas.MakeCameraSnapshot();
                    m_webCamTextureManager.WebCamTexture.Stop();
                    m_snapshotHeadPose = m_centerEyeAnchor.transform.ToOVRPose();
                }
                else
                {
                    m_webCamTextureManager.WebCamTexture.Play();
                    m_cameraCanvas.ResumeStreamingFromCamera();
                    m_snapshotHeadPose = OVRPose.identity;
                }

                UpdateRaysRendering();
            }

            if (OVRInput.GetDown(OVRInput.Button.Two))
            {
                m_isDebugOn ^= true;
                Debug.Log($"PCA: SpatialSnapshotManager: DEBUG mode is {(m_isDebugOn ? "ON" : "OFF")}");
                UpdateRaysRendering();

                if (m_snapshotTaken)
                {
                    // Enable or disable the debug translation of the markers
                    TranslateMarkersForDebug(m_isDebugOn);
                }
            }

            if (!m_snapshotTaken)
            {
                UpdateMarkerPoses();

                if (m_isDebugOn)
                {
                    // Move the updated markers forward to better see them
                    TranslateMarkersForDebug(moveForward: true);
                }
            }
        }

        /// <summary>
        /// Calculate the dimensions of the canvas based on the distance from the camera origin and the camera resolution
        /// </summary>
        private void ScaleCameraCanvas()
        {
            var cameraCanvasRectTransform = m_cameraCanvas.GetComponentInChildren<RectTransform>();
            var leftSidePointInCamera = PassthroughCameraUtils.ScreenPointToRayInCamera(CameraEye, new Vector2Int(0, CameraResolution.y / 2));
            var rightSidePointInCamera = PassthroughCameraUtils.ScreenPointToRayInCamera(CameraEye, new Vector2Int(CameraResolution.x, CameraResolution.y / 2));
            var horizontalFoVDegrees = Vector3.Angle(leftSidePointInCamera.direction, rightSidePointInCamera.direction);
            var horizontalFoVRadians = horizontalFoVDegrees / 180 * Math.PI;
            var newCanvasWidthInMeters = 2 * m_canvasDistance * Math.Tan(horizontalFoVRadians / 2);
            var localScale = (float)(newCanvasWidthInMeters / cameraCanvasRectTransform.sizeDelta.x);
            cameraCanvasRectTransform.localScale = new Vector3(localScale, localScale, localScale);
        }

        private void UpdateRaysRendering()
        {
            // Hide rays' middle segments and rendering only their tips
            // when rays' origins are too close to the headset. Otherwise, it looks ugly
            foreach (var rayGo in new[] { m_rayGo1, m_rayGo2, m_rayGo3, m_rayGo4 })
            {
                rayGo.GetComponent<CameraToWorldRayRenderer>().RenderMiddleSegment(m_snapshotTaken || m_isDebugOn);
            }
        }

        private void UpdateMarkerPoses()
        {
            var headPose = OVRPlugin.GetNodePoseStateImmediate(OVRPlugin.Node.Head).Pose.ToOVRPose();
            m_headMarker.transform.position = headPose.position;
            m_headMarker.transform.rotation = headPose.orientation;

            var cameraPose = PassthroughCameraUtils.GetCameraPoseInWorld(CameraEye);
            m_cameraMarker.transform.position = cameraPose.position;
            m_cameraMarker.transform.rotation = cameraPose.rotation;

            // Position the canvas in front of the camera
            m_cameraCanvas.transform.position = cameraPose.position + cameraPose.rotation * Vector3.forward * m_canvasDistance;
            m_cameraCanvas.transform.rotation = cameraPose.rotation;

            // Position the rays pointing to 4 corners of the canvas / image
            var rays = new[]
            {
                new { rayGo = m_rayGo1, u = 0, v = 0 },
                new { rayGo = m_rayGo2, u = 0, v = CameraResolution.y },
                new { rayGo = m_rayGo3, u = CameraResolution.x, v = CameraResolution.y },
                new { rayGo = m_rayGo4, u = CameraResolution.x, v = 0 }
            };

            foreach (var item in rays)
            {
                var rayInWorld = PassthroughCameraUtils.ScreenPointToRayInWorld(CameraEye, new Vector2Int(item.u, item.v));
                item.rayGo.transform.position = rayInWorld.origin;
                item.rayGo.transform.LookAt(rayInWorld.origin + rayInWorld.direction);

                var angleWithCameraForwardDegree =
                    Vector3.Angle(item.rayGo.transform.forward, cameraPose.rotation * Vector3.forward);
                // The original size of the ray GameObject along z axis is 0.5f. Hardcoding it here for simplicity
                var zScale = (float)(m_canvasDistance / Math.Cos(angleWithCameraForwardDegree / 180 * Math.PI) / 0.5);
                item.rayGo.transform.localScale = new Vector3(item.rayGo.transform.localScale.x, item.rayGo.transform.localScale.y, zScale);

                var label = item.rayGo.GetComponentInChildren<Text>();
                label.text = $"({item.u:F0}, {item.v:F0})";
            }
        }

        private void TranslateMarkersForDebug(bool moveForward)
        {
            var gameObjects = new[]
            {
                m_headMarker, m_cameraMarker, m_cameraCanvas.gameObject, m_rayGo1, m_rayGo2, m_rayGo3, m_rayGo4
            };

            var direction = m_snapshotTaken ? m_snapshotHeadPose.orientation : m_centerEyeAnchor.transform.rotation;

            foreach (var go in gameObjects)
            {
                go.transform.position += direction * m_headSpaceDebugShift * (moveForward ? 1 : -1);
            }
        }

        private void RecenterCallBack()
        {
            if (m_snapshotTaken)
            {
                m_snapshotTaken = false;
                m_webCamTextureManager.WebCamTexture.Play();
                m_cameraCanvas.ResumeStreamingFromCamera();
                m_snapshotHeadPose = OVRPose.identity;
            }
        }
    }
}
