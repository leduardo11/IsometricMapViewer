using System;
using System.Collections.Generic;

namespace IsometricMapViewer.Editor;

public sealed class EditorState
{
    public EditorTool ActiveTool { get; set; } = EditorTool.GroundBrush;
    public CollisionPaintMode ActiveCollisionMode { get; set; } = CollisionPaintMode.Blocked;

    public short SelectedGroundSprite { get; set; } = 0;
    public short SelectedGroundFrame { get; set; } = 0;

    public short SelectedObjectSprite { get; set; } = 100; // Default to first tree
    public short SelectedObjectFrame { get; set; } = 0;

    public int BrushSize { get; set; } = 1; // 1 = 1x1, 2 = 2x2, 3 = 3x3

    // Available Sprite Lists for Quick Cycling
    public List<short> AvailableGroundSprites { get; } = [];
    public List<short> AvailableObjectSprites { get; } = [];

    // Layer Visibility Toggles
    public bool ShowGround { get; set; } = true;
    public bool ShowShadows { get; set; } = true;
    public bool ShowObjects { get; set; } = true;
    public bool ShowTrees { get; set; } = true;
    public bool ShowCollision { get; set; } = false;
    public bool ShowGrid { get; set; } = false;

    // UI Paneling Toggles
    public bool ShowUI { get; set; } = true;
    public bool ShowPalette { get; set; } = false;
    public bool ShowLegend { get; set; } = true;

    // Hover Cell Coordinate
    public int HoverCellX { get; set; } = -1;
    public int HoverCellY { get; set; } = -1;
    public bool IsHoverValid { get; set; } = false;

    // Toast Status Notification
    public string StatusMessage { get; private set; } = "";
    public float StatusTimer { get; private set; } = 0f;

    public void SetStatus(string message, float durationSeconds = 3.0f)
    {
        StatusMessage = message;
        StatusTimer = durationSeconds;
    }

    public void CycleSprite(int delta)
    {
        if (ActiveTool == EditorTool.GroundBrush && AvailableGroundSprites.Count > 0)
        {
            int idx = AvailableGroundSprites.IndexOf(SelectedGroundSprite);
            if (idx < 0) idx = 0;
            idx = (idx + delta + AvailableGroundSprites.Count) % AvailableGroundSprites.Count;
            SelectedGroundSprite = AvailableGroundSprites[idx];
            SelectedGroundFrame = 0;
            SetStatus($"Ground: #{SelectedGroundSprite}");
        }
        else if (ActiveTool == EditorTool.ObjectPlacer && AvailableObjectSprites.Count > 0)
        {
            int idx = AvailableObjectSprites.IndexOf(SelectedObjectSprite);
            if (idx < 0) idx = 0;
            idx = (idx + delta + AvailableObjectSprites.Count) % AvailableObjectSprites.Count;
            SelectedObjectSprite = AvailableObjectSprites[idx];
            SelectedObjectFrame = 0;
            SetStatus($"Object: #{SelectedObjectSprite}");
        }
    }

    public void CycleFrame(int delta, int frameCount)
    {
        if (frameCount <= 0) frameCount = 1;

        if (ActiveTool == EditorTool.GroundBrush)
        {
            SelectedGroundFrame = (short)((SelectedGroundFrame + delta + frameCount) % frameCount);
            SetStatus($"Ground Frame: {SelectedGroundFrame} / {frameCount - 1}");
        }
        else if (ActiveTool == EditorTool.ObjectPlacer)
        {
            SelectedObjectFrame = (short)((SelectedObjectFrame + delta + frameCount) % frameCount);
            SetStatus($"Object Frame: {SelectedObjectFrame} / {frameCount - 1}");
        }
    }

    public void Update(float dt)
    {
        if (StatusTimer > 0f)
        {
            StatusTimer -= dt;
            if (StatusTimer <= 0f)
            {
                StatusMessage = "";
                StatusTimer = 0f;
            }
        }
    }
}
