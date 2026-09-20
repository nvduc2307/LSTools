using LSTool.Tools.Columns.ColumnRebar.models;
using LSTool.Tools.Generals.SettingRebarStandard.models;
using System.Collections.ObjectModel;

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
        public SettingRebarStandardModelUI SettingRebarStandardModel { get; set; }
        public RelayCommand OkCommand { get; set; }
        public RelayCommand CreateTeiCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }

        // ── Preset management ─────────────────────────────────────────────
        [ObservableProperty]
        private ObservableCollection<ColumnRebarPresetModel> _presets
            = new ObservableCollection<ColumnRebarPresetModel>();

        [ObservableProperty]
        private ColumnRebarPresetModel _selectedPreset;

        [ObservableProperty]
        private string _newPresetName;

        public RelayCommand SaveCommand { get; set; }
        public RelayCommand LoadCommand { get; set; }
        public RelayCommand SaveAsCommand { get; set; }
        public RelayCommand RemoveCommand { get; set; }

        /// Thông báo trạng thái hiển thị inline (tự ẩn sau vài giây)
        [ObservableProperty]
        private string _statusText = string.Empty;
    }
}
