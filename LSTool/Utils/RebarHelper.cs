using Autodesk.Revit.DB.Structure;

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
            var rebar = Rebar.CreateFreeForm(
                document,
                rebarBarTypes.FirstOrDefault(x => x.Name == rebarName),
                host,
                new List<CurveLoop>() { cl },
                RebarStyle.Standard);
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
            var options = new BarTerminationsData(document);
            options.HookTypeIdAtStart = hookStart.Id;
            options.HookTypeIdAtEnd = hookend.Id;
            Rebar.CreateFromCurves(
                document,
                RebarStyle.StirrupTie,
                rebarBarTypes.FirstOrDefault(x => x.Name == rebarName),
                host,
                normal,
                shape,
                options,
                true, true);
        }
    }
}
