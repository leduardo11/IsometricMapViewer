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

            var uniqueTiles = _map.Tiles.Cast<MapTile>()
                                        .Where(t => t.TileSprite != -1)
                                        .Select(t => (SpriteID: (int)t.TileSprite, FrameIndex: (int)t.TileFrame))
                                        .Distinct()
                                        .OrderBy(t => t.SpriteID)
                                        .ThenBy(t => t.FrameIndex)
                                        .ToList();

            int tileCount = uniqueTiles.Count;

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

            Texture2D tilesetTexture = _gameRenderer.CreateTilesetTexture(uniqueTiles, columns);

            using (FileStream stream = new(imagePath, FileMode.Create))
            {
                tilesetTexture.SaveAsPng(stream, tilesetTexture.Width, tilesetTexture.Height);
            }
            tilesetTexture.Dispose();

            XDocument doc = new(
                new XElement("tileset",
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
                        new XAttribute("height", imageHeight)
                    )
                )
            );

            doc.Save(outputPath);
            ConsoleLogger.LogInfo($"Tileset exported to {outputPath}");
            _isExporting = false;
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