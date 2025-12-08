using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using EliteDangerousStarMap.Models;
using StarSystemData.Models;
using StarSystemData.Runtime;

namespace EliteDangerousStarMap.Services;

/// <summary>
/// Service that provides star system data from local JSON file or API
/// Replaces runtime API calls with pre-loaded data for offline operation
/// </summary>
public class StarSystemDataService : IDisposable
{
    private readonly List<StarSystem> _cachedSystems;
    private readonly Dictionary<string, StarSystem> _byName;
    private readonly Dictionary<int, StarSystem> _byId;
    private bool _disposed;
    
    /// <summary>
    /// Whether data was loaded successfully
    /// </summary>
    public bool IsLoaded { get; private set; }
    
    /// <summary>
    /// Number of systems in the database
    /// </summary>
    public int SystemCount => _cachedSystems.Count;
    
    /// <summary>
    /// Source of the data that was loaded
    /// </summary>
    public string DataSource { get; private set; } = "None";
    
    /// <summary>
    /// Creates a new star system data service
    /// </summary>
    public StarSystemDataService()
    {
        _cachedSystems = new List<StarSystem>();
        _byName = new Dictionary<string, StarSystem>(StringComparer.OrdinalIgnoreCase);
        _byId = new Dictionary<int, StarSystem>();
    }
    
    /// <summary>
    /// Loads star system data, preferring local file then falling back to API
    /// </summary>
    /// <param name="dataSourceMode">DataSourceMode.LocalFirst or DataSourceMode.ApiFirst</param>
    /// <param name="localFilePath">Path to local JSON file</param>
    /// <param name="apiCenterSystem">Center system for API sphere query</param>
    /// <param name="apiRadius">Radius for API sphere query</param>
    public async Task LoadAsync(
        DataSourceMode dataSourceMode = DataSourceMode.LocalFirst,
        string localFilePath = "Content/StarSystems.json",
        string apiCenterSystem = "Sol",
        int apiRadius = 100)
    {
        if (IsLoaded) return;
        
        List<JsonStarSystem>? systems = null;
        
        if (dataSourceMode == DataSourceMode.LocalFirst)
        {
            // Try local file first
            systems = LoadFromFile(localFilePath);
            if (systems != null && systems.Count > 0)
            {
                DataSource = $"Local file: {localFilePath}";
            }
            else
            {
                // Fall back to API
                systems = await FetchFromApiAsync(apiCenterSystem, apiRadius);
                if (systems != null && systems.Count > 0)
                {
                    DataSource = "EDSM API (fallback)";
                }
            }
        }
        else // ApiFirst
        {
            // Try API first
            systems = await FetchFromApiAsync(apiCenterSystem, apiRadius);
            if (systems != null && systems.Count > 0)
            {
                DataSource = "EDSM API";
            }
            else
            {
                // Fall back to local file
                systems = LoadFromFile(localFilePath);
                if (systems != null && systems.Count > 0)
                {
                    DataSource = $"Local file (fallback): {localFilePath}";
                }
            }
        }
        
        if (systems != null)
        {
            PopulateDatabase(systems);
            IsLoaded = _cachedSystems.Count > 0;
        }
    }
    
    /// <summary>
    /// Loads data from a local JSON file
    /// </summary>
    private List<JsonStarSystem>? LoadFromFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                System.Diagnostics.Debug.WriteLine($"Local file not found: {filePath}");
                return null;
            }
            
            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<List<JsonStarSystem>>(json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load from file: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Fetches data from EDSM API
    /// </summary>
    private async Task<List<JsonStarSystem>?> FetchFromApiAsync(string centerSystem, int radius)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var url = $"https://www.edsm.net/api-v1/sphere?systemName={Uri.EscapeDataString(centerSystem)}&radius={radius}&showCoordinates=1";
            
            var response = await client.GetStringAsync(url);
            return JsonConvert.DeserializeObject<List<JsonStarSystem>>(response);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"API fetch failed: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Populates the internal database from JSON data
    /// </summary>
    private void PopulateDatabase(List<JsonStarSystem> systems)
    {
        _cachedSystems.Clear();
        _byName.Clear();
        _byId.Clear();
        
        foreach (var json in systems)
        {
            if (json.Coords == null) continue;
            
            var system = new StarSystem
            {
                Id = json.Id,
                Name = json.Name ?? string.Empty,
                Coords = new Coordinates
                {
                    X = json.Coords.X,
                    Y = json.Coords.Y,
                    Z = json.Coords.Z
                },
                RequirePermit = json.RequirePermit,
                PermitName = json.PermitName,
                CoordsLocked = json.CoordsLocked
            };
            
            _cachedSystems.Add(system);
            
            if (!string.IsNullOrEmpty(system.Name) && !_byName.ContainsKey(system.Name))
            {
                _byName[system.Name] = system;
            }
            
            if (!_byId.ContainsKey(system.Id))
            {
                _byId[system.Id] = system;
            }
        }
    }
    
    /// <summary>
    /// Gets all star systems
    /// </summary>
    public List<StarSystem> GetAllSystems()
    {
        return _cachedSystems.ToList();
    }
    
    /// <summary>
    /// Gets a star system by its exact name (case-insensitive)
    /// </summary>
    public StarSystem? GetSystemByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return _byName.TryGetValue(name, out var system) ? system : null;
    }
    
    /// <summary>
    /// Gets a star system by its EDSM ID
    /// </summary>
    public StarSystem? GetSystemById(int id)
    {
        return _byId.TryGetValue(id, out var system) ? system : null;
    }
    
    /// <summary>
    /// Gets all systems within a sphere around a center point
    /// </summary>
    public List<StarSystem> GetSystemsInSphere(Vector3 center, float radius)
    {
        var radiusSquared = radius * radius;
        return _cachedSystems
            .Where(s => Vector3.DistanceSquared(center, s.WorldPosition) <= radiusSquared)
            .ToList();
    }
    
    /// <summary>
    /// Gets all systems within a sphere around a named system
    /// </summary>
    public List<StarSystem> GetSystemsInSphere(string systemName, float radius)
    {
        var center = GetSystemByName(systemName);
        if (center == null) return new List<StarSystem>();
        
        return GetSystemsInSphere(center.WorldPosition, radius);
    }
    
    /// <summary>
    /// Gets the nearest systems to a position
    /// </summary>
    public List<StarSystem> GetNearestSystems(Vector3 position, int count)
    {
        return _cachedSystems
            .OrderBy(s => Vector3.DistanceSquared(position, s.WorldPosition))
            .Take(count)
            .ToList();
    }
    
    /// <summary>
    /// Searches for systems by partial name match
    /// </summary>
    public List<StarSystem> SearchByName(string partialName, int maxResults = 10)
    {
        if (string.IsNullOrEmpty(partialName)) return new List<StarSystem>();
        
        return _cachedSystems
            .Where(s => s.Name.Contains(partialName, StringComparison.OrdinalIgnoreCase))
            .Take(maxResults)
            .ToList();
    }
    
    /// <summary>
    /// Calculates distance between two systems by name
    /// </summary>
    public float? GetDistance(string systemName1, string systemName2)
    {
        var system1 = GetSystemByName(systemName1);
        var system2 = GetSystemByName(systemName2);
        
        if (system1 == null || system2 == null) return null;
        
        return system1.DistanceTo(system2);
    }
    
    /// <summary>
    /// Gets a route between two systems (simple direct route for now)
    /// </summary>
    public List<StarSystem> GetRoute(string fromSystem, string toSystem)
    {
        var from = GetSystemByName(fromSystem);
        var to = GetSystemByName(toSystem);
        
        if (from == null || to == null) return new List<StarSystem>();
        
        return new List<StarSystem> { from, to };
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _cachedSystems.Clear();
            _byName.Clear();
            _byId.Clear();
            _disposed = true;
        }
    }
}

/// <summary>
/// JSON model for deserializing star system data
/// </summary>
internal class JsonStarSystem
{
    [JsonProperty("name")]
    public string? Name { get; set; }
    
    [JsonProperty("id")]
    public int Id { get; set; }
    
    [JsonProperty("coords")]
    public JsonCoords? Coords { get; set; }
    
    [JsonProperty("requirePermit")]
    public bool RequirePermit { get; set; }
    
    [JsonProperty("permitName")]
    public string? PermitName { get; set; }
    
    [JsonProperty("coordsLocked")]
    public bool CoordsLocked { get; set; }
}

/// <summary>
/// JSON model for coordinates
/// </summary>
internal class JsonCoords
{
    [JsonProperty("x")]
    public double X { get; set; }
    
    [JsonProperty("y")]
    public double Y { get; set; }
    
    [JsonProperty("z")]
    public double Z { get; set; }
}
