namespace LSTool.Tools.Beams.BeamRebar.models
{
    public class BeamBearingModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double Thickness { get; set; } //mm
        public bool IsBeam { get; set; } // true = gối là dầm chính (dầm phụ gác lên)
    }
}
