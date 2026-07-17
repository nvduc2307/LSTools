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
            if (obj == null)
                throw new Exception(error);
            _settingRebarStandardModel = obj;
        }
        private void ValidateQtyRebar()
        {
            if (_viewModel.ColumnConcreteModels == null) return;
            var count = _viewModel.ColumnConcreteModels.Count;
            if (count <= 1) return;
            foreach (var item in _viewModel.ColumnConcreteModels)
            {
                var index = _viewModel.ColumnConcreteModels.IndexOf(item);
                if (index == count - 1) continue;
                if (_viewModel.ColumnConcreteModels[index + 1].SpacingDX > _viewModel.ColumnConcreteModels[index].SpacingDX)
                    throw new Exception($"Số lượng X của cột thứ {index + 2} nhiều hơn cột bên dưới");
                if (_viewModel.ColumnConcreteModels[index + 1].SpacingDY > _viewModel.ColumnConcreteModels[index].SpacingDY)
                    throw new Exception($"Số lượng Y của cột thứ {index + 2} nhiều hơn cột bên dưới");
            }
        }
    }
}
