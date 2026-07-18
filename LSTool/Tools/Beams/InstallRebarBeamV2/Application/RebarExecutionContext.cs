using Autodesk.Revit.DB;
using HcBimUtils;
using HcBimUtils.DocumentUtils;
using LSTool.Tools.Beams.InstallRebarBeamV2.models;
using LSTool.Tools.Beams.InstallRebarBeamV2.viewModels;
using RIMT.Utils.RevitElements;
using RIMT.Utils.RevRebars;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Application
{
    public sealed class RebarExecutionContext
    {
        private readonly IReadOnlyDictionary<string, RebarBarTypeCustom> _barTypesByName;

        private RebarExecutionContext(
            Document document,
            Element temporaryHost,
            XYZ xAxis,
            XYZ yAxis,
            XYZ zAxis,
            IReadOnlyDictionary<string, RebarBarTypeCustom> barTypesByName,
            ElementId targetHostId)
        {
            Document = document;
            TemporaryHost = temporaryHost;
            XAxis = xAxis;
            YAxis = yAxis;
            ZAxis = zAxis;
            _barTypesByName = barTypesByName;
            TargetHostId = targetHostId;
            Metrics = new RebarExecutionMetrics();
        }

        public Document Document { get; }
        public Element TemporaryHost { get; }
        public XYZ XAxis { get; }
        public XYZ YAxis { get; }
        public XYZ ZAxis { get; }
        public ElementId TargetHostId { get; }
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
            var temporaryHost = document.CreateHost(BuiltInCategory.OST_StructuralFraming);

            return new RebarExecutionContext(
                document,
                temporaryHost,
                beam.BoxElement.VTX,
                beam.BoxElement.VTY,
                beam.BoxElement.VTZ,
                barTypesByName,
                primaryBeamMember.Element.Id);
        }

        public RebarBarTypeCustom GetBarType(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("A rebar type name is required.");
            if (!_barTypesByName.TryGetValue(name, out var result))
                throw new InvalidOperationException($"Rebar type '{name}' was not found in the active document.");
            return result;
        }
    }
}
