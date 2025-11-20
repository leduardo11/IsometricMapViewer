namespace IsometricMapViewer.src
{
    public class GridRenderer(SpriteBatch spriteBatch, Map map, Texture2D highlightTexture)
    {
        private readonly SpriteBatch _spriteBatch = spriteBatch;
        private readonly Map _map = map;
        private readonly Texture2D _highlightTexture = highlightTexture;

        public void Draw(CameraHandler camera, bool showGridLines, bool showTileProperties)
        {
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, camera.TransformMatrix);

            if (showGridLines)
            {
                DrawGridLines();
            }

            if (showTileProperties)
            {
                DrawTileProperties(camera);
            }

            _spriteBatch.End();
        }

        private void DrawGridLines()
        {
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
                if (tile.IsWater) DrawTileOutline(pos, Color.Cyan);
            }
        }

        private void DrawTileOutline(Vector2 position, Color color)
        {
            int tileWidth = Constants.TileWidth;
            int tileHeight = Constants.TileHeight;
            Rectangle rect = new Rectangle((int)position.X, (int)position.Y, tileWidth, tileHeight);
            int thickness = 2;
            _spriteBatch.Draw(_highlightTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            _spriteBatch.Draw(_highlightTexture, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
            _spriteBatch.Draw(_highlightTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            _spriteBatch.Draw(_highlightTexture, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
        }

        private static Vector2 ToScreenCoordinates(int tileX, int tileY)
        {
            return new Vector2(tileX * Constants.TileWidth, tileY * Constants.TileHeight);
        }
    }
}
