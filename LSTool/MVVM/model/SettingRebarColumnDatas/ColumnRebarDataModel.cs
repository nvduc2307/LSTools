using CommunityToolkit.Mvvm.ComponentModel;

namespace LSTool.MVVM.model.SettingRebarColumnDatas
{
    public partial class ColumnRebarDataModelUI : ObservableObject
    {
        private int _quantityDX;
        private int _quantityDY;
        public int Id { get; set; }
        public string Name { get; set; }
        [ObservableProperty]
        private int _b;
        [ObservableProperty]
        private int _h;
        [ObservableProperty]
        private string _diameterDX;
        public List<string> DiameterDXs { get; set; }
        public int QuantityDX
        {
            get => _quantityDX;
            set
            {
                _quantityDX = value;
                OnPropertyChanged();
                QuantityDXActionChange?.Invoke(this);
            }
        }
        public Action<ColumnRebarDataModelUI> QuantityDXActionChange { get; set; }
        [ObservableProperty]
        private string _diameterDY;
        public List<string> DiameterDYs { get; set; }
        public int QuantityDY
        {
            get => _quantityDY;
            set
            {
                _quantityDY = value;
                OnPropertyChanged();
                QuantityDYActionChange?.Invoke(this);
            }
        }
        public Action<ColumnRebarDataModelUI> QuantityDYActionChange { get; set; }
        public List<string> DiameterSTEnds { get; set; }
        public string DiameterSTEnd { get; set; }
        public int SpacingSTEnd { get; set; }
        public string DiameterSTMid { get; set; }
        public List<string> DiameterSTMids { get; set; }
        public int SpacingSTMid { get; set; }
    }
    public class ColumnRebarDataModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int B { get; set; }
        public int H { get; set; }
        public string DiameterDX {  get; set; }
        public int QuantityDX {  get; set; }
        public string DiameterDY {  get; set; }
        public int QuantityDY {  get; set; }
        public string DiameterSTEnd { get; set; }
        public int SpacingSTEnd { get; set; }
        public string DiameterSTMid { get; set; }
        public int SpacingSTMid { get; set; }
    }
}
