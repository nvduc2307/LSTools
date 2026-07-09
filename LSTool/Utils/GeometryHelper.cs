using Autodesk.Revit.UI;
using Nice3point.Revit.Extensions.Runtime;

namespace LSTool.Utils
{
    public static class GeometryHelper
    {
        public static double Distance(this XYZ p, Line l)
        {
            var d = 0.0;
            try
            {
                d = p.Distance(l.GetEndPoint(0));
                var dir = l.Direction;
                var vt = (l.GetEndPoint(0) - p).Normalize();
                if (dir.DotProduct(vt).IsAlmostEqual(0)) return p.Distance(l.GetEndPoint(0));
                if (Math.Abs(dir.DotProduct(vt)).IsAlmostEqual(1)) return 0;

                var angle = dir.DotProduct(vt) > 0
                    ? vt.AngleTo(dir)
                    : vt.AngleTo(-dir);
                d = Math.Sin(angle) * d;
            }
            catch (Exception)
            {
                d = 0.0;
            }
            return d;
        }

        public static double Distance(this XYZ p)
        {
            var result = 0.0;
            try
            {
                result = Math.Sqrt(p.X * p.X + p.Y * p.Y + p.Z * p.Z);
            }
            catch (Exception)
            {
            }
            return result;
        }

        public static double Distance(this XYZ p1, XYZ p2)
        {
            var x = p1.X - p2.X;
            var y = p1.Y - p2.Y;
            var z = p1.Z - p2.Z;
            return Math.Sqrt(x * x + y * y + z * z);
        }

        public static XYZ VectorNormal(this XYZ vt)
        {
            return vt / vt.Distance();
        }

        public static XYZ MidPoint(this XYZ p1, XYZ p2)
        {
            var x = (p1.X + p2.X) * 0.5;
            var y = (p1.Y + p2.Y) * 0.5;
            var z = (p1.Z + p2.Z) * 0.5;
            return new XYZ(x, y, z);
        }

        public static double AngleTo(this XYZ vt1, XYZ vt2)
        {
            var result = 0.0;
            try
            {
                var cos = vt1.DotProduct(vt2) / (vt1.Distance() * vt2.Distance());
                result = Math.Acos(cos);
            }
            catch (Exception)
            {
            }
            return result;
        }
        public static XYZ Rotate(this XYZ p, XYZ c, double degRad)
        {
            var pn = new XYZ(p.X, p.Y, 0);
            var cn = new XYZ(c.X, c.Y, 0);
            var x = cn.X + (pn.X - cn.X) * Math.Cos(degRad) - (pn.Y - cn.Y) * Math.Sin(degRad);
            var y = cn.Y + (pn.X - cn.X) * Math.Sin(degRad) + (pn.Y - cn.Y) * Math.Cos(degRad);
            return new XYZ(x, y, c.Z);
        }

        public static XYZ Round(this XYZ p, int n = 4)
        {
            return new XYZ(Math.Round(p.X, n), Math.Round(p.Y, n), Math.Round(p.Z, n));
        }

        public static XYZ PointToLine(this XYZ p, Line l)
        {
            var result = p;
            try
            {
                var dir = (l.GetEndPoint(1) - l.GetEndPoint(0)).Normalize();
                var d = p.Distance(l.GetEndPoint(0));
                var vt = (l.GetEndPoint(0) - p).Normalize();
                if (dir.DotProduct(vt).IsAlmostEqual(0)) return l.GetEndPoint(0);
                if (Math.Abs(dir.DotProduct(vt)).IsAlmostEqual(1, 0.00000001)) return p;

                var normal = dir.CrossProduct(vt);

                var vti = dir.CrossProduct(normal).DotProduct(vt) > 0
                    ? dir.CrossProduct(normal).Normalize()
                    : -dir.CrossProduct(normal).Normalize();

                var angle = dir.DotProduct(vt) > 0
                    ? vt.AngleTo(dir)
                    : vt.AngleTo(-dir);

                d = Math.Sin(angle) * d;

                result = p + vti * d;
            }
            catch (Exception)
            {
                result = p;
            }
            //ddang sai
            return result;
        }

        public static XYZ Mirror(this XYZ p, Line l)
        {
            var pm = p.PointToLine(l);
            return p.Mirror(pm);
        }

        public static XYZ Mirror(this XYZ p, XYZ pc)
        {
            return new XYZ(pc.X * 2 - p.X, pc.Y * 2 - p.Y, pc.Z * 2 - p.Z);
        }

        public static ModelCurve CreateModelLine(this Document doc, Line l, XYZ normal)
        {
            var pl = Plane.CreateByNormalAndOrigin(normal, l.Midpoint());
            var sk = SketchPlane.Create(doc, pl);
            var ml = doc.Create.NewModelCurve(l, sk);
            return ml;
        }

        public static List<Curve> PointsToCurves(this List<XYZ> points, bool isClose = false)
        {
            var curves = new List<Curve>();
            points = points.Where(x => x != null).ToList();
            var pCount = points.Count;
            if (isClose && pCount > 2)
            {
                for (int i = 0; i < pCount; i++)
                {
                    var j = i == 0 ? pCount - 1 : i - 1;
                    var sp = points[j];
                    var ep = points[i];

                    curves.Add(Line.CreateBound(sp, ep));
                }
            }
            else
            {
                for (int i = 0; i < points.Count - 1; i++)
                {
                    var sp = points[i];
                    var ep = points[i + 1];

                    curves.Add(Line.CreateBound(sp, ep));
                }
            }
            return curves;
        }
        public static CurveLoop PointsToCurveLoop(this List<XYZ> points)
        {
            CurveLoop result = null;
            try
            {
                points = points.Distinct(new ComparePoint()).ToList();
                if (points.Count < 3) return result;
                result = new CurveLoop();
                var cs = points.PointsToCurves(true);
                foreach (var c in cs)
                {
                    result.Append(c);
                }
            }
            catch (Exception)
            {
                result = null;
            }
            return result;
        }
        public static bool IsSame(this XYZ p1, XYZ p2)
        {
            return p1.X.IsAlmostEqual(p2.X) && p1.Y.IsAlmostEqual(p2.Y) && p1.Z.IsAlmostEqual(p2.Z);
        }
        public static bool IsParallel(this XYZ vt1, XYZ vt2)
        {
            return Math.Abs(vt1.DotProduct(vt2)).IsAlmostEqual(1, 0.001);
        }
        public static XYZ GetCenter(this List<XYZ> ps)
        {
            XYZ result = null;
            try
            {
                var minx = ps.Min(p => p.X);
                var miny = ps.Min(p => p.Y);
                var minz = ps.Min(p => p.Z);

                var maxx = ps.Max(p => p.X);
                var maxy = ps.Max(p => p.Y);
                var maxz = ps.Max(p => p.Z);

                var min = new XYZ(minx, miny, minz);
                var max = new XYZ(maxx, maxy, maxz);
                result = min.MidPoint(max);
            }
            catch (Exception)
            {
            }
            return result;
        }
        public static XYZ RayIntersectPlane(this XYZ point, XYZ vecRay, PlanarFace planarFace)
        {
            XYZ p = null;

            var ori = planarFace.Origin;
            var normal = planarFace.FaceNormal;
            if (vecRay.DotProduct(normal) != 0)
            {
                var dist = (ori - point).DotProduct(normal) / (vecRay.DotProduct(normal));
                p = point.Add(dist * vecRay.Normalize());
            }
            return p;
        }
        public static XYZ RayIntersectPlane(this XYZ point, XYZ vecRay, Plane plane)
        {
            XYZ p = null;
            var ori = plane.Origin;
            var normal = plane.Normal.Normalize();
            if (vecRay.DotProduct(normal) != 0)
            {
                var dist = (ori - point).DotProduct(normal) / (vecRay.DotProduct(normal));
                p = point.Add(dist * vecRay.Normalize());
            }
            return p;
        }
        public static XYZ RayIntersectLine(this XYZ p, XYZ vtRay, Line line, bool inScopeLine = true)
        {
            var sp = line.GetEndPoint(0);
            var ep = line.GetEndPoint(1);
            var fNor = Plane.CreateByNormalAndOrigin(line.Direction, p);
            var pVg = sp.RayIntersectPlane(fNor.Normal, fNor);
            if (pVg == null) return null;
            if (Math.Round(pVg.DistanceTo(p).ToMillimeters(), 0) <= 1) return pVg;
            var vtNor = (p - pVg).Normalize();
            var f = Plane.CreateByNormalAndOrigin(vtNor, sp);
            var pInter = p.RayIntersectPlane(vtRay, f);
            if (pInter == null) return null;
            var dirCheck = Math.Round(pInter.DistanceTo(sp).ToMillimeters(), 0) <= 1
                ? (pInter - ep).Normalize()
                : (pInter - sp).Normalize();
            if (!dirCheck.IsParallel(line.Direction)) return null;
            if (!inScopeLine) return pInter;
            var dirCheck1 = (pInter - sp).Normalize();
            var dirCheck2 = (pInter - ep).Normalize();
            if (dirCheck1.DotProduct(dirCheck2) > 0) return null;
            return pInter;
        }
        public static XYZ GetModelCoordinatesAtCursor(this UIDocument uidoc)
        {
            UIView uiview = ViewHelper.GetActiveUIView(uidoc);
            var view = uidoc.Document.ActiveView;
            if (uiview is null) return XYZ.Zero;
            var rect = uiview.GetWindowRectangle();
            var p = System.Windows.Forms.Cursor.Position;
            double u = (p.X - rect.Left) / (double)(rect.Right - rect.Left);
            double v = (p.Y - rect.Top) / (double)(rect.Bottom - rect.Top);
            XYZ R = view.RightDirection, U = view.UpDirection;
            double dx = (double)(p.X - rect.Left)
              / (rect.Right - rect.Left);

            double dy = (double)(p.Y - rect.Bottom)
              / (rect.Top - rect.Bottom);

            IList<XYZ> corners = uiview.GetZoomCorners();
            XYZ a = corners[0];
            XYZ b = corners[1];
            XYZ diag = b - a;
            double w = diag.DotProduct(R), hgt = diag.DotProduct(U);
            XYZ B = a + (u * w) * R + ((1 - v) * hgt) * U;
            return B;
        }
    }
}
