using Microsoft.ML;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using SketchRoom.AI.Predictions.Models;

namespace SketchRoom.AI.Predictions
{
    public class PredictionResult
    {
        public string Label { get; set; }
        public float Confidence { get; set; } // valoare între 0 și 1
    }
    public class LetterPredictor : IDisposable
    {
        private readonly InferenceSession _session;

        private readonly string[] _labels = new[]
        {
            "0", "1", "2", "3", "4", "5", "6", "7", "8", "9"
        };

        public LetterPredictor(string modelFileName)
        {
            var modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", modelFileName);
            _session = new InferenceSession(modelPath);
        }

        public PredictionResult Predict(float[] pixels)
        {
            if (pixels.Length != 28 * 28)
                throw new ArgumentException("Input must be 28x28 (784 floats).");

            var inputTensor = new DenseTensor<float>(pixels, new[] { 1, 28, 28, 1 });
            var inputName = _session.InputMetadata.Keys.First();

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(inputName, inputTensor)
            };

            using var results = _session.Run(inputs);
            var output = results.First().AsEnumerable<float>().ToArray();

            int predictedIndex = Array.IndexOf(output, output.Max());
            float confidence = output[predictedIndex]; // valoare între 0.0 - 1.0

            return predictedIndex >= 0 && predictedIndex < _labels.Length
                ? new PredictionResult
                {
                    Label = _labels[predictedIndex],
                    Confidence = confidence
                }
                : new PredictionResult { Label = "?", Confidence = 0 };
        }

        public void Dispose()
        {
            _session.Dispose();
        }
    }
}
