using Newtonsoft.Json;
using LSTool.MVVM.model.SettingRebarColumnDatas;
using LSTool.MVVM.view;
using LSTool.MVVM.viewModel;
using LSTool.Tools.Columns.CreateColumn.model;
using LSTool.Utils;
using System.IO;
using System.Windows;

namespace LSTool.AutoCad.Action.Actions
{
    public class SettingColumnAction
    {
        private Document _document;
        private SettingRebarColumnView _view;
        private SettingRebarColumnVM _viewModel;

        public SettingColumnAction(Document document)
        {
            _document = document;
            _viewModel = new SettingRebarColumnVM()
            {
                SettingRebarColumnModel = GetSettingRebarColumnModel(),
                OkCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(_OkCommand),
                CancelCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(_CancelCommand),
            };
            _view = new SettingRebarColumnView() { DataContext = _viewModel };
        }

        private void _CancelCommand()
        {
            _view.Close();
        }

        private void _OkCommand()
        {
            var path = $"{PathHelper.Appdatas}\\SettingColumn.json";
            if (!File.Exists(path)) return;
            var content = JsonConvert.SerializeObject(_viewModel.SettingRebarColumnModel);
            File.WriteAllText(path, content);
            IO.ShowInfo("Complete!");
            _view.Close();
        }

        public SettingFrameModel GetSettingRebarColumnModel()
        {
            var pathTemplate = $"{PathHelper.FolderDatas}\\SettingColumn.json";
            var path = $"{PathHelper.Appdatas}\\SettingColumn.json";
            if (!File.Exists(path))
            {
                if (!Directory.Exists(PathHelper.Appdatas))
                    Directory.CreateDirectory(PathHelper.Appdatas);
                File.Copy(pathTemplate, path);
            }
            var datas = JsonConvert.DeserializeObject<SettingFrameModel>(File.ReadAllText(path));
            return datas;
        }

        public List<RebarColumnSettingModel> GetRebarColumnSettings()
        {
            var pathTemplate = $"{PathHelper.FolderDatas}\\RebarColumnSetting.json";
            var path         = $"{PathHelper.Appdatas}\\RebarColumnSetting.json";
            if (!File.Exists(path))
            {
                if (!Directory.Exists(PathHelper.Appdatas))
                    Directory.CreateDirectory(PathHelper.Appdatas);
                File.Copy(pathTemplate, path);
            }
            var datas = JsonConvert.DeserializeObject<List<RebarColumnSettingModel>>(File.ReadAllText(path));
            return datas ?? new List<RebarColumnSettingModel>();
        }

        public void SaveRebarColumnSettings(List<RebarColumnSettingModel> settings)
        {
            var path = $"{PathHelper.Appdatas}\\RebarColumnSetting.json";
            if (!Directory.Exists(PathHelper.Appdatas))
                Directory.CreateDirectory(PathHelper.Appdatas);
            File.WriteAllText(path, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }

        public void Execute()
        {
            _view.ShowDialog();
        }
    }
}
