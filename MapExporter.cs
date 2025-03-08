using System;
using System.IO;
using System.Xml;
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
                ConsoleLogger.LogInfo("Starting map export to PNG...");
                string mapFolder = Path.Combine(Constants.OutputPath, Constants.MapName);
                Directory.CreateDirectory(mapFolder);
                string exportPath = Path.Combine(mapFolder, $"{Constants.MapName}.png");
                Texture2D exportedTexture = _gameRenderer.RenderFullMapToTexture();
                SaveTextureToFile(exportedTexture, exportPath);
                exportedTexture.Dispose();
            }
            finally
            {
                lock (_exportLock)
                {
                    _isExporting = false;
                }
            }
        }

        public void ExportToTsx()
        {
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
                ConsoleLogger.LogInfo("Starting map export to TSX...");
                string mapFolder = Path.Combine(Constants.OutputPath, Constants.MapName);
                Directory.CreateDirectory(mapFolder);
                string exportPath = Path.Combine(mapFolder, $"{Constants.MapName}.tsx");
                CreateTsxFile(exportPath);
                ConsoleLogger.LogInfo($"TSX file created at: {exportPath}");
            }
            finally
            {
                lock (_exportLock)
                {
                    _isExporting = false;
                }
            }
        }

        private void CreateTsxFile(string filePath)
        {
            XmlWriterSettings settings = new XmlWriterSettings { Indent = true };
            using XmlWriter writer = XmlWriter.Create(filePath, settings);

            writer.WriteStartDocument();
            writer.WriteStartElement("tileset");
            writer.WriteAttributeString("version", "1.10");
            writer.WriteAttributeString("tiledversion", "1.11.2");
            writer.WriteAttributeString("name", "Promisedland");
            writer.WriteAttributeString("tilewidth", "32");
            writer.WriteAttributeString("tileheight", "32");
            writer.WriteAttributeString("tilecount", (_map.Width * _map.Height).ToString());
            writer.WriteAttributeString("columns", _map.Width.ToString());

            // Write image element
            writer.WriteStartElement("image");
            writer.WriteAttributeString("source", "Promisedland.png");
            writer.WriteAttributeString("width", (_map.Width * Constants.TileWidth).ToString());
            writer.WriteAttributeString("height", (_map.Height * Constants.TileHeight).ToString());
            writer.WriteEndElement(); // </image>

            // Write properties for each tile position
            for (int y = 0; y < _map.Height; y++)
            {
                for (int x = 0; x < _map.Width; x++)
                {
                    var tile = _map.Tiles[x, y];
                    int tileId = y * _map.Width + x; // Tile ID based on position

                    // Check if any property is true
                    bool isBlocked = !tile.IsMoveAllowed; // Inverted logic
                    bool hasProperties = isBlocked || tile.IsTeleport || tile.IsFarmingAllowed || tile.IsWater;

                    if (!hasProperties)
                        continue; // Skip tiles with no true properties

                    writer.WriteStartElement("tile");
                    writer.WriteAttributeString("id", tileId.ToString());

                    writer.WriteStartElement("properties");

                    // Only write properties that are true
                    if (isBlocked)
                        WriteProperty(writer, "IsBlocked", true);
                    if (tile.IsTeleport)
                        WriteProperty(writer, "IsTeleport", true);
                    if (tile.IsFarmingAllowed)
                        WriteProperty(writer, "IsFarmingAllowed", true);
                    if (tile.IsWater)
                        WriteProperty(writer, "IsWater", true);

                    writer.WriteEndElement(); // </properties>
                    writer.WriteEndElement(); // </tile>
                }
            }

            writer.WriteEndElement(); // </tileset>
            writer.WriteEndDocument();
        }

        private void WriteProperty(XmlWriter writer, string name, bool value)
        {
            writer.WriteStartElement("property");
            writer.WriteAttributeString("name", name);
            writer.WriteAttributeString("type", "bool");
            writer.WriteAttributeString("value", "true"); // Only write true values
            writer.WriteEndElement();
        }

        private static void SaveTextureToFile(Texture2D texture, string filePath)
        {
            using FileStream stream = new(filePath, FileMode.Create);
            texture.SaveAsPng(stream, texture.Width, texture.Height);
            ConsoleLogger.LogInfo($"Map PNG created at: {filePath}");
        }

        public void Dispose()
        {
        }
    }
}