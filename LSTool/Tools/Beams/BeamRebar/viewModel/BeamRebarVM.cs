using LSTool.Tools.Beams.BeamRebar.models;

namespace LSTool.Tools.Beams.BeamRebar.viewModel
{
    public partial class BeamRebarVM : ObservableObject
    {
        private BeamRebarModel _beamRebarModel;
        [ObservableProperty]
        private List<BeamRebarModel> _beamRebarModels;
        public BeamRebarModel BeamRebarModel
        {
            get => _beamRebarModel;
            set
            {
                _beamRebarModel = value;
                OnPropertyChanged();
                BeamRebarModelChangeAction?.Invoke();
            }
        }
        public Action BeamRebarModelChangeAction { get; set; }

        [ObservableProperty]
        private List<BeamRebarSettingModel> _beamRebarSettings;
        [ObservableProperty]
        private BeamRebarSettingModel _beamRebarSetting;
        [ObservableProperty]
        private string _beamSettingName;
        public RelayCommand SaveAsCommand { get; set; }
        public RelayCommand SaveCommand { get; set; }
        public RelayCommand DeleteCommand { get; set; }
        public RelayCommand LoadCommand { get; set; }
        public RelayCommand OkCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }
        public RelayCommand CreateTieVerticalCommand { get; set; }
        public RelayCommand CreateTieHorizontalCommand { get; set; }
    }
}
