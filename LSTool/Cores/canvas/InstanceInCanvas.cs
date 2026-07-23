using System.Windows;
using System.Windows.Controls;

namespace LSTool.Cores.canvas
{
    public abstract class InstanceInCanvas
    {
        public Canvas CanvasPageBase { get; }
        public UIElement UIElement { get; set; }
        public OptionStyleInstanceInCanvas Options { get; set; }
        public bool IsSelected { get; set; }
        public InstanceInCanvas(Canvas canvasPageBase, OptionStyleInstanceInCanvas options)
        {
            CanvasPageBase = canvasPageBase;
            Options = options;
        }

        public void DrawInCanvas()
        {
            if (CanvasPageBase != null)
                CanvasPageBase.Children.Add(UIElement);
        }
    }
}
