using System;
using System.Collections.Generic;
using System.IO;

namespace IsometricMapViewer.Loaders
{
    public class SpriteLoader(GraphicsDevice graphicsDevice) : IDisposable
    {
        private readonly GraphicsDevice _graphicsDevice = graphicsDevice;
        private readonly Dictionary<string, SpriteFile> _spriteFiles = [];
        private readonly Dictionary<int, Texture2D> _spriteTextures = [];

        public void LoadSprites()
        {
            foreach (var (fileName, startIndex, count) in Constants.SpritesToLoad)
            {
                string filePath = Path.Combine("Sprites", fileName);
                var spriteFile = new SpriteFile(_graphicsDevice);
                try
                {
                    spriteFile.Load(filePath, startIndex);
                    _spriteFiles[fileName] = spriteFile;
                    foreach (var sprite in spriteFile.Sprites)
                    {
                        PreMultiplyAlpha(sprite.Texture);
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

            return new Constants.SpriteFrame { Left = 0, Top = 0, Width = Constants.TileWidth, Height = Constants.TileHeight, PivotX = 0, PivotY = 0 };
        }

        private void PreMultiplyAlpha(Texture2D texture)
        {
            using RenderTarget2D renderTarget = new(_graphicsDevice, texture.Width, texture.Height, false, SurfaceFormat.Color, DepthFormat.None);
            Viewport viewportBackup = _graphicsDevice.Viewport;
            try
            {
                _graphicsDevice.SetRenderTarget(renderTarget);
                _graphicsDevice.Clear(Color.Black);
                SpriteBatch spriteBatch = new(_graphicsDevice);
                spriteBatch.Begin(SpriteSortMode.Deferred, Constants.BlendColorBlendState);
                spriteBatch.Draw(texture, texture.Bounds, Color.White);
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, Constants.BlendAlphaBlendState);
                spriteBatch.Draw(texture, texture.Bounds, Color.White);
                spriteBatch.End();
                Color[] data = new Color[texture.Width * texture.Height];
                renderTarget.GetData(data);
                _graphicsDevice.SetRenderTarget(null);
                _graphicsDevice.Textures[0] = null;
                texture.SetData(data);
            }
            finally
            {
                _graphicsDevice.Viewport = viewportBackup;
            }
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
