namespace IsometricMapViewer
{
    public static class Constants
    {
        public const string OutputPath = "/home/leduardo";
        public const string MapName = "aresden";
        public const int TileWidth = 32;
        public const int TileHeight = 32;
        public const int ExpectedTileSize = 10;
        public const int HeaderBufferSize = 256;

        public static readonly (string fileName, int startIndex, int count)[] SpritesToLoad =
        [
         ("maptiles1.spr", 0, 32),
         ("structures1.spr", 51, 5),
         ("sinside1.spr", 70, 27),
         ("trees1.spr", 100, 46),
         ("treeshadows.spr", 150, 46),
         ("objects1.spr", 200, 10),
         ("objects2.spr", 211, 5),
         ("objects3.spr", 216, 4),
         ("objects4.spr", 220, 2),
         ("tile223-225.spr", 223, 3),
         ("tile226-229.spr", 226, 4),
         ("objects5.spr", 230, 9),
         ("objects6.spr", 238, 4),
         ("objects7.spr", 242, 7),
         ("maptiles2.spr", 300, 15),
         ("maptiles4.spr", 320, 10),
         ("maptiles5.spr", 330, 19),
         ("maptiles6.spr", 349, 4),
         ("maptiles353-361.spr", 353, 9),
         ("tile363-366.spr", 363, 4),
         ("tile367-367.spr", 367, 1),
         ("tile370-381.spr", 370, 12),
         ("tile382-387.spr", 382, 6),
         ("tile388-402.spr", 388, 15),
         ("tile403-405.spr", 403, 3),
         ("tile406-421.spr", 406, 16),
         ("tile422-429.spr", 422, 8),
         ("tile430-443.spr", 430, 14),
         ("tile444-444.spr", 444, 1),
         ("tile445-461.spr", 445, 17),
         ("tile462-473.spr", 462, 12),
         ("tile474-478.spr", 474, 5),
         ("tile479-488.spr", 479, 10),
         ("tile489-522.spr", 489, 34),
         ("tile523-530.spr", 523, 8),
         ("tile531-540.spr", 531, 10),
         ("tile541-545.spr", 541, 5)
        ];

        // Default camera zoom level to show the entire 250x250 map in the 1280x720 viewport (eagle view)
        public const float DefaultCameraZoom = 0.14f;

        // Minimum and maximum zoom levels for camera control, allowing scaling for visibility
        public const float MinCameraZoom = 0.01f; // Allow even further zoom out for eagle view
        public const float MaxCameraZoom = 2.0f; // Keep max zoom for finer control if needed

        // Camera movement speed, scaled by tile width for consistent movement
        public const float BaseCameraSpeed = 10.0f;

        // Structure for sprite frame data in sprite files
        public struct SpriteFrame
        {
            public int Left;
            public int Top;
            public int Width;
            public int Height;
            public int PivotX;
            public int PivotY;
        }
    }
}