using Microsoft.ML.Data;

namespace MerckActivityML
{
    public class ActivityDataTrain
    {
        [LoadColumn(0)]
        public string MoleculeID = string.Empty;

        [LoadColumn(1), ColumnName("Label")]
        public float MoleculeActivity;

        [LoadColumn(2, 11082)]
        public float[] Features = Array.Empty<float>();
    }
}
