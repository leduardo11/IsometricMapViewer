using System;
using System.IO;
using System.Xml;
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

        public string ExportToPng()
        {
            return WithExportLock(ExportPngInternal);
        }

        public string ExportToTiledMap()
        {
            return WithExportLock(() =>
            {
                string pngPath = ExportPngInternal();
                string tsxPath = ExportTsxInternal();
                string tmxPath = ExportTmxInternal();
                return tsxPath;
            });
        }

        private string WithExportLock(Func<string> exportAction)
        {
            lock (_exportLock)
            {
                if (_isExporting)
                {
                    ConsoleLogger.LogWarning("Export already in progress.");
                    return null;
                }
                _isExporting = true;
                try
                {
                    return exportAction();
                }
                finally
                {
                    _isExporting = false;
                }
            }
        }

        private string ExportPngInternal()
        {
            ConsoleLogger.LogInfo("Starting map export to PNG...");
            string mapFolder = Path.Combine(Constants.OutputPath, Constants.MapName);
            Directory.CreateDirectory(mapFolder);
            string exportPath = Path.Combine(mapFolder, $"{Constants.MapName}.png");
            using Texture2D exportedTexture = _gameRenderer.RenderFullMapToTexture();
            SaveTextureToFile(exportedTexture, exportPath);
            ConsoleLogger.LogInfo($"Map PNG created at: {exportPath}");
            return exportPath;
        }

        private string ExportTsxInternal()
        {
            ConsoleLogger.LogInfo("Starting map export to TSX...");
            string mapFolder = Path.Combine(Constants.OutputPath, Constants.MapName);
            Directory.CreateDirectory(mapFolder);
            string exportPath = Path.Combine(mapFolder, $"{Constants.MapName}.tsx");
            CreateTsxFile(exportPath);
            ConsoleLogger.LogInfo($"TSX file created at: {exportPath}");
            return exportPath;
        }

        private string ExportTmxInternal()
        {
            ConsoleLogger.LogInfo("Starting map export to TMX...");
            string mapFolder = Path.Combine(Constants.OutputPath, Constants.MapName);
            Directory.CreateDirectory(mapFolder);
            string exportPath = Path.Combine(mapFolder, $"{Constants.MapName}.tmx");
            CreateTmxFile(exportPath);
            ConsoleLogger.LogInfo($"TMX file created at: {exportPath}");
            return exportPath;
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

            writer.WriteStartElement("image");
            writer.WriteAttributeString("source", $"{Constants.MapName}.png");
            writer.WriteAttributeString("width", (_map.Width * Constants.TileWidth).ToString());
            writer.WriteAttributeString("height", (_map.Height * Constants.TileHeight).ToString());
            writer.WriteEndElement();

            for (int y = 0; y < _map.Height; y++)
            {
                for (int x = 0; x < _map.Width; x++)
                {
                    var tile = _map.Tiles[x, y];
                    int tileId = y * _map.Width + x;
                    bool isBlocked = !tile.IsMoveAllowed;
                    bool hasProperties = isBlocked || tile.IsTeleport || tile.IsFarmingAllowed || tile.IsWater;

                    if (!hasProperties)
                        continue;

                    writer.WriteStartElement("tile");
                    writer.WriteAttributeString("id", tileId.ToString());
                    writer.WriteStartElement("properties");

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

        private static void WriteProperty(XmlWriter writer, string name, bool value)
        {
            writer.WriteStartElement("property");
            writer.WriteAttributeString("name", name);
            writer.WriteAttributeString("type", "bool");
            writer.WriteAttributeString("value", "true");
            writer.WriteEndElement();
        }

        private static void SaveTextureToFile(Texture2D texture, string filePath)
        {
            using FileStream stream = new(filePath, FileMode.Create);
            texture.SaveAsPng(stream, texture.Width, texture.Height);
        }

        private void CreateTmxFile(string filePath)
        {
            XmlWriterSettings settings = new() { Indent = true };
            using XmlWriter writer = XmlWriter.Create(filePath, settings);

            writer.WriteStartDocument();
            writer.WriteStartElement("map");
            writer.WriteAttributeString("version", "1.10");
            writer.WriteAttributeString("tiledversion", "1.11.2");
            writer.WriteAttributeString("orientation", "orthogonal");
            writer.WriteAttributeString("renderorder", "right-down");
            writer.WriteAttributeString("width", _map.Width.ToString());
            writer.WriteAttributeString("height", _map.Height.ToString());
            writer.WriteAttributeString("tilewidth", "32");
            writer.WriteAttributeString("tileheight", "32");
            writer.WriteAttributeString("infinite", "0");

            // Main tileset
            writer.WriteStartElement("tileset");
            writer.WriteAttributeString("firstgid", "1");
            writer.WriteAttributeString("source", $"{Constants.MapName}.tsx");
            writer.WriteEndElement();

            // Main tile layer
            writer.WriteStartElement("layer");
            writer.WriteAttributeString("id", "1");
            writer.WriteAttributeString("name", "Tile Layer 1");
            writer.WriteAttributeString("width", _map.Width.ToString());
            writer.WriteAttributeString("height", _map.Height.ToString());
            writer.WriteStartElement("data");
            writer.WriteAttributeString("encoding", "csv");

            for (int y = 0; y < _map.Height; y++)
            {
                for (int x = 0; x < _map.Width; x++)
                {
                    int tileId = y * _map.Width + x + 1;
                    writer.WriteString(tileId.ToString());
                    if (x < _map.Width - 1) writer.WriteString(",");
                }
                if (y < _map.Height - 1) writer.WriteString(",\n");
            }
            writer.WriteEndElement(); // </data>
            writer.WriteEndElement(); // </layer>

            // Debug layer (if grid is shown)
            if (_gameRenderer.ShowGrid)
            {
                string mapFolder = Path.Combine(Constants.OutputPath, Constants.MapName);
                string debugTexturePath = Path.Combine(mapFolder, "debug_outlines.png");
                using Texture2D debugTexture = CreateDebugTexture();
                SaveTextureToFile(debugTexture, debugTexturePath); // Save as a file
                int debugFirstGid = _map.Width * _map.Height + 1;

                // Debug tileset
                writer.WriteStartElement("tileset");
                writer.WriteAttributeString("firstgid", debugFirstGid.ToString());
                writer.WriteAttributeString("name", "DebugOutlines");
                writer.WriteAttributeString("tilewidth", "32");
                writer.WriteAttributeString("tileheight", "32");
                writer.WriteAttributeString("tilecount", "4");
                writer.WriteAttributeString("columns", "1");
                writer.WriteStartElement("image");
                writer.WriteAttributeString("source", "debug_outlines.png"); // Reference the file
                writer.WriteAttributeString("width", "32");
                writer.WriteAttributeString("height", "128");
                writer.WriteEndElement();
                writer.WriteEndElement();

                // Debug tile layer
                writer.WriteStartElement("layer");
                writer.WriteAttributeString("id", "2");
                writer.WriteAttributeString("name", "DebugOverlay");
                writer.WriteAttributeString("width", _map.Width.ToString());
                writer.WriteAttributeString("height", _map.Height.ToString());
                writer.WriteStartElement("data");
                writer.WriteAttributeString("encoding", "csv");

                for (int y = 0; y < _map.Height; y++)
                {
                    for (int x = 0; x < _map.Width; x++)
                    {
                        var tile = _map.Tiles[x, y];
                        int gid = 0; // 0 means no tile
                        if (tile.IsTeleport)
                            gid = debugFirstGid + 1; // Blue outline
                        else if (!tile.IsMoveAllowed)
                            gid = debugFirstGid;     // Red outline
                        else if (tile.IsFarmingAllowed)
                            gid = debugFirstGid + 2; // Green outline
                        else if (tile.IsWater)
                            gid = debugFirstGid + 3; // Cyan outline
                        writer.WriteString(gid.ToString());
                        if (x < _map.Width - 1) writer.WriteString(",");
                    }
                    if (y < _map.Height - 1) writer.WriteString(",\n");
                }
                writer.WriteEndElement(); // </data>
                writer.WriteEndElement(); // </layer>
            }

            writer.WriteEndElement(); // </map>
            writer.WriteEndDocument();
        }

        private Texture2D CreateDebugTexture()
        {
            Texture2D texture = _gameRenderer.CreateTexture2D(32, 128); // Four 32x32 tiles, stacked vertically
            Color[] data = new Color[32 * 128];
            Color[] colors = [Color.Red, Color.Blue, Color.Green, Color.Cyan]; // Order: blocked, teleport, farming, water

            for (int tile = 0; tile < 4; tile++)
            {
                Color color = colors[tile];
                int offsetY = tile * 32;
                // Top border
                for (int x = 0; x < 32; x++) data[x + offsetY * 32] = color;
                // Bottom border
                for (int x = 0; x < 32; x++) data[x + (offsetY + 31) * 32] = color;
                // Left border
                for (int y = 0; y < 32; y++) data[offsetY + y * 32] = color;
                // Right border
                for (int y = 0; y < 32; y++) data[31 + (offsetY + y) * 32] = color;
            }
            texture.SetData(data);
            return texture;
        }

        public void Dispose()
        {
        }
    }
}