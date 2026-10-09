using LSTool.MVVM.model.Structures;
using System.Collections.ObjectModel;

namespace LSTool.MVVM.viewModel
{
    public class ManageRebarAnchorageHookMainBarVM
    {
        public ObservableCollection<RebarAnchorageHookMainBarModel> RebarAnchorageHookMainBars { get; set; }
        public RelayCommand OkCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }
    }
}
