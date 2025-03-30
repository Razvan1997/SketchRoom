using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SketchRoom.AI.Training.Models
{
    public class SketchData
    {
        [LoadColumn(0)]
        public string Label { get; set; } = string.Empty;

        [LoadColumn(1, 784)]
        [VectorType(784)]
        public float[] PixelValues { get; set; } = new float[784];
    }

    public class SketchPrediction
    {
        [ColumnName("PredictedLabel")]
        public string PredictedLabel { get; set; } = string.Empty;
    }
}
