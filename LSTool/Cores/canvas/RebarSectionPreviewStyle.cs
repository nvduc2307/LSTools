namespace LSTool.Cores.canvas
{
    /// <summary>Kích thước chấm thép dùng chung cho mặt cắt dầm và cột (px).</summary>
    internal static class RebarSectionPreviewStyle
    {
        public const double MinimumDiameter = 4.0;
        public const double MaximumDiameter = 6.0;
        private const double OccupiedSpacingRatio = 0.75;

        /// <summary>
        /// Span là bề rộng khả dụng trước khi trừ đường kính chấm.
        /// Dành ít nhất 25% khoảng cách tâm làm khoảng hở giữa các chấm.
        /// </summary>
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
