using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using LSTool.Compatibility;
using RIMT.Utils.Compares;

namespace RIMT.Utils.RevPoints
{
    public class RevPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }
    public class RevPolygon
    {
        public int Id { get; set; }
        public List<RevPoint> Shape { get; set; }
    }
    public static class RevPointUtils
    {
        public static XYZ CenterPoint(this List<XYZ> points)
        {
            points = points.Distinct(new ComparePoint()).ToList();
            if (!points.Any()) return null;
            if (points.Count == 1)
                return points.FirstOrDefault();
            var x = points.Select(a => a.X).ToList();
            var y = points.Select(a => a.Y).ToList();
            var z = points.Select(a => a.Z).ToList();
            var min = new XYZ(x.Min(), y.Min(), z.Min());
            var max = new XYZ(x.Max(), y.Max(), z.Max());
            var center = max.Midpoint(min);
            return center;
        }

        public static IEnumerable<XYZ> GetPoint(this IEnumerable<Rebar> rebars)
        {
            var results = new List<XYZ>();
            foreach (var rebar in rebars)
            {
                try
                {
                    var paths = rebar.GetCenterlineCurves(true, false, false, MultiplanarOption.IncludeAllMultiplanarCurves, 0);
                    foreach (var curve in paths)
                    {
                        results.Add(curve.GetEndPoint(0));
                        results.Add(curve.GetEndPoint(1));
                    }
                }
                catch (Exception)
                {
                }
            }
            return results;
        }

        public static IEnumerable<XYZ> GetPoint(this IEnumerable<FamilyInstance> familyInstances)
        {
            var results = new List<XYZ>();
            foreach (var familyInstance in familyInstances)
            {
                try
                {
                    var solid = familyInstance.GetSingleSolid();
                    var faces = solid.GetFacesFromSolid();
                    foreach (var face in faces)
                    {
                        var points = face.GetPoints();
                        results.AddRange(points);
                    }
                }
                catch (Exception)
                {
                }
            }
            return results;
        }

        public static List<Curve> PointsToCurves(this List<XYZ> points, bool isClose = false)
        {
            var curves = new List<Curve>();
            try
            {
                var pc = points.Count;
                for (int i = 0; i < pc; i++)
                {
                    if (isClose)
                    {
                        var j = i == 0 ? pc - 1 : i - 1;
                        curves.Add(points[j].CreateLine(points[i]));
                    }
                    else
                    {
                        if (i < pc - 1)
                        {
                            var sp = points[i];
                            var ep = points[i + 1];
                            curves.Add(sp.CreateLine(ep));
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return curves;
        }
    }
}
