# Exporting Maps for BudgetDungeon

This tool converts .amd map files to BudgetDungeon-compatible format (PNG + JSON).

## Quick Start

### Export Single Map (GUI)
```bash
dotnet run
# Press Ctrl+B to export current map
```

### Export Single Map (CLI)
```bash
dotnet run -- --export-budget-dungeon arefarm /home/leduardo/maps
```

### Export All Maps (Batch)
```bash
./export-all-maps.sh /home/leduardo/maps
```

## Output Format

Each map generates two files:

### 1. {mapname}.png
Visual representation of the map with all tiles and objects rendered.

### 2. {mapname}.json
Grid collision data in BudgetDungeon format:

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

## Tile Type Mapping

| .amd Property | BudgetDungeon Type | Description |
|---------------|-------------------|-------------|
| IsTeleport | TELEPORT (2) | Teleport tiles |
| IsWater | WATER (4) | Water tiles |
| IsFarmingAllowed | FARM (3) | Farmable tiles |
| !IsMoveAllowed | BLOCKED (1) | Blocked/collision tiles |
| (default) | WALKABLE (0) | Walkable tiles (not exported) |

**Note**: Only non-walkable tiles are exported to keep JSON files small.

## Configuration

Edit `src/Core/Constants.cs`:

```csharp
public const string OutputPath = "/home/leduardo";  // Where to save exports
public const string MapName = "arefarm";            // Default map to load
```

## GUI Controls

| Key | Action |
|-----|--------|
| Ctrl+B | Export for BudgetDungeon (PNG + JSON) |
| Ctrl+P | Export PNG only |
| Ctrl+T | Export to Tiled format (TSX/TMX) |
| Ctrl+S | Save map changes |
| Ctrl+M | Toggle tile movement allowed |
| Ctrl+E | Toggle tile teleport |
| Ctrl+F | Toggle tile farming |
| Ctrl+W | Toggle tile water |
| G | Toggle grid overlay |
| O | Toggle objects visibility |
| F1 | Show all hotkeys |

## Loading in BudgetDungeon

```csharp
// Load the JSON
var json = File.ReadAllText("arefarm.json");
var mapData = JsonSerializer.Deserialize<BudgetDungeonMapData>(json);

// Create grid with default walkable tiles
var grid = new Grid(new GridCreateParams(
    Width: mapData.Width,
    Height: mapData.Height,
    Origin: Vector2.Zero,
    CreateDefaultTiles: true  // All tiles start as WALKABLE
));

// Apply collision/special tiles
foreach (var tile in mapData.Tiles)
{
    grid.SetTile(new SetTileParams(
        Coord: new GridCoord(tile.X, tile.Y),
        Type: (TileType)tile.Type,
        Gid: 0
    ));
}

// Use the grid in your WorldMap
var worldMap = new WorldMap(new MapData 
{ 
    Id = mapData.Id,
    Grid = grid 
});
```

## Available Maps

Check `resources/maps/` for available .amd files:
- arefarm.amd
- aresden.amd
- default.amd
- elvfarm.amd
- elvine.amd
- huntzone1.amd
- huntzone2.amd
- 2ndmiddle.amd

## Troubleshooting

### "Map file not found"
- Ensure the .amd file exists in `resources/maps/`
- Check the map name matches the filename (without .amd extension)

### "Missing sprite file"
- Some sprite files may be missing from `resources/sprites/`
- The map will still export, but visual representation may be incomplete
- Missing sprites show as magenta (255, 0, 255) tiles

### JSON is empty or has few tiles
- This is normal! Only non-walkable tiles are exported
- A mostly-walkable map will have a small tiles array
- All tiles default to WALKABLE in BudgetDungeon

## Performance

- CLI export: ~1-2 seconds per map (JSON only)
- GUI export: ~2-3 seconds per map (PNG + JSON)
- Batch export: Processes all maps sequentially

## Examples

### Export specific map with custom output
```bash
dotnet run -- --export-budget-dungeon elvine ~/game-assets/maps
```

### Export all maps to project directory
```bash
./export-all-maps.sh ../BudgetDungeon.Server/resources/maps
```

### Change map in GUI
1. Edit `src/Core/Constants.cs`
2. Change `MapName` to desired map
3. Run `dotnet run`
4. Press Ctrl+B to export
