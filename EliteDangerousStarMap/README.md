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

## API Data Source

This application uses the [Elite Dangerous Star Map (EDSM)](https://www.edsm.net/) API:
- `/api-v1/systems` - Get list of star systems
- `/api-v1/system` - Get details about a specific system
- `/api-v1/sphere` - Get systems within a radius of a reference system

## Project Structure

```
EliteDangerousStarMap/
├── CameraAnimation/              # Reusable camera animation library
│   ├── Interfaces/               # IAnimatedCamera, ICameraAnimation
│   ├── Animations/               # Orbit, Flyby, Zoom, ShipFollow, ReturnToControl
│   ├── Core/                     # AnimationController, AnimationSettings
│   └── README.md                 # Library documentation
├── EliteDangerousStarMap.Game/
│   ├── Content/                  # Game content (fonts, etc.)
│   ├── Input/                    # Input handling (camera controller)
│   ├── Models/                   # Data models (StarSystem, PlayerShip, FlightLog, ShipFlightController)
│   ├── Rendering/                # 3D rendering (spheres, lines, cubes)
│   ├── Services/                 # API services (EDSM)
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

## License

This project is for educational purposes. Elite Dangerous is © Frontier Developments.
EDSM data is provided by the EDSM community.
