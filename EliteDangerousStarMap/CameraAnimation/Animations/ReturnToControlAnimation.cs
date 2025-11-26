using Microsoft.Xna.Framework;
using CameraAnimation.Interfaces;

namespace CameraAnimation.Animations;

/// <summary>
/// A quick, smooth animation that transitions the camera back to a saved position.
/// Used when returning from automated animation mode to user control.
/// </summary>
/// <remarks>
/// Extension Points:
/// - Add different easing options for return animation
/// - Add momentum carry-over from previous animation
/// - Add path smoothing to avoid objects
/// </remarks>
public class ReturnToControlAnimation : BaseCameraAnimation
{
    /// <inheritdoc/>
    public override string Name => "ReturnToControl";

    /// <summary>
    /// The duration of the return animation in seconds.
    /// Should be quick but not jarring.
    /// </summary>
    public float ReturnDuration { get; set; } = 0.5f;

    /// <inheritdoc/>
    public override float Duration => ReturnDuration;

    /// <summary>
    /// The target position to return to.
    /// </summary>
    public Vector3 TargetPosition { get; set; }

    /// <summary>
    /// The target yaw to return to.
    /// </summary>
    public float TargetYaw { get; set; }

    /// <summary>
    /// The target pitch to return to.
    /// </summary>
    public float TargetPitch { get; set; }

    /// <summary>
    /// The target zoom to return to.
    /// </summary>
    public float TargetZoom { get; set; }

    private Vector3 _animStartPosition;
    private float _animStartYaw;
    private float _animStartPitch;
    private float _animStartZoom;

    /// <summary>
    /// Creates a return animation that will transition to the specified camera state.
    /// </summary>
    /// <param name="targetPosition">The position to return to.</param>
    /// <param name="targetYaw">The yaw to return to.</param>
    /// <param name="targetPitch">The pitch to return to.</param>
    /// <param name="targetZoom">The zoom to return to.</param>
    /// <param name="duration">Optional custom duration (default 0.5 seconds).</param>
    public ReturnToControlAnimation(Vector3 targetPosition, float targetYaw, float targetPitch, float targetZoom, float duration = 0.5f)
    {
        TargetPosition = targetPosition;
        TargetYaw = targetYaw;
        TargetPitch = targetPitch;
        TargetZoom = targetZoom;
        ReturnDuration = duration;
        IsLooping = false; // Return animation should only play once
    }

    /// <summary>
    /// Default constructor for when target will be set later.
    /// </summary>
    public ReturnToControlAnimation()
    {
        IsLooping = false;
    }

    /// <inheritdoc/>
    public override void Initialize(IAnimatedCamera camera, Vector3? focusPoint = null)
    {
        base.Initialize(camera, focusPoint);
        
        // Store the starting point (current animated position)
        _animStartPosition = camera.Position;
        _animStartYaw = camera.Yaw;
        _animStartPitch = camera.Pitch;
        _animStartZoom = camera.Zoom;
    }

    /// <inheritdoc/>
    protected override CameraTransform CalculateTransform(GameTime gameTime, IAnimatedCamera camera)
    {
        // Use smooth step for a pleasant deceleration
        float t = SmoothStep(Progress);
        
        // Interpolate from animation start to target
        Vector3 currentTarget = Vector3.Lerp(_animStartPosition, TargetPosition, t);
        Vector3 positionDelta = currentTarget - camera.Position;
        
        // Handle yaw wrapping for shortest rotation path
        float yawDiff = TargetYaw - _animStartYaw;
        while (yawDiff > MathF.PI) yawDiff -= 2 * MathF.PI;
        while (yawDiff < -MathF.PI) yawDiff += 2 * MathF.PI;
        
        float targetYaw = _animStartYaw + yawDiff * t;
        float yawDelta = targetYaw - camera.Yaw;
        
        float targetPitch = MathHelper.Lerp(_animStartPitch, TargetPitch, t);
        float pitchDelta = targetPitch - camera.Pitch;
        
        float targetZoom = MathHelper.Lerp(_animStartZoom, TargetZoom, t);
        float zoomDelta = targetZoom - camera.Zoom;
        
        return new CameraTransform(positionDelta, yawDelta, pitchDelta, zoomDelta);
    }
}
