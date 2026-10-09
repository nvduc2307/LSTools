using LSTool.MVVM.model.SettingRebarColumnDatas;

namespace LSTool.MVVM.viewModel
{
    public class SettingRebarColumnVM
    {
        public SettingFrameModel SettingRebarColumnModel { get; set; }
        public RelayCommand OkCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }
    }
}
