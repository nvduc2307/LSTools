using LSTool.Tools.Beams.InstallRebarBeamV2.iservices;
using LSTool.Tools.Beams.InstallRebarBeamV2.models;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.service
{
    public class BeamStressRuleTypeService : IBeamStressRuleTypeService
    {
        public void Update(List<RebarBeam> rebarBeams, BeamStressRuleType beamStressRuleType)
        {
            var qRebarBeams = rebarBeams.Count;
            if (qRebarBeams == 1)
            {
                foreach (var rebarBeam in rebarBeams)
                {
                    rebarBeam.BeamStressRule.Stress =
                        new List<double> { beamStressRuleType.StressStart, 1 - beamStressRuleType.StressStart - beamStressRuleType.StressEnd, beamStressRuleType.StressEnd };
                }
            }
            else
            {
                var index = 0;
                foreach (var rebarBeam in rebarBeams)
                {
                    rebarBeam.BeamStressRule.Stress = index == 0
                        ? new List<double> { beamStressRuleType.StressStart, 1 - beamStressRuleType.StressStart - beamStressRuleType.StressMid, beamStressRuleType.StressMid }
                        : index == qRebarBeams - 1
                        ? new List<double> { beamStressRuleType.StressMid, 1 - beamStressRuleType.StressMid - beamStressRuleType.StressEnd, beamStressRuleType.StressEnd }
                        : new List<double> { beamStressRuleType.StressMid, 1 - beamStressRuleType.StressMid - beamStressRuleType.StressMid, beamStressRuleType.StressMid };
                    index++;
                }
            }
        }
    }
}


