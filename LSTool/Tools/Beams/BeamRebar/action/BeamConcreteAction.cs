using Autodesk.Revit.UI;
using LSTool.MVVM.Models;
using LSTool.Tools.Beams.BeamRebar.models;
using LSTool.Tools.Beams.BeamRebar.Utils;
using LSTool.Utils;

namespace LSTool.Tools.Beams.BeamRebar.action
{
    public class BeamConcreteAction
    {
        private UIDocument _uidocument;
        private Document _document;
        private readonly double _cover;
        /// <summary>cover: lớp bảo vệ dầm lấy từ setting (ManageConcreteCover); &lt;= 0 → dùng mặc định.</summary>
        public BeamConcreteAction(UIDocument uidocument, double cover = 0)
        {
            _cover = cover > 0 ? cover : BeamRebarModel.COVER;
            _uidocument = uidocument;
            _document = _uidocument.Document;
        }

        public List<FamilyInstance> SelectBeams()
        {
            var elements = _uidocument.Selection.PickElements(_document, null, _beamSelectedFilter);
            var beams = ValidateBeams(elements);
            return beams;
        }

        public List<FamilyInstance> ValidateBeams(List<Element> elements)
        {
            const double maxOffsetMm = 500.0;  // lệch trục tối đa
            const double widthToleranceMm = 1.0; // dung sai chiều rộng

            if (elements == null || !elements.Any())
                throw new Exception("Element is not found");
            if (elements.Any(x => x is not FamilyInstance))
                throw new Exception("Element is not FamilyInstance");

            var beams = elements
                .Select(x => x as FamilyInstance)
                .Where(x => x != null)
                .ToList();

            // ── Hệ tọa độ local chung cho cả nhóm dầm ───────────────────────
            BeamRebarUtils.GetBeamsLocalCoordinate(beams,
                out XYZ vtx, out XYZ vty, out XYZ vtz);

            // Dầm chuẩn = dầm đầu tiên được chọn
            var bf = beams[0];
            var originBf = bf.GetTransform().Origin;
            if (!bf.GetTransform().BasisY.IsParallel(vty))
                throw new Exception($"Dầm chuẩn [{bf.Id}] bị xoay tiết diện, không hỗ trợ.");
            BeamRebarUtils.GetBeamDimensions(bf, vtx, vty, vtz,
                out _, out double widthBf, out _, out _); // widthBf: theo VTY (bề rộng ngang)
            if (widthBf <= 0)
                throw new Exception($"Không đọc được chiều rộng dầm chuẩn [{bf.Id}]");

            // ── kiểm tra từng dầm còn lại ────────────────────────────────────
            foreach (var b in beams.Skip(1))
            {
                var transb = b.GetTransform();

                // 1. Cùng phương trên mặt bằng (song song hoặc ngược chiều)
                var bxB = new XYZ(transb.BasisX.X, transb.BasisX.Y, 0);
                if (bxB.GetLength() < 1e-6 || !bxB.Normalize().IsParallel(vtx))
                    throw new Exception($"Dầm [{b.Id}] không cùng phương với dầm chuẩn [{bf.Id}]");

                // 2. Tiết diện không bị xoay (Cross-Section Rotation):
                //    BasisY của dầm phải nằm ngang, song song VTY chung
                if (!transb.BasisY.IsParallel(vty))
                    throw new Exception($"Dầm [{b.Id}] bị xoay tiết diện khác với dầm chuẩn [{bf.Id}]");

                // 3. Lệch trục ngang (theo VTY) không quá 500 mm
                var v = transb.Origin - originBf;
                var offsetMm = Math.Abs(v.DotProduct(vty)).ToMillimeters();
                if (offsetMm > maxOffsetMm)
                    throw new Exception(
                        $"Dầm [{b.Id}] lệch khỏi trục dầm chuẩn {offsetMm:F0} mm " +
                        $"(cho phép tối đa {maxOffsetMm} mm)");

                // 4. Cùng chiều rộng (đo theo VTY của hệ chung)
                BeamRebarUtils.GetBeamDimensions(b, vtx, vty, vtz,
                    out _, out double widthB, out _, out _);
                if (widthB <= 0)
                    throw new Exception($"Không đọc được chiều rộng dầm [{b.Id}]");
                if (Math.Abs(widthB - widthBf) > widthToleranceMm)
                    throw new Exception(
                        $"Dầm [{b.Id}] có chiều rộng {widthB:F0} mm khác với " +
                        $"dầm chuẩn {widthBf:F0} mm");
            }

            // Sắp xếp theo thứ tự dọc trục VTX chung (từ đầu đến cuối)
            var result = beams
                .OrderBy(x => x.GetTransform().Origin.DotProduct(vtx))
                .ToList();
            return result;
        }

        public List<BeamRebarModel> GetConcreteModels(List<FamilyInstance> objs)
        {
            var results = new List<BeamRebarModel>();
            if (objs == null || !objs.Any()) return results;

            // Hệ tọa độ local CHUNG cho mọi dầm → Start/End, Left/Right, Top/Bot nhất quán
            BeamRebarUtils.GetBeamsLocalCoordinate(objs,
                out XYZ vtx,   // dọc trục dầm
                out XYZ vty,   // hướng ngang
                out XYZ vtz);  // hướng đứng (↑, Z global)

            var beamsSorted = objs
                .OrderBy(x => x.GetTransform().Origin.DotProduct(vtx))
                .ToList();

            foreach (var beam in beamsSorted)
            {
                try
                {
                    var index = beamsSorted.IndexOf(beam) + 1;

                    BeamRebarUtils.GetBeamDimensions(
                        beam, vtx, vty, vtz,
                        out XYZ center,
                        out double width,   // mm – theo VTY (bề rộng)
                        out double height,  // mm – theo VTZ (chiều cao)
                        out double length);

                    if (center == null || width <= 0 || height <= 0 || length <= 0)
                        throw new InvalidOperationException($"Không đọc được kích thước dầm [{beam.Id}].");

                    BeamRebarUtils.GetRebarSetting(beam,
                        out RebarModel stirrupStart,
                        out RebarModel stirrupMid,
                        out RebarModel stirrupEnd,
                        out RebarModel top1, out RebarModel top2, out RebarModel top3,
                        out RebarModel bot1, out RebarModel bot2, out RebarModel bot3,
                        out RebarModel sideBar);

                    // Top1 và Bot1 luôn tối thiểu 2 thanh
                    if (top1 != null) { top1.MinSpacing = 2; top1.Spacing = top1.Spacing; }
                    if (bot1 != null) { bot1.MinSpacing = 2; bot1.Spacing = bot1.Spacing; }

                    // SideBar: tự động tính theo chiều cao dầm (readonly trên UI)
                    if (sideBar != null)
                    {
                        sideBar.Name = "D10";       // mặc định D10
                        sideBar.Diameter = 10;
                        sideBar.Spacing = height >= 700 ? 1 : 0;
                    }

                    var model = new BeamRebarModel
                    {

                        Name = $"Dầm {index}",
                        Id = beam.UniqueId,
                        Cover = _cover,
                        Center = center,
                        VTX = vtx,
                        VTY = vty,
                        VTZ = vtz,
                        Width = width,
                        Height = height,
                        Length = length,
                        SectionStart = new BeamRebarSectionModel
                        {
                            Stirrup = stirrupStart,
                            RebarTop1 = top1,           // shared – sync across sections
                            RebarTop2 = top2,
                            RebarTop3 = top3,
                            RebarBot1 = bot1,           // shared – sync across sections
                            RebarBot2 = bot2,
                            RebarBot3 = bot3,
                            SideBar = sideBar,          // shared – 1 đường kính cho cả dầm
                        },
                        SectionMid = new BeamRebarSectionModel
                        {
                            Stirrup = stirrupMid,
                            RebarTop1 = top1,           // shared – sync across sections
                            RebarTop2 = top2?.Clone(),
                            RebarTop3 = top3?.Clone(),
                            RebarBot1 = bot1,           // shared – sync across sections
                            RebarBot2 = bot2?.Clone(),
                            RebarBot3 = bot3?.Clone(),
                            SideBar = sideBar,
                        },
                        SectionEnd = new BeamRebarSectionModel
                        {
                            Stirrup = stirrupEnd,
                            RebarTop1 = top1,           // shared – sync across sections
                            RebarTop2 = top2?.Clone(),
                            RebarTop3 = top3?.Clone(),
                            RebarBot1 = bot1,           // shared – sync across sections
                            RebarBot2 = bot2?.Clone(),
                            RebarBot3 = bot3?.Clone(),
                            SideBar = sideBar,
                        },

                    };
                    BeamRebarUtils.GetBeamFaces(
                        model,
                        out BeamFaceModel fLeft,
                        out BeamFaceModel fTop,
                        out BeamFaceModel fRight,
                        out BeamFaceModel fBot);
                    if (fLeft == null || fTop == null || fRight == null || fBot == null)
                        throw new InvalidOperationException($"Không lấy được mặt dầm [{beam.Id}].");
                    model.FaceLeft = fLeft;
                    model.FaceTop = fTop;
                    model.FaceRight = fRight;
                    model.FaceBot = fBot;

                    model.BeamBearingStart = BeamRebarUtils.GetBeamBearing(_document, model, BeamBearingType.Start);
                    model.BeamBearingEnd = BeamRebarUtils.GetBeamBearing(_document, model, BeamBearingType.End);
                    results.Add(model);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Lỗi xử lý dầm [{beam.Id}]: {ex.Message}", ex);
                }
            }

            return results;
        }
        private bool _beamSelectedFilter(Element element)
        {
            if (element is not FamilyInstance fa) return false;
            if (fa.Category.BuiltInCategory != BuiltInCategory.OST_StructuralFraming) return false;
            return true;
        }
    }
}
