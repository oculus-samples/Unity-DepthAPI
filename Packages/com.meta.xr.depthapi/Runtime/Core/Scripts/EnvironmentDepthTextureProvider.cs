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
        public static readonly string DepthTexturePropertyName = "_EnvironmentDepthTexture";
        public static readonly string ReprojectionMatricesPropertyName = "_EnvironmentDepthReprojectionMatrices";
        public static readonly string ZBufferParamsPropertyName = "_EnvironmentDepthZBufferParams";

        public static readonly int DepthTextureID = Shader.PropertyToID(DepthTexturePropertyName);
        public static readonly int ReprojectionMatricesID = Shader.PropertyToID(ReprojectionMatricesPropertyName);
        public static readonly int ZBufferParamsID = Shader.PropertyToID(ZBufferParamsPropertyName);

        private bool _shouldEnableDepthRendering;
        private bool _depthRenderingEnabled;

        private XRDisplaySubsystem _xrDisplay;

        private void Start()
        {
            _xrDisplay = OVRManager.GetCurrentDisplaySubsystem();
        }

        public void EnableEnvironmentDepth()
        {
            if (!_depthRenderingEnabled || !_shouldEnableDepthRendering)
            {
                _shouldEnableDepthRendering = true;
                Utils.SetupEnvironmentDepth(new Utils.EnvironmentDepthCreateParams() { removeHands = false });
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
            if (Utils.GetEnvironmentDepthTextureId(ref id))
            {
                var rt = _xrDisplay.GetRenderTexture(id);
                Shader.SetGlobalTexture(DepthTextureID, rt);
            }
            else
            {
                Debug.LogWarning("DepthAPI: no environment texture");
                return;
            }

            Matrix4x4[] reprojectionMatrices = new Matrix4x4[2] { Matrix4x4.identity, Matrix4x4.identity };

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
            reprojectionMatrices[0] = EnvironmentDepthUtils.CalculateReprojection(leftEyeData, leftEyeFrustrum.Fov);

            var rightEyeData = Utils.GetEnvironmentDepthFrameDesc(1);
            reprojectionMatrices[1] = EnvironmentDepthUtils.CalculateReprojection(rightEyeData, rightEyeFrustrum.Fov);

            // Assume NearZ and FarZ are the same for left and right eyes
            float depthNearZ = leftEyeData.nearZ;
            float depthFarZ = leftEyeData.farZ;

            Vector4 depthZBufferParams = EnvironmentDepthUtils.ComputeNdcToLinearDepthParameters(depthNearZ, depthFarZ);

            Shader.SetGlobalMatrixArray(ReprojectionMatricesID,
                reprojectionMatrices);
            Shader.SetGlobalVector(ZBufferParamsID,
                depthZBufferParams);
        }
    }
}
