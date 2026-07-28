using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using LSTool.Licensing;
using LSTool.Tools.Columns.ColumnRebar.actions;
using LSTool.Utils;

namespace LSTool.Tools.Columns.ColumnRebar
{
    [Transaction(TransactionMode.Manual)]
    public class ColumnRebarCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (!LicenseGate.EnsureFeature(LicenseFeatures.ColumnRebar))
            {
                return Result.Cancelled;
            }

            var result = Result.Succeeded;
            var uiDocument = commandData.Application.ActiveUIDocument;
            var document = uiDocument.Document;
            using (var tsg = new TransactionGroup(document, "Command"))
            {
                tsg.Start();
                try
                {
                    var action = new ColumnRebarAction(uiDocument);
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
