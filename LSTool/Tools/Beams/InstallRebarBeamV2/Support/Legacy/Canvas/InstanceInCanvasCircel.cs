using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using Wpf = System.Windows;
using Ellipse = System.Windows.Shapes.Ellipse;

namespace RIMT.Utils.canvass
{
    public class InstanceInCanvasCircel : InstanceInCanvas
    {
        public Wpf.Point VectorInit { get; set; }
        public Wpf.Point Point { get; set; }
        public double Diameter { get; set; }
        public TextBlock Title { get; set; }
        public InstanceInCanvasCircel(CanvasPageBase canvasPageBase, OptionStyleInstanceInCanvas options, Wpf.Point centerBase, double diameter, Wpf.Point point, Wpf.Point vectorInit, string title) : base(canvasPageBase, options)
        {
            Diameter = diameter;
            Title = new TextBlock();
            Title.Text = title;
            canvasPageBase.Parent.Children.Add(Title);
            Point = point;
            VectorInit = vectorInit;
            UIElement = new Ellipse()
            {
                Height = diameter,
                Width = diameter,
                StrokeThickness = Options.Thickness,
                StrokeDashArray = Options.LineStyle,
                Stroke = Options.ColorBrush,
                Fill = Options.Fill,
            };
            GenerateUi();
        }
        private void GenerateUi()
        {
            if (UIElement is Wpf.Shapes.Ellipse el)
            {
                el.Cursor = System.Windows.Input.Cursors.Hand;
                el.Fill = Options.Fill;
            };
            var p = new Wpf.Point(Point.X - Diameter / 2 - VectorInit.X * Diameter / 2,
                Point.Y - Diameter / 2 - VectorInit.Y * Diameter / 2);
            Canvas.SetLeft(UIElement, p.X);
            Canvas.SetTop(UIElement, p.Y);

            Canvas.SetLeft(Title, p.X + 4);
            Canvas.SetTop(Title, p.Y - 1);
        }
    }
}
