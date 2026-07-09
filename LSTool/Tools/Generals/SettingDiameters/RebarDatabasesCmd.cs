using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.External;
using TPTool.Tools.SettingDiameters.RebarDatabases.action;
using TPTool.Tools.SettingDiameters.RebarDatabases.views;
using TPTool.Utils.Messages;

namespace LSTool.Tools.Generals.SettingDiameters
{
    [Transaction(TransactionMode.Manual)]
    public class RebarDatabasesCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            var result = Result.Succeeded;
            var uiDocument = commandData.Application.ActiveUIDocument;
            var document = uiDocument.Document;
            using (var tsg = new TransactionGroup(document, "RebarDatabasesCmd"))
            {
                tsg.Start();
                try
                {
                    var action = new RebarDatabasesAction(uiDocument);
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
