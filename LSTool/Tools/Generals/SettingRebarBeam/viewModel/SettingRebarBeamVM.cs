using LSTool.Tools.Beams.BeamRebar.models;

namespace LSTool.Tools.Generals.SettingRebarBeam.viewModel
{
    public class SettingRebarBeamVM
    {
        public SettingBeamModel SettingBeamModel { get; set; }
        public CommunityToolkit.Mvvm.Input.RelayCommand OkCommand { get; set; }
        public CommunityToolkit.Mvvm.Input.RelayCommand CancelCommand { get; set; }
    }
}
