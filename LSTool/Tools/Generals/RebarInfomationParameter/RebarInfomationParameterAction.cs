using Autodesk.Revit.DB.Structure;
using LSTool.Utils;
using System.IO;

namespace LSTool.Tools.Generals.RebarInfomationParameter
{
    public class RebarInfomationParameterAction
    {
        public static void SetReMark(Rebar rebar, string reMark, string reHost)
        {
            var para_reMark = rebar.LookupParameter(RebarInfomationParameterName.RPName_TP_ReMark);
            var para_reHost = rebar.LookupParameter(RebarInfomationParameterName.RPName_TP_ReHost);
            if (para_reMark == null) return;
            para_reMark.Set(reMark);
            if (para_reHost == null) return;
            para_reHost.Set(reHost);
        }
        public static void CheckParameter(Element element)
        {
            var hasPara = CheckHasParameter(element);
            if (!hasPara)
                AddParameter(element.Document);
        }
        public static void AddParameter(Document document)
        {
            var pathShareParameter = $"{PathHelper.Templates}\\ShareParameterRebarKBr.txt";
            if (!File.Exists(pathShareParameter)) return;
            using (var ts = new Transaction(document, "AddParameter"))
            {
                ts.SkipAllWarnings();
                ts.Start();
                ParameterHelper
                    .CreateSharedParameters(
                        document,
                        pathShareParameter,
                        BuiltInCategory.OST_Rebar);
                ts.Commit();
            }
        }
        private static bool CheckHasParameter(Element element)
        {
            if (!ParameterHelper.HasParameter(element, RebarInfomationParameterName.RPName_TP_ReMark)) return false;
            if (!ParameterHelper.HasParameter(element, RebarInfomationParameterName.RPName_TP_ReHost)) return false;
            return true;
        }
    }
}
