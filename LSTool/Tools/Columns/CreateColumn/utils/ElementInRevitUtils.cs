using Autodesk.Revit.UI;

namespace LSTool.Tools.Columns.CreateColumn.utils
{
    public class ElementInRevitUtils
    {
        public static void SelectedElement(UIDocument uidocument, string uniqueId)
        {
            var ele = uidocument.Document.GetElement(uniqueId);
            if (ele == null) return;
            uidocument.Selection.SetElementIds(new List<ElementId>() { ele.Id });
        }
    }
}
