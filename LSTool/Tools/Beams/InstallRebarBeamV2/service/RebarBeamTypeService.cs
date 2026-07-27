using Newtonsoft.Json;
using LSTool.Tools.Beams.InstallRebarBeamV2.viewModels;
using LSTool.Tools.Beams.InstallRebarBeamV2.exceptions;
using LSTool.Tools.Beams.InstallRebarBeamV2.iservices;
using LSTool.Tools.Beams.InstallRebarBeamV2.models;
using RIMT.Utils;
using System.IO;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.service
{
    public class RebarBeamTypeService : IRebarBeamTypeService
    {
        private IDrawRebarBeamInCanvasSerice _drawRebarBeamInCanvasSerice;
        public RebarBeamTypeService(
            IDrawRebarBeamInCanvasSerice drawRebarBeamInCanvasSerice)
        {
            _drawRebarBeamInCanvasSerice = drawRebarBeamInCanvasSerice;
        }
        public void Apply(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel)
        {
            try
            {
                var elementInstances = installRebarBeamV2ViewModel.ElementInstances;
                if (elementInstances.RebarBeamTypeSelected.RebarBeamSectionStart == null) throw new Exception(InstallRebarBeamV2Exceptions.EXCEPTION_DATA_NOT_FOUND);
                if (elementInstances.RebarBeamTypeSelected.RebarBeamSectionMid == null) throw new Exception(InstallRebarBeamV2Exceptions.EXCEPTION_DATA_NOT_FOUND);
                if (elementInstances.RebarBeamTypeSelected.RebarBeamSectionEnd == null) throw new Exception(InstallRebarBeamV2Exceptions.EXCEPTION_DATA_NOT_FOUND);
                elementInstances.InitDataRebarBeamApply();
            }
            catch (Exception ex)
            {
                IO.ShowWarning(ex.Message);
            }
        }

        public void Delete(List<RebarBeam> rebarBeamTypes, string nameType, string pathSave)
        {
            try
            {
                List<RebarBeam> rebarBeams = [.. rebarBeamTypes];
                if (rebarBeams.Count == 0) throw new Exception("Khong co type nao");
                var isRebarBeamTypeExist = rebarBeams.FirstOrDefault(x => x.NameType == nameType);
                if (isRebarBeamTypeExist != null)
                {
                    rebarBeams.Remove(isRebarBeamTypeExist);
                    var content = JsonConvert.SerializeObject(rebarBeams);
                    File.WriteAllText(pathSave, content);
                }
            }
            catch (Exception ex)
            {
                IO.ShowWarning(ex.Message);
            }
        }

        public void Save(List<RebarBeam> rebarBeamTypes, RebarBeam rebarBeamSave, string pathSave)
        {
            try
            {
                rebarBeamSave.EnsureMainStirrupShapeSelected();
                List<RebarBeam> rebarBeamSaveNews = [.. new List<RebarBeam>() { rebarBeamSave }];
                var rebarBeamSaveTarget = rebarBeamSaveNews.FirstOrDefault();
                RebarBeam.ResetActionChange(rebarBeamSaveTarget);
                List<RebarBeam> rebarBeams = [.. rebarBeamTypes];
                var rebarBeam = rebarBeams.FirstOrDefault(x => x.NameType == rebarBeamSaveTarget.NameType);
                var indexOf = rebarBeams.IndexOf(rebarBeam);
                rebarBeams.Insert(indexOf, rebarBeamSaveTarget);
                rebarBeams.RemoveAt(indexOf + 1);
                var content = JsonConvert.SerializeObject(rebarBeams);
                File.WriteAllText(pathSave, content);
            }
            catch (Exception)
            {
            }
        }

        public void SaveAs(List<RebarBeam> rebarBeamTypes, string nameType, string pathSave)
        {
            try
            {
                List<RebarBeam> rebarBeams = [.. rebarBeamTypes];
                if (string.IsNullOrEmpty(nameType)) throw new Exception("nameType is not empty");
                var isRebarBeamTypeExist = rebarBeams.Any(x => x.NameType == nameType);
                if (isRebarBeamTypeExist) throw new Exception("Type is existed");
                var rebarBeamType = new RebarBeam
                {
                    NameType = nameType,
                    MainStirrupType1 = true
                };
                rebarBeams.Add(rebarBeamType);
                foreach (var item in rebarBeams)
                {
                    RebarBeam.ResetActionChange(item);
                }
                var content = JsonConvert.SerializeObject(rebarBeams);
                File.WriteAllText(pathSave, content);
            }
            catch (Exception ex)
            {
                IO.ShowWarning(ex.Message);
            }
        }
    }
}


