# BudgetDungeon Map Exporter

Gothic-themed tool to convert .amd map files to BudgetDungeon format (JSON grid data).

![Gothic UI](https://img.shields.io/badge/UI-Gothic%20Theme-8B4513)
![Raylib](https://img.shields.io/badge/Raylib-7.0.2-red)
![.NET](https://img.shields.io/badge/.NET-10.0-purple)

## Features

- 🎨 **Gothic UI** - Dark, medieval-themed interface
- 🗺️ **Visual Map Preview** - See tile types at a glance
- 📦 **BudgetDungeon Format** - Direct JSON export for your game
- ⚡ **CLI Mode** - Batch export without GUI
- 🎯 **Tile Type Mapping** - WALKABLE, BLOCKED, TELEPORT, FARM, WATER

## Quick Start

### GUI Mode
```bash
dotnet run
# TAB to open export panel
# Select map and click Export
```

### CLI Mode
```bash
# Single map
dotnet run -- --export arefarm ~/output

# All maps
./export-all-maps.sh ~/BudgetDungeon.Server/resources/maps
```

## Output Format

### JSON Structure
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

**Note**: Only non-walkable tiles are exported. All other tiles default to WALKABLE.

### Tile Type Mapping

| .amd Property | BudgetDungeon Type | Priority |
|---------------|-------------------|----------|
| IsTeleport | TELEPORT (2) | 1 (highest) |
| IsWater | WATER (4) | 2 |
| IsFarmingAllowed | FARM (3) | 3 |
| !IsMoveAllowed | BLOCKED (1) | 4 |
| (default) | WALKABLE (0) | 5 (default) |

## Integration with BudgetDungeon

```csharp
// 1. Load JSON
var json = File.ReadAllText("arefarm.json");
var data = JsonSerializer.Deserialize<BudgetDungeonMapData>(json);

// 2. Create grid (all tiles start as WALKABLE)
var grid = new Grid(new GridCreateParams(
    Width: data.Width,
    Height: data.Height,
    Origin: Vector2.Zero,
    CreateDefaultTiles: true
));

// 3. Apply collision tiles
foreach (var tile in data.Tiles)
{
    grid.SetTile(new SetTileParams(
        Coord: new GridCoord(tile.X, tile.Y),
        Type: (TileType)tile.Type,
        Gid: 0
    ));
}

// 4. Use in WorldMap
var worldMap = new WorldMap(new MapData { Id = data.Id, Grid = grid });
```

## Available Maps

- 2ndmiddle (250x250)
- arefarm (250x250)
- aresden (300x300)
- default (170x170)
- elvfarm (250x250)
- elvine (250x250)
- huntzone1 (250x250)
- huntzone2 (250x250)

## GUI Controls

| Key | Action |
|-----|--------|
| TAB | Toggle export panel |
| ESC | Close export panel |
| Mouse | Select map, click export |

## Map Analysis

```bash
./analyze-map.sh /path/to/exported/map.json
```

Output:
```
Map ID: arefarm
Dimensions: 250x250 tiles
Total Tiles: 62500

Tile Type Distribution:
WALKABLE:    31168 ( 49.9%)
BLOCKED:     27969 ( 44.8%)
TELEPORT:       48 (  0.1%)
FARM:         3315 (  5.3%)
WATER:           0 (  0.0%)

File Size: 2.1M
```

## Project Structure

```
IsometricMapViewer/
├── resources/
│   ├── maps/          # .amd map files
│   └── sprites/       # .spr sprite files (for preview)
├── src/
│   ├── Core/
│   │   ├── ExporterApp.cs           # Main application
│   │   ├── BudgetDungeonExporter.cs # Export logic
│   │   ├── Map.cs                   # Map loading
│   │   └── Constants.cs
│   ├── UI/
│   │   ├── GothicUI.cs              # Gothic UI components
│   │   ├── ColorKeys.cs             # Gothic color palette
│   │   └── UIConfig.cs              # UI constants
│   └── Loaders/                     # Map/sprite loaders
├── Program.cs                       # Entry point
├── export-all-maps.sh              # Batch export script
└── analyze-map.sh                  # Map analysis tool
```

## Gothic UI Theme

The exporter uses a dark, medieval-inspired color palette:

- **Background**: Deep stone grays (#28282E)
- **Accents**: Muted gold (#C8B464)
- **Text**: Bone white (#C8BEA0)
- **Borders**: Weathered stone (#5A5A64)

## Performance

- **CLI Export**: ~1 second per map (JSON only)
- **GUI Export**: ~1-2 seconds per map
- **Batch Export**: Processes all 8 maps in ~10 seconds

## Requirements

- .NET 10.0
- Raylib-cs 7.0.2
- Linux/Windows/macOS

## Building

```bash
dotnet build
dotnet run
```

## Tips

1. **Space-Efficient**: Only non-walkable tiles are exported, keeping JSON files small
2. **Visual Preview**: Use GUI mode to see tile distribution before export
3. **Batch Processing**: Use CLI mode for automated exports
4. **Analysis**: Run `analyze-map.sh` to see tile statistics

## License

MIT

## Credits

- Built with [Raylib](https://www.raylib.com/)
- Gothic UI inspired by classic RPGs
- Designed for [BudgetDungeon](https://github.com/yourusername/BudgetDungeon)
