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
using com.meta.xr.depthapi.utils;
using TMPro;
using UnityEngine;

namespace DepthAPISample
{
    [RequireComponent(typeof(OcclusionDepthBias))]
    public class Poster : MonoBehaviour
    {
        public Action<Poster> OnHighlight;
        [SerializeField] private GameObject _highlight;
        [SerializeField] private TextMeshPro _biasText;
        [SerializeField] private AudioClip _highlightAudio;
        private OcclusionDepthBias _depthBiasComponent;
        private AudioSource _audioSource;
        private bool _isHighlit;

        private void Awake()
        {
            _depthBiasComponent = GetComponent<OcclusionDepthBias>();
            _audioSource = GetComponent<AudioSource>();
        }

        public void Highlight()
        {
            _highlight.SetActive(true);
            _biasText.gameObject.SetActive(true);
            _isHighlit = true;
            if (_audioSource != null)
            {
                _audioSource.clip = _highlightAudio;
                _audioSource.Play();
            }
            OnHighlight?.Invoke(this);
        }

        public void Unhighlight()
        {
            _biasText.gameObject.SetActive(false);
            _highlight.SetActive(false);
            _isHighlit = false;
        }

        public void AdjustDepthBias(float val)
        {
            if (!_isHighlit) return;
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
