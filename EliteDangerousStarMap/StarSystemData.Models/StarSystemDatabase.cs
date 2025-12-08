namespace StarSystemData.Models;

/// <summary>
/// A database of star systems with efficient lookup capabilities.
/// Uses simple float arrays for coordinates to avoid MonoGame dependency.
/// </summary>
public class StarSystemDatabase
{
    private readonly List<StarSystemRecord> _systems;
    private readonly Dictionary<string, StarSystemRecord> _byName;
    private readonly Dictionary<int, StarSystemRecord> _byId;
    
    /// <summary>
    /// Creates a new empty database
    /// </summary>
    public StarSystemDatabase()
    {
        _systems = new List<StarSystemRecord>();
        _byName = new Dictionary<string, StarSystemRecord>(StringComparer.OrdinalIgnoreCase);
        _byId = new Dictionary<int, StarSystemRecord>();
    }
    
    /// <summary>
    /// Creates a database with the given systems
    /// </summary>
    public StarSystemDatabase(IEnumerable<StarSystemRecord> systems)
    {
        _systems = systems.ToList();
        _byName = new Dictionary<string, StarSystemRecord>(StringComparer.OrdinalIgnoreCase);
        _byId = new Dictionary<int, StarSystemRecord>();
        
        foreach (var system in _systems)
        {
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
    /// Gets all systems in the database
    /// </summary>
    public IReadOnlyList<StarSystemRecord> Systems => _systems;
    
    /// <summary>
    /// Gets the number of systems in the database
    /// </summary>
    public int Count => _systems.Count;
    
    /// <summary>
    /// Adds a system to the database
    /// </summary>
    public void Add(StarSystemRecord system)
    {
        _systems.Add(system);
        if (!string.IsNullOrEmpty(system.Name) && !_byName.ContainsKey(system.Name))
        {
            _byName[system.Name] = system;
        }
        if (!_byId.ContainsKey(system.Id))
        {
            _byId[system.Id] = system;
        }
    }
    
    /// <summary>
    /// Gets a system by its name (case-insensitive)
    /// </summary>
    public StarSystemRecord? GetByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return _byName.TryGetValue(name, out var system) ? system : null;
    }
    
    /// <summary>
    /// Gets a system by its ID
    /// </summary>
    public StarSystemRecord? GetById(int id)
    {
        return _byId.TryGetValue(id, out var system) ? system : null;
    }
    
    /// <summary>
    /// Gets all systems within a sphere around a center point
    /// </summary>
    public IReadOnlyList<StarSystemRecord> GetSystemsInSphere(float centerX, float centerY, float centerZ, float radius)
    {
        var radiusSquared = radius * radius;
        var results = new List<StarSystemRecord>();
        
        foreach (var system in _systems)
        {
            var dx = system.X - centerX;
            var dy = system.Y - centerY;
            var dz = system.Z - centerZ;
            var distanceSquared = dx * dx + dy * dy + dz * dz;
            
            if (distanceSquared <= radiusSquared)
            {
                results.Add(system);
            }
        }
        
        return results;
    }
    
    /// <summary>
    /// Gets all systems within a sphere around a named system
    /// </summary>
    public IReadOnlyList<StarSystemRecord> GetSystemsInSphere(string systemName, float radius)
    {
        var center = GetByName(systemName);
        if (center == null) return Array.Empty<StarSystemRecord>();
        
        return GetSystemsInSphere(center.X, center.Y, center.Z, radius);
    }
    
    /// <summary>
    /// Gets the nearest systems to a position
    /// </summary>
    public IReadOnlyList<StarSystemRecord> GetNearestSystems(float x, float y, float z, int count)
    {
        return _systems
            .OrderBy(s => DistanceSquared(x, y, z, s.X, s.Y, s.Z))
            .Take(count)
            .ToList();
    }
    
    /// <summary>
    /// Searches for systems by partial name match
    /// </summary>
    public IReadOnlyList<StarSystemRecord> SearchByName(string partialName, int maxResults = 10)
    {
        if (string.IsNullOrEmpty(partialName)) return Array.Empty<StarSystemRecord>();
        
        return _systems
            .Where(s => s.Name.Contains(partialName, StringComparison.OrdinalIgnoreCase))
            .Take(maxResults)
            .ToList();
    }
    
    /// <summary>
    /// Calculates distance between two systems
    /// </summary>
    public static float Distance(StarSystemRecord a, StarSystemRecord b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        var dz = a.Z - b.Z;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
    
    private static float DistanceSquared(float x1, float y1, float z1, float x2, float y2, float z2)
    {
        var dx = x1 - x2;
        var dy = y1 - y2;
        var dz = z1 - z2;
        return dx * dx + dy * dy + dz * dz;
    }
}
