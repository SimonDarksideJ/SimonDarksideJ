# Elite Dangerous Star Map - Setup & Usage Instructions

This document provides comprehensive instructions for setting up, building, running, and extending the Elite Dangerous Star Map project.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Project Overview](#project-overview)
3. [Getting Started](#getting-started)
4. [Building the Project](#building-the-project)
5. [Running the Application](#running-the-application)
6. [Usage Guide](#usage-guide)
7. [Configuration](#configuration)
8. [Adding More Star Systems](#adding-more-star-systems)
9. [Extending the Project](#extending-the-project)
10. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Software

| Software | Version | Download |
|----------|---------|----------|
| .NET SDK | 9.0+ | [Download](https://dotnet.microsoft.com/download) |
| Git | Latest | [Download](https://git-scm.com/) |

### Optional Software

| Software | Purpose | Download |
|----------|---------|----------|
| Visual Studio 2022 | Full IDE experience | [Download](https://visualstudio.microsoft.com/) |
| VS Code | Lightweight editor | [Download](https://code.visualstudio.com/) |
| MonoGame MGCB Editor | Content editing | Installed via `dotnet tool` |

### Verify Prerequisites

```bash
# Check .NET version (should be 9.0+)
dotnet --version

# Check Git
git --version
```

---

## Project Overview

### Solution Structure

```
EliteDangerousStarMap/
├── EliteDangerousStarMap.sln              # Main solution file
├── ARCHITECTURE.md                         # Design documentation
├── INSTRUCTIONS.md                         # This file
├── README.md                               # User documentation
│
├── EliteDangerousStarMap.Game/            # Main game (.NET 9.0)
├── CameraAnimation/                        # Camera animation library (.NET 9.0)
├── StarSystemData/                         # Runtime data library (.NET 8.0)
├── StarSystemData.Models/                  # Data models (.NET 8.0)
├── StarSystemData.Pipeline/                # Content pipeline (.NET 8.0)
└── StarSystemData.Tests/                   # Unit tests (.NET 8.0)
```

### Project Dependencies

```
StarSystemData.Models (.NET 8.0)  ←── Core data models
         ↑
    ┌────┴────┐
    │         │
StarSystemData.Pipeline   StarSystemData
    (.NET 8.0)              (.NET 8.0)
         ↓                       ↓
     MGCB (Build)     EliteDangerousStarMap.Game
                              (.NET 9.0)
                                  ↑
                          CameraAnimation
                              (.NET 9.0)
```

---

## Getting Started

### 1. Clone the Repository

```bash
# Clone the repository
git clone https://github.com/SimonDarksideJ/SimonDarksideJ.git
cd SimonDarksideJ/EliteDangerousStarMap

# Or if migrated to dedicated repository
git clone https://github.com/SimonDarksideJ/MonoGameEliteDangerousStarMap.git
cd MonoGameEliteDangerousStarMap
```

### 2. Restore Dependencies

```bash
# Restore all NuGet packages
dotnet restore
```

### 3. Install MonoGame Tools (Optional)

```bash
# Install MGCB Editor for content management
dotnet tool restore

# Or install globally
dotnet tool install -g dotnet-mgcb-editor
```

---

## Building the Project

### Standard Build

```bash
# Build in Debug mode
dotnet build

# Build in Release mode
dotnet build -c Release
```

### Build Order (Important)

The content pipeline requires the StarSystemData libraries to be built first:

```bash
# Step 1: Build data models (required for MGCB)
dotnet build StarSystemData.Models -c Release

# Step 2: Build content pipeline extension
dotnet build StarSystemData.Pipeline -c Release

# Step 3: Build runtime library
dotnet build StarSystemData -c Release

# Step 4: Build camera animation library
dotnet build CameraAnimation -c Release

# Step 5: Build main game (this also runs content pipeline)
dotnet build EliteDangerousStarMap.Game -c Release
```

### Clean Build

```bash
# Clean all build artifacts
dotnet clean

# Full clean and rebuild
dotnet clean && dotnet build
```

### Content Pipeline Build

The content pipeline processes `StarSystems.json` during build. To rebuild content:

```bash
# Delete content cache
rm -rf EliteDangerousStarMap.Game/Content/bin
rm -rf EliteDangerousStarMap.Game/Content/obj

# Rebuild
dotnet build EliteDangerousStarMap.Game
```

---

## Running the Application

### Run from Command Line

```bash
# Run in Debug mode
dotnet run --project EliteDangerousStarMap.Game

# Run in Release mode (better performance)
dotnet run --project EliteDangerousStarMap.Game -c Release
```

### Run from Visual Studio

1. Open `EliteDangerousStarMap.sln` in Visual Studio
2. Set `EliteDangerousStarMap.Game` as the startup project
3. Press F5 to run with debugging, or Ctrl+F5 to run without

### Run Unit Tests

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test -v n

# Run specific test project
dotnet test StarSystemData.Tests
```

---

## Usage Guide

### Controls

| Control | Action |
|---------|--------|
| **W** | Move forward |
| **S** | Move backward |
| **A** | Strafe left |
| **D** | Strafe right |
| **Space** | Move up |
| **Q** | Move down |
| **Shift** | Move faster |
| **Ctrl** | Move slower |
| **Right Mouse + Drag** | Look around |
| **Mouse Scroll** | Zoom in/out |
| **Left Click** | Select star system |
| **Tab** | Open settings screen |
| **ESC** | Exit application |

### Basic Workflow

1. **Launch**: Run the application to see the 3D star map centered on Sol

2. **Navigate**: Use WASD + mouse to move through the star field

3. **Select a System**: Left-click on any star to:
   - Highlight it (yellow color)
   - Show system info card with name, coordinates, distance
   - Display route line from current position

4. **Fly to System**: Click the "Fly To" button in the info card to:
   - Start ship travel animation
   - Watch the ship fly between systems
   - View flight log with status updates

5. **Camera Animations**: Stop interacting for 30 seconds (configurable):
   - Camera enters automated animation mode
   - Press any key or move mouse to regain control

### Settings Screen (Tab Key)

Press **Tab** to open settings:

- **Idle Timeout**: Time before auto-animation (5-120 seconds)
- **Auto Animation**: Enable/disable automatic camera animations
- **Up/Down Arrows**: Adjust timeout value
- **A Key**: Toggle auto-animation
- **Enter**: Save settings
- **Tab or Escape**: Close settings

---

## Configuration

### Animation Settings

Settings are persisted in `animation_settings.json`:

```json
{
  "IdleTimeout": 30,
  "AutoAnimationEnabled": true,
  "OrbitDuration": 20,
  "ZoomDuration": 10,
  "OrbitEnabled": true,
  "ZoomEnabled": true,
  "MinZoom": 30,
  "MaxZoom": 150
}
```

### Content Pipeline Configuration

Edit `EliteDangerousStarMap.Game/Content/Content.mgcb`:

```
#begin StarSystems.json
/importer:StarSystemDataImporter
/processor:StarSystemDataProcessor
/processorParam:DataSource=LocalFirst      # LocalFirst or ApiFirst
/processorParam:LocalFilePath=StarSystems.json
/processorParam:ApiCenterSystem=Sol
/processorParam:ApiRadius=100
/build:StarSystems.json
```

| Parameter | Description | Values |
|-----------|-------------|--------|
| `DataSource` | Where to get star data | `LocalFirst`, `ApiFirst` |
| `LocalFilePath` | Path to local JSON file | Relative to Content folder |
| `ApiCenterSystem` | Center system for API query | Any valid system name |
| `ApiRadius` | Radius in light-years | 10-500 recommended |

---

## Adding More Star Systems

### Option 1: Download from EDSM

1. Visit [EDSM API](https://www.edsm.net/api-v1/sphere)
2. Query systems: `https://www.edsm.net/api-v1/sphere?systemName=Sol&radius=200`
3. Save response as `StarSystems.json` in `Content/` folder

### Option 2: Edit Local JSON

Edit `EliteDangerousStarMap.Game/Content/StarSystems.json`:

```json
[
    {
        "name": "New System",
        "id": 99999,
        "coords": { "x": 100.5, "y": -50.2, "z": 75.8 },
        "requirePermit": false,
        "coordsLocked": false
    }
]
```

### Option 3: Use API at Build Time

Set `DataSource=ApiFirst` in Content.mgcb to fetch fresh data during build.

---

## Extending the Project

### Adding Custom Camera Animations

1. Create a new animation class in `CameraAnimation/Animations/`:

```csharp
public class SpinAnimation : BaseCameraAnimation
{
    public override string Name => "Spin";
    public override float Duration => 10f;
    
    public float SpinSpeed { get; set; } = 1f;
    
    protected override CameraTransform CalculateTransform(
        GameTime gameTime, IAnimatedCamera camera)
    {
        float angle = _elapsedTime * SpinSpeed;
        var offset = new Vector3(
            MathF.Cos(angle) * 100,
            0,
            MathF.Sin(angle) * 100);
            
        return new CameraTransform
        {
            Position = _focusPoint + offset,
            LookAt = _focusPoint,
            Up = Vector3.Up,
            Weight = 1f
        };
    }
}
```

2. Register in `StarMapManager.cs`:

```csharp
_animationController.RegisterAnimation(new SpinAnimation { SpinSpeed = 0.5f });
```

### Adding Custom Data Providers

Implement `IStarSystemDataProvider` for alternative data sources:

```csharp
public class DatabaseStarSystemProvider : IStarSystemDataProvider
{
    private readonly SqliteConnection _connection;
    
    public StarSystemInfo? GetSystemByName(string name)
    {
        // Query database
    }
    
    // Implement other interface methods...
}
```

### Adding New Ship Models

Replace the cube primitive in `StarMapManager.cs`:

```csharp
// Instead of CubeRenderer, load a 3D model
_shipModel = Content.Load<Model>("ShipModel");

// In Draw method
Matrix world = Matrix.CreateScale(0.1f) 
    * Matrix.CreateRotationY(_shipRotation)
    * Matrix.CreateTranslation(_flightController.ShipPosition);
    
foreach (var mesh in _shipModel.Meshes)
{
    mesh.Draw();
}
```

---

## Troubleshooting

### Build Errors

#### "Could not find StarSystemData.Pipeline.dll"

The content pipeline DLLs must be built before the game:

```bash
dotnet build StarSystemData.Models -c Release
dotnet build StarSystemData.Pipeline -c Release
dotnet build EliteDangerousStarMap.Game
```

#### "Content pipeline failed to process StarSystems.json"

1. Check that `StarSystems.json` exists in `Content/` folder
2. Verify JSON is valid (use a JSON validator)
3. Clean and rebuild content:

```bash
rm -rf EliteDangerousStarMap.Game/Content/bin
rm -rf EliteDangerousStarMap.Game/Content/obj
dotnet build
```

### Runtime Errors

#### "Could not load font 'DefaultFont'"

Content hasn't been built:

```bash
dotnet build EliteDangerousStarMap.Game
```

#### "No star systems loaded"

1. Check that `StarSystems.json` exists and is valid
2. Try rebuilding content pipeline:

```bash
dotnet clean
dotnet build -c Release
```

### Performance Issues

#### Low Frame Rate

1. Run in Release mode: `dotnet run -c Release`
2. Reduce star count in `StarSystems.json`
3. Check that you're not in Debug mode in Visual Studio

#### High Memory Usage

1. Reduce star system count
2. Lower sphere detail in `SphereRenderer.cs`
3. Disable unused animations

### Common Issues

| Issue | Solution |
|-------|----------|
| Black screen on launch | Check GPU drivers, rebuild content |
| No stars visible | Check camera position, zoom out |
| Settings not saving | Check file permissions for `animation_settings.json` |
| Flight log not saving | Check/create `FlightLogs/` directory permissions |

---

## Development Tips

### Debugging Content Pipeline

Add console output to `StarSystemDataProcessor.cs`:

```csharp
Console.WriteLine($"Processing {systems.Count} systems...");
```

Then run build to see output:

```bash
dotnet build 2>&1 | grep "Processing"
```

### Hot Reload (Visual Studio)

1. Enable Hot Reload in Visual Studio 2022+
2. Make changes to non-structural code
3. Changes apply without restart

### Performance Profiling

Use Visual Studio Profiler or `dotnet-trace`:

```bash
dotnet tool install -g dotnet-trace
dotnet-trace collect -- dotnet run --project EliteDangerousStarMap.Game
```

---

## API Reference

### EDSM API Endpoints

| Endpoint | Purpose | Example |
|----------|---------|---------|
| `/api-v1/systems` | List systems | `?systemName=Sol` |
| `/api-v1/system` | System details | `?systemName=Sol&showCoordinates=1` |
| `/api-v1/sphere` | Systems in radius | `?systemName=Sol&radius=50` |

### Key Classes

| Class | Purpose | Location |
|-------|---------|----------|
| `Game1` | MonoGame entry point | `EliteDangerousStarMap.Game/` |
| `StarMapManager` | Core orchestration | `EliteDangerousStarMap.Game/` |
| `FpsCameraController` | Camera with FPS controls | `Input/` |
| `AnimationController` | Animation management | `CameraAnimation/Core/` |
| `ShipFlightController` | Ship movement | `Models/` |
| `UiRenderer` | UI drawing | `UI/` |

---

## License

This project is for educational purposes. Elite Dangerous is © Frontier Developments.
EDSM data is provided by the EDSM community.

---

## Support

- **Issues**: Open a GitHub issue
- **Documentation**: See [ARCHITECTURE.md](ARCHITECTURE.md) for design details
- **API Docs**: [EDSM API](https://www.edsm.net/api)
