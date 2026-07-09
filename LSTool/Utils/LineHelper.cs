using Autodesk.Revit.DB.Structure;

namespace LSTool.Utils
{
    public static class LineHelper
    {
        public static XYZ Direction(this Line l)
        {
            return (l.GetEndPoint(1) - l.GetEndPoint(0)).Normalize();
        }
        public static XYZ Direction(this Curve l)
        {
            return (l.GetEndPoint(1) - l.GetEndPoint(0)).Normalize();
        }
        public static XYZ Midpoint(this Line l)
        {
            return (l.GetEndPoint(0) + l.GetEndPoint(1)) * 0.5;
        }
        public static XYZ Midpoint(this Curve l)
        {
            return (l.GetEndPoint(0) + l.GetEndPoint(1)) * 0.5;
        }
        public static List<Line> GetLines(this Element element)
        {
            var results = new List<Line>();
            if (element is AssemblyInstance) return results;
            if (element is Rebar) return results;
            var document = element.Document;
            var options = new Options();
            options.DetailLevel = ViewDetailLevel.Fine;
            var geo = element.get_Geometry(options);
            var solids = new List<Solid>();
            var lines = new List<Line>();
            foreach (var item in geo)
            {
                if (item is GeometryInstance geoIns)
                {
                    foreach (var item1 in geoIns.GetInstanceGeometry())
                    {
                        if (item1 is Solid sol)
                            if (sol.Volume > 0) solids.Add(sol);
                        if (item1 is Line l1)
                            lines.Add(l1);
                    }
                }
                if (item is Solid solid)
                    if (solid.Volume > 0) solids.Add(solid);
                if (item is Line l2)
                    lines.Add(l2);
            }
            if (!solids.Any()) return lines;
            foreach (var solid in solids)
            {
                var crs = solid.GetFacesFromSolid()
                    .Select(x => x.GetFirstCurveLoop().ToList())
                    .Select(x => x)
                    .Aggregate((a, b) => a.Concat(b).ToList())
                    .Where(x => x is Line)
                    .Cast<Line>()
                    .ToList();
                lines.AddRange(crs);
            }
            results = lines;
            return results;
        }
        public static void CreateCurves(this Document document, List<Curve> curves)
        {
            foreach (var l in curves)
            {
                try
                {
                    var nor = l.Direction().IsParallel(XYZ.BasisZ) ? l.Direction().CrossProduct(XYZ.BasisX) : l.Direction().CrossProduct(XYZ.BasisZ);
                    var plane = Plane.CreateByNormalAndOrigin(nor, l.Midpoint());
                    var sket = SketchPlane.Create(document, plane);

                    document.Create.NewModelCurve(l, sket);
                }
                catch (Exception)
                {
                }
            }
        }
        public static void CreateCurves(this Document document, Curve curve)
        {
            try
            {
                var nor = curve.Direction().IsParallel(XYZ.BasisZ) ? curve.Direction().CrossProduct(XYZ.BasisX) : curve.Direction().CrossProduct(XYZ.BasisZ);
                var plane = Plane.CreateByNormalAndOrigin(nor, curve.Midpoint());
                var sket = SketchPlane.Create(document, plane);

                document.Create.NewModelCurve(curve, sket);
            }
            catch (Exception)
            {
            }
        }
    }
}
