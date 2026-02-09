using System;
using System.Numerics;
using Raylib_cs;

namespace IsometricMapViewer.Handlers
{
    public class CameraHandler
    {
        private Vector2 _position;
        private float _zoom;
        private float _minZoom;
        private readonly Map _map;
        private int _viewRangeX;
        private int _viewRangeY;

        public Vector2 Position => _position;
        public Camera2D Camera { get; private set; }

        public float Zoom
        {
            get => _zoom;
            private set => _zoom = Math.Clamp(value, _minZoom, Constants.MaxCameraZoom);
        }

        public CameraHandler(Map map)
        {
            _map = map;
            _zoom = Constants.DefaultCameraZoom;
            _position = Vector2.Zero;
            Initialize();
            _position = new Vector2(_map.Width * Constants.TileWidth / 2, _map.Height * Constants.TileHeight / 2);
            CalculateMinZoom();
            UpdateCamera();
        }

        private void Initialize()
        {
            int centerX = _map.Width / 2;
            int centerY = _map.Height / 2;
            FocusOnPoint(new Vector2(centerX * Constants.TileWidth, centerY * Constants.TileHeight));
            UpdateViewRange();
        }

        public void FitToMap()
        {
            int screenWidth = Raylib.GetScreenWidth();
            int screenHeight = Raylib.GetScreenHeight();
            float zoomWidth = screenWidth / (float)(_map.Width * Constants.TileWidth);
            float zoomHeight = screenHeight / (float)(_map.Height * Constants.TileHeight);
            Zoom = Math.Min(zoomWidth, zoomHeight);
            Vector2 mapCenter = new(_map.Width * Constants.TileWidth / 2, _map.Height * Constants.TileHeight / 2);
            FocusOnPoint(mapCenter);
        }

        public void Move(Vector2 movement)
        {
            _position += movement;
            ClampPosition();
            UpdateCamera();
        }

        public void ZoomAt(float zoomFactor, Vector2 screenPosition)
        {
            var worldBefore = ScreenToWorld(screenPosition);
            Zoom *= zoomFactor;
            var worldAfter = ScreenToWorld(screenPosition);
            _position += worldBefore - worldAfter;
            ClampPosition();
            UpdateCamera();
        }

        public void FocusOnPoint(Vector2 worldPosition)
        {
            int screenWidth = Raylib.GetScreenWidth();
            int screenHeight = Raylib.GetScreenHeight();
            _position = new Vector2(
                worldPosition.X - screenWidth / (2 * _zoom),
                worldPosition.Y - screenHeight / (2 * _zoom)
            );
            ClampPosition();
            UpdateCamera();
        }

        public Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            return Raylib.GetScreenToWorld2D(screenPosition, Camera);
        }

        public Rectangle GetViewBounds()
        {
            int screenWidth = Raylib.GetScreenWidth();
            int screenHeight = Raylib.GetScreenHeight();
            int tileWidth = Constants.TileWidth;
            int tileHeight = Constants.TileHeight;
            int maxViewRange = Math.Max(_map.Width, _map.Height);
            _viewRangeX = Math.Min((int)(screenWidth / (tileWidth * _zoom)) + 1, maxViewRange);
            _viewRangeY = Math.Min((int)(screenHeight / (tileHeight * _zoom)) + 1, maxViewRange);
            var topLeft = ScreenToWorld(Vector2.Zero);
            int startX = (int)(topLeft.X / tileWidth);
            int startY = (int)(topLeft.Y / tileHeight);
            return new Rectangle(startX, startY, _viewRangeX, _viewRangeY);
        }

        private void UpdateViewRange()
        {
            int screenWidth = Raylib.GetScreenWidth();
            int screenHeight = Raylib.GetScreenHeight();
            int tileWidth = Constants.TileWidth;
            int tileHeight = Constants.TileHeight;
            int maxViewRange = Math.Max(_map.Width, _map.Height);
            _viewRangeX = Math.Min((int)(screenWidth / (tileWidth * _zoom)) + 1, maxViewRange);
            _viewRangeY = Math.Min((int)(screenHeight / (tileHeight * _zoom)) + 1, maxViewRange);
        }

        private void UpdateCamera()
        {
            int screenWidth = Raylib.GetScreenWidth();
            int screenHeight = Raylib.GetScreenHeight();
            Camera = new Camera2D
            {
                Target = _position,
                Offset = new Vector2(screenWidth / 2f, screenHeight / 2f),
                Rotation = 0f,
                Zoom = _zoom
            };
        }

        private void CalculateMinZoom()
        {
            int screenWidth = Raylib.GetScreenWidth();
            int screenHeight = Raylib.GetScreenHeight();
            float mapPixelWidth = _map.Width * Constants.TileWidth;
            float mapPixelHeight = _map.Height * Constants.TileHeight;
            float zoomWidth = screenWidth / mapPixelWidth;
            float zoomHeight = screenHeight / mapPixelHeight;
            _minZoom = Math.Max(zoomWidth, zoomHeight);
        }

        private void ClampPosition()
        {
            int screenWidth = Raylib.GetScreenWidth();
            int screenHeight = Raylib.GetScreenHeight();
            float scaledMapWidth = _map.Width * Constants.TileWidth * _zoom;
            float scaledMapHeight = _map.Height * Constants.TileHeight * _zoom;
            float mapWidth = _map.Width * Constants.TileWidth;
            float mapHeight = _map.Height * Constants.TileHeight;

            if (scaledMapWidth < screenWidth)
            {
                _position.X = mapWidth / 2;
            }
            else
            {
                float minX = screenWidth / (2 * _zoom);
                float maxX = mapWidth - screenWidth / (2 * _zoom);
                _position.X = Math.Clamp(_position.X, minX, maxX);
            }

            if (scaledMapHeight < screenHeight)
            {
                _position.Y = mapHeight / 2;
            }
            else
            {
                float minY = screenHeight / (2 * _zoom);
                float maxY = mapHeight - screenHeight / (2 * _zoom);
                _position.Y = Math.Clamp(_position.Y, minY, maxY);
            }
        }
    }
}
