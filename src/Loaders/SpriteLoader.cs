using System;
using System.Collections.Generic;
using System.IO;
using Raylib_cs;

namespace IsometricMapViewer.Loaders
{
    public class SpriteLoader : IDisposable
    {
        private readonly Dictionary<string, SpriteFile> _spriteFiles = [];
        private readonly Dictionary<int, Texture2D> _spriteTextures = [];

        public void LoadSprites()
        {
            foreach (var (fileName, startIndex, count) in Constants.SpritesToLoad)
            {
                string filePath = Path.Combine("resources", "sprites", fileName);
                var spriteFile = new SpriteFile();
                try
                {
                    spriteFile.Load(filePath, startIndex);
                    _spriteFiles[fileName] = spriteFile;
                    foreach (var sprite in spriteFile.Sprites)
                    {
                        _spriteTextures[sprite.Index] = sprite.Texture;
                    }
                }
                catch (Exception ex)
                {
                    ConsoleLogger.LogError($"Failed to load {filePath}: {ex.Message}");
                    spriteFile.Dispose();
                }
            }
        }

        public Constants.SpriteFrame GetSpriteFrame(int spriteId, int frameIndex)
        {
            foreach (var spriteFile in _spriteFiles.Values)
            {
                var sprite = spriteFile.GetSpriteById(spriteId);

                if (sprite != null && frameIndex >= 0 && frameIndex < sprite.Frames.Count)
                    return sprite.Frames[frameIndex];
            }

            return new Constants.SpriteFrame 
            { 
                Left = 0, 
                Top = 0, 
                Width = Constants.TileWidth, 
                Height = Constants.TileHeight, 
                PivotX = 0, 
                PivotY = 0 
            };
        }

        public Texture2D GetTexture(int spriteId)
        {
            _spriteTextures.TryGetValue(spriteId, out Texture2D texture);
            return texture;
        }

        public void Dispose()
        {
            foreach (var spriteFile in _spriteFiles.Values)
            {
                spriteFile.Dispose();
            }
        }
    }
}
