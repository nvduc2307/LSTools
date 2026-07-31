using Autodesk.Revit.DB;
using LSTool.Compatibility;
using PointCanvas = System.Windows.Point;
using VectorCanvas = System.Windows.Vector;
using RIMT.Utils;

namespace RIMT.Utils.canvass
{
    public static class GeometryInCanvas
    {
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
        public static IEnumerable<PointCanvas> ConvertPoint(this IEnumerable<XYZ> points, double canvasScale)
        {
            var result = new List<PointCanvas>();
            if (points.Count() > 0)
            {
                foreach (var point in points)
                {
                    result.Add(point.ConvertPoint(canvasScale));
                }
            }
            return result;
        }
        public static PointCanvas ConvertPoint(this XYZ point, double canvasScale)
        {
            return new PointCanvas(point.X.FootToMm() * canvasScale, -point.Y.FootToMm() * canvasScale);
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
        public static PointCanvas ConvertPointRToC(this XYZ p, XYZ centerRevit, CanvasPageBase canvasPageBase, PointCanvas originInCanvas = new PointCanvas())
        {
            var centerCanvas = originInCanvas.X == 0 && originInCanvas.Y == 0
                ? canvasPageBase.Center
                : originInCanvas;
            var scale = canvasPageBase.Scale;
            var scaleX = canvasPageBase.ScaleX;
            var scaleY = canvasPageBase.ScaleY;
            var result = new PointCanvas();
            try
            {
                var centerObj = new PointCanvas(centerRevit.X * scaleX, -centerRevit.Y * scaleY);
                var vtMove = new VectorCanvas(centerCanvas.X - centerObj.X, centerCanvas.Y - centerObj.Y);
                result = new PointCanvas(p.X * scaleX + vtMove.X, -p.Y * scaleY + vtMove.Y);
            }
            catch (Exception)
            {
            }
            return result;
        }
        public static XYZ ConvertPointCToR(this PointCanvas p, XYZ centerRevit, CanvasPageBase canvasPageBase)
        {
            var centerCanvas = canvasPageBase.Center;
            var scale = canvasPageBase.Scale;
            var scaleX = canvasPageBase.ScaleX;
            var scaleY = canvasPageBase.ScaleY;
            var result = new XYZ();
            try
            {
                var centerObj = new PointCanvas(centerRevit.X, -centerRevit.Y);
                var vtMove = new VectorCanvas(centerObj.X - centerCanvas.X / scaleX, centerObj.Y - centerCanvas.Y / scaleY);
                var pRealInCanvas = new PointCanvas(p.X / scaleX + vtMove.X, p.Y / scaleY + vtMove.Y);
                result = new XYZ(pRealInCanvas.X, -pRealInCanvas.Y, 0);
            }
            catch (Exception)
            {
            }
            return result;
        }
        public static double AngleRadTo(this PointCanvas vt1, PointCanvas vt2)
        {
            var result = 0.0;
            try
            {
                var vt1Dis = vt1.VtDistance();
                var vt2Dis = vt2.VtDistance();
                if (Math.Abs(vt1Dis) < 1e-9) throw new Exception();
                if (Math.Abs(vt2Dis) < 1e-9) throw new Exception();
                result = Math.Acos((vt1.X * vt2.X + vt1.Y * vt2.Y) / (vt1Dis * vt2Dis));
                result = result.ToString().Contains("NaN") ? 0 : result;
                if (result.ToString().Contains("NaN"))
                {
                    IO.ShowInfo(vt1.ToString());
                    IO.ShowInfo(vt2.ToString());
                    IO.ShowInfo(vt1Dis.ToString());
                    IO.ShowInfo(vt2Dis.ToString());
                }
            }
            catch (Exception)
            {
            }
            return result;
        }
        public static bool IsSame(this PointCanvas p1, PointCanvas p2)
        {
            return Math.Abs(p1.X - p2.X) <= 0.01 && Math.Abs(p1.Y - p2.Y) <= 0.01;
        }
    }
}
