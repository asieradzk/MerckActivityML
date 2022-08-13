using Microsoft.ML.Data;

namespace MerckActivityML
{
    public class ActivityPrediction
    {
        [ColumnName("Score")]
        public float PredictedActivity;
    }
}