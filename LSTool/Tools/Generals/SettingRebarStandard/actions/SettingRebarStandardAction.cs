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
            obj.LC = _viewModel.SettingRebarStandardModel.LC;
            obj.EC = _viewModel.SettingRebarStandardModel.EC;
            obj.EB = _viewModel.SettingRebarStandardModel.EB;
            obj.CoverC = _viewModel.SettingRebarStandardModel.CoverC;
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
            return GetSetting(_document, _LCAction);
        }

        public static SettingRebarStandardModelUI GetSetting(
            Document document,
            Action<SettingRebarStandardModelUI>? lcAction = null)
        {
            var result = new SettingRebarStandardModelUI()
            {
                L1 = 40,
                G = 10,
                L2 = 30,
                HMin = 10,
                LC = 0.25,
                EC = 100,
                EB = 100,
                CoverC = 30,
                LCAction = lcAction
            };
            try
            {
                var settingRebarSchema = new SettingRebarStandardSchema(
                    SettingRebarStandardSchema.GUID,
                    SettingRebarStandardSchema.NAME);
                var content = settingRebarSchema.Read(
                    document.ProjectInformation);
                if (string.IsNullOrEmpty(content)) return result;
                var obj = JsonConvert.DeserializeObject<SettingRebarStandardModel>(content);
                if (obj == null) return result;
                result.L1 = obj.L1;
                result.G = obj.G;
                result.L2 = obj.L2;
                result.HMin = obj.HMin;
                result.LC = obj.LC;
                result.EC = obj.EC;
                result.EB = obj.EB;
                result.CoverC = obj.CoverC;
            }
            catch (Exception)
            {
            }
            return result;
        }

        private void _LCAction(SettingRebarStandardModelUI uI)
        {
            if (uI.LC > 0 && uI.LC <= 0.4) return;
            uI.LC = 0.25;
        }
    }
}
