using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using IsometricMapViewer.Rendering;
using Raylib_cs;

namespace IsometricMapViewer
{
    public class BudgetDungeonExporter
    {
        private readonly GameRenderer _renderer;
        private readonly Map _map;

        public BudgetDungeonExporter(GameRenderer renderer, Map map)
        {
            _renderer = renderer;
            _map = map;
        }

        public void ExportForBudgetDungeon(string outputPath, string mapName)
        {
            var mapFolder = Path.Combine(outputPath, mapName);
            Directory.CreateDirectory(mapFolder);

            // Export PNG
            var pngPath = Path.Combine(mapFolder, $"{mapName}.png");
            ExportMapImage(pngPath);
            ConsoleLogger.LogInfo($"Exported map image to: {pngPath}");

            // Export JSON
            var jsonPath = Path.Combine(mapFolder, $"{mapName}.json");
            ExportMapJson(jsonPath, mapName);
            ConsoleLogger.LogInfo($"Exported map data to: {jsonPath}");
        }

        public void ExportJsonOnly(string jsonPath, string mapName)
        {
            ExportMapJson(jsonPath, mapName);
        }

        private void ExportMapImage(string filePath)
        {
            Image mapImage = _renderer.RenderFullMapToImage();
            Raylib.ExportImage(mapImage, filePath);
            Raylib.UnloadImage(mapImage);
        }

        private void ExportMapJson(string filePath, string mapName)
        {
            var mapData = new BudgetDungeonMapData
            {
                Id = mapName,
                Width = _map.Width,
                Height = _map.Height,
                TileSize = Constants.TileSize,
                Tiles = ConvertTilesToBudgetDungeonFormat()
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(mapData, options);
            File.WriteAllText(filePath, json);
        }

        private List<BudgetDungeonTile> ConvertTilesToBudgetDungeonFormat()
        {
            var tiles = new List<BudgetDungeonTile>();

            for (int y = 0; y < _map.Height; y++)
            {
                for (int x = 0; x < _map.Width; x++)
                {
                    var tile = _map.Tiles[x, y];
                    var tileType = MapTileToBudgetDungeonType(tile);

                    // Only export non-walkable tiles to save space
                    if (tileType != BudgetDungeonTileType.WALKABLE)
                    {
                        tiles.Add(new BudgetDungeonTile
                        {
                            X = x,
                            Y = y,
                            Type = tileType
                        });
                    }
                }
            }

            return tiles;
        }

        private static BudgetDungeonTileType MapTileToBudgetDungeonType(MapTile tile)
        {
            // Priority order: Teleport > Water > Farm > Blocked > Walkable
            if (tile.IsTeleport)
                return BudgetDungeonTileType.TELEPORT;
            
            if (tile.IsWater)
                return BudgetDungeonTileType.WATER;
            
            if (tile.IsFarmingAllowed)
                return BudgetDungeonTileType.FARM;
            
            if (!tile.IsMoveAllowed)
                return BudgetDungeonTileType.BLOCKED;

            return BudgetDungeonTileType.WALKABLE;
        }
    }

    // Data structures matching BudgetDungeon format
    public class BudgetDungeonMapData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("tileSize")]
        public int TileSize { get; set; }

        [JsonPropertyName("tiles")]
        public List<BudgetDungeonTile> Tiles { get; set; } = new();
    }

    public class BudgetDungeonTile
    {
        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }

        [JsonPropertyName("type")]
        public BudgetDungeonTileType Type { get; set; }
    }

    public enum BudgetDungeonTileType : byte
    {
        WALKABLE = 0,
        BLOCKED = 1,
        TELEPORT = 2,
        FARM = 3,
        WATER = 4
    }
}
