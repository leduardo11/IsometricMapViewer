using System;
using System.IO;
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
                lock (_exportLock) { _isExporting = false; }
            }
        }

        private static void SaveTextureToFile(Texture2D texture, string filePath)
        {
            using FileStream stream = new(filePath, FileMode.Create);
            texture.SaveAsPng(stream, texture.Width, texture.Height);
            ConsoleLogger.LogInfo($"Map PNG created at: {filePath}");
        }

        public void Dispose() { }
    }
}