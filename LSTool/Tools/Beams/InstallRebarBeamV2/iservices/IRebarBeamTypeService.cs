using LSTool.Tools.Beams.InstallRebarBeamV2.viewModels;
using LSTool.Tools.Beams.InstallRebarBeamV2.models;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.iservices
{
    public interface IRebarBeamTypeService
    {
        public void SaveAs(List<RebarBeam> rebarBeamTypes, string nameType, string pathSave);
        public void Apply(InstallRebarBeamV2ViewModel vm);
        public void Delete(List<RebarBeam> rebarBeamTypes, string nameType, string pathSave);
        public void Save(List<RebarBeam> rebarBeamTypes, RebarBeam rebarBeamSave, string pathSave);
    }
}


