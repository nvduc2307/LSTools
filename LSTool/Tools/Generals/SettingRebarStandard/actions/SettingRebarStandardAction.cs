using Autodesk.Revit.UI;
using LSTool.Tools.Generals.SettingRebarStandard.models;
using LSTool.Tools.Generals.SettingRebarStandard.viewModels;
using LSTool.Tools.Generals.SettingRebarStandard.views;
using Newtonsoft.Json;

namespace LSTool.Tools.Generals.SettingRebarStandard.actions
{
    public class SettingRebarStandardAction
    {
        private UIDocument _uidocument;
        private Document _document;
        private SettingRebarStandardView _view;
        private SettingRebarStandardVM _viewModel;
        private SettingRebarStandardSchema _settingRebarSchema;
        public SettingRebarStandardAction(UIDocument uidocument)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;
            _settingRebarSchema = new SettingRebarStandardSchema(
                SettingRebarStandardSchema.GUID,
                SettingRebarStandardSchema.NAME);
            _viewModel = new SettingRebarStandardVM()
            {
                SettingRebarStandardModel = GetSetting(),
                OkCommand = new RelayCommand(_OkCommand),
                CancelCommand = new RelayCommand(_CancelCommand)
            };
            _view = new SettingRebarStandardView() { DataContext = _viewModel };
        }

        private void _CancelCommand()
        {
            _view?.Close();
        }

        private void _OkCommand()
        {
            _view?.Close();
            var obj = new SettingRebarStandardModel();
            obj.L1 = _viewModel.SettingRebarStandardModel.L1;
            obj.G = _viewModel.SettingRebarStandardModel.G;
            obj.L2 = _viewModel.SettingRebarStandardModel.L2;
            obj.HMin = _viewModel.SettingRebarStandardModel.HMin;
            var content = JsonConvert.SerializeObject(obj);
            using (var ts = new Transaction(_document, "new transaction"))
            {
                ts.Start();
                _settingRebarSchema.Write(_document.ProjectInformation, content);
                ts.Commit();
            }
        }

        public void Execute()
        {
            _view?.ShowDialog();
        }
        public SettingRebarStandardModelUI GetSetting()
        {
            var result = new SettingRebarStandardModelUI()
            {
                L1 = 40,
                G = 10,
                L2 = 30,
                HMin = 10,
            };
            var content = _settingRebarSchema.Read(_document.ProjectInformation);
            if (string.IsNullOrEmpty(content)) return result;
            var obj = JsonConvert.DeserializeObject<SettingRebarStandardModel>(content);
            if (obj == null) return result;
            result.L1 = obj.L1;
            result.G = obj.G;
            result.L2 = obj.L2;
            result.HMin = obj.HMin;
            return result;
        }
    }
}
