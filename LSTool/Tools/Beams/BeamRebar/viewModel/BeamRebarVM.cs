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

        // L1 / L2 (total anchorage) and L3 (min hook): multiples of the bar diameter (xd).
        [ObservableProperty]
        private double _anchorStartTopL1 = 40;
        [ObservableProperty]
        private double _anchorStartTopL3 = 10;
        [ObservableProperty]
        private double _anchorStartBotL2 = 30;
        [ObservableProperty]
        private double _anchorStartBotL3 = 10;
        [ObservableProperty]
        private double _anchorEndTopL1 = 40;
        [ObservableProperty]
        private double _anchorEndTopL3 = 10;
        [ObservableProperty]
        private double _anchorEndBotL2 = 30;
        [ObservableProperty]
        private double _anchorEndBotL3 = 10;
        public const double MinAnchorHookFactor = 10;

        partial void OnAnchorStartTopL1Changed(double value) => AnchorEndTopL1 = value;
        partial void OnAnchorStartBotL2Changed(double value) => AnchorEndBotL2 = value;
        partial void OnAnchorStartTopL3Changed(double value) { if (value < MinAnchorHookFactor) { AnchorStartTopL3 = MinAnchorHookFactor; return; } AnchorEndTopL3 = value; AnchorStartBotL3 = value; }
        partial void OnAnchorStartBotL3Changed(double value) { if (value < MinAnchorHookFactor) { AnchorStartBotL3 = MinAnchorHookFactor; return; } AnchorEndBotL3 = value; AnchorStartTopL3 = value; }
        partial void OnAnchorEndTopL3Changed(double value) { if (value < MinAnchorHookFactor) AnchorEndTopL3 = MinAnchorHookFactor; }
        partial void OnAnchorEndBotL3Changed(double value) { if (value < MinAnchorHookFactor) AnchorEndBotL3 = MinAnchorHookFactor; }

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
