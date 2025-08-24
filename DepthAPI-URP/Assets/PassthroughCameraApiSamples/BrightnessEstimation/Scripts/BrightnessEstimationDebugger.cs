// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PassthroughCameraSamples.BrightnessEstimation
{
    [MetaCodeSample("PassthroughCameraApiSamples-BrightnessEstimation")]
    public class BrightnessEstimationDebugger : MonoBehaviour
    {
        [SerializeField] private Text m_debugger;
        [SerializeField] private UnityEvent m_onTooDark;
        [SerializeField] private UnityEvent m_onTooLight;
        [Range(0, 100)][SerializeField] private float m_minBrightnessLevel = 10;
        [Range(0, 100)][SerializeField] private float m_maxBrightnessLevel = 50;

        private int m_isDark = 2;
        private string m_brightnessStatus = "";

        public void OnChangeBrightness(float value)
        {
            if (m_debugger)
            {
                m_debugger.text = $"Brightness level: {value} \n\n {m_brightnessStatus}";
            }

            if (value <= m_minBrightnessLevel && m_isDark != 2)
            {
                m_onTooDark?.Invoke();
                m_isDark = 2;
            }
            else if (value >= m_maxBrightnessLevel && m_isDark != 1)
            {
                m_onTooLight?.Invoke();
                m_isDark = 1;
            }
        }

        public void TooDark()
        {
            m_brightnessStatus = "IS TOO DARK, TURN LIGHTS ON!";
        }

        public void TooLight()
        {
            m_brightnessStatus = "TOO BRIGHT, TURN LIGHTS OFF!";
        }
    }
}
