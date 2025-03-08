using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IsometricMapViewer.Handlers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace IsometricMapViewer.Rendering
{
    public class GameRenderer : IDisposable
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Map _map;
        private readonly Texture2D _highlightTexture;
        private readonly Dictionary<int, Texture2D> _spriteTextures = [];
        private readonly Dictionary<string, SpriteFile> _spriteFiles = [];
        public bool ShowGrid { get; set; } = false;
        public bool ShowObjects { get; set; } = true;

        public GameRenderer(SpriteBatch spriteBatch, SpriteFont font, GraphicsDevice graphicsDevice, Map map)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _map = map;
            _highlightTexture = new Texture2D(graphicsDevice, 1, 1);
            _highlightTexture.SetData(new[] { Color.White });
        }

        public void LoadSprites()
        {
            foreach (var (fileName, startIndex, count) in Constants.SpritesToLoad)
            {
                string filePath = Path.Combine("Sprites", fileName);
                var spriteFile = new SpriteFile(_spriteBatch.GraphicsDevice);

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

        public void DrawMap(CameraHandler camera)
        {
            _spriteBatch.Begin(SpriteSortMode.Texture, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null, null, camera.TransformMatrix);
            Rectangle viewBounds = camera.GetViewBounds();
            var visibleTiles = _map.GetVisibleTiles(viewBounds);

            foreach (var tile in visibleTiles)
            {
                Vector2 pos = ToScreenCoordinates(tile.X, tile.Y);
                DrawSpriteIfExists(tile.TileSprite, tile.TileFrame, pos, false);
            }

            if (ShowObjects)
            {
                foreach (var tile in visibleTiles)
                {
                    Vector2 pos = ToScreenCoordinates(tile.X, tile.Y);
                    DrawSpriteIfExists(tile.ObjectSprite, tile.ObjectFrame, pos, true);
                }
            }
            _spriteBatch.End();
        }

        public void DrawGrid(CameraHandler camera)
        {
            if (!ShowGrid)
                return;

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, camera.TransformMatrix);

            float mapWidth = _map.Width * Constants.TileWidth;
            float mapHeight = _map.Height * Constants.TileHeight;

            for (int x = 0; x <= _map.Width; x++)
            {
                int posX = x * Constants.TileWidth;
                Rectangle verticalLine = new Rectangle(posX, 0, 1, (int)mapHeight);
                _spriteBatch.Draw(_highlightTexture, verticalLine, Color.Black);
            }

            for (int y = 0; y <= _map.Height; y++)
            {
                int posY = y * Constants.TileHeight;
                Rectangle horizontalLine = new Rectangle(0, posY, (int)mapWidth, 1);
                _spriteBatch.Draw(_highlightTexture, horizontalLine, Color.Black);
            }

            DrawTileHighlights(camera);
            _spriteBatch.End();
        }

        public void DrawTileHighlight(CameraHandler camera, MapTile hoveredTile)
        {
            if (hoveredTile == null)
                return;

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, camera.TransformMatrix);
            Vector2 pos = ToScreenCoordinates(hoveredTile.X, hoveredTile.Y);
            Rectangle highlightRect = new Rectangle((int)pos.X, (int)pos.Y, Constants.TileWidth, Constants.TileHeight);
            _spriteBatch.Draw(_highlightTexture, highlightRect, null, Color.Yellow * 0.5f, 0f, Vector2.Zero, SpriteEffects.None, 0f);
            _spriteBatch.End();
        }

        private void DrawTileHighlights(CameraHandler camera)
        {
            Rectangle viewBounds = camera.GetViewBounds();
            var visibleTiles = _map.GetVisibleTiles(viewBounds);
            foreach (var tile in visibleTiles)
            {
                Vector2 pos = ToScreenCoordinates(tile.X, tile.Y);
                if (tile.IsTeleport)
                    DrawTileOutline(pos, Color.Blue);
                if (!tile.IsMoveAllowed)
                    DrawTileOutline(pos, Color.Red);
            }
        }

        private void DrawTileOutline(Vector2 position, Color color)
        {
            int tileWidth = Constants.TileWidth;
            int tileHeight = Constants.TileHeight;
            Rectangle rect = new((int)position.X, (int)position.Y, tileWidth, tileHeight);
            int thickness = 2;
            _spriteBatch.Draw(_highlightTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            _spriteBatch.Draw(_highlightTexture, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
            _spriteBatch.Draw(_highlightTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            _spriteBatch.Draw(_highlightTexture, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
        }

        private void DrawSpriteIfExists(int spriteId, int frameIndex, Vector2 position, bool isObjectSprite = false)
        {
            if (!_spriteTextures.TryGetValue(spriteId, out Texture2D texture))
                return;
            Constants.SpriteFrame frame = GetSpriteFrame(spriteId, frameIndex);
            Rectangle sourceRect = new(frame.Left, frame.Top, frame.Width, frame.Height);
            if (isObjectSprite)
            {
                position.X += frame.PivotX;
                position.Y += frame.PivotY;
            }
            _spriteBatch.Draw(texture, position, sourceRect, Color.White);
        }

        public void DrawDebugOverlay(CameraHandler camera, MapTile hoveredTile, Vector2 mouseWorldPos)
        {
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            string debugText = hoveredTile != null ? $"Hovered Tile: (X: {hoveredTile.X}, Y: {hoveredTile.Y})\n" +
                                                     $"Tile Sprite: {hoveredTile.TileSprite} ({GetSpriteFileName(hoveredTile.TileSprite)})\n" +
                                                     $"Object Sprite: {hoveredTile.ObjectSprite} ({GetSpriteFileName(hoveredTile.ObjectSprite)})\n" +
                                                     $"Move: {hoveredTile.IsMoveAllowed}, Teleport: {hoveredTile.IsTeleport}\n" +
                                                     $"Farm: {hoveredTile.IsFarmingAllowed}, Water: {hoveredTile.IsWater}\n" : "No tile hovered\n";
            debugText += $"Zoom Level: {camera.Zoom:F2}\nGrid: {(ShowGrid ? "On" : "Off")}\nObjects: {(ShowObjects ? "On" : "Off")}";
            _spriteBatch.DrawString(_font, debugText, new Vector2(10, 10), Color.White);
            _spriteBatch.End();
        }

        public Texture2D RenderFullMapToTexture()
        {
            int mapWidth = _map.Width * Constants.TileWidth;
            int mapHeight = _map.Height * Constants.TileHeight;
            RenderTarget2D renderTarget = new(_spriteBatch.GraphicsDevice, mapWidth, mapHeight);
            _spriteBatch.GraphicsDevice.SetRenderTarget(renderTarget);
            _spriteBatch.GraphicsDevice.Clear(Color.Transparent);
            _spriteBatch.Begin(SpriteSortMode.Texture, BlendState.NonPremultiplied, SamplerState.PointClamp);

            for (int y = 0; y < _map.Height; y++)
            {
                for (int x = 0; x < _map.Width; x++)
                {
                    var tile = _map.Tiles[x, y];
                    Vector2 pos = new(x * Constants.TileWidth, y * Constants.TileHeight);
                    DrawSpriteIfExists(tile.TileSprite, tile.TileFrame, pos, false);
                    DrawSpriteIfExists(tile.ObjectSprite, tile.ObjectFrame, pos, true);
                }
            }
            _spriteBatch.End();
            _spriteBatch.GraphicsDevice.SetRenderTarget(null);
            return renderTarget;
        }

        public Texture2D CreateTilesetTexture(List<(int SpriteID, int FrameIndex)> uniqueTiles, int columns)
        {
            int tileWidth = Constants.TileWidth;  // 32
            int tileHeight = Constants.TileHeight; // 32
            int tileCount = uniqueTiles.Count;
            int rows = (tileCount + columns - 1) / columns; // Ceiling division
            int imageWidth = columns * tileWidth;
            int imageHeight = rows * tileHeight;

            // Use _spriteBatch.GraphicsDevice instead of _graphicsDevice
            RenderTarget2D renderTarget = new(_spriteBatch.GraphicsDevice, imageWidth, imageHeight);

            _spriteBatch.GraphicsDevice.SetRenderTarget(renderTarget);
            _spriteBatch.GraphicsDevice.Clear(Color.Transparent);

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);

            for (int i = 0; i < tileCount; i++)
            {
                var (spriteID, frameIndex) = uniqueTiles[i];
                var sprite = GetSprite(spriteID);

                if (sprite != null && frameIndex >= 0 && frameIndex < sprite.Frames.Count)
                {
                    var frame = sprite.Frames[frameIndex];
                    Rectangle sourceRect = new(frame.Left, frame.Top, frame.Width, frame.Height);
                    Vector2 position = new((i % columns) * tileWidth, (i / columns) * tileHeight);
                    _spriteBatch.Draw(sprite.Texture, position, sourceRect, Color.White);
                }
            }
            _spriteBatch.End();
            _spriteBatch.GraphicsDevice.SetRenderTarget(null);

            return renderTarget;
        }

        public Texture2D CreateTexture2D(int width, int height)
        {
            return new Texture2D(_spriteBatch.GraphicsDevice, width, height);
        }

        public void Dispose()
        {
            foreach (var spriteFile in _spriteFiles.Values)
            {
                spriteFile.Dispose();
            }
            _highlightTexture.Dispose();
        }

        private static string GetSpriteFileName(int spriteId)
        {
            if (spriteId == -1) return "None";

            var spriteLoad = Constants.SpritesToLoad
                .FirstOrDefault(s => spriteId >= s.startIndex && spriteId < s.startIndex + s.count);

            return spriteLoad != default
                ? spriteLoad.fileName
                : "Unknown";
        }

        private static Vector2 ToScreenCoordinates(int tileX, int tileY)
        {
            return new Vector2(tileX * Constants.TileWidth, tileY * Constants.TileHeight);
        }

        private Constants.SpriteFrame GetSpriteFrame(int spriteId, int frameIndex)
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

        private Sprite GetSprite(int spriteID)
        {
            foreach (var spriteFile in _spriteFiles.Values)
            {
                var sprite = spriteFile.GetSpriteById(spriteID);
                if (sprite != null) return sprite;
            }
            return null;
        }
    }
}