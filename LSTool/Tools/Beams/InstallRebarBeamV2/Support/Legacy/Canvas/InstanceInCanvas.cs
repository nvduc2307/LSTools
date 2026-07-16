using System.Windows;

namespace RIMT.Utils.canvass
{
    public abstract class InstanceInCanvas
    {
        public CanvasPageBase CanvasPageBase { get; }
        public UIElement UIElement { get; set; }
        public OptionStyleInstanceInCanvas Options { get; set; }
        public InstanceInCanvas(CanvasPageBase canvasPageBase, OptionStyleInstanceInCanvas options)
        {
            CanvasPageBase = canvasPageBase;
            Options = options;
        }

        public void DrawInCanvas()
        {
            if (CanvasPageBase != null)
                CanvasPageBase.Parent.Children.Add(UIElement);
        }
    }
}
