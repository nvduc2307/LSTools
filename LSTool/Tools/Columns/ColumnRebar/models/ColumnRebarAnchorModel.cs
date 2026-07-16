namespace LSTool.Tools.Columns.ColumnRebar.models
{
    public class ColumnRebarAnchorModel
    {
        public double AC { get; set; }
    }
    public partial class ColumnRebarAnchorModelUI : ObservableObject
    {
        private double _aC;
        public double AC
        {
            get => _aC;
            set
            {
                _aC = value;
                OnPropertyChanged();
                ACAction?.Invoke(this);
            }
        }
        public Action<ColumnRebarAnchorModelUI> ACAction { get; set; }
    }
}
