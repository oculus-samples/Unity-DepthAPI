/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Unity.XR.Oculus;
using UnityEngine;
using UnityEngine.XR;

namespace Meta.XR.Depth
{
    /// <summary>
    /// Surfaces _EnvironmentDepthTexture and complementary information
    /// for reprojection and movement compensation to shaders globally.
    /// </summary>
    public class EnvironmentDepthTextureProvider : MonoBehaviour
    {
#if DEPTH_API_SUPPORTED
        public static readonly string DepthTexturePropertyName = "_EnvironmentDepthTexture";
        public static readonly string ReprojectionMatricesPropertyName = "_EnvironmentDepthReprojectionMatrices";
        public static readonly string Reprojection3DOFMatricesPropertyName = "_EnvironmentDepth3DOFReprojectionMatrices";
        public static readonly string ZBufferParamsPropertyName = "_EnvironmentDepthZBufferParams";

        public static readonly int DepthTextureID = Shader.PropertyToID(DepthTexturePropertyName);
        public static readonly int ReprojectionMatricesID = Shader.PropertyToID(ReprojectionMatricesPropertyName);
        public static readonly int Reprojection3DOFMatricesID = Shader.PropertyToID(Reprojection3DOFMatricesPropertyName);
        public static readonly int ZBufferParamsID = Shader.PropertyToID(ZBufferParamsPropertyName);

        // Required for per object occlusion shaders
        public bool Enable6DoFCalculations = true;

        // Required for screenspace shaders
        public bool Enable3DoFCalculations = false;

        public Transform CustomTrackingSpaceTransform = null;

        private bool _shouldEnableDepthRendering;
        private bool _depthRenderingEnabled;

        private XRDisplaySubsystem _xrDisplay;

        private readonly Matrix4x4[] _reprojectionMatrices =
            new Matrix4x4[2] { Matrix4x4.identity, Matrix4x4.identity };

        private bool _areHandsRemoved;

        private void Start()
        {
            _xrDisplay = OVRManager.GetCurrentDisplaySubsystem();

            if (CustomTrackingSpaceTransform == null)
            {
                CustomTrackingSpaceTransform = FindObjectOfType<OVRCameraRig>()?.trackingSpace;
            }
        }

        public void EnableEnvironmentDepth()
        {
            if (!_depthRenderingEnabled || !_shouldEnableDepthRendering)
            {
                _shouldEnableDepthRendering = true;
                Utils.SetupEnvironmentDepth(new Utils.EnvironmentDepthCreateParams() { removeHands = _areHandsRemoved });
            }
        }

        public void DisableEnvironmentDepth()
        {
            Utils.ShutdownEnvironmentDepth();
            _depthRenderingEnabled = false;
        }

        public bool GetEnvironmentDepthEnabled()
        {
            return _depthRenderingEnabled;
        }

        public void RemoveHands(bool areHandsRemoved)
        {
            if (!Utils.GetEnvironmentDepthHandRemovalSupported())
            {
                return;
            }
            _areHandsRemoved = areHandsRemoved;
            Utils.SetEnvironmentDepthHandRemoval(areHandsRemoved);
        }

        private void Update()
        {
            if (_shouldEnableDepthRendering)
            {
                _shouldEnableDepthRendering = !_shouldEnableDepthRendering;
                Utils.SetEnvironmentDepthRendering(true);
                _depthRenderingEnabled = true;
                return;
            }

            uint id = 0;
            if (Utils.GetEnvironmentDepthTextureId(ref id) && _xrDisplay != null && _xrDisplay.running)
            {
                var rt = _xrDisplay.GetRenderTexture(id);
                Shader.SetGlobalTexture(DepthTextureID, rt);
            }
            else
            {
                Debug.LogWarning("DepthAPI: no environment texture");
                return;
            }

            OVRPlugin.Frustumf2 leftEyeFrustrum;
            OVRPlugin.Frustumf2 rightEyeFrustrum;
            if (!OVRPlugin.GetNodeFrustum2(OVRPlugin.Node.EyeLeft, out leftEyeFrustrum) ||
                !OVRPlugin.GetNodeFrustum2(OVRPlugin.Node.EyeRight, out rightEyeFrustrum))
            {
                return;
            }
#if !UNITY_EDITOR && UNITY_ANDROID
            if (OculusSettings.s_Settings.SymmetricProjection)
            {
                leftEyeFrustrum.Fov.RightTan = rightEyeFrustrum.Fov.RightTan;
                rightEyeFrustrum.Fov.LeftTan = leftEyeFrustrum.Fov.LeftTan;
            }
#endif

            var leftEyeData = Utils.GetEnvironmentDepthFrameDesc(0);
            var rightEyeData = Utils.GetEnvironmentDepthFrameDesc(1);

            // Assume NearZ and FarZ are the same for left and right eyes
            float depthNearZ = leftEyeData.nearZ;
            float depthFarZ = leftEyeData.farZ;

            // Calculate Environment Depth Camera parameters
            Vector4 depthZBufferParams = EnvironmentDepthUtils.ComputeNdcToLinearDepthParameters(depthNearZ, depthFarZ);
            Shader.SetGlobalVector(ZBufferParamsID,
                depthZBufferParams);

            if (Enable6DoFCalculations)
            {
                // Calculate 6DOF reprojection matrices
                _reprojectionMatrices[0] = EnvironmentDepthUtils.CalculateReprojection(leftEyeData, leftEyeFrustrum.Fov);
                _reprojectionMatrices[1] = EnvironmentDepthUtils.CalculateReprojection(rightEyeData, rightEyeFrustrum.Fov);

                if (CustomTrackingSpaceTransform != null && !CustomTrackingSpaceTransform.worldToLocalMatrix.isIdentity)
                {
                    var worldToLocalMatrix = CustomTrackingSpaceTransform.worldToLocalMatrix;
                    _reprojectionMatrices[0] *= worldToLocalMatrix;
                    _reprojectionMatrices[1] *= worldToLocalMatrix;
                }

                Shader.SetGlobalMatrixArray(ReprojectionMatricesID, _reprojectionMatrices);
            }

            if (Enable3DoFCalculations)
            {
                // Calculate 3DOF reprojection matrices
                _reprojectionMatrices[0] = EnvironmentDepthUtils.Calculate3DOFReprojection(leftEyeData, leftEyeFrustrum.Fov);
                _reprojectionMatrices[1] = EnvironmentDepthUtils.Calculate3DOFReprojection(rightEyeData, rightEyeFrustrum.Fov);

                Shader.SetGlobalMatrixArray(Reprojection3DOFMatricesID, _reprojectionMatrices);
            }
        }
#endif
    }
}
