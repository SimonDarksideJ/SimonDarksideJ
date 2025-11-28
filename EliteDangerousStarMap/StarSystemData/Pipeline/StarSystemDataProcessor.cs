using Microsoft.Xna.Framework.Content.Pipeline;
using Newtonsoft.Json;
using StarSystemData.Models;
using System.ComponentModel;

namespace StarSystemData.Pipeline;

/// <summary>
/// Content importer for star system data files (JSON format)
/// </summary>
[ContentImporter(".json", DisplayName = "Star System Data Importer", DefaultProcessor = "StarSystemDataProcessor")]
public class StarSystemDataImporter : ContentImporter<StarSystemImportData>
{
    public override StarSystemImportData Import(string filename, ContentImporterContext context)
    {
        context.Logger.LogMessage($"Importing star system data from: {filename}");
        
        var json = File.ReadAllText(filename);
        
        return new StarSystemImportData
        {
            JsonContent = json,
            SourceFilePath = filename
        };
    }
}

/// <summary>
/// Intermediate data from import stage
/// </summary>
public class StarSystemImportData
{
    public string JsonContent { get; set; } = string.Empty;
    public string SourceFilePath { get; set; } = string.Empty;
}

/// <summary>
/// Content processor for star system data
/// </summary>
[ContentProcessor(DisplayName = "Star System Data Processor")]
public class StarSystemDataProcessor : ContentProcessor<StarSystemImportData, StarSystemDatabase>
{
    /// <summary>
    /// Data source mode - ApiFirst or LocalFirst
    /// </summary>
    [DisplayName("Data Source Mode")]
    [Description("ApiFirst: Try API first, fall back to local. LocalFirst: Use local file only.")]
    [DefaultValue(DataSourceMode.ApiFirst)]
    public DataSourceMode DataSource { get; set; } = DataSourceMode.ApiFirst;
    
    /// <summary>
    /// Path to local JSON file for fallback
    /// </summary>
    [DisplayName("Local File Path")]
    [Description("Path to local JSON file used as fallback or primary source")]
    [DefaultValue("StarSystems.json")]
    public string LocalFilePath { get; set; } = "StarSystems.json";
    
    /// <summary>
    /// Center system for API sphere query
    /// </summary>
    [DisplayName("API Center System")]
    [Description("Center system name for API sphere query")]
    [DefaultValue("Sol")]
    public string ApiCenterSystem { get; set; } = "Sol";
    
    /// <summary>
    /// Radius in light years for API sphere query
    /// </summary>
    [DisplayName("API Radius")]
    [Description("Radius in light years for API sphere query")]
    [DefaultValue(100)]
    public int ApiRadius { get; set; } = 100;
    
    public override StarSystemDatabase Process(StarSystemImportData input, ContentProcessorContext context)
    {
        context.Logger.LogMessage($"Processing star system data with mode: {DataSource}");
        
        List<EdsmSystemJson>? systems = null;
        
        if (DataSource == DataSourceMode.LocalFirst)
        {
            // Use local file only
            context.Logger.LogMessage("Using local file as data source");
            systems = LoadFromJson(input.JsonContent, context);
        }
        else // ApiFirst
        {
            // Try API first
            context.Logger.LogMessage("Attempting to fetch data from EDSM API...");
            systems = FetchFromApi(context).GetAwaiter().GetResult();
            
            if (systems == null || systems.Count == 0)
            {
                context.Logger.LogWarning(null, null, "API fetch failed or returned no data, falling back to local file");
                systems = LoadFromJson(input.JsonContent, context);
            }
        }
        
        if (systems == null || systems.Count == 0)
        {
            context.Logger.LogWarning(null, null, "No star systems loaded, creating sample data");
            systems = CreateSampleData();
        }
        
        // Convert to database
        var database = new StarSystemDatabase();
        foreach (var system in systems)
        {
            if (system.Coords == null) continue;
            
            database.Add(new StarSystemRecord
            {
                Id = system.Id,
                Name = system.Name ?? string.Empty,
                X = (float)system.Coords.X,
                Y = (float)system.Coords.Y,
                Z = (float)system.Coords.Z,
                RequirePermit = system.RequirePermit,
                PermitName = system.PermitName,
                CoordsLocked = system.CoordsLocked
            });
        }
        
        context.Logger.LogMessage($"Processed {database.Count} star systems");
        
        return database;
    }
    
    private List<EdsmSystemJson>? LoadFromJson(string json, ContentProcessorContext context)
    {
        try
        {
            return JsonConvert.DeserializeObject<List<EdsmSystemJson>>(json);
        }
        catch (Exception ex)
        {
            context.Logger.LogWarning(null, null, $"Failed to parse JSON: {ex.Message}");
            return null;
        }
    }
    
    private async Task<List<EdsmSystemJson>?> FetchFromApi(ContentProcessorContext context)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var url = $"https://www.edsm.net/api-v1/sphere?systemName={Uri.EscapeDataString(ApiCenterSystem)}&radius={ApiRadius}&showCoordinates=1";
            
            context.Logger.LogMessage($"Fetching from: {url}");
            var response = await client.GetStringAsync(url);
            
            return JsonConvert.DeserializeObject<List<EdsmSystemJson>>(response);
        }
        catch (Exception ex)
        {
            context.Logger.LogWarning(null, null, $"API fetch failed: {ex.Message}");
            return null;
        }
    }
    
    private List<EdsmSystemJson> CreateSampleData()
    {
        return new List<EdsmSystemJson>
        {
            new EdsmSystemJson { Name = "Sol", Id = 27, Coords = new EdsmCoordsJson { X = 0, Y = 0, Z = 0 }, RequirePermit = true, PermitName = "Sol" },
            new EdsmSystemJson { Name = "Alpha Centauri", Id = 28, Coords = new EdsmCoordsJson { X = 3.03, Y = -0.08, Z = 3.15 } },
            new EdsmSystemJson { Name = "Barnard's Star", Id = 29, Coords = new EdsmCoordsJson { X = -3.03, Y = 1.37, Z = 4.94 } },
            new EdsmSystemJson { Name = "Wolf 359", Id = 30, Coords = new EdsmCoordsJson { X = 3.86, Y = 6.47, Z = 1.93 } },
            new EdsmSystemJson { Name = "Lalande 21185", Id = 31, Coords = new EdsmCoordsJson { X = -6.54, Y = 1.65, Z = 4.93 } },
            new EdsmSystemJson { Name = "Sirius", Id = 32, Coords = new EdsmCoordsJson { X = 6.22, Y = -1.01, Z = -1.79 } },
            new EdsmSystemJson { Name = "Luyten 726-8", Id = 33, Coords = new EdsmCoordsJson { X = 5.82, Y = -3.00, Z = -0.67 } },
            new EdsmSystemJson { Name = "Ross 154", Id = 34, Coords = new EdsmCoordsJson { X = 1.93, Y = -0.94, Z = -9.46 } },
            new EdsmSystemJson { Name = "Ross 248", Id = 35, Coords = new EdsmCoordsJson { X = 7.45, Y = -2.42, Z = 7.19 } },
            new EdsmSystemJson { Name = "Epsilon Eridani", Id = 36, Coords = new EdsmCoordsJson { X = 1.93, Y = -7.76, Z = -6.92 } },
        };
    }
}

/// <summary>
/// JSON model for EDSM API response
/// </summary>
internal class EdsmSystemJson
{
    [JsonProperty("name")]
    public string? Name { get; set; }
    
    [JsonProperty("id")]
    public int Id { get; set; }
    
    [JsonProperty("coords")]
    public EdsmCoordsJson? Coords { get; set; }
    
    [JsonProperty("requirePermit")]
    public bool RequirePermit { get; set; }
    
    [JsonProperty("permitName")]
    public string? PermitName { get; set; }
    
    [JsonProperty("coordsLocked")]
    public bool CoordsLocked { get; set; }
}

/// <summary>
/// JSON model for EDSM coordinates
/// </summary>
internal class EdsmCoordsJson
{
    [JsonProperty("x")]
    public double X { get; set; }
    
    [JsonProperty("y")]
    public double Y { get; set; }
    
    [JsonProperty("z")]
    public double Z { get; set; }
}
