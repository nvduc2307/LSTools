using Autodesk.Revit.DB.Structure;
using LSTool.MVVM.Models;
using LSTool.Tools.Beams.BeamRebar.models;
using LSTool.Tools.Beams.BeamRebar.types;
using Newtonsoft.Json;
using LSTool.Utils;
using System.IO;

namespace LSTool.Tools.Beams.BeamRebar.Utils
{
    public class BeamRebarUtils
    {
        public static void UpdateDiamterToBeamRebarConcreate(List<BeamRebarModel> beams, List<RebarBarType> diameters, double cover)
        {
            var diamterNames = diameters.Select(x => x.Name).ToList();
            foreach (var beam in beams)
            {
                beam.Cover = cover;
                beam.SectionStart.RebarTop1.Diameters = [.. diamterNames];
                beam.SectionStart.RebarTop1.NameChangeAction = _DiameterNameActionChange;

                beam.SectionStart.RebarTop2.Diameters = [.. diamterNames];
                beam.SectionStart.RebarTop2.NameChangeAction = _DiameterNameActionChange;

                beam.SectionStart.RebarTop3.Diameters = [.. diamterNames];
                beam.SectionStart.RebarTop3.NameChangeAction = _DiameterNameActionChange;

                beam.SectionStart.SideBar.Diameters = [.. diamterNames];
                beam.SectionStart.SideBar.NameChangeAction = _DiameterNameActionChange;

                beam.SectionStart.Stirrup.Diameters = [.. diamterNames];
                beam.SectionStart.Stirrup.NameChangeAction = _DiameterNameActionChange;

                beam.SectionStart.RebarBot1.Diameters = [.. diamterNames];
                beam.SectionStart.RebarBot1.NameChangeAction = _DiameterNameActionChange;

                beam.SectionStart.RebarBot2.Diameters = [.. diamterNames];
                beam.SectionStart.RebarBot2.NameChangeAction = _DiameterNameActionChange;

                beam.SectionStart.RebarBot3.Diameters = [.. diamterNames];
                beam.SectionStart.RebarBot3.NameChangeAction = _DiameterNameActionChange;

                beam.SectionMid.RebarTop1.Diameters = [.. diamterNames];
                beam.SectionMid.RebarTop1.NameChangeAction = _DiameterNameActionChange;

                beam.SectionMid.RebarTop2.Diameters = [.. diamterNames];
                beam.SectionMid.RebarTop2.NameChangeAction = _DiameterNameActionChange;

                beam.SectionMid.RebarTop3.Diameters = [.. diamterNames];
                beam.SectionMid.RebarTop3.NameChangeAction = _DiameterNameActionChange;

                beam.SectionMid.SideBar.Diameters = [.. diamterNames];
                beam.SectionMid.SideBar.NameChangeAction = _DiameterNameActionChange;

                beam.SectionMid.Stirrup.Diameters = [.. diamterNames];
                beam.SectionMid.Stirrup.NameChangeAction = _DiameterNameActionChange;

                beam.SectionMid.RebarBot1.Diameters = [.. diamterNames];
                beam.SectionMid.RebarBot1.NameChangeAction = _DiameterNameActionChange;

                beam.SectionMid.RebarBot2.Diameters = [.. diamterNames];
                beam.SectionMid.RebarBot2.NameChangeAction = _DiameterNameActionChange;

                beam.SectionMid.RebarBot3.Diameters = [.. diamterNames];
                beam.SectionMid.RebarBot3.NameChangeAction = _DiameterNameActionChange;

                beam.SectionEnd.RebarTop1.Diameters = [.. diamterNames];
                beam.SectionEnd.RebarTop1.NameChangeAction = _DiameterNameActionChange;

                beam.SectionEnd.RebarTop2.Diameters = [.. diamterNames];
                beam.SectionEnd.RebarTop2.NameChangeAction = _DiameterNameActionChange;

                beam.SectionEnd.RebarTop3.Diameters = [.. diamterNames];
                beam.SectionEnd.RebarTop3.NameChangeAction = _DiameterNameActionChange;

                beam.SectionEnd.SideBar.Diameters = [.. diamterNames];
                beam.SectionEnd.SideBar.NameChangeAction = _DiameterNameActionChange;

                beam.SectionEnd.Stirrup.Diameters = [.. diamterNames];
                beam.SectionEnd.Stirrup.NameChangeAction = _DiameterNameActionChange;

                beam.SectionEnd.RebarBot1.Diameters = [.. diamterNames];
                beam.SectionEnd.RebarBot1.NameChangeAction = _DiameterNameActionChange;

                beam.SectionEnd.RebarBot2.Diameters = [.. diamterNames];
                beam.SectionEnd.RebarBot2.NameChangeAction = _DiameterNameActionChange;

                beam.SectionEnd.RebarBot3.Diameters = [.. diamterNames];
                beam.SectionEnd.RebarBot3.NameChangeAction = _DiameterNameActionChange;
            }
        }

        private static void _DiameterNameActionChange(RebarModel model)
        {
        }

        public static SettingBeamModel GetSettingBeamModel()
        {
            try
            {
                var pathTemplate = $"{PathHelper.FolderDatas}\\SettingBeam.json";
                var path = $"{PathHelper.Appdatas}\\SettingBeam.json";
                if (!File.Exists(path))
                {
                    if (!Directory.Exists(PathHelper.Appdatas))
                        Directory.CreateDirectory(PathHelper.Appdatas);
                    File.Copy(pathTemplate, path);
                }
                return JsonConvert.DeserializeObject<SettingBeamModel>(File.ReadAllText(path))
                       ?? new SettingBeamModel();
            }
            catch (Exception)
            {
                return new SettingBeamModel();
            }
        }

        public static List<RebarBarType> GetDiamters(Document document)
        {
            var diameters = new FilteredElementCollector(document)
                .WhereElementIsElementType()
                .OfClass(typeof(RebarBarType))
                .Cast<RebarBarType>()
                .Where(x => x.Name.Contains("D"))
                .OrderBy(x => x.Name)
                .ToList();
            return diameters;
        }
        public static bool IsDifferentTopElevation(
            BeamRebarModel beam1,
            BeamRebarModel beam2,
            out double differenceMm,
            out bool beam1IsHigher)
        {
            const double mmPerFoot = 304.8;
            var vtz = beam1.VTZ;
            var top1Ft = beam1.Center.DotProduct(vtz)
                         + beam1.Height / mmPerFoot / 2.0;
            var top2Ft = beam2.Center.DotProduct(vtz)
                         + beam2.Height / mmPerFoot / 2.0;

            var diffFt = top1Ft - top2Ft;
            differenceMm = Math.Abs(diffFt) * mmPerFoot;
            beam1IsHigher = diffFt > 0;

            const double toleranceMm = 1.0;
            return differenceMm > toleranceMm;
        }

        public static bool IsDifferentBotElevation(
            BeamRebarModel beam1,
            BeamRebarModel beam2,
            out double differenceMm,
            out bool beam1IsHigher)
        {
            const double mmPerFoot = 304.8;
            var vtz = beam1.VTZ;
            var bot1Ft = beam1.Center.DotProduct(vtz)
                         - beam1.Height / mmPerFoot / 2.0;
            var bot2Ft = beam2.Center.DotProduct(vtz)
                         - beam2.Height / mmPerFoot / 2.0;

            var diffFt = bot1Ft - bot2Ft;
            differenceMm = Math.Abs(diffFt) * mmPerFoot;
            beam1IsHigher = diffFt > 0;

            const double toleranceMm = 1.0;
            return differenceMm > toleranceMm;
        }


        public static bool IsTopLayer(BeamRebarLayerType layer) =>
            layer == BeamRebarLayerType.Top1 || layer == BeamRebarLayerType.Top2 || layer == BeamRebarLayerType.Top3;

        public static int LayerOrder(BeamRebarLayerType layer) => layer switch
        {
            BeamRebarLayerType.Top1 or BeamRebarLayerType.Bot1 => 0,
            BeamRebarLayerType.Top2 or BeamRebarLayerType.Bot2 => 1,
            _ => 2,
        };

        public static RebarModel GetLayerRebar(BeamRebarSectionModel sec, BeamRebarLayerType layer) => layer switch
        {
            BeamRebarLayerType.Top1 => sec.RebarTop1,
            BeamRebarLayerType.Top2 => sec.RebarTop2,
            BeamRebarLayerType.Top3 => sec.RebarTop3,
            BeamRebarLayerType.Bot1 => sec.RebarBot1,
            BeamRebarLayerType.Bot2 => sec.RebarBot2,
            _ => sec.RebarBot3,
        };

        public static int GetLayerQty(BeamRebarSectionModel sec, BeamRebarLayerType layer)
        {
            if (sec == null) return 0;
            var rb = GetLayerRebar(sec, layer);
            var qty = rb?.Spacing ?? 0;
            if (LayerOrder(layer) == 0) qty = Math.Max(2, qty);
            return Math.Max(0, qty);
        }

        public static int GetGridQty(IEnumerable<BeamRebarModel> beams)
        {
            var qtys = new List<int> { 2 };
            foreach (var beam in beams ?? Enumerable.Empty<BeamRebarModel>())
            {
                if (beam == null) continue;
                foreach (var sec in new[] { beam.SectionStart, beam.SectionMid, beam.SectionEnd })
                {
                    if (sec == null) continue;
                    foreach (BeamRebarLayerType layer in Enum.GetValues(typeof(BeamRebarLayerType)))
                        qtys.Add(GetLayerQty(sec, layer));
                }
            }
            return qtys.Max();
        }

        public static Dictionary<int, double> SolveGridPositions(int qty, int gridQty, double usableHalfW)
        {
            var result = new Dictionary<int, double>();
            if (qty <= 0) return result;
            gridQty = Math.Max(gridQty, Math.Max(qty, 2));
            qty = Math.Min(qty, gridQty);

            var start = -usableHalfW;
            var end = usableHalfW;
            var spacing = (end - start) / (gridQty - 1);
            var qtyDu = qty % 2;
            var haft = (qty - qtyDu) / 2;

            for (int i = 0; i < haft; i++)
                result[i + 1] = start + i * spacing;
            if (qtyDu == 1)
            {
                var mid = 1 + gridQty / 2;
                result[mid] = start + (mid - 1) * spacing;
            }
            for (int i = 0; i < haft; i++)
                result[gridQty - i] = end - i * spacing;

            return result.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        }

        public static void GetBeamsLocalCoordinate(
            IList<FamilyInstance> beams,
            out XYZ vtx,
            out XYZ vty,
            out XYZ vtz)
        {
            if (beams == null || !beams.Any())
                throw new ArgumentException("Danh sách dầm rỗng.", nameof(beams));

            const double eps = 1e-6;

            vtz = XYZ.BasisZ;

            var bx = beams[0].GetTransform().BasisX;
            vtx = new XYZ(bx.X, bx.Y, 0);
            if (vtx.GetLength() < eps)
                throw new InvalidOperationException($"Dầm [{beams[0].Id}] có trục thẳng đứng, không hỗ trợ.");
            vtx = vtx.Normalize();

            var flip = Math.Abs(vtx.X) > eps ? vtx.X < 0 : vtx.Y < 0;
            if (flip) vtx = -vtx;

            vty = vtz.CrossProduct(vtx).Normalize();
        }

        public static void GetBeamDimensions(
            FamilyInstance beam,
            XYZ vtx, XYZ vty, XYZ vtz,
            out XYZ center,
            out double width,
            out double height,
            out double length)
        {
            center = null;
            width = 0;
            height = 0;
            length = 0;
            try
            {
                var ps = beam.GetSolid()
                    .Select(s => s.GetPoints())
                    .Aggregate((a, b) => a.Concat(b).ToList())
                    .ToList();

                center = ps.GetCenter();

                var planeCross = Plane.CreateByNormalAndOrigin(vtx, center);
                var planeTop = Plane.CreateByNormalAndOrigin(vty, center);
                var planeSide = Plane.CreateByNormalAndOrigin(vtz, center);

                var psAlongX = ps
                    .Select(p => p.RayIntersectPlane(planeTop.Normal, planeTop))
                    .Select(p => p.RayIntersectPlane(planeSide.Normal, planeSide))
                    .Distinct(new ComparePoint())
                    .OrderBy(p => p.DotProduct(vtx))
                    .ToList();

                var psSection = ps
                    .Select(p => p.RayIntersectPlane(planeCross.Normal, planeCross))
                    .ToList();

                var psAlongY = psSection
                    .Select(p => p.RayIntersectPlane(planeSide.Normal, planeSide))
                    .Distinct(new ComparePoint())
                    .OrderBy(p => p.DotProduct(vty))
                    .ToList();

                var psAlongZ = psSection
                    .Select(p => p.RayIntersectPlane(planeTop.Normal, planeTop))
                    .Distinct(new ComparePoint())
                    .OrderBy(p => p.DotProduct(vtz))
                    .ToList();

                if (!psAlongX.Any() || !psAlongY.Any() || !psAlongZ.Any())
                    throw new InvalidOperationException("Không đủ điểm để tính kích thước.");

                length = Math.Round(psAlongX.First().DistanceTo(psAlongX.Last()).ToMillimeters(), 0);
                width = Math.Round(psAlongY.First().DistanceTo(psAlongY.Last()).ToMillimeters(), 0);
                height = Math.Round(psAlongZ.First().DistanceTo(psAlongZ.Last()).ToMillimeters(), 0);

                if (width < 50) throw new InvalidOperationException($"Width={width} < 50mm.");
                if (height < 50) throw new InvalidOperationException($"Height={height} < 50mm.");
                if (length < 50) throw new InvalidOperationException($"Length={length} < 50mm.");
            }
            catch (Exception)
            {
                center = null;
                width = 0;
                height = 0;
                length = 0;
            }
        }

        public static void GetBeamFaces(
            BeamRebarModel beam,
            out BeamFaceModel fLeft,
            out BeamFaceModel fTop,
            out BeamFaceModel fRight,
            out BeamFaceModel fBot)
        {
            fLeft = null;
            fTop = null;
            fRight = null;
            fBot = null;
            try
            {
                if (beam?.Center == null || beam.VTX == null || beam.VTY == null || beam.VTZ == null)
                    throw new InvalidOperationException("Beam chưa có Center/VTX/VTY/VTZ.");

                var vtx = beam.VTX;
                var vty = beam.VTY;
                var vtz = beam.VTZ;
                var halfL = beam.Length.FromMillimeters() / 2;
                var halfW = beam.Width.FromMillimeters() / 2;
                var halfH = beam.Height.FromMillimeters() / 2;

                var cStart = beam.Center - vtx * halfL;
                var cEnd = beam.Center + vtx * halfL;

                XYZ P(XYZ c, double sy, double sz) => c + vty * (sy * halfW) + vtz * (sz * halfH);

                var p1b = P(cStart, -1, -1);
                var p2b = P(cStart, +1, -1);
                var p3b = P(cStart, +1, +1);
                var p4b = P(cStart, -1, +1);

                var p1t = P(cEnd, -1, -1);
                var p2t = P(cEnd, +1, -1);
                var p3t = P(cEnd, +1, +1);
                var p4t = P(cEnd, -1, +1);

                fLeft = new BeamFaceModel()
                {
                    HostId = beam.Id,
                    FaceType = (int)BeamFaceType.Left,
                    Pb1 = p4b,
                    Pb2 = p1b,
                    Pt1 = p4t,
                    Pt2 = p1t,
                    Plane = Plane.CreateByNormalAndOrigin(-vty, p4b)
                };
                fTop = new BeamFaceModel()
                {
                    HostId = beam.Id,
                    FaceType = (int)BeamFaceType.Top,
                    Pb1 = p3b,
                    Pb2 = p4b,
                    Pt1 = p3t,
                    Pt2 = p4t,
                    Plane = Plane.CreateByNormalAndOrigin(vtz, p3b)
                };
                fRight = new BeamFaceModel()
                {
                    HostId = beam.Id,
                    FaceType = (int)BeamFaceType.Right,
                    Pb1 = p2b,
                    Pb2 = p3b,
                    Pt1 = p2t,
                    Pt2 = p3t,
                    Plane = Plane.CreateByNormalAndOrigin(vty, p2b)
                };
                fBot = new BeamFaceModel()
                {
                    HostId = beam.Id,
                    FaceType = (int)BeamFaceType.Bottom,
                    Pb1 = p1b,
                    Pb2 = p2b,
                    Pt1 = p1t,
                    Pt2 = p2t,
                    Plane = Plane.CreateByNormalAndOrigin(-vtz, p1b)
                };
            }
            catch (Exception)
            {
                fLeft = null;
                fTop = null;
                fRight = null;
                fBot = null;
            }
        }

        public static void GetRebarSetting(
            FamilyInstance beam,
            out RebarModel stirrupStart,
            out RebarModel stirrupMid,
            out RebarModel stirrupEnd,
            out RebarModel top1, out RebarModel top2, out RebarModel top3,
            out RebarModel bot1, out RebarModel bot2, out RebarModel bot3,
            out RebarModel sideBar)
        {
            stirrupStart = new RebarModel { Name = "D10", Diameter = 10, Spacing = 150 };
            stirrupMid = new RebarModel { Name = "D10", Diameter = 10, Spacing = 200 };
            stirrupEnd = new RebarModel { Name = "D10", Diameter = 10, Spacing = 150 };
            top1 = new RebarModel { Name = "D16", Diameter = 16, Spacing = 3 };
            top2 = new RebarModel { Name = "D16", Diameter = 16, Spacing = 0 };
            top3 = new RebarModel { Name = "D16", Diameter = 16, Spacing = 0 };
            bot1 = new RebarModel { Name = "D16", Diameter = 16, Spacing = 3 };
            bot2 = new RebarModel { Name = "D16", Diameter = 16, Spacing = 0 };
            bot3 = new RebarModel { Name = "D16", Diameter = 16, Spacing = 0 };
            sideBar = new RebarModel { Name = "D10", Diameter = 10, Spacing = 1 };
            try
            {
                var pStDia = beam.LookupParameter(BeamRebarParameterName.LS_ST_Diameter);
                var pStSpacStart = beam.LookupParameter(BeamRebarParameterName.LS_ST_Spacing_Start);
                var pStSpacMid = beam.LookupParameter(BeamRebarParameterName.LS_ST_Spacing_Mid);
                var pStSpacEnd = beam.LookupParameter(BeamRebarParameterName.LS_ST_Spacing_End);

                var stDia = pStDia?.AsString() ?? "D10";
                var stSpacStart = pStSpacStart != null ? (int)Math.Round(pStSpacStart.AsDouble().ToMillimeters()) : 150;
                var stSpacMid = pStSpacMid != null ? (int)Math.Round(pStSpacMid.AsDouble().ToMillimeters()) : 200;
                var stSpacEnd = pStSpacEnd != null ? (int)Math.Round(pStSpacEnd.AsDouble().ToMillimeters()) : 150;

                stirrupStart = new RebarModel { Name = stDia, Diameter = ParseDiameter(stDia), Spacing = stSpacStart };
                stirrupMid = new RebarModel { Name = stDia, Diameter = ParseDiameter(stDia), Spacing = stSpacMid };
                stirrupEnd = new RebarModel { Name = stDia, Diameter = ParseDiameter(stDia), Spacing = stSpacEnd };

                top1 = ReadRebarModel(beam, BeamRebarParameterName.LS_TOP1_Diameter, BeamRebarParameterName.LS_TOP1_Count) ?? top1;
                top2 = ReadRebarModel(beam, BeamRebarParameterName.LS_TOP2_Diameter, BeamRebarParameterName.LS_TOP2_Count) ?? top2;
                top3 = ReadRebarModel(beam, BeamRebarParameterName.LS_TOP3_Diameter, BeamRebarParameterName.LS_TOP3_Count) ?? top3;

                bot1 = ReadRebarModel(beam, BeamRebarParameterName.LS_BOT1_Diameter, BeamRebarParameterName.LS_BOT1_Count) ?? bot1;
                bot2 = ReadRebarModel(beam, BeamRebarParameterName.LS_BOT2_Diameter, BeamRebarParameterName.LS_BOT2_Count) ?? bot2;
                bot3 = ReadRebarModel(beam, BeamRebarParameterName.LS_BOT3_Diameter, BeamRebarParameterName.LS_BOT3_Count) ?? bot3;

                sideBar = ReadRebarModel(beam, BeamRebarParameterName.LS_SIDEBAR_Diameter, BeamRebarParameterName.LS_SIDEBAR_Count) ?? sideBar;
            }
            catch (Exception)
            {
            }
        }

        public static RebarModel ReadRebarModel(
            FamilyInstance beam, string diameterParamName, string countParamName)
        {
            try
            {
                var pDia = beam.LookupParameter(diameterParamName);
                var pCount = beam.LookupParameter(countParamName);
                if (pDia == null && pCount == null) return null;

                var diaStr = pDia?.AsString() ?? "D16";
                var count = pCount != null ? (int)Math.Round(pCount.AsDouble()) : 0;
                return new RebarModel
                {
                    Name = diaStr,
                    Diameter = ParseDiameter(diaStr),
                    Spacing = count,
                };
            }
            catch
            {
                return null;
            }
        }

        public static int ParseDiameter(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return 0;
            var digits = new string(name.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out var d) ? d : 0;
        }

        public static BeamBearingModel GetBeamBearing(
            Document document,
            BeamRebarModel beam,
            BeamBearingType bearingType)
        {
            const double mmPerFoot = 304.8;
            const double searchToleranceMm = 500;

            try
            {
                var vtx = beam.VTX;
                var vty = beam.VTY;
                var vtz = beam.VTZ;

                var halfLen = beam.Length / mmPerFoot / 2.0;
                var pStart = beam.Center - vtx * halfLen;
                var pEnd = beam.Center + vtx * halfLen;

                var anchorPt = bearingType == BeamBearingType.Start ? pStart : pEnd;
                var outwardDir = bearingType == BeamBearingType.Start ? -vtx : vtx;

                var endFacePlane = Plane.CreateByNormalAndOrigin(vtx, anchorPt);

                double toleranceFt = searchToleranceMm / mmPerFoot;
                double halfW = beam.Width / mmPerFoot / 2.0 + toleranceFt;
                double halfH = beam.Height / mmPerFoot / 2.0 + toleranceFt;

                var bbMin = anchorPt
                    - vty * halfW
                    - vtz * halfH
                    - outwardDir * toleranceFt;
                var bbMax = anchorPt
                    + vty * halfW
                    + vtz * halfH
                    + outwardDir * toleranceFt;

                var minPt = new XYZ(
                    Math.Min(bbMin.X, bbMax.X),
                    Math.Min(bbMin.Y, bbMax.Y),
                    Math.Min(bbMin.Z, bbMax.Z));
                var maxPt = new XYZ(
                    Math.Max(bbMin.X, bbMax.X),
                    Math.Max(bbMin.Y, bbMax.Y),
                    Math.Max(bbMin.Z, bbMax.Z));

                var outline = new Outline(minPt, maxPt);
                var bbFilter = new BoundingBoxIntersectsFilter(outline);

                var candidates = new FilteredElementCollector(document)
                    .WhereElementIsNotElementType()
                    .WherePasses(bbFilter)
                    .Where(e => e.UniqueId != beam.Id)
                    .Where(e =>
                        e is FamilyInstance fi
                            ? fi.Category.BuiltInCategory is
                                BuiltInCategory.OST_StructuralColumns or
                                BuiltInCategory.OST_Columns or
                                BuiltInCategory.OST_StructuralFraming
                            : e.Category?.BuiltInCategory is
                                BuiltInCategory.OST_Walls)
                    .Where(e => !_isParallelFraming(e, vtx))
                    .ToList();

                if (!candidates.Any()) return null;

                var insideFt = 50.0 / mmPerFoot;
                var rayLenFt = insideFt + toleranceFt + 5000.0 / mmPerFoot;
                var offsetZ = Math.Max(0, beam.Height / 2.0 - 50.0) / mmPerFoot;
                var rayOrigins = new[] { anchorPt, anchorPt + vtz * offsetZ, anchorPt - vtz * offsetZ };
                var options = new SolidCurveIntersectionOptions
                {
                    ResultType = SolidCurveIntersectionMode.CurveSegmentsInside
                };

                BeamBearingModel best = null;
                double bestEntryFt = double.MaxValue;
                int bestPriority = int.MaxValue;
                const double sameEntryFt = 10.0 / mmPerFoot;

                foreach (var candidate in candidates)
                {
                    try
                    {
                        var solids = candidate.GetSolid();
                        if (solids == null || !solids.Any()) continue;

                        var hits = new List<(double entry, double exit)>();
                        foreach (var origin in rayOrigins)
                        {
                            var ray = Line.CreateBound(
                                origin - outwardDir * insideFt,
                                origin + outwardDir * (rayLenFt - insideFt));
                            foreach (var solid in solids)
                            {
                                if (solid == null || solid.Volume <= 0) continue;
                                SolidCurveIntersection res;
                                try { res = solid.IntersectWithCurve(ray, options); }
                                catch { continue; }
                                if (res == null) continue;
                                for (int i = 0; i < res.SegmentCount; i++)
                                {
                                    var seg = res.GetCurveSegment(i);
                                    var d0 = (seg.GetEndPoint(0) - origin).DotProduct(outwardDir);
                                    var d1 = (seg.GetEndPoint(1) - origin).DotProduct(outwardDir);
                                    var entry = Math.Min(d0, d1);
                                    var exit = Math.Max(d0, d1);
                                    if (exit <= 1.0 / mmPerFoot) continue;
                                    if (entry > toleranceFt) continue;
                                    hits.Add((Math.Max(0, entry), exit));
                                }
                            }
                        }
                        if (!hits.Any()) continue;

                        var minEntry = hits.Min(h => h.entry);
                        var thicknessFt = hits
                            .Where(h => h.entry - minEntry <= sameEntryFt)
                            .Max(h => h.exit);

                        var isBeamSupport = candidate is FamilyInstance fiC
                            && fiC.Category.BuiltInCategory == BuiltInCategory.OST_StructuralFraming;
                        var priority = isBeamSupport ? 1 : 0;

                        var better = minEntry < bestEntryFt - sameEntryFt
                            || (Math.Abs(minEntry - bestEntryFt) <= sameEntryFt && priority < bestPriority);
                        if (!better) continue;

                        bestEntryFt = minEntry;
                        bestPriority = priority;
                        best = new BeamBearingModel
                        {
                            Id = candidate.UniqueId,
                            Name = candidate.Name,
                            Thickness = Math.Round(thicknessFt * mmPerFoot, 0),
                            IsBeam = isBeamSupport,
                        };
                    }
                    catch {  }
                }

                return best;
            }
            catch
            {
                return null;
            }
        }

        private static bool _isParallelFraming(Element e, XYZ vtx)
        {
            if (e is not FamilyInstance fi) return false;
            if (fi.Category?.BuiltInCategory != BuiltInCategory.OST_StructuralFraming) return false;
            var bx = fi.GetTransform().BasisX;
            var bxH = new XYZ(bx.X, bx.Y, 0);
            if (bxH.GetLength() < 1e-6) return false;
            return bxH.Normalize().IsParallel(vtx);
        }
    }
}