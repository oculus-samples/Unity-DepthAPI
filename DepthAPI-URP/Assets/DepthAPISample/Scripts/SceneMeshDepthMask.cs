using System.Collections.Generic;
using Meta.XR.EnvironmentDepth;
using Meta.XR.MRUtilityKit;
using TMPro;
using UnityEngine;

namespace DepthAPISample
{
    public class SceneMeshDepthMask : MonoBehaviour
    {
        private const float depthBiasAdjustDecay = 10;

        [SerializeField] TextMeshProUGUI MaskDepthBiasText;
        private EnvironmentDepthManager _environmentDepthManager;
        private float _maskBiasAdjustValue = 0.2f;
        private List<MeshFilter> _wallMeshFilters;

        private bool _isMaskOn;
        private float _depthBiasAdjust = 0;

        private void Awake()
        {
            _environmentDepthManager = FindAnyObjectByType<EnvironmentDepthManager>();
            _wallMeshFilters = new List<MeshFilter>();
        }
        public void LoadRoomMesh()
        {
            if ((MRUK.Instance.GetCurrentRoom() == null) || (_environmentDepthManager == null))
            {
                return;
            }
            _wallMeshFilters.Clear();
            for (var i = 0; i < MRUK.Instance.GetCurrentRoom().WallAnchors.Count; i++)
            {
                Debug.Log("Found wall");
                _wallMeshFilters.Add(MRUK.Instance.GetCurrentRoom().WallAnchors[i].gameObject.GetComponentInChildren<MeshFilter>());
            }

            _environmentDepthManager.MaskMeshFilters = _wallMeshFilters;
        }

        private void Update()
        {
            if (OVRInput.GetDown(OVRInput.RawButton.B))
            {
                if (!_isMaskOn)
                {
                    LoadRoomMesh();
                }
                else
                {
                    _wallMeshFilters.Clear();
                }
                _isMaskOn = !_isMaskOn;
            }

            if (OVRInput.Get(OVRInput.RawButton.RThumbstickLeft))
            {
                _environmentDepthManager.MaskBias -= _maskBiasAdjustValue * Time.deltaTime;
            }

            if (OVRInput.Get(OVRInput.RawButton.RThumbstickRight))
            {
                _environmentDepthManager.MaskBias += _maskBiasAdjustValue * Time.deltaTime;
            }

            if (Mathf.Abs(_depthBiasAdjust) > 0.01f)
            {
                _environmentDepthManager.MaskBias += _maskBiasAdjustValue * _depthBiasAdjust * Time.deltaTime;
                _depthBiasAdjust -= Time.deltaTime * depthBiasAdjustDecay * Mathf.Sign(_depthBiasAdjust);
            }
            else
            {
                _depthBiasAdjust = 0;
            }


            MaskDepthBiasText.text = "Mask bias " + _environmentDepthManager.MaskBias.ToString("#.000");
        }

        public void OnMicroGesture(OVRHand.MicrogestureType gesture)
        {
            switch (gesture)
            {
                case OVRHand.MicrogestureType.SwipeForward:
                {
                    _depthBiasAdjust += 1;
                }
                break;
                case OVRHand.MicrogestureType.SwipeBackward:
                {
                    _depthBiasAdjust += -1;
                }
                break;
                case OVRHand.MicrogestureType.ThumbTap:
                {
                    if (!_isMaskOn)
                    {
                        LoadRoomMesh();
                    }
                    else
                    {
                        _wallMeshFilters.Clear();
                    }
                    _isMaskOn = !_isMaskOn;
                }
                break;
            }
        }
    }
}
