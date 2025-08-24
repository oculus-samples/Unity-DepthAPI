// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using Unity.Sentis;
using UnityEditor;
using UnityEngine;
using FF = Unity.Sentis.Functional;

namespace PassthroughCameraSamples.MultiObjectDetection.Editor
{
    [MetaCodeSample("PassthroughCameraApiSamples-MultiObjectDetection")]
    [CustomEditor(typeof(SentisInferenceRunManager))]
    public class SentisModelEditorConverter : UnityEditor.Editor
    {
        private const string FILEPATH = "Assets/PassthroughCameraApiSamples/MultiObjectDetection/SentisInference/Model/yolov9sentis.sentis";
        private SentisInferenceRunManager m_targetClass;
        private float m_iouThreshold;
        private float m_scoreThreshold;

        public void OnEnable()
        {
            m_targetClass = (SentisInferenceRunManager)target;
            m_iouThreshold = serializedObject.FindProperty("m_iouThreshold").floatValue;
            m_scoreThreshold = serializedObject.FindProperty("m_scoreThreshold").floatValue;
        }

        public override void OnInspectorGUI()
        {
            _ = DrawDefaultInspector();

            if (GUILayout.Button("Generate Yolov9 Sentis model with Non-Max-Supression layer"))
            {
                OnEnable(); // Get the latest values from the serialized object
                ConvertModel(); // convert the ONNX model to sentis
            }
        }

        private void ConvertModel()
        {
            //Load model
            var model = ModelLoader.Load(m_targetClass.OnnxModel);

            //Here we transform the output of the model by feeding it through a Non-Max-Suppression layer.
            var graph = new FunctionalGraph();
            var input = graph.AddInput(model, 0);

            var centersToCornersData = new[]
            {
                        1,      0,      1,      0,
                        0,      1,      0,      1,
                        -0.5f,  0,      0.5f,   0,
                        0,      -0.5f,  0,      0.5f
            };
            var centersToCorners = FF.Constant(new TensorShape(4, 4), centersToCornersData);
            var modelOutput = FF.Forward(model, input)[0];  //shape(1,N,85)
            // Following for yolo model. in (1, 84, N) out put shape
            var boxCoords = modelOutput[0, ..4, ..].Transpose(0, 1);
            var allScores = modelOutput[0, 4.., ..].Transpose(0, 1);
            var scores = FF.ReduceMax(allScores, 1);    //shape=(N)
            var classIDs = FF.ArgMax(allScores, 1); //shape=(N)
            var boxCorners = FF.MatMul(boxCoords, centersToCorners);    //shape=(N,4)
            var indices = FF.NMS(boxCorners, scores, m_iouThreshold, m_scoreThreshold); //shape=(N)
            var indices2 = indices.Unsqueeze(-1).BroadcastTo(new[] { 4 });  //shape=(N,4)
            var labelIDs = FF.Gather(classIDs, 0, indices); //shape=(N)
            var coords = FF.Gather(boxCoords, 0, indices2); //shape=(N,4)

            var modelFinal = graph.Compile(coords, labelIDs);

            //Export the model to Sentis format
            ModelQuantizer.QuantizeWeights(QuantizationType.Uint8, ref modelFinal);
            ModelWriter.Save(FILEPATH, modelFinal);

            // refresh assets
            AssetDatabase.Refresh();
        }
    }
}
