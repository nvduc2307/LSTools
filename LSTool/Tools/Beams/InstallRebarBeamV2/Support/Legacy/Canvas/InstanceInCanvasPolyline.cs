using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Shapes;
using Point = System.Windows.Point;

namespace RIMT.Utils.canvass
{
    public class InstanceInCanvasPolyline : InstanceInCanvas
    {
        public List<Point> Points { get; set; }
        public InstanceInCanvasPolyline(CanvasPageBase canvasPageBase, OptionStyleInstanceInCanvas options, List<Point> points) : base(canvasPageBase, options)
        {
            Points = points;
            var pll = new Polyline();
            foreach (Point p in points)
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
