using Autodesk.Revit.DB.Structure;
using LSTool.Compatibility;
using RIMT.Utils.RevRebars;

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
                .Select(x=>x.ToList())
                .ToList();
            var qty = dirs.Count;
            if (qty ==1)
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
                var ps = shape.Select(x=>x.GetEndPoint(0)).ToList();
                var plane = Plane.CreateByNormalAndOrigin(normal, ps[0]);
                if(ps.Any(x=>x.RayIntersectPlane(plane.Normal, plane).DistanceTo(x).FootToMm() > 5))
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
            rebar.SetSolidRebar3DView(document.ActiveView);
            if(rebar != null)
                rebar.SetSolidRebar3DView(document.ActiveView);
#else
            var rebar = Rebar.CreateFreeForm(
                document,
                rebarBarTypes.FirstOrDefault(x => x.Name == rebarName),
                host,
                new List<CurveLoop>() { cl },
                RebarStyle.Standard);
            if(rebar.Rebar != null)
                rebar.Rebar.SetSolidRebar3DView(document.ActiveView);
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
            rebar.SetSolidRebar3DView(document.ActiveView);
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
            rebar.SetSolidRebar3DView(document.ActiveView);
            if(rebar == null)
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
        public static void CreateRebarStirrupTie(
            Document document,
            List<Curve> shape,
            string rebarName,
            XYZ normal,
            RebarHookType hookStart,
            RebarHookType hookend,
            List<RebarBarType> rebarBarTypes,
            Element host)
        {
#if REVIT2025 || REVIT2024 || REVIT2023 || REVIT2022 || REVIT2021
            var rebar = Rebar.CreateFromCurves(
                document,
                RebarStyle.StirrupTie,
                rebarBarTypes.FirstOrDefault(x => x.Name == rebarName),
                hookStart,
                hookend,
                host,
                normal,
                shape,
                RebarHookOrientation.Right,
                RebarHookOrientation.Right,
                true, true);
            rebar.SetSolidRebar3DView(document.ActiveView);
#else
            var options = new BarTerminationsData(document);
            options.HookTypeIdAtStart = hookStart.Id;
            options.HookTypeIdAtEnd = hookend.Id;
            var rebar = Rebar.CreateFromCurves(
                document,
                RebarStyle.StirrupTie,
                rebarBarTypes.FirstOrDefault(x => x.Name == rebarName),
                host,
                normal,
                shape,
                options,
                true, true);
            rebar.SetSolidRebar3DView(document.ActiveView);
#endif
        }
    }
}
