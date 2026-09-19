using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using IsometricMapViewer.Editor;
using IsometricMapViewer.Handlers;
using IsometricMapViewer.Loaders;
using Raylib_cs;

namespace IsometricMapViewer.Rendering;

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

    public void DrawEditorMap(CameraHandler camera, EditorState state)
    {
        Raylib.BeginMode2D(camera.Camera);

        var bounds = camera.GetViewBounds();
        // Expand bounds slightly to avoid culling tall trees whose roots are just off-screen
        int padX = 5;
        int padY = 8;
        int startX = Math.Max(0, (int)bounds.X - padX);
        int endX = Math.Min(_map.Width, (int)(bounds.X + bounds.Width) + padX);
        int startY = Math.Max(0, (int)bounds.Y - padY);
        int endY = Math.Min(_map.Height, (int)(bounds.Y + bounds.Height) + padY);

        // 1. Ground Pass
        if (state.ShowGround)
        {
            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    var tile = _map.Tiles[x, y];
                    if (tile.TileSprite >= 0)
                    {
                        Vector2 pos = ToScreenCoordinates(x, y);
                        DrawSpriteIfExists(tile.TileSprite, tile.TileFrame, pos, isObjectSprite: false);
                    }
                }
            }
        }

        // 2. Tree Shadows Pass
        if (state.ShowShadows)
        {
            Color shadowColor = new(255, 255, 255, (int)Constants.ShadowAlpha);
            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    var tile = _map.Tiles[x, y];
                    if (tile.IsTree && tile.ObjectSprite > 0)
                    {
                        Vector2 pos = ToScreenCoordinates(x, y);
                        DrawSpriteIfExists(tile.ShadowSprite, tile.ObjectFrame, pos, isObjectSprite: true, tint: shadowColor);
                    }
                }
            }
        }

        // 3. Objects & Trees Pass (sorted by Y for proper occlusion)
        var objectsToDraw = new List<(int Sprite, int Frame, Vector2 Pos, int SortY)>();
        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                var tile = _map.Tiles[x, y];
                if (tile.ObjectSprite <= 0) continue;

                bool isTree = tile.IsTree;
                if ((isTree && state.ShowTrees) || (!isTree && state.ShowObjects))
                {
                    Vector2 pos = ToScreenCoordinates(x, y);
                    int sortY = y * 1000 + x;
                    objectsToDraw.Add((tile.ObjectSprite, tile.ObjectFrame, pos, sortY));
                }
            }
        }

        objectsToDraw.Sort((a, b) => a.SortY.CompareTo(b.SortY));
        foreach (var obj in objectsToDraw)
        {
            DrawSpriteIfExists(obj.Sprite, obj.Frame, obj.Pos, isObjectSprite: true);
        }

        // 4. Collision Overlay Pass
        if (state.ShowCollision)
        {
            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    var tile = _map.Tiles[x, y];
                    int px = x * Constants.TileWidth;
                    int py = y * Constants.TileHeight;

                    if (tile.IsWater)
                    {
                        Raylib.DrawRectangle(px, py, Constants.TileWidth, Constants.TileHeight, Constants.ColorWater);
                    }
                    else if (!tile.IsMoveAllowed)
                    {
                        Raylib.DrawRectangle(px, py, Constants.TileWidth, Constants.TileHeight, Constants.ColorBlocked);
                    }
                    else if (tile.IsTeleport)
                    {
                        Raylib.DrawRectangle(px, py, Constants.TileWidth, Constants.TileHeight, Constants.ColorTeleport);
                    }
                    else if (tile.IsFarmingAllowed)
                    {
                        Raylib.DrawRectangle(px, py, Constants.TileWidth, Constants.TileHeight, Constants.ColorFarm);
                    }
                }
            }
        }

        // 5. Grid Pass
        if (state.ShowGrid)
        {
            _gridRenderer.Draw(camera, true, true);
        }

        // 6. Editor Cursor & Ghost Pass
        if (state.IsHoverValid)
        {
            DrawCursorAndGhost(state);
        }

        Raylib.EndMode2D();
    }

    private void DrawCursorAndGhost(EditorState state)
    {
        int size = state.BrushSize;
        Color ghostTint = new(255, 255, 255, 170);

        for (int dy = 0; dy < size; dy++)
        {
            for (int dx = 0; dx < size; dx++)
            {
                int cx = state.HoverCellX + dx;
                int cy = state.HoverCellY + dy;
                if (cx < 0 || cx >= _map.Width || cy < 0 || cy >= _map.Height) continue;

                Vector2 pos = ToScreenCoordinates(cx, cy);

                // Draw cell bounding box
                Raylib.DrawRectangleLines(
                    (int)pos.X,
                    (int)pos.Y,
                    Constants.TileWidth,
                    Constants.TileHeight,
                    Color.Yellow);

                // Ghost preview
                switch (state.ActiveTool)
                {
                    case EditorTool.GroundBrush:
                        DrawSpriteIfExists(state.SelectedGroundSprite, state.SelectedGroundFrame, pos, isObjectSprite: false, tint: ghostTint);
                        break;

                    case EditorTool.ObjectPlacer:
                        if (dx == 0 && dy == 0) // Only draw object at root cell
                        {
                            DrawSpriteIfExists(state.SelectedObjectSprite, state.SelectedObjectFrame, pos, isObjectSprite: true, tint: ghostTint);
                        }
                        break;

                    case EditorTool.CollisionPainter:
                        Color colColor = state.ActiveCollisionMode switch
                        {
                            CollisionPaintMode.Blocked => Constants.ColorBlocked,
                            CollisionPaintMode.Water => Constants.ColorWater,
                            CollisionPaintMode.Teleport => Constants.ColorTeleport,
                            CollisionPaintMode.Farm => Constants.ColorFarm,
                            _ => new Color(200, 200, 200, 80)
                        };
                        Raylib.DrawRectangle((int)pos.X, (int)pos.Y, Constants.TileWidth, Constants.TileHeight, colColor);
                        break;

                    case EditorTool.Eraser:
                        Raylib.DrawRectangle((int)pos.X, (int)pos.Y, Constants.TileWidth, Constants.TileHeight, new Color(255, 0, 0, 90));
                        break;
                }
            }
        }
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
            Constants.ColorHighlight
        );
        Raylib.EndMode2D();
    }

    public void DrawDebugOverlay(CameraHandler camera, MapTile hoveredTile, Vector2 mouseWorldPos)
    {
        using var debugRenderer = new DebugRenderer(_font);
        debugRenderer.ShowHotkeys = this.ShowHotkeys;
        debugRenderer.Draw(camera, hoveredTile, mouseWorldPos);
    }

    public Image RenderFullMapToImage() => RenderMapToImage(true, true);
    public Image RenderObjectsToImage() => RenderMapToImage(false, true);
    public Image RenderMapWithoutObjectsToImage() => RenderMapToImage(true, false);

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

    private void DrawSpriteIfExists(int spriteId, int frameIndex, Vector2 position, bool isObjectSprite = false, Color? tint = null)
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

        Raylib.DrawTextureRec(texture, sourceRect, position, tint ?? Color.White);
    }

    private static Vector2 ToScreenCoordinates(int tileX, int tileY)
    {
        return new Vector2(tileX * Constants.TileWidth, tileY * Constants.TileHeight);
    }
}
