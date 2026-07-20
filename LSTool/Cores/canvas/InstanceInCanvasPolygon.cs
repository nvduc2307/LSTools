using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace LSTool.Cores.canvas
{
    public class InstanceInCanvasPolygon : InstanceInCanvas
    {
        public List<System.Windows.Point> Points { get; set; }
        public InstanceInCanvasPolygon(Canvas canvasPageBase, OptionStyleInstanceInCanvas options, List<System.Windows.Point> points) : base(canvasPageBase, options)
        {
            Points = points;
            var plg = new Polygon();
            foreach (System.Windows.Point p in points)
            {
                plg.Points.Add(p);
            }
            plg.StrokeThickness = Options.Thickness;
            plg.StrokeDashArray = Options.LineStyle;
            plg.Stroke = Options.ColorBrush;

            if (options.Fill != null) plg.Fill = options.Fill;

            UIElement = plg;

        }
    }
}
