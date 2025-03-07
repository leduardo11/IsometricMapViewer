using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace IsometricMapViewer.Handlers
{
    public class InputHandler(CameraHandler camera, GraphicsDevice graphicsDevice, Game game)
    {
        private readonly CameraHandler _camera = camera;
        private readonly Viewport _viewport = graphicsDevice.Viewport;
        private readonly Game _game = game;
        private MouseState _previousMouseState = Mouse.GetState();
        private KeyboardState _previousKeyboardState = Keyboard.GetState();
        private Vector2 _dragStartPosition;
        private bool _isDragging;
        private string _gotoInput = "";

        public void Update(GameTime gameTime)
        {
            var mouseState = Mouse.GetState();
            var keyboardState = Keyboard.GetState();

            HandleMouseDragging(mouseState);
            HandleZoom(mouseState);
            HandleKeyboardZoom(keyboardState);
            HandleKeyboardMovement(keyboardState);
            HandleApplicationClose(keyboardState);
            HandleGotoCommand(keyboardState);
            HandleGridToggle(keyboardState);
            HandleExportMap(keyboardState);

            _previousMouseState = mouseState;
            _previousKeyboardState = keyboardState;
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
            int scrollWheelDelta = mouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue;

            if (scrollWheelDelta != 0)
            {
                float zoomFactor = scrollWheelDelta > 0 ? 1.1f : 0.9f;
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

        private void HandleGotoCommand(KeyboardState keyboardState)
        {
            if (keyboardState.IsKeyDown(Keys.G) && string.IsNullOrEmpty(_gotoInput))
            {
                _gotoInput = "G";
            }

            if (_gotoInput.StartsWith('G'))
            {
                foreach (Keys key in keyboardState.GetPressedKeys())
                {
                    if (key == Keys.Back && _gotoInput.Length > 1)
                    {
                        _gotoInput = _gotoInput.Substring(0, _gotoInput.Length - 1);
                    }
                    else
                    {
                        string charForKey = GetCharForKey(key);
                        if (!string.IsNullOrEmpty(charForKey))
                        {
                            if (!(_gotoInput == "G" && charForKey == "g"))
                            {
                                _gotoInput += charForKey;
                            }
                        }
                    }
                }

                if (keyboardState.IsKeyDown(Keys.Enter) && _gotoInput.Contains(','))
                {
                    string[] coords = [.. _gotoInput[1..].Split(',').Select(c => c.Trim())];

                    if (coords.Length == 2 && int.TryParse(coords[0], out int targetX) && int.TryParse(coords[1], out int targetY))
                    {
                        Map map = ((MainGame)_game).Map;

                        if (targetX >= 0 && targetX < map.Width && targetY >= 0 && targetY < map.Height)
                        {
                            Vector2 targetWorldPos = new Vector2(targetX * Constants.TileWidth, targetY * Constants.TileHeight);
                            _camera.FocusOnPoint(targetWorldPos);
                        }
                        else
                        {
                            ConsoleLogger.LogWarning($"Invalid coordinates: X={targetX}, Y={targetY} - Must be within 0 to {map.Width - 1},{map.Height - 1}");
                        }
                    }
                    _gotoInput = "";
                }
            }
        }

        private void HandleGridToggle(KeyboardState keyboardState)
        {
            if ((keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl)) &&
                keyboardState.IsKeyDown(Keys.G) && !_previousKeyboardState.IsKeyDown(Keys.G))
            {
                ((MainGame)_game).ToggleGrid();
            }
        }

        private void HandleExportMap(KeyboardState keyboardState)
        {
            if ((keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl)) &&
                keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
            {
                ((MainGame)_game).ExportMap();
            }
        }

        private static string GetCharForKey(Keys key)
        {
            if (key >= Keys.A && key <= Keys.Z)
            {
                return ((char)('a' + (key - Keys.A))).ToString();
            }

            return key switch
            {
                Keys.D0 or Keys.NumPad0 => "0",
                Keys.D1 or Keys.NumPad1 => "1",
                Keys.D2 or Keys.NumPad2 => "2",
                Keys.D3 or Keys.NumPad3 => "3",
                Keys.D4 or Keys.NumPad4 => "4",
                Keys.D5 or Keys.NumPad5 => "5",
                Keys.D6 or Keys.NumPad6 => "6",
                Keys.D7 or Keys.NumPad7 => "7",
                Keys.D8 or Keys.NumPad8 => "8",
                Keys.D9 or Keys.NumPad9 => "9",
                Keys.OemComma => ",",
                _ => null,
            };
        }
    }
}
