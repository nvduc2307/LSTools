using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using LSTool.Compatibility;
using LSTool.Tools.Beams.InstallRebarBeamV2.Application;
using LSTool.Tools.Beams.InstallRebarBeamV2.Application.Diagnostics;
using LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Geometry;
using LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Plans;
using LSTool.Tools.Beams.InstallRebarBeamV2.iservices;
using LSTool.Tools.Beams.InstallRebarBeamV2.models;
using LSTool.Tools.Beams.InstallRebarBeamV2.viewModels;
using RIMT.Utils.BoundingBoxs;
using RIMT.Utils.Solids;
using RIMT.Utils.RevRebars;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Geometry.MainBars
{
    /// <summary>
    /// Adapts the pure Bent/Z station/elevation kernel to validated Revit beam
    /// and column geometry. The adapter is deliberately narrow: phase one
    /// supports exactly two straight, horizontal, collinear physical beams and
    /// one unambiguous structural column at their joint.
    /// </summary>
    public sealed class BentZTransitionPlanner
    {
        private const double DirectionTolerance = 1e-6;
        private const double RectangularVolumeRelativeTolerance = 1e-6;
        private readonly ISubInstallRebarBeamInModelService _geometryService;

        public BentZTransitionPlanner(
            ISubInstallRebarBeamInModelService geometryService)
        {
            _geometryService = geometryService
                ?? throw new ArgumentNullException(nameof(geometryService));
        }

        public IReadOnlyList<MainBarRunPlan> Apply(
            InstallRebarBeamV2ViewModel viewModel,
            RebarExecutionContext context,
            RebarBeamMainBarLevelType level,
            RebarBeamMainBarGroupType group,
            RebarBarTypeCustom barType,
            IReadOnlyList<MainBarBeamReal> legacyGeometry,
            string stageName)
        {
            if (viewModel == null) throw new ArgumentNullException(nameof(viewModel));
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (barType == null) throw new ArgumentNullException(nameof(barType));
            if (legacyGeometry == null)
                throw new ArgumentNullException(nameof(legacyGeometry));

            var defaultHostBeamId = viewModel.ElementInstances.Beam.ElementSubs
                .FirstOrDefault()?.Id
                ?? throw new InvalidOperationException(
                    "The selected beam set has no physical target host.");
            var legacyRuns = CreateLegacyRuns(
                legacyGeometry,
                level,
                group,
                barType,
                defaultHostBeamId,
                stageName);

            if (legacyGeometry.Count == 0)
            {
                var configuredQuantity = _geometryService
                    .GetRebarBeamGroupLevelInfo(viewModel, level, group)
                    .Select(bar => bar.Quantity)
                    .DefaultIfEmpty(0)
                    .Max();
                if (configuredQuantity > 0)
                {
                    throw Unsupported(
                        context,
                        stageName,
                        "ConfiguredBarsHaveNoGeometry",
                        "The group has a positive configured quantity, but "
                        + "the geometry service returned no main-bar runs.");
                }
                return legacyRuns;
            }

            var geometryToleranceFt = Math.Max(
                context.Document.Application.ShortCurveTolerance,
                1.0.MmToFoot());
            var maximumBentZDeltaMm =
                viewModel.SettingRebarStandardModel?.EB ?? 0.0;
            if (double.IsNaN(maximumBentZDeltaMm)
                || double.IsInfinity(maximumBentZDeltaMm)
                || maximumBentZDeltaMm <= 0.0)
            {
                throw Unsupported(
                    context,
                    stageName,
                    "InvalidRebarStandardEB",
                    "Rebar standard eB must be greater than zero to "
                    + "classify the Bent/Z elevation change.");
            }
            var terminalRuns = legacyGeometry
                .Select((geometry, index) => new
                {
                    Geometry = geometry,
                    Index = index,
                    TerminalPath = ExtractTerminalPath(
                        geometry,
                        context.XAxis,
                        geometryToleranceFt)
                })
                .ToList();
            var policyClassification =
                MainBarTransitionPolicyClassifier.Classify(
                    terminalRuns
                        .Select(item =>
                            item.TerminalPath.CoreEnd.Z
                            - item.TerminalPath.CoreStart.Z)
                        .ToList(),
                    geometryToleranceFt,
                    maximumBentZDeltaMm.MmToFoot());
            context.DiagnosticLog?.Record(
                "main.transition.policy.classified",
                new
                {
                    stageName,
                    level = level.ToString(),
                    group = group.ToString(),
                    isValid = policyClassification.IsValid,
                    policy = policyClassification.Policy.ToString(),
                    failure = policyClassification.Failure.ToString(),
                    source = "BarCenterlineDeltaZ",
                    alignmentToleranceMm = Math.Round(
                        geometryToleranceFt.FootToMm(),
                        3),
                    maximumBentZDeltaMm,
                    maximumBentZDeltaSource =
                        "SettingRebarStandardModelUI.EB",
                    minimumLaneDeltaZMm = Math.Round(
                        policyClassification.MinimumDeltaZ.FootToMm(),
                        3),
                    maximumLaneDeltaZMm = Math.Round(
                        policyClassification.MaximumDeltaZ.FootToMm(),
                        3),
                    laneDeltaZValuesMm = terminalRuns
                        .Select(item => Math.Round(
                            Math.Abs(
                                item.TerminalPath.CoreEnd.Z
                                - item.TerminalPath.CoreStart.Z)
                            .FootToMm(),
                            3))
                        .ToList()
                });
            if (!policyClassification.IsValid)
            {
                throw Unsupported(
                    context,
                    stageName,
                    $"TransitionPolicy{policyClassification.Failure}",
                    policyClassification.Message);
            }

            if (policyClassification.Policy
                == MainBarTransitionPolicy.LegacyAligned)
            {
                context.DiagnosticLog?.Record("main.transition.classified", new
                {
                    stageName,
                    policy = "LegacyAligned",
                    reason = "All bar-centerline elevation differences are "
                        + "within the alignment tolerance.",
                    policySource = "BarCenterlineDeltaZ",
                    runCount = legacyRuns.Count
                });
                ValidateMainBarSeparation(
                    legacyRuns,
                    context,
                    stageName,
                    geometryToleranceFt);
                return legacyRuns;
            }

            var joint = ResolveJointGeometry(
                viewModel,
                context,
                geometryToleranceFt);
            if (policyClassification.Policy
                == MainBarTransitionPolicy.IndependentAnchorage35D)
            {
                return PlanIndependentJointAnchorage(
                    viewModel,
                    context,
                    level,
                    group,
                    barType,
                    legacyGeometry,
                    stageName,
                    joint,
                    geometryToleranceFt);
            }

            var expectedTransitionRunCount = ValidateJointBarCompatibility(
                viewModel,
                context,
                level,
                group,
                joint,
                stageName);
            var laneAlignmentToleranceFt = Math.Min(
                geometryToleranceFt,
                0.01.MmToFoot());
            var laneValidation = BentZTransitionGeometry.ValidateLaneSet(
                terminalRuns
                    .Select(item => new BentZLanePair(
                        item.TerminalPath.CoreStart.DotProduct(joint.AxisY),
                        item.TerminalPath.CoreEnd.DotProduct(joint.AxisY)))
                    .ToList(),
                expectedTransitionRunCount,
                laneAlignmentToleranceFt,
                Math.Max(
                    barType.ModelBarDiameter,
                    barType.BarDiameter)
                + 0.1.MmToFoot());
            if (!laneValidation.IsValid)
            {
                throw Unsupported(
                    context,
                    stageName,
                    $"Transition{laneValidation.Failure}",
                    laneValidation.Message
                    + " No partial or overlapping Bent/Z result will be created.");
            }

            var bendClearance = CalculateBendClearance(
                viewModel,
                context,
                barType,
                joint,
                stageName,
                true);
            var minimumSegmentLengthFt = Math.Max(
                context.Document.Application.ShortCurveTolerance * 2.0,
                barType.StandardBendDiameter
                + Math.Max(barType.ModelBarDiameter, barType.BarDiameter));
            var minimumStraightAfterBendFt = Math.Max(
                context.Document.Application.ShortCurveTolerance * 2.0,
                Math.Max(barType.ModelBarDiameter, barType.BarDiameter));

            var runs = new List<MainBarRunPlan>(legacyGeometry.Count);
            for (var index = 0; index < legacyGeometry.Count; index++)
            {
                var geometry = legacyGeometry[index];
                var terminalPath = ExtractTerminalPath(
                    geometry,
                    joint.AxisX,
                    geometryToleranceFt);
                var elevationChange =
                    terminalPath.CoreEnd.Z - terminalPath.CoreStart.Z;

                if (Math.Abs(elevationChange) <= geometryToleranceFt)
                {
                    runs.Add(legacyRuns[index]);
                    continue;
                }

                var run = PlanTransitionRun(
                    terminalPath,
                    joint,
                    level,
                    group,
                    barType,
                    index,
                    stageName,
                    bendClearance.BendInsetFt,
                    bendClearance.CenterlineClearanceFt,
                    bendClearance.CenterlineBendRadiusFt,
                    bendClearance.MainBarRadiusFt,
                    bendClearance.ModeledColumnRebars,
                    minimumSegmentLengthFt,
                    minimumStraightAfterBendFt,
                    geometryToleranceFt,
                    context);
                runs.Add(run);
            }

            ValidateMainBarSeparation(
                runs,
                context,
                stageName,
                geometryToleranceFt);
            context.DiagnosticLog?.Record("main.transition.classified", new
            {
                stageName,
                policy = "BentZThroughColumn",
                jointColumnId = joint.ColumnId,
                leftBeamId = joint.LeftBeam.Id,
                rightBeamId = joint.RightBeam.Id,
                columnStartMm = Math.Round(joint.ColumnStart.FootToMm(), 3),
                columnEndMm = Math.Round(joint.ColumnEnd.FootToMm(), 3),
                bendPointPlacement = "InsideAdjacentBeams",
                bendOffsetIntoBeamMm = Math.Round(
                    bendClearance.BendInsetFt.FootToMm(),
                    3),
                centerlineClearanceMm = Math.Round(
                    bendClearance.CenterlineClearanceFt.FootToMm(),
                    3),
                centerlineBendRadiusMm = Math.Round(
                    bendClearance.CenterlineBendRadiusFt.FootToMm(),
                    3),
                columnCoverMm = Math.Round(
                    bendClearance.ColumnCoverFt.FootToMm(),
                    3),
                jointReinforcementClearanceMm = Math.Round(
                    bendClearance.JointReinforcementDiameterFt.FootToMm(),
                    3),
                usedModeledJointReinforcement =
                    bendClearance.UsedModeledColumnReinforcement,
                modeledJointRebarCount =
                    bendClearance.ModeledColumnRebarCount,
                modeledColumnStirrupCount =
                    bendClearance.ModeledColumnStirrupCount,
                usedConfiguredBeamStirrupFallback =
                    bendClearance
                        .UsedConfiguredBeamStirrupFallback,
                transitionRunCount = runs.Count(run =>
                    run.Kind == MainBarRunKind.BentZTransition)
            });

            return runs;
        }

        private IReadOnlyList<MainBarRunPlan>
            PlanIndependentJointAnchorage(
                InstallRebarBeamV2ViewModel viewModel,
                RebarExecutionContext context,
                RebarBeamMainBarLevelType level,
                RebarBeamMainBarGroupType group,
                RebarBarTypeCustom barType,
                IReadOnlyList<MainBarBeamReal> legacyGeometry,
                string stageName,
                BeamJointGeometry joint,
                double toleranceFt)
        {
            var expectedLaneCount = ValidateJointBarCompatibility(
                viewModel,
                context,
                level,
                group,
                joint,
                stageName);
            if (legacyGeometry.Count != expectedLaneCount)
            {
                throw Unsupported(
                    context,
                    stageName,
                    "IndependentAnchorLaneCountMismatch",
                    $"The joint requires {expectedLaneCount} source lanes, "
                    + $"but legacy geometry exposed {legacyGeometry.Count}.");
            }

            var nominalBarDiameterFt = barType.BarDiameter;
            double requiredAnchorageFt;
            try
            {
                requiredAnchorageFt =
                    TemporaryIndependentJointAnchorageRule
                        .GetRequiredLength(nominalBarDiameterFt);
            }
            catch (Exception exception)
            {
                throw Unsupported(
                    context,
                    stageName,
                    "IndependentAnchorDiameterInvalid",
                    $"Rebar type '{barType.NameStyle}' has no valid nominal "
                    + $"diameter for the temporary 35D rule: "
                    + exception.Message);
            }

            var bendClearance = CalculateBendClearance(
                viewModel,
                context,
                barType,
                joint,
                stageName,
                true);
            var modelBarDiameterFt = Math.Max(
                barType.ModelBarDiameter,
                barType.BarDiameter);
            var minimumStraightLengthFt = Math.Max(
                context.Document.Application.ShortCurveTolerance * 2.0,
                modelBarDiameterFt);
            var laneToleranceFt = Math.Min(
                toleranceFt,
                0.01.MmToFoot());

            var lanes = legacyGeometry
                .Select((geometry, index) =>
                    ResolveIndependentLane(
                        geometry,
                        index,
                        joint,
                        level,
                        toleranceFt,
                        context,
                        stageName))
                .ToList();
            var bentBeam = lanes[0].BentBeam;
            var straightBeam = lanes[0].StraightBeam;
            if (lanes.Any(lane =>
                    lane.BentBeam.Id != bentBeam.Id
                    || lane.StraightBeam.Id != straightBeam.Id))
            {
                throw Unsupported(
                    context,
                    stageName,
                    "IndependentAnchorLaneRoleMismatch",
                    "Independent-anchor lanes disagree about which physical "
                    + "beam owns the bent and straight-through runs.");
            }
            ValidateIndependentSourceLaneSet(
                lanes.Select(lane => lane.StraightLaneY).ToList(),
                expectedLaneCount,
                modelBarDiameterFt,
                laneToleranceFt,
                "Straight",
                context,
                stageName);
            ValidateIndependentSourceLaneSet(
                lanes.Select(lane => lane.BentLaneY).ToList(),
                expectedLaneCount,
                modelBarDiameterFt,
                laneToleranceFt,
                "Bent",
                context,
                stageName);

            var bentLaneEnvelope = ResolveConfiguredBeamLaneEnvelope(
                viewModel,
                context,
                level,
                group,
                barType,
                bentBeam,
                joint.AxisY,
                stageName);
            var straightLaneEnvelope = ResolveConfiguredBeamLaneEnvelope(
                viewModel,
                context,
                level,
                group,
                barType,
                straightBeam,
                joint.AxisY,
                stageName);
            var straightMinY = Math.Max(
                Math.Max(
                    straightLaneEnvelope.MinY,
                    bentLaneEnvelope.MinY),
                joint.ColumnMinY
                    + bendClearance.CenterlineClearanceFt);
            var straightMaxY = Math.Min(
                Math.Min(
                    straightLaneEnvelope.MaxY,
                    bentLaneEnvelope.MaxY),
                joint.ColumnMaxY
                    - bendClearance.CenterlineClearanceFt);
            foreach (var lane in lanes)
            {
                if (!IsInside(
                        lane.StraightLaneY,
                        straightMinY,
                        straightMaxY,
                        laneToleranceFt))
                {
                    throw Unsupported(
                        context,
                        stageName,
                        "IndependentStraightLaneOutsideEnvelope",
                        $"Straight-through lane {lane.LaneIndex + 1} cannot "
                        + "remain inside the cover-reduced straight-side beam, "
                        + "column and bent-side beam envelopes.");
                }
            }

            var bentMinY = Math.Max(
                bentLaneEnvelope.MinY,
                joint.ColumnMinY
                    + bendClearance.CenterlineClearanceFt);
            var bentMaxY = Math.Min(
                bentLaneEnvelope.MaxY,
                joint.ColumnMaxY
                    - bendClearance.CenterlineClearanceFt);
            if (lanes.Count > 1)
            {
                // Do not move an outer bar farther toward the beam face than
                // the already-approved legacy control line. This preserves the
                // configured cage even if a custom/asymmetric cover produces
                // a narrower usable interval than the physical beam box.
                bentMinY = Math.Max(
                    bentMinY,
                    lanes.Min(lane => lane.BentLaneY));
                bentMaxY = Math.Min(
                    bentMaxY,
                    lanes.Max(lane => lane.BentLaneY));
            }
            var requiredLaneSeparationFt =
                modelBarDiameterFt + 0.2.MmToFoot();
            var staggerInput =
                new IndependentJointLaneStaggerInput(
                    lanes
                        .Select(lane => lane.BentLaneY)
                        .ToList(),
                    lanes
                        .Select(lane => lane.StraightLaneY)
                        .ToList(),
                    bentMinY,
                    bentMaxY,
                    requiredLaneSeparationFt,
                    1.0,
                    laneToleranceFt);
            var staggerPlan =
                IndependentJointLaneStaggerGeometry.Plan(
                    staggerInput);
            if (staggerPlan.Status
                != IndependentJointLaneStaggerStatus.Planned)
            {
                throw Unsupported(
                    context,
                    stageName,
                    $"IndependentAnchor{staggerPlan.Failure}",
                    "The bent-side anchors cannot be staggered safely "
                    + "from the straight-through anchors: "
                    + staggerPlan.Message);
            }

            var verticalLimitZ =
                level == RebarBeamMainBarLevelType.RebarBot
                    ? Math.Min(
                          bentBeam.TopZ,
                          straightBeam.TopZ)
                      - bendClearance.CenterlineClearanceFt
                    : Math.Max(
                          bentBeam.BottomZ,
                          straightBeam.BottomZ)
                      + bendClearance.CenterlineClearanceFt;
            var runs = new List<MainBarRunPlan>(
                lanes.Count * 2);
            var closestModeledClearancesMm =
                new List<double>();

            for (var laneIndex = 0;
                 laneIndex < lanes.Count;
                 laneIndex++)
            {
                var lane = lanes[laneIndex];
                var shiftedBentY =
                    staggerPlan.ShiftedBentLaneYs[laneIndex];
                var direction =
                    lane.StraightSide.Station
                    >= lane.BentSide.Station
                        ? 1.0
                        : -1.0;
                var jointStart = direction > 0.0
                    ? joint.ColumnStart
                    : joint.ColumnEnd;
                var jointEnd = direction > 0.0
                    ? joint.ColumnEnd
                    : joint.ColumnStart;
                var input =
                    new IndependentJointAnchorageInput(
                        lane.BentSide.Station,
                        jointStart,
                        jointEnd,
                        lane.StraightSide.Station,
                        lane.BentSide.CorePoint.Z,
                        lane.StraightSide.CorePoint.Z,
                        verticalLimitZ,
                        requiredAnchorageFt,
                        bendClearance.BendInsetFt,
                        bendClearance.CenterlineClearanceFt,
                        bendClearance.CenterlineBendRadiusFt,
                        minimumStraightLengthFt,
                        toleranceFt);
                var planned =
                    IndependentJointAnchorageGeometry.Plan(
                        input);
                if (planned.Status
                    != IndependentJointAnchorageStatus.Planned)
                {
                    throw Unsupported(
                        context,
                        stageName,
                        $"IndependentAnchor{planned.Failure}",
                        $"Lane {laneIndex + 1} cannot satisfy the temporary "
                        + $"35D anchorage rule: {planned.Message}");
                }

                var straightOrderedPoints =
                    BuildIndependentOrderedPoints(
                        lane.StraightSide,
                        planned.StraightThroughPoints,
                        lane.StraightLaneY,
                        joint,
                        toleranceFt);
                var bentOrderedPoints =
                    BuildIndependentOrderedPoints(
                        lane.BentSide,
                        planned.BentVerticalPoints,
                        shiftedBentY,
                        joint,
                        toleranceFt);

                ValidateIndependentContainment(
                    lane,
                    planned,
                    lane.StraightLaneY,
                    shiftedBentY,
                    bentBeam,
                    straightBeam,
                    joint,
                    bentLaneEnvelope,
                    straightLaneEnvelope,
                    bendClearance.CenterlineClearanceFt,
                    toleranceFt,
                    context,
                    stageName);

                var straightRun = new MainBarRunPlan(
                    $"{stageName}.lane.{laneIndex + 1}.straight",
                    MainBarRunKind
                        .IndependentStraightThroughAnchor,
                    level,
                    group,
                    laneIndex,
                    barType,
                    straightOrderedPoints,
                    new[] { straightBeam.Id, bentBeam.Id },
                    straightBeam.Id,
                    joint.ColumnId,
                    0.0,
                    bendClearance.CenterlineBendRadiusFt,
                    requiredAnchorageFt,
                    planned.StraightProvidedAnchorageLength);
                var bentRun = new MainBarRunPlan(
                    $"{stageName}.lane.{laneIndex + 1}.bent",
                    MainBarRunKind.IndependentBentJointAnchor,
                    level,
                    group,
                    laneIndex,
                    barType,
                    bentOrderedPoints,
                    new[] { bentBeam.Id },
                    bentBeam.Id,
                    joint.ColumnId,
                    planned.BentVerticalPoints[
                        planned.BentVerticalPoints.Count - 1]
                        .Elevation
                    - planned.BentVerticalPoints[0].Elevation,
                    bendClearance.CenterlineBendRadiusFt,
                    requiredAnchorageFt,
                    planned.BentProvidedAnchorageLength);

                foreach (var plannedRun in new[]
                         {
                             straightRun,
                             bentRun
                         })
                {
                    var clash = ValidateNoModeledJointRebarClash(
                        CreatePlannedRunCenterline(
                            plannedRun,
                            toleranceFt),
                        bendClearance.MainBarRadiusFt,
                        bendClearance.ModeledColumnRebars,
                        stageName,
                        context);
                    if (clash.ClosestSurfaceClearanceFt.HasValue)
                    {
                        closestModeledClearancesMm.Add(
                            clash.ClosestSurfaceClearanceFt.Value
                                .FootToMm());
                    }
                }

                runs.Add(straightRun);
                runs.Add(bentRun);
                context.DiagnosticLog?.Record(
                    "main.independent-anchor.run.planned",
                    new
                    {
                        stageName,
                        lane = laneIndex + 1,
                        straightRunId = straightRun.RunId,
                        bentRunId = bentRun.RunId,
                        barLevel = level.ToString(),
                        straightHostBeamId = straightBeam.Id,
                        bentHostBeamId = bentBeam.Id,
                        nominalBarDiameterMm = Math.Round(
                            nominalBarDiameterFt.FootToMm(),
                            3),
                        anchorageMultiplier =
                            TemporaryIndependentJointAnchorageRule
                                .DiameterMultiplier,
                        requiredAnchorageMm = Math.Round(
                            requiredAnchorageFt.FootToMm(),
                            3),
                        straightProvidedAnchorageMm = Math.Round(
                            planned.StraightProvidedAnchorageLength
                                .FootToMm(),
                            3),
                        bentProvidedAnchorageMm = Math.Round(
                            planned.BentProvidedAnchorageLength
                                .FootToMm(),
                            3),
                        originalBentLaneYMm = Math.Round(
                            lane.BentLaneY.FootToMm(),
                            3),
                        shiftedBentLaneYMm = Math.Round(
                            shiftedBentY.FootToMm(),
                            3),
                        bentLaneShiftMm = Math.Round(
                            (shiftedBentY - lane.BentLaneY)
                                .FootToMm(),
                            3),
                        shiftScope = "EntireBentSideRun",
                        straightAnchorageDatum =
                            "Straight-side column face to bent-side tail",
                        bentAnchorageDatum =
                            "Sharp bend vertex to vertical tail endpoint"
                    });
            }

            ValidateMainBarSeparation(
                runs,
                context,
                stageName,
                toleranceFt);
            context.DiagnosticLog?.Record(
                "main.transition.classified",
                new
                {
                    stageName,
                    policy = "IndependentJointAnchorage35D",
                    policySource = "BarCenterlineDeltaZ",
                    barLevel = level.ToString(),
                    jointColumnId = joint.ColumnId,
                    straightBeamId = straightBeam.Id,
                    bentBeamId = bentBeam.Id,
                    sourceLaneCount = lanes.Count,
                    createdRunCount = runs.Count,
                    requiredAnchorageMm = Math.Round(
                        requiredAnchorageFt.FootToMm(),
                        3),
                    maximumBentLaneShiftMm = Math.Round(
                        staggerPlan.MaximumAbsoluteDisplacement
                            .FootToMm(),
                        3),
                    bentBeamLaneMinMm = Math.Round(
                        bentLaneEnvelope.MinY.FootToMm(),
                        3),
                    bentBeamLaneMaxMm = Math.Round(
                        bentLaneEnvelope.MaxY.FootToMm(),
                        3),
                    straightBeamLaneMinMm = Math.Round(
                        straightLaneEnvelope.MinY.FootToMm(),
                        3),
                    straightBeamLaneMaxMm = Math.Round(
                        straightLaneEnvelope.MaxY.FootToMm(),
                        3),
                    laneShiftScope = "EntireBentSideRun",
                    straightAnchorageDatum =
                        "Straight-side column face to bent-side tail",
                    bentAnchorageDatum =
                        "Sharp bend vertex to vertical tail endpoint",
                    columnCoverMm = Math.Round(
                        bendClearance.ColumnCoverFt.FootToMm(),
                        3),
                    jointReinforcementClearanceMm = Math.Round(
                        bendClearance.JointReinforcementDiameterFt
                            .FootToMm(),
                        3),
                    usedConfiguredBeamStirrupFallback =
                        bendClearance
                            .UsedConfiguredBeamStirrupFallback,
                    closestModeledRebarSurfaceClearanceMm =
                        closestModeledClearancesMm.Count == 0
                            ? (double?)null
                            : Math.Round(
                                closestModeledClearancesMm.Min(),
                                3)
                });
            return runs;
        }

        private BeamLaneEnvelope ResolveConfiguredBeamLaneEnvelope(
            InstallRebarBeamV2ViewModel viewModel,
            RebarExecutionContext context,
            RebarBeamMainBarLevelType level,
            RebarBeamMainBarGroupType group,
            RebarBarTypeCustom barType,
            BeamEnvelope beam,
            XYZ axisY,
            string stageName)
        {
            try
            {
                var member = viewModel.ElementInstances.Beam.ElementSubs
                    .SingleOrDefault(item => item.Id == beam.Id)
                    ?? throw new InvalidOperationException(
                        $"Physical beam {beam.Id} is unavailable.");
                var stirrup = _geometryService
                    .GetStirrupGroupInfo(
                        viewModel,
                        RebarBeamSectionType.SectionStart)
                    .LastOrDefault()
                    ?? throw new InvalidOperationException(
                        "The start-section stirrup configuration is unavailable.");
                var stirrupType = context.GetBarType(stirrup.Diameter);
                var stirrupDiameterFt = stirrupType.ModelBarDiameter > 0.0
                    ? stirrupType.ModelBarDiameter
                    : stirrupType.BarDiameter;
                var mainBarDiameterFt = barType.ModelBarDiameter > 0.0
                    ? barType.ModelBarDiameter
                    : barType.BarDiameter;
                if (stirrupDiameterFt <= 0.0
                    || mainBarDiameterFt <= 0.0)
                {
                    throw new InvalidOperationException(
                        "The beam stirrup or main-bar model diameter is not positive.");
                }

                // Keep these arguments identical to MainBarGeometryService so
                // the stagger solver works inside the same cover/stirrup cage
                // that produced the original legacy lanes.
                var centerlineCoverFt =
                    stirrupDiameterFt + mainBarDiameterFt / 2.0;
                var additionalSideInsetFt =
                    mainBarDiameterFt / 4.0;
                var controlPoints = _geometryService.GetPointControls(
                    viewModel,
                    member,
                    level,
                    group,
                    centerlineCoverFt,
                    additionalSideInsetFt);
                if (controlPoints == null || controlPoints.Count < 2
                    || controlPoints.Any(point => point == null))
                {
                    throw new InvalidOperationException(
                        "The configured main-bar control line is unavailable.");
                }

                var laneYs = controlPoints
                    .Select(point => point.DotProduct(axisY))
                    .ToList();
                var minimum = laneYs.Min();
                var maximum = laneYs.Max();
                if (double.IsNaN(minimum)
                    || double.IsInfinity(minimum)
                    || double.IsNaN(maximum)
                    || double.IsInfinity(maximum)
                    || maximum <= minimum)
                {
                    throw new InvalidOperationException(
                        "The configured main-bar control line has no usable width.");
                }
                return new BeamLaneEnvelope(
                    beam.Id,
                    minimum,
                    maximum);
            }
            catch (Exception exception)
            {
                throw Unsupported(
                    context,
                    stageName,
                    "IndependentBeamLaneEnvelopeInvalid",
                    $"The cover/stirrup lane envelope for beam {beam.Id} "
                    + $"could not be resolved safely: {exception.Message}");
            }
        }

        private static IndependentLane ResolveIndependentLane(
            MainBarBeamReal geometry,
            int laneIndex,
            BeamJointGeometry joint,
            RebarBeamMainBarLevelType level,
            double toleranceFt,
            RebarExecutionContext context,
            string stageName)
        {
            var terminalPath = ExtractTerminalPath(
                geometry,
                joint.AxisX,
                toleranceFt);
            var startSide = new TerminalSide(
                terminalPath.CoreStart,
                terminalPath.Prefix
                    .Concat(new[] { terminalPath.CoreStart })
                    .ToList(),
                terminalPath.CoreStart
                    .DotProduct(joint.AxisX));
            var endSide = new TerminalSide(
                terminalPath.CoreEnd,
                terminalPath.Suffix
                    .Reverse()
                    .Concat(new[] { terminalPath.CoreEnd })
                    .ToList(),
                terminalPath.CoreEnd
                    .DotProduct(joint.AxisX));

            TerminalSide leftSide;
            TerminalSide rightSide;
            if (startSide.Station
                    <= joint.ColumnStart + toleranceFt
                && endSide.Station
                    >= joint.ColumnEnd - toleranceFt)
            {
                leftSide = startSide;
                rightSide = endSide;
            }
            else if (endSide.Station
                         <= joint.ColumnStart + toleranceFt
                     && startSide.Station
                         >= joint.ColumnEnd - toleranceFt)
            {
                leftSide = endSide;
                rightSide = startSide;
            }
            else
            {
                throw Unsupported(
                    context,
                    stageName,
                    "IndependentAnchorSourceMappingFailed",
                    $"Lane {laneIndex + 1} does not expose one terminal on "
                    + "each physical side of the joint.");
            }

            var elevationDelta =
                rightSide.CorePoint.Z - leftSide.CorePoint.Z;
            if (Math.Abs(elevationDelta) <= toleranceFt)
            {
                throw Unsupported(
                    context,
                    stageName,
                    "IndependentAnchorElevationMappingFailed",
                    $"Lane {laneIndex + 1} does not retain two distinct "
                    + "bar-centerline elevations for independent anchorage.");
            }

            TerminalSide bentSide;
            if (level == RebarBeamMainBarLevelType.RebarBot)
            {
                // Bottom reinforcement: the lower bar bends upward.
                bentSide = leftSide.CorePoint.Z < rightSide.CorePoint.Z
                    ? leftSide
                    : rightSide;
            }
            else if (level == RebarBeamMainBarLevelType.RebarTop)
            {
                // Top reinforcement is the vertical mirror: the higher bar
                // bends downward.
                bentSide = leftSide.CorePoint.Z > rightSide.CorePoint.Z
                    ? leftSide
                    : rightSide;
            }
            else
            {
                throw Unsupported(
                    context,
                    stageName,
                    "IndependentAnchorLevelUnsupported",
                    $"Independent anchorage does not support level '{level}'.");
            }
            var straightSide = ReferenceEquals(bentSide, leftSide)
                ? rightSide
                : leftSide;
            var bentBeam = ReferenceEquals(bentSide, leftSide)
                ? joint.LeftBeam
                : joint.RightBeam;
            var straightBeam = bentBeam.Id == joint.LeftBeam.Id
                ? joint.RightBeam
                : joint.LeftBeam;
            return new IndependentLane(
                laneIndex,
                bentSide,
                straightSide,
                bentBeam,
                straightBeam,
                bentSide.CorePoint.DotProduct(joint.AxisY),
                straightSide.CorePoint.DotProduct(joint.AxisY));
        }

        private static void ValidateIndependentSourceLaneSet(
            IReadOnlyList<double> laneYs,
            int expectedLaneCount,
            double modelBarDiameterFt,
            double toleranceFt,
            string role,
            RebarExecutionContext context,
            string stageName)
        {
            var validation =
                BentZTransitionGeometry.ValidateLaneSet(
                    laneYs
                        .Select(y => new BentZLanePair(y, y))
                        .ToList(),
                    expectedLaneCount,
                    toleranceFt,
                    modelBarDiameterFt + 0.1.MmToFoot());
            if (!validation.IsValid)
            {
                throw Unsupported(
                    context,
                    stageName,
                    $"Independent{role}{validation.Failure}",
                    $"{role} independent-anchor lanes are invalid: "
                    + validation.Message);
            }
        }

        private static IReadOnlyList<XYZ>
            BuildIndependentOrderedPoints(
                TerminalSide sourceSide,
                IReadOnlyList<BentZStationPoint> plannedCorePoints,
                double laneY,
                BeamJointGeometry joint,
                double toleranceFt)
        {
            if (plannedCorePoints == null
                || plannedCorePoints.Count < 2)
            {
                throw new InvalidOperationException(
                    "Independent anchorage returned no usable core points.");
            }

            var result = sourceSide.OuterToCorePoints
                .Select(point =>
                    SnapToLane(point, joint.AxisY, laneY))
                .ToList();
            var mappedCore = plannedCorePoints
                .Select(point =>
                    PointFromStation(
                        point,
                        laneY,
                        joint.AxisX,
                        joint.AxisY))
                .ToList();
            if (result.Count == 0
                || result[result.Count - 1]
                    .DistanceTo(mappedCore[0])
                    > toleranceFt)
            {
                throw new InvalidOperationException(
                    "Independent anchorage source geometry does not meet "
                    + "the planned core run.");
            }
            result[result.Count - 1] = mappedCore[0];
            result.AddRange(mappedCore.Skip(1));
            return result;
        }

        private static XYZ PointFromStation(
            BentZStationPoint point,
            double laneY,
            XYZ axisX,
            XYZ axisY)
        {
            return axisX * point.Station
                + axisY * laneY
                + XYZ.BasisZ * point.Elevation;
        }

        private static void ValidateIndependentContainment(
            IndependentLane lane,
            IndependentJointAnchorageResult planned,
            double straightLaneY,
            double bentLaneY,
            BeamEnvelope bentBeam,
            BeamEnvelope straightBeam,
            BeamJointGeometry joint,
            BeamLaneEnvelope bentLaneEnvelope,
            BeamLaneEnvelope straightLaneEnvelope,
            double centerlineClearanceFt,
            double toleranceFt,
            RebarExecutionContext context,
            string stageName)
        {
            var bentZ = lane.BentSide.CorePoint.Z;
            var straightZ = lane.StraightSide.CorePoint.Z;
            foreach (var envelope in new[]
                     {
                         straightLaneEnvelope,
                         bentLaneEnvelope
                     })
            {
                if (!IsInside(
                        straightLaneY,
                        envelope.MinY,
                        envelope.MaxY,
                        toleranceFt))
                {
                    throw Unsupported(
                        context,
                        stageName,
                        "IndependentStraightOutsideBeamCover",
                        $"Straight-through lane {lane.LaneIndex + 1} leaves "
                        + $"the cover-reduced envelope of beam "
                        + $"{envelope.Id}.");
                }
            }
            if (!IsInside(
                    straightLaneY,
                    joint.ColumnMinY + centerlineClearanceFt,
                    joint.ColumnMaxY - centerlineClearanceFt,
                    toleranceFt))
            {
                throw Unsupported(
                    context,
                    stageName,
                    "IndependentStraightOutsideColumnCover",
                    $"Straight-through lane {lane.LaneIndex + 1} leaves "
                    + "the cover-reduced column envelope.");
            }
            if (!IsInside(
                    bentLaneY,
                    bentLaneEnvelope.MinY,
                    bentLaneEnvelope.MaxY,
                    toleranceFt)
                || !IsInside(
                    bentLaneY,
                    joint.ColumnMinY + centerlineClearanceFt,
                    joint.ColumnMaxY - centerlineClearanceFt,
                    toleranceFt))
            {
                throw Unsupported(
                    context,
                    stageName,
                    "IndependentBentOutsideTransverseCover",
                    $"Bent lane {lane.LaneIndex + 1} leaves the "
                    + "cover-reduced bent-side beam or column envelope.");
            }

            if (!IsInside(
                    bentZ,
                    bentBeam.BottomZ + centerlineClearanceFt,
                    bentBeam.TopZ - centerlineClearanceFt,
                    toleranceFt)
                || !IsInside(
                    straightZ,
                    straightBeam.BottomZ + centerlineClearanceFt,
                    straightBeam.TopZ - centerlineClearanceFt,
                    toleranceFt)
                || !IsInside(
                    straightZ,
                    bentBeam.BottomZ + centerlineClearanceFt,
                    bentBeam.TopZ - centerlineClearanceFt,
                    toleranceFt))
            {
                throw Unsupported(
                    context,
                    stageName,
                    "IndependentAnchorOutsideBeamVerticalCover",
                    $"Lane {lane.LaneIndex + 1} does not fit inside the "
                    + "cover-reduced beam depth.");
            }

            var straightEnd =
                planned.StraightThroughPoints[
                    planned.StraightThroughPoints.Count - 1];
            if (!IsInside(
                    straightEnd.Station,
                    bentBeam.MinX,
                    bentBeam.MaxX,
                    toleranceFt))
            {
                throw Unsupported(
                    context,
                    stageName,
                    "IndependentStraightAnchorOutsideBentSideBeam",
                    $"Straight-through lane {lane.LaneIndex + 1} cannot "
                    + "place its required anchorage inside the bent-side "
                    + "beam.");
            }

            var bentEnd =
                planned.BentVerticalPoints[
                    planned.BentVerticalPoints.Count - 1];
            if (!IsInside(
                    bentZ,
                    joint.ColumnBottomZ
                        + centerlineClearanceFt,
                    joint.ColumnTopZ
                        - centerlineClearanceFt,
                    toleranceFt)
                || !IsInside(
                    straightZ,
                    joint.ColumnBottomZ
                        + centerlineClearanceFt,
                    joint.ColumnTopZ
                        - centerlineClearanceFt,
                    toleranceFt)
                || !IsInside(
                    bentEnd.Elevation,
                    Math.Max(
                        joint.ColumnBottomZ,
                        Math.Max(
                            bentBeam.BottomZ,
                            straightBeam.BottomZ))
                    + centerlineClearanceFt,
                    Math.Min(
                        joint.ColumnTopZ,
                        Math.Min(
                            bentBeam.TopZ,
                            straightBeam.TopZ))
                    - centerlineClearanceFt,
                    toleranceFt))
            {
                throw Unsupported(
                    context,
                    stageName,
                    "IndependentAnchorOutsideColumnVerticalCover",
                    $"Independent lane {lane.LaneIndex + 1} leaves the "
                    + "cover-reduced column/joint height.");
            }
        }

        private static bool IsInside(
            double value,
            double minimum,
            double maximum,
            double tolerance)
        {
            return maximum >= minimum
                && value >= minimum - tolerance
                && value <= maximum + tolerance;
        }

        private MainBarRunPlan PlanTransitionRun(
            TerminalPath terminalPath,
            BeamJointGeometry joint,
            RebarBeamMainBarLevelType level,
            RebarBeamMainBarGroupType group,
            RebarBarTypeCustom barType,
            int laneIndex,
            string stageName,
            double bendInsetFt,
            double centerlineClearanceFt,
            double centerlineBendRadiusFt,
            double mainBarRadiusFt,
            IReadOnlyList<ModeledColumnRebar> modeledColumnRebars,
            double minimumSegmentLengthFt,
            double minimumStraightAfterBendFt,
            double toleranceFt,
            RebarExecutionContext context)
        {
            var startStation = terminalPath.CoreStart.DotProduct(joint.AxisX);
            var endStation = terminalPath.CoreEnd.DotProduct(joint.AxisX);
            var direction = endStation >= startStation ? 1.0 : -1.0;
            var jointStart = direction > 0.0
                ? joint.ColumnStart
                : joint.ColumnEnd;
            var jointEnd = direction > 0.0
                ? joint.ColumnEnd
                : joint.ColumnStart;

            var startY = terminalPath.CoreStart.DotProduct(joint.AxisY);
            var endY = terminalPath.CoreEnd.DotProduct(joint.AxisY);
            var laneAlignmentToleranceFt = Math.Min(
                toleranceFt,
                0.01.MmToFoot());
            if (Math.Abs(endY - startY) > laneAlignmentToleranceFt)
            {
                throw Unsupported(
                    context,
                    stageName,
                    "TransverseLaneMismatch",
                    $"Bent/Z lane {laneIndex + 1} changes transverse position "
                    + $"by {Math.Abs(endY - startY).FootToMm():0.###} mm. "
                    + "Phase one supports only one-to-one collinear lanes.");
            }
            var laneY = (startY + endY) / 2.0;
            var coreStart = SnapToLane(
                terminalPath.CoreStart,
                joint.AxisY,
                laneY);
            var coreEnd = SnapToLane(
                terminalPath.CoreEnd,
                joint.AxisY,
                laneY);
            var prefix = terminalPath.Prefix
                .Select(point => SnapToLane(point, joint.AxisY, laneY))
                .ToList();
            var suffix = terminalPath.Suffix
                .Select(point => SnapToLane(point, joint.AxisY, laneY))
                .ToList();
            var minimumZ = Math.Min(
                coreStart.Z,
                coreEnd.Z);
            var maximumZ = Math.Max(
                coreStart.Z,
                coreEnd.Z);
            // The legacy cage can lie exactly on the nominal cover boundary.
            // CenterlineClearanceFt deliberately includes a 0.01 mm
            // post-create verification budget, so use that same small budget
            // here instead of rejecting an otherwise valid 51.50 mm cage at
            // a computed 51.51 mm boundary. Do not use the broader 1 mm
            // geometry tolerance for cover containment.
            var envelopeToleranceFt = Math.Min(
                toleranceFt,
                0.01.MmToFoot());
            var envelopeCheck = BentZColumnEnvelopeRule.Evaluate(
                laneY,
                minimumZ,
                maximumZ,
                joint.ColumnMinY,
                joint.ColumnMaxY,
                joint.ColumnBottomZ,
                joint.ColumnTopZ,
                centerlineClearanceFt,
                envelopeToleranceFt);
            context.DiagnosticLog?.Record(
                "main.transition.column-envelope.checked",
                new
                {
                    stageName,
                    lane = laneIndex + 1,
                    fits = envelopeCheck.Fits,
                    violations = envelopeCheck.Violations.ToString(),
                    laneYMm = Math.Round(laneY.FootToMm(), 3),
                    runMinimumZMm = Math.Round(minimumZ.FootToMm(), 3),
                    runMaximumZMm = Math.Round(maximumZ.FootToMm(), 3),
                    columnMinimumYMm =
                        Math.Round(joint.ColumnMinY.FootToMm(), 3),
                    columnMaximumYMm =
                        Math.Round(joint.ColumnMaxY.FootToMm(), 3),
                    columnMinimumZMm =
                        Math.Round(joint.ColumnBottomZ.FootToMm(), 3),
                    columnMaximumZMm =
                        Math.Round(joint.ColumnTopZ.FootToMm(), 3),
                    allowedMinimumYMm = Math.Round(
                        envelopeCheck.AllowedMinimumY.FootToMm(),
                        3),
                    allowedMaximumYMm = Math.Round(
                        envelopeCheck.AllowedMaximumY.FootToMm(),
                        3),
                    allowedMinimumZMm = Math.Round(
                        envelopeCheck.AllowedMinimumZ.FootToMm(),
                        3),
                    allowedMaximumZMm = Math.Round(
                        envelopeCheck.AllowedMaximumZ.FootToMm(),
                        3),
                    minimumYMarginMm = Math.Round(
                        envelopeCheck.MinimumYMargin.FootToMm(),
                        3),
                    maximumYMarginMm = Math.Round(
                        envelopeCheck.MaximumYMargin.FootToMm(),
                        3),
                    minimumZMarginMm = Math.Round(
                        envelopeCheck.MinimumZMargin.FootToMm(),
                        3),
                    maximumZMarginMm = Math.Round(
                        envelopeCheck.MaximumZMargin.FootToMm(),
                        3),
                    centerlineClearanceMm = Math.Round(
                        centerlineClearanceFt.FootToMm(),
                        3),
                    envelopeToleranceMm = Math.Round(
                        envelopeToleranceFt.FootToMm(),
                        3)
                });
            if (!envelopeCheck.Fits)
            {
                throw Unsupported(
                    context,
                    stageName,
                    "TransitionOutsideColumnClearance",
                    $"Bent/Z lane {laneIndex + 1} does not fit inside the "
                    + "cover-reduced column envelope. Failed side(s): "
                    + $"{envelopeCheck.Violations}. Margins [minY, maxY, "
                    + "minZ, maxZ] = ["
                    + $"{envelopeCheck.MinimumYMargin.FootToMm():0.###}, "
                    + $"{envelopeCheck.MaximumYMargin.FootToMm():0.###}, "
                    + $"{envelopeCheck.MinimumZMargin.FootToMm():0.###}, "
                    + $"{envelopeCheck.MaximumZMargin.FootToMm():0.###}] mm; "
                    + $"allowed tolerance "
                    + $"{envelopeToleranceFt.FootToMm():0.###} mm.");
            }

            var input = new BentZTransitionInput(
                startStation,
                jointStart,
                jointEnd,
                endStation,
                coreStart.Z,
                coreEnd.Z,
                bendInsetFt,
                minimumSegmentLengthFt,
                toleranceFt);
            var planned = BentZTransitionGeometry.Plan(input);
            if (planned.Status != BentZTransitionStatus.Planned)
            {
                throw Unsupported(
                    context,
                    stageName,
                    $"BentZ{planned.Failure}",
                    $"Bent/Z lane {laneIndex + 1} is unsupported: "
                    + planned.Message);
            }
            var enteringBeam = direction > 0.0
                ? joint.LeftBeam
                : joint.RightBeam;
            var leavingBeam = direction > 0.0
                ? joint.RightBeam
                : joint.LeftBeam;
            var enteringBendStation = planned.Points[1].Station;
            var leavingBendStation = planned.Points[2].Station;
            if (!IsInside(
                    enteringBendStation,
                    enteringBeam.MinX,
                    enteringBeam.MaxX,
                    toleranceFt)
                || !IsInside(
                    leavingBendStation,
                    leavingBeam.MinX,
                    leavingBeam.MaxX,
                    toleranceFt))
            {
                throw Unsupported(
                    context,
                    stageName,
                    "BentZBendPointOutsideBeam",
                    $"Bent/Z lane {laneIndex + 1} cannot place both bend "
                    + "vertices inside the participating beam solids. "
                    + $"Entering beam {enteringBeam.Id}: bend "
                    + $"{enteringBendStation.FootToMm():0.###} mm, envelope "
                    + $"[{enteringBeam.MinX.FootToMm():0.###}, "
                    + $"{enteringBeam.MaxX.FootToMm():0.###}] mm. Leaving "
                    + $"beam {leavingBeam.Id}: bend "
                    + $"{leavingBendStation.FootToMm():0.###} mm, envelope "
                    + $"[{leavingBeam.MinX.FootToMm():0.###}, "
                    + $"{leavingBeam.MaxX.FootToMm():0.###}] mm.");
            }
            var bendValidation = ValidateBendGeometry(
                planned.Points,
                bendInsetFt,
                centerlineClearanceFt,
                centerlineBendRadiusFt,
                minimumStraightAfterBendFt,
                toleranceFt,
                laneIndex,
                stageName,
                context);

            var corePoints = planned.Points
                .Select(point =>
                    coreStart
                    + joint.AxisX * (point.Station - startStation)
                    + XYZ.BasisZ
                    * (point.Elevation - coreStart.Z))
                .ToList();
            corePoints[0] = coreStart;
            corePoints[corePoints.Count - 1] = coreEnd;
            var clashValidation = ValidateNoModeledColumnRebarClash(
                corePoints,
                centerlineBendRadiusFt,
                bendValidation.TangentSetback,
                mainBarRadiusFt,
                modeledColumnRebars,
                toleranceFt,
                laneIndex,
                stageName,
                context);

            var orderedPoints = new List<XYZ>(
                prefix.Count
                + corePoints.Count
                + suffix.Count);
            orderedPoints.AddRange(prefix);
            orderedPoints.AddRange(corePoints);
            orderedPoints.AddRange(suffix);

            // A cross-member Rebar can only have one Revit host. Preserve the
            // command's established primary-host contract until the project
            // explicitly approves a different scheduling/ownership rule.
            var ownerBeamId = context.TargetHostId.Value;
            if (ownerBeamId != joint.LeftBeam.Id
                && ownerBeamId != joint.RightBeam.Id)
            {
                throw Unsupported(
                    context,
                    stageName,
                    "BentZOwnerHostUnavailable",
                    "The default target host is not one of the two "
                    + "participating beams.");
            }

            context.DiagnosticLog?.Record("main.transition.run.planned", new
            {
                stageName,
                runId = $"{stageName}.lane.{laneIndex + 1}",
                lane = laneIndex + 1,
                policy = "BentZThroughColumn",
                bendPointPlacement = "InsideAdjacentBeams",
                bendOffsetIntoBeamMm =
                    Math.Round(bendInsetFt.FootToMm(), 3),
                enteringBeamId = enteringBeam.Id,
                leavingBeamId = leavingBeam.Id,
                enteringBendStationMm =
                    Math.Round(enteringBendStation.FootToMm(), 3),
                leavingBendStationMm =
                    Math.Round(leavingBendStation.FootToMm(), 3),
                deltaZMm = Math.Round(
                    (coreEnd.Z - coreStart.Z)
                    .FootToMm(),
                    3),
                bendAngleDegrees = Math.Round(
                    bendValidation.AngleRadians * 180.0 / Math.PI,
                    3),
                tangentSetbackMm = Math.Round(
                    bendValidation.TangentSetback.FootToMm(),
                    3),
                remainingDiagonalStraightMm = Math.Round(
                    bendValidation.RemainingDiagonalStraight.FootToMm(),
                    3),
                closestModeledColumnRebarClearanceMm =
                    clashValidation.ClosestSurfaceClearanceFt.HasValue
                        ? Math.Round(
                            clashValidation.ClosestSurfaceClearanceFt.Value
                                .FootToMm(),
                            3)
                        : (double?)null,
                jointColumnId = joint.ColumnId,
                targetHostBeamId = ownerBeamId,
                ownerRule = "ExistingPrimaryBeamHost",
                points = orderedPoints
                    .Select(RebarDiagnosticLog.PointSnapshot)
                    .ToList()
            });

            return new MainBarRunPlan(
                $"{stageName}.lane.{laneIndex + 1}",
                MainBarRunKind.BentZTransition,
                level,
                group,
                laneIndex,
                barType,
                orderedPoints,
                new[] { joint.LeftBeam.Id, joint.RightBeam.Id },
                ownerBeamId,
                joint.ColumnId,
                coreEnd.Z - coreStart.Z,
                centerlineBendRadiusFt);
        }

        private static XYZ SnapToLane(
            XYZ point,
            XYZ transverseAxis,
            double laneCoordinate)
        {
            return point + transverseAxis * (
                laneCoordinate - point.DotProduct(transverseAxis));
        }

        private static BentZBendValidationResult ValidateBendGeometry(
            IReadOnlyList<BentZStationPoint> points,
            double bendInsetFt,
            double centerlineClearanceFt,
            double centerlineBendRadiusFt,
            double minimumStraightAfterBendFt,
            double toleranceFt,
            int laneIndex,
            string stageName,
            RebarExecutionContext context)
        {
            var validation =
                BentZTransitionGeometry.ValidateRoundedBends(
                    points,
                    bendInsetFt,
                    centerlineClearanceFt,
                    centerlineBendRadiusFt,
                    minimumStraightAfterBendFt,
                    toleranceFt);
            if (!validation.IsValid)
            {
                throw Unsupported(
                    context,
                    stageName,
                    $"BentZ{validation.Failure}",
                    $"Bent/Z lane {laneIndex + 1} is unsupported after "
                    + $"rounded-bend validation: {validation.Message}");
            }
            return validation;
        }

        private static ClashValidation ValidateNoModeledColumnRebarClash(
            IReadOnlyList<XYZ> corePoints,
            double centerlineBendRadiusFt,
            double tangentSetbackFt,
            double mainBarRadiusFt,
            IReadOnlyList<ModeledColumnRebar> modeledColumnRebars,
            double toleranceFt,
            int laneIndex,
            string stageName,
            RebarExecutionContext context)
        {
            var candidateCurves = CreateRoundedCenterline(
                corePoints,
                centerlineBendRadiusFt,
                tangentSetbackFt,
                toleranceFt);
            return ValidateNoModeledJointRebarClash(
                candidateCurves,
                mainBarRadiusFt,
                modeledColumnRebars,
                stageName,
                context,
                laneIndex);
        }

        private static ClashValidation ValidateNoModeledJointRebarClash(
            IReadOnlyList<Curve> candidateCurves,
            double mainBarRadiusFt,
            IReadOnlyList<ModeledColumnRebar> modeledColumnRebars,
            string stageName,
            RebarExecutionContext context,
            int? laneIndex = null)
        {
            if (candidateCurves == null
                || candidateCurves.Count == 0
                || candidateCurves.Any(curve =>
                    curve == null || curve.Length <= 0.0))
            {
                throw Unsupported(
                    context,
                    stageName,
                    "JointCandidateGeometryInvalid",
                    "The planned joint bar has no complete centerline "
                    + "geometry for clash validation.");
            }
            // Reserve the 0.01 mm post-create geometry-verification budget
            // in addition to the 0.1 mm collision tolerance.
            var clashToleranceFt = 0.11.MmToFoot();
            double? closestSurfaceClearanceFt = null;

            foreach (var modeledBar in modeledColumnRebars
                         ?? Array.Empty<ModeledColumnRebar>())
            {
                var rebar = modeledBar?.Rebar;
                if (rebar == null || !rebar.IsValidObject)
                {
                    throw Unsupported(
                        context,
                        stageName,
                        "JointColumnRebarInvalid",
                        "A modeled joint rebar became unavailable while "
                        + "validating the planned joint path.");
                }

                var exposedCenterline = false;
                for (var position = 0;
                     position < rebar.NumberOfBarPositions;
                     position++)
                {
                    if (!rebar.DoesBarExistAtPosition(position)) continue;
                    IList<Curve> existingCurves;
                    try
                    {
                        existingCurves = rebar.GetCenterlineCurves(
                            true,
                            false,
                            false,
                            MultiplanarOption
                                .IncludeAllMultiplanarCurves,
                            position);
                    }
                    catch (Exception exception)
                    {
                        throw Unsupported(
                            context,
                            stageName,
                            "JointColumnRebarGeometryUnavailable",
                            $"Centerline geometry for joint rebar "
                            + $"{rebar.Id.Value} could not be read: "
                            + exception.Message);
                    }
                    if (existingCurves == null
                        || existingCurves.Count == 0)
                    {
                        continue;
                    }
                    exposedCenterline = true;

                    foreach (var candidateCurve in candidateCurves)
                    {
                        foreach (var existingCurve in existingCurves)
                        {
                            if (existingCurve == null) continue;
                            double centerlineDistanceFt;
                            try
                            {
                                centerlineDistanceFt =
                                    GetMinimumCurveDistance(
                                        candidateCurve,
                                        existingCurve);
                            }
                            catch (Exception exception)
                            {
                                throw Unsupported(
                                    context,
                                    stageName,
                                    "JointColumnRebarClashCheckFailed",
                                    $"Clearance to joint rebar "
                                    + $"{rebar.Id.Value} could not be "
                                    + $"computed: {exception.Message}");
                            }
                            if (double.IsNaN(centerlineDistanceFt)
                                || double.IsInfinity(centerlineDistanceFt))
                            {
                                throw Unsupported(
                                    context,
                                    stageName,
                                    "JointColumnRebarClashCheckFailed",
                                    $"A finite clearance to joint rebar "
                                    + $"{rebar.Id.Value} could not be computed.");
                            }
                            var surfaceClearanceFt =
                                centerlineDistanceFt
                                - mainBarRadiusFt
                                - modeledBar.DiameterFt / 2.0;
                            closestSurfaceClearanceFt =
                                !closestSurfaceClearanceFt.HasValue
                                || surfaceClearanceFt
                                < closestSurfaceClearanceFt.Value
                                    ? surfaceClearanceFt
                                    : closestSurfaceClearanceFt;
                            if (surfaceClearanceFt
                                <= clashToleranceFt)
                            {
                                throw Unsupported(
                                    context,
                                    stageName,
                                    "JointPlannedRebarClash",
                                    $"Planned joint lane "
                                    + $"{(laneIndex.HasValue
                                        ? (laneIndex.Value + 1).ToString()
                                        : "(independent)")} conflicts "
                                    + $"with modeled joint rebar "
                                    + $"{rebar.Id.Value} at bar position "
                                    + $"{position + 1}. Surface clearance is "
                                    + $"{surfaceClearanceFt.FootToMm():0.###} "
                                    + "mm; more than 0.1 mm is required.");
                            }
                        }
                    }
                }

                if (!exposedCenterline)
                {
                    throw Unsupported(
                        context,
                        stageName,
                        "JointColumnRebarGeometryUnavailable",
                        $"Modeled joint rebar {rebar.Id.Value} has no "
                        + "readable centerline positions.");
                }
            }

            return new ClashValidation(closestSurfaceClearanceFt);
        }

        private static List<Curve> CreateRoundedCenterline(
            IReadOnlyList<XYZ> points,
            double radiusFt,
            double tangentSetbackFt,
            double toleranceFt)
        {
            if (points == null || points.Count != 4)
                throw new InvalidOperationException(
                    "A rounded Bent/Z centerline requires four points.");

            var firstDirection =
                (points[1] - points[0]).Normalize();
            var diagonalDirection =
                (points[2] - points[1]).Normalize();
            var lastDirection =
                (points[3] - points[2]).Normalize();
            var firstFillet = CreateFilletArc(
                points[1],
                firstDirection,
                diagonalDirection,
                radiusFt,
                tangentSetbackFt);
            var secondFillet = CreateFilletArc(
                points[2],
                diagonalDirection,
                lastDirection,
                radiusFt,
                tangentSetbackFt);
            var curves = new List<Curve>
            {
                Line.CreateBound(points[0], firstFillet.Start),
                firstFillet.Arc,
                Line.CreateBound(firstFillet.End, secondFillet.Start),
                secondFillet.Arc,
                Line.CreateBound(secondFillet.End, points[3])
            };
            if (curves.Any(curve =>
                    curve == null || curve.Length <= toleranceFt))
            {
                throw new InvalidOperationException(
                    "A rounded Bent/Z centerline contains an unusable curve.");
            }
            return curves;
        }

        private static FilletArcGeometry CreateFilletArc(
            XYZ vertex,
            XYZ incomingDirection,
            XYZ outgoingDirection,
            double radiusFt,
            double tangentSetbackFt)
        {
            var start = vertex
                - incomingDirection * tangentSetbackFt;
            var end = vertex
                + outgoingDirection * tangentSetbackFt;
            var bisector =
                -incomingDirection + outgoingDirection;
            if (bisector.GetLength() <= DirectionTolerance)
            {
                throw new InvalidOperationException(
                    "A Bent/Z fillet has no stable angle bisector.");
            }
            bisector = bisector.Normalize();
            var deflectionAngle = incomingDirection.AngleTo(
                outgoingDirection);
            var cosineHalfAngle = Math.Cos(
                deflectionAngle / 2.0);
            if (cosineHalfAngle <= DirectionTolerance)
            {
                throw new InvalidOperationException(
                    "A Bent/Z fillet angle is too large.");
            }
            var center = vertex
                + bisector * (radiusFt / cosineHalfAngle);
            var startRadius = (start - center).Normalize();
            var endRadius = (end - center).Normalize();
            var middleRadius = startRadius + endRadius;
            if (middleRadius.GetLength() <= DirectionTolerance)
            {
                throw new InvalidOperationException(
                    "A Bent/Z fillet midpoint could not be resolved.");
            }
            var middle = center
                + middleRadius.Normalize() * radiusFt;
            return new FilletArcGeometry(
                start,
                end,
                Arc.Create(start, end, middle));
        }

        private static double GetMinimumCurveDistance(
            Curve first,
            Curve second)
        {
            // Bounded line segments have an exact deterministic solver below.
            // Use it before Revit's general closest-point API so ordinary and
            // parallel line pairs follow the same code path.
            if (first is Line firstLine
                && second is Line secondLine
                && firstLine.IsBound
                && secondLine.IsBound)
            {
                return GetMinimumSegmentDistance(
                    firstLine.GetEndPoint(0),
                    firstLine.GetEndPoint(1),
                    secondLine.GetEndPoint(0),
                    secondLine.GetEndPoint(1));
            }

            // Despite being exposed as an out parameter, Revit requires the
            // result collection to be non-null when ComputeClosestPoints is
            // called. Passing an uninitialized variable causes
            // ArgumentNullException ("resultList") before any distance is
            // evaluated.
            IList<ClosestPointsPairBetweenTwoCurves> closestPoints =
                new List<ClosestPointsPairBetweenTwoCurves>();
            try
            {
                first.ComputeClosestPoints(
                    second,
                    true,
                    true,
                    false,
                    out closestPoints);
            }
            catch (Exception exception) when (
                exception is Autodesk.Revit.Exceptions.InvalidOperationException
                || exception is System.InvalidOperationException)
            {
                if (first is Arc firstArc
                    && second is Arc secondArc
                    && AreCoaxialBoundArcs(firstArc, secondArc))
                {
                    return GetEndpointToCurveFallbackDistance(
                        firstArc,
                        secondArc);
                }

                // Revit also reports infinitely many solutions for other
                // singular curve pairs. Without an exact bounded solver for
                // that pair, preserve the fail-closed behavior.
                throw;
            }
            if (closestPoints != null && closestPoints.Count > 0)
            {
                return closestPoints.Min(pair => pair.Distance);
            }

            return GetEndpointToCurveFallbackDistance(first, second);
        }

        private static double GetEndpointToCurveFallbackDistance(
            Curve first,
            Curve second)
        {
            var fallbackDistances = new List<double>();
            if (first.IsBound)
            {
                fallbackDistances.Add(second.Distance(
                    first.GetEndPoint(0)));
                fallbackDistances.Add(second.Distance(
                    first.GetEndPoint(1)));
            }
            if (second.IsBound)
            {
                fallbackDistances.Add(first.Distance(
                    second.GetEndPoint(0)));
                fallbackDistances.Add(first.Distance(
                    second.GetEndPoint(1)));
            }
            return fallbackDistances.Count == 0
                ? double.NaN
                : fallbackDistances.Min();
        }

        private static bool AreCoaxialBoundArcs(
            Arc first,
            Arc second)
        {
            if (!first.IsBound || !second.IsBound) return false;
            var firstNormal = first.Normal.Normalize();
            var secondNormal = second.Normal.Normalize();
            if (firstNormal.CrossProduct(secondNormal).GetLength()
                > DirectionTolerance)
            {
                return false;
            }

            var centerDelta = second.Center - first.Center;
            var perpendicularCenterDelta = centerDelta
                - firstNormal * centerDelta.DotProduct(firstNormal);
            return perpendicularCenterDelta.GetLength()
                <= DirectionTolerance;
        }

        private static double GetMinimumSegmentDistance(
            XYZ firstStart,
            XYZ firstEnd,
            XYZ secondStart,
            XYZ secondEnd)
        {
            var firstDirection = firstEnd - firstStart;
            var secondDirection = secondEnd - secondStart;
            var offset = firstStart - secondStart;
            var firstLengthSquared =
                firstDirection.DotProduct(firstDirection);
            var crossProjection =
                firstDirection.DotProduct(secondDirection);
            var secondLengthSquared =
                secondDirection.DotProduct(secondDirection);
            var firstOffset =
                firstDirection.DotProduct(offset);
            var secondOffset =
                secondDirection.DotProduct(offset);
            var denominator =
                firstLengthSquared * secondLengthSquared
                - crossProjection * crossProjection;
            var firstNumerator = denominator;
            var secondNumerator = denominator;
            var firstDenominator = denominator;
            var secondDenominator = denominator;
            const double epsilon = 1e-12;

            if (denominator < epsilon)
            {
                firstNumerator = 0.0;
                firstDenominator = 1.0;
                secondNumerator = secondOffset;
                secondDenominator = secondLengthSquared;
            }
            else
            {
                firstNumerator =
                    crossProjection * secondOffset
                    - secondLengthSquared * firstOffset;
                secondNumerator =
                    firstLengthSquared * secondOffset
                    - crossProjection * firstOffset;
                if (firstNumerator < 0.0)
                {
                    firstNumerator = 0.0;
                    secondNumerator = secondOffset;
                    secondDenominator = secondLengthSquared;
                }
                else if (firstNumerator > firstDenominator)
                {
                    firstNumerator = firstDenominator;
                    secondNumerator =
                        secondOffset + crossProjection;
                    secondDenominator = secondLengthSquared;
                }
            }

            if (secondNumerator < 0.0)
            {
                secondNumerator = 0.0;
                if (-firstOffset < 0.0)
                {
                    firstNumerator = 0.0;
                }
                else if (-firstOffset > firstLengthSquared)
                {
                    firstNumerator = firstDenominator;
                }
                else
                {
                    firstNumerator = -firstOffset;
                    firstDenominator = firstLengthSquared;
                }
            }
            else if (secondNumerator > secondDenominator)
            {
                secondNumerator = secondDenominator;
                var adjustedFirstOffset =
                    -firstOffset + crossProjection;
                if (adjustedFirstOffset < 0.0)
                {
                    firstNumerator = 0.0;
                }
                else if (adjustedFirstOffset > firstLengthSquared)
                {
                    firstNumerator = firstDenominator;
                }
                else
                {
                    firstNumerator = adjustedFirstOffset;
                    firstDenominator = firstLengthSquared;
                }
            }

            var firstParameter =
                Math.Abs(firstNumerator) < epsilon
                    ? 0.0
                    : firstNumerator / firstDenominator;
            var secondParameter =
                Math.Abs(secondNumerator) < epsilon
                    ? 0.0
                    : secondNumerator / secondDenominator;
            var closestOffset = offset
                + firstDirection * firstParameter
                - secondDirection * secondParameter;
            return closestOffset.GetLength();
        }

        private static void ValidateMainBarSeparation(
            IReadOnlyList<MainBarRunPlan> currentRuns,
            RebarExecutionContext context,
            string stageName,
            double toleranceFt)
        {
            var priorRuns = context.GetRegisteredMainBarPlans()
                .SelectMany(plan => plan.Runs)
                .ToList();
            var pairs = new List<Tuple<MainBarRunPlan, MainBarRunPlan>>();
            for (var firstIndex = 0;
                 firstIndex < currentRuns.Count;
                 firstIndex++)
            {
                for (var secondIndex = firstIndex + 1;
                     secondIndex < currentRuns.Count;
                     secondIndex++)
                {
                    pairs.Add(Tuple.Create(
                        currentRuns[firstIndex],
                        currentRuns[secondIndex]));
                }
                pairs.AddRange(priorRuns.Select(priorRun =>
                    Tuple.Create(
                        currentRuns[firstIndex],
                        priorRun)));
            }

            var curveCache =
                new Dictionary<MainBarRunPlan, List<Curve>>();
            foreach (var pair in pairs)
            {
                var firstRun = pair.Item1;
                var secondRun = pair.Item2;
                if (!firstRun.RequiresStrictGeometryValidation
                    && !secondRun.RequiresStrictGeometryValidation)
                {
                    continue;
                }
                if (firstRun.Level != secondRun.Level
                    && (firstRun.Kind == MainBarRunKind.Legacy
                        || secondRun.Kind == MainBarRunKind.Legacy))
                {
                    // Legacy top and bottom bars may intentionally share an
                    // outer-end hook station. That pre-existing detail is
                    // outside the different-section joint transition and
                    // produced false zero-distance clashes when only the
                    // opposite face used a strict transition plan.
                    context.DiagnosticLog?.Record(
                        "main.separation.pair.skipped",
                        new
                        {
                            stageName,
                            firstRunId = firstRun.RunId,
                            secondRunId = secondRun.RunId,
                            reason =
                                "OppositeFaceLegacyOuterHook"
                        });
                    continue;
                }
                double minimumCenterlineDistanceFt;
                string separationSolver;
                try
                {
                    if (TryGetPerpendicularTranslatedRunDistance(
                            firstRun,
                            secondRun,
                            toleranceFt,
                            out minimumCenterlineDistanceFt))
                    {
                        // Repeated lanes have congruent centerlines translated
                        // perpendicular to every segment and bend plane. Their
                        // exact separation is the translation magnitude, even
                        // when each run includes end hooks and fillet arcs.
                        separationSolver =
                            "PerpendicularTranslatedRun";
                    }
                    else if (firstRun.OrderedPoints?.Count == 2
                        && secondRun.OrderedPoints?.Count == 2)
                    {
                        // Independent straight anchors are finite segments.
                        // Resolve them directly from the plan so parallel
                        // lanes never enter Revit's singular closest-curve
                        // solver.
                        minimumCenterlineDistanceFt =
                            GetMinimumSegmentDistance(
                                firstRun.OrderedPoints[0],
                                firstRun.OrderedPoints[1],
                                secondRun.OrderedPoints[0],
                                secondRun.OrderedPoints[1]);
                        separationSolver = "DirectPlanSegment";
                    }
                    else
                    {
                        if (!curveCache.TryGetValue(
                                firstRun,
                                out var firstCurves))
                        {
                            firstCurves = CreatePlannedRunCenterline(
                                firstRun,
                                toleranceFt);
                            curveCache[firstRun] = firstCurves;
                        }
                        if (!curveCache.TryGetValue(
                                secondRun,
                                out var secondCurves))
                        {
                            secondCurves = CreatePlannedRunCenterline(
                                secondRun,
                                toleranceFt);
                            curveCache[secondRun] = secondCurves;
                        }
                        minimumCenterlineDistanceFt = firstCurves
                            .SelectMany(firstCurve => secondCurves.Select(
                                secondCurve => GetMinimumCurveDistance(
                                    firstCurve,
                                    secondCurve)))
                            .Min();
                        separationSolver = "BoundedCurvePairs";
                    }
                }
                catch (Exception exception)
                {
                    throw Unsupported(
                        context,
                        stageName,
                        "MainBarSeparationCheckFailed",
                        $"Clearance between '{firstRun.RunId}' and "
                        + $"'{secondRun.RunId}' could not be computed: "
                        + exception.Message);
                }
                if (double.IsNaN(minimumCenterlineDistanceFt)
                    || double.IsInfinity(minimumCenterlineDistanceFt))
                {
                    throw Unsupported(
                        context,
                        stageName,
                        "MainBarSeparationCheckFailed",
                        $"A finite clearance between '{firstRun.RunId}' and "
                        + $"'{secondRun.RunId}' could not be computed.");
                }

                var firstDiameterFt = Math.Max(
                    firstRun.BarType.ModelBarDiameter,
                    firstRun.BarType.BarDiameter);
                var secondDiameterFt = Math.Max(
                    secondRun.BarType.ModelBarDiameter,
                    secondRun.BarType.BarDiameter);
                var requiredCenterlineDistanceFt =
                    firstDiameterFt / 2.0
                    + secondDiameterFt / 2.0
                    // Both newly created bars may differ from their planned
                    // centerlines by up to 0.01 mm after Revit resolves the
                    // shape, so reserve two verification budgets.
                    + 0.12.MmToFoot();
                context.DiagnosticLog?.Record(
                    "main.separation.pair.resolved",
                    new
                    {
                        stageName,
                        firstRunId = firstRun.RunId,
                        secondRunId = secondRun.RunId,
                        solver = separationSolver,
                        centerlineDistanceMm = Math.Round(
                            minimumCenterlineDistanceFt.FootToMm(),
                            3),
                        requiredCenterlineDistanceMm = Math.Round(
                            requiredCenterlineDistanceFt.FootToMm(),
                            3)
                    });
                if (minimumCenterlineDistanceFt
                    <= requiredCenterlineDistanceFt)
                {
                    throw Unsupported(
                        context,
                        stageName,
                        "MainBarRunClash",
                        $"Main-bar runs '{firstRun.RunId}' and "
                        + $"'{secondRun.RunId}' overlap or do not have enough "
                        + "surface clearance. Computed centerline distance: "
                        + $"{minimumCenterlineDistanceFt.FootToMm():0.###} mm; "
                        + "required: "
                        + $"{requiredCenterlineDistanceFt.FootToMm():0.###} mm.");
                }
            }
        }

        private static bool TryGetPerpendicularTranslatedRunDistance(
            MainBarRunPlan firstRun,
            MainBarRunPlan secondRun,
            double toleranceFt,
            out double distanceFt)
        {
            distanceFt = 0.0;
            var firstPoints = firstRun?.OrderedPoints;
            var secondPoints = secondRun?.OrderedPoints;
            if (firstPoints == null
                || secondPoints == null
                || firstPoints.Count < 2
                || firstPoints.Count != secondPoints.Count
                || toleranceFt <= 0.0)
            {
                return false;
            }
            if (Math.Abs(
                    firstRun.CenterlineBendRadiusFt
                    - secondRun.CenterlineBendRadiusFt)
                > toleranceFt)
            {
                return false;
            }

            var translation = secondPoints[0] - firstPoints[0];
            for (var index = 0; index < firstPoints.Count; index++)
            {
                if ((secondPoints[index]
                     - firstPoints[index]
                     - translation).GetLength() > toleranceFt)
                {
                    return false;
                }
            }

            for (var index = 1; index < firstPoints.Count; index++)
            {
                var segment =
                    firstPoints[index] - firstPoints[index - 1];
                var segmentLength = segment.GetLength();
                if (segmentLength <= toleranceFt)
                {
                    return false;
                }
                var translationAlongSegmentFt = Math.Abs(
                    translation.DotProduct(segment) / segmentLength);
                if (translationAlongSegmentFt > toleranceFt)
                {
                    return false;
                }
            }

            distanceFt = translation.GetLength();
            return !double.IsNaN(distanceFt)
                && !double.IsInfinity(distanceFt);
        }

        internal static List<Curve> CreatePlannedRunCenterline(
            MainBarRunPlan run,
            double toleranceFt)
        {
            if (run?.OrderedPoints == null
                || run.OrderedPoints.Count < 2)
            {
                throw new InvalidOperationException(
                    "A main-bar run has no usable ordered point chain.");
            }
            if (run.OrderedPoints.Count == 2)
            {
                return CreateBoundLines(
                    run.OrderedPoints,
                    toleranceFt);
            }
            if (run.CenterlineBendRadiusFt <= 0.0)
            {
                throw new InvalidOperationException(
                    $"Bent/Z run '{run.RunId}' has no centerline bend radius.");
            }

            var bendsByVertex =
                new Dictionary<int, FilletArcGeometry>();
            for (var segmentIndex = 1;
                 segmentIndex < run.OrderedPoints.Count;
                 segmentIndex++)
            {
                var segment = run.OrderedPoints[segmentIndex]
                    - run.OrderedPoints[segmentIndex - 1];
                if (segment.GetLength() <= toleranceFt)
                {
                    throw new InvalidOperationException(
                        $"Main-bar run '{run.RunId}' contains a short "
                        + $"segment at index {segmentIndex - 1}.");
                }
            }

            for (var vertexIndex = 1;
                 vertexIndex < run.OrderedPoints.Count - 1;
                 vertexIndex++)
            {
                var incomingDirection =
                    (run.OrderedPoints[vertexIndex]
                     - run.OrderedPoints[vertexIndex - 1]).Normalize();
                var outgoingDirection =
                    (run.OrderedPoints[vertexIndex + 1]
                     - run.OrderedPoints[vertexIndex]).Normalize();
                var crossLength = incomingDirection
                    .CrossProduct(outgoingDirection)
                    .GetLength();
                var dotProduct =
                    incomingDirection.DotProduct(outgoingDirection);
                if (crossLength <= DirectionTolerance)
                {
                    if (dotProduct <= 0.0)
                    {
                        throw new InvalidOperationException(
                            $"Main-bar run '{run.RunId}' reverses direction "
                            + $"at vertex {vertexIndex}.");
                    }
                    continue;
                }

                var bendAngle = incomingDirection.AngleTo(
                    outgoingDirection);
                var tangentSetbackFt =
                    run.CenterlineBendRadiusFt
                    * Math.Tan(bendAngle / 2.0);
                if (double.IsNaN(tangentSetbackFt)
                    || double.IsInfinity(tangentSetbackFt)
                    || tangentSetbackFt <= toleranceFt)
                {
                    throw new InvalidOperationException(
                        $"Main-bar run '{run.RunId}' has an invalid bend "
                        + $"at vertex {vertexIndex}.");
                }
                bendsByVertex[vertexIndex] = CreateFilletArc(
                    run.OrderedPoints[vertexIndex],
                    incomingDirection,
                    outgoingDirection,
                    run.CenterlineBendRadiusFt,
                    tangentSetbackFt);
            }

            var result = new List<Curve>(
                run.OrderedPoints.Count * 2);
            var currentPoint = run.OrderedPoints[0];
            for (var vertexIndex = 1;
                 vertexIndex < run.OrderedPoints.Count - 1;
                 vertexIndex++)
            {
                if (!bendsByVertex.TryGetValue(
                        vertexIndex,
                        out var bend))
                {
                    continue;
                }

                var incomingDirection =
                    (run.OrderedPoints[vertexIndex]
                     - run.OrderedPoints[vertexIndex - 1]).Normalize();
                var remainingStraight = bend.Start - currentPoint;
                var signedRemainingLength =
                    remainingStraight.DotProduct(incomingDirection);
                var transverseRemainder = remainingStraight
                    - incomingDirection * signedRemainingLength;
                if (signedRemainingLength <= toleranceFt
                    || transverseRemainder.GetLength() > toleranceFt)
                {
                    throw new InvalidOperationException(
                        $"Main-bar run '{run.RunId}' has no straight length "
                        + $"before bend vertex {vertexIndex}.");
                }
                result.Add(Line.CreateBound(currentPoint, bend.Start));
                result.Add(bend.Arc);
                currentPoint = bend.End;
            }

            var finalPoint =
                run.OrderedPoints[run.OrderedPoints.Count - 1];
            var finalDirection =
                (finalPoint
                 - run.OrderedPoints[
                     run.OrderedPoints.Count - 2]).Normalize();
            var finalStraight = finalPoint - currentPoint;
            var signedFinalLength =
                finalStraight.DotProduct(finalDirection);
            var transverseFinalRemainder = finalStraight
                - finalDirection * signedFinalLength;
            if (signedFinalLength <= toleranceFt
                || transverseFinalRemainder.GetLength() > toleranceFt)
            {
                throw new InvalidOperationException(
                    $"Main-bar run '{run.RunId}' has no straight length "
                    + "after its final bend.");
            }
            result.Add(Line.CreateBound(currentPoint, finalPoint));
            return result;
        }

        private static List<Curve> CreateBoundLines(
            IReadOnlyList<XYZ> points,
            double toleranceFt)
        {
            var result = new List<Curve>();
            for (var index = 1; index < points.Count; index++)
            {
                if (points[index - 1] == null || points[index] == null
                    || points[index - 1].DistanceTo(points[index])
                    <= toleranceFt)
                {
                    throw new InvalidOperationException(
                        "A main-bar run contains a null or short segment.");
                }
                result.Add(Line.CreateBound(
                    points[index - 1],
                    points[index]));
            }
            return result;
        }

        private BeamJointGeometry ResolveJointGeometry(
            InstallRebarBeamV2ViewModel viewModel,
            RebarExecutionContext context,
            double toleranceFt)
        {
            var members = viewModel.ElementInstances.Beam.ElementSubs;
            if (members.Count != 2)
            {
                throw Unsupported(
                    context,
                    "main geometry",
                    "BentZRequiresTwoBeams",
                    "Different-section phase one requires exactly two "
                    + "physical beams.");
            }

            var memberLines = members
                .Select(member => new
                {
                    Member = member,
                    Line = (member.Element.Location as LocationCurve)?.Curve as Line
                })
                .ToList();
            if (memberLines.Any(item => item.Line == null))
            {
                throw Unsupported(
                    context,
                    "main geometry",
                    "NonLinearBeam",
                    "Different-section phase one requires a straight "
                    + "LocationCurve on both beams.");
            }

            var rawDirection =
                memberLines[0].Line.Direction;
            var axisX = StableHorizontalDirection(rawDirection);
            var axisY = XYZ.BasisZ.CrossProduct(axisX).Normalize();
            if (context.XAxis == null
                || context.XAxis.GetLength() <= DirectionTolerance
                || Math.Abs(
                    Math.Abs(context.XAxis.Normalize().DotProduct(axisX))
                    - 1.0) > DirectionTolerance)
            {
                throw Unsupported(
                    context,
                    "main geometry",
                    "LegacyAxisMismatch",
                    "The legacy assembly axis does not match the physical "
                    + "beam LocationCurve axis. Geometry generation was stopped "
                    + "before creating a wrong transition.");
            }
            if (context.YAxis == null
                || context.YAxis.GetLength() <= DirectionTolerance
                || Math.Abs(
                    Math.Abs(context.YAxis.Normalize().DotProduct(axisY))
                    - 1.0) > DirectionTolerance)
            {
                throw Unsupported(
                    context,
                    "main geometry",
                    "LegacyPlaneMismatch",
                    "The legacy creation-plane normal does not match the "
                    + "physical beam plane.");
            }
            foreach (var item in memberLines)
            {
                var direction = item.Line.Direction;
                if (Math.Abs(direction.Z) > DirectionTolerance
                    || Math.Abs(Math.Abs(direction.DotProduct(axisX)) - 1.0)
                    > DirectionTolerance)
                {
                    throw Unsupported(
                        context,
                        "main geometry",
                        "NonHorizontalOrNonParallelBeam",
                        "Different-section phase one supports only straight, "
                        + "horizontal, parallel beams.");
                }
            }

            var ordered = memberLines
                .OrderBy(item =>
                    item.Line.Evaluate(0.5, true).DotProduct(axisX))
                .ThenBy(item => item.Member.Id)
                .ToList();
            var left = CreateBeamEnvelope(ordered[0].Member, axisX, axisY);
            var right = CreateBeamEnvelope(ordered[1].Member, axisX, axisY);
            var leftCenterY = (left.MinY + left.MaxY) / 2.0;
            var rightCenterY = (right.MinY + right.MaxY) / 2.0;
            if (Math.Abs(rightCenterY - leftCenterY) > toleranceFt)
            {
                throw Unsupported(
                    context,
                    "main geometry",
                    "NonCollinearBeams",
                    $"Beam centerlines differ transversely by "
                    + $"{Math.Abs(rightCenterY - leftCenterY).FootToMm():0.###} mm.");
            }

            var leftFacingStation = left.MaxX;
            var rightFacingStation = right.MinX;
            if (rightFacingStation < leftFacingStation - toleranceFt)
            {
                throw Unsupported(
                    context,
                    "main geometry",
                    "OverlappingBeamEnvelopes",
                    "The two beam envelopes overlap along the run. "
                    + "A unique joint window cannot be resolved.");
            }
            var seamStation =
                (leftFacingStation + rightFacingStation) / 2.0;
            var seamY = (leftCenterY + rightCenterY) / 2.0;
            var seamZ = (
                Math.Max(left.BottomZ, right.BottomZ)
                + Math.Min(left.TopZ, right.TopZ)) / 2.0;

            var candidates = new List<ColumnEnvelope>();
            var unsupportedCandidateIds = new List<long>();
            var columns = new FilteredElementCollector(context.Document)
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .WhereElementIsNotElementType()
                .ToElements();
            foreach (var column in columns)
            {
                var points = GetElementPoints(column);
                if (points.Count == 0) continue;
                var envelope = CreateColumnEnvelope(
                    column,
                    points,
                    axisX,
                    axisY);
                if (seamStation < envelope.MinX - toleranceFt
                    || seamStation > envelope.MaxX + toleranceFt
                    || seamY < envelope.MinY - toleranceFt
                    || seamY > envelope.MaxY + toleranceFt
                    || seamZ < envelope.BottomZ - toleranceFt
                    || seamZ > envelope.TopZ + toleranceFt)
                {
                    continue;
                }
                if (!IsRectangularAlignedColumn(
                        column,
                        axisX,
                        axisY))
                {
                    unsupportedCandidateIds.Add(column.Id.Value);
                    continue;
                }
                candidates.Add(envelope);
            }

            if (candidates.Count != 1)
            {
                var unsupportedGeometry =
                    candidates.Count == 0 && unsupportedCandidateIds.Count > 0;
                throw Unsupported(
                    context,
                    "main geometry",
                    unsupportedGeometry
                        ? "JointColumnGeometryUnsupported"
                        : candidates.Count == 0
                        ? "JointColumnNotFound"
                        : "JointColumnAmbiguous",
                    unsupportedGeometry
                        ? "The structural column at the joint is rotated or "
                          + "non-rectangular. Different-section phase one "
                          + "requires an "
                          + "axis-aligned rectangular column. Candidate ids: "
                          + string.Join(", ", unsupportedCandidateIds)
                        : candidates.Count == 0
                        ? "No structural column encloses the beam joint."
                        : $"The beam joint intersects {candidates.Count} structural columns; "
                          + "a unique transition window is required.");
            }

            var selected = candidates[0];
            if (left.MaxX < selected.MinX - toleranceFt
                || left.MaxX > selected.MaxX + toleranceFt
                || right.MinX < selected.MinX - toleranceFt
                || right.MinX > selected.MaxX + toleranceFt)
            {
                throw Unsupported(
                    context,
                    "main geometry",
                    "BeamsDoNotMeetJointColumn",
                    "Both facing beam envelopes must terminate at or inside "
                    + "the resolved column envelope.");
            }
            return new BeamJointGeometry(
                axisX,
                axisY,
                left,
                right,
                selected.Id,
                selected.MinX,
                selected.MaxX,
                selected.MinY,
                selected.MaxY,
                selected.BottomZ,
                selected.TopZ);
        }

        private int ValidateJointBarCompatibility(
            InstallRebarBeamV2ViewModel viewModel,
            RebarExecutionContext context,
            RebarBeamMainBarLevelType level,
            RebarBeamMainBarGroupType group,
            BeamJointGeometry joint,
            string stageName)
        {
            var activeBars = _geometryService
                .GetRebarBeamGroupLevelInfo(viewModel, level, group)
                .Where(bar => bar.Quantity > 0)
                .ToList();
            if (activeBars.Any(bar =>
                    string.IsNullOrWhiteSpace(bar.Diameter)))
            {
                throw Unsupported(
                    context,
                    stageName,
                    "RunBarTypeMissing",
                    "Different-section reinforcement requires a bar type "
                    + "for every active section "
                    + "of the continuous run.");
            }
            var activeDiameters = activeBars
                .Select(bar => bar.Diameter)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (activeDiameters.Count != 1)
            {
                throw Unsupported(
                    context,
                    stageName,
                    "RunBarTypeMismatch",
                    "Different-section phase one requires one bar type "
                    + "across every "
                    + "active section of the continuous run. Active types: "
                    + (activeDiameters.Count == 0
                        ? "(none)"
                        : string.Join(", ", activeDiameters)));
            }

            var axisFollowsLegacyOrder =
                joint.AxisX.DotProduct(context.XAxis) >= 0.0;
            var leftJointSection = axisFollowsLegacyOrder
                ? RebarBeamSectionType.SectionEnd
                : RebarBeamSectionType.SectionStart;
            var rightJointSection = axisFollowsLegacyOrder
                ? RebarBeamSectionType.SectionStart
                : RebarBeamSectionType.SectionEnd;
            var leftBars = _geometryService.GetRebarBeamGroupInfo(
                viewModel,
                leftJointSection,
                level,
                group);
            var rightBars = _geometryService.GetRebarBeamGroupInfo(
                viewModel,
                rightJointSection,
                level,
                group);
            var leftBar = leftBars.FirstOrDefault(
                bar => bar.HostId == joint.LeftBeam.Id);
            var rightBar = rightBars.FirstOrDefault(
                bar => bar.HostId == joint.RightBeam.Id);
            if (leftBar == null || rightBar == null)
            {
                throw Unsupported(
                    context,
                    stageName,
                    "JointBarConfigurationMissing",
                    "The end/start bar configuration at the joint is incomplete.");
            }
            if (leftBar.Quantity != rightBar.Quantity)
            {
                throw Unsupported(
                    context,
                    stageName,
                    "JointBarQuantityMismatch",
                    $"Different-section phase one requires one-to-one lanes, "
                    + "but joint quantities are "
                    + $"{leftBar.Quantity} and {rightBar.Quantity}.");
            }
            if (leftBar.Quantity <= 0)
            {
                throw Unsupported(
                    context,
                    stageName,
                    "JointBarQuantityMissing",
                    "Different-section phase one requires at least one active "
                    + "one-to-one lane at "
                    + "the beam joint.");
            }
            if (!string.Equals(
                    leftBar.Diameter,
                    rightBar.Diameter,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw Unsupported(
                    context,
                    stageName,
                    "JointBarTypeMismatch",
                    $"Different-section phase one cannot merge bar types "
                    + $"'{leftBar.Diameter}' and "
                    + $"'{rightBar.Diameter}'.");
            }
            return leftBar.Quantity;
        }

        private BendClearance CalculateBendClearance(
            InstallRebarBeamV2ViewModel viewModel,
            RebarExecutionContext context,
            RebarBarTypeCustom barType,
            BeamJointGeometry joint,
            string stageName,
            bool allowConfiguredBeamStirrupFallback)
        {
            var bendDiameterFt = barType.StandardBendDiameter;
            var barDiameterFt = Math.Max(
                barType.ModelBarDiameter,
                barType.BarDiameter);
            if (bendDiameterFt <= 0.0 || barDiameterFt <= 0.0)
            {
                throw Unsupported(
                    context,
                    stageName,
                    "InvalidBendDiameter",
                    $"Rebar type '{barType.NameStyle}' has no valid standard "
                    + "bend diameter or model diameter.");
            }

            ColumnReinforcementClearance
                modeledColumnReinforcement;
            try
            {
                modeledColumnReinforcement =
                    ResolveModeledJointReinforcement(
                        context.Document,
                        joint.ColumnId,
                        joint.LeftBeam.Id,
                        joint.RightBeam.Id);
            }
            catch (Exception exception)
            {
                throw Unsupported(
                    context,
                    stageName,
                    "JointRebarDataInvalid",
                    "Modeled reinforcement on the column or participating "
                    + $"beams could not be characterized safely: "
                    + exception.Message);
            }
            ConfiguredBeamStirrupClearance configuredBeamStirrup = null;
            TemporaryJointStirrupSelection stirrupSelection;
            try
            {
                var modeledStirrupDiameterFt =
                    modeledColumnReinforcement.StirrupCount > 0
                        ? modeledColumnReinforcement
                            .MaximumStirrupDiameterFt
                        : 0.0;
                if (modeledStirrupDiameterFt <= 0.0
                    && allowConfiguredBeamStirrupFallback)
                {
                    configuredBeamStirrup =
                        ResolveConfiguredBeamStirrupClearance(
                            viewModel,
                            context);
                }
                stirrupSelection =
                    TemporaryJointStirrupFallbackRule.Resolve(
                        modeledStirrupDiameterFt,
                        configuredBeamStirrup?.DiameterFt ?? 0.0,
                        "joint stirrup diameter");
            }
            catch (Exception exception)
            {
                throw Unsupported(
                    context,
                    stageName,
                    "JointColumnStirrupDataMissing",
                    $"Column {joint.ColumnId} has no modeled stirrup/tie "
                    + "geometry with a resolvable bar diameter"
                    + (allowConfiguredBeamStirrupFallback
                        ? " and no valid configured beam-stirrup fallback"
                        : string.Empty)
                    + $": {exception.Message}");
            }
            // The cover-reduced cage envelope is governed by the modeled
            // column tie when available. A different-section transition may
            // use the largest configured participating-beam stirrup diameter
            // as a temporary proxy; it never invents modeled rebar geometry.
            // Longitudinal column bars and beam-hosted bars are checked against
            // their actual centerline geometry below rather than inflating
            // every column face by the largest bar diameter.
            var jointReinforcementDiameterFt =
                stirrupSelection.Value;
            var coverFt = ResolveColumnCover(
                context.Document,
                joint.ColumnId);
            if (coverFt <= 0.0)
            {
                throw Unsupported(
                    context,
                    stageName,
                    "JointColumnCoverMissing",
                    $"Column {joint.ColumnId} has no resolvable positive "
                    + "RebarCoverType. The temporary stirrup fallback does "
                    + "not substitute column cover.");
            }
            var centerlineBendRadiusFt =
                bendDiameterFt / 2.0 + barDiameterFt / 2.0;
            var centerlineClearanceFt =
                coverFt + jointReinforcementDiameterFt
                + barDiameterFt / 2.0
                // Preserve the cover/cage envelope after allowing the
                // post-create centerline to differ by at most 0.01 mm.
                + 0.01.MmToFoot();

            // The actual tangent setback is R * tan(theta / 2). A Bent/Z
            // crank has theta in (0, 90 degrees), so reserving a full R is a
            // conservative offset from each column face into its adjacent
            // beam. PlanTransitionRun still validates the exact angle and
            // remaining tangent lengths for every lane.
            var bendInsetFt =
                centerlineClearanceFt + centerlineBendRadiusFt;

            context.DiagnosticLog?.Record(
                "main.transition.clearance.resolved",
                new
                {
                    stageName,
                    jointColumnId = joint.ColumnId,
                    modeledColumnStirrupCount =
                        modeledColumnReinforcement.StirrupCount,
                    jointStirrupDiameterMm = Math.Round(
                        jointReinforcementDiameterFt.FootToMm(),
                        3),
                    jointStirrupDiameterSource =
                        stirrupSelection.UsedConfiguredBeamFallback
                            ? "ConfiguredBeamStirrupFallback"
                            : "ModeledColumnStirrup",
                    configuredBeamStirrupTypes =
                        configuredBeamStirrup?.TypeNames
                        ?? Array.Empty<string>(),
                    columnCoverMm = Math.Round(
                        coverFt.FootToMm(),
                        3),
                    columnCoverSource = "ModeledColumnCover"
                });

            return new BendClearance(
                bendInsetFt,
                centerlineClearanceFt,
                centerlineBendRadiusFt,
                barDiameterFt / 2.0,
                coverFt,
                jointReinforcementDiameterFt,
                modeledColumnReinforcement.Rebars,
                modeledColumnReinforcement.StirrupCount,
                stirrupSelection.UsedConfiguredBeamFallback);
        }

        private ConfiguredBeamStirrupClearance
            ResolveConfiguredBeamStirrupClearance(
                InstallRebarBeamV2ViewModel viewModel,
                RebarExecutionContext context)
        {
            if (viewModel == null)
                throw new ArgumentNullException(nameof(viewModel));

            var configuredStirrups = new List<RebarBeamStirrup>();
            foreach (var sectionType in new[]
                     {
                         RebarBeamSectionType.SectionStart,
                         RebarBeamSectionType.SectionMid,
                         RebarBeamSectionType.SectionEnd
                     })
            {
                configuredStirrups.AddRange(
                    _geometryService.GetStirrupGroupInfo(
                        viewModel,
                        sectionType)
                    ?? new List<RebarBeamStirrup>());
            }

            var typeNames = configuredStirrups
                .Where(stirrup =>
                    stirrup != null
                    && !string.IsNullOrWhiteSpace(stirrup.Diameter))
                .Select(stirrup => stirrup.Diameter)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (typeNames.Count == 0)
            {
                throw new InvalidOperationException(
                    "The participating beams have no configured stirrup type.");
            }

            var diameterFt = typeNames
                .Select(name => context.GetBarType(name))
                .Select(type => type.ModelBarDiameter > 0.0
                    ? type.ModelBarDiameter
                    : type.BarDiameter)
                .Where(value =>
                    !double.IsNaN(value)
                    && !double.IsInfinity(value)
                    && value > 0.0)
                .DefaultIfEmpty(0.0)
                .Max();
            if (diameterFt <= 0.0)
            {
                throw new InvalidOperationException(
                    "The configured beam stirrup types have no positive "
                    + "model or nominal diameter.");
            }

            return new ConfiguredBeamStirrupClearance(
                diameterFt,
                typeNames);
        }

        private static ColumnReinforcementClearance
            ResolveModeledJointReinforcement(
            Document document,
            long columnId,
            long leftBeamId,
            long rightBeamId)
        {
            var column = document.GetElement(new ElementId(columnId));
            var hostData = column == null
                ? null
                : RebarHostData.GetRebarHostData(column);
            if (hostData == null)
                return ColumnReinforcementClearance.Empty;

            var maximumBarDiameterFt = 0.0;
            var maximumStirrupDiameterFt = 0.0;
            var stirrupCount = 0;
            var rebars = new List<ModeledColumnRebar>();
            var seenRebarIds = new HashSet<long>();
            foreach (var hostId in new[]
                     {
                         columnId,
                         leftBeamId,
                         rightBeamId
                     }.Distinct())
            {
                var host = document.GetElement(new ElementId(hostId));
                var currentHostData = host == null
                    ? null
                    : RebarHostData.GetRebarHostData(host);
                if (currentHostData == null) continue;

                foreach (var rebar in currentHostData.GetRebarsInHost()
                             ?? Array.Empty<Rebar>())
                {
                    if (rebar == null || !rebar.IsValidObject)
                    {
                        throw new InvalidOperationException(
                            $"Host {hostId} contains an unavailable rebar "
                            + "record.");
                    }
                    if (!seenRebarIds.Add(rebar.Id.Value)) continue;

                    var type = document.GetElement(
                        rebar.GetTypeId()) as RebarBarType;
                    if (type == null)
                    {
                        throw new InvalidOperationException(
                            $"Rebar {rebar.Id.Value} on joint host {hostId} "
                            + "has no resolvable RebarBarType.");
                    }
                    var diameter = type
                        .get_Parameter(
                            BuiltInParameter.REBAR_MODEL_BAR_DIAMETER)
                        ?.AsDouble() ?? 0.0;
                    if (diameter <= 0.0)
                    {
                        diameter = type
                            .get_Parameter(
                                BuiltInParameter.REBAR_BAR_DIAMETER)
                            ?.AsDouble() ?? 0.0;
                    }
                    if (diameter <= 0.0)
                    {
                        throw new InvalidOperationException(
                            $"Rebar {rebar.Id.Value} on joint host {hostId} "
                            + "has no positive model or nominal diameter.");
                    }

                    if (hostId == columnId)
                    {
                        maximumBarDiameterFt = Math.Max(
                            maximumBarDiameterFt,
                            diameter);
                    }
                    var shape = document.GetElement(
                        rebar.GetShapeId()) as RebarShape;
                    var isStirrup =
                        shape?.RebarStyle == RebarStyle.StirrupTie;
                    rebars.Add(new ModeledColumnRebar(
                        rebar,
                        diameter,
                        isStirrup));
                    if (hostId == columnId && isStirrup)
                    {
                        stirrupCount++;
                        maximumStirrupDiameterFt = Math.Max(
                            maximumStirrupDiameterFt,
                            diameter);
                    }
                }
            }

            return new ColumnReinforcementClearance(
                maximumBarDiameterFt,
                maximumStirrupDiameterFt,
                rebars,
                stirrupCount);
        }

        private static double ResolveColumnCover(
            Document document,
            long columnId)
        {
            var column = document.GetElement(new ElementId(columnId));
            if (column == null) return 0.0;

            var coverDistances = new List<double>();
            foreach (var parameterId in new[]
            {
                BuiltInParameter.CLEAR_COVER_OTHER,
                BuiltInParameter.CLEAR_COVER_TOP,
                BuiltInParameter.CLEAR_COVER_BOTTOM
            })
            {
                var coverTypeId = column.get_Parameter(parameterId)
                    ?.AsElementId();
                if (coverTypeId == null
                    || coverTypeId == ElementId.InvalidElementId)
                {
                    continue;
                }
                var coverType =
                    document.GetElement(coverTypeId) as RebarCoverType;
                if (coverType?.CoverDistance > 0.0)
                    coverDistances.Add(coverType.CoverDistance);
            }
            return coverDistances.DefaultIfEmpty(0.0).Max();
        }

        private static IReadOnlyList<MainBarRunPlan> CreateLegacyRuns(
            IReadOnlyList<MainBarBeamReal> geometry,
            RebarBeamMainBarLevelType level,
            RebarBeamMainBarGroupType group,
            RebarBarTypeCustom barType,
            long targetHostBeamId,
            string stageName)
        {
            var modelDiameterFt = Math.Max(
                barType.ModelBarDiameter,
                barType.BarDiameter);
            var centerlineBendRadiusFt =
                barType.StandardBendDiameter > 0.0
                && modelDiameterFt > 0.0
                    ? barType.StandardBendDiameter / 2.0
                      + modelDiameterFt / 2.0
                    : 0.0;
            return geometry
                .Select((item, index) => new MainBarRunPlan(
                    $"{stageName}.lane.{index + 1}",
                    MainBarRunKind.Legacy,
                    level,
                    group,
                    index,
                    barType,
                    item.MainPoints,
                    new[] { targetHostBeamId },
                    targetHostBeamId,
                    centerlineBendRadiusFt:
                        centerlineBendRadiusFt))
                .ToList();
        }

        private static TerminalPath ExtractTerminalPath(
            MainBarBeamReal geometry,
            XYZ axis,
            double toleranceFt)
        {
            if (geometry?.MainPoints == null
                || geometry.MainPoints.Count < 2)
            {
                throw new InvalidOperationException(
                    "A main-bar run requires at least two ordered points.");
            }

            var points = geometry.MainPoints;
            var startIndex = IsTerminalLeg(
                points[0],
                points[1],
                axis,
                toleranceFt)
                ? 1
                : 0;
            var endIndex = IsTerminalLeg(
                points[points.Count - 2],
                points[points.Count - 1],
                axis,
                toleranceFt)
                ? points.Count - 2
                : points.Count - 1;
            if (endIndex <= startIndex)
            {
                throw new InvalidOperationException(
                    "Terminal anchorage consumed the entire main-bar core run.");
            }

            if (geometry.StartPoint == null || geometry.EndPoint == null)
            {
                throw new InvalidOperationException(
                    "The raw main-bar run endpoints are unavailable.");
            }
            var normalizedAxis = axis.Normalize();
            var transverseAxis =
                XYZ.BasisZ.CrossProduct(normalizedAxis).Normalize();
            var startCorrection =
                transverseAxis * (
                    geometry.StartPoint.DotProduct(transverseAxis)
                    - points[startIndex].DotProduct(transverseAxis))
                + XYZ.BasisZ * (
                    geometry.StartPoint.Z - points[startIndex].Z);
            var endCorrection =
                transverseAxis * (
                    geometry.EndPoint.DotProduct(transverseAxis)
                    - points[endIndex].DotProduct(transverseAxis))
                + XYZ.BasisZ * (
                    geometry.EndPoint.Z - points[endIndex].Z);
            var prefix = startIndex == 1
                ? new[]
                {
                    points[0] + startCorrection
                }
                : Array.Empty<XYZ>();
            var suffix = endIndex == points.Count - 2
                ? new[]
                {
                    points[points.Count - 1]
                    + endCorrection
                }
                : Array.Empty<XYZ>();
            return new TerminalPath(
                points[startIndex] + startCorrection,
                points[endIndex] + endCorrection,
                prefix,
                suffix);
        }

        private static bool IsTerminalLeg(
            XYZ start,
            XYZ end,
            XYZ axis,
            double toleranceFt)
        {
            if (start == null || end == null || axis == null) return false;
            var vector = end - start;
            return vector.GetLength() > toleranceFt
                && Math.Abs(vector.DotProduct(axis)) <= toleranceFt;
        }

        private static XYZ StableHorizontalDirection(XYZ direction)
        {
            if (direction == null)
                throw new InvalidOperationException("A beam direction is required.");
            var planar = new XYZ(direction.X, direction.Y, 0.0);
            if (planar.GetLength() <= DirectionTolerance)
                throw new InvalidOperationException(
                    "A horizontal beam direction could not be resolved.");
            planar = planar.Normalize();
            var dominant = Math.Abs(planar.X) >= Math.Abs(planar.Y)
                ? planar.X
                : planar.Y;
            return dominant < 0.0 ? -planar : planar;
        }

        private static BeamEnvelope CreateBeamEnvelope(
            BoxElement member,
            XYZ axisX,
            XYZ axisY)
        {
            var points = GetBoxPoints(member);
            if (points.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Beam {member.Id} has no usable solid geometry.");
            }
            return new BeamEnvelope(
                member.Id,
                points.Min(point => point.DotProduct(axisX)),
                points.Max(point => point.DotProduct(axisX)),
                points.Min(point => point.DotProduct(axisY)),
                points.Max(point => point.DotProduct(axisY)),
                points.Min(point => point.Z),
                points.Max(point => point.Z));
        }

        private static ColumnEnvelope CreateColumnEnvelope(
            Element column,
            IReadOnlyList<XYZ> points,
            XYZ axisX,
            XYZ axisY)
        {
            return new ColumnEnvelope(
                column.Id.Value,
                points.Min(point => point.DotProduct(axisX)),
                points.Max(point => point.DotProduct(axisX)),
                points.Min(point => point.DotProduct(axisY)),
                points.Max(point => point.DotProduct(axisY)),
                points.Min(point => point.Z),
                points.Max(point => point.Z));
        }

        private static List<XYZ> GetBoxPoints(BoxElement box)
        {
            var result = new List<XYZ>();
            foreach (var solid in box.Solids ?? new List<Solid>())
            {
                if (solid == null || solid.Volume <= 0.0) continue;
                result.AddRange(solid.GetPoints().Where(point => point != null));
            }
            if (result.Count > 0) return result;

            var corners = box.BoxElementPoint;
            if (corners == null) return result;
            result.AddRange(new[]
            {
                corners.P1, corners.P2, corners.P3, corners.P4,
                corners.P5, corners.P6, corners.P7, corners.P8
            }.Where(point => point != null));
            return result;
        }

        private static List<XYZ> GetElementPoints(Element element)
        {
            var result = new List<XYZ>();
            try
            {
                foreach (var solid in element.GetSolidsExtensions())
                {
                    if (solid == null || solid.Volume <= 0.0) continue;
                    result.AddRange(
                        solid.GetPoints().Where(point => point != null));
                }
            }
            catch
            {
                // An unusable column is not a candidate. The caller still
                // fails explicitly if no unique valid candidate remains.
            }
            return result;
        }

        private static bool IsRectangularAlignedColumn(
            Element column,
            XYZ axisX,
            XYZ axisY)
        {
            try
            {
                var hasHorizontalEdge = false;
                var solids = column.GetSolidsExtensions()
                    .Where(solid => solid != null && solid.Volume > 0.0)
                    .ToList();
                if (solids.Count != 1 || solids[0].Edges.Size != 12)
                    return false;

                var solid = solids[0];
                var points = solid.GetPoints()
                    .Where(point => point != null)
                    .ToList();
                if (points.Count == 0) return false;
                var expectedBoxVolume =
                    (points.Max(point => point.DotProduct(axisX))
                     - points.Min(point => point.DotProduct(axisX)))
                    * (points.Max(point => point.DotProduct(axisY))
                       - points.Min(point => point.DotProduct(axisY)))
                    * (points.Max(point => point.Z)
                       - points.Min(point => point.Z));
                var volumeTolerance = Math.Max(
                    1e-9,
                    expectedBoxVolume
                    * RectangularVolumeRelativeTolerance);
                if (expectedBoxVolume <= 0.0
                    || Math.Abs(solid.Volume - expectedBoxVolume)
                    > volumeTolerance)
                {
                    return false;
                }

                foreach (Edge edge in solid.Edges)
                {
                    var line = edge.AsCurve() as Line;
                    if (line == null) return false;
                    var direction = line.Direction;
                    if (Math.Abs(direction.Z)
                        >= 1.0 - DirectionTolerance)
                        continue;
                    if (Math.Abs(direction.Z) > DirectionTolerance)
                        return false;
                    var isAlongX =
                        Math.Abs(Math.Abs(direction.DotProduct(axisX)) - 1.0)
                        <= DirectionTolerance;
                    var isAlongY =
                        Math.Abs(Math.Abs(direction.DotProduct(axisY)) - 1.0)
                        <= DirectionTolerance;
                    if (!isAlongX && !isAlongY) return false;
                    hasHorizontalEdge = true;
                }
                return hasHorizontalEdge;
            }
            catch
            {
                return false;
            }
        }

        private static InvalidOperationException Unsupported(
            RebarExecutionContext context,
            string stageName,
            string code,
            string message)
        {
            context?.DiagnosticLog?.Record("main.transition.unsupported", new
            {
                stageName,
                code,
                message
            });
            return new InvalidOperationException(
                $"Different-section reinforcement is unsupported ({code}). "
                + message);
        }

        private sealed class TerminalSide
        {
            public XYZ CorePoint { get; }
            public IReadOnlyList<XYZ> OuterToCorePoints { get; }
            public double Station { get; }

            public TerminalSide(
                XYZ corePoint,
                IReadOnlyList<XYZ> outerToCorePoints,
                double station)
            {
                CorePoint = corePoint
                    ?? throw new ArgumentNullException(nameof(corePoint));
                OuterToCorePoints = outerToCorePoints
                    ?? throw new ArgumentNullException(
                        nameof(outerToCorePoints));
                Station = station;
            }
        }

        private sealed class IndependentLane
        {
            public int LaneIndex { get; }
            public TerminalSide BentSide { get; }
            public TerminalSide StraightSide { get; }
            public BeamEnvelope BentBeam { get; }
            public BeamEnvelope StraightBeam { get; }
            public double BentLaneY { get; }
            public double StraightLaneY { get; }

            public IndependentLane(
                int laneIndex,
                TerminalSide bentSide,
                TerminalSide straightSide,
                BeamEnvelope bentBeam,
                BeamEnvelope straightBeam,
                double bentLaneY,
                double straightLaneY)
            {
                LaneIndex = laneIndex;
                BentSide = bentSide
                    ?? throw new ArgumentNullException(nameof(bentSide));
                StraightSide = straightSide
                    ?? throw new ArgumentNullException(nameof(straightSide));
                BentBeam = bentBeam
                    ?? throw new ArgumentNullException(nameof(bentBeam));
                StraightBeam = straightBeam
                    ?? throw new ArgumentNullException(nameof(straightBeam));
                BentLaneY = bentLaneY;
                StraightLaneY = straightLaneY;
            }
        }

        private sealed class TerminalPath
        {
            public XYZ CoreStart { get; }
            public XYZ CoreEnd { get; }
            public IReadOnlyList<XYZ> Prefix { get; }
            public IReadOnlyList<XYZ> Suffix { get; }

            public TerminalPath(
                XYZ coreStart,
                XYZ coreEnd,
                IReadOnlyList<XYZ> prefix,
                IReadOnlyList<XYZ> suffix)
            {
                CoreStart = coreStart;
                CoreEnd = coreEnd;
                Prefix = prefix;
                Suffix = suffix;
            }
        }

        private sealed class BeamJointGeometry
        {
            public XYZ AxisX { get; }
            public XYZ AxisY { get; }
            public BeamEnvelope LeftBeam { get; }
            public BeamEnvelope RightBeam { get; }
            public long ColumnId { get; }
            public double ColumnStart { get; }
            public double ColumnEnd { get; }
            public double ColumnMinY { get; }
            public double ColumnMaxY { get; }
            public double ColumnBottomZ { get; }
            public double ColumnTopZ { get; }

            public BeamJointGeometry(
                XYZ axisX,
                XYZ axisY,
                BeamEnvelope leftBeam,
                BeamEnvelope rightBeam,
                long columnId,
                double columnStart,
                double columnEnd,
                double columnMinY,
                double columnMaxY,
                double columnBottomZ,
                double columnTopZ)
            {
                AxisX = axisX;
                AxisY = axisY;
                LeftBeam = leftBeam;
                RightBeam = rightBeam;
                ColumnId = columnId;
                ColumnStart = columnStart;
                ColumnEnd = columnEnd;
                ColumnMinY = columnMinY;
                ColumnMaxY = columnMaxY;
                ColumnBottomZ = columnBottomZ;
                ColumnTopZ = columnTopZ;
            }
        }

        private sealed class BendClearance
        {
            public double BendInsetFt { get; }
            public double CenterlineClearanceFt { get; }
            public double CenterlineBendRadiusFt { get; }
            public double MainBarRadiusFt { get; }
            public double ColumnCoverFt { get; }
            public double JointReinforcementDiameterFt { get; }
            public IReadOnlyList<ModeledColumnRebar> ModeledColumnRebars { get; }
            public int ModeledColumnRebarCount =>
                ModeledColumnRebars.Count;
            public int ModeledColumnStirrupCount { get; }
            public bool UsedConfiguredBeamStirrupFallback { get; }
            public bool UsedModeledColumnReinforcement =>
                ModeledColumnRebarCount > 0;

            public BendClearance(
                double bendInsetFt,
                double centerlineClearanceFt,
                double centerlineBendRadiusFt,
                double mainBarRadiusFt,
                double columnCoverFt,
                double jointReinforcementDiameterFt,
                IReadOnlyList<ModeledColumnRebar> modeledColumnRebars,
                int modeledColumnStirrupCount,
                bool usedConfiguredBeamStirrupFallback)
            {
                BendInsetFt = bendInsetFt;
                CenterlineClearanceFt = centerlineClearanceFt;
                CenterlineBendRadiusFt = centerlineBendRadiusFt;
                MainBarRadiusFt = mainBarRadiusFt;
                ColumnCoverFt = columnCoverFt;
                JointReinforcementDiameterFt =
                    jointReinforcementDiameterFt;
                ModeledColumnRebars = modeledColumnRebars
                    ?? Array.Empty<ModeledColumnRebar>();
                ModeledColumnStirrupCount = modeledColumnStirrupCount;
                UsedConfiguredBeamStirrupFallback =
                    usedConfiguredBeamStirrupFallback;
            }
        }

        private sealed class ConfiguredBeamStirrupClearance
        {
            public double DiameterFt { get; }
            public IReadOnlyList<string> TypeNames { get; }

            public ConfiguredBeamStirrupClearance(
                double diameterFt,
                IReadOnlyList<string> typeNames)
            {
                DiameterFt = diameterFt;
                TypeNames = typeNames ?? Array.Empty<string>();
            }
        }

        private sealed class ColumnReinforcementClearance
        {
            public static readonly ColumnReinforcementClearance Empty =
                new ColumnReinforcementClearance(
                    0.0,
                    0.0,
                    Array.Empty<ModeledColumnRebar>(),
                    0);

            public double MaximumBarDiameterFt { get; }
            public double MaximumStirrupDiameterFt { get; }
            public IReadOnlyList<ModeledColumnRebar> Rebars { get; }
            public int RebarCount => Rebars.Count;
            public int StirrupCount { get; }

            public ColumnReinforcementClearance(
                double maximumBarDiameterFt,
                double maximumStirrupDiameterFt,
                IReadOnlyList<ModeledColumnRebar> rebars,
                int stirrupCount)
            {
                MaximumBarDiameterFt = maximumBarDiameterFt;
                MaximumStirrupDiameterFt =
                    maximumStirrupDiameterFt;
                Rebars = rebars ?? Array.Empty<ModeledColumnRebar>();
                StirrupCount = stirrupCount;
            }
        }

        private sealed class ModeledColumnRebar
        {
            public Rebar Rebar { get; }
            public double DiameterFt { get; }
            public bool IsStirrup { get; }

            public ModeledColumnRebar(
                Rebar rebar,
                double diameterFt,
                bool isStirrup)
            {
                Rebar = rebar;
                DiameterFt = diameterFt;
                IsStirrup = isStirrup;
            }
        }

        private sealed class FilletArcGeometry
        {
            public XYZ Start { get; }
            public XYZ End { get; }
            public Arc Arc { get; }

            public FilletArcGeometry(
                XYZ start,
                XYZ end,
                Arc arc)
            {
                Start = start;
                End = end;
                Arc = arc;
            }
        }

        private sealed class ClashValidation
        {
            public double? ClosestSurfaceClearanceFt { get; }

            public ClashValidation(
                double? closestSurfaceClearanceFt)
            {
                ClosestSurfaceClearanceFt =
                    closestSurfaceClearanceFt;
            }
        }

        private sealed class BeamEnvelope
        {
            public long Id { get; }
            public double MinX { get; }
            public double MaxX { get; }
            public double MinY { get; }
            public double MaxY { get; }
            public double BottomZ { get; }
            public double TopZ { get; }

            public BeamEnvelope(
                long id,
                double minX,
                double maxX,
                double minY,
                double maxY,
                double bottomZ,
                double topZ)
            {
                Id = id;
                MinX = minX;
                MaxX = maxX;
                MinY = minY;
                MaxY = maxY;
                BottomZ = bottomZ;
                TopZ = topZ;
            }
        }

        private sealed class BeamLaneEnvelope
        {
            public long Id { get; }
            public double MinY { get; }
            public double MaxY { get; }

            public BeamLaneEnvelope(
                long id,
                double minY,
                double maxY)
            {
                Id = id;
                MinY = minY;
                MaxY = maxY;
            }
        }

        private sealed class ColumnEnvelope
        {
            public long Id { get; }
            public double MinX { get; }
            public double MaxX { get; }
            public double MinY { get; }
            public double MaxY { get; }
            public double BottomZ { get; }
            public double TopZ { get; }

            public ColumnEnvelope(
                long id,
                double minX,
                double maxX,
                double minY,
                double maxY,
                double bottomZ,
                double topZ)
            {
                Id = id;
                MinX = minX;
                MaxX = maxX;
                MinY = minY;
                MaxY = maxY;
                BottomZ = bottomZ;
                TopZ = topZ;
            }
        }
    }
}
