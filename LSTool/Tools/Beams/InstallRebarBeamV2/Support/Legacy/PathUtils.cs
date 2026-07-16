using System.IO;

namespace RIMT.Utils.Paths
{
    public static class PathUtils
    {
        private static readonly object MigrationLock = new object();
        private static bool _presetsMigrated;

        public static string AppDataRimT { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LSTool");

        public static string AssemblyDirectory { get; } = AppContext.BaseDirectory;

        public static string PathData { get; } = Path.Combine(AppDataRimT, "Data") + Path.DirectorySeparatorChar;

        public static void MigrateCreateRebarBeamPresets()
        {
            lock (MigrationLock)
            {
                if (_presetsMigrated) return;
                _presetsMigrated = true;

                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var sourceRoot = Path.Combine(appData, "RIMT", "CreateRebarBeam");
                var targetRoot = Path.Combine(AppDataRimT, "CreateRebarBeam");
                if (!Directory.Exists(sourceRoot)) return;

                try
                {
                    foreach (var sourceFile in Directory.EnumerateFiles(
                                 sourceRoot,
                                 "*",
                                 SearchOption.AllDirectories))
                    {
                        var relativePath = sourceFile
                            .Substring(sourceRoot.Length)
                            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        var targetFile = Path.Combine(targetRoot, relativePath);
                        if (File.Exists(targetFile)) continue;
                        var targetDirectory = Path.GetDirectoryName(targetFile);
                        if (!string.IsNullOrEmpty(targetDirectory)) Directory.CreateDirectory(targetDirectory);
                        File.Copy(sourceFile, targetFile, false);
                    }
                }
                catch
                {
                    // Preset migration is best-effort; a read-only profile must not block the command.
                }
            }
        }
    }
}
