using Microsoft.Xna.Framework;
using CameraAnimation.Interfaces;

namespace CameraAnimation.Animations;

/// <summary>
/// Orbits the camera around a focus point at a constant distance and speed.
/// The camera always looks at the focus point during the orbit.
/// </summary>
/// <remarks>
/// Extension Points:
/// - Add orbit axis customization (orbit around any axis, not just Y)
/// - Add orbit path variation (elliptical, figure-8)
/// - Add height oscillation during orbit
/// </remarks>
public class OrbitAnimation : BaseCameraAnimation
{
    /// <inheritdoc/>
    public override string Name => "Orbit";

    /// <summary>
    /// The duration of one complete orbit in seconds.
    /// </summary>
    public float OrbitDuration { get; set; } = 20f;

    /// <inheritdoc/>
    public override float Duration => OrbitDuration;

    /// <summary>
    /// The distance from the focus point during orbit.
    /// If null, uses the initial distance from the focus point.
    /// </summary>
    public float? OrbitRadius { get; set; }

    /// <summary>
    /// The height offset above the orbit plane.
    /// </summary>
    public float HeightOffset { get; set; } = 0f;

    /// <summary>
    /// Whether to orbit clockwise (true) or counter-clockwise (false).
    /// </summary>
    public bool Clockwise { get; set; } = true;

    private float _currentAngle;
    private float _actualRadius;

    /// <inheritdoc/>
    public override void Initialize(IAnimatedCamera camera, Vector3? focusPoint = null)
    {
        base.Initialize(camera, focusPoint ?? Vector3.Zero);
        
        // Calculate initial angle based on camera position relative to focus
        var toCamera = camera.Position - (FocusPoint ?? Vector3.Zero);
        _currentAngle = MathF.Atan2(toCamera.X, toCamera.Z);
        
        // Use specified radius or calculate from current distance
        _actualRadius = OrbitRadius ?? new Vector2(toCamera.X, toCamera.Z).Length();
        if (_actualRadius < 10f) _actualRadius = 50f; // Minimum orbit radius
    }

    /// <inheritdoc/>
    protected override CameraTransform CalculateTransform(GameTime gameTime, IAnimatedCamera camera)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        // Calculate rotation speed based on orbit duration
        float angularSpeed = (2 * MathF.PI) / OrbitDuration;
        if (!Clockwise) angularSpeed = -angularSpeed;
        
        _currentAngle += angularSpeed * deltaTime;
        
        // Calculate new position on orbit
        var focus = FocusPoint ?? Vector3.Zero;
        float x = focus.X + _actualRadius * MathF.Sin(_currentAngle);
        float z = focus.Z + _actualRadius * MathF.Cos(_currentAngle);
        float y = focus.Y + HeightOffset;
        
        Vector3 targetPosition = new Vector3(x, y, z);
        Vector3 positionDelta = targetPosition - camera.Position;
        
        // Calculate yaw to look at focus point
        Vector3 toFocus = focus - targetPosition;
        float targetYaw = MathF.Atan2(toFocus.X, toFocus.Z);
        float yawDelta = targetYaw - camera.Yaw;
        
        // Normalize yaw delta to shortest rotation
        while (yawDelta > MathF.PI) yawDelta -= 2 * MathF.PI;
        while (yawDelta < -MathF.PI) yawDelta += 2 * MathF.PI;
        
        // Calculate pitch to look at focus point
        float horizontalDist = new Vector2(toFocus.X, toFocus.Z).Length();
        float targetPitch = -MathF.Atan2(toFocus.Y, horizontalDist);
        float pitchDelta = targetPitch - camera.Pitch;
        
        return new CameraTransform(positionDelta, yawDelta, pitchDelta, 0);
    }
}
