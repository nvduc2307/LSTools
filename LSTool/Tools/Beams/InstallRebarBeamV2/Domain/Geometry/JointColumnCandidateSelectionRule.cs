using System;
using System.Collections.Generic;
using System.Linq;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Geometry
{
    public enum JointColumnCandidateSelectionFailure
    {
        None = 0,
        InvalidInput = 1,
        NoCandidateContainsWorkingRange = 2,
        Ambiguous = 3
    }

    public sealed class JointColumnCandidateInterval
    {
        public long Id { get; }
        public double MinimumX { get; }
        public double MaximumX { get; }
        public double MinimumY { get; }
        public double MaximumY { get; }
        public double BottomZ { get; }
        public double TopZ { get; }
        public bool IsJoinedToEveryBeam { get; }

        public JointColumnCandidateInterval(
            long id,
            double bottomZ,
            double topZ,
            bool isJoinedToEveryBeam = false)
            : this(
                id,
                double.NaN,
                double.NaN,
                double.NaN,
                double.NaN,
                bottomZ,
                topZ,
                isJoinedToEveryBeam)
        {
        }

        public JointColumnCandidateInterval(
            long id,
            double minimumX,
            double maximumX,
            double minimumY,
            double maximumY,
            double bottomZ,
            double topZ,
            bool isJoinedToEveryBeam = false)
        {
            Id = id;
            MinimumX = minimumX;
            MaximumX = maximumX;
            MinimumY = minimumY;
            MaximumY = maximumY;
            BottomZ = bottomZ;
            TopZ = topZ;
            IsJoinedToEveryBeam = isJoinedToEveryBeam;
        }
    }

    public sealed class JointColumnCandidateSelectionResult
    {
        public JointColumnCandidateSelectionFailure Failure { get; }
        public long? SelectedId { get; }
        public IReadOnlyList<long> ContainingCandidateIds { get; }
        public bool UsedCommonJoinPriority { get; }
        public bool UsedEquivalentEnvelopeCollapse { get; }
        public bool IsSelected =>
            Failure == JointColumnCandidateSelectionFailure.None
            && SelectedId.HasValue;

        internal JointColumnCandidateSelectionResult(
            JointColumnCandidateSelectionFailure failure,
            long? selectedId,
            IReadOnlyList<long> containingCandidateIds,
            bool usedCommonJoinPriority,
            bool usedEquivalentEnvelopeCollapse)
        {
            Failure = failure;
            SelectedId = selectedId;
            ContainingCandidateIds = containingCandidateIds;
            UsedCommonJoinPriority = usedCommonJoinPriority;
            UsedEquivalentEnvelopeCollapse = usedEquivalentEnvelopeCollapse;
        }
    }

    /// <summary>
    /// Resolves the structural column that contains the actual vertical
    /// working range of a transition. A column that only touches the joint at
    /// a level boundary is not a candidate unless it contains that full range.
    /// Truly overlapping or duplicated columns remain ambiguous.
    /// </summary>
    public static class JointColumnCandidateSelectionRule
    {
        public static JointColumnCandidateSelectionResult Evaluate(
            IReadOnlyList<JointColumnCandidateInterval> candidates,
            double workingMinimumZ,
            double workingMaximumZ,
            double tolerance)
        {
            if (candidates == null
                || !IsFinite(workingMinimumZ)
                || !IsFinite(workingMaximumZ)
                || !IsFinite(tolerance)
                || tolerance < 0.0
                || workingMaximumZ < workingMinimumZ
                || candidates.Any(candidate =>
                    candidate == null
                    || !IsFinite(candidate.BottomZ)
                    || !IsFinite(candidate.TopZ)
                    || candidate.TopZ < candidate.BottomZ))
            {
                return Result(
                    JointColumnCandidateSelectionFailure.InvalidInput,
                    null,
                    Array.Empty<long>(),
                    false,
                    false);
            }

            var containing = candidates
                .Where(candidate =>
                    candidate.BottomZ <= workingMinimumZ + tolerance
                    && candidate.TopZ >= workingMaximumZ - tolerance)
                .OrderBy(candidate => candidate.Id)
                .ToList();
            if (containing.Count == 0)
            {
                return Result(
                    JointColumnCandidateSelectionFailure
                        .NoCandidateContainsWorkingRange,
                    null,
                    Array.Empty<long>(),
                    false,
                    false);
            }
            var joinedToEveryBeam = containing
                .Where(candidate => candidate.IsJoinedToEveryBeam)
                .ToList();
            var usedCommonJoinPriority = joinedToEveryBeam.Count > 0;
            var selectionPool = usedCommonJoinPriority
                ? joinedToEveryBeam
                : containing;
            if (selectionPool.Count > 1)
            {
                var reference = selectionPool[0];
                if (HasComparableEnvelope(reference)
                    && selectionPool.All(candidate =>
                        HasComparableEnvelope(candidate)
                        && AreEquivalentEnvelopes(
                            reference,
                            candidate,
                            tolerance)))
                {
                    return Result(
                        JointColumnCandidateSelectionFailure.None,
                        selectionPool[0].Id,
                        selectionPool
                            .Select(candidate => candidate.Id)
                            .ToList(),
                        usedCommonJoinPriority,
                        true);
                }
                return Result(
                    JointColumnCandidateSelectionFailure.Ambiguous,
                    null,
                    selectionPool.Select(candidate => candidate.Id).ToList(),
                    usedCommonJoinPriority,
                    false);
            }

            return Result(
                JointColumnCandidateSelectionFailure.None,
                selectionPool[0].Id,
                new[] { selectionPool[0].Id },
                usedCommonJoinPriority,
                false);
        }

        private static JointColumnCandidateSelectionResult Result(
            JointColumnCandidateSelectionFailure failure,
            long? selectedId,
            IReadOnlyList<long> containingCandidateIds,
            bool usedCommonJoinPriority,
            bool usedEquivalentEnvelopeCollapse)
        {
            return new JointColumnCandidateSelectionResult(
                failure,
                selectedId,
                containingCandidateIds,
                usedCommonJoinPriority,
                usedEquivalentEnvelopeCollapse);
        }

        private static bool HasComparableEnvelope(
            JointColumnCandidateInterval candidate)
        {
            return IsFinite(candidate.MinimumX)
                && IsFinite(candidate.MaximumX)
                && IsFinite(candidate.MinimumY)
                && IsFinite(candidate.MaximumY)
                && candidate.MaximumX >= candidate.MinimumX
                && candidate.MaximumY >= candidate.MinimumY;
        }

        private static bool AreEquivalentEnvelopes(
            JointColumnCandidateInterval first,
            JointColumnCandidateInterval second,
            double tolerance)
        {
            return Math.Abs(first.MinimumX - second.MinimumX) <= tolerance
                && Math.Abs(first.MaximumX - second.MaximumX) <= tolerance
                && Math.Abs(first.MinimumY - second.MinimumY) <= tolerance
                && Math.Abs(first.MaximumY - second.MaximumY) <= tolerance
                && Math.Abs(first.BottomZ - second.BottomZ) <= tolerance
                && Math.Abs(first.TopZ - second.TopZ) <= tolerance;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
