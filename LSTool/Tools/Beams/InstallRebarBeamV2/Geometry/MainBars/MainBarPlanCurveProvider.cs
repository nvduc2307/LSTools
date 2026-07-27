using Autodesk.Revit.DB;
using LSTool.Tools.Beams.InstallRebarBeamV2.Application;
using LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Plans;
using LSTool.Tools.Beams.InstallRebarBeamV2.models;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Geometry.MainBars
{
    /// <summary>
    /// Supplies dependent secondary-stirrup writers from the canonical main-bar
    /// plans. This prevents those writers from recalculating legacy Start-End
    /// diagonals after a Bent/Z plan has already been approved.
    /// </summary>
    public static class MainBarPlanCurveProvider
    {
        public static List<Line> GetLaneReferenceLines(
            RebarExecutionContext context,
            RebarBeamMainBarLevelType level,
            RebarBeamMainBarGroupType group,
            long? targetHostBeamId = null)
        {
            var plan = context.GetMainBarPlan(level, group);
            var runs = SelectRunsForPhysicalBeam(
                plan,
                targetHostBeamId);
            var result = new List<Line>(runs.Count);
            foreach (var run in runs)
            {
                var segments = CreateHorizontalSegments(run, context);
                if (segments.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Main-bar run '{run.RunId}' has no axial reference segment.");
                }
                result.Add(segments
                    .OrderByDescending(line =>
                        Math.Abs((
                            line.GetEndPoint(1) - line.GetEndPoint(0))
                            .DotProduct(context.XAxis)))
                    .First());
            }
            return result;
        }

        public static List<Line> GetHorizontalSegments(
            RebarExecutionContext context,
            RebarBeamMainBarLevelType level,
            RebarBeamMainBarGroupType group,
            long? targetHostBeamId = null)
        {
            var plan = context.GetMainBarPlan(level, group);
            return SelectRunsForPhysicalBeam(
                    plan,
                    targetHostBeamId)
                    .SelectMany(run => CreateHorizontalSegments(run, context))
                    .ToList();
        }

        private static IReadOnlyList<MainBarRunPlan>
            SelectRunsForPhysicalBeam(
                MainBarCreationPlan plan,
                long? targetHostBeamId)
        {
            if (!targetHostBeamId.HasValue
                || !plan.Runs.Any(run =>
                    run.IsIndependentJointAnchorage))
            {
                return plan.Runs;
            }

            return plan.Runs
                .Where(run =>
                    run.TargetHostBeamId
                    == targetHostBeamId.Value)
                .ToList();
        }

        private static List<Line> CreateHorizontalSegments(
            MainBarRunPlan run,
            RebarExecutionContext context)
        {
            if (run.OrderedPoints == null || run.OrderedPoints.Count < 2)
            {
                throw new InvalidOperationException(
                    $"Main-bar run '{run.RunId}' has no usable point chain.");
            }

            var tolerance =
                context.Document.Application.ShortCurveTolerance;
            var result = new List<Line>();
            for (var index = 1; index < run.OrderedPoints.Count; index++)
            {
                var start = run.OrderedPoints[index - 1];
                var end = run.OrderedPoints[index];
                var vector = end - start;
                if (vector.GetLength() < tolerance)
                {
                    throw new InvalidOperationException(
                        $"Main-bar run '{run.RunId}' contains a short segment.");
                }
                var direction = vector.Normalize();
                if (Math.Abs(
                        Math.Abs(direction.DotProduct(context.XAxis))
                        - 1.0) > 1e-6)
                    continue;
                result.Add(Line.CreateBound(start, end));
            }
            return result;
        }
    }
}
