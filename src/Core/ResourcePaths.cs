using System;
using System.IO;

namespace IsometricMapViewer
{
    // Resolves the repository's resources/ tree whether the app is launched with
    // `dotnet run` (CWD = project root) or from bin/ (where only the small
    // resources are copied). PAKs are too large to copy to the output, so the
    // resources root is discovered by walking up from CWD and the app base dir.
    public static class ResourcePaths
    {
        private static string? _root;

        public static string Root => _root ??= FindRoot();

        public static string Paks => Path.Combine(Root, "resources", "paks");
        public static string Maps => Path.Combine(Root, "resources", "maps");
        public static string Sprites => Path.Combine(Root, "resources", "sprites");
        public static string Fonts => Path.Combine(Root, "resources", "fonts");

        private static string FindRoot()
        {
            foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                var dir = new DirectoryInfo(start);
                while (dir is not null)
                {
                    if (Directory.Exists(Path.Combine(dir.FullName, "resources", "paks")))
                        return dir.FullName;
                    dir = dir.Parent;
                }
            }

            return Directory.GetCurrentDirectory();
        }
    }
}
