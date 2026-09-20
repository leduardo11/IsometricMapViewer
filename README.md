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

3. **Export**:
   - Click **Export Grid** or press `G` for server (JSON)
   - Click **Export PNG** or press `P` for client (image)

## Controls

| Key/Action | Function |
|------------|----------|
| **+ / -** (or KP + / -) | Zoom in/out (keyboard; no wheel required) |
| **Mouse Wheel** | Zoom in/out |
| **WASD / Arrows** | Pan camera |
| **Right/Middle Drag** | Pan camera |
| **F** | Fit map to screen |
| **1-5** | Select tool (Ground / Object / Collision / Eraser / Eyedropper) |
| **G / S / O / T / C / #** | Toggle Ground / Shadows / Objects / Trees / Collision / Grid |
| **Tab** | Toggle sprite palette |
| **H / F1** | Toggle shortcut legend |
| **B** | Cycle brush size |
| **[ / ]** | Cycle selected sprite |
| **, / .** | Cycle selected frame |
| **Ctrl+S** | Save map (.amd) |
| **Ctrl+Z / Ctrl+Y** | Undo / Redo |
| **Ctrl+1..7, Ctrl+0** | Export formats (see below) |

## Tiles (PAKs)

All PAKs from `repos/helbreath_lite` are vendored under `resources/paks/`
(`tiles/`, `objects/`, plus `npcs/`, `items/`, `players/`, `effects/`, `pets/`,
`interface/`, `art/`). The editor reads the canonical tile/object PAKs directly
through `HelbreathAtlasPacker`'s `PakReader`/`SpriteCache`, so every map tile is
available in the palette. Legacy `resources/sprites/*.spr` files are only a
fallback for PAKs that are missing.

## HelbreathAtlasPacker Exports

The editor references `HelbreathAssetPipeline.Packer.Core` and can emit every
map export format the packer supports. Output root is
`MapExporter:AtlasOutputPath` in `appsettings.json` (default `resources/exported`).

| Hotkey | Format | Output |
|--------|--------|--------|
| Ctrl+1 | Base package | `<root>/<map>/` (`atlas.json`, `tilemap.json`, `collision.json`, `manifest.json`, `frame-map.json`, `atlases/`) |
| Ctrl+2 (Ctrl+E) | map@2 RPG | `<root>/rpg/<map>/<map>.map.json` + `atlases/` |
| Ctrl+3 | Godot | `<root>/<map>/<map> TileSet.tres`, `<map>.tscn`, `minimap.png`, `map-info.json` |
| Ctrl+4 | Tiled | `<root>/tiled/<map>/` (`ground.tsx`, `objects.tsx`, PNGs, `manifest.json`) |
| Ctrl+5 | Map shot | `<root>/shots/<map>-shot.png` |
| Ctrl+6 | Master tiles | `<root>/tiles/` (full tile/object vocabulary atlas) |
| Ctrl+7 | Olympia | `<root>/OlympiaAssets/` (needs `MapExporter:OlympiaSourcePath`) |
| Ctrl+0 | Export all | All of the above for the current map + master tiles |

CLI equivalents:

```bash
dotnet run -- --atlas <package|rpg|godot|tiled|map-shot|master|olympia|all> <mapname> [output-root]
```

## UI Buttons

- **Export Grid** - Export JSON grid data for BudgetDungeon server
- **Export PNG** - Export PNG image for BudgetDungeon client
- **Show/Hide Objects** - Toggle object layer visibility
- **Show/Hide Grid** - Toggle grid overlay
- **Fit Map** - Reset camera to fit entire map

## Output Files

For each map, two files are generated:

### 1. Grid JSON (for server)
`{mapname}.json` - Collision and tile type data

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

### 2. PNG Image (for client)
`{mapname}.png` - Full visual map with all sprites and objects

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
- `HelbreathAtlasPacker` checked out as a sibling directory (`../HelbreathAtlasPacker`);
  the map exporter project-references `HelbreathAssetPipeline.Packer.Core`.

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
