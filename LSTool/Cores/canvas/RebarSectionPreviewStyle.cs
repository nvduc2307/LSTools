namespace LSTool.Cores.canvas
{
    internal static class RebarSectionPreviewStyle
    {
        public const double MinimumDiameter = 4.0;
        public const double MaximumDiameter = 6.0;
        private const double OccupiedSpacingRatio = 0.75;

        public static double GetDiameterLimit(double span, int gridQuantity)
        {
            return Math.Max(0, span) / (1 + (Math.Max(2, gridQuantity) - 1) / OccupiedSpacingRatio);
        }

        public static double GetDiameter(double diameterMm, double pixelsPerMm, double limit)
        {
            return Math.Max(0, Math.Min(limit,
                Math.Min(MaximumDiameter, Math.Max(MinimumDiameter, diameterMm * pixelsPerMm))));
        }
    }
}
