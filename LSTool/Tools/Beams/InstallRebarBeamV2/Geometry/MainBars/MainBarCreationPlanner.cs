using LSTool.Tools.Beams.InstallRebarBeamV2.Application;
using LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Plans;
using LSTool.Tools.Beams.InstallRebarBeamV2.iservices;
using LSTool.Tools.Beams.InstallRebarBeamV2.models;
using LSTool.Tools.Beams.InstallRebarBeamV2.viewModels;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Geometry.MainBars
{
    public sealed class MainBarCreationPlanner
    {
        private readonly ISubInstallRebarBeamInModelService _geometryService;

        public MainBarCreationPlanner(ISubInstallRebarBeamInModelService geometryService)
        {
            _geometryService = geometryService
                ?? throw new ArgumentNullException(nameof(geometryService));
        }

        public MainBarCreationPlan Plan(
            InstallRebarBeamV2ViewModel viewModel,
            RebarExecutionContext context,
            RebarBeamMainBarLevelType level,
            RebarBeamMainBarGroupType group)
        {
            var levelName = level == RebarBeamMainBarLevelType.RebarTop ? "top" : "bottom";
            var groupNumber = (int)group;
            var stageName = $"{levelName} level {groupNumber}";
            var rebarInfo = _geometryService.GetRebarBeamGroupInfo(
                    viewModel,
                    RebarBeamSectionType.SectionStart,
                    level,
                    group)
                .FirstOrDefault()
                ?? throw new InvalidOperationException($"No {stageName} configuration was found.");
            var barType = context.GetBarType(rebarInfo.Diameter);
            var geometry = _geometryService.GetMainBarBeamReals(
                viewModel,
                level,
                group,
                barType.ModelBarDiameter / 4);

            return new MainBarCreationPlan(stageName, barType, geometry);
        }
    }
}
