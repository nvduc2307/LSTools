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
            var rebarBeams = viewModel.ElementInstances?.RebarBeams;
            var spanCount = rebarBeams?.Count ?? 0;
            var inputSnapshots = groupInfos
                .Select((info, index) =>
                {
                    var sectionOrdinal = spanCount > 0
                        ? index / spanCount
                        : -1;
                    var spanOrdinal = spanCount > 0
                        ? index % spanCount
                        : index;
                    var rebarBeam = spanCount > 0
                        && spanOrdinal < rebarBeams.Count
                            ? rebarBeams[spanOrdinal]
                            : null;

                    return new
                    {
                        order = index,
                        section = GetSectionName(sectionOrdinal),
                        spanIndex = rebarBeam?.SpanIndex ?? spanOrdinal + 1,
                        beamId = info.HostId,
                        quantity = info.Quantity,
                        diameter = info.Diameter,
                        isActive = info.Quantity > 0
                    };
                })
                .ToList();
            context.DiagnosticLog?.Record(
                "main.plan.inputs",
                new
                {
                    stageName,
                    level = level.ToString(),
                    group = group.ToString(),
                    spanCount,
                    inputs = inputSnapshots
                });
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
                        code = "ActiveBarTypeMissing",
                        activeInputs = inputSnapshots
                            .Where(input => input.isActive)
                            .ToList()
                    });
                throw new InvalidOperationException(
                    $"{stageName} has an active section without a bar type. "
                    + "Creation was stopped before geometry planning.");
            }
            var activeDiameterNames = activeInfos
                .Select(info => info.Diameter)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var requiresUniformDiameter =
                group == RebarBeamMainBarGroupType.GroupLevel1;
            if (requiresUniformDiameter && activeDiameterNames.Count > 1)
            {
                context.DiagnosticLog?.Record(
                    "main.plan.unsupported",
                    new
                    {
                        stageName,
                        code = "MultipleBarTypesInOneRunStage",
                        activeDiameterNames,
                        activeInputs = inputSnapshots
                            .Where(input => input.isActive)
                            .ToList()
                    });
                throw new InvalidOperationException(
                    $"{stageName} uses multiple active bar types "
                    + $"({string.Join(", ", activeDiameterNames)}). "
                    + "The current lane planner cannot map a safe bar type "
                    + "per run, so creation was stopped.");
            }
            var splitByDiameter = activeDiameterNames.Count > 1;
            var runs = new List<MainBarRunPlan>();
            foreach (var diameterName in activeDiameterNames)
            {
                var barType = context.GetBarType(diameterName);
                var diameterFilter = splitByDiameter
                    ? diameterName
                    : null;
                var runStageName = splitByDiameter
                    ? GetDiameterStageName(stageName, diameterName)
                    : stageName;
                var geometry = _geometryService.GetMainBarBeamReals(
                    viewModel,
                    level,
                    group,
                    barType.ModelBarDiameter / 4,
                    diameterFilter);
                var diameterRuns = _bentZTransitionPlanner.Apply(
                    viewModel,
                    context,
                    level,
                    group,
                    barType,
                    geometry,
                    runStageName,
                    diameterFilter);
                runs.AddRange(diameterRuns);
                context.DiagnosticLog?.Record(
                    "main.plan.diameter.completed",
                    new
                    {
                        stageName,
                        runStageName,
                        diameter = diameterName,
                        geometryCount = geometry.Count,
                        runCount = diameterRuns.Count
                    });
            }

            return new MainBarCreationPlan(stageName, runs);
        }

        private static string GetDiameterStageName(
            string stageName,
            string diameterName)
        {
            var safeDiameter = new string((diameterName ?? "unknown")
                .Where(character => char.IsLetterOrDigit(character))
                .ToArray());
            return $"{stageName}.diameter.{safeDiameter}";
        }

        private static string GetSectionName(int sectionOrdinal)
        {
            switch (sectionOrdinal)
            {
                case 0:
                    return "Start";
                case 1:
                    return "Mid";
                case 2:
                    return "End";
                default:
                    return "Unknown";
            }
        }
    }
}
