using Autodesk.Revit.UI;
using LSTool.Tools.Columns.ColumnRebar.models;
using Newtonsoft.Json;

namespace LSTool.Tools.Columns.ColumnRebar.actions
{
    public class ColumnRebarAnchorAction
    {
        private UIDocument _uidocument;
        private Document _document;
        private ColumnRebarAnchorSchema _columnRebarAnchorSchema;
        public ColumnRebarAnchorAction(UIDocument uidocument)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;
            _columnRebarAnchorSchema = new ColumnRebarAnchorSchema(
                ColumnRebarAnchorSchema.GUID,
                ColumnRebarAnchorSchema.NAME);
        }
        public ColumnRebarAnchorModelUI GetColumnRebarAnchor()
        {
            var result = new ColumnRebarAnchorModelUI() { AC = 500 };
            var content = _columnRebarAnchorSchema.Read(_document.ProjectInformation);
            if (string.IsNullOrEmpty(content)) return result;
            var obj = JsonConvert.DeserializeObject<ColumnRebarAnchorModel>(content);
            if (obj == null) return result;
            result.AC = obj.AC;
            return result;
        }
        public void SaveColumnRebarAnchor(ColumnRebarAnchorModelUI obj)
        {
            using (var ts = new SubTransaction(_document))
            {
                ts.Start();
                var ele = new ColumnRebarAnchorModel()
                {
                    AC = obj.AC,
                };
                var content = JsonConvert.SerializeObject(ele);
                _columnRebarAnchorSchema.Write(_document.ProjectInformation, content);
                ts.Commit();
            }
        }
    }
}
