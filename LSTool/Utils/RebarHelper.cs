using Autodesk.Revit.DB.Structure;
using LSTool.Compatibility;

namespace LSTool.Utils
{
    public class RebarHelper
    {
        public static Element CreateRebarHost(
        Document document,
        BuiltInCategory builtInCategory = BuiltInCategory.OST_StructuralFoundation)
        {
            return DirectShape.CreateElement(document, new ElementId(builtInCategory));
        }
        public static void SetSolidRebar3DView(Rebar rebar, Autodesk.Revit.DB.View view)
        {
            if (view is View3D view3d)
            {
                if (rebar != null)
                {
#if REVIT2021 || REVIT2022
                    rebar.SetSolidInView(view3d, true);
#endif
                    rebar.SetUnobscuredInView(view3d, true);
                }
            }
        }
        public static List<Curve> GenerateShape(List<Curve> shape)
        {
            var ps = shape.Select(x => x.GetEndPoint(0));
            var qty = shape.Count;
            if (qty == 1) return shape;
            for (int i = 0; i < qty - 1; i++)
            {
                var cur1 = shape[i];
                var cur2 = shape[i + 1];
                if (!cur1.Direction().IsParallel(cur2.Direction()))
                    continue;
                var cn = Line.CreateBound(cur1.GetEndPoint(0), cur2.GetEndPoint(1));
                shape.Insert(i, cn);
                shape.Remove(shape[i + 1]);
                shape.Remove(shape[i + 1]);
                qty = shape.Count;
                i--;
            }
            return shape;
        }
        public static bool IsRebarFreeForm(List<Curve> shape, out XYZ normal)
        {
            normal = null;
            var result = true;
            if (!shape.Any()) return result;
            var dirFir = shape[0].Direction();
            var dirs = shape
                .Select(x => x.Direction())
                .GroupBy(x => x.IsParallel(dirFir))
                .Select(x => x.ToList())
                .ToList();
            var qty = dirs.Count;
            if (qty == 1)
            {
                result = false;
                var dir = shape[0].Direction();
                normal = dir.IsParallel(XYZ.BasisZ)
                    ? dir.CrossProduct(XYZ.BasisX)
                    : dir.CrossProduct(XYZ.BasisZ);
            }
            else
            {
                var dir1 = dirs[0].FirstOrDefault();
                var dir2 = dirs[1].FirstOrDefault();
                if (dir1 == null) return result;
                if (dir2 == null) return result;
                normal = dir1.CrossProduct(dir2);
                var ps = shape.Select(x => x.GetEndPoint(0)).ToList();
                var plane = Plane.CreateByNormalAndOrigin(normal, ps[0]);
                if (ps.Any(x => x.RayIntersectPlane(plane.Normal, plane).DistanceTo(x).FootToMm() > 5))
                {
                    normal = null;
                    result = true;
                }
                else
                {
                    normal = dir1.CrossProduct(dir2);
                    result = false;
                }
            }
            return result;
        }
        public static void CreateRebar(
            Document document,
            List<Curve> shape,
            string rebarName,
            string meshName,
            List<RebarBarType> rebarBarTypes,
            Element host)
        {
            var cl = new CurveLoop();
            foreach (var c in shape)
            {
                cl.Append(c);
            }
#if REVIT2025 || REVIT2024 || REVIT2023 || REVIT2022 || REVIT2021
            var rebar = Rebar.CreateFreeForm(
                document,
                rebarBarTypes.FirstOrDefault(x => x.Name == rebarName),
                host,
                new List<CurveLoop>() { cl },
                out RebarFreeFormValidationResult validationResult);
#else
            var rebar = Rebar.CreateFreeForm(
                document,
                rebarBarTypes.FirstOrDefault(x => x.Name == rebarName),
                host,
                new List<CurveLoop>() { cl },
                RebarStyle.Standard);
            
#endif
        }
        public static void CreateRebar(
            Document document,
            List<Curve> shape,
            string rebarName,
            XYZ normal,
            List<RebarBarType> rebarBarTypes,
            Element host)
        {
            shape = GenerateShape(shape);
#if REVIT2025 || REVIT2024 || REVIT2023 || REVIT2022 || REVIT2021
            var rebar = Rebar.CreateFromCurves(
                document,
                RebarStyle.Standard,
                rebarBarTypes.FirstOrDefault(x => x.Name == rebarName),
                null,
                null,
                host,
                normal,
                shape,
                RebarHookOrientation.Right,
                RebarHookOrientation.Right,
                true, true);
            SetSolidRebar3DView(rebar, document.ActiveView);
#else
            var options = new BarTerminationsData(document);
            options.HookTypeIdAtStart = new ElementId(-1);
            options.HookTypeIdAtEnd = new ElementId(-1);
            var rebar = Rebar.CreateFromCurves(
                document,
                RebarStyle.Standard,
                rebarBarTypes.FirstOrDefault(x => x.Name == rebarName),
                host,
                normal,
                shape,
                options,
                true, true);
            SetSolidRebar3DView(rebar, document.ActiveView);
            if (rebar == null)
            {
                CreateRebar(
                document,
                shape,
                rebarName,
                "A",
                rebarBarTypes,
                host);
            }
#endif
        }
        public static Rebar CreateRebarStirrupTie(
            Document document,
            List<Curve> shape,
            string rebarName,
            XYZ normal,
            RebarHookType hookStart,
            RebarHookType hookend,
            List<RebarBarType> rebarBarTypes,
            Element host,
            RebarHookOrientation hookOrientStart = RebarHookOrientation.Right,
            RebarHookOrientation hookOrientEnd = RebarHookOrientation.Right,
            double hookLengthMm = 0)
        {
            var barType = rebarBarTypes.FirstOrDefault(x => x.Name == rebarName)
                ?? rebarBarTypes.FirstOrDefault(x => x.Name.Contains(rebarName))
                ?? rebarBarTypes.FirstOrDefault();
            if (barType == null || shape == null || !shape.Any()) return null;

#if REVIT2025 || REVIT2024 || REVIT2023 || REVIT2022 || REVIT2021
            var rebar = Rebar.CreateFromCurves(
                document,
                RebarStyle.StirrupTie,
                barType,
                hookStart,
                hookend,
                host,
                normal,
                shape,
                hookOrientStart,
                hookOrientEnd,
                true, true);
            SetSolidRebar3DView(rebar, document.ActiveView);
            if (rebar != null && hookLengthMm > 0)
                SetRebarHookLength(document, rebar, hookLengthMm);
            return rebar;
#else
            var options = new BarTerminationsData(document);
            if (hookStart != null) options.HookTypeIdAtStart = hookStart.Id;
            if (hookend != null) options.HookTypeIdAtEnd = hookend.Id;
            options.TerminationOrientationAtStart = hookOrientStart == RebarHookOrientation.Right
                ? RebarTerminationOrientation.Right
                : RebarTerminationOrientation.Left;
            options.TerminationOrientationAtEnd = hookOrientEnd == RebarHookOrientation.Right 
                ? RebarTerminationOrientation.Right
                : RebarTerminationOrientation.Left;
            var rebar = Rebar.CreateFromCurves(
                document,
                RebarStyle.StirrupTie,
                barType,
                host,
                normal,
                shape,
                options,
                true, true);
            SetSolidRebar3DView(rebar, document.ActiveView);
            if (rebar != null && hookLengthMm > 0)
                SetRebarHookLength(document, rebar, hookLengthMm);
            return rebar;
#endif
        }

        public static void SetRebarHookLength(Document document, Rebar rebar, double hookLengthMm)
        {
            if (rebar == null || hookLengthMm <= 0) return;
            try
            {
                rebar.EnableHookLengthOverride(true);
                if (rebar.IsHookLengthOverrideEnabled())
                {
                    rebar.GetOverridableHookParameters(out _, out var startHookParams, out _, out var endHookParams);
                    var lenFeet = hookLengthMm.FromMillimeters();

                    if (startHookParams != null)
                    {
                        foreach (var paramId in startHookParams)
                        {
                            SetParameterValue(document, rebar, paramId, lenFeet);
                        }
                    }
                    if (endHookParams != null)
                    {
                        foreach (var paramId in endHookParams)
                        {
                            SetParameterValue(document, rebar, paramId, lenFeet);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetRebarHookLength error: {ex}");
            }
        }

        private static void SetParameterValue(Document document, Rebar rebar, ElementId paramId, double value)
        {
            if (paramId == null || paramId == ElementId.InvalidElementId) return;
            var elem = document.GetElement(paramId);
            if (elem is ParameterElement pe)
            {
                var p = rebar.get_Parameter(pe.GetDefinition());
                if (p != null && !p.IsReadOnly)
                {
                    p.Set(value);
                    return;
                }
            }
            if (elem != null && !string.IsNullOrEmpty(elem.Name))
            {
                var p = rebar.LookupParameter(elem.Name);
                if (p != null && !p.IsReadOnly)
                {
                    p.Set(value);
                }
            }
        }

        /// <summary>
        /// Tao 1 dai stirrup roi dung SetLayoutAsMaximumSpacing de Revit chia deu thanh RebarSet.
        /// Moi zone (Start/Mid/End) goi 1 lan -> tao 3 RebarSet cho 1 nhip dam.
        /// </summary>
        public static void CreateRebarStirrupTieSet(
            Document document,
            List<Curve> shape,
            string rebarName,
            XYZ normal,
            RebarHookType hookStart,
            RebarHookType hookEnd,
            List<RebarBarType> rebarBarTypes,
            Element host,
            double spacingMm,
            double zoneLengthMm)
        {
            if (spacingMm <= 0 || zoneLengthMm <= 0) return;
#if REVIT2025 || REVIT2024 || REVIT2023 || REVIT2022 || REVIT2021
            var rebar = Rebar.CreateFromCurves(
                document, RebarStyle.StirrupTie,
                rebarBarTypes.FirstOrDefault(x => x.Name == rebarName),
                hookStart, hookEnd,
                host, normal, shape,
                RebarHookOrientation.Right,
                RebarHookOrientation.Right,
                true, true);
            rebar.GetShapeDrivenAccessor().SetLayoutAsMaximumSpacing(
                spacingMm.FromMillimeters(),
                zoneLengthMm.FromMillimeters(),
                true,
                true,
                true);
                SetSolidRebar3DView(rebar, document.ActiveView);
#else
            var options = new BarTerminationsData(document);
            options.HookTypeIdAtStart = hookStart.Id;
            options.HookTypeIdAtEnd = hookEnd.Id;
            var rebar = Rebar.CreateFromCurves(
                document, RebarStyle.StirrupTie,
                rebarBarTypes.FirstOrDefault(x => x.Name == rebarName),
                host, normal, shape, options, true, true);
            rebar.GetShapeDrivenAccessor().SetLayoutAsMaximumSpacing(
                spacingMm.FromMillimeters(),
                zoneLengthMm.FromMillimeters(),
                true, true, true);
            SetSolidRebar3DView(rebar, document.ActiveView);
#endif
        }

        /// <summary>
        /// Tạo đai phụ (StirrupTie) có hook + hướng hook + chiều dài hook rồi dùng SetLayoutAsMaximumSpacing
        /// để Revit chia đều thành RebarSet theo hướng normal (giống CreateRebarStirrupTieSet nhưng đủ tuỳ chọn như CreateRebarStirrupTie).
        /// Trả về null nếu tạo lỗi (thanh đã tạo dở được xoá).
        /// </summary>
        public static Rebar CreateRebarStirrupTieSetEx(
            Document document,
            List<Curve> shape,
            string rebarName,
            XYZ normal,
            RebarHookType hookStart,
            RebarHookType hookEnd,
            RebarHookOrientation hookOrientStart,
            RebarHookOrientation hookOrientEnd,
            List<RebarBarType> rebarBarTypes,
            Element host,
            double spacingMm,
            double zoneLengthMm,
            double hookLengthMm,
            out string error)
        {
            error = null;
            Rebar rebar = null;
            try
            {
                if (spacingMm <= 0 || zoneLengthMm <= 0) { error = "spacing / zone length <= 0"; return null; }
                rebar = CreateRebarStirrupTie(
                    document, shape, rebarName, normal, hookStart, hookEnd,
                    rebarBarTypes, host, hookOrientStart, hookOrientEnd, hookLengthMm);
                if (rebar == null) { error = "Rebar.CreateFromCurves trả về null"; return null; }
                rebar.GetShapeDrivenAccessor().SetLayoutAsMaximumSpacing(
                    spacingMm.FromMillimeters(),
                    zoneLengthMm.FromMillimeters(),
                    true, true, true);
                return rebar;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                try
                {
                    if (rebar != null && rebar.IsValidObject)
                        document.Delete(rebar.Id);
                }
                catch (Exception) { }
                return null;
            }
        }

        /// <summary>
        /// Tạo Rebar Set (Standard, không hook) layout Fixed Number:
        /// shape = thanh đầu tiên (phía âm của normal), normal = phương rải (vuông góc mặt phẳng thanh),
        /// qty thanh rải về phía +normal trên chiều dài arrayLengthFt (bao gồm thanh đầu và thanh cuối).
        /// Trả về null nếu tạo lỗi (để caller fallback sang thanh lẻ).
        /// </summary>
        public static Rebar CreateRebarFixedNumberSet(
            Document document,
            List<Curve> shape,
            string rebarName,
            XYZ normal,
            List<RebarBarType> rebarBarTypes,
            Element host,
            int qty,
            double arrayLengthFt,
            out string error)
        {
            error = null;
            Rebar rebar = null;
            try
            {
                if (qty <= 0 || shape == null || !shape.Any()) { error = "shape rỗng"; return null; }
                shape = GenerateShape(shape);
                var barType = rebarBarTypes.FirstOrDefault(x => x.Name == rebarName);
                if (barType == null) { error = $"không tìm thấy RebarBarType {rebarName}"; return null; }

#if REVIT2025 || REVIT2024 || REVIT2023 || REVIT2022 || REVIT2021
                rebar = Rebar.CreateFromCurves(
                    document, RebarStyle.Standard,
                    barType,
                    null, null,
                    host, normal, shape,
                    RebarHookOrientation.Right,
                    RebarHookOrientation.Right,
                    true, true);
#else
                var options = new BarTerminationsData(document);
                options.HookTypeIdAtStart = new ElementId(-1);
                options.HookTypeIdAtEnd   = new ElementId(-1);
                rebar = Rebar.CreateFromCurves(
                    document, RebarStyle.Standard,
                    barType, host, normal, shape, options, true, true);
#endif
                if (rebar == null) { error = "Rebar.CreateFromCurves trả về null"; return null; }
                if (qty > 1)
                {
                    var accessor = rebar.GetShapeDrivenAccessor();
                    try
                    {
                        // barsOnNormalSide = true, includeFirstBar = true, includeLastBar = true
                        accessor.SetLayoutAsFixedNumber(qty, arrayLengthFt, true, true, true);
                    }
                    catch (Exception)
                    {
                        // dự phòng: layout theo khoảng cách thay vì chiều dài mảng
                        accessor.SetLayoutAsNumberWithSpacing(qty, arrayLengthFt / (qty - 1), true, true, true);
                    }
                }
                SetSolidRebar3DView(rebar, document.ActiveView);
                return rebar;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                // xoá thanh đã tạo dở để caller fallback sang thanh lẻ mà không bị trùng
                try
                {
                    if (rebar != null && rebar.IsValidObject)
                        document.Delete(rebar.Id);
                }
                catch (Exception) { }
                return null;
            }
        }

        /// <summary>
        /// Tạo 1 rebar Standard (không hook) rồi dùng SetLayoutAsFixedNumber
        /// để Revit phân bổ thành RebarSet theo hướng normal.
        /// shape    = shape của thanh đầu tiên (leftmost/bottommost).
        /// normal   = hướng phân bổ (normal cho top/bot bars phân bổ theo chiều ngang).
        /// qty      = số thanh; spacingFt = khoảng cách tâm–tâm giữa các thanh (feet).
        /// </summary>
        public static void CreateRebarStandardSet(
            Document document,
            List<Curve> shape,
            string rebarName,
            XYZ normal,
            List<RebarBarType> rebarBarTypes,
            Element host,
            int qty,
            double lengthArr)
        {
            if (qty <= 0) return;
            shape = GenerateShape(shape);
            var barType = rebarBarTypes.FirstOrDefault(x => x.Name == rebarName);
            if (barType == null) return;

#if REVIT2025 || REVIT2024 || REVIT2023 || REVIT2022 || REVIT2021
            var rebar = Rebar.CreateFromCurves(
                document, RebarStyle.Standard,
                barType,
                null, null,
                host, normal, shape,
                RebarHookOrientation.Right,
                RebarHookOrientation.Right,
                true, true);

            if (qty > 1)
                rebar.GetShapeDrivenAccessor()
                     .SetLayoutAsFixedNumber(qty, lengthArr, true, true, false);
#else
            var options = new BarTerminationsData(document);
            options.HookTypeIdAtStart = new ElementId(-1);
            options.HookTypeIdAtEnd   = new ElementId(-1);
            var rebar = Rebar.CreateFromCurves(
                document, RebarStyle.Standard,
                barType, host, normal, shape, options, true, true);

            if (qty > 1)
                rebar.GetShapeDrivenAccessor()
                     .SetLayoutAsFixedNumber(qty, lengthArr, true, true, true);
            SetSolidRebar3DView(rebar, document.ActiveView);
#endif
        }
    }
}
