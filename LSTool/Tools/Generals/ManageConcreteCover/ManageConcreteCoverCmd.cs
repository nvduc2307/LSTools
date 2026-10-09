using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using LSTool.Tools.Generals.ManageConcreteCover.action;
using LSTool.Utils;

namespace LSTool.Tools.Generals.ManageConcreteCover
{
    [Transaction(TransactionMode.Manual)]
    public class ManageConcreteCoverCmd : IExternalCommand
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
                    var action = new ManageConcreteCoverAction(document);
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
