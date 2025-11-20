using System;
using System.Linq;
using System.Numerics;
using IsometricMapViewer.src;
using Raylib_cs;

namespace IsometricMapViewer
{
    public class DebugRenderer : IDisposable
    {
        public bool ShowHotkeys { get; set; } = false;
        private readonly int fontSize = 18;
        private readonly int lineSpacing = 22;

        public void Draw(CameraHandler camera, MapTile hoveredTile, Vector2 mouseWorldPos)
        {
            // Draw debug info
            string debugText = hoveredTile != null
                ? $"Hovered Tile: (X: {hoveredTile.X}, Y: {hoveredTile.Y})\n" +
                  $"Tile Sprite: {hoveredTile.TileSprite}\n" +
                  $"Object Sprite: {hoveredTile.ObjectSprite}\n"
                : "No tile hovered\n";
            Raylib.DrawText(debugText, 10, 10, fontSize, Color.White);

            // Draw hotkeys or help text
            int screenWidth = Raylib.GetScreenWidth();
            int maxTextWidth = ShowHotkeys
                ? Constants.Hotkeys.Max(h => Raylib.MeasureText($"{h.KeyCombo}: {h.Description}", fontSize)) + 10
                : Raylib.MeasureText("Press F1 for Help", fontSize) + 10;
            int startX = screenWidth - maxTextWidth;

            if (ShowHotkeys)
            {
                int hotkeyY = 10;
                Raylib.DrawText("Hotkeys:", startX, hotkeyY, fontSize, Color.Yellow);
                hotkeyY += lineSpacing;
                foreach (var (keyCombo, description) in Constants.Hotkeys)
                {
                    Raylib.DrawText($"{keyCombo}: {description}", startX, hotkeyY, fontSize, Color.White);
                    hotkeyY += lineSpacing;
                }
            }
            else
            {
                Raylib.DrawText("Press F1 for Help", startX, 10, fontSize, Color.Yellow);
            }

            // Draw property legends for hovered tile
            if (hoveredTile != null)
            {
                int legendX = 10;
                int legendY = Raylib.GetScreenHeight() - 100;
                int squareSize = 16;
                int spacing = 6;

                legendY = DrawPropertyLegend("Blocked", Color.Red, !hoveredTile.IsMoveAllowed, legendX, legendY, squareSize, spacing);
                legendY = DrawPropertyLegend("Farmable", Color.Green, hoveredTile.IsFarmingAllowed, legendX, legendY, squareSize, spacing);
                legendY = DrawPropertyLegend("Water", Color.Pink, hoveredTile.IsWater, legendX, legendY, squareSize, spacing);
                legendY = DrawPropertyLegend("Teleport", Color.Blue, hoveredTile.IsTeleport, legendX, legendY, squareSize, spacing);
            }
        }

        private int DrawPropertyLegend(string propertyName, Color color, bool condition, int x, int y, int squareSize, int spacing)
        {
            if (condition)
            {
                Raylib.DrawRectangle(x, y, squareSize, squareSize, color);
                Raylib.DrawText(propertyName, x + squareSize + spacing, y, fontSize, Color.White);
                y += squareSize + spacing;
            }
            return y;
        }

        public void Dispose()
        {
            // Nothing to dispose in Raylib for this renderer
        }
    }
}
