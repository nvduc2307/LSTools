namespace LSTool.Tools.Generals.SettingRebarStandard.models
{
    public partial class SettingRebarStandardModelUI : ObservableObject
    {
        [ObservableProperty]
        private int _l1;
        [ObservableProperty]
        private int _g;
        [ObservableProperty]
        private int _l2;
        [ObservableProperty]
        private int _hMin;
        private double _lC;
        public double LC
        {
            get => _lC;
            set
            {
                _lC = value;
                OnPropertyChanged();
                LCAction?.Invoke(this);
            }
        }
        public Action<SettingRebarStandardModelUI>? LCAction { get; set; }
        [ObservableProperty]
        private double _eC;
        [ObservableProperty]
        private double _eB;
        [ObservableProperty]
        private double _coverC;
    }
    public class SettingRebarStandardModel
    {
        public int L1 { get; set; }
        public int G { get; set; }
        public int L2 { get; set; }
        public int HMin { get; set; }
        public double LC { get; set; }
        public double EC { get; set; }
        public double EB { get; set; }
        public double CoverC { get; set; }
    }
}
