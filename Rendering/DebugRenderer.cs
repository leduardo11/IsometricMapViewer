using System;
using System.Linq;
using IsometricMapViewer.Handlers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace IsometricMapViewer.Rendering
{
    public class DebugRenderer : IDisposable
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _debugHighlightTexture;
        public bool ShowHotkeys { get; set; } = false;

        public DebugRenderer(SpriteBatch spriteBatch, SpriteFont font, GraphicsDevice graphicsDevice)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _debugHighlightTexture = new Texture2D(graphicsDevice, 1, 1);
            _debugHighlightTexture.SetData(new[] { Color.White });
        }

        public void Draw(CameraHandler camera, MapTile hoveredTile, Vector2 mouseWorldPos)
        {
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            string debugText = hoveredTile != null
                ? $"Hovered Tile: (X: {hoveredTile.X}, Y: {hoveredTile.Y})\n" +
                  $"Tile Sprite: {hoveredTile.TileSprite}\n" +
                  $"Object Sprite: {hoveredTile.ObjectSprite}\n"
                : "No tile hovered\n";
            _spriteBatch.DrawString(_font, debugText, new Vector2(10, 10), Color.White);

            float maxTextWidth = ShowHotkeys
                ? Constants.Hotkeys.Max(h => _font.MeasureString($"{h.KeyCombo}: {h.Description}").X) + 10
                : _font.MeasureString("Press F1 for Help").X + 10;
            float startX = _spriteBatch.GraphicsDevice.Viewport.Width - maxTextWidth;

            if (ShowHotkeys)
            {
                float hotkeyY = 10;
                _spriteBatch.DrawString(_font, "Hotkeys:", new Vector2(startX, hotkeyY), Color.Yellow);
                hotkeyY += _font.LineSpacing;
                foreach (var (keyCombo, description) in Constants.Hotkeys)
                {
                    _spriteBatch.DrawString(_font, $"{keyCombo}: {description}", new Vector2(startX, hotkeyY), Color.White);
                    hotkeyY += _font.LineSpacing;
                }
            }
            else
            {
                _spriteBatch.DrawString(_font, "Press F1 for Help", new Vector2(startX, 10), Color.Yellow);
            }

            if (hoveredTile != null)
            {
                float legendX = 10;
                float legendY = _spriteBatch.GraphicsDevice.Viewport.Height - 100;
                float squareSize = 10;
                float spacing = 5;

                if (!hoveredTile.IsMoveAllowed)
                {
                    _spriteBatch.Draw(_debugHighlightTexture, new Rectangle((int)legendX, (int)legendY, (int)squareSize, (int)squareSize), Color.Red);
                    _spriteBatch.DrawString(_font, "Blocked", new Vector2(legendX + squareSize + spacing, legendY), Color.White);
                    legendY += squareSize + spacing;
                }

                if (hoveredTile.IsFarmingAllowed)
                {
                    _spriteBatch.Draw(_debugHighlightTexture, new Rectangle((int)legendX, (int)legendY, (int)squareSize, (int)squareSize), Color.Green);
                    _spriteBatch.DrawString(_font, "Farmable", new Vector2(legendX + squareSize + spacing, legendY), Color.White);
                    legendY += squareSize + spacing;
                }

                if (hoveredTile.IsWater)
                {
                    _spriteBatch.Draw(_debugHighlightTexture, new Rectangle((int)legendX, (int)legendY, (int)squareSize, (int)squareSize), Color.Cyan);
                    _spriteBatch.DrawString(_font, "Water", new Vector2(legendX + squareSize + spacing, legendY), Color.White);
                    legendY += squareSize + spacing;
                }

                if (hoveredTile.IsTeleport)
                {
                    _spriteBatch.Draw(_debugHighlightTexture, new Rectangle((int)legendX, (int)legendY, (int)squareSize, (int)squareSize), Color.Blue);
                    _spriteBatch.DrawString(_font, "Teleport", new Vector2(legendX + squareSize + spacing, legendY), Color.White);
                }
            }

            _spriteBatch.End();
        }

        public void Dispose()
        {
            _debugHighlightTexture.Dispose();
        }
    }
}