using LSTool.Tools.Beams.BeamRebar.types;

namespace LSTool.Tools.Beams.BeamRebar.models
{
    public class BeamRebarPositionModel
    {
        public string HostId { get; set; }
        public BeamRebarLayerType Layer { get; set; }
        public BeamRebarZoneType Zone { get; set; }
        public int Index { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double Diameter { get; set; }
    }
}
