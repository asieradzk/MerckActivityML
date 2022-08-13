using System.Text;
using System.Collections.Concurrent;
using NaturalSort.Extension;

namespace MerckActivityML;

public sealed class DataMerger
{
    private readonly ConcurrentStack<string> _preMergedTable;

    private readonly List<ActivityDataTable> _loadedTables;
    private readonly string _outputDirectory;

    private string _mergedTable = string.Empty;
    private int _totalRows;
    private int _processedRows;

    private DataMerger(List<ActivityDataTable> loadedTables, string outputDirectory)
    {
        _preMergedTable = new();

        _loadedTables = loadedTables;
        _outputDirectory = outputDirectory;
    }

    public static async Task<DataMerger> Load(IEnumerable<string> paths, char separator, string outputDirectory)
    {
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
        foreach (var item in paths)
        {
            Console.WriteLine(item);
        }

        Console.WriteLine("Loading tables, this could take a couple minutes.");
        var tables = await Task.WhenAll(paths.Select(path => File.ReadAllTextAsync(path)));

        var activityDataTables = tables
            .Select(table => new ActivityDataTable(table, separator))
            .ToList();

        Console.WriteLine("Tables loaded!");
        return new(activityDataTables, outputDirectory);
    }

    public async Task MergeTables()
    {
        Console.WriteLine("Merging data, this might take several hours..");

        foreach (var item in _loadedTables)
        {
            _totalRows += item.Rows.Count - 2;
        }

        // Progress updater
        ThreadPool.QueueUserWorkItem(async _ =>
        {
            using var progress = new ProgressBar();

            while ((double)_processedRows / _totalRows < 1)
            {
                progress.Report((double)_processedRows / _totalRows);
                await Task.Delay(100);
            }
        });

        // Create list of unique headers
        var possibleHeaders = _loadedTables
            .SelectMany(table => table.Headers)
            .Distinct()
            .ToList();

        // Sort the headers
        // Take the first 2 headers and keep them, remove duplicates, sort, concacenate
        possibleHeaders = possibleHeaders
            .Take(2)
            .Concat(possibleHeaders
                .Skip(2)
                .OrderBy(s => s, StringComparison.OrdinalIgnoreCase.WithNaturalSort()))
            .ToList();

        // Create raw string
        AddRow(possibleHeaders);

        // Do this for each cached file
        foreach (var bag in _loadedTables)
        {
            Parallel.ForEach(bag.Rows, row => RowConstructor(row, possibleHeaders));
        }

        /* string builder runs out of Capacity (Int32.Max) for big data sets
        
         
         StringBuilder sb = new StringBuilder();

        for (int i = 0; i < preMergedTable.Count; i++)
        {
            sb.Append(preMergedTable.ElementAt(preMergedTable.Count - i - 1));
            
        }
        mergedTable = sb.ToString();
        */

        _mergedTable += _preMergedTable.ElementAt(_preMergedTable.Count - 1);

        File.WriteAllText(_outputDirectory + @"\test.csv", _mergedTable);

        var path = _outputDirectory + @"\test.csv";

        foreach (var element in _preMergedTable.Skip(1))
        {
            await File.AppendAllTextAsync(path, element);
        }

        Console.WriteLine("You have " + possibleHeaders.Count + " headers");
    }

    //construct rows in multiple threads, this will shuffle them
    //For each row find value or type 0 if missing
    private void RowConstructor(Dictionary<string, string> bag, List<string> possibleHeaders)
    {
        var words = possibleHeaders.ConvertAll(header => bag.GetValueOrDefault(header) ?? "0");

        AddRow(words);
        _processedRows++;

        //Console.WriteLine("Constructed rows: " + processedRows.ToString());
    }

    private void AddRow<T>(T words) where T : IEnumerable<string>
    {
        var builder = new StringBuilder();

        foreach (var word in words)
        {
            builder.Append(word).Append(',');
        }

        builder.AppendLine();

        _preMergedTable.Push(builder.ToString());
    }
}

// Unwrapped/cached form of a table from a text file
public class ActivityDataTable
{
    public readonly string[] Headers;

    public readonly List<Dictionary<string, string>> Rows;

    public ActivityDataTable(string rawData, char separatorChar, bool pseudoNormalise=true, int columnToNormalise = 2)
    {
        Rows = new List<Dictionary<string, string>>();
        var pRows = new ConcurrentBag<Dictionary<string, string>>();

        using (var reader = new StringReader(rawData))
        {
            Headers = reader.ReadLine()?.Split(separatorChar) ?? throw new NullReferenceException();
        }

        string[] rawRows = rawData.Split('\n');
        float max = 0;
        float min = 0;
        if (pseudoNormalise)
        {
            var values = new List<float>();
            for (int i = 1; i < rawRows.Length - 1; i++)
            {
                float v = float.Parse(rawRows[i].Split(',')[columnToNormalise - 1]);

                values.Add(v);
            }
            max = values.Max();
            min = values.Min();
        }

        Parallel.For(1, rawRows.Length - 1, i =>
        {
            var a = new Dictionary<string, string>();
            var values = rawRows[i].Split(separatorChar);

            for (int ae = 0; ae < Headers.Length; ae++)
            {
                if (pseudoNormalise && ae == columnToNormalise - 1)
                {
                    a.Add(Headers[ae], ((float.Parse(values[ae]) - min) / (max - min)).ToString());
                    continue;
                }

                a.Add(Headers[ae], values[ae]);
            }
            pRows.Add(a);
        });

        foreach (var item in pRows)
        {
            Rows.Add(item);
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
