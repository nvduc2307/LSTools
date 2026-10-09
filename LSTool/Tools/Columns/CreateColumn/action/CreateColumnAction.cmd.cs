using LSTool.Tools.Columns.CreateColumn.model;
using LSTool.Tools.Columns.CreateColumn.utils;
using LSTool.Utils;

namespace LSTool.Tools.Columns.CreateColumn.action
{
    public partial class CreateColumnAction
    {
        private void _ColumnConcreteModelAction()
        {
            _canvasAction?.DrawSection(_viewModel.ColumnConcreteModels, _viewModel.ColumnConcreteModel);
            ElementInRevitUtils.SelectedElement(_uidocument, _viewModel.ColumnConcreteModel.Id);
        }

        private void _CancelCommand()
        {
            _view.Close();
        }

        private void _CreateTeiCommand()
        {
            _canvasAction?.CreateTies(_viewModel.ColumnConcreteModel);
        }

        private void _OkCommand()
        {
            _view.Close();

            using (var ts = new Transaction(_document, "new transaction"))
            {
                ts.SkipAllWarnings();
                ts.Start();
                _columnConcreteAction.SetRebarSetting(
                    _document,
                    _viewModel.ColumnConcreteModels);
                _columnRebarStirrupAction.CreateStirrupMain(
                    _viewModel.ColumnConcreteModels);
                _columnRebarMainAction.CreateRebarMain(
                    _viewModel.ColumnConcreteModels);
                _columnRebarStirrupAction.CreateStirrupSub(
                    _viewModel.ColumnConcreteModels);
                _columnRebarStirrupAction.SaveSettingColumnStirrupPosition(
                    _viewModel.ColumnConcreteModels);
                ts.Commit();
            }
        }

        private void _SaveAsCommand()
        {
            var name = _viewModel.ColumnSettingName?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                IO.ShowInfo("Tên setting không được để trống.");
                return;
            }
            var existing = _viewModel.RebarColumnSettings ?? new List<RebarColumnSettingModel>();
            if (existing.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                IO.ShowInfo($"Setting \"{name}\" đã tồn tại. Vui lòng chọn tên khác.");
                return;
            }
            var col   = _viewModel.ColumnConcreteModel;
            var newId = existing.Any() ? existing.Max(x => x.Id) + 1 : 1;
            var newSetting = new RebarColumnSettingModel()
            {
                Id         = newId,
                Name       = name,
                DiameterDX = col?.DiameterDX ?? string.Empty,
                DiameterDY = col?.DiameterDY ?? string.Empty,
                DiameterST = col?.DiameterST ?? string.Empty,
                SpacingDX  = col?.SpacingDX  ?? 0,
                SpacingDY  = col?.SpacingDY  ?? 0,
                SpacingST  = col?.SpacingST  ?? 0,
                SpacingSTE = col?.SpacingSTE ?? 0,
            };
            var updatedList = existing.ToList();
            updatedList.Add(newSetting);
            _settingRebarColumnAction.SaveRebarColumnSettings(updatedList);
            _viewModel.RebarColumnSettings = updatedList;
            _viewModel.RebarColumnSetting  = newSetting;
        }

        private void _DeleteCommand()
        {
            var setting = _viewModel.RebarColumnSetting;
            if (setting == null) return;
            if (setting.Name.Equals("Default", StringComparison.OrdinalIgnoreCase))
            {
                IO.ShowInfo("Không thể xóa setting \"Default\".");
                return;
            }
            var confirm = IO.ShowQuestion(
                $"Bạn có chắc chắn muốn xóa setting \"{setting.Name}\" không?",
                "Xác nhận xóa");
            if (confirm != DialogResult.Yes) return;
            var list = (_viewModel.RebarColumnSettings ?? new List<RebarColumnSettingModel>()).ToList();
            list.RemoveAll(x => x.Id == setting.Id);
            _settingRebarColumnAction.SaveRebarColumnSettings(list);
            _viewModel.RebarColumnSettings = list;
            _viewModel.RebarColumnSetting  = list.FirstOrDefault();
        }

        private void _SaveCommand()
        {
            var setting = _viewModel.RebarColumnSetting;
            var col     = _viewModel.ColumnConcreteModel;
            if (setting == null || col == null) return;
            setting.DiameterDX  = col.DiameterDX;
            setting.DiameterDY  = col.DiameterDY;
            setting.DiameterST  = col.DiameterST;
            setting.SpacingDX   = col.SpacingDX;
            setting.SpacingDY   = col.SpacingDY;
            setting.SpacingST   = col.SpacingST;
            setting.SpacingSTE  = col.SpacingSTE;
            var list = _viewModel.RebarColumnSettings ?? new List<RebarColumnSettingModel>();
            var idx  = list.FindIndex(x => x.Id == setting.Id);
            if (idx >= 0) list[idx] = setting;
            _settingRebarColumnAction.SaveRebarColumnSettings(list);
            IO.ShowInfo($"Đã lưu setting \"{setting.Name}\".");
        }

        private void _LoadCommand()
        {
            var setting = _viewModel.RebarColumnSetting;
            var col = _viewModel.ColumnConcreteModel;
            if (setting == null || col == null) return;
            col.DiameterDX  = setting.DiameterDX;
            col.DiameterDY  = setting.DiameterDY;
            col.DiameterST  = setting.DiameterST;
            col.SpacingDX   = setting.SpacingDX;
            col.SpacingDY   = setting.SpacingDY;
            col.SpacingST   = setting.SpacingST;
            col.SpacingSTE  = setting.SpacingSTE;
            _canvasAction?.Redraw();
        }
    }
}
