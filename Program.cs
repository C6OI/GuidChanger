using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace GuidChanger; 

public static class Program {
    public static void Main() {
        AppDomain.CurrentDomain.UnhandledException += ExceptionHandler;
        
        Console.Write("Enter path to db.json: ");
        string path = Console.ReadLine()!;
        if (!File.Exists(path)) throw new FileNotFoundException();
        
        Deserialize(path);
        Console.ReadLine();
    }

    static void ExceptionHandler(object? sender, UnhandledExceptionEventArgs e) {
        Exception exception = (Exception)e.ExceptionObject;
        
        Console.WriteLine($"{exception.Message} {Environment.NewLine}" +
                          $"Press any key to continue...");
        
        Console.ReadLine();
        Environment.Exit(exception.HResult);
    }
    
    static void Deserialize(string path) {
        Database database = JsonConvert.DeserializeObject<Database>(File.ReadAllText(path));
        
        foreach (Asset asset in database.Bundles.SelectMany(bundle => bundle.Assets)) Console.WriteLine(asset.ObjectName);
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

