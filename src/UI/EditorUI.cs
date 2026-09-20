using System;
using System.Numerics;
using IsometricMapViewer.Editor;
using IsometricMapViewer.Handlers;
using Raylib_cs;

namespace IsometricMapViewer.UI;

public static class EditorUI
{
    private static readonly Color BgDark = new(20, 22, 28, 240);
    private static readonly Color BgPill = new(40, 44, 55, 220);
    private static readonly Color BgPillActive = new(60, 110, 200, 230);
    private static readonly Color TextWhite = new(240, 240, 245, 255);
    private static readonly Color TextMuted = new(160, 165, 180, 255);
    private static readonly Color BorderDark = new(60, 65, 80, 255);
    private static readonly Color AccentGold = new(255, 205, 90, 255);
    private static readonly Color AccentCyan = new(100, 210, 255, 255);

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

        // Left Section: Map Title & Info
        string title = $"{Constants.MapName} ({map.Width}x{map.Height})";
        Raylib.DrawTextEx(font, title, new Vector2(curX, curY), 18, 1, TextWhite);
        curX += (int)Raylib.MeasureTextEx(font, title, 18, 1).X + 20;

        // Cursor Coordinates
        string coordText = state.IsHoverValid ? $"[{state.HoverCellX}, {state.HoverCellY}]" : "[--, --]";
        Raylib.DrawTextEx(font, coordText, new Vector2(curX, curY + 2), 16, 1, AccentGold);
        curX += (int)Raylib.MeasureTextEx(font, coordText, 16, 1).X + 16;

        // Zoom level
        string zoomText = $"Zoom: {(int)(camera.Zoom * 100)}%";
        Raylib.DrawTextEx(font, zoomText, new Vector2(curX, curY + 2), 16, 1, AccentCyan);
        curX += (int)Raylib.MeasureTextEx(font, zoomText, 16, 1).X + 20;

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

        // Palette and Legend Toggle Buttons
        curX = DrawTogglePill(font, "Palette [Tab]", state.ShowPalette, curX, 10, () => state.ShowPalette = !state.ShowPalette);
        curX = DrawTogglePill(font, "Legend [H]", state.ShowLegend, curX, 10, () => state.ShowLegend = !state.ShowLegend);

        // Right Section: Undo / Redo indicator
        string undoRedoText = $"Undo ({history.UndoCount}) ^Z | Redo ({history.RedoCount}) ^Y";
        float urW = Raylib.MeasureTextEx(font, undoRedoText, 15, 1).X;
        int urX = screenW - (int)urW - 20;
        if (urX > curX + 10)
        {
            Raylib.DrawTextEx(font, undoRedoText, new Vector2(urX, curY + 2), 15, 1, TextMuted);
        }

        // 2. Left Tool Dock
        DrawToolDock(font, state, history);

        // 3. Left Legend Dock (Shortcuts & Options Cheat-Sheet)
        if (state.ShowLegend)
        {
            DrawLegendDock(font, state);
        }

        // 4. Status Toast Notification (Centered bottom)
        if (state.StatusTimer > 0f && !string.IsNullOrEmpty(state.StatusMessage))
        {
            DrawToast(font, state.StatusMessage, screenW, screenH);
        }
    }

    private static void DrawToolDock(Font font, EditorState state, CommandHistory history)
    {
        int dockW = 240;
        int dockH = 260;
        int dockX = 16;
        int dockY = 60;

        Raylib.DrawRectangle(dockX, dockY, dockW, dockH, BgDark);
        Raylib.DrawRectangleLines(dockX, dockY, dockW, dockH, BorderDark);

        int y = dockY + 10;
        Raylib.DrawTextEx(font, "TOOLS [1 - 5]", new Vector2(dockX + 14, y), 14, 1, AccentCyan);
        y += 22;

        y = DrawToolItem(font, "[1] Ground Brush", EditorTool.GroundBrush, state, dockX + 12, y, dockW - 24);
        y = DrawToolItem(font, "[2] Place Object", EditorTool.ObjectPlacer, state, dockX + 12, y, dockW - 24);
        y = DrawToolItem(font, "[3] Paint Collision", EditorTool.CollisionPainter, state, dockX + 12, y, dockW - 24);
        y = DrawToolItem(font, "[4] Eraser", EditorTool.Eraser, state, dockX + 12, y, dockW - 24);
        y = DrawToolItem(font, "[5] Eyedropper", EditorTool.Eyedropper, state, dockX + 12, y, dockW - 24);

        y += 6;
        Raylib.DrawLine(dockX + 12, y, dockX + dockW - 12, y, BorderDark);
        y += 10;

        // Current Brush / Selection info
        if (state.ActiveTool == EditorTool.GroundBrush)
        {
            Raylib.DrawTextEx(font, $"Ground: #{state.SelectedGroundSprite} (F:{state.SelectedGroundFrame})", new Vector2(dockX + 14, y), 14, 1, TextWhite);
        }
        else if (state.ActiveTool == EditorTool.ObjectPlacer)
        {
            Raylib.DrawTextEx(font, $"Object: #{state.SelectedObjectSprite} (F:{state.SelectedObjectFrame})", new Vector2(dockX + 14, y), 14, 1, TextWhite);
        }
        else if (state.ActiveTool == EditorTool.CollisionPainter)
        {
            Color col = state.ActiveCollisionMode switch
            {
                CollisionPaintMode.Blocked => Color.Red,
                CollisionPaintMode.Water => Color.SkyBlue,
                CollisionPaintMode.Walkable => Color.Green,
                CollisionPaintMode.Teleport => Color.Magenta,
                CollisionPaintMode.Farm => Color.Lime,
                _ => Color.White
            };
            Raylib.DrawTextEx(font, $"Collision: {state.ActiveCollisionMode}", new Vector2(dockX + 14, y), 14, 1, col);
        }

        y += 20;
        Raylib.DrawTextEx(font, $"Brush: {state.BrushSize}x{state.BrushSize} [B to cycle]", new Vector2(dockX + 14, y), 13, 1, AccentGold);
    }

    private static void DrawLegendDock(Font font, EditorState state)
    {
        int dockW = 240;
        int dockH = 520;
        int dockX = 16;
        int dockY = 328;

        Raylib.DrawRectangle(dockX, dockY, dockW, dockH, BgDark);
        Raylib.DrawRectangleLines(dockX, dockY, dockW, dockH, BorderDark);

        int y = dockY + 10;
        Raylib.DrawTextEx(font, "CONTROLS & SHORTCUTS [H]", new Vector2(dockX + 14, y), 14, 1, AccentGold);
        y += 22;

        DrawLegendLine(font, "[ / ]", "Cycle Sprite (PgUp/Dn)", dockX + 14, y); y += 22;
        DrawLegendLine(font, ", / .", "Cycle Frame (< / >)", dockX + 14, y); y += 22;
        DrawLegendLine(font, "B", "Cycle Brush (1x1..3x3)", dockX + 14, y); y += 22;
        DrawLegendLine(font, "Tab", "Toggle Sprite Palette", dockX + 14, y); y += 22;

        y += 4;
        Raylib.DrawLine(dockX + 12, y, dockX + dockW - 12, y, BorderDark);
        y += 8;

        Raylib.DrawTextEx(font, "COLLISION KEYS", new Vector2(dockX + 14, y), 12, 1, AccentCyan);
        y += 18;
        DrawLegendLine(font, "M / K", "Blocked / Walkable", dockX + 14, y); y += 20;
        DrawLegendLine(font, "W / L", "Water / Teleport", dockX + 14, y); y += 20;
        DrawLegendLine(font, "R", "Farming Zone", dockX + 14, y); y += 20;

        y += 4;
        Raylib.DrawLine(dockX + 12, y, dockX + dockW - 12, y, BorderDark);
        y += 8;

        Raylib.DrawTextEx(font, "PROJECT & CAMERA", new Vector2(dockX + 14, y), 12, 1, AccentCyan);
        y += 18;
        DrawLegendLine(font, "^S", "Save Map (.amd)", dockX + 14, y); y += 20;
        DrawLegendLine(font, "+ / -", "Zoom In / Out", dockX + 14, y); y += 20;
        DrawLegendLine(font, "^Z / ^Y", "Undo / Redo", dockX + 14, y); y += 20;
        DrawLegendLine(font, "R-Drag", "Pan  | Wheel: Zoom", dockX + 14, y); y += 20;

        y += 4;
        Raylib.DrawLine(dockX + 12, y, dockX + dockW - 12, y, BorderDark);
        y += 8;

        Raylib.DrawTextEx(font, "EXPORT (ATLASPACKER)", new Vector2(dockX + 14, y), 12, 1, AccentGold);
        y += 18;
        DrawLegendLine(font, "^1", "Package   ^2 RPG", dockX + 14, y); y += 20;
        DrawLegendLine(font, "^3", "Godot     ^4 Tiled", dockX + 14, y); y += 20;
        DrawLegendLine(font, "^5", "Map Shot  ^6 Master", dockX + 14, y); y += 20;
        DrawLegendLine(font, "^7", "Olympia   ^0 Export All", dockX + 14, y);
    }

    private static void DrawLegendLine(Font font, string badge, string desc, int x, int y)
    {
        Raylib.DrawTextEx(font, badge, new Vector2(x, y), 13, 1, AccentGold);
        int badgeW = (int)Raylib.MeasureTextEx(font, badge, 13, 1).X;
        Raylib.DrawTextEx(font, desc, new Vector2(x + Math.Max(badgeW + 8, 56), y), 13, 1, TextWhite);
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
