using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IsometricMapViewer.Handlers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace IsometricMapViewer
{
    public class Map
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public MapTile[,] Tiles { get; private set; }

        public Map()
        {
            Width = 250;
            Height = 250;
            Tiles = new MapTile[0, 0];
        }

        public bool Load(string amdFilePath)
        {
            try
            {
                using var stream = File.OpenRead(amdFilePath);
                using var reader = new BinaryReader(stream);

                byte[] headerBuffer = reader.ReadBytes(Constants.HeaderBufferSize);
                string header = System.Text.Encoding.ASCII.GetString(headerBuffer).TrimEnd('\0');
                var headerValues = ParseHeader(header);

                if (!headerValues.TryGetValue("MAPSIZEX", out int width) || !headerValues.TryGetValue("MAPSIZEY", out int height))
                {
                    ConsoleLogger.LogError("Missing MAPSIZEX or MAPSIZEY in header");
                    return false;
                }

                ConsoleLogger.LogInfo($"Map size: {width}x{height}");

                Width = width;
                Height = height;
                Tiles = new MapTile[Width, Height];

                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        Tiles[x, y] = new MapTile(x, y);
                    }
                }


                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        byte[] tileBuffer = reader.ReadBytes(Constants.ExpectedTileSize);
                        Tiles[x, y] = MapTile.Parse(tileBuffer, x, y);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError($"Failed to load map {amdFilePath}: {ex.Message}");
                return false;
            }
        }

        public void ValidateMapSprites(Dictionary<int, Tile> loadedTiles)
        {
            var usedTileSprites = new HashSet<int>(Tiles.Cast<MapTile>()
                .Select(t => (int)t.TileSprite)
                .Where(id => id != -1));
            var usedObjectSprites = new HashSet<int>(Tiles.Cast<MapTile>()
                .Select(t => (int)t.ObjectSprite)
                .Where(id => id != -1));

            // Identify missing sprites
            var missingTileSprites = usedTileSprites.Where(id => !loadedTiles.ContainsKey(id)).ToList();
            var missingObjectSprites = usedObjectSprites.Where(id => !loadedTiles.ContainsKey(id)).ToList();

            // Calculate loaded counts
            int loadedTileSpritesCount = usedTileSprites.Count - missingTileSprites.Count;
            int loadedObjectSpritesCount = usedObjectSprites.Count - missingObjectSprites.Count;

            // Log summary
            ConsoleLogger.LogInfo("🟢 Map Loading Summary:");
            ConsoleLogger.LogInfo($"  - Total Unique Tile Sprites Used: {usedTileSprites.Count}");
            ConsoleLogger.LogInfo($"  - Loaded Tile Sprites: {loadedTileSpritesCount}");

            if (missingTileSprites.Count > 0)
            {
                ConsoleLogger.LogWarning($"  - Missing Tile Sprites: {missingTileSprites.Count}");
                ConsoleLogger.LogWarning($"    IDs: {string.Join(", ", missingTileSprites.OrderBy(x => x))}");
            }
            else
            {
                ConsoleLogger.LogInfo("  - No missing tile sprites.");
            }

            ConsoleLogger.LogInfo($"  - Total Unique Object Sprites Used: {usedObjectSprites.Count}");
            ConsoleLogger.LogInfo($"  - Loaded Object Sprites: {loadedObjectSpritesCount}");

            if (missingObjectSprites.Count > 0)
            {
                ConsoleLogger.LogWarning($"  - Missing Object Sprites: {missingObjectSprites.Count}");
                ConsoleLogger.LogWarning($"    IDs: {string.Join(", ", missingObjectSprites.OrderBy(x => x))}");
            }
            else
            {
                ConsoleLogger.LogInfo("  - No missing object sprites.");
            }
        }

        private static Dictionary<string, int> ParseHeader(string header)
        {
            var values = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var tokens = header.Split(new char[] { '=', ',', '\t', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < tokens.Length - 1; i++)
            {
                string key = tokens[i].Trim();

                if (int.TryParse(tokens[i + 1], out int value))
                {
                    values[key] = value;
                }
            }
            return values;
        }

        public MapTile GetTileAtWorldPosition(Vector2 worldPos)
        {
            int tileX = (int)(worldPos.X / Constants.TileWidth);
            int tileY = (int)(worldPos.Y / Constants.TileHeight);

            if (tileX >= 0 && tileX < Width && tileY >= 0 && tileY < Height)
            {
                return Tiles[tileX, tileY];
            }

            return null;
        }

        public IEnumerable<MapTile> GetVisibleTiles(Rectangle viewBounds)
        {
            int startX = Math.Max(0, viewBounds.Left);
            int endX = Math.Min(Width, viewBounds.Right);
            int startY = Math.Max(0, viewBounds.Top);
            int endY = Math.Min(Height, viewBounds.Bottom);
            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    yield return Tiles[x, y];
                }
            }
        }

        public void UpdateTileProperties(int x, int y, bool isMoveAllowed, bool isTeleport, bool isFarmingAllowed, bool isWater)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                throw new ArgumentOutOfRangeException("Tile coordinates out of range");
            }
            Tiles[x, y].SetProperties(isMoveAllowed, isTeleport, isFarmingAllowed, isWater);
        }

        public void Save(string amdFilePath)
        {
            try
            {
                using var stream = File.OpenWrite(amdFilePath);
                using var writer = new BinaryWriter(stream);

                // Write header
                string header = $"MAPSIZEX={Width},MAPSIZEY={Height}\0";
                byte[] headerBytes = System.Text.Encoding.ASCII.GetBytes(header);

                if (headerBytes.Length > Constants.HeaderBufferSize)
                {
                    throw new InvalidOperationException("Header too large");
                }

                writer.Write(headerBytes);
                int padding = Constants.HeaderBufferSize - headerBytes.Length;

                if (padding > 0)
                {
                    writer.Write(new byte[padding]);
                }

                // Write tile data
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        var tile = Tiles[x, y];
                        writer.Write((short)tile.TileSprite);
                        writer.Write((short)tile.TileFrame);
                        writer.Write((short)tile.ObjectSprite);
                        writer.Write((short)tile.ObjectFrame);
                        byte flags = 0;
                        if (!tile.IsMoveAllowed) flags |= 0x80;
                        if (tile.IsTeleport) flags |= 0x40;
                        if (tile.IsFarmingAllowed) flags |= 0x20;
                        if (tile.IsWater) flags |= 0x10;
                        writer.Write(flags);
                        writer.Write((byte)0); // Byte 9, assumed unused
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError($"Failed to save map {amdFilePath}: {ex.Message}");
            }
        }

        public void AssociateTilesWithSprites(Dictionary<int, Tile> cacheTiles, Texture2D defaultTexture)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var tile = Tiles[x, y];

                    if (tile.TileSprite != -1)
                    {
                        tile.BaseTile = cacheTiles.TryGetValue(tile.TileSprite, out Tile baseTile)
                            ? baseTile
                            : new Tile(defaultTexture);
                    }

                    if (tile.ObjectSprite != -1)
                    {
                        tile.ObjectTile = cacheTiles.TryGetValue(tile.ObjectSprite, out Tile objectTile)
                            ? objectTile
                            : new Tile(defaultTexture);
                    }
                }
            }
        }
    }

    public class MapTile(int x, int y)
    {
        public int X { get; } = x;
        public int Y { get; } = y;
        public short TileSprite { get; private set; }
        public short TileFrame { get; private set; }
        public short ObjectSprite { get; private set; }
        public short ObjectFrame { get; private set; }
        public bool IsMoveAllowed { get; private set; }
        public bool IsTeleport { get; private set; }
        public bool IsFarmingAllowed { get; private set; }
        public bool IsWater { get; private set; }
        public Tile BaseTile { get; set; } = null!;
        public Tile ObjectTile { get; set; } = null!;

        public static MapTile Parse(byte[] data, int x, int y)
        {
            var tile = new MapTile(x, y)
            {
                TileSprite = BitConverter.ToInt16(data, 0),
                TileFrame = BitConverter.ToInt16(data, 2),
                ObjectSprite = BitConverter.ToInt16(data, 4),
                ObjectFrame = BitConverter.ToInt16(data, 6)
            };

            byte flags = data[8];
            tile.IsMoveAllowed = (flags & 0x80) == 0;
            tile.IsTeleport = (flags & 0x40) != 0;
            tile.IsFarmingAllowed = (flags & 0x20) != 0;
            tile.IsWater = (flags & 0x10) != 0;
            return tile;
        }

        public void SetProperties(bool isMoveAllowed, bool isTeleport, bool isFarmingAllowed, bool isWater)
        {
            IsMoveAllowed = isMoveAllowed;
            IsTeleport = isTeleport;
            IsFarmingAllowed = isFarmingAllowed;
            IsWater = isWater;
        }
    }



    public struct TileProperties
    {
        public bool IsMoveAllowed;
        public bool IsTeleport;
        public bool IsFarmingAllowed;
        public bool IsWater;

        public TileProperties(bool isMoveAllowed, bool isTeleport, bool isFarmingAllowed, bool isWater)
        {
            IsMoveAllowed = isMoveAllowed;
            IsTeleport = isTeleport;
            IsFarmingAllowed = isFarmingAllowed;
            IsWater = isWater;
        }

        public override bool Equals(object obj)
        {
            if (obj is TileProperties other)
            {
                return IsMoveAllowed == other.IsMoveAllowed &&
                       IsTeleport == other.IsTeleport &&
                       IsFarmingAllowed == other.IsFarmingAllowed &&
                       IsWater == other.IsWater;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(IsMoveAllowed, IsTeleport, IsFarmingAllowed, IsWater);
        }
    }

    public class Tile(Texture2D texture)
    {
        public Texture2D Texture { get; } = texture ?? throw new ArgumentNullException(nameof(texture), "Tile texture cannot be null");
    }
}