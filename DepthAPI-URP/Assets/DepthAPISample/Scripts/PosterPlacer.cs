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

using System.Collections.Generic;
using UnityEngine;

namespace DepthAPISample
{
    public class PosterPlacer : MonoBehaviour
    {
        [SerializeField] private Transform _rayOrigin;
        [SerializeField] private Poster _posterPrefab;
        [SerializeField] private PosterPreview _posterPreviewPrefab;
        [SerializeField] private OVRInput.RawButton _posterPlacingButton = OVRInput.RawButton.RIndexTrigger;
        [SerializeField] private OVRInput.RawButton _postersCleanupButton = OVRInput.RawButton.B;
        [SerializeField] private OVRInput.RawButton _posterIncreaseBiasValueButton = OVRInput.RawButton.RThumbstickUp;
        [SerializeField] private OVRInput.RawButton _posterDecreaseBiasValueButton = OVRInput.RawButton.RThumbstickDown;
        [SerializeField] private OVRInput.RawButton _posterRotateRightButton = OVRInput.RawButton.RThumbstickRight;
        [SerializeField] private OVRInput.RawButton _posterRotateLeftButton = OVRInput.RawButton.RThumbstickLeft;
        [SerializeField] private float _depthBiasChangeValue = .01f;
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private ChangeInputListener _inputListener;
        private LayerMask _layerMaskWall;
        private LayerMask _layerMaskPoster;
        private PosterPreview _posterPreview;
        private float _previewPosterDepthBiasValue = 0.06f; //Posters placed will have their initial depth bias set to this value
        private GameObject _currentHighlightedPosterObject;
        private List<Poster> _currentPosters;
        private bool _isPosterHit;
        private bool _isUsingHands;
        private float _rotationValue;

        private void Awake()
        {
            _layerMaskWall = LayerMask.GetMask("Wall");
            _layerMaskPoster = LayerMask.GetMask("Poster");

            _posterPreview = Instantiate(_posterPreviewPrefab);
            _posterPreview.SetDepthBias(_previewPosterDepthBiasValue);

            _currentPosters = new List<Poster>();

            _isUsingHands = _inputListener.IsUsingHands();
        }

        private void OnEnable()
        {
            _inputListener.OnInputChanged += HandleInputChanged;
        }

        private void OnDisable()
        {
            _inputListener.OnInputChanged -= HandleInputChanged;
        }

        private void Update()
        {
            _lineRenderer.enabled = !_isUsingHands;
            _posterPreview.gameObject.SetActive(!_isUsingHands);
            if (_isUsingHands)
            {
                return;
            }

            Ray ray = new Ray(_rayOrigin.position, _rayOrigin.position + _rayOrigin.forward);
            RaycastHit hit;
            if (OVRInput.Get(_posterIncreaseBiasValueButton))
            {
                if (_currentHighlightedPosterObject != null)
                {
                    _currentHighlightedPosterObject.GetComponent<Poster>().AdjustDepthBias(_depthBiasChangeValue * Time.deltaTime);
                }
                else
                {
                    if (_posterPreview != null)
                    {
                        _previewPosterDepthBiasValue += _depthBiasChangeValue * Time.deltaTime;
                        _posterPreview.SetDepthBias(_previewPosterDepthBiasValue);
                    }
                }
            }
            if (OVRInput.Get(_posterDecreaseBiasValueButton))
            {
                if (_currentHighlightedPosterObject != null)
                {
                    _currentHighlightedPosterObject.GetComponent<Poster>().AdjustDepthBias(-_depthBiasChangeValue * Time.deltaTime);
                }
                else
                {
                    if (_posterPreview != null)
                    {
                        _previewPosterDepthBiasValue -= _depthBiasChangeValue * Time.deltaTime;
                        _posterPreview.SetDepthBias(_previewPosterDepthBiasValue);
                    }
                }
            }
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, _layerMaskPoster))
            {
                _isPosterHit = true;
                HidePosterPreview();

                _lineRenderer.startColor = Color.white;
                _lineRenderer.endColor = Color.white;
                _lineRenderer.SetPositions(new Vector3[] { _rayOrigin.position, hit.point });

                if (_currentHighlightedPosterObject != hit.collider.gameObject)
                {
                    if (_currentHighlightedPosterObject != null)
                    {
                        _currentHighlightedPosterObject.GetComponent<Poster>().Unhighlight();
                    }

                    _currentHighlightedPosterObject = hit.collider.gameObject;
                    _currentHighlightedPosterObject.GetComponent<Poster>().Highlight();
                }
            }
            else
            {
                _isPosterHit = false;
                if (_currentHighlightedPosterObject != null)
                {
                    _currentHighlightedPosterObject.GetComponent<Poster>().Unhighlight();
                    _currentHighlightedPosterObject = null;
                }
            }

            if (!_isPosterHit)
            {
                if (Physics.Raycast(ray, out hit, Mathf.Infinity, _layerMaskWall))
                {
                    ProjectPoster(hit);
                    UnhighlightAllPosters();
                }
                else
                {
                    HidePosterPreview();
                    _lineRenderer.startColor = Color.red;
                    _lineRenderer.endColor = Color.red;
                    _lineRenderer.SetPositions(new Vector3[] { _rayOrigin.position, _rayOrigin.position + _rayOrigin.forward * 10f });
                }
            }
            if (OVRInput.GetDown(_postersCleanupButton))
            {
                ClearPosters();
            }

            if (OVRInput.Get(_posterRotateRightButton))
            {
                _rotationValue += Time.deltaTime * 50f;
            }

            if (OVRInput.Get(_posterRotateLeftButton))
            {
                _rotationValue -= Time.deltaTime * 50f;
            }
        }

        private void UnhighlightAllPosters()
        {
            foreach (var tempPoster in _currentPosters)
            {
                tempPoster.Unhighlight();
            }
        }

        private void ProjectPoster(RaycastHit hit)
        {
            _lineRenderer.startColor = Color.green;
            _lineRenderer.endColor = Color.green;
            _lineRenderer.SetPositions(new Vector3[] { _rayOrigin.position, hit.point });

            PlacePreviewPoster(hit);
            if (OVRInput.GetDown(_posterPlacingButton))
            {
                PlacePoster(hit);
            }
        }
        private void PlacePoster(RaycastHit hit)
        {
            if (_posterPrefab == null)
            {
                Debug.LogError("Poster prefab is not assigned.");
                return;
            }
            var poster = Instantiate(_posterPrefab, hit.point, _posterPreview.transform.rotation);
            poster.SetDepthBias(_previewPosterDepthBiasValue);

            _currentPosters.Add(poster);
            poster.OnHighlight += (poster) =>
            {
                foreach (var tempPoster in _currentPosters)
                {
                    if (tempPoster == poster)
                    {
                        continue;
                    }
                    tempPoster.Unhighlight();
                }
            };

            Debug.Log("Poster placed on the wall.");
        }

        private void PlacePreviewPoster(RaycastHit hit)
        {
            if (_posterPreview == null)
            {
                Debug.LogError("Poster preview prefab is not assigned.");
                return;
            }
            _posterPreview.gameObject.SetActive(true);
            //detect if on horizontal surface
            float minAllowanceDegrees = 75;
            float maxAllowanceDegrees = 105;
            bool isOnHorizontalPlane;
            float angle = Vector3.Angle(-hit.normal, Vector3.up);
            isOnHorizontalPlane = (angle >= minAllowanceDegrees && angle <= maxAllowanceDegrees);

            Vector3 upwardsRotation;

            if (!isOnHorizontalPlane)
            {
                upwardsRotation = Quaternion.Euler(0, _rotationValue, 0) * Vector3.forward;
            }
            else
            {
                upwardsRotation = Quaternion.Euler(-_rotationValue, 0, 0) * Vector3.up;
            }

            _posterPreview.transform.rotation = Quaternion.LookRotation(-hit.normal, upwardsRotation);

            _posterPreview.transform.position = hit.point;
        }

        private void HidePosterPreview()
        {
            _posterPreview.gameObject.SetActive(false);
            _rotationValue = 0;
        }

        private void ClearPosters()
        {
            foreach (var poster in _currentPosters)
            {
                Destroy(poster.gameObject);
            }
            _currentPosters.Clear();
        }

        private void HandleInputChanged(bool isUsingHands) => _isUsingHands = isUsingHands;
    }
}
