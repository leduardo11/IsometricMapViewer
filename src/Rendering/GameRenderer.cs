using System;
using System.Numerics;
using IsometricMapViewer.Handlers;
using IsometricMapViewer.Loaders;
using Raylib_cs;

namespace IsometricMapViewer.Rendering
{
    public class GameRenderer : IDisposable
    {
        private readonly Font _font;
        private readonly Map _map;
        private readonly GridRenderer _gridRenderer;
        private readonly SpriteLoader _spriteLoader;

        public bool ShowGrid { get; set; } = false;
        public bool ShowHotkeys { get; set; } = false;
        public bool ShowObjects { get; set; } = true;

        public GameRenderer(Font font, Map map, SpriteLoader spriteLoader)
        {
            _font = font;
            _map = map;
            _gridRenderer = new GridRenderer(map);
            _spriteLoader = spriteLoader;
        }

        public void DrawMap(CameraHandler camera)
        {
            Raylib.BeginMode2D(camera.Camera);
            RenderMap(camera.GetViewBounds(), true, ShowObjects);
            Raylib.EndMode2D();
        }

        public void DrawGrid(CameraHandler camera)
        {
            if (ShowGrid)
            {
                Raylib.BeginMode2D(camera.Camera);
                _gridRenderer.Draw(camera, true, true);
                Raylib.EndMode2D();
            }
        }

        public void DrawTileHighlight(CameraHandler camera, MapTile hoveredTile)
        {
            if (hoveredTile == null) return;
            
            Raylib.BeginMode2D(camera.Camera);
            Vector2 pos = ToScreenCoordinates(hoveredTile.X, hoveredTile.Y);
            Raylib.DrawRectangle(
                (int)pos.X, 
                (int)pos.Y, 
                Constants.TileWidth, 
                Constants.TileHeight, 
                new Color(255, 255, 0, 128)
            );
            Raylib.EndMode2D();
        }

        public void DrawDebugOverlay(CameraHandler camera, MapTile hoveredTile, Vector2 mouseWorldPos)
        {
            using var debugRenderer = new DebugRenderer(_font);
            debugRenderer.ShowHotkeys = this.ShowHotkeys;
            debugRenderer.Draw(camera, hoveredTile, mouseWorldPos);
        }

        public Image RenderFullMapToImage()
        {
            return RenderMapToImage(true, true);
        }

        public Image RenderObjectsToImage()
        {
            return RenderMapToImage(false, true);
        }

        private Image RenderMapToImage(bool drawTiles, bool drawObjects)
        {
            int mapWidth = _map.Width * Constants.TileWidth;
            int mapHeight = _map.Height * Constants.TileHeight;
            
            RenderTexture2D renderTarget = Raylib.LoadRenderTexture(mapWidth, mapHeight);
            
            Raylib.BeginTextureMode(renderTarget);
            Raylib.ClearBackground(Color.Blank);
            RenderMapDirect(drawTiles, drawObjects);
            Raylib.EndTextureMode();
            
            Image img = Raylib.LoadImageFromTexture(renderTarget.Texture);
            Raylib.UnloadRenderTexture(renderTarget);
            Raylib.ImageFlipVertical(ref img);
            
            return img;
        }

        public Image RenderMapWithoutObjectsToImage()
        {
            return RenderMapToImage(true, false);
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        private void RenderMap(Rectangle bounds, bool drawTiles, bool drawObjects)
        {
            var visibleTiles = _map.GetVisibleTiles(bounds);
            foreach (var tile in visibleTiles)
            {
                Vector2 pos = ToScreenCoordinates(tile.X, tile.Y);

                if (drawTiles)
                    DrawSpriteIfExists(tile.TileSprite, tile.TileFrame, pos, false);
                if (drawObjects)
                    DrawSpriteIfExists(tile.ObjectSprite, tile.ObjectFrame, pos, true);
            }
        }

        private void RenderMapDirect(bool drawTiles, bool drawObjects)
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

        private void DrawSpriteIfExists(int spriteId, int frameIndex, Vector2 position, bool isObjectSprite = false)
        {
            var texture = _spriteLoader.GetTexture(spriteId);

            if (texture.Id == 0) return;

            Constants.SpriteFrame frame = _spriteLoader.GetSpriteFrame(spriteId, frameIndex);
            Rectangle sourceRect = new(frame.Left, frame.Top, frame.Width, frame.Height);

            if (isObjectSprite)
            {
                position.X += frame.PivotX;
                position.Y += frame.PivotY;
            }
            
            Raylib.DrawTextureRec(texture, sourceRect, position, Color.White);
        }

        private static Vector2 ToScreenCoordinates(int tileX, int tileY)
        {
            return new Vector2(tileX * Constants.TileWidth, tileY * Constants.TileHeight);
        }
    }
}
