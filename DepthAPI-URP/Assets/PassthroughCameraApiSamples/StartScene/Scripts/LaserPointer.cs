// Copyright (c) Meta Platforms, Inc. and affiliates.
// Original Source code from Oculus Starter Samples (https://github.com/oculus-samples/Unity-StarterSamples)

using Meta.XR.Samples;
using UnityEngine;

namespace PassthroughCameraSamples.StartScene
{
    [MetaCodeSample("PassthroughCameraApiSamples-StartScene")]
    public class LaserPointer : OVRCursor
    {
        public enum LaserBeamBehaviorEnum
        {
            On, // laser beam always on
            Off, // laser beam always off
            OnWhenHitTarget, // laser beam only activates when hit valid target
        }

        public GameObject CursorVisual;
        public float MaxLength = 10.0f;

        private LaserBeamBehaviorEnum m_laserBeamBehavior;
        private bool m_restoreOnInputAcquired = false;

        public LaserBeamBehaviorEnum LaserBeamBehavior
        {
            set
            {
                m_laserBeamBehavior = value;
                m_lineRenderer.enabled = LaserBeamBehavior is not LaserBeamBehaviorEnum.Off and not LaserBeamBehaviorEnum.OnWhenHitTarget;
            }
            get => m_laserBeamBehavior;
        }

        private Vector3 m_startPoint;
        private Vector3 m_forward;
        private Vector3 m_endPoint;
        private bool m_hitTarget;
        private LineRenderer m_lineRenderer;

        private void Awake()
        {
            m_lineRenderer = GetComponent<LineRenderer>();
        }

        private void Start()
        {
            if (CursorVisual) CursorVisual.SetActive(false);
            OVRManager.InputFocusAcquired += OnInputFocusAcquired;
            OVRManager.InputFocusLost += OnInputFocusLost;
        }

        public override void SetCursorStartDest(Vector3 start, Vector3 dest, Vector3 normal)
        {
            m_startPoint = start;
            m_endPoint = dest;
            m_hitTarget = true;
        }

        public override void SetCursorRay(Transform t)
        {
            m_startPoint = t.position;
            m_forward = t.forward;
            m_hitTarget = false;
        }

        private void LateUpdate()
        {
            m_lineRenderer.SetPosition(0, m_startPoint);
            if (m_hitTarget)
            {
                m_lineRenderer.SetPosition(1, m_endPoint);
                UpdateLaserBeam(m_startPoint, m_endPoint);
                if (CursorVisual)
                {
                    CursorVisual.transform.position = m_endPoint;
                    CursorVisual.SetActive(true);
                }
            }
            else
            {
                UpdateLaserBeam(m_startPoint, m_startPoint + MaxLength * m_forward);
                m_lineRenderer.SetPosition(1, m_startPoint + MaxLength * m_forward);
                if (CursorVisual) CursorVisual.SetActive(false);
            }
        }

        // make laser beam a behavior with a prop that enables or disables
        private void UpdateLaserBeam(Vector3 start, Vector3 end)
        {
            if (LaserBeamBehavior == LaserBeamBehaviorEnum.Off)
            {
                return;
            }
            else if (LaserBeamBehavior == LaserBeamBehaviorEnum.On)
            {
                m_lineRenderer.SetPosition(0, start);
                m_lineRenderer.SetPosition(1, end);
            }
            else if (LaserBeamBehavior == LaserBeamBehaviorEnum.OnWhenHitTarget)
            {
                if (m_hitTarget)
                {
                    if (!m_lineRenderer.enabled)
                    {
                        m_lineRenderer.enabled = true;
                        m_lineRenderer.SetPosition(0, start);
                        m_lineRenderer.SetPosition(1, end);
                    }
                }
                else
                {
                    if (m_lineRenderer.enabled)
                    {
                        m_lineRenderer.enabled = false;
                    }
                }
            }
        }

        private void OnDisable()
        {
            if (CursorVisual) CursorVisual.SetActive(false);
        }

        public void OnInputFocusLost()
        {
            if (gameObject && gameObject.activeInHierarchy)
            {
                m_restoreOnInputAcquired = true;
                gameObject.SetActive(false);
            }
        }

        public void OnInputFocusAcquired()
        {
            if (m_restoreOnInputAcquired && gameObject)
            {
                m_restoreOnInputAcquired = false;
                gameObject.SetActive(true);
            }
        }

        private void OnDestroy()
        {
            OVRManager.InputFocusAcquired -= OnInputFocusAcquired;
            OVRManager.InputFocusLost -= OnInputFocusLost;
        }
    }
}
