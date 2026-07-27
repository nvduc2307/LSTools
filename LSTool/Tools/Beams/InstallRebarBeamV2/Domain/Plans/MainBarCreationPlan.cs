using Autodesk.Revit.DB;
using LSTool.Tools.Beams.InstallRebarBeamV2.models;
using RIMT.Utils.RevRebars;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Plans
{
    public enum MainBarRunKind
    {
        Legacy = 0,
        BentZTransition = 1,
        IndependentStraightThroughAnchor = 2,
        IndependentBentJointAnchor = 3
    }

    public sealed class MainBarRunPlan
    {
        public string RunId { get; }
        public MainBarRunKind Kind { get; }
        public RebarBeamMainBarLevelType Level { get; }
        public RebarBeamMainBarGroupType Group { get; }
        public int LaneIndex { get; }
        public RebarBarTypeCustom BarType { get; }
        public IReadOnlyList<XYZ> OrderedPoints { get; }
        public IReadOnlyList<long> ParticipatingBeamIds { get; }
        public long TargetHostBeamId { get; }
        public long? JointElementId { get; }
        public double ElevationChangeFt { get; }
        public double CenterlineBendRadiusFt { get; }
        public double RequiredAnchorageLengthFt { get; }
        public double ProvidedAnchorageLengthFt { get; }
        public bool RequiresStrictGeometryValidation =>
            Kind != MainBarRunKind.Legacy;
        public bool IsIndependentJointAnchorage =>
            Kind == MainBarRunKind.IndependentStraightThroughAnchor
            || Kind == MainBarRunKind.IndependentBentJointAnchor;

        public MainBarRunPlan(
            string runId,
            MainBarRunKind kind,
            RebarBeamMainBarLevelType level,
            RebarBeamMainBarGroupType group,
            int laneIndex,
            RebarBarTypeCustom barType,
            IReadOnlyList<XYZ> orderedPoints,
            IReadOnlyList<long> participatingBeamIds,
            long targetHostBeamId,
            long? jointElementId = null,
            double elevationChangeFt = 0.0,
            double centerlineBendRadiusFt = 0.0,
            double requiredAnchorageLengthFt = 0.0,
            double providedAnchorageLengthFt = 0.0)
        {
            if (string.IsNullOrWhiteSpace(runId))
                throw new ArgumentException("A main-bar run id is required.", nameof(runId));
            if (targetHostBeamId <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(targetHostBeamId),
                    "A physical target beam is required.");

            RunId = runId;
            Kind = kind;
            Level = level;
            Group = group;
            LaneIndex = laneIndex;
            BarType = barType ?? throw new ArgumentNullException(nameof(barType));
            OrderedPoints = orderedPoints ?? throw new ArgumentNullException(nameof(orderedPoints));
            ParticipatingBeamIds = participatingBeamIds
                ?? throw new ArgumentNullException(nameof(participatingBeamIds));
            TargetHostBeamId = targetHostBeamId;
            JointElementId = jointElementId;
            ElevationChangeFt = elevationChangeFt;
            CenterlineBendRadiusFt = centerlineBendRadiusFt;
            RequiredAnchorageLengthFt = requiredAnchorageLengthFt;
            ProvidedAnchorageLengthFt = providedAnchorageLengthFt;
        }
    }

    public sealed class MainBarCreationPlan
    {
        public string StageName { get; }
        public IReadOnlyList<MainBarRunPlan> Runs { get; }

        public MainBarCreationPlan(
            string stageName,
            IReadOnlyList<MainBarRunPlan> runs)
        {
            StageName = stageName ?? throw new ArgumentNullException(nameof(stageName));
            Runs = runs ?? throw new ArgumentNullException(nameof(runs));
        }
    }
}
