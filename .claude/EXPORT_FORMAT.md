# BudgetDungeon Export Format

This tool exports .amd map files to a format compatible with BudgetDungeon.

## Output Files

For each map, two files are generated:

1. **{mapname}.png** - Visual representation of the map
2. **{mapname}.json** - Grid data with tile collision information

## JSON Format

```json
{
  "id": "mapname",
  "width": 250,
  "height": 250,
  "tileSize": 32,
  "tiles": [
    {
      "x": 10,
      "y": 5,
      "type": "BLOCKED"
    },
    {
      "x": 15,
      "y": 20,
      "type": "TELEPORT"
    }
  ]
}
```

### Fields

- **id**: Map identifier (string)
- **width**: Map width in tiles (int)
- **height**: Map height in tiles (int)
- **tileSize**: Size of each tile in pixels (int, default: 32)
- **tiles**: Array of non-walkable tiles (only tiles that aren't WALKABLE are included to save space)

### Tile Types

The exporter maps .amd tile properties to BudgetDungeon tile types:

| .amd Property | BudgetDungeon Type | Value |
|---------------|-------------------|-------|
| IsTeleport = true | TELEPORT | 2 |
| IsWater = true | WATER | 4 |
| IsFarmingAllowed = true | FARM | 3 |
| IsMoveAllowed = false | BLOCKED | 1 |
| (default) | WALKABLE | 0 |

**Priority Order**: If a tile has multiple properties, the exporter uses this priority:
1. TELEPORT (highest)
2. WATER
3. FARM
4. BLOCKED
5. WALKABLE (default)

## Usage

### GUI Mode
1. Run the application: `dotnet run`
2. Press **Ctrl + B** to export the current map for BudgetDungeon
3. Files will be saved to: `{OutputPath}/{MapName}/`

### Configuration

Edit `src/Core/Constants.cs`:

```csharp
public const string OutputPath = "/home/leduardo";  // Output directory
public const string MapName = "arefarm";            // Map to load/export
```

## Integration with BudgetDungeon

The exported JSON format matches your `Grid.ExportCollisionGrid()` structure:

```csharp
// In your BudgetDungeon server
var mapData = JsonSerializer.Deserialize<BudgetDungeonMapData>(jsonContent);

// Create grid
var grid = new Grid(new GridCreateParams(
    Width: mapData.Width,
    Height: mapData.Height,
    Origin: Vector2.Zero,
    CreateDefaultTiles: true
));

// Apply collision tiles
foreach (var tile in mapData.Tiles)
{
    grid.SetTile(new SetTileParams(
        Coord: new GridCoord(tile.X, tile.Y),
        Type: (TileType)tile.Type,
        Gid: 0
    ));
}
```

## Example Output

For a 250x250 map with some blocked areas and teleports:

```json
{
  "id": "arefarm",
  "width": 250,
  "height": 250,
  "tileSize": 32,
  "tiles": [
    { "x": 0, "y": 0, "type": "BLOCKED" },
    { "x": 1, "y": 0, "type": "BLOCKED" },
    { "x": 50, "y": 50, "type": "TELEPORT" },
    { "x": 100, "y": 100, "type": "WATER" },
    { "x": 150, "y": 150, "type": "FARM" }
  ]
}
```

Only non-walkable tiles are included, so a mostly-walkable map will have a small JSON file.
