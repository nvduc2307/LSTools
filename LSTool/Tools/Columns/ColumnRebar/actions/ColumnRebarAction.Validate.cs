using Autodesk.Revit.UI;
using LSTool.Tools.Columns.ColumnRebar.viewModels;
using LSTool.Tools.Columns.ColumnRebar.views;
using LSTool.Tools.Generals.SettingDiameters.action;
using LSTool.Tools.Generals.SettingRebarStandard.models;
using Newtonsoft.Json;

namespace LSTool.Tools.Columns.ColumnRebar.actions
{
    public partial class ColumnRebarAction
    {
        private void Validate()
        {
            ValidateSettingRebarStandard();
            ValidateRebarDiameter();
            _columnConcreteAction.ValidateShareParameter();
        }
        private void ValidateRebarDiameter()
        {
            var action = new RebarDatabasesAction(_uidocument);
            var rebarDatas = action.GetRebarBarTypes();

            using (var ts = new Transaction(_document, "RebarDatabasesAction"))
            {
                ts.Start();
                action.CreateRebarBarType(rebarDatas);
                ts.Commit();
            }
        }
        private void ValidateSettingRebarStandard()
        {
            var error = "SettingRebarStandard is not found";
            var content = _settingRebarStandardSchema.Read(_document.ProjectInformation);
            if (string.IsNullOrEmpty(content))
                throw new Exception(error);
            var obj = JsonConvert.DeserializeObject<SettingRebarStandardModel>(content);
            if(obj == null)
                throw new Exception(error);
            _settingRebarStandardModel = obj;
        }
    }
}
