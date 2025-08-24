// Copyright (c) Meta Platforms, Inc. and affiliates.
// Original Source code from Oculus Starter Samples (https://github.com/oculus-samples/Unity-StarterSamples)

using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PassthroughCameraSamples.StartScene
{
    [MetaCodeSample("PassthroughCameraApiSamples-StartScene")]
    public class ReturnToStartScene : MonoBehaviour
    {
        public bool ShowStartButtonTooltip = true;
        private static ReturnToStartScene s_instance;
        [SerializeField] private GameObject m_tooltip;
        private const float FORWARDTOOLTIPOFFSET = -0.05f;
        private const float UPWARDTOOLTIPOFFSET = -0.003f;

        private void Awake()
        {
            if (s_instance == null)
            {
                s_instance = this;
                m_tooltip.SetActive(ShowStartButtonTooltip);
                DontDestroyOnLoad(gameObject);
            }
            else if (s_instance != this)
            {
                s_instance.ToggleStartButtonTooltip(
                    ShowStartButtonTooltip); // copy the setting from the new loaded scene
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // Since this is the Start scene, we can assume it's the first index in build settings
            if (OVRInput.GetUp(OVRInput.Button.Start) && SceneManager.GetActiveScene().buildIndex != 0)
            {
                SceneManager.LoadScene(0);
            }

            if (ShowStartButtonTooltip)
            {
                var finalRotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch) *
                                    Quaternion.Euler(45, 0, 0);
                var forwardOffsetPosition = finalRotation * Vector3.forward * FORWARDTOOLTIPOFFSET;
                var upwardOffsetPosition = finalRotation * Vector3.up * UPWARDTOOLTIPOFFSET;
                var finalPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch) +
                                    forwardOffsetPosition + upwardOffsetPosition;
                m_tooltip.transform.rotation = finalRotation;
                m_tooltip.transform.position = finalPosition;
            }
        }

        private void ToggleStartButtonTooltip(bool shouldShowTooltip)
        {
            ShowStartButtonTooltip = shouldShowTooltip;
            m_tooltip.SetActive(ShowStartButtonTooltip);
        }
    }
}
