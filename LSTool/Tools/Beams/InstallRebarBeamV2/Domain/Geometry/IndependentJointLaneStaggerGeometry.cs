using System;
using System.Collections.Generic;
using System.Linq;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Geometry
{
    /// <summary>
    /// Describes whether the bent-anchor lanes can be moved away from the
    /// fixed straight-through lanes without violating the usable beam width.
    /// </summary>
    public enum IndependentJointLaneStaggerStatus
    {
        Planned = 0,
        Unsupported = 1
    }

    /// <summary>
    /// Machine-readable reason why a transverse lane layout cannot be planned
    /// or why a supplied layout does not satisfy the same constraints.
    /// </summary>
    public enum IndependentJointLaneStaggerFailure
    {
        None = 0,
        MissingInput = 1,
        NonFiniteValue = 2,
        InvalidTolerance = 3,
        InvalidBounds = 4,
        InvalidRequiredSeparation = 5,
        OriginalLaneOutsideBounds = 6,
        DuplicateOriginalLane = 7,
        NoFeasibleLayout = 8,
        AmbiguousMinimumDisplacementLayout = 9,
        OutputCountMismatch = 10,
        OutputLaneOutsideBounds = 11,
        InsufficientStraightLaneSeparation = 12,
        InsufficientBentLaneSeparation = 13,
        InvalidPreferredShiftDirection = 14
    }

    /// <summary>
    /// Revit-independent inputs for transverse staggering.
    ///
    /// Bent lane order is not significant. The planned output is returned in
    /// the same order as OriginalBentLaneYs. StraightThroughLaneYs are fixed
    /// obstacles. MinAllowedY and MaxAllowedY are centerline limits after
    /// cover, tie and bar-radius deductions have already been applied.
    /// PreferredShiftDirection must be +1 or -1 and must be derived from a
    /// stable physical transverse axis.
    /// </summary>
    public sealed class IndependentJointLaneStaggerInput
    {
        public IReadOnlyList<double>? OriginalBentLaneYs { get; }
        public IReadOnlyList<double>? StraightThroughLaneYs { get; }
        public double MinAllowedY { get; }
        public double MaxAllowedY { get; }
        public double RequiredCenterlineSeparation { get; }
        public double PreferredShiftDirection { get; }
        public double Tolerance { get; }

        public IndependentJointLaneStaggerInput(
            IReadOnlyList<double>? originalBentLaneYs,
            IReadOnlyList<double>? straightThroughLaneYs,
            double minAllowedY,
            double maxAllowedY,
            double requiredCenterlineSeparation,
            double preferredShiftDirection,
            double tolerance)
        {
            OriginalBentLaneYs = originalBentLaneYs;
            StraightThroughLaneYs = straightThroughLaneYs;
            MinAllowedY = minAllowedY;
            MaxAllowedY = maxAllowedY;
            RequiredCenterlineSeparation =
                requiredCenterlineSeparation;
            PreferredShiftDirection = preferredShiftDirection;
            Tolerance = tolerance;
        }
    }

    public sealed class IndependentJointLaneStaggerResult
    {
        private static readonly IReadOnlyList<double> NoLanes =
            Array.AsReadOnly(new double[0]);

        public IndependentJointLaneStaggerStatus Status { get; }
        public IndependentJointLaneStaggerFailure Failure { get; }
        public string Message { get; }
        public IReadOnlyList<double> ShiftedBentLaneYs { get; }
        public double TotalAbsoluteDisplacement { get; }
        public double MaximumAbsoluteDisplacement { get; }

        private IndependentJointLaneStaggerResult(
            IndependentJointLaneStaggerStatus status,
            IndependentJointLaneStaggerFailure failure,
            string message,
            IReadOnlyList<double> shiftedBentLaneYs,
            double totalAbsoluteDisplacement,
            double maximumAbsoluteDisplacement)
        {
            Status = status;
            Failure = failure;
            Message = message;
            ShiftedBentLaneYs = shiftedBentLaneYs;
            TotalAbsoluteDisplacement = totalAbsoluteDisplacement;
            MaximumAbsoluteDisplacement = maximumAbsoluteDisplacement;
        }

        internal static IndependentJointLaneStaggerResult Unsupported(
            IndependentJointLaneStaggerFailure failure,
            string message)
        {
            return new IndependentJointLaneStaggerResult(
                IndependentJointLaneStaggerStatus.Unsupported,
                failure,
                message,
                NoLanes,
                0.0,
                0.0);
        }

        internal static IndependentJointLaneStaggerResult Planned(
            double[] shiftedBentLaneYs,
            double totalAbsoluteDisplacement,
            double maximumAbsoluteDisplacement)
        {
            return new IndependentJointLaneStaggerResult(
                IndependentJointLaneStaggerStatus.Planned,
                IndependentJointLaneStaggerFailure.None,
                string.Empty,
                Array.AsReadOnly(shiftedBentLaneYs),
                totalAbsoluteDisplacement,
                maximumAbsoluteDisplacement);
        }
    }

    public sealed class IndependentJointLaneStaggerValidationResult
    {
        public bool IsValid { get; }
        public IndependentJointLaneStaggerFailure Failure { get; }
        public string Message { get; }

        private IndependentJointLaneStaggerValidationResult(
            bool isValid,
            IndependentJointLaneStaggerFailure failure,
            string message)
        {
            IsValid = isValid;
            Failure = failure;
            Message = message;
        }

        internal static IndependentJointLaneStaggerValidationResult Valid()
        {
            return new IndependentJointLaneStaggerValidationResult(
                true,
                IndependentJointLaneStaggerFailure.None,
                string.Empty);
        }

        internal static IndependentJointLaneStaggerValidationResult Unsupported(
            IndependentJointLaneStaggerFailure failure,
            string message)
        {
            return new IndependentJointLaneStaggerValidationResult(
                false,
                failure,
                message);
        }
    }

    /// <summary>
    /// Pure one-dimensional lane optimizer used to make the deep-beam bent
    /// anchors physically separate from the shallow-beam straight-through
    /// anchors.
    ///
    /// The optimizer preserves lane order and minimizes total absolute
    /// displacement. Equal-cost layouts are ranked by their total displacement
    /// in PreferredShiftDirection. Candidate positions include all constraint boundaries,
    /// original lanes and their bar-spacing closures, which are the break
    /// points of this one-dimensional L1 problem. If more than one distinct
    /// equal-cost and equal-preference layout remains, the result is
    /// unsupported. The caller must derive PreferredShiftDirection from a
    /// stable physical axis; reversing that axis must reverse the preference.
    /// </summary>
    public static class IndependentJointLaneStaggerGeometry
    {
        public static IndependentJointLaneStaggerResult Plan(
            IndependentJointLaneStaggerInput input)
        {
            InputSnapshot? snapshot;
            IndependentJointLaneStaggerResult? inputFailure =
                ValidateAndSnapshotInput(input, out snapshot);
            if (inputFailure != null || snapshot == null)
            {
                return inputFailure
                    ?? IndependentJointLaneStaggerResult.Unsupported(
                        IndependentJointLaneStaggerFailure.MissingInput,
                        "Lane-stagger input is required.");
            }

            double numericalEpsilon = NumericalEpsilon(snapshot.Tolerance);
            List<IndexedLane> orderedBentLanes = snapshot.BentLaneYs
                .Select(
                    (laneY, originalIndex) =>
                        new IndexedLane(laneY, originalIndex))
                .OrderBy(lane => lane.Y)
                .ThenBy(lane => lane.OriginalIndex)
                .ToList();
            List<double> candidates = CreateCandidates(
                snapshot,
                orderedBentLanes.Count,
                numericalEpsilon);
            if (candidates.Count == 0)
            {
                return IndependentJointLaneStaggerResult.Unsupported(
                    IndependentJointLaneStaggerFailure.NoFeasibleLayout,
                    "No transverse centerline remains after applying the "
                    + "straight-lane clearance bands.");
            }

            PlacementState?[,] states =
                new PlacementState?[orderedBentLanes.Count, candidates.Count];
            for (int candidateIndex = 0;
                candidateIndex < candidates.Count;
                candidateIndex++)
            {
                states[0, candidateIndex] = new PlacementState(
                    Math.Abs(
                        candidates[candidateIndex]
                        - orderedBentLanes[0].Y),
                    snapshot.PreferredDirection
                    * (candidates[candidateIndex]
                        - orderedBentLanes[0].Y),
                    -1,
                    false);
            }

            double costEpsilon =
                numericalEpsilon * Math.Max(1, orderedBentLanes.Count);
            for (int laneIndex = 1;
                laneIndex < orderedBentLanes.Count;
                laneIndex++)
            {
                for (int candidateIndex = 0;
                    candidateIndex < candidates.Count;
                    candidateIndex++)
                {
                    double bestPreviousCost = double.PositiveInfinity;
                    double bestPreviousPreference =
                        double.NegativeInfinity;
                    int bestPreviousIndex = -1;
                    bool previousIsAmbiguous = false;

                    for (int previousIndex = 0;
                        previousIndex < candidateIndex;
                        previousIndex++)
                    {
                        if (candidates[candidateIndex]
                                - candidates[previousIndex]
                            + numericalEpsilon
                            < snapshot.RequiredSeparation)
                        {
                            continue;
                        }

                        PlacementState? previousState =
                            states[laneIndex - 1, previousIndex];
                        if (previousState == null)
                        {
                            continue;
                        }

                        if (previousState.Cost
                            < bestPreviousCost - costEpsilon)
                        {
                            bestPreviousCost = previousState.Cost;
                            bestPreviousPreference =
                                previousState.PreferenceScore;
                            bestPreviousIndex = previousIndex;
                            previousIsAmbiguous =
                                previousState.IsAmbiguous;
                        }
                        else if (Math.Abs(
                                     previousState.Cost
                                     - bestPreviousCost)
                                 <= costEpsilon
                                 && previousState.PreferenceScore
                                     > bestPreviousPreference
                                         + costEpsilon)
                        {
                            bestPreviousPreference =
                                previousState.PreferenceScore;
                            bestPreviousIndex = previousIndex;
                            previousIsAmbiguous =
                                previousState.IsAmbiguous;
                        }
                        else if (Math.Abs(
                                     previousState.Cost
                                     - bestPreviousCost)
                                 <= costEpsilon
                                 && Math.Abs(
                                     previousState.PreferenceScore
                                     - bestPreviousPreference)
                                     <= costEpsilon)
                        {
                            previousIsAmbiguous = true;
                        }
                    }

                    if (bestPreviousIndex < 0)
                    {
                        continue;
                    }

                    states[laneIndex, candidateIndex] =
                        new PlacementState(
                            bestPreviousCost
                            + Math.Abs(
                                candidates[candidateIndex]
                                - orderedBentLanes[laneIndex].Y),
                            bestPreviousPreference
                            + snapshot.PreferredDirection
                            * (candidates[candidateIndex]
                                - orderedBentLanes[laneIndex].Y),
                            bestPreviousIndex,
                            previousIsAmbiguous);
                }
            }

            int finalLaneIndex = orderedBentLanes.Count - 1;
            double bestCost = double.PositiveInfinity;
            double bestPreference = double.NegativeInfinity;
            int bestFinalCandidateIndex = -1;
            bool bestIsAmbiguous = false;
            for (int candidateIndex = 0;
                candidateIndex < candidates.Count;
                candidateIndex++)
            {
                PlacementState? state =
                    states[finalLaneIndex, candidateIndex];
                if (state == null)
                {
                    continue;
                }

                if (state.Cost < bestCost - costEpsilon)
                {
                    bestCost = state.Cost;
                    bestPreference = state.PreferenceScore;
                    bestFinalCandidateIndex = candidateIndex;
                    bestIsAmbiguous = state.IsAmbiguous;
                }
                else if (Math.Abs(state.Cost - bestCost) <= costEpsilon
                         && state.PreferenceScore
                             > bestPreference + costEpsilon)
                {
                    bestPreference = state.PreferenceScore;
                    bestFinalCandidateIndex = candidateIndex;
                    bestIsAmbiguous = state.IsAmbiguous;
                }
                else if (Math.Abs(state.Cost - bestCost) <= costEpsilon
                         && Math.Abs(
                             state.PreferenceScore - bestPreference)
                             <= costEpsilon)
                {
                    bestIsAmbiguous = true;
                }
            }

            if (bestFinalCandidateIndex < 0)
            {
                return IndependentJointLaneStaggerResult.Unsupported(
                    IndependentJointLaneStaggerFailure.NoFeasibleLayout,
                    "The usable transverse width cannot contain all bent "
                    + "anchors at the required centerline separation.");
            }
            if (bestIsAmbiguous)
            {
                return IndependentJointLaneStaggerResult.Unsupported(
                    IndependentJointLaneStaggerFailure
                        .AmbiguousMinimumDisplacementLayout,
                    "More than one minimum-displacement transverse layout "
                    + "exists. A caller-supplied side preference or another "
                    + "physical constraint is required.");
            }

            var shiftedByOriginalIndex =
                new double[orderedBentLanes.Count];
            int reconstructionCandidateIndex =
                bestFinalCandidateIndex;
            for (int laneIndex = finalLaneIndex;
                laneIndex >= 0;
                laneIndex--)
            {
                IndexedLane lane = orderedBentLanes[laneIndex];
                shiftedByOriginalIndex[lane.OriginalIndex] =
                    candidates[reconstructionCandidateIndex];
                PlacementState state =
                    states[laneIndex, reconstructionCandidateIndex]!;
                reconstructionCandidateIndex = state.PreviousCandidateIndex;
            }

            IndependentJointLaneStaggerValidationResult validation =
                Validate(input, shiftedByOriginalIndex);
            if (!validation.IsValid)
            {
                return IndependentJointLaneStaggerResult.Unsupported(
                    validation.Failure,
                    validation.Message);
            }

            double maximumDisplacement = 0.0;
            for (int index = 0;
                index < shiftedByOriginalIndex.Length;
                index++)
            {
                maximumDisplacement = Math.Max(
                    maximumDisplacement,
                    Math.Abs(
                        shiftedByOriginalIndex[index]
                        - snapshot.BentLaneYs[index]));
            }

            return IndependentJointLaneStaggerResult.Planned(
                shiftedByOriginalIndex,
                bestCost,
                maximumDisplacement);
        }

        public static IndependentJointLaneStaggerValidationResult Validate(
            IndependentJointLaneStaggerInput input,
            IReadOnlyList<double>? shiftedBentLaneYs)
        {
            InputSnapshot? snapshot;
            IndependentJointLaneStaggerResult? inputFailure =
                ValidateAndSnapshotInput(input, out snapshot);
            if (inputFailure != null || snapshot == null)
            {
                return IndependentJointLaneStaggerValidationResult.Unsupported(
                    inputFailure?.Failure
                        ?? IndependentJointLaneStaggerFailure.MissingInput,
                    inputFailure?.Message
                        ?? "Lane-stagger input is required.");
            }
            if (shiftedBentLaneYs == null)
            {
                return IndependentJointLaneStaggerValidationResult.Unsupported(
                    IndependentJointLaneStaggerFailure.MissingInput,
                    "Shifted bent lanes are required.");
            }
            if (shiftedBentLaneYs.Count != snapshot.BentLaneYs.Length)
            {
                return IndependentJointLaneStaggerValidationResult.Unsupported(
                    IndependentJointLaneStaggerFailure.OutputCountMismatch,
                    "The shifted-lane count must equal the original bent-lane "
                    + "count.");
            }
            if (shiftedBentLaneYs.Any(laneY => !IsFinite(laneY)))
            {
                return IndependentJointLaneStaggerValidationResult.Unsupported(
                    IndependentJointLaneStaggerFailure.NonFiniteValue,
                    "All shifted bent-lane coordinates must be finite.");
            }

            for (int index = 0;
                index < shiftedBentLaneYs.Count;
                index++)
            {
                double shiftedY = shiftedBentLaneYs[index];
                if (shiftedY < snapshot.MinAllowedY - snapshot.Tolerance
                    || shiftedY >
                        snapshot.MaxAllowedY + snapshot.Tolerance)
                {
                    return IndependentJointLaneStaggerValidationResult
                        .Unsupported(
                            IndependentJointLaneStaggerFailure
                                .OutputLaneOutsideBounds,
                            "A shifted bent lane lies outside the usable "
                            + "centerline bounds.");
                }

                for (int straightIndex = 0;
                    straightIndex < snapshot.StraightLaneYs.Length;
                    straightIndex++)
                {
                    if (Math.Abs(
                            shiftedY
                            - snapshot.StraightLaneYs[straightIndex])
                        + snapshot.Tolerance
                        < snapshot.RequiredSeparation)
                    {
                        return IndependentJointLaneStaggerValidationResult
                            .Unsupported(
                                IndependentJointLaneStaggerFailure
                                    .InsufficientStraightLaneSeparation,
                                "A bent anchor is too close to a "
                                + "straight-through anchor.");
                    }
                }
            }

            for (int firstIndex = 0;
                firstIndex < shiftedBentLaneYs.Count;
                firstIndex++)
            {
                for (int secondIndex = firstIndex + 1;
                    secondIndex < shiftedBentLaneYs.Count;
                    secondIndex++)
                {
                    if (Math.Abs(
                            shiftedBentLaneYs[firstIndex]
                            - shiftedBentLaneYs[secondIndex])
                        + snapshot.Tolerance
                        < snapshot.RequiredSeparation)
                    {
                        return IndependentJointLaneStaggerValidationResult
                            .Unsupported(
                                IndependentJointLaneStaggerFailure
                                    .InsufficientBentLaneSeparation,
                                "Two bent anchors are too close to each "
                                + "other.");
                    }
                }
            }

            return IndependentJointLaneStaggerValidationResult.Valid();
        }

        private static IndependentJointLaneStaggerResult?
            ValidateAndSnapshotInput(
                IndependentJointLaneStaggerInput? input,
                out InputSnapshot? snapshot)
        {
            snapshot = null;
            if (input == null
                || input.OriginalBentLaneYs == null
                || input.StraightThroughLaneYs == null
                || input.OriginalBentLaneYs.Count == 0)
            {
                return IndependentJointLaneStaggerResult.Unsupported(
                    IndependentJointLaneStaggerFailure.MissingInput,
                    "At least one bent lane and a straight-lane collection "
                    + "are required.");
            }

            var bentLanes = input.OriginalBentLaneYs.ToArray();
            var straightLanes = input.StraightThroughLaneYs.ToArray();
            if (!IsFinite(input.MinAllowedY)
                || !IsFinite(input.MaxAllowedY)
                || !IsFinite(input.RequiredCenterlineSeparation)
                || !IsFinite(input.PreferredShiftDirection)
                || !IsFinite(input.Tolerance)
                || bentLanes.Any(laneY => !IsFinite(laneY))
                || straightLanes.Any(laneY => !IsFinite(laneY)))
            {
                return IndependentJointLaneStaggerResult.Unsupported(
                    IndependentJointLaneStaggerFailure.NonFiniteValue,
                    "All lane coordinates, bounds, separation and tolerance "
                    + "must be finite.");
            }
            if (input.Tolerance <= 0.0)
            {
                return IndependentJointLaneStaggerResult.Unsupported(
                    IndependentJointLaneStaggerFailure.InvalidTolerance,
                    "Tolerance must be positive.");
            }
            if (input.MaxAllowedY - input.MinAllowedY
                <= input.Tolerance)
            {
                return IndependentJointLaneStaggerResult.Unsupported(
                    IndependentJointLaneStaggerFailure.InvalidBounds,
                    "The maximum usable Y must be greater than the minimum "
                    + "usable Y.");
            }
            if (input.RequiredCenterlineSeparation <= input.Tolerance)
            {
                return IndependentJointLaneStaggerResult.Unsupported(
                    IndependentJointLaneStaggerFailure
                        .InvalidRequiredSeparation,
                    "Required centerline separation must exceed tolerance.");
            }
            if (Math.Abs(
                    Math.Abs(input.PreferredShiftDirection) - 1.0)
                > input.Tolerance)
            {
                return IndependentJointLaneStaggerResult.Unsupported(
                    IndependentJointLaneStaggerFailure
                        .InvalidPreferredShiftDirection,
                    "Preferred shift direction must be +1 or -1.");
            }
            if (bentLanes.Any(
                    laneY =>
                        laneY < input.MinAllowedY - input.Tolerance
                        || laneY >
                            input.MaxAllowedY + input.Tolerance))
            {
                return IndependentJointLaneStaggerResult.Unsupported(
                    IndependentJointLaneStaggerFailure
                        .OriginalLaneOutsideBounds,
                    "Every original bent lane must lie inside the usable "
                    + "centerline bounds.");
            }

            double[] orderedOriginals = bentLanes
                .OrderBy(laneY => laneY)
                .ToArray();
            for (int index = 1;
                index < orderedOriginals.Length;
                index++)
            {
                if (orderedOriginals[index]
                        - orderedOriginals[index - 1]
                    <= input.Tolerance)
                {
                    return IndependentJointLaneStaggerResult.Unsupported(
                        IndependentJointLaneStaggerFailure
                            .DuplicateOriginalLane,
                        "Original bent lanes must be unique within "
                        + "tolerance.");
                }
            }

            snapshot = new InputSnapshot(
                bentLanes,
                straightLanes,
                input.MinAllowedY,
                input.MaxAllowedY,
                input.RequiredCenterlineSeparation,
                Math.Sign(input.PreferredShiftDirection),
                input.Tolerance);
            return null;
        }

        private static List<double> CreateCandidates(
            InputSnapshot snapshot,
            int laneCount,
            double numericalEpsilon)
        {
            var roots = new List<double>
            {
                snapshot.MinAllowedY,
                snapshot.MaxAllowedY
            };
            roots.AddRange(snapshot.BentLaneYs);
            foreach (double straightLaneY in snapshot.StraightLaneYs)
            {
                roots.Add(
                    straightLaneY - snapshot.RequiredSeparation);
                roots.Add(
                    straightLaneY + snapshot.RequiredSeparation);
            }

            var candidates = new List<double>();
            foreach (double root in roots)
            {
                for (int step = -laneCount;
                    step <= laneCount;
                    step++)
                {
                    double candidate =
                        root
                        + step * snapshot.RequiredSeparation;
                    if (candidate
                            < snapshot.MinAllowedY - numericalEpsilon
                        || candidate
                            > snapshot.MaxAllowedY + numericalEpsilon)
                    {
                        continue;
                    }
                    candidate = Math.Max(
                        snapshot.MinAllowedY,
                        Math.Min(snapshot.MaxAllowedY, candidate));
                    if (snapshot.StraightLaneYs.All(
                            straightLaneY =>
                                Math.Abs(candidate - straightLaneY)
                                + numericalEpsilon
                                >= snapshot.RequiredSeparation))
                    {
                        candidates.Add(candidate);
                    }
                }
            }

            candidates.Sort();
            var uniqueCandidates = new List<double>();
            foreach (double candidate in candidates)
            {
                if (uniqueCandidates.Count == 0
                    || Math.Abs(
                            candidate
                            - uniqueCandidates[
                                uniqueCandidates.Count - 1])
                        > numericalEpsilon)
                {
                    uniqueCandidates.Add(candidate);
                }
            }
            return uniqueCandidates;
        }

        private static double NumericalEpsilon(double tolerance)
        {
            return Math.Max(1e-12, tolerance * 1e-6);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private sealed class IndexedLane
        {
            public double Y { get; }
            public int OriginalIndex { get; }

            public IndexedLane(double y, int originalIndex)
            {
                Y = y;
                OriginalIndex = originalIndex;
            }
        }

        private sealed class PlacementState
        {
            public double Cost { get; }
            public double PreferenceScore { get; }
            public int PreviousCandidateIndex { get; }
            public bool IsAmbiguous { get; }

            public PlacementState(
                double cost,
                double preferenceScore,
                int previousCandidateIndex,
                bool isAmbiguous)
            {
                Cost = cost;
                PreferenceScore = preferenceScore;
                PreviousCandidateIndex = previousCandidateIndex;
                IsAmbiguous = isAmbiguous;
            }
        }

        private sealed class InputSnapshot
        {
            public double[] BentLaneYs { get; }
            public double[] StraightLaneYs { get; }
            public double MinAllowedY { get; }
            public double MaxAllowedY { get; }
            public double RequiredSeparation { get; }
            public double PreferredDirection { get; }
            public double Tolerance { get; }

            public InputSnapshot(
                double[] bentLaneYs,
                double[] straightLaneYs,
                double minAllowedY,
                double maxAllowedY,
                double requiredSeparation,
                double preferredDirection,
                double tolerance)
            {
                BentLaneYs = bentLaneYs;
                StraightLaneYs = straightLaneYs;
                MinAllowedY = minAllowedY;
                MaxAllowedY = maxAllowedY;
                RequiredSeparation = requiredSeparation;
                PreferredDirection = preferredDirection;
                Tolerance = tolerance;
            }
        }
    }
}
