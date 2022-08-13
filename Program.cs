using MerckActivityML;

//.csv file paths are separated by |
//Needs at least 2 files, all directories must be valid there is no exception handling
const string dataFiles = @"C:\TrainingSet\ACT1_competition_training.csv|C:\TrainingSet\ACT2_competition_training.csv";

var paths = dataFiles.Split('|');
var dataMerger = await DataMerger.Load(paths, ',', "output");

await dataMerger.MergeTables();

MLPipeline.GenerateModel(@"C:\Users\blue\Desktop\MerckActivityML\MerckActivityML\test.csv");
