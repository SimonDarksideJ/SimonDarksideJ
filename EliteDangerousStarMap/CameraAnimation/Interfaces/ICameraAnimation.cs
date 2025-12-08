using Microsoft.Xna.Framework;

namespace CameraAnimation.Interfaces;

/// <summary>
/// Defines the state of a camera animation.
/// </summary>
public enum AnimationState
{
    /// <summary>Animation has not started yet.</summary>
    NotStarted,
    
    /// <summary>Animation is currently running.</summary>
    Running,
    
    /// <summary>Animation is paused.</summary>
    Paused,
    
    /// <summary>Animation has completed.</summary>
    Completed
}

/// <summary>
/// Interface for camera animations. Implement this to create custom animation behaviors.
/// Animations are designed to be stackable - multiple animations can run simultaneously,
/// each contributing to the final camera transform.
/// </summary>
/// <remarks>
/// Extension Points:
/// - Create custom animations by implementing this interface
/// - Add animation events (OnStart, OnComplete, OnLoop)
/// - Add easing functions for smooth transitions
/// - Add animation chaining for complex sequences
/// </remarks>
public interface ICameraAnimation
{
    /// <summary>
    /// Gets the unique name of this animation type.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the current state of the animation.
    /// </summary>
    AnimationState State { get; }

    /// <summary>
    /// Gets whether this animation should loop indefinitely.
    /// </summary>
    bool IsLooping { get; set; }

    /// <summary>
    /// Gets the total duration of one animation cycle in seconds.
    /// For looping animations, this is the duration of one loop.
    /// </summary>
    float Duration { get; }

    /// <summary>
    /// Gets the current progress of the animation (0.0 to 1.0).
    /// </summary>
    float Progress { get; }

    /// <summary>
    /// Gets or sets the weight of this animation when blending with others (0.0 to 1.0).
    /// Higher weight means this animation has more influence on the final result.
    /// </summary>
    float Weight { get; set; }

    /// <summary>
    /// Initializes the animation with the camera's current state.
    /// Called when the animation starts or when stacking onto an existing animation.
    /// </summary>
    /// <param name="camera">The camera to animate.</param>
    /// <param name="focusPoint">Optional point of interest for the animation to focus on.</param>
    void Initialize(IAnimatedCamera camera, Vector3? focusPoint = null);

    /// <summary>
    /// Updates the animation and applies it to the camera.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    /// <param name="camera">The camera to update.</param>
    /// <returns>
    /// A CameraTransform containing the position and rotation deltas for this frame.
    /// When stacking animations, these deltas are combined.
    /// </returns>
    CameraTransform Update(GameTime gameTime, IAnimatedCamera camera);

    /// <summary>
    /// Resets the animation to its initial state.
    /// </summary>
    void Reset();

    /// <summary>
    /// Pauses the animation.
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes a paused animation.
    /// </summary>
    void Resume();
}

/// <summary>
/// Represents a camera transform delta used for animation blending.
/// </summary>
public struct CameraTransform
{
    /// <summary>Position delta to apply.</summary>
    public Vector3 PositionDelta;
    
    /// <summary>Yaw rotation delta in radians.</summary>
    public float YawDelta;
    
    /// <summary>Pitch rotation delta in radians.</summary>
    public float PitchDelta;
    
    /// <summary>Zoom delta (additive).</summary>
    public float ZoomDelta;

    /// <summary>
    /// Creates a new camera transform.
    /// </summary>
    public CameraTransform(Vector3 positionDelta, float yawDelta, float pitchDelta, float zoomDelta)
    {
        PositionDelta = positionDelta;
        YawDelta = yawDelta;
        PitchDelta = pitchDelta;
        ZoomDelta = zoomDelta;
    }

    /// <summary>
    /// Returns an empty transform with no changes.
    /// </summary>
    public static CameraTransform Zero => new(Vector3.Zero, 0, 0, 0);

    /// <summary>
    /// Combines two transforms by adding their deltas.
    /// </summary>
    public static CameraTransform operator +(CameraTransform a, CameraTransform b)
    {
        return new CameraTransform(
            a.PositionDelta + b.PositionDelta,
            a.YawDelta + b.YawDelta,
            a.PitchDelta + b.PitchDelta,
            a.ZoomDelta + b.ZoomDelta
        );
    }

    /// <summary>
    /// Scales a transform by a weight value.
    /// </summary>
    public static CameraTransform operator *(CameraTransform t, float weight)
    {
        return new CameraTransform(
            t.PositionDelta * weight,
            t.YawDelta * weight,
            t.PitchDelta * weight,
            t.ZoomDelta * weight
        );
    }
}
