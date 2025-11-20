using System;
using System.IO;
using Raylib_cs;
using System.Numerics;
using IsometricMapViewer.src;

namespace IsometricMapViewer
{
    public class MainGame : IDisposable
    {
        private Vector2 _mouseWorldPos = new Vector2();
        private CameraHandler _camera;
        private InputHandler _inputHandler;
        private Map _map;
        private GameRenderer _renderer;
        private MapExporter _exporter;
        private MapTile _hoveredTile;
        private string _mapPath;
        private bool _showGrid = true;
        private bool _showObjects = true;
        private bool _showHotkeys = false;
        private static readonly string MapsFolder = Path.Combine("..", "resources", "Maps"); public Map Map => _map;
        public MapTile HoveredTile => _hoveredTile;

        public MainGame()
        {
            _mapPath = GetFirstMapFilePath();

            if (string.IsNullOrEmpty(_mapPath))
            {
                ConsoleLogger.LogError("No .amd files found in the Maps folder. Exiting.");
                Environment.Exit(1);
            }

            _map = LoadMap(_mapPath);

            if (_map == null)
            {
                ConsoleLogger.LogError("Failed to initialize map. Exiting.");
                Environment.Exit(1);
            }

            var tileLoader = new TileLoader();
            tileLoader.PreloadAllSprites();
            _map.ValidateMapSprites(tileLoader.GetTiles());
            _camera = new CameraHandler(_map);
            _camera.FitToMap();
            _inputHandler = new InputHandler(_camera, this);

            var spriteLoader = new SpriteLoader();
            spriteLoader.LoadSprites();
            _renderer = new GameRenderer(_map, spriteLoader);
            _exporter = new MapExporter(_renderer, _map);
        }

        public void UpdateAndDraw()
        {
            _inputHandler.Update();
            var mousePos = Raylib.GetMousePosition();
            _mouseWorldPos = new System.Numerics.Vector2(mousePos.X, mousePos.Y);
            _hoveredTile = _map.GetTileAtWorldPosition(_mouseWorldPos);

            _renderer.ShowGrid = _showGrid;
            _renderer.ShowObjects = _showObjects;
            _renderer.ShowHotkeys = _showHotkeys;

            _renderer.DrawMap(_camera);
            _renderer.DrawGrid(_camera);
            _renderer.DrawTileHighlight(_camera, _hoveredTile);
            _renderer.DrawDebugOverlay(_camera, _hoveredTile, _mouseWorldPos);
        }
        public void ToggleGrid() => _showGrid = !_showGrid;
        public void ToggleObjects() => _showObjects = !_showObjects;
        public void ToggleHotkeysDisplay() => _showHotkeys = !_showHotkeys;

        private static string GetFirstMapFilePath()
        {
            string mapPath = Path.Combine(MapsFolder, Constants.MapName + ".amd");

            if (File.Exists(mapPath))
                return mapPath;

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

        public void ExportMapToPng() => _exporter.ExportToPng();
        public void ExportObjectsToPng() => _exporter.ExportObjectsToPng();
        public void ExportMapToTsx() => _exporter.ExportToTiledMap();

        public void Dispose()
        {
            _renderer?.Dispose();
            _exporter?.Dispose();
        }
    }
}
