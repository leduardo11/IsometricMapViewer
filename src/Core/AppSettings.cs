namespace IsometricMapViewer;

public class AppSettings
{
    public MapExporterSettings MapExporter { get; set; } = new();
}

public class MapExporterSettings
{
    public string MapName { get; set; } = "arefarm";
    public string OutputPath { get; set; } = "/home/leduardo/exported-maps";
    public bool ShowObjects { get; set; } = true;
    public bool ShowGrid { get; set; } = false;

    // Root directory for every HelbreathAtlasPacker map export format.
    public string AtlasOutputPath { get; set; } = "resources/exported";

    // Optional Olympia OPK source tree for the Olympia export.
    public string OlympiaSourcePath { get; set; } = "";
}
