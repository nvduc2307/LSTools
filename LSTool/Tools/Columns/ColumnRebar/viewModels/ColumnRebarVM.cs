using LSTool.Tools.Columns.ColumnRebar.models;

namespace LSTool.Tools.Columns.ColumnRebar.viewModels
{
    public partial class ColumnRebarVM : ObservableObject
    {
        private ColumnConcreteModel _columnConcreteModel;
        [ObservableProperty]
        private List<ColumnConcreteModel> _columnConcreteModels;
        public ColumnConcreteModel ColumnConcreteModel
        {
            get => _columnConcreteModel;
            set
            {
                _columnConcreteModel = value;
                OnPropertyChanged();
                ColumnConcreteModelAction?.Invoke();
            }
        }
        public Action ColumnConcreteModelAction { get; set; }
        public ColumnRebarAnchorModelUI ColumnRebarAnchorModelUI { get; set; }
        public RelayCommand OkCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }
    }
}
