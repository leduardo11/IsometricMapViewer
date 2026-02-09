# BudgetDungeon Map Exporter

Simple Gothic-themed tool to export .amd maps to BudgetDungeon JSON format.

## Quick Start

1. **Configure** - Edit `appsettings.json`:
```json
{
  "MapExporter": {
    "MapName": "arefarm",
    "OutputPath": "/home/leduardo/exported-maps",
    "ShowObjects": true,
    "ShowGrid": false
  }
}
```

2. **Run**:
```bash
dotnet run
```

3. **Export** - Click "Export Map" button or press `E`

## Controls

| Key/Action | Function |
|------------|----------|
| **E** | Quick export |
| **F** | Fit map to screen |
| **TAB** | Toggle UI |
| **WASD / Arrows** | Pan camera |
| **Mouse Drag** | Pan camera |
| **Mouse Wheel** | Zoom in/out |

## UI Buttons

- **Export Map** - Export current map to JSON
- **Show/Hide Objects** - Toggle object layer visibility
- **Show/Hide Grid** - Toggle grid overlay
- **Fit Map** - Reset camera to fit entire map

## Output Format

```json
{
  "id": "arefarm",
  "width": 250,
  "height": 250,
  "tileSize": 32,
  "tiles": [
    { "x": 10, "y": 5, "type": "BLOCKED" },
    { "x": 15, "y": 20, "type": "TELEPORT" }
  ]
}
```

**Tile Types**: WALKABLE (0), BLOCKED (1), TELEPORT (2), FARM (3), WATER (4)

Only non-walkable tiles are exported (space-efficient).

## CLI Export

For batch processing without GUI:

```bash
dotnet run -- --export <mapname> [output-path]
```

Example:
```bash
dotnet run -- --export arefarm ~/maps
```

## Integration with BudgetDungeon

```csharp
var json = File.ReadAllText("arefarm.json");
var data = JsonSerializer.Deserialize<BudgetDungeonMapData>(json);

var grid = new Grid(new GridCreateParams(
    Width: data.Width,
    Height: data.Height,
    Origin: Vector2.Zero,
    CreateDefaultTiles: true
));

foreach (var tile in data.Tiles)
{
    grid.SetTile(new SetTileParams(
        Coord: new GridCoord(tile.X, tile.Y),
        Type: (TileType)tile.Type,
        Gid: 0
    ));
}
```

## Features

- ✅ Full sprite rendering with camera controls
- ✅ Gothic-themed UI
- ✅ Configure via appsettings.json
- ✅ Fast single-map loading
- ✅ Toggle objects/grid visibility
- ✅ Export to BudgetDungeon JSON format
- ✅ CLI mode for automation

## Requirements

- .NET 10.0
- Raylib-cs 7.0.2

## Building

```bash
dotnet build
dotnet run
```

## Available Maps

Place .amd files in `resources/maps/`:
- 2ndmiddle, arefarm, aresden, default
- elvfarm, elvine, huntzone1, huntzone2

Change `MapName` in `appsettings.json` to load different maps.
