using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using LSTool.Tools.Beams.InstallRebarBeamV2.Application;
using LSTool.Tools.Beams.InstallRebarBeamV2.Application.Diagnostics;
using LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Plans;
using LSTool.Tools.Beams.InstallRebarBeamV2.Geometry.MainBars;
using RIMT.Utils.RevRebars;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Revit.Writers
{
    public sealed class MainBarRebarWriter
    {
        public List<Rebar> Create(
            MainBarCreationPlan plan,
            RebarExecutionContext context)
        {
            try
            {
                var result = new List<Rebar>(plan.Runs.Count);
                var createdRuns =
                    new List<Tuple<MainBarRunPlan, Rebar>>(
                        plan.Runs.Count);
                var requiresExactGeometryValidation = plan.Runs.Any(
                    run => run.RequiresStrictGeometryValidation);
                if (requiresExactGeometryValidation)
                {
                    ValidateShapeSeedAvailability(
                        plan,
                        context.Document);
                }
                foreach (var run in plan.Runs)
                {
                    var targetHostId =
                        context.GetBeamHost(run.TargetHostBeamId).Id;
                    var curves = CreateStrictCurves(
                        run,
                        -context.YAxis,
                        context.Document.Application.ShortCurveTolerance);
                    context.DiagnosticLog?.Record(
                        "main.rebar.create.requested",
                        new
                        {
                            plan.StageName,
                            run.RunId,
                            kind = run.Kind.ToString(),
                            run.LaneIndex,
                            barType = run.BarType.NameStyle,
                            nominalBarDiameterMm = Math.Round(
                                run.BarType.BarDiameter * 304.8,
                                3),
                            modelBarDiameterMm = Math.Round(
                                run.BarType.ModelBarDiameter * 304.8,
                                3),
                            standardBendDiameterMm = Math.Round(
                                run.BarType.StandardBendDiameter * 304.8,
                                3),
                            plannedCenterlineBendRadiusMm = Math.Round(
                                run.CenterlineBendRadiusFt * 304.8,
                                3),
                            run.TargetHostBeamId,
                            targetHostId = targetHostId.Value,
                            run.JointElementId,
                            requiredAnchorageMm = Math.Round(
                                run.RequiredAnchorageLengthFt * 304.8,
                                3),
                            providedAnchorageMm = Math.Round(
                                run.ProvidedAnchorageLengthFt * 304.8,
                                3),
                            deltaZMm = Math.Round(
                                run.ElevationChangeFt * 304.8,
                                3),
                            points = run.OrderedPoints
                                .Select(RebarDiagnosticLog.PointSnapshot)
                                .ToList()
                        });
                    var rebar = RebarCreationCompat.CreateFromCurves(
                        context.Document,
                        RebarStyle.Standard,
                        run.BarType.RebarBarType,
                        context.TemporaryHost,
                        -context.YAxis,
                        curves,
                        true,
                        true);
                    if (rebar == null)
                    {
                        throw new InvalidOperationException(
                            $"Revit could not create main-bar run '{run.RunId}'. "
                            + "No compatible rebar shape was returned.");
                    }
                    ValidateCreatedRebar(run, rebar, context);
                    RevRebarUtils.SetSolidRebar3DView(rebar, context.Document.ActiveView);
                    context.RegisterTargetHost(
                        rebar,
                        run.TargetHostBeamId);
                    context.DiagnosticLog?.RecordRebar(
                        "main.rebar.created",
                        rebar,
                        run.TargetHostBeamId,
                        targetHostId,
                        run.RunId);
                    context.RegisterMainBarRun(rebar, run);
                    createdRuns.Add(Tuple.Create(run, rebar));
                    result.Add(rebar);
                }

                if (requiresExactGeometryValidation)
                {
                    context.Document.Regenerate();
                    foreach (var createdRun in createdRuns)
                    {
                        ValidateActualCenterline(
                            createdRun.Item1,
                            createdRun.Item2,
                            context.Document,
                            context.DiagnosticLog,
                            "after-create");
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to create {plan.StageName} bars.", ex);
            }
        }

        private static List<Curve> CreateStrictCurves(
            MainBarRunPlan run,
            XYZ planeNormal,
            double shortCurveTolerance)
        {
            if (run?.OrderedPoints == null || run.OrderedPoints.Count < 2)
            {
                throw new InvalidOperationException(
                    $"Main-bar run '{run?.RunId}' requires at least two ordered points.");
            }
            if (planeNormal == null || planeNormal.GetLength() <= 0.0)
            {
                throw new InvalidOperationException(
                    $"Main-bar run '{run.RunId}' has no valid creation-plane normal.");
            }

            var normal = planeNormal.Normalize();
            var minimumLength = Math.Max(shortCurveTolerance, 1e-9);
            var planeOffset = run.OrderedPoints[0].DotProduct(normal);
            var curves = new List<Curve>(run.OrderedPoints.Count - 1);
            for (var index = 0; index < run.OrderedPoints.Count; index++)
            {
                var point = run.OrderedPoints[index];
                if (!IsFinite(point))
                {
                    throw new InvalidOperationException(
                        $"Main-bar run '{run.RunId}' contains a null or non-finite "
                        + $"point at index {index}.");
                }
                if (Math.Abs(point.DotProduct(normal) - planeOffset)
                    > minimumLength)
                {
                    throw new InvalidOperationException(
                        $"Main-bar run '{run.RunId}' is not planar at point {index}.");
                }
                if (index == 0) continue;

                var previous = run.OrderedPoints[index - 1];
                var length = previous.DistanceTo(point);
                if (length < minimumLength)
                {
                    throw new InvalidOperationException(
                        $"Main-bar run '{run.RunId}' has a zero/short segment "
                        + $"between points {index - 1} and {index} "
                        + $"({length * 304.8:0.###} mm).");
                }
                curves.Add(Line.CreateBound(previous, point));
            }

            if (curves.Count != run.OrderedPoints.Count - 1)
            {
                throw new InvalidOperationException(
                    $"Main-bar run '{run.RunId}' produced an incomplete curve chain.");
            }
            if (run.Kind == MainBarRunKind.BentZTransition
                && run.OrderedPoints.Count < 4)
            {
                throw new InvalidOperationException(
                    $"Bent/Z run '{run.RunId}' must retain both transition vertices.");
            }
            if (run.Kind
                    == MainBarRunKind.IndependentStraightThroughAnchor
                && run.OrderedPoints.Count < 2)
            {
                throw new InvalidOperationException(
                    $"Independent straight-through run '{run.RunId}' "
                    + "requires a complete source-to-anchor point chain.");
            }
            if (run.Kind
                    == MainBarRunKind.IndependentBentJointAnchor
                && run.OrderedPoints.Count < 3)
            {
                throw new InvalidOperationException(
                    $"Independent bent-anchor run '{run.RunId}' must retain "
                    + "its horizontal and vertical legs.");
            }

            return curves;
        }

        private static void ValidateCreatedRebar(
            MainBarRunPlan run,
            Rebar rebar,
            RebarExecutionContext context)
        {
            var currentHostId = rebar.GetHostId();
            if (currentHostId == null
                || currentHostId.Value != context.TemporaryHost.Id.Value)
            {
                throw new InvalidOperationException(
                    $"Main-bar run '{run.RunId}' was created on host "
                    + $"{currentHostId?.Value}; expected temporary host "
                    + $"{context.TemporaryHost.Id.Value}.");
            }
            if (!run.RequiresStrictGeometryValidation) return;

            var shapeId = rebar.GetShapeId();
            if (shapeId == null
                || shapeId.Value == ElementId.InvalidElementId.Value)
            {
                throw new InvalidOperationException(
                    $"Strict main-bar run '{run.RunId}' has no resolved "
                    + "RebarShape. "
                    + "Load a shape seed with enough segment parameters and "
                    + "run the command again.");
            }
            var shape = context.Document.GetElement(shapeId) as RebarShape;
            var definition = shape?.GetRebarShapeDefinition()
                as RebarShapeDefinitionBySegments;
            var requiredShapeSegments =
                GetRequiredShapeSegmentCount(run);
            if (definition == null
                || definition.NumberOfSegments < requiredShapeSegments)
            {
                throw new InvalidOperationException(
                    $"Strict main-bar run '{run.RunId}' resolved to an "
                    + "incompatible RebarShape. A shape-driven definition "
                    + $"with at least {requiredShapeSegments} segments is "
                    + "required.");
            }
        }

        private static int GetRequiredShapeSegmentCount(
            MainBarRunPlan run)
        {
            var result = 1;
            XYZ previousDirection = null;
            for (var index = 1;
                 index < run.OrderedPoints.Count;
                 index++)
            {
                var direction =
                    (run.OrderedPoints[index]
                     - run.OrderedPoints[index - 1]).Normalize();
                if (previousDirection != null
                    && previousDirection
                        .CrossProduct(direction)
                        .GetLength() > 1e-6)
                {
                    result++;
                }
                previousDirection = direction;
            }
            return result;
        }

        private static void ValidateShapeSeedAvailability(
            MainBarCreationPlan plan,
            Document document)
        {
            var requiredSegmentCount = plan.Runs
                .Where(run =>
                    run.RequiresStrictGeometryValidation)
                .Select(GetRequiredShapeSegmentCount)
                .DefaultIfEmpty(0)
                .Max();
            if (requiredSegmentCount <= 0) return;

            var hasSeed = new FilteredElementCollector(document)
                .OfClass(typeof(RebarShape))
                .Cast<RebarShape>()
                .Where(shape =>
                    shape.RebarStyle == RebarStyle.Standard)
                .Select(shape =>
                    shape.GetRebarShapeDefinition()
                        as RebarShapeDefinitionBySegments)
                .Any(definition =>
                    definition != null
                    && definition.NumberOfSegments
                    >= requiredSegmentCount);
            if (!hasSeed)
            {
                throw new InvalidOperationException(
                    $"The active document has no Standard RebarShape seed "
                    + $"with at least {requiredSegmentCount} segment "
                    + "parameters. Load a compatible main-bar shape family "
                    + "and run the command again.");
            }
        }

        public static void ValidateActualCenterline(
            MainBarRunPlan run,
            Rebar rebar,
            Document document,
            RebarDiagnosticLog diagnosticLog,
            string phase)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            if (rebar == null || !rebar.IsValidObject)
            {
                throw new InvalidOperationException(
                    $"Main-bar run '{run.RunId}' has no valid created Rebar "
                    + $"during {phase ?? "validation"}.");
            }
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            // Flat legacy plans retain their established behavior. When a
            // stage contains any different-section run, the caller validates
            // every created run through this strict postcondition.
            if (run.CenterlineBendRadiusFt <= 0.0
                && run.OrderedPoints.Count > 2)
            {
                throw new InvalidOperationException(
                    $"Main-bar run '{run.RunId}' has no centerline bend "
                    + "radius for actual-geometry validation.");
            }
            if (rebar.NumberOfBarPositions != 1
                || !rebar.DoesBarExistAtPosition(0))
            {
                throw new InvalidOperationException(
                    $"Main-bar run '{run.RunId}' must create exactly one "
                    + "physical bar position.");
            }

            IList<Curve> actualCurves;
            try
            {
                actualCurves = rebar.GetCenterlineCurves(
                    true,
                    false,
                    false,
                    MultiplanarOption.IncludeAllMultiplanarCurves,
                    0);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"Actual centerline geometry for main-bar run "
                    + $"'{run.RunId}' could not be read.",
                    exception);
            }
            if (actualCurves == null || actualCurves.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Main-bar run '{run.RunId}' returned no actual "
                    + "centerline curves.");
            }

            var constructionTolerance = Math.Max(
                document.Application.ShortCurveTolerance,
                1e-9);
            var expectedCurves =
                BentZTransitionPlanner.CreatePlannedRunCenterline(
                    run,
                    constructionTolerance);
            if (expectedCurves.Count != actualCurves.Count)
            {
                throw new InvalidOperationException(
                    $"Main-bar run '{run.RunId}' changed curve topology "
                    + $"during {phase ?? "validation"}: expected "
                    + $"{expectedCurves.Count} curves, actual "
                    + $"{actualCurves.Count}.");
            }

            var directDeviationFt = GetSequenceDeviation(
                expectedCurves,
                actualCurves.ToList());
            var reversedDeviationFt = GetSequenceDeviation(
                expectedCurves,
                actualCurves.Reverse().ToList());
            var maximumDeviationFt = Math.Min(
                directDeviationFt,
                reversedDeviationFt);
            var allowedDeviationFt = 0.01 / 304.8;
            if (double.IsNaN(maximumDeviationFt)
                || double.IsInfinity(maximumDeviationFt)
                || maximumDeviationFt > allowedDeviationFt)
            {
                throw new InvalidOperationException(
                    $"Main-bar run '{run.RunId}' changed geometry during "
                    + $"{phase ?? "validation"}. Maximum expected/actual "
                    + $"deviation is {maximumDeviationFt * 304.8:0.###} mm; "
                    + "the limit is 0.01 mm.");
            }

            diagnosticLog?.Record(
                "main.rebar.geometry.validated",
                new
                {
                    phase,
                    run.RunId,
                    rebarId = rebar.Id.Value,
                    curveCount = actualCurves.Count,
                    maximumDeviationMm = Math.Round(
                        maximumDeviationFt * 304.8,
                        6)
                });
        }

        private static double GetSequenceDeviation(
            IReadOnlyList<Curve> expected,
            IReadOnlyList<Curve> actual)
        {
            var maximumDeviation = 0.0;
            for (var index = 0; index < expected.Count; index++)
            {
                maximumDeviation = Math.Max(
                    maximumDeviation,
                    GetCurveDeviation(
                        expected[index],
                        actual[index]));
            }
            return maximumDeviation;
        }

        private static double GetCurveDeviation(
            Curve expected,
            Curve actual)
        {
            if (expected == null || actual == null)
                return double.PositiveInfinity;
            var directEndpointDeviation = Math.Max(
                expected.GetEndPoint(0).DistanceTo(
                    actual.GetEndPoint(0)),
                expected.GetEndPoint(1).DistanceTo(
                    actual.GetEndPoint(1)));
            var reversedEndpointDeviation = Math.Max(
                expected.GetEndPoint(0).DistanceTo(
                    actual.GetEndPoint(1)),
                expected.GetEndPoint(1).DistanceTo(
                    actual.GetEndPoint(0)));
            var endpointDeviation = Math.Min(
                directEndpointDeviation,
                reversedEndpointDeviation);

            if (expected is Line && actual is Line)
                return endpointDeviation;
            if (expected is Arc expectedArc
                && actual is Arc actualArc)
            {
                return new[]
                {
                    endpointDeviation,
                    expectedArc.Center.DistanceTo(actualArc.Center),
                    Math.Abs(expectedArc.Radius - actualArc.Radius),
                    expectedArc.Evaluate(0.5, true).DistanceTo(
                        actualArc.Evaluate(0.5, true)),
                    Math.Abs(expectedArc.Length - actualArc.Length)
                }.Max();
            }
            return double.PositiveInfinity;
        }

        private static bool IsFinite(XYZ point)
        {
            return point != null
                && IsFinite(point.X)
                && IsFinite(point.Y)
                && IsFinite(point.Z);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
