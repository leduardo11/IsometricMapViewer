using System;
using System.IO;
using System.Numerics;
using IsometricMapViewer.Handlers;
using IsometricMapViewer.Loaders;
using IsometricMapViewer.Rendering;
using Raylib_cs;

namespace IsometricMapViewer
{
    public class MainGame
    {
        private Vector2 _mouseWorldPos;
        private CameraHandler _camera;
        private InputHandler _inputHandler;
        private Map _map;
        private GameRenderer _renderer;
        private MapExporter _exporter;
        private MapTile _hoveredTile;
        private string _mapPath;
        private static readonly string MapsFolder = Path.Combine("resources", "maps");
        private Font _font;
        
        public Map Map => _map;
        public MapTile HoveredTile => _hoveredTile;

        public void Run()
        {
            Raylib.InitWindow(1280, 720, "Isometric Map Viewer");
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
            _mapPath = GetFirstMapFilePath();

            if (string.IsNullOrEmpty(_mapPath))
            {
                ConsoleLogger.LogError("No .amd files found in the Maps folder. Exiting.");
                return;
            }

            _map = LoadMap(_mapPath);

            if (_map == null)
            {
                ConsoleLogger.LogError("Failed to initialize map. Exiting.");
                return;
            }

            _map.ValidateMapSprites(tileLoader.GetTiles());
            _camera = new CameraHandler(_map);
            _camera.FitToMap();
            _inputHandler = new InputHandler(_camera, this);
        }

        private void LoadContent()
        {
            _font = Raylib.LoadFont("resources/fonts/DejaVuSansMono.ttf");
            var spriteLoader = new SpriteLoader();
            spriteLoader.LoadSprites();
            _renderer = new GameRenderer(_font, _map, spriteLoader);
            _exporter = new MapExporter(_renderer, _map);
        }

        private void Update()
        {
            _inputHandler.Update();
            var mousePos = Raylib.GetMousePosition();
            _mouseWorldPos = _camera.ScreenToWorld(mousePos);
            _hoveredTile = _map.GetTileAtWorldPosition(_mouseWorldPos);
        }

        private void Draw()
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);
            
            _renderer.DrawMap(_camera);
            _renderer.DrawGrid(_camera);
            _renderer.DrawTileHighlight(_camera, _hoveredTile);
            _renderer.DrawDebugOverlay(_camera, _hoveredTile, _mouseWorldPos);
            
            Raylib.EndDrawing();
        }

        public void ToggleGrid()
        {
            _renderer.ShowGrid = !_renderer.ShowGrid;
        }

        public void ToggleObjects()
        {
            _renderer.ShowObjects = !_renderer.ShowObjects;
        }

        public void ToggleFullscreen()
        {
            Raylib.ToggleFullscreen();
        }

        public void ToggleHotkeysDisplay()
        {
            _renderer.ShowHotkeys = !_renderer.ShowHotkeys;
        }

        private static string GetFirstMapFilePath()
        {
            string mapPath = Path.Combine(MapsFolder, Constants.MapName + ".amd");

            if (File.Exists(mapPath))
            {
                return mapPath;
            }

            ConsoleLogger.LogWarning($"Map '{Constants.MapName}.amd' not found. Falling back to first available map.");

            var amdFiles = Directory.GetFiles(MapsFolder, "*.amd");

            return amdFiles.Length > 0 ? amdFiles[0] : null;
        }

        private static Map LoadMap(string mapPath)
        {
            var map = new Map();

            if (!map.Load(mapPath))
            {
                ConsoleLogger.LogError($"Failed to load map: {mapPath}");
                return null;
            }

            return map;
        }

        public void SaveMap()
        {
            if (!string.IsNullOrEmpty(_mapPath))
            {
                _map.Save(_mapPath);
                ConsoleLogger.LogInfo($"Map saved to {_mapPath}");
            }
            else
            {
                ConsoleLogger.LogWarning("No map path to save to.");
            }
        }

        public void ExportMapToPng()
        {
            _exporter.ExportToPng(_renderer.ShowObjects);
        }

        public void ExportObjectsToPng()
        {
            _exporter.ExportObjectsToPng();
        }

        public void ExportMapToTsx()
        {
            _exporter.ExportToTiledMap();
        }

        private void Dispose()
        {
            _renderer?.Dispose();
            _exporter?.Dispose();
            Raylib.UnloadFont(_font);
        }
    }
}
