using LSTool.MVVM.model.Structures;

namespace LSTool.MVVM.viewModel
{
    public class ManageRebarAnchorageLengthVM
    {
        public System.Collections.ObjectModel.ObservableCollection<RebarAnchorageLengthModel> RebarAnchorageLengths { get; set; }
        public CommunityToolkit.Mvvm.Input.RelayCommand OkCommand { get; set; }
        public CommunityToolkit.Mvvm.Input.RelayCommand CancelCommand { get; set; }
    }
}
