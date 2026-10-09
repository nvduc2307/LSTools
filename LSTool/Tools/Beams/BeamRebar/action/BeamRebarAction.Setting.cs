using LSTool.Tools.Beams.BeamRebar.models;
using LSTool.Utils;

namespace LSTool.Tools.Beams.BeamRebar.action
{
    /// <summary>Save / Save As / Remove / Load setting thép dầm (giống tool cột).</summary>
    public partial class BeamRebarAction
    {
        private BeamRebarSettingAction _settingAction = new BeamRebarSettingAction();

        private void _SaveAsCommand()
        {
            var name = _viewModel.BeamSettingName?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                IO.ShowInfo("Tên setting không được để trống.");
                return;
            }
            var existing = _viewModel.BeamRebarSettings ?? new List<BeamRebarSettingModel>();
            if (existing.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                IO.ShowInfo($"Setting \"{name}\" đã tồn tại. Vui lòng chọn tên khác.");
                return;
            }
            var newSetting = new BeamRebarSettingModel
            {
                Id = existing.Any() ? existing.Max(x => x.Id) + 1 : 1,
                Name = name,
            };
            BeamRebarSettingAction.Capture(_viewModel.BeamRebarModel, newSetting);
            var list = existing.ToList();
            list.Add(newSetting);
            _settingAction.SaveSettings(list);
            _viewModel.BeamRebarSettings = list;
            _viewModel.BeamRebarSetting = newSetting;
        }

        private void _SaveCommand()
        {
            var setting = _viewModel.BeamRebarSetting;
            if (setting == null || _viewModel.BeamRebarModel == null)
            {
                IO.ShowInfo("Chưa chọn setting. Hãy dùng Save As để tạo setting mới.");
                return;
            }
            BeamRebarSettingAction.Capture(_viewModel.BeamRebarModel, setting);
            var list = _viewModel.BeamRebarSettings ?? new List<BeamRebarSettingModel>();
            var idx = list.FindIndex(x => x.Id == setting.Id);
            if (idx >= 0) list[idx] = setting;
            _settingAction.SaveSettings(list);
            IO.ShowInfo($"Đã lưu setting \"{setting.Name}\".");
        }

        private void _DeleteCommand()
        {
            var setting = _viewModel.BeamRebarSetting;
            if (setting == null) return;
            var confirm = IO.ShowQuestion(
                $"Bạn có chắc chắn muốn xóa setting \"{setting.Name}\" không?",
                "Xác nhận xóa");
            if (confirm != DialogResult.Yes) return;
            var list = (_viewModel.BeamRebarSettings ?? new List<BeamRebarSettingModel>()).ToList();
            list.RemoveAll(x => x.Id == setting.Id);
            _settingAction.SaveSettings(list);
            _viewModel.BeamRebarSettings = list;
            _viewModel.BeamRebarSetting = list.FirstOrDefault();
        }

        private void _LoadCommand()
        {
            var setting = _viewModel.BeamRebarSetting;
            var beam = _viewModel.BeamRebarModel;
            if (setting == null || beam == null) return;
            BeamRebarSettingAction.Apply(setting, beam);
            _canvasAction?.Redraw();
        }
    }
}
