using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace IsometricMapViewer.src
{
    public class SpriteFile(GraphicsDevice graphicsDevice) : IDisposable
    {
        private readonly GraphicsDevice _graphicsDevice = graphicsDevice;
        private readonly List<Sprite> _sprites = [];

        public void Load(string filePath, int startIndex = 0)
        {
            byte[] buffer = File.ReadAllBytes(filePath);
            Load(buffer, startIndex);
        }

        public void Load(byte[] fileData, int startIndex = 0)
        {
            int offset = 0;
            int totalSprites = BitConverter.ToInt16(fileData, offset);
            offset += 2;

            for (int i = 0; i < totalSprites; i++)
            {
                Sprite sprite = new() { Index = startIndex + i };
                int frameCount = BitConverter.ToInt16(fileData, offset);
                offset += 2;
                sprite.ImageLength = BitConverter.ToInt32(fileData, offset);
                offset += 4;
                sprite.Width = BitConverter.ToInt32(fileData, offset);
                offset += 4;
                sprite.Height = BitConverter.ToInt32(fileData, offset);
                offset += 4;
                offset++; // Skip padding byte

                for (int f = 0; f < frameCount; f++)
                {
                    Constants.SpriteFrame frame = new()
                    {
                        Left = BitConverter.ToInt16(fileData, offset),
                        Top = BitConverter.ToInt16(fileData, offset + 2),
                        Width = BitConverter.ToInt16(fileData, offset + 4),
                        Height = BitConverter.ToInt16(fileData, offset + 6),
                        PivotX = BitConverter.ToInt16(fileData, offset + 8),
                        PivotY = BitConverter.ToInt16(fileData, offset + 10)
                    };
                    sprite.Frames.Add(frame);
                    offset += 12;
                }
                _sprites.Add(sprite);
            }

            for (int i = 0; i < totalSprites; i++)
            {
                _sprites[i].StartLocation = BitConverter.ToInt32(fileData, offset);
                offset += 4;
                _sprites[i].ImageData = new byte[_sprites[i].ImageLength];
                Buffer.BlockCopy(fileData, offset, _sprites[i].ImageData, 0, _sprites[i].ImageLength);
                offset += _sprites[i].ImageLength;

                try
                {
                    using MemoryStream stream = new(_sprites[i].ImageData);
                    _sprites[i].Texture = Texture2D.FromStream(_graphicsDevice, stream);
                }
                catch (Exception ex)
                {
                    _sprites[i].Texture = new Texture2D(_graphicsDevice, 32, 32);
                    Color[] data = new Color[32 * 32];
                    Array.Fill(data, Color.Magenta);
                    _sprites[i].Texture.SetData(data);
                    ConsoleLogger.LogError($"Failed to load texture for sprite {_sprites[i].Index}: {ex.Message}");
                }
            }
        }

        public Sprite GetSpriteById(int id) => _sprites.FirstOrDefault(s => s.Index == id);
        public List<Sprite> Sprites => _sprites;

        public void Dispose()
        {
            foreach (Sprite sprite in _sprites)
            {
                sprite.Texture?.Dispose();
            }
        }
    }

    public class Sprite
    {
        public List<Constants.SpriteFrame> Frames { get; } = new List<Constants.SpriteFrame>();
        public byte[] ImageData { get; set; }
        public Texture2D Texture { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Index { get; set; }
        public int ImageLength { get; set; }
        public int StartLocation { get; set; }
    }
}
