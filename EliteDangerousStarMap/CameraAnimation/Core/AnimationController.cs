using Microsoft.Xna.Framework;
using CameraAnimation.Interfaces;
using CameraAnimation.Animations;

namespace CameraAnimation.Core;

/// <summary>
/// Represents the current mode of the animation controller.
/// </summary>
public enum AnimationMode
{
    /// <summary>User is in control of the camera.</summary>
    UserControl,
    
    /// <summary>Waiting for idle timeout before starting animations.</summary>
    WaitingForIdle,
    
    /// <summary>Running automated "sleep" animations around focus point.</summary>
    Animating,
    
    /// <summary>Transitioning back to user control.</summary>
    ReturningToControl,
    
    /// <summary>Following a moving target (e.g., ship in flight).</summary>
    FollowingTarget,
    
    /// <summary>Transitioning back to follow mode after user input.</summary>
    ReturningToFollow
}

/// <summary>
/// Manages camera animations with idle detection and smooth transitions.
/// Supports stacking multiple animations that blend together.
/// </summary>
/// <remarks>
/// Extension Points:
/// - Add animation presets/profiles for quick configuration
/// - Add animation triggers based on game events
/// - Add animation recording and playback
/// - Add scripted animation sequences
/// - Add multi-camera coordination
/// </remarks>
public class AnimationController
{
    private readonly List<ICameraAnimation> _activeAnimations = new();
    private readonly List<ICameraAnimation> _availableAnimations = new();
    private IAnimatedCamera? _targetCamera;
    
    // Idle detection
    private Vector3 _lastPosition;
    private float _lastYaw;
    private float _lastPitch;
    private float _lastZoom;
    private float _idleTime;
    
    // Saved state for return animation
    private Vector3 _savedPosition;
    private float _savedYaw;
    private float _savedPitch;
    private float _savedZoom;
    
    // Return animation
    private ReturnToControlAnimation? _returnAnimation;
    
    // Follow mode
    private ShipFollowAnimation? _followAnimation;
    private bool _isFollowModeActive;

    /// <summary>
    /// Gets the current animation mode.
    /// </summary>
    public AnimationMode Mode { get; private set; } = AnimationMode.UserControl;

    /// <summary>
    /// Gets or sets the idle timeout in seconds before animations start.
    /// </summary>
    public float IdleTimeout { get; set; } = 30f;

    /// <summary>
    /// Gets or sets the threshold for detecting camera movement.
    /// </summary>
    public float MovementThreshold { get; set; } = 0.1f;

    /// <summary>
    /// Gets or sets whether automatic animation is enabled.
    /// </summary>
    public bool AutoAnimationEnabled { get; set; } = true;

    /// <summary>
    /// Gets the current idle time in seconds.
    /// </summary>
    public float CurrentIdleTime => _idleTime;

    /// <summary>
    /// Gets or sets an optional focus point for animations.
    /// </summary>
    public Vector3? FocusPoint { get; set; }
    
    /// <summary>
    /// Gets whether follow mode is currently active (even if user has temporary control).
    /// </summary>
    public bool IsFollowModeActive => _isFollowModeActive;
    
    /// <summary>
    /// Gets the current ship follow animation if active.
    /// </summary>
    public ShipFollowAnimation? FollowAnimation => _followAnimation;

    /// <summary>
    /// Event fired when animation mode changes.
    /// </summary>
    public event Action<AnimationMode>? ModeChanged;

    /// <summary>
    /// Gets the list of available animations that can be activated.
    /// </summary>
    public IReadOnlyList<ICameraAnimation> AvailableAnimations => _availableAnimations;

    /// <summary>
    /// Gets the list of currently active animations.
    /// </summary>
    public IReadOnlyList<ICameraAnimation> ActiveAnimations => _activeAnimations;

    /// <summary>
    /// Registers the target camera that will be animated.
    /// </summary>
    /// <param name="camera">The camera to animate.</param>
    public void SetTargetCamera(IAnimatedCamera camera)
    {
        _targetCamera = camera;
        _lastPosition = camera.Position;
        _lastYaw = camera.Yaw;
        _lastPitch = camera.Pitch;
        _lastZoom = camera.Zoom;
    }

    /// <summary>
    /// Registers an animation type that can be used.
    /// </summary>
    /// <param name="animation">The animation to register.</param>
    public void RegisterAnimation(ICameraAnimation animation)
    {
        _availableAnimations.Add(animation);
    }

    /// <summary>
    /// Adds an animation to the active stack.
    /// </summary>
    /// <param name="animation">The animation to activate.</param>
    public void ActivateAnimation(ICameraAnimation animation)
    {
        if (_targetCamera == null) return;
        
        animation.Initialize(_targetCamera, FocusPoint);
        _activeAnimations.Add(animation);
    }

    /// <summary>
    /// Removes an animation from the active stack.
    /// </summary>
    /// <param name="animation">The animation to deactivate.</param>
    public void DeactivateAnimation(ICameraAnimation animation)
    {
        animation.Reset();
        _activeAnimations.Remove(animation);
    }

    /// <summary>
    /// Clears all active animations.
    /// </summary>
    public void ClearAnimations()
    {
        foreach (var animation in _activeAnimations)
        {
            animation.Reset();
        }
        _activeAnimations.Clear();
    }

    /// <summary>
    /// Updates the animation controller.
    /// Should be called every frame.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    /// <param name="hasUserInput">Whether the user provided any input this frame.</param>
    public void Update(GameTime gameTime, bool hasUserInput)
    {
        if (_targetCamera == null) return;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        switch (Mode)
        {
            case AnimationMode.UserControl:
            case AnimationMode.WaitingForIdle:
                if (_isFollowModeActive)
                {
                    // In follow mode but user has control
                    UpdateIdleDetectionForFollow(deltaTime, hasUserInput);
                }
                else
                {
                    UpdateIdleDetection(deltaTime, hasUserInput);
                }
                break;

            case AnimationMode.Animating:
                if (hasUserInput || DetectCameraMovement())
                {
                    StartReturnToControl();
                }
                else
                {
                    UpdateAnimations(gameTime);
                }
                break;

            case AnimationMode.ReturningToControl:
                UpdateReturnAnimation(gameTime);
                break;
                
            case AnimationMode.FollowingTarget:
                if (hasUserInput)
                {
                    // User wants control, save current state and give them control
                    SaveCameraState();
                    _idleTime = 0;
                    SetMode(AnimationMode.UserControl);
                }
                else
                {
                    UpdateFollowAnimation(gameTime);
                }
                break;
                
            case AnimationMode.ReturningToFollow:
                UpdateReturnToFollowAnimation(gameTime);
                break;
        }

        // Update tracking
        _lastPosition = _targetCamera.Position;
        _lastYaw = _targetCamera.Yaw;
        _lastPitch = _targetCamera.Pitch;
        _lastZoom = _targetCamera.Zoom;
    }

    private void UpdateIdleDetection(float deltaTime, bool hasUserInput)
    {
        if (hasUserInput || DetectCameraMovement())
        {
            _idleTime = 0;
            if (Mode == AnimationMode.WaitingForIdle)
            {
                SetMode(AnimationMode.UserControl);
            }
        }
        else
        {
            _idleTime += deltaTime;
            
            if (Mode == AnimationMode.UserControl && _idleTime > 0)
            {
                SetMode(AnimationMode.WaitingForIdle);
            }
            
            if (AutoAnimationEnabled && _idleTime >= IdleTimeout)
            {
                StartAnimating();
            }
        }
    }

    private bool DetectCameraMovement()
    {
        if (_targetCamera == null) return false;

        float positionDelta = Vector3.Distance(_targetCamera.Position, _lastPosition);
        float yawDelta = MathF.Abs(_targetCamera.Yaw - _lastYaw);
        float pitchDelta = MathF.Abs(_targetCamera.Pitch - _lastPitch);
        float zoomDelta = MathF.Abs(_targetCamera.Zoom - _lastZoom);

        return positionDelta > MovementThreshold ||
               yawDelta > 0.01f ||
               pitchDelta > 0.01f ||
               zoomDelta > 0.01f;
    }

    private void StartAnimating()
    {
        if (_targetCamera == null) return;

        // Save current camera state for return animation
        _savedPosition = _targetCamera.Position;
        _savedYaw = _targetCamera.Yaw;
        _savedPitch = _targetCamera.Pitch;
        _savedZoom = _targetCamera.Zoom;

        // Start default animations if none are active
        if (_activeAnimations.Count == 0 && _availableAnimations.Count > 0)
        {
            // Activate all registered animations with stacking
            foreach (var animation in _availableAnimations)
            {
                ActivateAnimation(animation);
            }
        }

        SetMode(AnimationMode.Animating);
    }

    private void StartReturnToControl()
    {
        if (_targetCamera == null) return;

        // Clear active animations
        ClearAnimations();

        // Create return animation
        _returnAnimation = new ReturnToControlAnimation(
            _savedPosition,
            _savedYaw,
            _savedPitch,
            _savedZoom,
            0.5f
        );
        _returnAnimation.Initialize(_targetCamera);

        SetMode(AnimationMode.ReturningToControl);
    }

    private void UpdateAnimations(GameTime gameTime)
    {
        if (_targetCamera == null) return;

        // Combine transforms from all active animations
        CameraTransform combinedTransform = CameraTransform.Zero;
        var completedAnimations = new List<ICameraAnimation>();

        foreach (var animation in _activeAnimations)
        {
            var transform = animation.Update(gameTime, _targetCamera);
            combinedTransform += transform;

            if (animation.State == AnimationState.Completed)
            {
                completedAnimations.Add(animation);
            }
        }

        // Apply combined transform
        _targetCamera.Position += combinedTransform.PositionDelta;
        _targetCamera.Yaw += combinedTransform.YawDelta;
        _targetCamera.Pitch += combinedTransform.PitchDelta;
        _targetCamera.Zoom += combinedTransform.ZoomDelta;
        _targetCamera.UpdateMatrices();

        // Remove completed non-looping animations
        foreach (var animation in completedAnimations)
        {
            _activeAnimations.Remove(animation);
        }
    }

    private void UpdateReturnAnimation(GameTime gameTime)
    {
        if (_targetCamera == null || _returnAnimation == null) return;

        var transform = _returnAnimation.Update(gameTime, _targetCamera);

        _targetCamera.Position += transform.PositionDelta;
        _targetCamera.Yaw += transform.YawDelta;
        _targetCamera.Pitch += transform.PitchDelta;
        _targetCamera.Zoom += transform.ZoomDelta;
        _targetCamera.UpdateMatrices();

        if (_returnAnimation.State == AnimationState.Completed)
        {
            _returnAnimation = null;
            _idleTime = 0;
            SetMode(AnimationMode.UserControl);
        }
    }

    private void SetMode(AnimationMode newMode)
    {
        if (Mode != newMode)
        {
            Mode = newMode;
            ModeChanged?.Invoke(newMode);
        }
    }

    /// <summary>
    /// Forces the controller back to user control mode immediately.
    /// </summary>
    public void ForceUserControl()
    {
        ClearAnimations();
        _returnAnimation = null;
        _idleTime = 0;
        SetMode(AnimationMode.UserControl);
    }

    /// <summary>
    /// Forces animations to start immediately, ignoring idle timeout.
    /// </summary>
    public void ForceStartAnimations()
    {
        if (_targetCamera != null)
        {
            _savedPosition = _targetCamera.Position;
            _savedYaw = _targetCamera.Yaw;
            _savedPitch = _targetCamera.Pitch;
            _savedZoom = _targetCamera.Zoom;
        }
        StartAnimating();
    }
    
    /// <summary>
    /// Starts follow mode with a target position provider.
    /// </summary>
    /// <param name="positionProvider">Function that returns the current target position.</param>
    /// <param name="velocityProvider">Optional function that returns the target velocity.</param>
    public void StartFollowMode(Func<Vector3> positionProvider, Func<Vector3>? velocityProvider = null)
    {
        if (_targetCamera == null) return;
        
        // Save current camera state
        SaveCameraState();
        
        // Clear any existing animations
        ClearAnimations();
        _returnAnimation = null;
        
        // Create and initialize follow animation
        _followAnimation = new ShipFollowAnimation
        {
            IsLooping = true,
            AutoSwitchModes = true,
            Weight = 1f
        };
        _followAnimation.SetTargetProvider(positionProvider, velocityProvider);
        _followAnimation.Initialize(_targetCamera);
        
        _isFollowModeActive = true;
        _idleTime = 0;
        SetMode(AnimationMode.FollowingTarget);
    }
    
    /// <summary>
    /// Stops follow mode and returns to normal operation.
    /// </summary>
    public void StopFollowMode()
    {
        _isFollowModeActive = false;
        _followAnimation?.Reset();
        _followAnimation = null;
        _idleTime = 0;
        SetMode(AnimationMode.UserControl);
    }
    
    private void UpdateIdleDetectionForFollow(float deltaTime, bool hasUserInput)
    {
        if (hasUserInput || DetectCameraMovement())
        {
            _idleTime = 0;
            SetMode(AnimationMode.UserControl);
        }
        else
        {
            _idleTime += deltaTime;
            
            if (_idleTime >= IdleTimeout)
            {
                // Return to following the target
                StartReturnToFollow();
            }
            else if (Mode == AnimationMode.UserControl && _idleTime > 0)
            {
                SetMode(AnimationMode.WaitingForIdle);
            }
        }
    }
    
    private void UpdateFollowAnimation(GameTime gameTime)
    {
        if (_targetCamera == null || _followAnimation == null) return;
        
        var transform = _followAnimation.Update(gameTime, _targetCamera);
        
        _targetCamera.Position += transform.PositionDelta;
        _targetCamera.Yaw += transform.YawDelta;
        _targetCamera.Pitch += transform.PitchDelta;
        _targetCamera.Zoom += transform.ZoomDelta;
        _targetCamera.UpdateMatrices();
    }
    
    private void StartReturnToFollow()
    {
        if (_targetCamera == null || _followAnimation == null) return;
        
        // Create return animation - but we need to calculate where the follow animation would put us
        // For simplicity, just smoothly transition to follow mode
        _returnAnimation = null; // No specific return target, follow animation will take over
        SetMode(AnimationMode.FollowingTarget);
        _idleTime = 0;
    }
    
    private void UpdateReturnToFollowAnimation(GameTime gameTime)
    {
        // This mode smoothly blends back to follow mode
        // For now, just switch directly
        SetMode(AnimationMode.FollowingTarget);
        _idleTime = 0;
    }
    
    private void SaveCameraState()
    {
        if (_targetCamera == null) return;
        
        _savedPosition = _targetCamera.Position;
        _savedYaw = _targetCamera.Yaw;
        _savedPitch = _targetCamera.Pitch;
        _savedZoom = _targetCamera.Zoom;
    }
}
