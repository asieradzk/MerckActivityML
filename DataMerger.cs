using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;
using NaturalSort.Extension;
using System.Collections.Immutable;

namespace MerckActivityML
{
    public class DataMerger
    {


        public List<ActivityDataTable> loadedTables = new List<ActivityDataTable>();
        public ConcurrentBag<string> preMergedTable = new ConcurrentBag<string>();
        public string mergedTable = "";
        public int totalRows = 0;
        public int processedRows = 0;
        public string savedirectory;

        //Load tables from text files
        public void DataLoader(string paths, char separatorChar, string saveDirectory)
        {
            savedirectory = saveDirectory;

            Console.WriteLine("Table synthesiser activated!");

            Console.WriteLine(@"
  .-')             ('-. _  .-')    ('-.    _ .-') _    .-') _.-. .-')          
 ( OO ).         _(  OO( \( -O )  ( OO ).-( (  OO) )  (  OO) \  ( OO )         
(_)---\_) ,-.-')(,------,------.  / . --. /\     .'_,(_)----.,--. ,--. ,-.-')  
/    _ |  |  |OO)|  .---|   /`. ' | \-.  \ ,`'--..._|       ||  .'   / |  |OO) 
\  :` `.  |  |  \|  |   |  /  | .-'-'  |  ||  |  \  '--.   / |      /, |  |  \ 
 '..`''.) |  |(_(|  '--.|  |_.' |\| |_.'  ||  |   ' (_/   /  |     ' _)|  |(_/ 
.-._)   \,|  |_.'|  .--'|  .  '.' |  .-.  ||  |   / :/   /___|  .   \ ,|  |_.' 
\       (_|  |   |  `---|  |\  \  |  | |  ||  '--'  |        |  |\   (_|  |    
 `-----'  `--'   `------`--' '--' `--' `--'`-------'`--------`--' '--' `--'    
");
            Console.WriteLine("Following files were submited: ");

            


            List<string> loadedData = new List<string>();
            string[] pathed = paths.Split('|');

            foreach (var item in pathed)
            {
                Console.WriteLine(item);
            }

            Console.WriteLine("Loading tables, this should take couple minutes.");

            foreach (var item in pathed)
            {
                loadedData.Add(File.ReadAllText(item));
            }

            foreach (var item in loadedData)
            {
                ActivityDataTable newTable = new ActivityDataTable(item, separatorChar);
                loadedTables.Add(newTable);
            }

            Console.WriteLine("Tables loaded, merging data, this might take several hours");

            MergeTables();

        }

        public void MergeTables()
        {
            
            foreach (var item in loadedTables)
            {
                totalRows += item.rows.Count - 2;
            }


            List<string> possibleHeaders = new List<string>();

            void ProgressUpdater()
            {
            using (var progress = new ProgressBar())
                {
                    while ((double)processedRows / (double)totalRows < 1)
                    {
                        progress.Report((double)processedRows / totalRows);
                        Thread.Sleep(100);
                    }
                }
                

            }

            Thread t = new Thread(ProgressUpdater);
            t.Start();

            //Create list of unique headers
            foreach (var item in loadedTables)
            {
                //cache headers
                for (int i = 0; i < item.headers.Length; i++)
                {
                    if (!possibleHeaders.Contains(item.headers[i]))
                    {
                        possibleHeaders.Add(item.headers[i]);
                    }
                }

            }
            //Sort the headers
            //Take the first 2 headers and keep them, remove duplicates, sort, concacenate
            possibleHeaders = possibleHeaders.Take(2).Concat(possibleHeaders.Skip(2).OrderBy(s => s, StringComparison.OrdinalIgnoreCase.WithNaturalSort())).Distinct().ToList();

            //Create raw string
            AddRow(possibleHeaders);

            


            //Do this for each cached file
            foreach (var bag in loadedTables)
            {
                Parallel.For(0, bag.rows.Count, row =>
               {


                   rowConstructor(bag.rows, row);

               });


            }



            /* string builder runs out of Capacity (Int32.Max) for big data sets
            
             
             StringBuilder sb = new StringBuilder();

            for (int i = 0; i < preMergedTable.Count; i++)
            {
                sb.Append(preMergedTable.ElementAt(preMergedTable.Count - i - 1));
                
            }
            mergedTable = sb.ToString();
            */



            mergedTable += preMergedTable.ElementAt(preMergedTable.Count - 1);

            File.WriteAllText(savedirectory+  @"\test.csv", mergedTable);

            for (int i = 1; i < preMergedTable.Count; i++)
            {
                File.AppendAllText(savedirectory + @"\test.csv", preMergedTable.ElementAt(preMergedTable.Count - i - 1));
            }

            




            Console.WriteLine("You have " + possibleHeaders.Count + " headers");

            //construct rows in multiple threads, this will shuffle them
            //For each row find value or type 0 if missing
            void rowConstructor(List<Dictionary<string, string>> bag, int row)
            {
                List<string> words = new List<string>();
                ConcurrentDictionary<int, string> pseudoList = new ConcurrentDictionary<int, string>();
                Parallel.For(0, possibleHeaders.Count, column =>
                {

                    concurrentRowGenerator(column);


                });

                for (int i = 0; i < pseudoList.Count; i++)
                {
                    words.Add(pseudoList[i]);
                }
                AddRow(words);
                processedRows++;

                

                //Console.WriteLine("Constructed rows: " + processedRows.ToString());

                void concurrentRowGenerator(int column)
                {

                    if (bag.ElementAt(row).ContainsKey(possibleHeaders[column]))
                    {
                        pseudoList.TryAdd(column, bag.ElementAt(row)[possibleHeaders[column]]);
                    }
                    else
                    {
                        pseudoList.TryAdd(column, "0");
                    }
                }


                

            }

            

            void AddRow(List<string> words)
            {
                string line = "";
                for (int i = 0; i < words.Count; i++)
                {
                    line += (words[i] + ",");
                }
                preMergedTable.Add(line + "\n");
            }
        }


    }


    //Unwrapped/cached form of a table from a text file
    public class ActivityDataTable
    {
        public string[] headers;

        public List<Dictionary<string, string>> rows;



        public ActivityDataTable(string rawData, char separatorChar, bool pseudoNormalise=true, int columnToNormalise=2)
        {
            rows = new List<Dictionary<string, string>>();
            ConcurrentBag<Dictionary<string, string>> pRows = new ConcurrentBag<Dictionary<string, string>>();

            using (var reader = new StringReader(rawData))
            {
                headers = reader.ReadLine().Split(separatorChar);
            }

            string[] rawRows = rawData.Split('\n');
            float max = 0;
            float min = 0;
            if (pseudoNormalise)
            {
                List<float> values = new List<float>();
                for (int i = 1; i < rawRows.Length - 1; i++)
                {
                    float v = float.Parse(rawRows[i].Split(',')[columnToNormalise - 1]);
                    

                    values.Add(v);

                }
                max = values.Max();
                min = values.Min();
            }

            Parallel.For(1, rawRows.Count() - 1, i =>
            {
                Dictionary<string, string> a = new Dictionary<string, string>();
                string[] values = rawRows[i].Split(separatorChar);



                for (int ae = 0; ae < headers.Length; ae++)
                {
                    if(pseudoNormalise)
                    {
                        if(ae == columnToNormalise - 1)
                        {
                            a.Add(headers[ae], ((float.Parse(values[ae]) - min)/(max - min)).ToString());
                            continue;
                        }
                    }
                    
                    a.Add(headers[ae], values[ae]);

                }
                pRows.Add(a);

            });

            
            foreach (var item in pRows)
            {
                rows.Add(item);
            }




            /*
            for (int i = 0; i < rows.Count; i++)
            {
                var e = rows[i];
                foreach (var item in e)
                {
                    if (item.Value.Split('_')[0] == "D")
                    {
                        Console.WriteLine(item.Value + "Row: " + i);
                    }
                }
            }
            */
        }
    }
}
    

        


        





    

