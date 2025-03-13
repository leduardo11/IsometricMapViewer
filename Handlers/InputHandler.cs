using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace IsometricMapViewer.Handlers
{
    public class InputHandler
    {
        private readonly CameraHandler _camera;
        private readonly Viewport _viewport;
        private readonly MainGame _game;
        private MouseState _previousMouseState;
        private KeyboardState _previousKeyboardState;
        private Vector2 _dragStartPosition;
        private bool _isDragging;
        private readonly Dictionary<Keys, Action> _ctrlHotkeys;
        private readonly Dictionary<Keys, Action> _directHotkeys;

        public InputHandler(CameraHandler camera, GraphicsDevice graphicsDevice, Game game)
        {
            _camera = camera;
            _viewport = graphicsDevice.Viewport;
            _game = game as MainGame ?? throw new ArgumentException("Game must be of type MainGame", nameof(game));
            _previousMouseState = Mouse.GetState();
            _previousKeyboardState = Keyboard.GetState();

            _ctrlHotkeys = new Dictionary<Keys, Action>
            {
                { Keys.P, () => _game.ExportMapToPng() },
                { Keys.T, () => _game.ExportMapToTsx() },
                { Keys.O, () => _game.ExportObjectsToPng() },
                { Keys.S, () => _game.SaveMap() },
                { Keys.M, () => ToggleTileProperty(t => (!t.IsMoveAllowed, t.IsTeleport, t.IsFarmingAllowed, t.IsWater)) },
                { Keys.E, () => ToggleTileProperty(t => (t.IsMoveAllowed, !t.IsTeleport, t.IsFarmingAllowed, t.IsWater)) },
                { Keys.F, () => ToggleTileProperty(t => (t.IsMoveAllowed, t.IsTeleport, !t.IsFarmingAllowed, t.IsWater)) },
                { Keys.W, () => ToggleTileProperty(t => (t.IsMoveAllowed, t.IsTeleport, t.IsFarmingAllowed, !t.IsWater)) }
            };

            _directHotkeys = new Dictionary<Keys, Action>
            {
                { Keys.G, ()=> _game.ToggleGrid() },
                { Keys.O, ()=> _game.ToggleObjects() },
                { Keys.H, () => _game.ToggleThumbnails() },
            };
        }

        public void Update(GameTime gameTime)
        {
            var mouseState = Mouse.GetState();
            var keyboardState = Keyboard.GetState();

            HandleMouseDragging(mouseState);
            HandleZoom(mouseState);
            HandleKeyboardZoom(keyboardState);
            HandleKeyboardMovement(keyboardState);
            HandleApplicationClose(keyboardState);
            HandleHotkeys(keyboardState);
            HandleScreenResolutionToggle(keyboardState);
            HandleShowHotkeyToggle(keyboardState);

            _previousMouseState = mouseState;
            _previousKeyboardState = keyboardState;
        }

        private void HandleHotkeys(KeyboardState keyboardState)
        {
            bool isCtrlDown = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);

            if (isCtrlDown)
            {
                foreach (var kv in _ctrlHotkeys)
                {
                    if (keyboardState.IsKeyDown(kv.Key) && !_previousKeyboardState.IsKeyDown(kv.Key))
                    {
                        kv.Value.Invoke();
                    }
                }
            }
            else
            {
                foreach (var kv in _directHotkeys)
                {
                    if (keyboardState.IsKeyDown(kv.Key) && !_previousKeyboardState.IsKeyDown(kv.Key))
                    {
                        kv.Value.Invoke();
                    }
                }
            }
        }

        private void HandleMouseDragging(MouseState mouseState)
        {
            Vector2 mousePos = mouseState.Position.ToVector2();
            if (mouseState.RightButton == ButtonState.Pressed || mouseState.MiddleButton == ButtonState.Pressed)
            {
                if (!_isDragging && _viewport.Bounds.Contains(mouseState.Position))
                {
                    _dragStartPosition = mousePos;
                    _isDragging = true;
                }
                else if (_isDragging)
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

        private void HandleZoom(MouseState mouseState)
        {
            int scrollDelta = mouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue;
            if (scrollDelta != 0)
            {
                float zoomFactor = scrollDelta > 0 ? 1.1f : 0.9f;
                float newZoom = _camera.Zoom * zoomFactor;
                _camera.ZoomAt(newZoom / _camera.Zoom, mouseState.Position.ToVector2());
            }
        }

        private void HandleKeyboardZoom(KeyboardState keyboardState)
        {
            if ((keyboardState.IsKeyDown(Keys.OemPlus) || keyboardState.IsKeyDown(Keys.Add)) &&
                !_previousKeyboardState.IsKeyDown(Keys.OemPlus) && !_previousKeyboardState.IsKeyDown(Keys.Add))
            {
                _camera.ZoomAt(1.1f, new Vector2(_viewport.Width / 2, _viewport.Height / 2));
            }
            else if ((keyboardState.IsKeyDown(Keys.OemMinus) || keyboardState.IsKeyDown(Keys.Subtract)) &&
                !_previousKeyboardState.IsKeyDown(Keys.OemMinus) && !_previousKeyboardState.IsKeyDown(Keys.Subtract))
            {
                _camera.ZoomAt(0.9f, new Vector2(_viewport.Width / 2, _viewport.Height / 2));
            }
        }

        private void HandleKeyboardMovement(KeyboardState keyboardState)
        {
            Vector2 movement = Vector2.Zero;
            float effectiveSpeed = Constants.BaseCameraSpeed * Constants.TileWidth / 32f / _camera.Zoom;

            if (keyboardState.IsKeyDown(Keys.Left) || keyboardState.IsKeyDown(Keys.A))
                movement.X -= effectiveSpeed;
            if (keyboardState.IsKeyDown(Keys.Right) || keyboardState.IsKeyDown(Keys.D))
                movement.X += effectiveSpeed;
            if (keyboardState.IsKeyDown(Keys.Up) || keyboardState.IsKeyDown(Keys.W))
                movement.Y -= effectiveSpeed;
            if (keyboardState.IsKeyDown(Keys.Down) || keyboardState.IsKeyDown(Keys.S))
                movement.Y += effectiveSpeed;

            if (movement != Vector2.Zero)
            {
                _camera.Move(movement);
            }
        }

        private void HandleApplicationClose(KeyboardState keyboardState)
        {
            if (keyboardState.IsKeyDown(Keys.Escape))
            {
                _game.Exit();
            }
        }

        private void HandleScreenResolutionToggle(KeyboardState keyboardState)
        {
            if ((keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt)) &&
                keyboardState.IsKeyDown(Keys.Enter) && !_previousKeyboardState.IsKeyDown(Keys.Enter))
            {
                _game.ToggleFullscreen();
            }
        }

        private void HandleShowHotkeyToggle(KeyboardState keyboardState)
        {
            if (keyboardState.IsKeyDown(Keys.F1) && !_previousKeyboardState.IsKeyDown(Keys.F1))
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