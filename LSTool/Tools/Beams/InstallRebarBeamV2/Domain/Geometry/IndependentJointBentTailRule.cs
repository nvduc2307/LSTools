using System;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Geometry
{
    public enum IndependentJointBentTailPolicy
    {
        FullAnchorage = 0,
        LongestStraightThenHMin = 1
    }

    /// <summary>
    /// Chooses the vertical tail for an independent joint anchor. Full
    /// anchorage remains preferred whenever it fits. When it does not fit,
    /// the horizontal leg stays as long as the geometry permits and the
    /// vertical tail falls back to hMin diameters from General Setting.
    /// Concrete-envelope validation remains the geometry kernel's job.
    /// </summary>
    public sealed class IndependentJointBentTailPlan
    {
        public IndependentJointBentTailPolicy Policy { get; }
        public double RequiredBentTailLength { get; }
        public bool UsesHMinFallback =>
            Policy
                == IndependentJointBentTailPolicy
                    .LongestStraightThenHMin;

        internal IndependentJointBentTailPlan(
            IndependentJointBentTailPolicy policy,
            double requiredBentTailLength)
        {
            Policy = policy;
            RequiredBentTailLength = requiredBentTailLength;
        }
    }

    public static class IndependentJointBentTailRule
    {
        public static IndependentJointBentTailPlan Resolve(
            double fullAnchorageLength,
            double nominalBarDiameter,
            double hMinDiameterMultiplier,
            double availableVerticalLength,
            double tolerance)
        {
            EnsurePositiveFinite(
                fullAnchorageLength,
                nameof(fullAnchorageLength));
            EnsurePositiveFinite(
                nominalBarDiameter,
                nameof(nominalBarDiameter));
            EnsurePositiveFinite(
                hMinDiameterMultiplier,
                nameof(hMinDiameterMultiplier));
            EnsureFinite(
                availableVerticalLength,
                nameof(availableVerticalLength));
            if (double.IsNaN(tolerance)
                || double.IsInfinity(tolerance)
                || tolerance < 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(tolerance),
                    "Tolerance must be finite and non-negative.");
            }

            if (availableVerticalLength + tolerance
                >= fullAnchorageLength)
            {
                return new IndependentJointBentTailPlan(
                    IndependentJointBentTailPolicy.FullAnchorage,
                    fullAnchorageLength);
            }

            double hMinLength =
                hMinDiameterMultiplier * nominalBarDiameter;
            EnsurePositiveFinite(hMinLength, "hMinLength");
            return new IndependentJointBentTailPlan(
                IndependentJointBentTailPolicy
                    .LongestStraightThenHMin,
                hMinLength);
        }

        private static void EnsurePositiveFinite(
            double value,
            string parameterName)
        {
            EnsureFinite(value, parameterName);
            if (value <= 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    "A positive value is required.");
            }
        }

        private static void EnsureFinite(
            double value,
            string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    "A finite value is required.");
            }
        }
    }
}
