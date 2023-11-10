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
        [SerializeField] private Poster posterPrefab;
        [SerializeField] private OVRInput.RawButton _posterPlacingButton = OVRInput.RawButton.RIndexTrigger;
        [SerializeField] private OVRInput.RawButton _postersCleanupButton = OVRInput.RawButton.B;
        [SerializeField] private OVRInput.RawButton _posterIncreaseBiasValueButton = OVRInput.RawButton.RThumbstickUp;
        [SerializeField] private OVRInput.RawButton _posterDecreaseBiasValueButton = OVRInput.RawButton.RThumbstickDown;
        [SerializeField] private float _depthBiasChangeValue = .3f;
        [SerializeField] private LineRenderer _lineRenderer;
        private LayerMask _layerMaskWall;
        private LayerMask _layerMaskPoster;
        private Poster _currentPosterGhost;
        private GameObject _currentHighlightedPosterObject;
        private List<Poster> _currentPosters;
        private bool _isPosterHit;

        private void Awake()
        {
            _layerMaskWall = LayerMask.GetMask("Wall");
            _layerMaskPoster = LayerMask.GetMask("Poster");
            _currentPosterGhost = Instantiate(posterPrefab);
            var renderers = _currentPosterGhost.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var rend in renderers)
            {
                rend.material.color = new Color(rend.material.color.r, rend.material.color.g, rend.material.color.b, 0.5f);
                rend.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            }
            _currentPosters = new List<Poster>();
        }
        private void Update()
        {
            Ray ray = new Ray(_rayOrigin.position, _rayOrigin.position + _rayOrigin.forward);
            RaycastHit hit;
            if (OVRInput.Get(_posterIncreaseBiasValueButton))
            {
                if (_currentHighlightedPosterObject != null)
                    _currentHighlightedPosterObject.GetComponent<Poster>().AdjustDepthBias(_depthBiasChangeValue * Time.deltaTime);
            }
            if (OVRInput.Get(_posterDecreaseBiasValueButton))
            {
                if (_currentHighlightedPosterObject != null)
                    _currentHighlightedPosterObject.GetComponent<Poster>().AdjustDepthBias(-_depthBiasChangeValue * Time.deltaTime);
            }
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, _layerMaskPoster))
            {
                _isPosterHit = true;
                HideGhostPoster();

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
                    _isPosterHit = false;
                    HideGhostPoster();
                    _lineRenderer.startColor = Color.red;
                    _lineRenderer.endColor = Color.red;
                    _lineRenderer.SetPositions(new Vector3[] { _rayOrigin.position, _rayOrigin.position + _rayOrigin.forward * 10f });
                }
            }

            if (OVRInput.GetDown(_postersCleanupButton))
            {
                ClearPosters();
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

            PlaceGhostPoster(hit);
            if (OVRInput.GetDown(_posterPlacingButton))
            {
                PlacePoster(hit);
            }
        }
        private void PlacePoster(RaycastHit hit)
        {
            if (posterPrefab == null)
            {
                Debug.LogError("Poster prefab is not assigned.");
                return;
            }
            var poster = Instantiate(posterPrefab, hit.point, Quaternion.LookRotation(-hit.normal));
            poster.transform.SetParent(hit.transform);
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

        private void PlaceGhostPoster(RaycastHit hit)
        {
            if (_currentPosterGhost == null)
            {
                Debug.LogError("Poster prefab is not assigned.");
                return;
            }
            _currentPosterGhost.gameObject.SetActive(true);
            _currentPosterGhost.transform.rotation = Quaternion.LookRotation(-hit.normal);
            _currentPosterGhost.transform.position = hit.point;
        }

        private void HideGhostPoster()
        {
            _currentPosterGhost.gameObject.SetActive(false);
        }

        private void ClearPosters()
        {
            foreach (var poster in _currentPosters)
            {
                Destroy(poster.gameObject);
            }
            _currentPosters.Clear();
        }
    }
}
