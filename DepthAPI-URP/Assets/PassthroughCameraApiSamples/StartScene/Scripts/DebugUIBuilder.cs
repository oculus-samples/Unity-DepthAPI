// Copyright (c) Meta Platforms, Inc. and affiliates.
// Original Source code from Oculus Starter Samples (https://github.com/oculus-samples/Unity-StarterSamples)

using System.Collections.Generic;
using Meta.XR.Samples;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.StartScene
{
    [MetaCodeSample("PassthroughCameraApiSamples-StartScene")]
    public class DebugUIBuilder : MonoBehaviour
    {
        // room for extension:
        // support update funcs
        // fix bug where it seems to appear at a random offset
        // support remove

        // Convenience consts for clarity when using multiple debug panes.
        // But note that you can an arbitrary number of panes if you add them in the inspector.
        public const int DEBUG_PANE_CENTER = 0;
        public const int DEBUG_PANE_RIGHT = 1;
        public const int DEBUG_PANE_LEFT = 2;

        [SerializeField]
        private RectTransform m_buttonPrefab = null;

        [SerializeField]
        private RectTransform[] m_additionalButtonPrefab = null;

        [SerializeField]
        private RectTransform m_labelPrefab = null;

        [SerializeField]
        private RectTransform m_sliderPrefab = null;

        [SerializeField]
        private RectTransform m_dividerPrefab = null;

        [SerializeField]
        private RectTransform m_togglePrefab = null;

        [SerializeField]
        private RectTransform m_radioPrefab = null;

        [SerializeField]
        private RectTransform m_textPrefab = null;

        [SerializeField]
        private GameObject m_uiHelpersToInstantiate = null;

        [SerializeField]
        private Transform[] m_targetContentPanels = null;

        private bool[] m_reEnable;

        [SerializeField]
        private List<GameObject> m_toEnable = null;

        [SerializeField]
        private List<GameObject> m_toDisable = null;

        public static DebugUIBuilder Instance;

        public delegate void OnClick();

        public delegate void OnToggleValueChange(Toggle t);

        public delegate void OnSlider(float f);

        public delegate bool ActiveUpdate();

        public float ElementSpacing = 16.0f;
        public float MarginH = 16.0f;
        public float MarginV = 16.0f;
        private Vector2[] m_insertPositions;
        private List<RectTransform>[] m_insertedElements;
        private Vector3 m_menuOffset;
        private OVRCameraRig m_rig;
        private Dictionary<string, ToggleGroup> m_radioGroups = new();
        private LaserPointer m_lp;
        private LineRenderer m_lr;

        public LaserPointer.LaserBeamBehaviorEnum LaserBeamBehavior = LaserPointer.LaserBeamBehaviorEnum.OnWhenHitTarget;
        public bool IsHorizontal = false;
        public bool UsePanelCentricRelayout = false;

        public void Awake()
        {
            Debug.Assert(Instance == null);
            Instance = this;
            m_menuOffset = transform.position;
            gameObject.SetActive(false);
            m_rig = FindFirstObjectByType<OVRCameraRig>();
            for (var i = 0; i < m_toEnable.Count; ++i)
            {
                m_toEnable[i].SetActive(false);
            }

            m_insertPositions = new Vector2[m_targetContentPanels.Length];
            for (var i = 0; i < m_insertPositions.Length; ++i)
            {
                m_insertPositions[i].x = MarginH;
                m_insertPositions[i].y = -MarginV;
            }

            m_insertedElements = new List<RectTransform>[m_targetContentPanels.Length];
            for (var i = 0; i < m_insertedElements.Length; ++i)
            {
                m_insertedElements[i] = new List<RectTransform>();
            }

            if (m_uiHelpersToInstantiate)
            {
                _ = Instantiate(m_uiHelpersToInstantiate);
            }

            m_lp = FindFirstObjectByType<LaserPointer>();
            if (!m_lp)
            {
                Debug.LogError("Debug UI requires use of a LaserPointer and will not function without it. " +
                            "Add one to your scene, or assign the UIHelpers prefab to the DebugUIBuilder in the inspector.");
                return;
            }

            m_lp.LaserBeamBehavior = LaserBeamBehavior;

            if (!m_toEnable.Contains(m_lp.gameObject))
            {
                m_toEnable.Add(m_lp.gameObject);
            }

            GetComponent<OVRRaycaster>().pointer = m_lp.gameObject;
            m_lp.gameObject.SetActive(false);
        }

        public void Show()
        {
            Relayout();
            gameObject.SetActive(true);
            transform.position = m_rig.transform.TransformPoint(m_menuOffset);
            var newEulerRot = m_rig.transform.rotation.eulerAngles;
            newEulerRot.x = 0.0f;
            newEulerRot.z = 0.0f;
            transform.eulerAngles = newEulerRot;

            if (m_reEnable == null || m_reEnable.Length < m_toDisable.Count) m_reEnable = new bool[m_toDisable.Count];
            m_reEnable.Initialize();
            var len = m_toDisable.Count;
            for (var i = 0; i < len; ++i)
            {
                if (m_toDisable[i])
                {
                    m_reEnable[i] = m_toDisable[i].activeSelf;
                    m_toDisable[i].SetActive(false);
                }
            }

            len = m_toEnable.Count;
            for (var i = 0; i < len; ++i)
            {
                m_toEnable[i].SetActive(true);
            }

            var numPanels = m_targetContentPanels.Length;
            for (var i = 0; i < numPanels; ++i)
            {
                m_targetContentPanels[i].gameObject.SetActive(m_insertedElements[i].Count > 0);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);

            for (var i = 0; i < m_reEnable.Length; ++i)
            {
                if (m_toDisable[i] && m_reEnable[i])
                {
                    m_toDisable[i].SetActive(true);
                }
            }

            var len = m_toEnable.Count;
            for (var i = 0; i < len; ++i)
            {
                m_toEnable[i].SetActive(false);
            }
        }

        // Currently a slow brute-force method that lays out every element.
        // As this is intended as a debug UI, it might be fine, but there are many simple optimizations we can make.
        private void StackedRelayout()
        {
            for (var panelIdx = 0; panelIdx < m_targetContentPanels.Length; ++panelIdx)
            {
                var canvasRect = m_targetContentPanels[panelIdx].GetComponent<RectTransform>();
                var elems = m_insertedElements[panelIdx];
                var elemCount = elems.Count;
                var x = MarginH;
                var y = -MarginV;
                var maxWidth = 0.0f;
                for (var elemIdx = 0; elemIdx < elemCount; ++elemIdx)
                {
                    var r = elems[elemIdx];
                    r.anchoredPosition = new Vector2(x, y);

                    if (IsHorizontal)
                    {
                        x += r.rect.width + ElementSpacing;
                    }
                    else
                    {
                        y -= r.rect.height + ElementSpacing;
                    }

                    maxWidth = Mathf.Max(r.rect.width + 2 * MarginH, maxWidth);
                }

                canvasRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth);
                canvasRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, -y + MarginV);
            }
        }

        private void PanelCentricRelayout()
        {
            if (!IsHorizontal)
            {
                Debug.Log("Error:Panel Centeric relayout is implemented only for horizontal panels");
                return;
            }

            for (var panelIdx = 0; panelIdx < m_targetContentPanels.Length; ++panelIdx)
            {
                var canvasRect = m_targetContentPanels[panelIdx].GetComponent<RectTransform>();
                var elems = m_insertedElements[panelIdx];
                var elemCount = elems.Count;
                var x = MarginH;
                _ = -MarginV;
                var maxWidth = x;
                for (var elemIdx = 0; elemIdx < elemCount; ++elemIdx)
                {
                    var r = elems[elemIdx];
                    maxWidth += r.rect.width + ElementSpacing;
                }

                maxWidth -= ElementSpacing;
                maxWidth += MarginH;
                var totalmaxWidth = maxWidth;
                x = -0.5f * totalmaxWidth;
                var y = -MarginV;
                //Offset the UI  elements half of total lenght of the panel.
                for (var elemIdx = 0; elemIdx < elemCount; ++elemIdx)
                {
                    var r = elems[elemIdx];
                    if (elemIdx == 0)
                    {
                        x += MarginH;
                    }

                    x += 0.5f * r.rect.width;
                    r.anchoredPosition = new Vector2(x, y);
                    x += r.rect.width * 0.5f + ElementSpacing;
                    maxWidth = Mathf.Max(r.rect.width + 2 * MarginH, maxWidth);
                }

                canvasRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth);
                canvasRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, -y + MarginV);
            }
        }

        private void Relayout()
        {
            if (UsePanelCentricRelayout)
            {
                PanelCentricRelayout();
            }
            else
            {
                StackedRelayout();
            }
        }

        private void AddRect(RectTransform r, int targetCanvas)
        {
            if (targetCanvas > m_targetContentPanels.Length)
            {
                Debug.LogError("Attempted to add debug panel to canvas " + targetCanvas + ", but only " +
                            m_targetContentPanels.Length +
                            " panels were provided. Fix in the inspector or pass a lower value for target canvas.");
                return;
            }

            r.transform.SetParent(m_targetContentPanels[targetCanvas], false);
            m_insertedElements[targetCanvas].Add(r);
            if (gameObject.activeInHierarchy)
            {
                Relayout();
            }
        }

        public RectTransform AddButton(string label, OnClick handler = null, int buttonIndex = -1, int targetCanvas = 0,
            bool highResolutionText = false)
        {
            var buttonRT = buttonIndex == -1
                ? Instantiate(m_buttonPrefab).GetComponent<RectTransform>()
                : Instantiate(m_additionalButtonPrefab[buttonIndex]).GetComponent<RectTransform>();
            var button = buttonRT.GetComponentInChildren<Button>();
            if (handler != null)
                button.onClick.AddListener(delegate { handler(); });


            if (highResolutionText)
            {
                ((TextMeshProUGUI)buttonRT.GetComponentsInChildren(typeof(TextMeshProUGUI), true)[0]).text = label;
            }
            else
            {
                ((Text)buttonRT.GetComponentsInChildren(typeof(Text), true)[0]).text = label;
            }

            AddRect(buttonRT, targetCanvas);
            return buttonRT;
        }

        public RectTransform AddLabel(string label, int targetCanvas = 0)
        {
            var rt = Instantiate(m_labelPrefab).GetComponent<RectTransform>();
            rt.GetComponent<Text>().text = label;
            AddRect(rt, targetCanvas);
            return rt;
        }

        public RectTransform AddSlider(string label, float min, float max, OnSlider onValueChanged,
            bool wholeNumbersOnly = false, int targetCanvas = 0)
        {
            var rt = Instantiate(m_sliderPrefab);
            var s = rt.GetComponentInChildren<Slider>();
            s.minValue = min;
            s.maxValue = max;
            s.onValueChanged.AddListener(delegate (float f) { onValueChanged(f); });
            s.wholeNumbers = wholeNumbersOnly;
            AddRect(rt, targetCanvas);
            return rt;
        }

        public RectTransform AddDivider(int targetCanvas = 0)
        {
            var rt = Instantiate(m_dividerPrefab);
            AddRect(rt, targetCanvas);
            return rt;
        }

        public RectTransform AddToggle(string label, OnToggleValueChange onValueChanged, int targetCanvas = 0)
        {
            var rt = Instantiate(m_togglePrefab);
            AddRect(rt, targetCanvas);
            var buttonText = rt.GetComponentInChildren<Text>();
            buttonText.text = label;
            var t = rt.GetComponentInChildren<Toggle>();
            t.onValueChanged.AddListener(delegate { onValueChanged(t); });
            return rt;
        }

        public RectTransform AddToggle(string label, OnToggleValueChange onValueChanged, bool defaultValue,
            int targetCanvas = 0)
        {
            var rt = Instantiate(m_togglePrefab);
            AddRect(rt, targetCanvas);
            var buttonText = rt.GetComponentInChildren<Text>();
            buttonText.text = label;
            var t = rt.GetComponentInChildren<Toggle>();
            t.isOn = defaultValue;
            t.onValueChanged.AddListener(delegate { onValueChanged(t); });
            return rt;
        }

        public RectTransform AddRadio(string label, string group, OnToggleValueChange handler, int targetCanvas = 0)
        {
            var rt = Instantiate(m_radioPrefab);
            AddRect(rt, targetCanvas);
            var buttonText = rt.GetComponentInChildren<Text>();
            buttonText.text = label;
            var tb = rt.GetComponentInChildren<Toggle>();
            group ??= "default";
            ToggleGroup tg = null;
            var isFirst = false;
            if (!m_radioGroups.ContainsKey(group))
            {
                tg = tb.gameObject.AddComponent<ToggleGroup>();
                m_radioGroups[group] = tg;
                isFirst = true;
            }
            else
            {
                tg = m_radioGroups[group];
            }

            tb.group = tg;
            tb.isOn = isFirst;
            tb.onValueChanged.AddListener(delegate { handler(tb); });
            return rt;
        }

        public RectTransform AddTextField(string label, int targetCanvas = 0)
        {
            var textRT = Instantiate(m_textPrefab).GetComponent<RectTransform>();
            var inputField = textRT.GetComponentInChildren<InputField>();
            inputField.text = label;
            AddRect(textRT, targetCanvas);
            return textRT;
        }

        public void ToggleLaserPointer(bool isOn)
        {
            if (m_lp)
            {
                m_lp.enabled = isOn;
            }
        }
    }
}
