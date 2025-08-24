// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using Meta.XR.Samples;
using Unity.Sentis;
using UnityEngine;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    [MetaCodeSample("PassthroughCameraApiSamples-MultiObjectDetection")]
    public class SentisInferenceRunManager : MonoBehaviour
    {
        [Header("Sentis Model config")]
        [SerializeField] private Vector2Int m_inputSize = new(640, 640);
        [SerializeField] private BackendType m_backend = BackendType.CPU;
        [SerializeField] private ModelAsset m_sentisModel;
        [SerializeField] private int m_layersPerFrame = 25;
        [SerializeField] private TextAsset m_labelsAsset;
        public bool IsModelLoaded { get; private set; } = false;

        [Header("UI display references")]
        [SerializeField] private SentisInferenceUiManager m_uiInference;

        [Header("[Editor Only] Convert to Sentis")]
        public ModelAsset OnnxModel;
        [SerializeField, Range(0, 1)] private float m_iouThreshold = 0.6f;
        [SerializeField, Range(0, 1)] private float m_scoreThreshold = 0.23f;
        [Space(40)]

        private Worker m_engine;
        private IEnumerator m_schedule;
        private bool m_started = false;
        private Tensor<float> m_input;
        private Model m_model;
        private int m_download_state = 0;
        private Tensor<float> m_output;
        private Tensor<int> m_labelIDs;
        private Tensor<float> m_pullOutput;
        private Tensor<int> m_pullLabelIDs;
        private bool m_isWaiting = false;

        #region Unity Functions
        private IEnumerator Start()
        {
            // Wait for the UI to be ready because when Sentis load the model it will block the main thread.
            yield return new WaitForSeconds(0.05f);

            m_uiInference.SetLabels(m_labelsAsset);
            LoadModel();
        }

        private void Update()
        {
            InferenceUpdate();
        }

        private void OnDestroy()
        {
            if (m_schedule != null)
            {
                StopCoroutine(m_schedule);
            }
            m_input?.Dispose();
            m_engine?.Dispose();
        }
        #endregion

        #region Public Functions
        public void RunInference(Texture targetTexture)
        {
            // If the inference is not running prepare the input
            if (!m_started)
            {
                // clean last input
                m_input?.Dispose();
                // check if we have a texture from the camera
                if (!targetTexture)
                {
                    return;
                }
                // Update Capture data
                m_uiInference.SetDetectionCapture(targetTexture);
                // Convert the texture to a Tensor and schedule the inference
                m_input = TextureConverter.ToTensor(targetTexture, m_inputSize.x, m_inputSize.y, 3);
                m_schedule = m_engine.ScheduleIterable(m_input);
                m_download_state = 0;
                m_started = true;
            }
        }

        public bool IsRunning()
        {
            return m_started;
        }
        #endregion

        #region Inference Functions
        private void LoadModel()
        {
            //Load model
            var model = ModelLoader.Load(m_sentisModel);
            Debug.Log($"Sentis model loaded correctly with iouThreshold: {m_iouThreshold} and scoreThreshold: {m_scoreThreshold}");
            //Create engine to run model
            m_engine = new Worker(model, m_backend);
            //Run a inference with an empty input to load the model in the memory and not pause the main thread.
            var input = TextureConverter.ToTensor(new Texture2D(m_inputSize.x, m_inputSize.y), m_inputSize.x, m_inputSize.y, 3);
            m_engine.Schedule(input);
            IsModelLoaded = true;
        }

        private void InferenceUpdate()
        {
            // Run the inference layer by layer to not block the main thread.
            if (m_started)
            {
                try
                {
                    if (m_download_state == 0)
                    {
                        var it = 0;
                        while (m_schedule.MoveNext())
                        {
                            if (++it % m_layersPerFrame == 0)
                                return;
                        }
                        m_download_state = 1;
                    }
                    else
                    {
                        // Get the result once all layers are processed
                        GetInferencesResults();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Sentis error: {e.Message}");
                }
            }
        }

        private void PollRequestOuput()
        {
            // Get the output 0 (coordinates data) from the model output using Sentis pull request.
            m_pullOutput = m_engine.PeekOutput(0) as Tensor<float>;
            if (m_pullOutput.dataOnBackend != null)
            {
                m_pullOutput.ReadbackRequest();
                m_isWaiting = true;
            }
            else
            {
                Debug.LogError("Sentis: No data output m_output");
                m_download_state = 4;
            }
        }

        private void PollRequestLabelIDs()
        {
            // Get the output 1 (labels ID data) from the model output using Sentis pull request.
            m_pullLabelIDs = m_engine.PeekOutput(1) as Tensor<int>;
            if (m_pullLabelIDs.dataOnBackend != null)
            {
                m_pullLabelIDs.ReadbackRequest();
                m_isWaiting = true;
            }
            else
            {
                Debug.LogError("Sentis: No data output m_labelIDs");
                m_download_state = 4;
            }
        }

        private void GetInferencesResults()
        {
            // Get the different outputs in diferent frames to not block the main thread.
            switch (m_download_state)
            {
                case 1:
                    if (!m_isWaiting)
                    {
                        PollRequestOuput();
                    }
                    else
                    {
                        if (m_pullOutput.IsReadbackRequestDone())
                        {
                            m_output = m_pullOutput.ReadbackAndClone();
                            m_isWaiting = false;

                            if (m_output.shape[0] > 0)
                            {
                                Debug.Log("Sentis: m_output ready");
                                m_download_state = 2;
                            }
                            else
                            {
                                Debug.LogError("Sentis: m_output empty");
                                m_download_state = 4;
                            }
                        }
                    }
                    break;
                case 2:
                    if (!m_isWaiting)
                    {
                        PollRequestLabelIDs();
                    }
                    else
                    {
                        if (m_pullLabelIDs.IsReadbackRequestDone())
                        {
                            m_labelIDs = m_pullLabelIDs.ReadbackAndClone();
                            m_isWaiting = false;

                            if (m_labelIDs.shape[0] > 0)
                            {
                                Debug.Log("Sentis: m_labelIDs ready");
                                m_download_state = 3;
                            }
                            else
                            {
                                Debug.LogError("Sentis: m_labelIDs empty");
                                m_download_state = 4;
                            }
                        }
                    }
                    break;
                case 3:
                    m_uiInference.DrawUIBoxes(m_output, m_labelIDs, m_inputSize.x, m_inputSize.y);
                    m_download_state = 5;
                    break;
                case 4:
                    m_uiInference.OnObjectDetectionError();
                    m_download_state = 5;
                    break;
                case 5:
                    m_download_state++;
                    m_started = false;
                    m_output?.Dispose();
                    m_labelIDs?.Dispose();
                    break;
            }
        }
        #endregion
    }
}
