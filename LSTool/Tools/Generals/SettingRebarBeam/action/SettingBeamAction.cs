using LSTool.Tools.Beams.BeamRebar.models;
using LSTool.Tools.Beams.BeamRebar.Utils;
using LSTool.Tools.Generals.SettingRebarBeam.view;
using LSTool.Tools.Generals.SettingRebarBeam.viewModel;
using LSTool.Utils;
using Newtonsoft.Json;
using System.IO;

namespace LSTool.Tools.Generals.SettingRebarBeam.action
{
    /// <summary>Cài đặt chung cho dầm (E0, StressZone) – lưu vào SettingBeam.json, giống SettingColumnAction của cột.</summary>
    public class SettingBeamAction
    {
        private Document _document;
        private SettingRebarBeamView _view;
        private SettingRebarBeamVM _viewModel;

        public SettingBeamAction(Document document)
        {
            _document = document;
            _viewModel = new SettingRebarBeamVM()
            {
                SettingBeamModel = GetSettingBeamModel(),
                OkCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(_OkCommand),
                CancelCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(_CancelCommand),
            };
            _view = new SettingRebarBeamView() { DataContext = _viewModel };
        }

        private void _CancelCommand()
        {
            _view.Close();
        }

        private void _OkCommand()
        {
            var model = _viewModel.SettingBeamModel;
            if (model.E0 < 0)
            {
                IO.ShowInfo("E0 phải >= 0.");
                return;
            }
            if (model.StressZone <= 0 || model.StressZone > 0.4)
            {
                IO.ShowInfo("StressZone phải trong khoảng (0, 0.4].");
                return;
            }
            if (!Directory.Exists(PathHelper.Appdatas))
                Directory.CreateDirectory(PathHelper.Appdatas);
            var path = $"{PathHelper.Appdatas}\\SettingBeam.json";
            File.WriteAllText(path, JsonConvert.SerializeObject(model, Formatting.Indented));
            IO.ShowInfo("Complete!");
            _view.Close();
        }

        /// <summary>Đọc SettingBeam.json (tự copy từ file mẫu nếu chưa có).</summary>
        public SettingBeamModel GetSettingBeamModel() => BeamRebarUtils.GetSettingBeamModel();

        public void Execute()
        {
            _view.ShowDialog();
        }
    }
}
