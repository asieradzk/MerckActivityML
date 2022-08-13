# Merck-Molecular-Activity-in-ML.NET
An example solution completely in ML.NET and c# for the 2012 Merck Molecular Activity Kaggle Challenge

Run either TrainModel() or GenerateSheet() method from the Program class.
Make sure to run the solution with `dotnet run -c release`.

GenerateSheet() method will combine specified .csv files into one big table containing all common headers where missing values will be set to 0. For me this took 150 gb of ram and about 8 hours on my 10980XE at 150 watts, generated sheet had 3.3 gb.

This sheet can be loaded by ML pipeline with the columns from 2 to n (11083 - 1 for entire set) serving as feature vector. These are already loaded in a stacked version of constant size suitable for regression pipelines.

The method will normalise activity values between 0 and 1 using the [zi=xi−min(x)/max(x)−min(x)] formula. This ensures that ordering and range for training with these arbitrary values remains the same. If you dont want to normalise it this behaviour is hardcoded in DataMerger.cs -> ActivityDataTable class -> pseudoNormalise bool

TrainModel() method will train a lightgbm on your specified table, you can tune hyperparameters, swap trainers or save model in the MLPipeline class

Contact:
contact@exmachinasoft.com
