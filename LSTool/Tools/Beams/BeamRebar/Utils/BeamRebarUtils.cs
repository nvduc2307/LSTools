using Autodesk.Revit.DB.Structure;
using LSTool.MVVM.Models;
using LSTool.Tools.Beams.BeamRebar.models;
using LSTool.Utils;

namespace LSTool.Tools.Beams.BeamRebar.Utils
{
    public class BeamRebarUtils
    {
        public static void UpdateDiamterToBeamRebarConcreate(BeamRebarModel beam, List<RebarBarType> diameters)
        {
            var diamterNames = diameters.Select(x=>x.Name).ToList();
            beam.SectionStart.RebarTop1.Diameters = [.. diamterNames];
            beam.SectionStart.RebarTop1.NameChangeAction = _DiameterNameActionChange;
        }

        private static void _DiameterNameActionChange(RebarModel model)
        {
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
        /// <summary>
        /// Kiểm tra xem 2 dầm liên tiếp có chênh cao độ mặt trên (top face) không.
        /// </summary>
        /// <param name="beam1">Dầm thứ nhất.</param>
        /// <param name="beam2">Dầm thứ hai.</param>
        /// <param name="differenceMm">Độ chênh lệch cao độ tính bằng mm.</param>
        /// <param name="beam1IsHigher">true nếu dầm 1 cao hơn dầm 2.</param>
        /// <returns>true nếu có chênh cao độ (> 1 mm), false nếu bằng nhau.</returns>
        public static bool IsDifferentTopElevation(
            BeamRebarModel beam1,
            BeamRebarModel beam2,
            out double differenceMm,
            out bool beam1IsHigher)
        {
            // Cao độ mặt top = tâm chiếu lên VTZ + nửa chiều cao tiết diện
            // VTZ ≈ BasisZ của transform dầm (hướng thẳng đứng với dầm nằm ngang)
            // Height lưu theo mm → /304.8 để ra feet (đơn vị của Center)
            const double mmPerFoot = 304.8;
            var top1Ft = beam1.Center.DotProduct(beam1.VTZ)
                         + beam1.Height / mmPerFoot / 2.0;
            var top2Ft = beam2.Center.DotProduct(beam2.VTZ)
                         + beam2.Height / mmPerFoot / 2.0;

            var diffFt = top1Ft - top2Ft;
            differenceMm = Math.Abs(diffFt) * mmPerFoot;
            beam1IsHigher = diffFt > 0;

            const double toleranceMm = 1.0;
            return differenceMm > toleranceMm;
        }

        /// <summary>
        /// Kiểm tra xem 2 dầm liên tiếp có chênh cao độ mặt dưới (bot face) không.
        /// </summary>
        /// <param name="beam1">Dầm thứ nhất.</param>
        /// <param name="beam2">Dầm thứ hai.</param>
        /// <param name="differenceMm">Độ chênh lệch cao độ tính bằng mm.</param>
        /// <param name="beam1IsHigher">true nếu đáy dầm 1 cao hơn đáy dầm 2.</param>
        /// <returns>true nếu có chênh cao độ (> 1 mm), false nếu bằng nhau.</returns>
        public static bool IsDifferentBotElevation(
            BeamRebarModel beam1,
            BeamRebarModel beam2,
            out double differenceMm,
            out bool beam1IsHigher)
        {
            // Cao độ mặt bot = tâm chiếu lên VTZ – nửa chiều cao tiết diện
            const double mmPerFoot = 304.8;
            var bot1Ft = beam1.Center.DotProduct(beam1.VTZ)
                         - beam1.Height / mmPerFoot / 2.0;
            var bot2Ft = beam2.Center.DotProduct(beam2.VTZ)
                         - beam2.Height / mmPerFoot / 2.0;

            var diffFt = bot1Ft - bot2Ft;
            differenceMm = Math.Abs(diffFt) * mmPerFoot;
            beam1IsHigher = diffFt > 0;

            const double toleranceMm = 1.0;
            return differenceMm > toleranceMm;
        }

        /// <summary>
        /// Đọc kích thước hình học của dầm từ solid vertices,
        /// theo đúng cách của ColumnConcreteAction.GetDistanceColumn.
        /// </summary>
        public static void GetBeamDimensions(
            FamilyInstance beam,
            XYZ vtx, XYZ vty, XYZ vtz,
            out XYZ center,
            out double width,    // mm – theo VTY (chiều cao tiết diện)
            out double height,   // mm – theo VTZ (chiều rộng tiết diện)
            out double length)   // mm – theo VTX (chiều dài dầm)
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

                var planeCross = Plane.CreateByNormalAndOrigin(vtx, center); // mặt phẳng vuông góc dầm
                var planeTop = Plane.CreateByNormalAndOrigin(vty, center);
                var planeSide = Plane.CreateByNormalAndOrigin(vtz, center);

                // Chiều dài: chiếu điểm lên mặt phẳng ngang → project theo VTX
                var psAlongX = ps
                    .Select(p => p.RayIntersectPlane(planeTop.Normal, planeTop))
                    .Distinct(new ComparePoint())
                    .OrderBy(p => p.DotProduct(vtx))
                    .ToList();

                // Chiều cao / rộng: chiếu lên mặt cắt vuông góc trục dầm
                var psSection = ps
                    .Select(p => p.RayIntersectPlane(planeCross.Normal, planeCross))
                    .ToList();

                var psAlongY = psSection
                    .Distinct(new ComparePoint())
                    .OrderBy(p => p.DotProduct(vty))
                    .ToList();

                var psAlongZ = psSection
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

        /// <summary>
        /// Đọc các thông số thép từ Revit shared parameter của dầm.
        /// Nếu không tìm thấy hoặc giá trị không hợp lệ sẽ trả về default.
        /// </summary>
        public static void GetRebarSetting(
            FamilyInstance beam,
            out RebarModel stirrupStart,
            out RebarModel stirrupMid,
            out RebarModel stirrupEnd,
            out RebarModel top1, out RebarModel top2, out RebarModel top3,
            out RebarModel bot1, out RebarModel bot2, out RebarModel bot3,
            out RebarModel sideBar)
        {
            // Giá trị mặc định
            stirrupStart = new RebarModel { Name = "D10", Diameter = 10, Spacing = 150 };
            stirrupMid = new RebarModel { Name = "D10", Diameter = 10, Spacing = 200 };
            stirrupEnd = new RebarModel { Name = "D10", Diameter = 10, Spacing = 150 };
            top1 = new RebarModel { Name = "D16", Diameter = 16 };
            top2 = new RebarModel { Name = "D16", Diameter = 16 };
            top3 = new RebarModel { Name = "D16", Diameter = 16 };
            bot1 = new RebarModel { Name = "D16", Diameter = 16 };
            bot2 = new RebarModel { Name = "D16", Diameter = 16 };
            bot3 = new RebarModel { Name = "D16", Diameter = 16 };
            sideBar = new RebarModel { Name = "D12", Diameter = 12 };
            try
            {
                // ── Đai ──────────────────────────────────────────────────────
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

                // ── Thép trên ────────────────────────────────────────────────
                top1 = ReadRebarModel(beam, BeamRebarParameterName.LS_TOP1_Diameter, BeamRebarParameterName.LS_TOP1_Count) ?? top1;
                top2 = ReadRebarModel(beam, BeamRebarParameterName.LS_TOP2_Diameter, BeamRebarParameterName.LS_TOP2_Count) ?? top2;
                top3 = ReadRebarModel(beam, BeamRebarParameterName.LS_TOP3_Diameter, BeamRebarParameterName.LS_TOP3_Count) ?? top3;

                // ── Thép dưới ────────────────────────────────────────────────
                bot1 = ReadRebarModel(beam, BeamRebarParameterName.LS_BOT1_Diameter, BeamRebarParameterName.LS_BOT1_Count) ?? bot1;
                bot2 = ReadRebarModel(beam, BeamRebarParameterName.LS_BOT2_Diameter, BeamRebarParameterName.LS_BOT2_Count) ?? bot2;
                bot3 = ReadRebarModel(beam, BeamRebarParameterName.LS_BOT3_Diameter, BeamRebarParameterName.LS_BOT3_Count) ?? bot3;

                // ── Thép hông ─────────────────────────────────────────────────
                sideBar = ReadRebarModel(beam, BeamRebarParameterName.LS_SIDEBAR_Diameter, BeamRebarParameterName.LS_SIDEBAR_Count) ?? sideBar;
            }
            catch (Exception)
            {
                // Giữ nguyên giá trị mặc định nếu đọc parameter thất bại
            }
        }

        /// <summary>Đọc 1 nhóm thép (đường kính + số lượng) từ parameter.</summary>
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
                    Spacing = count, // RebarModel.Spacing tái dụng lưu số lượng (qty)
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Tách số từ tên đường kính. VD: "D16" → 16.</summary>
        public static int ParseDiameter(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return 0;
            var digits = new string(name.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out var d) ? d : 0;
        }

        /// <summary>
        /// Tìm đối tượng gối dầm (cột / tường) gần nhất ở đầu hoặc cuối dầm.
        /// Thickness = khoảng cách từ mặt đầu/cuối dầm đến điểm ngoài cùng của đối tượng gối, chiếu theo VTX.
        /// </summary>
        public static BeamBearingModel GetBeamBearing(
            Document document,
            BeamRebarModel beam,
            BeamBearingType bearingType)
        {
            const double mmPerFoot = 304.8;
            const double searchToleranceMm = 500; // mở rộng vùng tìm kiếm

            try
            {
                var vtx = beam.VTX;  // hướng trục dầm
                var vty = beam.VTY;
                var vtz = beam.VTZ;

                // Điểm đầu và cuối dầm (cạnh giữa tiết diện)
                var halfLen = beam.Length / mmPerFoot / 2.0;
                var pStart = beam.Center - vtx * halfLen;  // điểm đầu dầm
                var pEnd = beam.Center + vtx * halfLen;  // điểm cuối dầm

                // Điểm neo và hướng chiếu theo loại gối
                var anchorPt = bearingType == BeamBearingType.Start ? pStart : pEnd;
                var outwardDir = bearingType == BeamBearingType.Start ? -vtx : vtx; // hướng ra ngoài khỏi dầm

                // Mặt phẳng tại mặt đầu/cuối dầm (pháp tuyến = VTX)
                var endFacePlane = Plane.CreateByNormalAndOrigin(vtx, anchorPt);

                // Độ mở rộng tìm kiếm theo phương ngang và dọc
                double toleranceFt = searchToleranceMm / mmPerFoot;
                double halfW = beam.Width / mmPerFoot / 2.0 + toleranceFt;
                double halfH = beam.Height / mmPerFoot / 2.0 + toleranceFt;

                // BoundingBox tìm kiếm: quét searchToleranceMm ra ngoài khỏi mặt đầu/cuối
                var bbMin = anchorPt
                    - vty * halfH
                    - vtz * halfW
                    - outwardDir * toleranceFt;  // có thể lấp vào trong dầm 1 chút
                var bbMax = anchorPt
                    + vty * halfH
                    + vtz * halfW
                    + outwardDir * toleranceFt;

                // Normalize bb (min < max từng trục)
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

                // Lấy các element có thể là gối dầm (cột + tường + dầm)
                var candidates = new FilteredElementCollector(document)
                    .WhereElementIsNotElementType()
                    .WherePasses(bbFilter)
                    .Where(e =>
                        e is FamilyInstance fi
                            ? fi.Category.BuiltInCategory is
                                BuiltInCategory.OST_StructuralColumns or
                                BuiltInCategory.OST_Columns or
                                BuiltInCategory.OST_StructuralFraming
                            : e.Category?.BuiltInCategory is
                                BuiltInCategory.OST_Walls)
                    .ToList();

                if (!candidates.Any()) return null;

                // Với mỗi candidate: tính khoảng cách từ anchorPt đến điểm gần nhất
                // chiếu theo VTX, chọn element gần nhất
                BeamBearingModel best = null;
                double bestDistFt = double.MaxValue;

                foreach (var candidate in candidates)
                {
                    try
                    {
                        // Lấy tất cả đỉnh của solid candidate
                        var pts = candidate.GetSolid()
                            .Select(s => s.GetPoints())
                            .Aggregate((a, b) => a.Concat(b).ToList())
                            .ToList();
                        if (!pts.Any()) continue;

                        // Chiếu tất cả đỉnh lên trục VTX
                        // Điểm gần nhất với anchorPt theo hướng trục dầm
                        var projAnchor = anchorPt.DotProduct(vtx);

                        // Hướng ra ngoài: điểm ngoài cùng theo outwardDir
                        // (max theo DotProduct(outwardDir))
                        var projectedPts = pts
                            .Select(p => p.DotProduct(vtx))
                            .ToList();

                        // Điểm gần mặt đầu/cuối dầm nhất = điểm có DotProduct(VTX) gần projAnchor nhất
                        var nearestProj = projectedPts
                            .OrderBy(v => Math.Abs(v - projAnchor))
                            .FirstOrDefault();

                        var distFt = Math.Abs(nearestProj - projAnchor);
                        if (distFt >= bestDistFt) continue;

                        // Điểm ngoài cùng theo outwardDir (xa nhất khỏi tâm dầm)
                        var outermostProj = bearingType == BeamBearingType.Start
                            ? projectedPts.Min()   // Start: outward = -VTX → nhỏ nhất
                            : projectedPts.Max();  // End:   outward = +VTX → lớn nhất

                        // Thickness = khoảng từ mặt đầu/cuối dầm đến điểm ngoài cùng của gối
                        var thicknessFt = Math.Abs(outermostProj - projAnchor);

                        bestDistFt = distFt;
                        best = new BeamBearingModel
                        {
                            Id = candidate.UniqueId,
                            Name = candidate.Name,
                            Thickness = Math.Round(thicknessFt * mmPerFoot, 0),
                        };
                    }
                    catch { /* bỏ qua nếu solid không lấy được */ }
                }

                return best;
            }
            catch
            {
                return null;
            }
        }
    }
}