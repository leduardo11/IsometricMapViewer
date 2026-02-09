# IsometricMapViewer - BudgetDungeon Export Summary

## What Was Done

Successfully migrated the IsometricMapViewer from MonoGame to Raylib-cs and added BudgetDungeon export functionality.

## Features

### 1. Raylib Migration ✓
- Converted from MonoGame to Raylib-cs 7.0.2
- All rendering, input, and camera systems now use Raylib
- Project structure reorganized to `resources/` folder

### 2. BudgetDungeon Export ✓
- Exports .amd maps to PNG + JSON format
- JSON format matches your `Grid.ExportCollisionGrid()` structure
- Tile type mapping: WALKABLE, BLOCKED, TELEPORT, FARM, WATER
- Only non-walkable tiles exported (space-efficient)

### 3. Export Methods

#### GUI Mode (with PNG)
```bash
dotnet run
# Press Ctrl+B to export current map
```

#### CLI Mode (JSON only)
```bash
dotnet run -- --export-budget-dungeon <mapname> [output-path]
```

#### Batch Export
```bash
./export-all-maps.sh /output/directory
```

## File Structure

```
IsometricMapViewer/
├── resources/
│   ├── maps/          # .amd map files
│   ├── sprites/       # .spr sprite files
│   └── fonts/         # Font files
├── src/
│   ├── Core/
│   │   ├── MainGame.cs
│   │   ├── Constants.cs
│   │   ├── BudgetDungeonExporter.cs  # NEW
│   │   └── Map.cs
│   ├── Handlers/      # Input & Camera
│   ├── Loaders/       # Sprite & Tile loaders
│   └── Rendering/     # Rendering systems
├── Program.cs         # Entry point with CLI support
├── export-all-maps.sh # Batch export script
├── EXPORT_FORMAT.md   # Format documentation
└── README_EXPORT.md   # Usage guide
```

## Export Format

### JSON Structure
```json
{
  "id": "mapname",
  "width": 250,
  "height": 250,
  "tileSize": 32,
  "tiles": [
    { "x": 10, "y": 5, "type": "BLOCKED" },
    { "x": 15, "y": 20, "type": "TELEPORT" }
  ]
}
```

### Tile Type Mapping
| .amd Property | BudgetDungeon | Value |
|---------------|---------------|-------|
| IsTeleport | TELEPORT | 2 |
| IsWater | WATER | 4 |
| IsFarmingAllowed | FARM | 3 |
| !IsMoveAllowed | BLOCKED | 1 |
| (default) | WALKABLE | 0 |

## Integration with BudgetDungeon

```csharp
// 1. Load JSON
var json = File.ReadAllText("arefarm.json");
var mapData = JsonSerializer.Deserialize<BudgetDungeonMapData>(json);

// 2. Create grid (all tiles start as WALKABLE)
var grid = new Grid(new GridCreateParams(
    Width: mapData.Width,
    Height: mapData.Height,
    Origin: Vector2.Zero,
    CreateDefaultTiles: true
));

// 3. Apply collision tiles
foreach (var tile in mapData.Tiles)
{
    grid.SetTile(new SetTileParams(
        Coord: new GridCoord(tile.X, tile.Y),
        Type: (TileType)tile.Type,
        Gid: 0
    ));
}

// 4. Use in WorldMap
var worldMap = new WorldMap(new MapData { Id = mapData.Id, Grid = grid });
```

## Test Results

### Exported Maps
- ✓ arefarm: 250x250 tiles, 31,332 non-walkable tiles
- ✓ default: 170x170 tiles
- All 8 maps available for export

### Performance
- CLI export: ~1 second per map (JSON only)
- GUI export: ~2-3 seconds per map (PNG + JSON)

## Available Maps

1. arefarm.amd
2. aresden.amd
3. default.amd
4. elvfarm.amd
5. elvine.amd
6. huntzone1.amd
7. huntzone2.amd
8. 2ndmiddle.amd

## GUI Controls

| Key | Action |
|-----|--------|
| **Ctrl+B** | **Export for BudgetDungeon** |
| Ctrl+P | Export PNG only |
| Ctrl+T | Export to Tiled (TSX/TMX) |
| Ctrl+S | Save map |
| Ctrl+M | Toggle movement allowed |
| Ctrl+E | Toggle teleport |
| Ctrl+F | Toggle farming |
| Ctrl+W | Toggle water |
| G | Toggle grid |
| O | Toggle objects |
| F1 | Show help |

## Next Steps

1. **Export all maps**:
   ```bash
   ./export-all-maps.sh ~/BudgetDungeon.Server/resources/maps
   ```

2. **Copy PNG images** (for visual reference):
   - Run GUI mode: `dotnet run`
   - Load each map (edit Constants.MapName)
   - Press Ctrl+B to export with PNG

3. **Integrate in BudgetDungeon**:
   - Copy JSON files to your server's maps folder
   - Update MapRegistry to load JSON format
   - Use the integration code above

## Configuration

Edit `src/Core/Constants.cs`:
```csharp
public const string OutputPath = "/home/leduardo";  // Output directory
public const string MapName = "arefarm";            // Map to load
```

## Notes

- Only non-walkable tiles are exported (space-efficient)
- All tiles default to WALKABLE in BudgetDungeon
- PNG export requires GUI mode (uses Raylib rendering)
- CLI mode is headless (JSON only, no graphics initialization)
- Missing sprites show as magenta in PNG exports
