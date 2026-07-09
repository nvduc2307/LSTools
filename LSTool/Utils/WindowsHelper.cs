using System.Windows;

namespace LSTool.Utils
{
    public static class WindowsHelper
    {
        public static void Escape(this Window window)
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            new System.Windows.Interop.WindowInteropHelper(window) { Owner = Autodesk.Windows.ComponentManager.ApplicationWindow };
            window.PreviewKeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Escape) window.Close(); };
        }
    }
}
