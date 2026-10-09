using LSTool.MVVM.model.Structures;

namespace LSTool.MVVM.viewModel
{
    public class ManageConcreteCoverVM
    {
        public ConcreteCoverModel ColumnCover { get; set; }
        public ConcreteCoverModel WallCover { get; set; }
        public ConcreteCoverModel BeamCover { get; set; }
        public ConcreteCoverModel SlabCover { get; set; }
        public RelayCommand OkCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }
    }
}
