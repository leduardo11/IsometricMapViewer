using IsometricMapViewer;
using System;

// Check for command-line export mode
if (args.Length > 0 && args[0] == "--export-budget-dungeon")
{
    if (args.Length < 2)
    {
        Console.WriteLine("Usage: dotnet run -- --export-budget-dungeon <mapname> [output-path]");
        Console.WriteLine("Example: dotnet run -- --export-budget-dungeon arefarm /home/leduardo/maps");
        return;
    }

    string mapName = args[1];
    string outputPath = args.Length > 2 ? args[2] : "/home/leduardo";
    
    ExportMapForBudgetDungeon(mapName, outputPath);
    return;
}

// Normal GUI mode
var game = new MainGame();
game.Run();

static void ExportMapForBudgetDungeon(string mapName, string outputPath)
{
    Console.WriteLine($"Exporting map '{mapName}' for BudgetDungeon...");
    
    try
    {
        var mapPath = System.IO.Path.Combine("resources", "maps", $"{mapName}.amd");
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
        
        // Export JSON only (no PNG in headless mode)
        var exporter = new BudgetDungeonExporter(null, map);
        var mapFolder = System.IO.Path.Combine(outputPath, mapName);
        System.IO.Directory.CreateDirectory(mapFolder);
        
        var jsonPath = System.IO.Path.Combine(mapFolder, $"{mapName}.json");
        exporter.ExportJsonOnly(jsonPath, mapName);
        
        Console.WriteLine($"✓ Exported map data to: {jsonPath}");
        Console.WriteLine($"  Note: Use GUI mode (Ctrl+B) to also export PNG image");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during export: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
}
