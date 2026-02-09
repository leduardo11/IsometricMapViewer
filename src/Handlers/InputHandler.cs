using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace IsometricMapViewer.Handlers
{
    public class InputHandler
    {
        private readonly CameraHandler _camera;
        private readonly MainGame _game;
        private Vector2 _dragStartPosition;
        private bool _isDragging;
        private readonly Dictionary<KeyboardKey, Action> _ctrlHotkeys;
        private readonly Dictionary<KeyboardKey, Action> _directHotkeys;
        private bool _wasCtrlPPressed;
        private bool _wasCtrlTPressed;
        private bool _wasCtrlOPressed;
        private bool _wasCtrlSPressed;
        private bool _wasCtrlMPressed;
        private bool _wasCtrlEPressed;
        private bool _wasCtrlFPressed;
        private bool _wasCtrlWPressed;
        private bool _wasGPressed;
        private bool _wasOPressed;
        private bool _wasF1Pressed;
        private bool _wasEnterPressed;

        public InputHandler(CameraHandler camera, MainGame game)
        {
            _camera = camera;
            _game = game;

            _ctrlHotkeys = new Dictionary<KeyboardKey, Action>
            {
                { KeyboardKey.P, () => _game.ExportMapToPng() },
                { KeyboardKey.T, () => _game.ExportMapToTsx() },
                { KeyboardKey.O, () => _game.ExportObjectsToPng() },
                { KeyboardKey.S, () => _game.SaveMap() },
                { KeyboardKey.M, () => ToggleTileProperty(t => (!t.IsMoveAllowed, t.IsTeleport, t.IsFarmingAllowed, t.IsWater)) },
                { KeyboardKey.E, () => ToggleTileProperty(t => (t.IsMoveAllowed, !t.IsTeleport, t.IsFarmingAllowed, t.IsWater)) },
                { KeyboardKey.F, () => ToggleTileProperty(t => (t.IsMoveAllowed, t.IsTeleport, !t.IsFarmingAllowed, t.IsWater)) },
                { KeyboardKey.W, () => ToggleTileProperty(t => (t.IsMoveAllowed, t.IsTeleport, t.IsFarmingAllowed, !t.IsWater)) }
            };

            _directHotkeys = new Dictionary<KeyboardKey, Action>
            {
                { KeyboardKey.G, ToggleGrid },
                { KeyboardKey.O, ToggleObjects }
            };
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

            if (isCtrlDown)
            {
                bool isCtrlPDown = Raylib.IsKeyDown(KeyboardKey.P);
                if (isCtrlPDown && !_wasCtrlPPressed) _ctrlHotkeys[KeyboardKey.P].Invoke();
                _wasCtrlPPressed = isCtrlPDown;

                bool isCtrlTDown = Raylib.IsKeyDown(KeyboardKey.T);
                if (isCtrlTDown && !_wasCtrlTPressed) _ctrlHotkeys[KeyboardKey.T].Invoke();
                _wasCtrlTPressed = isCtrlTDown;

                bool isCtrlODown = Raylib.IsKeyDown(KeyboardKey.O);
                if (isCtrlODown && !_wasCtrlOPressed) _ctrlHotkeys[KeyboardKey.O].Invoke();
                _wasCtrlOPressed = isCtrlODown;

                bool isCtrlSDown = Raylib.IsKeyDown(KeyboardKey.S);
                if (isCtrlSDown && !_wasCtrlSPressed) _ctrlHotkeys[KeyboardKey.S].Invoke();
                _wasCtrlSPressed = isCtrlSDown;

                bool isCtrlMDown = Raylib.IsKeyDown(KeyboardKey.M);
                if (isCtrlMDown && !_wasCtrlMPressed) _ctrlHotkeys[KeyboardKey.M].Invoke();
                _wasCtrlMPressed = isCtrlMDown;

                bool isCtrlEDown = Raylib.IsKeyDown(KeyboardKey.E);
                if (isCtrlEDown && !_wasCtrlEPressed) _ctrlHotkeys[KeyboardKey.E].Invoke();
                _wasCtrlEPressed = isCtrlEDown;

                bool isCtrlFDown = Raylib.IsKeyDown(KeyboardKey.F);
                if (isCtrlFDown && !_wasCtrlFPressed) _ctrlHotkeys[KeyboardKey.F].Invoke();
                _wasCtrlFPressed = isCtrlFDown;

                bool isCtrlWDown = Raylib.IsKeyDown(KeyboardKey.W);
                if (isCtrlWDown && !_wasCtrlWPressed) _ctrlHotkeys[KeyboardKey.W].Invoke();
                _wasCtrlWPressed = isCtrlWDown;
            }
            else
            {
                _wasCtrlPPressed = false;
                _wasCtrlTPressed = false;
                _wasCtrlOPressed = false;
                _wasCtrlSPressed = false;
                _wasCtrlMPressed = false;
                _wasCtrlEPressed = false;
                _wasCtrlFPressed = false;
                _wasCtrlWPressed = false;

                bool isGDown = Raylib.IsKeyDown(KeyboardKey.G);
                if (isGDown && !_wasGPressed) _directHotkeys[KeyboardKey.G].Invoke();
                _wasGPressed = isGDown;

                bool isODown = Raylib.IsKeyDown(KeyboardKey.O);
                if (isODown && !_wasOPressed) _directHotkeys[KeyboardKey.O].Invoke();
                _wasOPressed = isODown;
            }
        }

        private void HandleMouseDragging()
        {
            Vector2 mousePos = Raylib.GetMousePosition();
            if (Raylib.IsMouseButtonDown(MouseButton.Right) || Raylib.IsMouseButtonDown(MouseButton.Middle))
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
            float scrollDelta = Raylib.GetMouseWheelMove();
            if (scrollDelta != 0)
            {
                float zoomFactor = scrollDelta > 0 ? 1.1f : 0.9f;
                _camera.ZoomAt(zoomFactor, Raylib.GetMousePosition());
            }
        }

        private void HandleKeyboardZoom()
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Equal) || Raylib.IsKeyPressed(KeyboardKey.KpAdd))
            {
                int screenWidth = Raylib.GetScreenWidth();
                int screenHeight = Raylib.GetScreenHeight();
                _camera.ZoomAt(1.1f, new Vector2(screenWidth / 2, screenHeight / 2));
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Minus) || Raylib.IsKeyPressed(KeyboardKey.KpSubtract))
            {
                int screenWidth = Raylib.GetScreenWidth();
                int screenHeight = Raylib.GetScreenHeight();
                _camera.ZoomAt(0.9f, new Vector2(screenWidth / 2, screenHeight / 2));
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
            // Raylib handles window close with WindowShouldClose()
        }

        private void HandleScreenResolutionToggle()
        {
            bool isAltDown = Raylib.IsKeyDown(KeyboardKey.LeftAlt) || Raylib.IsKeyDown(KeyboardKey.RightAlt);
            bool isEnterDown = Raylib.IsKeyDown(KeyboardKey.Enter);
            
            if (isAltDown && isEnterDown && !_wasEnterPressed)
            {
                _game.ToggleFullscreen();
            }
            _wasEnterPressed = isEnterDown;
        }

        private void HandleShowHotkeyToggle()
        {
            bool isF1Down = Raylib.IsKeyDown(KeyboardKey.F1);
            if (isF1Down && !_wasF1Pressed)
            {
                _game.ToggleHotkeysDisplay();
            }
            _wasF1Pressed = isF1Down;
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

        private void ToggleGrid()
        {
            _game.ToggleGrid();
        }

        private void ToggleObjects()
        {
            _game.ToggleObjects();
        }
    }
}
