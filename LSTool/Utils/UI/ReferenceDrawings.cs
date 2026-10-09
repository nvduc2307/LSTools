using System.Globalization;
using FormattedText = System.Windows.Media.FormattedText;
using FlowDirection = System.Windows.FlowDirection;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Pen = System.Windows.Media.Pen;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace LSTool.Utils.UI;
/// <summary>Assembly-owned vector drawings for the compact UI and Revit ribbon.</summary>
public static class ReferenceDrawings
{
    private static readonly Brush Ink = new SolidColorBrush(Color.FromRgb(23, 32, 51));
    private static readonly Brush Blue = new SolidColorBrush(Color.FromRgb(37, 99, 235));
    public static ImageSource AppIcon
    {
        get
        {
            var group = new DrawingGroup();
            using (var dc = group.Open())
            {
                dc.DrawRoundedRectangle(Blue, null, new Rect(0, 0, 20, 20), 3, 3);
                var text = new FormattedText("LS", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"), 12, Brushes.White, 1);
                dc.DrawText(text, new Point(3, 2));
            }
            var image = new DrawingImage(group); image.Freeze(); return image;
        }
    }
    public static ImageSource MainBarHook => Hook(false);
    public static ImageSource StirrupHook => Hook(true);
    private static ImageSource Hook(bool stirrup)
    {
        var group = new DrawingGroup();
        using (var dc = group.Open())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, 140, 230));
            if (!stirrup)
            {
                Label(dc, "Hook geometry", 4, 2);
                Path(dc, "M 120,65 L 55,65 Q 30,65 30,90 L 30,160 Q 30,185 55,185 L 120,185", Ink, 5);
                Path(dc, "M 30,48 L 120,48 M 30,43 L 30,53 M 120,43 L 120,53 M 15,65 L 15,185 M 10,65 L 20,65 M 10,185 L 20,185 M 30,200 L 120,200 M 30,195 L 30,205 M 120,195 L 120,205 M 55,95 L 36,79", Blue, 1);
                Label(dc, "A", 69, 29); Label(dc, "R", 56, 89); Label(dc, "B", 0, 118); Label(dc, "C", 69, 205);
            }
            else
            {
                Label(dc, "90° hook", 3, 1); Path(dc, "M 124,58 L 54,58 Q 35,58 35,39 L 35,24", Ink, 5); Label(dc, "R", 52, 29);
                Label(dc, "135° hook", 3, 78); Path(dc, "M 124,138 L 62,138 Q 53,138 47,132 L 30,115", Ink, 5); Label(dc, "R", 63, 111);
                Label(dc, "180° hook", 3, 158); Path(dc, "M 124,218 L 52,218 Q 25,218 25,194 Q 25,178 52,178 L 77,178", Ink, 5); Label(dc, "R", 55, 191);
            }
        }
        var image = new DrawingImage(group); image.Freeze(); return image;
    }
    private static void Label(DrawingContext dc, string value, double x, double y)
    {
        var text = new FormattedText(value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 12, Ink, 1);
        dc.DrawText(text, new Point(x, y));
    }
    private static void Path(DrawingContext dc, string data, Brush stroke, double thickness) => dc.DrawGeometry(null, new Pen(stroke, thickness), Geometry.Parse(data));
    public static ImageSource Ribbon(string name, int size)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.PushTransform(new ScaleTransform(size / 32.0, size / 32.0));
            switch (name)
            {
                case "Diameter": dc.DrawEllipse(null, new Pen(Blue, 2), new Point(16, 16), 11, 11); Path(dc, "M 4,28 L 28,4", Blue, 2); break;
                case "Cover": dc.DrawRectangle(null, new Pen(Ink, 1.5), new Rect(3, 3, 26, 26)); dc.DrawRectangle(null, new Pen(Blue, 2), new Rect(8, 8, 16, 16)); break;
                case "Parameter": Path(dc, "M 5,5 L 27,5 L 27,27 L 5,27 Z M 5,12 L 27,12 M 5,20 L 27,20 M 13,5 L 13,27 M 21,5 L 21,27", Blue, 1.7); break;
                case "Beam": Path(dc, "M 3,12 L 22,3 L 29,7 L 10,16 Z M 3,12 L 3,23 L 10,28 L 29,19 L 29,7 M 10,16 L 10,28", Ink, 1.8); break;
                case "BeamSettings":
                    dc.DrawRectangle(null, new Pen(Ink, 1.8), new Rect(3, 4, 26, 9));
                    Path(dc, "M 5,21 L 27,21 M 5,28 L 27,28", Blue, 2);
                    dc.DrawEllipse(Brushes.White, new Pen(Blue, 2), new Point(11, 21), 3, 3);
                    dc.DrawEllipse(Brushes.White, new Pen(Blue, 2), new Point(22, 28), 3, 3); break;
                case "ColumnSettings":
                    dc.DrawRectangle(null, new Pen(Ink, 1.8), new Rect(3, 3, 9, 26));
                    Path(dc, "M 19,4 L 19,28 M 27,4 L 27,28", Blue, 2);
                    dc.DrawEllipse(Brushes.White, new Pen(Blue, 2), new Point(19, 11), 3, 3);
                    dc.DrawEllipse(Brushes.White, new Pen(Blue, 2), new Point(27, 23), 3, 3); break;
                case "Column": Path(dc, "M 10,5 L 17,2 L 24,6 L 17,10 Z M 10,5 L 10,27 L 17,31 L 24,27 L 24,6 M 17,10 L 17,31", Ink, 1.8); break;
            }
            dc.Pop();
        }
        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual); bitmap.Freeze(); return bitmap;
    }
}
