// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.ShaderSample
{
    [MetaCodeSample("PassthroughCameraApiSamples-ShaderSample")]
    public class ShaderSampleManager : MonoBehaviour
    {
        // Create a field to attach the reference to the webCamTextureManager prefab
        [SerializeField] private WebCamTextureManager m_webCamTextureManager;
        [SerializeField] private Text m_debugText;
        [SerializeField] private MeshRenderer m_renderer;

        private IEnumerator Start()
        {
            while (m_webCamTextureManager.WebCamTexture == null)
            {
                yield return null;
            }
            m_debugText.text += "\nWebCamTexture Object ready and playing.";
            // Set WebCamTexture GPU texture as a Main texture of our material
            m_renderer.material.SetTexture("_MainTex", m_webCamTextureManager.WebCamTexture);
        }

        private void Update() => m_debugText.text = PassthroughCameraPermissions.HasCameraPermission == true ? "Permission granted." : "No permission granted.";
    }
}
