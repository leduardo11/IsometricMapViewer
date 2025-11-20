using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Raylib_cs;
using IsometricMapViewer.src;

namespace IsometricMapViewer
{
    public class TileLoader
    {
        private readonly Dictionary<string, SpriteFile> spriteSheets = [];
        private readonly Texture2D defaultTexture;

        public TileLoader()
        {
            // Create a 32x32 magenta texture using Raylib
            Image img = Raylib.GenImageColor(32, 32, Color.Magenta);
            defaultTexture = Raylib.LoadTextureFromImage(img);
            Raylib.UnloadImage(img);
        }

        public bool IsLoadingComplete => spriteSheets.Count > 0;

        public void LoadTiles(CameraHandler camera, Map map)
        {
            var viewBounds = camera.GetViewBounds();
            var visibleTiles = map.GetVisibleTiles(viewBounds);
            var uniqueSpriteIds = new HashSet<int>();

            foreach (var tile in visibleTiles)
            {
                if (tile.TileSprite != -1) uniqueSpriteIds.Add(tile.TileSprite);
                if (tile.ObjectSprite != -1) uniqueSpriteIds.Add(tile.ObjectSprite);
            }

            foreach (var spriteId in uniqueSpriteIds)
            {
                LoadSpriteSheetForId(spriteId);
            }
            map.AssociateTilesWithSprites(GetTiles(), defaultTexture);
        }

        public void PreloadAllSprites()
        {
            var spriteDataList = new List<(string filePath, int startIndex, int count, byte[] fileData)>();

            Parallel.ForEach(Constants.SpritesToLoad, spriteLoad =>
            {
                string filePath = Path.Combine("Sprites", spriteLoad.fileName);

                if (!File.Exists(filePath))
                {
                    ConsoleLogger.LogError($"Missing sprite file: {filePath}");
                    return;
                }

                try
                {
                    byte[] fileData = File.ReadAllBytes(filePath);

                    lock (spriteDataList)
                    {
                        spriteDataList.Add((filePath, spriteLoad.startIndex, spriteLoad.count, fileData));
                    }
                }
                catch (Exception ex)
                {
                    ConsoleLogger.LogError($"Failed to read {filePath}: {ex.Message}");
                }
            });

            foreach (var (filePath, startIndex, count, fileData) in spriteDataList)
            {
                if (spriteSheets.ContainsKey(filePath)) continue;

                try
                {
                    SpriteFile spriteFile = new();
                    spriteFile.Load(fileData, startIndex);
                    spriteSheets[filePath] = spriteFile;
                }
                catch (Exception ex)
                {
                    ConsoleLogger.LogError($"Failed to preload {filePath}: {ex.Message}");
                }
            }
        }

        private void LoadSpriteSheetForId(int spriteId)
        {
            var spriteLoad = Constants.SpritesToLoad.FirstOrDefault(s => spriteId >= s.startIndex && spriteId < s.startIndex + s.count);

            if (spriteLoad == default)
            {
                ConsoleLogger.LogWarning($"No sprite sheet found for ID {spriteId}");
                return;
            }

            string filePath = Path.Combine("Sprites", spriteLoad.fileName);

            if (!spriteSheets.ContainsKey(filePath))
            {
                try
                {
                    SpriteFile spriteFile = new();
                    spriteFile.Load(filePath, spriteLoad.startIndex);
                    spriteSheets[filePath] = spriteFile;
                }
                catch (FileNotFoundException ex)
                {
                    ConsoleLogger.LogError($"Sprite file not found: {ex.FileName}");
                }
                catch (Exception ex)
                {
                    ConsoleLogger.LogError($"Failed to load sprite file {filePath}: {ex.Message}");
                }
            }
        }

        public Dictionary<int, Tile> GetTiles()
        {
            var tiles = new Dictionary<int, Tile>(Constants.SpritesToLoad.Sum(s => s.count));

            foreach (var (fileName, startIndex, count) in Constants.SpritesToLoad)
            {
                string filePath = Path.Combine("Sprites", fileName);

                if (spriteSheets.TryGetValue(filePath, out var spriteFile))
                {
                    for (int i = 0; i < spriteFile.Sprites.Count; i++)
                    {
                        int spriteId = startIndex + i;
                        tiles[spriteId] = new Tile(spriteFile.Sprites[i].Texture ?? defaultTexture);
                    }
                }
            }
            return tiles;
        }
    }
}
