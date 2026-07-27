using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Shapes;

namespace LSTool.Cores.canvas
{
    public class InstanceInCanvasPolyline : InstanceInCanvas
    {
        public List<System.Windows.Point> Points { get; set; }
        public InstanceInCanvasPolyline(Canvas canvasPageBase, OptionStyleInstanceInCanvas options, List<System.Windows.Point> points) : base(canvasPageBase, options)
        {
            Points = points;
            var pll = new Polyline();
            foreach (System.Windows.Point p in points)
            {
                pll.Points.Add(p);
            }
            pll.StrokeThickness = Options.Thickness;
            pll.StrokeDashArray = Options.LineStyle;
            pll.Stroke = Options.ColorBrush;

            UIElement = pll;
        }
    }
}
