using HcBimUtils;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.models
{
    public class CoverBeam
    {
        public double TopCover { get; set; }
        public double BottomCover { get; set; }
        public double RightCover { get; set; }
        public double LeftCover { get; set; }
    }

    public class CoverFootBeam(CoverBeam cover)
    {
        public double TopCover { get; } = cover.TopCover.MmToFoot();
        public double BottomCover { get; } = cover.BottomCover.MmToFoot();
        public double RightCover { get; } = cover.RightCover.MmToFoot();
        public double LeftCover { get; } = cover.LeftCover.MmToFoot();
    }
}


