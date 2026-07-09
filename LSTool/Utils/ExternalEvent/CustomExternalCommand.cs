using Autodesk.Revit.UI;

namespace LSTool.Utils.ExternalEvent
{
    public class CustomExternalCommand : IExternalEventHandler
    {
        private string _name;
        public Action Action { get; set; }
        public CustomExternalCommand(string nameEvent)
        {
            _name = nameEvent;
        }
        public void Execute(UIApplication app)
        {
            var doc = app.ActiveUIDocument.Document;

            using (var ts = new Transaction(doc, "new transaction"))
            {
                ts.Start();
                Action?.Invoke();
                ts.Commit();
            }
        }
        public string GetName() => _name;
    }
}
