// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace PassthroughCameraSamples.CameraToWorld
{
    [MetaCodeSample("PassthroughCameraApiSamples-CameraToWorld")]
    public class CameraToWorldRayRenderer : MonoBehaviour
    {
        [SerializeField] private GameObject m_middleSegment;

        public void RenderMiddleSegment(bool shouldRender)
        {
            m_middleSegment.SetActive(shouldRender);
        }
    }
}
