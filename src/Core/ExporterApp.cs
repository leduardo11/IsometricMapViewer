using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using IsometricMapViewer.Editor;
using IsometricMapViewer.Handlers;
using IsometricMapViewer.Loaders;
using IsometricMapViewer.Rendering;
using IsometricMapViewer.UI;
using Microsoft.Extensions.Configuration;
using Raylib_cs;

namespace IsometricMapViewer;

public class ExporterApp
{
    private Map _map = null!;
    private BudgetDungeonExporter _exporter = null!;
    private AtlasPackerExporter _atlasExporter = null!;
    private CameraHandler _camera = null!;
    private GameRenderer _renderer = null!;
    private SpriteLoader _spriteLoader = null!;
    private Font _font;
    private AppSettings _settings = null!;

    private EditorState _editorState = null!;
    private CommandHistory _history = null!;
    private EditorInputHandler _editorInputHandler = null!;
    private PaletteUI _paletteUI = null!;

    private bool _isExporting = false;
    private string _exportingMessage = "";

    public ExporterApp()
    {
        LoadSettings();
    }

    private void LoadSettings()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        _settings = new AppSettings();
        config.GetSection("MapExporter").Bind(_settings.MapExporter);
    }

    public void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(UIConfig.WINDOW_WIDTH, UIConfig.WINDOW_HEIGHT, UIConfig.WINDOW_TITLE);
        Raylib.SetTargetFPS(60);

        Initialize();
        LoadContent();

        while (!Raylib.WindowShouldClose())
        {
            Update();
            Draw();
        }

        Dispose();
        Raylib.CloseWindow();
    }

    private void Initialize()
    {
        var tileLoader = new TileLoader();
        tileLoader.PreloadAllSprites();

        LoadMap(_settings.MapExporter.MapName);

        if (_map != null)
        {
            _map.ValidateMapSprites(tileLoader.GetTiles());
            _camera = new CameraHandler(_map);
            _camera.FitToMap();
        }
    }

    private void LoadContent()
    {
        _font = Raylib.LoadFont(Path.Combine(ResourcePaths.Fonts, "DejaVuSansMono.ttf"));
        _spriteLoader = new SpriteLoader();
        _spriteLoader.LoadSprites();

        _renderer = new GameRenderer(_font, _map, _spriteLoader)
        {
            ShowObjects = _settings.MapExporter.ShowObjects,
            ShowGrid = _settings.MapExporter.ShowGrid
        };

        _editorState = new EditorState
        {
            ShowObjects = _settings.MapExporter.ShowObjects,
            ShowGrid = _settings.MapExporter.ShowGrid
        };
        _history = new CommandHistory();
        _paletteUI = new PaletteUI(_spriteLoader);

        _editorInputHandler = new EditorInputHandler(
            _map,
            _camera,
            _editorState,
            _history,
            _spriteLoader,
            SaveMapAmd,
            ExportAtlas
        );

        _atlasExporter = new AtlasPackerExporter(
            ResourcePaths.Paks,
            ResourcePaths.Maps,
            _settings.MapExporter.AtlasOutputPath);
    }

    private void Update()
    {
        float dt = Raylib.GetFrameTime();
        _editorState.Update(dt);

        if (_isExporting) return;

        _editorInputHandler.Update();
    }

    private void Draw()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);

        // Draw Map with 5 layers & editor ghost
        if (_map != null && _renderer != null)
        {
            _renderer.DrawEditorMap(_camera, _editorState);
        }

        // Draw HUD: Navigation Bar, Tool Dock, Status Toasts
        EditorUI.Draw(_map, _camera, _editorState, _history, _font);

        // Draw Sprite Palette if open
        _paletteUI.Draw(_editorState, _spriteLoader, _font);

        // Draw export loading overlay if active
        if (_isExporting)
            DrawExportingOverlay();

        Raylib.EndDrawing();
    }

    private void DrawExportingOverlay()
    {
        int screenW = Raylib.GetScreenWidth();
        int screenH = Raylib.GetScreenHeight();

        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 190));

        int boxWidth = 500;
        int boxHeight = 140;
        int boxX = (screenW - boxWidth) / 2;
        int boxY = (screenH - boxHeight) / 2;

        var boxBounds = new Rectangle(boxX, boxY, boxWidth, boxHeight);
        Raylib.DrawRectangleRec(boxBounds, new Color(25, 28, 36, 255));
        Raylib.DrawRectangleLinesEx(boxBounds, 2, new Color(80, 140, 255, 255));

        float time = (float)Raylib.GetTime();
        int dots = ((int)(time * 2) % 4);
        string loadingText = _exportingMessage + new string('.', dots);

        int textWidth = Raylib.MeasureText(loadingText, UIConfig.FONT_LARGE);
        int textX = boxX + (boxWidth - textWidth) / 2;
        int textY = boxY + 36;

        Raylib.DrawText(loadingText, textX, textY, UIConfig.FONT_LARGE, Color.Gold);

        string infoText = "Processing map and generating packages...";
        int infoWidth = Raylib.MeasureText(infoText, UIConfig.FONT_SMALL);
        int infoX = boxX + (boxWidth - infoWidth) / 2;
        int infoY = textY + 45;

        Raylib.DrawText(infoText, infoX, infoY, UIConfig.FONT_SMALL, Color.LightGray);
    }

    private void LoadMap(string mapName)
    {
        try
        {
            var mapPath = Path.Combine(ResourcePaths.Maps, $"{mapName}.amd");
            _map = new Map();

            if (!_map.Load(mapPath))
            {
                ConsoleLogger.LogError($"Failed to load map: {mapName}");
                return;
            }

            _exporter = new BudgetDungeonExporter(null, _map);
            ConsoleLogger.LogInfo($"Loaded map: {mapName}");
        }
        catch (Exception ex)
        {
            ConsoleLogger.LogError($"Error loading map: {ex.Message}");
        }
    }

    private void SaveMapAmd()
    {
        if (_map == null) return;
        try
        {
            string mapName = _settings.MapExporter.MapName;
            string mapPath = Path.Combine(ResourcePaths.Maps, $"{mapName}.amd");

            // Create backup on first edit if not yet created
            string bakPath = mapPath + ".bak";
            if (File.Exists(mapPath) && !File.Exists(bakPath))
            {
                File.Copy(mapPath, bakPath, overwrite: false);
            }

            _map.Save(mapPath);
            _editorState.SetStatus($"✓ Saved AMD to {mapPath}");
            ConsoleLogger.LogInfo($"Map saved to {mapPath}");
        }
        catch (Exception ex)
        {
            _editorState.SetStatus($"Save error: {ex.Message}");
            ConsoleLogger.LogError($"Error saving map: {ex.Message}");
        }
    }

    private void ExportAtlas(AtlasExportKind kind)
    {
        if (_map == null || _atlasExporter == null || _isExporting) return;

        // Persist edits first so the packer compiles the live map.
        SaveMapAmd();

        _isExporting = true;
        _exportingMessage = $"Exporting {kind}";

        try
        {
            string mapName = _settings.MapExporter.MapName;
            string result = kind switch
            {
                AtlasExportKind.Package => _atlasExporter.ExportPackage(mapName),
                AtlasExportKind.Rpg => _atlasExporter.ExportRpg(mapName),
                AtlasExportKind.Godot => _atlasExporter.ExportGodot(mapName),
                AtlasExportKind.Tiled => _atlasExporter.ExportTiled(mapName),
                AtlasExportKind.MapShot => _atlasExporter.ExportMapShot(mapName),
                AtlasExportKind.MasterTiles => _atlasExporter.ExportMasterTiles(),
                AtlasExportKind.Olympia => _atlasExporter.ExportOlympia(ResolveOlympiaSource()),
                AtlasExportKind.All => RunExportAll(mapName),
                _ => string.Empty
            };

            _editorState.SetStatus($"✓ {kind} exported: {result}");
            ConsoleLogger.LogInfo($"{kind} export complete: {result}");
        }
        catch (Exception ex)
        {
            _editorState.SetStatus($"{kind} export failed: {ex.Message}");
            ConsoleLogger.LogError($"{kind} export error: {ex.Message}");
        }
        finally
        {
            _isExporting = false;
        }
    }

    private string RunExportAll(string mapName)
    {
        _atlasExporter.ExportAll(mapName);
        return Path.Combine(_atlasExporter.OutputRoot, mapName);
    }

    private string ResolveOlympiaSource()
    {
        string configured = _settings.MapExporter.OlympiaSourcePath;
        if (!string.IsNullOrWhiteSpace(configured) && Directory.Exists(configured))
            return configured;

        throw new InvalidOperationException(
            "Set MapExporter:OlympiaSourcePath in appsettings.json to an Olympia OPK source tree.");
    }

    public void ExportGrid()
    {
        if (_map == null || _exporter == null || _isExporting) return;

        _isExporting = true;
        _exportingMessage = "Exporting Grid";

        try
        {
            string mapName = _settings.MapExporter.MapName;
            var mapFolder = Path.Combine(_settings.MapExporter.OutputPath, mapName);
            Directory.CreateDirectory(mapFolder);

            var jsonPath = Path.Combine(mapFolder, $"{mapName}.json");
            _exporter.ExportJsonOnly(jsonPath, mapName);

            _editorState.SetStatus($"✓ Grid exported to {mapFolder}");
        }
        catch (Exception ex)
        {
            _editorState.SetStatus($"Grid export failed: {ex.Message}");
        }
        finally
        {
            _isExporting = false;
        }
    }

    public void ExportPNG()
    {
        if (_map == null || _renderer == null || _isExporting) return;

        _isExporting = true;
        _exportingMessage = "Exporting PNG";

        try
        {
            string mapName = _settings.MapExporter.MapName;
            var mapFolder = Path.Combine(_settings.MapExporter.OutputPath, mapName);
            Directory.CreateDirectory(mapFolder);

            var pngPath = Path.Combine(mapFolder, $"{mapName}.png");

            Image mapImage = _renderer.RenderFullMapToImage();
            Raylib.ExportImage(mapImage, pngPath);
            Raylib.UnloadImage(mapImage);

            _editorState.SetStatus($"✓ PNG exported to {mapFolder}");
        }
        catch (Exception ex)
        {
            _editorState.SetStatus($"PNG export failed: {ex.Message}");
        }
        finally
        {
            _isExporting = false;
        }
    }

    private void Dispose()
    {
        _renderer?.Dispose();
        _spriteLoader?.Dispose();
        Raylib.UnloadFont(_font);
    }
}
