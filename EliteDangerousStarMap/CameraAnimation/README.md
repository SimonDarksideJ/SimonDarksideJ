# Camera Animation Library

A reusable animated camera system for MonoGame with idle detection, multiple animation modes, and smooth transitions.

## Features

- **Idle Detection**: Automatically starts animations after configurable idle timeout
- **Multiple Animation Modes**: Orbit, Flyby, Zoom (stackable)
- **Smooth Transitions**: Return-to-control animation for seamless user experience
- **Multi-Camera Support**: Works with any camera implementing `IAnimatedCamera`
- **Settings Persistence**: JSON-based settings save/load

## Usage

### 1. Implement IAnimatedCamera

Make your camera class implement `IAnimatedCamera`:

```csharp
public class MyCamera : IAnimatedCamera
{
    public Vector3 Position { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public float Zoom { get; set; }
    public Matrix ViewMatrix { get; }
    public Matrix ProjectionMatrix { get; }
    public Vector3 Forward { get; }
    public Vector3 Right { get; }
    public Vector3 Up { get; }
    
    public void UpdateMatrices() { /* ... */ }
    public void UpdateProjectionMatrix(int width, int height) { /* ... */ }
}
```

### 2. Create Animation Controller

```csharp
var controller = new AnimationController();
controller.SetTargetCamera(myCamera);
controller.IdleTimeout = 30f; // Seconds before animation starts

// Register animations
controller.RegisterAnimation(new OrbitAnimation { OrbitDuration = 20f });
controller.RegisterAnimation(new ZoomAnimation { MinZoom = 0.5f, MaxZoom = 2f });
```

### 3. Update Every Frame

```csharp
// In your Update method
bool hasUserInput = /* check if user provided input */;
controller.Update(gameTime, hasUserInput);
```

## Animation Types

### OrbitAnimation
Orbits the camera around a focus point.

**Properties:**
- `OrbitDuration`: Time for one complete orbit (seconds)
- `OrbitRadius`: Distance from focus point (null = use current)
- `HeightOffset`: Vertical offset above orbit plane
- `Clockwise`: Direction of orbit

### FlybyAnimation
Moves camera along a path past the focus point.

**Properties:**
- `FlybyDuration`: Duration of flyby (seconds)
- `Speed`: Movement speed
- `PassDistance`: Minimum distance to focus point
- `LookAtFocus`: Whether to look at focus point

### ZoomAnimation
Smoothly zooms in and out.

**Properties:**
- `ZoomDuration`: Duration of zoom cycle (seconds)
- `MinZoom`: Minimum zoom level
- `MaxZoom`: Maximum zoom level
- `DollyZoom`: Move camera with zoom for Vertigo effect

### ShipFollowAnimation
Follows a moving target (like a ship) with various camera styles.

**Properties:**
- `MinModeDuration`: Minimum time in one mode before switching
- `MaxModeDuration`: Maximum time in one mode before switching
- `AutoSwitchModes`: Whether to automatically switch between modes

**Follow Modes:**
- `Centered`: Keep target in center of view
- `Orbit`: Slowly orbit around the target
- `ThirdPerson`: Follow from behind based on velocity
- `ZoomPulse`: Gradual zoom in/out while following

**Usage:**
```csharp
var follow = new ShipFollowAnimation();
follow.SetTargetProvider(
    () => ship.Position,          // Position provider
    () => ship.Velocity           // Optional velocity provider
);
controller.StartFollowMode(() => ship.Position, () => ship.Velocity);
```

## Extension Points

### Creating Custom Animations

1. Implement `ICameraAnimation` or extend `BaseCameraAnimation`:

```csharp
public class ShakeAnimation : BaseCameraAnimation
{
    public override string Name => "Shake";
    public override float Duration => 2f;
    
    protected override CameraTransform CalculateTransform(
        GameTime gameTime, IAnimatedCamera camera)
    {
        // Calculate shake offset
        var random = new Random();
        return new CameraTransform(
            new Vector3(
                (float)(random.NextDouble() - 0.5) * Intensity,
                (float)(random.NextDouble() - 0.5) * Intensity,
                0),
            0, 0, 0);
    }
}
```

### Potential Extensions

1. **Animation Presets/Profiles**
   - Save/load animation combinations
   - Quick switching between profiles

2. **Animation Triggers**
   - Game event-based triggers
   - Distance-based triggers
   - Time-based triggers

3. **Path-Based Animations**
   - Bezier curve paths
   - Waypoint sequences
   - Spline interpolation

4. **Camera Effects**
   - Screen shake
   - Focus pull (depth of field)
   - Motion blur simulation

5. **Multi-Camera Coordination**
   - Synchronized animations across cameras
   - Camera transitions/cuts
   - Picture-in-picture support

6. **Recording/Playback**
   - Record user camera movements
   - Play back recorded animations
   - Animation editor integration

7. **Scene Analysis**
   - Auto-generate interesting camera paths
   - Focus on points of interest
   - Avoid obstacles/occlusion

## Settings

Settings are saved as JSON and include:

```json
{
  "IdleTimeout": 30,
  "AutoAnimationEnabled": true,
  "OrbitDuration": 20,
  "FlybyDuration": 15,
  "ZoomDuration": 10,
  "OrbitEnabled": true,
  "FlybyEnabled": false,
  "ZoomEnabled": true,
  "MinZoom": 0.5,
  "MaxZoom": 2.0,
  "ReturnDuration": 0.5
}
```

## Architecture

```
CameraAnimation/
├── Interfaces/
│   ├── IAnimatedCamera.cs    # Camera abstraction
│   └── ICameraAnimation.cs   # Animation interface + CameraTransform
├── Animations/
│   ├── BaseCameraAnimation.cs      # Base class with common logic
│   ├── OrbitAnimation.cs           # Orbit around focus
│   ├── FlybyAnimation.cs           # Fly past focus
│   ├── ZoomAnimation.cs            # Zoom in/out
│   └── ReturnToControlAnimation.cs # Smooth return to user control
└── Core/
    ├── AnimationController.cs  # Main orchestrator
    └── AnimationSettings.cs    # Persistent settings
```

## Multi-Camera Support

The animation system supports multiple cameras by using separate `AnimationController` instances:

```csharp
// Main camera
var mainController = new AnimationController();
mainController.SetTargetCamera(mainCamera);

// Missile camera (different animations)
var missileController = new AnimationController();
missileController.SetTargetCamera(missileCamera);
missileController.RegisterAnimation(new FlybyAnimation { Speed = 100f });

// Split-screen player 2
var p2Controller = new AnimationController();
p2Controller.SetTargetCamera(player2Camera);
```

Each controller maintains its own state and animations independently.
