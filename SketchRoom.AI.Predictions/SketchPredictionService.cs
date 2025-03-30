using Microsoft.ML;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using SketchRoom.AI.Predictions.Models;

namespace SketchRoom.AI.Predictions
{
    public class LetterPredictor
    {
        private readonly InferenceSession _session;

        public LetterPredictor(string modelName)
        {
            var modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", modelName);
            _session = new InferenceSession(modelPath);
        }

        public string Predict(float[] pixels)
        {
            var inputTensor = new DenseTensor<float>(pixels, new[] { 1, 1, 28, 28 });

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input", inputTensor)
            };

            using var results = _session.Run(inputs);

            var output = results.First().AsEnumerable<float>().ToArray();
            int predictedIndex = Array.IndexOf(output, output.Max());

            char predictedLetter = (char)('A' + predictedIndex); // Dacă modelul returnează A–Z
            return predictedLetter.ToString();
        }
    }
}
