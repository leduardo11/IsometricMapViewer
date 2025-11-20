using Raylib_cs;
using static Raylib_cs.Raylib;
using IsometricMapViewer;

InitWindow(800, 600, "isometric_map_viewer");
SetTargetFPS(60);

using var game = new MainGame();

while (!WindowShouldClose())
{
	BeginDrawing();
	ClearBackground(Color.Black);
	game.UpdateAndDraw();
	EndDrawing();
}

CloseWindow();
