using Microsoft.ML;
using SketchRoom.AI.Training.Models;

namespace SketchRoom.AI.Training
{
    internal class Program
    {
        static string dataPath = "data.csv";
        static string modelPath = "sketchModel.zip";

        static void Main(string[] args)
        {
            var mlContext = new MLContext(seed: 1);

            var data = mlContext.Data.LoadFromTextFile<SketchData>(
                path: dataPath,
                hasHeader: true,
                separatorChar: ',');

            // Split into train/test
            var split = mlContext.Data.TrainTestSplit(data, testFraction: 0.2);

            var pipeline = mlContext.Transforms.Conversion
                .MapValueToKey("Label")
                .Append(mlContext.Transforms.Concatenate("Features", nameof(SketchData.PixelValues)))
                .Append(mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy("Label", "Features"))
                .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            Console.WriteLine("Training the model...");
            var model = pipeline.Fit(split.TrainSet);

            Console.WriteLine("Evaluating...");
            var predictions = model.Transform(split.TestSet);
            var metrics = mlContext.MulticlassClassification.Evaluate(predictions);

            Console.WriteLine($"MicroAccuracy: {metrics.MicroAccuracy:P2}");
            Console.WriteLine($"MacroAccuracy: {metrics.MacroAccuracy:P2}");

            Console.WriteLine("Saving model...");
            mlContext.Model.Save(model, data.Schema, modelPath);
            Console.WriteLine($"Model saved to {modelPath}");
        }
    }
}
