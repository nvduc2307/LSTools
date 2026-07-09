using LSTool.Utils;
using System.IO;

namespace LSTool.Tools.Generals.RebarInfomationParameter
{
    public class ConcreteInfomationParameterAction
    {
        public static void CheckParameter(Element element)
        {
            var hasPara = CheckHasParameter(element);
            if (!hasPara)
                AddParameter(element.Document);
        }
        public static void AddParameter(Document document)
        {
            var pathShareParameter = $"{PathHelper.Templates}\\ShareParameterConcreteKBr.txt";
            if (!File.Exists(pathShareParameter)) return;
            using (var ts = new Transaction(document, "AddParameter"))
            {
                ts.SkipAllWarnings();
                ts.Start();
                ParameterHelper
                    .CreateSharedParameters(
                        document,
                        pathShareParameter,
                        BuiltInCategory.OST_GenericModel);
                ts.Commit();
            }
        }
        private static bool CheckHasParameter(Element element)
        {
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M1_X_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M1_X_SPACING)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M1_Y_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M1_Y_SPACING)) return false;

            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M2_X_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M2_X_SPACING)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M2_Y_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M2_Y_SPACING)) return false;

            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M3_X_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M3_X_SPACING)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M3_Y_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M3_Y_SPACING)) return false;

            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M4_X_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M4_X_SPACING)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M4_Y_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M4_Y_SPACING)) return false;

            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M5_X_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M5_X_SPACING)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M5_Y_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M5_Y_SPACING)) return false;

            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M6_X_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M6_X_SPACING)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M6_Y_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M6_Y_SPACING)) return false;

            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M7_X_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M7_X_SPACING)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M7_Y_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M7_Y_SPACING)) return false;

            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M8_X_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M8_X_SPACING)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M8_Y_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M8_Y_SPACING)) return false;

            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M9_X_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M9_X_SPACING)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M9_Y_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M9_Y_SPACING)) return false;

            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M10_X_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M10_X_SPACING)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M10_Y_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M10_Y_SPACING)) return false;

            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M11_X_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M11_X_SPACING)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M11_Y_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M11_Y_SPACING)) return false;

            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M12_X_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M12_X_SPACING)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M12_Y_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M12_Y_SPACING)) return false;

            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M13_X_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M13_X_SPACING)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M13_Y_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M13_Y_SPACING)) return false;

            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M14_X_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M14_X_SPACING)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M14_Y_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M14_Y_SPACING)) return false;

            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M15_X_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M15_X_SPACING)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M15_Y_DIAMETER)) return false;
            if (!ParameterHelper.HasParameter(element, ConcreteInfomationParameterName.RPName_M15_Y_SPACING)) return false;
            return true;
        }
    }
}
