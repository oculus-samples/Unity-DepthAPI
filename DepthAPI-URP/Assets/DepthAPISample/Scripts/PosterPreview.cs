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

using com.meta.xr.depthapi.utils;
using TMPro;
using UnityEngine;

namespace DepthAPISample
{
    [RequireComponent(typeof(OcclusionDepthBias))]
    public class PosterPreview : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _biasText;
        private OcclusionDepthBias _depthBiasComponent;
        void Awake()
        {
            _depthBiasComponent = GetComponent<OcclusionDepthBias>();
        }

        public void AdjustDepthBias(float val)
        {
            _depthBiasComponent.AdjustDepthBias(val);
            _biasText.text = $"Depth bias set to:\n{_depthBiasComponent.DepthBiasValue}";
        }
        public void SetDepthBias(float val)
        {
            _depthBiasComponent.SetDepthBias(val);
            _biasText.text = $"Depth bias set to:\n{_depthBiasComponent.DepthBiasValue}";
        }
    }
}
