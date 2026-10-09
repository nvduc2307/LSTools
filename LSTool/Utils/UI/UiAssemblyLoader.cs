using System.IO;
using System.Reflection;

namespace LSTool.Utils.UI
{
    /// <summary>
    /// Makes Fluent assemblies available before WPF reads a window's BAML.
    /// Revit loads LSTool outside its application directory; BAML assembly-name
    /// lookups have no requesting assembly and cannot probe the add-in folder.
    /// </summary>
    internal static class UiAssemblyLoader
    {
        private static readonly object Sync = new object();
        private static bool _initialized;

        public static void Initialize()
        {
            lock (Sync)
            {
                if (_initialized) return;

                var directory = Path.GetDirectoryName(typeof(UiAssemblyLoader).Assembly.Location);
                if (string.IsNullOrEmpty(directory))
                    throw new InvalidOperationException("Cannot locate the LSTool UI library directory.");

                // Load only our UI libraries, leaving Revit's shared assemblies alone.
                Assembly.LoadFrom(Path.Combine(directory, "Wpf.Ui.Abstractions.dll"));
                Assembly.LoadFrom(Path.Combine(directory, "Wpf.Ui.dll"));
                _initialized = true;
            }
        }
    }
}
