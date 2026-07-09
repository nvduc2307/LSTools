namespace LSTool.Utils
{
    public static class SolidHelper
    {
        public static CurveLoop GetFirstCurveLoop(this Face face)
        {
            return (from x in face.GetEdgesAsCurveLoops()
                    orderby x.GetExactLength() descending
                    select x).FirstOrDefault();
        }
        public static List<Face> GetFacesFromSolid(this Solid solid)
        {
            List<Face> list = new List<Face>();
            if (solid == null)
            {
                return list;
            }

            list.AddRange(solid.Faces.Cast<Face>());
            return list;
        }
        public static Solid GetSolid(this List<Solid> solids)
        {
            try
            {
                solids = solids
                    .Where(s => s != null)
                    .Where(s => s.Volume > 0)
                    .ToList();
                if (!solids.Any()) return null;
                if (solids.Count == 1) return solids.First();
                var s = solids
                    .Aggregate((s1, s2) => BooleanOperationsUtils.ExecuteBooleanOperation(s1, s2, BooleanOperationsType.Union));
                return s;
            }
            catch (Exception ex)
            {
                var mes = ex.Message;
            }
            return null;
        }
        public static List<Solid> GetSolids(this Element element)
        {
            List<Solid> list = new List<Solid>();
            foreach (GeometryObject item in element.get_Geometry(new Options
            {
                IncludeNonVisibleObjects = true,
                ComputeReferences = true
            }))
            {
                Solid solid = item as Solid;
                GeometryInstance geometryInstance = item as GeometryInstance;
                if (solid != null && solid.Volume > 1E-06)
                {
                    list.Add(solid);
                }

                if (!(geometryInstance != null))
                {
                    continue;
                }

                foreach (GeometryObject item2 in geometryInstance.GetInstanceGeometry())
                {
                    solid = item2 as Solid;
                    if (solid != null && solid.Volume > 1E-06)
                    {
                        list.Add(solid);
                    }
                }
            }

            return list;
        }
        public static DirectShape CreateDirectShape(this Solid solid, Document document, BuiltInCategory builtInCategory = BuiltInCategory.OST_GenericModel)
        {
            DirectShape result = null;
            try
            {
                result = DirectShape.CreateElement(document, new ElementId(builtInCategory));
                result.SetShape([solid]);
            }
            catch (Exception)
            {
            }
            return result;
        }
        public static Solid CreateSolid(this XYZ pCenter, XYZ vtx, XYZ vty, XYZ vtz, double heightMm, double widthMm, double thicknessMm = 0)
        {
            Solid result = null;
            try
            {
                var p1 = pCenter - vtx * widthMm.FromMillimeters() / 2 - vty * heightMm.FromMillimeters() / 2 - vtz * heightMm.FromMillimeters() / 2;
                var p2 = p1 + vty * heightMm.FromMillimeters();
                var p3 = p2 + vtx * widthMm.FromMillimeters();
                var p4 = p3 - vty * heightMm.FromMillimeters();

                var ps = new List<XYZ>() { p1, p2, p3, p4 };
                thicknessMm = thicknessMm == 0 ? heightMm : thicknessMm;
                result = ps.CreateSolid(vtz, thicknessMm);
            }
            catch (Exception)
            {
            }
            return result;
        }
        public static Solid CreateSolid(this XYZ pCenter, XYZ dir, double heightMm, double widthMm)
        {
            Solid result = null;
            try
            {
                var vtx = dir;
                var vty = vtx.IsParallel(XYZ.BasisZ)
                    ? vtx.CrossProduct(XYZ.BasisX)
                    : vtx.CrossProduct(XYZ.BasisZ);
                var vtz = vtx.CrossProduct(vty);

                var p1 = pCenter - vtx * widthMm.FromMillimeters() / 2 - vty * heightMm.FromMillimeters() / 2 - vtz * heightMm.FromMillimeters() / 2;
                var p2 = p1 + vty * heightMm.FromMillimeters();
                var p3 = p2 + vtx * widthMm.FromMillimeters();
                var p4 = p3 - vty * heightMm.FromMillimeters();

                var ps = new List<XYZ>() { p1, p2, p3, p4 };
                result = ps.CreateSolid(vtz, heightMm);
            }
            catch (Exception)
            {
            }
            return result;
        }
        public static Solid CreateSolid(this List<XYZ> polygons, XYZ normal, double thicknessMm)
        {
            Solid result = null;
            var polygonsCount = polygons.Count;
            if (polygonsCount > 2)
            {
                //create list curveloop
                var curveLoop = new CurveLoop();
                for (int i = 0; i < polygonsCount; i++)
                {
                    var j = i == 0 ? polygonsCount - 1 : i - 1;
                    curveLoop.Append(Line.CreateBound(polygons[j], polygons[i]));
                }
                //create solid
                result = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>() { curveLoop }, normal, thicknessMm.FromMillimeters());
            }
            return result;
        }
        public static Solid CreateSolidVertical(this List<XYZ> polygons, double heightMm)
        {
            Solid result = null;
            var polygonsCount = polygons.Count;
            if (polygonsCount > 2)
            {
                //create list curveloop
                var curveLoop = new CurveLoop();
                for (int i = 0; i < polygonsCount; i++)
                {
                    if (i != polygonsCount - 1)
                    {
                        var p1 = new XYZ(polygons[i].X, polygons[i].Y, polygons[0].Z);
                        var p2 = new XYZ(polygons[i + 1].X, polygons[i + 1].Y, polygons[0].Z);
                        curveLoop.Append(Line.CreateBound(p1, p2));
                    }
                    else
                    {
                        var p1 = new XYZ(polygons[i].X, polygons[i].Y, polygons[0].Z);
                        var p2 = new XYZ(polygons[0].X, polygons[0].Y, polygons[0].Z);
                        curveLoop.Append(Line.CreateBound(p1, p2));
                    }
                }
                //create solid
                result = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>() { curveLoop }, XYZ.BasisZ, heightMm.FromMillimeters());
            }
            return result;
        }
        public static List<XYZ> GetPoints(this Face face)
        {
            List<XYZ> list = new List<XYZ>();
            foreach (Curve item in (from x in face.GetEdgesAsCurveLoops()
                                    orderby x.GetExactLength() descending
                                    select x).FirstOrDefault())
            {
                list.Add(item.GetEndPoint(0));
            }

            return list;
        }
        public static List<XYZ> GetPoints(this Solid solid)
        {
            var result = new List<XYZ>();
            try
            {
                result = solid
                .GetFacesFromSolid()
                .Select(x => x.GetPoints())
                .Aggregate((a, b) => a.Concat(b)
                .Distinct(new ComparePoint())
                .ToList());
            }
            catch (Exception)
            {

            }
            return result;
        }
        public static XYZ GetCenter(this Solid solid)
        {
            XYZ result = null;
            try
            {
                var ps = solid.GetPoints();
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
        public static Solid GetSingleSolid(this Element e)
        {
            Solid solid = null;
            foreach (GeometryObject item in e.get_Geometry(new Options
            {
                ComputeReferences = true
            }))
            {
                if (item is Solid)
                {
                    solid = item as Solid;
                    if (solid.Faces.Size != 0 && solid.Edges.Size != 0)
                    {
                        break;
                    }
                }

                if (!(item is GeometryInstance))
                {
                    continue;
                }

                foreach (GeometryObject item2 in (item as GeometryInstance).GetInstanceGeometry())
                {
                    solid = item2 as Solid;
                    if (solid != null && solid.Faces.Size != 0 && solid.Edges.Size != 0)
                    {
                        goto end_IL_00b1;
                    }
                }

                continue;
            end_IL_00b1:
                break;
            }

            return solid;
        }
        public static BoundingBoxXYZ GetBoundingBoxXYZ(this Solid solid)
        {
            var result = new BoundingBoxXYZ();
            try
            {
                var ps = solid.GetPoints();
                var minx = ps.Min(x => x.X);
                var miny = ps.Min(x => x.Y);
                var minz = ps.Min(x => x.Z);
                var maxx = ps.Max(x => x.X);
                var maxy = ps.Max(x => x.Y);
                var maxz = ps.Max(x => x.Z);
                result.Min = new XYZ(minx, miny, minz);
                result.Max = new XYZ(maxx, maxy, maxz);
            }
            catch (Exception)
            {
                result = null;
            }
            return result;
        }
        public static Solid OffsetSolid(this Solid solid, double offsetMm)
        {
            var result = solid;
            try
            {
                var boundingBoxXyz = solid.GetBoundingBoxXYZ();
                if (boundingBoxXyz == null) throw new Exception();
                var outline = new Outline(new XYZ(boundingBoxXyz.Min.X - offsetMm.FromMillimeters(), boundingBoxXyz.Min.Y - offsetMm.FromMillimeters(), boundingBoxXyz.Min.Z - offsetMm.FromMillimeters()),
                    new XYZ(boundingBoxXyz.Max.X + offsetMm.FromMillimeters(), boundingBoxXyz.Max.Y + offsetMm.FromMillimeters(), boundingBoxXyz.Max.Z + offsetMm.FromMillimeters()));

                var slbox = new BoundingBoxXYZ();
                slbox.Min = outline.MinimumPoint;
                slbox.Max = outline.MaximumPoint;
                result = slbox.SolidFromBoundingbox();
            }
            catch (Exception)
            {
                result = solid;
            }
            return result;
        }
        public static List<Solid> GetSolid(this Element element)
        {
            try
            {
                var result = new List<Solid>();
                var options = new Options();
                options.ComputeReferences = true;
                options.DetailLevel = ViewDetailLevel.Fine;
                var document = element.Document;
                if (element is AssemblyInstance ass)
                {
                    var solids = ass
                    .GetMemberIds()
                    .Select(x =>
                    {
                        var sls = new List<Solid>();
                        try
                        {
                            var ele = document.GetElement(x);
                            var geo = ele.get_Geometry(options);
                            foreach (var item in geo)
                            {
                                if (item is GeometryInstance geoIns)
                                {
                                    foreach (var item1 in geoIns.GetInstanceGeometry())
                                    {
                                        if (item1 is Solid sol)
                                            if (sol.Volume > 0) sls.Add(sol);
                                    }
                                    ;
                                }
                                if (item is Solid solid)
                                    sls.Add(solid);
                            }
                        }
                        catch (Exception)
                        {
                        }
                        return sls;
                    })
                    .Aggregate((a, b) => a.Concat(b).ToList());
                    if (solids.Any()) result.AddRange(solids);
                }
                else
                {
                    var geo = element.get_Geometry(options);
                    if (geo == null) return result;
                    foreach (var item in geo)
                    {
                        if (item is GeometryInstance geoIns)
                        {
                            foreach (var item1 in geoIns.GetInstanceGeometry())
                            {
                                if (item1 is Solid sol)
                                    if (sol.Volume > 0) result.Add(sol);
                            }
                            ;
                        }
                        if (item is Solid solid)
                            result.Add(solid);
                    }
                }
                return result;
            }
            catch (Exception)
            {
            }
            return new List<Solid>();
        }
        public static List<Solid> GetSolidsExtensions(this Element element)
        {
            var result = new List<Solid>();
            try
            {
                var document = element.Document;
                if (element is AssemblyInstance ass)
                {
                    var solids = ass
                    .GetMemberIds()
                    .Select(x =>
                    {
                        var sls = new List<Solid>();
                        try
                        {
                            sls = document.GetElement(x).GetSolid();
                        }
                        catch (Exception)
                        {
                        }
                        return sls;
                    })
                    .Aggregate((a, b) => a.Concat(b).ToList());
                    if (solids.Any()) result.AddRange(solids);
                }
                else
                {
                    var solids = element.GetSolid();
                    if (solids.Any()) result.AddRange(solids);
                }
            }
            catch (Exception)
            {
            }
            return result;
        }
        public static Solid SolidFromBoundingbox(this BoundingBoxXYZ bb, double height = 100)
        {
            XYZ min = bb.Min;
            XYZ max = bb.Max;
            XYZ xYZ = min;
            XYZ xYZ2 = new XYZ(min.X, max.Y, min.Z);
            XYZ xYZ3 = new XYZ(max.X, max.Y, min.Z);
            XYZ xYZ4 = new XYZ(max.X, min.Y, min.Z);
            Line curve = Line.CreateBound(xYZ, xYZ2);
            Line curve2 = Line.CreateBound(xYZ2, xYZ3);
            Line curve3 = Line.CreateBound(xYZ3, xYZ4);
            Line curve4 = Line.CreateBound(xYZ4, xYZ);
            CurveLoop curveLoop = new CurveLoop();
            curveLoop.Append(curve);
            curveLoop.Append(curve2);
            curveLoop.Append(curve3);
            curveLoop.Append(curve4);
            return GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { curveLoop }, XYZ.BasisZ, height.FromMillimeters());
        }
        public static List<Edge> GetEdges(this Element element, XYZ dir)
        {
            var result = new List<Edge>();
            var fSolids = element.GetSolid();
            if (fSolids == null) return result;
            if (!fSolids.Any()) return result;
            foreach (var fSolid in fSolids)
            {
                foreach (var item in fSolid.Edges)
                {
                    if (!(item is Edge edge)) continue;
                    if (edge.Reference == null) continue;
                    if (!(edge.AsCurve() is Line l)) continue;
                    if (!l.Direction.IsParallel(dir)) continue;
                    result.Add(edge);
                }
            }
            return result;
        }
    }
}
