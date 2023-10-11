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

using Meta.XR.Depth;
using UnityEngine;

namespace DepthAPISample
{
    /// <summary>
    /// Sets occlusion type for this object.
    /// </summary>
    public class OcclusionController : MonoBehaviour
    {
        [SerializeField]
        private OcclusionType _occlusionType;

        [SerializeField]
        private Renderer _renderer;

        private Material _material;

        private void Awake()
        {
            _material = _renderer.material;
        }

        void Update()
        {
            UpdateMaterialKeywords();
        }

        private void UpdateMaterialKeywords()
        {
            switch (_occlusionType)
            {
                case OcclusionType.HardOcclusion:
                    _material.DisableKeyword(EnvironmentDepthOcclusionController.SoftOcclusionKeyword);
                    _material.EnableKeyword(EnvironmentDepthOcclusionController.HardOcclusionKeyword);
                    break;
                case OcclusionType.SoftOcclusion:
                    _material.DisableKeyword(EnvironmentDepthOcclusionController.HardOcclusionKeyword);
                    _material.EnableKeyword(EnvironmentDepthOcclusionController.SoftOcclusionKeyword);
                    break;
                default:
                    _material.DisableKeyword(EnvironmentDepthOcclusionController.HardOcclusionKeyword);
                    _material.DisableKeyword(EnvironmentDepthOcclusionController.SoftOcclusionKeyword);
                    break;
            }
        }
    }
}
