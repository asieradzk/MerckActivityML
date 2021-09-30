using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.ML;
using Microsoft.ML.Trainers.FastTree;
using Microsoft.ML.Trainers.LightGbm;
using Microsoft.ML.Trainers;
using System.IO;
namespace MerckActivityML
{
    class MLPipeline
    {
        

        public void GenerateModel(string dataPath)
        {
            var context = new MLContext();



            var trainData = context.Data.LoadFromTextFile<ActivityDataTrain>(dataPath, hasHeader: true, separatorChar: ',');
            /*
            string a = File.ReadAllLines(@"C:\Users\blue\Desktop\MerckActivityML\MerckActivityML\test.csv")[0];
            var b = a.Split(',');
            Console.WriteLine("Got: " + b.Length + " headers...");
            */
            trainData = context.Data.ShuffleRows(trainData);
            var testTrainSplit = context.Data.TrainTestSplit(trainData, testFraction: 0.1);

            var options = new LightGbmRegressionTrainer.Options()
            {
                LabelColumnName = "Label",
                FeatureColumnName = "Features",
                Verbose = false,
                Silent = false,
                LearningRate = 0.01,
                NumberOfLeaves = 10000,
                NumberOfIterations = 100,
                MinimumExampleCountPerLeaf = 1,
                
                UseZeroAsMissingValue = true,
                HandleMissingValue = true,
                Booster = new Microsoft.ML.Trainers.LightGbm.GradientBooster.Options()
                {
                    
                    
                }





            };


            
           

            var pipeline = context.Regression.Trainers.LightGbm(options);


            var model = pipeline.Fit(testTrainSplit.TrainSet);


            var predictions = model.Transform(testTrainSplit.TestSet);

            var metrics = context.Regression.Evaluate(predictions);

            Console.WriteLine("R-squared" + metrics.RSquared);




            

            void Predict(ActivityDataTrain data)
            {
                var predictionEngine = context.Model.CreatePredictionEngine<ActivityDataTrain, ActivityPrediction>(model);
                var prediction = predictionEngine.Predict(data);
                Console.WriteLine("Prediction" + prediction.predictedActivity);
            }




        }


    }
}
