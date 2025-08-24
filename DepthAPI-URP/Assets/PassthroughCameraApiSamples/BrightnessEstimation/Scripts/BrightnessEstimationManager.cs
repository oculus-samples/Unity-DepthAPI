// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;

namespace PassthroughCameraSamples.BrightnessEstimation
{
    [MetaCodeSample("PassthroughCameraApiSamples-BrightnessEstimation")]
    public class BrightnessEstimationManager : MonoBehaviour
    {
        [SerializeField] private WebCamTextureManager m_webCamTextureManager;
        [SerializeField] private float m_refreshTime = 0.05f;
        [SerializeField][Range(1, 100)] private int m_bufferSize = 10;
        [SerializeField] private UnityEvent<float> m_onBrightnessChange;
        [SerializeField] private UnityEngine.UI.Text m_debugger;

        private float m_refreshCurrentTime = 0.0f;
        private List<float> m_brightnessVals = new();
        private Color32[] m_pixelsBuffer;

        private void Update()
        {
            var webCamTexture = m_webCamTextureManager.WebCamTexture;
            if (webCamTexture != null)
            {
                // Get the WebCamTexture CPU image
                // Process WebCamTexture data
                if (!IsWaiting())
                {
                    m_debugger.text = GetRoomAmbientLight(webCamTexture);
                    m_onBrightnessChange?.Invoke(GetGlobalBrigthnessLevel());
                }
            }
            else
            {
                m_debugger.text = PassthroughCameraPermissions.HasCameraPermission == true ? "Permission granted." : "No permission granted.";
            }
        }

        /// <summary>
        /// Estimate the Brightness Level using a Texture2D
        /// </summary>
        /// <returns>String data for debugging purposes</returns>
        private string GetRoomAmbientLight(WebCamTexture webCamTexture)
        {
            if (!webCamTexture.isPlaying)
            {
                return "WebCamTexture is not playing.";
            }
            m_refreshCurrentTime = m_refreshTime;
            var w = webCamTexture.width;
            var h = webCamTexture.height;

            m_pixelsBuffer ??= new Color32[w * h];
            _ = webCamTexture.GetPixels32(m_pixelsBuffer);

            float colorSum = 0;
            for (int x = 0, len = m_pixelsBuffer.Length; x < len; x++)
            {
                colorSum += 0.2126f * m_pixelsBuffer[x].r + 0.7152f * m_pixelsBuffer[x].g + 0.0722f * m_pixelsBuffer[x].b;
            }
            var brightnessVals = Mathf.Floor(colorSum / (w * h));

            m_brightnessVals.Add(brightnessVals);

            if (m_brightnessVals.Count > m_bufferSize)
            {
                m_brightnessVals.RemoveAt(0);
            }

            return $"Current brigthnessLevel: {brightnessVals}\nGlobal value: {GetGlobalBrigthnessLevel()}";
        }

        /// <summary>
        /// Return true if the waiting time is bigger than zero.
        /// </summary>
        /// <returns>True or False</returns>
        private bool IsWaiting()
        {
            m_refreshCurrentTime -= Time.deltaTime;
            return m_refreshCurrentTime > 0.0f;
        }

        /// <summary>
        /// Get the average Brightness level based on the buffer size.
        /// </summary>
        /// <returns>Average brightness level (float)</returns>
        private float GetGlobalBrigthnessLevel()
        {
            if (m_brightnessVals.Count == 0)
            {
                return -1;
            }

            var sum = 0.0f;
            foreach (var b in m_brightnessVals)
            {
                sum += b;
            }
            return sum / m_brightnessVals.Count;
        }
    }
}
