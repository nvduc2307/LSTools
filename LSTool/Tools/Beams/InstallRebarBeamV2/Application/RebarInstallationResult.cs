using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Plans;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Application
{
    public sealed class RebarInstallationResult
    {
        public List<Rebar> TopLevel1 { get; set; } = new();
        public List<Rebar> TopLevel2 { get; set; } = new();
        public List<Rebar> TopLevel3 { get; set; } = new();
        public List<Rebar> BottomLevel1 { get; set; } = new();
        public List<Rebar> BottomLevel2 { get; set; } = new();
        public List<Rebar> BottomLevel3 { get; set; } = new();
        public List<Rebar> SideBars { get; set; } = new();
        public List<Rebar> DantoryBars { get; set; } = new();
        public List<Rebar> MainStirrups { get; set; } = new();
        public List<Rebar> SecondaryVerticalStirrups { get; set; } = new();
        public List<Rebar> SecondaryHorizontalMainStirrups { get; set; } = new();
        public List<Rebar> SecondaryHorizontalSideStirrups { get; set; } = new();
        public ElementId TemporaryHostId { get; set; }
        public ElementId TargetHostId { get; set; }
        public IReadOnlyDictionary<long, ElementId> TargetHostIdsByRebarId { get; set; } =
            new Dictionary<long, ElementId>();
        public IReadOnlyDictionary<long, MainBarRunPlan>
            MainBarRunsByRebarId { get; set; } =
                new Dictionary<long, MainBarRunPlan>();
        public RebarExecutionMetrics Metrics { get; set; }

        public IEnumerable<Rebar> AllRebars => TopLevel1
            .Concat(TopLevel2)
            .Concat(TopLevel3)
            .Concat(BottomLevel1)
            .Concat(BottomLevel2)
            .Concat(BottomLevel3)
            .Concat(SideBars)
            .Concat(DantoryBars)
            .Concat(MainStirrups)
            .Concat(SecondaryVerticalStirrups)
            .Concat(SecondaryHorizontalMainStirrups)
            .Concat(SecondaryHorizontalSideStirrups);

        public IEnumerable<Rebar> AllStirrups => MainStirrups
            .Concat(SecondaryVerticalStirrups)
            .Concat(SecondaryHorizontalMainStirrups)
            .Concat(SecondaryHorizontalSideStirrups);
    }
}
