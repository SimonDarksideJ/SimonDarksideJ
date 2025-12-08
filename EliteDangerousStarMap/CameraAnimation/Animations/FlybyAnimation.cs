using Microsoft.Xna.Framework;
using CameraAnimation.Interfaces;

namespace CameraAnimation.Animations;

/// <summary>
/// Moves the camera along a path, passing by points of interest.
/// Creates a cinematic flyby effect.
/// </summary>
/// <remarks>
/// Extension Points:
/// - Add Bezier curve paths for smoother movement
/// - Add multiple waypoint support
/// - Add speed variation based on distance to focus point
/// - Add automatic path generation based on scene analysis
/// </remarks>
public class FlybyAnimation : BaseCameraAnimation
{
    /// <inheritdoc/>
    public override string Name => "Flyby";

    /// <summary>
    /// The duration of the flyby in seconds.
    /// </summary>
    public float FlybyDuration { get; set; } = 15f;

    /// <inheritdoc/>
    public override float Duration => FlybyDuration;

    /// <summary>
    /// The speed of movement in units per second.
    /// </summary>
    public float Speed { get; set; } = 20f;

    /// <summary>
    /// The direction of the flyby (normalized direction vector).
    /// </summary>
    public Vector3 Direction { get; set; } = Vector3.Forward;

    /// <summary>
    /// Whether the camera should look at the focus point during the flyby.
    /// </summary>
    public bool LookAtFocus { get; set; } = true;

    /// <summary>
    /// The minimum distance to pass by the focus point.
    /// </summary>
    public float PassDistance { get; set; } = 30f;

    private Vector3 _startPosition;
    private Vector3 _endPosition;
    private Vector3 _currentDirection;

    /// <inheritdoc/>
    public override void Initialize(IAnimatedCamera camera, Vector3? focusPoint = null)
    {
        base.Initialize(camera, focusPoint ?? Vector3.Zero);

        // Generate a flyby path that passes by the focus point
        var focus = FocusPoint ?? Vector3.Zero;
        
        // Randomize direction somewhat for variety
        var random = new Random();
        float angleOffset = (float)(random.NextDouble() - 0.5) * MathF.PI * 0.5f;
        
        Matrix rotation = Matrix.CreateRotationY(angleOffset);
        _currentDirection = Vector3.Transform(Direction, rotation);
        _currentDirection.Normalize();
        
        // Calculate perpendicular offset to pass by the focus point
        Vector3 perpendicular = Vector3.Cross(_currentDirection, Vector3.Up);
        perpendicular.Normalize();
        
        // Start position: offset from focus in the opposite direction of travel
        float travelDistance = Speed * FlybyDuration;
        _startPosition = focus - _currentDirection * (travelDistance / 2) + perpendicular * PassDistance;
        _endPosition = focus + _currentDirection * (travelDistance / 2) + perpendicular * PassDistance;
    }

    /// <inheritdoc/>
    protected override CameraTransform CalculateTransform(GameTime gameTime, IAnimatedCamera camera)
    {
        // Interpolate position along the flyby path
        float t = EaseInOutSine(Progress);
        Vector3 targetPosition = Vector3.Lerp(_startPosition, _endPosition, t);
        Vector3 positionDelta = targetPosition - camera.Position;
        
        float yawDelta = 0;
        float pitchDelta = 0;
        
        if (LookAtFocus && FocusPoint.HasValue)
        {
            // Calculate rotation to look at focus point
            Vector3 toFocus = FocusPoint.Value - targetPosition;
            
            float targetYaw = MathF.Atan2(toFocus.X, toFocus.Z);
            yawDelta = targetYaw - camera.Yaw;
            
            // Normalize yaw delta
            while (yawDelta > MathF.PI) yawDelta -= 2 * MathF.PI;
            while (yawDelta < -MathF.PI) yawDelta += 2 * MathF.PI;
            
            float horizontalDist = new Vector2(toFocus.X, toFocus.Z).Length();
            float targetPitch = -MathF.Atan2(toFocus.Y, horizontalDist);
            pitchDelta = targetPitch - camera.Pitch;
        }
        else
        {
            // Look in the direction of travel
            float targetYaw = MathF.Atan2(_currentDirection.X, _currentDirection.Z);
            yawDelta = (targetYaw - camera.Yaw) * 0.1f; // Smooth transition
        }
        
        return new CameraTransform(positionDelta, yawDelta, pitchDelta, 0);
    }
}
