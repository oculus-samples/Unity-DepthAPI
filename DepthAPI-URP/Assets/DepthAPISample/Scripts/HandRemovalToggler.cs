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
using UnityEngine;

namespace DepthAPISample
{
    public class HandRemovalToggler : MonoBehaviour
    {
        public Action<HandsRemovalStyle> OnHandsRemovalStyleChanged;
        public enum HandsRemovalStyle
        {
            None = 0,
            VirtualHandMask = 1
        }

        [SerializeField] private OVRInput.RawButton _handsRemovalStyleToggler = OVRInput.RawButton.X;
        [SerializeField] private Renderer _leftHandVisuals;
        [SerializeField] private Renderer _rightHandVisuals;
        private EnvironmentDepthManager _depthTextureManager;
        private HandsRemovalStyle _occlStyle = HandsRemovalStyle.VirtualHandMask;
        private int _occlustionType;

        private void Awake()
        {
            _depthTextureManager = GetComponent<EnvironmentDepthManager>();
        }

        private void Start()
        {
            _occlustionType = (int)_occlStyle;
            SetHandsOcclusionStyle(_occlStyle);
        }

        void Update()
        {
            if (OVRInput.GetDown(_handsRemovalStyleToggler))
            {
                _occlustionType = ((_occlustionType + 1) % Enum.GetValues(typeof(HandsRemovalStyle)).Length);
                _occlStyle = (HandsRemovalStyle)_occlustionType;
                SetHandsOcclusionStyle(_occlStyle);
            }
        }

        private void SetHandsOcclusionStyle(HandsRemovalStyle style)
        {
            switch (style)
            {
                case HandsRemovalStyle.None:
                    _depthTextureManager.RemoveHands = false;
                    _leftHandVisuals.gameObject.SetActive(false);
                    _rightHandVisuals.gameObject.SetActive(false);
                    break;
                case HandsRemovalStyle.VirtualHandMask:
                    _depthTextureManager.RemoveHands = true;
                    _leftHandVisuals.gameObject.SetActive(true);
                    _rightHandVisuals.gameObject.SetActive(true);
                    break;
            }

            OnHandsRemovalStyleChanged?.Invoke(style);
        }

        public void OnMicroGestureLeftHand(OVRHand.MicrogestureType gesture)
        {
            if (gesture == OVRHand.MicrogestureType.ThumbTap)
            {
                _occlustionType = ((_occlustionType + 1) % Enum.GetValues(typeof(HandsRemovalStyle)).Length);
                _occlStyle = (HandsRemovalStyle)_occlustionType;
                SetHandsOcclusionStyle(_occlStyle);
            }
        }
    }
}
