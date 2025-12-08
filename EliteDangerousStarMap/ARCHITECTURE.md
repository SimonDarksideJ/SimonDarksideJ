# Elite Dangerous Star Map - Architecture Documentation

## Overview

The Elite Dangerous Star Map is a MonoGame DesktopGL application that visualizes star systems from the Elite Dangerous universe. It provides an interactive 3D view of the galaxy with camera controls, system selection, and ship flight simulation.

## Project Structure

```
EliteDangerousStarMap/
├── EliteDangerousStarMap.sln              # Solution file
├── ARCHITECTURE.md                         # This document
├── README.md                               # User-facing documentation
│
├── CameraAnimation/                        # Reusable camera animation library (.NET 9.0)
│   ├── CameraAnimation.csproj
│   ├── README.md
│   ├── Interfaces/
│   │   ├── IAnimatedCamera.cs             # Camera interface for animation support
│   │   └── ICameraAnimation.cs            # Base animation interface
│   ├── Core/
│   │   ├── AnimationController.cs         # Manages animation lifecycle and transitions
│   │   └── AnimationSettings.cs           # Persistent settings with JSON serialization
│   └── Animations/
│       ├── BaseCameraAnimation.cs         # Base class for animations
│       ├── OrbitAnimation.cs              # Orbits around focus point
│       ├── FlybyAnimation.cs              # Cinematic flyby paths
│       ├── ZoomAnimation.cs               # Zoom in/out effect
│       ├── ReturnToControlAnimation.cs    # Smooth transition to user control
│       └── ShipFollowAnimation.cs         # Follows moving ship target
│
├── StarSystemData.Models/                  # Core data models (.NET 8.0 - MGCB compatible)
│   ├── StarSystemData.Models.csproj
│   ├── DataSourceMode.cs                  # API-first vs Local-first enum
│   ├── StarSystemRecord.cs                # Binary-serializable system data
│   └── StarSystemDatabase.cs              # Collection with spatial indexing
│
├── StarSystemData.Pipeline/                # Content pipeline extension (.NET 8.0)
│   ├── StarSystemData.Pipeline.csproj
│   ├── StarSystemDataProcessor.cs         # MGCB content importer/processor
│   └── StarSystemDataWriter.cs            # Binary content writer
│
├── StarSystemData/                         # Runtime library (.NET 8.0)
│   ├── StarSystemData.csproj
│   ├── README.md
│   └── Runtime/
│       ├── StarSystemDataReader.cs        # ContentManager reader
│       ├── IStarSystemDataProvider.cs     # Query interface
│       └── ContentStarSystemProvider.cs   # Query implementation
│
├── StarSystemData.Tests/                   # Unit tests (.NET 8.0)
│   ├── StarSystemData.Tests.csproj
│   └── StarSystemQueryTests.cs            # 40 unit tests
│
└── EliteDangerousStarMap.Game/            # Main game project (.NET 9.0)
    ├── EliteDangerousStarMap.Game.csproj
    ├── Program.cs                          # Entry point
    ├── Game1.cs                            # MonoGame Game class
    ├── StarMapManager.cs                   # Core orchestration
    ├── Content/
    │   ├── Content.mgcb                    # MonoGame content builder file
    │   ├── DefaultFont.spritefont
    │   └── StarSystems.json               # Local star system data (50 systems)
    ├── Input/
    │   └── FpsCameraController.cs         # FPS camera with IAnimatedCamera
    ├── Models/
    │   ├── StarSystem.cs                  # Runtime star system model
    │   ├── PlayerShip.cs                  # Player state and position
    │   ├── ShipFlightController.cs        # Flight state machine
    │   └── FlightLog.cs                   # Flight event logging
    ├── Rendering/
    │   ├── SphereRenderer.cs              # Procedural sphere mesh
    │   ├── LineRenderer.cs                # Grid and route lines
    │   └── CubeRenderer.cs                # Ship cube primitive
    ├── Services/
    │   ├── EdsmApiService.cs              # EDSM API client (legacy fallback)
    │   └── StarSystemDataService.cs       # Runtime data service
    └── UI/
        └── UiRenderer.cs                  # HUD, info cards, settings
```

## Project Dependencies

The star system data is split across three .NET 8.0 projects for MGCB compatibility:

```
StarSystemData.Models (.NET 8.0)  <-- Pure data models, no MonoGame dependency
         ^
         |
    +----+----+
    |         |
StarSystemData.Pipeline (.NET 8.0)    StarSystemData (.NET 8.0)
    |                                       |
    v                                       v
MGCB (Build Time)              EliteDangerousStarMap.Game (.NET 9.0)
```

## Core Systems

### 1. Star System Data Management

#### Data Flow (Build Time)
```
EDSM API ─────┐
              │──> StarSystemDataProcessor ──> Binary .xnb file
Local JSON ───┘
```

#### Data Flow (Runtime)
```
Content Pipeline (.xnb) ──> StarSystemDataReader ──> IStarSystemDataProvider ──> Game Logic
```

#### Key Interfaces

**IStarSystemDataProvider** - Runtime query interface
```csharp
public interface IStarSystemDataProvider
{
    IReadOnlyList<StarSystem> GetAllSystems();
    StarSystem? GetSystemByName(string name);
    StarSystem? GetSystemById(int id);
    IReadOnlyList<StarSystem> GetSystemsInSphere(Vector3 center, float radius);
    IReadOnlyList<StarSystem> GetSystemsInSphere(string systemName, float radius);
    IReadOnlyList<StarSystem> GetNearestSystems(Vector3 position, int count);
    IReadOnlyList<StarSystem> SearchByName(string partialName, int maxResults = 10);
}
```

### 2. Camera System

#### Camera Controller Hierarchy
```
IAnimatedCamera (interface)
    └── FpsCameraController
            ├── Position, Rotation, View/Projection matrices
            ├── FPS movement (WASD + mouse look)
            └── GetPickRay() for system selection
```

#### Animation Controller States
```
AnimationMode
├── UserControl        ─ User is actively controlling camera
├── WaitingForIdle     ─ Counting down to animation start
├── Animating          ─ Running registered animations
├── ReturningToControl ─ Smoothly transitioning back to user
├── FollowingTarget    ─ Following ship during flight
└── ReturningToFollow  ─ Transitioning back to follow mode
```

#### Animation Stack
Multiple animations can run simultaneously with weights:
```
Frame Update:
  OrbitAnimation.GetTransform()    * weight 1.0 ──┐
  ZoomAnimation.GetTransform()     * weight 0.5 ──┼──> Blended CameraTransform
  FlybyAnimation.GetTransform()    * weight 1.0 ──┘
```

### 3. Ship Flight System

#### Flight State Machine
```
FlightState
├── Idle          ─ Ship not moving
├── WarmingUp     ─ 2s preparation phase
├── Accelerating  ─ Power curve acceleration (ease-in)
├── Cruising      ─ Constant cruise speed
├── Decelerating  ─ Power curve deceleration (ease-out)
├── Arriving      ─ Scale animation into system
└── Cooldown      ─ 2s recharge before next jump
```

#### Speed Curve
```
Speed
  ^
  │      ┌──────────────┐
  │     /                \
  │    /                  \
  │   /                    \
  │──'                      '──
  └──────────────────────────> Time
   Accel    Cruise    Decel
```

### 4. Rendering Pipeline

#### 3D Rendering Order
1. Reference grid (LineRenderer)
2. Coordinate axes (LineRenderer)
3. Route lines to target/destination (LineRenderer)
4. Star spheres - player, selected, normal (SphereRenderer)
5. Ship cube if flying (CubeRenderer)

#### 2D UI Order
1. HUD (system count, position, animation status)
2. Flight status bar (if flying)
3. Flight log panel (if has entries)
4. System info card (if selected)
5. Settings screen (if open)
6. Control hints

### 5. Input Handling

#### Input Priority
1. Settings screen (blocks all other input when open)
2. UI button clicks (Fly To button)
3. Camera control (WASD, mouse)
4. System selection (left click)

#### Camera Control During Flight
- Mouse rotation: Rotates view but stays locked to ship
- WASD movement: Switches to free camera mode
- Idle timeout: Returns to follow mode

## Data Structures

### StarSystem (Runtime Model)
```csharp
public class StarSystem
{
    public string Name { get; set; }
    public int Id { get; set; }
    public Coordinates? Coords { get; set; }
    public bool RequirePermit { get; set; }
    public string? PermitName { get; set; }
    public bool CoordsLocked { get; set; }
    public float StarSize { get; set; }
    public bool IsSelected { get; set; }
    
    public Vector3 WorldPosition { get; }
    public float DistanceTo(StarSystem other);
}
```

### StarSystemRecord (Binary Serializable)
```csharp
public struct StarSystemRecord
{
    public int Id;
    public string Name;
    public float X, Y, Z;
    public bool RequirePermit;
    public string? PermitName;
    public bool CoordsLocked;
}
```

### CameraTransform
```csharp
public struct CameraTransform
{
    public Vector3 Position;
    public Vector3 LookAt;
    public Vector3 Up;
    public float Weight;
}
```

## Configuration

### Animation Settings (JSON persisted)
```json
{
    "IdleTimeout": 30.0,
    "AutoAnimationEnabled": true,
    "OrbitEnabled": true,
    "OrbitDuration": 20.0,
    "ZoomEnabled": true,
    "ZoomDuration": 10.0,
    "MinZoom": 30.0,
    "MaxZoom": 150.0,
    "FlybyEnabled": false,
    "FlybyDuration": 15.0
}
```

### Content Pipeline Properties
```xml
<!-- StarSystems.json content properties -->
<DataSource>ApiFirst | LocalFirst</DataSource>
<LocalFilePath>Content/StarSystems.json</LocalFilePath>
<ApiRadius>100</ApiRadius>
<ApiCenterSystem>Sol</ApiCenterSystem>
```

## Extension Points

### Custom Animations
Implement `ICameraAnimation`:
```csharp
public class MyAnimation : ICameraAnimation
{
    public bool IsComplete { get; }
    public float Weight { get; }
    public void Reset(IAnimatedCamera camera, Vector3 focusPoint);
    public CameraTransform GetTransform(GameTime gameTime);
}
```

### Custom Data Providers
Implement `IStarSystemDataProvider` for alternative data sources (e.g., SQLite, remote API caching).

### Multi-Camera Support
Register multiple cameras with AnimationController:
```csharp
_animationController.SetTargetCamera(mainCamera);
// Switch for split-screen
_animationController.SetTargetCamera(player2Camera);
```

## Build Process

1. **dotnet restore** - Restore NuGet packages
2. **dotnet build** - Build all projects including content pipeline
3. Content pipeline runs during build:
   - Fetches data from EDSM API (or uses local file)
   - Processes into binary format
   - Outputs to Content/bin/

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| MonoGame.Framework.DesktopGL | 3.8.* | Game framework |
| MonoGame.Content.Builder.Task | 3.8.* | Content pipeline |
| Newtonsoft.Json | 13.0.4 | JSON serialization |

## Threading Model

- **Main Thread**: Game loop, rendering, input
- **Async/Task**: Initial data loading from API/content
- **Content Pipeline**: Runs during build, separate from runtime

## Error Handling

- API failures: Fall back to sample data
- Missing content: Generate procedural sample systems
- Invalid input: Ignored with debug logging
- Disposed objects: Null checks throughout
