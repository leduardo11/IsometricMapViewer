using System;
using System.IO;
using System.Numerics;
using IsometricMapViewer.Handlers;
using IsometricMapViewer.Loaders;
using IsometricMapViewer.Rendering;
using IsometricMapViewer.UI;
using Microsoft.Extensions.Configuration;
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
        private AppSettings _settings;
        private string _statusMessage = "";
        private float _statusTimer = 0f;
        private bool _showUI = true;
        private bool _isExporting = false;
        private string _exportingMessage = "";

        public ExporterApp()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            _settings = new AppSettings();
            config.GetSection("MapExporter").Bind(_settings.MapExporter);
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
            
            LoadMap(_settings.MapExporter.MapName);
            
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
            _renderer.ShowObjects = _settings.MapExporter.ShowObjects;
            _renderer.ShowGrid = _settings.MapExporter.ShowGrid;
        }

        private void Update()
        {
            if (_statusTimer > 0)
                _statusTimer -= Raylib.GetFrameTime();

            // Don't process input during export
            if (_isExporting)
                return;

            // Toggle UI
            if (Raylib.IsKeyPressed(KeyboardKey.Tab))
                _showUI = !_showUI;

            // Camera controls
            HandleCameraControls();

            // Quick export shortcuts
            if (Raylib.IsKeyPressed(KeyboardKey.G))
                ExportGrid();
            
            if (Raylib.IsKeyPressed(KeyboardKey.P))
                ExportPNG();
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

            // Draw the map
            if (_map != null && _renderer != null)
            {
                _renderer.DrawMap(_camera);
                if (_renderer.ShowGrid)
                    _renderer.DrawGrid(_camera);
            }

            // Draw UI
            if (_showUI)
                DrawUI();

            // Draw export loading overlay
            if (_isExporting)
                DrawExportingOverlay();

            // Draw status message
            if (_statusTimer > 0)
                DrawStatusMessage();

            Raylib.EndDrawing();
        }

        private void DrawUI()
        {
            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();

            // Top bar with map info
            var topBarBounds = new Rectangle(0, 0, screenW, 40);
            Raylib.DrawRectangleRec(topBarBounds, new Color(25, 25, 30, 200));
            Raylib.DrawRectangleLinesEx(topBarBounds, 1, ColorKeys.Stone);

            string mapInfo = $"{_settings.MapExporter.MapName} - {_map.Width}x{_map.Height} - Zoom: {_camera.Zoom:F2}x";
            Raylib.DrawText(mapInfo, 11, 11, UIConfig.FONT_LARGE, ColorKeys.Shadow);
            Raylib.DrawText(mapInfo, 10, 10, UIConfig.FONT_LARGE, ColorKeys.Gold);

            // Right side panel with buttons
            int panelWidth = 200;
            int panelX = screenW - panelWidth;
            int panelY = 50;
            int buttonY = panelY + 10;

            var panelBounds = new Rectangle(panelX, panelY, panelWidth, 360);
            GothicUI.Panel(panelBounds, "");

            // Export Grid button (JSON for server)
            if (GothicUI.Button(new Rectangle(panelX + 10, buttonY, panelWidth - 20, UIConfig.BUTTON_HEIGHT), 
                "Export Grid"))
            {
                ExportGrid();
            }
            buttonY += UIConfig.BUTTON_HEIGHT + UIConfig.SPACING;

            // Export PNG button (for client)
            if (GothicUI.Button(new Rectangle(panelX + 10, buttonY, panelWidth - 20, UIConfig.BUTTON_HEIGHT), 
                "Export PNG"))
            {
                ExportPNG();
            }
            buttonY += UIConfig.BUTTON_HEIGHT + UIConfig.SPACING;

            // Separator
            buttonY += 10;

            // Toggle Objects button
            string objectsText = _renderer.ShowObjects ? "Hide Objects" : "Show Objects";
            if (GothicUI.Button(new Rectangle(panelX + 10, buttonY, panelWidth - 20, UIConfig.BUTTON_HEIGHT), 
                objectsText))
            {
                _renderer.ShowObjects = !_renderer.ShowObjects;
            }
            buttonY += UIConfig.BUTTON_HEIGHT + UIConfig.SPACING;

            // Toggle Grid button
            string gridText = _renderer.ShowGrid ? "Hide Grid" : "Show Grid";
            if (GothicUI.Button(new Rectangle(panelX + 10, buttonY, panelWidth - 20, UIConfig.BUTTON_HEIGHT), 
                gridText))
            {
                _renderer.ShowGrid = !_renderer.ShowGrid;
            }
            buttonY += UIConfig.BUTTON_HEIGHT + UIConfig.SPACING;

            // Fit Map button
            if (GothicUI.Button(new Rectangle(panelX + 10, buttonY, panelWidth - 20, UIConfig.BUTTON_HEIGHT), 
                "Fit Map"))
            {
                _camera.FitToMap();
            }

            // Bottom help text
            string help = "TAB: Toggle UI  |  G: Export Grid  |  P: Export PNG  |  F: Fit  |  WASD: Pan";
            int helpY = screenH - 30;
            Raylib.DrawRectangle(0, helpY - 5, screenW, 35, new Color(25, 25, 30, 200));
            Raylib.DrawText(help, 11, helpY + 1, UIConfig.FONT_SMALL, ColorKeys.Shadow);
            Raylib.DrawText(help, 10, helpY, UIConfig.FONT_SMALL, ColorKeys.Bone);
        }

        private void DrawStatusMessage()
        {
            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();
            
            int textWidth = Raylib.MeasureText(_statusMessage, UIConfig.FONT_LARGE);
            int x = (screenW - textWidth) / 2;
            int y = screenH - 100;

            var bgBounds = new Rectangle(x - 20, y - 10, textWidth + 40, 40);
            Raylib.DrawRectangleRec(bgBounds, ColorKeys.DarkStone);
            Raylib.DrawRectangleLinesEx(bgBounds, 2, ColorKeys.Gold);

            Raylib.DrawText(_statusMessage, x + 1, y + 1, UIConfig.FONT_LARGE, ColorKeys.Shadow);
            Raylib.DrawText(_statusMessage, x, y, UIConfig.FONT_LARGE, ColorKeys.Gold);
        }

        private void DrawExportingOverlay()
        {
            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();

            // Semi-transparent dark overlay
            Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 180));

            // Loading box
            int boxWidth = 500;
            int boxHeight = 150;
            int boxX = (screenW - boxWidth) / 2;
            int boxY = (screenH - boxHeight) / 2;

            var boxBounds = new Rectangle(boxX, boxY, boxWidth, boxHeight);
            Raylib.DrawRectangleRec(boxBounds, ColorKeys.DarkStone);
            Raylib.DrawRectangleLinesEx(boxBounds, 3, ColorKeys.Gold);

            // Animated loading text with dots
            float time = (float)Raylib.GetTime();
            int dots = ((int)(time * 2) % 4);
            string loadingText = _exportingMessage + new string('.', dots);
            
            int textWidth = Raylib.MeasureText(loadingText, UIConfig.FONT_LARGE);
            int textX = boxX + (boxWidth - textWidth) / 2;
            int textY = boxY + 40;

            Raylib.DrawText(loadingText, textX + 1, textY + 1, UIConfig.FONT_LARGE, ColorKeys.Shadow);
            Raylib.DrawText(loadingText, textX, textY, UIConfig.FONT_LARGE, ColorKeys.Gold);

            // Info text
            string infoText = "Please wait, this may take a moment for large maps";
            int infoWidth = Raylib.MeasureText(infoText, UIConfig.FONT_SMALL);
            int infoX = boxX + (boxWidth - infoWidth) / 2;
            int infoY = textY + 50;

            Raylib.DrawText(infoText, infoX + 1, infoY + 1, UIConfig.FONT_SMALL, ColorKeys.Shadow);
            Raylib.DrawText(infoText, infoX, infoY, UIConfig.FONT_SMALL, ColorKeys.Bone);
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

        private void ExportGrid()
        {
            if (_map == null || _exporter == null || _isExporting) return;

            // Set exporting state and message
            _isExporting = true;
            _exportingMessage = "Exporting Grid";
            
            // Force one frame render to show the loading overlay
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);
            if (_map != null && _renderer != null)
            {
                _renderer.DrawMap(_camera);
                if (_renderer.ShowGrid)
                    _renderer.DrawGrid(_camera);
            }
            if (_showUI)
                DrawUI();
            DrawExportingOverlay();
            Raylib.EndDrawing();

            try
            {
                string mapName = _settings.MapExporter.MapName;
                var mapFolder = Path.Combine(_settings.MapExporter.OutputPath, mapName);
                Directory.CreateDirectory(mapFolder);

                var jsonPath = Path.Combine(mapFolder, $"{mapName}.json");
                _exporter.ExportJsonOnly(jsonPath, mapName);

                ShowStatus($"✓ Grid exported to {mapFolder}");
            }
            catch (Exception ex)
            {
                ShowStatus($"Grid export failed: {ex.Message}");
            }
            finally
            {
                _isExporting = false;
            }
        }

        private void ExportPNG()
        {
            if (_map == null || _renderer == null || _isExporting) return;

            // Set exporting state and message
            _isExporting = true;
            _exportingMessage = "Exporting PNG";
            
            // Force one frame render to show the loading overlay
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);
            if (_map != null && _renderer != null)
            {
                _renderer.DrawMap(_camera);
                if (_renderer.ShowGrid)
                    _renderer.DrawGrid(_camera);
            }
            if (_showUI)
                DrawUI();
            DrawExportingOverlay();
            Raylib.EndDrawing();

            try
            {
                string mapName = _settings.MapExporter.MapName;
                var mapFolder = Path.Combine(_settings.MapExporter.OutputPath, mapName);
                Directory.CreateDirectory(mapFolder);

                var pngPath = Path.Combine(mapFolder, $"{mapName}.png");
                
                // Render full map to image
                Image mapImage = _renderer.RenderFullMapToImage();
                Raylib.ExportImage(mapImage, pngPath);
                Raylib.UnloadImage(mapImage);

                ShowStatus($"✓ PNG exported to {mapFolder}");
            }
            catch (Exception ex)
            {
                ShowStatus($"PNG export failed: {ex.Message}");
            }
            finally
            {
                _isExporting = false;
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
