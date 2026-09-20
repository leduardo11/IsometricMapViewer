using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using IsometricMapViewer.Editor;
using IsometricMapViewer.Loaders;
using Raylib_cs;

namespace IsometricMapViewer.UI;

public class PaletteUI
{
    private static readonly Color BgPanel = new(24, 26, 32, 245);
    private static readonly Color BorderPanel = new(60, 65, 80, 255);
    private static readonly Color BgTabActive = new(60, 110, 200, 240);
    private static readonly Color BgTab = new(40, 44, 55, 220);
    private static readonly Color CellBg = new(35, 38, 48, 255);
    private static readonly Color CellHover = new(80, 120, 200, 180);
    private static readonly Color CellSelected = new(255, 215, 0, 220);

    private int _tab = 0; // 0 = Ground, 1 = Objects
    private int _groundPage = 0;
    private int _objectPage = 0;
    private const int ItemsPerPage = 24;

    private readonly List<Sprite> _groundSprites = [];
    private readonly List<Sprite> _objectSprites = [];

    public PaletteUI(SpriteLoader spriteLoader)
    {
        PopulateSprites(spriteLoader);
    }

    private void PopulateSprites(SpriteLoader spriteLoader)
    {
        var all = spriteLoader.GetAllSprites().ToList();

        // Categorize into Ground (terrain tiles) vs Objects (structures, trees,
        // placeable objects), matching the PAK catalog's tiles/ vs objects/ split.
        foreach (var s in all)
        {
            if (s.Texture.Id == 0) continue;

            // Structures (50..69), trees (100..145) and objects (200..248)
            if ((s.Index >= 50 && s.Index <= 69) ||
                (s.Index >= 100 && s.Index <= 145) ||
                (s.Index >= 200 && s.Index <= 248))
            {
                _objectSprites.Add(s);
            }
            else if (s.Index < 150 || s.Index > 195) // exclude raw shadow sprite pack from palette selection
            {
                _groundSprites.Add(s);
            }
        }
    }

    public void Draw(EditorState state, SpriteLoader spriteLoader, Font font)
    {
        if (!state.ShowPalette) return;

        int screenW = Raylib.GetScreenWidth();
        int screenH = Raylib.GetScreenHeight();

        int panelW = 340;
        int panelX = screenW - panelW;
        int panelY = 48;
        int panelH = screenH - panelY;

        // Panel background
        Raylib.DrawRectangle(panelX, panelY, panelW, panelH, BgPanel);
        Raylib.DrawLine(panelX, panelY, panelX, screenH, BorderPanel);

        // Header Tabs: [Ground Tiles] [Objects & Trees] [X]
        int tabW = 130;
        int tabH = 32;
        int tabY = panelY + 8;

        // Ground Tab
        Rectangle groundTabRect = new(panelX + 12, tabY, tabW, tabH);
        DrawTab(font, "Ground Tiles", _tab == 0, groundTabRect, () => _tab = 0);

        // Objects Tab
        Rectangle objTabRect = new(panelX + 12 + tabW + 6, tabY, tabW, tabH);
        DrawTab(font, "Objects/Trees", _tab == 1, objTabRect, () => _tab = 1);

        // Close button
        Rectangle closeRect = new(panelX + panelW - 36, tabY + 2, 26, 26);
        Vector2 mouse = Raylib.GetMousePosition();
        bool closeHover = Raylib.CheckCollisionPointRec(mouse, closeRect);
        if (closeHover && Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            state.ShowPalette = false;
            return;
        }
        Raylib.DrawRectangleRec(closeRect, closeHover ? Color.Red : new Color(60, 60, 70, 200));
        Raylib.DrawTextEx(font, "X", new Vector2(closeRect.X + 8, closeRect.Y + 4), 16, 1, Color.White);

        // Grid of Sprites
        var currentList = _tab == 0 ? _groundSprites : _objectSprites;
        int page = _tab == 0 ? _groundPage : _objectPage;
        int maxPage = Math.Max(0, (currentList.Count - 1) / ItemsPerPage);
        if (page > maxPage) page = maxPage;

        int gridX = panelX + 16;
        int gridY = tabY + tabH + 16;
        int cols = 4;
        int itemSize = 68;
        int spacing = 10;

        int startIndex = page * ItemsPerPage;
        int endIndex = Math.Min(startIndex + ItemsPerPage, currentList.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            int indexOnPage = i - startIndex;
            int col = indexOnPage % cols;
            int row = indexOnPage / cols;

            int ix = gridX + col * (itemSize + spacing);
            int iy = gridY + row * (itemSize + spacing);

            var sprite = currentList[i];
            bool isSelected = (_tab == 0 && state.SelectedGroundSprite == sprite.Index) ||
                             (_tab == 1 && state.SelectedObjectSprite == sprite.Index);

            Rectangle itemRect = new(ix, iy, itemSize, itemSize);
            bool isHovered = Raylib.CheckCollisionPointRec(mouse, itemRect);

            if (isHovered && Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                if (_tab == 0)
                {
                    state.SelectedGroundSprite = (short)sprite.Index;
                    state.SelectedGroundFrame = 0;
                    state.ActiveTool = EditorTool.GroundBrush;
                    state.SetStatus($"Selected Ground #{sprite.Index}");
                }
                else
                {
                    state.SelectedObjectSprite = (short)sprite.Index;
                    state.SelectedObjectFrame = 0;
                    state.ActiveTool = EditorTool.ObjectPlacer;
                    state.SetStatus($"Selected Object #{sprite.Index}");
                }
            }

            // Draw cell background
            Color cellColor = isSelected ? CellSelected : (isHovered ? CellHover : CellBg);
            Raylib.DrawRectangleRec(itemRect, cellColor);
            Raylib.DrawRectangleLinesEx(itemRect, 1, isSelected ? Color.Gold : BorderPanel);

            // Draw sprite preview centered
            if (sprite.Texture.Id != 0 && sprite.Frames.Count > 0)
            {
                var fr = sprite.Frames[0];
                Rectangle src = new(fr.Left, fr.Top, fr.Width, fr.Height);
                float maxDim = Math.Max(fr.Width, fr.Height);
                float scale = maxDim > (itemSize - 18) ? (itemSize - 18) / maxDim : 1f;
                float destW = fr.Width * scale;
                float destH = fr.Height * scale;
                float destX = ix + (itemSize - destW) / 2f;
                float destY = iy + 4 + (itemSize - 22 - destH) / 2f;

                Raylib.DrawTexturePro(sprite.Texture, src, new Rectangle(destX, destY, destW, destH), Vector2.Zero, 0f, Color.White);
            }

            // Draw Sprite ID label at bottom of cell
            string idLabel = $"#{sprite.Index}";
            float lblW = Raylib.MeasureTextEx(font, idLabel, 11, 1).X;
            Raylib.DrawTextEx(font, idLabel, new Vector2(ix + (itemSize - lblW) / 2f, iy + itemSize - 14), 11, 1, isSelected ? Color.Black : Color.LightGray);
        }

        // Pagination Bar at Bottom
        int pagY = panelY + panelH - 44;
        Raylib.DrawLine(panelX, pagY - 8, panelX + panelW, pagY - 8, BorderPanel);

        string pageText = $"Page {page + 1} / {maxPage + 1} ({currentList.Count} items)";
        Raylib.DrawTextEx(font, pageText, new Vector2(panelX + 16, pagY + 6), 13, 1, Color.Gray);

        Rectangle prevRect = new(panelX + panelW - 96, pagY + 2, 38, 26);
        Rectangle nextRect = new(panelX + panelW - 50, pagY + 2, 38, 26);

        if (DrawButton(font, "<", prevRect, page > 0))
        {
            if (_tab == 0) _groundPage = Math.Max(0, _groundPage - 1);
            else _objectPage = Math.Max(0, _objectPage - 1);
        }

        if (DrawButton(font, ">", nextRect, page < maxPage))
        {
            if (_tab == 0) _groundPage = Math.Min(maxPage, _groundPage + 1);
            else _objectPage = Math.Min(maxPage, _objectPage + 1);
        }
    }

    private static void DrawTab(Font font, string text, bool active, Rectangle rect, Action onClick)
    {
        Vector2 mouse = Raylib.GetMousePosition();
        bool hovered = Raylib.CheckCollisionPointRec(mouse, rect);

        if (hovered && Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            onClick();
        }

        Raylib.DrawRectangleRec(rect, active ? BgTabActive : (hovered ? new Color(50, 55, 68, 230) : BgTab));
        Raylib.DrawRectangleLinesEx(rect, 1, BorderPanel);
        float tw = Raylib.MeasureTextEx(font, text, 14, 1).X;
        Raylib.DrawTextEx(font, text, new Vector2(rect.X + (rect.Width - tw) / 2f, rect.Y + 8), 14, 1, active ? Color.White : Color.LightGray);
    }

    private static bool DrawButton(Font font, string text, Rectangle rect, bool enabled)
    {
        Vector2 mouse = Raylib.GetMousePosition();
        bool hovered = enabled && Raylib.CheckCollisionPointRec(mouse, rect);

        Color bg = !enabled ? new Color(30, 32, 40, 180) : (hovered ? new Color(70, 120, 220, 240) : new Color(50, 55, 70, 220));
        Raylib.DrawRectangleRec(rect, bg);
        Raylib.DrawRectangleLinesEx(rect, 1, BorderPanel);

        float tw = Raylib.MeasureTextEx(font, text, 14, 1).X;
        Raylib.DrawTextEx(font, text, new Vector2(rect.X + (rect.Width - tw) / 2f, rect.Y + 5), 14, 1, enabled ? Color.White : Color.DarkGray);

        return hovered && Raylib.IsMouseButtonPressed(MouseButton.Left);
    }
}
