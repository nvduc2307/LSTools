using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace RIMT.Utils.canvass
{
    public static class CanvasZoomPan
    {
        private static readonly MatrixTransform _transform = new MatrixTransform();

        private static Point _initialMousePosition;

        private static float Zoomfactor = 1.1f;

        public static void ActiveZoomPan(this Canvas canvas)
        {
            canvas.MouseDown += PanAndZoomCanvas_MouseDown;
            canvas.MouseMove += PanAndZoomCanvas_MouseMove;
            canvas.MouseWheel += PanAndZoomCanvas_MouseWheel;
        }

        private static void PanAndZoomCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                _initialMousePosition = _transform.Inverse.Transform(e.GetPosition(sender as Canvas));
            }
        }

        private static void PanAndZoomCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.MiddleButton != MouseButtonState.Pressed)
            {
                return;
            }

            Point point = _transform.Inverse.Transform(e.GetPosition(sender as Canvas));
            Vector vector = Point.Subtract(point, _initialMousePosition);
            TranslateTransform translateTransform = new TranslateTransform(vector.X, vector.Y);
            _transform.Matrix = translateTransform.Value * _transform.Matrix;
            foreach (UIElement child in (sender as Canvas).Children)
            {
                child.RenderTransform = _transform;
            }
        }

        private static void PanAndZoomCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            float num = Zoomfactor;
            if (e.Delta < 0)
            {
                num = 1f / num;
            }

            Point position = e.GetPosition(sender as Canvas);
            Matrix matrix = _transform.Matrix;
            matrix.ScaleAt(num, num, position.X, position.Y);
            _transform.Matrix = matrix;
            foreach (UIElement child in (sender as Canvas).Children)
            {
                double left = Canvas.GetLeft(child);
                double top = Canvas.GetTop(child);
                double length = left * (double)num;
                double length2 = top * (double)num;
                Canvas.SetLeft(child, length);
                Canvas.SetTop(child, length2);
                child.RenderTransform = _transform;
            }
        }
    }
}
