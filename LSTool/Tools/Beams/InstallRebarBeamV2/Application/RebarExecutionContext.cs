using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using LSTool.Compatibility;
using LSTool.Tools.Beams.InstallRebarBeamV2.Application.Diagnostics;
using LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Plans;
using LSTool.Tools.Beams.InstallRebarBeamV2.models;
using LSTool.Tools.Beams.InstallRebarBeamV2.viewModels;
using RIMT.Utils.RevitElements;
using RIMT.Utils.RevRebars;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Application
{
    public sealed class RebarExecutionContext
    {
        private readonly IReadOnlyDictionary<string, RebarBarTypeCustom> _barTypesByName;
        private readonly IReadOnlyDictionary<long, Element> _beamHostsById;
        private readonly Dictionary<long, ElementId> _targetHostIdsByRebarId = new();
        private readonly Dictionary<long, MainBarRunPlan>
            _mainBarRunsByRebarId = new();
        private readonly Dictionary<string, MainBarCreationPlan> _mainBarPlans =
            new(StringComparer.Ordinal);

        private RebarExecutionContext(
            Document document,
            Element temporaryHost,
            XYZ xAxis,
            XYZ yAxis,
            XYZ zAxis,
            IReadOnlyDictionary<string, RebarBarTypeCustom> barTypesByName,
            IReadOnlyDictionary<long, Element> beamHostsById,
            ElementId targetHostId,
            RebarDiagnosticLog diagnosticLog)
        {
            Document = document;
            TemporaryHost = temporaryHost;
            XAxis = xAxis;
            YAxis = yAxis;
            ZAxis = zAxis;
            _barTypesByName = barTypesByName;
            _beamHostsById = beamHostsById;
            TargetHostId = targetHostId;
            DiagnosticLog = diagnosticLog;
            Metrics = new RebarExecutionMetrics();
        }

        public Document Document { get; }
        public Element TemporaryHost { get; }
        public XYZ XAxis { get; }
        public XYZ YAxis { get; }
        public XYZ ZAxis { get; }
        public ElementId TargetHostId { get; }
        public RebarDiagnosticLog DiagnosticLog { get; }
        public IReadOnlyDictionary<long, ElementId> TargetHostIdsByRebarId =>
            _targetHostIdsByRebarId;
        public IReadOnlyDictionary<long, MainBarRunPlan>
            MainBarRunsByRebarId => _mainBarRunsByRebarId;
        public RebarExecutionMetrics Metrics { get; }

        public void RegisterMainBarPlan(
            RebarBeamMainBarLevelType level,
            RebarBeamMainBarGroupType group,
            MainBarCreationPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var key = MainBarPlanKey(level, group);
            if (_mainBarPlans.ContainsKey(key))
            {
                throw new InvalidOperationException(
                    $"Main-bar plan {key} was registered more than once.");
            }
            _mainBarPlans[key] = plan;
            DiagnosticLog?.Record("main.plan.registered", new
            {
                key,
                plan.StageName,
                runCount = plan.Runs.Count,
                bentZRunCount = plan.Runs.Count(
                    run => run.Kind == MainBarRunKind.BentZTransition),
                independentStraightRunCount = plan.Runs.Count(
                    run => run.Kind
                        == MainBarRunKind
                            .IndependentStraightThroughAnchor),
                independentBentRunCount = plan.Runs.Count(
                    run => run.Kind
                        == MainBarRunKind.IndependentBentJointAnchor)
            });
        }

        public MainBarCreationPlan GetMainBarPlan(
            RebarBeamMainBarLevelType level,
            RebarBeamMainBarGroupType group)
        {
            var key = MainBarPlanKey(level, group);
            if (!_mainBarPlans.TryGetValue(key, out var plan))
            {
                throw new InvalidOperationException(
                    $"Main-bar plan {key} is unavailable.");
            }
            return plan;
        }

        public IReadOnlyList<MainBarCreationPlan>
            GetRegisteredMainBarPlans()
        {
            return _mainBarPlans.Values.ToList();
        }

        public static RebarExecutionContext Create(InstallRebarBeamV2ViewModel viewModel)
        {
            if (viewModel?.ElementInstances == null)
                throw new ArgumentNullException(nameof(viewModel));

            var document = AC.Document
                ?? throw new InvalidOperationException("The active Revit document is unavailable.");
            if (!document.IsModifiable)
                throw new InvalidOperationException(
                    "A transaction is required to initialize the rebar execution context.");

            var beam = viewModel.ElementInstances.Beam
                ?? throw new InvalidOperationException("The selected beam model is unavailable.");
            var barTypesByName = viewModel.ElementInstances.RebarBarTypesByName
                ?? throw new InvalidOperationException("Rebar bar types have not been initialized.");
            var primaryBeamMember = beam.ElementSubs.FirstOrDefault()
                ?? throw new InvalidOperationException("The selected beam has no physical members.");
            foreach (var member in beam.ElementSubs)
            {
                if (member?.Element == null
                    || !member.Element.IsValidObject
                    || RebarHostData.GetRebarHostData(member.Element) == null)
                {
                    throw new InvalidOperationException(
                        $"Physical beam {member?.Id} is not a legal Revit "
                        + "rebar host. Enable rebar hosting on the family "
                        + "before running this command.");
                }
            }
            var beamHostsById = beam.ElementSubs.ToDictionary(
                member => member.Id,
                member => member.Element);
            var temporaryHost = document.CreateHost(BuiltInCategory.OST_StructuralFraming);

            var context = new RebarExecutionContext(
                document,
                temporaryHost,
                beam.BoxElement.VTX,
                beam.BoxElement.VTY,
                beam.BoxElement.VTZ,
                barTypesByName,
                beamHostsById,
                primaryBeamMember.Element.Id,
                viewModel.DiagnosticLog);
            context.DiagnosticLog?.Record("execution.context.created", new
            {
                temporaryHostId = temporaryHost.Id.Value,
                defaultTargetHostId = primaryBeamMember.Element.Id.Value,
                physicalHosts = beamHostsById.Select(pair => new
                {
                    beamId = pair.Key,
                    hostId = pair.Value?.Id.Value,
                    hostName = pair.Value?.Name
                }).ToList(),
                barTypeCount = barTypesByName.Count
            });
            return context;
        }

        public RebarBarTypeCustom GetBarType(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("A rebar type name is required.");
            if (!_barTypesByName.TryGetValue(name, out var result))
                throw new InvalidOperationException($"Rebar type '{name}' was not found in the active document.");
            return result;
        }

        public Element GetBeamHost(long beamId)
        {
            if (!_beamHostsById.TryGetValue(beamId, out var host)
                || host == null
                || !host.IsValidObject)
            {
                throw new InvalidOperationException(
                    $"Physical beam host {beamId} is unavailable in the selected span set.");
            }
            return host;
        }

        public void RegisterTargetHost(Rebar rebar, long sourceBeamId)
        {
            if (rebar == null || !rebar.IsValidObject)
                throw new ArgumentException("A valid created rebar is required.", nameof(rebar));
            var targetHost = GetBeamHost(sourceBeamId);
            _targetHostIdsByRebarId[rebar.Id.Value] = targetHost.Id;
            DiagnosticLog?.Record("host.target.registered", new
            {
                rebarId = rebar.Id.Value,
                sourceBeamId,
                targetHostId = targetHost.Id.Value,
                currentHostId = rebar.GetHostId()?.Value
            });
        }

        public void RegisterMainBarRun(
            Rebar rebar,
            MainBarRunPlan run)
        {
            if (rebar == null || !rebar.IsValidObject)
            {
                throw new ArgumentException(
                    "A valid created rebar is required.",
                    nameof(rebar));
            }
            if (run == null) throw new ArgumentNullException(nameof(run));
            if (_mainBarRunsByRebarId.ContainsKey(rebar.Id.Value))
            {
                throw new InvalidOperationException(
                    $"Main-bar rebar {rebar.Id.Value} was registered more "
                    + "than once.");
            }
            _mainBarRunsByRebarId[rebar.Id.Value] = run;
            DiagnosticLog?.Record("main.run.rebar.registered", new
            {
                rebarId = rebar.Id.Value,
                run.RunId,
                kind = run.Kind.ToString()
            });
        }

        private static string MainBarPlanKey(
            RebarBeamMainBarLevelType level,
            RebarBeamMainBarGroupType group)
        {
            return $"{(int)level}:{(int)group}";
        }
    }
}
