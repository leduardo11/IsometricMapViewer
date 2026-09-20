using System;
using System.IO;
using HelbreathAssetPipeline.Packer;
using HelbreathAssetPipeline.Packer.Export;
using HelbreathAssetPipeline.Packer.Export.Godot;
using HelbreathAssetPipeline.Packer.Export.Tiled;

namespace IsometricMapViewer
{
    public enum AtlasExportKind
    {
        Package,
        Rpg,
        Godot,
        Tiled,
        MapShot,
        MasterTiles,
        Olympia,
        All
    }

    // Thin adapter over the HelbreathAtlasPacker pipeline. IsometricMapViewer
    // owns the editor; the packer owns every map export format. This class only
    // wires the editor's resources (paks/maps) to the packer's compilers so the
    // editor can emit every format the packer supports without duplicating any
    // of its logic.
    public sealed class AtlasPackerExporter
    {
        private readonly string _paksDir;
        private readonly string _mapsDir;
        private readonly string _outputRoot;

        public AtlasPackerExporter(string paksDir, string mapsDir, string outputRoot)
        {
            _paksDir = Path.GetFullPath(paksDir);
            _mapsDir = Path.GetFullPath(mapsDir);
            _outputRoot = Path.GetFullPath(outputRoot);
        }

        public string OutputRoot => _outputRoot;

        // Engine-agnostic base package: atlas.json + tilemap.json +
        // collision.json + manifest.json + frame-map.json + atlases/.
        public string ExportPackage(string mapName)
        {
            var (map, pakCache) = LoadInputs(mapName);
            return ExportPackageInternal(map, pakCache, mapName);
        }

        // map@2 — the clean 6-layer RPG package (rpg_world).
        public string ExportRpg(string mapName)
        {
            var (map, pakCache) = LoadInputs(mapName);
            return RpgMapExporter.Export(map, pakCache, Path.Combine(_outputRoot, "rpg", mapName));
        }

        // Native Godot 4.x resources: TileSet.tres + map.tscn + minimap.png.
        // Mirrors the CLI: the package dir doubles as the Godot export dir.
        public string ExportGodot(string mapName)
        {
            string pkgDir = ExportPackage(mapName);
            GodotExporter.ExportMap(mapName, pkgDir, pkgDir, mapName + "/");
            return pkgDir;
        }

        // Tiled TSX tilesets (ground.tsx + objects.tsx + PNGs + manifest).
        public string ExportTiled(string mapName)
        {
            var report = TiledTilesetExporter.ExportMap(mapName, _paksDir, _mapsDir, _outputRoot);
            return report.OutputDir;
        }

        // Rendered PNG screenshot of the compiled package.
        public string ExportMapShot(string mapName)
        {
            string pkgDir = ExportPackage(mapName);
            return WriteMapShot(mapName, pkgDir);
        }

        // Full tile + object vocabulary atlas (every frame of every catalog pak).
        public string ExportMasterTiles()
        {
            MasterTileCompiler.Compile(_paksDir, _outputRoot);
            return Path.Combine(_outputRoot, "tiles");
        }

        // Everything above for one map, compiling the map + atlas only once.
        public void ExportAll(string mapName)
        {
            var (map, pakCache) = LoadInputs(mapName);
            string pkgDir = ExportPackageInternal(map, pakCache, mapName);

            RpgMapExporter.Export(map, pakCache, Path.Combine(_outputRoot, "rpg", mapName));
            GodotExporter.ExportMap(mapName, pkgDir, pkgDir, mapName + "/");
            TiledTilesetExporter.ExportMap(mapName, _paksDir, _mapsDir, _outputRoot);
            WriteMapShot(mapName, pkgDir);
            MasterTileCompiler.Compile(_paksDir, _outputRoot);
        }

        // Olympia OPK tree → OlympiaAssets/ (OPK sprites + AMD maps + metadata).
        // Requires an Olympia-style source tree, not the classic PAK set.
        public string ExportOlympia(string sourceRoot)
        {
            string outRoot = Path.Combine(_outputRoot, "OlympiaAssets");
            OlympiaExporter.Export(sourceRoot, outRoot);
            return outRoot;
        }

        private string ExportPackageInternal(AmdMap map, PakCache pakCache, string mapName)
        {
            string outDir = Path.Combine(_outputRoot, mapName);
            string atlasDir = Path.Combine(outDir, "atlases");
            Directory.CreateDirectory(atlasDir);

            var (atlas, tileMap, collision, frameMap) = SceneCompiler.Compile(map, pakCache, atlasDir);
            var manifest = new MapManifest(map.Name, map.Name, $"{map.Name}.collision", null);
            PackageWriter.Write(outDir, map.Name, atlas, tileMap, collision, manifest, frameMap);
            return outDir;
        }

        private string WriteMapShot(string mapName, string pkgDir)
        {
            string shotsDir = Path.Combine(_outputRoot, "shots");
            Directory.CreateDirectory(shotsDir);
            string outputPath = Path.Combine(shotsDir, $"{mapName}-shot.png");

            MapShotRenderer.Render(new MapShotOptions
            {
                MapName = mapName,
                PackageDir = pkgDir,
                OutputPath = outputPath
            });
            return outputPath;
        }

        private (AmdMap Map, PakCache Cache) LoadInputs(string mapName)
        {
            string amdPath = Path.Combine(_mapsDir, $"{mapName}.amd");
            if (!File.Exists(amdPath))
                throw new FileNotFoundException($"Map not found: {amdPath}");

            EnsureCanonicalHeader(amdPath);

            var map = AmdParser.Parse(amdPath);
            var pakCache = PakCache.Load(_paksDir);
            if (pakCache.PakCount == 0)
                throw new InvalidOperationException($"No tile/object PAKs found under {_paksDir}");

            return (map, pakCache);
        }

        // The packer's AmdParser requires the canonical space-separated header
        // (MAPSIZEX = N MAPSIZEY = N TILESIZE = 10); it defaults TILESIZE to 9
        // otherwise and misreads the 10-byte tile records. Maps previously saved
        // by this viewer used a comma-separated header without TILESIZE, so
        // rewrite the header once, in place, before compiling.
        private static void EnsureCanonicalHeader(string amdPath)
        {
            byte[] data = File.ReadAllBytes(amdPath);
            int headerLen = Math.Min(Constants.HeaderBufferSize, data.Length);
            string header = System.Text.Encoding.ASCII.GetString(data, 0, headerLen);
            if (header.Contains("TILESIZE", StringComparison.Ordinal)) return;

            var map = new Map();
            if (!map.Load(amdPath))
                throw new InvalidDataException($"Could not read AMD header: {amdPath}");

            map.Save(amdPath);
        }
    }
}
