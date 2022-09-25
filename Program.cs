using System.Diagnostics;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace GuidChanger; 

public static class Program {
    static readonly Stopwatch Time = new();
    static Database _database;
    static int _xd;
    
    public static async Task Main() {
        Time.Start();
        AppDomain.CurrentDomain.UnhandledException += ExceptionHandler;
        
        Console.WriteLine("Enter path to db.json: ");
        string db = Console.ReadLine()!;
        if (!File.Exists(db)) throw new FileNotFoundException();
        
        Console.WriteLine("Enter path to the project: ");
        string project = Console.ReadLine()!;
        if (!Directory.Exists(project)) throw new DirectoryNotFoundException();

        
        Deserialize(db);
        await ReadProject(project);
        
        Time.Stop();
        TimeSpan elapsed = Time.Elapsed;
        
        Console.WriteLine($"Finished in {elapsed.Minutes}:{elapsed.Seconds}; {_xd} guids found");
        Console.ReadLine();
    }

    static void Deserialize(string path) {
        _database = JsonConvert.DeserializeObject<Database>(File.ReadAllText(path));
    }

    static async Task ReadProject(string path) {
        List<string> projectDirectories = Directory
            .GetDirectories(path, "*", SearchOption.AllDirectories).ToList();
        
        List<string> assetBundlesDirectories = Directory
            .GetDirectories(Path.Combine(path, "Asset_Bundles"), "*", SearchOption.AllDirectories).ToList();
        
        List<string> files = projectDirectories
            .SelectMany(d => Directory.GetFiles(d, "*.*", SearchOption.AllDirectories)).ToList();

        await Parallel.ForEachAsync(assetBundlesDirectories.Where(dir => !dir.EndsWith("tanks") && 
                                                              !dir.EndsWith("tank") && 
                                                              !dir.EndsWith("clientresources") && 
                                                              !dir.EndsWith("content") &&
                                                              !dir.EndsWith("weapon") &&
                                                              !dir.EndsWith("hull")), async (directory, dirCancel) => {
            
            Bundle bundle = _database.Bundles.FirstOrDefault(b => directory.EndsWith(b.BundleName));
            if (bundle.Equals(default(Bundle))) return;

            await Parallel.ForEachAsync(Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories), dirCancel,
                async (file, fileCancel) => {
                    
                    Console.WriteLine(file);
                    if (!Path.GetExtension(file).EndsWith("meta")) return;

                    Asset asset =
                        bundle.Assets.FirstOrDefault(a => Path.GetFileName(a.ObjectName) == Path.GetFileNameWithoutExtension(file));
                    if (asset.Equals(default(Asset))) return;
                
                    string[] fileContent = await File.ReadAllLinesAsync(file, fileCancel);

                    Parallel.For(0, fileContent.Length, (i, state) => {
                        string str = fileContent[i];

                        if (!str.Trim().StartsWith("guid")) return;
                    
                        Console.WriteLine($"Old {str}\nNew guid: {asset.Guid}");
                        str = $"guid: {asset.Guid}";
                        fileContent[i] = str;
                        _xd++;
                        state.Break();
                    });

                    // DO NOT TOUCH THIS IN TESTING PHASE
                    //File.WriteAllLines(file, fileContent);
                });
        });
    }
    
    static void ExceptionHandler(object? sender, UnhandledExceptionEventArgs e) {
        Exception exception = (Exception)e.ExceptionObject;
        
        Console.WriteLine($"{exception.Message} {Environment.NewLine}" +
                          "Press any key to continue...");
        
        Console.ReadLine();
        Environment.Exit(exception.HResult);
    }
}

public struct Asset {
    [JsonPropertyName("guid")] public string Guid { get; set; }
    [JsonPropertyName("objectName")] public string ObjectName { get; set; }
    [JsonPropertyName("typeHash")] public int TypeHash { get; set; }
}

public struct Bundle {
    [JsonPropertyName("bundleName")] public string BundleName { get; set; }
    [JsonPropertyName("hash")] public string Hash { get; set; }
    [JsonPropertyName("crc")] public object Crc { get; set; }
    [JsonPropertyName("cacheCrc")] public object CacheCrc { get; set; }
    [JsonPropertyName("size")] public int Size { get; set; }
    [JsonPropertyName("dependenciesNames")] public List<string> DependenciesNames { get; set; }
    [JsonPropertyName("assets")] public List<Asset> Assets { get; set; }
    [JsonPropertyName("modificationHash")] public int ModificationHash { get; set; }
}

public struct Database {
    [JsonPropertyName("bundles")] public List<Bundle> Bundles { get; set; }
    [JsonPropertyName("rootGuids")] public List<string> RootGuids { get; set; }
}