using LSTool.MVVM.Models;

namespace LSTool.Tools.Columns.ColumnRebar.models
{
    public partial class ColumnConcreteModel : ConcreteModel
    {
        public double HeightBeamZone { get; set; } = 400;
        public ColumnFaceModel FaceLeft { get; set; }
        public ColumnFaceModel FaceTop { get; set; }
        public ColumnFaceModel FaceRight { get; set; }
        public ColumnFaceModel FaceBottom { get; set; }
        public List<string> DiameterDXs { get; set; }
        public List<string> DiameterDYs { get; set; }
        public List<string> DiameterSTs { get; set; }
        [ObservableProperty]
        private string _diameterDX;
        [ObservableProperty]
        private string _diameterDY;
        [ObservableProperty]
        private string _diameterST;
        [ObservableProperty]
        private double _spacingDX;
        [ObservableProperty]
        private double _spacingDY;
        [ObservableProperty]
        private double _spacingST;
        [ObservableProperty]
        private double _spacingSTE;
    }
}
