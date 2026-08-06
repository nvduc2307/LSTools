using System;
using System.Linq;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Geometry
{
    public enum RectangularColumnFallbackFailure
    {
        None = 0,
        NotSplitEdgeFailure = 1,
        CurrentSolidCountInvalid = 2,
        OriginalGeometryUnsupported = 3,
        InvalidMeasurement = 4,
        CurrentVolumeMismatch = 5,
        EnvelopeMismatch = 6
    }

    public sealed class RectangularColumnFallbackResult
    {
        public bool IsAllowed =>
            Failure == RectangularColumnFallbackFailure.None;
        public RectangularColumnFallbackFailure Failure { get; }
        public string Message { get; }

        internal RectangularColumnFallbackResult(
            RectangularColumnFallbackFailure failure,
            string message)
        {
            Failure = failure;
            Message = message;
        }
    }

    /// <summary>
    /// Allows a joined/cut Revit column to use its original rectangular
    /// geometry only when the current solid is still the same axis-aligned
    /// box and merely has split topology edges. Cuts, voids, rotations and
    /// changed envelopes remain unsupported.
    /// </summary>
    public static class RectangularColumnPostProcessingFallbackRule
    {
        public static RectangularColumnFallbackResult Evaluate(
            bool failedOnlyBecauseOfSplitEdges,
            int currentSolidCount,
            double currentSolidVolumeCubicFt,
            double currentExpectedBoxVolumeCubicFt,
            double currentSizeXmm,
            double currentSizeYmm,
            double currentHeightMm,
            bool originalGeometrySupported,
            double originalSizeXmm,
            double originalSizeYmm,
            double originalHeightMm,
            double dimensionToleranceMm,
            double relativeVolumeTolerance,
            double minimumVolumeToleranceCubicFt)
        {
            if (!failedOnlyBecauseOfSplitEdges)
            {
                return Failed(
                    RectangularColumnFallbackFailure.NotSplitEdgeFailure,
                    "The current geometry did not fail only because its "
                    + "box edges were split by Revit post-processing.");
            }
            if (currentSolidCount != 1)
            {
                return Failed(
                    RectangularColumnFallbackFailure
                        .CurrentSolidCountInvalid,
                    "The current geometry must contain exactly one solid.");
            }
            if (!originalGeometrySupported)
            {
                return Failed(
                    RectangularColumnFallbackFailure
                        .OriginalGeometryUnsupported,
                    "The original family geometry is not a supported "
                    + "axis-aligned rectangular column.");
            }
            if (!ArePositiveFinite(
                    currentSolidVolumeCubicFt,
                    currentExpectedBoxVolumeCubicFt,
                    currentSizeXmm,
                    currentSizeYmm,
                    currentHeightMm,
                    originalSizeXmm,
                    originalSizeYmm,
                    originalHeightMm,
                    dimensionToleranceMm,
                    relativeVolumeTolerance,
                    minimumVolumeToleranceCubicFt))
            {
                return Failed(
                    RectangularColumnFallbackFailure.InvalidMeasurement,
                    "All geometry measurements and tolerances must be "
                    + "positive and finite.");
            }

            var volumeTolerance = Math.Max(
                minimumVolumeToleranceCubicFt,
                currentExpectedBoxVolumeCubicFt
                * relativeVolumeTolerance);
            if (Math.Abs(
                    currentSolidVolumeCubicFt
                    - currentExpectedBoxVolumeCubicFt)
                > volumeTolerance)
            {
                return Failed(
                    RectangularColumnFallbackFailure.CurrentVolumeMismatch,
                    "The current solid does not fill its projected "
                    + "rectangular box; a cut or void may be present.");
            }

            if (Math.Abs(currentSizeXmm - originalSizeXmm)
                    > dimensionToleranceMm
                || Math.Abs(currentSizeYmm - originalSizeYmm)
                    > dimensionToleranceMm
                || Math.Abs(currentHeightMm - originalHeightMm)
                    > dimensionToleranceMm)
            {
                return Failed(
                    RectangularColumnFallbackFailure.EnvelopeMismatch,
                    "The current and original column envelopes differ.");
            }

            return new RectangularColumnFallbackResult(
                RectangularColumnFallbackFailure.None,
                "The current solid is the same rectangular box as the "
                + "original family geometry; only its topology edges were "
                + "split by Revit post-processing.");
        }

        private static RectangularColumnFallbackResult Failed(
            RectangularColumnFallbackFailure failure,
            string message)
        {
            return new RectangularColumnFallbackResult(failure, message);
        }

        private static bool ArePositiveFinite(params double[] values)
        {
            return values != null
                && values.Length > 0
                && values.All(value =>
                    !double.IsNaN(value)
                    && !double.IsInfinity(value)
                    && value > 0.0);
        }
    }
}
