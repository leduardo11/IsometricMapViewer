using System;
using System.Numerics;
using IsometricMapViewer.src;
using Raylib_cs;

namespace IsometricMapViewer
{
    public class InputHandler
    {
        private readonly CameraHandler _camera;
        private readonly MainGame _game;
        private Vector2 _dragStartPosition;
        private bool _isDragging;
        private bool _prevF1;
        private bool _prevEnter;
        private bool _prevCtrl;
        private bool _prevG;
        private bool _prevO;

        public InputHandler(CameraHandler camera, MainGame game)
        {
            _camera = camera;
            _game = game ?? throw new ArgumentException("Game must be of type MainGame", nameof(game));
        }

        public void Update()
        {
            HandleMouseDragging();
            HandleZoom();
            HandleKeyboardZoom();
            HandleKeyboardMovement();
            HandleApplicationClose();
            HandleHotkeys();
            HandleScreenResolutionToggle();
            HandleShowHotkeyToggle();
        }

        private void HandleHotkeys()
        {
            bool isCtrlDown = Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl);

            // Ctrl hotkeys
            if (isCtrlDown)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.P)) _game.ExportMapToPng();
                if (Raylib.IsKeyPressed(KeyboardKey.T)) _game.ExportMapToTsx();
                if (Raylib.IsKeyPressed(KeyboardKey.O)) _game.ExportObjectsToPng();
                if (Raylib.IsKeyPressed(KeyboardKey.S)) _game.SaveMap();
                if (Raylib.IsKeyPressed(KeyboardKey.M)) ToggleTileProperty(t => (!t.IsMoveAllowed, t.IsTeleport, t.IsFarmingAllowed, t.IsWater));
                if (Raylib.IsKeyPressed(KeyboardKey.E)) ToggleTileProperty(t => (t.IsMoveAllowed, !t.IsTeleport, t.IsFarmingAllowed, t.IsWater));
                if (Raylib.IsKeyPressed(KeyboardKey.F)) ToggleTileProperty(t => (t.IsMoveAllowed, t.IsTeleport, !t.IsFarmingAllowed, t.IsWater));
                if (Raylib.IsKeyPressed(KeyboardKey.W)) ToggleTileProperty(t => (t.IsMoveAllowed, t.IsTeleport, t.IsFarmingAllowed, !t.IsWater));
            }
            else
            {
                if (Raylib.IsKeyPressed(KeyboardKey.G)) _game.ToggleGrid();
                if (Raylib.IsKeyPressed(KeyboardKey.O)) _game.ToggleObjects();
            }
        }

        private void HandleMouseDragging()
        {
            Vector2 mousePos = Raylib.GetMousePosition();
            bool rightDown = Raylib.IsMouseButtonDown(MouseButton.Right);
            bool middleDown = Raylib.IsMouseButtonDown(MouseButton.Middle);

            if (rightDown || middleDown)
            {
                if (!_isDragging)
                {
                    _dragStartPosition = mousePos;
                    _isDragging = true;
                }
                else
                {
                    Vector2 delta = mousePos - _dragStartPosition;
                    _camera.Move(delta / _camera.Zoom);
                    _dragStartPosition = mousePos;
                }
            }
            else
            {
                _isDragging = false;
            }
        }

        private void HandleZoom()
        {
            float wheel = Raylib.GetMouseWheelMove();
            if (wheel != 0)
            {
                float zoomFactor = wheel > 0 ? 1.1f : 0.9f;
                _camera.ZoomAt(zoomFactor, Raylib.GetMousePosition());
            }
        }

        private void HandleKeyboardZoom()
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Equal) || Raylib.IsKeyPressed(KeyboardKey.KpAdd))
            {
                _camera.ZoomAt(1.1f, new Vector2(Raylib.GetScreenWidth() / 2f, Raylib.GetScreenHeight() / 2f));
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Minus) || Raylib.IsKeyPressed(KeyboardKey.KpSubtract))
            {
                _camera.ZoomAt(0.9f, new Vector2(Raylib.GetScreenWidth() / 2f, Raylib.GetScreenHeight() / 2f));
            }
        }

        private void HandleKeyboardMovement()
        {
            Vector2 movement = Vector2.Zero;
            float effectiveSpeed = Constants.BaseCameraSpeed * Constants.TileWidth / 32f / _camera.Zoom;

            if (Raylib.IsKeyDown(KeyboardKey.Left) || Raylib.IsKeyDown(KeyboardKey.A))
                movement.X -= effectiveSpeed;
            if (Raylib.IsKeyDown(KeyboardKey.Right) || Raylib.IsKeyDown(KeyboardKey.D))
                movement.X += effectiveSpeed;
            if (Raylib.IsKeyDown(KeyboardKey.Up) || Raylib.IsKeyDown(KeyboardKey.W))
                movement.Y -= effectiveSpeed;
            if (Raylib.IsKeyDown(KeyboardKey.Down) || Raylib.IsKeyDown(KeyboardKey.S))
                movement.Y += effectiveSpeed;

            if (movement != Vector2.Zero)
            {
                _camera.Move(movement);
            }
        }

        private void HandleApplicationClose()
        {
            if (Raylib.IsKeyDown(KeyboardKey.Escape))
            {
                Raylib.CloseWindow();
            }
        }

        private void HandleScreenResolutionToggle()
        {
            bool altDown = Raylib.IsKeyDown(KeyboardKey.LeftAlt) || Raylib.IsKeyDown(KeyboardKey.RightAlt);
            bool enterPressed = Raylib.IsKeyPressed(KeyboardKey.Enter);

            if (altDown && enterPressed)
            {
                Raylib.ToggleFullscreen();
            }
        }

        private void HandleShowHotkeyToggle()
        {
            bool f1Pressed = Raylib.IsKeyPressed(KeyboardKey.F1);
            if (f1Pressed)
            {
                _game.ToggleHotkeysDisplay();
            }
        }

        private void ToggleTileProperty(Func<MapTile, (bool, bool, bool, bool)> getNewProperties)
        {
            var hoveredTile = _game.HoveredTile;
            if (hoveredTile != null)
            {
                var (newMoveAllowed, newTeleport, newFarmingAllowed, newWater) = getNewProperties(hoveredTile);
                _game.Map.UpdateTileProperties(
                    hoveredTile.X, hoveredTile.Y,
                    newMoveAllowed,
                    newTeleport,
                    newFarmingAllowed,
                    newWater
                );
            }
        }
    }
}
