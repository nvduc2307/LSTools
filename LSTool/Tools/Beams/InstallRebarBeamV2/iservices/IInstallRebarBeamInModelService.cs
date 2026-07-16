using Autodesk.Revit.DB.Structure;
using LSTool.Tools.Beams.InstallRebarBeamV2.viewModels;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.iservices
{
    public interface IInstallRebarBeamInModelService
    {
        public List<Rebar> InstallRebarTop1(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel);
        public List<Rebar> InstallRebarTop2(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel);
        public List<Rebar> InstallRebarTop3(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel);
        public List<Rebar> InstallRebarBot1(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel);
        public List<Rebar> InstallRebarBot2(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel);
        public List<Rebar> InstallRebarBot3(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel);
        public List<Rebar> InstallRebarSide(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel);
        public List<Rebar> InstallRebarStirrup(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel);
        public List<Rebar> InstallRebarSubVerticalStirrup(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel);
        public List<Rebar> InstallRebarSubHorizontalStirrupForMainRebar(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel);
        public List<Rebar> InstallRebarSubHorizontalStirrupForSideRebar(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel);
        public List<Rebar> InstallRebarDantory(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel);
    }
}


