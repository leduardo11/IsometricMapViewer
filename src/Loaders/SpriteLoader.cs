using System;
using System.Collections.Generic;
using System.IO;
using HelbreathAssetPipeline.Packer;
using Raylib_cs;

namespace IsometricMapViewer.Loaders
{
    public class SpriteLoader : IDisposable
    {
        private readonly Dictionary<string, SpriteFile> _spriteFiles = [];
        private readonly Dictionary<int, Sprite> _pakSprites = [];
        private readonly Dictionary<int, Texture2D> _spriteTextures = [];

        public void LoadSprites()
        {
            // PAKs from helbreath_lite are the canonical tile/object source.
            // Load them first so the legacy .spr files only fill gaps.
            LoadPakSprites();
            LoadSprSprites();
        }

        private void LoadPakSprites()
        {
            string paksRoot = ResourcePaths.Paks;
            if (!Directory.Exists(paksRoot))
            {
                ConsoleLogger.LogWarning($"PAK directory not found: {paksRoot}");
                return;
            }

            var spriteCache = new SpriteCache();

            foreach (var entry in TilePakCatalog.Entries)
            {
                string pakPath = Path.Combine(paksRoot, entry.Pak.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(pakPath))
                {
                    ConsoleLogger.LogWarning($"Missing PAK file: {pakPath}");
                    continue;
                }

                PakFile pak;
                try
                {
                    pak = PakReader.Read(pakPath);
                }
                catch (Exception ex)
                {
                    ConsoleLogger.LogError($"Failed to read {pakPath}: {ex.Message}");
                    continue;
                }

                for (int local = 0; local < pak.Sprites.Length; local++)
                {
                    int index = entry.IdStart + local;
                    if (index > entry.IdEnd) break;

                    var image = spriteCache.Image(pakPath, pak, local);
                    if (image is null) continue;

                    var (pixels, width, height) = image.Value;
                    Texture2D texture = CreateTexture(pixels, width, height);

                    var sprite = new Sprite
                    {
                        Index = index,
                        Texture = texture,
                        Width = width,
                        Height = height
                    };

                    foreach (var frame in pak.Sprites[local].Frames)
                    {
                        var (fx, fy, fw, fh) = frame.Rect;
                        sprite.Frames.Add(new Constants.SpriteFrame
                        {
                            Left = fx,
                            Top = fy,
                            Width = fw,
                            Height = fh,
                            PivotX = frame.PivotX,
                            PivotY = frame.PivotY
                        });
                    }

                    _pakSprites[index] = sprite;
                    _spriteTextures[index] = texture;
                }
            }

            ConsoleLogger.LogInfo($"Loaded {_pakSprites.Count} sprites from {TilePakCatalog.Entries.Length} PAKs");
        }

        private void LoadSprSprites()
        {
            foreach (var (fileName, startIndex, count) in Constants.SpritesToLoad)
            {
                // Skip any .spr whose whole range is already covered by a PAK.
                bool covered = true;
                for (int i = 0; i < count; i++)
                {
                    if (!_pakSprites.ContainsKey(startIndex + i)) { covered = false; break; }
                }
                if (covered) continue;

                string filePath = Path.Combine(ResourcePaths.Sprites, fileName);
                var spriteFile = new SpriteFile();
                try
                {
                    spriteFile.Load(filePath, startIndex);
                    _spriteFiles[fileName] = spriteFile;
                    foreach (var sprite in spriteFile.Sprites)
                    {
                        if (_pakSprites.ContainsKey(sprite.Index)) continue;
                        _spriteTextures[sprite.Index] = sprite.Texture;
                    }
                }
                catch (Exception ex)
                {
                    ConsoleLogger.LogError($"Failed to load {filePath}: {ex.Message}");
                    spriteFile.Dispose();
                }
            }
        }

        private static unsafe Texture2D CreateTexture(byte[] pixels, int width, int height)
        {
            fixed (byte* dataPtr = pixels)
            {
                Image image = new()
                {
                    Data = dataPtr,
                    Width = width,
                    Height = height,
                    Mipmaps = 1,
                    Format = PixelFormat.UncompressedR8G8B8A8
                };
                return Raylib.LoadTextureFromImage(image);
            }
        }

        public int GetSpriteFrameCount(int spriteId)
        {
            if (_pakSprites.TryGetValue(spriteId, out var pakSprite))
                return pakSprite.Frames.Count;

            foreach (var spriteFile in _spriteFiles.Values)
            {
                var sprite = spriteFile.GetSpriteById(spriteId);
                if (sprite != null) return sprite.Frames.Count;
            }
            return 1;
        }

        public Constants.SpriteFrame GetSpriteFrame(int spriteId, int frameIndex)
        {
            if (_pakSprites.TryGetValue(spriteId, out var pakSprite) &&
                frameIndex >= 0 && frameIndex < pakSprite.Frames.Count)
            {
                return pakSprite.Frames[frameIndex];
            }

            foreach (var spriteFile in _spriteFiles.Values)
            {
                var sprite = spriteFile.GetSpriteById(spriteId);

                if (sprite != null && frameIndex >= 0 && frameIndex < sprite.Frames.Count)
                    return sprite.Frames[frameIndex];
            }

            return new Constants.SpriteFrame
            {
                Left = 0,
                Top = 0,
                Width = Constants.TileWidth,
                Height = Constants.TileHeight,
                PivotX = 0,
                PivotY = 0
            };
        }

        public Texture2D GetTexture(int spriteId)
        {
            _spriteTextures.TryGetValue(spriteId, out Texture2D texture);
            return texture;
        }

        public IEnumerable<Sprite> GetAllSprites()
        {
            foreach (var sprite in _pakSprites.Values)
            {
                yield return sprite;
            }

            foreach (var spriteFile in _spriteFiles.Values)
            {
                foreach (var sprite in spriteFile.Sprites)
                {
                    if (_pakSprites.ContainsKey(sprite.Index)) continue;
                    yield return sprite;
                }
            }
        }

        public void Dispose()
        {
            foreach (var sprite in _pakSprites.Values)
            {
                if (sprite.Texture.Id != 0)
                {
                    Raylib.UnloadTexture(sprite.Texture);
                }
            }
            _pakSprites.Clear();

            foreach (var spriteFile in _spriteFiles.Values)
            {
                spriteFile.Dispose();
            }
        }
    }
}
