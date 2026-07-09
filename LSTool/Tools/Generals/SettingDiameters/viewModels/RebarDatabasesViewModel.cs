using LSTool.Tools.Generals.SettingDiameters.models;
using System.Collections.ObjectModel;

namespace LSTool.Tools.Generals.SettingDiameters.viewModels
{
    public class RebarDatabasesViewModel : ObservableObject
    {
        private ObservableCollection<RebarBarTypeModel> _rebarBarTypes;
        public ObservableCollection<RebarBarTypeModel> RebarBarTypes
        {
            get => _rebarBarTypes;
            set
            {
                _rebarBarTypes = value;
                OnPropertyChanged();
            }
        }
        public RelayCommand OkCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }
        public RelayCommand ResetCommand { get; set; }
    }
}
