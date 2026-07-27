namespace LSTool.Tools.Beams.InstallRebarBeamV2.models
{
    public class RebarBeamSideBar : RebarBaseInfo
    {
        private int _quantitySide;
        public int QuantitySide
        {
            get => _quantitySide;
            set
            {
                _quantitySide = value;
                OnPropertyChanged(nameof(QuantitySide));
                QuantitySideChange?.Invoke();
            }
        }
        public Action QuantitySideChange { get; set; }
    }
}


