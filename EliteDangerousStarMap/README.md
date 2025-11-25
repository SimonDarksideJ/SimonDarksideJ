# Elite Dangerous Star Map

A MonoGame DesktopGL application that visualizes the Elite Dangerous universe using data from the [EDSM API](https://www.edsm.net/api-v1/).

## Features

- **3D Star Map Visualization**: View star systems from the Elite Dangerous universe rendered as spheres in 3D space
- **FPS-Style Navigation**: Navigate through space using standard WASD controls and mouse look
- **Interactive System Selection**: Click on star systems to select them and view information
- **Route Visualization**: See a route line drawn from your current system to the selected target
- **System Info Cards**: View system details including name, coordinates, and distance

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
| ESC | Exit |

## Requirements

- .NET 8.0 or later
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
├── EliteDangerousStarMap.Game/
│   ├── Content/          # Game content (fonts, etc.)
│   ├── Input/            # Input handling (camera controller)
│   ├── Models/           # Data models (StarSystem, PlayerShip)
│   ├── Rendering/        # 3D rendering (spheres, lines)
│   ├── Services/         # API services (EDSM)
│   ├── UI/               # User interface rendering
│   ├── Game1.cs          # Main game class
│   ├── StarMapManager.cs # Star map orchestration
│   └── Program.cs        # Entry point
└── EliteDangerousStarMap.sln
```

## License

This project is for educational purposes. Elite Dangerous is © Frontier Developments.
EDSM data is provided by the EDSM community.
