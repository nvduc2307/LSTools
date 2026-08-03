using System;
using System.Collections.Generic;
using System.Linq;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Geometry
{
    public enum IndependentJointDualLaneStaggerStatus
    {
        Planned = 0,
        Unsupported = 1
    }

    public enum IndependentJointDualLaneStaggerFailure
    {
        None = 0,
        MissingInput = 1,
        CountMismatch = 2,
        InvalidValue = 3,
        NoFeasibleLayout = 4,
        InvalidOutput = 5
    }

    /// <summary>
    /// Inputs for the fail-safe second-stage lane layout. Both run families
    /// are allowed to move, but every resulting centerline remains inside the
    /// same configured cage bounds and all six centerlines are interleaved at
    /// the requested separation.
    /// </summary>
    public sealed class IndependentJointDualLaneStaggerInput
    {
        public IReadOnlyList<double>? OriginalBentLaneYs { get; }
        public IReadOnlyList<double>? OriginalStraightLaneYs { get; }
        public double MinAllowedY { get; }
        public double MaxAllowedY { get; }
        public double RequiredCenterlineSeparation { get; }
        public double PreferredBentDirection { get; }
        public double Tolerance { get; }

        public IndependentJointDualLaneStaggerInput(
            IReadOnlyList<double>? originalBentLaneYs,
            IReadOnlyList<double>? originalStraightLaneYs,
            double minAllowedY,
            double maxAllowedY,
            double requiredCenterlineSeparation,
            double preferredBentDirection,
            double tolerance)
        {
            OriginalBentLaneYs = originalBentLaneYs;
            OriginalStraightLaneYs = originalStraightLaneYs;
            MinAllowedY = minAllowedY;
            MaxAllowedY = maxAllowedY;
            RequiredCenterlineSeparation =
                requiredCenterlineSeparation;
            PreferredBentDirection = preferredBentDirection;
            Tolerance = tolerance;
        }
    }

    public sealed class IndependentJointDualLaneStaggerResult
    {
        private static readonly IReadOnlyList<double> NoLanes =
            Array.AsReadOnly(new double[0]);

        public IndependentJointDualLaneStaggerStatus Status { get; }
        public IndependentJointDualLaneStaggerFailure Failure { get; }
        public string Message { get; }
        public IReadOnlyList<double> ShiftedBentLaneYs { get; }
        public IReadOnlyList<double> ShiftedStraightLaneYs { get; }
        public double TotalAbsoluteDisplacement { get; }
        public double MaximumBentDisplacement { get; }
        public double MaximumStraightDisplacement { get; }

        private IndependentJointDualLaneStaggerResult(
            IndependentJointDualLaneStaggerStatus status,
            IndependentJointDualLaneStaggerFailure failure,
            string message,
            IReadOnlyList<double> shiftedBentLaneYs,
            IReadOnlyList<double> shiftedStraightLaneYs,
            double totalAbsoluteDisplacement,
            double maximumBentDisplacement,
            double maximumStraightDisplacement)
        {
            Status = status;
            Failure = failure;
            Message = message;
            ShiftedBentLaneYs = shiftedBentLaneYs;
            ShiftedStraightLaneYs = shiftedStraightLaneYs;
            TotalAbsoluteDisplacement = totalAbsoluteDisplacement;
            MaximumBentDisplacement = maximumBentDisplacement;
            MaximumStraightDisplacement = maximumStraightDisplacement;
        }

        internal static IndependentJointDualLaneStaggerResult Unsupported(
            IndependentJointDualLaneStaggerFailure failure,
            string message)
        {
            return new IndependentJointDualLaneStaggerResult(
                IndependentJointDualLaneStaggerStatus.Unsupported,
                failure,
                message,
                NoLanes,
                NoLanes,
                0.0,
                0.0,
                0.0);
        }

        internal static IndependentJointDualLaneStaggerResult Planned(
            double[] shiftedBentLaneYs,
            double[] shiftedStraightLaneYs,
            double totalAbsoluteDisplacement,
            double maximumBentDisplacement,
            double maximumStraightDisplacement)
        {
            return new IndependentJointDualLaneStaggerResult(
                IndependentJointDualLaneStaggerStatus.Planned,
                IndependentJointDualLaneStaggerFailure.None,
                string.Empty,
                Array.AsReadOnly(shiftedBentLaneYs),
                Array.AsReadOnly(shiftedStraightLaneYs),
                totalAbsoluteDisplacement,
                maximumBentDisplacement,
                maximumStraightDisplacement);
        }
    }

    /// <summary>
    /// Second-stage optimizer used only when fixed straight lanes leave no
    /// feasible positions for all bent tails. Corresponding straight/bent
    /// lanes are interleaved in a stable physical direction, then the existing
    /// minimum-displacement solver places the combined centerlines. No cover
    /// or separation constraint is relaxed.
    /// </summary>
    public static class IndependentJointDualLaneStaggerGeometry
    {
        public static IndependentJointDualLaneStaggerResult Plan(
            IndependentJointDualLaneStaggerInput input)
        {
            IndependentJointDualLaneStaggerResult? failure =
                ValidateInput(input);
            if (failure != null)
            {
                return failure;
            }

            double[] bent = input.OriginalBentLaneYs!.ToArray();
            double[] straight = input.OriginalStraightLaneYs!.ToArray();
            var orderedBent = bent
                .Select((value, index) => new IndexedLane(value, index))
                .OrderBy(item => item.Value)
                .ThenBy(item => item.Index)
                .ToArray();
            var orderedStraight = straight
                .Select((value, index) => new IndexedLane(value, index))
                .OrderBy(item => item.Value)
                .ThenBy(item => item.Index)
                .ToArray();

            var combinedOriginals = new List<double>(bent.Length * 2);
            var mapping = new List<LaneMapping>(bent.Length * 2);
            bool bentOnPositiveSide = input.PreferredBentDirection > 0.0;
            for (int index = 0; index < orderedBent.Length; index++)
            {
                if (bentOnPositiveSide)
                {
                    Add(
                        combinedOriginals,
                        mapping,
                        orderedStraight[index],
                        false);
                    Add(
                        combinedOriginals,
                        mapping,
                        orderedBent[index],
                        true);
                }
                else
                {
                    Add(
                        combinedOriginals,
                        mapping,
                        orderedBent[index],
                        true);
                    Add(
                        combinedOriginals,
                        mapping,
                        orderedStraight[index],
                        false);
                }
            }

            var combinedInput = new IndependentJointLaneStaggerInput(
                combinedOriginals,
                Array.AsReadOnly(new double[0]),
                input.MinAllowedY,
                input.MaxAllowedY,
                input.RequiredCenterlineSeparation,
                input.PreferredBentDirection,
                input.Tolerance,
                true,
                true);
            IndependentJointLaneStaggerResult combinedPlan =
                IndependentJointLaneStaggerGeometry.Plan(combinedInput);
            if (combinedPlan.Status
                != IndependentJointLaneStaggerStatus.Planned)
            {
                return IndependentJointDualLaneStaggerResult.Unsupported(
                    IndependentJointDualLaneStaggerFailure.NoFeasibleLayout,
                    "Balanced straight/bent interleaving is unavailable: "
                    + combinedPlan.Message);
            }

            var shiftedBent = new double[bent.Length];
            var shiftedStraight = new double[straight.Length];
            for (int index = 0; index < mapping.Count; index++)
            {
                LaneMapping lane = mapping[index];
                if (lane.IsBent)
                    shiftedBent[lane.OriginalIndex] =
                        combinedPlan.ShiftedBentLaneYs[index];
                else
                    shiftedStraight[lane.OriginalIndex] =
                        combinedPlan.ShiftedBentLaneYs[index];
            }

            if (!IsValidOutput(input, shiftedBent, shiftedStraight))
            {
                return IndependentJointDualLaneStaggerResult.Unsupported(
                    IndependentJointDualLaneStaggerFailure.InvalidOutput,
                    "Balanced interleaving did not satisfy every cover and "
                    + "centerline-separation constraint.");
            }

            double maximumBent = MaximumDisplacement(bent, shiftedBent);
            double maximumStraight = MaximumDisplacement(
                straight,
                shiftedStraight);
            return IndependentJointDualLaneStaggerResult.Planned(
                shiftedBent,
                shiftedStraight,
                combinedPlan.TotalAbsoluteDisplacement,
                maximumBent,
                maximumStraight);
        }

        private static IndependentJointDualLaneStaggerResult? ValidateInput(
            IndependentJointDualLaneStaggerInput? input)
        {
            if (input == null
                || input.OriginalBentLaneYs == null
                || input.OriginalStraightLaneYs == null
                || input.OriginalBentLaneYs.Count == 0)
            {
                return IndependentJointDualLaneStaggerResult.Unsupported(
                    IndependentJointDualLaneStaggerFailure.MissingInput,
                    "Bent and straight lane collections are required.");
            }
            if (input.OriginalBentLaneYs.Count
                != input.OriginalStraightLaneYs.Count)
            {
                return IndependentJointDualLaneStaggerResult.Unsupported(
                    IndependentJointDualLaneStaggerFailure.CountMismatch,
                    "Bent and straight lane counts must match for balanced "
                    + "interleaving.");
            }
            double[] laneValues = input.OriginalBentLaneYs
                .Concat(input.OriginalStraightLaneYs)
                .ToArray();
            double[] scalarValues =
            {
                input.MinAllowedY,
                input.MaxAllowedY,
                input.RequiredCenterlineSeparation,
                input.PreferredBentDirection,
                input.Tolerance
            };
            if (laneValues.Concat(scalarValues).Any(value =>
                    double.IsNaN(value)
                    || double.IsInfinity(value))
                || input.Tolerance <= 0.0
                || input.RequiredCenterlineSeparation <= input.Tolerance
                || input.MaxAllowedY - input.MinAllowedY <= input.Tolerance
                || Math.Abs(
                    Math.Abs(input.PreferredBentDirection) - 1.0)
                    > input.Tolerance
                || laneValues.Any(value =>
                        value < input.MinAllowedY - input.Tolerance
                        || value > input.MaxAllowedY + input.Tolerance))
            {
                return IndependentJointDualLaneStaggerResult.Unsupported(
                    IndependentJointDualLaneStaggerFailure.InvalidValue,
                    "Lane values, bounds, direction, separation and "
                    + "tolerance must define a finite usable layout.");
            }
            return null;
        }

        private static bool IsValidOutput(
            IndependentJointDualLaneStaggerInput input,
            IReadOnlyList<double> bent,
            IReadOnlyList<double> straight)
        {
            var all = bent.Concat(straight).ToArray();
            if (all.Any(value =>
                    value < input.MinAllowedY - input.Tolerance
                    || value > input.MaxAllowedY + input.Tolerance))
            {
                return false;
            }
            for (int first = 0; first < all.Length; first++)
            {
                for (int second = first + 1;
                    second < all.Length;
                    second++)
                {
                    if (Math.Abs(all[first] - all[second])
                        + input.Tolerance
                        < input.RequiredCenterlineSeparation)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static void Add(
            ICollection<double> values,
            ICollection<LaneMapping> mapping,
            IndexedLane lane,
            bool isBent)
        {
            values.Add(lane.Value);
            mapping.Add(new LaneMapping(lane.Index, isBent));
        }

        private static double MaximumDisplacement(
            IReadOnlyList<double> original,
            IReadOnlyList<double> shifted)
        {
            double maximum = 0.0;
            for (int index = 0; index < original.Count; index++)
            {
                maximum = Math.Max(
                    maximum,
                    Math.Abs(shifted[index] - original[index]));
            }
            return maximum;
        }

        private sealed class IndexedLane
        {
            public double Value { get; }
            public int Index { get; }

            public IndexedLane(double value, int index)
            {
                Value = value;
                Index = index;
            }
        }

        private sealed class LaneMapping
        {
            public int OriginalIndex { get; }
            public bool IsBent { get; }

            public LaneMapping(int originalIndex, bool isBent)
            {
                OriginalIndex = originalIndex;
                IsBent = isBent;
            }
        }
    }
}
