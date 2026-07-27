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
        private readonly BentZTransitionPlanner _bentZTransitionPlanner;

        public MainBarCreationPlanner(ISubInstallRebarBeamInModelService geometryService)
        {
            _geometryService = geometryService
                ?? throw new ArgumentNullException(nameof(geometryService));
            _bentZTransitionPlanner =
                new BentZTransitionPlanner(_geometryService);
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
            var groupInfos = _geometryService.GetRebarBeamGroupLevelInfo(
                viewModel,
                level,
                group);
            var activeInfos = groupInfos
                .Where(info => info.Quantity > 0)
                .ToList();
            if (activeInfos.Count == 0)
            {
                context.DiagnosticLog?.Record(
                    "main.plan.inactive",
                    new
                    {
                        stageName,
                        runCount = 0
                    });
                return new MainBarCreationPlan(
                    stageName,
                    Array.Empty<MainBarRunPlan>());
            }
            if (activeInfos.Any(info =>
                    string.IsNullOrWhiteSpace(info.Diameter)))
            {
                context.DiagnosticLog?.Record(
                    "main.plan.unsupported",
                    new
                    {
                        stageName,
                        code = "ActiveBarTypeMissing"
                    });
                throw new InvalidOperationException(
                    $"{stageName} has an active section without a bar type. "
                    + "Creation was stopped before geometry planning.");
            }
            var activeDiameterNames = activeInfos
                .Select(info => info.Diameter)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (activeDiameterNames.Count > 1)
            {
                context.DiagnosticLog?.Record(
                    "main.plan.unsupported",
                    new
                    {
                        stageName,
                        code = "MultipleBarTypesInOneRunStage",
                        activeDiameterNames
                    });
                throw new InvalidOperationException(
                    $"{stageName} uses multiple active bar types "
                    + $"({string.Join(", ", activeDiameterNames)}). "
                    + "The current lane planner cannot map a safe bar type "
                    + "per run, so creation was stopped.");
            }
            var rebarInfo = activeInfos[0];
            var barType = context.GetBarType(rebarInfo.Diameter);
            var geometry = _geometryService.GetMainBarBeamReals(
                viewModel,
                level,
                group,
                barType.ModelBarDiameter / 4);
            var runs = _bentZTransitionPlanner.Apply(
                viewModel,
                context,
                level,
                group,
                barType,
                geometry,
                stageName);

            return new MainBarCreationPlan(stageName, runs);
        }
    }
}
