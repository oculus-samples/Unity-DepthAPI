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

using UnityEngine;

namespace Meta.XR.Depth
{
    [RequireComponent(typeof(EnvironmentDepthTextureProvider))]
    public class EnvironmentDepthOcclusionController : MonoBehaviour
    {
#if DEPTH_API_SUPPORTED
        public static readonly string HardOcclusionKeyword = "HARD_OCCLUSION";
        public static readonly string SoftOcclusionKeyword = "SOFT_OCCLUSION";

        [SerializeField] private OcclusionType _occlusionType = OcclusionType.NoOcclusion;

        private EnvironmentDepthTextureProvider _depthTextureProvider;


        private void Start()
        {
            _depthTextureProvider = GetComponent<EnvironmentDepthTextureProvider>();

            EnableOcclusionType(_occlusionType, true);
        }

#if UNITY_EDITOR
        private void OnApplicationQuit()
        {
            Shader.DisableKeyword(HardOcclusionKeyword);
            Shader.DisableKeyword(SoftOcclusionKeyword);
        }
#endif

        public void EnableOcclusionType(OcclusionType newOcclusionType, bool updateDepthTextureProvider = true)
        {
            _occlusionType = newOcclusionType;

            if (updateDepthTextureProvider)
            {
                if (_occlusionType == OcclusionType.NoOcclusion && _depthTextureProvider.GetEnvironmentDepthEnabled())
                {
                    _depthTextureProvider.DisableEnvironmentDepth();
                }
                else
                {
                    _depthTextureProvider.EnableEnvironmentDepth();
                }
            }

            switch (_occlusionType)
            {
                case OcclusionType.HardOcclusion:
                    Shader.DisableKeyword(SoftOcclusionKeyword);
                    Shader.EnableKeyword(HardOcclusionKeyword);
                    break;
                case OcclusionType.SoftOcclusion:
                    Shader.DisableKeyword(HardOcclusionKeyword);
                    Shader.EnableKeyword(SoftOcclusionKeyword);
                    break;
                default:
                    Shader.DisableKeyword(HardOcclusionKeyword);
                    Shader.DisableKeyword(SoftOcclusionKeyword);
                    break;
            }

        }
#endif
    }
}
