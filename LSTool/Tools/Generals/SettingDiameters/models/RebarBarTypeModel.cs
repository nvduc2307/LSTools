namespace LSTool.Tools.Generals.SettingDiameters.models
{
    public partial class RebarBarTypeModel : ObservableObject
    {
        [ObservableProperty]
        private string _nameStyle;
        [ObservableProperty]
        private double _modelBarDiameter;
        [ObservableProperty]
        private double _barDiameter;
        [ObservableProperty]
        private double _barDiameterReal;
        [ObservableProperty]
        private double _standardBendDiameter;
        [ObservableProperty]
        private double _standardHookBendDiameter;
        [ObservableProperty]
        private double _stirrupOrTieBendDiameter;
        [ObservableProperty]
        private double _maximumBendRadius;
    }
}
