using LSTool.Tools.Beams.BeamRebar.types;

namespace LSTool.Tools.Beams.BeamRebar.models
{
    /// <summary>
    /// Vị trí 1 thanh thép chủ trên tiết diện dầm (dùng lại khi tạo đai móc phụ).
    /// Y, Z là offset (feet) so với tâm dầm theo VTY (ngang) / VTZ (đứng).
    /// Index là vị trí trên lưới chia chung (1 → GridQty), giống ColumnRebarPositionModel.
    /// </summary>
    public class BeamRebarPositionModel
    {
        public string HostId { get; set; }
        public BeamRebarLayerType Layer { get; set; }
        public BeamRebarZoneType Zone { get; set; }
        public int Index { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double Diameter { get; set; } // mm
    }
}
