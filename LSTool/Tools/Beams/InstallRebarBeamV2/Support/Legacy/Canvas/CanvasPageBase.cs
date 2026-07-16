using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf = System.Windows;
using Cursors = System.Windows.Input.Cursors;

namespace RIMT.Utils.canvass
{
    public class CanvasPageBase
    {
        public Canvas Parent { get; private set; }
        public double Height { get; set; }
        public double Width { get; set; }
        public double Scale { get; set; }
        public double ScaleX { get; set; }
        public double ScaleY { get; set; }
        public double RatioScale { get; set; }
        public Wpf.Point Center { get; private set; }
        public Vector VTX { get; private set; }
        public Vector VTY { get; private set; }
        public double DistanceCrossScreen { get; private set; }
        public CanvasPageBase(Canvas parent)
        {
            parent.Cursor = Cursors.Hand;
            Parent = parent;
            Width = parent.Width;
            Height = parent.Height;
            DistanceCrossScreen = Math.Sqrt(Width * Width + Height * Height);
            RatioScale = 0.7;
            Scale = 0.1;
            ScaleX = Scale;
            ScaleY = Scale;
            Center = new Wpf.Point(Width / 2, Height / 2);
            VTX = new Vector(1, 0);
            VTY = new Vector(0, 1);
            parent.Background = StyleColorInCanvas.Color2;
        }
        public CanvasPageBase(System.Drawing.Size size)
        {
            Width = size.Width;
            Height = size.Height;
            DistanceCrossScreen = Math.Sqrt(Width * Width + Height * Height);
            RatioScale = 0.7;
            Scale = 0.1;
            ScaleX = Scale;
            ScaleY = Scale;
            Center = new Wpf.Point(Width / 2, Height / 2);
            VTX = new Vector(1, 0);
            VTY = new Vector(0, 1);
        }
        public void GenerateSizeCanvas()
        {
            Width = Parent.ActualWidth;
            Height = Parent.ActualHeight;
            DistanceCrossScreen = Math.Sqrt(Width * Width + Height * Height);
            Center = new Wpf.Point(Width / 2, Height / 2);
        }
    }
}
