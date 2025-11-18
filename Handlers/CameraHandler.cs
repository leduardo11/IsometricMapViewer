using System;

namespace IsometricMapViewer.Handlers
{
    public class CameraHandler
    {
        private Vector2 _position;
        private float _zoom;
        private float _minZoom;
        private Vector2 _minBoundary;
        private Vector2 _maxBoundary;
        private readonly GraphicsDevice _graphicsDevice;
        private readonly Map _map;
        private int _viewRangeX;
        private int _viewRangeY;

        public Vector2 Position => _position;

        public float Zoom
        {
            get => _zoom;
            private set => _zoom = MathHelper.Clamp(value, _minZoom, Constants.MaxCameraZoom);
        }

        public Matrix TransformMatrix { get; private set; }
        public Viewport Viewport => _graphicsDevice.Viewport;

        public CameraHandler(GraphicsDevice graphicsDevice, Map map)
        {
            _graphicsDevice = graphicsDevice;
            _map = map;
            _zoom = Constants.DefaultCameraZoom;
            _position = Vector2.Zero;
            Initialize();
            _position = new Vector2(_map.Width * Constants.TileWidth / 2, _map.Height * Constants.TileHeight / 2);
            CalculateMinZoom();
        }

        private void Initialize()
        {
            int tileWidth = Constants.TileWidth;
            int tileHeight = Constants.TileHeight;
            var mapWidth = _map.Width * tileWidth;
            var mapHeight = _map.Height * tileHeight;
            _minBoundary = new Vector2(0, 0);
            _maxBoundary = new Vector2(mapWidth - Viewport.Width / _zoom, mapHeight - Viewport.Height / _zoom);
            int centerX = _map.Width / 2;
            int centerY = _map.Height / 2;
            FocusOnPoint(new Vector2(centerX * tileWidth, centerY * tileHeight));
            UpdateViewRange();
        }

        public void FitToMap()
        {
            float zoomWidth = Viewport.Width / (float)(_map.Width * Constants.TileWidth);
            float zoomHeight = Viewport.Height / (float)(_map.Height * Constants.TileHeight);
            Zoom = Math.Min(zoomWidth, zoomHeight);
            Vector2 mapCenter = new(_map.Width * Constants.TileWidth / 2, _map.Height * Constants.TileHeight / 2);
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
            _position = new Vector2(worldPosition.X - Viewport.Width / (2 * _zoom), worldPosition.Y - Viewport.Height / (2 * _zoom));
            ClampPosition();
            UpdateTransformMatrix();
        }

        public Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            return Vector2.Transform(screenPosition, Matrix.Invert(TransformMatrix));
        }

        public Rectangle GetViewBounds()
        {
            int tileWidth = Constants.TileWidth;
            int tileHeight = Constants.TileHeight;
            int maxViewRange = Math.Max(_map.Width, _map.Height);
            _viewRangeX = Math.Min((int)(Viewport.Width / (tileWidth * _zoom)) + 1, maxViewRange);
            _viewRangeY = Math.Min((int)(Viewport.Height / (tileHeight * _zoom)) + 1, maxViewRange);
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
            _viewRangeX = Math.Min((int)(Viewport.Width / (tileWidth * _zoom)) + 1, maxViewRange);
            _viewRangeY = Math.Min((int)(Viewport.Height / (tileHeight * _zoom)) + 1, maxViewRange);
        }

        private void UpdateTransformMatrix()
        {
            TransformMatrix = Matrix.CreateTranslation(-_position.X, -_position.Y, 0) *
                              Matrix.CreateScale(_zoom) *
                              Matrix.CreateTranslation(Viewport.Width / 2, Viewport.Height / 2, 0);
        }

        private void CalculateMinZoom()
        {
            // Map dimensions in pixels
            float mapPixelWidth = _map.Width * Constants.TileWidth;
            float mapPixelHeight = _map.Height * Constants.TileHeight;

            // Viewport dimensions
            float viewportWidth = Viewport.Width;
            float viewportHeight = Viewport.Height;

            // Calculate zoom levels for width and height
            float zoomWidth = viewportWidth / mapPixelWidth;
            float zoomHeight = viewportHeight / mapPixelHeight;

            // Use the larger value to ensure the map fills the viewport
            _minZoom = Math.Max(zoomWidth, zoomHeight);
        }

        private void ClampPosition()
        {
            float scaledMapWidth = _map.Width * Constants.TileWidth * _zoom;
            float scaledMapHeight = _map.Height * Constants.TileHeight * _zoom;
            float mapWidth = _map.Width * Constants.TileWidth;
            float mapHeight = _map.Height * Constants.TileHeight;

            // Handle X-axis
            if (scaledMapWidth < Viewport.Width)
            {
                // Map is smaller than viewport, center it
                _position.X = mapWidth / 2;
            }
            else
            {
                // Map is larger, clamp position so edges stay in view
                float minX = Viewport.Width / (2 * _zoom); // Left edge aligns with viewport left
                float maxX = mapWidth - Viewport.Width / (2 * _zoom); // Right edge aligns with viewport right
                _position.X = MathHelper.Clamp(_position.X, minX, maxX);
            }

            // Handle Y-axis
            if (scaledMapHeight < Viewport.Height)
            {
                // Map is smaller than viewport, center it
                _position.Y = mapHeight / 2;
            }
            else
            {
                // Map is larger, clamp position so edges stay in view
                float minY = Viewport.Height / (2 * _zoom); // Top edge aligns with viewport top
                float maxY = mapHeight - Viewport.Height / (2 * _zoom); // Bottom edge aligns with viewport bottom
                _position.Y = MathHelper.Clamp(_position.Y, minY, maxY);
            }
        }
    }
}
