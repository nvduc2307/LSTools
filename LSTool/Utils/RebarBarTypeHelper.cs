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
            SetDiameterParameter(
                rebarBarType,
                BuiltInParameter.REBAR_BAR_DIAMETER,
                diameter);
            SetDiameterParameter(
                rebarBarType,
                BuiltInParameter.REBAR_MODEL_BAR_DIAMETER,
                diameter);
        }

        private static void SetDiameterParameter(
            RebarBarType rebarBarType,
            BuiltInParameter parameterId,
            double diameter)
        {
            var parameter = rebarBarType.get_Parameter(parameterId);
            if (parameter == null || parameter.IsReadOnly) return;
            if (Math.Abs(
                    parameter.AsDouble().ToMillimeters()
                    - diameter.ToMillimeters()) > 1)
            {
                parameter.Set(diameter);
            }
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
