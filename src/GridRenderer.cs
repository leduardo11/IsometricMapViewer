using System.Numerics;
using IsometricMapViewer.src;
using Raylib_cs;

namespace IsometricMapViewer
{
    public class GridRenderer(Map map, Texture2D highlightTexture)
    {
        private readonly Map _map = map;
        private readonly Texture2D _highlightTexture = highlightTexture;

        public void Draw(CameraHandler camera, bool showGridLines, bool showTileProperties)
        {
            if (showGridLines)
            {
                DrawGridLines(camera);
            }

            if (showTileProperties)
            {
                DrawTileProperties(camera);
            }
        }

        private void DrawGridLines(CameraHandler camera)
        {
            float mapWidth = _map.Width * Constants.TileWidth;
            float mapHeight = _map.Height * Constants.TileHeight;

            for (int x = 0; x <= _map.Width; x++)
            {
                int posX = x * Constants.TileWidth;
                Vector2 start = WorldToScreen(new Vector2(posX, 0), camera);
                Vector2 end = WorldToScreen(new Vector2(posX, mapHeight), camera);
                Raylib.DrawLineEx(start, end, 1, Color.Black);
            }

            for (int y = 0; y <= _map.Height; y++)
            {
                int posY = y * Constants.TileHeight;
                Vector2 start = WorldToScreen(new Vector2(0, posY), camera);
                Vector2 end = WorldToScreen(new Vector2(mapWidth, posY), camera);
                Raylib.DrawLineEx(start, end, 1, Color.Black);
            }
        }

        private void DrawTileProperties(CameraHandler camera)
        {
            Rectangle viewBounds = camera.GetViewBounds();
            var visibleTiles = _map.GetVisibleTiles(viewBounds);

            foreach (var tile in visibleTiles)
            {
                Vector2 pos = ToScreenCoordinates(tile.X, tile.Y);
                pos = WorldToScreen(pos, camera);

                if (tile.IsTeleport) DrawTileOutline(pos, Color.Blue, camera);
                if (!tile.IsMoveAllowed) DrawTileOutline(pos, Color.Red, camera);
                if (tile.IsFarmingAllowed) DrawTileOutline(pos, Color.Green, camera);
                if (tile.IsWater) DrawTileOutline(pos, Color.Pink, camera);
            }
        }

        private void DrawTileOutline(Vector2 position, Color color, CameraHandler camera)
        {
            int tileWidth = (int)(Constants.TileWidth * camera.Zoom);
            int tileHeight = (int)(Constants.TileHeight * camera.Zoom);
            int thickness = 2;

            // Top
            Raylib.DrawRectangle((int)position.X, (int)position.Y, tileWidth, thickness, color);
            // Bottom
            Raylib.DrawRectangle((int)position.X, (int)(position.Y + tileHeight - thickness), tileWidth, thickness, color);
            // Left
            Raylib.DrawRectangle((int)position.X, (int)position.Y, thickness, tileHeight, color);
            // Right
            Raylib.DrawRectangle((int)(position.X + tileWidth - thickness), (int)position.Y, thickness, tileHeight, color);
        }

        private static Vector2 ToScreenCoordinates(int tileX, int tileY)
        {
            return new Vector2(tileX * Constants.TileWidth, tileY * Constants.TileHeight);
        }

        private static Vector2 WorldToScreen(Vector2 worldPos, CameraHandler camera)
        {
            Vector2 screenCenter = new Vector2(Raylib.GetScreenWidth() / 2f, Raylib.GetScreenHeight() / 2f);
            Vector2 offset = (worldPos - camera.Position) * camera.Zoom;
            return screenCenter + offset;
        }
    }
}
