using System;
using System.Linq;
using IsometricMapViewer.Handlers;

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

            // Draw debug info
            string debugText = hoveredTile != null
                ? $"Hovered Tile: (X: {hoveredTile.X}, Y: {hoveredTile.Y})\n" +
                  $"Tile Sprite: {hoveredTile.TileSprite}\n" +
                  $"Object Sprite: {hoveredTile.ObjectSprite}\n"
                : "No tile hovered\n";
            _spriteBatch.DrawString(_font, debugText, new Vector2(10, 10), Color.White);

            // Draw hotkeys or help text
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

            // Draw property legends for hovered tile
            if (hoveredTile != null)
            {
                float legendX = 10;
                float legendY = _spriteBatch.GraphicsDevice.Viewport.Height - 100;
                float squareSize = 10;
                float spacing = 5;

                // Use helper method to draw each property
                legendY = DrawPropertyLegend("Blocked", Color.Red, !hoveredTile.IsMoveAllowed, legendX, legendY, squareSize, spacing);
                legendY = DrawPropertyLegend("Farmable", Color.Green, hoveredTile.IsFarmingAllowed, legendX, legendY, squareSize, spacing);
                legendY = DrawPropertyLegend("Water", Color.Cyan, hoveredTile.IsWater, legendX, legendY, squareSize, spacing);
                legendY = DrawPropertyLegend("Teleport", Color.Blue, hoveredTile.IsTeleport, legendX, legendY, squareSize, spacing);
            }

            _spriteBatch.End();
        }

        private float DrawPropertyLegend(string propertyName, Color color, bool condition, float x, float y, float squareSize, float spacing)
        {
            if (condition)
            {
                _spriteBatch.Draw(_debugHighlightTexture, new Rectangle((int)x, (int)y, (int)squareSize, (int)squareSize), color);
                _spriteBatch.DrawString(_font, propertyName, new Vector2(x + squareSize + spacing, y), Color.White);
                y += squareSize + spacing;
            }
            return y;
        }

        public void Dispose()
        {
            _debugHighlightTexture.Dispose();
        }
    }
}
