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
using UnityEngine;

namespace DepthAPISample
{
    public class WallViewController : MonoBehaviour
    {
        [SerializeField] private OVRInput.RawButton _wallsVisibilityToggleButton = OVRInput.RawButton.Y;
        private bool _areWallsVisible;

        private MeshRenderer _meshRenderer;
        private Material _material;

        void Start()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _material = _meshRenderer.material;
            if (_material != null)
            {
                AdjustTextureTiling();
            }
        }

        private void AdjustTextureTiling()
        {
            _material.mainTextureScale = new Vector2(transform.localScale.x, transform.localScale.y);
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                _areWallsVisible = !_areWallsVisible;
                SetWallsVisible(_areWallsVisible);
            }
            if (OVRInput.GetDown(_wallsVisibilityToggleButton))
            {
                _areWallsVisible = !_areWallsVisible;
                SetWallsVisible(_areWallsVisible);
            }
        }

        private void SetWallsVisible(bool isOn)
        {
            _meshRenderer.enabled = isOn;
        }
    }
}
