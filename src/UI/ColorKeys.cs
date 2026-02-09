using Raylib_cs;

namespace IsometricMapViewer.UI;

public static class ColorKeys
{
    // UI Colors - Gothic Dark Theme
    public static readonly Color OverlayDim = new(10, 10, 15, 200);
    
    // Panel Colors
    public static readonly Color PanelBackground = new(25, 25, 30, 220);
    public static readonly Color PanelBorder = new(90, 90, 100, 255);
    public static readonly Color PanelHeaderText = new(180, 180, 190, 255);
    
    // Button Colors
    public static readonly Color ButtonBackground = new(60, 60, 70, 255);
    public static readonly Color ButtonHover = new(80, 80, 90, 255);
    
    // Game Colors
    public static Color DarkStone => new(40, 40, 45, 255);
    public static Color Stone => new(80, 75, 70, 255);
    public static Color LightStone => new(120, 115, 110, 255);
    public static Color Gold => new(200, 180, 100, 255);
    public static Color DarkGold => new(140, 120, 60, 255);
    public static Color Bone => new(200, 190, 170, 255);
    public static Color Shadow => new(30, 30, 35, 255);
    public static Color Blood => new(140, 20, 20, 255);
    public static Color Rust => new(120, 70, 50, 255);
}
