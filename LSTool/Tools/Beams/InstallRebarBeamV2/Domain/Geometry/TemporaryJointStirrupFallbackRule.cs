using System;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Geometry
{
    /// <summary>
    /// Temporary rollout rule for projects whose joint column does not yet
    /// contain modeled reinforcement metadata. A positive modeled value always
    /// wins; the configured beam value is used only when the modeled value is
    /// unavailable. Keeping this decision isolated makes it straightforward to
    /// replace with an explicit UI/project rule later.
    /// </summary>
    public static class TemporaryJointStirrupFallbackRule
    {
        public static TemporaryJointStirrupSelection Resolve(
            double modeledValue,
            double configuredBeamFallback,
            string valueName)
        {
            if (IsPositiveFinite(modeledValue))
            {
                return new TemporaryJointStirrupSelection(
                    modeledValue,
                    false);
            }

            if (IsPositiveFinite(configuredBeamFallback))
            {
                return new TemporaryJointStirrupSelection(
                    configuredBeamFallback,
                    true);
            }

            throw new ArgumentOutOfRangeException(
                nameof(configuredBeamFallback),
                $"A positive finite modeled or configured {valueName} is required.");
        }

        private static bool IsPositiveFinite(double value)
        {
            return !double.IsNaN(value)
                && !double.IsInfinity(value)
                && value > 0.0;
        }
    }

    public readonly struct TemporaryJointStirrupSelection
    {
        public double Value { get; }
        public bool UsedConfiguredBeamFallback { get; }

        public TemporaryJointStirrupSelection(
            double value,
            bool usedConfiguredBeamFallback)
        {
            Value = value;
            UsedConfiguredBeamFallback =
                usedConfiguredBeamFallback;
        }
    }
}
