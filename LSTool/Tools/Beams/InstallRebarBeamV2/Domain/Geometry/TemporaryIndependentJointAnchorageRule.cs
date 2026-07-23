using System;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Geometry
{
    /// <summary>
    /// Temporary project rule approved for the first Independent Joint
    /// Anchorage rollout. Keeping the multiplier behind this boundary allows
    /// a later UI/configuration value to replace it without changing the
    /// geometry planner.
    /// </summary>
    public static class TemporaryIndependentJointAnchorageRule
    {
        public const double DiameterMultiplier = 35.0;

        public static double GetRequiredLength(
            double nominalBarDiameter)
        {
            if (double.IsNaN(nominalBarDiameter)
                || double.IsInfinity(nominalBarDiameter)
                || nominalBarDiameter <= 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(nominalBarDiameter),
                    "A positive finite nominal bar diameter is required.");
            }

            return DiameterMultiplier * nominalBarDiameter;
        }
    }
}
