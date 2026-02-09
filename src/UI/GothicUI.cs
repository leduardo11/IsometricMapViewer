using System.Numerics;
using Raylib_cs;

namespace IsometricMapViewer.UI;

public static class GothicUI
{
    public static void DrawFullscreenOverlay(Color color)
    {
        Raylib.DrawRectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), color);
    }

    public static void Panel(Rectangle bounds, string title = "")
    {
        Raylib.DrawRectangleRec(bounds, ColorKeys.DarkStone);
        Raylib.DrawRectangleLinesEx(bounds, UIConfig.BORDER, ColorKeys.Stone);
        Raylib.DrawRectangleLinesEx(
            new Rectangle(bounds.X + 1, bounds.Y + 1, bounds.Width - 2, bounds.Height - 2),
            1, ColorKeys.Shadow);

        if (!string.IsNullOrEmpty(title))
        {
            int titleWidth = Raylib.MeasureText(title, UIConfig.FONT_XL);
            int titleX = (int)(bounds.X + (bounds.Width - titleWidth) / 2);
            int titleY = (int)(bounds.Y + UIConfig.PADDING);
            
            Raylib.DrawText(title, titleX + 1, titleY + 1, UIConfig.FONT_XL, ColorKeys.Shadow);
            Raylib.DrawText(title, titleX, titleY, UIConfig.FONT_XL, ColorKeys.Gold);
        }
    }

    public static void Label(int x, int y, string text, Color? color = null)
    {
        var textColor = color ?? ColorKeys.Bone;
        Raylib.DrawText(text, x + 1, y + 1, UIConfig.FONT_LARGE, ColorKeys.Shadow);
        Raylib.DrawText(text, x, y, UIConfig.FONT_LARGE, textColor);
    }

    public static bool Button(Rectangle bounds, string text)
    {
        var mouse = Raylib.GetMousePosition();
        bool hovered = Raylib.CheckCollisionPointRec(mouse, bounds);
        bool clicked = hovered && Raylib.IsMouseButtonPressed(MouseButton.Left);

        var bgColor = hovered ? ColorKeys.Stone : ColorKeys.DarkStone;
        var borderColor = hovered ? ColorKeys.Gold : ColorKeys.Bone;

        Raylib.DrawRectangleRec(bounds, bgColor);
        Raylib.DrawRectangleLinesEx(bounds, UIConfig.BORDER, borderColor);

        int textWidth = Raylib.MeasureText(text, UIConfig.FONT_LARGE);
        int textX = (int)(bounds.X + (bounds.Width - textWidth) / 2);
        int textY = (int)(bounds.Y + (bounds.Height - UIConfig.FONT_LARGE) / 2);

        Raylib.DrawText(text, textX + 1, textY + 1, UIConfig.FONT_LARGE, ColorKeys.Shadow);
        Raylib.DrawText(text, textX, textY, UIConfig.FONT_LARGE, ColorKeys.Bone);

        return clicked;
    }

    public static Rectangle CenteredPanel(float width, float height) =>
        new((Raylib.GetScreenWidth() - width) / 2, 
            (Raylib.GetScreenHeight() - height) / 2, 
            width, height);

    public static void ProgressBar(int x, int y, int width, int height, float progress, string label)
    {
        Raylib.DrawRectangle(x, y, width, height, ColorKeys.DarkStone);
        Raylib.DrawRectangle(x + 2, y + 2, (int)((width - 4) * progress), height - 4, ColorKeys.Gold);
        Raylib.DrawRectangleLinesEx(new Rectangle(x, y, width, height), 1, ColorKeys.Stone);

        if (!string.IsNullOrEmpty(label))
        {
            int textWidth = Raylib.MeasureText(label, UIConfig.FONT_SMALL);
            int textX = x + (width - textWidth) / 2;
            int textY = y + (height - UIConfig.FONT_SMALL) / 2;
            
            Raylib.DrawText(label, textX + 1, textY + 1, UIConfig.FONT_SMALL, ColorKeys.Shadow);
            Raylib.DrawText(label, textX, textY, UIConfig.FONT_SMALL, ColorKeys.Bone);
        }
    }
}
