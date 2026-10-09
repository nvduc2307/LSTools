using LSTool.MVVM.model.Structures;

namespace LSTool.MVVM.viewModel
{
    public class ManageRebarAnchorageHookStirrupVM
    {
        public System.Collections.ObjectModel.ObservableCollection<RebarAnchorageHookStirrupModel> RebarAnchorageHookStirrups { get; set; }
        public CommunityToolkit.Mvvm.Input.RelayCommand OkCommand { get; set; }
        public CommunityToolkit.Mvvm.Input.RelayCommand CancelCommand { get; set; }
    }
}
