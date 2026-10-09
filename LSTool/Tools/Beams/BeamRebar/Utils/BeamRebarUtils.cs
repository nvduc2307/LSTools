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
                //start
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

                //mid
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

                //end
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

        /// <summary>
        /// Đọc cài đặt dầm (StressZone, E0) từ SettingBeam.json – giống SettingColumnAction.GetSettingRebarColumnModel.
        /// Lỗi đọc file → trả về giá trị mặc định.
        /// </summary>
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
            // Cao độ mặt top = tâm chiếu lên VTZ (hướng đứng) + nửa chiều cao tiết diện
            // Height = kích thước theo VTZ (mm) → /304.8 để ra feet (đơn vị của Center)
            // Dùng chung VTZ của beam1 để 2 dầm so sánh trên cùng 1 trục
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
            // Cao độ mặt bot = tâm chiếu lên VTZ (hướng đứng) – nửa chiều cao tiết diện
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

        // ── Lớp thép & lưới chia (dùng chung cho mô hình Revit và canvas) ────────

        public static bool IsTopLayer(BeamRebarLayerType layer) =>
            layer == BeamRebarLayerType.Top1 || layer == BeamRebarLayerType.Top2 || layer == BeamRebarLayerType.Top3;

        /// <summary>0 = lớp 1, 1 = lớp 2, 2 = lớp 3.</summary>
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

        /// <summary>Số thanh của lớp (RebarModel.Spacing = qty). Top1/Bot1 tối thiểu 2 thanh.</summary>
        public static int GetLayerQty(BeamRebarSectionModel sec, BeamRebarLayerType layer)
        {
            if (sec == null) return 0;
            var rb = GetLayerRebar(sec, layer);
            var qty = rb?.Spacing ?? 0;
            if (LayerOrder(layer) == 0) qty = Math.Max(2, qty);
            return Math.Max(0, qty);
        }

        /// <summary>GridQty = số thanh lớn nhất của mọi lớp, mọi mặt cắt, mọi nhịp (tối thiểu 2).</summary>
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

        /// <summary>
        /// Chia vị trí thép giống ColumnRebarMainAction.SolvePositionInstallRebar:
        /// khoảng chia = bề rộng / (gridQty − 1); lấy đối xứng từ 2 góc vào, lẻ thì thêm thanh giữa.
        /// Trả về Index (1 → gridQty) → offset theo VTY (âm = phía Left) trong khoảng [-usableHalfW, +usableHalfW].
        /// </summary>
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
                // thanh giữa nằm đúng điểm chia của lưới (lưới chẵn thì lệch tâm 1 nửa bước)
                var mid = 1 + gridQty / 2;
                result[mid] = start + (mid - 1) * spacing;
            }
            for (int i = 0; i < haft; i++)
                result[gridQty - i] = end - i * spacing;

            return result.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        }

        /// <summary>
        /// Tính hệ tọa độ local CHUNG cho một nhóm dầm cùng phương.
        /// Lấy phương theo dầm đầu tiên rồi chuẩn hóa chiều VTX (ưu tiên +X, rồi +Y thế giới)
        /// → không phụ thuộc thứ tự chọn hay dầm bị vẽ ngược chiều.
        ///   VTZ = trục Z global (XYZ.BasisZ) – hướng đứng
        ///   VTX = BasisX của dầm chiếu lên mặt phẳng ngang (dọc trục dầm, nằm ngang)
        ///   VTY = VTZ × VTX (hướng ngang tiết diện, tương đương BasisY)
        /// </summary>
        public static void GetBeamsLocalCoordinate(
            IList<FamilyInstance> beams,
            out XYZ vtx,
            out XYZ vty,
            out XYZ vtz)
        {
            if (beams == null || !beams.Any())
                throw new ArgumentException("Danh sách dầm rỗng.", nameof(beams));

            const double eps = 1e-6;

            // VTZ = trục Z global
            vtz = XYZ.BasisZ;

            // VTX = BasisX chiếu lên mặt phẳng ngang
            var bx = beams[0].GetTransform().BasisX;
            vtx = new XYZ(bx.X, bx.Y, 0);
            if (vtx.GetLength() < eps)
                throw new InvalidOperationException($"Dầm [{beams[0].Id}] có trục thẳng đứng, không hỗ trợ.");
            vtx = vtx.Normalize();

            // Chuẩn hóa chiều VTX
            var flip = Math.Abs(vtx.X) > eps ? vtx.X < 0 : vtx.Y < 0;
            if (flip) vtx = -vtx;

            // VTY theo quy tắc tay phải (X × Y = Z → Y = Z × X, tương đương BasisY)
            vty = vtz.CrossProduct(vtx).Normalize();
        }

        /// <summary>
        /// Đọc kích thước hình học của dầm từ solid vertices,
        /// theo đúng cách của ColumnConcreteAction.GetDistanceColumn.
        /// </summary>
        public static void GetBeamDimensions(
            FamilyInstance beam,
            XYZ vtx, XYZ vty, XYZ vtz,
            out XYZ center,
            out double width,    // mm – theo VTY (bề rộng ngang tiết diện)
            out double height,   // mm – theo VTZ (chiều cao đứng tiết diện)
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
                    .Select(p => p.RayIntersectPlane(planeSide.Normal, planeSide))
                    .Distinct(new ComparePoint())
                    .OrderBy(p => p.DotProduct(vtx))
                    .ToList();

                // Chiều cao / rộng: chiếu lên mặt cắt vuông góc trục dầm
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

        /// <summary>
        /// Lấy 4 mặt bao của dầm (Left / Top / Right / Bot), tương tự
        /// ColumnConcreteAction.GetFaceColumn.
        /// Quy ước trục (giống GetBeamDimensions và các hàm tạo thép):
        ///   VTX = dọc trục dầm, VTY = hướng ngang, VTZ = hướng đứng ↑ (Z global).
        ///   beam.Width  = kích thước theo VTY (bề rộng tiết diện)
        ///   beam.Height = kích thước theo VTZ (chiều cao tiết diện)
        /// Pb1/Pb2 = 2 điểm của mặt tại đầu dầm (Start, -VTX),
        /// Pt1/Pt2 = 2 điểm tương ứng tại cuối dầm (End, +VTX).
        /// Mặt Left nằm phía -VTY, Right phía +VTY (khớp với pTopLeft trong CreateStirrup).
        /// </summary>
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
                var halfL = beam.Length.FromMillimeters() / 2; // VTX
                var halfW = beam.Width.FromMillimeters() / 2;  // VTY (ngang)
                var halfH = beam.Height.FromMillimeters() / 2; // VTZ (đứng)

                var cStart = beam.Center - vtx * halfL;
                var cEnd = beam.Center + vtx * halfL;

                // Tiết diện (nhìn theo +VTX): p1 = dưới-trái, p2 = dưới-phải, p3 = trên-phải, p4 = trên-trái
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
            top1 = new RebarModel { Name = "D16", Diameter = 16, Spacing = 3 };
            top2 = new RebarModel { Name = "D16", Diameter = 16, Spacing = 0 };
            top3 = new RebarModel { Name = "D16", Diameter = 16, Spacing = 0 };
            bot1 = new RebarModel { Name = "D16", Diameter = 16, Spacing = 3 };
            bot2 = new RebarModel { Name = "D16", Diameter = 16, Spacing = 0 };
            bot3 = new RebarModel { Name = "D16", Diameter = 16, Spacing = 0 };
            sideBar = new RebarModel { Name = "D10", Diameter = 10, Spacing = 1 };
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
        /// Tìm đối tượng gối ở đầu hoặc cuối dầm: cột, tường hoặc dầm chính (khi dựng dầm phụ).
        /// Thickness = khoảng cách theo VTX từ mặt đầu/cuối dầm đến mép xa của gối (điểm tia ra khỏi gối).
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
                double halfW = beam.Width / mmPerFoot / 2.0 + toleranceFt;  // VTY (ngang)
                double halfH = beam.Height / mmPerFoot / 2.0 + toleranceFt; // VTZ (đứng)

                // BoundingBox tìm kiếm: quét searchToleranceMm ra ngoài khỏi mặt đầu/cuối
                var bbMin = anchorPt
                    - vty * halfW
                    - vtz * halfH
                    - outwardDir * toleranceFt;  // có thể lấp vào trong dầm 1 chút
                var bbMax = anchorPt
                    + vty * halfW
                    + vtz * halfH
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

                // Lấy các element có thể là gối dầm: cột, tường, dầm chính (dầm khác phương)
                var candidates = new FilteredElementCollector(document)
                    .WhereElementIsNotElementType()
                    .WherePasses(bbFilter)
                    .Where(e => e.UniqueId != beam.Id) // bỏ qua chính dầm đang xét
                    .Where(e =>
                        e is FamilyInstance fi
                            ? fi.Category.BuiltInCategory is
                                BuiltInCategory.OST_StructuralColumns or
                                BuiltInCategory.OST_Columns or
                                BuiltInCategory.OST_StructuralFraming
                            : e.Category?.BuiltInCategory is
                                BuiltInCategory.OST_Walls)
                    .Where(e => !_isParallelFraming(e, vtx)) // dầm cùng phương (nhịp kế, dầm song song) không phải gối
                    .ToList();

                if (!candidates.Any()) return null;

                // Bắn tia từ mặt đầu/cuối dầm theo hướng ra ngoài (tại tâm, gần mặt trên, gần mặt dưới tiết diện)
                // → gối là đối tượng tia gặp đầu tiên; Thickness = từ mặt dầm đến điểm tia ra khỏi gối.
                // Cách này đúng cho cả dầm chính đặt xiên (dầm phụ gác lên dầm chính).
                var insideFt = 50.0 / mmPerFoot;                         // bắt đầu lùi vào trong dầm 50mm
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

                        // (entry, exit) theo outwardDir, tính từ mặt đầu/cuối dầm
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
                                    if (exit <= 1.0 / mmPerFoot) continue;   // nằm hoàn toàn trong dầm
                                    if (entry > toleranceFt) continue;       // cách mặt dầm quá xa
                                    hits.Add((Math.Max(0, entry), exit));
                                }
                            }
                        }
                        if (!hits.Any()) continue;

                        var minEntry = hits.Min(h => h.entry);
                        var thicknessFt = hits
                            .Where(h => h.entry - minEntry <= sameEntryFt)
                            .Max(h => h.exit);

                        // ưu tiên cột / tường hơn dầm khi cùng khoảng cách
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
                    catch { /* bỏ qua nếu solid không lấy được */ }
                }

                return best;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Dầm (StructuralFraming) cùng phương với trục dầm đang xét → không coi là gối.</summary>
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