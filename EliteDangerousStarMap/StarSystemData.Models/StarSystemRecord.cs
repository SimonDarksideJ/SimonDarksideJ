namespace StarSystemData.Models;

/// <summary>
/// A binary-serializable star system record for content pipeline
/// </summary>
public class StarSystemRecord
{
    /// <summary>
    /// EDSM system ID
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// System name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// X coordinate in light years from Sol
    /// </summary>
    public float X { get; set; }
    
    /// <summary>
    /// Y coordinate in light years from Sol
    /// </summary>
    public float Y { get; set; }
    
    /// <summary>
    /// Z coordinate in light years from Sol
    /// </summary>
    public float Z { get; set; }
    
    /// <summary>
    /// Whether the system requires a permit to enter
    /// </summary>
    public bool RequirePermit { get; set; }
    
    /// <summary>
    /// Name of the required permit, if any
    /// </summary>
    public string? PermitName { get; set; }
    
    /// <summary>
    /// Whether the coordinates are locked/official
    /// </summary>
    public bool CoordsLocked { get; set; }
}
