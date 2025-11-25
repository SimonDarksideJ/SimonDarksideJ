using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace EliteDangerousStarMap.Models;

/// <summary>
/// Represents a star system from the EDSM API
/// </summary>
public class StarSystem
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("coords")]
    public Coordinates? Coords { get; set; }

    [JsonProperty("requirePermit")]
    public bool RequirePermit { get; set; }

    [JsonProperty("permitName")]
    public string? PermitName { get; set; }

    [JsonProperty("coordsLocked")]
    public bool CoordsLocked { get; set; }

    /// <summary>
    /// Gets the 3D world position from the star system coordinates
    /// </summary>
    public Vector3 WorldPosition => Coords != null 
        ? new Vector3((float)Coords.X, (float)Coords.Y, (float)Coords.Z) 
        : Vector3.Zero;

    /// <summary>
    /// Estimated size for rendering (can be enhanced with actual API data)
    /// </summary>
    public float StarSize { get; set; } = 1.0f;

    /// <summary>
    /// Whether this system is currently selected
    /// </summary>
    [JsonIgnore]
    public bool IsSelected { get; set; }

    /// <summary>
    /// Calculates distance to another star system
    /// </summary>
    public float DistanceTo(StarSystem other)
    {
        return Vector3.Distance(WorldPosition, other.WorldPosition);
    }
}

/// <summary>
/// Coordinates from EDSM API
/// </summary>
public class Coordinates
{
    [JsonProperty("x")]
    public double X { get; set; }

    [JsonProperty("y")]
    public double Y { get; set; }

    [JsonProperty("z")]
    public double Z { get; set; }
}
