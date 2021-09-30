using Microsoft.ML.Data;


namespace MerckActivityML
{
    public class ActivityDataTrain
    {
        [LoadColumn(0)]
        public string moleculeID;

        [LoadColumn(1), ColumnName("Label")]
        public float moleculeActivity;



        [LoadColumn(2, 11082)]
        public float[] Features;


    }
}