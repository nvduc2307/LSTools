using LSTool.MVVM.model.Structures;

namespace LSTool.MVVM.viewModel
{
    public class ManageRebarLapLengthVM
    {
        public System.Collections.ObjectModel.ObservableCollection<RebarLapLengthModel> RebarLapLengths { get; set; }
        public CommunityToolkit.Mvvm.Input.RelayCommand OkCommand { get; set; }
        public CommunityToolkit.Mvvm.Input.RelayCommand CancelCommand { get; set; }
    }
}
