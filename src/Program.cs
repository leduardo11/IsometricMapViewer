using Raylib_cs;
using static Raylib_cs.Raylib;

InitWindow(800, 600, "isometric_map_viewer");
SetTargetFPS(60);

while (!WindowShouldClose())
{
	BeginDrawing();
	ClearBackground(Color.Black);
	EndDrawing();
}

CloseWindow();
