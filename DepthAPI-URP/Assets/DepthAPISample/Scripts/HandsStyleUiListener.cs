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

using TMPro;
using UnityEngine;

namespace DepthAPISample
{
    public class HandsStyleUiListener : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _handsStyleText;

        [SerializeField] private HandRemovalToggler _handsRemovalToggler;

        [SerializeField] private OVRHand _ovrHand;

        [SerializeField] private GameObject _useHandsPromptUi;
        [SerializeField] private GameObject _inputInstructionsUi;

        private void OnEnable()
        {
            _handsRemovalToggler.OnHandsRemovalStyleChanged += UpdateHandsStyleText;
        }

        private void OnDisable()
        {
            _handsRemovalToggler.OnHandsRemovalStyleChanged -= UpdateHandsStyleText;
        }

        private void Update()
        {
            if (_ovrHand != null)
            {
                _useHandsPromptUi.SetActive(!_ovrHand.IsTracked);
                _inputInstructionsUi.SetActive(_ovrHand.IsTracked);
            }
        }

        private void UpdateHandsStyleText(HandRemovalToggler.HandsRemovalStyle style)
        {
            _handsStyleText.text = $"Hands removal style: {style}";
        }
    }
}
