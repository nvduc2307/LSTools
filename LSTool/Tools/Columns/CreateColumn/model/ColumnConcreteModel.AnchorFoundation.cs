using Autodesk.Revit.DB.Structure;
using LSTool.Compatibility;
using LSTool.MVVM.model.Structures;
using LSTool.Tools.Generals.ManageConcreteCover.action;

namespace LSTool.Tools.Columns.CreateColumn.model
{
    public partial class ColumnConcreteModel
    {
        /// <summary>
        /// Tìm đối tượng móng (OST_StructuralFoundation) nằm ngay bên dưới cột này.
        /// Sử dụng BoundingBoxIntersectsFilter mở rộng xuống dưới đáy cột.
        /// Trả về null nếu không tìm thấy móng.
        /// </summary>
        /// <param name="document">Revit Document hiện tại.</param>
        public Element FindFoundationBelow(Document document)
        {
            if (Center == null) return null;
            try
            {
                var host = document.GetElement(Id);
                if (host == null) return null;

                var bb = host.get_BoundingBox(null);
                if (bb == null) return null;

                // Mở rộng vùng tìm kiếm: xuống dưới đáy cột 10 m, ngang 50 mm tolerance
                var searchDepth = (10.0 * 1000.0).FromMillimeters(); // 10 000 mm = 10 m
                var tolerance   = 50.0.FromMillimeters();             // 50 mm

                var min = new XYZ(bb.Min.X - tolerance, bb.Min.Y - tolerance, bb.Min.Z - searchDepth);
                var max = new XYZ(bb.Max.X + tolerance, bb.Max.Y + tolerance, bb.Min.Z + tolerance);

                var bbFilter = new BoundingBoxIntersectsFilter(new Outline(min, max));

                var foundations = new FilteredElementCollector(document)
                    .WhereElementIsNotElementType()
                    .WherePasses(bbFilter)
                    .OfCategory(BuiltInCategory.OST_StructuralFoundation)
                    .ToList();

                if (!foundations.Any()) return null;

                // Ưu tiên móng có mép trên (Max.Z) gần đáy cột nhất
                return foundations
                    .OrderByDescending(f =>
                    {
                        var fBb = f.get_BoundingBox(null);
                        return fBb?.Max.Z ?? double.MinValue;
                    })
                    .FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Tính chiều dài neo xuống móng hiệu dụng (feet) cho cột dưới cùng.
        ///
        /// Logic:
        ///   1. Tính chiều dài neo lý thuyết = Ldt × d (mm) hoặc fallback 30d.
        ///   2. Tìm móng bên dưới cột (FindFoundationBelow).
        ///   3. Nếu KHÔNG có móng → trả về giá trị lý thuyết.
        ///   4. Nếu CÓ móng → tính chiều sâu khả dụng (đáy cột → đáy móng, feet).
        ///   5. Nếu chiều sâu khả dụng ≥ lý thuyết → dùng lý thuyết (đủ neo, không cần hết chiều sâu móng).
        ///   6. Nếu chiều sâu khả dụng &lt; lý thuyết  → dùng chiều sâu khả dụng (neo hết phần có thể).
        /// </summary>
        /// <param name="document">Revit Document hiện tại.</param>
        /// <param name="diameterMm">Đường kính thanh thép chính (mm).</param>
        /// <param name="anchorLengthModel">
        ///     Bản ghi <see cref="RebarAnchorageLengthModel"/> tra từ bảng tiêu chuẩn.
        ///     Nếu null sẽ dùng fallback 30d.
        /// </param>
        /// <returns>Chiều dài neo xuống (feet) đã điều chỉnh theo chiều sâu thực tế của móng.</returns>
        public double GetAnchorFoundationFt(
            Document document,
            double diameterMm,
            RebarAnchorageLengthModel anchorLengthModel)
        {
            var diameterFt = diameterMm.FromMillimeters();

            // ── 1. Chiều dài neo lý thuyết (tiêu chuẩn) ─────────────────────
            var theoreticalFt = anchorLengthModel != null
                ? (anchorLengthModel.Ldt * diameterMm).FromMillimeters()  // Ldt × d (mm) → feet
                : 30.0 * diameterFt;                                       // fallback: 30d

            // ── 2. Tìm móng bên dưới cột ──────────────────────────────────────
            var foundation = FindFoundationBelow(document);
            if (foundation == null)
                return theoreticalFt;

            // ── 3. Tính chiều sâu khả dụng trong móng ────────────────────────
            var colElement = document.GetElement(Id);
            var colBb      = colElement?.get_BoundingBox(null);
            if (colBb == null) return theoreticalFt;

            var columnBaseZ     = colBb.Min.Z;               // đáy cột (feet)
            var foundBb         = foundation.get_BoundingBox(null);
            if (foundBb == null) return theoreticalFt;

            var foundationBaseZ = foundBb.Min.Z;             // đáy móng (feet)

            // ── 4. Lấy lớp bảo vệ của móng ──────────────────────────────────
            // Trừ 2 lần cover (trên + dưới) để neo nằm trong vùng cốt thép của móng,
            // không chọc qua lớp bê tông bảo vệ đáy móng.
            var coverAction        = new ManageConcreteCoverAction(document);
            var foundationCoverMm  = coverAction.GetFoundationCover()?.CoverValue ?? 50.0; // mm, fallback 50
            var foundationCoverFt  = foundationCoverMm.FromMillimeters();

            // chiều sâu khả dụng = (đáy cột - đáy móng) - 2 × cover móng
            var availableDepthFt = columnBaseZ - foundationBaseZ - 2.0 * foundationCoverFt;
            if (availableDepthFt <= 0) return theoreticalFt;

            // ── 5. Điều chỉnh chiều dài neo ───────────────────────────────────
            // Nếu chiều sâu khả dụng ≥ lý thuyết → lấy lý thuyết (đủ yêu cầu, không cần hết chiều sâu)
            // Nếu chiều sâu khả dụng <  lý thuyết → lấy chiều sâu khả dụng (neo hết phần có thể)
            return availableDepthFt >= theoreticalFt
                ? theoreticalFt
                : availableDepthFt;
        }
    }
}
