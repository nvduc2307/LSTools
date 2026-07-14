using Autodesk.Revit.DB.Structure;

namespace LSTool.Utils
{
    public class RebarBarTypeHelper
    {
        public static RebarBarType CreateNewType(Document document, string nameDia, double diameter, double bendRadius)
        {
            var rebarBarType = RebarBarType.Create(document);
            rebarBarType.Name = nameDia;
            SetRebarBendDiameter(rebarBarType, bendRadius);
            SetRebarDiameter(rebarBarType, diameter);
            return rebarBarType;
        }
        public static void SetRebarDiameter(RebarBarType rebarBarType, double diameter)
        {
            if (rebarBarType == null || diameter <= 0)
                return;
            if (Math.Abs(rebarBarType.get_Parameter(BuiltInParameter.REBAR_BAR_DIAMETER).AsDouble().ToMillimeters() - diameter.ToMillimeters()) > 1)
                rebarBarType.get_Parameter(BuiltInParameter.REBAR_BAR_DIAMETER).Set(diameter);
#if REVIT2022 || REVIT2023 || REVIT2024
            if (Math.Abs(rebarBarType.BarModelDiameter.ToMillimeters() - diameter.ToMillimeters()) > 1)
                rebarBarType.BarModelDiameter = diameter;
#endif
        }
        public static void SetRebarBendDiameter(RebarBarType rebarBarType, double bendDiameter)
        {
            if (rebarBarType == null || bendDiameter <= 0)
                return;
            if(Math.Abs(rebarBarType.StandardBendDiameter.ToMillimeters() - bendDiameter.ToMillimeters()) > 1)
            rebarBarType.StandardBendDiameter = bendDiameter;
            if(Math.Abs(rebarBarType.StandardHookBendDiameter.ToMillimeters() - bendDiameter.ToMillimeters()) > 1)
            rebarBarType.StandardHookBendDiameter = bendDiameter;
            if(Math.Abs(rebarBarType.StirrupTieBendDiameter.ToMillimeters() - bendDiameter.ToMillimeters()) > 1)
            rebarBarType.StirrupTieBendDiameter = bendDiameter;

        }
    }
}
