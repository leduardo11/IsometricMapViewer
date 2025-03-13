using System;
using System.Collections.Generic;
using System.Linq;
using IsometricMapViewer.Handlers;
using IsometricMapViewer.Loaders;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace IsometricMapViewer.Rendering
{
    public class GameRenderer : IDisposable
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Map _map;
        private readonly Texture2D _highlightTexture;
        private readonly GridRenderer _gridRenderer;
        private readonly SpriteLoader _spriteLoader;
        private const int _thumbnailPanelWidth = 200; // Width of the side panel
        private const int _thumbnailSize = 64; // Max size for thumbnails
        private const int _thumbnailSpacing = 10; // Space between thumbnails
        private int _thumbnailScrollOffset = 0; // Index of the first visible thumbnail
        private List<KeyValuePair<int, Texture2D>> _sortedTextures; // Cached sorted sprite list

        public bool ShowGrid { get; set; } = false;
        public bool ShowHotkeys { get; set; } = false;
        public bool ShowObjects { get; set; } = true;
        public bool ShowThumbnails { get; set; } = false;

        public GameRenderer(SpriteBatch spriteBatch, SpriteFont font, GraphicsDevice graphicsDevice, Map map, SpriteLoader spriteLoader)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _map = map;
            _highlightTexture = new Texture2D(graphicsDevice, 1, 1);
            _highlightTexture.SetData(new[] { Color.White });
            _gridRenderer = new GridRenderer(spriteBatch, map, _highlightTexture);
            _spriteLoader = spriteLoader;
            _sortedTextures = [.. _spriteLoader.GetAllTextures().OrderBy(k => k.Key)];
        }

        public void DrawMap(CameraHandler camera)
        {
            RenderMap(camera.TransformMatrix, camera.GetViewBounds(), true, ShowObjects);
        }

        public void DrawGrid(CameraHandler camera)
        {
            if (ShowGrid)
            {
                _gridRenderer.Draw(camera, true, true);
            }
        }

        public void DrawTileHighlight(CameraHandler camera, MapTile hoveredTile)
        {
            if (hoveredTile == null) return;
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, camera.TransformMatrix);
            Vector2 pos = ToScreenCoordinates(hoveredTile.X, hoveredTile.Y);
            Rectangle highlightRect = new((int)pos.X, (int)pos.Y, Constants.TileWidth, Constants.TileHeight);
            _spriteBatch.Draw(_highlightTexture, highlightRect, null, Color.Yellow * 0.5f, 0f, Vector2.Zero, SpriteEffects.None, 0f);
            _spriteBatch.End();
        }

        public void DrawDebugOverlay(CameraHandler camera, MapTile hoveredTile, Vector2 mouseWorldPos)
        {
            using var debugRenderer = new DebugRenderer(_spriteBatch, _font, _spriteBatch.GraphicsDevice);
            debugRenderer.ShowHotkeys = this.ShowHotkeys;
            debugRenderer.Draw(camera, hoveredTile, mouseWorldPos);
        }

        public void DrawThumbnails()
        {
            if (!ShowThumbnails) return;

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            _spriteBatch.Draw(_highlightTexture, ThumbnailPanelBounds, Color.Gray * 0.5f);

            int verticalStep = _thumbnailSize + _thumbnailSpacing;
            int visibleThumbnails = (int)Math.Ceiling((float)_spriteBatch.GraphicsDevice.Viewport.Height / verticalStep) + 1;

            for (int i = _thumbnailScrollOffset; i < _thumbnailScrollOffset + visibleThumbnails && i < _sortedTextures.Count; i++)
            {
                var kvp = _sortedTextures[i];
                int spriteId = kvp.Key;
                Texture2D texture = kvp.Value;

                // Scale thumbnail to fit within thumbnailSize while preserving aspect ratio
                float scale = Math.Min((float)_thumbnailSize / texture.Width, (float)_thumbnailSize / texture.Height);
                int destWidth = (int)(texture.Width * scale);
                int destHeight = (int)(texture.Height * scale);

                // Center thumbnail horizontally in the panel
                int x = ThumbnailPanelBounds.X + (_thumbnailPanelWidth - destWidth) / 2;
                int y = ThumbnailPanelBounds.Y + (i - _thumbnailScrollOffset) * verticalStep;

                Rectangle destRect = new Rectangle(x, y, destWidth, destHeight);
                _spriteBatch.Draw(texture, destRect, Color.White);

                // Draw sprite ID below the thumbnail
                string idText = $"ID: {spriteId}";
                Vector2 textPosition = new Vector2(x, y + _thumbnailSize + 5);
                _spriteBatch.DrawString(_font, idText, textPosition, Color.White);
            }
            _spriteBatch.End();
        }

        public Texture2D RenderFullMapToTexture()
        {
            return RenderMapToTexture(true, true);
        }

        public Texture2D RenderObjectsToTexture()
        {
            return RenderMapToTexture(false, true);
        }

        public int ThumbnailScrollOffset
        {
            get => _thumbnailScrollOffset;
            set => _thumbnailScrollOffset = value;
        }

        public Rectangle ThumbnailPanelBounds
        {
            get
            {
                int panelX = _spriteBatch.GraphicsDevice.Viewport.Width - _thumbnailPanelWidth;
                int panelY = 0;
                int panelHeight = _spriteBatch.GraphicsDevice.Viewport.Height;
                return new Rectangle(panelX, panelY, _thumbnailPanelWidth, panelHeight);
            }
        }

        public void ScrollThumbnails(int direction)
        {
            int verticalStep = _thumbnailSize + _thumbnailSpacing;
            int visibleThumbnails = (int)Math.Ceiling((float)_spriteBatch.GraphicsDevice.Viewport.Height / verticalStep);
            int maxOffset = Math.Max(0, _sortedTextures.Count - visibleThumbnails);
            _thumbnailScrollOffset = MathHelper.Clamp(_thumbnailScrollOffset + direction, 0, maxOffset);
        }

        public Texture2D CreateTexture2D(int width, int height)
        {
            return new Texture2D(_spriteBatch.GraphicsDevice, width, height);
        }

        public void Dispose()
        {
            _highlightTexture.Dispose();
        }

        private RenderTarget2D RenderMapToTexture(bool drawTiles, bool drawObjects)
        {
            int mapWidth = _map.Width * Constants.TileWidth;
            int mapHeight = _map.Height * Constants.TileHeight;
            RenderTarget2D renderTarget = new(_spriteBatch.GraphicsDevice, mapWidth, mapHeight);
            RenderMap(Matrix.Identity, null, drawTiles, drawObjects, renderTarget);
            return renderTarget;
        }

        private void RenderMap(Matrix? transform, Rectangle? bounds, bool drawTiles,
                               bool drawObjects, RenderTarget2D renderTarget = null)
        {
            if (renderTarget != null)
            {
                _spriteBatch.GraphicsDevice.SetRenderTarget(renderTarget);
                _spriteBatch.GraphicsDevice.Clear(Color.Transparent);
            }

            _spriteBatch.Begin(
                SpriteSortMode.Texture,
                renderTarget != null ? Constants.PremultipliedBlendState : BlendState.NonPremultiplied,
                SamplerState.PointClamp,
                null, null, null, transform
            );

            if (bounds.HasValue)
            {
                var visibleTiles = _map.GetVisibleTiles(bounds.Value);
                foreach (var tile in visibleTiles)
                {
                    Vector2 pos = ToScreenCoordinates(tile.X, tile.Y);

                    if (drawTiles)
                        DrawSpriteIfExists(tile.TileSprite, tile.TileFrame, pos, false);
                    if (drawObjects)
                        DrawSpriteIfExists(tile.ObjectSprite, tile.ObjectFrame, pos, true);
                }
            }
            else
            {
                for (int y = 0; y < _map.Height; y++)
                {
                    for (int x = 0; x < _map.Width; x++)
                    {
                        var tile = _map.Tiles[x, y];
                        Vector2 pos = ToScreenCoordinates(x, y);

                        if (drawTiles)
                            DrawSpriteIfExists(tile.TileSprite, tile.TileFrame, pos, false);
                        if (drawObjects)
                            DrawSpriteIfExists(tile.ObjectSprite, tile.ObjectFrame, pos, true);
                    }
                }
            }

            _spriteBatch.End();

            if (renderTarget != null)
            {
                _spriteBatch.GraphicsDevice.SetRenderTarget(null);
            }
        }

        private void DrawSpriteIfExists(int spriteId, int frameIndex, Vector2 position, bool isObjectSprite = false)
        {
            var texture = _spriteLoader.GetTexture(spriteId);

            if (texture == null) return;

            Constants.SpriteFrame frame = _spriteLoader.GetSpriteFrame(spriteId, frameIndex);
            Rectangle sourceRect = new(frame.Left, frame.Top, frame.Width, frame.Height);

            if (isObjectSprite)
            {
                position.X += frame.PivotX;
                position.Y += frame.PivotY;
            }
            _spriteBatch.Draw(texture, position, sourceRect, Color.White);
        }

        private static Vector2 ToScreenCoordinates(int tileX, int tileY)
        {
            return new Vector2(tileX * Constants.TileWidth, tileY * Constants.TileHeight);
        }
    }
}