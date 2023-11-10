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

namespace DepthAPISample
{
    using UnityEngine;
    [RequireComponent(typeof(OVRSceneManager))]
    public class SceneAPIWallsToggler : MonoBehaviour
    {
        [SerializeField] private Material _wallsMaterial;
        [SerializeField] private OVRInput.RawButton _wallsVisibilityToggleButton = OVRInput.RawButton.Y;
        [SerializeField] private float _alphaValueWhenVisible = 0.5f;
        private bool _areWallsVisible;

        private void Awake()
        {
            _areWallsVisible = false;
            SetWallsVisible(_areWallsVisible);
        }

        private void Update()
        {
            if (OVRInput.GetDown(_wallsVisibilityToggleButton))
            {
                _areWallsVisible = !_areWallsVisible;
                SetWallsVisible(_areWallsVisible);
            }
        }

        private void SetWallsVisible(bool isOn)
        {
            Debug.Log("Toggling wall view");
            var alphaVal = isOn ? _alphaValueWhenVisible : 0f;
            _wallsMaterial.SetColor("_BaseColor", new Color(1f, 1f, 1f, alphaVal));
        }
    }
}
