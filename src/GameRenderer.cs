using System;
using System.Numerics;
using IsometricMapViewer.src;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace IsometricMapViewer
{
    public class GameRenderer : IDisposable
    {
        private readonly Map _map;
        private readonly Texture2D _highlightTexture;
        private readonly GridRenderer _gridRenderer;
        private readonly SpriteLoader _spriteLoader;

        public bool ShowGrid { get; set; } = false;
        public bool ShowHotkeys { get; set; } = false;
        public bool ShowObjects { get; set; } = true;

        public GameRenderer(Map map, SpriteLoader spriteLoader)
        {
            _map = map;
            _spriteLoader = spriteLoader;
            _highlightTexture = CreateHighlightTexture();
            _gridRenderer = new GridRenderer(map, _highlightTexture);
        }

        public void DrawMap(CameraHandler camera)
        {
            RenderMap(camera, camera.GetViewBounds(), true, ShowObjects);
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
            Vector2 pos = ToScreenCoordinates(hoveredTile.X, hoveredTile.Y);
            Raylib.DrawRectangleV(pos, new Vector2(Constants.TileWidth, Constants.TileHeight), ColorAlpha(Color.Yellow, 0.5f));
        }

        public void DrawDebugOverlay(CameraHandler camera, MapTile hoveredTile, Vector2 mouseWorldPos)
        {
            using var debugRenderer = new DebugRenderer();
            debugRenderer.ShowHotkeys = this.ShowHotkeys;
            debugRenderer.Draw(camera, hoveredTile, mouseWorldPos);
        }

        // These methods are not directly portable to Raylib-cs, but you can use RenderTexture2D if needed.
        // For now, they are left as stubs or can be implemented if you need map exporting.
        public RenderTexture2D RenderFullMapToTexture()
        {
            return RenderMapToTexture(true, true);
        }

        public RenderTexture2D RenderObjectsToTexture()
        {
            return RenderMapToTexture(false, true);
        }

        private RenderTexture2D RenderMapToTexture(bool drawTiles, bool drawObjects)
        {
            int mapWidth = _map.Width * Constants.TileWidth;
            int mapHeight = _map.Height * Constants.TileHeight;
            RenderTexture2D renderTarget = Raylib.LoadRenderTexture(mapWidth, mapHeight);
            Raylib.BeginTextureMode(renderTarget);
            Raylib.ClearBackground(Color.Blank);
            RenderMap(null, null, drawTiles, drawObjects);
            Raylib.EndTextureMode();
            return renderTarget;
        }

        public void Dispose()
        {
            Raylib.UnloadTexture(_highlightTexture);
        }

        private void RenderMap(CameraHandler camera, Rectangle? bounds, bool drawTiles, bool drawObjects, RenderTexture2D? renderTarget = null)
        {
            // If rendering to a texture, BeginTextureMode should already be called.
            Rectangle viewBounds = bounds ?? new Rectangle(0, 0, _map.Width, _map.Height);

            for (int y = (int)viewBounds.Y; y < (int)(viewBounds.Y + viewBounds.Height); y++)
            {
                for (int x = (int)viewBounds.X; x < (int)(viewBounds.X + viewBounds.Width); x++)
                {
                    if (x < 0 || y < 0 || x >= _map.Width || y >= _map.Height) continue;
                    var tile = _map.Tiles[x, y];
                    Vector2 pos = ToScreenCoordinates(x, y);

                    if (camera != null)
                    {
                        // Apply camera transform
                        pos = WorldToScreen(pos, camera);
                    }

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

            if (texture.Id == 0) return; // Raylib default: id==0 means invalid

            Constants.SpriteFrame frame = _spriteLoader.GetSpriteFrame(spriteId, frameIndex);
            Rectangle sourceRect = new Rectangle(frame.Left, frame.Top, frame.Width, frame.Height);

            Vector2 drawPos = position;
            if (isObjectSprite)
            {
                drawPos.X += frame.PivotX;
                drawPos.Y += frame.PivotY;
            }

            Raylib.DrawTextureRec(texture, sourceRect, drawPos, Color.White);
        }

        private static Vector2 ToScreenCoordinates(int tileX, int tileY)
        {
            return new Vector2(tileX * Constants.TileWidth, tileY * Constants.TileHeight);
        }

        private static Vector2 WorldToScreen(Vector2 worldPos, CameraHandler camera)
        {
            // Apply camera transform: scale and translate
            Vector2 screenCenter = new Vector2(Raylib.GetScreenWidth() / 2f, Raylib.GetScreenHeight() / 2f);
            Vector2 offset = (worldPos - camera.Position) * camera.Zoom;
            return screenCenter + offset;
        }

        private static Texture2D CreateHighlightTexture()
        {
            Image img = Raylib.GenImageColor(1, 1, Color.White);
            Texture2D tex = Raylib.LoadTextureFromImage(img);
            Raylib.UnloadImage(img);
            return tex;
        }
    }
}
