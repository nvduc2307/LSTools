using System.Runtime.InteropServices;
using Point = System.Windows.Point;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
namespace LSTool.Utils.UI;
/// <summary>Fits compact dialogs to the current monitor in device-independent units.</summary>
public static class WindowLayout
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(WindowLayout), new PropertyMetadata(false, OnEnabledChanged));
    public static void SetEnabled(DependencyObject target, bool value) => target.SetValue(EnabledProperty, value);
    public static bool GetEnabled(DependencyObject target) => (bool)target.GetValue(EnabledProperty);
    private static void OnEnabledChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is not Window window) return;
        if ((bool)args.NewValue)
        {
            window.SourceInitialized += OnSourceInitialized;
            window.PreviewKeyDown += OnPreviewKeyDown;
        }
        else
        {
            window.SourceInitialized -= OnSourceInitialized;
            window.PreviewKeyDown -= OnPreviewKeyDown;
        }
    }
    private static void OnPreviewKeyDown(object? sender, System.Windows.Input.KeyEventArgs args)
    {
        if (args.Key != System.Windows.Input.Key.Escape || sender is not Window window) return;
        args.Handled = true;
        window.Close();
    }
    private static void OnSourceInitialized(object? sender, EventArgs args)
    {
        if (sender is not Window window) return;
        FitToMonitor(window, true);
        var source = (HwndSource)PresentationSource.FromVisual(window);
        source?.AddHook((IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled) =>
        {
            if (message == 0x02E0) // Let WPF apply WM_DPICHANGED before measuring in DIPs.
                window.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => FitToMonitor(window, false)));
            return IntPtr.Zero;
        });
    }
    private static void FitToMonitor(Window window, bool center)
    {
        var helper = new WindowInteropHelper(window);
        var handle = center ? (helper.Owner != IntPtr.Zero ? helper.Owner : GetForegroundWindow()) : helper.Handle;
        var info = new MonitorInfo { Size = Marshal.SizeOf(typeof(MonitorInfo)) };
        if (!GetMonitorInfo(MonitorFromWindow(handle, 2), ref info)) return;
        var transform = PresentationSource.FromVisual(window)?.CompositionTarget?.TransformFromDevice ?? System.Windows.Media.Matrix.Identity;
        var topLeft = transform.Transform(new Point(info.Work.Left, info.Work.Top));
        var bottomRight = transform.Transform(new Point(info.Work.Right, info.Work.Bottom));
        var width = Math.Max(1, bottomRight.X - topLeft.X - 24);
        var height = Math.Max(1, bottomRight.Y - topLeft.Y - 24);
        window.MinWidth = Math.Min(window.MinWidth, width);
        window.MinHeight = Math.Min(window.MinHeight, height);
        window.MaxWidth = width;
        window.MaxHeight = height;
        window.Width = Math.Min(window.Width, width);
        window.Height = Math.Min(window.Height, height);
        if (center)
        {
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = topLeft.X + (bottomRight.X - topLeft.X - window.Width) / 2;
            window.Top = topLeft.Y + (bottomRight.Y - topLeft.Y - window.Height) / 2;
        }
        else
        {
            window.Left = Math.Max(topLeft.X, Math.Min(window.Left, bottomRight.X - window.Width));
            window.Top = Math.Max(topLeft.Y, Math.Min(window.Top, bottomRight.Y - window.Height));
        }
    }
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public Rect Monitor, Work; public uint Flags; }
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr handle, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
}
