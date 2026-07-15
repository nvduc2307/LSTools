namespace LSTool.Tools.Columns.ColumnRebar.models
{
    public class ColumnFaceModel
    {
        public string HostId { get; set; }
        public int FaceType { get; set; }
        public XYZ Pb1 { get; set; }
        public XYZ Pb2 { get; set; }
        public XYZ Pt1 { get; set; }
        public XYZ Pt2 { get; set; }
        public Plane Plane { get; set; }
    }
}
