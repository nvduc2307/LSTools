using System;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Geometry
{
    [Flags]
    public enum BentZColumnEnvelopeViolation
    {
        None = 0,
        InvalidInput = 1,
        EmptyEnvelope = 2,
        MinimumY = 4,
        MaximumY = 8,
        MinimumZ = 16,
        MaximumZ = 32
    }

    public sealed class BentZColumnEnvelopeResult
    {
        public BentZColumnEnvelopeViolation Violations { get; }
        public double AllowedMinimumY { get; }
        public double AllowedMaximumY { get; }
        public double AllowedMinimumZ { get; }
        public double AllowedMaximumZ { get; }
        public double MinimumYMargin { get; }
        public double MaximumYMargin { get; }
        public double MinimumZMargin { get; }
        public double MaximumZMargin { get; }
        public double Tolerance { get; }

        public bool Fits =>
            Violations == BentZColumnEnvelopeViolation.None;

        internal BentZColumnEnvelopeResult(
            BentZColumnEnvelopeViolation violations,
            double allowedMinimumY,
            double allowedMaximumY,
            double allowedMinimumZ,
            double allowedMaximumZ,
            double minimumYMargin,
            double maximumYMargin,
            double minimumZMargin,
            double maximumZMargin,
            double tolerance)
        {
            Violations = violations;
            AllowedMinimumY = allowedMinimumY;
            AllowedMaximumY = allowedMaximumY;
            AllowedMinimumZ = allowedMinimumZ;
            AllowedMaximumZ = allowedMaximumZ;
            MinimumYMargin = minimumYMargin;
            MaximumYMargin = maximumYMargin;
            MinimumZMargin = minimumZMargin;
            MaximumZMargin = maximumZMargin;
            Tolerance = tolerance;
        }
    }

    /// <summary>
    /// Checks a Bent/Z centerline against the cover-reduced column envelope.
    /// The caller supplies a deliberately small numerical tolerance. A negative
    /// margin whose magnitude does not exceed that tolerance is accepted.
    /// </summary>
    public static class BentZColumnEnvelopeRule
    {
        public static BentZColumnEnvelopeResult Evaluate(
            double laneY,
            double runMinimumZ,
            double runMaximumZ,
            double columnMinimumY,
            double columnMaximumY,
            double columnMinimumZ,
            double columnMaximumZ,
            double centerlineClearance,
            double tolerance)
        {
            if (!AreFinite(
                    laneY,
                    runMinimumZ,
                    runMaximumZ,
                    columnMinimumY,
                    columnMaximumY,
                    columnMinimumZ,
                    columnMaximumZ,
                    centerlineClearance,
                    tolerance)
                || centerlineClearance < 0.0
                || tolerance < 0.0
                || runMaximumZ < runMinimumZ)
            {
                return Invalid(
                    BentZColumnEnvelopeViolation.InvalidInput,
                    tolerance);
            }

            var allowedMinimumY =
                columnMinimumY + centerlineClearance;
            var allowedMaximumY =
                columnMaximumY - centerlineClearance;
            var allowedMinimumZ =
                columnMinimumZ + centerlineClearance;
            var allowedMaximumZ =
                columnMaximumZ - centerlineClearance;
            if (allowedMaximumY < allowedMinimumY
                || allowedMaximumZ < allowedMinimumZ)
            {
                return new BentZColumnEnvelopeResult(
                    BentZColumnEnvelopeViolation.EmptyEnvelope,
                    allowedMinimumY,
                    allowedMaximumY,
                    allowedMinimumZ,
                    allowedMaximumZ,
                    double.NaN,
                    double.NaN,
                    double.NaN,
                    double.NaN,
                    tolerance);
            }

            var minimumYMargin = laneY - allowedMinimumY;
            var maximumYMargin = allowedMaximumY - laneY;
            var minimumZMargin = runMinimumZ - allowedMinimumZ;
            var maximumZMargin = allowedMaximumZ - runMaximumZ;
            var numericalScale = Math.Max(
                1.0,
                Math.Max(
                    Math.Max(
                        Math.Abs(allowedMinimumY),
                        Math.Abs(allowedMaximumY)),
                    Math.Max(
                        Math.Abs(allowedMinimumZ),
                        Math.Abs(allowedMaximumZ))));
            var comparisonThreshold =
                -tolerance - numericalScale * 1e-12;
            var violations = BentZColumnEnvelopeViolation.None;
            if (minimumYMargin < comparisonThreshold)
                violations |= BentZColumnEnvelopeViolation.MinimumY;
            if (maximumYMargin < comparisonThreshold)
                violations |= BentZColumnEnvelopeViolation.MaximumY;
            if (minimumZMargin < comparisonThreshold)
                violations |= BentZColumnEnvelopeViolation.MinimumZ;
            if (maximumZMargin < comparisonThreshold)
                violations |= BentZColumnEnvelopeViolation.MaximumZ;

            return new BentZColumnEnvelopeResult(
                violations,
                allowedMinimumY,
                allowedMaximumY,
                allowedMinimumZ,
                allowedMaximumZ,
                minimumYMargin,
                maximumYMargin,
                minimumZMargin,
                maximumZMargin,
                tolerance);
        }

        private static BentZColumnEnvelopeResult Invalid(
            BentZColumnEnvelopeViolation violation,
            double tolerance)
        {
            return new BentZColumnEnvelopeResult(
                violation,
                double.NaN,
                double.NaN,
                double.NaN,
                double.NaN,
                double.NaN,
                double.NaN,
                double.NaN,
                double.NaN,
                tolerance);
        }

        private static bool AreFinite(params double[] values)
        {
            foreach (var value in values)
            {
                if (double.IsNaN(value) || double.IsInfinity(value))
                    return false;
            }
            return true;
        }
    }
}
