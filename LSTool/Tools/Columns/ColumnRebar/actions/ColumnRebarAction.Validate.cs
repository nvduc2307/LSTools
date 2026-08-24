using LSTool.Tools.Generals.SettingDiameters.action;
using LSTool.Tools.Generals.SettingRebarStandard.models;
using LSTool.Utils;
using Newtonsoft.Json;

namespace LSTool.Tools.Columns.ColumnRebar.actions
{
    public partial class ColumnRebarAction
    {
        private void Validate()
        {
            ValidateRebarDiameter();
            ValidateRebarHost();
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
        private void ValidateRebarHost()
        {
            using (var ts = new Transaction(_document, "RebarDatabasesAction"))
            {
                ts.Start();
                _host = RebarHelper.CreateRebarHost(_document);
                ts.Commit();
            }
        }
        private void ValidateQtyRebar()
        {
            if (_viewModel.ColumnConcreteModels == null) return;
            var qty = _viewModel.ColumnConcreteModels.Count;
            foreach (var col in _viewModel.ColumnConcreteModels)
            {
                var index = _viewModel.ColumnConcreteModels.IndexOf(col);
                if (index == qty - 1) continue;
                var nextColumn = _viewModel.ColumnConcreteModels[index + 1];
                if (nextColumn.SpacingDX > col.SpacingDX)
                    throw new Exception(
                        $"Số lượng X của {nextColumn.Name} nhiều hơn cột bên dưới");
                if (nextColumn.SpacingDY > col.SpacingDY)
                    throw new Exception(
                        $"Số lượng Y của {nextColumn.Name} nhiều hơn cột bên dưới");
            }
        }
    }
}
