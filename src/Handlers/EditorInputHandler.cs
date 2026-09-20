using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using IsometricMapViewer.Editor;
using IsometricMapViewer.Loaders;
using Raylib_cs;

namespace IsometricMapViewer.Handlers;

public class EditorInputHandler
{
    private readonly Map _map;
    private readonly CameraHandler _camera;
    private readonly EditorState _state;
    private readonly CommandHistory _history;
    private readonly SpriteLoader _spriteLoader;
    private readonly Action _onSave;
    private readonly Action<AtlasExportKind> _onAtlasExport;

    private BatchEditorCommand? _activeStroke;
    private readonly HashSet<(int X, int Y)> _strokeVisited = [];

    public EditorInputHandler(
        Map map,
        CameraHandler camera,
        EditorState state,
        CommandHistory history,
        SpriteLoader spriteLoader,
        Action onSave,
        Action<AtlasExportKind> onAtlasExport)
    {
        _map = map;
        _camera = camera;
        _state = state;
        _history = history;
        _spriteLoader = spriteLoader;
        _onSave = onSave;
        _onAtlasExport = onAtlasExport;

        PopulateAvailableSprites();
    }

    private void PopulateAvailableSprites()
    {
        if (_state.AvailableGroundSprites.Count == 0 || _state.AvailableObjectSprites.Count == 0)
        {
            _state.AvailableGroundSprites.Clear();
            _state.AvailableObjectSprites.Clear();

            foreach (var sprite in _spriteLoader.GetAllSprites())
            {
                if (sprite.Texture.Id == 0) continue;

                if ((sprite.Index >= 50 && sprite.Index <= 69) ||
                    (sprite.Index >= 100 && sprite.Index <= 145) ||
                    (sprite.Index >= 200 && sprite.Index <= 248))
                {
                    _state.AvailableObjectSprites.Add((short)sprite.Index);
                }
                else if (sprite.Index < 150 || sprite.Index > 195)
                {
                    _state.AvailableGroundSprites.Add((short)sprite.Index);
                }
            }

            _state.AvailableGroundSprites.Sort();
            _state.AvailableObjectSprites.Sort();
        }
    }

    public void Update()
    {
        UpdateHoverState();
        HandleCameraControls();
        HandleShortcuts();
        HandleToolInteraction();
    }

    private void UpdateHoverState()
    {
        Vector2 mousePos = Raylib.GetMousePosition();
        bool isOverUI = IsMouseOverUI(mousePos);

        if (isOverUI)
        {
            _state.IsHoverValid = false;
        }
        else
        {
            _state.IsHoverValid = _camera.GetCellAtScreenPos(mousePos, out int cx, out int cy);
            _state.HoverCellX = cx;
            _state.HoverCellY = cy;
        }
    }

    private bool IsMouseOverUI(Vector2 mouse)
    {
        if (!_state.ShowUI) return false;

        int screenW = Raylib.GetScreenWidth();

        // Top Navigation Bar
        if (mouse.Y < 48) return true;

        // Left Tool Dock & Legend Panel
        int maxDockY = _state.ShowLegend ? 848 : 350;
        if (mouse.X >= 16 && mouse.X <= 286 && mouse.Y >= 64 && mouse.Y <= maxDockY) return true;

        // Right Palette Panel
        if (_state.ShowPalette && mouse.X >= screenW - 350) return true;

        return false;
    }

    private void HandleCameraControls()
    {
        // Right Mouse Drag to pan
        if (Raylib.IsMouseButtonDown(MouseButton.Right) || Raylib.IsMouseButtonDown(MouseButton.Middle))
        {
            var delta = Raylib.GetMouseDelta();
            _camera.Move(new Vector2(-delta.X, -delta.Y) / _camera.Zoom);
        }

        // Mouse Wheel Zoom toward mouse
        float wheel = Raylib.GetMouseWheelMove();
        if (wheel != 0)
        {
            float zoomFactor = wheel > 0 ? 1.15f : 0.85f;
            _camera.ZoomAt(zoomFactor, Raylib.GetMousePosition());
        }

        // Keyboard Zoom (+ / -) toward screen center, for wheels that are broken.
        if (Raylib.IsKeyPressed(KeyboardKey.Equal) || Raylib.IsKeyPressed(KeyboardKey.KpAdd))
        {
            _camera.ZoomAt(1.15f, new Vector2(Raylib.GetScreenWidth() / 2f, Raylib.GetScreenHeight() / 2f));
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Minus) || Raylib.IsKeyPressed(KeyboardKey.KpSubtract))
        {
            _camera.ZoomAt(0.85f, new Vector2(Raylib.GetScreenWidth() / 2f, Raylib.GetScreenHeight() / 2f));
        }

        // Keyboard Panning (WASD / Arrows)
        Vector2 movement = Vector2.Zero;
        float speed = 12f / _camera.Zoom;

        if (Raylib.IsKeyDown(KeyboardKey.W) || Raylib.IsKeyDown(KeyboardKey.Up)) movement.Y -= speed;
        if (Raylib.IsKeyDown(KeyboardKey.S) || Raylib.IsKeyDown(KeyboardKey.Down)) movement.Y += speed;
        if (Raylib.IsKeyDown(KeyboardKey.A) || Raylib.IsKeyDown(KeyboardKey.Left)) movement.X -= speed;
        if (Raylib.IsKeyDown(KeyboardKey.D) || Raylib.IsKeyDown(KeyboardKey.Right)) movement.X += speed;

        if (movement != Vector2.Zero)
            _camera.Move(movement);

        if (Raylib.IsKeyPressed(KeyboardKey.F))
            _camera.FitToMap();
    }

    private void HandleShortcuts()
    {
        bool ctrl = Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl);

        // Undo / Redo
        if (ctrl && Raylib.IsKeyPressed(KeyboardKey.Z))
        {
            if (_history.Undo())
                _state.SetStatus($"Undid: {_history.LastRedoDescription}");
        }
        else if (ctrl && Raylib.IsKeyPressed(KeyboardKey.Y))
        {
            if (_history.Redo())
                _state.SetStatus($"Redid: {_history.LastUndoDescription}");
        }

        // Quick Save & Export
        if (ctrl && Raylib.IsKeyPressed(KeyboardKey.S))
        {
            _onSave();
        }
        else if (ctrl && Raylib.IsKeyPressed(KeyboardKey.E))
        {
            _onAtlasExport(AtlasExportKind.Rpg);
        }

        // HelbreathAtlasPacker export formats (Ctrl + number)
        if (ctrl)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.One)) _onAtlasExport(AtlasExportKind.Package);
            else if (Raylib.IsKeyPressed(KeyboardKey.Two)) _onAtlasExport(AtlasExportKind.Rpg);
            else if (Raylib.IsKeyPressed(KeyboardKey.Three)) _onAtlasExport(AtlasExportKind.Godot);
            else if (Raylib.IsKeyPressed(KeyboardKey.Four)) _onAtlasExport(AtlasExportKind.Tiled);
            else if (Raylib.IsKeyPressed(KeyboardKey.Five)) _onAtlasExport(AtlasExportKind.MapShot);
            else if (Raylib.IsKeyPressed(KeyboardKey.Six)) _onAtlasExport(AtlasExportKind.MasterTiles);
            else if (Raylib.IsKeyPressed(KeyboardKey.Seven)) _onAtlasExport(AtlasExportKind.Olympia);
            else if (Raylib.IsKeyPressed(KeyboardKey.Zero)) _onAtlasExport(AtlasExportKind.All);
        }

        // Palette Toggle [Tab]
        if (Raylib.IsKeyPressed(KeyboardKey.Tab))
        {
            _state.ShowPalette = !_state.ShowPalette;
        }

        // Legend Toggle [H] or [F1]
        if (!ctrl && (Raylib.IsKeyPressed(KeyboardKey.H) || Raylib.IsKeyPressed(KeyboardKey.F1)))
        {
            _state.ShowLegend = !_state.ShowLegend;
        }

        // Brush Size Cycle [B]
        if (!ctrl && Raylib.IsKeyPressed(KeyboardKey.B))
        {
            _state.BrushSize = (_state.BrushSize % 3) + 1;
            _state.SetStatus($"Brush Size: {_state.BrushSize}x{_state.BrushSize}");
        }

        // Tool Selection [1-5]
        if (!ctrl && Raylib.IsKeyPressed(KeyboardKey.One)) _state.ActiveTool = EditorTool.GroundBrush;
        if (!ctrl && Raylib.IsKeyPressed(KeyboardKey.Two)) _state.ActiveTool = EditorTool.ObjectPlacer;
        if (!ctrl && Raylib.IsKeyPressed(KeyboardKey.Three)) _state.ActiveTool = EditorTool.CollisionPainter;
        if (!ctrl && Raylib.IsKeyPressed(KeyboardKey.Four)) _state.ActiveTool = EditorTool.Eraser;
        if (!ctrl && Raylib.IsKeyPressed(KeyboardKey.Five)) _state.ActiveTool = EditorTool.Eyedropper;

        // Layer Toggles
        if (!ctrl && Raylib.IsKeyPressed(KeyboardKey.G)) _state.ShowGround = !_state.ShowGround;
        if (!ctrl && Raylib.IsKeyPressed(KeyboardKey.S)) _state.ShowShadows = !_state.ShowShadows;
        if (!ctrl && Raylib.IsKeyPressed(KeyboardKey.O)) _state.ShowObjects = !_state.ShowObjects;
        if (!ctrl && Raylib.IsKeyPressed(KeyboardKey.T)) _state.ShowTrees = !_state.ShowTrees;
        if (!ctrl && Raylib.IsKeyPressed(KeyboardKey.C)) _state.ShowCollision = !_state.ShowCollision;

        // Sprite Cycling: [ / ] or PageDown / PageUp
        if (Raylib.IsKeyPressed(KeyboardKey.LeftBracket) || Raylib.IsKeyPressed(KeyboardKey.PageDown))
        {
            _state.CycleSprite(-1);
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.RightBracket) || Raylib.IsKeyPressed(KeyboardKey.PageUp))
        {
            _state.CycleSprite(+1);
        }

        // Frame Cycling: , / . or Home / End
        if (Raylib.IsKeyPressed(KeyboardKey.Comma) || Raylib.IsKeyPressed(KeyboardKey.Home))
        {
            int spriteId = _state.ActiveTool == EditorTool.GroundBrush ? _state.SelectedGroundSprite : _state.SelectedObjectSprite;
            int frameCount = _spriteLoader.GetSpriteFrameCount(spriteId);
            _state.CycleFrame(-1, frameCount);
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Period) || Raylib.IsKeyPressed(KeyboardKey.End))
        {
            int spriteId = _state.ActiveTool == EditorTool.GroundBrush ? _state.SelectedGroundSprite : _state.SelectedObjectSprite;
            int frameCount = _spriteLoader.GetSpriteFrameCount(spriteId);
            _state.CycleFrame(+1, frameCount);
        }

        // Collision Paint Mode Selection (when in collision tool)
        if (_state.ActiveTool == EditorTool.CollisionPainter)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.M)) _state.ActiveCollisionMode = CollisionPaintMode.Blocked;
            if (Raylib.IsKeyPressed(KeyboardKey.K)) _state.ActiveCollisionMode = CollisionPaintMode.Walkable;
            if (Raylib.IsKeyPressed(KeyboardKey.W)) _state.ActiveCollisionMode = CollisionPaintMode.Water;
            if (Raylib.IsKeyPressed(KeyboardKey.L)) _state.ActiveCollisionMode = CollisionPaintMode.Teleport;
            if (Raylib.IsKeyPressed(KeyboardKey.R)) _state.ActiveCollisionMode = CollisionPaintMode.Farm;
        }
    }

    private void HandleToolInteraction()
    {
        if (IsMouseOverUI(Raylib.GetMousePosition())) return;

        // Eyedropper on click
        if (_state.ActiveTool == EditorTool.Eyedropper)
        {
            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && _state.IsHoverValid)
            {
                var tile = _map.Tiles[_state.HoverCellX, _state.HoverCellY];
                if (tile.ObjectSprite > 0)
                {
                    _state.SelectedObjectSprite = tile.ObjectSprite;
                    _state.SelectedObjectFrame = tile.ObjectFrame;
                    _state.ActiveTool = EditorTool.ObjectPlacer;
                    _state.SetStatus($"Sampled Object #{tile.ObjectSprite}");
                }
                else if (tile.TileSprite >= 0)
                {
                    _state.SelectedGroundSprite = tile.TileSprite;
                    _state.SelectedGroundFrame = tile.TileFrame;
                    _state.ActiveTool = EditorTool.GroundBrush;
                    _state.SetStatus($"Sampled Ground #{tile.TileSprite}");
                }
            }
            return;
        }

        // Start Stroke
        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && _state.IsHoverValid)
        {
            _activeStroke = new BatchEditorCommand($"Apply {_state.ActiveTool}");
            _strokeVisited.Clear();
            ApplyCurrentToolToBrush();
        }
        // Continue Stroke
        else if (Raylib.IsMouseButtonDown(MouseButton.Left) && _state.IsHoverValid && _activeStroke != null)
        {
            ApplyCurrentToolToBrush();
        }
        // Commit Stroke
        else if (Raylib.IsMouseButtonReleased(MouseButton.Left) && _activeStroke != null)
        {
            if (_activeStroke.Count > 0)
            {
                _history.Execute(_activeStroke);
            }
            _activeStroke = null;
            _strokeVisited.Clear();
        }
    }

    private void ApplyCurrentToolToBrush()
    {
        if (_activeStroke == null || !_state.IsHoverValid) return;

        int size = _state.BrushSize;
        for (int dy = 0; dy < size; dy++)
        {
            for (int dx = 0; dx < size; dx++)
            {
                int cx = _state.HoverCellX + dx;
                int cy = _state.HoverCellY + dy;

                if (cx < 0 || cx >= _map.Width || cy < 0 || cy >= _map.Height) continue;
                if (!_strokeVisited.Add((cx, cy))) continue;

                switch (_state.ActiveTool)
                {
                    case EditorTool.GroundBrush:
                        var groundCmd = new PaintGroundCommand(_map, cx, cy, _state.SelectedGroundSprite, _state.SelectedGroundFrame);
                        groundCmd.Execute();
                        _activeStroke.Add(groundCmd);
                        break;

                    case EditorTool.ObjectPlacer:
                        if (dx == 0 && dy == 0) // Only place at origin of brush
                        {
                            var objCmd = new PlaceObjectCommand(_map, cx, cy, _state.SelectedObjectSprite, _state.SelectedObjectFrame);
                            objCmd.Execute();
                            _activeStroke.Add(objCmd);
                        }
                        break;

                    case EditorTool.Eraser:
                        var clearCmd = new ClearObjectCommand(_map, cx, cy);
                        clearCmd.Execute();
                        _activeStroke.Add(clearCmd);
                        break;

                    case EditorTool.CollisionPainter:
                        var props = _state.ActiveCollisionMode switch
                        {
                            CollisionPaintMode.Blocked => new TileProperties(false, false, false, false),
                            CollisionPaintMode.Walkable => new TileProperties(true, false, false, false),
                            CollisionPaintMode.Water => new TileProperties(false, false, false, true),
                            CollisionPaintMode.Teleport => new TileProperties(true, true, false, false),
                            CollisionPaintMode.Farm => new TileProperties(true, false, true, false),
                            _ => new TileProperties(true, false, false, false)
                        };
                        var colCmd = new PaintCollisionCommand(_map, cx, cy, props);
                        colCmd.Execute();
                        _activeStroke.Add(colCmd);
                        break;
                }
            }
        }
    }
}
