using System.IO;
using IsometricMapViewer.Handlers;
using IsometricMapViewer.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace IsometricMapViewer
{
    public class MainGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Vector2 _mouseWorldPos;
        private CameraHandler _camera;
        private InputHandler _inputHandler;
        private Map _map;
        private GameRenderer _renderer;
        private MapExporter _exporter;
        private MapTile _hoveredTile;
        public Map Map => _map;
        private static readonly string MapsFolder = Path.Combine("Maps");

        public MainGame()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 1280,
                PreferredBackBufferHeight = 720,
            };
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            var tileLoader = new TileLoader(GraphicsDevice);
            tileLoader.PreloadAllSprites();
            string mapPath = GetFirstMapFilePath();

            if (string.IsNullOrEmpty(mapPath))
            {
                ConsoleLogger.LogError("No .amd files found in the Maps folder. Exiting.");
                Exit();
                return;
            }

            _map = LoadMap(mapPath);

            if (_map == null)
            {
                ConsoleLogger.LogError("Failed to initialize map. Exiting.");
                Exit();
                return;
            }

            _map.ValidateMapSprites(tileLoader.GetTiles());
            _camera = new CameraHandler(GraphicsDevice, _map);
            Vector2 startPos = new(Map.Width * Constants.TileWidth / 2,
                                 Map.Height * Constants.TileHeight / 2);
            _camera.FitToMap();
            _inputHandler = new InputHandler(_camera, GraphicsDevice, this);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            var font = Content.Load<SpriteFont>("Default");
            _renderer = new GameRenderer(_spriteBatch, font, GraphicsDevice, _map);
            _renderer.LoadSprites();
            _exporter = new MapExporter(_renderer, _map);
        }

        protected override void Update(GameTime gameTime)
        {
            _inputHandler.Update(gameTime);
            var mouseState = Mouse.GetState();
            _mouseWorldPos = _camera.ScreenToWorld(mouseState.Position.ToVector2());
            _hoveredTile = _map.GetTileAtWorldPosition(_mouseWorldPos);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            _renderer.DrawMap(_camera);
            _renderer.DrawGrid(_camera);
            _renderer.DrawTileHighlight(_camera, _hoveredTile);
            _renderer.DrawDebugOverlay(_camera, _hoveredTile, _mouseWorldPos);
            base.Draw(gameTime);
        }

        public void ToggleGrid()
        {
            _renderer.ShowGrid = !_renderer.ShowGrid;
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

        public void ExportMap()
        {
            _exporter.ExportToTsx();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _renderer?.Dispose();
                _exporter?.Dispose();
                _spriteBatch?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}