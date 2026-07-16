using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Shapes;
using Point = System.Windows.Point;
using Line = System.Windows.Shapes.Line;

namespace RIMT.Utils.canvass
{
    public class InstanceInCanvasLine : InstanceInCanvas
    {

        public Point P1 { get; set; }
        public Point P2 { get; set; }
        public InstanceInCanvasLine(CanvasPageBase Parent, OptionStyleInstanceInCanvas Options, Point p1, Point p2) : base(Parent, Options)
        {
            P1 = p1;
            P2 = p2;
            UIElement = new Line()
            {
                X1 = p1.X,
                Y1 = p1.Y,
                X2 = p2.X,
                Y2 = p2.Y,
                StrokeThickness = Options.Thickness,
                StrokeDashArray = Options.LineStyle,
                Stroke = Options.ColorBrush
            };
        }
    }
}
