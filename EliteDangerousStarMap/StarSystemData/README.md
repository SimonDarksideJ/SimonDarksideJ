# StarSystemData Runtime Library

A MonoGame runtime library for querying star system data loaded from the content pipeline.

## Projects Overview

The star system data functionality is split across three .NET projects for MGCB compatibility:

- **StarSystemData.Models** (.NET 8.0) - Core data models, consumed by MGCB and runtime
- **StarSystemData.Pipeline** (.NET 8.0) - Content pipeline extension for build-time processing
- **StarSystemData** (.NET 8.0) - Runtime components including ContentTypeReader and query providers

## Features

- **Build-time Data Processing**: Processes star system data during content build, not at runtime
- **Fallback Support**: Uses local JSON file if API is unavailable
- **Configurable Data Source**: Choose between API-first or Local-first modes
- **Efficient Binary Format**: Optimized for fast loading at runtime
- **Spatial Queries**: Built-in support for sphere and nearest-neighbor queries
- **No Runtime API Dependency**: Game works completely offline after build

## Installation

1. Add reference to `StarSystemData.Models` and `StarSystemData` projects in your game project
2. Build `StarSystemData.Pipeline` to get the DLLs for MGCB
3. Add references in Content.mgcb file:

```
/reference:../../StarSystemData.Pipeline/bin/Release/net8.0/StarSystemData.Pipeline.dll
/reference:../../StarSystemData.Models/bin/Release/net8.0/StarSystemData.Models.dll
/reference:../../StarSystemData.Pipeline/bin/Release/net8.0/Newtonsoft.Json.dll
```

4. Add your star system JSON file to content

## Configuration

### DataSourceMode Enum

```csharp
public enum DataSourceMode
{
    ApiFirst,   // Try API first, fall back to local file
    LocalFirst  // Use local file, ignore API
}
```

### Content Pipeline Properties

In your Content.mgcb file:

```
#begin StarSystems.json
/importer:StarSystemDataImporter
/processor:StarSystemDataProcessor
/processorParam:DataSource=LocalFirst
/processorParam:LocalFilePath=StarSystems.json
/processorParam:ApiCenterSystem=Sol
/processorParam:ApiRadius=100
/build:StarSystems.json
```

## Usage

### Loading Data at Runtime

```csharp
// In LoadContent
var database = Content.Load<StarSystemDatabase>("StarSystems");
var provider = new ContentStarSystemProvider(database);

// Query systems
var sol = provider.GetSystemByName("Sol");
var nearby = provider.GetSystemsInSphere("Sol", 50);
var nearest = provider.GetNearestSystems(position, 10);
```

### IStarSystemDataProvider Interface

```csharp
public interface IStarSystemDataProvider
{
    IReadOnlyList<StarSystemInfo> GetAllSystems();
    StarSystemInfo? GetSystemByName(string name);
    StarSystemInfo? GetSystemById(int id);
    IReadOnlyList<StarSystemInfo> GetSystemsInSphere(Vector3 center, float radius);
    IReadOnlyList<StarSystemInfo> GetSystemsInSphere(string systemName, float radius);
    IReadOnlyList<StarSystemInfo> GetNearestSystems(Vector3 position, int count);
    IReadOnlyList<StarSystemInfo> SearchByName(string partialName, int maxResults = 10);
}
```

## Local JSON File Format

The local fallback file should be an array of star system objects matching the EDSM API format:

```json
[
    {
        "name": "Sol",
        "id": 27,
        "coords": { "x": 0, "y": 0, "z": 0 },
        "requirePermit": true,
        "permitName": "Sol",
        "coordsLocked": false
    },
    {
        "name": "Alpha Centauri",
        "id": 28,
        "coords": { "x": 3.03, "y": -0.08, "z": 3.15 },
        "requirePermit": false,
        "coordsLocked": false
    }
]
```

## Binary Format

The compiled .xnb file contains:

1. System count (int32)
2. For each system:
   - Id (int32)
   - Name (string with length prefix)
   - X, Y, Z coordinates (float32 × 3)
   - RequirePermit (bool)
   - PermitName (nullable string)
   - CoordsLocked (bool)

## Extension Points

### Custom Data Sources

Implement `IStarSystemDataProvider` for alternative data sources:

```csharp
public class SqliteStarSystemProvider : IStarSystemDataProvider
{
    // Implementation for SQLite database
}
```

### Additional Processing

Override `StarSystemDataProcessor` to add custom processing:

```csharp
public class MyProcessor : StarSystemDataProcessor
{
    protected override void PostProcess(StarSystemDatabase database)
    {
        // Add custom processing logic
    }
}
```

## Dependencies

- MonoGame.Framework.DesktopGL 3.8+
- MonoGame.Framework.Content.Pipeline 3.8+ (for Pipeline project)
- Newtonsoft.Json 13.0+
- .NET 8.0 (for MGCB compatibility)
