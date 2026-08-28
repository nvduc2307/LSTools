using LSTool.MVVM.Models;

namespace LSTool.Tools.Beams.BeamRebar.models
{
    public class BeamRebarModel : ConcreteModel
    {
        public static double COVER = 40;
        public BeamRebarSectionModel SectionStart { get; set; }
        public BeamRebarSectionModel SectionMid { get; set; }
        public BeamRebarSectionModel SectionEnd { get; set; }
        public BeamBearingModel BeamBearingStart { get; set; }
        public BeamBearingModel BeamBearingEnd { get; set; }
    }
}
