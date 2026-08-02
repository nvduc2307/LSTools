using Autodesk.Revit.DB;
using LSTool.Tools.Beams.InstallRebarBeamV2.viewModels;
using LSTool.Tools.Beams.InstallRebarBeamV2.models;
using RIMT.Utils.BoundingBoxs;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.iservices
{
    public interface ISubInstallRebarBeamInModelService
    {
        public void GenerateRebarDeverlop(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            MainBarBeamReal mainBarBeamReal);
        public List<MainBarBeamReal> GetSideBarBeamReals(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            double extentCover);
        public List<MainBarBeamReal> GetMainBarBeamReals(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            RebarBeamMainBarLevelType rebarBeamMainBarLevelType,
            RebarBeamMainBarGroupType rebarBeamMainBarGroupType,
            double extentCover,
            string diameterFilter = null);
        public List<RebarBeamMainBar> GetRebarBeamAllSection(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel);
        public List<RebarBeamSectionStart> GetRebarBeamSectionStart(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            RebarBeamSectionType sectionType);
        public List<RebarBeamSectionMid> GetRebarBeamSectionMid(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            RebarBeamSectionType sectionType);
        public List<RebarBeamSectionEnd> GetRebarBeamSectionEnd(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            RebarBeamSectionType sectionType);
        public List<RebarBeamMainBar> GetRebarBeamGroupLevelInfo(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            RebarBeamMainBarLevelType rebarBeamMainBarLevelType,
            RebarBeamMainBarGroupType rebarBeamMainBarGroupType);
        public List<RebarBeamMainBar> GetRebarBeamGroupInfo(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            RebarBeamSectionType sectionType,
            RebarBeamMainBarLevelType rebarBeamMainBarLevelType,
            RebarBeamMainBarGroupType rebarBeamMainBarGroupType);
        public List<RebarBeamStirrup> GetStirrupGroupInfo(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            RebarBeamSectionType sectionType);
        public List<XYZ> GetPointControls(
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            BoxElement boxElement,
            RebarBeamMainBarLevelType rebarBeamMainBarLevelType,
            RebarBeamMainBarGroupType rebarBeamMainBarGroupType,
            double coverFt,
            double extentCoverSide = 0);
    }
}


