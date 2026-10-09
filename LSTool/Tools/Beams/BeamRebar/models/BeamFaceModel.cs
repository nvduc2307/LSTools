namespace LSTool.Tools.Beams.BeamRebar.models
{
    public class BeamFaceModel
    {
        public string HostId { get; set; }
        public int FaceType { get; set; }
        public XYZ Pb1 { get; set; }
        public XYZ Pb2 { get; set; }
        public XYZ Pt1 { get; set; }
        public XYZ Pt2 { get; set; }
        public Plane Plane { get; set; }
        public double RebarQty { get; set; }
        public double RebarQtyNext { get; set; }
        public double Cover { get; set; }
        public double CoverBase { get; set; }
        public double Diameter { get; set; }
    }
}
