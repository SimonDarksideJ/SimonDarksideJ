using Microsoft.Xna.Framework;
using CameraAnimation.Interfaces;

namespace CameraAnimation.Animations;

/// <summary>
/// Base class for camera animations providing common functionality.
/// Derive from this class to create custom animations.
/// </summary>
public abstract class BaseCameraAnimation : ICameraAnimation
{
    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public AnimationState State { get; protected set; } = AnimationState.NotStarted;

    /// <inheritdoc/>
    public bool IsLooping { get; set; } = true;

    /// <inheritdoc/>
    public abstract float Duration { get; }

    /// <inheritdoc/>
    public float Progress { get; protected set; }

    /// <inheritdoc/>
    public float Weight { get; set; } = 1.0f;

    /// <summary>
    /// The elapsed time since animation started.
    /// </summary>
    protected float ElapsedTime { get; set; }

    /// <summary>
    /// The initial camera position when animation started.
    /// </summary>
    protected Vector3 InitialPosition { get; set; }

    /// <summary>
    /// The initial camera yaw when animation started.
    /// </summary>
    protected float InitialYaw { get; set; }

    /// <summary>
    /// The initial camera pitch when animation started.
    /// </summary>
    protected float InitialPitch { get; set; }

    /// <summary>
    /// The initial camera zoom when animation started.
    /// </summary>
    protected float InitialZoom { get; set; }

    /// <summary>
    /// Optional focus point for animations that orbit or look at a target.
    /// </summary>
    protected Vector3? FocusPoint { get; set; }

    /// <inheritdoc/>
    public virtual void Initialize(IAnimatedCamera camera, Vector3? focusPoint = null)
    {
        InitialPosition = camera.Position;
        InitialYaw = camera.Yaw;
        InitialPitch = camera.Pitch;
        InitialZoom = camera.Zoom;
        FocusPoint = focusPoint;
        ElapsedTime = 0;
        Progress = 0;
        State = AnimationState.Running;
    }

    /// <inheritdoc/>
    public CameraTransform Update(GameTime gameTime, IAnimatedCamera camera)
    {
        if (State != AnimationState.Running)
            return CameraTransform.Zero;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        ElapsedTime += deltaTime;

        if (Duration > 0)
        {
            Progress = ElapsedTime / Duration;
            
            if (Progress >= 1.0f)
            {
                if (IsLooping)
                {
                    ElapsedTime %= Duration;
                    Progress = ElapsedTime / Duration;
                }
                else
                {
                    Progress = 1.0f;
                    State = AnimationState.Completed;
                }
            }
        }

        return CalculateTransform(gameTime, camera) * Weight;
    }

    /// <summary>
    /// Calculates the transform delta for this animation frame.
    /// Override this to implement custom animation behavior.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    /// <param name="camera">The camera being animated.</param>
    /// <returns>The transform delta for this frame.</returns>
    protected abstract CameraTransform CalculateTransform(GameTime gameTime, IAnimatedCamera camera);

    /// <inheritdoc/>
    public virtual void Reset()
    {
        ElapsedTime = 0;
        Progress = 0;
        State = AnimationState.NotStarted;
    }

    /// <inheritdoc/>
    public void Pause()
    {
        if (State == AnimationState.Running)
            State = AnimationState.Paused;
    }

    /// <inheritdoc/>
    public void Resume()
    {
        if (State == AnimationState.Paused)
            State = AnimationState.Running;
    }

    /// <summary>
    /// Applies an easing function to the progress value for smoother animations.
    /// </summary>
    /// <param name="t">Progress value from 0 to 1.</param>
    /// <returns>Eased progress value.</returns>
    protected static float EaseInOutSine(float t)
    {
        return -(MathF.Cos(MathF.PI * t) - 1) / 2;
    }

    /// <summary>
    /// Applies a smooth step easing function.
    /// </summary>
    protected static float SmoothStep(float t)
    {
        return t * t * (3 - 2 * t);
    }
}
