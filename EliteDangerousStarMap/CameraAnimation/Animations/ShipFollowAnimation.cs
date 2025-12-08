using Microsoft.Xna.Framework;
using CameraAnimation.Interfaces;

namespace CameraAnimation.Animations;

/// <summary>
/// Different modes for following a moving target
/// </summary>
public enum ShipFollowMode
{
    /// <summary>Simple follow with ship centered in view</summary>
    Centered,
    
    /// <summary>Orbit around the ship slowly</summary>
    Orbit,
    
    /// <summary>Third-person view from behind the ship</summary>
    ThirdPerson,
    
    /// <summary>Zoom out and back in slowly</summary>
    ZoomPulse
}

/// <summary>
/// Animation that follows a moving target (ship) with various camera modes.
/// Designed for travel/flight scenarios.
/// </summary>
public class ShipFollowAnimation : BaseCameraAnimation
{
    private readonly Random _random = new();
    
    // Target tracking
    private Func<Vector3>? _targetPositionProvider;
    private Func<Vector3>? _targetVelocityProvider;
    private Vector3 _lastTargetPosition;
    
    // Follow mode
    private ShipFollowMode _currentMode = ShipFollowMode.Centered;
    private float _modeTimer;
    private float _modeDuration;
    
    // Orbit mode
    private float _orbitAngle;
    private float _orbitRadius = 30f;
    private float _orbitSpeed = 0.3f; // Radians per second
    
    // Zoom pulse mode
    private float _zoomPhase;
    private float _baseZoom = 1f;
    private float _zoomAmplitude = 0.3f;
    
    // Third person
    private float _thirdPersonDistance = 20f;
    private float _thirdPersonHeight = 5f;
    
    // Smoothing
    private Vector3 _smoothedPosition;
    private float _smoothingFactor = 5f;
    
    /// <inheritdoc/>
    public override string Name => "ShipFollow";

    /// <inheritdoc/>
    public override float Duration => 0f; // Continuous, no fixed duration

    /// <summary>
    /// Minimum time to stay in one follow mode before switching
    /// </summary>
    public float MinModeDuration { get; set; } = 5f;

    /// <summary>
    /// Maximum time to stay in one follow mode before switching
    /// </summary>
    public float MaxModeDuration { get; set; } = 15f;

    /// <summary>
    /// Whether to automatically switch between follow modes
    /// </summary>
    public bool AutoSwitchModes { get; set; } = true;

    /// <summary>
    /// Current follow mode
    /// </summary>
    public ShipFollowMode CurrentMode => _currentMode;

    /// <summary>
    /// Sets the target position provider function
    /// </summary>
    public void SetTargetProvider(Func<Vector3> positionProvider, Func<Vector3>? velocityProvider = null)
    {
        _targetPositionProvider = positionProvider;
        _targetVelocityProvider = velocityProvider;
        _lastTargetPosition = positionProvider();
        _smoothedPosition = _lastTargetPosition;
    }

    /// <summary>
    /// Forces a specific follow mode
    /// </summary>
    public void SetFollowMode(ShipFollowMode mode)
    {
        _currentMode = mode;
        _modeTimer = 0;
        RandomizeModeDuration();
    }

    /// <inheritdoc/>
    public override void Initialize(IAnimatedCamera camera, Vector3? focusPoint = null)
    {
        base.Initialize(camera, focusPoint);
        
        if (_targetPositionProvider != null)
        {
            _lastTargetPosition = _targetPositionProvider();
            _smoothedPosition = _lastTargetPosition;
        }
        
        _orbitAngle = (float)_random.NextDouble() * MathF.PI * 2f;
        _zoomPhase = 0;
        _baseZoom = camera.Zoom;
        
        RandomizeModeDuration();
    }

    /// <inheritdoc/>
    protected override CameraTransform CalculateTransform(GameTime gameTime, IAnimatedCamera camera)
    {
        if (_targetPositionProvider == null)
            return CameraTransform.Zero;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        // Get current target position
        Vector3 targetPosition = _targetPositionProvider();
        Vector3 velocity = _targetVelocityProvider?.Invoke() ?? (targetPosition - _lastTargetPosition) / Math.Max(deltaTime, 0.001f);
        
        // Smooth the target position
        _smoothedPosition = Vector3.Lerp(_smoothedPosition, targetPosition, _smoothingFactor * deltaTime);
        
        // Update mode timer
        _modeTimer += deltaTime;
        if (AutoSwitchModes && _modeTimer >= _modeDuration)
        {
            SwitchToRandomMode();
        }
        
        // Calculate camera transform based on current mode
        CameraTransform transform = _currentMode switch
        {
            ShipFollowMode.Centered => CalculateCenteredFollow(deltaTime, camera, _smoothedPosition, velocity),
            ShipFollowMode.Orbit => CalculateOrbitFollow(deltaTime, camera, _smoothedPosition),
            ShipFollowMode.ThirdPerson => CalculateThirdPersonFollow(deltaTime, camera, _smoothedPosition, velocity),
            ShipFollowMode.ZoomPulse => CalculateZoomPulseFollow(deltaTime, camera, _smoothedPosition),
            _ => CameraTransform.Zero
        };
        
        _lastTargetPosition = targetPosition;
        
        return transform;
    }

    private CameraTransform CalculateCenteredFollow(float deltaTime, IAnimatedCamera camera, Vector3 target, Vector3 velocity)
    {
        // Keep camera at fixed distance, looking at target
        Vector3 offset = new Vector3(0, 10f, -40f);
        Vector3 desiredPosition = target + offset;
        
        Vector3 positionDelta = (desiredPosition - camera.Position) * 3f * deltaTime;
        
        // Calculate yaw/pitch to look at target
        Vector3 toTarget = target - camera.Position;
        float targetYaw = MathF.Atan2(toTarget.X, toTarget.Z);
        float horizontalDist = new Vector2(toTarget.X, toTarget.Z).Length();
        float targetPitch = -MathF.Atan2(toTarget.Y, horizontalDist);
        
        float yawDelta = NormalizeAngle(targetYaw - camera.Yaw) * 3f * deltaTime;
        float pitchDelta = (targetPitch - camera.Pitch) * 3f * deltaTime;
        
        return new CameraTransform(positionDelta, yawDelta, pitchDelta, 0);
    }

    private CameraTransform CalculateOrbitFollow(float deltaTime, IAnimatedCamera camera, Vector3 target)
    {
        _orbitAngle += _orbitSpeed * deltaTime;
        
        // Calculate orbit position
        float x = target.X + _orbitRadius * MathF.Sin(_orbitAngle);
        float z = target.Z + _orbitRadius * MathF.Cos(_orbitAngle);
        float y = target.Y + 10f;
        
        Vector3 desiredPosition = new Vector3(x, y, z);
        Vector3 positionDelta = (desiredPosition - camera.Position) * 2f * deltaTime;
        
        // Look at target
        Vector3 toTarget = target - camera.Position;
        float targetYaw = MathF.Atan2(toTarget.X, toTarget.Z);
        float horizontalDist = new Vector2(toTarget.X, toTarget.Z).Length();
        float targetPitch = -MathF.Atan2(toTarget.Y, horizontalDist);
        
        float yawDelta = NormalizeAngle(targetYaw - camera.Yaw) * 3f * deltaTime;
        float pitchDelta = (targetPitch - camera.Pitch) * 3f * deltaTime;
        
        return new CameraTransform(positionDelta, yawDelta, pitchDelta, 0);
    }

    private CameraTransform CalculateThirdPersonFollow(float deltaTime, IAnimatedCamera camera, Vector3 target, Vector3 velocity)
    {
        // Calculate behind direction based on velocity or default
        Vector3 behindDir;
        if (velocity.LengthSquared() > 0.01f)
        {
            behindDir = -Vector3.Normalize(velocity);
        }
        else
        {
            behindDir = new Vector3(0, 0, -1);
        }
        
        Vector3 desiredPosition = target + behindDir * _thirdPersonDistance + Vector3.Up * _thirdPersonHeight;
        Vector3 positionDelta = (desiredPosition - camera.Position) * 4f * deltaTime;
        
        // Look at target
        Vector3 toTarget = target - camera.Position;
        float targetYaw = MathF.Atan2(toTarget.X, toTarget.Z);
        float horizontalDist = new Vector2(toTarget.X, toTarget.Z).Length();
        float targetPitch = -MathF.Atan2(toTarget.Y, horizontalDist);
        
        float yawDelta = NormalizeAngle(targetYaw - camera.Yaw) * 4f * deltaTime;
        float pitchDelta = (targetPitch - camera.Pitch) * 4f * deltaTime;
        
        return new CameraTransform(positionDelta, yawDelta, pitchDelta, 0);
    }

    private CameraTransform CalculateZoomPulseFollow(float deltaTime, IAnimatedCamera camera, Vector3 target)
    {
        _zoomPhase += deltaTime * 0.5f; // Slow zoom cycle
        float zoomOffset = MathF.Sin(_zoomPhase * MathF.PI * 2f) * _zoomAmplitude;
        float targetZoom = _baseZoom + zoomOffset;
        float zoomDelta = (targetZoom - camera.Zoom) * 2f * deltaTime;
        
        // Also do centered follow
        Vector3 offset = new Vector3(0, 15f, -50f);
        Vector3 desiredPosition = target + offset;
        Vector3 positionDelta = (desiredPosition - camera.Position) * 2f * deltaTime;
        
        // Look at target
        Vector3 toTarget = target - camera.Position;
        float targetYaw = MathF.Atan2(toTarget.X, toTarget.Z);
        float horizontalDist = new Vector2(toTarget.X, toTarget.Z).Length();
        float targetPitch = -MathF.Atan2(toTarget.Y, horizontalDist);
        
        float yawDelta = NormalizeAngle(targetYaw - camera.Yaw) * 2f * deltaTime;
        float pitchDelta = (targetPitch - camera.Pitch) * 2f * deltaTime;
        
        return new CameraTransform(positionDelta, yawDelta, pitchDelta, zoomDelta);
    }

    private void SwitchToRandomMode()
    {
        var modes = Enum.GetValues<ShipFollowMode>();
        ShipFollowMode newMode;
        do
        {
            newMode = modes[_random.Next(modes.Length)];
        } while (newMode == _currentMode && modes.Length > 1);
        
        _currentMode = newMode;
        _modeTimer = 0;
        RandomizeModeDuration();
        
        // Reset mode-specific state
        if (newMode == ShipFollowMode.ZoomPulse)
        {
            _zoomPhase = 0;
        }
    }

    private void RandomizeModeDuration()
    {
        _modeDuration = MinModeDuration + (float)_random.NextDouble() * (MaxModeDuration - MinModeDuration);
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > MathF.PI) angle -= MathF.PI * 2f;
        while (angle < -MathF.PI) angle += MathF.PI * 2f;
        return angle;
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        base.Reset();
        _modeTimer = 0;
        _orbitAngle = 0;
        _zoomPhase = 0;
    }
}
