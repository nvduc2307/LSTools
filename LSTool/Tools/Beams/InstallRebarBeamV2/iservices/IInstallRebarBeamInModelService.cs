using LSTool.Tools.Beams.InstallRebarBeamV2.Application;
using LSTool.Tools.Beams.InstallRebarBeamV2.viewModels;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.iservices
{
    public interface IInstallRebarBeamInModelService
    {
        RebarInstallationResult InstallAll(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel);
    }
}
