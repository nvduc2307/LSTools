using LSTool.Tools.Generals.SettingRebarStandard.models;

namespace LSTool.Tools.Generals.SettingRebarStandard.viewModels
{
    public class SettingRebarStandardVM
    {
        public SettingRebarStandardModelUI SettingRebarStandardModel { get; set; }
        public RelayCommand OkCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }
    }
}
