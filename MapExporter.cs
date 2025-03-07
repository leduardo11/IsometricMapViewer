using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using IsometricMapViewer.Rendering;
using Microsoft.Xna.Framework.Graphics;

namespace IsometricMapViewer
{
    public class MapExporter(GameRenderer gameRenderer, Map map) : IDisposable
    {
        private readonly GameRenderer _gameRenderer = gameRenderer;
        private readonly Map _map = map;
        private readonly object _exportLock = new();
        private bool _isExporting = false;

        public void ExportToPng()
        {
            if (_isExporting)
            {
                ConsoleLogger.LogWarning("Export already in progress.");
                return;
            }

            ConsoleLogger.LogInfo("Starting map export...");
            Texture2D exportedTexture = _gameRenderer.RenderFullMapToTexture();
            SaveTextureToFile(exportedTexture);
            exportedTexture.Dispose();
            _isExporting = false;
        }

        public void ExportToTsx()
        {
            if (_isExporting)
            {
                ConsoleLogger.LogWarning("Export already in progress.");
                return;
            }
            _isExporting = true;
            ConsoleLogger.LogInfo("Starting tileset export to .tsx...");

            string outputPath = Path.Combine(Constants.OutputPath, $"{Constants.MapName}_tileset.tsx");
            string imageFileName = $"{Constants.MapName}_tileset.png";
            string imagePath = Path.Combine(Constants.OutputPath, imageFileName);

            // Collect unique combinations of (TileSprite, TileFrame, Properties)
            var uniqueCombinations = _map.Tiles.Cast<MapTile>()
                .Where(t => t.TileSprite != -1)
                .Select(t => (SpriteID: (int)t.TileSprite, FrameIndex: (int)t.TileFrame,
                              Properties: new TileProperties(t.IsMoveAllowed, t.IsTeleport, t.IsFarmingAllowed, t.IsWater)))
                .Distinct()
                .OrderBy(t => t.SpriteID)
                .ThenBy(t => t.FrameIndex)
                .ThenBy(t => t.Properties.IsMoveAllowed)
                .ThenBy(t => t.Properties.IsTeleport)
                .ThenBy(t => t.Properties.IsFarmingAllowed)
                .ThenBy(t => t.Properties.IsWater)
                .ToList();

            int tileCount = uniqueCombinations.Count;
            if (tileCount == 0)
            {
                ConsoleLogger.LogError("No tiles to export.");
                _isExporting = false;
                return;
            }

            int columns = Constants.ExpectedTileSize;
            int rows = (tileCount + columns - 1) / columns;
            int imageWidth = columns * Constants.TileWidth;
            int imageHeight = rows * Constants.TileHeight;

            // Create tileset texture with all combinations (includes visual duplicates)
            Texture2D tilesetTexture = _gameRenderer.CreateTilesetTexture(
                uniqueCombinations.Select(c => (c.SpriteID, c.FrameIndex)).ToList(), columns);
            using (FileStream stream = new(imagePath, FileMode.Create))
            {
                tilesetTexture.SaveAsPng(stream, tilesetTexture.Width, tilesetTexture.Height);
            }
            tilesetTexture.Dispose();

            // Create the .tsx XML with tile properties
            var tilesetElement = new XElement("tileset",
                new XAttribute("version", "1.9"),
                new XAttribute("tiledversion", "1.9.2"),
                new XAttribute("name", "BaseTileset"),
                new XAttribute("tilewidth", Constants.TileWidth),
                new XAttribute("tileheight", Constants.TileHeight),
                new XAttribute("tilecount", tileCount),
                new XAttribute("columns", columns),
                new XElement("image",
                    new XAttribute("source", imageFileName),
                    new XAttribute("width", imageWidth),
                    new XAttribute("height", imageHeight))
            );

            // Add <tile> elements with properties
            for (int i = 0; i < uniqueCombinations.Count; i++)
            {
                var combination = uniqueCombinations[i];
                var propertiesElement = new XElement("properties",
                    new XElement("property",
                        new XAttribute("name", "IsMoveAllowed"),
                        new XAttribute("type", "bool"),
                        new XAttribute("value", combination.Properties.IsMoveAllowed)),
                    new XElement("property",
                        new XAttribute("name", "IsTeleport"),
                        new XAttribute("type", "bool"),
                        new XAttribute("value", combination.Properties.IsTeleport)),
                    new XElement("property",
                        new XAttribute("name", "IsFarmingAllowed"),
                        new XAttribute("type", "bool"),
                        new XAttribute("value", combination.Properties.IsFarmingAllowed)),
                    new XElement("property",
                        new XAttribute("name", "IsWater"),
                        new XAttribute("type", "bool"),
                        new XAttribute("value", combination.Properties.IsWater))
                );
                var tileElement = new XElement("tile",
                    new XAttribute("id", i),
                    propertiesElement);
                tilesetElement.Add(tileElement);
            }

            XDocument doc = new XDocument(tilesetElement);
            doc.Save(outputPath);
            ConsoleLogger.LogInfo($"Tileset exported to {outputPath}");
            _isExporting = false;
        }

        public void ExportToTmx()
        {
            ConsoleLogger.LogInfo("Starting map export to .tmx...");
            string tsxPath = Path.Combine(Constants.OutputPath, $"{Constants.MapName}_tileset.tsx");

            if (!File.Exists(tsxPath))
            {
                ConsoleLogger.LogInfo("Tileset file not found. Generating tileset...");
                ExportToTsx();
            }

            lock (_exportLock)
            {
                if (_isExporting)
                {
                    ConsoleLogger.LogWarning("Export already in progress.");
                    return;
                }
                _isExporting = true;
            }

            try
            {
                string outputPath = Path.Combine(Constants.OutputPath, $"{Constants.MapName}.tmx");

                // Step 1: Collect unique combinations of tile attributes
                var uniqueCombinations = _map.Tiles.Cast<MapTile>()
                    .Where(t => t.TileSprite != -1) // Exclude empty tiles
                    .Select(t => (SpriteID: (int)t.TileSprite, FrameIndex: (int)t.TileFrame,
                                  Properties: new TileProperties(t.IsMoveAllowed, t.IsTeleport, t.IsFarmingAllowed, t.IsWater)))
                    .Distinct()
                    .OrderBy(t => t.SpriteID)
                    .ThenBy(t => t.FrameIndex)
                    .ThenBy(t => t.Properties.IsMoveAllowed)
                    .ThenBy(t => t.Properties.IsTeleport)
                    .ThenBy(t => t.Properties.IsFarmingAllowed)
                    .ThenBy(t => t.Properties.IsWater)
                    .ToList();

                // Step 2: Create a mapping from each unique combination to a global tile ID (GID)
                var tileIdMapping = uniqueCombinations
                    .Select((combo, index) => (Key: (combo.SpriteID, combo.FrameIndex, combo.Properties), GID: index + 1))
                    .ToDictionary(x => x.Key, x => x.GID);

                // Step 3: Generate CSV data for the tile layer
                string csvData = string.Join(",", Enumerable.Range(0, _map.Height).SelectMany(y =>
                    Enumerable.Range(0, _map.Width).Select(x =>
                    {
                        var tile = _map.Tiles[x, y];
                        if (tile.TileSprite == -1) // Empty tile
                            return "0";
                        else
                        {
                            var key = (tile.TileSprite, tile.TileFrame,
                                       new TileProperties(tile.IsMoveAllowed, tile.IsTeleport, tile.IsFarmingAllowed, tile.IsWater));
                            return tileIdMapping[key].ToString();
                        }
                    })));

                // Step 4: Construct the .tmx XML file with orientation attribute
                XDocument tmxDoc = new XDocument(
                    new XElement("map",
                        new XAttribute("version", "1.9"),
                        new XAttribute("tiledversion", "1.9.2"),
                        new XAttribute("orientation", "orthogonal"),
                        new XAttribute("width", _map.Width),
                        new XAttribute("height", _map.Height),
                        new XAttribute("tilewidth", Constants.TileWidth),
                        new XAttribute("tileheight", Constants.TileHeight),
                        new XElement("tileset",
                            new XAttribute("firstgid", 1),
                            new XAttribute("source", $"{Constants.MapName}_tileset.tsx")
                        ),
                        new XElement("layer",
                            new XAttribute("id", 1),
                            new XAttribute("name", "Base Layer"),
                            new XAttribute("width", _map.Width),
                            new XAttribute("height", _map.Height),
                            new XElement("data",
                                new XAttribute("encoding", "csv"),
                                csvData
                            )
                        )
                    )
                );

                // Save the XML to the output file
                tmxDoc.Save(outputPath);
                ConsoleLogger.LogInfo($"Map exported to {outputPath}");
            }
            finally
            {
                lock (_exportLock)
                {
                    _isExporting = false;
                }
            }
        }

        private static void SaveTextureToFile(Texture2D texture)
        {
            string exportPath = Path.Combine(Constants.OutputPath, $"{Constants.MapName}.png");
            using FileStream stream = new(exportPath, FileMode.Create);
            texture.SaveAsPng(stream, texture.Width, texture.Height);
            ConsoleLogger.LogInfo($"Map PNG created at: {exportPath}");
        }
        public void Dispose() { }
    }
}