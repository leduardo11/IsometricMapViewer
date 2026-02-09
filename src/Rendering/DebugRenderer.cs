using System;
using System.Linq;
using System.Numerics;
using IsometricMapViewer.Handlers;
using Raylib_cs;

namespace IsometricMapViewer.Rendering
{
    public class DebugRenderer : IDisposable
    {
        private readonly Font _font;
        public bool ShowHotkeys { get; set; } = false;

        public DebugRenderer(Font font)
        {
            _font = font;
        }

        public void Draw(CameraHandler camera, MapTile hoveredTile, Vector2 mouseWorldPos)
        {
            // Draw debug info
            string debugText = hoveredTile != null
                ? $"Hovered Tile: (X: {hoveredTile.X}, Y: {hoveredTile.Y})\n" +
                  $"Tile Sprite: {hoveredTile.TileSprite}\n" +
                  $"Object Sprite: {hoveredTile.ObjectSprite}\n"
                : "No tile hovered\n";
            
            Raylib.DrawTextEx(_font, debugText, new Vector2(10, 10), 20, 1, Color.White);

            // Draw hotkeys or help text
            int screenWidth = Raylib.GetScreenWidth();
            float fontSize = 20;
            
            if (ShowHotkeys)
            {
                float maxTextWidth = Constants.Hotkeys
                    .Select(h => Raylib.MeasureTextEx(_font, $"{h.KeyCombo}: {h.Description}", fontSize, 1).X)
                    .Max() + 10;
                float startX = screenWidth - maxTextWidth;
                float hotkeyY = 10;
                
                Raylib.DrawTextEx(_font, "Hotkeys:", new Vector2(startX, hotkeyY), fontSize, 1, Color.Yellow);
                hotkeyY += fontSize + 5;
                
                foreach (var (keyCombo, description) in Constants.Hotkeys)
                {
                    Raylib.DrawTextEx(_font, $"{keyCombo}: {description}", new Vector2(startX, hotkeyY), fontSize, 1, Color.White);
                    hotkeyY += fontSize + 5;
                }
            }
            else
            {
                string helpText = "Press F1 for Help";
                float textWidth = Raylib.MeasureTextEx(_font, helpText, fontSize, 1).X;
                float startX = screenWidth - textWidth - 10;
                Raylib.DrawTextEx(_font, helpText, new Vector2(startX, 10), fontSize, 1, Color.Yellow);
            }

            // Draw property legends for hovered tile
            if (hoveredTile != null)
            {
                int screenHeight = Raylib.GetScreenHeight();
                float legendX = 10;
                float legendY = screenHeight - 100;
                float squareSize = 10;
                float spacing = 5;

                legendY = DrawPropertyLegend("Blocked", Color.Red, !hoveredTile.IsMoveAllowed, legendX, legendY, squareSize, spacing);
                legendY = DrawPropertyLegend("Farmable", Color.Green, hoveredTile.IsFarmingAllowed, legendX, legendY, squareSize, spacing);
                legendY = DrawPropertyLegend("Water", new Color(0, 255, 255, 255), hoveredTile.IsWater, legendX, legendY, squareSize, spacing);
                legendY = DrawPropertyLegend("Teleport", Color.Blue, hoveredTile.IsTeleport, legendX, legendY, squareSize, spacing);
            }
        }

        private float DrawPropertyLegend(string propertyName, Color color, bool condition, float x, float y, float squareSize, float spacing)
        {
            if (condition)
            {
                Raylib.DrawRectangle((int)x, (int)y, (int)squareSize, (int)squareSize, color);
                Raylib.DrawTextEx(_font, propertyName, new Vector2(x + squareSize + spacing, y), 20, 1, Color.White);
                y += squareSize + spacing + 15;
            }
            return y;
        }

        public void Dispose()
        {
            // No cleanup needed for Raylib
        }
    }
}
