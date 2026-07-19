using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HcBimUtils;
using HcBimUtils.DocumentUtils;
using LSTool.Tools.Beams.InstallRebarBeamV2.Application.Diagnostics;
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
        public RebarExecutionMetrics Metrics { get; }

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
    }
}
