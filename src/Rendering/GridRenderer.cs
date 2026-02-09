using System.Numerics;
using IsometricMapViewer.Handlers;
using Raylib_cs;

namespace IsometricMapViewer.Rendering
{
    public class GridRenderer(Map map)
    {
        private readonly Map _map = map;

        public void Draw(CameraHandler camera, bool showGridLines, bool showTileProperties)
        {
            if (showGridLines)
            {
                DrawGridLines();
            }

            if (showTileProperties)
            {
                DrawTileProperties(camera);
            }
        }

        private void DrawGridLines()
        {
            float mapWidth = _map.Width * Constants.TileWidth;
            float mapHeight = _map.Height * Constants.TileHeight;

            for (int x = 0; x <= _map.Width; x++)
            {
                int posX = x * Constants.TileWidth;
                Raylib.DrawLine(posX, 0, posX, (int)mapHeight, Color.Black);
            }

            for (int y = 0; y <= _map.Height; y++)
            {
                int posY = y * Constants.TileHeight;
                Raylib.DrawLine(0, posY, (int)mapWidth, posY, Color.Black);
            }
        }

        private void DrawTileProperties(CameraHandler camera)
        {
            Rectangle viewBounds = camera.GetViewBounds();
            var visibleTiles = _map.GetVisibleTiles(viewBounds);

            foreach (var tile in visibleTiles)
            {
                Vector2 pos = ToScreenCoordinates(tile.X, tile.Y);
                if (tile.IsTeleport) DrawTileOutline(pos, Color.Blue);
                if (!tile.IsMoveAllowed) DrawTileOutline(pos, Color.Red);
                if (tile.IsFarmingAllowed) DrawTileOutline(pos, Color.Green);
                if (tile.IsWater) DrawTileOutline(pos, new Color(0, 255, 255, 255));
            }
        }

        private void DrawTileOutline(Vector2 position, Color color)
        {
            int tileWidth = Constants.TileWidth;
            int tileHeight = Constants.TileHeight;
            int thickness = 2;
            
            // Top
            Raylib.DrawRectangle((int)position.X, (int)position.Y, tileWidth, thickness, color);
            // Bottom
            Raylib.DrawRectangle((int)position.X, (int)position.Y + tileHeight - thickness, tileWidth, thickness, color);
            // Left
            Raylib.DrawRectangle((int)position.X, (int)position.Y, thickness, tileHeight, color);
            // Right
            Raylib.DrawRectangle((int)position.X + tileWidth - thickness, (int)position.Y, thickness, tileHeight, color);
        }

        private static Vector2 ToScreenCoordinates(int tileX, int tileY)
        {
            return new Vector2(tileX * Constants.TileWidth, tileY * Constants.TileHeight);
        }
    }
}
