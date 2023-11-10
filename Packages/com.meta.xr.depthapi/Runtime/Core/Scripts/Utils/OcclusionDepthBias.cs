using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace com.meta.xr.depthapi.utils
{
    public class OcclusionDepthBias : MonoBehaviour
    {
        public float DepthBiasValue { get; private set; }
        public bool DoesAffectChildren;
        private List<Material> _materials;

        void Awake()
        {
            _materials = new List<Material>();
            if (DoesAffectChildren)
            {
                var renderers = GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers)
                {
                    foreach (var material in rend.materials)
                        _materials.Add(material);
                }
            }
            else
            {
                TryGetComponent(out Renderer rend);
                if (rend != null)
                    foreach (var material in rend.materials)
                        _materials.Add(material);
            }

            SetDepthBias(DepthBiasValue);
        }

        public void SetDepthBias(float value)
        {
            DepthBiasValue = value;
            if (_materials.Count <= 0)
            {
                Debug.LogWarning("No materials found on object. This component will not do anything");
            }
            else
            {
                foreach (var material in _materials)
                {
                    material.SetFloat("_EnvironmentDepthBias", DepthBiasValue);
                }
            }
        }

        public void AdjustDepthBias(float value)
        {
            DepthBiasValue += value;
            SetDepthBias(DepthBiasValue);
        }
    }
}
