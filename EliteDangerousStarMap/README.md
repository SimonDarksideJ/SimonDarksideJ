# Elite Dangerous Star Map

A MonoGame DesktopGL application that visualizes the Elite Dangerous universe using data from the [EDSM API](https://www.edsm.net/api-v1/).

## Features

- **3D Star Map Visualization**: View star systems from the Elite Dangerous universe rendered as spheres in 3D space
- **FPS-Style Navigation**: Navigate through space using standard WASD controls and mouse look
- **Interactive System Selection**: Click on star systems to select them and view information
- **Route Visualization**: See a route line drawn from your current system to the selected target
- **System Info Cards**: View system details including name, coordinates, and distance with "Fly To" button
- **Ship Flight System**: Fly between star systems with realistic power curve movement
- **Animated Camera System**: Auto-camera with orbit, flyby, zoom, and ship follow animations
- **Flight Log**: View real-time status updates and random events during flight
- **Settings Screen**: Configure animation settings via Tab key

## Controls

| Control | Action |
|---------|--------|
| W/A/S/D | Move forward/left/backward/right |
| Space | Move up |
| Q | Move down |
| Right Mouse + Drag | Look around |
| Mouse Scroll | Zoom in/out |
| Left Click | Select star system |
| Shift | Move faster |
| Ctrl | Move slower |
| Tab | Open settings screen |
| ESC | Exit |

## Ship Flight System

Click on a star system to select it, then click the **"Fly To"** button to start a journey.

### Flight Mechanics

- **Power Curve Speed**: Ship accelerates smoothly, reaches cruise speed, then decelerates on approach
- **Visual Ship**: A cube represents the ship during flight (placeholder for future 3D model)
- **Scale Animation**: Ship scales up when departing and down when arriving for a hyperspace effect
- **Cooldown**: 2-second recharge between jumps

### Flight Log

During flight, a log panel shows:
- Status updates (warming up, accelerating, cruising, decelerating)
- Random events (space whales, pirates, nebulae sightings, etc.)
- Arrival confirmation

Flight logs are automatically saved to `FlightLogs/` directory.

### Camera During Flight

When flying, the camera enters **Follow Mode** with multiple view styles:
- **Centered**: Ship stays in center of view
- **Orbit**: Camera slowly orbits the ship
- **Third-Person**: Camera follows behind the ship
- **Zoom Pulse**: Gradual zoom in/out effect

You can still control the camera during flight. After idle timeout, camera returns to follow mode.

## Animated Camera System

When the camera is idle (no user input) for a configurable timeout (default 30 seconds), the camera automatically switches to an animated mode featuring:

- **Orbit**: Camera smoothly orbits around the focus point
- **Zoom**: Camera zooms in and out in a rhythmic pattern
- **Flyby**: Camera performs cinematic flyby passes (optional)

These animations can be **stacked** to create combined effects. When user input is detected, the camera smoothly transitions back to manual control.

### Configuring Animations

Press **Tab** to open the settings screen where you can:
- Adjust idle timeout (Up/Down arrows)
- Toggle auto-animation (A key)
- Save settings (Enter)

## Gameplay Flow

1. **Explore**: Navigate the star map and look at different systems
2. **Select**: Click on a system to see its info card and route
3. **Fly**: Click "Fly To" to start the journey
4. **Watch**: Camera follows the ship with cinematic animations
5. **Arrive**: Ship arrives at destination, camera returns to normal
6. **Repeat**: Select a new target and continue exploring

## Requirements

- .NET 9.0 or later
- MonoGame 3.8 or later

## Building

```bash
cd EliteDangerousStarMap
dotnet build
```

## Running

```bash
dotnet run --project EliteDangerousStarMap.Game
```

## Data Source

### Content Pipeline (Preferred)

Star system data is now compiled at build time using a custom content pipeline extension. This provides:

- **Offline Support**: No internet connection required at runtime
- **Faster Loading**: Pre-compiled binary format loads instantly
- **Data Source Options**:
  - `ApiFirst`: Fetch from EDSM API at build time, fallback to local JSON
  - `LocalFirst`: Use local JSON file only (for offline builds)

Configure in `Content/Content.mgcb`:
```
/processorParam:DataSource=ApiFirst
/processorParam:LocalFilePath=StarSystems.json
/processorParam:ApiCenterSystem=Sol
/processorParam:ApiRadius=100
```

### Fallback to Runtime API

If content pipeline data is not available, the game falls back to the [EDSM API](https://www.edsm.net/api-v1/):
- `/api-v1/systems` - Get list of star systems
- `/api-v1/system` - Get details about a specific system
- `/api-v1/sphere` - Get systems within a radius

## Project Structure

```
EliteDangerousStarMap/
├── ARCHITECTURE.md               # Detailed design documentation
├── CameraAnimation/              # Reusable camera animation library
│   ├── Interfaces/               # IAnimatedCamera, ICameraAnimation
│   ├── Animations/               # Orbit, Flyby, Zoom, ShipFollow, ReturnToControl
│   ├── Core/                     # AnimationController, AnimationSettings
│   └── README.md                 # Library documentation
├── StarSystemData/               # Content pipeline extension library
│   ├── Models/                   # StarSystemRecord, StarSystemDatabase
│   ├── Pipeline/                 # Importer, Processor, Writer
│   ├── Runtime/                  # Reader, IStarSystemDataProvider
│   └── README.md                 # Library documentation
├── StarSystemData.Tests/         # Unit tests for data provider
├── EliteDangerousStarMap.Game/
│   ├── Content/                  # Game content (fonts, star systems data)
│   │   ├── Content.mgcb          # Content builder configuration
│   │   └── StarSystems.json      # Local fallback data
│   ├── Input/                    # Input handling (camera controller)
│   ├── Models/                   # Data models (StarSystem, PlayerShip, etc.)
│   ├── Rendering/                # 3D rendering (spheres, lines, cubes)
│   ├── Services/                 # Data services (StarSystemDataService, EdsmApiService)
│   ├── UI/                       # User interface rendering
│   ├── Game1.cs                  # Main game class
│   ├── StarMapManager.cs         # Star map orchestration
│   └── Program.cs                # Entry point
└── EliteDangerousStarMap.sln
```

## Camera Animation Library

The `CameraAnimation` library is a reusable component that can be used in any MonoGame project. It supports:

- Multiple cameras (e.g., main camera, missile camera, split-screen)
- Custom animation types via `ICameraAnimation` interface
- Stackable animations with weight-based blending
- Smooth return-to-control transitions
- **Ship Follow Mode** for tracking moving targets

See [CameraAnimation/README.md](CameraAnimation/README.md) for detailed documentation and extension points.

## Star System Data Library

The `StarSystemData` library provides:

- **Build-time Data Fetching**: Retrieves data from EDSM API during content build
- **Efficient Binary Format**: Fast loading at runtime
- **Spatial Queries**: Sphere searches, nearest neighbors
- **Offline Operation**: No API calls needed at runtime

See [StarSystemData/README.md](StarSystemData/README.md) for usage and extension points.

## Running Tests

```bash
dotnet test StarSystemData.Tests
```

## License

This project is for educational purposes. Elite Dangerous is © Frontier Developments.
EDSM data is provided by the EDSM community.
