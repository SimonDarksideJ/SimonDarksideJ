using Microsoft.Xna.Framework;

namespace EliteDangerousStarMap.Models;

/// <summary>
/// Represents the player's ship in the star map
/// </summary>
public class PlayerShip
{
    /// <summary>
    /// Current star system where the ship is located
    /// </summary>
    public StarSystem? CurrentSystem { get; set; }

    /// <summary>
    /// Target star system for navigation
    /// </summary>
    public StarSystem? TargetSystem { get; set; }

    /// <summary>
    /// World position of the ship (same as current system position)
    /// </summary>
    public Vector3 Position => CurrentSystem?.WorldPosition ?? Vector3.Zero;

    /// <summary>
    /// Calculates distance to target system
    /// </summary>
    public float DistanceToTarget => 
        CurrentSystem != null && TargetSystem != null 
            ? CurrentSystem.DistanceTo(TargetSystem) 
            : 0f;

    /// <summary>
    /// Sets the ship to a random starting system
    /// </summary>
    public void SetRandomStartSystem(List<StarSystem> systems, Random random)
    {
        if (systems.Count > 0)
        {
            var index = random.Next(systems.Count);
            CurrentSystem = systems[index];
        }
    }
}
