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

using System;
using Meta.XR.EnvironmentDepth;
using TMPro;
using UnityEngine;

namespace DepthAPISample
{
    /// <summary>
    /// Applies global shader settings to set occlusion types responding to controller input.
    /// </summary>
    public class OcclusionToggler : MonoBehaviour
    {
        [SerializeField]
        private EnvironmentDepthManager _occlusionController;

        private int _currentOcclusionTypeIndex = (int)OcclusionShadersMode.SoftOcclusion;

        [SerializeField]
        private TextMeshProUGUI _currentOcclusionsModeText;

        void Start()
        {
            SetOcclusionType();
        }

        void Update()
        {
            if (OVRInput.GetDown(OVRInput.RawButton.A))
            {
                SwitchToNextOcclusionType();
            }
        }

        private void SwitchToNextOcclusionType()
        {
            _currentOcclusionTypeIndex = (_currentOcclusionTypeIndex + 1) % Enum.GetValues(typeof(OcclusionShadersMode)).Length;
            SetOcclusionType();
        }

        private void SetOcclusionType()
        {
            var newType = (OcclusionShadersMode)_currentOcclusionTypeIndex;

            _occlusionController.OcclusionShadersMode = newType;

            if (_currentOcclusionsModeText)
                _currentOcclusionsModeText.text = $"Occlusion mode: \n{newType.ToString()}";
        }

        public void OnMicroGestureRightHand(OVRHand.MicrogestureType gesture)
        {
            if (gesture == OVRHand.MicrogestureType.ThumbTap)
            {
                SwitchToNextOcclusionType();
            }
        }
    }
}
