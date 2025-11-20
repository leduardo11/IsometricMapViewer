using System;

namespace IsometricMapViewer
{
    public static class ConsoleLogger
    {
        private static bool _isVerbose = true;
        public static bool IsVerbose { get => _isVerbose; set => _isVerbose = value; }
        public static void LogInfo(string message) => Console.WriteLine($"[INFO] 🟢 {message}");
        public static void LogWarning(string message) => Console.WriteLine($"[WARN] 🟠 {message}");
        public static void LogError(string message) => Console.WriteLine($"[ERR] 🔴 {message}");
        public static void LogVerbose(string message) { if (_isVerbose) Console.WriteLine($"[VERB] ⚪ {message}"); }
    }
}
