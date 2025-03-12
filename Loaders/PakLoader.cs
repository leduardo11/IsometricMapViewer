using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace IsometricMapViewer.Loaders
{
    public class PakLoader(GraphicsDevice graphicsDevice) : IDisposable
    {
        private readonly GraphicsDevice _graphicsDevice = graphicsDevice;
        private readonly Dictionary<int, Texture2D> _spriteTextures = [];

        public void Load(string filePath, int startIndex)
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                using var reader = new BinaryReader(stream);
                stream.Seek(20, SeekOrigin.Begin);
                int totalSprites = reader.ReadInt32();
                ConsoleLogger.LogInfo($"Loading {totalSprites} sprites from {filePath}");
                stream.Seek(24, SeekOrigin.Begin);

                for (int i = 0; i < totalSprites; i++)
                {
                    int width = reader.ReadInt32();
                    int height = reader.ReadInt32();
                    int dataLength = reader.ReadInt32();
                    byte[] imageData = reader.ReadBytes(dataLength);
                    Texture2D texture;

                    try
                    {
                        using var memoryStream = new MemoryStream(imageData);
                        texture = Texture2D.FromStream(_graphicsDevice, memoryStream);
                    }
                    catch (Exception ex)
                    {
                        ConsoleLogger.LogError($"Failed to load sprite {i} from {filePath}: {ex.Message}");
                        texture = new Texture2D(_graphicsDevice, 32, 32);
                        Color[] fallbackData = new Color[32 * 32];
                        Array.Fill(fallbackData, Color.Magenta);
                        texture.SetData(fallbackData);
                    }
                    _spriteTextures[startIndex + i] = texture;
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError($"Failed to load {filePath}: {ex.Message}");
            }
        }

        public Texture2D GetTexture(int spriteId)
        {
            _spriteTextures.TryGetValue(spriteId, out Texture2D texture);
            return texture;
        }

        public void Dispose()
        {
            foreach (var texture in _spriteTextures.Values)
            {
                texture?.Dispose();
            }
            _spriteTextures.Clear();
        }
    }
}