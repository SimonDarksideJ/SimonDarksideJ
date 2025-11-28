using Microsoft.Xna.Framework;
using StarSystemData.Models;

namespace StarSystemData.Runtime;

/// <summary>
/// Interface for querying star system data at runtime
/// </summary>
public interface IStarSystemDataProvider
{
    /// <summary>
    /// Gets the total number of systems in the database
    /// </summary>
    int SystemCount { get; }
    
    /// <summary>
    /// Gets all star systems
    /// </summary>
    IReadOnlyList<StarSystemInfo> GetAllSystems();
    
    /// <summary>
    /// Gets a star system by its exact name (case-insensitive)
    /// </summary>
    StarSystemInfo? GetSystemByName(string name);
    
    /// <summary>
    /// Gets a star system by its EDSM ID
    /// </summary>
    StarSystemInfo? GetSystemById(int id);
    
    /// <summary>
    /// Gets all systems within a sphere around a center point
    /// </summary>
    /// <param name="center">Center position in 3D space</param>
    /// <param name="radius">Radius in light years</param>
    IReadOnlyList<StarSystemInfo> GetSystemsInSphere(Vector3 center, float radius);
    
    /// <summary>
    /// Gets all systems within a sphere around a named system
    /// </summary>
    /// <param name="systemName">Name of the center system</param>
    /// <param name="radius">Radius in light years</param>
    IReadOnlyList<StarSystemInfo> GetSystemsInSphere(string systemName, float radius);
    
    /// <summary>
    /// Gets the nearest systems to a position
    /// </summary>
    /// <param name="position">Reference position</param>
    /// <param name="count">Maximum number of systems to return</param>
    IReadOnlyList<StarSystemInfo> GetNearestSystems(Vector3 position, int count);
    
    /// <summary>
    /// Searches for systems by partial name match
    /// </summary>
    /// <param name="partialName">Partial name to search for</param>
    /// <param name="maxResults">Maximum number of results</param>
    IReadOnlyList<StarSystemInfo> SearchByName(string partialName, int maxResults = 10);
    
    /// <summary>
    /// Calculates distance between two systems by name
    /// </summary>
    float? GetDistance(string systemName1, string systemName2);
    
    /// <summary>
    /// Gets a route between two systems (simple line for now)
    /// </summary>
    IReadOnlyList<StarSystemInfo> GetRoute(string fromSystem, string toSystem);
}

/// <summary>
/// Read-only star system information for runtime use
/// </summary>
public class StarSystemInfo
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Vector3 Position { get; init; }
    public bool RequirePermit { get; init; }
    public string? PermitName { get; init; }
    public bool CoordsLocked { get; init; }
    
    /// <summary>
    /// Creates StarSystemInfo from a StarSystemRecord
    /// </summary>
    public static StarSystemInfo FromRecord(StarSystemRecord record)
    {
        return new StarSystemInfo
        {
            Id = record.Id,
            Name = record.Name,
            Position = new Vector3(record.X, record.Y, record.Z),
            RequirePermit = record.RequirePermit,
            PermitName = record.PermitName,
            CoordsLocked = record.CoordsLocked
        };
    }
    
    /// <summary>
    /// Calculates distance to another system
    /// </summary>
    public float DistanceTo(StarSystemInfo other)
    {
        return Vector3.Distance(Position, other.Position);
    }
    
    /// <summary>
    /// Calculates distance to a position
    /// </summary>
    public float DistanceTo(Vector3 position)
    {
        return Vector3.Distance(Position, position);
    }
}
