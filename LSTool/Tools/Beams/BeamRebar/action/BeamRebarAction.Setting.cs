using LSTool.Tools.Beams.BeamRebar.models;
using LSTool.Utils;

namespace LSTool.Tools.Beams.BeamRebar.action
{
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
            _CaptureAnchor(newSetting);
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
            _CaptureAnchor(setting);
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
            _viewModel.AnchorStartTopL1 = setting.AnchorStartTopL1;
            _viewModel.AnchorStartTopL3 = setting.AnchorStartTopL3;
            _viewModel.AnchorStartBotL2 = setting.AnchorStartBotL2;
            _viewModel.AnchorStartBotL3 = setting.AnchorStartBotL3;
            _viewModel.AnchorEndTopL1 = setting.AnchorEndTopL1;
            _viewModel.AnchorEndTopL3 = setting.AnchorEndTopL3;
            _viewModel.AnchorEndBotL2 = setting.AnchorEndBotL2;
            _viewModel.AnchorEndBotL3 = setting.AnchorEndBotL3;
            _canvasAction?.Redraw();
        }

        private void _CaptureAnchor(BeamRebarSettingModel setting)
        {
            setting.AnchorStartTopL1 = _viewModel.AnchorStartTopL1;
            setting.AnchorStartTopL3 = _viewModel.AnchorStartTopL3;
            setting.AnchorStartBotL2 = _viewModel.AnchorStartBotL2;
            setting.AnchorStartBotL3 = _viewModel.AnchorStartBotL3;
            setting.AnchorEndTopL1 = _viewModel.AnchorEndTopL1;
            setting.AnchorEndTopL3 = _viewModel.AnchorEndTopL3;
            setting.AnchorEndBotL2 = _viewModel.AnchorEndBotL2;
            setting.AnchorEndBotL3 = _viewModel.AnchorEndBotL3;
        }
    }
}
