using LSTool.Tools.Beams.InstallRebarBeamV2.models;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.iservices
{
    public interface IBeamStressRuleTypeService
    {
        public void Update(List<RebarBeam> rebarBeams, BeamStressRuleType beamStressRuleType);
    }
}


