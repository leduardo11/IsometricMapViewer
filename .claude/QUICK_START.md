# Quick Start - Export Maps for BudgetDungeon

## TL;DR

```bash
# Export all maps at once
./export-all-maps.sh ~/BudgetDungeon.Server/resources/maps

# Or export single map
dotnet run -- --export-budget-dungeon arefarm ~/output

# Or use GUI (includes PNG)
dotnet run
# Press Ctrl+B
```

## What You Get

Each map exports to:
- `{mapname}.json` - Grid collision data (WALKABLE, BLOCKED, TELEPORT, FARM, WATER)
- `{mapname}.png` - Visual map image (GUI mode only)

## JSON Format

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

Only non-walkable tiles are exported. All other tiles default to WALKABLE.

## Load in BudgetDungeon

```csharp
var json = File.ReadAllText("arefarm.json");
var data = JsonSerializer.Deserialize<BudgetDungeonMapData>(json);

var grid = new Grid(new GridCreateParams(
    Width: data.Width,
    Height: data.Height,
    Origin: Vector2.Zero,
    CreateDefaultTiles: true  // All WALKABLE by default
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

## Available Maps

- 2ndmiddle (250x250)
- arefarm (250x250)
- aresden (300x300)
- default (170x170)
- elvfarm (250x250)
- elvine (250x250)
- huntzone1 (250x250)
- huntzone2 (250x250)

## Analyze Exported Map

```bash
./analyze-map.sh /path/to/map.json
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
```

## GUI Controls

| Key | Action |
|-----|--------|
| Ctrl+B | Export for BudgetDungeon |
| Ctrl+M | Toggle tile blocked |
| Ctrl+E | Toggle tile teleport |
| Ctrl+F | Toggle tile farm |
| Ctrl+W | Toggle tile water |
| G | Toggle grid |
| F1 | Show help |

## Configuration

Edit `src/Core/Constants.cs`:
```csharp
public const string MapName = "arefarm";  // Map to load in GUI
```

## That's It!

Your maps are now ready to use in BudgetDungeon.
