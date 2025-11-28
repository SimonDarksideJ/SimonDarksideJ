using Microsoft.Xna.Framework;
using StarSystemData.Models;

namespace StarSystemData.Runtime;

/// <summary>
/// Implementation of IStarSystemDataProvider using content pipeline data
/// </summary>
public class ContentStarSystemProvider : IStarSystemDataProvider
{
    private readonly StarSystemDatabase _database;
    private readonly List<StarSystemInfo> _cachedSystems;
    private readonly Dictionary<string, StarSystemInfo> _byName;
    private readonly Dictionary<int, StarSystemInfo> _byId;
    
    /// <summary>
    /// Creates a new provider with the given database
    /// </summary>
    public ContentStarSystemProvider(StarSystemDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        
        // Convert records to runtime info objects
        _cachedSystems = new List<StarSystemInfo>();
        _byName = new Dictionary<string, StarSystemInfo>(StringComparer.OrdinalIgnoreCase);
        _byId = new Dictionary<int, StarSystemInfo>();
        
        foreach (var record in _database.Systems)
        {
            var info = StarSystemInfo.FromRecord(record);
            
            _cachedSystems.Add(info);
            
            if (!string.IsNullOrEmpty(info.Name) && !_byName.ContainsKey(info.Name))
            {
                _byName[info.Name] = info;
            }
            
            if (!_byId.ContainsKey(info.Id))
            {
                _byId[info.Id] = info;
            }
        }
    }
    
    /// <inheritdoc />
    public int SystemCount => _cachedSystems.Count;
    
    /// <inheritdoc />
    public IReadOnlyList<StarSystemInfo> GetAllSystems() => _cachedSystems;
    
    /// <inheritdoc />
    public StarSystemInfo? GetSystemByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return _byName.TryGetValue(name, out var system) ? system : null;
    }
    
    /// <inheritdoc />
    public StarSystemInfo? GetSystemById(int id)
    {
        return _byId.TryGetValue(id, out var system) ? system : null;
    }
    
    /// <inheritdoc />
    public IReadOnlyList<StarSystemInfo> GetSystemsInSphere(Vector3 center, float radius)
    {
        var radiusSquared = radius * radius;
        return _cachedSystems
            .Where(s => Vector3.DistanceSquared(center, s.Position) <= radiusSquared)
            .ToList();
    }
    
    /// <inheritdoc />
    public IReadOnlyList<StarSystemInfo> GetSystemsInSphere(string systemName, float radius)
    {
        var center = GetSystemByName(systemName);
        if (center == null) return Array.Empty<StarSystemInfo>();
        
        return GetSystemsInSphere(center.Position, radius);
    }
    
    /// <inheritdoc />
    public IReadOnlyList<StarSystemInfo> GetNearestSystems(Vector3 position, int count)
    {
        return _cachedSystems
            .OrderBy(s => Vector3.DistanceSquared(position, s.Position))
            .Take(count)
            .ToList();
    }
    
    /// <inheritdoc />
    public IReadOnlyList<StarSystemInfo> SearchByName(string partialName, int maxResults = 10)
    {
        if (string.IsNullOrEmpty(partialName)) return Array.Empty<StarSystemInfo>();
        
        return _cachedSystems
            .Where(s => s.Name.Contains(partialName, StringComparison.OrdinalIgnoreCase))
            .Take(maxResults)
            .ToList();
    }
    
    /// <inheritdoc />
    public float? GetDistance(string systemName1, string systemName2)
    {
        var system1 = GetSystemByName(systemName1);
        var system2 = GetSystemByName(systemName2);
        
        if (system1 == null || system2 == null) return null;
        
        return system1.DistanceTo(system2);
    }
    
    /// <inheritdoc />
    public IReadOnlyList<StarSystemInfo> GetRoute(string fromSystem, string toSystem)
    {
        var from = GetSystemByName(fromSystem);
        var to = GetSystemByName(toSystem);
        
        if (from == null || to == null) return Array.Empty<StarSystemInfo>();
        
        // For now, just return a direct route (start and end)
        // In the future, this could implement pathfinding through nearby systems
        return new List<StarSystemInfo> { from, to };
    }
}
