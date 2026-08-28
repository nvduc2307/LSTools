using LSTool.Tools.Beams.BeamRebar.models;

namespace LSTool.Tools.Beams.BeamRebar.viewModel
{
    public class BeamRebarVM : ObservableObject
    {
        private BeamRebarModel _beamRebarModel;
        public List<BeamRebarModel> BeamRebarModels { get; set; }
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
        public RelayCommand OkCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }
    }
}
