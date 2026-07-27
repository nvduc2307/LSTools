using System.Windows;
using System.Windows.Controls;

namespace LSTool.Cores.canvas
{
    public class InstanceInCanvasLine : InstanceInCanvas
    {

        public System.Windows.Point P1 { get; set; }
        public System.Windows.Point P2 { get; set; }
        public InstanceInCanvasLine(Canvas Parent, OptionStyleInstanceInCanvas Options, System.Windows.Point p1, System.Windows.Point p2) : base(Parent, Options)
        {
            P1 = p1;
            P2 = p2;
            UIElement = new System.Windows.Shapes.Line()
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
