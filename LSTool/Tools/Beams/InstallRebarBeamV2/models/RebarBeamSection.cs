namespace LSTool.Tools.Beams.InstallRebarBeamV2.models
{
    public abstract class RebarBeamSection
    {
        public int RebarBeamSectionType { get; set; }
        public RebarBeamTop RebarBeamTop { get; set; }
        public RebarBeamBot RebarBeamBot { get; set; }
        public RebarBeamSideBar RebarBeamSideBar { get; set; }
        public RebarBeamStirrup RebarBeamStirrup { get; set; }
    }

    public class RebarBeamSectionStart : RebarBeamSection
    {
        public RebarBeamSectionStart()
        {
            RebarBeamSectionType = (int)models.RebarBeamSectionType.SectionStart;
        }
    }
    public class RebarBeamSectionMid : RebarBeamSection
    {
        public RebarBeamSectionMid()
        {
            RebarBeamSectionType = (int)models.RebarBeamSectionType.SectionMid;
        }
    }
    public class RebarBeamSectionEnd : RebarBeamSection
    {
        public RebarBeamSectionEnd()
        {
            RebarBeamSectionType = (int)models.RebarBeamSectionType.SectionEnd;
        }
    }
}


