using System;
using System.Collections.Generic;
using System.Linq;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Geometry
{
    public enum MainBarTransitionPolicy
    {
        LegacyAligned = 0,
        BentZContinuous = 1,
        IndependentAnchorage35D = 2
    }

    public enum MainBarTransitionClassificationFailure
    {
        None = 0,
        MissingLaneDelta = 1,
        InvalidValue = 2,
        InvalidThreshold = 3,
        InconsistentLanePolicy = 4
    }

    public sealed class MainBarTransitionClassification
    {
        public bool IsValid { get; }
        public MainBarTransitionPolicy Policy { get; }
        public MainBarTransitionClassificationFailure Failure { get; }
        public string Message { get; }
        public double MinimumDeltaZ { get; }
        public double MaximumDeltaZ { get; }

        private MainBarTransitionClassification(
            bool isValid,
            MainBarTransitionPolicy policy,
            MainBarTransitionClassificationFailure failure,
            string message,
            double minimumDeltaZ,
            double maximumDeltaZ)
        {
            IsValid = isValid;
            Policy = policy;
            Failure = failure;
            Message = message;
            MinimumDeltaZ = minimumDeltaZ;
            MaximumDeltaZ = maximumDeltaZ;
        }

        internal static MainBarTransitionClassification Valid(
            MainBarTransitionPolicy policy,
            double minimumDeltaZ,
            double maximumDeltaZ)
        {
            return new MainBarTransitionClassification(
                true,
                policy,
                MainBarTransitionClassificationFailure.None,
                string.Empty,
                minimumDeltaZ,
                maximumDeltaZ);
        }

        internal static MainBarTransitionClassification Unsupported(
            MainBarTransitionClassificationFailure failure,
            string message,
            double minimumDeltaZ = 0.0,
            double maximumDeltaZ = 0.0)
        {
            return new MainBarTransitionClassification(
                false,
                MainBarTransitionPolicy.LegacyAligned,
                failure,
                message,
                minimumDeltaZ,
                maximumDeltaZ);
        }
    }

    /// <summary>
    /// Classifies a complete main-bar lane set from centerline elevation
    /// differences only. Physical top/bottom face alignment is deliberately
    /// not part of this policy.
    /// </summary>
    public static class MainBarTransitionPolicyClassifier
    {
        public static MainBarTransitionClassification Classify(
            IReadOnlyList<double> signedLaneDeltaZValues,
            double alignmentTolerance,
            double maximumBentZDelta)
        {
            if (signedLaneDeltaZValues == null
                || signedLaneDeltaZValues.Count == 0)
            {
                return MainBarTransitionClassification.Unsupported(
                    MainBarTransitionClassificationFailure.MissingLaneDelta,
                    "At least one lane elevation difference is required.");
            }
            if (!IsFinite(alignmentTolerance)
                || !IsFinite(maximumBentZDelta)
                || alignmentTolerance < 0.0
                || maximumBentZDelta <= alignmentTolerance)
            {
                return MainBarTransitionClassification.Unsupported(
                    MainBarTransitionClassificationFailure.InvalidThreshold,
                    "The Bent/Z threshold must be finite and greater than the "
                    + "alignment tolerance.");
            }
            if (signedLaneDeltaZValues.Any(value => !IsFinite(value)))
            {
                return MainBarTransitionClassification.Unsupported(
                    MainBarTransitionClassificationFailure.InvalidValue,
                    "Every lane elevation difference must be finite.");
            }

            var absoluteDeltas = signedLaneDeltaZValues
                .Select(Math.Abs)
                .ToList();
            var minimumDelta = absoluteDeltas.Min();
            var maximumDelta = absoluteDeltas.Max();
            var numericalScale = Math.Max(
                1.0,
                Math.Max(maximumDelta, maximumBentZDelta));
            var numericalEpsilon = numericalScale * 1e-12;
            var lanePolicies = absoluteDeltas
                .Select(delta => ClassifyOne(
                    delta,
                    alignmentTolerance,
                    maximumBentZDelta,
                    numericalEpsilon))
                .Distinct()
                .ToList();
            if (lanePolicies.Count != 1)
            {
                return MainBarTransitionClassification.Unsupported(
                    MainBarTransitionClassificationFailure
                        .InconsistentLanePolicy,
                    "The lane elevation differences cross a transition-policy "
                    + "boundary and cannot be planned as one bar group.",
                    minimumDelta,
                    maximumDelta);
            }

            return MainBarTransitionClassification.Valid(
                lanePolicies[0],
                minimumDelta,
                maximumDelta);
        }

        private static MainBarTransitionPolicy ClassifyOne(
            double absoluteDelta,
            double alignmentTolerance,
            double maximumBentZDelta,
            double numericalEpsilon)
        {
            if (absoluteDelta <= alignmentTolerance + numericalEpsilon)
                return MainBarTransitionPolicy.LegacyAligned;
            if (absoluteDelta <= maximumBentZDelta + numericalEpsilon)
                return MainBarTransitionPolicy.BentZContinuous;
            return MainBarTransitionPolicy.IndependentAnchorage35D;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
