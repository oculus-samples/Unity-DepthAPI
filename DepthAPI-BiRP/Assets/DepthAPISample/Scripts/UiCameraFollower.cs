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

namespace DepthAPISample
{
    public class UiCameraFollower : MonoBehaviour
    {
        private Transform _centerEyeCamera;

        private void Awake()
        {
            if (OVRManager.instance != null)
            {
                _centerEyeCamera = OVRManager.instance.GetComponentInChildren<OVRCameraRig>().centerEyeAnchor;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (_centerEyeCamera == null) return;
            var disp = transform.position - _centerEyeCamera.position;
            if (disp.sqrMagnitude < 0.0001f)
            {
                disp = Vector3.forward;
            }

            var lerpT = Mathf.SmoothStep(0.3f, 0.9f, Time.deltaTime / 50.0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(disp), lerpT);
        }
    }
}
