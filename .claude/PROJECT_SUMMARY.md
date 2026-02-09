# BudgetDungeon Map Exporter - Project Summary

## What Was Built

A Gothic-themed map exporter tool that converts .amd map files to BudgetDungeon format (JSON grid + PNG image).

## Key Features

### 1. Dual Export System
- **Export Grid (G)** - JSON collision data for server
- **Export PNG (P)** - Visual map image for client

### 2. Full Map Rendering
- Renders actual sprites and textures (not just colored tiles)
- Camera controls: pan (WASD/mouse), zoom (wheel), fit (F)
- Toggle objects and grid visibility

### 3. Configuration via appsettings.json
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

### 4. Gothic UI Theme
- Dark medieval color palette
- Non-intrusive (toggle with TAB)
- Clean button interface
- Top info bar + right panel + bottom help

## Technology Stack

- **.NET 10.0**
- **Raylib-cs 7.0.2** - Graphics and rendering
- **Microsoft.Extensions.Configuration** - Settings management

## Project Structure

```
IsometricMapViewer/
├── appsettings.json              # Configuration
├── resources/
│   ├── maps/                     # .amd map files
│   ├── sprites/                  # .spr sprite files
│   └── fonts/                    # Font files
├── src/
│   ├── Core/
│   │   ├── ExporterApp.cs        # Main application
│   │   ├── AppSettings.cs        # Configuration classes
│   │   ├── BudgetDungeonExporter.cs
│   │   ├── Map.cs
│   │   └── Constants.cs
│   ├── UI/
│   │   ├── GothicUI.cs           # Gothic UI components
│   │   ├── ColorKeys.cs          # Color palette
│   │   └── UIConfig.cs
│   ├── Handlers/
│   │   ├── CameraHandler.cs      # Camera system
│   │   └── InputHandler.cs
│   ├── Loaders/
│   │   ├── SpriteLoader.cs
│   │   └── TileLoader.cs
│   └── Rendering/
│       ├── GameRenderer.cs       # Map rendering
│       ├── GridRenderer.cs
│       └── DebugRenderer.cs
├── Program.cs                    # Entry point
└── README.md
```

## Output Format

### Grid JSON (Server)
```json
{
  "id": "arefarm",
  "width": 250,
  "height": 250,
  "tileSize": 32,
  "tiles": [
    { "x": 10, "y": 5, "type": "BLOCKED" }
  ]
}
```

**Tile Types:**
- WALKABLE (0) - Default, not exported
- BLOCKED (1) - Collision tiles
- TELEPORT (2) - Teleport points
- FARM (3) - Farmable areas
- WATER (4) - Water tiles

### PNG Image (Client)
Full visual map with all sprites and objects rendered.

## Controls

| Key | Action |
|-----|--------|
| **G** | Export Grid (JSON) |
| **P** | Export PNG |
| **F** | Fit map to screen |
| **TAB** | Toggle UI |
| **WASD/Arrows** | Pan camera |
| **Mouse Drag** | Pan camera |
| **Mouse Wheel** | Zoom |

## UI Buttons

1. **Export Grid** - JSON for server
2. **Export PNG** - Image for client
3. **Show/Hide Objects** - Toggle object layer
4. **Show/Hide Grid** - Toggle grid overlay
5. **Fit Map** - Reset camera view

## Usage

### GUI Mode
```bash
# 1. Configure map in appsettings.json
# 2. Run
dotnet run

# 3. Export
# - Click "Export Grid" or press G
# - Click "Export PNG" or press P
```

### CLI Mode
```bash
# Export single map (JSON only)
dotnet run -- --export arefarm ~/output

# Batch export all maps
./export-all-maps.sh ~/output
```

## Integration with BudgetDungeon

### Server (Grid JSON)
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

### Client (PNG Image)
Load the PNG as a texture for visual representation.

## Migration History

1. **Started with**: MonoGame-based IsometricMapViewer
2. **Migrated to**: Raylib-cs for better cross-platform support
3. **Simplified**: Removed Tiled export, focused on BudgetDungeon
4. **Added**: Gothic UI theme matching game aesthetic
5. **Optimized**: Single map loading via configuration
6. **Finalized**: Dual export (Grid + PNG) for server/client

## Performance

- **Startup**: ~1-2 seconds (loads one map)
- **Grid Export**: <1 second
- **PNG Export**: ~1-2 seconds (renders full map)
- **Memory**: Efficient (only one map loaded)

## Available Maps

- 2ndmiddle (250x250)
- arefarm (250x250)
- aresden (300x300)
- default (170x170)
- elvfarm (250x250)
- elvine (250x250)
- huntzone1 (250x250)
- huntzone2 (250x250)

## Key Design Decisions

1. **Single map loading** - Fast startup, configure via JSON
2. **Separate exports** - Grid for server logic, PNG for client visuals
3. **Gothic UI** - Matches BudgetDungeon aesthetic
4. **Non-intrusive UI** - Toggle with TAB, doesn't block map view
5. **Full rendering** - Shows actual sprites, not simplified tiles
6. **Space-efficient JSON** - Only exports non-walkable tiles

## Future Enhancements (Optional)

- Batch export both Grid + PNG via CLI
- Map preview thumbnails
- Export progress bar for large maps
- Custom tile type mapping configuration
- Multi-map comparison view

## Credits

- Built with [Raylib](https://www.raylib.com/)
- Gothic UI inspired by classic RPGs
- Designed for [BudgetDungeon](https://github.com/yourusername/BudgetDungeon)

---

**Status**: ✅ Complete and ready for production use
**Last Updated**: 2026-02-09
