using System.Windows;
using System.Windows.Controls;
using wd = System.Windows;

namespace LSTool.Cores.canvas
{
    public class InstanceInCanvasCircel : InstanceInCanvas
    {
        public int Id { get; set; }
        public int HostId { get; set; }
        public wd.Point Point { get; set; }
        public double Diameter { get; set; }
        public Action<InstanceInCanvasCircel> ClickAction { get; set; }
        public InstanceInCanvasCircel(Canvas canvasPageBase, OptionStyleInstanceInCanvas options, wd.Point centerBase, double diameter) : base(canvasPageBase, options)
        {
            Diameter = diameter;
            Point = centerBase;
            UIElement = new wd.Shapes.Ellipse()
            {
                Height = diameter,
                Width = diameter,
                StrokeThickness = Options.Thickness,
                StrokeDashArray = Options.LineStyle,
                Stroke = Options.ColorBrush,
                Fill = Options.Fill,
            };
            UIElement.MouseLeftButtonUp += UIElement_MouseLeftButtonUp;
            GenerateUi();
        }

        public void UpdateStatus()
        {
            if (!(UIElement is wd.Shapes.Ellipse el)) return;
            el.Fill = !IsSelected ? Options.Fill : StyleColorInCanvas.Color_Selected1;
            el.Stroke = !IsSelected ? Options.Fill : StyleColorInCanvas.Color_Selected1;
        }
        private void UIElement_MouseLeftButtonUp(object sender, wd.Input.MouseButtonEventArgs e)
        {
            ClickAction?.Invoke(this);
        }
        private void GenerateUi()
        {
            if (!(UIElement is wd.Shapes.Ellipse el)) return;
            el.Cursor = System.Windows.Input.Cursors.Hand;
            el.Fill = Options.Fill;
            var p = new wd.Point(Point.X - Diameter / 2,
                Point.Y - Diameter / 2);
            Canvas.SetLeft(UIElement, p.X);
            Canvas.SetTop(UIElement, p.Y);
        }
    }
}
