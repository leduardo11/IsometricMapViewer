using IsometricMapViewer;
using System;

// Check for command-line export mode
if (args.Length > 0 && args[0] == "--export")
{
    if (args.Length < 2)
    {
        Console.WriteLine("Usage: dotnet run -- --export <mapname> [output-path]");
        Console.WriteLine("Example: dotnet run -- --export arefarm /home/leduardo/maps");
        return;
    }

    string mapName = args[1];
    string outputPath = args.Length > 2 ? args[2] : "/home/leduardo/exported-maps";
    
    ExportMapCLI(mapName, outputPath);
    return;
}

// HelbreathAtlasPacker export mode:
//   dotnet run -- --atlas <format> <mapname> [output-root]
// formats: package | rpg | godot | tiled | map-shot | master | olympia | all
if (args.Length > 0 && args[0] == "--atlas")
{
    if (args.Length < 3)
    {
        Console.WriteLine("Usage: dotnet run -- --atlas <format> <mapname> [output-root]");
        Console.WriteLine("Formats: package | rpg | godot | tiled | map-shot | master | olympia | all");
        return;
    }

    string format = args[1];
    string mapName = args[2];
    string outputRoot = args.Length > 3 ? args[3] : "resources/exported";

    RunAtlasExport(format, mapName, outputRoot);
    return;
}

// Headless smoke test: load every PAK-backed sprite (no visible window).
if (args.Length > 0 && args[0] == "--verify-sprites")
{
    Raylib_cs.Raylib.SetConfigFlags(Raylib_cs.ConfigFlags.HiddenWindow);
    Raylib_cs.Raylib.InitWindow(320, 240, "verify-sprites");

    var loader = new IsometricMapViewer.Loaders.SpriteLoader();
    loader.LoadSprites();

    int total = 0, ground = 0, objects = 0, empty = 0;
    foreach (var sprite in loader.GetAllSprites())
    {
        total++;
        if (sprite.Texture.Id == 0) empty++;
        else if ((sprite.Index >= 50 && sprite.Index <= 69) ||
                 (sprite.Index >= 100 && sprite.Index <= 145) ||
                 (sprite.Index >= 200 && sprite.Index <= 248)) objects++;
        else if (sprite.Index < 150 || sprite.Index > 195) ground++;
    }

    Console.WriteLine($"sprites={total} ground={ground} objects={objects} no-texture={empty}");

    loader.Dispose();
    Raylib_cs.Raylib.CloseWindow();
    return;
}

// GUI mode
var app = new ExporterApp();
app.Run();

static void RunAtlasExport(string format, string mapName, string outputRoot)
{
    var exporter = new AtlasPackerExporter(
        ResourcePaths.Paks,
        ResourcePaths.Maps,
        outputRoot);

    try
    {
        string result = format.ToLowerInvariant() switch
        {
            "package" => exporter.ExportPackage(mapName),
            "rpg" => exporter.ExportRpg(mapName),
            "godot" => exporter.ExportGodot(mapName),
            "tiled" => exporter.ExportTiled(mapName),
            "map-shot" or "mapshot" => exporter.ExportMapShot(mapName),
            "master" => exporter.ExportMasterTiles(),
            "olympia" => exporter.ExportOlympia(mapName),
            "all" => RunAtlasExportAll(exporter, mapName),
            _ => throw new ArgumentException($"Unknown format '{format}'")
        };

        Console.WriteLine($"✓ {format} export complete: {result}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Export failed: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
    }
}

static string RunAtlasExportAll(AtlasPackerExporter exporter, string mapName)
{
    exporter.ExportAll(mapName);
    return System.IO.Path.Combine(exporter.OutputRoot, mapName);
}

static void ExportMapCLI(string mapName, string outputPath)
{
    Console.WriteLine($"Exporting map '{mapName}' for BudgetDungeon...");
    
    try
    {
        var mapPath = System.IO.Path.Combine(ResourcePaths.Maps, $"{mapName}.amd");
        if (!System.IO.File.Exists(mapPath))
        {
            Console.WriteLine($"Error: Map file not found: {mapPath}");
            return;
        }
        
        var map = new Map();
        if (!map.Load(mapPath))
        {
            Console.WriteLine($"Error: Failed to load map: {mapPath}");
            return;
        }
        
        Console.WriteLine($"✓ Loaded map: {map.Width}x{map.Height} tiles");
        
        var exporter = new BudgetDungeonExporter(null, map);
        var mapFolder = System.IO.Path.Combine(outputPath, mapName);
        System.IO.Directory.CreateDirectory(mapFolder);
        
        var jsonPath = System.IO.Path.Combine(mapFolder, $"{mapName}.json");
        exporter.ExportJsonOnly(jsonPath, mapName);
        
        Console.WriteLine($"✓ Exported map data to: {jsonPath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during export: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
}
