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
}
