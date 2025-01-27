using System.Collections.Generic;
using Meta.XR.EnvironmentDepth;
using Meta.XR.MRUtilityKit;
using TMPro;
using UnityEngine;

namespace DepthAPISample
{
    public class SceneMeshDepthMask : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI MaskDepthBiasText;
        [SerializeField] private OVRInput.RawButton _maskToggleButton = OVRInput.RawButton.B;
        [SerializeField] private OVRInput.RawButton _maskBiasAdjustDecreaseButton = OVRInput.RawButton.RThumbstickLeft;
        [SerializeField] private OVRInput.RawButton _maskBiasAdjustIncreaseButton = OVRInput.RawButton.RThumbstickRight;
        private EnvironmentDepthManager _environmentDepthManager;
        private float _maskBiasAdjustValue = 0.2f;
        private List<MeshFilter> _wallMeshFilters = new();

        private bool _isMaskOn;

        private void Awake()
        {
            _environmentDepthManager = FindAnyObjectByType<EnvironmentDepthManager>();
        }

        private void LoadRoomMesh()
        {
            if (_environmentDepthManager == null)
                return;
            if ((MRUK.Instance.GetCurrentRoom() == null) || (_environmentDepthManager == null))
            {
                return;
            }
            _wallMeshFilters.Clear();
            for (var i = 0; i < MRUK.Instance.GetCurrentRoom().WallAnchors.Count; i++)
            {
                _wallMeshFilters.Add(MRUK.Instance.GetCurrentRoom().WallAnchors[i].gameObject.GetComponentInChildren<MeshFilter>());
            }

            _environmentDepthManager.MaskMeshFilters = _wallMeshFilters;
        }

        private void Update()
        {
            if (OVRInput.GetDown(_maskToggleButton))
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

            if (OVRInput.Get(_maskBiasAdjustDecreaseButton))
            {
                _environmentDepthManager.MaskBias -= _maskBiasAdjustValue * Time.deltaTime;
            }

            if (OVRInput.Get(_maskBiasAdjustIncreaseButton))
            {
                _environmentDepthManager.MaskBias += _maskBiasAdjustValue * Time.deltaTime;
            }
            if (MaskDepthBiasText != null)
                MaskDepthBiasText.text = "Mask bias " + _environmentDepthManager.MaskBias.ToString("#.000");
        }
    }
}
