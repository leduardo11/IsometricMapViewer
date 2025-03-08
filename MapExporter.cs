using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using IsometricMapViewer.Rendering;
using Microsoft.Xna.Framework;
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

        public void ExportToTmx()
        {
            ConsoleLogger.LogInfo("Starting map export to .tmx...");
            string outputPath = Path.Combine(Constants.OutputPath, $"{Constants.MapName}.tmx");

            // Ensure tilesets exist
            if (!File.Exists(Path.Combine(Constants.OutputPath, "BaseTileset.tsx"))) ExportBaseTileset();
            if (!File.Exists(Path.Combine(Constants.OutputPath, "ObjectTileset.tsx"))) ExportObjectTileset();
            if (!File.Exists(Path.Combine(Constants.OutputPath, "PropertiesTileset.tsx"))) ExportPropertiesTileset();

            lock (_exportLock)
            {
                if (_isExporting) { ConsoleLogger.LogWarning("Export already in progress."); return; }
                _isExporting = true;
            }

            try
            {
                // Generate CSV data for layers
                string groundData = string.Join(",", Enumerable.Range(0, _map.Height).SelectMany(y =>
                    Enumerable.Range(0, _map.Width).Select(x =>
                        _map.Tiles[x, y].TileSprite == -1 ? "0" : (_map.Tiles[x, y].TileSprite + 1).ToString())));

                string objectsData = string.Join(",", Enumerable.Range(0, _map.Height).SelectMany(y =>
                    Enumerable.Range(0, _map.Width).Select(x =>
                        _map.Tiles[x, y].ObjectSprite == -1 ? "0" : (_map.Tiles[x, y].ObjectSprite + 1).ToString())));

                var propertiesData = new List<string>();
                
                for (int y = 0; y < _map.Height; y++)
                {
                    for (int x = 0; x < _map.Width; x++)
                    {
                        var tile = _map.Tiles[x, y];
                        propertiesData.Add(tile.IsMoveAllowed || tile.IsTeleport || tile.IsFarmingAllowed || tile.IsWater ? "2" : "0");
                    }
                }
                string propertiesCsv = string.Join(",", propertiesData);

                // Create the map structure using helper methods
                XElement mapElement = CreateMapElement(_map.Width, _map.Height, Constants.TileWidth, Constants.TileHeight);

                // Add tileset references
                mapElement.Add(CreateTilesetReference(1, "BaseTileset.tsx"));
                mapElement.Add(CreateTilesetReference(1000, "ObjectTileset.tsx"));
                mapElement.Add(CreateTilesetReference(2000, "PropertiesTileset.tsx"));

                // Add layers
                mapElement.Add(CreateLayerElement(1, "Ground", groundData));
                mapElement.Add(CreateLayerElement(2, "Objects", objectsData));
                XElement propertiesLayer = CreateLayerElement(3, "Properties", propertiesCsv);
                mapElement.Add(propertiesLayer);

                // Add per-tile properties
                var tileElements = new List<XElement>();

                for (int y = 0; y < _map.Height; y++)
                {
                    for (int x = 0; x < _map.Width; x++)
                    {
                        var tile = _map.Tiles[x, y];
                        if (tile.IsMoveAllowed || tile.IsTeleport || tile.IsFarmingAllowed || tile.IsWater)
                        {
                            int gid = 2000 + 1; // PropertiesTileset firstgid + 1
                            tileElements.Add(CreateTileWithProperties(gid, tile.IsMoveAllowed, tile.IsTeleport, tile.IsFarmingAllowed, tile.IsWater));
                        }
                    }
                }

                if (tileElements.Count > 0)
                {
                    propertiesLayer.Add(new XElement("tiles", tileElements));
                }

                // Save the document
                XDocument tmxDoc = new(mapElement);
                tmxDoc.Save(outputPath);
                ConsoleLogger.LogInfo($"Map exported to {outputPath}");
            }
            finally
            {
                lock (_exportLock) { _isExporting = false; }
            }
        }

        private void ExportBaseTileset()
        {
            var uniqueBaseSprites = _map.Tiles.Cast<MapTile>()
                .Where(t => t.TileSprite != -1)
                .Select(t => (SpriteID: (int)t.TileSprite, FrameIndex: (int)t.TileFrame))
                .Distinct()
                .OrderBy(t => t.SpriteID)
                .ThenBy(t => t.FrameIndex)
                .ToList();
            ExportTileset("BaseTileset", uniqueBaseSprites, "BaseTileset.png");
        }

        private void ExportObjectTileset()
        {
            var uniqueObjectSprites = _map.Tiles.Cast<MapTile>()
                .Where(t => t.ObjectSprite != -1)
                .Select(t => (SpriteID: (int)t.ObjectSprite, FrameIndex: (int)t.ObjectFrame))
                .Distinct()
                .OrderBy(t => t.SpriteID)
                .ThenBy(t => t.FrameIndex)
                .ToList();
            ExportTileset("ObjectTileset", uniqueObjectSprites, "ObjectTileset.png");
        }

        private void ExportPropertiesTileset()
        {
            string outputPath = Path.Combine(Constants.OutputPath, "PropertiesTileset.tsx");
            int tileCount = 2; // Empty + Property Tile
            int columns = 2;
            int imageWidth = Constants.TileWidth * columns;
            int imageHeight = Constants.TileHeight;

            Texture2D tilesetTexture = _gameRenderer.CreateTexture2D(imageWidth, imageHeight);
            Color[] data = new Color[imageWidth * imageHeight];
            Array.Fill(data, Color.Transparent); // Empty tile

            for (int x = Constants.TileWidth; x < imageWidth; x++)
            {
                for (int y = 0; y < imageHeight; y++)
                {
                    data[y * imageWidth + x] = Color.Gray * 0.5f; // Property tile
                }
            }

            tilesetTexture.SetData(data);
            string imageFileName = "PropertiesTileset.png";
            string imagePath = Path.Combine(Constants.OutputPath, imageFileName);
            using (FileStream stream = new(imagePath, FileMode.Create))
            {
                tilesetTexture.SaveAsPng(stream, tilesetTexture.Width, tilesetTexture.Height);
            }

            var imageElement = new XElement("image",
                new XAttribute("source", imageFileName),
                new XAttribute("width", imageWidth),
                new XAttribute("height", imageHeight));

            var tileElement = CreatePropertyTileElement();
            var tilesetElement = CreateTilesetElement("PropertiesTileset", Constants.TileWidth, Constants.TileHeight, tileCount, columns, imageElement, tileElement);

            XDocument doc = new(tilesetElement);
            doc.Save(outputPath);
            ConsoleLogger.LogInfo($"Properties tileset exported to {outputPath}");

            tilesetTexture.Dispose();
        }

        private static void SaveTextureToFile(Texture2D texture)
        {
            string exportPath = Path.Combine(Constants.OutputPath, $"{Constants.MapName}.png");
            using FileStream stream = new(exportPath, FileMode.Create);
            texture.SaveAsPng(stream, texture.Width, texture.Height);
            ConsoleLogger.LogInfo($"Map PNG created at: {exportPath}");
        }

        private void ExportTileset(string tilesetName, List<(int SpriteID, int FrameIndex)> uniqueSprites, string imageFileName)
        {
            string outputPath = Path.Combine(Constants.OutputPath, $"{tilesetName}.tsx");
            int tileCount = uniqueSprites.Count;

            if (tileCount == 0)
            {
                ConsoleLogger.LogInfo($"No {tilesetName} tiles to export.");
                return;
            }

            int columns = Constants.ExpectedTileSize;
            Texture2D tilesetTexture = _gameRenderer.CreateTilesetTexture(uniqueSprites, columns);
            string imagePath = Path.Combine(Constants.OutputPath, imageFileName);

            using (FileStream stream = new(imagePath, FileMode.Create))
            {
                tilesetTexture.SaveAsPng(stream, tilesetTexture.Width, tilesetTexture.Height);
            }

            var imageElement = new XElement("image",
                new XAttribute("source", imageFileName),
                new XAttribute("width", tilesetTexture.Width),
                new XAttribute("height", tilesetTexture.Height));

            var tilesetElement = CreateTilesetElement(tilesetName, Constants.TileWidth, Constants.TileHeight, tileCount, columns, imageElement);

            XDocument doc = new(tilesetElement);
            doc.Save(outputPath);
            ConsoleLogger.LogInfo($"{tilesetName} exported to {outputPath}");

            tilesetTexture.Dispose();
        }

        private static XElement CreateMapElement(int width, int height, int tileWidth, int tileHeight)
        {
            return new XElement("map",
                new XAttribute("version", "1.9"),
                new XAttribute("tiledversion", "1.9.2"),
                new XAttribute("orientation", "orthogonal"),
                new XAttribute("width", width),
                new XAttribute("height", height),
                new XAttribute("tilewidth", tileWidth),
                new XAttribute("tileheight", tileHeight));
        }

        private static XElement CreateTilesetReference(int firstGid, string source)
        {
            return new XElement("tileset",
                new XAttribute("firstgid", firstGid),
                new XAttribute("source", source));
        }

        private static XElement CreateTileWithProperties(int gid, bool isMoveAllowed, bool isTeleport, bool isFarmingAllowed, bool isWater)
        {
            return new XElement("tile",
                new XAttribute("gid", gid),
                new XElement("properties",
                    new XElement("property", new XAttribute("name", "IsMoveAllowed"), new XAttribute("type", "bool"), new XAttribute("value", isMoveAllowed)),
                    new XElement("property", new XAttribute("name", "IsTeleport"), new XAttribute("type", "bool"), new XAttribute("value", isTeleport)),
                    new XElement("property", new XAttribute("name", "IsFarmingAllowed"), new XAttribute("type", "bool"), new XAttribute("value", isFarmingAllowed)),
                    new XElement("property", new XAttribute("name", "IsWater"), new XAttribute("type", "bool"), new XAttribute("value", isWater))));
        }

        private static XElement CreateTilesetElement(string name, int tileWidth, int tileHeight, int tileCount, int columns, XElement imageElement = null, XElement tileElement = null)
        {
            var tilesetElement = new XElement("tileset",
                new XAttribute("version", "1.9"),
                new XAttribute("tiledversion", "1.9.2"),
                new XAttribute("name", name),
                new XAttribute("tilewidth", tileWidth),
                new XAttribute("tileheight", tileHeight),
                new XAttribute("tilecount", tileCount),
                new XAttribute("columns", columns));

            if (imageElement != null)
            {
                tilesetElement.Add(imageElement);
            }

            if (tileElement != null)
            {
                tilesetElement.Add(tileElement);
            }

            return tilesetElement;
        }

        private static XElement CreatePropertyTileElement()
        {
            return new XElement("tile",
                new XAttribute("id", 1),
                new XElement("properties",
                    new XElement("property", new XAttribute("name", "IsMoveAllowed"), new XAttribute("type", "bool"), new XAttribute("value", "false")),
                    new XElement("property", new XAttribute("name", "IsTeleport"), new XAttribute("type", "bool"), new XAttribute("value", "false")),
                    new XElement("property", new XAttribute("name", "IsFarmingAllowed"), new XAttribute("type", "bool"), new XAttribute("value", "false")),
                    new XElement("property", new XAttribute("name", "IsWater"), new XAttribute("type", "bool"), new XAttribute("value", "false"))));
        }

        private XElement CreateLayerElement(int id, string name, string csvData)
        {
            return new XElement("layer",
                new XAttribute("id", id),
                new XAttribute("name", name),
                new XAttribute("width", _map.Width),
                new XAttribute("height", _map.Height),
                new XElement("data", new XAttribute("encoding", "csv"), csvData));
        }

        public void Dispose() { }
    }
}