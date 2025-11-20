using System;
using System.Numerics;
using IsometricMapViewer.src;
using Raylib_cs;

namespace IsometricMapViewer
{
    public class CameraHandler
    {
        private Vector2 _position;
        private float _zoom;
        private float _minZoom;
        private Vector2 _minBoundary;
        private Vector2 _maxBoundary;
        private readonly Map _map;
        private int _viewRangeX;
        private int _viewRangeY;

        public Vector2 Position => _position;

        public float Zoom
        {
            get => _zoom;
            private set => _zoom = Math.Clamp(value, _minZoom, Constants.MaxCameraZoom);
        }

        public Matrix3x2 TransformMatrix { get; private set; }

        public CameraHandler(Map map)
        {
            _map = map;
            _zoom = Constants.DefaultCameraZoom;
            _position = Vector2.Zero;
            Initialize();
            _position = new Vector2(_map.Width * Constants.TileWidth / 2f, _map.Height * Constants.TileHeight / 2f);
            CalculateMinZoom();
        }

        private int ViewportWidth => Raylib.GetScreenWidth();
        private int ViewportHeight => Raylib.GetScreenHeight();

        private void Initialize()
        {
            int tileWidth = Constants.TileWidth;
            int tileHeight = Constants.TileHeight;
            var mapWidth = _map.Width * tileWidth;
            var mapHeight = _map.Height * tileHeight;
            _minBoundary = new Vector2(0, 0);
            _maxBoundary = new Vector2(mapWidth - ViewportWidth / _zoom, mapHeight - ViewportHeight / _zoom);
            int centerX = _map.Width / 2;
            int centerY = _map.Height / 2;
            FocusOnPoint(new Vector2(centerX * tileWidth, centerY * tileHeight));
            UpdateViewRange();
        }

        public void FitToMap()
        {
            float zoomWidth = ViewportWidth / (float)(_map.Width * Constants.TileWidth);
            float zoomHeight = ViewportHeight / (float)(_map.Height * Constants.TileHeight);
            Zoom = Math.Min(zoomWidth, zoomHeight);
            Vector2 mapCenter = new(_map.Width * Constants.TileWidth / 2f, _map.Height * Constants.TileHeight / 2f);
            FocusOnPoint(mapCenter);
        }

        public void Move(Vector2 movement)
        {
            _position += movement;
            ClampPosition();
            UpdateTransformMatrix();
        }

        public void ZoomAt(float zoomFactor, Vector2 screenPosition)
        {
            var worldBefore = ScreenToWorld(screenPosition);
            Zoom *= zoomFactor;
            var worldAfter = ScreenToWorld(screenPosition);
            _position += worldBefore - worldAfter;
            ClampPosition();
            UpdateTransformMatrix();
        }

        public void FocusOnPoint(Vector2 worldPosition)
        {
            _position = new Vector2(
                worldPosition.X - ViewportWidth / (2f * _zoom),
                worldPosition.Y - ViewportHeight / (2f * _zoom)
            );
            ClampPosition();
            UpdateTransformMatrix();
        }

        public Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            // Inverse transform: (screen - translation) / zoom + position
            var invZoom = 1f / _zoom;
            var world = (screenPosition - new Vector2(ViewportWidth / 2f, ViewportHeight / 2f)) * invZoom + _position;
            return world;
        }

        public Rectangle GetViewBounds()
        {
            int tileWidth = Constants.TileWidth;
            int tileHeight = Constants.TileHeight;
            int maxViewRange = Math.Max(_map.Width, _map.Height);
            _viewRangeX = Math.Min((int)(ViewportWidth / (tileWidth * _zoom)) + 1, maxViewRange);
            _viewRangeY = Math.Min((int)(ViewportHeight / (tileHeight * _zoom)) + 1, maxViewRange);
            var topLeft = ScreenToWorld(Vector2.Zero);
            int startX = (int)(topLeft.X / tileWidth);
            int startY = (int)(topLeft.Y / tileHeight);
            return new Rectangle(startX, startY, _viewRangeX, _viewRangeY);
        }

        private void UpdateViewRange()
        {
            int tileWidth = Constants.TileWidth;
            int tileHeight = Constants.TileHeight;
            int maxViewRange = Math.Max(_map.Width, _map.Height);
            _viewRangeX = Math.Min((int)(ViewportWidth / (tileWidth * _zoom)) + 1, maxViewRange);
            _viewRangeY = Math.Min((int)(ViewportHeight / (tileHeight * _zoom)) + 1, maxViewRange);
        }

        private void UpdateTransformMatrix()
        {
            // 2D transform: scale, then translate to screen center, then translate by -position
            TransformMatrix =
                Matrix3x2.CreateTranslation(-_position) *
                Matrix3x2.CreateScale(_zoom) *
                Matrix3x2.CreateTranslation(new Vector2(ViewportWidth / 2f, ViewportHeight / 2f));
        }

        private void CalculateMinZoom()
        {
            float mapPixelWidth = _map.Width * Constants.TileWidth;
            float mapPixelHeight = _map.Height * Constants.TileHeight;
            float viewportWidth = ViewportWidth;
            float viewportHeight = ViewportHeight;
            float zoomWidth = viewportWidth / mapPixelWidth;
            float zoomHeight = viewportHeight / mapPixelHeight;
            _minZoom = Math.Max(zoomWidth, zoomHeight);
        }

        private void ClampPosition()
        {
            float scaledMapWidth = _map.Width * Constants.TileWidth * _zoom;
            float scaledMapHeight = _map.Height * Constants.TileHeight * _zoom;
            float mapWidth = _map.Width * Constants.TileWidth;
            float mapHeight = _map.Height * Constants.TileHeight;

            // X-axis
            if (scaledMapWidth < ViewportWidth)
            {
                _position.X = mapWidth / 2f;
            }
            else
            {
                float minX = ViewportWidth / (2f * _zoom);
                float maxX = mapWidth - ViewportWidth / (2f * _zoom);
                _position.X = Math.Clamp(_position.X, minX, maxX);
            }

            // Y-axis
            if (scaledMapHeight < ViewportHeight)
            {
                _position.Y = mapHeight / 2f;
            }
            else
            {
                float minY = ViewportHeight / (2f * _zoom);
                float maxY = mapHeight - ViewportHeight / (2f * _zoom);
                _position.Y = Math.Clamp(_position.Y, minY, maxY);
            }
        }
    }
}
