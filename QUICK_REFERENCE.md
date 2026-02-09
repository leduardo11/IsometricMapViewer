# Quick Reference Card

## 🚀 Export Commands

```bash
# GUI Mode (with visual preview)
dotnet run

# CLI Mode (single map)
dotnet run -- --export <mapname> [output-path]

# Batch Export (all maps)
./export-all-maps.sh [output-directory]
```

## 🎮 GUI Controls

| Key | Action |
|-----|--------|
| `TAB` | Toggle export panel |
| `ESC` | Close panel |
| `Mouse Click` | Select map / Export |

## 📊 Map Analysis

```bash
./analyze-map.sh /path/to/map.json
```

## 🗺️ Available Maps

1. **2ndmiddle** - 250x250
2. **arefarm** - 250x250  
3. **aresden** - 300x300
4. **default** - 170x170
5. **elvfarm** - 250x250
6. **elvine** - 250x250
7. **huntzone1** - 250x250
8. **huntzone2** - 250x250

## 📦 Output Format

```json
{
  "id": "mapname",
  "width": 250,
  "height": 250,
  "tileSize": 32,
  "tiles": [
    { "x": 10, "y": 5, "type": "BLOCKED" }
  ]
}
```

## 🎯 Tile Types

| Type | Value | Color Preview |
|------|-------|---------------|
| WALKABLE | 0 | Dark Gray |
| BLOCKED | 1 | Stone Gray |
| TELEPORT | 2 | Purple |
| FARM | 3 | Green |
| WATER | 4 | Blue |

## 🔗 BudgetDungeon Integration

```csharp
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

## 📁 Default Output Path

`/home/leduardo/exported-maps/{mapname}/{mapname}.json`

## ⚡ Performance

- CLI: ~1 sec/map
- GUI: ~1-2 sec/map
- Batch: ~10 sec for all 8 maps

## 💡 Tips

- Only non-walkable tiles are exported (space-efficient)
- All tiles default to WALKABLE in BudgetDungeon
- Use GUI for visual preview before export
- Use CLI for automation/batch processing
