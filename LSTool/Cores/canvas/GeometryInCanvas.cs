using PointCanvas = System.Windows.Point;
using VectorCanvas = System.Windows.Vector;

namespace LSTool.Cores.canvas
{
    public static class GeometryInCanvas
    {
        public static PointCanvas Mid(this PointCanvas p1, PointCanvas p2)
        {
            return new PointCanvas(0.5*(p1.X + p2.X), 0.5 * (p1.Y + p2.Y)); 
        }
        public static double VtDistance(this PointCanvas p)
        {
            return Math.Sqrt(p.X * p.X + p.Y * p.Y);
        }
        public static PointCanvas Vt(this PointCanvas p1, PointCanvas p2)
        {
            return new PointCanvas(p2.X - p1.X, p2.Y - p1.Y);
        }
        public static PointCanvas VtNormal(this PointCanvas p)
        {
            var d = p.VtDistance();
            return new PointCanvas(p.X / d, p.Y / d);
        }
        public static PointCanvas GetVector(this PointCanvas p1, PointCanvas p2)
        {
            return new PointCanvas(p2.X - p1.X, p2.Y - p1.Y);
        }
        public static PointCanvas Rotate(this PointCanvas p, PointCanvas c, double angle)
        {
            var x = (p.X - c.X) * Math.Cos(angle) - (p.Y - c.Y) * Math.Sin(angle) + c.X;
            var y = (p.X - c.X) * Math.Sin(angle) + (p.Y - c.Y) * Math.Cos(angle) + c.Y;
            return new PointCanvas(x, y);
        }
        public static PointCanvas RotateVector(this PointCanvas p, PointCanvas c, double angle)
        {
            var pOri = new PointCanvas();
            var p0 = pOri.Rotate(c, angle);
            var p1 = p.Rotate(c, angle);
            var vt = new PointCanvas(p1.X - p0.X, p1.Y - p0.Y);

            return vt.VtNormal();
        }
        public static PointCanvas Translate(this PointCanvas p, PointCanvas vt)
        {
            return new PointCanvas(p.X + vt.X, p.Y + vt.Y);
        }
        public static PointCanvas RotateAndTranslate(this PointCanvas p, PointCanvas c, double angle, PointCanvas vt)
        {
            var pn = p.Rotate(c, angle);
            return pn.Translate(vt);
        }
    }
}
