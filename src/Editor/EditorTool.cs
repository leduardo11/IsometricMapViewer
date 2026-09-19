namespace IsometricMapViewer.Editor;

public enum EditorTool
{
    Select,
    GroundBrush,
    ObjectPlacer,
    CollisionPainter,
    Eraser,
    Eyedropper
}

public enum CollisionPaintMode
{
    Blocked,
    Walkable,
    Water,
    Teleport,
    Farm
}
