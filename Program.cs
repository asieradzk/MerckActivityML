using System;
using System.Threading;

namespace MerckActivityML
{
    class Program
    {
        static void Main(string[] args)
        {

            
            //GenerateSheet();


            TrainModel();


            void GenerateSheet()
            {
                //.csv file paths are separated by |
                //Needs at least 2 files, all directories must be valid there is no exception handling
                string dataFiles = @"C:\TrainingSet\ACT1_competition_training.csv|C:\TrainingSet\ACT2_competition_training.csv";


                DataMerger DataMerger = new DataMerger();

                //input directory to save file for instance @"C:\folder\folder"
                DataMerger.DataLoader(dataFiles, ',', @"C:\folder");
            }

            void TrainModel()
            {
                //Hyperparameters are hardcoded inside MLPipeline class

                MLPipeline pipe = new MLPipeline();

                //Specify path to the table containing data you wish to train on
                pipe.GenerateModel(@"C:\Users\blue\Desktop\MerckActivityML\MerckActivityML\test.csv");

            }
        }

        
    }
}
