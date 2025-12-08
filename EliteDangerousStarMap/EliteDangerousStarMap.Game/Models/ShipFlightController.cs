using Microsoft.Xna.Framework;

namespace EliteDangerousStarMap.Models;

/// <summary>
/// Flight state enumeration
/// </summary>
public enum FlightState
{
    Idle,
    WarmingUp,
    Accelerating,
    Cruising,
    Decelerating,
    Arriving,
    Cooldown
}

/// <summary>
/// Manages ship flight between star systems with power curve movement
/// </summary>
public class ShipFlightController
{
    private readonly FlightLog _flightLog;
    private readonly Random _random = new();
    
    // Flight state
    private FlightState _state = FlightState.Idle;
    private float _stateTimer;
    private float _journeyProgress;
    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private float _journeyDistance;
    
    // Event timing
    private float _nextEventTimer;
    
    // Configuration
    public float WarmupDuration { get; set; } = 2f;
    public float AccelerationDuration { get; set; } = 3f;
    public float DecelerationDuration { get; set; } = 3f;
    public float CooldownDuration { get; set; } = 2f;
    public float CruiseSpeed { get; set; } = 50f; // Units per second at max speed
    public float MinEventInterval { get; set; } = 3f;
    public float MaxEventInterval { get; set; } = 8f;
    public float ArrivalThreshold { get; set; } = 0.5f; // Distance to consider "arrived"
    public float DecelerationTimeoutMultiplier { get; set; } = 1.5f; // Safety timeout for deceleration
    
    /// <summary>
    /// Current flight state
    /// </summary>
    public FlightState State => _state;
    
    /// <summary>
    /// Whether the ship is currently flying
    /// </summary>
    public bool IsFlying => _state != FlightState.Idle;
    
    /// <summary>
    /// Current world position of the ship
    /// </summary>
    public Vector3 CurrentPosition { get; private set; }
    
    /// <summary>
    /// Current visual scale of the ship (for transition effects)
    /// </summary>
    public float ShipScale { get; private set; } = 1f;
    
    /// <summary>
    /// The flight log for this journey
    /// </summary>
    public FlightLog FlightLog => _flightLog;
    
    /// <summary>
    /// Journey progress (0 to 1)
    /// </summary>
    public float JourneyProgress => _journeyProgress;
    
    /// <summary>
    /// Current speed as a fraction of cruise speed
    /// </summary>
    public float CurrentSpeedFraction { get; private set; }
    
    /// <summary>
    /// Source system for current/last journey
    /// </summary>
    public StarSystem? FromSystem { get; private set; }
    
    /// <summary>
    /// Destination system for current/last journey
    /// </summary>
    public StarSystem? ToSystem { get; private set; }
    
    /// <summary>
    /// Event fired when journey completes
    /// </summary>
    public event Action<StarSystem>? OnJourneyComplete;

    public ShipFlightController()
    {
        _flightLog = new FlightLog();
    }

    /// <summary>
    /// Starts a flight from one system to another
    /// </summary>
    public void StartFlight(StarSystem fromSystem, StarSystem toSystem)
    {
        FromSystem = fromSystem;
        ToSystem = toSystem;
        
        _startPosition = fromSystem.WorldPosition;
        _targetPosition = toSystem.WorldPosition;
        _journeyDistance = Vector3.Distance(_startPosition, _targetPosition);
        
        CurrentPosition = _startPosition;
        _journeyProgress = 0;
        ShipScale = 0; // Start invisible, will scale up
        
        _flightLog.StartJourney(fromSystem.Name, toSystem.Name);
        
        SetState(FlightState.WarmingUp);
        _flightLog.AddEntry("Warming up jump engines", LogEntryType.Status);
        
        ResetEventTimer();
    }

    /// <summary>
    /// Updates the flight controller
    /// </summary>
    public void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        _flightLog.Update(deltaTime);
        
        if (_state == FlightState.Idle) return;
        
        _stateTimer += deltaTime;
        
        // Check for random events during flight
        if (_state == FlightState.Accelerating || _state == FlightState.Cruising || _state == FlightState.Decelerating)
        {
            _nextEventTimer -= deltaTime;
            if (_nextEventTimer <= 0)
            {
                _flightLog.AddRandomEvent();
                ResetEventTimer();
            }
        }
        
        switch (_state)
        {
            case FlightState.WarmingUp:
                UpdateWarmup(deltaTime);
                break;
            case FlightState.Accelerating:
                UpdateAcceleration(deltaTime);
                break;
            case FlightState.Cruising:
                UpdateCruising(deltaTime);
                break;
            case FlightState.Decelerating:
                UpdateDeceleration(deltaTime);
                break;
            case FlightState.Arriving:
                UpdateArriving(deltaTime);
                break;
            case FlightState.Cooldown:
                UpdateCooldown(deltaTime);
                break;
        }
    }

    private void UpdateWarmup(float deltaTime)
    {
        // Scale ship up during warmup
        float progress = _stateTimer / WarmupDuration;
        ShipScale = EaseOutQuad(Math.Clamp(progress, 0f, 1f));
        
        if (_stateTimer >= WarmupDuration)
        {
            _flightLog.AddEntry("Jump started", LogEntryType.Status);
            SetState(FlightState.Accelerating);
        }
    }

    private void UpdateAcceleration(float deltaTime)
    {
        // Power curve acceleration (ease in)
        float accelProgress = _stateTimer / AccelerationDuration;
        CurrentSpeedFraction = EaseInQuad(Math.Clamp(accelProgress, 0f, 1f));
        
        MoveShip(deltaTime);
        
        if (_stateTimer >= AccelerationDuration)
        {
            _flightLog.AddEntry("Reached cruising velocity", LogEntryType.Status);
            SetState(FlightState.Cruising);
        }
    }

    private void UpdateCruising(float deltaTime)
    {
        CurrentSpeedFraction = 1f;
        MoveShip(deltaTime);
        
        // Check if we need to start decelerating
        float remainingDistance = Vector3.Distance(CurrentPosition, _targetPosition);
        float decelerationDistance = CalculateDecelerationDistance();
        
        if (remainingDistance <= decelerationDistance)
        {
            _flightLog.AddEntry("Slowing to transition back to normal speed for system entry", LogEntryType.Status);
            SetState(FlightState.Decelerating);
        }
    }

    private void UpdateDeceleration(float deltaTime)
    {
        // Power curve deceleration (ease out)
        float decelProgress = _stateTimer / DecelerationDuration;
        CurrentSpeedFraction = 1f - EaseInQuad(Math.Clamp(decelProgress, 0f, 1f));
        CurrentSpeedFraction = Math.Max(CurrentSpeedFraction, 0.1f); // Minimum speed
        
        MoveShip(deltaTime);
        
        // Check if we've arrived
        float remainingDistance = Vector3.Distance(CurrentPosition, _targetPosition);
        if (remainingDistance < ArrivalThreshold || _stateTimer >= DecelerationDuration * DecelerationTimeoutMultiplier)
        {
            CurrentPosition = _targetPosition;
            _journeyProgress = 1f;
            _flightLog.AddEntry($"Arrived at star", LogEntryType.Status);
            SetState(FlightState.Arriving);
        }
    }

    private void UpdateArriving(float deltaTime)
    {
        // Scale ship down during arrival
        float progress = _stateTimer / 1f; // 1 second arrival animation
        ShipScale = 1f - EaseInQuad(Math.Clamp(progress, 0f, 1f));
        
        if (_stateTimer >= 1f)
        {
            _flightLog.AddEntry("Recharging jump drive", LogEntryType.Status);
            SetState(FlightState.Cooldown);
        }
    }

    private void UpdateCooldown(float deltaTime)
    {
        if (_stateTimer >= CooldownDuration)
        {
            CompleteJourney();
        }
    }

    private void MoveShip(float deltaTime)
    {
        float speed = CruiseSpeed * CurrentSpeedFraction;
        float movement = speed * deltaTime;
        
        Vector3 direction = Vector3.Normalize(_targetPosition - CurrentPosition);
        CurrentPosition += direction * movement;
        
        // Update journey progress
        float traveled = Vector3.Distance(_startPosition, CurrentPosition);
        _journeyProgress = Math.Clamp(traveled / _journeyDistance, 0f, 1f);
    }

    private float CalculateDecelerationDistance()
    {
        // Approximate distance needed to decelerate
        // Using average speed during deceleration
        float averageSpeed = CruiseSpeed * 0.5f;
        return averageSpeed * DecelerationDuration;
    }

    private void SetState(FlightState newState)
    {
        _state = newState;
        _stateTimer = 0;
    }

    private void CompleteJourney()
    {
        _flightLog.MarkArrival();
        _flightLog.SaveToFile();
        
        var destination = ToSystem;
        SetState(FlightState.Idle);
        CurrentSpeedFraction = 0;
        ShipScale = 0;
        
        if (destination != null)
        {
            OnJourneyComplete?.Invoke(destination);
        }
    }

    private void ResetEventTimer()
    {
        _nextEventTimer = MinEventInterval + (float)_random.NextDouble() * (MaxEventInterval - MinEventInterval);
    }

    /// <summary>
    /// Cancels the current flight
    /// </summary>
    public void CancelFlight()
    {
        if (_state != FlightState.Idle)
        {
            _flightLog.AddEntry("Flight cancelled", LogEntryType.Warning);
            SetState(FlightState.Idle);
            CurrentSpeedFraction = 0;
            ShipScale = 0;
        }
    }

    // Easing functions
    private static float EaseInQuad(float t) => t * t;
    private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
    // Note: EaseInOutQuad is provided for potential future use in more complex flight paths
    // private static float EaseInOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - MathF.Pow(-2f * t + 2f, 2f) / 2f;
}
