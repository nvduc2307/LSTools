using System;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Geometry
{
    public static class RebarCenterlineBendRadiusRule
    {
        public static double Resolve(
            double standardBendDiameter,
            double modelBarDiameter,
            double nominalBarDiameter)
        {
            var effectiveBarDiameter = IsPositiveFinite(modelBarDiameter)
                ? modelBarDiameter
                : nominalBarDiameter;
            if (!IsPositiveFinite(standardBendDiameter))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(standardBendDiameter),
                    standardBendDiameter,
                    "Standard bend diameter must be positive and finite.");
            }
            if (!IsPositiveFinite(effectiveBarDiameter))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(modelBarDiameter),
                    modelBarDiameter,
                    "Model or nominal bar diameter must be positive and finite.");
            }

            return standardBendDiameter / 2.0
                   + effectiveBarDiameter / 2.0;
        }

        private static bool IsPositiveFinite(double value)
        {
            return !double.IsNaN(value)
                   && !double.IsInfinity(value)
                   && value > 0.0;
        }
    }
}
