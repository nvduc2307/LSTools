using LSTool.Tools.Columns.CreateColumn.model;

namespace LSTool.Tools.Columns.CreateColumn.viewModel
{
    public partial class CreateColumnVM : ObservableObject
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

        [ObservableProperty]
        private List<RebarColumnSettingModel> _rebarColumnSettings;
        [ObservableProperty]
        private RebarColumnSettingModel _rebarColumnSetting;
        [ObservableProperty]
        private string _columnSettingName;
        public RelayCommand SaveAsCommand { get; set; }
        public RelayCommand SaveCommand { get; set; }
        public RelayCommand DeleteCommand { get; set; }
        public RelayCommand LoadCommand { get; set; }
        public RelayCommand OkCommand { get; set; }
        public RelayCommand CreateTeiCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }
    }
}
