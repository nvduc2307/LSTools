namespace LSTool.Tools.Generals.SettingRebarStandard.models
{
    public partial class SettingRebarStandardModelUI : ObservableObject
    {
        [ObservableProperty]
        private double _l1;
        [ObservableProperty]
        private double _g;
        [ObservableProperty]
        private double _l2;
        [ObservableProperty]
        private double _hMin;
    }
    public class SettingRebarStandardModel
    {
        public double L1 { get; set; }
        public double G { get; set; }
        public double L2 { get; set; }
        public double HMin { get; set; }
    }
}
