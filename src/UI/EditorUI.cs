using System;
using System.Numerics;
using IsometricMapViewer.Editor;
using IsometricMapViewer.Handlers;
using Raylib_cs;

namespace IsometricMapViewer.UI;

public static class EditorUI
{
    private static readonly Color BgDark = new(20, 22, 28, 235);
    private static readonly Color BgPill = new(40, 44, 55, 220);
    private static readonly Color BgPillActive = new(60, 110, 200, 230);
    private static readonly Color TextWhite = new(240, 240, 245, 255);
    private static readonly Color TextMuted = new(160, 165, 180, 255);
    private static readonly Color BorderDark = new(60, 65, 80, 255);

    public static void Draw(Map map, CameraHandler camera, EditorState state, CommandHistory history, Font font)
    {
        if (!state.ShowUI) return;

        int screenW = Raylib.GetScreenWidth();
        int screenH = Raylib.GetScreenHeight();

        // 1. Top Navigation Bar (Height: 48px)
        Raylib.DrawRectangle(0, 0, screenW, 48, BgDark);
        Raylib.DrawLine(0, 48, screenW, 48, BorderDark);

        int curX = 16;
        int curY = 14;

        // Map Title & Info
        string title = $"{Constants.MapName} ({map.Width}x{map.Height})";
        Raylib.DrawTextEx(font, title, new Vector2(curX, curY), 18, 1, TextWhite);
        curX += (int)Raylib.MeasureTextEx(font, title, 18, 1).X + 24;

        // Cursor Coordinates
        string coordText = state.IsHoverValid ? $"[{state.HoverCellX}, {state.HoverCellY}]" : "[--, --]";
        Raylib.DrawTextEx(font, coordText, new Vector2(curX, curY + 2), 16, 1, Color.Yellow);
        curX += (int)Raylib.MeasureTextEx(font, coordText, 16, 1).X + 20;

        // Zoom level
        string zoomText = $"Zoom: {(int)(camera.Zoom * 100)}%";
        Raylib.DrawTextEx(font, zoomText, new Vector2(curX, curY + 2), 16, 1, TextMuted);
        curX += (int)Raylib.MeasureTextEx(font, zoomText, 16, 1).X + 24;

        // Separator
        Raylib.DrawLine(curX, 8, curX, 40, BorderDark);
        curX += 16;

        // Layer Toggles
        curX = DrawTogglePill(font, "G:Ground", state.ShowGround, curX, 10, () => state.ShowGround = !state.ShowGround);
        curX = DrawTogglePill(font, "S:Shadows", state.ShowShadows, curX, 10, () => state.ShowShadows = !state.ShowShadows);
        curX = DrawTogglePill(font, "O:Objects", state.ShowObjects, curX, 10, () => state.ShowObjects = !state.ShowObjects);
        curX = DrawTogglePill(font, "T:Trees", state.ShowTrees, curX, 10, () => state.ShowTrees = !state.ShowTrees);
        curX = DrawTogglePill(font, "C:Collision", state.ShowCollision, curX, 10, () => state.ShowCollision = !state.ShowCollision);
        curX = DrawTogglePill(font, "#:Grid", state.ShowGrid, curX, 10, () => state.ShowGrid = !state.ShowGrid);

        // Separator
        Raylib.DrawLine(curX, 8, curX, 40, BorderDark);
        curX += 16;

        // Palette Toggle Button
        DrawTogglePill(font, "Palette (Tab)", state.ShowPalette, curX, 10, () => state.ShowPalette = !state.ShowPalette);

        // Undo / Redo indicator (Right-aligned)
        string undoRedoText = $"Undo ({history.UndoCount}) ^Z | Redo ({history.RedoCount}) ^Y";
        float urW = Raylib.MeasureTextEx(font, undoRedoText, 15, 1).X;
        Raylib.DrawTextEx(font, undoRedoText, new Vector2(screenW - urW - 16, curY + 2), 15, 1, TextMuted);

        // 2. Left Tool Dock (Tool selector & brush properties)
        DrawToolDock(font, state, history);

        // 3. Status Toast Notification (Centered bottom)
        if (state.StatusTimer > 0f && !string.IsNullOrEmpty(state.StatusMessage))
        {
            DrawToast(font, state.StatusMessage, screenW, screenH);
        }
    }

    private static void DrawToolDock(Font font, EditorState state, CommandHistory history)
    {
        int dockW = 200;
        int dockH = 280;
        int dockX = 16;
        int dockY = 64;

        Raylib.DrawRectangle(dockX, dockY, dockW, dockH, BgDark);
        Raylib.DrawRectangleLines(dockX, dockY, dockW, dockH, BorderDark);

        int y = dockY + 12;
        Raylib.DrawTextEx(font, "TOOLS [1-5]", new Vector2(dockX + 14, y), 14, 1, TextMuted);
        y += 24;

        y = DrawToolItem(font, "[1] Ground Brush", EditorTool.GroundBrush, state, dockX + 12, y, dockW - 24);
        y = DrawToolItem(font, "[2] Place Object", EditorTool.ObjectPlacer, state, dockX + 12, y, dockW - 24);
        y = DrawToolItem(font, "[3] Paint Collision", EditorTool.CollisionPainter, state, dockX + 12, y, dockW - 24);
        y = DrawToolItem(font, "[4] Eraser", EditorTool.Eraser, state, dockX + 12, y, dockW - 24);
        y = DrawToolItem(font, "[5] Eyedropper", EditorTool.Eyedropper, state, dockX + 12, y, dockW - 24);

        y += 10;
        Raylib.DrawLine(dockX + 12, y, dockX + dockW - 12, y, BorderDark);
        y += 12;

        // Current Brush / Selection info
        if (state.ActiveTool == EditorTool.GroundBrush)
        {
            Raylib.DrawTextEx(font, $"Ground: {state.SelectedGroundSprite}:{state.SelectedGroundFrame}", new Vector2(dockX + 14, y), 14, 1, TextWhite);
        }
        else if (state.ActiveTool == EditorTool.ObjectPlacer)
        {
            Raylib.DrawTextEx(font, $"Object: {state.SelectedObjectSprite}:{state.SelectedObjectFrame}", new Vector2(dockX + 14, y), 14, 1, TextWhite);
        }
        else if (state.ActiveTool == EditorTool.CollisionPainter)
        {
            Raylib.DrawTextEx(font, $"Mode: {state.ActiveCollisionMode}", new Vector2(dockX + 14, y), 14, 1, Color.Orange);
        }

        y += 20;
        Raylib.DrawTextEx(font, $"Brush: {state.BrushSize}x{state.BrushSize} [B]", new Vector2(dockX + 14, y), 14, 1, TextMuted);
    }

    private static int DrawToolItem(Font font, string label, EditorTool tool, EditorState state, int x, int y, int w)
    {
        bool isActive = state.ActiveTool == tool;
        Rectangle rect = new(x, y, w, 28);
        Vector2 mouse = Raylib.GetMousePosition();
        bool hovered = Raylib.CheckCollisionPointRec(mouse, rect);

        if (hovered && Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            state.ActiveTool = tool;
            isActive = true;
        }

        Color bg = isActive ? BgPillActive : (hovered ? new Color(50, 55, 70, 240) : BgPill);
        Raylib.DrawRectangleRec(rect, bg);
        Raylib.DrawTextEx(font, label, new Vector2(x + 10, y + 6), 14, 1, isActive ? Color.White : TextWhite);

        return y + 32;
    }

    private static int DrawTogglePill(Font font, string label, bool isActive, int x, int y, Action onClick)
    {
        float textW = Raylib.MeasureTextEx(font, label, 14, 1).X;
        int pillW = (int)textW + 16;
        int pillH = 28;
        Rectangle rect = new(x, y, pillW, pillH);

        Vector2 mouse = Raylib.GetMousePosition();
        bool hovered = Raylib.CheckCollisionPointRec(mouse, rect);

        if (hovered && Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            onClick();
        }

        Color bg = isActive ? BgPillActive : (hovered ? new Color(55, 60, 75, 230) : BgPill);
        Raylib.DrawRectangleRec(rect, bg);
        Raylib.DrawRectangleLinesEx(rect, 1, BorderDark);
        Raylib.DrawTextEx(font, label, new Vector2(x + 8, y + 6), 14, 1, isActive ? Color.White : TextMuted);

        return x + pillW + 8;
    }

    private static void DrawToast(Font font, string message, int screenW, int screenH)
    {
        float textW = Raylib.MeasureTextEx(font, message, 18, 1).X;
        int boxW = (int)textW + 40;
        int boxH = 44;
        int boxX = (screenW - boxW) / 2;
        int boxY = screenH - boxH - 24;

        Raylib.DrawRectangle(boxX, boxY, boxW, boxH, new Color(25, 30, 40, 245));
        Raylib.DrawRectangleLines(boxX, boxY, boxW, boxH, new Color(80, 140, 255, 200));
        Raylib.DrawTextEx(font, message, new Vector2(boxX + 20, boxY + 12), 18, 1, Color.White);
    }
}
