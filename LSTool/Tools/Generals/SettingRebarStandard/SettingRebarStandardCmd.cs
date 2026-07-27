using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using LSTool.Tools.Generals.SettingRebarStandard.actions;
using LSTool.Utils;

namespace LSTool.Tools.Generals.SettingRebarStandard
{
    [Transaction(TransactionMode.Manual)]
    public class SettingRebarStandardCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var result = Result.Succeeded;
            var uiDocument = commandData.Application.ActiveUIDocument;
            var document = uiDocument.Document;
            using (var tsg = new TransactionGroup(document, "Command"))
            {
                tsg.Start();
                try
                {
                    var action = new SettingRebarStandardAction(uiDocument);
                    action.Execute();
                    tsg.Assimilate();
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException) { }
                catch (Exception ex)
                {
                    IO.ShowWarning(ex.Message);
                    tsg.RollBack();
                    result = Result.Failed;
                }
            }
            return result;

        }
    }
}
