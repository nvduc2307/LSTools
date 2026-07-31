using Autodesk.Revit.DB;
using LSTool.Compatibility;
using RIMT.Utils.Compares;

namespace RIMT.Utils.Solids
{
    public static class SolidUtils
    {
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
        public static Solid CreateSolid(this Line l, double heightMm, double widthMm)
        {
            Solid result = null;
            try
            {
                var vtx = l.Direction;
                var vty = vtx.IsParallel(XYZ.BasisZ)
                    ? vtx.CrossProduct(XYZ.BasisX)
                    : vtx.CrossProduct(XYZ.BasisZ);
                var vtz = vtx.CrossProduct(vty);

                var p1 = l.GetEndPoint(0) - vtz * widthMm.MmToFoot() / 2 - vty * heightMm.MmToFoot() / 2;
                var p2 = p1 + vty * heightMm.MmToFoot();
                var p3 = p2 + vtz * widthMm.MmToFoot();
                var p4 = p3 - vty * heightMm.MmToFoot();

                var ps = new List<XYZ>() { p1, p2, p3, p4 };
                result = ps.CreateSolid(vtx, l.Length.FootToMm());
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

                var p1 = pCenter - vtx * widthMm.MmToFoot() / 2 - vty * heightMm.MmToFoot() / 2 - vtz * heightMm.MmToFoot() / 2;
                var p2 = p1 + vty * heightMm.MmToFoot();
                var p3 = p2 + vtx * widthMm.MmToFoot();
                var p4 = p3 - vty * heightMm.MmToFoot();

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
                result = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>() { curveLoop }, normal, thicknessMm.MmToFoot());
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
                result = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>() { curveLoop }, XYZ.BasisZ, heightMm.MmToFoot());
            }
            return result;
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
                var outline = new Outline(new XYZ(boundingBoxXyz.Min.X - offsetMm.MmToFoot(), boundingBoxXyz.Min.Y - offsetMm.MmToFoot(), boundingBoxXyz.Min.Z - offsetMm.MmToFoot()),
                    new XYZ(boundingBoxXyz.Max.X + offsetMm.MmToFoot(), boundingBoxXyz.Max.Y + offsetMm.MmToFoot(), boundingBoxXyz.Max.Z + offsetMm.MmToFoot()));

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

        public static Solid OffsetSolid1(this Solid solid, double offsetMm, string axis)
        {
            var result = solid;
            try
            {
                var boundingBoxXyz = solid.GetBoundingBoxXYZ();
                if (boundingBoxXyz == null) throw new Exception();

                double offset = offsetMm.MmToFoot();
                double fixedSize = 2.MmToFoot(); 

                double xCenter = (boundingBoxXyz.Min.X + boundingBoxXyz.Max.X) / 2.0;
                double yCenter = (boundingBoxXyz.Min.Y + boundingBoxXyz.Max.Y) / 2.0;
                double zCenter = (boundingBoxXyz.Min.Z + boundingBoxXyz.Max.Z) / 2.0;

                double minZ = zCenter - fixedSize / 2.0;
                double maxZ = zCenter + fixedSize / 2.0;

                double minX, maxX, minY, maxY;

                switch (axis.ToUpper())
                {
                    case "X":
                        minX = boundingBoxXyz.Min.X - offset;
                        maxX = boundingBoxXyz.Max.X + offset;
                        minY = yCenter - fixedSize / 2.0;
                        maxY = yCenter + fixedSize / 2.0;
                        break;

                    case "Y":
                        minY = boundingBoxXyz.Min.Y - offset;
                        maxY = boundingBoxXyz.Max.Y + offset;
                        minX = xCenter - fixedSize / 2.0;
                        maxX = xCenter + fixedSize / 2.0;
                        break;

                    default:
                        minX = boundingBoxXyz.Min.X - offset;
                        maxX = boundingBoxXyz.Max.X + offset;
                        minY = boundingBoxXyz.Min.Y - offset;
                        maxY = boundingBoxXyz.Max.Y + offset;
                        break;
                }

                var outline = new Outline(
                    new XYZ(minX, minY, minZ),
                    new XYZ(maxX, maxY, maxZ)
                );

                var slbox = new BoundingBoxXYZ
                {
                    Min = outline.MinimumPoint,
                    Max = outline.MaximumPoint
                };

                result = slbox.SolidFromBoundingbox();
            }
            catch (Exception)
            {
                result = solid;
            }

            return result;
        }

        public static Solid CreateDumbbellSolid(
             this Solid beamSolid,
             double offsetMm,
             string axis)
        {
            var bb = beamSolid.GetBoundingBoxXYZ();
            if (bb == null) return beamSolid;

            double offset = offsetMm.MmToFoot();
            double coreSize = 2.MmToFoot();   
            double expand = 10.MmToFoot();       
            double trim = 50.MmToFoot(); 


            double xCenter = (bb.Min.X + bb.Max.X) / 2;
            double yCenter = (bb.Min.Y + bb.Max.Y) / 2;
            double zCenter = (bb.Min.Z + bb.Max.Z) / 2;

            Solid core, head1, head2;

            if (axis.ToUpper() == "X")
            {
                // ========= CORE =========
                core = CreateBoxSolid(
                    bb.Min.X + trim,
                    bb.Max.X - trim,
                    yCenter - coreSize / 2,
                    yCenter + coreSize / 2,
                    zCenter - coreSize / 2,
                    zCenter + coreSize / 2);

                // ========= HEAD 1 =========
                head1 = CreateBoxSolid(
                    bb.Min.X - offset,
                    bb.Min.X + trim,
                    bb.Min.Y - expand,
                    bb.Max.Y + expand,
                    bb.Min.Z - expand,
                    bb.Max.Z + expand);

                // ========= HEAD 2 =========
                head2 = CreateBoxSolid(
                    bb.Max.X - trim,
                    bb.Max.X + offset,
                    bb.Min.Y - expand,
                    bb.Max.Y + expand,
                    bb.Min.Z - expand,
                    bb.Max.Z + expand);
            }
            else // Y
            {
                core = CreateBoxSolid(
                    xCenter - coreSize / 2,
                    xCenter + coreSize / 2,
                    bb.Min.Y + trim,
                    bb.Max.Y - trim,
                    zCenter - coreSize / 2,
                    zCenter + coreSize / 2);

                head1 = CreateBoxSolid(
                    bb.Min.X - expand,
                    bb.Max.X + expand,
                    bb.Min.Y - offset,
                    bb.Min.Y + trim,
                    bb.Min.Z - expand,
                    bb.Max.Z + expand);

                head2 = CreateBoxSolid(
                    bb.Min.X - expand,
                    bb.Max.X + expand,
                    bb.Max.Y - trim,
                    bb.Max.Y + offset,
                    bb.Min.Z - expand,
                    bb.Max.Z + expand);
            }

            // ========= UNION =========
            Solid result = BooleanOperationsUtils.ExecuteBooleanOperation(
                core, head1, BooleanOperationsType.Union);

            result = BooleanOperationsUtils.ExecuteBooleanOperation(
                result, head2, BooleanOperationsType.Union);

            return result;
        }


        private static Solid CreateBoxSolid(
            double minX, double maxX,
            double minY, double maxY,
            double minZ, double maxZ)
        {
            var bb = new BoundingBoxXYZ
            {
                Min = new XYZ(minX, minY, minZ),
                Max = new XYZ(maxX, maxY, maxZ)
            };

            return bb.SolidFromBoundingbox();
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
                                sls = document.GetElement(x).GetSolids();
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
                    var solids = element.GetSolids();
                    if (solids.Any()) result.AddRange(solids);
                }
            }
            catch (Exception)
            {
            }
            return result;
        }

        public static Solid UnionSolids(List<Solid> solids)
        {
            if (solids == null || solids.Count == 0)
                return null;

            var validSolids = solids
                .Where(s => s != null && s.Volume > 1e-6)
                .ToList();

            if (!validSolids.Any())
                return null;

            Solid result = validSolids.First();

            foreach (var solid in validSolids.Skip(1))
            {
                try
                {
                    result = BooleanOperationsUtils.ExecuteBooleanOperation(result, solid, BooleanOperationsType.Union);
                }
                catch
                {
                }
            }

            return result;
        }


        public static void DrawSolidOnView(Document doc, Solid solid, string appId = "MyApp")
        {
            if (solid == null) return;

            using (Transaction trans = new Transaction(doc, "Draw Solid On View"))
            {
                trans.Start();

                DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                ds.ApplicationId = appId;
                ds.ApplicationDataId = Guid.NewGuid().ToString();
                ds.SetShape(new GeometryObject[] { solid });

                OverrideGraphicSettings ogs = new OverrideGraphicSettings();
                ogs.SetSurfaceForegroundPatternColor(new Autodesk.Revit.DB.Color(0, 255, 0));
                ogs.SetSurfaceForegroundPatternId(GetSolidFillPatternId(doc));
                doc.ActiveView.SetElementOverrides(ds.Id, ogs);

                trans.Commit();
            }
        }

        private static ElementId GetSolidFillPatternId(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(FillPatternElement))
                .Cast<FillPatternElement>()
                .First(x => x.GetFillPattern().IsSolidFill)
                .Id;
        }


        public static Solid ExpandSolid(Solid solid, double offsetX, double offsetY, double offsetZ)
        {
            if (solid == null) return null;

            // Lấy bounding box
            BoundingBoxXYZ bbox = solid.GetBoundingBox();
            if (bbox == null) return null;

            XYZ min = bbox.Min;
            XYZ max = bbox.Max;

            offsetX = offsetX.MmToFoot();
            offsetY = offsetY.MmToFoot();
            offsetZ = offsetZ.MmToFoot();

            // Mở rộng theo các chiều
            min = new XYZ(min.X - offsetX, min.Y - offsetY, min.Z - offsetZ);
            max = new XYZ(max.X + offsetX, max.Y + offsetY, max.Z + offsetZ);

            // Tạo lại solid từ bounding box mở rộng
            return CreateSolidFromBoundingBox(min, max);
        }

        // Hàm phụ: tạo solid từ bounding box
        private static Solid CreateSolidFromBoundingBox(XYZ min, XYZ max)
        {
            // 8 điểm của hộp
            List<XYZ> corners = new List<XYZ>
    {
        new XYZ(min.X, min.Y, min.Z),
        new XYZ(max.X, min.Y, min.Z),
        new XYZ(max.X, max.Y, min.Z),
        new XYZ(min.X, max.Y, min.Z),
        new XYZ(min.X, min.Y, max.Z),
        new XYZ(max.X, min.Y, max.Z),
        new XYZ(max.X, max.Y, max.Z),
        new XYZ(min.X, max.Y, max.Z)
    };

            // Tạo 6 mặt từ các điểm
            List<CurveLoop> faces = new List<CurveLoop>();

            // mặt đáy
            faces.Add(CurveLoop.Create(new List<Curve>
    {
        Line.CreateBound(corners[0], corners[1]),
        Line.CreateBound(corners[1], corners[2]),
        Line.CreateBound(corners[2], corners[3]),
        Line.CreateBound(corners[3], corners[0])
    }));

            // mặt trên
            faces.Add(CurveLoop.Create(new List<Curve>
    {
        Line.CreateBound(corners[4], corners[5]),
        Line.CreateBound(corners[5], corners[6]),
        Line.CreateBound(corners[6], corners[7]),
        Line.CreateBound(corners[7], corners[4])
    }));

            // các mặt bên
            faces.Add(CurveLoop.Create(new List<Curve>
    {
        Line.CreateBound(corners[0], corners[1]),
        Line.CreateBound(corners[1], corners[5]),
        Line.CreateBound(corners[5], corners[4]),
        Line.CreateBound(corners[4], corners[0])
    }));

            faces.Add(CurveLoop.Create(new List<Curve>
    {
        Line.CreateBound(corners[1], corners[2]),
        Line.CreateBound(corners[2], corners[6]),
        Line.CreateBound(corners[6], corners[5]),
        Line.CreateBound(corners[5], corners[1])
    }));

            faces.Add(CurveLoop.Create(new List<Curve>
    {
        Line.CreateBound(corners[2], corners[3]),
        Line.CreateBound(corners[3], corners[7]),
        Line.CreateBound(corners[7], corners[6]),
        Line.CreateBound(corners[6], corners[2])
    }));

            faces.Add(CurveLoop.Create(new List<Curve>
    {
        Line.CreateBound(corners[3], corners[0]),
        Line.CreateBound(corners[0], corners[4]),
        Line.CreateBound(corners[4], corners[7]),
        Line.CreateBound(corners[7], corners[3])
    }));

            // Tạo solid
            return GeometryCreationUtilities.CreateLoftGeometry(faces, new SolidOptions(ElementId.InvalidElementId, ElementId.InvalidElementId));
        }
    }
}
