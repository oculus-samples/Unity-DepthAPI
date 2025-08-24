// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    [MetaCodeSample("PassthroughCameraApiSamples-MultiObjectDetection")]
    public class DetectionUiBlinkText : MonoBehaviour
    {
        [SerializeField] private Text m_labelInfo;
        [SerializeField] private float m_blinkSpeed = 0.2f;
        private float m_blinkTime = 0.0f;
        private Color m_color;

        private void Start()
        {
            m_color = m_labelInfo.color;
        }

        private void LateUpdate()
        {
            m_blinkTime += Time.deltaTime;

            if (m_blinkTime >= m_blinkSpeed)
            {

                m_color.a = m_color.a > 0f ? 0f : 1f;
                m_labelInfo.color = m_color;
                m_blinkTime = 0;
            }
        }
    }
}
