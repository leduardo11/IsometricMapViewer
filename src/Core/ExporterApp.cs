using System;
using System.IO;
using System.Linq;
using System.Numerics;
using IsometricMapViewer.Handlers;
using IsometricMapViewer.Loaders;
using IsometricMapViewer.Rendering;
using IsometricMapViewer.UI;
using Raylib_cs;

namespace IsometricMapViewer
{
    public class ExporterApp
    {
        private Map _map;
        private BudgetDungeonExporter _exporter;
        private CameraHandler _camera;
        private GameRenderer _renderer;
        private Font _font;
        private readonly string[] _availableMaps;
        private int _selectedMapIndex = 0;
        private string _outputPath = "/home/leduardo/exported-maps";
        private bool _showExportPanel = false;
        private string _statusMessage = "";
        private float _statusTimer = 0f;
        private bool _isExporting = false;
        private float _exportProgress = 0f;
        private MapTile _hoveredTile;
        private Vector2 _mouseWorldPos;

        public ExporterApp()
        {
            var mapsPath = Path.Combine("resources", "maps");
            _availableMaps = Directory.GetFiles(mapsPath, "*.amd")
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(n => n)
                .ToArray();

            if (_availableMaps.Length == 0)
            {
                throw new Exception("No .amd map files found in resources/maps/");
            }
        }

        public void Run()
        {
            Raylib.InitWindow(UIConfig.WINDOW_WIDTH, UIConfig.WINDOW_HEIGHT, UIConfig.WINDOW_TITLE);
            Raylib.SetTargetFPS(60);

            Initialize();
            LoadContent();

            while (!Raylib.WindowShouldClose())
            {
                Update();
                Draw();
            }

            Dispose();
            Raylib.CloseWindow();
        }

        private void Initialize()
        {
            var tileLoader = new TileLoader();
            tileLoader.PreloadAllSprites();
            
            LoadMap(_availableMaps[_selectedMapIndex]);
            
            if (_map != null)
            {
                _map.ValidateMapSprites(tileLoader.GetTiles());
                _camera = new CameraHandler(_map);
                _camera.FitToMap();
            }
        }

        private void LoadContent()
        {
            _font = Raylib.LoadFont("resources/fonts/DejaVuSansMono.ttf");
            var spriteLoader = new SpriteLoader();
            spriteLoader.LoadSprites();
            _renderer = new GameRenderer(_font, _map, spriteLoader);
        }

        private void Update()
        {
            if (_statusTimer > 0)
                _statusTimer -= Raylib.GetFrameTime();

            // Update mouse world position
            var mousePos = Raylib.GetMousePosition();
            _mouseWorldPos = _camera.ScreenToWorld(mousePos);
            _hoveredTile = _map?.GetTileAtWorldPosition(_mouseWorldPos);

            // Camera controls (when panel is closed)
            if (!_showExportPanel)
            {
                HandleCameraControls();
            }

            // Keyboard shortcuts
            if (Raylib.IsKeyPressed(KeyboardKey.Tab))
                _showExportPanel = !_showExportPanel;

            if (Raylib.IsKeyPressed(KeyboardKey.Escape))
            {
                if (_showExportPanel)
                    _showExportPanel = false;
            }

            // Quick export
            if (Raylib.IsKeyPressed(KeyboardKey.E) && !_showExportPanel)
            {
                ExportCurrentMap();
            }

            // Map navigation
            if (!_showExportPanel)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.PageUp))
                {
                    _selectedMapIndex = (_selectedMapIndex - 1 + _availableMaps.Length) % _availableMaps.Length;
                    LoadMap(_availableMaps[_selectedMapIndex]);
                }
                else if (Raylib.IsKeyPressed(KeyboardKey.PageDown))
                {
                    _selectedMapIndex = (_selectedMapIndex + 1) % _availableMaps.Length;
                    LoadMap(_availableMaps[_selectedMapIndex]);
                }
            }
        }

        private void HandleCameraControls()
        {
            // Mouse drag
            if (Raylib.IsMouseButtonDown(MouseButton.Right) || Raylib.IsMouseButtonDown(MouseButton.Middle))
            {
                var delta = Raylib.GetMouseDelta();
                _camera.Move(new Vector2(-delta.X, -delta.Y) / _camera.Zoom);
            }

            // Mouse wheel zoom
            float wheel = Raylib.GetMouseWheelMove();
            if (wheel != 0)
            {
                float zoomFactor = wheel > 0 ? 1.1f : 0.9f;
                _camera.ZoomAt(zoomFactor, Raylib.GetMousePosition());
            }

            // Keyboard movement
            Vector2 movement = Vector2.Zero;
            float speed = 10f / _camera.Zoom;

            if (Raylib.IsKeyDown(KeyboardKey.W) || Raylib.IsKeyDown(KeyboardKey.Up))
                movement.Y -= speed;
            if (Raylib.IsKeyDown(KeyboardKey.S) || Raylib.IsKeyDown(KeyboardKey.Down))
                movement.Y += speed;
            if (Raylib.IsKeyDown(KeyboardKey.A) || Raylib.IsKeyDown(KeyboardKey.Left))
                movement.X -= speed;
            if (Raylib.IsKeyDown(KeyboardKey.D) || Raylib.IsKeyDown(KeyboardKey.Right))
                movement.X += speed;

            if (movement != Vector2.Zero)
                _camera.Move(movement);

            // Fit to map
            if (Raylib.IsKeyPressed(KeyboardKey.F))
                _camera.FitToMap();
        }

        private void Draw()
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            // Draw the actual map with sprites
            if (_map != null && _renderer != null)
            {
                _renderer.DrawMap(_camera);
            }

            // Draw export panel
            if (_showExportPanel)
            {
                GothicUI.DrawFullscreenOverlay(ColorKeys.OverlayDim);
                DrawExportPanel();
            }

            // Draw status message
            if (_statusTimer > 0)
                DrawStatusMessage();

            // Draw help text
            DrawHelpText();

            // Draw map info overlay
            if (!_showExportPanel)
                DrawMapInfo();

            Raylib.EndDrawing();
        }

        private void DrawMapInfo()
        {
            // Top-left: Map name and size
            string info = $"{_availableMaps[_selectedMapIndex]} - {_map.Width}x{_map.Height}";
            Raylib.DrawText(info, 11, 11, UIConfig.FONT_LARGE, ColorKeys.Shadow);
            Raylib.DrawText(info, 10, 10, UIConfig.FONT_LARGE, ColorKeys.Gold);

            // Top-right: Zoom level
            string zoom = $"Zoom: {_camera.Zoom:F2}x";
            int zoomWidth = Raylib.MeasureText(zoom, UIConfig.FONT_MEDIUM);
            int screenW = Raylib.GetScreenWidth();
            Raylib.DrawText(zoom, screenW - zoomWidth - 9, 11, UIConfig.FONT_MEDIUM, ColorKeys.Shadow);
            Raylib.DrawText(zoom, screenW - zoomWidth - 10, 10, UIConfig.FONT_MEDIUM, ColorKeys.Bone);
        }

        private void DrawExportPanel()
        {
            var bounds = GothicUI.CenteredPanel(UIConfig.PANEL_WIDTH, UIConfig.PANEL_HEIGHT);
            GothicUI.Panel(bounds, "Export to BudgetDungeon");

            int contentY = (int)bounds.Y + 80;
            int centerX = (int)(bounds.X + bounds.Width / 2);

            // Map selection
            GothicUI.Label(centerX - 40, contentY, "Select Map:");
            contentY += 30;

            int mapListY = contentY;
            int mapListHeight = 200;
            
            // Draw map list background
            var listBounds = new Rectangle(bounds.X + 20, mapListY, bounds.Width - 40, mapListHeight);
            Raylib.DrawRectangleRec(listBounds, ColorKeys.DarkStone);
            Raylib.DrawRectangleLinesEx(listBounds, 1, ColorKeys.Stone);

            // Draw maps
            for (int i = 0; i < _availableMaps.Length; i++)
            {
                int itemY = mapListY + 5 + (i * 25);
                var itemBounds = new Rectangle(bounds.X + 25, itemY, bounds.Width - 50, 22);
                
                bool isSelected = i == _selectedMapIndex;
                bool isHovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), itemBounds);

                if (isHovered && Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    _selectedMapIndex = i;
                    LoadMap(_availableMaps[i]);
                }

                Color bgColor = isSelected ? ColorKeys.Stone : (isHovered ? ColorKeys.ButtonHover : Color.Blank);
                if (bgColor.A > 0)
                    Raylib.DrawRectangleRec(itemBounds, bgColor);

                Color textColor = isSelected ? ColorKeys.Gold : ColorKeys.Bone;
                Raylib.DrawText(_availableMaps[i], (int)itemBounds.X + 5, (int)itemBounds.Y + 2, 
                    UIConfig.FONT_MEDIUM, textColor);
            }

            contentY += mapListHeight + 20;

            // Output path
            GothicUI.Label((int)bounds.X + 20, contentY, "Output Path:");
            contentY += 25;
            
            var pathBounds = new Rectangle(bounds.X + 20, contentY, bounds.Width - 40, 30);
            Raylib.DrawRectangleRec(pathBounds, ColorKeys.DarkStone);
            Raylib.DrawRectangleLinesEx(pathBounds, 1, ColorKeys.Stone);
            Raylib.DrawText(_outputPath, (int)pathBounds.X + 8, (int)pathBounds.Y + 6, 
                UIConfig.FONT_SMALL, ColorKeys.Bone);

            contentY += 50;

            // Export button
            int buttonY = (int)(bounds.Y + bounds.Height - UIConfig.BUTTON_HEIGHT - UIConfig.PADDING - 10);
            
            if (_isExporting)
            {
                GothicUI.ProgressBar(centerX - 150, buttonY, 300, UIConfig.BUTTON_HEIGHT, 
                    _exportProgress, "Exporting...");
            }
            else
            {
                if (GothicUI.Button(new Rectangle(centerX - 150, buttonY, 300, UIConfig.BUTTON_HEIGHT), 
                    "Export Map"))
                {
                    ExportCurrentMap();
                }
            }

            // Close button
            if (GothicUI.Button(new Rectangle(bounds.X + bounds.Width - 40, bounds.Y + 10, 30, 30), "X"))
            {
                _showExportPanel = false;
            }
        }

        private void DrawStatusMessage()
        {
            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();
            
            int textWidth = Raylib.MeasureText(_statusMessage, UIConfig.FONT_LARGE);
            int x = (screenW - textWidth) / 2;
            int y = screenH - 100;

            // Background
            var bgBounds = new Rectangle(x - 20, y - 10, textWidth + 40, 40);
            Raylib.DrawRectangleRec(bgBounds, ColorKeys.DarkStone);
            Raylib.DrawRectangleLinesEx(bgBounds, 2, ColorKeys.Gold);

            // Text
            Raylib.DrawText(_statusMessage, x + 1, y + 1, UIConfig.FONT_LARGE, ColorKeys.Shadow);
            Raylib.DrawText(_statusMessage, x, y, UIConfig.FONT_LARGE, ColorKeys.Gold);
        }

        private void DrawHelpText()
        {
            int screenH = Raylib.GetScreenHeight();
            int y = screenH - 60;
            
            if (_showExportPanel)
            {
                Raylib.DrawText("ESC: Close Panel", 10, y, UIConfig.FONT_SMALL, ColorKeys.Bone);
            }
            else
            {
                string help = "TAB: Export  |  E: Quick Export  |  F: Fit Map  |  PgUp/PgDn: Change Map  |  WASD/Arrows: Pan  |  Mouse: Drag/Zoom";
                Raylib.DrawText(help, 10, y, UIConfig.FONT_SMALL, ColorKeys.Bone);
            }
        }

        private void LoadMap(string mapName)
        {
            try
            {
                var mapPath = Path.Combine("resources", "maps", $"{mapName}.amd");
                _map = new Map();
                
                if (!_map.Load(mapPath))
                {
                    ShowStatus($"Failed to load map: {mapName}");
                    return;
                }

                _exporter = new BudgetDungeonExporter(null, _map);
                ShowStatus($"Loaded: {mapName}");
            }
            catch (Exception ex)
            {
                ShowStatus($"Error: {ex.Message}");
            }
        }

        private void ExportCurrentMap()
        {
            if (_map == null || _exporter == null) return;

            _isExporting = true;
            _exportProgress = 0f;

            try
            {
                string mapName = _availableMaps[_selectedMapIndex];
                var mapFolder = Path.Combine(_outputPath, mapName);
                Directory.CreateDirectory(mapFolder);

                _exportProgress = 0.5f;

                var jsonPath = Path.Combine(mapFolder, $"{mapName}.json");
                _exporter.ExportJsonOnly(jsonPath, mapName);

                _exportProgress = 1.0f;
                ShowStatus($"✓ Exported {mapName} to {mapFolder}");
            }
            catch (Exception ex)
            {
                ShowStatus($"Export failed: {ex.Message}");
            }
            finally
            {
                _isExporting = false;
                _exportProgress = 0f;
            }
        }

        private void ShowStatus(string message)
        {
            _statusMessage = message;
            _statusTimer = 3.0f;
            ConsoleLogger.LogInfo(message);
        }
        private void Dispose()
        {
            _renderer?.Dispose();
            Raylib.UnloadFont(_font);
        }
    }
}
